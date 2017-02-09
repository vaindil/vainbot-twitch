using Newtonsoft.Json;
using System;
using System.Configuration;
using System.Net.Http;
using System.Threading;
using TwitchLib;
using TwitchLib.Events.Client;
using TwitchLib.Models.Client;
using VainBotTwitch.Classes;

namespace VainBotTwitch
{
    class Program
    {
        static TwitchClient client;
        static HttpClient httpClient = new HttpClient();

        static void Main(string[] args) => new Program().Run();

        public void Run()
        {
            var username = ConfigurationManager.AppSettings["twitchUsername"];
            var oauth = ConfigurationManager.AppSettings["twitchOauth"];

            client = new TwitchClient(new ConnectionCredentials(username, oauth), "crendor");

            client.AddChatCommandIdentifier('!');
            client.OnChatCommandReceived += wowToken;

            client.Connect();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }
        
        async void wowToken(object sender, OnChatCommandReceivedArgs e)
        {
            if (e.Command.Command.ToLower() != "token")
                return;

            var channel = client.GetJoinedChannel(e.Command.ChatMessage.Channel);

            var result = await httpClient.GetAsync("https://wowtoken.info/snapshot.json");
            if (!result.IsSuccessStatusCode)
            {
                client.SendMessage(channel, "Couldn't get the WoW token info. Sorry!");
                return;
            }

            var resultString = await result.Content.ReadAsStringAsync();
            var tokenResponse = JsonConvert.DeserializeObject<WowTokenResponse>(resultString);

            var naPrice = tokenResponse.Na.Formatted.Buy;
            var euPrice = tokenResponse.Eu.Formatted.Buy;

            client.SendMessage(channel, "NA: " + naPrice + " | EU: " + euPrice);
        }
    }
}
