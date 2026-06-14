using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Shared.DTOs
{
    public class FileInfoDto
    {
        public string FileName { get; set; }

        public long FileSize { get; set; }
    }
}