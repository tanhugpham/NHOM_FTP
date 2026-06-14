# Testing Strategy Audit

## Executive Summary

The project has **zero unit tests** and **zero integration tests**. All testing to date has been manual. The server is moderately testable after recent refactors (dependency injection was not introduced, but thread-safety fixes improved isolation).

**Testability Score: 45/100**  
**Refactoring needed before comprehensive testing: Moderate**

---

## SECTION 1: Testability Audit

### Class-by-Class Analysis

| Class | File | Testable Today? | Reason |
|-------|------|:---:|--------|
| `AesEncryptionHelper` | `Shared/Security/AesEncryptionHelper.cs` | ✅ YES | Static methods, pure functions. Input → output. No dependencies. |
| `CertificateValidation` | `Shared/Security/CertificateValidation.cs` | ✅ YES | Static methods. Takes `X509Certificate2`, returns bool. |
| `CertificateHelper` | `Shared/Security/CertificateHelper.cs` | ⚠️ Partial | Reads from file system. Needs file paths. |
| `JsonHelper` | `Shared/Helpers/JsonHelper.cs` | ✅ YES | Pure Newtonsoft wrapper. |
| `TcpMessageHelper` | `Shared/Helpers/TcpMessageHelper.cs` | ⚠️ Partial | Requires `SslStream`. Needs mocking. |
| `AuthService` | `Server/Services/AuthService.cs` | ⚠️ Partial | Depends on `AppDbContext` (EF Core). Needs in-memory DB. |
| `AdminCleanupService` | `Server/Services/AdminCleanupService.cs` | ⚠️ Partial | Same - depends on `AppDbContext`. |
| `FileTransferStateService` | `Server/Services/FileTransferStateService.cs` | ⚠️ Partial | Depends on `AppDbContext`. |
| `TransferHistoryService` | `Server/Services/TransferHistoryService.cs` | ⚠️ Partial | Depends on `AppDbContext`. |
| `TcpServer` (handlers) | `Server/Networking/TcpServer.cs` | ❌ NO | Tightly coupled. Creates services internally (`new AuthService()`). Depends on certificates, TcpListener, SslStream. |
| `TcpClientService` | `Client/Networking/TcpClientService.cs` | ❌ NO | Creates TcpClient, SslStream internally. File system access for certs. |
| `Form1` | `Server/Form1.cs` | ❌ NO | WinForms UI. Not unit-testable. |
| `MainWindow` | `Client/MainWindow.xaml.cs` | ❌ NO | WPF UI. Not unit-testable. |

---

## SECTION 2: Unit Test Candidates (Ranked)

### HIGH VALUE (Estimated effort: 2-3 days total)

| Priority | Class | Tests | Value | Effort |
|:---:|-------|-------|:---:|:---:|
| 1 | `AesEncryptionHelper` | Encrypt/Decrypt roundtrip, key loading, null handling, empty data, large data | Verifies core encryption works | 2 hours |
| 2 | `CertificateValidation` | Expired cert, valid cert, self-signed detection, chain validation, null input | Verifies TLS security | 3 hours |
| 3 | `AuthService` | Register success, duplicate user, login success, wrong password, null inputs | Verifies auth flow | 4 hours |
| 4 | `TcpServer.HandlePushAccept` | Valid offer, missing offer, expired offer, file delivery, cleanup | Verifies critical push flow | 3 hours |
| 5 | `TcpServer.HandlePushReject` | Valid offer, missing offer, cleanup | Verifies rejection flow | 1 hour |

### MEDIUM VALUE (Estimated effort: 2-3 days)

| Priority | Class | Tests | Value | Effort |
|:---:|-------|-------|:---:|:---:|
| 6 | `TcpServer.HandleCheckForPush` | Single offer, multiple offers, no offers, after accept, after reject | Verifies multi-offer delivery | 2 hours |
| 7 | `TcpServer.HandleLogout` | Pending offers cleanup, active offers cleanup, no sessions | Verifies logout safety | 1 hour |
| 8 | `TcpServer.PushFilesToClientAsync` | Single file, multiple files, non-existent file, null filePaths | Verifies offer creation | 2 hours |
| 9 | `FileTransferStateService` | Save progress, get state, resume state, completed state | Verifies resume upload | 3 hours |
| 10 | `TransferHistoryService` | Save history, query history, empty history | Verifies audit trail | 2 hours |

### LOW VALUE (Estimated effort: 1 day)

| Priority | Class | Tests | Value | Effort |
|:---:|-------|-------|:---:|:---:|
| 11 | `CertificateHelper` | Load valid cert, load cert with wrong password, file not found | Verifies cert loading | 2 hours |
| 12 | `JsonHelper` | Serialize/Deserialize roundtrip, null, empty | Low value (Newtonsoft already tested) | 1 hour |
| 13 | `AdminCleanupService` | Clear all logs, empty DB | Low value | 2 hours |
| 14 | `TcpMessageHelper` | Send/Receive roundtrip | Needs SSL stream mock | 3 hours |

---

## SECTION 3: Integration Test Candidates

### Critical Flows (Must Test)

| Scenario | Steps | What It Verifies |
|----------|-------|------------------|
| **Register → Login** | Register user → Login with same creds → Success response | Full auth pipeline |
| **Upload → Download** | Login → Upload file → Refresh list → Download same file → Content matches | File transfer pipeline |
| **Resume Upload** | Upload 50% → Disconnect → Reconnect → Upload remaining → File complete | Resume mechanism |
| **Push Offer → Accept** | Admin pushes files → Client polls → Accept → Files delivered | Push offer pipeline |
| **Push Offer → Reject** | Admin pushes files → Client polls → Reject → No files delivered | Reject pipeline |
| **Multi-Offer** | Admin pushes 3 offers → Client receives all 3 → Accept 1 → Reject 1 → Remaining 1 | Multi-offer support |
| **Logout Cleans Offers** | Push 2 offers → Logout → Re-login → No pending offers | Cleanup correctness |
| **TLS Connection** | Connect with valid certs → Handshake succeeds | mTLS configuration |
| **TLS Rejection** | Connect with invalid cert → Handshake fails | Security enforcement |

---

## SECTION 4: Mocking Requirements

### What Must Be Abstracted

| Dependency | Used By | Mocking Strategy |
|------------|---------|------------------|
| `TcpClient` | `TcpServer`, `TcpClientService` | Create `ITcpClientFactory` interface. Inject mock. |
| `SslStream` | `TcpServer`, `TcpClientService` | Wrap in `ISslStream` interface. Use `MemoryStream`-backed mock. |
| `File System` | All services | Use `Path.Combine` + `TempFile` in integration tests. For unit tests, abstract via `IFileSystem`. |
| `Database (EF Core)` | All services | Use `InMemoryDatabase` provider. |
| `Configuration` | Certificate loading | Abstract via `IConfiguration` or pass values as parameters. |
| `DateTime.Now` | `TcpServer`, logging | Abstract via `IDateTimeProvider`. |
| `Guid` (offerId) | `PushFilesToClientAsync` | Pass `offerId` as parameter instead of generating inside. |

### Recommended Interface Changes

Before meaningful unit tests can be written for `TcpServer`, the following refactoring is needed:

1. **Constructor injection** instead of `new Service()`:
```csharp
// Before
private AuthService _authService = new AuthService();

// After
private readonly IAuthService _authService;
public TcpServer(IAuthService authService, ...) { _authService = authService; }
```

2. **ITcpClientHandler** interface for client management
3. **ISslStreamFactory** for TLS stream creation

---

## SECTION 5: Recommended Testing Stack

**Recommendation: xUnit + Moq + Entity Framework InMemory**

| Choice | Why |
|--------|-----|
| **xUnit** | Modern, widely adopted, runs on .NET Framework 4.7.2. Better test isolation (new instance per test). |
| **Moq** | Most popular .NET mocking framework. Supports .NET Framework 4.7.2. |
| **EF Core InMemory** | Built into EF Core. Already referenced in project. No extra package. |
| **TestServer** (optional) | For integration tests, can host in-process. |

### Why not MSTest or NUnit?
- **MSTest**: Tightly coupled to Visual Studio. Slower adoption of new features.
- **NUnit**: Older. Still good, but xUnit is more modern.
- **xUnit**: Better test isolation, `[Fact]` instead of `[Test]`, better parallelism.

---

## SECTION 6: Recommended Architecture Changes Before Testing

| Change | Effort | Impact |
|--------|:---:|--------|
| Extract `IAuthService` interface | 1 hour | Enables mocking auth in TcpServer tests |
| Extract `IFileTransferStateService` | 30 min | Enables mocking state in upload tests |
| Extract `ITransferHistoryService` | 30 min | Enables mocking history in download tests |
| Extract `ISharedFileService` | 30 min | Enables mocking share code in tests |
| Add constructor injection to `TcpServer` | 2 hours | Biggest impact. Enables full TcpServer testing |
| Add `IClock` interface for DateTime | 15 min | Enables deterministic time in tests |
| Make `offerId` a parameter of `PushFilesToClientAsync` | 5 min | Enables deterministic offer creation |

---

## SECTION 7: First 10 Tests to Implement

| # | Test Name | Class | Type |
|--:|-----------|-------|:---:|
| 1 | `Encrypt_Decrypt_Roundtrip_ReturnsOriginalData` | `AesEncryptionHelper` | Unit |
| 2 | `Encrypt_WithNullData_ThrowsArgumentNullException` | `AesEncryptionHelper` | Unit |
| 3 | `ValidateCertificateNotExpired_ValidCert_ReturnsVoid` | `CertificateValidation` | Unit |
| 4 | `ValidateNotSelfSigned_SelfSignedCert_Throws` | `CertificateValidation` | Unit |
| 5 | `Register_NewUser_ReturnsSuccess` | `AuthService` | Integration (in-memory DB) |
| 6 | `Login_WrongPassword_ReturnsFailure` | `AuthService` | Integration (in-memory DB) |
| 7 | `HandlePushAccept_ValidOffer_DeliversFiles` | `TcpServer` | Integration (after refactor) |
| 8 | `HandleCheckForPush_MultipleOffers_ReturnsAll` | `TcpServer` | Integration (after refactor) |
| 9 | `PushFilesToClientAsync_ThreeFiles_AllStored` | `TcpServer` | Unit (after refactor) |
| 10 | `HandleLogout_WithPendingOffers_AllCleaned` | `TcpServer` | Unit (after refactor) |

---

## SECTION 8: Estimated Total Effort

| Phase | Effort | Deliverable |
|-------|:---:|-------------|
| Architecture refactoring (interfaces + DI) | 1 day | Testable `TcpServer` |
| Unit test infrastructure (xUnit + Moq project) | 0.5 day | Test project scaffold |
| HIGH priority tests (5 test classes) | 2-3 days | 20-30 tests |
| MEDIUM priority tests (5 test classes) | 2-3 days | 20-30 tests |
| LOW priority tests (4 test classes) | 1 day | 10-15 tests |
| Integration tests (critical flows) | 2 days | 5-7 scenarios |
| CI/CD pipeline integration | 1 day | GitHub Actions + xUnit runner |

**Total: 8-12 days for comprehensive test coverage**

---

## SECTION 9: Risk Assessment

| Risk | Likelihood | Impact | Mitigation |
|------|:---:|:---:|-----------|
| Tests pass but production fails | Medium | High | Add integration tests for critical paths |
| Mock mismatch with real behavior | Medium | Medium | Use integration tests for DB + TLS |
| Time pressure skips test writing | High | High | Enforce PR gate with minimum coverage |
| TcpServer refactoring introduces bugs | Medium | High | Write tests first, refactor second |

---

## SECTION 10: Conclusion

**Current testability: 45/100**  
**Target testability after refactoring: 85/100**

The highest ROI is:
1. Adding interfaces + DI to `TcpServer` (1 day)
2. Testing `AesEncryptionHelper` + `CertificateValidation` immediately (no refactoring needed)
3. Testing `AuthService` with in-memory DB (immediate)

Do NOT skip architecture refactoring before testing TcpServer - it will save significant time in the long run.