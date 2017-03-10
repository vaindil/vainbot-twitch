using System.Data.Entity.ModelConfiguration;
using VainBotTwitch.Classes;

namespace VainBotTwitch.DbConfigs
{
    public class MultiStreamerConfig : EntityTypeConfiguration<MultiStreamer>
    {
        public MultiStreamerConfig()
        {
            ToTable("multi_streamer");
            HasKey(s => s.Username);

            Property(s => s.Username).HasColumnName("username").HasMaxLength(50);
        }
    }
}
