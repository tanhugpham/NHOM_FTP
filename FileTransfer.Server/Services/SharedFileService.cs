using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Server.Database;
using FileTransfer.Server.Entities;
namespace FileTransfer.Server.Services
{
    public class SharedFileService
    {
        public async Task<string> CreateShareCodeAsync(
            string ownerUsername,
            string fileName,
            string allowedUsername)
        {
            string shareCode =
                Guid.NewGuid()
                    .ToString("N")
                    .Substring(0, 8)
                    .ToUpper();

            using (AppDbContext db = new AppDbContext())
            {
                SharedFile sharedFile =
                    new SharedFile
                    {
                        OwnerUsername = ownerUsername,
                        FileName = fileName,
                        ShareCode = shareCode,
                        AllowedUsername = allowedUsername,
                        CreatedAt = DateTime.UtcNow,
                        IsActive = true
                    };

                db.SharedFiles.Add(sharedFile);

                await db.SaveChangesAsync();
            }

            return shareCode;
        }

        public SharedFile GetByShareCode(string shareCode)
        {
            using (AppDbContext db = new AppDbContext())
            {
                return db.SharedFiles
                    .FirstOrDefault(x =>
                        x.ShareCode == shareCode &&
                        x.IsActive);
            }
        }
    }
}
