using System;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace VainBotTwitch
{
    public static class Utils
    {
        static readonly Random rng = new Random();

        public static JoinedChannel GetChannel(this OnChatCommandReceivedArgs e, TwitchClient client)
        {
            return client.GetJoinedChannel(e.Command.ChatMessage.Channel);
        }

        public static string ToDisplayString(this decimal count)
        {
            var display = count.GetNumberString() + " ";

            if (count == 1)
                display += "slothy";
            else
                display += "slothies";

            return display;
        }

        static string GetNumberString(this decimal num)
        {
            if ((int)num == num)
                return ((int)num).ToString();
            else if (num == 3.14M)
                return "π";
            else
                return num.ToString();
        }

        public static string RandEmote()
        {
            var r = rng.Next(0, _emotes.Count);
            return _emotes[r];
        }

        static readonly List<string> _emotes = new List<string>
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
            "OhMyDog"
            "PogChamp",
            "ResidentSleeper",,
            "SeriousSloth",
            "SMOrc",
            "StinkyCheese",
            "SwiftRage",
            "TakeNRG",
            "TheIlluminati",
            "VoHiYo",
            "WutFace"
        };
    }
}
