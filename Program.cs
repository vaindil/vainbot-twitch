using System;
using System.Configuration;
using TwitchLib;
using TwitchLib.Events.Client;
using TwitchLib.Models.Client;

namespace VainBotTwitch
{
    class Program
    {
        static TwitchClient client;

        static void Main(string[] args) => new Program().Run();

        public void Run()
        {
            var username = ConfigurationManager.AppSettings["twitchUsername"];
            var oauth = ConfigurationManager.AppSettings["twitchOauth"];

            client = new TwitchClient(new ConnectionCredentials(username, oauth), "crendor");

            client.OnConnected += clientConnected;
            client.OnJoinedChannel += clientJoinedChannel;

            client.Connect();

            Console.ReadLine();
        }

        void clientConnected(object sender, OnConnectedArgs e)
        {
            Console.WriteLine("Connected!");
        }

        void clientJoinedChannel(object sender, OnJoinedChannelArgs e)
        {
            client.SendMessage(client.GetJoinedChannel(e.Channel), "Oh look it works.");
            Console.WriteLine($"Joined channel {e.Channel}");
        }
    }
}
