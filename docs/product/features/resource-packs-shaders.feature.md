# Feature: Resource Packs & Shaders Management

## 1. Bối cảnh & Mục tiêu
Trong Minecraft, gói tài nguyên (Resource Packs) và shader đồ họa (Shader Packs) là hai yếu tố tùy biến trải nghiệm hình ảnh quan trọng nhất của người chơi.
Hiện tại prototype V.A.Z đã có trang khám phá danh mục chung (`#page-resourcepacks`, `#page-shaders`), nhưng chưa có trình quản trị chuyên sâu theo từng Instance trong trang chi tiết (`#page-detail`).
Tính năng này xây dựng hai tab quản lý trực tiếp trong trang chi tiết Instance:
1. **Resource Packs Manager**:
   - Quản lý danh sách pack: xem ảnh thumbnail, tên, độ phân giải (16x, 32x, 64x), định dạng tương thích phiên bản Minecraft.
   - Sắp xếp thứ tự ưu tiên (Priority order): nút Di chuyển lên (Move Up) / Di chuyển xuống (Move Down) hoặc kéo thả để xác định pack nào ghi đè lên pack nào (Minecraft nạp từ trên xuống dưới).
   - Bật / Tắt (Enable / Disable) từng pack tức thì.
   - Cảnh báo định dạng không tương thích phiên bản Minecraft của Instance.
   - Thêm gói từ máy (.zip) bằng input file hoặc kéo-thả (drag & drop).
   - Nút dẫn nhanh sang trang Khám phá Resource Pack (`#page-resourcepacks`).
2. **Shaders Manager**:
   - Lựa chọn Shader pack đang áp dụng (hoặc Tắt shader).
   - Xem ảnh preview đồ họa chất lượng cao của từng shaderpack.
   - Tùy chỉnh Preset cấu hình (Profile: Thấp / Tiêu chuẩn / Cao / Cực đại - Low / Medium / High / Ultra).
   - Kiểm tra điều kiện tiên quyết (Dependency Check): Kiểm tra Instance đã cài mod shader loader chưa (Iris Shaders hoặc OptiFine).
     - Nếu đã cài: báo trạng thái "Sẵn sàng hoạt động (Iris)".
     - Nếu chưa cài: cảnh báo nổi bật kèm nút 1-click "Cài đặt Iris Shaders" (tự động thêm Iris Shaders vào danh sách mods của instance).
   - Thêm shaderpack từ file .zip máy tính (input file / drag-drop).
   - Nút dẫn nhanh sang trang Khám phá Shaders (`#page-shaders`).

## 2. User Stories
- **US-01 (Quản lý Resource Pack)**: Là người chơi, tôi muốn xem danh sách resource pack của instance, bật/tắt và đổi thứ tự ưu tiên các pack để game hiển thị đúng texture tôi muốn.
- **US-02 (Cảnh báo tương thích Pack)**: Là người chơi, tôi muốn thấy cảnh báo nếu resource pack tạo cho phiên bản Minecraft khác để biết trước khả năng lỗi hiển thị.
- **US-03 (Chọn & Xem trước Shader)**: Là người chơi, tôi muốn chọn nhanh shader pack kèm ảnh preview và chỉnh cấu hình profile (Low/Medium/High/Ultra) phù hợp với sức mạnh GPU.
- **US-04 (Tự động phát hiện thiếu Shader Loader)**: Là người chơi, tôi muốn launcher phát hiện instance chưa có Iris Shaders/OptiFine và cho phép cài 1-click để tôi không phải tự tìm kiếm thủ công.
- **US-05 (Import .zip tiện lợi)**: Là người chơi, tôi muốn kéo thả file .zip resource pack hoặc shaderpack vào giao diện để cài đặt ngay lập tức.
- **US-06 (Responsive & Trợ năng)**: Là người dùng trên màn hình nhỏ hoặc bàn phím, tôi muốn giao diện hiển thị gọn gàng ở 390px/620px, có viền focus `:focus-visible` rõ nét và tôn trọng tùy chọn giảm chuyển động (Reduced Motion).

## 3. Phạm vi triển khai (Web Prototype)
- **Tệp sửa đổi / bổ sung**:
  - `docs/design-preview/index.html`: Thêm tab button Resource Packs & Shaders vào `#page-detail`, thêm `#tab-resourcepacks` và `#tab-shaders`.
  - `docs/design-preview/styles.css`: Thêm kiểu dáng cho danh sách ưu tiên resource pack, nút up/down, cảnh báo tương thích, shader card selector, profile chips, ảnh preview, 0 `transition: all`.
  - `docs/design-preview/app.js`: Cập nhật `TAB_ORDER`, state quản lý `resourcepacks` và `shaders` theo instance, logic reorder priority, toggle enable, Iris dependency check & 1-click install, import .zip, tích hợp với trang catalog.
  - `tests/web-e2e/resource-packs-shaders.spec.ts`: Bộ test Playwright kiểm thử toàn bộ ACs.
- **Không sửa**:
  - Không sửa mã nguồn C# WinUI desktop app (`src/Vaz.WinUI`, `src/Vazer.Core`).
