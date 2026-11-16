# Báo cáo N3.3 — DND Mode, Error Filter & Critical Bypass

**Ngày:** 2026-11-16  
**Đợt:** N3.3 (Phase cuối N3 — DND + Filter + Critical Exception)  
**Tác giả:** AI Assistant (dưới chỉ đạo của Master)  
**Tiền đề:** HEAD 113fc3f (N3.2b), 516 test, 0 warning  
**Kết quả:** 518 test (+2 test mới), 0 warning Debug/Release, manual verify thành công với 3 ảnh screenshot bằng chứng  

---

## 1. Mục tiêu và phạm vi

Hoàn tất các tính năng thông báo nâng cao của N3:
1. **DND (Do Not Disturb) Quick Toggle trên TitleBar:** Icon mặt trăng (Moon glyph `&#xE708;`) nằm bên trái chuông thông báo. Click 1 chạm để bật/tắt DND. Màu icon chuyển đổi: Xám (`#9CA3AF`) khi OFF, Vàng hổ phách (`#F59E0B`) khi ON.
2. **Cài đặt DND trong Settings:** Toggle "Không làm phiền (DND)" nằm trên cùng nhóm Thông báo trong `SettingsPage.xaml`, có subtitle giải thích "Ẩn toast thông báo thường. Lỗi nghiêm trọng vẫn hiển thị." Đồng bộ 2 chiều với TitleBar button.
3. **Logic DND & Critical Exception:**
   - Khi DND = ON: Ẩn toàn bộ popup toast và âm thanh của thông báo thường (Success, Error, Warning, Progress, Info).
   - Ngoại lệ nghiêm trọng (`Priority == Critical`): Luôn hiển thị toast và phát âm thanh khẩn cấp bất kể DND hay Sound toggle.
   - Không mất dữ liệu: Dù toast bị ẩn, thông báo vẫn được thêm vào `History` và tăng `UnreadCount`.
4. **Bộ lọc trong Notification Center:** 2 chip [Tất cả] và [Chỉ lỗi]. Khi chọn [Chỉ lỗi], chỉ hiển thị `Type == Error` hoặc `Type == Warning`. Chip hoạt động session-only, không ảnh hưởng đến số đếm unread badge.

---

## 2. Chi tiết triển khai

### 2.1. Cấu hình & Persist (GlobalSettings & SettingsService)
- `src/Vazer.Core/Settings/GlobalSettings.cs` (line 52):
  ```csharp
  public bool NotificationDndEnabled { get; set; } = false;
  ```
- `src/Vazer.Core/Settings/ISettingsService.cs`:
  - Thêm event `event Action<GlobalSettings>? SettingsChanged;` thông báo khi cài đặt thay đổi sau khi nạp hoặc lưu.
- `src/Vazer.Core/Settings/SettingsService.cs`:
  - Bổ sung `NotificationDndEnabled = s.NotificationDndEnabled` trong phương thức `Normalize()` để đảm bảo không bị mất giá trị khi sanitize.
  - Kích hoạt `SettingsChanged?.Invoke(normalized)` sau `LoadAsync` và `SaveAsync`.

### 2.2. SettingsViewModel & SettingsPage
- `src/Vaz.WinUI/ViewModels/SettingsViewModel.cs`:
  - Line 160: `[ObservableProperty] private bool notificationDndEnabled;`
  - Line 631: `NotificationDndEnabled = settings.NotificationDndEnabled;` trong `Apply()`
  - Line 697: `NotificationDndEnabled = NotificationDndEnabled,` trong `ToModel()`
  - Lines 937-941:
    ```csharp
    partial void OnNotificationDndEnabledChanged(bool value)
    {
        if (_isApplying) return;
        _ = AutoSaveAsync();
    }
    ```
- `src/Vaz.WinUI/Views/SettingsPage.xaml` (lines 459-464):
  - Đặt ngay trên cùng section "Thông báo" (trước "Âm thanh thông báo"):
    ```xml
    <StackPanel Spacing="2">
        <ToggleSwitch
            Header="Không làm phiền (DND)"
            IsOn="{x:Bind ViewModel.NotificationDndEnabled, Mode=TwoWay}" />
        <TextBlock Text="Ẩn toast thông báo thường. Lỗi nghiêm trọng vẫn hiển thị." Style="{StaticResource CaptionTextBlockStyle}" Foreground="{ThemeResource MkVibeMutedTextBrush}" TextWrapping="Wrap" Margin="0,-4,0,0" />
    </StackPanel>
    ```

### 2.3. TitleBar Moon Button & Đồng bộ 2 chiều
- `src/Vaz.WinUI/MainWindow.xaml` (lines 59-70):
  - Thêm button `DndToggleButton` ngay bên trái `NotificationBellButton`:
    ```xml
    <Button
        x:Name="DndToggleButton"
        Click="DndToggleButton_Click"
        Padding="6"
        Background="Transparent"
        BorderThickness="0"
        CornerRadius="6"
        Margin="0,0,4,0"
        AutomationProperties.Name="Không làm phiền"
        ToolTipService.ToolTip="Không làm phiền (DND)">
        <FontIcon x:Name="DndIcon" Glyph="&#xE708;" FontSize="15" />
    </Button>
    ```
- `src/Vaz.WinUI/MainWindow.xaml.cs`:
  - Line 83-84: Định nghĩa 2 cọ màu:
    - `DndActiveBrush`: `#F59E0B` (Amber / Vàng)
    - `DndInactiveBrush`: `#9CA3AF` (Muted Gray / Xám)
  - Khởi tạo & subscribe:
    - Khi khởi tạo: đọc trạng thái ban đầu và tô màu tương ứng.
    - Đăng ký `_settingsService.SettingsChanged += OnSettingsChanged;` để nhận cập nhật tự động khi người dùng gạt toggle trong SettingsPage.
  - `DndToggleButton_Click`:
    - Đảo trạng thái: `newValue = !_settingsService.Current.NotificationDndEnabled`.
    - Cập nhật UI ngay lập tức (`UpdateDndVisual`).
    - Lưu bất đồng bộ xuống file thông qua `_settingsService.SaveAsync`.

### 2.4. Logic NotificationService & Critical Bypass
- `src/Vaz.WinUI/Services/NotificationService.cs`:
  - Phương thức `ShouldShowToast(NotificationType type, NotificationPriority priority)` (lines 55-77):
    ```csharp
    // Critical LUÔN hiện toast bất kể cài đặt DND hay loại thông báo
    if (priority == NotificationPriority.Critical)
    {
        return true;
    }

    var s = Settings;
    if (s.NotificationDndEnabled)
    {
        return false;
    }

    return type switch
    {
        NotificationType.Success => s.NotificationShowSuccess,
        NotificationType.Error => s.NotificationShowError,
        NotificationType.Warning => s.NotificationShowWarning,
        NotificationType.Progress => s.NotificationShowProgress,
        _ => true
    };
    ```
  - `PlaySoundSafe(NotificationItem notification)` (lines 564-575):
    - Nếu `priority == NotificationPriority.Critical`, luôn phát âm thanh khẩn cấp.
    - Với các thông báo thường: chỉ phát âm nếu master sound bật, item sound bật và `ShouldShowToast` trả về `true` (không bị DND chặn).
  - `Show(NotificationItem notification)`:
    - Giữ nguyên việc chèn vào `_history.Insert(0, item)` và tăng `UnreadCount` ngay cả khi `ShouldShowToast` trả về `false`.

### 2.5. Filter Chips trong NotificationCenterPanel
- `src/Vaz.WinUI/Views/Controls/NotificationCenterPanel.xaml` (lines 72-94):
  - Thêm 2 toggle chip phía dưới tiêu đề:
    ```xml
    <StackPanel Grid.Row="1" Orientation="Horizontal" Spacing="8" Padding="16,10,16,6">
        <ToggleButton x:Name="FilterAll" Content="Tất cả" IsChecked="True" Click="FilterAll_Click" FontSize="12" Padding="12,4" CornerRadius="14" />
        <ToggleButton x:Name="FilterErrorsOnly" Content="Chỉ lỗi" IsChecked="False" Click="FilterErrorsOnly_Click" FontSize="12" Padding="12,4" CornerRadius="14" />
    </StackPanel>
    ```
- `src/Vaz.WinUI/Views/Controls/NotificationCenterPanel.xaml.cs`:
  - `_filteredItems`: `ObservableCollection<NotificationItem>` làm `ItemsSource` cho `HistoryItemsControl`.
  - `FilterAll_Click`: Chọn Tất cả, bỏ Chỉ lỗi, nạp toàn bộ `_service.History`.
  - `FilterErrorsOnly_Click`: Chọn Chỉ lỗi, bỏ Tất cả, lọc `_service.History.Where(n => n.Type == Error || n.Type == Warning)`.
  - Cập nhật `EmptyStatePanel` theo số lượng phần tử đã lọc.
  - Không thay đổi `_service.UnreadCount` (đếm tất cả tin chưa đọc).

---

## 3. Kiểm thử tự động (Automated Tests)

File: `tests/Vazer.Core.Tests/SettingsServiceTests.cs`
1. `Test_SaveLoadDnd_RoundTrip`:
   - Xác nhận mặc định `NotificationDndEnabled == false`.
   - Bật DND (`true`) -> `SaveAsync` -> `LoadAsync` -> kiểm tra đọc ra `true`.
   - Tắt DND (`false`) -> `SaveAsync` -> `LoadAsync` -> kiểm tra đọc ra `false`.
2. `SettingsService_Normalize_CopiesNotificationDndEnabled`:
   - Xác nhận qua sự kiện `SettingsChanged` và chuẩn hóa model rằng trường `NotificationDndEnabled` được sao chép bảo toàn đầy đủ.

**Kết quả chạy test:**
- Tổng số test: **518 passed** (516 + 2 tests mới).
- Failed: 0, Skipped: 0, Warnings: 0.

---

## 4. Xác minh thực tế (Manual Verification)

1. **TitleBar Quick Toggle:**
   - Click moon icon -> icon chuyển sang màu Vàng hổ phách (`#F59E0B`), tooltip hiển thị "Không làm phiền (Đang bật)".
   - Click lần 2 -> icon chuyển sang màu Xám (`#9CA3AF`), tooltip "Không làm phiền (DND)".
2. **Đồng bộ với Settings Page:**
   - Vào Settings -> mục Thông báo: Toggle "Không làm phiền (DND)" hiển thị đúng trạng thái.
   - Bật toggle trong Settings -> TitleBar icon chuyển vàng tức thì.
   - Tắt toggle trên TitleBar -> Settings page toggle tự động tắt.
3. **Chặn Toast khi DND bật:**
   - Khi DND ON: Thông báo Success/Error/Warning/Progress thông thường không bật popup toast và không phát âm thanh.
   - Kiểm tra trung tâm thông báo: Các thông báo vẫn được lưu vào Lịch sử và Unread badge vẫn tăng số lượng.
4. **Critical Exception:**
   - Thông báo có `Priority == Critical` luôn hiển thị toast ghim (`IsSticky = true`, `AutoDismissMs = 0`) và phát âm thanh cảnh báo, ngay cả khi DND đang bật và toggle âm thanh tắt.
5. **Bộ lọc Notification Center:**
   - Click chip [Chỉ lỗi] -> danh sách chỉ lọc các mục có loại Lỗi (Error) hoặc Cảnh báo (Warning).
   - Click chip [Tất cả] -> danh sách phục hồi hiển thị đầy đủ mọi thông báo.

---

## 5. Bằng chứng hình ảnh (Evidence Screenshots)

1. `docs/evidence/dot22/n33-titlebar-moon.png`:
   - Icon mặt trăng trên TitleBar cạnh chuông thông báo ở trạng thái kích hoạt (màu vàng).
2. `docs/evidence/dot22/n33-settings-dnd.png`:
   - Trang Cài đặt với mục "Không làm phiền (DND)" ở đầu phần Thông báo kèm subtitle hướng dẫn.
3. `docs/evidence/dot22/n33-filter-errors.png`:
   - Trung tâm thông báo mở ra với chip [Chỉ lỗi] được chọn, danh sách chỉ hiển thị thông báo Error/Warning.

---

## 6. Xác nhận Gate N3.3
- [x] Build Debug + Release: 0 error, 0 warning.
- [x] Tests: 518/518 passed.
- [x] Bằng chứng 3 ảnh screenshot đầy đủ tại `docs/evidence/dot22/`.
- [x] Không tag, không push git.
