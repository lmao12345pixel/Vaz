# Feature Spec: Mod Manager Deep Flow

## 1. Bối cảnh & Mục tiêu

Trang chi tiết instance hiện đã có tab `#tab-mods` với các thao tác cơ bản: bật/tắt mod, lọc theo từ khóa, lọc theo trạng thái (all/on/off/update), sắp xếp tên, chọn hàng loạt (bulk enable/disable/uninstall), và mở catalog tải mod từ Modrinth.

Tuy nhiên, người chơi Minecraft cần một hệ thống quản lý mod có chiều sâu:
1. **Xem chi tiết mod (Mod Inspector)**: người dùng muốn xem thông tin tác giả, mô tả đầy đủ, phiên bản game/loader hỗ trợ, liên kết ngoài và danh sách thư viện/phụ thuộc (dependencies).
2. **Quản lý Dependencies & Cảnh báo xung đột (Dependency Resolution)**: phát hiện các mod đang bật nhưng thiếu dependency bắt buộc (vd: Iris cần Sodium; Sodium Extra cần Sodium; Continuity cần Fabric API). Cung cấp hành động 1-click để cài/bật dependency bị thiếu.
3. **Phân loại danh mục (Categories)**: lọc mod theo phân loại (Hiệu năng, Đồ họa, Công nghệ, Sinh tồn/QoL, Phép thuật) để quản lý modpack lớn nhanh chóng.
4. **Cài đặt kèm Dependency từ Catalog**: khi tải mod từ Modrinth Catalog, launcher tự động phát hiện các mod phụ thuộc chưa có trong instance và gợi ý cài đặt cùng lúc.
5. **Cảnh báo khi tắt/gỡ dependency (Cascade impact)**: thông báo người dùng khi vô hiệu hóa hoặc gỡ bỏ một mod nền tảng đang được các mod khác sử dụng.
6. **Thêm file mod ngoài (Import .jar / Drag-and-drop)**: hỗ trợ kéo thả file `.jar` vào danh sách hoặc nút thêm file cục bộ.

## 2. User Stories

- **US-MM-01 (Inspector)**: Là người quản lý modpack, tôi muốn nhấp vào một mod để mở bảng chi tiết (Inspector) xem mô tả, tác giả, phiên bản, dung lượng và các dependency để nắm rõ mod này làm gì.
- **US-MM-02 (Dependency Check)**: Là người chơi, tôi muốn launcher cảnh báo trực quan khi một mod bị thiếu dependency và cho tôi nút bấm để tự động cài mod thiếu, tránh việc crash game khi khởi động.
- **US-MM-03 (Category Filter)**: Là người chơi có modpack 50+ mods, tôi muốn lọc mod theo danh mục (Hiệu năng, Đồ họa,...) để tìm kiếm nhanh nhóm mod cần cấu hình.
- **US-MM-04 (Install with Dependencies)**: Là người chơi tải mod từ catalog, tôi muốn launcher tự động thêm các dependency cần thiết để không phải tự tìm từng mod phụ thuộc.
- **US-MM-05 (Safety Cascade)**: Là người chơi, khi tôi tắt một mod như Fabric API hay Sodium, launcher cần nhắc tôi rằng các mod khác sẽ bị ảnh hưởng.
- **US-MM-06 (Drop Jar)**: Là người tải mod từ nguồn ngoài, tôi muốn kéo thả file `.jar` vào launcher để cài đặt ngay lập tức.

## 3. Phạm vi

### Trong phạm vi (Web Prototype)
- Giao diện Inspector Drawer / Sheet trượt vào từ bên phải (hoặc modal trung tâm) hiển thị chi tiết mod, dependency list, nút thao tác.
- Badge cảnh báo "Thiếu dependency" trên danh sách mod khi có mod yêu cầu dependency chưa được cài hoặc đang bị tắt.
- Nhóm chip hoặc bộ lọc Category trên `modFilterBar`.
- Dialog xác nhận "Cài đặt kèm dependency" trong flow Download Mods.
- Vùng Drag & Drop `.jar` và nút "Import .jar" trong toolbar.
- Thông báo toast cảnh báo khi tắt một mod có các mod con phụ thuộc.
- Tương thích responsive (390px, 620px), focus accessibility và phím tắt (Escape đóng Inspector, Enter mở).

### Ngoài phạm vi
- Chưa port WinUI (`src/Vaz.WinUI`).
- Không gọi network thật ra Modrinth API ngoài luồng mock hiện có.
- Không can thiệp filesystem thật ngoài browser memory/localStorage.
