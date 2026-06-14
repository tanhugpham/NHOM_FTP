using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Shared.DTOs;
using System.Collections.Generic;

namespace FileTransfer.Shared.Responses
{
    public class FileListResponseDto : BaseResponseDto
    {
        public List<FileInfoDto> Files { get; set; }
    }
}
