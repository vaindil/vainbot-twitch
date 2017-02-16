using Newtonsoft.Json;
using System.Collections.Generic;

namespace VainBotTwitch.Classes
{
    public class OpenWeatherMapResponse
    {
        [JsonProperty(PropertyName = "coord")]
        public Coordinates Coordinates { get; set; }

        public List<Weather> Weather { get; set; }

        public string Base { get; set; }

        public WeatherMain Main { get; set; }

        public int Visibility { get; set; }

        public Wind Wind { get; set; }

        public Clouds Clouds { get; set; }

        [JsonProperty(PropertyName = "dt")]
        public long DateTime { get; set; }

        public string Name { get; set; }

        [JsonProperty(PropertyName = "cod")]
        public int ResponseCode { get; set; }
    }

    public class Coordinates
    {
        [JsonProperty(PropertyName = "lat")]
        public decimal Latitude { get; set; }

        [JsonProperty(PropertyName = "lon")]
        public decimal Longitude { get; set; }
    }

    public class Weather
    {
        public long Id { get; set; }

        public string Main { get; set; }

        public string Description { get; set; }

        public string Icon { get; set; }
    }

    public class WeatherMain
    {
        [JsonProperty(PropertyName = "temp")]
        public decimal Temperature { get; set; }

        public int Pressure { get; set; }

        public int Humidity { get; set; }

        [JsonProperty(PropertyName = "temp_min")]
        public decimal MinTemp { get; set; }

        [JsonProperty(PropertyName = "temp_max")]
        public decimal MaxTemp { get; set; }
    }

    public class Wind
    {
        public decimal Speed { get; set; }

        [JsonProperty(PropertyName = "deg")]
        public decimal Degrees { get; set; }
    }

    public class Clouds
    {
        public int All { get; set; }
    }
}
