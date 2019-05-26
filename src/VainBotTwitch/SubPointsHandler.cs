using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;
using VainBotTwitch.Classes;
using VainBotTwitch.Services;

namespace VainBotTwitch
{
    public class SubPointsHandler
    {
        private readonly BotConfig _config;
        private readonly TwitchClient _client;
        private readonly SlothyService _slothySvc;

        private readonly HttpClient _httpClient = new HttpClient();
        private readonly Random _rng = new Random();

        private readonly TwitchPubSub _pubSub;

#pragma warning disable IDE0052 // Remove unread private members
        private readonly Timer _manualUpdateTimer;
        private readonly Timer _batchSubUpdateTimer;
        private readonly Timer _pubSubReconnectTimer;
        private Timer _giftedSubBatchTimer;
#pragma warning restore IDE0052 // Remove unread private members

        private readonly List<GiftedSubBatch> _giftedSubBatches = new List<GiftedSubBatch>();
        private bool _areGiftSubBatchesBeingProcessed = false;

        private int _previousPoints;
        private int _currentPoints;

        public SubPointsHandler(BotConfig config, TwitchClient client, SlothyService slothySvc)
        {
            _config = config;
            _client = client;
            _slothySvc = slothySvc;

            _pubSub = new TwitchPubSub();
            _pubSub.OnPubSubServiceConnected += PubSubConnected;
            _pubSub.OnPubSubServiceClosed += PubSubClosed;
            _pubSub.OnPubSubServiceError += PubSubClosed;
            _pubSub.OnChannelSubscription += OnChannelSubscription;

            _manualUpdateTimer = new Timer(async _ => await GetCurrentPointsAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
            _batchSubUpdateTimer = new Timer(async _ => await HandleSubBatchAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            _pubSubReconnectTimer = new Timer(_ => ReconnectPubSub(), null, TimeSpan.Zero, TimeSpan.FromHours(18));
        }

        private void ReconnectPubSub()
        {
            _pubSub.OnPubSubServiceClosed -= PubSubClosed;

            try
            {
                _pubSub.Disconnect();
            }
            catch
            {
            }

            _pubSub.Connect();

            _pubSub.OnPubSubServiceClosed += PubSubClosed;
        }

        private void PubSubConnected(object sender, EventArgs e)
        {
            _pubSub.ListenToSubscriptions(_config.TwitchChannelId);
            _pubSub.SendTopics(_config.SubPointsAccessToken);
            LogToConsole("PubSub connected and topics sent");
        }

        private void PubSubClosed(object sender, EventArgs e)
        {
            _pubSub.Connect();
        }

        private async void OnChannelSubscription(object sender, OnChannelSubscriptionArgs e)
        {
            // a resub may or may not count toward current sub points because of the grace period,
            // so it has to be checked manually
            if (e.Subscription.Context == "resub")
            {
                await Task.Delay(10000);
                await GetCurrentPointsAsync();
                return;
            }

            var oldScore = _currentPoints;

            switch (e.Subscription.SubscriptionPlan)
            {
                case SubscriptionPlan.Prime:
                case SubscriptionPlan.Tier1:
                    _currentPoints++;
                    break;

                case SubscriptionPlan.Tier2:
                    _currentPoints += 2;
                    break;

                case SubscriptionPlan.Tier3:
                    _currentPoints += 6;
                    break;
            }

            LogToConsole($"New sub from {e.Subscription.Username}, tier: {e.Subscription.SubscriptionPlan} | " +
                $"Old count: {oldScore} | New count: {_currentPoints}");

            await UpdateRemoteCountAsync();

            if (e.Subscription.Context == "sub")
                await NewSubDieRollAsync(e.Subscription.DisplayName, e.Subscription.UserId);

            if (e.Subscription.Context == "subgift")
                await HandleGiftSubAsync(e.Subscription.DisplayName, e.Subscription.UserId);
        }

        private async Task NewSubDieRollAsync(string displayName, string userId)
        {
            var msg = $"{displayName} just subscribed! ";

            var roll = _rng.Next(1, 21);
            if (roll == 1)
            {
                msg += "You rolled a 1! Have a consolation slothy.";
                await _slothySvc.AddSlothiesAsync(userId, 1);
            }
            else if (roll == 20)
            {
                msg += "You rolled a 20!!! Enjoy your 20 bonus slothies.";
                await _slothySvc.AddSlothiesAsync(userId, 20);
            }
            else if (roll == 8 || roll == 11 || roll == 18)
            {
                msg += $"You rolled an {roll}!";
            }
            else
            {
                msg += $"You rolled a {roll}!";
            }

            _client.SendMessage(_config.TwitchChannel, msg);
        }

        private async Task HandleGiftSubAsync(string displayName, string userId)
        {
            while (_areGiftSubBatchesBeingProcessed)
            {
                await Task.Delay(1000);
            }

            var existing = _giftedSubBatches.Find(x => x.UserId == userId);
            if (existing != null)
                existing.NumSubs++;
            else
                _giftedSubBatches.Add(new GiftedSubBatch(displayName, userId));

            if (_giftedSubBatchTimer != null)
            {
                _giftedSubBatchTimer.Dispose();
                _giftedSubBatchTimer = new Timer(async _ => await ProcessGiftSubBatchesAsync(), null, 10000, Timeout.Infinite);
            }
        }

        private async Task ProcessGiftSubBatchesAsync()
        {
            _areGiftSubBatchesBeingProcessed = true;

            try
            {
                foreach (var batch in _giftedSubBatches)
                {
                    await _slothySvc.AddSlothiesAsync(batch.UserId, batch.NumSubs);

                    string msg;
                    if (batch.NumSubs == 1)
                    {
                        msg = $"Thank you {batch.DisplayName} for your gifted sub! Have a slothy.";
                    }
                    else
                    {
                        msg = $"Thank you {batch.DisplayName} for your {batch.NumSubs} gifted subs! Have some slothies.";
                    }

                    _client.SendMessage(_config.TwitchChannel, msg);
                }
            }
            catch (Exception ex)
            {
                LogToConsole($"Error processing gift sub batch: {ex.Message}");
            }
            finally
            {
                _giftedSubBatches.Clear();

                _giftedSubBatchTimer.Dispose();
                _giftedSubBatchTimer = null;

                _areGiftSubBatchesBeingProcessed = false;
            }
        }

        private async Task GetCurrentPointsAsync()
        {
            var request = new HttpRequestMessage(
                HttpMethod.Get,
                $"https://api.twitch.tv/api/channels/{_config.TwitchChannel}/subscriber_count");
            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", _config.SubPointsAccessToken);

            var response = await _httpClient.SendAsync(request);
            var counts = JsonConvert.DeserializeObject<TwitchSubCountResponse>(await response.Content.ReadAsStringAsync());

            LogToConsole($"Points manually queried from the API. Old score {_currentPoints} | new score {counts.Score}");

            _currentPoints = counts.Score;
        }

        private async Task HandleSubBatchAsync()
        {
            if (_previousPoints != _currentPoints)
            {
                LogToConsole($"Previous points: {_previousPoints} | New points: {_currentPoints} | Sending update to WS");
                _previousPoints = _currentPoints;
                await UpdateRemoteCountAsync();
            }
        }

        private async Task UpdateRemoteCountAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"{_config.SubPointsApiUrl}/{_currentPoints}");
            request.Headers.Authorization = new AuthenticationHeaderValue(_config.SubPointsApiSecret);

            await _httpClient.SendAsync(request);
        }

        private void LogToConsole(string message)
        {
            Console.WriteLine($"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff}: {message}");
        }

        private class GiftedSubBatch
        {
            public GiftedSubBatch(string displayName, string userId)
            {
                DisplayName = displayName;
                UserId = userId;
                NumSubs = 1;
            }

            public string DisplayName { get; set; }

            public string UserId { get; set; }

            public int NumSubs { get; set; }
        }

        /* *****************************
         * I wrote this before I discovered the sub count endpoint, I'm keeping it here because I'll
         * probably need to use it at some point, whenever Twitch decides to kill the old API.

        private async Task UpdateSubPointsFromApiAsync()
        {
            var tierList = new List<TwitchSubscriber>();
            var data = await CreateAndSendRequestAsync();
            tierList.AddRange(data.Data);

            while (data.Data.Count > 0 && data?.Pagination?.Cursor != null)
            {
                data = await CreateAndSendRequestAsync(data.Pagination.Cursor);
                tierList.AddRange(data.Data);
            }

            // broadcaster is considered a subscriber but doesn't count toward sub points
            tierList = tierList.Where(x => x.UserId != "7555574").ToList();

            var t1 = tierList.Where(x => x.Tier == "1000" || x.Tier == "Prime").ToList();
            var t2 = tierList.Where(x => x.Tier == "2000").ToList();
            var t3 = tierList.Where(x => x.Tier == "3000").ToList();

            var onePointCount = tierList.Count(x => x.Tier == "1000" || x.Tier == "Prime");
            var twoPointCount = tierList.Count(x => x.Tier == "2000");
            var sixPointCount = tierList.Count(x => x.Tier == "3000");

            var totalPoints = onePointCount + (2 * twoPointCount) + (6 * sixPointCount);
        }

        private async Task<TwitchSubscribersResponse> CreateAndSendRequestAsync(string cursor = null)
        {
            // endpoint isn't supported by library, so query it manually
            var url = "https://api.twitch.tv/helix/subscriptions?broadcaster_id=7555574&first=100";
            if (cursor != null)
                url += $"&after={cursor}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config["subPointsAccessToken"]);

            var response = await _httpClient.SendAsync(request);
            return JsonConvert.DeserializeObject<TwitchSubscribersResponse>(await response.Content.ReadAsStringAsync());
        }

        ****************************************** */
    }
}
