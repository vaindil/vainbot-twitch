using System;
using System.Collections.Generic;
using System.Linq;
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
        private List<SlothyBetRecord> _previousSlothyBetRecords;
        private SlothyBetType? _previousBetWinType;
        private bool _isBettingOpen;
        private bool _canProcessBets;
        private bool _canProcessReversal;

        public SlothyBetCommandHandler(TwitchClient client, SlothyService slothySvc)
        {
            _client = client;
            _slothySvc = slothySvc;

            _slothyBetRecords = new List<SlothyBetRecord>();
            _isBettingOpen = false;
            _canProcessReversal = true;
        }

        public async Task HandleCommandAsync(OnChatCommandReceivedArgs e)
        {
            var subCommand = "help";
            if (e.Command.ArgumentsAsList.Count >= 1)
            {
                subCommand = e.Command.ArgumentsAsList[0].ToLowerInvariant();
            }
            else if (_isBettingOpen)
            {
                subCommand = "status";
            }

            if (subCommand == "help")
            {
                _client.SendMessage(e, "Use !slothybet <amount> <win/lose> to bet. For example, " +
                    "to bet 10 slothies that the nerd will win, use !slothybet 10 win. A mod must " +
                    "open betting before you can place bets.");
                return;
            }

            if (subCommand == "status")
            {
                GetUserStatus(e);
                return;
            }

            if (e.Command.ArgumentsAsList.Count == 2)
            {
                var arg1 = subCommand;
                var arg2 = e.Command.ArgumentsAsList[1].ToLowerInvariant();

                decimal amount;
                try
                {
                    if (decimal.TryParse(arg1, out amount))
                        amount = Math.Round(amount, 2);
                    else if (decimal.TryParse(arg2, out amount))
                        amount = Math.Round(amount, 2);
                    else
                        throw new Exception();
                }
                catch
                {
                    _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: Invalid slothy amount.");
                    return;
                }

                try
                {
                    if (Utils.TryParseSlothyBetType(arg1, out var betType))
                        TakeBet(e, amount, betType);
                    else if (Utils.TryParseSlothyBetType(arg2, out betType))
                        TakeBet(e, amount, betType);
                    else
                        throw new Exception();
                }
                catch
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
                        return;

                    case "close":
                    case "closed":
                        CloseBetting(e);
                        return;

                    case "win":
                    case "won":
                        await ProcessBetsAsync(e, SlothyBetType.Win);
                        return;

                    case "lose":
                    case "loss":
                    case "lost":
                        await ProcessBetsAsync(e, SlothyBetType.Lose);
                        return;

                    case "reverse":
                    case "undo":
                        await ProcessWrongCommandAsync(e);
                        return;

                    case "forfeit":
                    case "forfeited":
                    case "forfeitted":
                    case "draw":
                        ProcessForfeit(e);
                        return;
                }
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

            var stats = CalculateStats();
            _client.SendMessage(e, $"Betting is now closed. | {stats}");
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

            var stats = CalculateStats();

            _client.SendMessage(e, $"Bets processed. | {stats}");

            _previousSlothyBetRecords = _slothyBetRecords.ToList();
            _previousBetWinType = type;

            _slothyBetRecords.Clear();
        }

        private async Task ProcessWrongCommandAsync(OnChatCommandReceivedArgs e)
        {
            if (!_canProcessReversal)
            {
                _client.SendMessage(e, "A bet reversal is already being processed.");
                return;
            }

            if (_previousSlothyBetRecords == null || _previousSlothyBetRecords.Count == 0 || !_previousBetWinType.HasValue)
            {
                _client.SendMessage(e, "No previous bets exist that can be reversed.");
                return;
            }

            _canProcessReversal = false;

            _client.SendMessage(e, "Reversing previous bet results...");

            foreach (var bet in _previousSlothyBetRecords)
            {
                if (bet.BetType == _previousBetWinType)
                    await _slothySvc.AddSlothiesAsync(bet.UserId, 0 - (2 * bet.Amount));
                else
                    await _slothySvc.AddSlothiesAsync(bet.UserId, 2 * bet.Amount);
            }

            _client.SendMessage(e, "Previous round of bets reversed.");

            if (_previousBetWinType == SlothyBetType.Win)
                _previousBetWinType = SlothyBetType.Lose;
            else
                _previousBetWinType = SlothyBetType.Win;

            _canProcessReversal = true;
        }

        private void ProcessForfeit(OnChatCommandReceivedArgs e)
        {
            if (!_canProcessBets)
            {
                _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: Bets cannot be processed right now.");
                return;
            }

            _canProcessBets = false;

            _previousSlothyBetRecords = _slothyBetRecords.ToList();
            _slothyBetRecords.Clear();
            _client.SendMessage(e, "Game was forfeited. All bets have been canceled.");
        }

        private string CalculateStats(List<SlothyBetRecord> records = null)
        {
            if (records == null)
                records = _slothyBetRecords;

            var winBets = records.FindAll(x => x.BetType == SlothyBetType.Win);
            var loseBets = records.FindAll(x => x.BetType == SlothyBetType.Lose);

            var winTotal = winBets.Sum(x => x.Amount);
            var loseTotal = loseBets.Sum(x => x.Amount);

            return $"Total bets: {records.Count} | Bets for win: {winBets.Count} | Bets for loss: {loseBets.Count} | " +
                $"Total slothies bet: {winTotal + loseTotal} | Total bet for win: {winTotal} | Total bet for loss: {loseTotal}";
        }
    }
}
