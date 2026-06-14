# Kế hoạch: Truyền nhiều chiều (Server → Client)

## Mục tiêu

Server có thể **chủ động gửi file đến Client** (không chỉ phản hồi request).

## Kiến trúc

Hiện tại: Client request → Server response (one-way pull)
Sau: Server push → Client receive + save (bidirectional)

## Các file cần sửa (6 files)

### 1. `FileTransfer.Shared/Enums/MessageType.cs`
**Thêm enum value:**
```csharp
ServerPushFile   // Server chủ động gửi file đến Client
```

### 2. `FileTransfer.Shared/DTOs/ServerPushFileDto.cs` (FILE MỚI)
```csharp
public class ServerPushFileDto
{
    public string FileName { get; set; }
    public long FileSize { get; set; }
    public byte[] FileData { get; set; }
}
```

### 3. `FileTransfer.Server/Networking/TcpServer.cs`
**Thêm method mới:**
```csharp
public async Task PushFileToClientAsync(string username, string filePath)
```
- Tìm TcpClient của username trong `_clientUsers`
- Đọc file từ disk
- Tạo `NetworkMessage` với type `ServerPushFile`
- Gửi qua SslStream

### 4. `FileTransfer.Client/MainWindow.xaml`
**Thêm UI:**
- Button "Browse" để chọn file muốn nhận
- Label hiển thị file đang được push từ Server

### 5. `FileTransfer.Client/MainWindow.xaml.cs`
**Sửa luồng nhận message:**
Hiện tại Client chỉ gửi → nhận response (sync).
**Cần thêm:** Luồng lắng nghe bất đồng bộ từ Server.

### 6. `FileTransfer.Client/Networking/TcpClientService.cs`
**Thêm method mới:**
```csharp
public async Task<ServerPushFileDto> WaitForServerPushAsync()
```
- Đọc message từ SslStream
- Parse type
- Nếu là `ServerPushFile` → deserialize DTO → return
- Nếu không → throw

### 7. `FileTransfer.Server/Form1.cs`
**Thêm UI:**
- ListBox hiển thị danh sách Client đang online
- Button "Push File" để chọn file gửi

## Luồng hoạt động

```
Server Admin                          Client
  │                                     │
  ├─ Chọn file trên Server UI           │
  ├─ Chọn Client từ danh sách online    │
  ├─ Click "Push File"                  │
  │                                     │
  ├─ ServerPushFile ───────────────────▶│
  │   { FileName, FileSize, FileData }  │
  │                                     ├─ Show SaveFileDialog
  │                                     ├─ File.WriteAllBytes()
  │                                     ├─ Show "Nhận file thành công"
  │◀── BaseResponse ────────────────────│
```

## Thứ tự implement

1. Tạo DTO mới (`ServerPushFileDto.cs`)
2. Sửa `MessageType.cs` (thêm enum)
3. Sửa `TcpClientService.cs` (thêm method nhận push)
4. Sửa `TcpServer.cs` (thêm method gửi push)
5. Sửa `Form1.cs` (thêm UI Server)
6. Sửa `MainWindow.xaml` + `.cs` (thêm UI Client)
7. Build + fix lỗi

## Rủi ro

- Client đang ở chế độ "gửi request → chờ response". Nếu Server push giữa lúc Client đang gửi upload chunk → conflict.
- **Giải pháp:** Luồng push dùng message type riêng (`ServerPushFile`). Client cần có cơ chế phân luồng đọc message.

---

**Tôi sẽ KHÔNG sửa code cho đến khi bạn approve plan này.**