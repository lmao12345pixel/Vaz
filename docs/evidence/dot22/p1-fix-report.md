# Báo cáo Triển khai Fix 3 Vấn đề UX Install (P1.2 + P1.3 + P1.4) — VAZ Launcher

**Ngày:** 2026-11-09  
**Người thực hiện:** Raez  
**Đợt:** P1.2 / P1.3 / P1.4 (Hoàn thành)  
**Tiền đề:** P1.1 PASSED (`1f2b600`). P0.1 PASSED (`e98b18f`). Tag `v0.19.0-rc1`.

---

## 1. Tổng quan Kết quả Triển khai

Đợt P1.2–P1.4 giải quyết triệt để 3 vấn đề UX Install đã được audit trong P1.1:
1. **V2 (P0 - Persistence):** Khắc phục lỗi mất trạng thái "Installed" của Resource Packs và Shaders sau khi restart app.
2. **V3 (P1 - Preview):** Khôi phục và hiển thị đầy đủ ảnh minh họa Shader sau khi cài đặt thông qua cơ chế cache thumbnail cục bộ và manifest.
3. **V1 (P1 - Feedback):** Bổ sung ProgressBar cho Resource Pack trong Modrinth Catalog; chuẩn hóa Toast Notification to, rõ ràng và có ảnh cho Shader.

---

## 2. Chi tiết Kỹ thuật Từng Vấn đề

### 2.1. V2 Fix — Kiến trúc Manifest & Persistence cho RP/Shader
- **Schema Manifest (`docs/refactor/manifest-schema.md`):**
  - Lưu tại thư mục gốc instance: `resourcepacks.json` và `shaderpacks.json`.
  - Quản lý danh sách `PackManifestEntry`: `fileName`, `projectId`, `slug`, `title`, `versionId`, `thumbnailLocal`, `thumbnailRemote`, `installedAt`, `loader`, `author`.
- **Dịch vụ Manifest (`Vazer.Core/Packs/PackManifest.cs`):**
  - Triển khai `IPackManifestService` và `PackManifestService`.
  - Hỗ trợ `LoadAsync`, `SaveAsync`, `AddOrUpdateEntryAsync`, `RemoveEntryAsync`, `DownloadThumbnailAsync`.
  - Cơ chế **Migration tự động (`EnsureManifestWithMigrationAsync`)**: Nếu instance cũ chưa có file manifest, hệ thống tự động quét các file `.zip` hiện có trong `resourcepacks/` hoặc `shaderpacks/`, tạo file manifest ban đầu với metadata tối thiểu (`fileName`, `installedAt`), không làm mất dữ liệu người dùng cũ.
- **Check Installed chuẩn xác (`ModDownloadManager.cs:55-98`):**
  - Xóa bỏ heuristic cũ `fileName.Contains(slug)` vốn luôn thất bại do tên file Modrinth CDN không chứa slug.
  - Thay bằng đối soát trực tiếp `projectId` hoặc `slug` trong `manifest.Packs` và xác nhận file thực tế tồn tại trên đĩa.
- **UI InstalledBadge (`InstanceDetailPage.xaml:1034, 1387`):**
  - Bind `Visibility` vào thuộc tính `IsInstalled` của `ResourcePackRow` và `ShaderPackRow`, hiển thị badge "Đã cài" màu xanh accent bền vững sau khi khởi động lại app.

### 2.2. V3 Fix — Cache Thumbnail & Khôi phục Ảnh Preview Shader
- **Cơ chế Cache Local (`PackManifest.cs` & `ModDownloadManager.cs:266-295`):**
  - Khi tải Shader / RP từ Modrinth, lấy URL ảnh gốc (`project.IconUrl` hoặc ảnh đầu tiên trong `project.Gallery`).
  - Tải file ảnh về lưu cục bộ tại `instancePath/thumbnails/shader-<slug>.jpg`.
  - Lưu cả `thumbnailLocal` và `thumbnailRemote` (dự phòng khi tải ảnh thất bại) vào manifest.
- **Khôi phục hiển thị (`InstanceDetailViewModel.ResourcePacksShaders.cs:515-565`):**
  - Khi nạp thư viện `LoadShadersCore()`, nạp đường dẫn ảnh từ manifest `entry.ThumbnailLocal ?? entry.ThumbnailRemote`.
  - `ShaderCardBanner` bind `ImageUriSource` hiển thị ảnh minh họa 16:9 sắc nét, chấm dứt tình trạng khung xám rỗng.

### 2.3. V1 Fix — Đồng bộ UX Feedback theo Đề xuất C
- **Thanh tiến trình Resource Pack (`ModrinthCatalogView.xaml:208-235`):**
  - Thêm cụm `ProgressBar` (Height=4, Maximum=100) + `StatusMessage` + nút "Hủy" vào top bar `ModrinthCatalogView.xaml` (tương tự như `ModBrowserView.xaml`).
  - Hiển thị trực quan phần trăm tiến độ khi người dùng cài đặt Resource Pack.
- **Toast Notification Shader to và có ảnh (`ModBrowserViewModel.cs:925-935` & `InstallNotificationHelper.cs`):**
  - Khi cài đặt Shader, nếu `IconUrl` bị null (đặc thù Modrinth Shader), helper tự động fallback lấy ảnh đầu tiên trong `row.Hit.Gallery` truyền vào `imageSource` của toast.
  - Kích hoạt `RichMediaContainer` (44x44px) trong `NotificationToast.xaml`, toast to và nổi bật, không còn bị co bé hay dễ bỏ sót.
  - Text thông báo chuẩn hóa theo loại item: `"Shader đã sẵn sàng trong instance."` hoặc `"Resource Pack đã sẵn sàng trong instance."` thay vì bị hardcode chữ "Mod".

### 2.4. Dọn dẹp Mock Data (P1.4)
- Xóa bỏ toàn bộ danh sách cứng `DefaultResourcePacks` và `DefaultShaders` trong `ResourcePackShaderData.cs`.
- Thư viện instance hoàn toàn nạp từ dữ liệu thực tế của instance đó, không còn hiển thị các pack/shader giả lập.

---

## 3. Tiến trình Test & Warning

- **Test Evolution:**
  - Baseline: 464 tests.
  - Sub-phase P1.2a (+3 tests manifest save/load): 467 tests.
  - Sub-phase P1.2b (+5 tests migration & installed check): 472 tests.
  - Sub-phase P1.2c (+4 tests thumbnail cache & preview): 476 tests.
  - Sub-phase P1.3 (+9 test executions formatting & gallery fallback): **485 tests**.
- **Kết quả nghiệm thu:**
  - `dotnet test`: **485/485 passed**, 0 failed, 0 skipped.
  - `dotnet build -c Debug`: **0 Warning, 0 Error**.
  - `dotnet build -c Release`: **0 Warning, 0 Error**.

---

## 4. Danh sách 5 Commit Tuần tự

| Sub-phase | Commit Hash | Message |
|---|---|---|
| **P1.2a** | `89f8223` | `feat(packs): manifest save for resourcepacks and shaderpacks` |
| **P1.2b** | `2c6b001` | `fix(packs): manifest load and installed check replace heuristic scan` |
| **P1.2c** | `8562014` | `fix(packs): cache thumbnail local and restore shader preview` |
| **P1.3** | `ee6d78f` | `feat(ui): unify install feedback for resourcepack and shader` |
| **P1.4** | *(commit hiện tại)* | `docs(evidence): P1.2 P1.3 P1.4 fix report` |
