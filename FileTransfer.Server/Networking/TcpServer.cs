using FileTransfer.Server.Entities;
using FileTransfer.Server.Services;

using FileTransfer.Shared.DTOs;
using FileTransfer.Shared.Enums;
using FileTransfer.Shared.Helpers;
using FileTransfer.Shared.Protocols;
using FileTransfer.Shared.Responses;
using FileTransfer.Shared.Security;

using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Security;
using System.Net.Sockets;
using System.Security.Authentication;
using System.Security.Cryptography.X509Certificates;
using System.Threading;
using System.Threading.Tasks;

namespace FileTransfer.Server.Networking
{
    public class TcpServer
    {
        private TcpListener _listener;
        private CancellationTokenSource _cts;

        private AuthService _authService =
            new AuthService();

        private TransferHistoryService _historyService =
            new TransferHistoryService();

        private FileTransferStateService _stateService =
            new FileTransferStateService();

        private SharedFileService _sharedFileService =
            new SharedFileService();

        private ConcurrentDictionary<string, string> _uploadingFiles =
            new ConcurrentDictionary<string, string>();

        private ConcurrentDictionary<TcpClient, string> _clientUsers =
            new ConcurrentDictionary<TcpClient, string>();

        // Pending push offers: username -> (offerId -> ServerPushOfferDto)
        private ConcurrentDictionary<string, ConcurrentDictionary<string, ServerPushOfferDto>> _pendingOffers =
            new ConcurrentDictionary<string, ConcurrentDictionary<string, ServerPushOfferDto>>();
        // Active offers ready for delivery: offerId -> (username, filePaths)
        private ConcurrentDictionary<string, (string username, string[] filePaths)> _activeOffers =
            new ConcurrentDictionary<string, (string username, string[] filePaths)>();

        private X509Certificate2 _serverCertificate;
        private X509Certificate2 _caCertificate;

        public event Action<string> OnLog;

        public bool IsRunning { get; private set; }

        /// <summary>
        /// Returns a snapshot of online usernames.
        /// </summary>
        public List<string> GetOnlineUsers()
        {
            return _clientUsers.Values.ToList();
        }

        /// <summary>
        /// Queues multiple files as a PushOffer for the specified user.
        /// The next time the client polls, they'll receive the offer (not file data).
        /// </summary>
        public Task<bool> PushFilesToClientAsync(
            string username, string[] filePaths)
        {
            if (filePaths == null || filePaths.Length == 0)
                return Task.FromResult(false);

            var fileList = new List<PushFileInfo>();
            long totalSize = 0;
            string offerId = Guid.NewGuid().ToString("N").Substring(0, 8);

            foreach (string fp in filePaths)
            {
                if (!File.Exists(fp)) continue;
                var fi = new FileInfo(fp);
                fileList.Add(new PushFileInfo
                {
                    FileName = fi.Name,
                    FileSize = fi.Length
                });
                totalSize += fi.Length;
            }

            if (fileList.Count == 0) return Task.FromResult(false);

            var offer = new ServerPushOfferDto
            {
                OfferId = offerId,
                FromUser = "Server",
                Files = fileList,
                TotalSize = totalSize,
                Status = "Pending"
            };

            // Safe multi-offer add
            var inner = _pendingOffers.GetOrAdd(username,
                _ => new ConcurrentDictionary<string, ServerPushOfferDto>());
            inner.TryAdd(offer.OfferId, offer);
            _activeOffers.TryAdd(offerId, (username, filePaths));

            OnLog?.Invoke("Push offer created for " + username + ": " 
                + fileList.Count + " files, " + totalSize + " bytes, offerId=" + offerId);

            return Task.FromResult(true);
        }

        public async Task StartAsync(int port)
        {
            if (IsRunning)
                return;

            try
            {
                string baseDir = AppDomain.CurrentDomain.BaseDirectory;

                string serverCertPath = Path.Combine(
                    baseDir,
                    ConfigurationManager.AppSettings["ServerCertificatePath"]);

                string caCertPath = Path.Combine(
                    baseDir,
                    ConfigurationManager.AppSettings["CACertificatePath"]);

                _serverCertificate = CertificateHelper.LoadServerCertificate(
                    serverCertPath);

                _caCertificate = CertificateHelper.LoadCACertificate(
                    caCertPath);

                OnLog?.Invoke("Server certificate loaded successfully");
            }
            catch (Exception ex)
            {
                OnLog?.Invoke(
                    "Failed to load server certificate: " + ex.Message);

                throw;
            }

            _cts = new CancellationTokenSource();

            _listener = new TcpListener(IPAddress.Any, port);
            _listener.Start();

            IsRunning = true;

            OnLog?.Invoke("Server started on port " + port);

            while (!_cts.Token.IsCancellationRequested)
            {
                try
                {
                    TcpClient client =
                        await _listener.AcceptTcpClientAsync();

                    OnLog?.Invoke(
                        "Client connected: "
                        + client.Client.RemoteEndPoint);

                    _ = HandleClientAsync(client);
                }
                catch (ObjectDisposedException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    OnLog?.Invoke("Accept error: " + ex.Message);
                }
            }
        }

        public void Stop()
        {
            if (!IsRunning)
                return;

            _cts.Cancel();
            _listener.Stop();

            IsRunning = false;

            _pendingOffers.Clear();
            _activeOffers.Clear();
            OnLog?.Invoke("Server stopped - all offer caches cleared");
        }

        private async Task HandleClientAsync(TcpClient client)
        {
            string clientIp = "Unknown";
            SslStream sslStream = null;

            try
            {
                if (client.Client.RemoteEndPoint is IPEndPoint endPoint)
                {
                    clientIp =
                        endPoint.Address.ToString();
                }

                NetworkStream networkStream =
                    client.GetStream();

                sslStream = new SslStream(
                    networkStream,
                    false,
                    ValidateClientCertificate);

                try
                {
                    await sslStream.AuthenticateAsServerAsync(
                        _serverCertificate,
                        clientCertificateRequired: true,
                        enabledSslProtocols: System.Security.Authentication.SslProtocols.Tls12,
                        checkCertificateRevocation: false);

                    OnLog?.Invoke(
                        "TLS handshake completed: "
                        + clientIp);
                }
                catch (AuthenticationException authEx)
                {
                    OnLog?.Invoke(
                        "TLS handshake failed for "
                        + clientIp
                        + ": "
                        + authEx.Message);

                    return;
                }

                while (client.Connected)
                {
                    string json =
                        await TcpMessageHelper.ReadStringAsync(sslStream);

                    BaseResponseDto responseDto;

                    try
                    {
                        NetworkMessage networkMessage =
                            JsonHelper.Deserialize<NetworkMessage>(json);

                        responseDto =
                            await HandleNetworkMessageAsync(
                                networkMessage,
                                client,
                                clientIp);
                    }
                    catch (Exception ex)
                    {
                        responseDto =
                            new BaseResponseDto
                            {
                                Success = false,
                                Message =
                                    "Server parse/process error: "
                                    + ex.Message
                            };
                    }

                    string responseJson =
                        JsonHelper.Serialize(responseDto);

                    await TcpMessageHelper.SendStringAsync(
                        sslStream,
                        responseJson);
                }
            }
            catch (Exception ex)
            {
                OnLog?.Invoke("Client error: " + ex.Message);
            }
            finally
            {
                if (_clientUsers.ContainsKey(client))
                {
                    _clientUsers.TryRemove(client, out _);
                }

                if (sslStream != null)
                {
                    sslStream.Close();
                    sslStream = null;
                }

                client.Close();

                OnLog?.Invoke("Client disconnected: " + clientIp);
            }
        }

        private bool ValidateClientCertificate(
            object sender,
            X509Certificate certificate,
            X509Chain chain,
            SslPolicyErrors sslPolicyErrors)
        {
            if (certificate == null)
            {
                OnLog?.Invoke(
                    "Client certificate required but not provided");

                return false;
            }

            X509Certificate2 cert2 =
                new X509Certificate2(certificate);

            try
            {
                CertificateValidation.ValidateCertificateNotExpired(cert2);

                CertificateValidation.ValidateCertificateChain(
                    cert2,
                    _caCertificate);

                CertificateValidation.ValidateNotSelfSigned(cert2);

                OnLog?.Invoke(
                    "Client certificate validated: "
                    + cert2.Subject);

                return true;
            }
            catch (Exception ex)
            {
                OnLog?.Invoke(
                    "Client certificate validation failed: "
                    + ex.Message);

                return false;
            }
        }

        private async Task<BaseResponseDto> HandleNetworkMessageAsync(
            NetworkMessage networkMessage,
            TcpClient client,
            string clientIp)
        {
            switch (networkMessage.Type)
            {
                case MessageType.Register:
                    return await HandleRegisterAsync(networkMessage);

                case MessageType.Login:
                    return await HandleLoginAsync(
                        networkMessage,
                        client,
                        clientIp);

                case MessageType.FileStart:
                    return HandleFileStart(
                        networkMessage,
                        client);

                case MessageType.FileChunk:
                    return HandleFileChunk(networkMessage);

                case MessageType.FileComplete:
                    return HandleFileComplete(
                        networkMessage,
                        client);

                case MessageType.GetFileList:
                    return HandleGetFileList(client);

                case MessageType.DownloadFile:
                    return HandleDownloadFile(
                        networkMessage,
                        client);

                case MessageType.ResumeCheck:
                    return HandleResumeCheck(networkMessage);
                case MessageType.CreateShareCode:
                    return HandleCreateShareCode(networkMessage, client);

                case MessageType.DownloadSharedFile:
                    return HandleDownloadSharedFile(networkMessage, client);

                case MessageType.CheckForPush:
                    return HandleCheckForPush(client);

                case MessageType.Logout:
                    return HandleLogout(client);

                case MessageType.PushAccept:
                    return HandlePushAccept(networkMessage, client);

                case MessageType.PushReject:
                    return HandlePushReject(networkMessage, client);

                default:
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Unknown message type"
                    };
            }
        }

        private async Task<BaseResponseDto> HandleRegisterAsync(
            NetworkMessage networkMessage)
        {
            RegisterRequestDto registerDto =
                JsonHelper.Deserialize<RegisterRequestDto>(
                    networkMessage.JsonBody);

            OnLog?.Invoke(
                "Register request: "
                + registerDto.Username);

            return await _authService.RegisterAsync(
                registerDto.Username,
                registerDto.Password);
        }

        private async Task<BaseResponseDto> HandleLoginAsync(
            NetworkMessage networkMessage,
            TcpClient client,
            string clientIp)
        {
            LoginRequestDto loginDto =
                JsonHelper.Deserialize<LoginRequestDto>(
                    networkMessage.JsonBody);

            OnLog?.Invoke(
                "Login request: "
                + loginDto.Username);

            BaseResponseDto response =
                await _authService.LoginAsync(
                    loginDto.Username,
                    loginDto.Password,
                    clientIp);

            if (response.Success)
            {
                _clientUsers[client] =
                    loginDto.Username;

                OnLog?.Invoke(
                    "User session saved: "
                    + loginDto.Username);
            }

            return response;
        }

        private BaseResponseDto HandleFileStart(
            NetworkMessage networkMessage,
            TcpClient client)
        {
            FileStartRequestDto startDto =
                JsonHelper.Deserialize<FileStartRequestDto>(
                    networkMessage.JsonBody);

            string username =
                GetCurrentUsername(client);

            string storageFolder =
                GetUserStorageFolder(username);

            string safeFileName =
                Path.GetFileName(startDto.FileName);

            string savePath =
                Path.Combine(storageFolder, safeFileName);

            File.WriteAllBytes(savePath, new byte[0]);

            _uploadingFiles[startDto.FileId] =
                savePath;

            _stateService.SaveProgressAsync(
                startDto.FileId,
                safeFileName,
                startDto.TotalBytes,
                0,
                -1,
                false).Wait();

            OnLog?.Invoke(
                "File start: "
                + username
                + "/"
                + safeFileName);

            return new BaseResponseDto
            {
                Success = true,
                Message = "File start OK"
            };
        }

        private BaseResponseDto HandleFileChunk(
            NetworkMessage networkMessage)
        {
            FileChunkDto chunkDto =
                JsonHelper.Deserialize<FileChunkDto>(
                    networkMessage.JsonBody);

            if (!_uploadingFiles.ContainsKey(chunkDto.FileId))
            {
                FileTransferState state =
                    _stateService.GetByFileId(chunkDto.FileId);

                if (state == null)
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "FileId not found"
                    };
                }

                string restoredPath =
                    FindFilePathByName(
                        Path.GetFileName(state.FileName));

                if (string.IsNullOrWhiteSpace(restoredPath))
                {
                    return new BaseResponseDto
                    {
                        Success = false,
                        Message = "Cannot restore upload file path"
                    };
                }

                _uploadingFiles[chunkDto.FileId] =
                    restoredPath;

                OnLog?.Invoke(
                    "Restored upload path: "
                    + restoredPath);
            }

            string savePath =
                _uploadingFiles[chunkDto.FileId];

            using (FileStream fs =
                new FileStream(
                    savePath,
                    FileMode.Append,
                    FileAccess.Write))
            {
                byte[] decryptedChunk =
                    AesEncryptionHelper.Decrypt(
                        chunkDto.ChunkData);

                fs.Write(
                    decryptedChunk,
                    0,
                    decryptedChunk.Length);
            }

            FileInfo info =
                new FileInfo(savePath);

            _stateService.SaveProgressAsync(
                chunkDto.FileId,
                Path.GetFileName(savePath),
                info.Length,
                info.Length,
                chunkDto.ChunkIndex,
                false).Wait();

            OnLog?.Invoke(
                "Chunk received: "
                + chunkDto.ChunkIndex);

            return new BaseResponseDto
            {
                Success = true,
                Message = "Chunk OK"
            };
        }

        private BaseResponseDto HandleFileComplete(
            NetworkMessage networkMessage,
            TcpClient client)
        {
            FileCompleteDto completeDto =
                JsonHelper.Deserialize<FileCompleteDto>(
                    networkMessage.JsonBody);

            string username =
                GetCurrentUsername(client);

            string safeFileName =
                Path.GetFileName(completeDto.FileName);

            string completePath;

            if (_uploadingFiles.ContainsKey(completeDto.FileId))
            {
                completePath =
                    _uploadingFiles[completeDto.FileId];
            }
            else
            {
                completePath =
                    Path.Combine(
                        GetUserStorageFolder(username),
                        safeFileName);
            }

            long completeSize = 0;

            if (File.Exists(completePath))
            {
                completeSize =
                    new FileInfo(completePath).Length;
            }

            _stateService.SaveProgressAsync(
                completeDto.FileId,
                safeFileName,
                completeSize,
                completeSize,
                -1,
                true).Wait();

            _historyService.SaveAsync(
                username,
                safeFileName,
                completeSize,
                "Upload",
                "Success").Wait();

            OnLog?.Invoke(
                "File upload complete: "
                + username
                + "/"
                + safeFileName);

            if (_uploadingFiles.ContainsKey(completeDto.FileId))
            {
                _uploadingFiles.TryRemove(completeDto.FileId, out _);
            }

            return new BaseResponseDto
            {
                Success = true,
                Message = "Upload hoàn tất: " + safeFileName
            };
        }

        private BaseResponseDto HandleGetFileList(
            TcpClient client)
        {
            string username =
                GetCurrentUsername(client);

            string storageFolder =
                GetUserStorageFolder(username);

            List<FileInfoDto> files =
                new List<FileInfoDto>();

            foreach (string filePath in Directory.GetFiles(storageFolder))
            {
                FileInfo info =
                    new FileInfo(filePath);

                files.Add(
                    new FileInfoDto
                    {
                        FileName = info.Name,
                        FileSize = info.Length
                    });
            }

            OnLog?.Invoke(
                "File list requested by: "
                + username);

            return new FileListResponseDto
            {
                Success = true,
                Message = "Lấy danh sách file thành công",
                Files = files
            };
        }

        private BaseResponseDto HandleDownloadFile(
            NetworkMessage networkMessage,
            TcpClient client)
        {
            DownloadFileRequestDto request =
                JsonHelper.Deserialize<DownloadFileRequestDto>(
                    networkMessage.JsonBody);

            string username =
                GetCurrentUsername(client);

            string storageFolder =
                GetUserStorageFolder(username);

            string safeFileName =
                Path.GetFileName(request.FileName);

            string filePath =
                Path.Combine(storageFolder, safeFileName);

            if (!File.Exists(filePath))
            {
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "File không tồn tại hoặc bạn không có quyền tải file này"
                };
            }

            byte[] fileData =
                File.ReadAllBytes(filePath);

            long fileSize =
                new FileInfo(filePath).Length;

            _historyService.SaveAsync(
                username,
                safeFileName,
                fileSize,
                "Download",
                "Success").Wait();

            OnLog?.Invoke(
                "Download requested: "
                + username
                + "/"
                + safeFileName);

            return new DownloadFileResponseDto
            {
                Success = true,
                Message = "Download file thành công",
                FileName = safeFileName,
                FileData = fileData
            };
        }

        private BaseResponseDto HandleCreateShareCode(
    NetworkMessage networkMessage,
    TcpClient client)
        {
            CreateShareCodeRequestDto request =
                JsonHelper.Deserialize<CreateShareCodeRequestDto>(
                    networkMessage.JsonBody);

            string ownerUsername =
                GetCurrentUsername(client);

            string safeFileName =
                Path.GetFileName(request.FileName);

            string ownerFolder =
                GetUserStorageFolder(ownerUsername);

            string filePath =
                Path.Combine(ownerFolder, safeFileName);

            if (!File.Exists(filePath))
            {
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "File không tồn tại trong tài khoản của bạn"
                };
            }

            string shareCode =
                _sharedFileService.CreateShareCodeAsync(
                    ownerUsername,
                    safeFileName,
                    request.AllowedUsername).Result;

            OnLog?.Invoke(
                ownerUsername
                + " created share code for "
                + safeFileName
                + ": "
                + shareCode);

            return new CreateShareCodeResponseDto
            {
                Success = true,
                Message = "Tạo mã chia sẻ thành công",
                ShareCode = shareCode
            };
        }

        private BaseResponseDto HandleDownloadSharedFile(
    NetworkMessage networkMessage,
    TcpClient client)
        {
            DownloadSharedFileRequestDto request =
                JsonHelper.Deserialize<DownloadSharedFileRequestDto>(
                    networkMessage.JsonBody);

            string currentUsername =
                GetCurrentUsername(client);

            SharedFile sharedFile =
                _sharedFileService.GetByShareCode(request.ShareCode);

            if (sharedFile == null)
            {
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Mã chia sẻ không hợp lệ hoặc đã bị khóa"
                };
            }

            if (!string.IsNullOrWhiteSpace(sharedFile.AllowedUsername) &&
                sharedFile.AllowedUsername != currentUsername)
            {
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "Bạn không có quyền tải file này"
                };
            }

            string ownerFolder =
                GetUserStorageFolder(sharedFile.OwnerUsername);

            string safeFileName =
                Path.GetFileName(sharedFile.FileName);

            string filePath =
                Path.Combine(ownerFolder, safeFileName);

            if (!File.Exists(filePath))
            {
                return new BaseResponseDto
                {
                    Success = false,
                    Message = "File chia sẻ không còn tồn tại trên server"
                };
            }

            byte[] fileData =
                File.ReadAllBytes(filePath);

            long fileSize =
                new FileInfo(filePath).Length;

            _historyService.SaveAsync(
                currentUsername,
                safeFileName,
                fileSize,
                "DownloadShared",
                "Success").Wait();

            OnLog?.Invoke(
                currentUsername
                + " downloaded shared file from "
                + sharedFile.OwnerUsername
                + ": "
                + safeFileName);

            return new DownloadFileResponseDto
            {
                Success = true,
                Message = "Download file chia sẻ thành công",
                FileName = safeFileName,
                FileData = fileData
            };
        }

        private BaseResponseDto HandleResumeCheck(
            NetworkMessage networkMessage)
        {
            ResumeCheckRequestDto request =
                JsonHelper.Deserialize<ResumeCheckRequestDto>(
                    networkMessage.JsonBody);

            FileTransferState state =
                _stateService.GetByFileId(request.FileId);

            if (state == null)
            {
                return new ResumeCheckResponseDto
                {
                    Success = true,
                    Message = "Không có trạng thái cũ",
                    LastChunkIndex = -1,
                    BytesReceived = 0,
                    IsCompleted = false
                };
            }

            return new ResumeCheckResponseDto
            {
                Success = true,
                Message = "Resume state found",
                LastChunkIndex = state.LastChunkIndex,
                BytesReceived = state.BytesReceived,
                IsCompleted = state.IsCompleted
            };
        }

        private BaseResponseDto HandleCheckForPush(TcpClient client)
        {
            string username = GetCurrentUsername(client);

            if (string.IsNullOrWhiteSpace(username) || username == "Unknown")
                return new BaseResponseDto { Success = false, Message = "Not authenticated" };

            // Return ALL pending offers for this user
            if (_pendingOffers.TryGetValue(username, out var inner) && inner.Count > 0)
            {
                var allOffers = inner.Values.ToList();
                string offerListJson = JsonHelper.Serialize(allOffers);

                OnLog?.Invoke("Sending " + allOffers.Count + " pending offers to " + username);

                return new BaseResponseDto
                {
                    Success = true,
                    Message = offerListJson
                };
            }

            return new BaseResponseDto { Success = false, Message = "No pending offer" };
        }

        private BaseResponseDto HandlePushAccept(NetworkMessage msg, TcpClient client)
        {
            string username = GetCurrentUsername(client);
            string offerId = msg.JsonBody;

            if (_pendingOffers.TryGetValue(username, out var inner))
            {
                if (inner.TryRemove(offerId, out ServerPushOfferDto offer))
                {
                    // Clean up inner dict if empty
                    if (inner.IsEmpty)
                        _pendingOffers.TryRemove(username, out _);

                    offer.Status = "Accepted";
                    OnLog?.Invoke("Push offer accepted by " + username + ": " + offerId);

                    // Deliver actual file data now
                    if (_activeOffers.TryRemove(offerId, out var entry))
                    {
                        var allFileData = new List<ServerPushFileDto>();
                        foreach (string fp in entry.filePaths)
                        {
                            if (!File.Exists(fp)) continue;
                            allFileData.Add(new ServerPushFileDto
                            {
                                FileName = Path.GetFileName(fp),
                                FileSize = new FileInfo(fp).Length,
                                FileData = File.ReadAllBytes(fp)
                            });
                        }

                        string dataJson = JsonHelper.Serialize(allFileData);
                        OnLog?.Invoke("Delivering " + allFileData.Count + " files to " + username);
                        OnLog?.Invoke("Offer cleaned from active offers: " + offerId);

                        return new BaseResponseDto { Success = true, Message = dataJson };
                    }
                    return new BaseResponseDto { Success = false, Message = "Offer expired" };
                }
                return new BaseResponseDto { Success = false, Message = "No pending offer" };
            }
            return new BaseResponseDto { Success = false, Message = "No pending offer" };
        }

        private BaseResponseDto HandlePushReject(NetworkMessage msg, TcpClient client)
        {
            string username = GetCurrentUsername(client);
            string offerId = msg.JsonBody;

            if (_pendingOffers.TryGetValue(username, out var inner))
            {
                if (inner.TryRemove(offerId, out ServerPushOfferDto offer))
                {
                    if (inner.IsEmpty)
                        _pendingOffers.TryRemove(username, out _);

                    offer.Status = "Rejected";
                    OnLog?.Invoke("Push offer rejected by " + username + ": " + offerId);

                    _activeOffers.TryRemove(offerId, out _);
                    OnLog?.Invoke("Offer cleaned from active offers: " + offerId);

                    return new BaseResponseDto { Success = true, Message = "Offer rejected" };
                }
                return new BaseResponseDto { Success = false, Message = "No pending offer" };
            }
            return new BaseResponseDto { Success = false, Message = "No pending offer" };
        }

        private BaseResponseDto HandleLogout(TcpClient client)
        {
            string username = GetCurrentUsername(client);

            if (username != "Unknown")
            {
                if (_clientUsers.ContainsKey(client))
                {
                    _clientUsers.TryRemove(client, out _);
                }

                _pendingOffers.TryRemove(username, out var removedOffers);
                if (removedOffers != null)
                    OnLog?.Invoke("Cleaned " + removedOffers.Count + " pending offers for: " + username);

                // Remove all active offers for this user (snapshot then remove)
                var activeKeys = _activeOffers.Keys.ToList();
                int cleaned = 0;
                foreach (var k in activeKeys)
                {
                    if (_activeOffers.TryGetValue(k, out var entry) && entry.username == username)
                    {
                        if (_activeOffers.TryRemove(k, out _))
                            cleaned++;
                    }
                }
                if (cleaned > 0)
                    OnLog?.Invoke("Cleaned " + cleaned + " active offers for: " + username);

                OnLog?.Invoke("User logged out: " + username);

                return new BaseResponseDto
                {
                    Success = true,
                    Message = "Logged out successfully"
                };
            }

            return new BaseResponseDto
            {
                Success = true,
                Message = "No active session"
            };
        }

        private string GetCurrentUsername(TcpClient client)
        {
            if (client != null &&
                _clientUsers.ContainsKey(client))
            {
                return _clientUsers[client];
            }

            return "Unknown";
        }

        private string GetStorageFolder()
        {
            string projectFolder =
                Directory.GetParent(
                    AppDomain.CurrentDomain.BaseDirectory)
                    .Parent
                    .Parent
                    .FullName;

            string storageFolder =
                Path.Combine(projectFolder, "Storage");

            if (!Directory.Exists(storageFolder))
            {
                Directory.CreateDirectory(storageFolder);
            }

            return storageFolder;
        }

        private string GetUserStorageFolder(string username)
        {
            string safeUsername =
                Path.GetFileName(username);

            string rootFolder =
                GetStorageFolder();

            string userFolder =
                Path.Combine(rootFolder, safeUsername);

            if (!Directory.Exists(userFolder))
            {
                Directory.CreateDirectory(userFolder);
            }

            return userFolder;
        }

        private string FindFilePathByName(string fileName)
        {
            string rootFolder =
                GetStorageFolder();

            string safeFileName =
                Path.GetFileName(fileName);

            foreach (string filePath in Directory.GetFiles(
                rootFolder,
                safeFileName,
                SearchOption.AllDirectories))
            {
                return filePath;
            }

            return null;
        }
    }
}