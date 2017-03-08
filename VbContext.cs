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
                .HasKey(r => r.Username);

            modelBuilder.Entity<SlothyRecord>()
                .Property(r => r.Username)
                .HasColumnName("username");

            modelBuilder.Entity<SlothyRecord>()
                .Property(r => r.Count)
                .IsRequired();

            base.OnModelCreating(modelBuilder);
        }
    }
}
