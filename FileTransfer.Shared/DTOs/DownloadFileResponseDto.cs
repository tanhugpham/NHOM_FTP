using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Shared.Responses;


namespace FileTransfer.Shared.DTOs
{
    public class DownloadFileResponseDto : BaseResponseDto
    {
        public string FileName { get; set; }

        public byte[] FileData { get; set; }
    }
}