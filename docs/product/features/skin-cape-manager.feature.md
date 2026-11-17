# Feature: Skin & Cape Customizer (Quản lý Ngoại hình & Áo choàng)

## 1. Mục tiêu & Bối cảnh
Trang quản lý Skin & Cape (`#page-skins`) cho phép người chơi tùy biến ngoại hình đại diện trong Minecraft với mô hình 3D trực quan tương tác thời gian thực:
- Xem trước mô hình 3D (3D Player Viewer) hỗ trợ các tư thế hoạt ảnh: đi bộ (walk), bay (fly), ngồi/rình rập (sneak), vẫy tay (wave), giương cung (bow).
- Thư viện mẫu skin có sẵn (Steve, Alex, Ari, Efe, Kai, Makena) kèm hỗ trợ nhập file PNG cá nhân (64x64 / 64x32) và tải skin theo tên người chơi bất kỳ.
- Quản lý bộ sưu tập Áo choàng (Cape) chính thức (Migrator, Vanilla, 15th Anniversary, Cherry) và nhập cape cá nhân.
- Chuyển đổi linh hoạt kiểu model cánh tay (Classic 4px vs Slim 3px).
- Liên kết và đồng bộ trạng thái skin/cape trực tiếp với tài khoản đang chọn.

## 2. Phạm vi (Scope)
- **Giao diện**:
  - Khung Canvas 3D `#skinViewer` với thanh công cụ xoay tự động `#skinAutoRotateBtn` và chọn hoạt ảnh `#skinAnimGroup`.
  - Nút chuyển đổi kiểu model `#skinModelToggleBtn` (Classic ↔ Slim).
  - Nút Áp dụng skin `#applySkinBtn` và Trả về mặc định `#resetSkinBtn`.
  - Bộ sưu tập Áo choàng `#skinCapeList` và nút tải cape `#capeUploadBtn`.
  - Lưới thẻ skin `#skinGrid` hiển thị thumbnail, tên, kiểu model, badge "Đang dùng", nút xóa cho custom skin.
  - Công cụ tìm nạp skin: ô nhập username `#skinGrabInput` + nút `#skinGrabBtn`, nút tải file `#skinUploadBtn` + vùng kéo thả `#skinDropZone`.
- **Tương tác**:
  - Kéo xoay mô hình 3D bằng chuột, tự động xoay quanh trục Y khi bật auto-rotate.
  - Tải skin từ file PNG kèm validation kích thước pixel chuẩn Minecraft.
  - Tải skin theo username Minecraft với cơ chế fallback sinh skin thuật toán nếu không có mạng.
  - Responsive 1 cột trên màn hình nhỏ (<=620px và 390px), outline focus-visible rõ ràng.

## 3. User Stories
- **US-1**: Là người chơi, tôi muốn xem mô hình 3D chuyển động của nhân vật để hình dung ngoại hình thực tế trong game.
- **US-2**: Là người chơi, tôi muốn đổi nhanh giữa các skin mặc định và skin tải từ bên ngoài để cá nhân hóa nhân vật.
- **US-3**: Là người chơi, tôi muốn chuyển đổi giữa tay to (Classic 4px) và tay thon (Slim 3px) để skin hiển thị không bị lỗi texture.
- **US-4**: Là người chơi, tôi muốn chọn và hiển thị áo choàng phía sau lưng nhân vật trong mô hình 3D.
- **US-5**: Là người chơi, tôi muốn nhập tên streamer/bạn bè để tải nhanh skin của họ về thư viện của mình.
