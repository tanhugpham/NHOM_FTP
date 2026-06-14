using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Shared.DTOs
{
    public class FileStartRequestDto
    {
        public string FileId { get; set; }

        public string FileName { get; set; }

        public long TotalBytes { get; set; }
    }
}
