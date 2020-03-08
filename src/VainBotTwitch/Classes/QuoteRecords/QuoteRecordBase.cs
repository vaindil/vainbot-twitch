using System;

namespace VainBotTwitch.Classes.QuoteRecords
{
    public abstract class QuoteRecordBase
    {
        public int Id { get; set; }

        public string Quote { get; set; }

        public string AddedBy { get; set; }

        public DateTime AddedAt { get; set; }

        public virtual string Name { get; }
    }
}
