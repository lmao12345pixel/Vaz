# Đợt 18.3a — Wizard Step 1 + 2 (Version & Loader Selection)

## 1. Tổng quan

Redesign dialog tạo instance thành Wizard 2 bước hiện đại:
- **Step 1**: Chọn phiên bản Minecraft qua Grid Cards 3 cột (tỷ lệ 16:9), cover art hoặc fallback gradient đa sắc theo seed, badge Release/Snapshot, ngày phát hành, nhãn "Đã cài", thanh tìm kiếm real-time, animation chọn (checkmark bounce + viền ngọc lục bảo).
- **Step 2**: Chọn Mod Loader qua 4 card ngang (Vanilla, Fabric, Quilt, NeoForge) với brand gradient và logo động, dropdown loader version slide-down nạp động từ `IMinecraftVersionService`, panel tóm tắt cấu hình & ước tính dung lượng (Live Preview), nút Tạo Instance.
- **8 animations cơ bản**: Card Staggered Entrance (#1), Card Hover Lift (#2), Card Selected (#3), Card Click (#4), Loader Logo Spin (#5), Search Focus (#6), Search Filter (#7), Dropdown Open / Slide (#8).
- **100% ThemeResource**: Hỗ trợ đầy đủ Light/Dark theme (ContentDialog đồng bộ `AppThemeManager.DialogTheme`).

## 2. Trạng thái từng việc

| Việc | Trạng thái | Commit | Ghi chú |
|---|---|---|---|
| 1. WizardState + CoverImageService | DONE | `2f0636e` | State model lưu step/version/loader; cache service tải ảnh cover từ Minecraft Wiki (cache local, fallback an toàn) |
| 2. Step 1: Version Selection | DONE | `b5ff9d3` | Grid cards 3 cột, real-time search box, badge checkmark bounce, hover lift, disable Tiếp tục khi chưa chọn |
| 3. Step 2: Loader Selection | DONE | `9fc8c2f` | 4 loader cards ngang, logo spin, dropdown loader version slide-down, live preview panel, validate nút Tạo |
| 4. Container tạm & Tích hợp | DONE | `75f58db` | ContentDialog kích thước 820×540, MaxWidth 880, chuyển bước mượt mà (slide + fade), không phụ thuộc PrimaryButton |

## 3. Danh sách file tạo / sửa

**Tạo mới:**
- `src/Vaz.WinUI/Models/WizardState.cs`: State chia sẻ giữa Step 1 và Step 2.
- `src/Vaz.WinUI/Services/CoverImageCacheService.cs`: Service tải và cache ảnh cover vào `%AppData%/Vazer/cache/version-covers/`.
- `src/Vaz.WinUI/Views/Controls/WizardVersionCover.xaml(.cs)`: Control hiển thị cover hoặc gradient fallback (không crash khi mạng offline).
- `src/Vaz.WinUI/Views/Controls/WizardVersionCard.xaml(.cs)`: Card 16:9 cho từng version Minecraft, hỗ trợ hover, press, checkmark bounce.
- `src/Vaz.WinUI/Views/Controls/WizardStep1Version.xaml(.cs)`: Step 1 chọn version, search bar, pagination grid.
- `src/Vaz.WinUI/Views/Controls/WizardStep2Loader.xaml(.cs)`: Step 2 chọn loader, logo spin, live preview panel.
- `scripts/record-wizard-demo.ps1`: Script automation ghi hình demo 30s.

**Sửa đổi:**
- `src/Vaz.WinUI/App.xaml.cs`: Đăng ký `CoverImageCacheService` vào DI.
- `src/Vaz.WinUI/Views/CreateInstanceDialog.xaml(.cs)`: Chuyển sang container chứa Step 1 & Step 2, thiết lập `RequestedTheme = AppThemeManager.DialogTheme`.
- `src/Vaz.WinUI/Views/InstancesPage.xaml.cs`: Làm mới danh sách khi `CreatedInstanceId != null`.
- `scripts/uia-lib.ps1`: Bổ sung `ClickHwnd` hỗ trợ automation modal dialog.

## 4. Test

- **Số test trước**: 390/390 pass.
- **Số test sau**: **390/390 pass** (100% suite pass, 0 fail, 0 skip).
- **Kiểm tra tĩnh**: `npm run check:static` — **PASSED** (build + unit test + agent config 12/12).
- **Biên dịch WinUI**: 0 lỗi, 0 cảnh báo từ code mới.

## 5. Evidence

### Video ghi hình (30s)
- Đường dẫn: `docs/evidence/dot18/videos/wizard-step1-2.mp4`
- Nội dung: Step 1 mở mượt mà → hover card → gõ tìm kiếm "1.21" → xóa tìm kiếm → click chọn version 26.3 → bấm "Tiếp tục →" → Step 2 mở ra → click lần lượt Fabric (logo spin + dropdown slide) → Quilt → NeoForge (cảnh báo chưa hỗ trợ) → Vanilla (InfoBar giải thích) → bấm "← Quay lại" về Step 1 với version vẫn được chọn.

### Bộ ảnh bằng chứng (Light & Dark theme)
1. `docs/evidence/dot18/screenshots/wizard-step1-dark.png`: Step 1 ở Dark theme, grid cards 3 cột.
2. `docs/evidence/dot18/screenshots/wizard-step1-selected.png`: Step 1 khi card 26.3 được chọn (checkmark badge + viền xanh lá, nút "Tiếp tục" sáng).
3. `docs/evidence/dot18/screenshots/wizard-step1-search.png`: Step 1 khi gõ "1.21" lọc kết quả tức thời.
4. `docs/evidence/dot18/screenshots/wizard-step1-light.png`: Step 1 ở Light theme (nền trắng sáng, độ tương phản cao).
5. `docs/evidence/dot18/screenshots/wizard-step2-dark.png`: Step 2 ở Dark theme với 4 thẻ loader ngang và panel tóm tắt.
6. `docs/evidence/dot18/screenshots/wizard-step2-fabric.png`: Step 2 khi chọn Fabric (dropdown nạp 0.20.1, preview cập nhật 226 MB).
7. `docs/evidence/dot18/screenshots/wizard-step2-light.png`: Step 2 ở Light theme.
8. `docs/evidence/dot18/screenshots/wizard-step2-vanilla.png`: Step 2 khi chọn Vanilla (InfoBar thông tin).
9. `docs/evidence/dot18/screenshots/wizard-step2-neoforge.png`: Step 2 khi chọn NeoForge (báo không có bản tương thích cho 26.3).

## 6. Known issues / Giới hạn kỹ thuật

1. **Cover Image Wiki**: Một số endpoint `https://minecraft.wiki/images/{v}_cover.png` trả về HTTP 403 do chính sách bot/user-agent của wiki. `CoverImageCacheService` bắt trọn lỗi và kích hoạt gradient fallback per-version tự động, đảm bảo giao diện luôn đẹp và không bao giờ crash.
2. **WinUI 3 ContentDialog MaxWidth**: Mặc định WinUI 3 giới hạn bề rộng ContentDialog ở mức 548px. Để hiển thị grid 3 cột và live preview panel, dialog đã ghi đè resource `ContentDialogMaxWidth` lên 880px trong `ContentDialog.Resources`.
3. **Thứ tự nút trong Wizard**: WinUI 3 ContentDialog không hỗ trợ ẩn/hiện động nút Primary riêng cho từng trang bên trong một cách mượt mà, do đó wizard sử dụng hệ thống nút riêng bên trong từng Step và gọi `Hide()` khi hoàn thành.

## 7. Đề xuất cho Phase tiếp theo (Đợt 18.3b)

- Thêm **Step 3: Cấu hình RAM & JVM** (hoặc tích hợp tùy chỉnh nâng cao trước khi bấm Tạo).
- Bổ sung hiệu ứng number roll (đếm số mượt mà) cho kích thước download trong panel Tóm tắt.
- Cho phép user cấu hình mirror tải cover art hoặc tải thumbnail chính thức từ launcher meta Mojang.

## 8. Self-review

- [x] Không làm hỏng 390 test hiện tại.
- [x] Không hardcode màu sắc — 100% dùng ThemeResource / static palette chuẩn.
- [x] Toàn bộ animation dùng `CubicEase` / `BackEase`, tôn trọng cờ `ReduceMotion`.
- [x] Đầy đủ bằng chứng video MP4 và 8+ ảnh screenshots ở cả hai chế độ Light/Dark.
- [x] **KHÔNG tự claim PASS manual test** — chờ xác nhận trải nghiệm thực tế từ người dùng.
