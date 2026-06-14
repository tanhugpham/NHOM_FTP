using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Shared.Responses;

namespace FileTransfer.Shared.DTOs
{
    public class CreateShareCodeResponseDto
        : BaseResponseDto
    {
        public string ShareCode { get; set; }
    }
}
