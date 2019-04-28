using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using VainBotTwitch.Commands;

namespace VainBotTwitch
{
    public class Program
    {
        private IConfiguration _config;
        private TwitchClient _client;
        private TwitchAPI _api;

        private MultitwitchCommandHandler _multiHandler;
        private QuoteCommandHandler _quoteHandler;
        private SlothyCommandHandler _slothyHandler;
        private SlothFactCommandHandler _slothFactHandler;
        private WoppyCommandHandler _woppyHandler;

        public static async Task Main() => await new Program().RealMainAsync();

        public async Task RealMainAsync()
        {
            _config = new ConfigurationBuilder()
                .AddJsonFile("config.json")
                .Build();

            _api = new TwitchAPI();
            _api.Settings.ClientId = _config["twitchClientId"];
            _api.Settings.AccessToken = _config["twitchOauth"];
            _client = new TwitchClient();
            _client.Initialize(new ConnectionCredentials(_config["twitchUsername"], _config["twitchOauth"]), _config["twitchChannel"]);

            _client.AddChatCommandIdentifier('!');
            _client.OnChatCommandReceived += CommandHandler;

            _multiHandler = new MultitwitchCommandHandler(_client, _api);
            await _multiHandler.InitializeAsync();

            _quoteHandler = new QuoteCommandHandler(_client);
            await _quoteHandler.InitializeAsync();

            _slothyHandler = new SlothyCommandHandler(_client, _api);
            await _slothyHandler.InitializeAsync();

            _slothFactHandler = new SlothFactCommandHandler(_client);

            _woppyHandler = new WoppyCommandHandler(_config, _client);

            _client.Connect();

            await Task.Delay(-1);
        }

        private async void CommandHandler(object sender, OnChatCommandReceivedArgs e)
        {
            if (e.Command.ChatMessage.BotUsername == e.Command.ChatMessage.Username)
                return;

            switch (e.Command.CommandText.ToLower())
            {
                case "multi":
                case "multitwitch":
                    await _multiHandler.HandleCommandAsync(e);
                    break;

                case "quote":
                case "quotes":
                    await _quoteHandler.HandleCommandAsync(e);
                    break;

                case "slothy":
                case "slothies":
                    await _slothyHandler.HandleCommandAsync(e);
                    break;

                case "slothfact":
                case "slothfacts":
                    _slothFactHandler.HandleCommand(e);
                    break;

                case "woppy":
                case "weather":
                    await _woppyHandler.HandleCommandAsync(e);
                    break;
            }
        }
    }
}
