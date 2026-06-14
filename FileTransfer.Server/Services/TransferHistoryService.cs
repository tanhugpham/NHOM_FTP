using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Server.Entities;
using FileTransfer.Server.Database;

namespace FileTransfer.Server.Services
{
    public class TransferHistoryService
    {
        public async Task SaveAsync(
            string username,
            string fileName,
            long fileSize,
            string transferType,
            string status)
        {
            using (AppDbContext db = new AppDbContext())
            {
                TransferHistory history =
                    new TransferHistory
                    {
                        Username = username,
                        FileName = fileName,
                        FileSize = fileSize,
                        TransferType = transferType,
                        Status = status,
                        CreatedAt = DateTime.UtcNow
                    };

                db.TransferHistories.Add(history);

                await db.SaveChangesAsync();
            }
        }
    }
}
