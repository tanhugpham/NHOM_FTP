using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

using FileTransfer.Server.Database;
using System.Linq;
using System.Threading.Tasks;

namespace FileTransfer.Server.Services
{
    public class AdminCleanupService
    {
        public async Task ClearLogsAsync()
        {
            using (AppDbContext db = new AppDbContext())
            {
                db.TransferHistories.RemoveRange(db.TransferHistories);
                db.ClientSessions.RemoveRange(db.ClientSessions);
                db.FileTransferStates.RemoveRange(db.FileTransferStates);
                db.SharedFiles.RemoveRange(db.SharedFiles);

                await db.SaveChangesAsync();
            }
        }
    }
}