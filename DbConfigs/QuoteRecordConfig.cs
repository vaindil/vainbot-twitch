using System.Data.Entity.ModelConfiguration;
using VainBotTwitch.Classes;

namespace VainBotTwitch.DbConfigs
{
    public class QuoteRecordConfig : EntityTypeConfiguration<QuoteRecord>
    {
        public QuoteRecordConfig()
        {
            ToTable("quote_record");
            HasKey(e => e.Id);

            Property(e => e.Quote).IsRequired().HasColumnName("quote").HasMaxLength(300);
            Property(e => e.AddedBy).IsRequired().HasColumnName("added_by").HasMaxLength(75);
            Property(e => e.AddedAt).IsRequired().HasColumnName("added_at");
        }
    }
}
