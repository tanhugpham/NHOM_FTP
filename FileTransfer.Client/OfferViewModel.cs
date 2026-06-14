using System;
using System.Collections.Generic;
using System.Linq;

namespace FileTransfer.Client
{
    public class OfferViewModel
    {
        public string OfferId { get; set; }
        public string FromUser { get; set; }
        public int FileCount { get; set; }
        public string SizeDisplay { get; set; }
        public DateTime ReceivedAt { get; set; }
        public string ReceivedAtDisplay { get; set; }
        public List<string> FileNames { get; set; }
        public long TotalSize { get; set; }
        public Shared.DTOs.ServerPushOfferDto OriginalOffer { get; set; }
        public bool IsDownloaded { get; set; }
        public string StatusDisplay { get; set; } = "⏳ Pending";
    }
}