using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Net.Http;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Events.Client;
using TwitchLib.Models.Client;
using TwitchLib.Services;
using VainBotTwitch.Classes;

namespace VainBotTwitch
{
    class Program
    {
        static TwitchClient client;
        static HttpClient httpClient = new HttpClient();
        static Random rng = new Random();
        static Regex validZip = new Regex(@"^[0-9]{5}$");
        static string openWeatherMapApiKey;

        static void Main(string[] args) => new Program().Run();

        public void Run()
        {
            var username = Environment.GetEnvironmentVariable("VB_TWITCH_USERNAME");
            if (username == null)
                username = ConfigurationManager.AppSettings["twitchUsername"];

            var oauth = Environment.GetEnvironmentVariable("VB_TWITCH_OAUTH");
            if (oauth == null)
                oauth = ConfigurationManager.AppSettings["twitchOauth"];

            openWeatherMapApiKey = Environment.GetEnvironmentVariable("VB_OPENWEATHERMAP_API_KEY");
            if (openWeatherMapApiKey == null)
                openWeatherMapApiKey = ConfigurationManager.AppSettings["openWeatherMapApiKey"];

            if (username == null)
                throw new ArgumentNullException(nameof(username), "No Twitch username found");

            if (oauth == null)
                throw new ArgumentNullException(nameof(oauth), "No Twitch OAuth token found");

            if (openWeatherMapApiKey == null)
                throw new ArgumentNullException(nameof(openWeatherMapApiKey), "No OpenWeatherMap API key found");

            client = new TwitchClient(new ConnectionCredentials(username, oauth), "crendor");

            client.AddChatCommandIdentifier('!');

            client.OnChatCommandReceived += CommandHandler;

            var throttler = new MessageThrottler(2, new TimeSpan(0, 0, 5));
            client.ChatThrottler = throttler;

            client.Connect();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        async void CommandHandler(object sender, OnChatCommandReceivedArgs e)
        {
            if (e.Command.ChatMessage.BotUsername == e.Command.ChatMessage.Username)
                return;

            var command = e.Command.Command.ToLower();

            if (e.Command.ArgumentsAsList.Count == 0)
            {
                switch (command)
                {
                    case "slothies":
                    case "slothy":
                        await GetSlothies(sender, e);
                        break;

                    case "slothfact":
                    case "slothfacts":
                        SlothFacts(sender, e);
                        break;

                    case "woppy":
                    case "weather":
                        await WoppyWeather(sender, e);
                        break;
                }

                return;
            }
        }

        async Task GetSlothies(object sender, OnChatCommandReceivedArgs e)
        {
            var channel = GetChannel(e);
            var username = e.Command.ChatMessage.Username.ToLower();
            var count = 0M;

            using (var db = new VbContext())
            {
                var record = await db.Slothies.FirstOrDefaultAsync(s => s.Username == username);
                if (record != null)
                    count = record.Count;
            }

            client.SendMessage(channel, $"{e.Command.ChatMessage.DisplayName} has {count.ToDisplayString()}.");
        }

        void SlothFacts(object sender, OnChatCommandReceivedArgs e)
        {
            var i = rng.Next(0, _slothFacts.Count);
            var channel = GetChannel(e);

            client.SendMessage(channel, _slothFacts[i]);
        }

        async Task WoppyWeather(object sender, OnChatCommandReceivedArgs e)
        {
            var channel = GetChannel(e);

            if (!validZip.IsMatch(e.Command.ArgumentsAsString))
            {
                client.SendMessage(channel, "That's not a valid US zip code. Try again!");
                return;
            }

            var response = await httpClient
                .GetAsync($"http://api.openweathermap.org/data/2.5/weather?zip={e.Command.ArgumentsAsString},us&APPID={openWeatherMapApiKey}");
            var respString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Weather error, code " + response.StatusCode.ToString());
                Console.WriteLine("Weather content: " + respString);

                client.SendMessage(channel, "Error getting the weather. IT'S THEIR FAULT, NOT MINE!");
                return;
            }

            var weather = JsonConvert.DeserializeObject<OpenWeatherMapResponse>(respString);

            var temp = (int)Math.Round(((9 / 5) * (weather.Main.Temperature - 273)) + 32);

            client.SendMessage(channel, $"WOPPY ACTIVATED! Weather for {e.Command.ArgumentsAsString}: " +
                $"{weather.Weather[0].Description}, {temp}° F");
        }

        static List<string> _slothFacts = new List<string>
        {
            "Sloths can sometimes maintain their grasp on limbs after death.",
            "Both two-toed and three-toed sloths grow to 1.5 to 2 feet long.",
            "The digestion process can take as long as a month to complete for an adult sloth!",
            "Sloths can rotate their heads around 270 degrees!",
            "Sloths have very slow metabolisms for creatures their size. This is why they can survive with leaves as their main source of food.",
            "Sloths are only about 25% muscle. They can't shiver if they get too cold!",
            "As much as 2/3 of a well-fed sloth's weight can be contained within its stomach chambers.",
            "Sloths mate and give birth while hanging in trees!",
            "Three-toed sloths have short stubby tails while two-toed sloths don't.",
            "To help conserve energy, a sloth's internal body temperature is only 30-34° C. It can drop even lower while they sleep.",
            "A sloth's laziness is actually a great method of survival. Its slow movement and camouflage helps it evade detection from natural predators.",
            "Sloths are sturdy! They are usually unharmed from falls.",
            "A sloth's maximum weight is about 40 pounds. Nearly two-thirds of this weight can be contained in the sloth's stomach compartments if it is well-fed.",
            "Three-toed sloths use their short tail to dig a hole and bury their poops! 💩",
            "The sloth can tolerate the largest change in body temperature of any mammal, from 74 to 92° F!",
            "Sloths only have one baby at a time.",
            "The outer hairs on a sloth actually grow in the opposite direction compared to other mammals.",
            "Sloths' internal organs are fixed to their ribcage. This prevents their lungs from being compressed while hanging upside down!",
            "There are two different families of sloths: Megalonychidae (two-toed) and Bradypodidae (three-toed).",
            "Sloths are the world's slowest-digesting mammal, only defecating once a week!",
            "Up until about 10,000 years ago, several species of ground sloths existed, such as Megatherium. This species grew to about the size of an elephant!",
            "Sloths are actually excellent swimmers! While in water they can slow their heart rate down to one-third its average pace. They can also move about three times faster in water with their version of the doggy paddle!",
            "The sloth is the world's slowest mammal!",
            "Sloths don't sweat and don't emit body odor. This helps avoid predation.",
            "Three-toed sloths have a maximum land speed of about 2 meters per minute!",
            "Sloths are excellent survivors. Of the five species of sloth, only one is currently endangered: the Maned Three-Toed Sloth.",
            "Healthy sloths generally live from 10 to 16 years in the wild. In captivity they can live to be over 30!",
            "A sloth can hold its breath for up to 40 minutes while in water!",
            "Despite the name, two-toed sloths actually have three toes. They only have two fingers though and they generally move a bit quicker than three-toed sloths.",
            "Sloths sometimes fatally mistake powerlines for trees. BibleThump",
            "Sloths tend to prefer the leaves of the Cecropia tree, sometimes known as pumpwoods."
        };

        JoinedChannel GetChannel(OnChatCommandReceivedArgs e)
        {
            return client.GetJoinedChannel(e.Command.ChatMessage.Channel);
        }
    }
}
