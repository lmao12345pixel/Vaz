---
feature: cosmetics-shop
status: WEB_VALIDATED
created: 2026-10-28
updated: 2026-10-28
verifier: web-verifier
---

# Cosmetics & Capes Store Contract

Contract quy định hành vi giao diện cho tính năng Cosmetics & Capes Store trên Web Prototype (`docs/design-preview/`), dùng làm executable specification trước khi port sang WinUI 3 desktop.

## Bảng tiêu chí nghiệm thu (Acceptance Criteria)

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế | Status |
|---|----------------------|---------------|-----------|------------------|--------|
| CS-AC-01 | Danh sách Cosmetics Cards hiển thị đầy đủ icon/thumbnail, tên vật phẩm, danh mục, nhãn độ hiếm (Common/Rare/Epic/Legendary), giá coin (hoặc Miễn phí) và các nút hành động | RUNTIME | Playwright: mở `#page-shop`, assert danh sách thẻ trong `#shopGrid` có đủ thumbnail, tên, category, rarity badge, giá coin | `npx playwright test tests/web-e2e/cosmetics-shop.spec.ts` -> PASS (13 cards render đủ thumb/icon, rarity, category, price, try/buy buttons) | PASS |
| CS-AC-02 | Bộ lọc Category chips (Tất cả, Capes, Wings, Hats, Effects, Bundles) và dropdown Sắp xếp lọc chính xác danh sách và cập nhật bộ đếm `#shopCount` | RUNTIME | Playwright: click lần lượt từng chip category và thay đổi sort, assert số lượng thẻ và nội dung thay đổi tương ứng | `npx playwright test tests/web-e2e/cosmetics-shop.spec.ts` -> PASS (chips All=13, Capes=4, Wings=3; sort Price asc sắp xếp tăng dần; `#shopCount` cập nhật) | PASS |
| CS-AC-03 | Tìm kiếm vật phẩm theo tên/từ khóa (`#shopSearch`), hiển thị empty state khi không tìm thấy kết quả nào | RUNTIME | Playwright: nhập từ khóa tìm kiếm, assert chỉ các thẻ khớp tên hiển thị; nhập từ khóa không tồn tại assert `#shopEmptyState` hiển thị | `npx playwright test tests/web-e2e/cosmetics-shop.spec.ts` -> PASS (query "dragon" -> 1 card; query "xyz999" -> `#shopEmptyState` hiển thị) | PASS |
| CS-AC-04 | Xem thử 3D trực tiếp (Live Try-On): click nút "Thử ngay" trên thẻ mở Modal xem trước `#shopTryOnModal` với mô hình 3D, tên, mô tả và nút thao tác | RUNTIME | Playwright: click nút "Thử ngay" trên thẻ, assert modal hiển thị, canvas 3D khởi tạo, nút Đóng hoặc Escape đóng modal | `npx playwright test tests/web-e2e/cosmetics-shop.spec.ts` -> PASS (Modal `#shopTryOnModalHost` mở, canvas `#shopViewer` khởi tạo, đóng qua Close button & Escape) | PASS |
| CS-AC-05 | Mua vật phẩm (Buy): kiểm tra số dư coins, trừ coins và chuyển trạng thái thẻ sang "Đã sở hữu" kèm toast thông báo; từ chối nếu không đủ coin | RUNTIME | Playwright: click Mua một vật phẩm chưa sở hữu đủ tiền, assert trừ coin, toast thông báo và thẻ đổi thành Đã sở hữu | `npx playwright test tests/web-e2e/cosmetics-shop.spec.ts` -> PASS (Mua Sakura Breeze Cape: 650 -> 500 Coins, toast hiển thị, card chuyển `.is-owned`) | PASS |
| CS-AC-06 | Trang bị / Tháo vật phẩm (Equip / Unequip): vật phẩm đã sở hữu có thể bấm "Trang bị", cập nhật badge "Đang dùng"; bấm "Tháo ra" để gỡ bỏ | RUNTIME | Playwright: click Trang bị trên vật phẩm đã sở hữu, assert xuất hiện badge "Đang dùng"; click Tháo ra assert gỡ bỏ | `npx playwright test tests/web-e2e/cosmetics-shop.spec.ts` -> PASS (Gỡ/dùng Emerald Knight Cape: toggle `.shop-equipped-badge` chính xác) | PASS |
| CS-AC-07 | Điểm danh nhận Coin hàng ngày (`#claimDailyCoinBtn`): bấm nhận thưởng cộng 100 Vaz Coins vào số dư `#shopCoinBalance` kèm toast chúc mừng | RUNTIME | Playwright: click `#claimDailyCoinBtn`, assert số dư coin tăng thêm 100 và toast hiển thị | `npx playwright test tests/web-e2e/cosmetics-shop.spec.ts` -> PASS (650 -> 750 Coins, button disabled với nhãn "Đã nhận hôm nay") | PASS |
| CS-AC-08 | Responsive (390px, 620px) không tràn ngang; phím Escape đóng modal; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s | RUNTIME | Playwright: kiểm tra scrollWidth <= clientWidth tại 390px và 620px; outline focus >= 2px; reduced motion | `npx playwright test tests/web-e2e/cosmetics-shop.spec.ts` -> PASS (scrollWidth <= clientWidth tại 620px và 390px, focus outline >= 2px, reduced motion 0.01ms) | PASS |
| CS-AC-09 | Trải nghiệm trực quan, bố cục thẩm mỹ, tương tác mua/trang bị và preview 3D mượt mà theo design tokens | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, chuyển sang tab Shop, trải nghiệm mua/thử đồ/trang bị, chụp ảnh kiểm chứng | Screenshot: `docs/design-preview/screenshots/evidence-shop-01-main.png`, `evidence-shop-02-tryon-modal.png`, `evidence-shop-03-owned-equipped.png`, `evidence-shop-04-mobile.png`. User xác nhận: "xac nhan vi lam shop demo truoc thoi con cac cai khac lam sau" | PASS |
| CS-AC-10 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%, 0 `transition: all` | AUTOMATED | Chạy `npm run check:static`, `npx playwright test`, grep CSS rule | `npm run check:static` PASS (build 0 error, test 192/192 pass, 12/12 agent pass); 0 `transition: all` | PASS |
