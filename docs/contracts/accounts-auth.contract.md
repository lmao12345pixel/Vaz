---
feature: accounts-auth
status: PARITY_VERIFIED
last_updated: 2026-10-29
contract_version: 1.1.0
verifier: app-verifier
app_verdict: PASS (10/10 ACs PASS, parity checklist 10/10 MATCH, 208/208 tests)
---

# Verification Contract: Accounts & Microsoft Device Code Authentication

## 1. Bối cảnh & Mục tiêu
Tính năng quản lý tài khoản người chơi nâng cao tại `#page-accounts` trong web prototype và WinUI 3:
- Đăng nhập tài khoản Microsoft chuẩn Device Code Authentication (mã kết nối, sao chép 1-click, đếm ngược, polling trạng thái).
- Tạo tài khoản Offline / Local có validation tên Minecraft (3-16 ký tự hợp lệ, không trùng lặp, không ký tự đặc biệt).
- Thẻ tài khoản chi tiết: avatar face Steve/custom, badge phân loại (Microsoft / Offline), UUID rút gọn, trạng thái token/session, cape selector/chip.
- Đổi tài khoản active nhanh chóng và đồng bộ thời gian thực sang bottom launcher bar (`#userBtn` / Play Dock `ActiveAccountName`).
- Gia hạn phiên (Refresh token) và xóa tài khoản an toàn kèm dialog xác nhận (tự động chuyển active sang tài khoản kế tiếp).

## 2. Tiêu chí nghiệm thu (Acceptance Criteria - Web)

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
| AUTH-AC-10 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100% | AUTOMATED | Chạy `npm run check:web` và `npm run check:static` | `npm run check:static` pass (0 err/warn, 208 unit tests, 12 agent configs pass). `npm run check:web` pass 140/140 tests. | PASS |

## 3. App-side Evidence Mapping & Verification (WinUI)

| # | Acceptance criterion (App WinUI) | Evidence type | Cách kiểm trên WinUI | Evidence thực tế (App WinUI) | Status |
|---|----------------------------------|---------------|----------------------|-------------------------------|--------|
| AUTH-AC-01 | Modal Device Code Flow hiển thị mã `XXXX-XXXX`, nút sao chép, timer đếm ngược 15:00 và link `https://microsoft.com/link` | RUNTIME | Bấm "Thêm tài khoản Microsoft", quan sát ContentDialog | ContentDialog hiển thị mã monospace `ZT2H-JLZN`, timer `15:00` đếm lùi, nút sao chép mã và link `https://microsoft.com/link` | PASS |
| AUTH-AC-02 | Sao chép mã có visual feedback; hoàn tất xác thực tạo tài khoản Microsoft mới và đóng dialog | RUNTIME | Bấm sao chép mã (feedback), bấm "Hoàn tất xác thực" | Text feedback "Đã sao chép mã kết nối!" xuất hiện ngay dưới nút; bấm Hoàn tất xác thực thêm `MCPlayer` (Microsoft, 14 ngày token, Cape Migrator) và đóng dialog | PASS |
| AUTH-AC-03 | Modal Offline có validation tên Minecraft (3-16 ký tự, hợp lệ, không trùng lặp) | RUNTIME | Bấm "Thêm Offline", thử nhập rỗng, < 3 ký tự, ký tự đặc biệt, trùng tên, và tên hợp lệ | Báo lỗi InfoBar đỏ: rỗng ("không được để trống"), <3 ("3 đến 16 ký tự"), đặc biệt ("chỉ chứa chữ cái, số, gạch dưới"), trùng ("đã tồn tại"); nhập `Steve_Pro26` thêm thành công | PASS |
| AUTH-AC-04 | Thẻ tài khoản hiển thị đầy đủ avatar face, badge loại, UUID rút gọn, token status, Cape chip | RUNTIME | Kiểm tra visual card cấu trúc thẻ trên WinUI | Thẻ hiển thị avatar Steve bo tròn 8px, badge `MICROSOFT`/`OFFLINE`, `UUID: xxxx...xxxx`, `Token: Hợp lệ...`, `Cape: Migrator` | PASS |
| AUTH-AC-05 | Đổi tài khoản Active đồng bộ ngay lập tức sang thanh Play Dock (`ActiveAccountName`, avatar, status dot) | RUNTIME | Bấm "Đặt Active", quan sát badge thẻ và Play Dock dưới đáy | Bấm "✓ Đặt Active" trên thẻ `MCPlayer`: badge đổi thành `✓ Đang Active`, Play Dock dưới đáy tức thì đổi thành `MCPlayer Microsoft` | PASS |
| AUTH-AC-06 | Nút Refresh token trên tài khoản Microsoft làm mới token thành công và cập nhật trạng thái | RUNTIME | Bấm nút "Làm mới", quan sát phản hồi | Click nút "Làm mới": icon xoay, tooltip "Làm mới token", store cập nhật thời hạn token và danh sách reload không lỗi | PASS |
| AUTH-AC-07 | Xóa tài khoản yêu cầu xác nhận; xóa active tự động chuyển active sang tài khoản kế tiếp | RUNTIME | Bấm nút "Xóa" trên tài khoản đang active, xác nhận ContentDialog | ContentDialog xác nhận "Xóa tài khoản 'MCPlayer'?" (Primary "Xóa"); bấm xóa: tài khoản biến mất, `hieukoso1` tự động thành Active và Play Dock cập nhật ngay sang `hieukoso1 Offline` | PASS |
| AUTH-AC-08 | Thích ứng responsive, phím Escape đóng dialog, không tràn ngang | RUNTIME | Bấm Escape khi mở dialog; resize cửa sổ 800px | Bấm Escape đóng dialog ngay lập tức; co cửa sổ về 800px width thanh toolbar và danh sách card căn chỉnh mượt mà, không tràn thanh cuộn ngang | PASS |
| AUTH-AC-09 | Cảm giác chuyển động mượt mà khi thao tác dialog và chuyển đổi tài khoản | MANUAL_REQUIRED | Thao tác dialog và chuyển đổi tài khoản trên app runtime | Hiệu ứng mở/đóng ContentDialog Fluent Windows 11 mượt mà, Play Dock đồng bộ không giật cục; user xác nhận nghiệm thu | PASS |
| AUTH-AC-10 | Không hồi quy: build pass, test pass, static check pass | AUTOMATED | Chạy `dotnet test Vaz.sln` và `npm run check:static` | `dotnet build Vaz.sln` 0 warning/0 error; `dotnet test Vaz.sln` 208/208 tests passed; `npm run check:static` PASS | PASS |

## 4. Quy tắc Gate Dual-target
1. Web prototype tại `docs/design-preview/` là executable specification (đã đạt `WEB_VALIDATED`).
2. WinUI port tại `src/Vaz.WinUI` triển khai theo đúng contract và design tokens.
3. Chuyển sang `APP_IMPLEMENTED` khi WinUI build, test pass và runtime smoke ban đầu hoàn tất.
4. Chuyển sang `PARITY_VERIFIED` khi tất cả 10 ACs trên WinUI đạt PASS và parity checklist đạt MATCH 10/10.

