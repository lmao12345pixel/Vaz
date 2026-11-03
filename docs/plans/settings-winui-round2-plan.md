# Kế hoạch chi tiết nghiệm thu & hoàn tất WinUI Settings (Round 2)

**Tính năng**: Comprehensive Launcher Settings (Cài đặt toàn diện)  
**Tệp hợp đồng**: `docs/contracts/settings.contract.md`  
**Trạng thái Web**: `WEB_VALIDATED` (Playwright 22/22 PASS, check:static PASS)  
**Mục tiêu App**: Chuyển từ `WEB_VALIDATED` sang `APP_IMPLEMENTED` và đạt `PARITY_VERIFIED`  
**Ngày lập**: 2026-10-29  

---

## 1. Bối cảnh & Mục tiêu

Sau vòng 1 (Round 1), App Verifier đánh giá `FAIL` do các vấn đề:
- **P0**: Giao diện Light theme chưa phủ toàn bộ shell (NavigationView pane và Dock dưới còn nền tối/chữ khó đọc), theme/accent chưa áp dụng ngay khi khởi động trước frame đầu tiên, màu accent chưa ghi đè triệt để xuống tầng control-level.
- **P1**: Thiếu asset ảnh Discord preview (`grass_block.png`, avatar skin), separator hiển thị gạch ngang thay vì `·`, thiếu cập nhật trạng thái khi bật/tắt RPC; JSON chẩn đoán thiếu thuộc tính `activeInstance`; JVM presets còn thừa option Shenandoah; CDN mirror default chưa đổi sang Cloudflare; nhãn nút chính ContentDialog chưa chuẩn hóa ("Xác nhận" / "Xóa").
- **P2**: Bước nhảy RAM chưa cố định 512 MB, nút Lưu cài đặt chưa áp dụng accent ở mọi trạng thái visual state.

Trong vòng 2 (Round 2), toàn bộ mã nguồn WinUI (`AppThemeManager`, `App.xaml(.cs)`, `MainWindow.xaml(.cs)`, `SettingsViewModel.cs`, `SettingsPage.xaml(.cs)`) và Core (`SettingsService.cs`) đã được hoàn thiện và vượt qua toàn bộ 208 unit tests tự động. Kế hoạch này chi tiết hóa các bước nghiệm thu để đưa tính năng về đích `PARITY_VERIFIED`.

---

## 2. Danh mục sửa đổi kỹ thuật đã triển khai (Round 2)

| Hạng mục | Tệp ảnh hưởng | Nội dung chi tiết |
|---|---|---|
| **Theme toàn shell (P0)** | `App.xaml`, `AppThemeManager.cs`, `MainWindow.xaml` | Khai báo `VazShellBackgroundBrush`, `VazPaneBackgroundBrush`, `VazDockBackgroundBrush`, `NavigationViewDefaultPaneBackground`. `AppThemeManager` đồng bộ màu surface cho toàn bộ cửa sổ khi đổi Theme (Dark, Light, Obsidian). |
| **Startup Theme (P0)** | `App.xaml.cs`, `MainWindow.xaml.cs` | Đọc `settings.json` trong `App.OnLaunched` qua `AppThemeManager.Initialize()`, gắn shell trong `MainWindow` trước khi gọi `ContentFrame.Navigate()`. Không bị nháy giao diện. |
| **Control-level Accent (P0)** | `AppThemeManager.cs` | Duyệt qua mọi ThemeDictionary của ứng dụng để cập nhật `AccentFillColor*`, `AccentTextFillColor*`, `FocusStrokeColorOuterBrush`, `TextControlElevationBorderFocusedBrush`. ToggleSwitch, Slider, NumberBox và focus ring nhận đúng màu nhấn. |
| **Giảm chuyển động (P0)** | `AppThemeManager.cs`, `SettingsViewModel.cs` | Đồng bộ `Timeline.AllowDependentAnimations = !ReduceMotion`. Gắn nhãn `MANUAL_REQUIRED` theo quy chuẩn kiểm thử. |
| **Discord Preview Art (P1)** | `Assets/blocks/*`, `Assets/skins/*`, `SettingsPage.xaml`, `SettingsViewModel.cs` | Bổ sung PNG `grass_block.png` và `steve.png` kèm phiên bản grayscale. Tự động đổi ảnh và hạ Opacity 0.45 khi tắt RPC. Cập nhật StatusText thông báo bật/tắt. Dấu phân cách chuẩn `·`. |
| **Báo cáo Chẩn đoán JSON (P1)** | `SettingsViewModel.cs` | Thêm trường `activeInstance` (chuỗi tên instance đang chọn hoặc `null`) đứng trước `instancesCount`. |
| **Chuẩn hóa Presets & Mirrors (P1)** | `GlobalSettings.cs`, `SettingsService.cs`, `SettingsViewModel.cs` | Rút gọn JVM Presets còn đúng 4 option (`standard`, `aikar`, `zgc`, `custom`). CDN Mirror còn đúng 4 option với mặc định là `cloudflare`. Tự động fallback dữ liệu cũ. |
| **ContentDialog Tiêu chuẩn (P1)** | `SettingsPage.xaml.cs` | Khôi phục mặc định: `DefaultButton = Primary`, nhãn `Xác nhận` / `Hủy`. Xóa cache: `DefaultButton = Primary`, nhãn `Xóa` / `Hủy`. |
| **Bước nhảy RAM 512 MB (P2)** | `SettingsPage.xaml`, `SettingsService.cs` | Cấu hình `StepFrequency="512"` và `SmallChange="512"` cho Slider và NumberBox. Backend làm tròn bội số 512 MB trước khi lưu. |
| **Nút Lưu Accent (P2)** | `App.xaml` (`VazAccentButtonStyle`), `SettingsPage.xaml` | Tạo Style nút chuẩn với VisualStateManager đầy đủ các trạng thái PointerOver, Pressed, Disabled bám sát bảng màu nhấn. |

---

## 3. Quy trình nghiệm thu 4 bước

```
[Bước 1: Tự động & Static] (Đã Đạt 208/208 Test)
            │
            ▼
[Bước 2: Runtime Smoke Test WinUI] (Chạy app thực tế kiểm tra AC-01 -> AC-10)
            │
            ▼
[Bước 3: Người dùng xác nhận Visual & Motion] (SET-AC-05 MANUAL_REQUIRED)
            │
            ▼
[Bước 4: Kiểm tra Responsive & Không hồi quy] (SET-AC-11: 1024px, 800px & trang cũ)
            │
            ▼
[Hoàn tất & Ký nhận PARITY_VERIFIED]
```

---

### Bước 1: Kiểm tra tự động & Static Analysis (Gate Check)

- **Mục tiêu**: Đảm bảo 100% mã nguồn biên dịch sạch, không warning, test suite không hồi quy.
- **Lệnh thực thi**:
  1. `dotnet build Vaz.sln` (Yêu cầu: 0 Error, 0 Warning).
  2. `dotnet test Vaz.sln` (Yêu cầu: 208/208 tests pass).
  3. `npm run check:static` (Yêu cầu: Pass cả build, unit test và agent validation).
- **Trạng thái**: **ĐÃ HOÀN THÀNH VÀ PASS**.

---

### Bước 2: Runtime Smoke Test WinUI (Desktop Testing)

Thực hiện chạy ứng dụng thật:
```powershell
dotnet run --project src/Vaz.WinUI/Vaz.WinUI.csproj
```

Kiểm tra trực quan theo từng tiêu chí nghiệm thu:

1. **SET-AC-01 (Điều hướng danh mục)**:
   - Nhấp lần lượt 8 chip: *Tất cả, Khởi chạy, Java & RAM, Giao diện, Mạng, Discord, Lưu trữ, Thông tin*.
   - **Kỳ vọng**: Đúng thẻ tương ứng xuất hiện; chip được chọn có viền và màu nền accent nhẹ. Chọn "Tất cả" hiển thị toàn bộ 7 nhóm thẻ.

2. **SET-AC-02 (Java & RAM)**:
   - Kéo Slider RAM tối đa và nhập NumberBox: kiểm tra tính đồng bộ 2 chiều.
   - Nhấn mũi tên lên/xuống của NumberBox: giá trị nhảy chính xác từng bước 512 MB.
   - Nhấn "Dò Java": kiểm tra điền đường dẫn Java 21 và hiển thị InfoBar thông báo.
   - Chọn preset JVM (Aikar, ZGC): cờ tối ưu tương ứng tự điền vào khung Arguments.

3. **SET-AC-03 (Độ phân giải & Toàn màn hình)**:
   - Nhập kích thước 1920x1080, bật toggle "Toàn màn hình". Lưu cài đặt, khởi động lại ứng dụng để kiểm tra giá trị còn nguyên.

4. **SET-AC-04 (Theme & Accent toàn diện)**:
   - Chuyển sang **Light Theme**: kiểm tra toàn bộ khung ứng dụng, bao gồm thanh điều hướng bên trái (NavigationView pane) và thanh Play Dock ở đáy chuyển sang tone màu sáng rõ ràng, chữ và icon tương phản cao, không bị nền đen chữ tối.
   - Chuyển sang **Obsidian Theme**: nền đen sâu, tương phản sắc nét.
   - Chọn các màu nhấn: **Emerald, Cyan, Violet, Amber, Rose**.
   - **Kỳ vọng**: Màu sắc đổi ngay lập tức trên các nút, ToggleSwitch, Slider, viền focus và chỉ báo mục chọn của thanh điều hướng.
   - **Kiểm tra tắt/mở lại**: Đóng app và chạy lại; màn hình đầu tiên (Instances) phải hiển thị ngay theme và accent đã lưu mà không cần vào trang Settings.

5. **SET-AC-06 (Mạng & Tải xuống)**:
   - Kiểm tra dropdown CDN Mirror: có 4 lựa chọn, mặc định hiển thị Cloudflare Global Edge.
   - Đổi số luồng tải sang 16 luồng, lưu cài đặt.

6. **SET-AC-07 (Discord Preview)**:
   - Bật/tắt toggle "Kích hoạt Discord Rich Presence":
     - Khi tắt: thẻ preview mờ đi (`Opacity 0.45`), ảnh khối cỏ và avatar chuyển sang đen trắng (grayscale), thanh thông báo ghi `"Đã tắt Discord Rich Presence."`.
     - Khi bật: thẻ preview rõ nét (`Opacity 1.0`), ảnh có màu sắc đầy đủ, thông báo ghi `"Đã bật Discord Rich Presence."`.
   - Nhập text vào "Trạng thái tùy biến in-game": nội dung trong thẻ preview thay đổi trực tiếp theo từng ký tự. Dấu phân cách giữa tên instance và phiên bản hiển thị đúng dấu chấm giữa `·`.

7. **SET-AC-08 (Lưu trữ & Cache)**:
   - Nhấn "Xóa cache tải xuống": xuất hiện ContentDialog với nút chính màu đỏ mang nhãn `"Xóa"`, nút phụ `"Hủy"`.
   - Bấm "Xóa": dung lượng cache chuyển về `0 MB`, status ghi nhận `"Đã xóa cache tải xuống · giải phóng 248 MB."`.

8. **SET-AC-09 (Thông tin phần cứng & Xuất JSON)**:
   - Khung thông tin hiển thị chi tiết OS, CPU, GPU, RAM, Java.
   - Bấm "Xuất báo cáo chẩn đoán (.json)": chọn nơi lưu, mở file JSON kiểm tra có đầy đủ khóa `activeInstance` (chứa tên instance hiện tại) và `instancesCount`.

9. **SET-AC-10 (Khôi phục mặc định & Lưu trữ bền vững)**:
   - Nhấn "Mặc định": ContentDialog xuất hiện với nút chính mang nhãn `"Xác nhận"` (`DefaultButton = Primary`).
   - Bấm "Xác nhận": toàn bộ thiết lập hoàn tác về giá trị gốc (RAM 4096 MB, Cloudflare mirror, Dark theme, Emerald accent).
   - Bấm "Lưu cài đặt": InfoBar thông báo lưu thành công, file `%APPDATA%/Vazer/Minecraft/settings.json` cập nhật đúng dữ liệu.

---

### Bước 3: Xác nhận trực quan của người dùng (`SET-AC-05` — MANUAL_REQUIRED)

- **Lý do**: WinUI 3 quản lý chuyển động ở tầng hệ thống và phụ thuộc vào compositor của Windows. Ứng dụng điều khiển cờ `Timeline.AllowDependentAnimations = !ReduceMotion`. Trải nghiệm mượt hay giảm giật cần sự xác nhận cảm nhận trực tiếp từ người dùng.
- **Thao tác**:
  1. Người dùng bật công tắc "Giảm chuyển động".
  2. Quan sát chuyển cảnh và thao tác chuyển tab/chip.
  3. Phản hồi xác nhận: `"Xác nhận SET-AC-05 đạt"` hoặc `"Đồng ý nghiệm thu chuyển động"`.

---

### Bước 4: Kiểm tra Responsive & Chống hồi quy (`SET-AC-11`)

1. **Co giãn cửa sổ (Responsive)**:
   - Thu hẹp chiều rộng cửa sổ ứng dụng về mức **1024px**: hàng Category chips tự động xuống dòng (WrapPanel), không có thanh cuộn ngang xuất hiện, nội dung trong form tự căn chỉnh vừa vặn.
   - Thu hẹp tiếp về mức **~800px**: ScrollViewer dọc hoạt động trơn tru, các thẻ cài đặt không bị cắt xén lề phải.
2. **Kiểm tra không hồi quy (Regression Check)**:
   - Chuyển sang trang **Instances**: danh sách instance hiển thị bình thường, play dock vẫn nhận diện đúng instance active.
   - Chuyển sang trang **Accounts**: danh sách tài khoản, avatar và trạng thái xác thực không bị ảnh hưởng bởi việc đổi theme/accent.

---

### Bước 5: Đóng cổng tính năng & Hoàn tất

Khi tất cả các bước trên đạt kết quả:
1. Cập nhật `docs/contracts/settings.contract.md`:
   - Đổi `status: PARITY_VERIFIED`.
   - Cập nhật bảng bằng chứng thực tế cho App.
2. Cập nhật `.agent/parity/settings.parity.md`:
   - Xác nhận tất cả các mục đạt trạng thái `MATCH`.
3. Cập nhật `.agent/tasks/settings.task.md`:
   - Đổi trạng thái task sang `DONE`.
4. Báo cáo tổng kết và thực hiện Git commit theo yêu cầu người dùng.
