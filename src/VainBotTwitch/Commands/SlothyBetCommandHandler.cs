using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using VainBotTwitch.Classes;
using VainBotTwitch.Services;

namespace VainBotTwitch.Commands
{
    public class SlothyBetCommandHandler
    {
        private readonly TwitchClient _client;
        private readonly SlothyService _slothySvc;

        private readonly List<SlothyBetRecord> _slothyBetRecords;
        private bool _isBettingOpen;
        private bool _canProcessBets;

        public SlothyBetCommandHandler(TwitchClient client, SlothyService slothySvc)
        {
            _client = client;
            _slothySvc = slothySvc;

            _slothyBetRecords = new List<SlothyBetRecord>();
            _isBettingOpen = false;
        }

        public async Task HandleCommandAsync(OnChatCommandReceivedArgs e)
        {
            var subCommand = "help";
            if (e.Command.ArgumentsAsList.Count >= 1)
            {
                subCommand = e.Command.ArgumentsAsList[0].ToLower();
            }
            else if (_isBettingOpen)
            {
                subCommand = "status";
            }

            if (subCommand == "help")
            {
                _client.SendMessage(e, "Use !slothybet <amount> <win/lose> to bet. For example, " +
                    "to bet 10 slothies that the nerd will win, use !slothybet 10 win.");
                return;
            }

            if (subCommand == "status")
            {
                GetUserStatus(e);
                return;
            }

            if (e.Command.ArgumentsAsList.Count == 2)
            {
                decimal amount;
                try
                {
                    if (decimal.TryParse(subCommand, out amount))
                        amount = Math.Round(amount, 2);
                }
                catch
                {
                    _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: Invalid slothy amount.");
                    return;
                }

                if (Utils.TryParseSlothyBetType(e.Command.ArgumentsAsList[1], out var type))
                {
                    TakeBet(e, amount, type);
                }
                else
                {
                    _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: You must specify either win or lose.");
                }

                return;
            }

            if (e.IsMod() && e.Command.ArgumentsAsList.Count == 1)
            {
                switch (subCommand)
                {
                    case "open":
                    case "opened":
                        OpenBetting(e);
                        break;

                    case "close":
                    case "closed":
                        CloseBetting(e);
                        break;

                    case "win":
                    case "won":
                        await ProcessBetsAsync(e, SlothyBetType.Win);
                        break;

                    case "lose":
                    case "loss":
                    case "lost":
                        await ProcessBetsAsync(e, SlothyBetType.Lose);
                        break;

                    case "forfeit":
                    case "forfeited":
                    case "forfeitted":
                    case "draw":
                        ProcessForfeit(e);
                        break;
                }

                return;
            }

            _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: Invalid slothy bet command.");
        }

        public void GetUserStatus(OnChatCommandReceivedArgs e)
        {
            if (_isBettingOpen)
            {
                var record = _slothyBetRecords.Find(x => x.UserId == e.Command.ChatMessage.UserId);
                if (record != null)
                {
                    _client.SendMessage(e, $"{e.Command.ChatMessage.Username} bet {record.Amount.ToDisplayString()} " +
                        $"that the nerd will {record.BetTypeString}.");
                }
                else
                {
                    _client.SendMessage(e, $"{e.Command.ChatMessage.Username} has not submitted a bet.");
                }
            }
            else if (_slothyBetRecords.Count > 0)
            {
                var record = _slothyBetRecords.Find(x => x.UserId == e.Command.ChatMessage.UserId);
                if (record != null)
                {
                    _client.SendMessage(e, $"{e.Command.ChatMessage.Username} bet {record.Amount.ToDisplayString()} " +
                        $"that the nerd will {record.BetTypeString}.");
                }
                else
                {
                    _client.SendMessage(e, $"{e.Command.ChatMessage.Username} did not submit a bet.");
                }
            }
            else
            {
                _client.SendMessage(e, "Betting is not currently in progress.");
            }
        }

        private void TakeBet(OnChatCommandReceivedArgs e, decimal amount, SlothyBetType type)
        {
            var msgBegin = $"@{e.Command.ChatMessage.DisplayName}:";

            if (!_isBettingOpen)
            {
                _client.SendMessage(e, $"{msgBegin} Betting is currently closed.");
                return;
            }

            if (amount <= 0)
            {
                _client.SendMessage(e, $"{msgBegin} You must bet a positive number of slothies.");
                return;
            }

            if (amount > 100)
            {
                _client.SendMessage(e, $"{msgBegin} You cannot bet more than 100 slothies at a time.");
                return;
            }

            var curCount = _slothySvc.GetSlothyCount(e.Command.ChatMessage.UserId);
            if (curCount <= -1000)
            {
                _client.SendMessage(e, $"{msgBegin} You must have more than -1,000 slothies to place bets.");
                return;
            }

            var existingRecord = _slothyBetRecords.Find(x => x.UserId == e.Command.ChatMessage.UserId);
            if (existingRecord != null)
            {
                _client.SendMessage(e, $"{msgBegin} You already placed a bet this round.");
                return;
            }

            var record = new SlothyBetRecord
            {
                UserId = e.Command.ChatMessage.UserId,
                Amount = amount,
                BetType = type
            };

            _slothyBetRecords.Add(record);

            _client.SendMessage(e, $"{msgBegin} Bet placed.");
        }

        private void OpenBetting(OnChatCommandReceivedArgs e)
        {
            if (_isBettingOpen)
            {
                _client.SendMessage(e, "Betting was already open.");
                return;
            }

            _isBettingOpen = true;
            _canProcessBets = false;
            _client.SendMessage(e, "Betting is now open.");
        }

        private void CloseBetting(OnChatCommandReceivedArgs e)
        {
            if (!_isBettingOpen)
            {
                _client.SendMessage(e, "Betting was already closed.");
                return;
            }

            _isBettingOpen = false;
            _canProcessBets = true;
            _client.SendMessage(e, "Betting is now closed.");
        }

        private async Task ProcessBetsAsync(OnChatCommandReceivedArgs e, SlothyBetType type)
        {
            if (!_canProcessBets)
            {
                _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: Bets cannot be processed right now. " +
                    "If betting is open, close it first.");
                return;
            }

            _canProcessBets = false;

            _client.SendMessage(e, "Processing bets...");

            foreach (var bet in _slothyBetRecords)
            {
                if (bet.BetType == type)
                    await _slothySvc.AddSlothiesAsync(bet.UserId, bet.Amount);
                else
                    await _slothySvc.AddSlothiesAsync(bet.UserId, 0 - bet.Amount);
            }

            _client.SendMessage(e, "All bets processed.");
            _slothyBetRecords.Clear();
        }

        private void ProcessForfeit(OnChatCommandReceivedArgs e)
        {
            if (!_canProcessBets)
            {
                _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: Bets cannot be processed right now.");
                return;
            }

            _canProcessBets = false;

            _slothyBetRecords.Clear();
            _client.SendMessage(e, "Game was forfeited. All bets have been canceled.");
        }
    }
}
