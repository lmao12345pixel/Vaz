# Báo cáo Triển khai Hệ thống Notification System (Đợt 16)

## 1. Tổng quan
- **Mục tiêu**: Xây dựng hệ thống Toast Notification và Notification Center chuẩn mực cho V.A.Z Launcher (.NET 8 / WinUI 3), thay thế hoàn toàn các popup lỗi `MessageDialog` dạng modal gây gián đoạn trải nghiệm người dùng và các banner lỗi `InfoBar` không đồng bộ. Đảm bảo hỗ trợ 100% ThemeResource (Light & Dark Mode), xử lý thread-safe đa luồng qua DispatcherQueue, tự động giới hạn stack và không rò rỉ bộ nhớ.
- **Trạng thái**: Hoàn thành (5/5 Phase).
- **Thời gian thực tế từng Phase**:
  - Phase A (Core Infrastructure): ~50 phút (ước lượng 1.5 giờ).
  - Phase B (Animations & Micro-interactions): ~30 phút (ước lượng 45 phút).
  - Phase C (Notification Center & History Panel): ~40 phút (ước lượng 1.0 giờ).
  - Phase D (Tích hợp Use Cases thực tế): ~35 phút (ước lượng 45 phút).
  - Phase E (Cleanup & Kiểm thử toàn diện): ~25 phút (ước lượng 30 phút).
  - **Tổng thời gian**: ~3 giờ. Không có phase nào vượt giới hạn 1.5 giờ.

---

## 2. Tiến độ theo Phase

| Phase | Trạng thái | Commit hash | Ghi chú |
|---|---|---|---|
| **Phase A: Core Infrastructure** | Done | `611fdf6` | Models `NotificationType`, `NotificationPriority`, `NotificationItem` (`Func<Task>` callbacks), `NotificationService` (Singleton, thread-safe, sound), `NotificationHost` (overlay, dynamic hit-test, clip geometry), `NotificationToast` cơ bản. |
| **Phase B: Animations** | Done | `3f3f8ea` | Slide In 350ms CubicEase, Slide Out 250ms, Hover scale 1.02 + hiện nút X (150ms), Pause/Resume auto-dismiss timer khi hover, Progress bar drain 100% -> 0% hoặc bind tiến trình thực tế. |
| **Phase C: Notification Center** | Done | `2de51a5` | Bell icon trên AppTitleBar với unread badge pill ("9+"), `NotificationCenterPanel` 400px trượt từ mép phải, danh sách lịch sử (tối đa 50 mục) nhóm theo ngày, xóa từng mục, xóa tất cả, tự động reset badge khi mở. |
| **Phase D: Tích hợp Use Cases** | Done | `ee922c9` | Tích hợp launch error (Critical, sticky, "Thử lại"), ModBrowser error ("Thử lại") & success (rich media icon), InstanceDetail error & success, download progress Minecraft/mod, mod updates notification, game start/stop (playtime tracking). |
| **Phase E: Cleanup & Integration** | Done | *(Xem commit Phase E)* | Gỡ bỏ toàn bộ `ShowErrorMessageAction`, `ShowCrashDialogAction` và error `InfoBar`. Kiểm thử đạt 14/14 test cases, tạo bộ bằng chứng 12 screenshots (6 types x 2 themes) và 5 video MP4. |

---

## 3. Danh sách file

| File | Hành động | Mục đích |
|---|---|---|
| `src/Vaz.WinUI/Models/NotificationType.cs` | Tạo mới | Định nghĩa enum loại thông báo (`Success`, `Warning`, `Error`, `Info`, `Update`, `Progress`) và mức ưu tiên (`Low`, `Normal`, `High`, `Critical`). |
| `src/Vaz.WinUI/Models/NotificationItem.cs` | Tạo mới | Model thông báo kế thừa `INotifyPropertyChanged`, hỗ trợ `Guid Id`, `Func<Task>` callbacks, `ProgressValue`, `GroupCount`, `GroupKey`, `IsSticky`, `ImageSource`, `IconGlyph`, `TypeColorHex`. |
| `src/Vaz.WinUI/Services/INotificationService.cs` | Tạo mới | Interface Singleton quản lý thông báo, `ActiveNotifications` (tối đa 5), `History` (tối đa 50), `UnreadCount`, các API show, dismiss, progress, sound. |
| `src/Vaz.WinUI/Services/NotificationService.cs` | Tạo mới | Triển khai Singleton thread-safe, nhóm duplicate thông báo trong 3s, phát âm thanh Win32/MediaPlayer, quản lý bộ sưu tập và DispatcherQueue. |
| `src/Vaz.WinUI/Views/Controls/NotificationHost.xaml(.cs)` | Tạo mới | UserControl overlay neo góc dưới phải cửa sổ, quản lý stack dọc tối đa 5 toast, dynamic `IsHitTestVisible` (click-through khi rỗng), clip geometry chống tràn. |
| `src/Vaz.WinUI/Views/Controls/NotificationToast.xaml(.cs)` | Tạo mới | UserControl hiển thị thẻ toast 400px với thanh màu 4px bên trái, header icon + title + X button (hover), body message + thumbnail, action buttons (`Func<Task>`), thanh tiến trình countdown/real-time đáy thẻ. |
| `src/Vaz.WinUI/Views/Controls/NotificationCenterPanel.xaml(.cs)` | Tạo mới | UserControl slide-over 400px hiển thị trung tâm thông báo, nhóm ngày (Hôm nay, Hôm qua, Cũ hơn), xóa từng item, nút Xóa tất cả, tự động đánh dấu đã đọc. |
| `src/Vaz.WinUI/App.xaml.cs` | Sửa | Đăng ký `INotificationService` làm Singleton trong DI container. |
| `src/Vaz.WinUI/MainWindow.xaml` | Sửa | Thêm Bell icon button kèm badge pill trên AppTitleBar, đặt `NotificationHost` và `NotificationCenterPanel` overlay trong Grid gốc `ShellRoot`. |
| `src/Vaz.WinUI/MainWindow.xaml.cs` | Sửa | Kết nối sự kiện Bell button, đồng bộ unread badge, gỡ bỏ `ContentDialog` modal cũ, hỗ trợ env flags phục vụ test automation. |
| `src/Vaz.WinUI/ViewModels/MainViewModel.cs` | Sửa | Thay thế dialog modal bằng Toast Notifications (khởi chạy lỗi, đóng game bất thường, RAM thấp, Minecraft đang chạy, tính thời gian chơi). |
| `src/Vaz.WinUI/ViewModels/ModBrowserViewModel.cs` | Sửa | Gỡ bỏ property `ErrorMessage`; thay thế toàn bộ error handling bằng `_notificationService.ShowError` với callback "Thử lại"; hiển thị toast Success kèm icon mod. |
| `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.cs` | Sửa | Tích hợp toast cho thao tác gỡ mod, bulk updates mod, cập nhật hoàn tất. |
| `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs` | Sửa | Tích hợp toast thông báo xóa Resource Pack và xóa Shader thành công/thất bại. |
| `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` | Sửa | Gỡ bỏ thẻ error `<InfoBar>` cũ; giữ lại các banner thông tin tĩnh. |
| `src/Vaz.WinUI/Views/ModBrowserView.xaml` | Sửa | Gỡ bỏ thẻ error `<InfoBar>` cũ kèm nút thử lại. |
| `scripts/generate-all-evidence.ps1` | Tạo mới | Script tự động chụp màn hình 12 loại toast (6 types x 2 themes) và các kịch bản thực tế. |
| `scripts/generate-videos.ps1` | Tạo mới | Script ghi hình và biên dịch 5 video MP4 chuẩn H.264 cho 5 Phase thông qua ffmpeg. |

---

## 4. Kết quả kiểm thử

### Kiểm tra tự động
- `dotnet test Vaz.sln`: 342/342 unit tests PASS (không giảm test nào, 0 failed, 0 skipped).
- `dotnet build Vaz.sln`: Succeeded (0 Warning, 0 Error).
- `npm run check:static`: PASSED toàn bộ (Build pass, Unit tests pass, 12/12 Agent configurations valid).

### 14 Test cases thủ công (Phase E2)

| STT | Nội dung kiểm thử | Kết quả | Ghi chú |
|---|---|---|---|
| 1 | Toast Success/Error/Warning/Info/Update/Progress hiển thị đúng | **PASS** | Đã chụp ảnh kiểm chứng cả 6 loại trên cả 2 theme Light & Dark. |
| 2 | Auto-dismiss sau đúng thời gian theo Priority | **PASS** | Low = 5s, Normal = 10s, High = 15s đếm ngược mượt mà qua thanh đáy thẻ. |
| 3 | Critical không tự tắt | **PASS** | Toast Critical (vd: lỗi khởi chạy game) có `AutoDismissMs = 0`, thanh đếm ngược ẩn, chỉ đóng khi người dùng tương tác. |
| 4 | Hover pause progress + hiện X | **PASS** | Di chuột vào toast: scale 1.02, thanh tiến trình tạm dừng, nút X hiện (Opacity 0 -> 1 trong 150ms). Di chuột ra: tiếp tục đếm ngược. |
| 5 | Click X dismiss mượt | **PASS** | Nhấn X: toast trượt ra mép phải (TranslateX 0 -> 400, Opacity 1 -> 0 trong 250ms) rồi gỡ khỏi stack. |
| 6 | Max 5 toast (thêm cái 6 đẩy cái cũ nhất ra) | **PASS** | Tự động loại bỏ toast cũ nhất khỏi active stack khi có toast thứ 6; item cũ vẫn lưu nguyên trong History. |
| 7 | Grouping 3 lỗi/3s gộp thành 1 | **PASS** | Các thông báo trùng Type + GroupKey trong vòng 3s tăng `GroupCount` (`x2`, `x3`) và cập nhật nội dung, không spam thêm thẻ mới. |
| 8 | Bell → Center mở, badge ẩn | **PASS** | Nhấn Bell: panel 400px trượt vào, tự động gọi `MarkAllRead()` làm ẩn badge pill ("3", "9+"). |
| 9 | Light/Dark theme đồng bộ | **PASS** | Toàn bộ giao diện sử dụng `ThemeResource` (`CardBackgroundFillColorDefaultBrush`, `TextFillColorPrimaryBrush`, `CardStrokeColorDefaultBrush`, v.v.). |
| 10 | Âm thanh phát đúng (nếu bật) | **PASS** | Phát âm thanh `ms-winsoundevent` bằng `MediaPlayer` (giải phóng sau 2s) và Win32 `MessageBeep` fallback; tự động tắt cho Progress toast. |
| 11 | Toast không tràn ra ngoài cửa sổ | **PASS** | Thiết lập hình học cắt `Clip` động trên `NotificationHost.SizeChanged`. |
| 12 | Click xuyên qua overlay khi không có toast | **PASS** | `IsHitTestVisible = false` khi stack rỗng, cho phép click xuyên suốt vào nội dung trang bên dưới; tự bật `true` khi có toast xuất hiện. |
| 13 | Nút "Thử lại" trong toast Error hoạt động | **PASS** | Action button liên kết delegate `Func<Task>`, thực thi bất đồng bộ mà không treo UI. |
| 14 | Progress cập nhật real-time | **PASS** | `ShowProgress`, `UpdateProgress`, `CompleteProgress` cập nhật thanh tiến trình theo phần trăm thực tế (0-100%). |

---

## 5. Bằng chứng

### 5 Video ngắn (15-20s) theo từng Phase
Thư mục lưu trữ: `docs/evidence/dot16/videos/`

1. **Phase A (Toast xuất hiện & đóng)**:
   - File: `docs/evidence/dot16/videos/phase_a_toast_appear.mp4`
   - Mô tả: Toast hiển thị chuẩn mực ở góc dưới phải cửa sổ, di chuyển chuột hover nút X và đóng toast mượt mà.
2. **Phase B (Slide in, hover pause, progress bar drain)**:
   - File: `docs/evidence/dot16/videos/phase_b_slide_hover.mp4`
   - Mô tả: Hiệu ứng trượt vào mép phải 350ms, thanh đếm ngược đáy thẻ tự động drain; khi hover chuột vào thẻ, scale phóng to 1.02 và thanh đếm dừng lại; đưa chuột ra ngoài tiếp tục drain.
3. **Phase C (Bell icon & Notification Center panel)**:
   - File: `docs/evidence/dot16/videos/phase_c_bell_panel.mp4`
   - Mô tả: Icon chuông trên thanh tiêu đề hiển thị badge số lượng thông báo; nhấn chuông mở Notification Center trượt ra từ mép phải; badge tự động ẩn; duyệt danh sách lịch sử.
4. **Phase D (Lỗi mạng & Nút Thử lại)**:
   - File: `docs/evidence/dot16/videos/phase_d_error_retry.mp4`
   - Mô tả: Thông báo lỗi khởi chạy game (Critical, thanh đỏ bên trái, sticky) hiển thị nút "Thử lại"; người dùng tương tác nút "Thử lại" kích hoạt gọi lại hàm khởi chạy.
5. **Phase E (Toàn cảnh xếp chồng 4 toast & Chuyển Theme)**:
   - File: `docs/evidence/dot16/videos/phase_e_overview_theme.mp4`
   - Mô tả: Toàn cảnh hệ thống với 4 thẻ toast xếp chồng trong Light Mode, hiển thị sắc nét với ThemeResource chuẩn Fluent Design.

### Ảnh chụp màn hình 6 loại Toast (Light & Dark Theme)
Thư mục lưu trữ: `docs/evidence/dot16/`

| Loại Toast | Dark Mode | Light Mode |
|---|---|---|
| **Success** (Thành công - Xanh lá) | `docs/evidence/dot16/toast_success_dark.png` | `docs/evidence/dot16/toast_success_light.png` |
| **Warning** (Cảnh báo - Vàng cam) | `docs/evidence/dot16/toast_warning_dark.png` | `docs/evidence/dot16/toast_warning_light.png` |
| **Error** (Lỗi - Đỏ) | `docs/evidence/dot16/toast_error_dark.png` | `docs/evidence/dot16/toast_error_light.png` |
| **Info** (Thông tin - Xanh dương) | `docs/evidence/dot16/toast_info_dark.png` | `docs/evidence/dot16/toast_info_light.png` |
| **Update** (Cập nhật - Tím) | `docs/evidence/dot16/toast_update_dark.png` | `docs/evidence/dot16/toast_update_light.png` |
| **Progress** (Tiến trình - Xanh ngọc) | `docs/evidence/dot16/toast_progress_dark.png` | `docs/evidence/dot16/toast_progress_light.png` |

### Các ảnh chụp kịch bản thực tế
- Xếp chồng 4 toast Dark Mode: `docs/evidence/dot16/toasts_dark.png`
- Xếp chồng 4 toast Light Mode: `docs/evidence/dot16/toasts_light.png`
- Nút đóng X hiện khi hover: `docs/evidence/dot16/toast_hover_close_button.png`
- Lỗi khởi chạy game (Critical sticky): `docs/evidence/dot16/toast_game_launch_error.png`
- Notification Center Dark Mode: `docs/evidence/dot16/notification_center_dark.png`
- Notification Center Light Mode: `docs/evidence/dot16/notification_center_light.png`

---

## 6. Vấn đề tồn đọng (Known Issues)

| Mô tả lỗi | Mức độ | Ảnh hưởng | Đề xuất fix |
|---|---|---|---|
| Khi có hơn 5 thông báo xuất hiện đồng thời trong 1 frame duy nhất, việc loại bỏ toast cũ nhất phụ thuộc vào Dispatcher queue | Low | Toast thứ 6 có thể nháy nhẹ trong 1 frame trước khi toast cũ nhất trượt ra | Thêm hàng đợi `Queue<NotificationItem>` nội bộ để trì hoãn render nếu active count >= 5. |
| Âm thanh MediaPlayer yêu cầu quyền media background trên một số bản Windows N edition không có Media Feature Pack | Low | Âm thanh không phát nhưng app không lỗi nhờ try-catch và Win32 `MessageBeep` fallback | Duy trì fallback Win32 `MessageBeep` hiện tại. |

---

## 7. Đề xuất bước tiếp theo
1. **Lưu trữ lịch sử thông báo vào Local Storage (SQLite/JSON)**: Hiện tại lịch sử 50 mục được lưu trong memory của phiên làm việc. Có thể persist vào file cấu hình để khi mở lại app người dùng vẫn xem lại được log cập nhật và tải mod.
2. **Tùy chỉnh bật/tắt âm thanh và thời gian hiển thị trong SettingsPage**: Cho phép người dùng chọn thời gian auto-dismiss (5s, 10s, 15s) và bật/tắt Notification sound theo sở thích.
3. **Phân loại bộ lọc (Filter) trong Notification Center**: Bổ sung chip lọc theo Type (Chỉ xem Lỗi, Cập nhật, hoặc Tải xuống) khi danh sách lịch sử có nhiều mục.

---

## 8. Tự đánh giá (Self-Review)
- **Điểm mạnh**:
  - Kiến trúc phân lớp cực kỳ sạch: Model -> Service -> Host/View -> ViewModel integration.
  - Tuân thủ 100% nguyên tắc không làm gián đoạn người dùng (non-blocking), thay thế triệt để các modal dialog khó chịu.
  - Hỗ trợ đầy đủ delegate `Func<Task>` giúp các action như "Thử lại", "Cập nhật tất cả" chạy bất đồng bộ trơn tru.
  - Bằng chứng video và hình ảnh được tự động hóa hoàn toàn với độ phân giải và chất lượng cao.
- **Rủi ro tiềm ẩn**:
  - Cần chú ý khi người dùng bật chế độ màn hình thu nhỏ (window < 600px width), chiều rộng toast 400px có thể chiếm phần lớn chiều ngang; trong tương lai có thể bổ sung Adaptive trigger co giãn chiều rộng về `min(400px, WindowWidth - 32px)`.
- **Cải thiện lần sau**:
  - Thiết kế thêm kịch bản kiểm thử tự động trực tiếp trên NotificationHost thông qua UI test runner.
