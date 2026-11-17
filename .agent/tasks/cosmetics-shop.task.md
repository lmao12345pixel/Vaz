# Task: Cosmetics & Capes Store

- **Feature**: `cosmetics-shop`
- **Contract**: `docs/contracts/cosmetics-shop.contract.md`
- **Feature Doc**: `docs/product/features/cosmetics-shop.feature.md`
- **Phase**: `WEB_VALIDATED`
- **Owner**: Web Team
- **Date**: 2026-10-28

## Acceptance Criteria Checklist
- [x] CS-AC-01: Danh sách Cosmetics Cards hiển thị đầy đủ icon/thumbnail, tên vật phẩm, danh mục, nhãn độ hiếm (Common/Rare/Epic/Legendary), giá coin (hoặc Miễn phí) và các nút hành động (RUNTIME)
- [x] CS-AC-02: Bộ lọc Category chips (Tất cả, Capes, Wings, Hats, Effects, Bundles) và dropdown Sắp xếp lọc chính xác danh sách và cập nhật bộ đếm `#shopCount` (RUNTIME)
- [x] CS-AC-03: Tìm kiếm vật phẩm theo tên/từ khóa (`#shopSearch`), hiển thị empty state khi không tìm thấy kết quả nào (RUNTIME)
- [x] CS-AC-04: Xem thử 3D trực tiếp (Live Try-On): click nút "Thử ngay" trên thẻ mở Modal xem trước `#shopTryOnModal` với mô hình 3D, tên, mô tả và nút thao tác (RUNTIME)
- [x] CS-AC-05: Mua vật phẩm (Buy): kiểm tra số dư coins, trừ coins và chuyển trạng thái thẻ sang "Đã sở hữu" kèm toast thông báo; từ chối nếu không đủ coin (RUNTIME)
- [x] CS-AC-06: Trang bị / Tháo vật phẩm (Equip / Unequip): vật phẩm đã sở hữu có thể bấm "Trang bị", cập nhật badge "Đang dùng"; bấm "Tháo ra" để gỡ bỏ (RUNTIME)
- [x] CS-AC-07: Điểm danh nhận Coin hàng ngày (`#claimDailyCoinBtn`): bấm nhận thưởng cộng 100 Vaz Coins vào số dư `#shopCoinBalance` kèm toast chúc mừng (RUNTIME)
- [x] CS-AC-08: Responsive (390px, 620px) không tràn ngang; phím Escape đóng modal; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s (RUNTIME)
- [x] CS-AC-09: Trải nghiệm trực quan, bố cục thẩm mỹ, tương tác mua/trang bị và preview 3D mượt mà theo design tokens (MANUAL_REQUIRED - User đã xác nhận)
- [x] CS-AC-10: Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%, 0 `transition: all` (AUTOMATED)

## Bằng chứng kiểm thử
1. `npx playwright test tests/web-e2e/cosmetics-shop.spec.ts`: 20/20 test pass (chromium + mobile-chrome).
2. `npm run check:static`: Pass 100% (.NET build 0 error, 192 unit tests pass, 12 agent configs valid).
3. 0 `transition: all` trên toàn bộ styles.
4. Screenshots lưu tại:
   - `docs/design-preview/screenshots/evidence-shop-01-main.png`
   - `docs/design-preview/screenshots/evidence-shop-02-tryon-modal.png`
   - `docs/design-preview/screenshots/evidence-shop-03-owned-equipped.png`
   - `docs/design-preview/screenshots/evidence-shop-04-mobile.png`
