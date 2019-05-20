using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;
using VainBotTwitch.Classes;

namespace VainBotTwitch
{
    public class SubPointsHandler
    {
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient = new HttpClient();

        private readonly TwitchPubSub _pubSub;

#pragma warning disable IDE0052 // Remove unread private members
        private readonly Timer _manualUpdateTimer;
        private readonly Timer _pubSubReconnectTimer;
#pragma warning restore IDE0052 // Remove unread private members

        private int _currentPoints;

        public SubPointsHandler(IConfiguration config)
        {
            _config = config;

            _pubSub = new TwitchPubSub();
            _pubSub.OnPubSubServiceConnected += PubSubConnected;
            _pubSub.OnPubSubServiceClosed += PubSubClosed;
            _pubSub.OnPubSubServiceError += PubSubClosed;
            _pubSub.OnChannelSubscription += OnChannelSubscription;

            _manualUpdateTimer = new Timer(async _ => await ManualUpdateAsync(), null, TimeSpan.Zero, TimeSpan.FromMinutes(10));
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
            _pubSub.ListenToSubscriptions("7555574");
            _pubSub.SendTopics(_config["subPointsAccessToken"]);
            LogToConsole("PubSub connected and topics sent");
        }

        private void PubSubClosed(object sender, EventArgs e)
        {
            _pubSub.Connect();
        }

        private async void OnChannelSubscription(object sender, OnChannelSubscriptionArgs e)
        {
            //var oldScore = _currentPoints;

            //switch (e.Subscription.SubscriptionPlan)
            //{
            //    case SubscriptionPlan.Prime:
            //    case SubscriptionPlan.Tier1:
            //        _currentPoints++;
            //        break;

            //    case SubscriptionPlan.Tier2:
            //        _currentPoints += 2;
            //        break;

            //    case SubscriptionPlan.Tier3:
            //        _currentPoints += 6;
            //        break;
            //}

            //LogToConsole($"New sub from {e.Subscription.Username}, tier: {e.Subscription.SubscriptionPlan} | " +
            //    $"Old count: {oldScore} | New count: {_currentPoints}");

            await Task.Delay(10000);
            await ManualUpdateAsync();
        }

        public async Task ManualUpdateAsync()
        {
            await GetCurrentPointsAsync();
            await UpdateRemoteCountAsync();
        }

        private async Task GetCurrentPointsAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Get, "https://api.twitch.tv/api/channels/crendor/subscriber_count");
            request.Headers.Authorization = new AuthenticationHeaderValue("OAuth", _config["subPointsAccessToken"]);

            var response = await _httpClient.SendAsync(request);
            var counts = JsonConvert.DeserializeObject<TwitchSubCountResponse>(await response.Content.ReadAsStringAsync());

            LogToConsole($"Points manually updated. Old score {_currentPoints} | new score {counts.Score}");

            _currentPoints = counts.Score;
        }

        private async Task UpdateRemoteCountAsync()
        {
            var request = new HttpRequestMessage(HttpMethod.Put, $"https://ws.vaindil.xyz/crendor/points/{_currentPoints}");
            request.Headers.Authorization = new AuthenticationHeaderValue(_config["subPointsApiSecret"]);

            await _httpClient.SendAsync(request);
        }

        private void LogToConsole(string message)
        {
            Console.WriteLine($"{DateTimeOffset.UtcNow:yyyy-MM-dd HH:mm:ss.fff}: {message}");
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
