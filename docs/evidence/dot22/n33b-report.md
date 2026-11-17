# N3.3b — Notification Center Không Mở Được

## Bước 1 — Triệu Chứng

**Triệu chứng: (A) Panel KHÔNG mở — click bell icon không mở Notification Center**

### Evidence UIA
- UIA tree sau khi click bell: **KHÔNG có** `NotificationCenterOverlay` visible.
- Bell button bounds: `(1068, 184)` trong TitleBar.
- Không có crash (process vẫn `Responding`).
- `crash.txt`: không có exception.

### Root Cause
**File: `MainWindow.xaml` (trước fix)**
- `AppTitleBar` Grid chiếm **toàn bộ** TitleBar bao gồm cả vùng chứa `NotificationBellButton` và `DndToggleButton`.
- `SetTitleBar(AppTitleBar)` khiến **toàn bộ TitleBar trở thành non-client drag region**.
- Khi user click bell, Win32 `WM_NCHITTEST` trả về `HTCAPTION` → click không đến được button → **panel không mở**.

**Triệu chứng thuộc loại (A): Panel KHÔNG mở** — bell click bị swallow bởi drag region.

## Bước 2 — Code Đọc

### MainWindow.xaml (trước fix)
```xml
<Grid x:Name="AppTitleBar" Grid.Row="0" Height="44" ... Padding="16,0,140,0">
    ...
    <StackPanel Grid.Column="1" Orientation="Horizontal">
        <!-- DndToggleButton + NotificationBellButton -->
    </StackPanel>
</Grid>
```
→ Toàn bộ `AppTitleBar` là drag region, bao gồm cả action buttons.

### MainWindow.xaml.cs
```csharp
ExtendsContentIntoTitleBar = true;
SetTitleBar(AppTitleBar);
```
→ `AppTitleBar` là cả drag area lẫn visual container cho buttons.

## Bước 3 — Fix

### MainWindow.xaml (sau fix)
```xml
<Grid Grid.Row="0" Height="44" Background="Transparent" Padding="16,0,140,0" VerticalAlignment="Center">
    <Grid.ColumnDefinitions>
        <ColumnDefinition Width="Auto" />
        <ColumnDefinition Width="*" />
        <ColumnDefinition Width="Auto" />
    </Grid.ColumnDefinitions>

    <!-- Draggable TitleBar Area: covers Branding + Center spacer, ends BEFORE Action Buttons -->
    <Grid x:Name="AppTitleBar" Grid.Column="0" Grid.ColumnSpan="2" Background="Transparent" />

    <!-- App Logo & Branding (Visual overlay, IsHitTestVisible=False) -->
    <StackPanel Grid.Column="0" Orientation="Horizontal" ... IsHitTestVisible="False">
        ...
    </StackPanel>

    <!-- Action Buttons (Pure Client Area, Outside AppTitleBar) -->
    <StackPanel Grid.Column="2" Orientation="Horizontal">
        <ToggleButton x:Name="DndToggleButton" ... />
        <Button x:Name="NotificationBellButton" ... />
    </StackPanel>
</Grid>
```

### MainWindow.xaml.cs (thêm passthrough regions)
```csharp
ExtendsContentIntoTitleBar = true;
SetTitleBar(AppTitleBar);

// Thêm loaded/sizechanged handlers
AppTitleBar.Loaded += (_, _) => UpdateTitleBarPassthroughRegions();
AppTitleBar.SizeChanged += (_, _) => UpdateTitleBarPassthroughRegions();
DndToggleButton.Loaded += (_, _) => UpdateTitleBarPassthroughRegions();
DndToggleButton.SizeChanged += (_, _) => UpdateTitleBarPassthroughRegions();
NotificationBellButton.Loaded += (_, _) => UpdateTitleBarPassthroughRegions();
NotificationBellButton.SizeChanged += (_, _) => UpdateTitleBarPassthroughRegions();

// Method mới
private void UpdateTitleBarPassthroughRegions()
{
    try
    {
        if (AppWindowTitleBar.IsCustomizationSupported() && AppTitleBar.XamlRoot != null)
        {
            double scale = AppTitleBar.XamlRoot.RasterizationScale;
            var nonClientInputSrc = InputNonClientPointerSource.GetForWindowId(this.AppWindow.Id);
            var rects = new List<RectInt32>();
            foreach (var element in new FrameworkElement[] { DndToggleButton, NotificationBellButton })
            {
                if (element.ActualWidth > 0 && element.ActualHeight > 0)
                {
                    var transform = element.TransformToVisual(null);
                    var bounds = transform.TransformBounds(new Rect(0, 0, element.ActualWidth, element.ActualHeight));
                    rects.Add(new RectInt32(
                        (int)Math.Round(bounds.X * scale),
                        (int)Math.Round(bounds.Y * scale),
                        (int)Math.Round(bounds.Width * scale),
                        (int)Math.Round(bounds.Height * scale)
                    ));
                }
            }
            if (rects.Count > 0)
            {
                nonClientInputSrc.SetRegionRects(NonClientRegionKind.Passthrough, rects.ToArray());
            }
        }
    }
    catch { /* defensive */ }
}
```

## Bước 4 — Verify

### Build
```
Debug: Build succeeded. 0 Warning(s).
Release: Build succeeded. 0 Warning(s).
```

### Test
```
dotnet test: 518 passed, 0 failed.
```

### UIA + Pixel
- Click bell (UIA InvokePattern) → Panel mở → header "Thông báo" xuất hiện.
- Filter chips [Tất cả] / [Chỉ lỗi] hoạt động.
- Physical mouse click bell → panel mở (xác nhận qua screenshot).
- Screenshot: `docs/evidence/dot22/n33b-notification-center-fixed.png` (235551 bytes).

### Restart Verify
- Kill + relaunch app → physical click bell → **Panel mở bình thường** (header "Thông báo" found).
- Click bell lần 2 → panel đóng (hoặc đóng qua close button).

### Filter Verify
- `_filteredItems` là `ObservableCollection<NotificationItem>` bind đúng `ItemsSource`.
- Filter logic hoạt động: [Tất cả] hiện tất cả items, [Chỉ lỗi] lọc theo `IsError`.

## Bước 5 — Report + Commit

### Commit
```
fix(notif): repair notification center open/bind after N3.3 filter changes
```

### Files Changed
- `src/Vaz.WinUI/MainWindow.xaml` — separate AppTitleBar from action buttons
- `src/Vaz.WinUI/MainWindow.xaml.cs` — add UpdateTitleBarPassthroughRegions

### Screenshot
- `docs/evidence/dot22/n33b-notification-center-fixed.png` — Panel mở với notifications visible

### Gate Status
- Build Debug + Release: 0 warning ✅
- Test: 518/518 ✅
- Triệu chứng xác định: (A) Panel KHÔNG mở ✅
- Root cause: `MainWindow.xaml` — AppTitleBar bao trùm action buttons ✅
- Fix verified: panel mở + list render + filter chips work ✅
- Screenshot tồn tại ✅
- Restart verify ✅
- KHÔNG tag, KHÔNG push ✅
