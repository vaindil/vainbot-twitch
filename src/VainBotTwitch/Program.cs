using Microsoft.Extensions.Configuration;
using System;
using System.Threading;
using System.Threading.Tasks;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;
using TwitchLib.PubSub;
using VainBotTwitch.Classes;
using VainBotTwitch.Commands;
using VainBotTwitch.PubSubHandlers;
using VainBotTwitch.Services;

namespace VainBotTwitch
{
    public class Program
    {
        private BotConfig _config;
        private TwitchClient _client;
        private TwitchAPI _api;
        private TwitchPubSub _pubSub;

        private SlothyService _slothySvc;
        private SlothyBetService _betSvc;

        private MultitwitchCommandHandler _multiHandler;
        private QuoteCommandHandler _quoteHandler;
        private SlothyCommandHandler _slothyHandler;
        private SlothyBetCommandHandler _slothyBetCommandHandler;
        private SlothFactCommandHandler _slothFactHandler;
        private WoppyCommandHandler _woppyHandler;

#pragma warning disable IDE0052 // Remove unread private members
        private SubPointsHandler _subPointsHandler;
        private StretchTimerHandler _stretchTimerHandler;
        private Timer _pubSubReconnectTimer;
#pragma warning restore IDE0052 // Remove unread private members

        public static async Task Main() => await new Program().RealMainAsync();

        public async Task RealMainAsync()
        {
            var config = new ConfigurationBuilder()
                .AddJsonFile("config.json")
                .Build();

            _config = new BotConfig(config);

            _api = new TwitchAPI();
            _api.Settings.ClientId = _config.TwitchClientId;
            _api.Settings.AccessToken = _config.TwitchOAuth;
            _client = new TwitchClient();
            _client.Initialize(new ConnectionCredentials(_config.TwitchUsername, _config.TwitchOAuth), _config.TwitchChannel);

            _client.OnConnected += (_, __) => Utils.LogToConsole("Connected to chat");
            _client.OnJoinedChannel += (_, e) => Utils.LogToConsole($"Joined chat channel {e.Channel}");

            _client.OnChatCommandReceived += CommandHandler;
            _client.OnConnectionError += ChatConnectionError;
            _client.OnDisconnected += ChatDisconnected;
            _client.OnError += ChatError;
            _client.OnIncorrectLogin += ChatIncorrectLogin;

            _client.AddChatCommandIdentifier('!');

            _pubSub = new TwitchPubSub();

            _slothySvc = new SlothyService();
            await _slothySvc.InitializeAsync();

            _multiHandler = new MultitwitchCommandHandler(_client, _api);
            await _multiHandler.InitializeAsync();

            _quoteHandler = new QuoteCommandHandler(_client);
            await _quoteHandler.InitializeAsync();

            _betSvc = new SlothyBetService();
            await _betSvc.InitializeAsync();

            _slothyHandler = new SlothyCommandHandler(_client, _api, _slothySvc);
            _slothFactHandler = new SlothFactCommandHandler(_client);
            _woppyHandler = new WoppyCommandHandler(_config, _client);

            _slothyBetCommandHandler = new SlothyBetCommandHandler(_client, _betSvc, _slothySvc);
            await _slothyBetCommandHandler.InitializeAsync();

            ConnectChat();

            _pubSub.OnPubSubServiceConnected += PubSubConnected;
            _pubSub.OnPubSubServiceClosed += PubSubClosed;
            _pubSub.OnPubSubServiceError += PubSubClosed;

            _subPointsHandler = new SubPointsHandler(_config, _client, _pubSub, _slothySvc);
            _stretchTimerHandler = new StretchTimerHandler(_config, _client, _pubSub);

            _pubSubReconnectTimer = new Timer(_ => ReconnectPubSub(), null, TimeSpan.Zero, TimeSpan.FromHours(18));

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
                case "lastquote":
                    await _quoteHandler.HandleCommandAsync(e);
                    break;

                case "slothy":
                case "slothies":
                    await _slothyHandler.HandleCommandAsync(e);
                    break;

                case "slothybet":
                case "slothiebet":
                case "bet":
                    await _slothyBetCommandHandler.HandleCommandAsync(e);
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

        private void ConnectChat()
        {
            Utils.LogToConsole("Connecting to chat");

            try
            {
                _client.Disconnect();
            }
            catch
            {
            }

            _client.Connect();
        }

        private void ChatConnectionError(object sender, OnConnectionErrorArgs e)
        {
            Utils.LogToConsole($"Chat connection error: {e.Error.Message}");
            ConnectChat();
        }

        private void ChatDisconnected(object sender, OnDisconnectedEventArgs e)
        {
            Utils.LogToConsole("Chat disconnected");
            ConnectChat();
        }

        private void ChatError(object sender, OnErrorEventArgs e)
        {
            Utils.LogToConsole($"Chat error: {e.Exception.Message}");
            ConnectChat();
        }

        private void ChatIncorrectLogin(object sender, OnIncorrectLoginArgs e)
        {
            Utils.LogToConsole($"Chat incorrect login: {e.Exception.Message}");
            ConnectChat();
        }

        private void ReconnectPubSub()
        {
            _pubSub.OnPubSubServiceClosed -= PubSubClosed;

            try
            {
                _pubSub.Disconnect();
            }
            catch
            {
            }

            _pubSub.Connect();

            _pubSub.OnPubSubServiceClosed += PubSubClosed;
        }

        private void PubSubConnected(object sender, EventArgs e)
        {
            _pubSub.ListenToSubscriptions(_config.TwitchChannelId);
            _pubSub.ListenToVideoPlayback(_config.TwitchChannel);
            _pubSub.SendTopics(_config.SubPointsAccessToken);
            Utils.LogToConsole("PubSub connected and topics sent");
        }

        private void PubSubClosed(object sender, EventArgs e)
        {
            _pubSub.Connect();
        }
    }
}
