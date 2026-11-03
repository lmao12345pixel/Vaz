---
id: motion-integration
status: WEB_VALIDATED
owner: team/product-owner
surfaces: [web]
web_route: "http://127.0.0.1:5173/"
app_route: "n/a (spec + prototype cho app; port WinUI làm sau WEB_VALIDATED)"
design_tokens: docs/design/tokens.json
updated: 2026-10-26
---

# Contract: Motion Integration (Main Prototype)

## 1. Mục đích

Ghép nối trải nghiệm chuyển động Shared Element đã được kiểm chứng từ `Motion Lab` (`motion-system.contract.md` - `WEB_VALIDATED`) vào giao diện chính (`index.html`) của V.A.Z launcher web prototype:
1. **Shared Element FLIP transition**: Di chuyển mượt mà giữa thẻ instance trong thư viện (`#instanceGrid .instance-card`) và trang chi tiết instance (`#page-detail`).
2. **Direct Clean Launch**: Khởi chạy trực tiếp ngay tại chỗ (loại bỏ popup modal 4 bước theo phản hồi người dùng, tối ưu tốc độ và sự đơn giản).

## 2. Phạm vi

### Trong phạm vi
- `docs/design-preview/index.html`: Cấu trúc icon và tiêu đề tương thích FLIP transition.
- `docs/design-preview/styles.css`: Styling cho shared element animations; tuân thủ tokens, không có `transition: all`.
- `docs/design-preview/app.js`: Tích hợp FLIP transition vào `openDetail()` và `closeDetail()`; giữ `launchInstance()` khởi chạy inline mượt mà, hỗ trợ dừng bằng Stop.
- `tests/web-e2e/motion-integration.spec.ts`: Bộ test Playwright kiểm thử toàn diện MI-AC-01 đến MI-AC-07 và MI-AC-09 trên chromium và mobile-chrome.

### Ngoài phạm vi — TUYỆT ĐỐI KHÔNG SỬA
- Chưa chỉnh sửa C# WinUI desktop (`src/Vaz.WinUI`).
- Không sửa file `motion-lab.html`, `motion-lab.css`, `motion-lab.js`.
- Không đổi selector mà các spec cũ phụ thuộc (`.toast`, `.mod-row`, `.tab`, `#crashAnalyzerBanner`, `#modInspectorHost`).

## 3. Design Tokens áp dụng

| Token | Giá trị | Dùng cho |
|---|---|---|
| `--dur-fast` | 140ms | Icon, status chip, button hover |
| `--dur-base` | 220ms | Reverse FLIP, status chip transition |
| `--dur-slow` | 380ms | FLIP card -> detail entrance |
| `--ease-out` | `cubic-bezier(0.16, 1, 0.3, 1)` | Chuyển động vào (entrance) |
| `--ease-standard` | `cubic-bezier(0.2, 0, 0, 1)` | Chuyển động mặc định |

## 4. Hành vi người dùng thấy

| ID | Hành vi | Kết quả mong đợi |
|----|---------|------------------|
| MI-01 | Click một instance card trong `#instanceGrid` | Card mở ra trang chi tiết `#page-detail`, icon và tên instance di chuyển liên tục từ vị trí card sang header trang chi tiết (FLIP transition) |
| MI-02 | Click nút Back (`#btnBackToInstances`) trong `#page-detail` | Đảo ngược chuyển động: icon và tiêu đề lướt trở về đúng vị trí card ban đầu trong danh sách `#instanceGrid` |
| MI-03 | Bấm nút Play trên instance card | Khởi chạy inline trực tiếp, không xuất hiện popup modal làm gián đoạn |
| MI-04 | Khởi chạy hoàn tất | Instance hiển thị chip trạng thái `Đang chạy`, nút chuyển sang icon `Stop` |
| MI-05 | Bấm nút Stop khi đang chạy | Dừng instance an toàn, chip trạng thái ẩn và nút quay lại icon `Launch` |
| MI-06 | Bật Reduced Motion | FLIP transition chạy tức thì (duration <= 0.05s), không gây chóng mặt |
| MI-07 | Thu nhỏ viewport về 390px / 620px | Màn hình không tràn ngang (`scrollWidth <= clientWidth + 1`) |

## 5. Acceptance criteria và bằng chứng

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế (Web Verifier, 2026-10-26) | Status |
|---|----------------------|---------------|-----------|---------------------------------------------|--------|
| MI-AC-01 | Click instance card trong #instanceGrid thực hiện FLIP transition (icon + title) vào header #page-detail, #page-detail hiển thị ngay | `RUNTIME` | Playwright: click instance card, đo vị trí boundingBox icon/title trước/sau, assert #page-detail visible | `tests/web-e2e/motion-integration.spec.ts:17` PASS (chromium 2.8s, mobile-chrome 4.7s). BoundingBox icon và title di chuyển mượt mà từ card sang header detail, #page-detail hiển thị ngay, #page-instances ẩn | PASS |
| MI-AC-02 | Click nút Back (#btnBackToInstances) thực hiện reverse FLIP transition về đúng vị trí card ban đầu, #page-instances hiển thị | `RUNTIME` | Playwright: click #btnBackToInstances, assert #page-instances visible, đo boundingBox quay về card | `tests/web-e2e/motion-integration.spec.ts:48` PASS (chromium 2.8s, mobile-chrome 5.9s). Reverse FLIP đưa icon và tiêu đề trở lại vị trí ban đầu trong card, #page-instances hiển thị, #page-detail ẩn | PASS |
| MI-AC-03 | Khởi chạy instance trực tiếp không popup modal, hiển thị trạng thái inline mượt mà | `RUNTIME` | Playwright: click launch button, assert không có modal che khuất | `tests/web-e2e/motion-integration.spec.ts:66` PASS (chromium 3.4s, mobile-chrome 5.0s). Bấm Play khởi chạy inline ngay, không có modal popup che khuất | PASS |
| MI-AC-04 | Khởi chạy hoàn tất đánh dấu running (Đang chạy), nút đổi sang Stop | `RUNTIME` | Playwright: assert card hiển thị chip `Đang chạy` và nút đổi title sang `Stop` | `tests/web-e2e/motion-integration.spec.ts:78` PASS (chromium 3.6s, mobile-chrome 5.4s). Khởi chạy hoàn tất hiện chip `Đang chạy`, nút đổi title `Stop` | PASS |
| MI-AC-05 | Click Stop dừng instance an toàn và trả về trạng thái bình thường | `RUNTIME` | Playwright: click Stop, assert chip running biến mất, nút đổi về `Launch` | `tests/web-e2e/motion-integration.spec.ts:92` PASS (chromium 4.4s, mobile-chrome 5.7s). Click Stop dừng instance an toàn, chip running biến mất, nút đổi về `Launch` | PASS |
| MI-AC-06 | Reduce motion: FLIP transition chạy tức thì, duration <= 0.05s | `RUNTIME` | Playwright: bật reduced motion, assert durations <= 0.05s | `tests/web-e2e/motion-integration.spec.ts:106` PASS (chromium 2.3s, mobile-chrome 3.7s). Bật reduce motion: animation/transition duration <= 0.05s, không bị layout jump | PASS |
| MI-AC-07 | Responsive (390px, 620px) FLIP và navigation không tràn ngang | `RUNTIME` | Playwright: đo scrollWidth <= clientWidth + 1 ở 390px và 620px | `tests/web-e2e/motion-integration.spec.ts:122` PASS (chromium 5.7s, mobile-chrome 7.6s). Ở cả 390px và 620px: scrollWidth <= clientWidth + 1 | PASS |
| MI-AC-08 | Chuyển động FLIP mượt mà, đúng nhịp độ design tokens | `MANUAL_REQUIRED` | User mở `http://127.0.0.1:5173/`, click card mở detail, bấm Back và xác nhận nguyên văn | User xác nhận nguyên văn: "xac mhan" (2026-10-26) | PASS |
| MI-AC-09 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100% | `AUTOMATED` | Chạy `npm run check:web` và `npm run check:static` | `npm run check:static` pass (build net8.0 0 error/0 warning, 192/192 tests pass, 12/12 agent configs pass). `styles.css` 0 matches `transition: all` | PASS |

## 6. Ràng buộc không hồi quy

- Mọi test suite hiện có: `website.spec.ts` (24 tests), `vaz.spec.ts` (6 tests), `crash-analyzer.spec.ts` (12 tests), `prototype-polish.spec.ts` (18 tests), `motion-lab.spec.ts` (18 tests), `mod-manager.spec.ts` (14 tests) phải tiếp tục PASS 100%.
- Không dùng `transition: all` hoặc `transition-property: all` trong `styles.css`.
- Không hardcode secret/token; không cài thêm thư viện npm nào ngoài vanilla JS/CSS.

## 7. Verification log (Web Verifier — 2026-10-26)

Môi trường: Playwright 1.49.0 / Node v20, 2 browser projects `chromium` + `mobile-chrome`, web server `tests/web-e2e/serve.mjs` tại `http://127.0.0.1:5173/`.

| Lệnh | Kết quả | Output tóm tắt |
|------|---------|----------------|
| `npm run check:static` | PASS | Build 0 error/0 warning, 192/192 test pass, 12/12 agent config pass |
| `npx playwright test tests/web-e2e/motion-integration.spec.ts` | PASS | 16/16 passed (8 test cases × 2 browser profiles, 1.2m) |
| Grep `styles.css` pattern `transition(-property)?: .*all` | PASS | 0 match |

## 8. Kết luận & Phase

- **Phase**: `WEB_VALIDATED`.
- **Lý do**: Toàn bộ 9 criteria (MI-AC-01 đến MI-AC-09) đều PASS: 8 criteria AUTOMATED/RUNTIME có Playwright runtime evidence thật, và MI-AC-08 (`MANUAL_REQUIRED`) đã có xác nhận nguyên văn từ người dùng ("xac mhan" ngày 2026-10-26).
- **Trạng thái**: Đã sẵn sàng làm spec chuẩn cho phase port WinUI app nếu cần.
