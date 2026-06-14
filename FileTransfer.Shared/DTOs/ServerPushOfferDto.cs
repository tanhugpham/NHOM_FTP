using System.Collections.Generic;

namespace FileTransfer.Shared.DTOs
{
    public class PushFileInfo
    {
        public string FileName { get; set; }
        public long FileSize { get; set; }
    }

    public class ServerPushOfferDto
    {
        public string OfferId { get; set; }
        public string FromUser { get; set; }
        public List<PushFileInfo> Files { get; set; }
        public long TotalSize { get; set; }
        public string Status { get; set; } // Pending, Accepted, Rejected
    }
}