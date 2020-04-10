using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace VainBotTwitch.Classes
{
    public class OpenWeatherMapResponse
    {
        [JsonPropertyName("coord")]
        public Coordinates Coordinates { get; set; }

        [JsonPropertyName("weather")]
        public List<Weather> Weather { get; set; }

        [JsonPropertyName("base")]
        public string Base { get; set; }

        [JsonPropertyName("main")]
        public WeatherMain Main { get; set; }

        [JsonPropertyName("visibility")]
        public int Visibility { get; set; }

        [JsonPropertyName("wind")]
        public Wind Wind { get; set; }

        [JsonPropertyName("clouds")]
        public Clouds Clouds { get; set; }

        [JsonPropertyName("dt")]
        public long DateTime { get; set; }

        [JsonPropertyName("name")]
        public string Name { get; set; }

        [JsonPropertyName("cod")]
        public int ResponseCode { get; set; }
    }

    public class Coordinates
    {
        [JsonPropertyName("lat")]
        public decimal Latitude { get; set; }

        [JsonPropertyName("lon")]
        public decimal Longitude { get; set; }
    }

    public class Weather
    {
        [JsonPropertyName("id")]
        public long Id { get; set; }

        [JsonPropertyName("main")]
        public string Main { get; set; }

        [JsonPropertyName("description")]
        public string Description { get; set; }

        [JsonPropertyName("icon")]
        public string Icon { get; set; }
    }

    public class WeatherMain
    {
        [JsonPropertyName("temp")]
        public decimal Temperature { get; set; }

        [JsonPropertyName("pressure")]
        public decimal Pressure { get; set; }

        [JsonPropertyName("humidity")]
        public decimal Humidity { get; set; }

        [JsonPropertyName("temp_min")]
        public decimal MinTemp { get; set; }

        [JsonPropertyName("temp_max")]
        public decimal MaxTemp { get; set; }
    }

    public class Wind
    {
        [JsonPropertyName("speed")]
        public decimal Speed { get; set; }

        [JsonPropertyName("deg")]
        public decimal Degrees { get; set; }
    }

    public class Clouds
    {
        [JsonPropertyName("all")]
        public int All { get; set; }
    }
}
