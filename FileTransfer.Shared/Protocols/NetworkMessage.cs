using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using FileTransfer.Shared.Enums;

namespace FileTransfer.Shared.Protocols
{
    public class NetworkMessage
    {
        public MessageType Type { get; set; }

        public string JsonBody { get; set; }    
    }
}
