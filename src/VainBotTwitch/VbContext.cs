﻿using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using VainBotTwitch.Classes;
using VainBotTwitch.Classes.QuoteRecords;
using VainBotTwitch.DbConfigs;

namespace VainBotTwitch
{
    public class VbContext : DbContext
    {
        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var config = new ConfigurationBuilder().AddJsonFile("config.json").Build();
            optionsBuilder.UseNpgsql(config["connectionString"]);
        }

        public DbSet<SlothyRecord> Slothies { get; set; }
        public DbSet<MultiStreamer> MultiStreamers { get; set; }
        public DbSet<CrendorQuoteRecord> CrendorQuotes { get; set; }
        public DbSet<OmarQuoteRecord> OmarQuotes { get; set; }
        public DbSet<SlothyBetRecord> SlothyBetRecords { get; set; }
        public DbSet<KeyValue> KeyValues { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.ApplyConfiguration(new SlothyRecordConfig());
            modelBuilder.ApplyConfiguration(new MultiStreamerConfig());
            modelBuilder.ApplyConfiguration(new CrendorQuoteRecordConfig());
            modelBuilder.ApplyConfiguration(new OmarQuoteRecordConfig());
            modelBuilder.ApplyConfiguration(new SlothyBetRecordConfig());
            modelBuilder.ApplyConfiguration(new KeyValueConfig());
        }
    }
}
