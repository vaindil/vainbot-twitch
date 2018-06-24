using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VainBotTwitch.Classes;

namespace VainBotTwitch.DbConfigs
{
    public class SlothyRecordConfig : IEntityTypeConfiguration<SlothyRecord>
    {
        public void Configure(EntityTypeBuilder<SlothyRecord> builder)
        {
            builder.ToTable("slothy_record");
            builder.HasKey(r => r.UserId);

            builder.Property(r => r.UserId).HasColumnName("user_id");
            builder.Property(r => r.Count).IsRequired().HasColumnName("count");
        }
    }
}
