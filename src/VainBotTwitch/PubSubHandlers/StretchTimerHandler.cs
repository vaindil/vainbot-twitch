using System;
using System.Threading;
using TwitchLib.Client;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Events;
using VainBotTwitch.Classes;

namespace VainBotTwitch.PubSubHandlers
{
    public class StretchTimerHandler
    {
        private readonly TwitchClient _client;
        private readonly TwitchPubSub _pubSub;
        private readonly BotConfig _config;

        private Timer _stretchTimer;

        public StretchTimerHandler(BotConfig config, TwitchClient client, TwitchPubSub pubSub)
        {
            _config = config;
            _client = client;
            _pubSub = pubSub;

            _pubSub.OnStreamUp += OnStreamUp;
            _pubSub.OnStreamDown += OnStreamDown;
        }

        private void OnStreamUp(object sender, OnStreamUpArgs e)
        {
            _stretchTimer = new Timer(
                _ => SendStretchMessage(),
                null,
                TimeSpan.FromSeconds(_config.StretchReminderFrequency),
                TimeSpan.FromSeconds(_config.StretchReminderFrequency));
        }

        private void OnStreamDown(object sender, OnStreamDownArgs e)
        {
            _stretchTimer?.Dispose();
            _stretchTimer = null;
        }

        private void SendStretchMessage()
        {
            _client.SendMessage(_config.TwitchChannel, $"@{_config.TwitchChannel} Go stretch ya nerd.");
        }
    }
}
