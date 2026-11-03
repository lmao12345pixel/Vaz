---
id: website-ui-polish
status: WEB_VALIDATED
owner: team/product-owner
surfaces: [website]
web_route: "website/"
app_route: "n/a (landing page is web-only)"
design_tokens: docs/design/tokens.json
updated: 2026-10-25
---

# Contract: Website UI/UX Animation & Cleanup

## 1. Mục đích

Tối ưu chuyển động (motion) và dọn dẹp UI/UX cho landing page chính thức tại `website/`:
chuyển động mượt, nhất quán, không giật, không gây layout shift, tôn trọng
`prefers-reduced-motion`, và vòng focus bàn phím rõ ràng. Đây là **web-only**; không
đụng WinUI hay `docs/design-preview/`.

## 2. Phạm vi

### Trong phạm vi

- `website/styles.css`: token chuyển động, thay `transition: all`, hover/press/focus states,
  keyframes, `@media (prefers-reduced-motion: reduce)`.
- `website/app.js`: navbar scrolled state, scroll-reveal, FAQ đo chiều cao thật, mobile drawer
  (Escape/outside/scroll-lock), rAF-throttle scroll handler, tôn trọng reduced motion cho
  skin viewer auto-rotate.
- `website/index.html`: chỉ thêm thuộc tính `data-reveal` / `aria-*` cần thiết; **không** đổi
  cấu trúc section, id, class hiện có mà Playwright phụ thuộc.
- Sửa tràn ngang navbar ở viewport ≤768px (CTA header trùng lặp với CTA trong drawer/hero):
  ẩn CTA header trên mobile, giữ CTA trong drawer và hero.
- `tests/web-e2e/website.spec.ts`: bổ sung test runtime cho các AC mới.

### Ngoài phạm vi

- `docs/design-preview/` (prototype nội bộ), `src/Vaz.WinUI/`, `src/Vazer.Core/`.
- Thay đổi nội dung copy, thêm/bớt section, đổi bố cục responsive hiện có.
- Thư viện animation bên thứ ba.

## 3. Token chuyển động (website-local, map từ tokens.json)

| Token | Giá trị | Dùng cho |
|-------|---------|----------|
| `--dur-fast` | `140ms` | hover màu, viền, icon |
| `--dur-base` | `240ms` | card lift, tab, drawer |
| `--dur-slow` | `420ms` | reveal, hero entrance |
| `--ease-standard` | `cubic-bezier(0.2, 0, 0, 1)` | mặc định |
| `--ease-out` | `cubic-bezier(0.16, 1, 0.3, 1)` | vào màn hình |
| `--ease-spring` | `cubic-bezier(0.34, 1.56, 0.64, 1)` | nhấn nút, icon FAQ |
| `--stagger-step` | `70ms` | reveal tuần tự |

Giữ alias `--transition-fast/normal/spring` trỏ về token mới để không vỡ style cũ.

## 4. Hành vi người dùng thấy

| ID | Hành vi | Kết quả mong đợi |
|----|---------|------------------|
| UX-01 | Cuộn trang xuống khỏi hero | Navbar thêm `.is-scrolled`: nền đậm hơn, viền dưới rõ hơn, shadow nhẹ; chuyển đổi mượt, không nhấp nháy |
| UX-02 | Cuộn tới section (features/showcase/compare/download/faq) | Nội dung reveal bằng `opacity` + `translateY(16px)` một lần, stagger theo thứ tự; không đổi kích thước layout |
| UX-03 | Mở/đóng FAQ có đáp án dài | Accordion mở đúng toàn bộ nội dung (không bị cắt ở 300px), chiều cao animate mượt, icon xoay 45°, chỉ một câu mở tại một thời điểm |
| UX-04 | Mở mobile drawer (<768px) | Drawer trượt+vào bằng `transform`/`opacity` (không `display: none` giật), khóa scroll body, đóng bằng nút X, phím `Escape`, click ra ngoài, hoặc chọn link; `aria-expanded` đúng |
| UX-05 | Hover / nhấn / Tab qua nút, link, tab, FAQ | Có hover lift nhẹ, `:active` lún xuống, `:focus-visible` ring 2px màu primary + offset; không dịch layout |
| UX-06 | Bật OS "Reduce motion" | Mọi animation/transition không thiết yếu bị tắt; `scroll-behavior: auto`; pulse dot đứng yên; reveal hiện ngay; skin viewer không auto-rotate |
| UX-07 | Chuyển showcase tab | Pane mới fade+slide 8px vào, không animate lại khi tải trang, không nháy trắng |
| UX-08 | Cuộn liên tục | Không có transition `all`; scroll handler dùng `requestAnimationFrame`, không đọc layout mỗi event |

## 5. Acceptance criteria và bằng chứng

| # | Acceptance criterion | Evidence | Cách kiểm | Evidence thực tế (Web Verifier, 2026-10-25) | Status |
|---|----------------------|----------|-----------|---------------------------------------------|--------|
| UX-AC-01 | Navbar có trạng thái `.is-scrolled` sau khi cuộn > 24px và mất khi về đầu trang | `RUNTIME` | Playwright: `page.evaluate(() => window.scrollTo(0, 600))`, assert `header.navbar.is-scrolled`; scroll về 0, assert class mất | Playwright pass (chromium + mobile-chrome). Đo trực tiếp: trước cuộn `class="navbar"`, bg `rgba(11,15,22,0.8)`, shadow `none`; sau cuộn 600px `class="navbar is-scrolled"`, bg `rgba(11,15,22,0.96)`, shadow `rgba(0,0,0,0.4) 0 10px 28px`, viền `0.08→0.14`; header height 69px không đổi; về 0 → `class="navbar"` | PASS |
| UX-AC-02 | Reveal chạy một lần với stagger, phần tử hiển thị đầy đủ sau khi cuộn tới | `RUNTIME` | Playwright: cuộn tới `#features`, assert `.pillar-card` visible và có class reveal-done/opacity 1; assert không còn `opacity: 0` | Playwright pass. Đo trực tiếp trang mới: `html.js-reveal-ready`, 3 `.pillar-card` có `opacity: 0`, `translate: 0 16px`; sau khi `#features` vào viewport, `transitionDelay` lần lượt `0s / 0.07s / 0.14s` và opacity tăng tuần tự (0.56 → 0.75/0.19 → … → 1). Cuộn đi/về lại vẫn giữ `is-revealed`, opacity `1`, `translate: none` | PASS |
| UX-AC-03 | FAQ mở hết nội dung dài, không bị cắt, đóng/mở đúng `aria-expanded` | `RUNTIME` | Playwright: mở FAQ, đo `scrollHeight <= clientHeight` của `.faq-answer` (sau transition), assert `aria-expanded="true"`, đóng lại assert `false` | Playwright pass. Đo trực tiếp với đáp án bị kéo dài gấp 5: `clientHeight=383`, `scrollHeight=383`, `rect.height=382.7` (>300), `overflow-y: hidden`, `aria-expanded="true"`, icon `matrix(0.7071…)` = xoay 45°, `openCount=1`; đóng: height `0`, `aria-expanded="false"`, `openCount=0` | PASS |
| UX-AC-04 | Mobile drawer mở/đóng bằng transform, Escape đóng, body scroll khóa | `RUNTIME` | Playwright mobile viewport: click `#navToggle`, assert `#mobileMenu` visible + body overflow hidden; press Escape, assert hidden + overflow restored | Playwright pass (2 test mobile). Đo trực tiếp 390×844: đóng → `display:flex`, `visibility:hidden`, `opacity:0`, `transform: translateY(-8px)`, `pointer-events:none` (không dùng `display:none`); mở → opacity 1 sau ~240ms, `transform: translateY(0)`, body `overflow-y: hidden`, `aria-expanded=true`; Escape → `visibility:hidden`, body `auto`, aria `false`; click ngoài và click link đều đóng; bấm lại `#navToggle` (nút ✕) → aria `false`, body `auto`, menu hidden sau ~300ms; link trong drawer `visibility:hidden` khi đóng | PASS |
| UX-AC-05 | Mọi control tương tác có `:focus-visible` outline rõ ràng | `RUNTIME` | Playwright: `Tab` vào `#navToggle`/link đầu tiên, assert computed `outline-width` > 0 hoặc box-shadow ring | Playwright pass. Đo trực tiếp trang mới + `Tab` lần 1: active element `a.brand`, `outline: 2px solid rgb(34,197,94)`, `outline-offset: 2px`. Trang khác sau vài lần Tab: `button.faq-question` cũng có `2px solid` + offset `2px` | PASS |
| UX-AC-06 | `prefers-reduced-motion: reduce` tắt animation/transition và reveal hiện ngay | `RUNTIME` | Playwright với `page.emulateMedia({ reducedMotion: "reduce" })`: assert `.pillar-card` opacity = 1 ngay không cần cuộn, `animation-name` của `.pulse-dot` là `none`, `scroll-behavior` = auto | Playwright pass. Đo trực tiếp `emulateMedia({reducedMotion:"reduce"})` + `matchMedia` = true: `.pillar-card` opacity `1`, `translate: none`, transition-duration `1e-05s`; `.pulse-dot` animation-name `none`; `scroll-behavior: auto`; `js-reveal-ready` không được gắn (fallback revealAll). Ghi chú: auto-rotate của skin viewer xác nhận bằng code path `shouldAutoRotate()` trả `isAutoRotating && !prefersReducedMotion`; không đo runtime được vì `skinViewer` là biến closure, không expose global | PASS |
| UX-AC-07 | Không còn `transition: all` trong `website/styles.css` | `AUTOMATED` | Command: `Select-String -Path website/styles.css -Pattern 'transition:\s*all'` trả về 0 kết quả | `Select-String -Path website/styles.css -Pattern 'transition:\s*all'` → `MATCHES=0`. Kiểm thêm `transition-property:\s*all` → 0; regex `transition[^;{]*\ball\b` → 0 dòng | PASS |
| UX-AC-08 | Không hồi quy: 8 test website cũ + toàn bộ web suite pass | `AUTOMATED` | `npm run check:web` — 100% pass trên chromium + mobile-chrome | `npm run check:web` → `42 passed (2.9m)`, exit 0, `WEB CHECK PASSED: Playwright (chromium + mobile-chrome)`. Trong đó 4 test website cũ × 2 project = 8 run đều pass; `npx playwright test tests/web-e2e/website.spec.ts` riêng → `24 passed (1.9m)`, exit 0. Lưu ý: Developer Report ghi 40, thực tế 42 (spec hiện có 12 case website). | PASS |
| UX-AC-09 | Build/unit test không hồi quy | `AUTOMATED` | `npm run check:static` — build 0 error, 192/192 unit test pass | `npm run check:static` → `Build succeeded. 0 Warning(s) 0 Error(s)`; `Passed! - Failed: 0, Passed: 192, Skipped: 0, Total: 192`; agent validation `12/12`; exit 0, `STATIC CHECK PASSED` | PASS |
| UX-AC-10 | Cảm giác chuyển động mượt, không giật khi cuộn/mở FAQ/drawer | `MANUAL_REQUIRED` | User xem `http://127.0.0.1:5173/website/`, cuộn hero→features, mở FAQ dài, mobile drawer <768px (Esc/click ngoài/link), Tab qua control, reduce-motion, xác nhận | **MANUAL evidence do user cung cấp (2026-10-25):** user đã mở `http://127.0.0.1:5173/website/` (server `npm run web:dev` đang chạy) và xác nhận nguyên văn **"Mượt, đạt yêu cầu"** cho câu hỏi "cảm giác chuyển động/animation có mượt và không bị giật không?". Verifier chỉ ghi nhận xác nhận của user, không tự đánh giá cảm giác. Kiểm tra bổ sung của verifier: server trả `200` cho `/website/`, nội dung đúng trang (`h1.hero-title`, `section#features`), mtime web files không đổi so với log mục 7 | PASS |
| UX-AC-11 | Không tràn ngang ở viewport 390px; navbar không đẩy `#navToggle` ra ngoài màn hình | `RUNTIME` | Playwright mobile 390×844: assert `document.documentElement.scrollWidth <= 390` và `#navToggle` boundingBox nằm trong viewport | Playwright pass. Đo trực tiếp 390×844: `documentElement.scrollWidth = 390`, `clientWidth = 390`, `body.scrollWidth = 390`; `#navToggle` bbox `x=332.44 → right=366` (trong 390), kích thước 34×44; CTA header `.nav-actions > a.btn-primary` có `display:none`; CTA trong drawer hiển thị | PASS |

## 6. Ghi chú kỹ thuật bắt buộc

- Chỉ animate `transform`, `opacity`; các thuộc tính màu/viền/shadow được phép nhưng phải khai báo tường minh.
- Không dùng `max-height` cố định cho accordion; dùng `grid-template-rows: 0fr → 1fr` hoặc set `--faq-h` từ `scrollHeight`.
- Reveal phải có fallback: nếu `IntersectionObserver` không có hoặc JS lỗi, nội dung vẫn hiển thị.
- Không đổi id/class mà `tests/web-e2e/website.spec.ts` đang dùng:
  `header.navbar`, `a.brand`, `#hero`, `h1.hero-title`, `#heroSkinCanvas`, `.pillar-card` (3),
  `.comparison-table`, `.download-card` (3), `.faq-item` (5), `.faq-question`, `.showcase-tab-btn`,
  `#tabContent-*`, `#interactivePlayBtn`, `#playBtnText`, `#fovSlider`, `#fovVal`, `button.skin-btn`.
- Mọi handler cuộn phải `{ passive: true }` + rAF throttle, và cleanup khi không cần.

## 7. Verification log (Web Verifier — 2026-10-25)

Môi trường: `@playwright/test` 1.64.1 đã cài (`node_modules/@playwright/test` = true),
`playwright.config.ts` có 2 project `chromium` + `mobile-chrome`, webServer `tests/web-e2e/serve.mjs`
tại `http://127.0.0.1:5173`.

| Lệnh | Kết quả | Output tóm tắt |
|------|---------|----------------|
| `node --check website/app.js` | PASS | exit 0, không có output |
| `Select-String -Path website/styles.css -Pattern 'transition:\s*all'` | PASS | 0 match |
| `npx playwright test tests/web-e2e/website.spec.ts` | PASS | 24 passed (1.9m), exit 0 — 12 case × 2 project |
| `npm run check:web` | PASS | 42 passed (2.9m), exit 0 — crash-analyzer 6 + vaz 3 + website 12, ×2 project |
| `npm run check:static` | PASS | build 0 error/0 warning; 192/192 test pass; agent config 12/12 |

Quan sát thêm (không chặn contract):

- `--faq-h` được set từ `scrollHeight` trong `website/app.js`, đúng cho phép của mục 6.
- Stagger dùng `transition-delay` inline 0/70/140ms và được clear sau animation — đúng thiết kế.
- Developer Report ghi `check:web = 40 passed`, thực tế 42 (spec đã có thêm 2 test drawer).
- `docs/design-preview/` đang dirty trong working tree nhưng mtime 19:43–20:17 (trước khi sửa
  `website/` lúc 23:19–23:52) và không chứa `is-scrolled` / `data-reveal` / `js-reveal-ready`;
  không có dấu hiệu task này sửa `docs/design-preview/`.

### Trạng thái

Không còn mục nào chờ — contract đã đạt `WEB_VALIDATED` ngày 2026-10-25 sau khi UX-AC-10
(`MANUAL_REQUIRED`) được user xác nhận nguyên văn "Mượt, đạt yêu cầu".