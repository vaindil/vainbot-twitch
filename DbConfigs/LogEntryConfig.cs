using System.Data.Entity.ModelConfiguration;
using VainBotTwitch.Classes;

namespace VainBotTwitch.DbConfigs
{
    public class LogEntryConfig : EntityTypeConfiguration<LogEntry>
    {
        public LogEntryConfig()
        {
            ToTable("log_entry");
            HasKey(e => e.Id);
            
            Property(e => e.Timestamp).IsRequired();
            Property(e => e.Message).IsRequired().HasMaxLength(2000);
        }
    }
}
