using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;
using VainBotTwitch.Classes;
using VainBotTwitch.Services;

namespace VainBotTwitch.PubSubHandlers
{
    public class SubPointsHandler
    {
        private readonly BotConfig _config;
        private readonly TwitchClient _client;
        private readonly TwitchPubSub _pubSub;
        private readonly TwitchAPI _api;
        private readonly SlothyService _slothySvc;

        private readonly HttpClient _httpClient = new HttpClient();
        private readonly Random _rng = new Random();

        private List<TwitchSubscriber> _currentSubs = new List<TwitchSubscriber>();

#pragma warning disable IDE0052 // Remove unread private members
        private Timer _manualUpdateTimer;
        private Timer _batchSubUpdateTimer;
        private Timer _giftedSubBatchTimer;
        private Timer _tokenRefreshTimer;
#pragma warning restore IDE0052 // Remove unread private members

        private readonly List<GiftedSubBatch> _giftedSubBatches = new List<GiftedSubBatch>();
        private bool _areGiftSubBatchesBeingProcessed;

        private int _previousPoints;
        private int _currentPoints;

        public SubPointsHandler(BotConfig config, TwitchClient client, TwitchPubSub pubSub, TwitchAPI api, SlothyService slothySvc)
        {
            _config = config;
            _client = client;
            _pubSub = pubSub;
            _api = api;
            _slothySvc = slothySvc;
        }

        public async Task InitializeAsync()
        {
            var raw = await KeyValueService.GetByKeyAsync("SubPointsTokens");
            if (!string.IsNullOrEmpty(raw?.Value))
            {
                var split = raw.Value.Split("|||||");
                _config.SubPointsRefreshToken = split[1];
            }

            await RefreshTokenAsync();

            _pubSub.OnChannelSubscription += OnChannelSubscription;

            if (_config.TrackSubPoints)
            {
                _manualUpdateTimer = new Timer(async _ => await UpdateSubPointsFromApiAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
                _batchSubUpdateTimer = new Timer(async _ => await HandleSubBatchAsync(), null, TimeSpan.Zero, TimeSpan.FromSeconds(5));
            }
        }

        private async Task RefreshTokenAsync()
        {
            _api.Settings.AccessToken = null;
            _api.Settings.ClientId = _config.SubPointsClientId;

            var resp = await _api.V5.Auth.RefreshAuthTokenAsync(_config.SubPointsRefreshToken, _config.SubPointsClientSecret);
            _config.SubPointsAccessToken = resp.AccessToken;
            _config.SubPointsRefreshToken = resp.RefreshToken;

            _api.Settings.AccessToken = _config.TwitchOAuth;
            _api.Settings.ClientId = _config.TwitchClientId;

            _tokenRefreshTimer?.Dispose();
            _tokenRefreshTimer = new Timer(async _ => await RefreshTokenAsync(), null, TimeSpan.FromSeconds(resp.ExpiresIn - 600), TimeSpan.FromMilliseconds(-1));

            await KeyValueService.CreateOrUpdateAsync("SubPointsTokens", $"{resp.AccessToken}|||||{resp.RefreshToken}");
        }

        private async void OnChannelSubscription(object sender, OnChannelSubscriptionArgs e)
        {
            if (e.Subscription.Context == "resub"
                && _config.TrackSubPoints
                && _currentSubs.Any(x => x.UserId == e.Subscription.UserId))
            {
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

            if (_config.VerboseSubPointsLogging)
            {
                Utils.LogToConsole($"New sub from {e.Subscription.Username}, tier: {e.Subscription.SubscriptionPlan} | " +
                    $"Old count: {oldScore} | New count: {_currentPoints}");
            }

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

            _giftedSubBatchTimer?.Dispose();
            _giftedSubBatchTimer = new Timer(async _ => await ProcessGiftSubBatchesAsync(), null, 10000, Timeout.Infinite);
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
                Utils.LogToConsole($"Error processing gift sub batch: {ex.Message}");
            }
            finally
            {
                _giftedSubBatches.Clear();

                _giftedSubBatchTimer.Dispose();
                _giftedSubBatchTimer = null;

                _areGiftSubBatchesBeingProcessed = false;
            }
        }

        private async Task HandleSubBatchAsync()
        {
            if (_previousPoints != _currentPoints)
            {
                if (_config.VerboseSubPointsLogging)
                    Utils.LogToConsole($"Previous points: {_previousPoints} | New points: {_currentPoints} | Sending update to WS");

                _previousPoints = _currentPoints;
                await UpdateRemoteCountAsync(true);
            }
        }

        private async Task UpdateRemoteCountAsync(bool hard)
        {
            if (!_config.TrackSubPoints)
                return;

            var method = HttpMethod.Post;
            if (hard)
                method = HttpMethod.Put;

            var request = new HttpRequestMessage(method, $"{_config.SubPointsApiUrl}/{_currentPoints}");
            request.Headers.Authorization = new AuthenticationHeaderValue(_config.SubPointsApiSecret);

            await _httpClient.SendAsync(request);

            if (_config.VerboseSubPointsLogging)
                Utils.LogToConsole("Sub points API update call sent");
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

        private async Task UpdateSubPointsFromApiAsync()
        {
            if (!_config.TrackSubPoints)
                return;

            var tierList = new List<TwitchSubscriber>();
            var data = await CreateAndSendRequestAsync();
            tierList.AddRange(data.Subscribers);

            while (data.Subscribers.Count > 0 && data?.Pagination?.Cursor != null)
            {
                data = await CreateAndSendRequestAsync(data.Pagination.Cursor);
                tierList.AddRange(data.Subscribers);
            }

            lock (_currentSubs)
            {
                _currentSubs = tierList.ToList();
            }

            // broadcaster is considered a subscriber but doesn't count toward sub points
            tierList = tierList.Where(x => x.UserId != "7555574").ToList();

            var t1 = tierList.Where(x => x.Tier == "1000" || x.Tier == "Prime").ToList();
            var t2 = tierList.Where(x => x.Tier == "2000").ToList();
            var t3 = tierList.Where(x => x.Tier == "3000").ToList();

            _currentPoints = t1.Count + (2 * t2.Count) + (6 * t3.Count);

            if (_config.VerboseSubPointsLogging)
                Utils.LogToConsole($"Sub points updated from API. Total: {_currentPoints} | T1: {t1.Count} | T2: {t2.Count} | T3: {t3.Count}");
        }

        private async Task<TwitchSubscribersResponse> CreateAndSendRequestAsync(string cursor = null)
        {
            // endpoint isn't supported by library, so query it manually
            var url = $"https://api.twitch.tv/helix/subscriptions?broadcaster_id={_config.TwitchChannelId}&first=100";
            if (cursor != null)
                url += $"&after={cursor}";

            var request = new HttpRequestMessage(HttpMethod.Get, url);
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _config.SubPointsAccessToken);
            request.Headers.Add("Client-ID", _config.SubPointsClientId);

            var response = await _httpClient.SendAsync(request);
            var body = await response.Content.ReadAsStringAsync();
            if (!response.IsSuccessStatusCode)
            {
                var errorBegin = $"Status code {response.StatusCode} when getting subscribers from API";
                Utils.LogToConsole($"{errorBegin}: {body}");
                await Utils.SendDiscordErrorWebhookAsync($"{_config.DiscordWebhookUserPing}: {errorBegin}", _config.DiscordWebhookUrl);
            }

            return JsonSerializer.Deserialize<TwitchSubscribersResponse>(body);
        }
    }
}
