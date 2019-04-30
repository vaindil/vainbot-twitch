﻿using System;
using System.Collections.Generic;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using VainBotTwitch.Classes;

namespace VainBotTwitch
{
    public static class Utils
    {
        private static readonly Random _rng = new Random();

        public static JoinedChannel GetChannel(this OnChatCommandReceivedArgs e, TwitchClient client)
        {
            return client.GetJoinedChannel(e.Command.ChatMessage.Channel);
        }

        public static void SendMessage(this TwitchClient client, OnChatCommandReceivedArgs e, string message)
        {
            message += $" {RandEmote()}";
            client.SendMessage(e.Command.ChatMessage.Channel, message);
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

        public static string RandEmote()
        {
            var r = _rng.Next(0, _emotes.Count);
            return _emotes[r];
        }

        public static bool IsMod(this OnChatCommandReceivedArgs e)
        {
            return e.Command.ChatMessage.IsBroadcaster || e.Command.ChatMessage.IsModerator;
        }

        public static bool TryParseSlothyBetType(string str, out SlothyBetType type)
        {
            str = str.ToLower();

            type = SlothyBetType.Win;

            if (str == "win" || str == "won")
            {
                return true;
            }

            if (str == "lose" || str == "loss" || str == "lost")
            {
                type = SlothyBetType.Lose;
                return true;
            }

            return false;
        }

        private static string GetNumberString(this decimal num)
        {
            if ((int)num == num)
                return ((int)num).ToString();
            else if (num == 3.14M)
                return "π";
            else
                return num.ToString();
        }

        private static readonly List<string> _emotes = new List<string>
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
            "PogChamp",
            "ResidentSleeper",
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
