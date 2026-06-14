using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Server.Entities
{
    public class ClientSession
    {
        public int Id { get; set; }

        public int UserId { get; set; }

        public string ClientIp { get; set; }

        public DateTime ConnectedAt { get; set; }

        public DateTime? DisconnectedAt { get; set; }

        public bool IsOnline { get; set; }
    }
}
