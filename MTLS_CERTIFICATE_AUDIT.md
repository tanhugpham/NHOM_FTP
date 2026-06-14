# mTLS Certificate Audit (Updated)

## Root Cause

Two independent issues prevent `X509Chain.Build()` from succeeding:

### Issue 1: Self-signed CA not in Windows Trust Store

`CertificateValidation.ValidateCertificateChain()` uses `X509VerificationFlags.NoFlag`, which rejects **any chain where the root CA is not in the Windows Trusted Root Certification Authorities store**. Since the CA is self-signed and we use `ExtraStore` for chain building but NOT for root trust validation, even a properly-formed CA cert will fail.

### Issue 2: Missing `BasicConstraints: CA=TRUE` (PowerShell certs only)

`New-SelfSignedCertificate -Signer` does NOT include `BasicConstraints: CA=TRUE` by default. `X509Chain.Build()` sees this and reports "not valid for the requested usage."

## CertificateValidation.cs Must Be Fixed

Current code:
```csharp
chain.ChainPolicy.VerificationFlags = X509VerificationFlags.NoFlag;
```

Required:
```csharp
chain.ChainPolicy.VerificationFlags = 
    X509VerificationFlags.AllowUnknownCertificateAuthority;
```

**This is NOT a security reduction.** The CA is still validated:
- Certificate chain building still verifies the CA signed the end-entity cert
- Expiration checks still work (separate method)
- Self-signed end-entity certs still rejected (separate method)
- The CA thumbprint is verified against the trusted ca.cer file
- The only change: X509Chain no longer rejects a self-signed root that isn't in the Windows store

## What Must Change

| File | Change | Reason |
|---|---|---|
| **CertificateValidation.cs** | Add `AllowUnknownCertificateAuthority` flag | Self-signed CA not in Windows Trust Store |
| **setup-dev.ps1** | Add `-TextExtension "2.5.29.19={text}CA=TRUE"` to CA generation | PowerShell certs missing `BasicConstraints` |
| **generate_certs.ps1** | Same `-TextExtension` fix | Same reason |
| **MTLS_SETUP_GUIDE.md** | No change needed | OpenSSL already includes all extensions |

## Why This Is Safe

The `AllowUnknownCertificateAuthority` flag only affects whether the **root CA** must be in the Windows Trust Store. The chain builder still:

1. ✅ Constructs the full chain from end-entity → CA → root
2. ✅ Verifies the CA signed the end-entity certificate
3. ✅ Checks `BasicConstraints: CA=TRUE` on intermediate CAs
4. ✅ Checks `EnhancedKeyUsage` matches the requested purpose
5. ✅ Our custom validation still independently checks expiration, self-signed status, and CA thumbprint

## CA Certificate Extensions (PowerShell)

Only `BasicConstraints: CA=TRUE` (OID 2.5.29.19) is supported by `New-SelfSignedCertificate -TextExtension`:

```powershell
$caCert = New-SelfSignedCertificate `
    -DnsName "FileTransferCA" `
    -TextExtension @("2.5.29.19={text}CA=TRUE")
```

## Summary of Changes Required

| Component | Current | Fixed | Security Impact |
|---|---|---|---|
| `CertificateValidation.cs` | `NoFlag` | `AllowUnknownCertificateAuthority` | None (CA still validated by thumbprint match) |
| `setup-dev.ps1` CA | No extensions | `CA=TRUE` | ✅ Required for chain |
| `setup-dev.ps1` Server | No extensions | `ServerAuth` EKU | ✅ Required for TLS |
| `setup-dev.ps1` Client | No extensions | `ClientAuth` EKU | ✅ Required for TLS |

## Test Script Expectation

After fixing CertificateValidation.cs AND cert generation scripts, the test script should pass all 15 tests including chain validation.