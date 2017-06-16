using System.Data.Entity.ModelConfiguration;
using VainBotTwitch.Classes;

namespace VainBotTwitch.DbConfigs
{
    public class AllowedElenaConfig : EntityTypeConfiguration<AllowedElena>
    {
        public AllowedElenaConfig()
        {
            ToTable("allowed_elena");
            HasKey(e => e.Username);

            Property(e => e.UnbannedBy).IsRequired().HasColumnName("unbanned_by");
            Property(e => e.UnbannedAt).IsRequired().HasColumnName("unbanned_at");
        }
    }
}
