using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using VainBotTwitch.Classes;

namespace VainBotTwitch.Commands
{
    public class QuoteCommandHandler
    {
        private readonly TwitchClient _client;
        private List<QuoteRecord> _quotes;

        private readonly Random _rng = new Random();

        public QuoteCommandHandler(TwitchClient client)
        {
            _client = client;
        }

        public async Task InitializeAsync()
        {
            using (var db = new VbContext())
            {
                _quotes = await db.Quotes
                    .OrderBy(x => x.Id)
                    .ToListAsync();
            }
        }

        public async Task HandleCommandAsync(OnChatCommandReceivedArgs e)
        {
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
                _client.SendMessage(e, "No quotes have been added. Yell at the mods.");
            }

            var quote = _quotes[_rng.Next(_quotes.Count)];

            _client.SendMessage(e, $"\"{quote.Quote}\" - Crendor, {quote.AddedAt.Year}");
        }

        private async Task AddQuoteAsync(OnChatCommandReceivedArgs e)
        {
            if (e.Command.ArgumentsAsString.Length > 280)
            {
                _client.SendMessage(e, $"@{e.Command.ChatMessage.Username}: Quote is too long, not added.");
                return;
            }

            var quoteRecord = new QuoteRecord
            {
                Quote = e.Command.ArgumentsAsString.Trim('"'),
                AddedBy = e.Command.ChatMessage.Username,
                AddedAt = DateTime.UtcNow
            };

            _quotes.Add(quoteRecord);

            using (var db = new VbContext())
            {
                db.Quotes.Add(quoteRecord);
                await db.SaveChangesAsync();
            }

            _client.SendMessage(e, $"@{e.Command.ChatMessage.Username}: Quote added.");
        }
    }
}
