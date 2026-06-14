# Secure File Transfer Client-Server

Ứng dụng truyền file thời gian thực mô hình Client-Server bằng C#, WPF, WinForms, TCP Socket, EF Core và PostgreSQL.

## Công nghệ sử dụng

- C# .NET Framework
- WPF Client
- WinForms Server
- TCP Socket
- JSON DTO Protocol
- Length-prefix TCP protocol
- EF Core
- PostgreSQL
- BCrypt password hashing
- AES file chunk encryption

## Kiến trúc

Client WPF  
→ TCP Socket  
→ Server WinForms  
→ EF Core  
→ PostgreSQL

Client không kết nối trực tiếp database.

## Tính năng chính

- Đăng ký / đăng nhập
- Mã hóa mật khẩu bằng BCrypt
- Upload file theo chunk
- Download file
- Hỗ trợ file lớn
- Resume upload khi mất kết nối
- Upload nhiều file
- Tiến trình realtime
- AES encryption cho file chunk
- Mỗi user có private storage riêng
- Chia sẻ file bằng ShareCode
- Lưu lịch sử truyền file
- Server dashboard UI

## Cấu trúc project

```text
FileTransferSolution
├── FileTransfer.Client
├── FileTransfer.Server
└── FileTransfer.Shared

Cấu hình database

Project dùng PostgreSQL qua EF Core Migration.

Không commit connection string thật lên GitHub.

Sau khi tạo PostgreSQL mới, cập nhật connection string trong Server rồi chạy:

Update-Database -Project FileTransfer.Server -StartupProject FileTransfer.Server
Chạy project
Mở solution bằng Visual Studio 2019.
Build toàn bộ solution.
Chạy FileTransfer.Server.
Start Server port 9000.
Chạy FileTransfer.Client.
Connect tới IP Server.
Register/Login.
Upload/Download file.
Demo LAN

Trên máy Client nhập IP LAN của máy Server, ví dụ:

192.168.1.10
9000

Không dùng 127.0.0.1 nếu chạy trên máy khác.

Lưu ý bảo mật

Không upload:

connection string thật
password database
file .env
thư mục bin/
thư mục obj/
thư mục Storage/

