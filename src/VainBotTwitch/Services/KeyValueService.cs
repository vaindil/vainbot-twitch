using System.Threading.Tasks;
using VainBotTwitch.Classes;

namespace VainBotTwitch.Services
{
    public static class KeyValueService
    {
        public static async Task<KeyValue> GetByKeyAsync(string key)
        {
            using var db = new VbContext();

            // returning a ValueTask without awaiting it in this function causes an Npgsql exception,
            // so it's done this way even though it's less optimal
            return await db.KeyValues.FindAsync(key);
        }

        public static async Task CreateOrUpdateAsync(string key, string value)
        {
            using var db = new VbContext();

            var kv = await db.KeyValues.FindAsync(key);
            if (kv == null)
                db.KeyValues.Add(new KeyValue(key, value));
            else
                kv.Value = value;

            await db.SaveChangesAsync();
        }

        public static async Task DeleteByKeyAsync(string key)
        {
            using var db = new VbContext();

            var kv = await db.KeyValues.FindAsync(key);
            if (kv != null)
            {
                db.KeyValues.Remove(kv);
                await db.SaveChangesAsync();
            }
        }
    }
}
