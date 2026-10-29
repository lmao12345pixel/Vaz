---
feature: discover-servers-news
status: WEB_VALIDATED
created: 2026-10-27
updated: 2026-10-27
verifier: web-verifier
---

# Verification Contract: Discover (Server Browser & News Feed)

## Bối cảnh & Mục tiêu
Tính năng Server Browser và News Feed giúp người chơi khám phá máy chủ nhiều người chơi (Multiplayer Servers) và cập nhật tin tức Minecraft/Launcher: xem danh sách trực quan, tìm kiếm/lọc thể loại, kiểm tra ping, lưu yêu thích, thêm/sửa/xóa server, kết nối nhanh (Quick Join) và xem bài viết tin tức chi tiết qua modal reader.

## Verification Contract Table

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế | Status |
|---|----------------------|---------------|-----------|------------------|--------|
| DSN-AC-01 | Danh sách Server Cards hiển thị đầy đủ icon, tên, địa chỉ IP (kèm nút copy), version & loader badge, MOTD Minecraft đa màu, số người chơi online/max và cột sóng ping trực tiếp (ms) | RUNTIME | Playwright: chuyển sang trang Servers, assert các thẻ server hiển thị đầy đủ icon, tên, IP, badge, MOTD đa màu, số người chơi và ping bar | `tests/web-e2e/discover-servers-news.spec.ts`: DSN-AC-01 passed (chromium: 2.8s, mobile-chrome: 4.9s) | PASS |
| DSN-AC-02 | Bộ lọc Category chips (Tất cả, Survival, Mini-games, Creative, Modded) và dropdown lọc (Tất cả, Yêu thích, Online) lọc chính xác danh sách server và cập nhật bộ đếm | RUNTIME | Playwright: click các chip thể loại và đổi filter dropdown, assert danh sách lọc đúng và `#serversCount` cập nhật | `tests/web-e2e/discover-servers-news.spec.ts`: DSN-AC-02 passed (chromium: 4.3s, mobile-chrome: 7.1s) | PASS |
| DSN-AC-03 | Tìm kiếm server theo tên, địa chỉ hoặc phiên bản (`#serverSearch`), hiển thị empty state phù hợp khi không có kết quả | RUNTIME | Playwright: nhập từ khóa tìm kiếm, assert kết quả lọc theo tên/IP/version; nhập từ khóa không khớp assert `#serversEmpty` hiển thị | `tests/web-e2e/discover-servers-news.spec.ts`: DSN-AC-03 passed (chromium: 2.8s, mobile-chrome: 4.8s) | PASS |
| DSN-AC-04 | Thêm server mới (`#addServerBtn`) qua Dialog với tên, địa chỉ, loader; tự động kiểm tra ping và thêm vào danh sách | RUNTIME | Playwright: mở dialog thêm server, nhập thông tin, xác nhận, assert server mới xuất hiện trong danh sách kèm toast | `tests/web-e2e/discover-servers-news.spec.ts`: DSN-AC-04 passed (chromium: 4.8s, mobile-chrome: 6.0s) | PASS |
| DSN-AC-05 | Chỉnh sửa server (Edit) và xóa server (Delete) yêu cầu dialog xác nhận; toggle yêu thích (Favorite) đánh dấu sao và lưu state | RUNTIME | Playwright: mở dialog edit sửa tên, toggle favorite kiểm tra icon đổi trạng thái; click xóa server, xác nhận dialog assert server bị gỡ bỏ | `tests/web-e2e/discover-servers-news.spec.ts`: DSN-AC-05 passed (chromium: 7.2s, mobile-chrome: 8.5s) | PASS |
| DSN-AC-06 | Nút "Chơi ngay" (Join) trên thẻ server kết nối bằng instance đang chọn (hoặc mở instance tương thích), nút "Sao chép IP" copy địa chỉ kèm toast | RUNTIME | Playwright: click Join assert toast kết nối; click copy IP assert toast "Đã sao chép địa chỉ" | `tests/web-e2e/discover-servers-news.spec.ts`: DSN-AC-06 passed (chromium: 2.7s, mobile-chrome: 5.1s) | PASS |
| DSN-AC-07 | News Carousel trên trang chủ hiển thị các slide tin tức với badge danh mục, ngày đăng, tiêu đề; nút Next/Prev và dots điều hướng hoạt động mượt | RUNTIME | Playwright: chuyển slide qua nút Next, Prev và click dots; assert slide active thay đổi và vị trí hiển thị chuẩn xác | `tests/web-e2e/discover-servers-news.spec.ts`: DSN-AC-07 passed (chromium: 3.6s, mobile-chrome: 5.1s) | PASS |
| DSN-AC-08 | Click vào slide tin tức mở Dialog đọc bài viết chi tiết (News Article Reader) với hero banner, nội dung đầy đủ và nút hành động nhanh | RUNTIME | Playwright: click vào slide tin tức, assert dialog đọc bài viết mở với đầy đủ tiêu đề, ngày, nội dung và nút hành động; đóng dialog thành công | `tests/web-e2e/discover-servers-news.spec.ts`: DSN-AC-08 passed (chromium: 3.5s, mobile-chrome: 5.0s) | PASS |
| DSN-AC-09 | Responsive (390px, 620px) không tràn ngang; phím Escape đóng các dialog; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s | RUNTIME | Playwright: đo scrollWidth <= clientWidth tại 390px và 620px; outline focus >= 2px; Escape đóng dialog; reduced motion | `tests/web-e2e/discover-servers-news.spec.ts`: DSN-AC-09 passed (chromium: 3.0s, mobile-chrome: 6.2s) | PASS |
| DSN-AC-10 | Cảm giác tương tác trực quan, chuyển slide carousel và mở dialog mượt mà theo design tokens | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, duyệt trang Servers và News Carousel, thao tác tương tác và xác nhận trực quan | User xác nhận nguyên văn: "xac nhan" (2026-10-27) | PASS |
| DSN-AC-11 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%; CSS không dùng `transition: all` | AUTOMATED | Chạy `npm run check:web` và `npm run check:static` | `npm run check:static` PASS (0 warn/err, 192 tests), 20/20 discover Playwright passed, 0 `transition: all` | PASS |

## Quy tắc Gate Dual-target
1. Prototype Web là executable specification; WinUI implement sau theo contract.
2. Không chuyển sang `WEB_VALIDATED` nếu bất kỳ AC nào chưa PASS (DSN-AC-10 đang BLOCKED chờ user sign-off).
3. Không port WinUI khi contract chưa đạt `WEB_VALIDATED`.

## Developer Report - Web

### Phase
WEB_SPEC → (chờ Web Verifier)

### Verification Contract (cập nhật)
| # | Acceptance criterion | Evidence type | Cách kiểm | Bằng chứng của Developer | Status |
|---|----------------------|---------------|-----------|--------------------------|--------|
| DSN-AC-01 | Hiển thị thẻ Server đầy đủ icon, IP, MOTD đa màu, ping bar | RUNTIME | Playwright `discover-servers-news.spec.ts` | pass (chromium + mobile-chrome) | PASS |
| DSN-AC-02 | Category chips & Filter dropdown | RUNTIME | Playwright `discover-servers-news.spec.ts` | pass (chromium + mobile-chrome) | PASS |
| DSN-AC-03 | Tìm kiếm server và empty state | RUNTIME | Playwright `discover-servers-news.spec.ts` | pass (chromium + mobile-chrome) | PASS |
| DSN-AC-04 | Thêm server qua dialog với ping check | RUNTIME | Playwright `discover-servers-news.spec.ts` | pass (chromium + mobile-chrome) | PASS |
| DSN-AC-05 | Sửa/xóa server xác nhận, toggle favorite | RUNTIME | Playwright `discover-servers-news.spec.ts` | pass (chromium + mobile-chrome) | PASS |
| DSN-AC-06 | Quick Join & Copy IP toast | RUNTIME | Playwright `discover-servers-news.spec.ts` | pass (chromium + mobile-chrome) | PASS |
| DSN-AC-07 | News Carousel navigation, Next/Prev, dots | RUNTIME | Playwright `discover-servers-news.spec.ts` | pass (chromium + mobile-chrome) | PASS |
| DSN-AC-08 | Dialog đọc tin tức chi tiết + action | RUNTIME | Playwright `discover-servers-news.spec.ts` | pass (chromium + mobile-chrome) | PASS |
| DSN-AC-09 | Responsive 390px/620px, Escape, focus ring, reduced motion | RUNTIME | Playwright `discover-servers-news.spec.ts` | pass (chromium + mobile-chrome) | PASS |
| DSN-AC-10 | Tương tác trực quan mượt mà | MANUAL_REQUIRED | Mở UI browser trực tiếp | Chờ user nghiệm thu trực quan | BLOCKED |
| DSN-AC-11 | Check suite & no transition: all | AUTOMATED | `npm run check:static`, `npm run check:web` | `check:static` (192 unit tests pass), `check:web` (196 tests pass) | PASS |

### Tệp thay đổi
| File | Thay đổi |
|------|----------|
| `docs/design-preview/index.html` | Thêm `#serverCategoryChips` toolbar vào trang Servers |
| `docs/design-preview/styles.css` | Styles cho Category chips, server cards, ping bars, news carousel; responsive 390px/620px; focus ring; reduced motion |
| `docs/design-preview/app.js` | Thêm server category filter, ping check trong dialog, Quick Join instance matching, news carousel reader dialog, sync chips |
| `tests/web-e2e/discover-servers-news.spec.ts` | 20 test Playwright e2e kiểm chứng đầy đủ runtime behaviors DSN-AC-01 đến 09 và DSN-AC-11 |

### Kiểm tra đã chạy
| Lệnh | Kết quả | Ghi chú |
|------|---------|---------|
| `node --check docs/design-preview/app.js` | PASS | Cú pháp JS hợp lệ, 0 error |
| `npx playwright test tests/web-e2e/discover-servers-news.spec.ts` | PASS | 20/20 passed (10 chromium + 10 mobile-chrome) |
| `npm run check:static` | PASS | Build 0 warning/0 error, 192 tests pass, 12/12 agent configs pass |
| `npm run check:web` | PASS | 196 passed toàn bộ (14.7m) |

### Runtime còn thiếu
- DSN-AC-10: Cảm giác tương tác trực quan (MANUAL_REQUIRED) cần user mở web trải nghiệm và nghiệm thu.
- AC 01 đến 09 (RUNTIME): Đã hoàn thành code và test Playwright, bàn giao cho Web Verifier thẩm định độc lập.

### Còn lại
- Chờ Web Verifier chạy thẩm định và User xác nhận DSN-AC-10 để hoàn tất chuyển sang `WEB_VALIDATED`.
