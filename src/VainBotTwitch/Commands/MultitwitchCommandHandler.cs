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
    public class MultitwitchCommandHandler
    {
        private readonly TwitchClient _client;
        private readonly TwitchAPI _api;

        private List<MultiStreamer> _streamers;

        public MultitwitchCommandHandler(TwitchClient client, TwitchAPI api)
        {
            _client = client;
            _api = api;
        }

        public async Task InitializeAsync()
        {
            using (var db = new VbContext())
            {
                _streamers = await db.MultiStreamers.ToListAsync();
            }
        }

        public async Task HandleCommandAsync(OnChatCommandReceivedArgs e)
        {
            var argCount = e.Command.ArgumentsAsList.Count;

            if (argCount == 0)
            {
                GetMultitwitch(e);
                return;
            }

            var arg1 = e.Command.ArgumentsAsList[0].ToLower();

            if (argCount == 1 && arg1 == "help")
            {
                if (!e.IsMod())
                {
                    _client.SendMessage(e, "See who the nerd is playing with and watch them all together using !multi.");
                }
                else
                {
                    _client.SendMessage(e, "Mods: Clear the multi link using !multi clear. " +
                        "Set streamers by providing a list. For example: !multi gmart strippin");
                }

                return;
            }

            if (argCount == 1 && arg1 == "clear")
            {
                await ClearMultitwitchAsync(e);
                return;
            }

            if (argCount > 0 && arg1 != "help" && e.IsMod())
            {
                await UpdateMultitwitchAsync(e);
            }
        }

        private void GetMultitwitch(OnChatCommandReceivedArgs e)
        {
            if (_streamers.Count == 0)
            {
                _client.SendMessage(e, "The nerd isn't playing with any other nerds.");
                return;
            }

            var url = "https://multitwitch.live/crendor/";

            foreach (var s in _streamers.Select(x => x.Username))
            {
                url += s + "/";
            }

            _client.SendMessage(e, "Watch ALL of the nerds! " + url);
        }

        private async Task ClearMultitwitchAsync(OnChatCommandReceivedArgs e)
        {
            using (var db = new VbContext())
            {
                db.MultiStreamers.RemoveRange(_streamers);
                await db.SaveChangesAsync();
            }

            _streamers.Clear();

            GetMultitwitch(e);
        }

        private async Task UpdateMultitwitchAsync(OnChatCommandReceivedArgs e)
        {
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

                _client.SendMessage(e, msg);
                return;
            }

            var validUsernames = await _api.V5.Users.GetUsersByNameAsync(streamers);
            if (validUsernames.Total != streamers.Count)
            {
                _client.SendMessage(e, $"At least one of those isn't a valid user, you nerd.");

                return;
            }

            using (var db = new VbContext())
            {
                db.MultiStreamers.RemoveRange(_streamers);
                await db.SaveChangesAsync();
            }

            _streamers.Clear();

            foreach (var s in streamers)
                _streamers.Add(new MultiStreamer(s));

            using (var db = new VbContext())
            {
                db.MultiStreamers.AddRange(_streamers);
                await db.SaveChangesAsync();
            }

            GetMultitwitch(e);
        }
    }
}
