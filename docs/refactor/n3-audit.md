# Báo cáo Audit N3.0 — Notification Settings & Persistence

- **Ngày thực hiện**: 2026-11-15
- **Đợt**: N3.0 (Audit — KHÔNG code)
- **Repo**: Vazer Launcher (`C:\OptizimeProject\Vaz`)
- **Tiền đề**: HEAD `cf60209`, 505 test pass, 0 warning.

---

## 1. Tổng quan & Bối cảnh

Đợt kiểm tra N0.1 đã chỉ ra 3 thiếu sót cốt lõi trong hệ thống thông báo của Vazer:
1. **Lịch sử thông báo bay màu khi restart**: `NotificationService.History` (tối đa 50 item) chỉ nằm trên RAM (`ObservableCollection<NotificationItem>`). Tắt launcher là toàn bộ lịch sử biến mất.
2. **Thiếu cấu hình người dùng**: Không có toggle tắt/bật toast, không có chế độ Không làm phiền (Do Not Disturb - DND), không có bộ lọc "Chỉ xem lỗi" trong Notification Center.
3. **Sự cố âm thanh**: `UiSoundEnabled` trong Settings thực chất là âm click UI của Minecraft (`SoundHelper.PlayClickSound`), không phải âm thông báo. Trong khi đó, thuộc tính `NotificationItem.IsSoundEnabled` được khai báo nhưng hoàn toàn bị bỏ quên (dead field); `PlaySoundSafe` luôn phát âm thanh `ms-winsoundevent:Notification.*`.

### Các rủi ro kỹ thuật khi hiện thực hóa N3:
- **Non-serializable Callbacks**: `NotificationItem` chứa `Func<Task>? ActionCallback1` và `Func<Task>? ActionCallback2` (cho các nút Action như Retry, Undo, Mở thư mục). Delegate/lambda không thể serialize sang JSON.
- **Concurrent Access & UI Thread Deadlock**: Thông báo có thể được bắn từ background worker (tải mod, kiểm tra cập nhật, Java crash) lẫn UI thread. Thao tác ghi file đĩa đồng bộ trong lock RAM hoặc tranh chấp giữa DispatcherQueue và disk I/O có thể gây giật lag hoặc deadlock.
- **Schema Evolution / Migration**: Cần mở rộng `GlobalSettings` mà không làm hỏng file `settings.json` hiện tại của người dùng, đảm bảo fallback an toàn về giá trị mặc định.

---

## 2. BƯỚC 1 — Inventory NotificationItem

Tập tin: `src/Vaz.WinUI/Models/NotificationItem.cs` (254 dòng).

### 2.1. Bảng phân loại chi tiết thuộc tính

| Tên Property | Kiểu dữ liệu | Phân loại | Ghi chú & Khả năng Serialize |
|---|---|---|---|
| `Id` | `Guid` | **Serializable** | Khóa định danh duy nhất (Primary Key). Mặc định `Guid.NewGuid()`. |
| `Type` | `NotificationType` (enum) | **Serializable** | `Success, Warning, Error, Info, Update, Progress`. Lưu dưới dạng int hoặc string. |
| `Priority` | `NotificationPriority` (enum) | **Serializable** | `Low, Normal, High, Critical`. |
| `Title` | `string` | **Serializable** | Tiêu đề thông báo. |
| `Message` | `string` | **Serializable** | Nội dung chi tiết thông báo. |
| `ImageSource` | `string?` | **Serializable** | Đường dẫn icon/ảnh tùy chọn. |
| `ActionLabel1` | `string?` | **Serializable** | Nhãn nút hành động 1 (ví dụ: "Thử lại", "Mở"). |
| `ActionCallback1` | `Func<Task>?` | **Non-serializable** | **Callback delegate**. Không thể serialize ra JSON. Phải tách khỏi persistence. |
| `ActionLabel2` | `string?` | **Serializable** | Nhãn nút hành động 2. |
| `ActionCallback2` | `Func<Task>?` | **Non-serializable** | **Callback delegate**. Không thể serialize ra JSON. Phải tách khỏi persistence. |
| `ProgressValue` | `double` | **Transient** | Tiến trình (-1.0 nếu không có progress). Chỉ có ý nghĩa trong session active. |
| `GroupCount` | `int` | **Serializable** | Số lượng thông báo được gộp (badge xN). Mặc định 1. |
| `GroupKey` | `string?` | **Serializable** | Khóa định danh để gộp thông báo tương tự. |
| `IsSticky` | `bool` | **Transient** | Cờ ghim toast không tự tắt. Chỉ có ý nghĩa với toast đang active. |
| `AutoDismissMs` | `int` | **Transient** | Thời gian hiển thị toast (5000ms/8000ms). Chỉ áp dụng cho toast. |
| `IsSoundEnabled` | `bool` | **Serializable** | Cờ bật âm thanh cho thông báo này. |
| `CreatedAt` | `DateTime` | **Serializable** | Thời điểm tạo thông báo. Mặc định `DateTime.Now`. |
| `HasImage` | `bool` (getter) | Computed | Tính từ `!string.IsNullOrWhiteSpace(ImageSource)`. Không lưu. |
| `HasAction1` | `bool` (getter) | Computed | Tính từ `!string.IsNullOrWhiteSpace(ActionLabel1)`. Không lưu. |
| `HasAction2` | `bool` (getter) | Computed | Tính từ `!string.IsNullOrWhiteSpace(ActionLabel2)`. Không lưu. |
| `IsProgressMode` | `bool` (getter) | Computed | Tính từ `Type == Progress || ProgressValue >= 0`. Không lưu. |
| `ProgressPercentText`| `string` (getter) | Computed | Chuỗi phần trăm hiển thị. Không lưu. |
| `DisplayTitle` | `string` (getter) | Computed | Title kèm `(x{GroupCount})`. Không lưu. |
| `GroupBadgeText` | `string` (getter) | Computed | Badge text `x{GroupCount}`. Không lưu. |
| `HasGroupBadge` | `bool` (getter) | Computed | Cờ hiển thị badge gộp. Không lưu. |
| `CreatedAtFormatted`| `string` (getter) | Computed | Định dạng `HH:mm:ss`. Không lưu. |
| `IconGlyph` | `string` (getter) | Computed | Ký tự Segoe Fluent Icons theo Type. Không lưu. |
| `TypeColorHex` | `string` (getter) | Computed | Mã màu Hex theo Type. Không lưu. |

### 2.2. Tổng kết thuộc tính
- **Tổng số public properties**: 28.
  - **Serializable Data**: 10 (`Id`, `Type`, `Priority`, `Title`, `Message`, `ImageSource`, `ActionLabel1`, `ActionLabel2`, `GroupCount`, `GroupKey`, `IsSoundEnabled`, `CreatedAt`).
  - **Non-serializable Callbacks**: 2 (`ActionCallback1`, `ActionCallback2`).
  - **Transient Session State**: 3 (`ProgressValue`, `IsSticky`, `AutoDismissMs`).
  - **Computed/Derived UI Getters**: 11 (bỏ qua khi serialize).
- **Khóa định danh (Identity Key)**: Thuộc tính `Id` (`Guid`), khởi tạo tự động bằng `Guid.NewGuid()`.

---

## 3. BƯỚC 2 — Inventory NotificationService

Tập tin: `src/Vaz.WinUI/Services/NotificationService.cs` (458 dòng).

### 2.1. Cấu trúc lưu trữ History & Active
- `ActiveNotifications`: `public ObservableCollection<NotificationItem> ActiveNotifications { get; } = new();` (`line 20`).
- `History`: `public ObservableCollection<NotificationItem> History { get; } = new();` (`line 21`).
- `UnreadCount`: `private int _unreadCount;` (`line 23-35`), có event `UnreadCountChanged`.

### 2.2. Điểm ghi nhận và vòng đời History
- **Thêm vào History**: `NotificationService.cs:90-95`.
  ```csharp
  History.Insert(0, notification);
  while (History.Count > MaxHistoryCount)
  {
      History.RemoveAt(History.Count - 1);
  }
  UnreadCount++;
  ```
  Thông báo mới nhất luôn chèn vào index 0 (Newest-first).
- **Xóa từng mục khỏi History**: `NotificationService.cs:383-396` (`RemoveFromHistory(Guid id)`).
- **Xóa toàn bộ History**: `NotificationService.cs:406-416` (`ClearHistory()`): gọi `History.Clear()` và `UnreadCount = 0`.
- **Đánh dấu đã đọc**: `NotificationService.cs:370-381` (`MarkAllRead()`): đặt `UnreadCount = 0`.

### 2.3. Chính sách thu dọn (Eviction Policy)
- Giới hạn: `private const int MaxHistoryCount = 50;` (`line 16`).
- Thuật toán thu dọn: **FIFO nghiêm ngặt** theo thứ tự thời gian (`RemoveAt(History.Count - 1)`). Bất kể `NotificationPriority`, mục cũ nhất sẽ bị loại bỏ khi vượt quá 50 mục.

### 2.4. Kiểm soát tương tranh (Concurrency Control)
- Dùng khóa luồng nội bộ: `private readonly object _lock = new();` (`line 18`).
- Bọc điều phối UI: `RunOnUIThread(() => { lock (_lock) { ... } });` (`line 43-45`).
- Nhận xét: Luồng điều phối hiện tại phụ thuộc `DispatcherQueue`. Nếu gọi persist vào file I/O trực tiếp bên trong `lock (_lock)` trên UI thread sẽ dẫn đến hiện tượng khựng khung hình (UI hitch/freeze). Cần cơ chế ghi file bất đồng bộ chạy nền qua `Task.Run` hoặc `Channel<T>`.

### 2.5. Cơ chế hiển thị của Notification Center
- Tập tin giao diện: `src/Vaz.WinUI/Views/Controls/NotificationCenterPanel.xaml` & `.xaml.cs`.
- Gắn dữ liệu: `HistoryItemsControl.ItemsSource = _service.History;` (`Panel.cs:31`).
- Nút Action trên panel: `NotificationCenterPanel.xaml:185-193` & `Panel.cs:139-169`.
  - Khi người dùng click nút action trong History panel, code kiểm tra:
    ```csharp
    if (model.ActionCallback1 != null)
    {
        await model.ActionCallback1();
    }
    ```
  - **Hành vi sau khi restart**: Khi thông báo được load lại từ file JSON, `ActionCallback1` là `null`. Vì vậy, nút Action có thể bị ẩn (`HasAction1` dựa trên Label nhưng Callback = null) hoặc bị vô hiệu hóa (disabled), tránh gọi delegate rỗng. Chấp nhận rằng lịch sử chỉ có tính chất tham khảo nhật ký (audit log).

---

## 4. BƯỚC 3 — Inventory Settings hiện tại

### 3.1. Cấu trúc và Vị trí lưu Settings
- Tập tin cấu hình lõi: `src/Vazer.Core/Settings/GlobalSettings.cs` (95 dòng).
- Tập tin xử lý lưu/đọc: `src/Vazer.Core/Settings/SettingsService.cs` (220 dòng).
- Vị trí lưu file: `{MinecraftOptions.RootPath}/settings.json` (mặc định `%APPDATA%\Vazer\Minecraft\settings.json`, `SettingsService.cs:48`).
- Định dạng JSON: `System.Text.Json` với Source Generator qua `VazJsonContext.Settings.GlobalSettings` (`SettingsService.cs:34-35`, `VazJsonContext.cs:46`).

### 3.2. Cơ chế ghi/đọc và Tính an toàn
- **Kiểm soát đồng thời**: Sử dụng `private readonly SemaphoreSlim _lock = new(1, 1);` (`SettingsService.cs:41`).
- **Ghi nguyên tử (Atomic Write)**: `SettingsService.cs:146-160`:
  1. Ghi ra file tạm: `_filePath + ".tmp"` qua `File.WriteAllTextAsync`.
  2. Di chuyển đè lên file thật: `File.Move(tmpPath, _filePath, true)`. Tránh file rỗng/cụt khi app crash.
- **Tự động phục hồi lỗi (Self-healing)**: `SettingsService.cs:78-90`: Tự sửa lỗi double-escaped quotes `\"`, backup file hỏng thành `.bak` (`line 162-176`) và trả về cấu hình mặc định an toàn.
- **Chuẩn hóa giá trị (Normalization)**: `SettingsService.cs:182-220`: Mọi giá trị thiếu hoặc vượt ngưỡng đều được kẹp khoảng (`Math.Clamp`) hoặc gán giá trị mặc định. Nếu schema JSON thiếu trường mới, `System.Text.Json` tự động giữ giá trị mặc định của C# class `GlobalSettings`.

### 3.3. Các Toggle âm thanh và giao diện hiện tại
- Trong `GlobalSettings.cs`:
  - `public bool UiSoundEnabled { get; set; } = true;` (`line 49`).
  - Nhãn hiển thị trong `SettingsPage.xaml:448-449`: "Phát âm thanh click xúc giác Minecraft khi tương tác".
  - Đây là âm click giao diện, không phải âm báo của Notification.
- Trong `NotificationItem.cs`:
  - `public bool IsSoundEnabled { get; set; } = true;` (`line 208`).
  - Trong `NotificationService.cs`: `PlaySoundSafe(notification.Type)` được gọi trực tiếp mà **không kiểm tra** `notification.IsSoundEnabled` hay bất kỳ setting nào.

### 3.4. Điểm mở rộng cho Notification Settings
Để bổ sung cài đặt thông báo, cần thêm vào:
1. `Vazer.Core/Settings/GlobalSettings.cs`:
   - `public bool NotificationsEnabled { get; set; } = true;` (Bật/tắt toàn bộ toast thông báo).
   - `public bool NotificationSoundEnabled { get; set; } = true;` (Bật/tắt âm thanh chuông thông báo).
   - `public bool NotificationDndEnabled { get; set; } = false;` (Chế độ Không làm phiền: không hiện toast popup, vẫn ghi vào History).
   - `public bool NotificationNotifyErrorsOnly { get; set; } = false;` (Chỉ bật toast đối với thông báo kiểu Error/Warning).
2. `Vazer.Core/Settings/SettingsService.cs:Normalize`:
   - Gán giá trị mặc định khi normalize dữ liệu đọc từ JSON.
3. `Vaz.WinUI/ViewModels/SettingsViewModel.cs`:
   - Khai báo các `[ObservableProperty]` tương ứng và binding vào `SettingsPage.xaml`.

---

## 5. BƯỚC 4 — Đề xuất kiến trúc Persistence

### 5.1. Tách biệt File lưu trữ History
Không lưu History thông báo chung vào `settings.json` (vì History thay đổi liên tục, kích thước biến động, không phải là cấu hình hệ thống).

- **Đường dẫn đề xuất**:
  `%APPDATA%\Vazer\notifications.json` (tương tự như log debug đang dùng tại `NotificationService.cs:83`).
- **Lý do**: Độc lập với thư mục instances/settings, tránh làm phình to `settings.json` và tách biệt hoàn toàn I/O thông báo khỏi I/O launcher configuration.

### 5.2. Schema JSON định dạng DTO (`NotificationRecordDto`)
Tạo một DTO riêng chuyên dụng cho persistence, loại bỏ hoàn toàn các trường callback và transient:

```json
{
  "version": 1,
  "unreadCount": 3,
  "items": [
    {
      "id": "c3b7a120-7f92-4f11-893a-867df3c94f02",
      "type": "Success",
      "priority": "Normal",
      "title": "Cài đặt hoàn tất",
      "message": "Đã cài đặt thành công Sodium 0.6.0",
      "imageSource": null,
      "actionLabel1": "Mở thư mục",
      "actionLabel2": null,
      "groupCount": 1,
      "groupKey": null,
      "createdAt": "2026-11-15T08:30:00Z"
    }
  ]
}
```

### 5.3. Xử lý Callback khi Khôi phục từ File (Deserialization)
1. Khi load `notifications.json`:
   - Chuyển đổi từ `NotificationRecordDto` sang `NotificationItem`.
   - Các trường `ActionCallback1 = null`, `ActionCallback2 = null`.
   - Các trường transient: `ProgressValue = -1.0`, `IsSticky = false`, `AutoDismissMs = 5000`.
2. Trên UI `NotificationCenterPanel`:
   - Nếu `ActionCallback1 == null`, nút action trên item lịch sử cũ sẽ tự động ẩn hoặc disabled (giao diện thể hiện rõ đây là thông báo đã lưu trữ).

### 5.4. Cơ chế ghi đĩa chống nghẽn (Non-blocking & Atomic)
- **Vấn đề**: Hàm `Show()` có thể được kích hoạt liên tục khi tải danh sách hàng loạt mod hoặc cập nhật tiến trình. Ghi đĩa đồng bộ trong `_lock` sẽ làm đơ UI.
- **Giải pháp**:
  - Dùng cờ báo hiệu (debounced persistence / throttle) hoặc `System.Threading.Channels.Channel<bool>`.
  - Khi có thông báo mới hoặc xóa lịch sử, gửi tín hiệu vào channel hoặc kích hoạt bộ đếm thời gian debounce (ví dụ: sau 500ms không có thông báo mới mới thực hiện ghi đĩa một lần).
  - Sử dụng ghi nguyên tử tương tự như `SettingsService`:
    ```csharp
    var tmpPath = _filePath + ".tmp";
    await File.WriteAllTextAsync(tmpPath, json, ct);
    File.Move(tmpPath, _filePath, overwrite: true);
    ```

### 5.5. Cơ chế Di chuyển Schema (Migration Strategy)
- Trường `"version": 1` nằm ở root của file JSON.
- Khi đọc file:
  - Nếu không có trường version hoặc version < 1: thực hiện migrate hoặc nạp dữ liệu với giá trị fallback an toàn.
  - Sử dụng `try-catch` bọc ngoài toàn bộ quá trình đọc `notifications.json`. Nếu file hỏng, backup sang `.bak` và khởi tạo danh sách rỗng, không bao giờ làm crash launcher.

### 5.6. Phân định Cài đặt DND và Bộ lọc "Chỉ lỗi"
- **DND (Do Not Disturb)**:
  - Là **Global Setting** được lưu trong `GlobalSettings.cs`.
  - Hành vi khi DND bật: Vẫn thêm vào `History`, vẫn tăng `UnreadCount`, nhưng **không** kích hoạt popup toast và **không** phát âm thanh.
  - **Ngoại lệ Critical (đã chốt)**: `NotificationPriority.Critical` luôn hiện toast + phát âm thanh kể cả khi DND bật. Logic trong `NotificationService.cs`:
    ```csharp
    if (notification.Priority == NotificationPriority.Critical || !settings.NotificationDndEnabled)
        ShowToast(notification);
    ```
    Critical áp dụng cho: game crash, mất session Microsoft, lỗi file system. `Priority.Error` (thường) vẫn ẩn khi DND ON.
  - **Vị trí toggle DND (đã chốt)**: CẢ 2 chỗ:
    1. **TitleBar quick toggle**: icon chuông → dropdown mini (toggle DND + "Mở Notification Center"). Thay đổi ngay lập tức không cần mở Settings.
    2. **Settings page full toggle**: toggle DND đầy đủ + toggle per-category (Success/Error/Warning/Progress).
- **Bộ lọc "Chỉ lỗi" trong Notification Center**:
  - **UI View Filter** (trạng thái hiển thị tức thời trong phiên), không persist vào disk. Người dùng bấm tab/chip "Tất cả" hoặc "Chỉ lỗi" để lọc nhanh danh sách hiển thị trên `NotificationCenterPanel`.

---

## 6. BƯỚC 5 — Kế hoạch phân chia Sub-phases (N3.1, N3.2, N3.3)

Nhằm đảm bảo an toàn tuyệt đối và duy trì 0 regression, phân kỳ N3 thành 3 giai đoạn nhỏ độc lập:

### Phase N3.1: History Persistence (Ưu tiên cao nhất — 1 phiên)
- **Mục tiêu**: Lưu trữ và khôi phục `History` cùng `UnreadCount` qua file `%APPDATA%\Vazer\notifications.json`.
- **Tập tin thay đổi**:
  - `src/Vaz.WinUI/Models/NotificationRecordDto.cs` (Tạo mới DTO tinh gọn).
  - `src/Vaz.WinUI/Services/NotificationHistoryStore.cs` (Tạo mới service chuyên trách đọc/ghi JSON bất đồng bộ).
  - `src/Vaz.WinUI/Services/NotificationService.cs`: Tích hợp gọi store khi add/remove/clear history; nạp lịch sử lúc khởi động.
  - `src/Vaz.WinUI/Views/Controls/NotificationCenterPanel.xaml.cs`: Nút Action ẨN HOÀN TOÀN (không disabled) — `ActionCallback1 == null` → `HasAction1 = false` → Visibility collapse. KHÔNG tooltip "Không khả dụng", KHÔNG nút xám.
  - **TitleBar dropdown mini**: Tạo mới flyout "Mở Notification Center" + toggle DND nhanh (gọi vào `SettingsService` trực tiếp).
- **Rủi ro**: Deadlock luồng UI nếu ghi file trong lock; lỗi callback null khi bấm nút action trên item cũ.
- **Bộ kiểm thử cần thêm**: Unit tests cho `NotificationHistoryStoreTests` (kiểm tra serialization, eviction 50 items, xử lý file hỏng).

### Phase N3.2: Notification Settings & Sound Toggles (Ưu tiên cao — 0.5 phiên)
- **Mục tiêu**: Thêm các cài đặt bật/tắt thông báo và tách bạch âm thanh thông báo khỏi `UiSoundEnabled`.
- **Tập tin thay đổi**:
  - `src/Vazer.Core/Settings/GlobalSettings.cs`: Thêm `NotificationsEnabled`, `NotificationSoundEnabled`.
  - `src/Vazer.Core/Settings/SettingsService.cs`: Cập nhật logic normalize.
  - `src/Vaz.WinUI/ViewModels/SettingsViewModel.cs`: Thêm properties và binding logic.
  - `src/Vaz.WinUI/Views/SettingsPage.xaml`: Bổ sung các toggle trong nhóm cài đặt Giao diện/Thông báo.
  - `src/Vaz.WinUI/Services/NotificationService.cs`: Kiểm tra `NotificationSoundEnabled` và `notification.IsSoundEnabled` trước khi gọi `PlaySoundSafe`.
- **Rủi ro**: Lỗi deserialization `settings.json` cũ nếu thiếu Source Generator mapping. Cần kiểm tra kỹ `VazJsonContext`.
- **Bộ kiểm thử cần thêm**: Cập nhật `SettingsServiceTests` để xác nhận các trường mới có giá trị mặc định chính xác.

### Phase N3.3: DND Mode & Notification Center Error Filter (0.5 phiên)
- **Mục tiêu**: Hiện thực hóa chế độ Không làm phiền (DND) và bộ lọc thông báo lỗi trong panel.
- **Tập tin thay đổi**:
  - `src/Vazer.Core/Settings/GlobalSettings.cs`: Thêm `NotificationDndEnabled`.
  - `src/Vaz.WinUI/Services/NotificationService.cs`: Chặn hiện toast và âm thanh khi DND bật.
  - `src/Vaz.WinUI/Views/Controls/NotificationCenterPanel.xaml` & `.xaml.cs`: Bổ sung segmented buttons / filter chips ("Tất cả", "Chỉ lỗi") để lọc hiển thị trên `HistoryItemsControl`.
- **Rủi ro**: DND vô tình chặn Critical — đã chốt ngoại lệ: `Priority.Critical` phá DND. `Priority.Error` thường vẫn ẩn.
- **Bộ kiểm thử cần thêm**: Unit test mô phỏng gửi thông báo khi bật DND và verify danh sách active rỗng.

---

## 7. Quyết định đã chốt từ Master

| Câu hỏi | Quyết định |
|---|---|
| Vị trí toggle DND | CẢ 2: TitleBar dropdown mini (icon chuông → toggle DND + "Mở Notification Center") + Settings page full toggle (DND + per-category). |
| Critical phá DND? | CÓ. `Priority.Critical` luôn hiện toast + âm thanh dù DND ON. `Priority.Error` thường vẫn ẩn khi DND ON. |
| Nút Action sau restart | ẨN HOÀN TOÀN. `ActionCallback1 == null` → `HasAction1 = false` → Visibility.Collapsed. Không disabled, không tooltip. Logic đã có sẵn tại `NotificationCenterPanel.cs:139-169`. |
