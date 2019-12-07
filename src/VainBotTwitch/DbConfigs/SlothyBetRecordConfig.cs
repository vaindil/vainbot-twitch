using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using System;
using VainBotTwitch.Classes;

namespace VainBotTwitch.DbConfigs
{
    public class SlothyBetRecordConfig : IEntityTypeConfiguration<SlothyBetRecord>
    {
        public void Configure(EntityTypeBuilder<SlothyBetRecord> builder)
        {
            builder.ToTable("slothy_bet_record");
            builder.HasKey(e => e.UserId);

            builder.Property(e => e.UserId).HasColumnName("user_id");
            builder.Property(e => e.Amount).IsRequired().HasColumnName("amount");
            builder.Property(e => e.BetType)
                .IsRequired()
                .HasColumnName("bet_type")
                .HasColumnType("text")
                .HasConversion(
                    x => x.ToString().ToLowerInvariant(),
                    y => (SlothyBetType)Enum.Parse(typeof(SlothyBetType), y, true));

            builder.Ignore(e => e.BetTypeString);
        }
    }
}
