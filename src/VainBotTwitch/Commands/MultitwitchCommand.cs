using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
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

            var url = "https://multitwitch.live/crendor/";

            foreach (var s in streamers)
            {
                url += s + "/";
            }

            client.SendMessage(e.GetChannel(client),
                               "Watch ALL of the nerds! " + url + $" {Utils.RandEmote()}");
        }

        public static async Task UpdateMultitwitch(object sender, OnChatCommandReceivedArgs e, TwitchAPI api)
        {
            var client = (TwitchClient)sender;

            if (e.Command.ArgumentsAsList.Count == 1
                && string.Equals(e.Command.ArgumentsAsList[0], "clear", System.StringComparison.OrdinalIgnoreCase))
            {
                using (var db = new VbContext())
                {
                    db.MultiStreamers.RemoveRange(db.MultiStreamers);
                    await db.SaveChangesAsync().ConfigureAwait(false);
                }

                client.SendMessage(e.GetChannel(client), $"The nerd isn't playing with any other nerds. {Utils.RandEmote()}");
                return;
            }

            var streamers = e.Command.ArgumentsAsList
                .Select(s => s.ToLower())
                .Select(s => s.TrimStart('@'))
                .Distinct()
                .ToList();

            var crendorRemoved = streamers.Remove("crendor");

            if (streamers.Count == 0)
            {
                var msg = "You didn't specify any users, you nerd.";
                if (crendorRemoved)
                    msg += " Crendor is automatically added, so he doesn't count.";

                msg += $" {Utils.RandEmote()}";

                client.SendMessage(e.GetChannel(client), msg);
                return;
            }

            var validUsernames = await api.V5.Users.GetUsersByNameAsync(streamers);
            if (validUsernames.Total != streamers.Count)
            {
                client.SendMessage(e.GetChannel(client),
                    $"At least one of those isn't a valid user, you nerd. {Utils.RandEmote()}");

                return;
            }

            using (var db = new VbContext())
            {
                db.MultiStreamers.RemoveRange(db.MultiStreamers);
                await db.SaveChangesAsync();
            }

            using (var db = new VbContext())
            {
                foreach (var u in streamers)
                {
                    db.MultiStreamers.Add(new MultiStreamer(u));
                }

                await db.SaveChangesAsync();
            }

            await GetMultitwitch(sender, e);
        }
    }
}
