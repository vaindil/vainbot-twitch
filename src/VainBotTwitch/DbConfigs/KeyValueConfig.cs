using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VainBotTwitch.Classes;

namespace VainBotTwitch.DbConfigs
{
    public class KeyValueConfig : IEntityTypeConfiguration<KeyValue>
    {
        public void Configure(EntityTypeBuilder<KeyValue> builder)
        {
            builder.ToTable("key_value");
            builder.HasKey(e => e.Key);

            builder.Property(e => e.Key).HasColumnName("key");
            builder.Property(e => e.Value).IsRequired().HasColumnName("value");
        }
    }
}
