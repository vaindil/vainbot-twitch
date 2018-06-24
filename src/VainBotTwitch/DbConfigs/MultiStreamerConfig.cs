using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VainBotTwitch.Classes;

namespace VainBotTwitch.DbConfigs
{
    public class MultiStreamerConfig : IEntityTypeConfiguration<MultiStreamer>
    {
        public void Configure(EntityTypeBuilder<MultiStreamer> builder)
        {
            builder.ToTable("multi_streamer");
            builder.HasKey(s => s.Username);

            builder.Property(s => s.Username).HasColumnName("username");
        }
    }
}
