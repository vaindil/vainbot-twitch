using Newtonsoft.Json;
using System;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Api.V5.Models.Users;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using VainBotTwitch.Services;

namespace VainBotTwitch.Commands
{
    public class SlothyCommandHandler
    {
        private readonly TwitchClient _client;
        private readonly TwitchAPI _api;
        private readonly SlothyService _slothySvc;

        private readonly string _vaindilId = "45447900";

        public SlothyCommandHandler(TwitchClient client, TwitchAPI api, SlothyService slothySvc)
        {
            _client = client;
            _api = api;
            _slothySvc = slothySvc;
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

            if (argCount == 1)
            {
                await GetSlothiesForUsernameAsync(e);
                return;
            }

            if (argCount == 2 && !e.IsMod())
            {
                _client.SendMessage(e, "You're not a mod, you nerd.");
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
            var count = _slothySvc.GetSlothyCount(e.Command.ChatMessage.UserId);
            _client.SendMessage(e, $"{e.Command.ChatMessage.DisplayName} has {GetSlothyDisplayString(count, e.Command.ChatMessage.UserId)}.");
        }

        private async Task GetSlothiesForUsernameAsync(OnChatCommandReceivedArgs e)
        {
            var username = e.Command.ArgumentsAsList[0].TrimStart('@');
            User user;
            try
            {
                var userResponse = await _api.V5.Users.GetUserByNameAsync(username);
                if (userResponse.Total != 1)
                    throw new Exception();

                user = userResponse.Matches[0];
            }
            catch
            {
                _client.SendMessage(e, $"{e.Command.ChatMessage.DisplayName}: That's not a valid user, you nerd.");
                return;
            }

            var count = _slothySvc.GetSlothyCount(user.Id);
            _client.SendMessage(e, $"{e.Command.ChatMessage.DisplayName}: {user.DisplayName} has {GetSlothyDisplayString(count, user.Id)}.");
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

            if (userId == _vaindilId)
            {
                _client.SendMessage(e, "vaindil's slothies can't be edited, you nerd.");
                return;
            }

            decimal count;

            bool validDecimal;
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
            count = await _slothySvc.AddSlothiesAsync(userId, count);

            _client.SendMessage(e, $"{origUsername} now has {count.ToDisplayString()}.");
        }

        private string GetSlothyDisplayString(decimal count, string userId)
        {
            if (userId == _vaindilId)
                return "an uncountable number of slothies";

            return count.ToDisplayString();
        }
    }
}
