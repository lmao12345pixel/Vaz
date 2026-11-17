# BÁO CÁO NGHIỆM THU N3.1 — PERSIST NOTIFICATION HISTORY

**Ngày thực hiện**: 2026-11-15  
**Đợt**: N3.1 (Phase P1 của Refactor N3)  
**Tác giả**: Master & AI Assistant  
**Tiền đề**: `6c4b497` (Audit N3.0 + 3 quyết định đã chốt)  
**Trạng thái**: HOÀN THÀNH — PASS 100% GATE

---

## 1. DTO Schema & Storage Path

### 1.1. Vị trí lưu trữ
- Đường dẫn: `%APPDATA%\Vazer\notifications.json` (`Environment.GetFolderPath(SpecialFolder.ApplicationData)/Vazer/notifications.json`).
- Độc lập hoàn toàn với `settings.json`, tránh phình to config app và cô lập disk I/O.

### 1.2. Schema DTO (`src/Vazer.Core/Models/NotificationRecordDto.cs`)
```csharp
public sealed class NotificationRecordDto
{
    public int Version { get; set; } = 1;
    public int UnreadCount { get; set; }
    public List<NotificationRecordItem> Items { get; set; } = new();
}

public sealed class NotificationRecordItem
{
    public Guid Id { get; set; }
    public string Type { get; set; } = "";
    public string Priority { get; set; } = "";
    public string Title { get; set; } = "";
    public string Message { get; set; } = "";
    public string? ImageSource { get; set; }
    public string? ActionLabel1 { get; set; }
    public string? ActionLabel2 { get; set; }
    public int GroupCount { get; set; }
    public string? GroupKey { get; set; }
    public bool IsSoundEnabled { get; set; }
    public DateTime CreatedAt { get; set; }
}
```

### 1.3. Source Generator JSON Context
Đã đăng ký cả 2 DTO class vào `VazJsonContext` (`src/Vazer.Core/Json/VazJsonContext.cs`):
```csharp
[JsonSerializable(typeof(NotificationRecordDto))]
[JsonSerializable(typeof(NotificationRecordItem))]
internal sealed partial class VazJsonContext : JsonSerializerContext
```
Đảm bảo an toàn 100% cho trimmed publish và AOT runtime.

---

## 2. Store API, Concurrency & Debounce

### 2.1. File: `src/Vazer.Core/Services/NotificationHistoryStore.cs`
- **Location**: `Vazer.Core/Services/` (Option A — cho phép unit test headless hoàn toàn không phụ thuộc WinUI runtime).

### 2.2. Public API
- `Task<NotificationRecordDto> LoadAsync()`:
  - Nếu file không tồn tại: trả về `new NotificationRecordDto()`.
  - Nếu JSON corrupt: tạo backup `.bak-{yyyyMMddHHmmssfff}` và fallback về DTO trống an toàn, không ném exception gây crash app.
  - Nếu `Version > 1`: fallback về DTO trống (chưa có migration system).
- `Task SaveAsync(NotificationRecordDto dto)`:
  - Atomic write bằng file tạm `notifications.json.tmp` rồi `File.Move(tmp, target, overwrite: true)`.
  - Luôn được bảo vệ bởi `SemaphoreSlim(1, 1)` chống race condition giữa nhiều luồng ghi.
- `void RequestSave(NotificationRecordDto dto)`:
  - Non-blocking (fire-and-forget), debounce 500ms qua `CancellationTokenSource`.
  - Coalesce các lượt ghi dồn dập (ví dụ khi có chuỗi thông báo liên tiếp) thành một lần ghi duy nhất của trạng thái cuối.

---

## 3. Tích hợp NotificationService & UI

### 3.1. DI Injection & Initialization
- `NotificationHistoryStore` được đăng ký singleton trong DI container (`App.xaml.cs:51`).
- `NotificationService` nhận `NotificationHistoryStore` qua constructor.
- Phương thức `InitializeAsync()` được gọi bất đồng bộ ngay sau khi MainWindow kích hoạt (`App.xaml.cs:157`):
  ```csharp
  window.Activate();
  _ = Services.GetRequiredService<INotificationService>().InitializeAsync();
  ```
- Map từ DTO sang `NotificationItem` phục hồi dữ liệu lịch sử và `UnreadCount`, không block UI thread.

### 3.2. 4 Điểm Kích Hoạt Ghi Lịch Sử (`RequestSave`)
1. **Thêm thông báo mới** (`NotificationService.cs:100`): Khi thêm item vào History (giới hạn FIFO 50 mục).
2. **Xóa một thông báo** (`NotificationService.cs:412`): Trong `RemoveFromHistory(Guid)`.
3. **Xóa toàn bộ thông báo** (`NotificationService.cs:436`): Trong `ClearHistory()`.
4. **Đánh dấu đã đọc** (`NotificationService.cs:399`): Trong `ResetUnreadCount()` / `MarkAllRead()`.

### 3.3. Xử lý Action Button sau Restart (Quyết định Master)
- Sau khi khởi động lại, các action callback là delegate runtime trong bộ nhớ nên mang giá trị `null`.
- Thuộc tính `HasAction1` và `HasAction2` trên `NotificationItem.cs` đã được cập nhật:
  ```csharp
  public bool HasAction1 => !string.IsNullOrWhiteSpace(ActionLabel1) && ActionCallback1 != null;
  public bool HasAction2 => !string.IsNullOrWhiteSpace(ActionLabel2) && ActionCallback2 != null;
  ```
- Kết quả: Khi callback `null`, `HasAction1 = false` khiến XAML binding `Visibility="{x:Bind HasAction1, Mode=OneWay}"` tự động thu gọn (`Collapsed`). Các nút Action cũ ẩn hoàn toàn, không xuất hiện nút disabled gây nhầm lẫn cho người dùng.

---

## 4. Danh sách Unit Tests (6/6 Pass)

Tập tin: `tests/Vazer.Core.Tests/NotificationHistoryStoreTests.cs`

| # | Tên Test | Mục đích kiểm thử | Kết quả |
|---|---|---|---|
| 1 | `SaveAndLoad_RoundTrip_PreservesFieldsCorrectly` | Lưu 3 items DTO, nạp lại kiểm tra đúng từng field, thứ tự, unread count | **PASS** |
| 2 | `LoadAsync_CorruptJson_ReturnsEmptyAndCreatesBakFile` | Ghi JSON dị dạng, nạp lại không crash, tạo file `.bak-*`, trả DTO rỗng | **PASS** |
| 3 | `LoadAsync_NoFile_ReturnsEmpty` | Khi file chưa tồn tại, trả về DTO rỗng mặc định Version 1 | **PASS** |
| 4 | `SaveAsync_AtomicWrite_LeavesValidFileAndTmpCleaned` | Ghi đĩa nguyên tử, file `.tmp` được dọn dẹp sạch sẽ sau `File.Move` | **PASS** |
| 5 | `RequestSave_Debounce_CoalescesRapidCallsIntoLastState` | Gửi 5 yêu cầu liên tiếp <500ms, chỉ ghi trạng thái thứ 5 sau 500ms | **PASS** |
| 6 | `SaveAsync_Concurrent_NoRaceCondition` | 10 tác vụ `SaveAsync` song song qua SemaphoreSlim không bị đè/hỏng file | **PASS** |

---

## 5. Bằng chứng Thực nghiệm (Manual Verification & Screenshot)

- **Screenshot**: `docs/evidence/dot22/n31-history-persisted.png`
- **Quy trình kiểm thử thực tế**:
  1. Ghi dữ liệu mẫu vào `%APPDATA%\Vazer\notifications.json` (3 thông báo từ phiên trước: Sodium Success, Launch Error, No Account Warning).
  2. Khởi động app sạch `Vaz.WinUI.exe`.
  3. Mở Notification Center qua nút chuông `NotificationBellButton`.
  4. Xác nhận 3 thông báo từ phiên trước hiển thị đầy đủ và chính xác trên panel. Nút action không có callback bị ẩn hoàn toàn.

---

## 6. Gate Check Summary

- **Compiler warnings**: 0 warning, 0 error (cả Debug và Release build).
- **Test suite**: **511 / 511 tests PASSED** (505 tests ban đầu + 6 tests mới).
- **Commit policy**: KHÔNG tạo tag, KHÔNG push remote.
