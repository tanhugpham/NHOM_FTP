using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Shared.DTOs
{
    public class FileChunkDto
    {
        public string FileId { get; set; }

        public byte[] ChunkData { get; set; }

        public int ChunkIndex { get; set; }

        public bool IsLastChunk { get; set; }
    }
}
