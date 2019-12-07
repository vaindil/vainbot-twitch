using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using VainBotTwitch.Classes;

namespace VainBotTwitch.Services
{
    public class SlothyBetService
    {
        private List<SlothyBetRecord> _betRecords = new List<SlothyBetRecord>();

        public int BetCount
        {
            get
            {
                return _betRecords.Count;
            }
        }

        public async Task InitializeAsync()
        {
            try
            {
                using var db = new VbContext();
                _betRecords = await db.SlothyBetRecords.ToListAsync();
            }
            catch (Exception ex)
            {
                Utils.LogToConsole($"Error getting all slothy bet records from DB: {ex.Message}");
            }
        }

        public SlothyBetRecord GetCurrentBet(string userId)
        {
            return _betRecords.Find(x => x.UserId == userId);
        }

        public List<SlothyBetRecord> GetAllCurrentBets()
        {
            return _betRecords.ToList();
        }

        public async Task<bool> AddOrUpdateBetAsync(SlothyBetRecord record)
        {
            var update = false;
            var existing = _betRecords.Find(x => x.UserId == record.UserId);
            if (existing != null)
            {
                _betRecords.Remove(existing);
                update = true;
            }

            _betRecords.Add(record);

            try
            {
                using var db = new VbContext();

                if (!update)
                    db.SlothyBetRecords.Add(record);
                else
                    db.SlothyBetRecords.Update(record);

                await db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Utils.LogToConsole($"Error saving slothy bet record to DB: {ex.Message}");
                return false;
            }
        }

        public async Task<bool> ClearBetsAsync()
        {
            _betRecords.Clear();

            try
            {
                using var db = new VbContext();
                db.SlothyBetRecords.RemoveRange(db.SlothyBetRecords);
                await db.SaveChangesAsync();

                return true;
            }
            catch (Exception ex)
            {
                Utils.LogToConsole($"Error truncating slothy bets: {ex.Message}");
                return false;
            }
        }
    }
}
