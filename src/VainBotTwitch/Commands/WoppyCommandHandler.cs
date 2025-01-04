﻿using System;
using System.Net;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using VainBotTwitch.Classes;

namespace VainBotTwitch.Commands
{
    public class WoppyCommandHandler
    {
        private readonly BotConfig _config;
        private readonly TwitchClient _client;
        private readonly HttpClient _httpClient = new HttpClient();
        private readonly Regex _validZip = new Regex("^[0-9]{5}$");

        public WoppyCommandHandler(BotConfig config, TwitchClient client)
        {
            _config = config;
            _client = client;
        }

        public async Task HandleCommandAsync(OnChatCommandReceivedArgs e)
        {
            await GetWoppyWeatherAsync(e);
        }

        private async Task GetWoppyWeatherAsync(OnChatCommandReceivedArgs e)
        {
            if (e.Command.ArgumentsAsList.Count > 0
                && string.Equals(e.Command.ArgumentsAsList[0], "help", StringComparison.OrdinalIgnoreCase))
            {
                _client.SendMessage(e, "Woppy the weather bot can get your current weather. Use !woppy followed by a US zip code, " +
                    "for example !woppy 90210. Only works in the US.");

                return;
            }

            if (!_validZip.IsMatch(e.Command.ArgumentsAsString))
            {
                _client.SendMessage(e, "That's not a valid US zip code. Try again!");
                return;
            }

            var response = await _httpClient
                .GetAsync("http://api.openweathermap.org/data/2.5/weather?" +
                    $"zip={e.Command.ArgumentsAsString},us&APPID={_config.OpenWeatherMapApiKey}");

            var respString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                if (response.StatusCode == HttpStatusCode.NotFound)
                {
                    _client.SendMessage(e, "That zip code doesn't exist. Try again!");
                }
                else
                {
                    await Utils.SendDiscordErrorWebhookAsync(
                    $"{_config.DiscordWebhookUserPing}: Error getting weather with Twitch bot.", _config.DiscordWebhookUrl);

                    _client.SendMessage(e, "Error getting the weather, sorry!");
                }

                return;
            }

            var weather = JsonSerializer.Deserialize<OpenWeatherMapResponse>(respString);

            var temp = (int)Math.Round(((weather.Main.Temperature * 9) / 5) - 459.67m, 1);

            _client.SendMessage(e, $"WOPPY ACTIVATED! Weather for {e.Command.ArgumentsAsString}: " +
                $"{weather.Weather[0].Description}, {temp}° F");
        }
    }
}
