using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Shared.DTOs
{
    public class CreateShareCodeRequestDto
    {
        public string FileName { get; set; }

        public string AllowedUsername { get; set; }
    }
}
