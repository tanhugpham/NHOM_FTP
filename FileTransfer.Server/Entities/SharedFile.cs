using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Server.Entities;
namespace FileTransfer.Server.Entities
{
    public class SharedFile
    {
        public int Id { get; set; }

        public string OwnerUsername { get; set; }

        public string FileName { get; set; }

        public string ShareCode { get; set; }

        public string AllowedUsername { get; set; }

        public DateTime CreatedAt { get; set; }

        public bool IsActive { get; set; }
    }
}
