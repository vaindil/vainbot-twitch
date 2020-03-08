using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using VainBotTwitch.Classes.QuoteRecords;

namespace VainBotTwitch.Commands
{
    public class QuoteCommandHandler<T> where T : QuoteRecordBase, new()
    {
        private readonly TwitchClient _client;
        private readonly string _name;
        private List<T> _quotes;

        private readonly Random _rng = new Random();
        private int? _lastQuoteId;

        public QuoteCommandHandler(TwitchClient client)
        {
            _client = client;
            if (typeof(T) == typeof(CrendorQuoteRecord))
                _name = "Crendor";
            else
                _name = "Nick";
        }

        public async Task InitializeAsync()
        {
            using var db = new VbContext();
            _quotes = await db.Set<T>()
                .OrderBy(x => x.Id)
                .ToListAsync();
        }

        public async Task HandleCommandAsync(OnChatCommandReceivedArgs e)
        {
            var lcaseCommand = e.Command.CommandText.ToLowerInvariant();

            if (lcaseCommand.StartsWith("last") && lcaseCommand.EndsWith("quote"))
            {
                GetLastQuoteId(e);
                return;
            }

            if (e.IsMod() && lcaseCommand.EndsWith("dbupdate"))
            {
                await InitializeAsync();
                _client.SendMessage(e, $"{_name} quotes updated.");
                return;
            }

            if (e.Command.ArgumentsAsList.Count == 1 && int.TryParse(e.Command.ArgumentsAsList[0], out var quoteId))
            {
                GetQuoteById(quoteId, e);
                return;
            }

            if (e.Command.ArgumentsAsList.Count > 0 && e.IsMod())
            {
                await AddQuoteAsync(e);
                return;
            }

            GetQuote(e);
        }

        private void GetQuote(OnChatCommandReceivedArgs e)
        {
            if (_quotes.Count == 0)
            {
                _client.SendMessage(e, $"No {_name} quotes have been added. Yell at the mods.");
                return;
            }

            var quote = _quotes[_rng.Next(_quotes.Count)];
            _lastQuoteId = quote.Id;

            _client.SendMessage(e, GetQuoteString(quote));
        }

        private void GetQuoteById(int quoteId, OnChatCommandReceivedArgs e)
        {
            var quote = _quotes.Find(x => x.Id == quoteId);

            var message = $"No {_name} quote with that ID exists.";
            if (quote != null)
            {
                message = GetQuoteString(quote);
                _lastQuoteId = quoteId;
            }

            _client.SendMessage(e, $"{e.Command.ChatMessage.DisplayName}: {message}");
        }

        private void GetLastQuoteId(OnChatCommandReceivedArgs e)
        {
            var message = $"No {_name} quote has been requested since the bot was last restarted.";
            if (_lastQuoteId.HasValue)
                message = $"The ID of the last {_name} quote was {_lastQuoteId}.";

            _client.SendMessage(e, $"{e.Command.ChatMessage.DisplayName}: {message}");
        }

        private async Task AddQuoteAsync(OnChatCommandReceivedArgs e)
        {
            if (e.Command.ArgumentsAsString.Length > 280)
            {
                _client.SendMessage(e, $"@{e.Command.ChatMessage.Username}: Quote is too long, not added.");
                return;
            }

            var quoteRecord = new T
            {
                Quote = e.Command.ArgumentsAsString.Trim('"'),
                AddedBy = e.Command.ChatMessage.Username,
                AddedAt = DateTime.UtcNow
            };

            _quotes.Add(quoteRecord);

            using (var db = new VbContext())
            {
                db.Set<T>().Add(quoteRecord);
                await db.SaveChangesAsync();
            }

            _client.SendMessage(e, $"@{e.Command.ChatMessage.Username}: {_name} quote added.");
        }

        private string GetQuoteString(T quote)
        {
            return $"\"{quote.Quote}\" - {_name}, {quote.AddedAt.Year}";
        }
    }
}
