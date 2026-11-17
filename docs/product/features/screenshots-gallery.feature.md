# Feature: Screenshots Gallery & Lightbox Viewer

- **Mã tính năng**: `screenshots-gallery`
- **Trạng thái**: Draft / `WEB_SPEC`
- **Phạm vi**: Web Prototype (`docs/design-preview/`) -> Port WinUI sau
- **Ngày tạo**: 2026-10-27

---

## 1. Bối cảnh & Mục tiêu

Trong Minecraft, người chơi thường xuyên nhấn **F2** để chụp lại những khoảnh khắc đẹp (công trình xây dựng, cảnh hoàng hôn shader, đánh bại Ender Dragon).
Hiện tại tab `#tab-screenshots` trong Instance Detail đã có khung cơ bản và lightbox đơn giản, nhưng cần được hoàn thiện sâu:
1. Hiển thị lưới ảnh trực quan, metadata đầy đủ (tên file, tiêu đề, thế giới, kích thước, ngày chụp).
2. Trình xem ảnh Lightbox toàn màn hình mượt mà với điều hướng Next/Prev (nút bấm + phím mũi tên Left/Right) và Escape.
3. Các thao tác thiết thực:
   - Đặt làm ảnh bìa instance (`Set as Instance Cover`) giúp cá nhân hóa instance.
   - Sao chép tên file / URL ảnh kèm toast.
   - Xóa ảnh an toàn có xác nhận.
   - Tải về (Download) ảnh.
   - Thêm ảnh mới (Upload file ảnh hoặc nút chụp demo F2).
   - Mở thư mục screenshots của instance.
4. Tìm kiếm và sắp xếp ảnh (mới nhất, cũ nhất).
5. Đảm bảo responsive trên mobile (390px/620px), focus-visible ring >= 2px, reduced-motion <= 0.05s, không tràn ngang.

---

## 2. User Stories

### US-1: Xem danh sách ảnh chụp màn hình
Là người chơi Minecraft,
Tôi muốn vào tab "Screenshots" trong chi tiết instance để xem toàn bộ ảnh chụp màn hình đã lưu,
Để tôi dễ dàng duyệt lại các khoảnh khắc trong thế giới game.

### US-2: Phóng to và duyệt ảnh toàn màn hình (Lightbox)
Là người chơi,
Tôi muốn click vào ảnh để mở Lightbox toàn màn hình, có nút Next/Previous và phím mũi tên để xem lần lượt các ảnh,
Để tôi chiêm ngưỡng ảnh chụp ở kích thước lớn nhất mà không bị rối mắt.

### US-3: Đặt ảnh làm ảnh bìa Instance
Là người chơi,
Tôi muốn có nút "Đặt làm ảnh bìa" ngay trên ảnh chụp để biến ảnh screenshot đẹp thành ảnh đại diện của instance trên trang chủ.

### US-4: Quản lý ảnh (Xóa, Sao chép, Tải về, Thêm ảnh)
Là người chơi,
Tôi muốn có thể xóa ảnh hỏng, sao chép tên ảnh, tải ảnh về máy hoặc thêm ảnh từ bên ngoài vào thư mục screenshots.

---

## 3. Tiêu chí nghiệm thu (Acceptance Criteria)

- **SG-AC-01**: Danh sách Screenshots hiển thị dạng lưới thẻ ảnh (Grid) với thumbnail, tiêu đề, tên file, ngày chụp, tag thế giới và độ phân giải.
- **SG-AC-02**: Tìm kiếm ảnh theo tên/tiêu đề/thế giới (`#screenshotSearch`) và sắp xếp theo mới nhất / cũ nhất (`#screenshotSort`).
- **SG-AC-03**: Nhấp vào ảnh hoặc nút Zoom mở Lightbox toàn màn hình (`#lightboxHost`) hiển thị ảnh chất lượng cao, tiêu đề, ngày, thế giới và độ phân giải.
- **SG-AC-04**: Điều hướng trong Lightbox: nút Kế tiếp (Next), Trước đó (Prev), phím mũi tên Trái/Phải để chuyển ảnh tuần tự, và nút đóng / phím Escape để thoát.
- **SG-AC-05**: Hành động trong Lightbox: nút Sao chép tên file / URL ảnh (kèm toast), nút Đặt làm ảnh bìa instance (`Set as Cover`), nút Tải về (Download) và nút Xóa ảnh.
- **SG-AC-06**: Xóa ảnh chụp màn hình yêu cầu dialog xác nhận; xóa thành công loại bỏ ảnh khỏi danh sách và cập nhật bộ đếm.
- **SG-AC-07**: Thêm ảnh chụp màn hình mới (Import ảnh qua input file hoặc kéo thả file ảnh .png/.jpg vào gallery dropzone), hoặc nút "Chụp demo (F2)" tạo ảnh mới tức thì kèm toast.
- **SG-AC-08**: Nút "Mở thư mục screenshots" hiển thị toast thông báo.
- **SG-AC-09**: Responsive (390px, 620px) không tràn ngang; phím Escape đóng Lightbox và dialog; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s.
- **SG-AC-10**: Cảm giác tương tác trực quan, mở/đóng Lightbox mượt mà theo design tokens. (MANUAL_REQUIRED)
- **SG-AC-11**: Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%, 0 `transition: all`. (AUTOMATED)
