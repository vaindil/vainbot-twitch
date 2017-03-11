﻿using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Events.Client;
using VainBotTwitch.Classes;

namespace VainBotTwitch.Commands
{
    public static class MultitwitchCommand
    {
        public static async Task GetMultitwitch(object sender, OnChatCommandReceivedArgs e)
        {
            var client = (TwitchClient)sender;

            List<string> streamers;

            using (var db = new VbContext())
            {
                streamers = await db.MultiStreamers.Select(s => s.Username).ToListAsync();
            }

            if (streamers.Count == 0)
            {
                client.SendMessage(e.GetChannel(client), $"The nerd isn't playing with any other nerds. {Utils.RandEmote()}");
                return;
            }

            var url = "http://multistre.am/crendor/";

            foreach (var s in streamers)
            {
                url += s + "/";
            }

            client.SendMessage(e.GetChannel(client),
                               $"Watch ALL of the nerds! " + url + $" {Utils.RandEmote()}");
        }

        public static async Task UpdateMultitwitch(object sender, OnChatCommandReceivedArgs e)
        {
            var client = (TwitchClient)sender;

            if (e.Command.ArgumentsAsList.Count == 1 && e.Command.ArgumentsAsList[0].ToLower() == "clear")
            {
                using (var db = new VbContext())
                {
                    db.MultiStreamers.RemoveRange(db.MultiStreamers);
                    await db.SaveChangesAsync();
                }

                client.SendMessage(e.GetChannel(client), $"The nerd isn't playing with any other nerds. {Utils.RandEmote()}");
                return;
            }

            var validUsernames = await TwitchApi.Users.GetUsersV5Async(e.Command.ArgumentsAsList);
            if (validUsernames.Count != e.Command.ArgumentsAsList.Count)
            {
                client.SendMessage(e.GetChannel(client),
                    $"At least one of those isn't a valid user, you nerd. {Utils.RandEmote()}");
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
    }
}
