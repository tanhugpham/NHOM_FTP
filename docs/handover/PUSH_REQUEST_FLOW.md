# Push Request Flow Audit

## 1. Files Involved

### UI Files
| File | Purpose |
|------|---------|
| `FileTransfer.Client/MainWindow.xaml` | Nút "Requests (n)" ở Activity Logs |
| `FileTransfer.Client/MainWindow.xaml.cs` | Poll timer, nhận offer từ server, mở RequestsWindow |
| `FileTransfer.Client/RequestsWindow.xaml` | Form riêng: DataGrid + summary cards + action buttons |
| `FileTransfer.Client/RequestsWindow.xaml.cs` | Xử lý Accept/Reject/Re-DL/Detail/Refresh |

### ViewModels
| File | Purpose |
|------|---------|
| `FileTransfer.Client/OfferViewModel.cs` | `OfferId`, `FromUser`, `FileCount`, `SizeDisplay`, `ReceivedAt`, `IsDownloaded`, `StatusDisplay`, `FileNames`, `OriginalOffer` |

### DTOs
| File | Purpose |
|------|---------|
| `FileTransfer.Shared/DTOs/ServerPushOfferDto.cs` | `OfferId`, `FromUser`, `Files[]`, `TotalSize`, `Status` |
| `FileTransfer.Shared/DTOs/ServerPushFileDto.cs` | `FileName`, `FileSize`, `FileData` (dùng khi deliver file) |
| `FileTransfer.Shared/DTOs/PushFileInfo.cs` | `FileName`, `FileSize` (trong ServerPushOfferDto) |

### Message Types
| Enum | Value | Purpose |
|------|-------|---------|
| `MessageType.CheckForPush` | 27 | Client poll server xem có offer không |
| `MessageType.PushOffer` | 28 | (Dự phòng - hiện dùng CheckForPush response) |
| `MessageType.PushAccept` | 29 | Client chấp nhận offer |
| `MessageType.PushReject` | 30 | Client từ chối offer |

### Client Networking
| File | Method | Purpose |
|------|--------|---------|
| `FileTransfer.Client/Networking/TcpClientService.cs` | `SendMessageAsync(string)` | Gửi JSON qua TLS, nhận response |
| `FileTransfer.Client/Networking/TcpClientService.cs` | `ConnectAsync(ip, port)` | Kết nối TLS |
| `FileTransfer.Client/Networking/TcpClientService.cs` | `Disconnect()` | Ngắt kết nối |

### Server Handlers
| File | Method | Purpose |
|------|--------|---------|
| `FileTransfer.Server/Networking/TcpServer.cs` | `PushFilesToClientAsync(username, filePaths[])` | Tạo offer mới, lưu vào `_pendingOffers` + `_activeOffers` |
| `FileTransfer.Server/Networking/TcpServer.cs` | `HandleCheckForPush(client)` | Trả về offer JSON cho client (dòng 930-945) |
| `FileTransfer.Server/Networking/TcpServer.cs` | `HandlePushAccept(msg, client)` | Xóa offer khỏi `_pendingOffers`, đọc file từ `_activeOffers`, gửi `List<ServerPushFileDto>` |
| `FileTransfer.Server/Networking/TcpServer.cs` | `HandlePushReject(msg, client)` | Xóa offer khỏi `_pendingOffers`, log reject |
| `FileTransfer.Server/Form1.cs` | `btnPushFile_Click` | Multi-select file dialog, gọi `PushFilesToClientAsync` |

### Database Tables

**No database tables are directly involved.** All push request data is stored **in-memory** via:

| Field | Type | Location |
|-------|------|----------|
| `_pendingOffers` | `ConcurrentDictionary<string, ServerPushOfferDto>` | `TcpServer.cs` line 51-52 |
| `_activeOffers` | `Dictionary<string, (string username, string[] filePaths)>` | `TcpServer.cs` line 54-55 |

After `Accept`, files are delivered directly from disk (`_activeOffers` maps `offerId → filePaths`). After delivery, the offer is removed from memory.

---

## 2. Flow Diagrams

### 2.1 Accept Flow

```
[UI] btnAccept_Click (RequestsWindow.xaml.cs:70)
  ↓ DataContext → OfferViewModel.OfferId
  ↓
[Client] SendMessageAsync
  ↓ NetworkMessage { Type: PushAccept, JsonBody: "offerId" }
  ↓
[Server] HandlePushAccept (TcpServer.cs:947)
  ↓ TryRemove _pendingOffers[username]
  ↓ TryGetValue _activeOffers[offerId] → filePaths[]
  ↓ Read all files from disk into List<ServerPushFileDto>
  ↓ JsonHelper.Serialize(allFileData)
  ↓ return BaseResponseDto { Success: true, Message: dataJson }
  ↓
[Client] Deserialize response.Message → List<ServerPushFileDto>
  ↓ Foreach file: SaveFileDialog → File.WriteAllBytes
  ↓
[Client] offer.IsDownloaded = true
  ↓ offer.StatusDisplay = "✅ Downloaded"
  ↓ RefreshGrid() → update summary counts
```

### 2.2 Reject Flow

```
[UI] btnReject_Click (RequestsWindow.xaml.cs:122)
  ↓ DataContext → OfferViewModel.OfferId
  ↓
[Client] SendMessageAsync
  ↓ NetworkMessage { Type: PushReject, JsonBody: "offerId" }
  ↓
[Server] HandlePushReject (TcpServer.cs:981)
  ↓ TryRemove _pendingOffers[username]
  ↓ offer.Status = "Rejected"
  ↓ OnLog: "Push offer rejected by user: offerId"
  ↓ return BaseResponseDto { Success: true }
  ↓
[Client] offer.StatusDisplay = "❌ Rejected"
  ↓ _offers.Remove(offer)
  ↓ RefreshGrid()
```

### 2.3 Re-Download Flow

```
[UI] btnRedownload_Click (RequestsWindow.xaml.cs:151)
  ↓ DataContext → OfferViewModel.OfferId
  ↓ (Same as Accept - sends PushAccept again)
  ↓
[Client] SendMessageAsync
  ↓ NetworkMessage { Type: PushAccept, JsonBody: "offerId" }
  ↓
[Server] HandlePushAccept (TcpServer.cs:947)
  ↓ TryRemove fails (already removed on first Accept)
  ↓ return BaseResponseDto { Success: false, Message: "No pending offer" }
  ↓
[Client] MessageBox: "Cannot re-download: offer may have expired."
```

**⚠️ BUG:** Re-Download will fail because `HandlePushAccept` does `TryRemove` on `_pendingOffers`. After first accept, the offer is gone. Re-DL sends `PushAccept` again but there's no offer to deliver.

**Fix needed:** Server should keep accepted offers in a separate dictionary or re-create them from `_activeOffers` if the client requests re-download.

### 2.4 Detail Flow

```
[UI] btnDetail_Click (RequestsWindow.xaml.cs:176)
  ↓ DataContext → OfferViewModel
  ↓ Format detail string: OfferId, FromUser, Time, Size, FileNames
  ↓ MessageBox.Show(detail)
  (No network call - purely local)
```

### 2.5 Refresh Flow

```
[UI] btnRefresh_Click (RequestsWindow.xaml.cs:181)
  ↓
[Client] RefreshGrid() (RequestsWindow.xaml.cs:190)
  ↓ _offers.Where(status ≠ "Rejected").OrderByDescending(ReceivedAt).ToList()
  ↓ dgOffers.ItemsSource = activeOffers
  ↓ PendingCount = count(!IsDownloaded && !Rejected)
  ↓ AcceptedCount = count(IsDownloaded && Accepted)
  ↓ RejectedCount = count(Rejected)
```

**⚠️ BUG:** `RefreshGrid()` only filters/sorts **local in-memory list** (`_offers`). It does **not** call `CheckForPush` to fetch new offers from server. New offers only arrive via `PushPollTimer_Tick` (every 5 seconds) in `MainWindow.xaml.cs`.

**To fetch new data on Refresh:** `RequestsWindow` would need access to the polling mechanism or call `CheckForPush` directly, then update `_offers` list (which is shared by reference with `MainWindow._offerList`).

---

## 3. Data Flow: Offer Creation to Client Display

```
[Server UI] Form1.btnPushFile_Click
  ↓ OpenFileDialog.MultiSelect = true
  ↓ filePaths[]
  ↓
[TcpServer] PushFilesToClientAsync(username, filePaths)
  ↓ Create ServerPushOfferDto { OfferId, Files, TotalSize, Status="Pending" }
  ↓ _pendingOffers[username] = offer
  ↓ _activeOffers[offerId] = (username, filePaths)
  ↓
[Client] PushPollTimer_Tick (every 5s)
  ↓ Send CheckForPush
  ↓
[Server] HandleCheckForPush
  ↓ TryGetValue _pendingOffers[username] → offer
  ↓ return BaseResponseDto { Success: true, Message: offerJson }
  ↓
[Client] Deserialize offer
  ↓ Create OfferViewModel
  ↓ _offerList.Add(vm)
  ↓ UpdateRequestsButton() → "Requests (n)"
  ↓ AddLog("Push offer received")
  ↓
[UI] User clicks "Requests (n)"
  ↓ btnOpenRequests_Click → new RequestsWindow(_offerList)
```

---

## 4. Summary

| Action | UI Handler | Client Method | Message Type | Server Handler | DB Involved |
|--------|-----------|---------------|--------------|----------------|-------------|
| **Receive Offer** | `PushPollTimer_Tick` | `SendMessageAsync` | `CheckForPush` | `HandleCheckForPush` | None (in-memory) |
| **Accept** | `btnAccept_Click` | `SendMessageAsync` | `PushAccept` | `HandlePushAccept` | None (reads disk) |
| **Reject** | `btnReject_Click` | `SendMessageAsync` | `PushReject` | `HandlePushReject` | None |
| **Re-Download** | `btnRedownload_Click` | `SendMessageAsync` | `PushAccept` | `HandlePushAccept` | None (⚠️ bug) |
| **Detail** | `btnDetail_Click` | None (local) | - | - | - |
| **Refresh** | `btnRefresh_Click` | None (local) | - | - | - |

### Known Issues:

1. **Re-Download bug**: Server removes offer after first accept. Re-DL will fail.
2. **Refresh does not fetch new data**: Only sorts/filters local in-memory list.
3. **No persistence**: Offers are in-memory only. Server restart loses all pending offers.
4. **No database audit trail**: Accept/reject history is only logged via `OnLog` (UI log), not stored in `TransferHistories` table.