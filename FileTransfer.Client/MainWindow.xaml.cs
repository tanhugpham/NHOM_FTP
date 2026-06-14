using FileTransfer.Client.Networking;

using FileTransfer.Shared.DTOs;
using FileTransfer.Shared.Enums;
using FileTransfer.Shared.Helpers;
using FileTransfer.Shared.Protocols;
using FileTransfer.Shared.Responses;

using Microsoft.Win32;

using System;
using System.IO;
using System.Security.Cryptography;
using System.Text;
using System.Windows;
using FileTransfer.Shared.Security;

namespace FileTransfer.Client
{
    public partial class MainWindow : Window
    {
        private TcpClientService _clientService;

        private string _selectedFilePath;

        private string[] _selectedFiles;

        private bool _isDisconnecting = false;

        private string _currentUsername = "";
        public MainWindow()
        {
            InitializeComponent();

            _clientService = new TcpClientService();

            btnRegister.IsEnabled = false;
            btnLogin.IsEnabled = false;
            btnDisconnect.IsEnabled = false;
            btnUpload.IsEnabled = false;
            btnRefreshFiles.IsEnabled = false;
            btnDownload.IsEnabled = false;

            txtStatus.Text = "Status: Disconnected";
            LoginPanel.Visibility = Visibility.Visible;
            MainPanel.Visibility = Visibility.Collapsed;

            LoginPanel.Visibility = Visibility.Visible;
            MainPanel.Visibility = Visibility.Collapsed;
            txtCurrentUser.Text = "User: -";
        }

        private async void btnConnect_Click(object sender, RoutedEventArgs e)
        {

            string ip = txtServerIp.Text.Trim();

            int port;
            if (!int.TryParse(txtPort.Text.Trim(), out port))
            {
                MessageBox.Show("Port không hợp lệ");
                return;
            }

            try
            {
                btnConnect.IsEnabled = false;
                txtStatus.Text = "Status: Connecting...";

                AddLog("Connecting to " + ip + ":" + port);

                await _clientService.ConnectAsync(ip, port);
                _isDisconnecting = false;

                AddLog("Connected successfully");

                txtStatus.Text = "Status: Connected";

                btnRegister.IsEnabled = true;
                btnLogin.IsEnabled = true;
                btnDisconnect.IsEnabled = true;
            }
            catch (Exception ex)
            {
                AddLog("Connect error: " + ex.Message);
                txtStatus.Text = "Status: Error";
                btnConnect.IsEnabled = true;
            }
        }

        private async void btnRegister_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username và Password không được trống");
                return;
            }

            RegisterRequestDto registerDto =
                new RegisterRequestDto
                {
                    Username = username,
                    Password = password
                };

            BaseResponseDto response =
                await SendRequestAsync(MessageType.Register, registerDto);

            MessageBox.Show(response.Message);

            if (response.Success)
            {
                txtStatus.Text = "Status: " + response.Message;
            }
        }

        private async void btnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = txtUsername.Text.Trim();
            string password = txtPassword.Password.Trim();

            if (string.IsNullOrWhiteSpace(username) ||
                string.IsNullOrWhiteSpace(password))
            {
                MessageBox.Show("Username và Password không được trống");
                return;
            }

            LoginRequestDto loginDto =
                new LoginRequestDto
                {
                    Username = username,
                    Password = password
                };

            BaseResponseDto response =
                await SendRequestAsync(MessageType.Login, loginDto);

            MessageBox.Show(response.Message);

            if (response.Success)
            {
                _currentUsername = txtUsername.Text.Trim();

                txtStatus.Text = "Status: " + response.Message;
                txtCurrentUser.Text = "User: " + _currentUsername;

                LoginPanel.Visibility = Visibility.Collapsed;
                MainPanel.Visibility = Visibility.Visible;

                btnUpload.IsEnabled =
                    _selectedFiles != null &&
                    _selectedFiles.Length > 0;

                btnRefreshFiles.IsEnabled = true;
                btnDownload.IsEnabled = true;
            }
        }

        private void btnBrowse_Click(object sender, RoutedEventArgs e)
        {
            OpenFileDialog dialog =
                new OpenFileDialog();

            dialog.Multiselect = true;

            bool? result =
                dialog.ShowDialog();

            if (result == true)
            {
                _selectedFiles = dialog.FileNames;

                if (_selectedFiles.Length > 0)
                {
                    _selectedFilePath = _selectedFiles[0];

                    txtSelectedFile.Text =
                        string.Join(" ; ", _selectedFiles);

                    AddLog(
                        "Selected "
                        + _selectedFiles.Length
                        + " files");

                    btnUpload.IsEnabled = true;
                }
            }
        }

        private async void btnUpload_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedFiles == null || _selectedFiles.Length == 0)
            {
                MessageBox.Show("Bạn chưa chọn file");
                return;
            }

            try
            {
                btnUpload.IsEnabled = false;

                foreach (string filePath in _selectedFiles)
                {
                    if (!File.Exists(filePath))
                    {
                        AddLog("File không tồn tại: " + filePath);
                        continue;
                    }

                    _selectedFilePath = filePath;

                    AddLog("Uploading file: " + Path.GetFileName(filePath));

                    bool success = await UploadSingleFileAsync(filePath);

                    if (!success)
                    {
                        AddLog("Upload failed: " + Path.GetFileName(filePath));
                        break;
                    }
                }

                await RefreshFileListAsync();

                MessageBox.Show("Upload nhiều file hoàn tất");
            }
            catch (Exception ex)
            {
                AddLog("Upload error: " + ex.Message);
                MessageBox.Show("Upload error: " + ex.Message);
            }
            finally
            {
                btnUpload.IsEnabled = true;
            }
        }

        private async System.Threading.Tasks.Task<bool> UploadSingleFileAsync(string filePath)
        {
            try
            {
                progressUpload.Value = 0;
                txtProgress.Text = "0%";

                string fileName = Path.GetFileName(filePath);
                FileInfo fileInfo = new FileInfo(filePath);

                string fileId = CreateStableFileId(filePath);

                int chunkSize = 64 * 1024;

                ResumeCheckResponseDto resumeResponse =
                    await SendResumeCheckAsync(fileId);

                int startChunkIndex = 0;
                long startBytePosition = 0;

                if (resumeResponse.Success && resumeResponse.IsCompleted)
                {
                    AddLog("File đã upload trước đó: " + fileName);
                    progressUpload.Value = 100;
                    txtProgress.Text = "100%";
                    return true;
                }

                if (resumeResponse.Success &&
                    resumeResponse.LastChunkIndex >= 0 &&
                    resumeResponse.BytesReceived > 0)
                {
                    startChunkIndex = resumeResponse.LastChunkIndex + 1;
                    startBytePosition = resumeResponse.BytesReceived;

                    int resumePercent =
                        (int)((startBytePosition * 100) / fileInfo.Length);

                    progressUpload.Value = resumePercent;
                    txtProgress.Text = resumePercent + "%";

                    AddLog(
                        "Resume upload "
                        + fileName
                        + " từ chunk "
                        + startChunkIndex);
                }
                else
                {
                    FileStartRequestDto startDto =
                        new FileStartRequestDto
                        {
                            FileId = fileId,
                            FileName = fileName,
                            TotalBytes = fileInfo.Length
                        };

                    BaseResponseDto startResponse =
                        await SendRequestAsync(MessageType.FileStart, startDto);

                    if (!startResponse.Success)
                    {
                        MessageBox.Show(startResponse.Message);
                        return false;
                    }

                    AddLog("Start new upload: " + fileName);
                }

                byte[] buffer = new byte[chunkSize];

                long totalSent = startBytePosition;
                int chunkIndex = startChunkIndex;

                using (FileStream fs =
                    new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    fs.Seek(startBytePosition, SeekOrigin.Begin);

                    int bytesRead;

                    while ((bytesRead = fs.Read(buffer, 0, buffer.Length)) > 0)
                    {
                        byte[] chunkData = new byte[bytesRead];

                        Array.Copy(buffer, chunkData, bytesRead);

                        totalSent += bytesRead;

                        bool isLastChunk = totalSent >= fileInfo.Length;

                        FileChunkDto chunkDto =
                            new FileChunkDto
                            {
                                FileId = fileId,
                                ChunkData = AesEncryptionHelper.Encrypt(chunkData),
                                ChunkIndex = chunkIndex,
                                IsLastChunk = isLastChunk
                            };

                        BaseResponseDto chunkResponse =
                            await SendRequestAsync(MessageType.FileChunk, chunkDto);

                        if (!chunkResponse.Success)
                        {
                            MessageBox.Show(
                                "Upload lỗi tại chunk "
                                + chunkIndex
                                + ": "
                                + chunkResponse.Message);

                            return false;
                        }

                        int percent =
                            (int)((totalSent * 100) / fileInfo.Length);

                        progressUpload.Value = percent;
                        txtProgress.Text = percent + "%";

                        AddLog(
                            "Uploaded "
                            + fileName
                            + " chunk "
                            + chunkIndex
                            + " - "
                            + percent
                            + "%");

                        chunkIndex++;
                    }
                }

                FileCompleteDto completeDto =
                    new FileCompleteDto
                    {
                        FileId = fileId,
                        FileName = fileName
                    };

                BaseResponseDto completeResponse =
                    await SendRequestAsync(MessageType.FileComplete, completeDto);

                if (completeResponse.Success)
                {
                    progressUpload.Value = 100;
                    txtProgress.Text = "100%";
                    txtStatus.Text = "Status: Upload completed";

                    AddLog("Upload completed: " + fileName);

                    return true;
                }

                MessageBox.Show(completeResponse.Message);
                return false;
            }
            catch (Exception ex)
            {
                AddLog("Upload file error: " + ex.Message);
                MessageBox.Show("Upload file error: " + ex.Message);
                return false;
            }
        }
        private async void btnRefreshFiles_Click(object sender, RoutedEventArgs e)
        {
            await RefreshFileListAsync();
        }

        private async System.Threading.Tasks.Task RefreshFileListAsync()
        {
            try
            {
                BaseResponseDto response =
                    await SendRequestAsync(
                        MessageType.GetFileList,
                        new { });

                if (!response.Success)
                {
                    MessageBox.Show(response.Message);
                    return;
                }

                FileListResponseDto fileListResponse =
                    response as FileListResponseDto;

                if (fileListResponse == null)
                {
                    fileListResponse =
                        JsonHelper.Deserialize<FileListResponseDto>(
                            JsonHelper.Serialize(response));
                }

                dgServerFiles.ItemsSource =
                    fileListResponse.Files;

                AddLog(
                    "Loaded server files: "
                    + fileListResponse.Files.Count);
            }
            catch (Exception ex)
            {
                AddLog("Refresh files error: " + ex.Message);
                MessageBox.Show("Refresh files error: " + ex.Message);
            }
        }

        private async void btnDownload_Click(object sender, RoutedEventArgs e)
        {
            FileInfoDto selectedFile =
                dgServerFiles.SelectedItem as FileInfoDto;

            if (selectedFile == null)
            {
                MessageBox.Show("Bạn chưa chọn file trên server");
                return;
            }

            SaveFileDialog saveDialog = new SaveFileDialog();
            saveDialog.FileName = selectedFile.FileName;

            bool? result = saveDialog.ShowDialog();

            if (result != true)
            {
                return;
            }

            try
            {
                progressDownload.Value = 0;
                txtDownloadProgress.Text = "0%";

                DownloadFileRequestDto request =
                    new DownloadFileRequestDto
                    {
                        FileName = selectedFile.FileName
                    };

                DownloadFileResponseDto response =
                    await SendDownloadRequestAsync(request);

                if (!response.Success)
                {
                    MessageBox.Show(response.Message);
                    return;
                }

                File.WriteAllBytes(
                    saveDialog.FileName,
                    response.FileData);

                progressDownload.Value = 100;
                txtDownloadProgress.Text = "100%";

                AddLog("Downloaded file: " + saveDialog.FileName);

                MessageBox.Show("Download thành công");
            }
            catch (Exception ex)
            {
                AddLog("Download error: " + ex.Message);
                MessageBox.Show("Download error: " + ex.Message);
            }
        }

        private async System.Threading.Tasks.Task<ResumeCheckResponseDto>
            SendResumeCheckAsync(string fileId)
        {
            ResumeCheckRequestDto request =
                new ResumeCheckRequestDto
                {
                    FileId = fileId
                };

            string dtoJson =
                JsonHelper.Serialize(request);

            NetworkMessage networkMessage =
                new NetworkMessage
                {
                    Type = MessageType.ResumeCheck,
                    JsonBody = dtoJson
                };

            string finalJson =
                JsonHelper.Serialize(networkMessage);

            AddLog("Sending: ResumeCheck");

            string responseJson =
                await _clientService.SendMessageAsync(finalJson);

            AddLog("Resume response received");

            return JsonHelper.Deserialize<ResumeCheckResponseDto>(
                responseJson);
        }

        private async System.Threading.Tasks.Task<DownloadFileResponseDto>
            SendDownloadRequestAsync(DownloadFileRequestDto request)
        {
            string dtoJson =
                JsonHelper.Serialize(request);

            NetworkMessage networkMessage =
                new NetworkMessage
                {
                    Type = MessageType.DownloadFile,
                    JsonBody = dtoJson
                };

            string finalJson =
                JsonHelper.Serialize(networkMessage);

            AddLog("Sending: DownloadFile");

            string responseJson =
                await _clientService.SendMessageAsync(finalJson);

            AddLog("Download response received");

            return JsonHelper.Deserialize<DownloadFileResponseDto>(
                responseJson);
        }

        private async System.Threading.Tasks.Task<CreateShareCodeResponseDto>
    SendCreateShareCodeAsync(CreateShareCodeRequestDto request)
        {
            string dtoJson =
                JsonHelper.Serialize(request);

            NetworkMessage networkMessage =
                new NetworkMessage
                {
                    Type = MessageType.CreateShareCode,
                    JsonBody = dtoJson
                };

            string finalJson =
                JsonHelper.Serialize(networkMessage);

            AddLog("Sending: CreateShareCode");

            string responseJson =
                await _clientService.SendMessageAsync(finalJson);

            AddLog("CreateShareCode response received");

            return JsonHelper.Deserialize<CreateShareCodeResponseDto>(
                responseJson);
        }

        private async System.Threading.Tasks.Task<DownloadFileResponseDto>
    SendDownloadSharedFileAsync(DownloadSharedFileRequestDto request)
        {
            string dtoJson =
                JsonHelper.Serialize(request);

            NetworkMessage networkMessage =
                new NetworkMessage
                {
                    Type = MessageType.DownloadSharedFile,
                    JsonBody = dtoJson
                };

            string finalJson =
                JsonHelper.Serialize(networkMessage);

            AddLog("Sending: DownloadSharedFile");

            string responseJson =
                await _clientService.SendMessageAsync(finalJson);

            AddLog("DownloadSharedFile response received");

            return JsonHelper.Deserialize<DownloadFileResponseDto>(
                responseJson);
        }

        private async System.Threading.Tasks.Task<BaseResponseDto>
            SendRequestAsync(
            MessageType messageType,
            object dto)
        {
            string dtoJson =
                JsonHelper.Serialize(dto);

            NetworkMessage networkMessage =
                new NetworkMessage
                {
                    Type = messageType,
                    JsonBody = dtoJson
                };

            string finalJson =
                JsonHelper.Serialize(networkMessage);

            AddLog("Sending: " + messageType);

            string responseJson =
                await _clientService.SendMessageAsync(finalJson);

            AddLog("Response: " + responseJson);

            if (messageType == MessageType.GetFileList)
            {
                return JsonHelper.Deserialize<FileListResponseDto>(
                    responseJson);
            }
           
            return JsonHelper.Deserialize<BaseResponseDto>(
                responseJson);
        }

        private string CreateStableFileId(string filePath)
        {
            FileInfo info = new FileInfo(filePath);

            string raw =
                info.Name
                + "|"
                + info.Length
                + "|"
                + info.LastWriteTimeUtc.Ticks;

            using (MD5 md5 = MD5.Create())
            {
                byte[] inputBytes =
                    Encoding.UTF8.GetBytes(raw);

                byte[] hashBytes =
                    md5.ComputeHash(inputBytes);

                StringBuilder sb =
                    new StringBuilder();

                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes[i].ToString("x2"));
                }

                return sb.ToString();
            }
        }

        private void btnDisconnect_Click(object sender, RoutedEventArgs e)
        {
            _isDisconnecting = true;
            _clientService.Disconnect();

            _currentUsername = "";
            txtCurrentUser.Text = "User: -";

            LoginPanel.Visibility = Visibility.Visible;
            MainPanel.Visibility = Visibility.Collapsed;

            AddLog("Disconnected");

            txtStatus.Text = "Status: Disconnected";

            btnConnect.IsEnabled = true;
            btnRegister.IsEnabled = false;
            btnLogin.IsEnabled = false;
            btnDisconnect.IsEnabled = false;
            btnUpload.IsEnabled = false;
            btnRefreshFiles.IsEnabled = false;
            btnDownload.IsEnabled = false;
            
        }

        private void AddLog(string message)
        {
            string log =
                DateTime.Now.ToString("HH:mm:ss")
                + " - "
                + message;

            lstLogs.Items.Add(log);
            lstLogs.ScrollIntoView(log);
        }

        private async void btnCreateShareCode_Click(object sender, RoutedEventArgs e)
        {
            FileInfoDto selectedFile =
                dgServerFiles.SelectedItem as FileInfoDto;

            if (selectedFile == null)
            {
                MessageBox.Show("Bạn chưa chọn file để chia sẻ");
                return;
            }

            string allowedUsername =
                txtAllowedUsername.Text.Trim();

            if (string.IsNullOrWhiteSpace(allowedUsername))
            {
                MessageBox.Show("Bạn phải nhập username người nhận");
                return;
            }

            try
            {
                CreateShareCodeRequestDto request =
                    new CreateShareCodeRequestDto
                    {
                        FileName = selectedFile.FileName,
                        AllowedUsername = allowedUsername
                    };

                CreateShareCodeResponseDto response =
                    await SendCreateShareCodeAsync(request);

                if (!response.Success)
                {
                    MessageBox.Show(response.Message);
                    return;
                }

                txtShareCodeResult.Text = response.ShareCode;

                AddLog(
                    "Created share code for "
                    + selectedFile.FileName
                    + ": "
                    + response.ShareCode);

                MessageBox.Show(
                    "Mã chia sẻ: "
                    + response.ShareCode);
            }
            catch (Exception ex)
            {
                AddLog("Create share code error: " + ex.Message);
                MessageBox.Show("Create share code error: " + ex.Message);
            }
        }
        private async void btnDownloadSharedFile_Click(object sender, RoutedEventArgs e)
        {
            string shareCode =
                txtInputShareCode.Text.Trim();

            if (string.IsNullOrWhiteSpace(shareCode))
            {
                MessageBox.Show("Bạn chưa nhập mã chia sẻ");
                return;
            }

            try
            {
                DownloadSharedFileRequestDto request =
                    new DownloadSharedFileRequestDto
                    {
                        ShareCode = shareCode
                    };

                DownloadFileResponseDto response =
                    await SendDownloadSharedFileAsync(request);

                if (!response.Success)
                {
                    MessageBox.Show(response.Message);
                    return;
                }

                SaveFileDialog saveDialog =
                    new SaveFileDialog();

                saveDialog.FileName =
                    response.FileName;

                bool? result =
                    saveDialog.ShowDialog();

                if (result != true)
                {
                    return;
                }

                File.WriteAllBytes(
                    saveDialog.FileName,
                    response.FileData);

                progressDownload.Value = 100;
                txtDownloadProgress.Text = "100%";

                AddLog(
                    "Downloaded shared file: "
                    + saveDialog.FileName);

                MessageBox.Show("Download file chia sẻ thành công");
            }
            catch (Exception ex)
            {
                AddLog("Download shared file error: " + ex.Message);
                MessageBox.Show("Download shared file error: " + ex.Message);
            }
        }
    }
}