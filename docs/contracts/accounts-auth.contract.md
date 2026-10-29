---
feature: accounts-auth
status: WEB_VALIDATED
last_updated: 2026-10-27
contract_version: 1.0.0
---

# Verification Contract: Accounts & Microsoft Device Code Authentication

## 1. Bối cảnh & Mục tiêu
Tính năng quản lý tài khoản người chơi nâng cao tại `#page-accounts` trong web prototype:
- Đăng nhập tài khoản Microsoft chuẩn Device Code Authentication (mã kết nối, sao chép 1-click, đếm ngược, polling trạng thái).
- Tạo tài khoản Offline / Local có validation tên Minecraft (3-16 ký tự hợp lệ).
- Thẻ tài khoản chi tiết: avatar face, badge phân loại (Microsoft / Offline), UUID rút gọn, trạng thái token/session, cape selector.
- Đổi tài khoản active nhanh chóng và đồng bộ thời gian thực sang bottom launcher bar (`#userBtn`).
- Gia hạn phiên (Refresh token) và xóa tài khoản an toàn kèm dialog xác nhận.

## 2. Tiêu chí nghiệm thu (Acceptance Criteria)

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế | Status |
|---|----------------------|---------------|-----------|------------------|--------|
| AUTH-AC-01 | Click "Add Microsoft" mở Modal Device Code Flow `#msAuthModalHost` với đầy đủ mã kết nối, nút sao chép 1-click, timer đếm ngược và link truy cập | RUNTIME | Playwright: click `#addMicrosoftBtn`, assert `#msAuthModalHost` visible, kiểm tra mã user code và timer đếm ngược | `npx playwright test tests/web-e2e/accounts-auth.spec.ts` (test 1, 9) pass trên Chromium (3.1s) & Mobile Chrome (5.3s) | PASS |
| AUTH-AC-02 | Sao chép mã Device Code hiển thị toast phản hồi; hoàn tất xác thực (hoặc nút mô phỏng) thêm tài khoản Microsoft mới và đóng modal | RUNTIME | Playwright: click nút sao chép mã (assert toast), click hoàn tất xác thực -> assert modal đóng và tài khoản mới xuất hiện | `npx playwright test tests/web-e2e/accounts-auth.spec.ts` (test 2, 10) pass trên Chromium (4.3s) & Mobile Chrome (5.7s) | PASS |
| AUTH-AC-03 | Click "Add Offline" mở dialog form tạo tài khoản với validation tên Minecraft (3-16 ký tự hợp lệ, báo lỗi nếu trùng hoặc sai định dạng) | RUNTIME | Playwright: click `#addOfflineBtn`, thử nhập tên sai (1 ký tự / chứa ký tự đặc biệt) assert lỗi validation; nhập tên đúng tạo tài khoản thành công | `npx playwright test tests/web-e2e/accounts-auth.spec.ts` (test 3, 11) pass trên Chromium (6.3s) & Mobile Chrome (6.4s) | PASS |
| AUTH-AC-04 | Thẻ tài khoản hiển thị đầy đủ avatar face, badge loại tài khoản (Microsoft / Offline), UUID, token status và Cape chip | RUNTIME | Playwright: assert cấu trúc thẻ tài khoản `.account-card` có đủ avatar, badge loại, UUID rút gọn và trạng thái token | `npx playwright test tests/web-e2e/accounts-auth.spec.ts` (test 4, 12) pass trên Chromium (2.6s) & Mobile Chrome (4.0s) | PASS |
| AUTH-AC-05 | Chuyển đổi tài khoản Active (`Set as Active`) cập nhật ngay lập tức badge trên thẻ và đồng bộ tên/avatar lên nút `#userBtn` ở bottom bar | RUNTIME | Playwright: click "Set active" trên tài khoản thứ hai, assert badge active đổi vị trí và `#userBtn` hiển thị đúng tên tài khoản mới | `npx playwright test tests/web-e2e/accounts-auth.spec.ts` (test 5, 13) pass trên Chromium (3.5s) & Mobile Chrome (4.5s) | PASS |
| AUTH-AC-06 | Nút "Refresh token" trên tài khoản Microsoft mô phỏng làm mới token thành công và cập nhật thời gian hiệu lực | RUNTIME | Playwright: click nút refresh token trên tài khoản Microsoft, assert toast thông báo và trạng thái token cập nhật | `npx playwright test tests/web-e2e/accounts-auth.spec.ts` (test 6, 14) pass trên Chromium (4.9s) & Mobile Chrome (4.5s) | PASS |
| AUTH-AC-07 | Xóa tài khoản yêu cầu xác nhận; nếu xóa tài khoản đang active thì tự động chuyển active sang tài khoản kế tiếp | RUNTIME | Playwright: xóa tài khoản active, xác nhận dialog -> assert tài khoản bị xóa và tài khoản khác tự động thành active | `npx playwright test tests/web-e2e/accounts-auth.spec.ts` (test 7, 15) pass trên Chromium (4.3s) & Mobile Chrome (5.3s) | PASS |
| AUTH-AC-08 | Responsive (390px, 620px) không tràn ngang; phím Escape đóng các modal; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s | RUNTIME | Playwright: set viewport 390px/620px kiểm tra scrollWidth, Escape đóng modal, outline focus >= 2px, reduced motion | `npx playwright test tests/web-e2e/accounts-auth.spec.ts` (test 8, 16) pass trên Chromium (3.5s) & Mobile Chrome (6.4s) | PASS |
| AUTH-AC-09 | Cảm giác chuyển động mượt mà khi mở/đóng modal xác thực và chuyển đổi tài khoản theo design tokens | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, chuyển sang tab Tài khoản, mở modal đăng nhập Microsoft và đổi active account | User xác nhận nguyên văn: "xac nhan" (2026-10-27) dựa trên ảnh chụp thực tế evidence-accounts-01, 02, 03 | PASS |
| AUTH-AC-10 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100% | AUTOMATED | Chạy `npm run check:web` và `npm run check:static` | `npm run check:static` pass (0 err/warn, 192 unit tests, 12 agent configs pass). `npm run check:web` pass 140/140 tests. | PASS |

## 3. Ràng buộc kỹ thuật & Kiến trúc
1. Không sửa đổi mã nguồn WinUI (`src/Vaz.WinUI`) hay Core (`src/Vazer.Core`).
2. CSS tuân thủ design tokens (`--dur-*`, `--ease-*`, `--accent`, `--bg-*`), tuyệt đối không dùng `transition: all` hay `transition-property: all`.
3. Không hardcode credentials thật; toàn bộ flow Device Code sử dụng giả lập an toàn và có thể reset/reload.
