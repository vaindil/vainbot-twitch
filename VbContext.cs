using MySql.Data.Entity;
using System.Data.Entity;
using VainBotTwitch.Classes;
using VainBotTwitch.DbConfigs;

namespace VainBotTwitch
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class VbContext : DbContext
    {
        public VbContext() : base("name=VbContext") { }

        public virtual DbSet<SlothyRecord> Slothies { get; set; }
        public virtual DbSet<MultiStreamer> MultiStreamers { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new SlothyRecordConfig());
            modelBuilder.Configurations.Add(new MultiStreamerConfig());
        }
    }
}
