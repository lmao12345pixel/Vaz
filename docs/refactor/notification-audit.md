# N0.1 — Audit Notification System (chỉ đọc, không sửa code)

Ngày: 2026-11-12 · Đợt: N0.1 · HEAD: `eb5ae2e` · Branch: `feat/browse-popular-mods`
Người thực hiện: Raez · Người giao: Master
Gate tại thời điểm audit: 502 test passed, 0 warning (đã chạy lại để xác nhận, không đổi).

Bug Master báo: toast bị "glass" — nền trong suốt, lộ nội dung đằng sau, đọc không rõ.

---

## BƯỚC 1 — Inventory hiện trạng

Hệ thống có **11 file source** (không tính `bin/`/`obj/`): 3 control XAML + 3 code-behind, 1 service + 1 interface, 2 model, 1 helper Core, 1 adapter Java.

### 1.1 Toast component

| Thuộc tính | Giá trị | File:line |
|---|---|---|
| Component | `NotificationToast : UserControl` | `Views/Controls/NotificationToast.xaml:1-7`, `.xaml.cs:19` |
| Width | 400 (cố định), `HorizontalAlignment=Right` | `NotificationToast.xaml:5-6` |
| Background | `{ThemeResource CardBackgroundFillColorDefaultBrush}` — **bán trong suốt** | `NotificationToast.xaml:18` |
| Border | `CardStrokeColorDefaultBrush`, 1px, `CornerRadius=8` | `NotificationToast.xaml:19-21` |
| Cấu trúc | thanh màu 4px trái + icon + ảnh 44×44 (tuỳ chọn) + Title/Message + 2 action + nút X + progress 2px | `NotificationToast.xaml:28-140` |
| Style nền | **SolidColorBrush bán trong suốt**, KHÔNG phải AcrylicBrush/Mica/Transparent trực tiếp | `NotificationToast.xaml:18` + `generic.xaml:2101` |
| Vị trí | góc dưới phải, `Margin="0,0,24,24"`, `Grid.RowSpan=3` (overlay trên mọi trang) | `MainWindow.xaml:255-261` |
| Host | `NotificationHost`, Grid 420 rộng, `Background={x:Null}`, StackPanel dọc `Spacing=8` | `NotificationHost.xaml:5-17` |
| Duration | Success/Warning/Info = 10000ms; Info Low = 5000ms; Error = 15000ms; Error Critical = 0 (sticky); Update = 15000ms; Progress = 0 (sticky) | `NotificationService.cs:105,128,152,175,200,240` |
| Animation | vào 350ms (slide + fade), ra 250ms, hover scale 200ms, nút X 150ms | `NotificationToast.xaml.cs:175,269,389,408` |

**Kết luận 1.1**: không phải "thiếu Background" — có Background, nhưng bản thân brush được chọn chỉ đục ~5% ở Dark.

### 1.2 Notification Center

| Thành phần | Có? | File:line |
|---|---|---|
| Panel trượt | CÓ, rộng 400, trượt từ phải 250ms vào / 200ms ra, backdrop `#33000000` | `NotificationCenterPanel.xaml:17-27`, `.xaml.cs:48-107` |
| Nền panel | `SolidBackgroundFillColorBaseBrush` — **đục** | `NotificationCenterPanel.xaml:22` |
| Header / Xoá tất cả / Đóng | CÓ | `NotificationCenterPanel.xaml:31-70`, `.xaml.cs:119-137` |
| Empty state | CÓ | `NotificationCenterPanel.xaml:80-105`, `.xaml.cs:41-46` |
| History | CÓ, `ObservableCollection<NotificationItem>`, tối đa 50, insert đầu (mới nhất trước) | `NotificationService.cs:16,21,80-85` |
| Persist history qua restart | **KHÔNG** — chỉ in-memory trong singleton; không có Save/Load/JSON nào cho History | `NotificationService.cs:21`, `App.xaml.cs:47` (AddSingleton, không lưu file) |
| Unread badge | CÓ, `UnreadCount` + event + `9+` | `NotificationService.cs:23-35,87`, `MainWindow.xaml:70-84`, `MainWindow.xaml.cs:131-138,252-261` |
| Mark all read | CÓ, gọi khi mở panel | `NotificationCenterPanel.xaml.cs:52`, `NotificationService.cs:360-371` |

**Kết luận 1.2**: Center đã có từ Đợt 16, khá hoàn chỉnh về UI; **history mất sạch khi restart app**.

### 1.3 Các loại notification đang phát (production, không tính DEMO)

| # | Sự kiện | Loại | File:line |
|---|---|---|---|
| 1 | Bắt đầu cài mod/RP/Shader (progress toast) | Progress | `ViewModels/ModBrowserViewModel.cs:891` |
| 2 | Cập nhật % tải | UpdateProgress | `ModBrowserViewModel.cs:910` |
| 3 | Cài thành công (mod/RP/Shader, kèm icon) | Success + CompleteProgress | `ModBrowserViewModel.cs:928,932` + `InstallNotificationHelper.cs:5-23` |
| 4 | Cài thất bại | Error + CompleteProgress | `ModBrowserViewModel.cs:946,948` |
| 5 | Cảnh báo kèm kết quả cài | Warning | `ModBrowserViewModel.cs:954` |
| 6 | Lỗi tải mod / retry | Error | `ModBrowserViewModel.cs:971` |
| 7 | Không kết nối được Modrinth (2 nơi) | Error + Retry | `ModBrowserViewModel.cs:588,763` |
| 8 | Chưa chọn Instance khi cài | Warning | `ModBrowserViewModel.cs:869` |
| 9 | Cài Performance Pack thất bại | Error + Retry | `ModBrowserViewModel.cs:1055` |
| 10 | Xoá RP thành công / lỗi | Success / Error | `InstanceDetailViewModel.ResourcePacksShaders.cs:425,410` |
| 11 | Xoá Shader thành công / lỗi | Success / Error | `InstanceDetailViewModel.ResourcePacksShaders.cs:708,693` |
| 12 | Gỡ mod thành công / 2 loại lỗi | Success / Error | `InstanceDetailViewModel.cs:762,768,774` |
| 13 | Cảnh báo tắt mod ảnh hưởng mod khác | Warning (inline) | `InstanceDetailViewModel.cs:334` |
| 14 | Có bản cập nhật mod | Update | `InstanceDetailViewModel.cs:1304` |
| 15 | Cập nhật mod xong / thất bại | Success / Error | `InstanceDetailViewModel.cs:1349,1356` |
| 16 | Chưa chọn Instance khi CHƠI | Warning | `MainViewModel.cs:304,545` |
| 17 | Cảnh báo Java/RAM khi khởi chạy | Warning | `MainViewModel.cs:337,357` |
| 18 | Tiến trình khởi chạy Minecraft | Progress 30% → 75% → Complete | `MainViewModel.cs:345,346,352,375` |
| 19 | Java version không phù hợp | Warning (qua adapter) | `JavaVersionMapper.cs:140` → `Services/JavaCompatibilityNotifier.cs:19-20` |
| 20 | **Minecraft crash** (exitCode≠0, chạy <5s, có nút "Xem log") | Error / Critical | `MainViewModel.cs:425-440` |
| 21 | Minecraft đóng bình thường (playtime) | Info | `MainViewModel.cs:450` |
| 22 | Khởi chạy thất bại (mạng / exception) | Error / Critical + Retry | `MainViewModel.cs:470,484-493` |
| 23 | Lỗi dừng tiến trình | Error | `MainViewModel.cs:533` |

**DEMO — KHÔNG phải production**: `MainWindow.xaml.cs:156-223` chỉ chạy khi có biến môi trường `VAZER_TEST_TOAST`. Các toast "RAM thấp", "Chưa chọn tài khoản", "Bản cập nhật VAZ 1.2.0", "3 bản cập nhật mod", "Minecraft đang chạy" ở đây là **giả lập**, callback phần lớn là `Task.CompletedTask` (no-op). Không được tính vào coverage thật.

**Các loại enum**: `Success, Warning, Error, Info, Update, Progress` (`Models/NotificationType.cs:3-11`); priority `Low, Normal, High, Critical` (`:13-19`).

### 1.4 Settings

| Tìm kiếm | Kết quả | File:line |
|---|---|---|
| Toggle bật/tắt toast | **KHÔNG CÓ** | `GlobalSettings.cs:20-70` (không có field notification nào) |
| DND / giờ yên lặng | **KHÔNG CÓ** | grep toàn `src` không ra |
| Filter "Chỉ lỗi" | **KHÔNG CÓ** | `NotificationCenterPanel.xaml` không có bộ lọc |
| Toggle âm thanh thông báo | **KHÔNG CÓ** — `UiSoundEnabled` là "âm thanh click xúc giác khi tương tác", không liên quan toast | `SettingsPage.xaml:447-449`, `GlobalSettings.cs:49` |
| `NotificationItem.IsSoundEnabled` | Khai báo `= true` nhưng **không nơi nào đọc** | `Models/NotificationItem.cs:208` |
| `PlaySoundSafe` | Luôn phát âm, bỏ qua mọi setting | `NotificationService.cs:411-453` |
| SettingsPage "Notification" | Là InfoBar nội bộ trang Settings, **KHÔNG** phải toast system | `SettingsViewModel.cs:194-195,743-746`, `SettingsPage.xaml:710-711` |
| Persist cài đặt thông báo | Không có gì để persist | `SettingsService.cs:204` (chỉ map các field hiện có) |

### 1.5 Queue / dedup

| Cơ chế | Có? | File:line |
|---|---|---|
| Giới hạn 5 toast | CÓ, 2 lớp: service cắt `ActiveNotifications`, host cắt `ToastStackPanel.Children` | `NotificationService.cs:15,68-74`; `NotificationHost.xaml.cs:91-104` |
| Chống spam / gom nhóm | CÓ, cửa sổ 3s, cùng `Type` + `GroupKey` (mặc định `Type:Title`), tăng `GroupCount` + badge "xN" | `NotificationService.cs:17,48-66`; `NotificationItem.cs:218-220`; `NotificationToast.xaml.cs:136-147` |
| Dedup map theo Id | CÓ (`_activeToastMap`), tránh thêm trùng | `NotificationHost.xaml.cs:17,86-89` |
| Dedup vĩnh viễn cho cùng lỗi | KHÔNG — quá 3s là toast mới | `NotificationService.cs:50-54` |
| Hàng đợi ưu tiên | KHÔNG — `Priority` chỉ dùng cho LiveSetting + sticky/duration | `NotificationService.cs:141-153`; `NotificationToast.xaml.cs:93-96` |
| Windows toast native khi minimize | **KHÔNG CÓ** — grep `AppNotification*`, `ToastNotification`, `Windows.UI.Notifications` trong source = 0 kết quả (chỉ có DLL projection trong `bin/`, không phải code gọi) | — |

---

## BƯỚC 2 — Bug background trong suốt

### 2.1 Background hiện tại

`NotificationToast.xaml:16-24` — Border `ToastCard` dùng `Background="{ThemeResource CardBackgroundFillColorDefaultBrush}"` (dòng 18). Không có `Opacity` riêng, không có Acrylic/Mica, không có lớp nền đệm nào phía sau (`NotificationHost.xaml:11` để `Background="{x:Null}"`).

### 2.2 Giá trị thực của brush (từ WindowsAppSDK 1.5.250108004, `Microsoft.WinUI/Themes/generic.xaml`)

| Theme | `CardBackgroundFillColorDefault` | Alpha | File:line SDK |
|---|---|---|---|
| `Default` (= Dark fallback) | `#0DFFFFFF` | `0x0D` = 13/255 ≈ **5.1% đục** | `generic.xaml:10` (bắt đầu Default), `:2016` (màu), `:2101` (brush) |
| `Light` | `#B3FFFFFF` | `0xB3` = 179/255 ≈ **70.2% đục** | `generic.xaml:5531` (bắt đầu Light), `:7537` (màu), `:7622` (brush) |
| HighContrast | `SystemColorWindowColor` (đục) | 100% | `generic.xaml:4794` |

App mặc định `Theme = "dark"` (`GlobalSettings.cs:43`), `AppThemeManager.cs:134-136` set `RequestedTheme` Light/Dark. Project **không override** `CardBackgroundFillColorDefaultBrush` ở bất kỳ chỗ nào (grep toàn `src/Vaz.WinUI/*.xaml` chỉ thấy nơi *dùng*, không thấy định nghĩa lại). App.xaml chỉ merge `XamlControlsResources` + `MinecraftTheme.xaml` + `MinecraftStyles.xaml` (`App.xaml:11-18`).

**Vậy toast Master thấy đang có nền `#0DFFFFFF` — 5% trắng đè lên nội dung bên dưới.**

### 2.3 So sánh với các card khác

| Component | Background | File:line | Tại sao không lộ |
|---|---|---|---|
| `NotificationCenterPanel` (panel) | `SolidBackgroundFillColorBaseBrush` = `#202020` Dark / `#F3F3F3` Light, **đục 100%** | `NotificationCenterPanel.xaml:22`; SDK `generic.xaml:204,5725,2112,7633` | Panel là lớp nền đục riêng |
| `ModrinthCardView` | **CÙNG** `CardBackgroundFillColorDefaultBrush` | `ModrinthCardView.xaml:73` | Card nằm trên page có nền `SolidBackgroundFillColorBaseBrush` (`ModBrowserView.xaml:72`) → 5% trắng đè lên nền đục vẫn đọc được |
| `InstancesPage` card | CÙNG brush | `InstancesPage.xaml:38,88` | Nằm trong shell có nền đục |
| `NotificationToast` | CÙNG brush | `NotificationToast.xaml:18` | **Là overlay `RowSpan=3` nổi trên MỌI nội dung, phía sau không có lớp đệm (`{x:Null}`)** → 5% trắng đè thẳng lên mod list / browser / inspector → lộ rõ |

Chính project đã ghi chú hiện tượng này ở chỗ khác: `App.xaml:58-62` — "NavigationView pane: mặc định WinUI dùng acrylic trong suốt → khi cửa sổ không có backdrop sẽ lộ nền đen ... Override bằng brush đặc". Toast chưa được xử lý tương tự.

Token `MkVibeSurfaceBrush` đã có sẵn và **đục 100%**: Light `#F5F0E6` (`Resources/MinecraftTheme.xaml:18`), Dark `#1A1D23` (`:53`), Default `#1A1D23` (`:88`).

### 2.4 Root cause

**Dùng nhầm brush bán trong suốt cho một lớp overlay không có backdrop.** Không phải thiếu Background, không phải `Opacity` thấp (không set), không phải Acrylic/Mica. `CardBackgroundFillColorDefaultBrush` được thiết kế cho card *nằm trên nền đục*; toast là lớp nổi nên cần brush đục tuyệt đối.

### 2.5 Giá trị hiện tại → đề xuất

| | Hiện tại | Đề xuất N1 |
|---|---|---|
| `NotificationToast.xaml:18` | `{ThemeResource CardBackgroundFillColorDefaultBrush}` = `#0DFFFFFF` (Dark) / `#B3FFFFFF` (Light) | `{ThemeResource SolidBackgroundFillColorBaseBrush}` = `#FF202020` (Dark) / `#FFF3F3F3` (Light) — đục 100%, **đã được chính CenterPanel dùng** (`NotificationCenterPanel.xaml:22`) nên đồng nhất, không cần thêm token |
| Phương án thay thế | — | `{ThemeResource MkVibeSurfaceBrush}` = `#FF1A1D23` / `#FFF5F0E6` — đục, mang màu nhận diện Minecraft Vibe; cần Master chọn (xem Câu hỏi mở) |
| `NotificationCenterPanel.xaml:112` (thẻ history) | CÙNG brush bán trong suốt | Nên đổi theo để đồng nhất; **mức độ thấp** vì thẻ nằm trên panel đục (`:22`) |

Không cần sửa `NotificationHost.xaml:11` (`{x:Null}`) nếu toast tự đục. Giữ nguyên `BorderBrush`/`CornerRadius`.

---

## BƯỚC 3 — Gap analysis

### 3.A UX / Visual

| Tiêu chí | Có | Thiếu / Rủi ro | File:line |
|---|---|---|---|
| Action buttons | CÓ 2 (`ActionButton1/2`, HyperlinkButton), card tap cũng gọi action1 | Callback production đa số thật (Retry/Search/Install), nhưng toast DEMO dùng no-op | `NotificationToast.xaml:90-105`, `.xaml.cs:342-379`; `ModBrowserViewModel.cs:588,763,971` |
| Nút đóng | CÓ, hiện khi hover | — | `NotificationToast.xaml:108-123`, `.xaml.cs:405-422` |
| Hover pause countdown | CÓ (pause/resume + scale 1.02) | — | `NotificationToast.xaml.cs:301-340` |
| Animation enter/exit | CÓ (350ms / 250ms) | **Không tôn trọng `ReduceMotion`** dù Settings có toggle; `RentStoryboard()` không đọc setting | `NotificationToast.xaml.cs:174,268`; `UiAnimationHelper.cs:1238`; `GlobalSettings.cs:41` |
| Dead code | — | `UiAnimationHelper.AnimateToastNotification` (`:752`) định nghĩa nhưng **không ai gọi** | `UiAnimationHelper.cs:752` |
| Toast stack chồng chéo | KHÔNG — StackPanel dọc, `Spacing=8`, `VerticalAlignment=Bottom`, có `Clip` | — | `NotificationHost.xaml:12-16`; `NotificationHost.xaml.cs:29-37` |
| Overlap taskbar | CÓ NGUY CƠ — margin cố định `24` tính từ đáy **cửa sổ**, không có logic dò vùng làm việc/taskbar. App chạy windowed (quan sát P0.6: cửa sổ 1100×700, có lúc y=56 → đáy 756 trong khi taskbar ≈ y=720) | Cần Master xác nhận bằng mắt | `MainWindow.xaml:255-261` |
| Accessibility | CÓ `AutomationLiveSetting` Assertive/Polite + tên nút đóng | — | `NotificationToast.xaml.cs:93-96`, `NotificationToast.xaml:121` |
| Progress → Success chuyển màu/icon | **THIẾU** — `OnNotificationPropertyChanged` chỉ xử lý `ProgressValue/Message/GroupCount/ImageSource`, không xử lý `Type`/`IconGlyph`/`TypeColorHex`/`AutoDismissMs`. Khi `CompleteProgress` đổi `Type` và `AutoDismissMs=5000`, toast **giữ nguyên màu/icon cũ và không khởi động lại countdown** | `NotificationToast.xaml.cs:108-134`; `NotificationService.cs:276-297` |

### 3.B Coverage sự kiện

| Sự kiện | Trạng thái | Bằng chứng / nơi nên phát |
|---|---|---|
| Cài mod / RP / Shader thành công | **CÓ** | `ModBrowserViewModel.cs:891,928,932`; `InstallNotificationHelper.cs:5-13` |
| Cài thất bại / lỗi tải / mất kết nối | **CÓ** | `ModBrowserViewModel.cs:588,763,948,971,1055` |
| Xoá RP / Shader / mod | **CÓ** | `InstanceDetailViewModel.ResourcePacksShaders.cs:410,425,693,708`; `InstanceDetailViewModel.cs:762-774` |
| Mod update available / cập nhật xong | **CÓ** | `InstanceDetailViewModel.cs:1304,1349,1356` |
| Java mismatch (18.7) | **CÓ** | `JavaVersionMapper.cs:140` → `JavaCompatibilityNotifier.cs:19-20` |
| Minecraft crash | **CÓ** | `MainViewModel.cs:425-440` |
| Download milestone | **CÓ một phần** — progress toast cập nhật liên tục % nhưng không có thông báo mốc (25/50/75%) riêng | `ModBrowserViewModel.cs:910`; `MainViewModel.cs:346,352` |
| **Instance created** | **THIẾU** — chỉ `CreateErrorBar` + `StatusMessage`, không gọi service | `CreateInstanceDialog.xaml.cs:293-345` (sau khi `instance != null`, và trong 2 nhánh catch/null) |
| **Instance deleted** | **THIẾU** — chỉ `Instances.Remove` + `StatusText` trên card, không gọi service; card bị gỡ khỏi list nên user cũng không thấy StatusText | `InstancesViewModel.cs:161-199` |
| **Version changed (18.5)** | **THIẾU** — chỉ set `StatusMessage` trong detail VM | `InstanceDetailViewModel.cs:1619-1623` |
| **Mod dependency missing** | **THIẾU** — chỉ `statusList.Add(... "Missing")` + `InspectorDepActionText`, không toast | `InstanceDetailViewModel.cs:845,918` |
| **Manifest migration (P1.2b)** | **THIẾU** — migration chạy im lặng, user không biết metadata bị khôi phục/thay đổi | `InstanceDetailViewModel.ResourcePacksShaders.cs:241,547`; `ModDownloadManager.cs:82`; `Vazer.Core/Packs/PackManifest.cs:117-200` |

### 3.C Settings / Persistence

| Tiêu chí | Có | Thiếu | File:line |
|---|---|---|---|
| Notification Center UI | CÓ | — | `NotificationCenterPanel.xaml` |
| History | CÓ (50 item, in-memory) | **Mất khi restart** — không Save/Load | `NotificationService.cs:16,21,80-85`; `App.xaml.cs:47` |
| Badge unread | CÓ (`9+`) | — | `MainWindow.xaml:70-84`; `MainWindow.xaml.cs:131-138,252-261` |
| Filter "Chỉ lỗi" | — | **THIẾU** | `NotificationCenterPanel.xaml:29-201` |
| Toggle tắt toast / tắt âm | — | **THIẾU** | `GlobalSettings.cs:20-70`; `SettingsPage.xaml:447-449` |
| DND mode | — | **THIẾU** | grep toàn src = 0 |
| Persist unread / history | — | **THIẾU** | `SettingsService.cs:204` |
| Âm thanh không tắt được | — | `IsSoundEnabled` khai báo nhưng không đọc; `PlaySoundSafe` luôn chạy | `NotificationItem.cs:208`; `NotificationService.cs:411-453` |

### 3.D Technical

| Tiêu chí | Hiện trạng | File:line |
|---|---|---|
| Nhiều notification cùng lúc | Xếp dọc, tối đa 5, cắt toast cũ nhất ở **cả 2 lớp** | `NotificationService.cs:15,68-74`; `NotificationHost.xaml.cs:91-104` |
| Dedup | Gom trong 3s theo `Type` + `GroupKey` → tăng `GroupCount`, badge `xN`. **Cùng lỗi lặp 5 lần cách nhau >3s → 5 toast + 5 history** | `NotificationService.cs:17,48-66`; `NotificationItem.cs:218-220` |
| Thread-safety | `lock (_lock)` + `RunOnUIThread` dispatch | `NotificationService.cs:18,43-45,455-465` |
| Cleanup / leak | `CleanUp()` hủy timer, storyboard, `RichMediaImage.Source = null` | `NotificationToast.xaml.cs:424-438` |
| Windows toast native khi minimize | **KHÔNG CÓ** | grep = 0 |
| `CompleteProgress` không refresh màu/icon/countdown | **BUG kỹ thuật** (đã nêu 3.A) | `NotificationToast.xaml.cs:108-134`; `NotificationService.cs:276-297` |
| Dead code | `AnimateToastNotification` không ai gọi | `UiAnimationHelper.cs:752` |
| DI | `AddSingleton<INotificationService, NotificationService>()` — đúng, 1 instance duy nhất | `App.xaml.cs:47` |

---

## BƯỚC 4 — Đề xuất thứ tự fix (N1 → N4)

Ước lượng dưới đây là **ước của Raez theo số file/độ rủi ro**, chưa Master chốt; mỗi sub-phase nên là 1 đợt có đánh số riêng, mỗi mục 1 commit.

### N1 (P0) — Fix background trong suốt + style sai

| Việc | File:line | Thay đổi | Rủi ro |
|---|---|---|---|
| N1.1 | `NotificationToast.xaml:18` | Đổi `CardBackgroundFillColorDefaultBrush` → `SolidBackgroundFillColorBaseBrush` (hoặc `MkVibeSurfaceBrush` nếu Master chọn) | Thấp: 1 dòng, brush đã có sẵn trong SDK/theme |
| N1.2 | `NotificationCenterPanel.xaml:112` | Đổi cùng brush cho thẻ history để đồng nhất | Rất thấp (panel nền đã đục) |
| N1.3 | `NotificationToast.xaml.cs:108-134` | Thêm case `Type`/`AutoDismissMs` để cập nhật icon/màu và khởi động lại countdown khi `CompleteProgress` | **Trung bình** — đụng vòng đời timer; cần test tay progress → success |

Ước lượng: 2 file XAML + 1 file CS, ~10-20 dòng. 1 đợt ngắn. Verify bắt buộc: chụp toast qua `VAZER_TEST_TOAST=type_success`, đo pixel vùng toast (đếm màu nền đục / không còn lộ nội dung), test tay Master 30s. Không sửa UI ngoài 3 dòng trên.

### N2 (P1) — Coverage bổ sung (chọn 5 sự kiến quan trọng nhất)

| Việc | File:line nên phát | Nội dung đề xuất |
|---|---|---|
| N2.1 Tạo instance | `CreateInstanceDialog.xaml.cs:293-345` (sau `instance != null`, trong catch + null) | Success "Đã tạo instance X" / Error kèm lý do |
| N2.2 Xoá instance | `InstancesViewModel.cs:161-199` (sau `DeleteAsync`, trong catch) | Success "Đã xoá instance X" / Error |
| N2.3 Đổi phiên bản | `InstanceDetailViewModel.cs:1619-1623` (sau `result.Success`) | Info/Success "Đã đổi sang 26.3 (fabric), giữ/xoá mods" |
| N2.4 Thiếu dependency | `InstanceDetailViewModel.cs:845,918` | Warning "Thiếu N dependency" kèm action "Cài ngay" trỏ tới flow hiện có |
| N2.5 Manifest migration | `InstanceDetailViewModel.ResourcePacksShaders.cs:241,547` + `PackManifest.cs:117-200` | Info khi migration khôi phục entry; đặc biệt hữu ích sau bug P0.5b |

Ước lượng: 4-5 file CS, ~1 dòng gọi service mỗi chỗ + thông điệp. Rủi ro thấp (chỉ thêm lời gọi, không đổi logic). Cảnh báo: nếu bắn toast trong mỗi lần mở tab RP, dễ spam → **phải dùng `GroupKey` hoặc chỉ bắn khi `isCorrupt`/có thay đổi**, tránh tạo regression mới.

### N3 (P2) — Settings + Persistence

| Việc | File:line | Rủi ro |
|---|---|---|
| Thêm field `NotificationsEnabled`, `NotificationSoundEnabled`, `DndEnabled`, `NotificationHistoryEnabled` | `Vazer.Core/Settings/GlobalSettings.cs:20-70` + `SettingsService.cs:204` | Trung bình: đụng schema JSON settings, cần migrate giá trị cũ (thiếu field → default) |
| Persist History + Unread xuống file (JSON, giới hạn 50) | `NotificationService.cs:20-21,80-87` — load trong constructor / save khi thay đổi | **Cao nhất**: IO trên UI thread, cần debounce; `NotificationItem` chứa `Func<Task>` callback **không serialize được** → phải tách DTO chỉ giữ Title/Message/Type/CreatedAt, mất action khi khôi phục |
| Toggle UI + filter "Chỉ lỗi" + DND | `Views/SettingsPage.xaml` (khu vực `:438-450`), `NotificationCenterPanel.xaml:29-201` | Trung bình |
| Sửa `PlaySoundSafe` đọc setting thay vì bỏ qua | `NotificationService.cs:411-453`, `NotificationItem.cs:208` | Thấp |

Ước lượng: 5-7 file, 1 đợt riêng. Rủi ro lớn nhất là serialize callback — nên hỏi Master trước (Câu hỏi mở).

### N4 (P3) — Technical

| Việc | File:line | Rủi ro |
|---|---|---|
| Dedup nâng cao (cùng lỗi trong N phút → gom, không tạo history mới) | `NotificationService.cs:17,48-66` | Trung bình: dễ làm mất thông báo lỗi quan trọng |
| Queue theo `Priority` (Critical lên trước, không bị cắt bởi giới hạn 5) | `NotificationService.cs:68-74`, `NotificationHost.xaml.cs:91-104` | Trung bình |
| Windows toast native khi app minimize | mới, cần `AppNotificationManager` + khai báo manifest | **Cao**: đụng packaging/MSIX, app hiện chạy unpackaged exe |
| `ReduceMotion` cho toast + xoá dead code | `NotificationToast.xaml.cs:169-299`, `UiAnimationHelper.cs:752` | Thấp |
| Dò vùng làm việc tránh taskbar | `MainWindow.xaml:255-261`, `NotificationHost.xaml.cs:29-37` | Thấp-trung bình |

Ước lượng: 2-4 file + có thể 1 file mới. **Khuyến nghị: chỉ làm 2 mục đầu + ReduceMotion/dead code**; native toast để sau v0.19.0.

---

## BƯỚC 5 — Câu hỏi mở cho Master (Raez KHÔNG đoán)

1. **N1 chọn brush nào?** `SolidBackgroundFillColorBaseBrush` (đục, đồng nhất với CenterPanel, ít thay đổi nhất — Raez nghiêng về cái này) hay `MkVibeSurfaceBrush` (`#1A1D23`/`#F5F0E6`, mang màu nhận diện nhưng lệch tông so với panel hiện tại)? Hoặc Master muốn giữ hiệu ứng kính bằng Acrylic có nền đục phía sau?
2. **Overlap taskbar**: Master có thực thấy toast bị taskbar che khi cửa sổ không maximize không? Raez chỉ thấy margin cố định 24px, chưa có bằng chứng pixel.
3. **N3 persist History**: có chấp nhận **mất nút action** sau restart (vì `Func<Task>` không serialize được) không? Hay chỉ persist dạng text thuần, hay bỏ qua persist?
4. **N2 manifest migration**: có muốn thông báo mỗi lần migration chạy, hay chỉ khi có thay đổi thật (tránh spam khi mở tab)?
5. **Phạm vi N4**: có đưa Windows toast native vào v0.19.0 không (đụng packaging), hay dời sau tag?
6. **Âm thanh**: `UiSoundEnabled` có nên đổi tên / gộp thành toggle âm thông báo riêng không?

---

## Kết luận audit

- **Bug glass: xác nhận, root cause rõ, có file:line + giá trị màu.** Sửa được bằng 1 dòng XAML (`NotificationToast.xaml:18`), rủi ro thấp.
- Hệ thống **đã có** từ Đợt 16 nhiều hơn dự kiến: Center panel, History(50), unread badge, progress toast, 2 action buttons, hover-pause, dedup 3s, giới hạn 5, accessibility, sound. Không nên làm lại từ đầu.
- **Thiếu lớn nhất**: persistence (mất history/unread khi restart), settings (không toggle/DND/filter), 5 sự kiện coverage, và 2 lỗi kỹ thuật nhỏ (`CompleteProgress` không refresh UI, `IsSoundEnabled` chết).
- **Thứ tự đề xuất**: N1 → N2 → N3 → N4. Tag v0.19.0 có thể cân nhắc **sau N1** nếu Master muốn chốt sớm; N2-N4 tách đợt riêng.

Ràng buộc N0.1: **không sửa file code nào**. Chỉ tạo file `docs/refactor/notification-audit.md`. Test chạy lại để xác nhận gate: 502 passed, 0 warning (không đổi).
