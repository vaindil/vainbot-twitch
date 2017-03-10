using MySql.Data.Entity;
using System.Data.Entity;
using VainBotTwitch.Classes;

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
            modelBuilder.Entity<SlothyRecord>()
                .ToTable("slothy_record")
                .HasKey(r => r.UserId);

            modelBuilder.Entity<SlothyRecord>()
                .Property(r => r.UserId)
                .HasColumnName("user_id");

            modelBuilder.Entity<SlothyRecord>()
                .Property(r => r.Count)
                .IsRequired()
                .HasPrecision(8, 2);

            modelBuilder.Entity<MultiStreamer>()
                .ToTable("multi_streamer")
                .HasKey(s => s.Username);

            modelBuilder.Entity<MultiStreamer>()
                .Property(s => s.Username)
                .HasColumnName("username")
                .HasMaxLength(50);

            base.OnModelCreating(modelBuilder);
        }
    }
}
