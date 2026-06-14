using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Server.Entities
{
    public class FileTransferState
    {
        public int Id { get; set; }

        public string FileId { get; set; }

        public string FileName { get; set; }

        public long TotalBytes { get; set; }

        public long BytesReceived { get; set; }

        public int LastChunkIndex { get; set; }

        public bool IsCompleted { get; set; }

        public DateTime UpdatedAt { get; set; }
    }
}
