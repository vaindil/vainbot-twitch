using System;

namespace VainBotTwitch.Classes
{
    public class LogEntry
    {
        public LogEntry() { }

        public LogEntry(string message)
        {
            Timestamp = DateTime.UtcNow;
            Message = message;
        }

        public int Id { get; set; }

        public DateTime Timestamp { get; set; }

        public string Message { get; set; }
    }
}
