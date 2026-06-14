using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace FileTransfer.Shared.Enums
{
    
    public enum MessageType
    {
        Ping,
        Register,
        Login,
        FileUpload = 3,
        FileStart,
        FileChunk,
        FileComplete,
        ResumeCheck,
        Error,
        GetFileList,
        DownloadFile, 
        CreateShareCode,
        DownloadSharedFile,
        ServerPushFile,
        CheckForPush
    }
}
