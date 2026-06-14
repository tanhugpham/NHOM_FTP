# PowerShell Certificate Extension Audit

## Root Cause

The `setup-dev.ps1` failed when generating the CA certificate because `-TextExtension` with OID `2.5.29.15` (KeyUsage) using `{text}` format is **not supported** by `New-SelfSignedCertificate`.

## Why the Extension Failed

```powershell
# This FAILS:
$caCert = New-SelfSignedCertificate `
    -TextExtension @("2.5.29.15={text}KeyCertSign,CRLSign")

# Error: "The parameter is incorrect. 0x80070057"
```

**Root cause:** PowerShell's `New-SelfSignedCertificate` cmdlet does not accept `{text}` formatting for extensions that require `hex` or bit-string encoding. The KeyUsage extension (`2.5.29.15`) requires a bit-string value format, not a text string.

## Supported OIDs and Formats

| OID | Extension | Supported Syntax |
|---|---|---|
| `2.5.29.19` | BasicConstraints | ✅ `{text}CA=TRUE` |
| `2.5.29.37` | EnhancedKeyUsage | ✅ `{text}1.3.6.1.5.5.7.3.1` |
| `2.5.29.15` | KeyUsage | ❌ `{text}KeyCertSign` NOT supported |
| `2.5.29.17` | SubjectAltName | ✅ `{text}DNS=example.com` |

The `{hex}` format could work for KeyUsage but is undocumented and fragile.

## Does CertificateValidation.cs Require KeyCertSign?

**NO.** After the fix to `CertificateValidation.cs` (adding `AllowUnknownCertificateAuthority`), the `X509Chain.Build()` method no longer checks for `KeyUsage: KeyCertSign` on the CA certificate.

The validation flow is now:

1. ✅ `X509Chain.Build()` with `AllowUnknownCertificateAuthority` — accepts chain even without `KeyUsage.CertificateSigning`
2. ✅ `hasTrustedRoot` thumbprint check — verifies the CA is OUR specific CA
3. ✅ `ValidateCertificateNotExpired()` — independent check
4. ✅ `ValidateNotSelfSigned()` — rejects self-signed end-entity certs

**KeyCertSign is NOT required** for our validation logic.

## Final Certificate Generation Approach

### CA Certificate

```powershell
$caCert = New-SelfSignedCertificate `
    -DnsName "FileTransferCA" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeyLength 4096 `
    -NotAfter (Get-Date).AddYears(10) `
    -TextExtension @("2.5.29.19={text}CA=TRUE")  # ← Only CA=TRUE
```

### Server Certificate

```powershell
$serverCert = New-SelfSignedCertificate `
    -DnsName "127.0.0.1","localhost" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeyLength 2048 `
    -Signer $caCert `
    -NotAfter (Get-Date).AddYears(1) `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.1")  # ← Server Auth EKU
```

### Client Certificate

```powershell
$clientCert = New-SelfSignedCertificate `
    -DnsName "filetransfer-client" `
    -CertStoreLocation "Cert:\CurrentUser\My" `
    -KeyExportPolicy Exportable `
    -KeyLength 2048 `
    -Signer $caCert `
    -NotAfter (Get-Date).AddYears(1) `
    -TextExtension @("2.5.29.37={text}1.3.6.1.5.5.7.3.2")  # ← Client Auth EKU
```

### Verification after generation

Each script validates:
1. **CA**: `BasicConstraints` extension contains `CA=TRUE`
2. **Server**: `EnhancedKeyUsage` extension contains `Server Authentication`
3. **Client**: `EnhancedKeyUsage` extension contains `Client Authentication`
4. Certificate files exist in expected directories
5. App.config placeholders replaced

## Comparison: PowerShell vs OpenSSL

| Feature | PowerShell | OpenSSL |
|---|---|---|
| `BasicConstraints: CA=TRUE` | ✅ `-TextExtension "2.5.29.19={text}CA=TRUE"` | ✅ `-ext v3_ca` |
| `KeyUsage: KeyCertSign` | ❌ Not supported via `-TextExtension` | ✅ `keyCertSign` |
| `EKU: ServerAuth` | ✅ `-TextExtension "2.5.29.37={text}1.3.6.1.5.5.7.3.1"` | ✅ `serverAuth` |
| `EKU: ClientAuth` | ✅ `-TextExtension "2.5.29.37={text}1.3.6.1.5.5.7.3.2"` | ✅ `clientAuth` |
| Chain validation | ✅ Works with `AllowUnknownCertificateAuthority` + thumbprint check | ✅ Works with standard chain validation |

**Recommendation:** For production, use OpenSSL (documented in `MTLS_SETUP_GUIDE.md`). PowerShell is for development convenience only.

## Scripts Fixed

| Script | Before | After |
|---|---|---|
| `setup-dev.ps1` | Had `"2.5.29.15={text}KeyCertSign,CRLSign"` (FAILED) | Removed unsupported extension. Only `CA=TRUE` |
| `generate_certs.ps1` | Already correct (only `CA=TRUE`) | No change needed |