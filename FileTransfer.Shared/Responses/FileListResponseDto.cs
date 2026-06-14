using System.Collections.Generic;
using FileTransfer.Shared.DTOs;

namespace FileTransfer.Shared.Responses
{
    public class FileListResponseDto : BaseResponseDto
    {
        public List<FileInfoDto> Files { get; set; }
    }
}
