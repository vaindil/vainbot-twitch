﻿using Newtonsoft.Json;
using System;

namespace VainBotTwitch.Classes
{
    public class WowTokenResponse
    {
        [JsonProperty(PropertyName = "NA")]
        public WowTokenWrapper Na { get; set; }

        [JsonProperty(PropertyName = "EU")]
        public WowTokenWrapper Eu { get; set; }
    }

    public class WowTokenWrapper
    {
        public long Timestamp { get; set; }

        public RawWowToken Raw { get; set; }

        public FormattedWowToken Formatted { get; set; }
    }

    public class RawWowToken
    {
        public long Buy { get; set; }

        [JsonProperty(PropertyName = "24min")]
        public long Min24 { get; set; }

        [JsonProperty(PropertyName = "24max")]
        public long Max24 { get; set; }

        [JsonProperty(PropertyName = "timeToSell")]
        public long TimeToSell { get; set; }

        public int Result { get; set; }

        public long Updated { get; set; }

        [JsonProperty(PropertyName = "updatedISO8601")]
        public DateTime UpdatedIso8601 { get; set; }
    }

    public class FormattedWowToken
    {
        public string Buy { get; set; }

        [JsonProperty(PropertyName = "24min")]
        public string Min24 { get; set; }

        [JsonProperty(PropertyName = "24max")]
        public string Max24 { get; set; }

        [JsonProperty(PropertyName = "24pct")]
        public decimal Pct24 { get; set; }

        [JsonProperty(PropertyName = "timeToSell")]
        public string TimeToSell { get; set; }

        public string Result { get; set; }

        public string Updated { get; set; }

        [JsonProperty(PropertyName = "updatedhtml")]
        public string UpdatedHtml { get; set; }

        [JsonProperty(PropertyName = "sparkurl")]
        public string SparkUrl { get; set; }

        public string Region { get; set; }
    }
}