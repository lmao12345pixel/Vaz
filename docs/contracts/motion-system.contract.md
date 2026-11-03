---
id: motion-system
status: WEB_VALIDATED
owner: team/product-owner
surfaces: [web]
web_route: "http://127.0.0.1:5173/motion-lab.html"
app_route: "n/a (spec + demo cho app; port WinUI làm ở contract riêng)"
design_tokens: docs/design/tokens.json
updated: 2026-10-26
---

# Contract: Motion System — Motion Lab + Shared Element + Launch Sequence

## 1. Mục đích

Biến "cảm giác mượt" thành thứ **xem được, đo được, bàn giao được**. Trang Motion Lab là
nơi demo toàn bộ chuyển động của app trên web; mỗi animation có tên, trigger, token
duration/easing, nút Replay và CSS snippet. Khi port sang WinUI, dev đối chiếu trực tiếp
với trang này thay vì đoán.

Đợt này làm 3 phần: khung **Motion Lab**, **Shared Element (card → detail)** và
**Launch Sequence**.

## 2. Phạm vi

### Trong phạm vi

- `docs/design-preview/motion-lab.html` (mới)
- `docs/design-preview/motion-lab.css` (mới)
- `docs/design-preview/motion-lab.js` (mới)
- `tests/web-e2e/motion-lab.spec.ts` (mới)

### Ngoài phạm vi — TUYỆT ĐỐI KHÔNG SỬA

- `docs/design-preview/styles.css`, `docs/design-preview/app.js`, `docs/design-preview/index.html`
  — đang được `prototype-ui-polish` verify; sửa sẽ làm invalid evidence của verifier.
- `docs/design-preview/README.md` (chỉ cập nhật sau khi verify xong, ở task khác)
- `website/`, `src/Vaz.WinUI/`, `src/Vazer.Core/`, `tests/web-e2e/serve.mjs`
- Không thêm screen/tab vào prototype chính; Motion Lab là trang độc lập.
- Không đổi `docs/design/tokens.json` — chỉ **đọc** giá trị.

## 3. Design tokens

Motion Lab **phải link `styles.css`** để dùng chung `:root` tokens, rồi override trong
`motion-lab.css` bằng scope `.ml-*`. Không copy giá trị token bằng tay.

| Token | Giá trị (nguồn: `styles.css` `:root`) | Dùng cho |
|---|---|---|
| `--dur-fast` | 140ms | hover, đổi màu, icon |
| `--dur-base` | 220ms | card, tab, row, toast |
| `--dur-slow` | 380ms | dialog / sheet / overlay entrance |
| `--ease-standard` | `cubic-bezier(0.2, 0, 0, 1)` | mặc định |
| `--ease-out` | `cubic-bezier(0.16, 1, 0.3, 1)` | entrance |
| `--ease-spring` | `cubic-bezier(0.34, 1.56, 0.64, 1)` | press, toggle, badge |

Nếu một token không tồn tại trong `styles.css`, Developer **phải báo lại** — không tự
định nghĩa giá trị khác trong `motion-lab.css`.

## 4. Hành vi người dùng thấy

| ID | Hành vi | Kết quả mong đợi |
|----|---------|------------------|
| MOTION-01 | Mở `/motion-lab.html` | Trang có tiêu đề, công tắc Reduce motion, công tắc theme, và ≥3 demo card |
| MOTION-02 | Nhìn bảng Token | Liệt kê 3 duration + 3 easing kèm giá trị computed thật từ `:root` |
| MOTION-03 | Click một instance card | Card thu nhỏ/mờ đi, panel detail mở ra; **icon + tên instance** di chuyển liên tục từ vị trí card sang header detail (không "nhảy") |
| MOTION-04 | Click Back trong detail | Chạy ngược lại: shared element về đúng card gốc, grid hiện lại |
| MOTION-05 | Click Play ở demo Launch | Overlay toàn màn hình mở; 4 bước chạy tuần tự (Java Runtime → Assets → Mods → World), mỗi bước có progress riêng; hết 4 bước hiện trạng thái hoàn tất rồi overlay đóng |
| MOTION-06 | Click Replay ở bất kỳ demo card | Animation chạy lại từ đầu, không cần reload trang |
| MOTION-07 | Bật Reduce motion | Mọi animation ≤ 0.05s, không auto-play, shared element hiện ngay; trạng thái được giữ sau reload |
| MOTION-08 | Thu nhỏ viewport 390px | Không tràn ngang; demo card xếp 1 cột; không phần tử nào bị cắt |
| MOTION-09 | Tab qua các nút Replay / Play / Back | `:focus-visible` ring ≥2px, offset ≥1px |

## 5. Acceptance criteria và bằng chứng

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế (Web Verifier, 2026-10-26) | Status |
|---|----------------------|---------------|-----------|---------------------------------------------|--------|
| MOTION-AC-01 | Trang `/motion-lab.html` load được, có ≥3 demo card, không lỗi console | `RUNTIME` | Playwright: goto, assert card count ≥3, bắt `pageerror`/`console error` = 0 | Playwright pass (chromium + mobile-chrome). 3 demo cards hiển thị (`#demoSharedElement`, `#demoLaunchSequence`, `#demoMicrointeractions`), 0 console error, 0 page error | PASS |
| MOTION-AC-02 | Bảng token hiển thị đúng giá trị computed từ `:root` | `RUNTIME` | Playwright: đọc `getComputedStyle(document.documentElement).getPropertyValue(...)`, so với text trong bảng | Playwright pass. 6/6 tokens trong bảng `.ml-token-table` khớp hoàn toàn với `getComputedStyle(document.documentElement)` | PASS |
| MOTION-AC-03 | Shared element card → detail: mở và đóng đều đúng, phần tử chia sẻ tồn tại ở cả 2 trạng thái | `RUNTIME` | Playwright: click card, assert detail visible + `.ml-shared` trong detail; đo boundingBox trước/sau; click Back, assert grid visible lại | Playwright pass. Click `#mlCardInst1`: detail hiện, grid ẩn, `.ml-shared-icon` và `.ml-shared-title` tồn tại ở cả 2 view và animate FLIP mượt. Click Back: grid hiện lại, detail ẩn | PASS |
| MOTION-AC-04 | Launch Sequence chạy đủ 4 bước tuần tự và kết thúc | `RUNTIME` | Playwright: click Play, poll từng bước active theo thứ tự, assert progress đạt 100%, assert overlay đóng sau khi hoàn tất | Playwright pass. Chạy tuần tự qua 4 stages (`java`, `assets`, `mods`, `world`), mỗi stage có progress đạt 100%, kết thúc thành công với thông báo `#mlLaunchSummary` "Khởi chạy game thành công!" và overlay tự đóng | PASS |
| MOTION-AC-05 | Nút Replay chạy lại animation | `RUNTIME` | Playwright: click Replay, assert computed duration/`animation-name` hoặc class trạng thái đổi rồi trở lại | Playwright pass. Replay ở Microinteractions tăng badge count và pulse badge. Replay ở Launch Sequence mở lại overlay và rerun tuần tự | PASS |
| MOTION-AC-06 | Reduce motion: animation ≤0.05s, giữ trạng thái sau reload | `RUNTIME` | Playwright: bật công tắc, assert `html` có `data-reduced-motion="true"`, đo `transitionDuration`/`animationDuration` ≤0.05s, reload → vẫn bật | Playwright pass. Bật switch: `html[data-reduced-motion="true"]`, transitionDuration <= 0.05s. Sau khi reload trang, trạng thái giảm chuyển động vẫn duy trì | PASS |
| MOTION-AC-07 | Không tràn ngang ở 390px | `RUNTIME` | Playwright mobile 390×844: `scrollWidth <= clientWidth + 1`; card xếp 1 cột | Playwright pass. Viewport 390×844: `scrollWidth <= clientWidth + 1`, 3 demo cards xếp dọc 1 cột duy nhất, chiều rộng > 320px không bị cắt xén | PASS |
| MOTION-AC-08 | `:focus-visible` rõ ràng trên control của Motion Lab | `RUNTIME` | Playwright: Tab vào Replay đầu tiên, assert `outlineWidth >= 2`, `outlineOffset >= 1` | Playwright pass. Tab vào control đo được `outlineWidth = 2px`, `outlineOffset = 2px`, outlineStyle `solid` | PASS |
| MOTION-AC-09 | `motion-lab.css` không có `transition: all` | `AUTOMATED` | `Select-String -Path docs/design-preview/motion-lab.css -Pattern 'transition:\s*all\|transition-property:\s*all'` → 0 match | `Select-String` trả về 0 match. Playwright spec assertion kiểm tra 0 match regex pass | PASS |
| MOTION-AC-10 | Không hồi quy: web suite + static vẫn pass | `AUTOMATED` | `npm run check:web` và `npm run check:static` — pass toàn bộ, dán output thật | `npm run check:web` pass 78/78 tests (chromium + mobile-chrome); `npm run check:static` pass (build 0 error, 192/192 tests pass, 12/12 agent configs pass) | PASS |
| MOTION-AC-11 | Cảm giác chuyển động mượt, shared element không giật | `MANUAL_REQUIRED` | User mở `http://127.0.0.1:5173/motion-lab.html`, xem card→detail, launch, replay, xác nhận | User xác nhận nguyên văn: "xac nhan" (2026-10-26) | PASS |

## 6. Ràng buộc không hồi quy

- `prototype-ui-polish.contract.md` và `crash-analyzer.contract.md` giữ nguyên trạng thái.
- `npm run check:web` hiện có **60 test** (crash-analyzer 12, prototype-polish 18, vaz 6, website 30)
  — sau task này phải là 60 + số test mới, không có test nào fail.
- Không đổi selector mà test cũ phụ thuộc: `.toast`, `.mod-row`, `.tab`, `#crashAnalyzerBanner`,
  `#dialogHost`, `#mcDevToolsBtn`, `#crashScenarioSelect`, `#simulateCrashBtn`.
- Không đổi localStorage key/shape của prototype (`localStorage` mới cho Motion Lab phải
  dùng namespace riêng, ví dụ `vaz.motionLab.*`).
- Không hardcode secret/token; không thêm thư viện animation bên thứ ba (vanilla CSS/JS).

## 7. Ghi chú triển khai

- Server `tests/web-e2e/serve.mjs` phục vụ `docs/design-preview/` tại root, nên
  `/motion-lab.html` phải truy cập được mà **không sửa** `serve.mjs`. Developer phải
  xác nhận bằng `curl` trước khi viết test.
- Shared element: dùng FLIP thật (đo `getBoundingClientRect()` trước/sau, set transform
  nghịch đảo rồi animate về 0) — không dùng `view-transition-name` vì cần kiểm được
  bằng boundingBox và phải port được sang WinUI `ConnectedAnimationService`.
- Launch Sequence: mỗi bước có `data-stage`, `data-state` (`idle`/`active`/`done`) để test
  poll được trạng thái, không chỉ dựa vào thời gian.
- Không auto-play animation khi load trang (tránh test flaky và tôn trọng reduced motion).

## 8. Verification log (Web Verifier — 2026-10-26)

Môi trường: Playwright 1.49.0 / Node v20, 2 browser projects `chromium` + `mobile-chrome`, web server `tests/web-e2e/serve.mjs` tại `http://127.0.0.1:5173/`.

| Lệnh | Kết quả | Output tóm tắt |
|------|---------|----------------|
| `npm run check:static` | PASS | Build 0 error/0 warning, 192/192 test pass, 12/12 agent config pass |
| `npm run check:web` | PASS | 78/78 passed (5.1m) — 39 test cases × 2 browser profiles |
| `npx playwright test tests/web-e2e/motion-lab.spec.ts` | PASS | 18 passed (9 test cases × 2 browser profiles) |
| `Select-String motion-lab.css -Pattern 'transition:\s*all'` | PASS | 0 match |

## 9. Trạng thái

`WEB_SPEC` — Giữ nguyên trạng thái do MOTION-AC-11 (`MANUAL_REQUIRED`) chưa được user kiểm chứng và xác nhận nguyên văn. 10/11 AC đã PASS với evidence thật.
