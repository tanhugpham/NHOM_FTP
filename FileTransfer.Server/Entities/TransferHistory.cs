using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Server.Entities
{
    public class TransferHistory
    {
        public int Id { get; set; }

        public string Username { get; set; }

        public string FileName { get; set; }

        public long FileSize { get; set; }

        public string TransferType { get; set; }

        public string Status { get; set; }

        public DateTime CreatedAt { get; set; }
    }
}
