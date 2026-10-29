# Kế hoạch Triển khai & Nghiệm thu: Mod Manager Deep Flow (WinUI Port)

## Mục tiêu
Port toàn diện tính năng **Mod Manager Deep Flow** từ Web Prototype (`docs/design-preview/ #tab-mods`) sang WinUI (`src/Vaz.WinUI`), đảm bảo đạt 100% tiêu chí theo contract `docs/contracts/mod-manager.contract.md` và tuân thủ `docs/design/tokens.json`.

---

## 5 Bước Triển Khai Chi Tiết

### Bước 1: Mô hình hóa Dữ liệu & Nghiệp vụ Quản lý Mod (`InstanceDetailViewModel.cs`)
1. **Metadata mở rộng cho `InstalledModRow`**:
   - `DisplayName`, `Description`, `Author`, `Category` (`performance`, `graphics`, `technology`, `survival`, `magic`), `IconUri`, `Loader`, `MinecraftVersion`.
   - `Dependencies`: Danh sách dependencies của mod.
   - `MissingDependencies`: Danh sách dependencies bắt buộc chưa được cài hoặc đang bị tắt.
   - `HasMissingDependencies`: Cờ boolean hiển thị badge `Thiếu dependency`.
   - `Links`: Modrinth url, Source code url.
2. **Bộ lọc danh mục (Category Chips)**:
   - Các danh mục: `all` (Tất cả), `performance` (Hiệu năng), `graphics` (Đồ họa), `technology` (Công nghệ), `survival` (Sinh tồn/QoL), `magic` (Phép thuật).
   - Cập nhật số đếm mod thời gian thực: `Hiển thị X / Y mods`.
3. **1-Click Resolve Dependencies (`MM-AC-02`)**:
   - Phương thức `ResolveDependenciesAsync(InstalledModRow mod)`: Tự động cài đặt/kích hoạt dependency thiếu (ví dụ `Fabric API` cho `Create`, `Sodium` cho `Sodium Extra`), xóa badge cảnh báo và hiển thị InfoBar/Toast.
4. **Cảnh báo khi Tắt/Gỡ Mod Dependency (`MM-AC-05`)**:
   - Kiểm tra xem mod sắp tắt/gỡ có đang là dependency cho mod khác không (ví dụ: tắt `Sodium` khi `Sodium Extra` đang bật).
   - Hiển thị hộp thoại xác nhận / cảnh báo ảnh hưởng trước khi thực hiện.
5. **Thêm file `.jar` từ bên ngoài (`MM-AC-06`)**:
   - Phương thức `ImportJarAsync(string sourceFilePath)`: Sao chép file `.jar` vào thư mục `mods/` của instance, phân tích metadata và thêm vào danh sách.

---

### Bước 2: Nâng cấp Giao diện WinUI (`InstanceDetailPage.xaml` & `.cs`)
1. **Hàng Category Chips**:
   - Sử dụng `ItemsControl` / `ListView` với `WrapPanel` chứa 6 chips danh mục.
   - Áp dụng accent highlight cho chip đang chọn.
   - Hiển thị text đếm `ModSummaryText` (`Hiển thị X / Y mods`).
2. **Toolbar Quản lý Mod**:
   - Bổ sung nút `+ Thêm .jar` mở `FileOpenPicker` chọn file `.jar`.
   - Hỗ trợ kéo thả file (Drag & Drop) trực tiếp vào danh sách mod.
3. **Danh sách Mod Row (`ListView`)**:
   - Hiển thị icon mod (hoặc icon khối hộp mặc định).
   - Tên mod hiển thị nổi bật, file name mờ hơn.
   - Badge `Thiếu dependency` màu amber/warning khi `HasMissingDependencies = true`.
   - Badge `↑ update` màu accent khi có bản cập nhật.
   - Nút bật/tắt (ToggleSwitch), nút mở Mod Inspector và nút Xóa.
4. **Mod Inspector Drawer / Panel (`MM-AC-01`, `MM-AC-02`)**:
   - Thiết kế drawer/overlay hoặc ContentDialog chuyên dụng với:
     - Icon mod, tiêu đề, version, loader (Fabric/Forge), tên file, tác giả.
     - Mô tả chi tiết mod, danh mục phân loại.
     - Trạng thái dependencies: `Thiếu: [Tên dependency]` hoặc `Đầy đủ`.
     - Nút `Cài dependency thiếu` (1-click resolve) nổi bật bằng accent style.
     - Danh sách liên kết: Modrinth, Mã nguồn.
     - Nút đóng (hỗ trợ phím Escape).

---

### Bước 3: Catalog Auto Dependency Resolution (`ModBrowserDialog.xaml` & `ViewModel`)
1. Khi bấm cài đặt mod từ Catalog (`ModBrowserDialog`), kiểm tra xem mod đó có dependency bắt buộc chưa có trong instance hay không (ví dụ cài `Sodium Extra` khi chưa có `Sodium`).
2. Nếu có, hiển thị ContentDialog: `"Cài đặt kèm dependencies?"` liệt kê các dependency cần thiết.
3. Khi người dùng xác nhận, tự động tải và cài đặt cả mod chính và các dependencies.

---

### Bước 4: Kiểm tra Tự động & Static Analysis
1. `dotnet build Vaz.sln` không lỗi biên dịch.
2. `dotnet test Vaz.sln` toàn bộ unit tests pass.
3. `npm run check:static` pass (build + tests + 12 agent configs).

---

### Bước 5: Kiểm tra Runtime Desktop Smoke (WinArm) & Nghiệm thu
1. **MM-AC-01**: Click mod row mở Mod Inspector với đầy đủ metadata.
2. **MM-AC-02**: Mod thiếu dependency hiển thị badge cảnh báo, bấm nút resolve tự động cài dependency và xóa badge.
3. **MM-AC-03**: Nhấp chuyển các category chips, kiểm tra danh sách lọc và text đếm mod.
4. **MM-AC-04**: Mở Catalog tìm và cài mod có dependency, kiểm tra dialog xác nhận và cài đủ cả hai.
5. **MM-AC-05**: Tắt mod đang làm dependency, kiểm tra hộp thoại cảnh báo ảnh hưởng.
6. **MM-AC-06**: Nhấn Thêm .jar hoặc kéo thả file `.jar` vào app, kiểm tra mod xuất hiện trong danh sách.
7. **MM-AC-07**: Co giãn cửa sổ 1024px và 800px không tràn ngang, phím Escape đóng Inspector.
8. **Đóng cổng**: Cập nhật contract sang `PARITY_VERIFIED`, checklist sang `MATCH`, task ledger sang `DONE`.
