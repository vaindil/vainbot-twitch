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

            base.OnModelCreating(modelBuilder);
        }
    }
}
