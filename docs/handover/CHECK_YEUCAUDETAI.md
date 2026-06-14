# Đánh giá yêu cầu đề tài

| # | Yêu cầu | Trạng thái | Ghi chú |
|---|---|---|---|
| 1 | Ứng dụng truyền file thời gian thực | ✅ ĐẠT | Client-Server qua TCP Socket realtime |
| 2 | Chứng thực client (CA + TLS/SSL) | ✅ ĐẠT | mTLS với CA certificate, SslStream TLS 1.2 |
| 3 | Tạo server + CSDL. Tạo tài khoản client | ✅ ĐẠT | WinForms Server + MySQL (5 tables), Register/Login |
| 4 | Đăng nhập bằng tài khoản (DTO) | ✅ ĐẠT | LoginRequestDto/RegisterRequestDto, BCrypt hash |
| 5 | Truyền file qua LAN/INTERNET | ✅ ĐẠT | TCP Socket, cấu hình IP/PORT thủ công |
| 6 | Progress bar thời gian thực | ✅ ĐẠT | progressUpload/progressDownload trên WPF Client |
| 7 | Hỗ trợ file lớn, resume khi ngắt | ✅ ĐẠT | Chunked upload (64KB), FileTransferStates tracking |
| 8 | Đồng thời gửi nhiều file | ✅ ĐẠT | Upload nhiều file qua vòng lặp trong UploadSingleFileAsync |
| 9 | Truyền nhiều chiều (Client ↔ Server) | ⚠️ MỘT PHẦN | Upload: Client→Server (có). Download: Server→Client (có). Nhưng Server không thể chủ động gửi file |
| 10 | Mã hoá AES/RSA | ⚠️ MỘT PHẦN | AES-256 CBC (có). RSA (chưa có) |
| 11 | Lưu lịch sử Client | ✅ ĐẠT | TransferHistories table lưu Upload/Download/DownloadShared |

## Sai sót cần cải thiện

### 1. Yêu cầu #9 - Truyền nhiều chiều (Server chủ động gửi file)
**Vấn đề:** Hiện tại Server chỉ phản hồi khi Client request. Server không thể chủ động push file đến Client.
**Cần thêm:** Server-initiated transfer message type + xử lý ở Client.

### 2. Yêu cầu #10 - Thiếu RSA
**Vấn đề:** Chỉ có AES-256. Yêu cầu ghi "AES/RSA" nhưng RSA chưa được implement.
**Cần thêm:** 
- RSA key pair cho mỗi user (lưu public key)
- Hoặc dùng RSA để trao đổi AES key (hybrid encryption)

### 3. Yêu cầu #8 - Upload nhiều file nhưng tuần tự
**Vấn đề:** Upload nhiều file chạy trong `foreach` tuần tự, không thực sự "đồng thời" (parallel).
**Cải thiện:** Dùng `Parallel.ForEach` hoặc `Task.WhenAll` để upload song song.

### 4. Yêu cầu #7 - Download không chunked
**Vấn đề:** Upload có chunked (64KB), resume. Download lại `File.ReadAllBytes` (load entire file vào RAM).
**Cải thiện:** Implement chunked download tương tự upload.

### 5. Yêu cầu #5 - Không có discovery/tự động tìm server
**Vấn đề:** Phải nhập thủ công IP/PORT. Không có broadcast discovery.
**Cải thiện:** Thêm UDP broadcast để Client tự động tìm Server trên LAN.

## Tổng quan: **9/11 đạt, 2/11 đạt một phần**