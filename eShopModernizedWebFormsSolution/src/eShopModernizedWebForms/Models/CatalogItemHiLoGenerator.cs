using Microsoft.EntityFrameworkCore;

namespace eShopModernizedWebForms.Models
{
    public class CatalogItemHiLoGenerator
    {
        private const int HiLoIncrement = 10;
        private int sequenceId = -1;
        private int remainningLoIds;
        private readonly object sequenceLock = new object();

        public int GetNextSequenceValue(CatalogDBContext db)
        {
            lock (sequenceLock)
            {
                if (remainningLoIds == 0)
                {
                    sequenceId = (int)db.Database
                        .SqlQueryRaw<long>("SELECT NEXT VALUE FOR catalog_hilo AS [Value]")
                        .AsEnumerable()
                        .Single();
                    remainningLoIds = HiLoIncrement - 1;
                    return sequenceId;
                }

                remainningLoIds--;
                return ++sequenceId;
            }
        }
    }
}
