﻿using MySql.Data.Entity;
using System.Data.Entity;
using VainBotTwitch.Classes;
using VainBotTwitch.DbConfigs;

namespace VainBotTwitch
{
    [DbConfigurationType(typeof(MySqlEFConfiguration))]
    public class VbContext : DbContext
    {
        public VbContext() : base("name=VbContext")
        {
        }

        public virtual DbSet<SlothyRecord> Slothies { get; set; }
        public virtual DbSet<MultiStreamer> MultiStreamers { get; set; }
        public virtual DbSet<LogEntry> LogEntries { get; set; }
        public virtual DbSet<QuoteRecord> Quotes { get; set; }

        protected override void OnModelCreating(DbModelBuilder modelBuilder)
        {
            modelBuilder.Configurations.Add(new SlothyRecordConfig());
            modelBuilder.Configurations.Add(new MultiStreamerConfig());
            modelBuilder.Configurations.Add(new LogEntryConfig());
            modelBuilder.Configurations.Add(new QuoteRecordConfig());
        }
    }
}
