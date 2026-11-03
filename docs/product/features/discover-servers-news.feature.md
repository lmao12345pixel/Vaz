# Feature: Khám phá Máy chủ & Bảng tin tức (Discover: Server Browser & News Feed)

## Bối cảnh & Mục tiêu
Tính năng Khám phá kết nối người chơi Minecraft với cộng đồng nhiều người chơi (Multiplayer Servers) và bảng tin tức cập nhật mới nhất (Minecraft & Launcher News).
Trang Máy chủ (`#page-servers`) giúp người chơi dễ dàng tìm kiếm, lọc theo thể loại (Survival, Minigames, Creative, Modded), xem trạng thái ping thời gian thực, quản lý server yêu thích và kết nối nhanh (1-click Join).
News Carousel trên trang chủ cung cấp các tin tức chính thức từ Mojang và bản cập nhật Vazer Launcher, cho phép người chơi đọc bài viết chi tiết và tương tác nhanh.

## User Stories
1. **Duyệt & Tìm kiếm Server**: Là người chơi, tôi muốn tìm kiếm server theo tên, địa chỉ IP hoặc phiên bản Minecraft và lọc nhanh bằng category chips để tìm được máy chủ phù hợp sở thích.
2. **Trạng thái & MOTD Trực quan**: Là người chơi, tôi muốn nhìn thấy cột sóng ping trực tiếp (ms), số lượng người chơi và MOTD đa sắc màu để biết máy chủ có đang hoạt động tốt hay không.
3. **Quản lý Server Cá nhân**: Là người chơi, tôi muốn thêm server mới (kèm kiểm tra ping tự động), sửa thông tin, xóa server và đánh dấu yêu thích (Favorite).
4. **Vào chơi nhanh (Quick Join)**: Là người chơi, tôi muốn bấm "Chơi ngay" trên thẻ server để kết nối trực tiếp bằng instance đang chọn, hoặc sao chép địa chỉ IP chỉ với một cú nhấp chuột.
5. **Duyệt Tin tức & Đọc chi tiết**: Là người chơi, tôi muốn xem các tin tức nổi bật trên banner carousel trang chủ với chuyển động mượt mà, và click vào để mở cửa sổ đọc bài viết chi tiết kèm các liên kết hành động tiện ích.

## Phạm vi (Scope)
- **Thuộc phạm vi**:
  - Giao diện danh sách Server Cards với MOTD Minecraft parser, cột sóng ping 5 vạch màu, badge version/loader, avatar icon.
  - Category chips lọc thể loại server: Tất cả, Sinh tồn (Survival), Trò chơi nhỏ (Mini-games), Sáng tạo (Creative), Modpack (Modded).
  - Tìm kiếm server thời gian thực qua `#serverSearch`.
  - Dialog Thêm Server mới (`#addServerBtn`) và Sửa Server với kiểm tra địa chỉ và ping.
  - Dialog Xóa Server an toàn.
  - Toggle Yêu thích (Favorite) lưu vào state và localStorage.
  - Nút "Chơi ngay" (Join) và "Sao chép IP" kèm toast thông báo.
  - News Carousel trên trang chủ: chuyển slide Next/Prev, dots chuyển slide, pause on hover.
  - Modal Đọc tin tức chi tiết (News Article Reader) với hero banner, nội dung định dạng và nút điều hướng liên quan.
  - Responsive 390px và 620px không tràn ngang, focus-visible ring >= 2px, reduced-motion <= 0.05s, không dùng `transition: all`.
- **Ngoài phạm vi**:
  - Không kết nối socket TCP thật tới server Minecraft ngoài Internet (dùng mô phỏng ping/status hợp lý trong môi trường prototype).
  - Không sửa đổi mã nguồn WinUI (`src/Vaz.WinUI`) hoặc Core (`src/Vazer.Core`).

## Tiêu chí chấp nhận (Acceptance Criteria)
Xem chi tiết tại `docs/contracts/discover-servers-news.contract.md`.
