# Secure File Transfer System

> Hệ thống truyền tải tệp tin an toàn sử dụng TCP Socket, Mutual TLS (mTLS), AES Encryption và cơ chế Resume Upload.

---

## Giới thiệu

Secure File Transfer System là đồ án xây dựng hệ thống truyền tải tệp tin giữa Client và Server theo mô hình Client-Server.

Hệ thống tập trung vào các yếu tố:

- Bảo mật truyền thông bằng Mutual TLS (mTLS)
- Mã hóa dữ liệu bằng AES
- Xác thực người dùng bằng BCrypt Password Hashing
- Upload và Download tệp tin dung lượng lớn
- Resume Upload khi mất kết nối
- Push File từ Server đến Client
- Multi Offer và Multi User Push
- Quản lý dữ liệu bằng MySQL

---

# Mục tiêu dự án

Xây dựng một hệ thống truyền tệp tin có khả năng:

- Truyền file an toàn qua TCP Socket
- Xác thực hai chiều giữa Client và Server
- Bảo vệ thông tin người dùng
- Hỗ trợ truyền file lớn
- Khôi phục upload khi kết nối bị gián đoạn
- Cho phép Server chủ động gửi file tới Client
- Hỗ trợ nhiều người dùng đồng thời

---

# Công nghệ sử dụng

## Backend

- C#
- .NET Framework 4.7.2
- TCP Socket Programming
- SSL/TLS
- Mutual TLS (mTLS)

## Database

- MySQL 8
- Entity Framework 6

## Security

- BCrypt
- AES-256 Encryption
- X509 Certificates
- Certificate Authority (CA)
- Mutual TLS

## Libraries

- Newtonsoft.Json
- MySql.Data
- Bcrypt.Net

---

# Kiến trúc hệ thống

```text
┌──────────────────────┐
│       Client         │
└──────────┬───────────┘
           │
           │  mTLS
           │
┌──────────▼───────────┐
│       Server         │
└──────────┬───────────┘
           │
           │
┌──────────▼───────────┐
│       MySQL          │
└──────────────────────┘
```

---

# Các tính năng chính

## Authentication

- Đăng ký tài khoản
- Đăng nhập
- BCrypt Password Hashing
- Session Management

---

## Upload File

- Upload file dung lượng lớn
- Chunk-based Upload
- Resume Upload
- Progress Tracking

---

## Download File

- Download file từ Server
- Kiểm tra trạng thái truyền tải

---

## Push Request

Server có thể chủ động gửi file đến Client.

Người dùng nhận được:

- Danh sách file
- Thông tin người gửi
- Kích thước file
- Thời gian gửi

Người dùng có thể:

- Accept
- Reject
- Xem chi tiết

---

## Multi Offer

Một người dùng có thể nhận nhiều Push Offer cùng lúc.

Ví dụ:

```text
Offer #1
Offer #2
Offer #3
```

Tất cả được quản lý độc lập.

---

## Multi User Push

Admin có thể chọn nhiều người dùng online và gửi cùng một bộ file.

Ví dụ:

```text
User A
User B
User C
```

Nhận cùng một Push Offer.

---

# Security Architecture

## Password Security

Hệ thống sử dụng:

```text
BCrypt
```

để lưu trữ mật khẩu dưới dạng Hash.

Không lưu mật khẩu dạng Plain Text.

---

## AES Encryption

File được mã hóa bằng:

```text
AES-256
```

trước khi truyền.

---

## Mutual TLS (mTLS)

Hệ thống sử dụng xác thực hai chiều:

```text
Client ↔ Server
```

Cả hai bên đều phải trình bày Certificate hợp lệ.

---

## Certificate Validation

Kiểm tra:

- Expiration
- Certificate Chain
- Certificate Authority
- Common Name (CN)

---

# Database

Một số bảng chính:

- Users
- UploadedFiles
- SharedFiles
- TransferHistories

---

# Thread Safety

Hệ thống sử dụng:

```text
ConcurrentDictionary
SemaphoreSlim
```

để xử lý:

- Multi Client
- Concurrent Upload
- Push Offer Management

---

# Cấu trúc Solution

```text
FileTransferSolution
│
├── FileTransfer.Client
│
├── FileTransfer.Server
│
├── FileTransfer.Shared
│
└── docs
```

---

# Hướng dẫn chạy dự án

## 1. Clone source

```bash
git clone <repository-url>
```

---

## 2. Cấu hình MySQL

Tạo database:

```sql
CREATE DATABASE transferfile_mysql;
```

---

## 3. Cấu hình Certificate

Tham khảo:

```text
docs/CA_SETUP_GUIDE.md
```

---

## 4. Cấu hình App.config

Server:

```xml
DbServer
DbName
DbUser
DbPassword

ServerCertificatePath
ServerCertPassword
```

Client:

```xml
ClientCertificatePath
ClientCertPassword
```

---

## 5. Build

Visual Studio 2019

```text
Build Solution
```

---

## 6. Chạy Server

```text
FileTransfer.Server
```

---

## 7. Chạy Client

```text
FileTransfer.Client
```

---

# Tài liệu học tập

Thư mục:

```text
DOCHIEU/
```

được xây dựng dành cho:

- Sinh viên
- Người mới học
- Người tiếp nhận dự án

Nội dung bao gồm:

- Kiến trúc hệ thống
- Networking
- Security
- Database
- mTLS
- AES
- Thread Safety
- Push Request
- Multi Offer
- Hướng dẫn bảo vệ đồ án

---

# Điểm nổi bật của đồ án

- TCP Socket Programming
- Mutual TLS (mTLS)
- AES Encryption
- BCrypt Authentication
- Resume Upload
- Push Request
- Multi User Push
- Multi Offer
- ConcurrentDictionary
- SemaphoreSlim
- MySQL + Entity Framework

---

# Hướng phát triển

Trong tương lai có thể mở rộng:

- JWT Authentication
- File Versioning
- Audit Log
- Notification Service
- Cloud Storage Integration
- Role-Based Access Control (RBAC)
- Key Vault / Secret Manager

---

# Tác giả

Phạm Tấn Hưng 

Đồ án được xây dựng với mục tiêu nghiên cứu:

- Secure File Transfer
- Network Programming
- Application Security
- Mutual TLS
- Secure Authentication
