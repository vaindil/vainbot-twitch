using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using VainBotTwitch.Classes;

namespace VainBotTwitch.Commands
{
    public class SlothyCommandHandler
    {
        private readonly TwitchClient _client;
        private readonly TwitchAPI _api;
        private List<SlothyRecord> _slothyRecords;

        public SlothyCommandHandler(TwitchClient client, TwitchAPI api)
        {
            _client = client;
            _api = api;
        }

        public async Task InitializeAsync()
        {
            using (var db = new VbContext())
            {
                _slothyRecords = await db.Slothies.ToListAsync();
            }
        }

        public async Task HandleCommandAsync(OnChatCommandReceivedArgs e)
        {
            var argCount = e.Command.ArgumentsAsList.Count;
            if (argCount == 0)
            {
                GetSlothies(e);
                return;
            }

            if (argCount > 0 && string.Equals(e.Command.ArgumentsAsList[0], "help", StringComparison.OrdinalIgnoreCase))
            {
                _client.SendMessage(e, "Slothies are a made-up points system. They give you nothing whatsoever other than " +
                    "bragging rights. Use !slothies to check how many you have.");

                return;
            }

            if (argCount == 2 && e.IsMod())
            {
                await UpdateSlothiesAsync(e);
                return;
            }

            _client.SendMessage(e, "That's not a valid slothies command, you nerd.");
        }

        private void GetSlothies(OnChatCommandReceivedArgs e)
        {
            var count = 0M;
            var record = _slothyRecords.Find(x => x.UserId == e.Command.ChatMessage.UserId);
            if (record != null)
                count = record.Count;

            _client.SendMessage(e, $"{e.Command.ChatMessage.DisplayName} has {count.ToDisplayString()}.");
        }

        private async Task UpdateSlothiesAsync(OnChatCommandReceivedArgs e)
        {
            var origUsername = e.Command.ArgumentsAsList[0].TrimStart('@');

            var username = origUsername.ToLower();
            if (username.Length >= 200)
            {
                _client.SendMessage(e, $"That's not a valid user, you nerd.");
                return;
            }

            var users = await _api.V5.Users.GetUserByNameAsync(username);
            if (users.Total != 1)
            {
                _client.SendMessage(e, "That's not a valid user, you nerd.");
                return;
            }

            var userId = users.Matches[0].Id;

            if (userId == e.Command.ChatMessage.UserId)
            {
                _client.SendMessage(e, "You can't change your own slothies, you nerd.");
                return;
            }

            if (userId == "45447900")
            {
                _client.SendMessage(e, "vaindil's slothies can't be edited, you nerd.");
                return;
            }

            var validDecimal = false;
            decimal count;
            try
            {
                // use try/catch to prevent overflow exception
                validDecimal = decimal.TryParse(e.Command.ArgumentsAsList[1], out count);
            }
            catch
            {
                _client.SendMessage(e, "You can't overflow me, you nerd.");
                return;
            }

            if (!validDecimal)
            {
                _client.SendMessage(e, "That's not a valid number, you nerd.");
                return;
            }

            count = Math.Round(count, 2);

            var isNew = false;
            var record = _slothyRecords.Find(x => x.UserId == userId);
            if (record != null)
            {
                record.Count += count;
            }
            else
            {
                isNew = true;
                record = new SlothyRecord
                {
                    UserId = userId,
                    Count = count
                };

                _slothyRecords.Add(record);
            }

            using (var db = new VbContext())
            {
                if (isNew)
                    db.Slothies.Add(record);
                else
                    db.Slothies.Update(record);

                await db.SaveChangesAsync();
            }

            _client.SendMessage(e, $"{origUsername} now has {count.ToDisplayString()}.");
        }
    }
}
