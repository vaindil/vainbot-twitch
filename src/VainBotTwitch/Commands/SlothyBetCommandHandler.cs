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
        private readonly SlothyBetService _betSvc;
        private readonly SlothyService _slothySvc;

        private List<SlothyBetRecord> _previousSlothyBetRecords;
        private SlothyBetType? _previousBetWinType;
        private bool _isBettingOpen;
        private bool _canProcessBets;
        private bool _canProcessCorrection;

        public SlothyBetCommandHandler(TwitchClient client, SlothyBetService betSvc, SlothyService slothySvc)
        {
            _client = client;
            _betSvc = betSvc;
            _slothySvc = slothySvc;

            _isBettingOpen = false;
            _canProcessBets = false;
            _canProcessCorrection = false;
        }

        public async Task InitializeAsync()
        {
            var kv = await KeyValueService.GetByKeyAsync(nameof(SlothyBetStatus));

            SlothyBetStatus status;
            if (kv?.Value == null)
                status = SlothyBetStatus.Closed;
            else
                status = (SlothyBetStatus)Enum.Parse(typeof(SlothyBetStatus), kv.Value, true);

            switch (status)
            {
                case SlothyBetStatus.Open:
                    _isBettingOpen = true;
                    _canProcessBets = false;
                    break;

                case SlothyBetStatus.InProgress:
                    _isBettingOpen = false;
                    _canProcessBets = true;
                    break;
            }
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
                if (_isBettingOpen)
                {
                    var arg1 = subCommand;
                    var arg2 = e.Command.ArgumentsAsList[1].ToLowerInvariant();

                    if (decimal.TryParse(arg1, out var amount))
                    {
                        amount = Math.Round(amount, 2);
                    }
                    else if (decimal.TryParse(arg2, out amount))
                    {
                        amount = Math.Round(amount, 2);
                    }
                    else
                    {
                        _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: Invalid slothy amount.");
                        return;
                    }

                    if (Utils.TryParseSlothyBetType(arg1, false, out var betType))
                        await TakeBetAsync(e, amount, betType);
                    else if (Utils.TryParseSlothyBetType(arg2, false, out betType))
                        await TakeBetAsync(e, amount, betType);
                    else
                        _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: You must specify either win or lose.");

                    return;
                }
                else if (!e.IsMod())
                {
                    _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: Betting is currently closed.");
                    return;
                }
                else if (subCommand == "fix" || subCommand == "correct" || subCommand == "correction")
                {
                    await ProcessWrongCommandAsync(e);
                    return;
                }
            }

            if (e.IsMod() && e.Command.ArgumentsAsList.Count == 1)
            {
                switch (subCommand)
                {
                    case "open":
                    case "opened":
                        await OpenBettingAsync(e);
                        return;

                    case "close":
                    case "closed":
                        await CloseBettingAsync(e);
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

                    case "fix":
                    case "correct":
                    case "correction":
                        if (e.Command.ArgumentsAsList.Count < 2)
                        {
                            _client.SendMessage(e, "Correct the previous bet by providing the outcome that should have been used. " +
                                "Example: !slothybet fix loss");
                            return;
                        }

                        return;

                    case "forfeit":
                    case "forfeited":
                    case "forfeitted":
                    case "draw":
                    case "void":
                        await ProcessForfeitAsync(e);
                        return;
                }
            }

            _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: Invalid slothy bet command.");
        }

        public void GetUserStatus(OnChatCommandReceivedArgs e)
        {
            if (_isBettingOpen)
            {
                var record = _betSvc.GetCurrentBet(e.Command.ChatMessage.UserId);
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
            else if (_betSvc.BetCount > 0)
            {
                var record = _betSvc.GetCurrentBet(e.Command.ChatMessage.UserId);
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

        private async Task TakeBetAsync(OnChatCommandReceivedArgs e, decimal amount, SlothyBetType type)
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

            var hadExistingBet = _betSvc.GetCurrentBet(e.Command.ChatMessage.UserId) != null;

            await _betSvc.AddOrUpdateBetAsync(new SlothyBetRecord
            {
                UserId = e.Command.ChatMessage.UserId,
                Amount = amount,
                BetType = type
            });

            if (!hadExistingBet)
                _client.SendMessage(e, $"{msgBegin} Bet placed.");
            else
                _client.SendMessage(e, $"{msgBegin} Bet updated.");
        }

        private async Task OpenBettingAsync(OnChatCommandReceivedArgs e)
        {
            if (_isBettingOpen)
            {
                _client.SendMessage(e, "Betting was already open.");
                return;
            }

            _isBettingOpen = true;
            _canProcessBets = false;
            await KeyValueService.CreateOrUpdateAsync(nameof(SlothyBetStatus), nameof(SlothyBetStatus.Open));

            _client.SendMessage(e, "Betting is now open.");
        }

        private async Task CloseBettingAsync(OnChatCommandReceivedArgs e)
        {
            if (!_isBettingOpen)
            {
                _client.SendMessage(e, "Betting was already closed.");
                return;
            }

            _isBettingOpen = false;
            _canProcessBets = true;

            await KeyValueService.CreateOrUpdateAsync(nameof(SlothyBetStatus), nameof(SlothyBetStatus.InProgress));

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
            _canProcessCorrection = false;

            _client.SendMessage(e, "Processing bets...");

            var currentBets = _betSvc.GetAllCurrentBets();

            foreach (var bet in currentBets)
            {
                if (bet.BetType == type)
                    await _slothySvc.AddSlothiesAsync(bet.UserId, bet.Amount);
                else
                    await _slothySvc.AddSlothiesAsync(bet.UserId, 0 - bet.Amount);
            }

            var stats = CalculateStats();

            _previousSlothyBetRecords = currentBets;
            _previousBetWinType = type;

            await _betSvc.ClearBetsAsync();
            await KeyValueService.CreateOrUpdateAsync(nameof(SlothyBetStatus), nameof(SlothyBetStatus.Closed));

            _canProcessCorrection = true;

            _client.SendMessage(e, $"Bets processed. | {stats}");
        }

        private async Task ProcessForfeitAsync(OnChatCommandReceivedArgs e)
        {
            if (!_canProcessBets)
            {
                _client.SendMessage(e, $"@{e.Command.ChatMessage.DisplayName}: Bets cannot be processed right now.");
                return;
            }

            _canProcessBets = false;
            _canProcessCorrection = false;

            _previousSlothyBetRecords = _betSvc.GetAllCurrentBets();
            _previousBetWinType = SlothyBetType.Void;

            await _betSvc.ClearBetsAsync();
            await KeyValueService.CreateOrUpdateAsync(nameof(SlothyBetStatus), nameof(SlothyBetStatus.Closed));

            _canProcessCorrection = true;

            _client.SendMessage(e, "All bets have been canceled.");
        }

        private async Task ProcessWrongCommandAsync(OnChatCommandReceivedArgs e)
        {
            if (_previousSlothyBetRecords == null || _previousSlothyBetRecords.Count == 0 || !_previousBetWinType.HasValue)
            {
                _client.SendMessage(e, "No previous bets exist that can be reversed.");
                return;
            }

            if (!_canProcessCorrection)
            {
                _client.SendMessage(e, "A bet correction is already being processed.");
                return;
            }

            if (!Utils.TryParseSlothyBetType(e.Command.ArgumentsAsList[1], true, out var correctType))
            {
                _client.SendMessage(e, "Couldn't parse the outcome that should have been used. Try win, loss, or void.");
                return;
            }

            if (_previousBetWinType == correctType)
            {
                _client.SendMessage(e, "The outcome you specified is the same outcome that was " +
                    "used to process the previous round of bets. No action taken.");
                return;
            }

            _canProcessCorrection = false;

            _client.SendMessage(e, "Correcting previous bet results...");

            foreach (var bet in _previousSlothyBetRecords)
            {
                if (_previousBetWinType == SlothyBetType.Win)
                {
                    // previously +x for correct bet, so adjustment is -2x for being incorrect
                    if (correctType == SlothyBetType.Lose && bet.BetType == SlothyBetType.Win)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, 0 - (2 * bet.Amount));

                    // previously -x for incorrect bet, so adjustment is +2x for being correct
                    else if (correctType == SlothyBetType.Lose && bet.BetType == SlothyBetType.Lose)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, 2 * bet.Amount);

                    // previously +x for correct bet, so adjustment is -x to void
                    else if (correctType == SlothyBetType.Void && bet.BetType == SlothyBetType.Win)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, 0 - bet.Amount);

                    // previously -x for incorrect bet, so adjustment is +x to void
                    else if (correctType == SlothyBetType.Void && bet.BetType == SlothyBetType.Lose)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, bet.Amount);
                }
                else if (_previousBetWinType == SlothyBetType.Lose)
                {
                    // previously -x for incorrect bet, so adjustment is +2x for being correct
                    if (correctType == SlothyBetType.Win && bet.BetType == SlothyBetType.Win)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, 2 * bet.Amount);

                    // previously +x for correct bet, so adjustment is -2x for being incorrect
                    else if (correctType == SlothyBetType.Win && bet.BetType == SlothyBetType.Lose)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, 0 - (2 * bet.Amount));

                    // previously -x for incorrect bet, so adjustment is +x to void
                    else if (correctType == SlothyBetType.Void && bet.BetType == SlothyBetType.Win)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, bet.Amount);

                    // previously +x for correct bet, so adjustment is -x to void
                    else if (correctType == SlothyBetType.Void && bet.BetType == SlothyBetType.Lose)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, 0 - bet.Amount);
                }
                else if (_previousBetWinType == SlothyBetType.Void)
                {
                    // previously 0 for voided bet, so adjustment is +x for being correct
                    if (correctType == SlothyBetType.Win && bet.BetType == SlothyBetType.Win)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, bet.Amount);

                    // previously 0 for voided bet, so adjustment is -x for being incorrect
                    else if (correctType == SlothyBetType.Win && bet.BetType == SlothyBetType.Lose)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, 0 - bet.Amount);

                    // previously 0 for voided bet, so adjustment is -x to void
                    else if (correctType == SlothyBetType.Lose && bet.BetType == SlothyBetType.Win)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, 0 - bet.Amount);

                    // previously 0 for voided bet, so adjustment is +x to void
                    else if (correctType == SlothyBetType.Lose && bet.BetType == SlothyBetType.Lose)
                        await _slothySvc.AddSlothiesAsync(bet.UserId, bet.Amount);
                }
            }

            var prevStr = _previousBetWinType.Value.ToString().ToLowerInvariant();
            var newStr = correctType.ToString().ToLowerInvariant();

            _client.SendMessage(e, $"Previous round of bets changed from {prevStr} to {newStr}.");

            _previousBetWinType = correctType;

            _canProcessCorrection = true;
        }

        private string CalculateStats(List<SlothyBetRecord> records = null)
        {
            if (records == null)
                records = _betSvc.GetAllCurrentBets();

            var winBets = records.FindAll(x => x.BetType == SlothyBetType.Win);
            var loseBets = records.FindAll(x => x.BetType == SlothyBetType.Lose);

            var winTotal = winBets.Sum(x => x.Amount);
            var loseTotal = loseBets.Sum(x => x.Amount);

            return $"Total bets: {records.Count} | Bets for win: {winBets.Count} | Bets for loss: {loseBets.Count} | " +
                $"Total slothies bet: {winTotal + loseTotal} | Total bet for win: {winTotal} | Total bet for loss: {loseTotal}";
        }
    }
}
