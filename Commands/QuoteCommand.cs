using System;
using System.Data.Entity;
using System.Threading.Tasks;
using TwitchLib;
using TwitchLib.Events.Client;
using VainBotTwitch.Classes;

namespace VainBotTwitch.Commands
{
    public static class QuoteCommand
    {
        public static async Task GetQuoteAsync(object sender, OnChatCommandReceivedArgs e, Random rng)
        {
            var client = (TwitchClient)sender;
            QuoteRecord quote;

            using (var db = new VbContext())
            {
                var count = await db.Quotes.CountAsync();
                var quoteId = rng.Next(count);
                quote = await db.Quotes.FindAsync(quoteId + 1);
            }

            client.SendMessage(e.GetChannel(client), $"\"{quote.Quote}\" - Crendor, {quote.AddedAt.Year}");
        }

        public static async Task AddQuoteAsync(object sender, OnChatCommandReceivedArgs e)
        {
            var client = (TwitchClient)sender;

            if (e.Command.ArgumentsAsString.Length > 280)
            {
                client.SendMessage(e.GetChannel(client), $"@{e.Command.ChatMessage.Username}: Quote is too long, not added.");
                return;
            }

            using (var db = new VbContext())
            {
                db.Quotes.Add(new QuoteRecord
                {
                    Quote = e.Command.ArgumentsAsString,
                    AddedBy = e.Command.ChatMessage.Username,
                    AddedAt = DateTime.UtcNow
                });

                await db.SaveChangesAsync();
            }

            client.SendMessage(e.GetChannel(client), $"@{e.Command.ChatMessage.Username}: Quote added.");
        }
    }
}
