# Báo cáo N3.2b — Fix Notification Toggle Persist

**Ngày:** 2026-11-16  
**Đợt:** N3.2b (Fix bug N3.2 — toggle không persist)  
**Tác giả:** AI Assistant (dưới chỉ đạo của Master)  
**Tiền đề:** HEAD 11a436d (N3.2), 514 test, 0 warning  
**Kết quả:** 516 test (+2 test mới), 0 warning Debug/Release, manual verify thành công với screenshot bằng chứng  

---

## 1. Root Cause Analysis
### Triệu chứng
Master test manual: tắt toggle "Thành công" (hoặc bất kỳ toggle notification nào) -> thoát Settings -> mở lại Settings -> toggle tự động quay về ON. File `%APPDATA%\Vazer\Minecraft\settings.json` có mtime từ ngày 11/08/2026 (trước N3.2 ~3 ngày), hoàn toàn không chứa các field `Notification*`.

### Nguyên nhân kỹ thuật
1. `src/Vaz.WinUI/ViewModels/SettingsViewModel.cs`:
   - Các thuộc tính `NotificationSoundEnabled`, `NotificationShowSuccess`, `NotificationShowError`, `NotificationShowWarning`, `NotificationShowProgress` (và cả `UiSoundEnabled` trước đây) được sinh tự động bởi CommunityToolkit MVVM qua `[ObservableProperty]`.
   - Khi người dùng gạt toggle trên XAML (`IsOn="{x:Bind ViewModel.NotificationShowSuccess, Mode=TwoWay}"`), property setter được gọi và thay đổi state trong ViewModel.
   - **Tuy nhiên**, không có phương thức partial hook `On...Changed(bool value)` nào được định nghĩa cho các property này.
   - Trước đây trong `SettingsViewModel.cs`, chỉ có `SelectedTheme`, `SelectedLanguage`, `SelectedFont`, `IsReduceMotion` là có `On...Changed` hook để trigger `AppThemeManager` hoặc `AutoSaveAsync`. Các toggle âm thanh và thông báo hoàn toàn không trigger lưu xuống file.
   - Do đó, `_settings.SaveAsync(...)` không bao giờ được gọi khi gạt toggle.

2. Khi mở lại Settings Page:
   - `SettingsViewModel.Apply(GlobalSettings model)` tải lại dữ liệu từ `_settings.Current`.
   - Vì file không được cập nhật, dữ liệu nạp lên nhận default C# của `GlobalSettings` (`true` cho tất cả các toggle thông báo).
   - Toggle tự động hiển thị lại là ON.

---

## 2. Giải pháp thực hiện

### 2.1. Thêm OnChanged hooks & AutoSaveAsync trong SettingsViewModel
File: `src/Vaz.WinUI/ViewModels/SettingsViewModel.cs` (lines 927-978)
Thêm các hook:
```csharp
partial void OnUiSoundEnabledChanged(bool value)
{
    if (_isApplying) return;
    _ = AutoSaveAsync();
}

partial void OnNotificationSoundEnabledChanged(bool value)
{
    if (_isApplying) return;
    _ = AutoSaveAsync();
}

partial void OnNotificationShowSuccessChanged(bool value)
{
    if (_isApplying) return;
    _ = AutoSaveAsync();
}

partial void OnNotificationShowErrorChanged(bool value)
{
    if (_isApplying) return;
    _ = AutoSaveAsync();
}

partial void OnNotificationShowWarningChanged(bool value)
{
    if (_isApplying) return;
    _ = AutoSaveAsync();
}

partial void OnNotificationShowProgressChanged(bool value)
{
    if (_isApplying) return;
    _ = AutoSaveAsync();
}

/// <summary>
/// N3.2b: Tự động lưu ngầm khi toggle cài đặt thay đổi, đảm bảo persist ra settings.json ngay lập tức.
/// </summary>
private async Task AutoSaveAsync()
{
    try
    {
        var model = ToModel();
        await _settings.SaveAsync(model);
    }
    catch (Exception ex)
    {
        HasError = true;
        ErrorMessage = ex.Message;
    }
}
```

- Bảo vệ chống loop khi nạp settings: Khi `Apply(GlobalSettings model)` chạy, cờ `_isApplying = true` được bật (và reset trong khối `finally`), do đó các `On...Changed` hook sẽ bỏ qua không trigger `AutoSaveAsync` thừa thãi.
- Khi người dùng tương tác trên UI: `_isApplying` là `false`, `AutoSaveAsync()` được gọi ngay lập tức, chuyển đổi model qua `ToModel()` (đã có đủ 5 field từ N3.2) và ghi file nguyên tử thông qua `_settings.SaveAsync(model)`.

### 2.2. Kiểm tra ToModel() và Apply()
- `ToModel()` (line 695-700): Đã ánh xạ chính xác 5 property sang `GlobalSettings`:
  ```csharp
  NotificationSoundEnabled = NotificationSoundEnabled,
  NotificationShowSuccess = NotificationShowSuccess,
  NotificationShowError = NotificationShowError,
  NotificationShowWarning = NotificationShowWarning,
  NotificationShowProgress = NotificationShowProgress,
  ```
- `Apply(GlobalSettings model)` (line 628-634): Đã nạp chính xác 5 trường từ `model`:
  ```csharp
  NotificationSoundEnabled = model.NotificationSoundEnabled;
  NotificationShowSuccess = model.NotificationShowSuccess;
  NotificationShowError = model.NotificationShowError;
  NotificationShowWarning = model.NotificationShowWarning;
  NotificationShowProgress = model.NotificationShowProgress;
  ```

---

## 3. Migration & Fallback file cũ
- File cũ (không chứa 5 trường `Notification*`): `System.Text.Json` deserializer sử dụng giá trị khởi tạo mặc định trong class `GlobalSettings` (`= true`), đảm bảo backward compatibility an toàn.
- Khi người dùng chuyển toggle sang OFF và lưu: File `settings.json` được serialize ghi đè với 5 trường mới, trường bị tắt lưu `false`.
- Các lần mở sau: `System.Text.Json` đọc chính xác giá trị `false` đã lưu.

---

## 4. Kiểm thử tự động (Automated Tests)
Thêm 2 test mới trong `tests/Vazer.Core.Tests/SettingsServiceTests.cs`:
1. `Test_SaveLoadNotificationToggles_RoundTrip`:
   - Khởi tạo `GlobalSettings` với cả 5 toggle = `false`.
   - Ghi đĩa qua `SaveAsync` và nạp lại qua `LoadAsync`.
   - Khẳng định cả 5 field đều đọc ra là `false`.
2. `Migration_OldSettingsFile_ToggleOffAndSave_PersistsFalse`:
   - Ghi file JSON cũ giả lập (không có 5 field notification).
   - `LoadAsync()` lần đầu: `NotificationShowSuccess` = `true` (fallback default an toàn).
   - Đặt `NotificationShowSuccess = false` và gọi `SaveAsync()`.
   - `LoadAsync()` lần hai từ đĩa: xác nhận `NotificationShowSuccess` = `false`, các trường khác giữ nguyên.

**Kết quả chạy test:**
- Tổng số test: 516 passed (trước đó là 514, tăng 2 test).
- Failed: 0, Skipped: 0, Warnings: 0.

---

## 5. Xác minh thực tế (Manual Verification)
1. **Khởi động ứng dụng Vazer Launcher thực tế:**
   - Ứng dụng WinUI 3 khởi chạy thành công.
   - Điều hướng vào tab **Cài đặt** -> cuộn xuống mục **Thông báo**.
   - Gạt tắt toggle **Thành công** (`NotificationShowSuccess = false`).
2. **Kiểm tra file `settings.json`:**
   - Đường dẫn: `%APPDATA%\Vazer\Minecraft\settings.json`.
   - Thời gian sửa đổi (`LastWriteTime`): Cập nhật ngay lập tức hôm nay (`2026-11-16`).
   - Nội dung JSON đọc trực tiếp bằng PowerShell:
     - `NotificationSoundEnabled : True`
     - `NotificationShowSuccess : False`
     - `NotificationShowError : True`
     - `NotificationShowWarning : True`
     - `NotificationShowProgress : True`
3. **Round-trip verify:**
   - Đóng ứng dụng hoàn toàn.
   - Mở lại Vazer Launcher -> vào Cài đặt -> mục Thông báo.
   - Toggle **Thành công** vẫn giữ nguyên trạng thái **OFF**.
   - Bật lại ON -> file cập nhật `true`. Tắt lại OFF -> file cập nhật `false`.

---

## 6. Bằng chứng hình ảnh (Evidence Screenshots)
- Ảnh 1: UI Cài đặt với toggle "Thành công" đã tắt:
  `docs/evidence/dot22/n32b-settings-toggle-off.png`
- Ảnh 2: Màn hình PowerShell xác thực file `settings.json` đã lưu `NotificationShowSuccess: false` với mtime mới:
  `docs/evidence/dot22/n32b-powershell-evidence.png`

---

## 7. Xác nhận tuân thủ quy tắc
- [x] Không đổi UI/XAML `SettingsPage.xaml`.
- [x] Không sửa `NotificationService.cs`.
- [x] Không tag, không push git.
- [x] Build Debug + Release: 0 warning.
- [x] Tests: 516/516 passed.
