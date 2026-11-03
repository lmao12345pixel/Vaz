# Feature: Cosmetics & Capes Store (Cửa hàng Vật phẩm & Ngoại hình)

## 1. Mục tiêu & Bối cảnh
Trang Cosmetics & Capes Store (`#page-shop`) là trung tâm mua sắm và tùy biến ngoại hình cho người chơi trong V.A.Z Launcher:
- Khám phá bộ sưu tập vật phẩm ngoại hình độc quyền: Áo choàng hiệu ứng (Capes), Cánh rồng/thiên thần (Wings), Mũ vương miện/khăn bandana (Hats), Hiệu ứng bước chân/hào quang (Aura/Trails), và Gói combo (Bundles).
- Hệ thống độ hiếm tiêu chuẩn: Phổ thông (Common), Hiếm (Rare), Sử thi (Epic), Huyền thoại (Legendary).
- Quản lý số dư Vaz Coins (`#shopCoinBalance`), hỗ trợ mua vật phẩm hoặc nhận coin miễn phí hàng ngày.
- Xem trước trực tiếp trên mô hình 3D (3D Live Try-On Modal) cho phép người chơi xoay quan sát vật phẩm gắn lên nhân vật trước khi mua.
- Trang bị (Equip) hoặc Tháo (Unequip) ngay sau khi sở hữu, đồng bộ trạng thái sử dụng vào nhân vật.

## 2. Phạm vi (Scope)
- **Giao diện**:
  - Thanh tiêu đề trang với bộ đếm vật phẩm `#shopCount` và widget số dư Coins `#shopCoinBalanceBox` kèm nút Nhận thưởng hằng ngày `#claimDailyCoinBtn`.
  - Thanh công cụ bộ lọc danh mục chips `#shopCategoryChips` (Tất cả, Capes, Wings, Hats, Effects, Bundles) và dropdown sắp xếp `#shopSortSelect`.
  - Ô tìm kiếm `#shopSearch`.
  - Lưới vật phẩm `#shopGrid`: hiển thị thumbnail, huy hiệu độ hiếm, tên, danh mục, giá (Coins hoặc Miễn phí), nhãn trạng thái ("Đang dùng", "Đã sở hữu"), nút "Thử ngay" và nút "Mua" / "Trang bị".
  - Modal xem thử 3D `#shopTryOnModal` với canvas 3D xoay 360°, chi tiết vật phẩm và nút hành động nhanh.
- **Tương tác**:
  - Mua vật phẩm: kiểm tra đủ coin, trừ tiền, cập nhật trạng thái đã sở hữu, lưu state vào localStorage.
  - Trang bị / Tháo: chuyển đổi trạng thái đang dùng, đồng bộ vào state.
  - Điểm danh nhận coin: cộng 100 Vaz Coins một lần mỗi phiên hoặc đếm ngược.
  - Responsive 390px, 620px không tràn ngang, phím Escape đóng modal, focus outline >= 2px.

## 3. User Stories
- **US-1**: Là người chơi, tôi muốn duyệt các mẫu áo choàng, cánh và mũ độc quyền để tùy biến diện mạo độc đáo trong game.
- **US-2**: Là người chơi, tôi muốn xem trước vật phẩm gắn lên nhân vật 3D để biết trước vẻ ngoài trước khi mua.
- **US-3**: Là người chơi, tôi muốn mua và trang bị vật phẩm bằng số dư coin tích lũy được.
- **US-4**: Là người chơi, tôi muốn tìm kiếm và lọc vật phẩm theo danh mục hoặc độ hiếm để dễ dàng chọn lựa.
