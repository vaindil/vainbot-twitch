using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using VainBotTwitch.Classes;

namespace VainBotTwitch.Services
{
    public class SlothyService
    {
        private List<SlothyRecord> _slothyRecords;

        public async Task InitializeAsync()
        {
            using var db = new VbContext();

            _slothyRecords = await db.Slothies.ToListAsync();
        }

        public decimal GetSlothyCount(string userId)
        {
            if (userId == "45447900")
                return decimal.MinValue;

            var record = _slothyRecords.Find(x => x.UserId == userId);
            if (record != null)
                return record.Count;
            else
                return 0M;
        }

        public async Task<decimal> AddSlothiesAsync(string userId, decimal count)
        {
            var isNew = false;
            var record = _slothyRecords.Find(x => x.UserId == userId);
            if (record != null)
            {
                try
                {
                    record.Count += count;
                }
                catch (OverflowException)
                {
                    if (count > 0)
                        record.Count = decimal.MaxValue;
                    else
                        record.Count = decimal.MinValue;
                }
            }
            else
            {
                isNew = true;
                record = new SlothyRecord
                {
                    UserId = userId,
                    Count = count
                };

                _slothyRecords.Add(record);
            }

            using (var db = new VbContext())
            {
                if (isNew)
                    db.Slothies.Add(record);
                else
                    db.Slothies.Update(record);

                await db.SaveChangesAsync();
            }

            return record.Count;
        }
    }
}
