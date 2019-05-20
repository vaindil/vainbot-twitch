using Newtonsoft.Json;

namespace VainBotTwitch.Classes
{
    public class TwitchSubCountResponse
    {
        [JsonProperty(PropertyName = "score")]
        public int Score { get; set; }

        [JsonProperty(PropertyName = "next_level")]
        public TwitchSubCountNextLevel NextLevel { get; set; }
    }

    public class TwitchSubCountNextLevel
    {
        [JsonProperty(PropertyName = "minimum_score")]
        public int MinimumScore { get; set; }
    }
}
