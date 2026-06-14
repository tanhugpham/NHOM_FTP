# Certificate Authority (CA) Setup Guide

## Overview

Hệ thống Secure File Transfer sử dụng **Mutual TLS (mTLS)**.

Điều này yêu cầu:

- Server có certificate riêng
- Client có certificate riêng
- Cả hai certificate đều được ký bởi cùng một Certificate Authority (CA)

Kiến trúc:

```
                CA Certificate
                      │
        ┌─────────────┴─────────────┐
        │                           │
  Server Certificate         Client Certificate
```

---

# Prerequisites

Cài đặt OpenSSL:

Windows:

```powershell
winget install ShiningLight.OpenSSL
```

Hoặc tải:

https://slproweb.com/products/Win32OpenSSL.html

Kiểm tra:

```powershell
openssl version
```

Ví dụ:

```text
OpenSSL 3.5.0
```

---

# Certificate Structure

Tạo thư mục:

```text
Certificates/
│
├── ca.key
├── ca.crt
│
├── server.key
├── server.csr
├── server.crt
├── server.pfx
│
├── client.key
├── client.csr
├── client.crt
├── client.pfx
│
└── ca.srl
```

---

# Step 1 - Create Certificate Authority

## Generate CA Private Key

```powershell
openssl genrsa -out ca.key 4096
```

---

## Generate CA Certificate

```powershell
openssl req -x509 `
    -new `
    -nodes `
    -key ca.key `
    -sha256 `
    -days 3650 `
    -out ca.crt `
    -subj "/CN=FileTransfer-CA"
```

Thông tin:

| Property | Value |
|----------|--------|
| CN | FileTransfer-CA |
| Expiry | 10 years |
| Purpose | Root CA |

---

# Step 2 - Create Server Certificate

## Generate Server Private Key

```powershell
openssl genrsa -out server.key 2048
```

---

## Generate CSR

```powershell
openssl req -new `
    -key server.key `
    -out server.csr `
    -subj "/CN=127.0.0.1"
```

Nếu deploy production:

```text
CN=myserver.domain.com
```

---

## Create Server Extension File

Tạo file:

```text
server.ext
```

Nội dung:

```text
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=serverAuth
subjectAltName=IP:127.0.0.1
```

---

## Sign Server Certificate

```powershell
openssl x509 `
    -req `
    -in server.csr `
    -CA ca.crt `
    -CAkey ca.key `
    -CAcreateserial `
    -out server.crt `
    -days 365 `
    -sha256 `
    -extfile server.ext
```

---

## Export Server PFX

```powershell
openssl pkcs12 -export `
    -out server.pfx `
    -inkey server.key `
    -in server.crt `
    -certfile ca.crt
```

Nhập password.

Ví dụ:

```text
Server@2026
```

Lưu password này vào:

```xml
<add key="ServerCertPassword" value="Server@2026"/>
```

---

# Step 3 - Create Client Certificate

## Generate Client Key

```powershell
openssl genrsa -out client.key 2048
```

---

## Generate CSR

```powershell
openssl req -new `
    -key client.key `
    -out client.csr `
    -subj "/CN=filetransfer-client"
```

---

## Create Client Extension File

Tạo:

```text
client.ext
```

Nội dung:

```text
authorityKeyIdentifier=keyid,issuer
basicConstraints=CA:FALSE
keyUsage=digitalSignature,keyEncipherment
extendedKeyUsage=clientAuth
```

---

## Sign Client Certificate

```powershell
openssl x509 `
    -req `
    -in client.csr `
    -CA ca.crt `
    -CAkey ca.key `
    -CAcreateserial `
    -out client.crt `
    -days 365 `
    -sha256 `
    -extfile client.ext
```

---

## Export Client PFX

```powershell
openssl pkcs12 -export `
    -out client.pfx `
    -inkey client.key `
    -in client.crt `
    -certfile ca.crt
```

Ví dụ password:

```text
Client@2026
```

Lưu vào:

```xml
<add key="ClientCertPassword" value="Client@2026"/>
```

---

# Step 4 - Deploy Certificates

## Server

Copy:

```text
server.pfx
ca.crt
```

vào:

```text
FileTransfer.Server\Certificates\
```

---

## Client

Copy:

```text
client.pfx
ca.crt
```

vào:

```text
FileTransfer.Client\Certificates\
```

---

# Step 5 - App.config

## Server

```xml
<add key="ServerCertificatePath" value="Certificates\server.pfx" />
<add key="CACertificatePath" value="Certificates\ca.crt" />
<add key="ServerCertPassword" value="Server@2026" />
```

---

## Client

```xml
<add key="ClientCertificatePath" value="Certificates\client.pfx" />
<add key="CACertificatePath" value="Certificates\ca.crt" />
<add key="ClientCertPassword" value="Client@2026" />
```

---

# Step 6 - Validation

Kiểm tra server cert:

```powershell
openssl verify -CAfile ca.crt server.crt
```

Kết quả:

```text
server.crt: OK
```

---

Kiểm tra client cert:

```powershell
openssl verify -CAfile ca.crt client.crt
```

Kết quả:

```text
client.crt: OK
```

---

# Security Notes

## Development

Có thể commit:

```text
ca.crt
```

Không chứa private key.

---

## Never Commit

Không được commit:

```text
ca.key
server.key
client.key

server.pfx
client.pfx
```

Thêm vào `.gitignore`:

```gitignore
*.key
*.pfx
*.csr
*.srl
```

---

# Troubleshooting

## "The specified network password is not correct"

Nguyên nhân:

- Sai password PFX
- Password trong App.config không khớp

---

## Certificate chain validation failed

Nguyên nhân:

- Client/Server dùng CA khác nhau
- ca.crt không đúng
- Certificate hết hạn

---

## CN validation failed

Nguyên nhân:

Server certificate:

```text
CN=127.0.0.1
```

nhưng client connect:

```text
localhost
```

CN phải khớp hostname hoặc IP được sử dụng để kết nối.

---

# Recommended Production Setup

- CA riêng
- Server certificate riêng cho từng máy
- Client certificate riêng cho từng user
- Secrets lưu trong Key Vault hoặc Secret Manager
- Không lưu password trực tiếp trong App.config