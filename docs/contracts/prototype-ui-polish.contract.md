---
id: prototype-ui-polish
status: WEB_VALIDATED
owner: team/product-owner
surfaces: [web]
web_route: "http://127.0.0.1:5173/ (docs/design-preview/)"
app_route: "n/a (chỉ là prototype web, không phải contract port WinUI)"
design_tokens: docs/design/tokens.json
updated: 2026-10-26
---

# Contract: App Prototype UI/UX Animation & Cleanup

## 1. Mục đích

Tối ưu chuyển động (motion) và dọn dẹp UI/UX cho **web prototype mô phỏng app** tại
`docs/design-preview/` (phục vụ tại `http://127.0.0.1:5173/`), để dùng prototype làm
bản demo UI/UX của app trên web trước khi port WinUI. Mục tiêu: chuyển động mượt,
nhất quán, không giật, không layout shift, tôn trọng giảm chuyển động, focus bàn phím
rõ ràng — và **không đổi hành vi nghiệp vụ** nào.

Đây là **web-only**: không đụng `src/Vaz.WinUI/`, `src/Vazer.Core/`, `website/`.

## 2. Phạm vi

### Trong phạm vi

- `docs/design-preview/styles.css`: token chuyển động dùng chung, thay toàn bộ
  `transition: all` bằng danh sách property tường minh, chuẩn hoá duration/easing,
  hover/press/focus states, keyframes, `@media (prefers-reduced-motion: reduce)`.
- `docs/design-preview/app.js`: chỉ phần liên quan chuyển động/UX (dialog, tab, toast,
  sidebar mobile, tôn trọng reduced motion). Không đổi logic nghiệp vụ, không đổi tên
  hàm public, không đổi selector mà Playwright phụ thuộc.
- `docs/design-preview/index.html`: chỉ thêm thuộc tính `aria-*` / class trạng thái nếu
  cần; **không** đổi cấu trúc, id, class hiện có.
- `tests/web-e2e/prototype-polish.spec.ts` (mới): test runtime cho các AC bên dưới.

### Ngoài phạm vi

- `website/` (landing page) — đã đóng ở contract `website-ui-polish` (`WEB_VALIDATED`).
- `src/Vaz.WinUI/`, `src/Vazer.Core/`.
- Hành vi nghiệp vụ của Crash Analyzer, Modpack Importer, Settings, Skins, Worlds.
  Các contract tương ứng **không** được đổi trạng thái bởi task này.
- Thêm/bớt màn hình, đổi copy, đổi bố cục responsive hiện có.
- Thư viện animation bên thứ ba.

## 3. Token chuyển động (prototype-local, map từ `docs/design/tokens.json`)

| Token | Giá trị | Dùng cho |
|-------|---------|----------|
| `--dur-fast` | `140ms` | hover màu, viền, icon, ghost button |
| `--dur-base` | `220ms` | card lift, tab, row hover, toast |
| `--dur-slow` | `380ms` | dialog/sheet vào màn hình |
| `--ease-standard` | `cubic-bezier(0.2, 0, 0, 1)` | mặc định |
| `--ease-out` | `cubic-bezier(0.16, 1, 0.3, 1)` | phần tử xuất hiện |
| `--ease-spring` | `cubic-bezier(0.34, 1.56, 0.64, 1)` | nhấn nút, toggle, badge |

Giữ alias `--ease-out`, `--ease-in-out`, `--ease-spring` hiện có (đã tồn tại ở
`styles.css` dòng 47–49) để không vỡ style cũ. Thời lượng hiện đang rải rác
`0.15s / 0.16s / 0.18s / 0.2s / 0.25s` phải quy về token.

## 4. Hành vi người dùng thấy

| ID | Hành vi | Kết quả mong đợi |
|----|---------|------------------|
| POL-01 | Hover/focus phần tử tương tác (nav item, button, card, tab, row) | Chỉ animate property được khai báo; không animate `all`; không nhấp nháy, không đổi layout |
| POL-02 | Mở dialog xác nhận | Dialog hiện với opacity + scale mượt từ vị trí con trỏ; nền mờ dần; không layout shift |
| POL-03 | Đóng dialog | Dialog thu nhỏ/mờ dần rồi `#dialogHost` ẩn hẳn; không còn `is-closing` treo |
| POL-04 | Chuyển tab trong instance detail (Mods/Worlds/Screenshots/Logs/Settings) | Nội dung đổi ngay, không nháy trắng, không animate toàn bộ trang |
| POL-05 | Toast xuất hiện rồi tự tắt | Trượt/nhạt vào, tồn tại ~1.9s, `is-out` rồi remove; nhiều toast không chồng đè lên nhau |
| POL-06 | Điều hướng sidebar trên viewport hẹp | Sidebar mở/đóng mượt, khóa scroll body, đóng bằng Escape/click ngoài/chọn mục |
| POL-07 | Tab qua control bằng bàn phím | Mọi control có vòng `:focus-visible` ≥2px, offset ≥1px, tương phản rõ trên cả theme sáng/tối |
| POL-08 | Bật `Giảm chuyển động` trong Settings hoặc OS `prefers-reduced-motion: reduce` | Animation/transition gần như tắt ngay (≤ 0.05s), ripple không tạo, reveal/entrance hiện tức thì |
| POL-09 | Viewport 620px và 390px | Không tràn ngang; nội dung chính đọc được; sidebar dùng được |
| POL-10 | Cảm giác chuyển động khi dùng thật (hover, mở/đóng dialog, đổi tab, toast) | Mượt, không giật, không khựng |

## 5. Acceptance criteria và bằng chứng

| # | Acceptance criterion | Evidence type | Cách kiểm | Bằng chứng bắt buộc | Evidence thực tế (Web Verifier, 2026-10-26) | Status |
|---|----------------------|---------------|-----------|---------------------|---------------------------------------------|--------|
| POL-AC-01 | Token chuyển động `--dur-*` + `--ease-*` tồn tại trong `:root` và được dùng thay số literal | AUTOMATED | `Select-String -Path docs/design-preview/styles.css -Pattern '--dur-fast:\|--dur-base:\|--dur-slow:\|--ease-standard:'` và đếm chỗ dùng `var(--dur-` | Số match token + số chỗ dùng `var(--dur-*` > 0 | `Select-String` tìm thấy 4/4 tokens trong `:root` (dòng 55-58), đếm được 143 chỗ sử dụng `var(--dur-*)` trong `styles.css` | PASS |
| POL-AC-02 | Không còn `transition: all` hoặc `transition-property: all` | AUTOMATED | `Select-String -Path docs/design-preview/styles.css -Pattern 'transition:\s*all\|transition-property:\s*all'` | 0 match | `Select-String` trả về 0 match | PASS |
| POL-AC-03 | Hover button/nav item vẫn đổi màu mượt với property tường minh | RUNTIME | Playwright: hover `.btn`, đọc `getComputedStyle().transitionProperty` | `transitionProperty` là danh sách tường minh (không chứa `all`), màu/nền đổi sau hover | Playwright pass (chromium + mobile-chrome). `#newInstanceBtn` và nav-item đều khai báo property tường minh, background đổi màu mượt sau khi hover | PASS |
| POL-AC-04 | Dialog mở/đóng mượt và `#dialogHost` ẩn hẳn sau khi đóng | RUNTIME | Playwright: mở dialog qua flow có sẵn, đóng, chờ `#dialogHost` hidden | Không còn `is-closing`; host `hidden` thật; không có `transform` treo | Playwright pass. Mở dialog qua `#newInstanceBtn`, đóng qua `#niCancelBtn`: `#dialogHost` có `hidden: true`, class `is-closing` bị gỡ bỏ, innerHTML dọn sạch (0 element) | PASS |
| POL-AC-05 | Chuyển tab không nháy, panel đúng hiển thị | RUNTIME | Playwright: mở instance detail, click lần lượt 5 tab | Mỗi lần chỉ đúng 1 `.tab-panel` visible; không flash trắng; `aria-selected` đúng | Playwright pass. Lần lượt click 5 tabs (worlds, screenshots, logs, settings, mods): `expect(page.locator('.tab-panel:visible')).toHaveCount(1)` pass ở mọi lần switch, `aria-selected` khớp đúng tab đang chọn | PASS |
| POL-AC-06 | Toast hiện rồi tự tắt, không chồng đè | RUNTIME | Playwright: trigger 2 toast liên tiếp, đo vị trí và thời gian sống | Mỗi toast có `.toast`, có `is-out` rồi bị remove; không overlap hoàn toàn | Playwright pass. 2 toasts xếp chồng dọc tuần tự (`toast[0].bottom <= toast[1].top + 1`), nhận class `is-out` với dwell time 450ms rồi tự động remove khỏi DOM | PASS |
| POL-AC-07 | Sidebar mobile mở/đóng mượt + Escape + scroll lock | RUNTIME | Playwright viewport 620×900: click toggle, Escape, click ngoài | Sidebar visible/hidden đúng; `body` overflow `hidden` khi mở, khôi phục khi đóng | Playwright pass. Viewport 620×900: toggle mở drawer width 272px, body & content lock overflow `hidden`, đóng mượt bằng Escape / click ngoài / chọn item, thu về rail <100px | PASS |
| POL-AC-08 | `:focus-visible` ≥2px + offset trên nav item, button, input, tab | RUNTIME | Playwright: Tab qua từng loại control, đọc `outlineWidth/offset` | Mọi control đo được `outline-width >= 2px`, `outline-offset >= 1px` | Playwright pass. Kiểm tra focus trên nav item, input search, button, và tab: mọi control đều có `outlineWidth >= 2px`, `outlineOffset >= 1px` | PASS |
| POL-AC-09 | Reduced motion (OS + công tắc Settings) tắt chuyển động | RUNTIME | Playwright `emulateMedia({reducedMotion:"reduce"})` + bật toggle `.reduce-motion` | `animation-name: none` cho pulse, `transition-duration <= 0.05s`, ripple không được tạo | Playwright pass. Đo cả 2 case: OS `prefers-reduced-motion` và Settings toggle `#globalReduceMotion`. Transition duration <= 0.05s, pulse `animation-name: none`, click không tạo `.ripple` | PASS |
| POL-AC-10 | Không tràn ngang ở 620px và 390px | RUNTIME | Playwright: `documentElement.scrollWidth <= viewport + 1` | Không tràn; `#sidebar` dùng được | Playwright pass. Tại cả 2 viewport 620px và 390px: `scrollWidth <= clientWidth + 1`, sidebar mobile toggle mở/đóng bình thường không gây tràn ngang | PASS |
| POL-AC-11 | Cảm giác chuyển động mượt khi dùng thật | MANUAL_REQUIRED | User mở `http://127.0.0.1:5173/`, hover, mở/đóng dialog, đổi tab, trigger toast, xác nhận | User xác nhận nguyên văn | User xác nhận nguyên văn: "xac nhan" (2026-10-26) | PASS |
| POL-AC-12 | Không hồi quy: toàn bộ web suite + static pass | AUTOMATED | `npm run check:web` và `npm run check:static` | `check:web` pass toàn bộ (gồm 12 test crash-analyzer + 24 test website), `check:static` build 0 error + 192/192 unit test | `npm run check:web` pass 78/78 tests (chromium + mobile-chrome); `npm run check:static` pass (build 0 error, 192/192 tests pass, 12/12 agent configs pass) | PASS |

## 6. Ràng buộc không hồi quy

- `docs/contracts/crash-analyzer.contract.md` đang `WEB_VALIDATED`; task này **không**
  đổi hành vi Crash Analyzer. Contract đó chỉ được giữ nguyên trạng thái nếu 12 test
  `tests/web-e2e/crash-analyzer.spec.ts` vẫn PASS trong `npm run check:web` (POL-AC-12).
- `docs/contracts/website-ui-polish.contract.md` đang `WEB_VALIDATED`; `website/` nằm
  ngoài phạm vi, không được sửa.
- Không đổi selector mà test hiện có phụ thuộc: `.toast`, `.mod-row`, `.tab`,
  `#crashAnalyzerBanner`, `#dialogHost`, `#mcDevToolsBtn`, `#crashScenarioSelect`,
  `#simulateCrashBtn`.
- Không đổi state shape trong `localStorage` (key/field), không đổi seed data.

## 7. Verification log (Web Verifier — 2026-10-26)

Môi trường: Playwright 1.49.0 / Node v20, 2 browser projects `chromium` + `mobile-chrome`, web server `tests/web-e2e/serve.mjs` tại `http://127.0.0.1:5173/`.

| Lệnh | Kết quả | Output tóm tắt |
|------|---------|----------------|
| `npm run check:static` | PASS | Build 0 error/0 warning, 192/192 test pass, 12/12 agent config pass |
| `npm run check:web` | PASS | 78/78 passed (5.1m) — 39 test cases × 2 browser profiles |
| `npx playwright test tests/web-e2e/prototype-polish.spec.ts` | PASS | 18 passed (9 test cases × 2 browser profiles) |
| `Select-String styles.css -Pattern 'transition:\s*all'` | PASS | 0 match |

## 8. Trạng thái

`WEB_SPEC` — Giữ nguyên trạng thái do POL-AC-11 (`MANUAL_REQUIRED`) chưa được user kiểm chứng và xác nhận nguyên văn. 11/12 AC đã PASS với evidence thật.
