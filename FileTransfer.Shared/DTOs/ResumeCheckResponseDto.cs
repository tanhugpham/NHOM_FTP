using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Shared.Responses;


namespace FileTransfer.Shared.DTOs
{
    public class ResumeCheckResponseDto : BaseResponseDto
    {
        public int LastChunkIndex { get; set; }

        public long BytesReceived { get; set; }

        public bool IsCompleted { get; set; }
    }
}
