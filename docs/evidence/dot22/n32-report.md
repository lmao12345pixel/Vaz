# BÁO CÁO NGHIỆM THU N3.2 — NOTIFICATION SETTINGS TOGGLES

**Ngày thực hiện**: 2026-11-15  
**Đợt**: N3.2 (Settings toggles — Phase P2 của Refactor N3)  
**Tác giả**: Master & AI Assistant  
**Tiền đề**: `f2e86e5` (N3.1 persist history), 511 test, 0 warning  
**Trạng thái**: HOÀN THÀNH — PASS 100% GATE  

---

## 1. GlobalSettings: 5 Field Mới
File: `src/Vazer.Core/Settings/GlobalSettings.cs:51-60`

```csharp
// Thông báo / Notifications
public bool NotificationSoundEnabled { get; set; } = true;   // Master toggle âm thông báo (tách khỏi UiSoundEnabled)

public bool NotificationShowSuccess { get; set; } = true;    // Toast Thành công

public bool NotificationShowError { get; set; } = true;      // Toast Lỗi

public bool NotificationShowWarning { get; set; } = true;    // Toast Cảnh báo

public bool NotificationShowProgress { get; set; } = true;   // Toast Tiến trình
```

- **Mặc định**: Tất cả đều là `true`, bảo toàn 100% behavior hiện tại.
- **Tách biệt**: `UiSoundEnabled` (âm click UI Minecraft) giữ nguyên chức năng ban đầu; `NotificationSoundEnabled` là master toggle độc lập cho âm thanh thông báo.
- **Serialization / Normalization**: `SettingsService.NormalizeSettings()` đã được cập nhật sao chép đầy đủ 5 field mới. `ISettingsService.Current` cung cấp access point đồng bộ cho các service tầng trên.
- **Migration**: Field mới có default `true`, settings.json cũ thiếu field parser C# tự lấy default `true` an toàn mà không cần migration script.

---

## 2. SettingsViewModel: 5 Property Bind & Sync Load/Save
File: `src/Vaz.WinUI/ViewModels/SettingsViewModel.cs`

### 2.1. Khai báo ObservableProperty (lines 160-164)
```csharp
[ObservableProperty] private bool notificationSoundEnabled = true;
[ObservableProperty] private bool notificationShowSuccess = true;
[ObservableProperty] private bool notificationShowError = true;
[ObservableProperty] private bool notificationShowWarning = true;
[ObservableProperty] private bool notificationShowProgress = true;
```

### 2.2. Sync LoadSettings (lines 630-634)
```csharp
NotificationSoundEnabled = settings.NotificationSoundEnabled;
NotificationShowSuccess = settings.NotificationShowSuccess;
NotificationShowError = settings.NotificationShowError;
NotificationShowWarning = settings.NotificationShowWarning;
NotificationShowProgress = settings.NotificationShowProgress;
```

### 2.3. Sync SaveSettings & Export (lines 695-699 & lines 808-812)
Ghi đè giá trị vào `GlobalSettings` trước khi gọi `_settingsService.SaveAsync()` (atomic write).

---

## 3. SettingsPage.xaml: Section "Thông báo"
File: `src/Vaz.WinUI/Views/SettingsPage.xaml:451-478`

- **Vị trí**: Đặt trong Card "Giao diện & Trải nghiệm", ngay bên dưới toggle `UiSoundEnabled` ("Phát âm thanh click xúc giác Minecraft khi tương tác").
- **Cấu trúc UI**:
  - Divider phân cách `<Border Height="1" Background="{ThemeResource CardStrokeColorDefaultBrush}" Margin="0,4,0,4" />`.
  - Tiêu đề nhóm "Thông báo" + subtitle giải thích.
  - Master toggle switch "Âm thanh thông báo" (`NotificationSoundEnabled`).
  - Phụ đề "Loại thông báo hiển thị:" kèm 4 ToggleSwitch:
    - "Thành công" (`NotificationShowSuccess`)
    - "Lỗi" (`NotificationShowError`)
    - "Cảnh báo" (`NotificationShowWarning`)
    - "Tiến trình" (`NotificationShowProgress`)
- **Theme & Style**: Sử dụng đồng bộ `StaticResource SettingsSectionTitleStyle`, `CaptionTextBlockStyle`, `MkVibeMutedTextBrush`, padding và margin đồng bộ với toàn trang Settings.

---

## 4. NotificationService: Logic Skip Toast vs History & Sound Check
File: `src/Vaz.WinUI/Services/NotificationService.cs`

### 4.1. DI Injection & Settings Sync
- `NotificationService(NotificationHistoryStore? store = null, ISettingsService? settingsService = null)` nhận `ISettingsService` qua DI.
- Getter `Settings => _settingsService?.Current ?? new GlobalSettings()` lấy config tức thì không block luồng.

### 4.2. Logic Skip Toast vs History (lines 104-142)
```csharp
bool showToast = ShouldShowToast(notification.Type, notification.Priority);

if (showToast)
{
    // Thêm vào ActiveNotifications và kích hoạt Toast Popup trên màn hình
    ActiveNotifications.Add(notification);
    NotificationAdded?.Invoke(this, notification);
}

// Thêm vào History: LUÔN LƯU VÀO HISTORY để an toàn, không mất data
History.Insert(0, notification);
UnreadCount++;
PersistHistoryLocked();
```
- **Quyết định đã chốt**: Khi user tắt một category (ví dụ Success), toast trên màn hình bị skip, nhưng thông báo **VẪN ĐƯỢC LƯU VÀO HISTORY** để user có thể kiểm tra lại trong Trung tâm thông báo khi cần.
- **Critical Exception**: `Priority == NotificationPriority.Critical` LUÔN hiện toast bất kể category toggle.

### 4.3. Sound Gate Logic (`PlaySoundSafe`, lines 559-570)
```csharp
if (notification.Type == NotificationType.Progress) return;

if (notification.Priority != NotificationPriority.Critical)
{
    if (!Settings.NotificationSoundEnabled) return;               // Tắt master toggle âm
    if (!notification.IsSoundEnabled) return;                    // Tắt âm riêng của item
    if (!ShouldShowToast(notification.Type, notification.Priority)) return; // Không phát âm nếu toast bị ẩn
}
```
- Nếu `NotificationSoundEnabled == false`: không phát âm thanh.
- Nếu loại toast bị tắt (ví dụ Success bị tắt): toast không hiện và **không phát âm thanh ma** (phantom sound).
- `Critical`: Luôn phát âm bất kể cấu hình.

---

## 5. Test List & Kết Quả
Đã thêm 3 unit tests mới trong `tests/Vazer.Core.Tests/SettingsServiceTests.cs`:
1. `Load_MissingNotificationFields_DefaultsAllToTrue`: Xác thực file json cũ không có field mới -> tất cả 5 field tự nhận giá trị default `true`.
2. `Save_CustomNotificationSettings_PersistsAndRoundTrips`: Lưu tùy chọn custom (tắt Success/Warning/Progress/Sound, giữ Error) -> đọc lại roundtrip bảo toàn 100%.
3. `Current_UpdatesSynchronously_AfterLoadAndSave`: Xác thực `ISettingsService.Current` cập nhật đồng bộ sau khi Load/Save để `NotificationService` đọc dữ liệu thời gian thực.

**Kết quả chạy**:
```
dotnet test tests/Vazer.Core.Tests/Vazer.Core.Tests.csproj
Passed!  - Failed: 0, Passed: 514, Skipped: 0, Total: 514, Duration: 4 s
```
(Tăng từ 511 -> 514 test, +3 test, 0 fail).

---

## 6. Build & Evidence Verification
### 6.1. Build Output
- `dotnet build -c Debug`: **0 Warning, 0 Error**
- `dotnet build -c Release`: **0 Warning, 0 Error**

### 6.2. Screenshot Evidence
- Đường dẫn: `docs/evidence/dot22/n32-settings-notification.png`
- File size: 57,312 bytes
- Mô tả ảnh: Giao diện Settings > tab "Giao diện", hiển thị rõ section "Thông báo" với toggle "Âm thanh thông báo" và 4 toggle loại thông báo ("Thành công", "Lỗi", "Cảnh báo", "Tiến trình").

---

## 7. Tổng Hợp Gate
- [x] Build Debug + Release: 0 warning, 0 error.
- [x] Test: 514 passed (+3 test), 0 failed.
- [x] Screenshot `n32-settings-notification.png` tồn tại và sắc nét.
- [x] Tách `NotificationSoundEnabled` khỏi `UiSoundEnabled`.
- [x] Skip toast nhưng vẫn lưu History (không mất data).
- [x] KHÔNG tag, KHÔNG push.
