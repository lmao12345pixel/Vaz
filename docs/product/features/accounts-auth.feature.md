# Feature: Accounts & Microsoft Device Code Authentication (Web Prototype)

## Bối cảnh & Mục tiêu
Tính năng quản lý tài khoản người chơi tại `#page-accounts` trong launcher:
1. Hỗ trợ đăng nhập Microsoft chuẩn Device Code Flow:
   - Hiển thị mã ghép nối (User Code) rõ ràng, hỗ trợ sao chép 1-click.
   - Link mở trang `https://microsoft.com/link`.
   - Trạng thái đếm ngược (countdown timer) và vòng lặp chờ xác thực (polling simulation).
   - Xử lý các tình huống: xác thực thành công, hủy bỏ, hết hạn mã.
2. Thêm tài khoản Offline / Non-premium:
   - Form nhập tên người chơi với bộ quy chuẩn Minecraft username (3-16 ký tự, a-z, 0-9, _).
   - Tùy chọn model skin Steve / Alex ban đầu.
3. Thẻ tài khoản (Account Card) đa năng:
   - Avatar face sắc nét, badge phân biệt Microsoft vs Offline.
   - UUID rút gọn, trạng thái phiên đăng nhập (Session/Token status).
   - Hiển thị và chuyển đổi nhanh Áo choàng (Cape).
   - 1-click Set Active, Refresh token gia hạn phiên, và Xóa tài khoản an toàn (kèm xác nhận).
4. Đồng bộ toàn launcher:
   - Đồng bộ ngay lập tức sang thanh điều khiển đáy (`#userBtn`), trang Skins và Instances.

## User Stories
- Là người chơi có bản quyền, tôi muốn đăng nhập Microsoft bằng mã Device Code để không phải nhập mật khẩu trực tiếp vào launcher.
- Là người chơi offline, tôi muốn tạo tài khoản cục bộ nhanh chóng với tên hợp lệ để bắt đầu chơi ngay.
- Là người dùng nhiều tài khoản, tôi muốn dễ dàng chuyển đổi tài khoản active và thấy giao diện thanh điều khiển dưới đáy cập nhật tức thì.

## Tiêu chí thiết kế
- Tuân thủ design tokens (`--dur-base`, `--dur-fast`, `--ease-out`, `--accent`, `--bg-*`).
- Tuyệt đối không dùng `transition: all` hoặc `transition-property: all`.
- Responsive hoàn hảo ở 390px và 620px, không tràn ngang (`scrollWidth <= clientWidth + 1`).
- Đầy đủ trạng thái `:focus-visible` (ring >= 2px, offset >= 1px) cho mọi nút bấm và input.
