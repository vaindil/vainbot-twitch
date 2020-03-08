using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using VainBotTwitch.Classes.QuoteRecords;

namespace VainBotTwitch.DbConfigs
{
    public class CrendorQuoteRecordConfig : IEntityTypeConfiguration<CrendorQuoteRecord>
    {
        public void Configure(EntityTypeBuilder<CrendorQuoteRecord> builder)
        {
            builder.ToTable("quote_record");
            builder.HasKey(e => e.Id);

            builder.Property(e => e.Id).HasColumnName("id");
            builder.Property(e => e.Quote).IsRequired().HasColumnName("quote");
            builder.Property(e => e.AddedBy).IsRequired().HasColumnName("added_by");
            builder.Property(e => e.AddedAt).IsRequired().HasColumnName("added_at");
        }
    }
}
