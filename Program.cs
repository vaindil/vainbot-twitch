using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
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

            var clientId = Environment.GetEnvironmentVariable("VB_TWITCH_CLIENT_ID");
            if (clientId == null)
                clientId = ConfigurationManager.AppSettings["twitchClientId"];

            openWeatherMapApiKey = Environment.GetEnvironmentVariable("VB_OPENWEATHERMAP_API_KEY");
            if (openWeatherMapApiKey == null)
                openWeatherMapApiKey = ConfigurationManager.AppSettings["openWeatherMapApiKey"];

            if (username == null)
                throw new ArgumentNullException(nameof(username), "No Twitch username found");

            if (oauth == null)
                throw new ArgumentNullException(nameof(oauth), "No Twitch OAuth token found");

            if (clientId == null)
                throw new ArgumentNullException(nameof(clientId), "No Twitch client ID found");

            if (openWeatherMapApiKey == null)
                throw new ArgumentNullException(nameof(openWeatherMapApiKey), "No OpenWeatherMap API key found");

            client = new TwitchClient(new ConnectionCredentials(username, oauth), "crendor");
            TwitchApi.SetClientId(clientId);

            client.AddChatCommandIdentifier('!');

            client.OnConnected += ConnectedLog;
            client.OnDisconnected += DisconnectedLog;
            client.OnConnectionError += ConnectionErrorLog;

            client.OnChatCommandReceived += CommandHandler;

            var throttler = new MessageThrottler(2, new TimeSpan(0, 0, 5));
            client.ChatThrottler = throttler;

            client.Connect();

            while (true)
            {
                Thread.Sleep(1000);
            }
        }

        async void ConnectedLog(object sender, OnConnectedArgs e)
        {
            var entry = new LogEntry($"Connected. Username: {e.Username} | Joined: {e.AutoJoinChannel}");

            await LogToDb(entry);
        }

        async void DisconnectedLog(object sender, OnDisconnectedArgs e)
        {
            var entry = new LogEntry($"Disconnected. Username: {e.Username}");

            await LogToDb(entry);
        }

        async void ConnectionErrorLog(object sender, OnConnectionErrorArgs e)
        {
            var entry = new LogEntry($"Connection error. Username: {e.Username}");

            await LogToDb(entry);
        }

        async void CommandHandler(object sender, OnChatCommandReceivedArgs e)
        {
            if (e.Command.ChatMessage.BotUsername == e.Command.ChatMessage.Username)
                return;

            var command = e.Command.Command.ToLower();
            var argCount = e.Command.ArgumentsAsList.Count;
            
            switch (command)
            {
                case "slothfact":
                case "slothfacts":
                    SlothFacts(sender, e);
                    return;

                case "woppy":
                case "weather":
                    await WoppyWeather(sender, e);
                    return;
            }
            
            if (command == "slothy" || command == "slothies")
            {
                if (argCount == 0)
                {
                    await GetSlothies(sender, e);
                    return;
                }

                if (argCount == 2)
                {
                    await UpdateSlothies(sender, e);
                    return;
                }

                client.SendMessage(GetChannel(e), $"That's not a valid slothies command, you nerd. {RandEmote()}");
                return;
            }
            
            if (command == "multi" || command == "multitwitch")
            {
                if (argCount == 0 || !e.Command.ChatMessage.IsModerator)
                {
                    await GetMultitwitch(sender, e);
                    return;
                }

                if (argCount != 0 && e.Command.ChatMessage.IsModerator)
                {
                    await UpdateMultitwitch(sender, e);
                    return;
                }
            }
        }

        async Task GetMultitwitch(object sender, OnChatCommandReceivedArgs e)
        {
            List<string> streamers;

            using (var db = new VbContext())
            {
                streamers = await db.MultiStreamers.Select(s => s.Username).ToListAsync();
            }

            if (streamers.Count == 0)
            {
                client.SendMessage(GetChannel(e), $"The nerd isn't playing with any other nerds. {RandEmote()}");
                return;
            }

            var url = "http://multistre.am/crendor/";

            foreach (var s in streamers)
            {
                url += s + "/";
            }

            client.SendMessage(GetChannel(e), $"Watch ALL of the nerds! " + url + $" {RandEmote()}");
        }

        async Task UpdateMultitwitch(object sender, OnChatCommandReceivedArgs e)
        {
            if (e.Command.ArgumentsAsList.Count == 1 && e.Command.ArgumentsAsList[0].ToLower() == "clear")
            {
                using (var db = new VbContext())
                {
                    db.MultiStreamers.RemoveRange(db.MultiStreamers);
                    await db.SaveChangesAsync();
                }

                client.SendMessage(GetChannel(e), $"The nerd isn't playing with any other nerds. {RandEmote()}");
                return;
            }

            var validUsernames = await TwitchApi.Users.GetUsersV5Async(e.Command.ArgumentsAsList);
            if (validUsernames.Count != e.Command.ArgumentsAsList.Count)
            {
                client.SendMessage(GetChannel(e),
                    $"At least one of those isn't a valid user, you nerd. {RandEmote()}");
            }

            using (var db = new VbContext())
            {
                db.MultiStreamers.RemoveRange(db.MultiStreamers);
                await db.SaveChangesAsync();
            }

            using (var db = new VbContext())
            {
                foreach (var u in e.Command.ArgumentsAsList)
                {
                    db.MultiStreamers.Add(new MultiStreamer(u));
                }

                await db.SaveChangesAsync();
            }

            await GetMultitwitch(sender, e);
        }

        async Task GetSlothies(object sender, OnChatCommandReceivedArgs e)
        {
            var channel = GetChannel(e);
            var count = 0M;

            using (var db = new VbContext())
            {
                var record = await db.Slothies.FindAsync(e.Command.ChatMessage.UserId);
                if (record != null)
                    count = record.Count;
            }

            client.SendMessage(
                channel, $"{e.Command.ChatMessage.DisplayName} has {count.ToDisplayString()}. {RandEmote()}");
        }

        async Task UpdateSlothies(object sender, OnChatCommandReceivedArgs e)
        {
            var channel = GetChannel(e);

            var username = e.Command.ArgumentsAsList[0].ToLower().TrimStart('@');
            if (username.Length >= 200)
            {
                client.SendMessage(channel, $"That's not a valid user, you nerd. {RandEmote()}");
                return;
            }

            var usernameList = new List<string> { username };
            var users = await TwitchApi.Users.GetUsersV5Async(usernameList);
            if (users.Count != 1)
            {
                client.SendMessage(channel, $"That's not a valid user, you nerd. {RandEmote()}");
                return;
            }

            var userId = users[0].Id.ToString();

            if (userId == e.Command.ChatMessage.UserId)
            {
                client.SendMessage(channel, $"You can't change your own slothies, you nerd. {RandEmote()}");
                return;
            }

            if (userId == "45447900")
            {
                client.SendMessage(channel, $"vaindil's slothies can't be edited, you nerd. {RandEmote()}");
                return;
            }

            var validDecimal = decimal.TryParse(e.Command.ArgumentsAsList[1], out var count);
            if (!validDecimal)
            {
                client.SendMessage(channel, $"That's not a valid number, you nerd. {RandEmote()}");
                return;
            }

            count = Math.Round(count, 2);

            if (!e.Command.ChatMessage.IsModerator)
            {
                client.SendMessage(channel, $"You're not a mod, you nerd. {RandEmote()}");
                return;
            }

            using (var db = new VbContext())
            {
                var record = await db.Slothies.FindAsync(userId);
                if (record == null)
                {
                    var newRecord = new SlothyRecord
                    {
                        UserId = userId,
                        Count = count
                    };

                    db.Slothies.Add(newRecord);
                }
                else
                {
                    record.Count += count;
                    count = record.Count;

                    if (count == 0)
                    {
                        db.Slothies.Remove(record);
                    }
                }

                await db.SaveChangesAsync();
            }

            client.SendMessage(channel, $"{username} now has {count.ToDisplayString()}. {RandEmote()}");
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
                client.SendMessage(channel, $"That's not a valid US zip code. Try again! {RandEmote()}");
                return;
            }

            var response = await httpClient
                .GetAsync($"http://api.openweathermap.org/data/2.5/weather?zip={e.Command.ArgumentsAsString},us&APPID={openWeatherMapApiKey}");
            var respString = await response.Content.ReadAsStringAsync();

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine("Weather error, code " + response.StatusCode.ToString());
                Console.WriteLine("Weather content: " + respString);

                client.SendMessage(channel, $"Error getting the weather. IT'S THEIR FAULT, NOT MINE! {RandEmote()}");
                return;
            }

            var weather = JsonConvert.DeserializeObject<OpenWeatherMapResponse>(respString);

            var temp = (int)Math.Round(((9 / 5) * (weather.Main.Temperature - 273)) + 32);

            client.SendMessage(channel, $"WOPPY ACTIVATED! Weather for {e.Command.ArgumentsAsString}: " +
                $"{weather.Weather[0].Description}, {temp}° F {RandEmote()}");
        }

        async Task LogToDb(LogEntry entry)
        {
            using (var db = new VbContext())
            {
                db.LogEntries.Add(entry);
                await db.SaveChangesAsync();
            }
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

        static List<string> _emotes = new List<string>
        {
            "4Head",
            "BabyRage",
            "BCWarrior",
            "BloodTrail",
            "CoolCat",
            "CorgiDerp",
            "CurseLit",
            "DansGame",
            "EleGiggle",
            "FailFish",
            "FrankerZ",
            "GivePLZ",
            "HeyGuys",
            "Jebaited",
            "Kappa",
            "KappaPride",
            "KappaRoss",
            "Keepo",
            "Kreygasm",
            "MingLee",
            "MrDestructoid",
            "OhMyDog",
            "OSsloth",
            "PogChamp",
            "ResidentSleeper",
            "SMOrc",
            "StinkyCheese",
            "SwiftRage",
            "TakeNRG",
            "TheIlliuminati",
            "VoHiYo",
            "WutFace"
        };

        JoinedChannel GetChannel(OnChatCommandReceivedArgs e)
        {
            return client.GetJoinedChannel(e.Command.ChatMessage.Channel);
        }

        string RandEmote()
        {
            var r = rng.Next(0, _emotes.Count);
            return _emotes[r];
        }
    }
}
