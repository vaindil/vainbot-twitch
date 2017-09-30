using System;

namespace VainBotTwitch.Classes
{
    public class QuoteRecord
    {
        public int Id { get; set; }

        public string Quote { get; set; }

        public string AddedBy { get; set; }

        public DateTime AddedAt { get; set; }
    }
}
