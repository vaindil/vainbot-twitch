using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VainBotTwitch.Classes
{
    public class TwitchSubscribersResponse
    {
        [JsonPropertyName("data")]
        public List<TwitchSubscriber> Subscribers { get; set; }

        [JsonPropertyName("pagination")]
        public TwitchPagination Pagination { get; set; }
    }

    public class TwitchSubscriber
    {
        [JsonPropertyName("broadcaster_id")]
        public string BroadcasterId { get; set; }

        [JsonPropertyName("broadcaster_name")]
        public string BroadcasterName { get; set; }

        [JsonPropertyName("is_gift")]
        public bool IsGift { get; set; }

        [JsonPropertyName("tier")]
        public string Tier { get; set; }

        [JsonPropertyName("plan_name")]
        public string PlanName { get; set; }

        [JsonPropertyName("user_id")]
        public string UserId { get; set; }

        [JsonPropertyName("user_name")]
        public string Username { get; set; }
    }

    public class TwitchPagination
    {
        [JsonPropertyName("cursor")]
        public string Cursor { get; set; }
    }
}
