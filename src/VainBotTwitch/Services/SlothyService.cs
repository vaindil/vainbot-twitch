using Microsoft.EntityFrameworkCore;
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
            var record = _slothyRecords.Find(x => x.UserId == userId);
            if (record != null)
                return record.Count;
            else
                return 0M;
        }

        public async Task<decimal> AddSlothiesAsync(string userId, decimal count)
        {
            if (userId == "45447900")
                return 0M;

            var isNew = false;
            var record = _slothyRecords.Find(x => x.UserId == userId);
            if (record != null)
            {
                record.Count += count;
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
