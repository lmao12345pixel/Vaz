# Feature: Modpack Browser & 1-Click Install

## Bối cảnh & Mục tiêu
Trong Minecraft hiện đại, người chơi thường không cài từng mod thủ công mà trải nghiệm các bộ modpack được cấu hình sẵn (Performance Fabric, Create Adventure, NeoForge Essentials, Cozy Fabric...).
Tính năng **Modpack Browser & 1-Click Install** cung cấp thư viện duyệt modpack phong phú, cho phép lọc theo thể loại, loader, phiên bản, xem chi tiết danh sách mod bên trong, chọn phiên bản phát hành, nhập file `.mrpack`, và cài đặt 1-click tự động tạo instance sẵn sàng chơi.

## User Stories
1. **Duyệt & Tìm kiếm Modpack**: Là người chơi, tôi muốn duyệt danh sách các modpack với hình ảnh đại diện, số lượng mod, lượt tải và tags, để dễ dàng tìm kiếm phong cách chơi phù hợp.
2. **Lọc theo Thể loại & Loader**: Là người chơi, tôi muốn bấm các chip thể loại (Hiệu năng, Phiêu lưu, Kỹ thuật, QoL) và chọn loader (Fabric, NeoForge) hoặc phiên bản MC để thu hẹp kết quả.
3. **Xem Chi tiết Modpack**: Là người chơi, tôi muốn nhấp vào xem thông tin chi tiết một modpack bao gồm ảnh hero banner, tác giả, mô tả, danh sách toàn bộ mod bên trong và chọn phiên bản pack mong muốn.
4. **Cài đặt 1-Click Tạo Instance**: Là người chơi, tôi muốn bấm "Cài đặt & Tạo Instance" để hệ thống tự động tải metadata, cấu hình loader, tải mod và thêm instance mới vào launcher.
5. **Nhập file .mrpack từ ngoài**: Là người chơi, tôi muốn kéo thả hoặc chọn file `.mrpack` từ máy để cài đặt modpack ngoài danh mục mặc định.

## Phạm vi (Scope)
- **Trong phạm vi (Web Prototype)**:
  - Trang `#page-modpacks` với header, ô tìm kiếm `#modpacksSearch`, category chips `#modpackCategoryChips`.
  - Bộ lọc Loader (`#modpacksLoader`), Phiên bản (`#modpacksVersion`), Sắp xếp (`#modpacksSort`).
  - Lưới thẻ Modpack cards `#modpacksGrid` hiển thị cover, icon, tên, author, mod count, size, downloads, loader badge, MC version.
  - Modpack Detail Modal: Banner, icon, metadata, version selector, danh sách mod thẻ/chips, nút cài đặt.
  - Modpack Install Progress Modal: Tiến trình tải từng giai đoạn (Metadata -> Loader -> Mods -> Hoàn tất), nút Hủy an toàn.
  - Import file `.mrpack` qua file input `#modpackFileInput` hoặc kéo thả dropzone `#modpackDropZone`.
  - Responsive 390px/620px, focus-visible outline >= 2px, reduced-motion <= 0.05s, 0 `transition: all`.
- **Ngoài phạm vi**:
  - Tải file thật từ mạng Modrinth/CurseForge qua API (mô phỏng delay và metadata trong prototype).
  - Port sang C# WinUI 3 (chỉ bắt đầu sau khi đạt `WEB_VALIDATED`).
