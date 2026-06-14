# Security Audit: TLS/SSL & mTLS Analysis

## 1. Kiểm tra toàn bộ source code về TLS/SSL

### Tìm `SslStream`
**Kết quả: KHÔNG có**
- FileTransfer.Client/Networking/TcpClientService.cs: dùng `NetworkStream` thuần
- FileTransfer.Server/Networking/TcpServer.cs: dùng `NetworkStream` thuần
- FileTransfer.Shared/Helpers/TcpMessageHelper.cs: nhận `NetworkStream` parameter

### Tìm `X509Certificate` / `X509Certificate2`
**Kết quả: KHÔNG có**
- Không xuất hiện trong bất kỳ file .cs nào
- Không có `using System.Security.Cryptography.X509Certificates;`

### Tìm `AuthenticateAsServer` / `AuthenticateAsClient`
**Kết quả: KHÔNG có**
- Không có trong TcpServer.cs
- Không có trong TcpClientService.cs

### Tìm `RemoteCertificateValidationCallback`
**Kết quả: KHÔNG có**

### Tìm các file .pfx, .cer, .crt, .pem
**Kết quả: KHÔNG có**
- Tìm trong toàn bộ directory tree (trừ packages/)
- Không có file certificate nào

### Tìm NuGet packages liên quan đến TLS/SSL
**Kết quả: KHÔNG có**
- packages.config chỉ có: BCrypt, EF Core, MySQL, Newtonsoft.Json, System.*
- Không có package nào liên quan đến SSL/TLS (như `System.Security.Cryptography.X509Certificates` không cần NuGet vì built-in)

---

## 2. Server Authentication

| Câu hỏi | Trả lời |
|---|---|
| Server có certificate riêng không? | **KHÔNG** |
| Certificate được load ở đâu? | Không có code load certificate |
| Có dùng CA ký certificate không? | **KHÔNG** |
| Certificate self-signed hay CA-signed? | Không có certificate nào |

---

## 3. Client Authentication

| Câu hỏi | Trả lời |
|---|---|
| Client có certificate riêng không? | **KHÔNG** |
| Server có yêu cầu client certificate không? | **KHÔNG** |
| Có kiểm tra certificate chain không? | **KHÔNG** |
| Có verify issuer không? | **KHÔNG** |
| Có verify thumbprint không? | **KHÔNG** |

---

## 4. TCP Socket Analysis

### Socket hiện tại đang truyền plaintext hay encrypted?

**KẾT LUẬN: PLAINTEXT (100% không mã hóa)**

Dòng code chứng minh:

**Server** (FileTransfer.Server/Networking/TcpServer.cs, dòng 111-112):
```csharp
NetworkStream stream = client.GetStream();
// Không có SslStream wrapping
// NetworkStream raw - không mã hóa
```

**Client** (FileTransfer.Client/Networking/TcpClientService.cs, dòng 27):
```csharp
_stream = _client.GetStream();
// Không có SslStream wrapping
// NetworkStream raw - không mã hóa
```

**Truyền dữ liệu** (FileTransfer.Shared/Helpers/TcpMessageHelper.cs):
```csharp
// Ghi: stream.WriteAsync(messageBytes) - plaintext
// Đọc: stream.ReadAsync(buffer) - plaintext
```

### Dữ liệu upload/download có được TLS bảo vệ không?

**KHÔNG.** Chỉ có AES encryption ở tầng application layer (cho file chunks), nhưng:
1. **TCP transport layer**: plaintext (không TLS)
2. **JSON messages**: plaintext (username, password gửi dạng JSON clear text)
3. **File chunks**: được AES encrypt bởi AesEncryptionHelper trước khi gửi
4. **Session management**: username mapping in-memory không được bảo vệ

---

## 5. Authentication hiện tại

### Đăng nhập đang dùng Username/Password đơn thuần?

**CÓ**, và có các vấn đề sau:

**File gốc: FileTransfer.Shared/DTOs/LoginRequestDto.cs**
```csharp
public class LoginRequestDto
{
    public string Username { get; set; }  // Gửi plaintext qua TCP
    public string Password { get; set; }  // Gửi plaintext qua TCP
}
```

**Luồng Login hiện tại:**
```
Client                                    Server
  │                                         │
  ├── JSON: { "Username": "admin",         │
  │     "Password": "mypassword" }         │
  │────Raw TCP (no TLS)──────────────────▶│
  │                                         │
  │◀──JSON: { "Success": true,             │
  │     "Message": "Đăng nhập thành công" }│
```

**Vấn đề:**
1. Username/Password gửi plaintext qua TCP - có thể bị sniff
2. Password được hash bằng BCrypt **ở server side**, nhưng log password được gửi dưới dạng raw text
3. Không có session token (JWT) - chỉ có in-memory Dictionary
4. Không có rate limiting - brute force attack dễ dàng
5. Không có 2FA

### Có Mutual TLS (mTLS) không?

**KHÔNG.** Không có bất kỳ hình thức certificate authentication nào.

---

## 6. Đánh giá so với yêu cầu: "Client Authentication using CA + TLS/SSL"

| Yêu cầu | Trạng thái | Chi tiết |
|---|---|---|
| TLS/SSL cho TCP | ❌ Chưa đạt | NetworkStream plaintext, không SslStream |
| Server Certificate | ❌ Chưa đạt | Không có .pfx, .cer, .crt |
| Client Certificate | ❌ Chưa đạt | Không có client certificate |
| CA-signed Certificate | ❌ Chưa đạt | Không có CA |
| Certificate Validation | ❌ Chưa đạt | Không có callback validation |
| mTLS (Mutual TLS) | ❌ Chưa đạt | Cả 2 phía đều không có certificate |

**Đánh giá tổng thể: ❌ CHƯA ĐẠT** (0/6 yêu cầu)

---

## 7. Đề xuất giải pháp mTLS

### Thành phần cần bổ sung

| STT | Thành phần | Mô tả | File cần sửa/tạo |
|---|---|---|---|
| 1 | **CA Certificate** | Certificate Authority để ký server & client cert | CA private key (bảo mật riêng) |
| 2 | **Server Certificate (.pfx)** | Server identity certificate + private key | `FileTransfer.Server/Certificates/server.pfx` |
| 3 | **Client Certificate (.pfx/.cer)** | Client identity certificate | `FileTransfer.Client/Certificates/client.pfx` |
| 4 | **CA Root Certificate (.cer)** | Dùng để verify server & client certs | `FileTransfer.Shared/Certificates/ca.cer` |
| 5 | **Certificate Validation** | Callback xác thực certificate chain | FileTransfer.Shared/Security/ |

### Files cần sửa (chi tiết)

#### File 1: FileTransfer.Server/Networking/TcpServer.cs
**Mức độ thay đổi: MEDIUM**

```csharp
// THÊM: using System.Security.Cryptography.X509Certificates;
// THÊM: using System.Net.Security;

// SỬA: Wrap NetworkStream bằng SslStream
X509Certificate2 serverCert = new X509Certificate2("Certificates/server.pfx", "password");
SslStream sslStream = new SslStream(
    client.GetStream(),
    false,
    ValidateClientCertificate  // RemoteCertificateValidationCallback
);
sslStream.AuthenticateAsServer(
    serverCert,
    clientCertificateRequired: true,  // Yêu cầu client cert (mTLS)
    enabledSslProtocols: SslProtocols.Tls12,
    checkCertificateRevocation: true
);
```

**Thay đổi trong class:**
- `HandleClientAsync()`: sửa `NetworkStream stream = client.GetStream()` → `SslStream`
- Thêm method `ValidateClientCertificate()`: verify CA chain
- Thêm field `_serverCertificate`: load một lần khi Start

#### File 2: FileTransfer.Client/Networking/TcpClientService.cs
**Mức độ thay đổi: MEDIUM**

```csharp
// THÊM: using System.Security.Cryptography.X509Certificates;
// THÊM: using System.Net.Security;

// SỬA: Wrap NetworkStream bằng SslStream với client certificate
X509Certificate2 clientCert = new X509Certificate2("Certificates/client.pfx", "password");

SslStream sslStream = new SslStream(
    _client.GetStream(),
    false,
    ValidateServerCertificate  // RemoteCertificateValidationCallback
);
sslStream.AuthenticateAsClient(
    targetHost: ip,
    clientCertificates: new X509CertificateCollection { clientCert },
    enabledSslProtocols: SslProtocols.Tls12,
    checkCertificateRevocation: true
);
```

#### File 3: FileTransfer.Shared/Helpers/TcpMessageHelper.cs
**Mức độ thay đổi: LOW**

Không cần sửa method signature nếu đổi `NetworkStream` → `SslStream` (SslStream kế thừa Stream).
Nhưng nên đổi parameter type từ `NetworkStream` → `Stream` để linh hoạt.
```csharp
// Tham số: NetworkStream → Stream
public static async Task SendStringAsync(Stream stream, string message)
public static async Task<string> ReadStringAsync(Stream stream)
```

#### File 4: FileTransfer.Shared/Security/
**Mức độ thay đổi: LOW**

Thêm class mới:
- `CertificateHelper.cs`: static methods để load cert từ file/resources
- `CertificateValidation.cs`: validation callbacks

#### File 5: FileTransfer.Client/MainWindow.xaml.cs và FileTransfer.Server/Form1.cs
**Mức độ thay đổi: LOW**

Có thể cần thêm:
- TextBox để nhập certificate password
- Label để hiển thị certificate status
- Error handling cho cert loading

#### File 6: FileTransfer.Server/App.config / FileTransfer.Client/App.config
**Mức độ thay đổi: LOW**

```xml
<appSettings>
    <add key="ServerCertificatePath" value="Certificates/server.pfx"/>
    <add key="ServerCertificatePassword" value="encrypted_password"/>
    <add key="CACertificatePath" value="Certificates/ca.cer"/>
</appSettings>
```

### Kiến trúc triển khai mTLS

```
┌─────────────────────┐          ┌──────────────────────┐
│   Certificate        │          │   Certificate         │
│   Authority (CA)     │          │   Authority (CA)      │
│   (Internal/3rd)     │          │   (Internal/3rd)      │
└──────────┬──────────┘          └───────────┬──────────┘
           │                                  │
    Signs Server Cert                  Signs Client Certs
           │                                  │
           ▼                                  ▼
┌─────────────────────┐          ┌──────────────────────┐
│  FileTransfer.Server│          │  FileTransfer.Client  │
├─────────────────────┤          ├──────────────────────┤
│ server.pfx          │          │ client.pfx           │
│ (cert + private key)│          │ (cert + private key) │
│                     │          │ ca.cer               │
│ ca.cer (CA root)    │          │ (CA root để verify)  │
├─────────────────────┤          ├──────────────────────┤
│ TcpServer           │          │ TcpClientService      │
│  ├─ Load server.pfx │          │  ├─ Load client.pfx  │
│  ├─ AuthenticateAs  │◄───TLS───┤  ├─ AuthenticateAs  │
│  │  Server(cert)    │ 1.2 mTLS │  │  Client(cert)     │
│  ├─ ValidateClient  │          │  ├─ ValidateServer   │
│  │  Cert(CA check)  │          │  │  Cert(CA check)   │
│  └─ SslStream       │          │  └─ SslStream        │
└─────────────────────┘          └──────────────────────┘
```

### Certificate Generation (OpenSSL commands)

```bash
# 1. Tạo CA private key và self-signed certificate
openssl req -x509 -newkey rsa:4096 -keyout ca-key.pem -out ca-cert.pem \
    -days 365 -nodes -subj "/CN=FileTransferCA/O=FileTransfer/C=VN"

# 2. Tạo Server CSR và sign bởi CA
openssl req -newkey rsa:2048 -keyout server-key.pem -out server.csr \
    -nodes -subj "/CN=fileserver.local/O=FileTransfer/C=VN"
openssl x509 -req -in server.csr -CA ca-cert.pem -CAkey ca-key.pem \
    -CAcreateserial -out server-cert.pem -days 365

# 3. Convert server cert sang .pfx (cho Windows/.NET)
openssl pkcs12 -export -in server-cert.pem -inkey server-key.pem \
    -out server.pfx -passout pass:YourPassword

# 4. Tạo Client CSR và sign bởi CA
openssl req -newkey rsa:2048 -keyout client-key.pem -out client.csr \
    -nodes -subj "/CN=client1/O=FileTransfer/C=VN"
openssl x509 -req -in client.csr -CA ca-cert.pem -CAkey ca-key.pem \
    -CAcreateserial -out client-cert.pem -days 365

# 5. Convert client cert sang .pfx
openssl pkcs12 -export -in client-cert.pem -inkey client-key.pem \
    -out client.pfx -passout pass:YourPassword

# 6. Export CA cert (.cer format for .NET)
openssl x509 -in ca-cert.pem -out ca.cer -outform DER
```

### Thứ tự triển khai (ước tính effort)

| Bước | Công việc | Files | Effort |
|---|---|---|---|
| 1 | Generate certificates (CA, Server, Client) | 6 files .pem/.pfx/.cer | Low (1 lần) |
| 2 | Add certificates vào project directories | server.pfx, client.pfx, ca.cer | Low |
| 3 | Sửa TcpMessageHelper: NetworkStream → Stream | 1 file | Low |
| 4 | Sửa TcpServer: thêm SslStream + cert validation | 1 file | Medium |
| 5 | Sửa TcpClientService: thêm SslStream + cert validation | 1 file | Medium |
| 6 | Thêm config paths trong App.config | 2 files | Low |
| 7 | Thêm certificate password handling | 2 files | Low |
| 8 | Test handshake, trust chain, cert rotation | - | Medium |

**Tổng effort: Medium (~1-2 days với testing)**

### Rủi ro khi implement

1. **Connection string trong DB vẫn hardcoded** - mTLS chỉ bảo vệ transport, không giải quyết được DB credential leak
2. **Certificate expiration** - cần quy trình renew cert
3. **Certificate password management** - cần lưu password an toàn (Windows Certificate Store khuyến nghị)
4. **Performance overhead** - TLS handshake tốn CPU
5. **Legacy compatibility** - .NET Framework 4.7.2 hỗ trợ TLS 1.2 đầy đủ, nhưng cần kiểm tra SslProtocols
6. **Certificate distribution** - cần cơ chế deploy cert đến client machines

### Tóm tắt: Gap Analysis

```
Yêu cầu: "Client Authentication using CA + TLS/SSL"
────────────────────────────────────────────────
Current State:  ❌ Chưa đạt (0%)
TLS Transport:  ❌ NetworkStream → cần SslStream
Server Cert:    ❌ Missing → cần server.pfx
Client Cert:    ❌ Missing → cần client.pfx
CA Validation:  ❌ Missing → cần ca.cer + callback
mTLS Handshake: ❌ Missing → cần AuthenticateAsServer/Client

Required Changes: 
- TcpServer.cs:    SỬA (add SslStream, cert validation)
- TcpClientSvc.cs: SỬA (add SslStream, cert validation)
- TcpMsgHelper.cs: SỬA nhẹ (NetworkStream → Stream)
- Server/App.config: THÊM config cert path
- Client/App.config: THÊM config cert path
- New certificates: THÊM ca.cer, server.pfx, client.pfx