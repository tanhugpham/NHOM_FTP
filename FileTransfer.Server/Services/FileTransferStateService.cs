using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Server.Database;
using FileTransfer.Server.Entities;


namespace FileTransfer.Server.Services
{
    public class FileTransferStateService
    {
        public async Task SaveProgressAsync(
            string fileId,
            string fileName,
            long totalBytes,
            long bytesReceived,
            int lastChunkIndex,
            bool isCompleted)
        {
            using (AppDbContext db = new AppDbContext())
            {
                FileTransferState state =
                    db.FileTransferStates
                    .FirstOrDefault(x => x.FileId == fileId);

                if (state == null)
                {
                    state = new FileTransferState
                    {
                        FileId = fileId
                    };

                    db.FileTransferStates.Add(state);
                }

                state.FileName = fileName;
                state.TotalBytes = totalBytes;
                state.BytesReceived = bytesReceived;
                state.LastChunkIndex = lastChunkIndex;
                state.IsCompleted = isCompleted;
                state.UpdatedAt = DateTime.UtcNow;

                await db.SaveChangesAsync();
            }
        }

        public FileTransferState GetByFileId(string fileId)
        {
            using (AppDbContext db = new AppDbContext())
            {
                return db.FileTransferStates
                    .FirstOrDefault(x => x.FileId == fileId);
            }
        }
    }
}
