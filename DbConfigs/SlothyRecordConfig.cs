using System.Data.Entity.ModelConfiguration;
using VainBotTwitch.Classes;

namespace VainBotTwitch.DbConfigs
{
    public class SlothyRecordConfig : EntityTypeConfiguration<SlothyRecord>
    {
        public SlothyRecordConfig()
        {
            ToTable("slothy_record");
            HasKey(r => r.UserId);
            
            Property(r => r.UserId).HasColumnName("user_id");
            Property(r => r.Count).IsRequired().HasPrecision(8, 2);
        }
    }
}
