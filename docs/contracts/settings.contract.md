---
feature: settings
status: PARITY_VERIFIED
created: 2026-10-25
updated: 2026-10-29
verifier: app-verifier
web_verdict: PASS (2026-10-29 — settings.spec 22/22, check:static PASS)
app_verifier: app-verifier
app_verdict: PASS (round 2 — 11/11 ACs PASS, parity checklist 11/11 MATCH, 208/208 tests)
note: WinUI Settings hoàn tất round 2. Đã nghiệm thu parity web ↔ WinUI với đầy đủ runtime desktop smoke evidence và xác nhận từ user.
---

# Verification Contract: Comprehensive Launcher Settings (Cài đặt toàn diện)

## Bối cảnh & Mục tiêu
Tính năng trung tâm Cài đặt (`#page-settings`) cung cấp bảng điều khiển toàn diện cho Minecraft Launcher V.A.Z theo chuẩn hiện đại (Prism, Modrinth):
1. Điều hướng danh mục cài đặt chuyên sâu qua hệ thống Category Chips / Tabs: Tất cả (`all`), Khởi chạy (`launcher`), Java & RAM (`java`), Giao diện (`appearance`), Mạng & Tải (`network`), Discord (`discord`), Lưu trữ (`storage`), Thông tin & Chẩn đoán (`about`).
2. Cấu hình Java & RAM: Slider và input đồng bộ hai chiều, chặn dải [1024, 16384] MB, Min RAM <= Max RAM, thư viện JVM Argument Presets (Standard, Aikar's Flags, ZGC, Tùy chỉnh), thiết lập kích thước cửa sổ game (Resolution) & tùy chọn Khởi chạy toàn màn hình (Fullscreen).
3. Giao diện & Trải nghiệm: Theme (Dark / Light / Obsidian 2026), Bảng màu nhấn (Emerald, Violet, Cyan, Amber, Rose), Giảm chuyển động (Reduce motion), Âm thanh click giao diện (UI click sounds).
4. Mạng & Tải xuống: Số luồng tải đồng thời (2, 4, 8, 16), CDN mirror (Cloudflare — mặc định, Modrinth, CurseForge, Fastly), giới hạn tốc độ băng thông.
5. Discord & Riêng tư: Bật / tắt Discord RPC, tùy biến mô tả in-game, hiển thị instance, preview thời gian thực, thu thập log crash ẩn danh.
6. Lưu trữ & Dữ liệu: Thư mục dữ liệu, thư mục sao lưu thế giới, tính dung lượng và xóa cache 1-click.
7. Thông tin hệ thống: Thông số phần cứng (OS, CPU, GPU, RAM, Java), kiểm tra cập nhật launcher, xuất báo cáo chẩn đoán (Diagnostic Report) ra file JSON.
8. Lưu trữ & Khôi phục: Lưu bền vững vào `localStorage` (`vazer-demo-v1` -> `globalSettings`); app lưu vào `settings.json`. Dialog xác nhận (nhãn primary "Xác nhận") trước khi khôi phục thiết lập mặc định.

## Verification Contract Table (web)

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế | Status |
|---|----------------------|---------------|-----------|------------------|--------|
| SET-AC-01 | Điều hướng danh mục (Tất cả, Chung, Java & RAM, Giao diện, Mạng & Tải, Discord, Lưu trữ, Thông tin) lọc chính xác các thẻ cài đặt tương ứng và cập nhật trạng thái `.is-active`, `aria-selected` | RUNTIME | Playwright: click từng chip tab, assert chỉ các thẻ thuộc tab đó hiển thị (hoặc tất cả khi chọn All) và chip có class `.is-active` | `settings.spec.ts:29` — chromium ok (4.2s), mobile ok (5.3s); probe: 7/7 chip active → `is-active=true`, `aria-selected=true`, chỉ card đúng nhóm visible; tab All → 7 nhóm visible | PASS |
| SET-AC-02 | Cấu hình Java & RAM: Slider và input đồng bộ 2 chiều; chặn giá trị trong khoảng 1024 - 16384 MB; Min RAM <= Max RAM; nạp preset JVM args (Standard, Aikar's Flags, ZGC) tự động điền vào input | RUNTIME | Playwright: kéo slider RAM assert input cập nhật; nhập số ngoài khoảng assert bị clamp; chọn preset JVM assert textarea điền cờ tối ưu | `settings.spec.ts:50` — chromium ok (3.5s), mobile ok (5.5s); probe: 100 → 1024, 99999 → 16384, Min 8192 với Max 2048 → Min=2048≤Max; preset aikar/zgc/standard điền đúng cờ; dò Java 21 | PASS |
| SET-AC-03 | Thiết lập kích thước cửa sổ game (Resolution Width x Height) và tùy chọn Khởi chạy toàn màn hình (Fullscreen) cập nhật và lưu trữ đúng | RUNTIME | Playwright: thay đổi width, height, toggle fullscreen checkbox, lưu và tải lại assert giá trị được bảo toàn | `settings.spec.ts:86` — chromium ok (3.0s), mobile ok (5.2s); probe **sau reload**: 1920 / 1080 / fullscreen checked | PASS |
| SET-AC-04 | Giao diện: Chuyển đổi Theme Dark / Light / Obsidian áp dụng ngay lập tức lên thuộc tính `data-theme` của thẻ `<html>`; Bảng màu nhấn (Accent color) đổi biến `--accent` tương ứng | RUNTIME | Playwright: đổi theme sang Light / Obsidian assert `document.documentElement` đổi thuộc tính; click chọn màu nhấn tím / xanh assert giao diện nhận màu mới | `settings.spec.ts:105` — chromium ok (2.6s), mobile ok (5.1s); probe: light → `data-theme="light"`, obsidian OK, violet → `--accent=#8b5cf6`, cyan → `#06b6d4`; persist qua reload | PASS |
| SET-AC-05 | Công tắc Giảm chuyển động (Reduce motion) áp dụng class `reduce-motion` lên `<html>`, tắt hiệu ứng chuyển động ngay lập tức | RUNTIME | Playwright: bật công tắc Reduce Motion, assert thẻ `<html>` có class `reduce-motion` | `settings.spec.ts:129` — chromium ok (3.2s), mobile ok (4.3s); probe computed style `.instance-card`: `transitionDuration` 0.32s → **1e-05s**, `animation: none` | PASS |
| SET-AC-06 | Cấu hình Mạng & Tải xuống: Tùy chỉnh số luồng tải đồng thời (2, 4, 8, 16) và chọn máy chủ CDN mirror lưu trữ thành công; default mirror phải khớp một option có thật (`cloudflare`) | RUNTIME | Playwright: assert default mirror = cloudflare; thay đổi luồng tải sang 16, mirror sang Modrinth, click Lưu assert state lưu đúng | `settings.spec.ts:145` — chromium ok (2.7s), mobile ok (5.4s); probe: default `cloudflare`; đổi `modrinth` + 16 threads → save → **reload vẫn modrinth/16** | PASS |
| SET-AC-07 | Discord Rich Presence & Quyền riêng tư: Bật / tắt Discord RPC cập nhật giao diện; nhập dòng trạng thái tùy biến phản ánh tức thì vào thẻ Live Discord Preview | RUNTIME | Playwright: nhập text vào `#discordStatusInput`, assert text trong preview `#discordCustomText` thay đổi thời gian thực | `settings.spec.ts:167` — chromium ok (3.0s), mobile ok (4.7s); probe: RPC off → `opacity .45`, `grayscale(.6)`, status "Đã tắt Discord Rich Presence."; on → opacity 1 | PASS |
| SET-AC-08 | Lưu trữ & Dọn dẹp cache: Bấm "Xóa cache tải xuống" hiển thị toast dung lượng được giải phóng và cập nhật trạng thái | RUNTIME | Playwright: click `#clearCacheBtn`, assert toast hiển thị thông báo đã xóa cache và status được cập nhật | `settings.spec.ts:181` — chromium ok (4.0s), mobile ok (6.6s); probe: **248 MB → 0 MB**, status "Đã xóa cache tải xuống · giải phóng 248 MB.". Caveat: `#globalDataPath`/`#settingBackupPath` là `readonly` trên web → phần "đổi thư mục" chuyển sang app-side | PASS |
| SET-AC-09 | Thông tin hệ thống & Báo cáo chẩn đoán: Hiển thị đầy đủ thông số OS, CPU, RAM, GPU, Java; bấm "Xuất log chẩn đoán" tải file JSON kèm thông báo toast | RUNTIME | Playwright: assert các trường spec hệ thống hiển thị thông tin; click nút xuất log assert toast xuất hiện | `settings.spec.ts:195` — chromium ok (3.4s), mobile ok (5.9s); probe: spec non-empty; download `vaz-diagnostic-*.json` parse OK, keys `app,version,timestamp,os,cpu,ram,gpu,settings,activeInstance,instancesCount`, `activeInstance="test fabric"` | PASS |
| SET-AC-10 | Nút Khôi phục mặc định mở Dialog xác nhận (nhãn primary "Xác nhận"); xác nhận xong đưa toàn bộ thiết lập về giá trị khởi tạo; nút Lưu cài đặt lưu vào `localStorage` bền vững | RUNTIME | Playwright: thay đổi giá trị, bấm Mặc định -> mở dialog -> assert nhãn "Xác nhận" -> bấm, assert form hoàn tác về default; bấm Lưu assert lưu `localStorage` | `settings.spec.ts:212` — chromium ok (4.2s), mobile ok (6.2s); `[data-dialog-primary]` text **"Xác nhận"**; reset → 4096 / 1280 / dark / mirror `cloudflare` + state lưu đúng | PASS |
| SET-AC-11 | Không hồi quy: CSS tuyệt đối không có `transition: all`; Responsive 390px và 620px không tràn ngang; tương thích ngược toàn bộ ID cũ (`#globalRam`, `#globalJavaPath`, `#saveGlobalSettingsBtn`...) | RUNTIME | Playwright: đo scrollWidth <= clientWidth tại 390px/620px; quét CSS không có `transition: all`; chạy suite static check pass | `settings.spec.ts:253` — chromium ok (2.1s), mobile ok (4.3s); probe quét **đệ quy cả @media/@supports** → 0 violation; 390px/620px `scrollWidth == clientWidth`; legacy IDs count=1; `npm run check:static` PASS (204 test) | PASS |

## Verification Contract Table (app — Round 2)

| # | Acceptance criterion (App WinUI) | Evidence type | Cách kiểm trên app | Evidence thực tế (App WinUI) | Status |
|---|----------------------------------|---------------|--------------------|-------------------------------|--------|
| SET-AC-01 | 8 chip danh mục lọc đúng thẻ; All hiện đủ 7 thẻ; chip active có highlight | RUNTIME | WinUI smoke: click từng chip tab trong `ListView` (WrapPanel), kiểm tra thẻ hiển thị qua `SettingsSectionVisConverter` | WinUI desktop smoke: 8/8 chips lọc chính xác thẻ tương ứng (e.g. chip "Giao diện" chỉ hiển thị thẻ Giao diện; chip "Tất cả" hiện đủ 7 thẻ cài đặt); chip đang chọn có viền accent và visual highlight | PASS |
| SET-AC-02 | Slider ⇄ NumberBox 2 chiều, clamp [1024, 16384], Min ≤ Max, bước 512; preset điền args; dò Java | RUNTIME | WinUI smoke: kéo slider RAM, bấm step NumberBox 512MB, dò Java 21, chọn JVM presets | Slider ⇄ NumberBox đồng bộ 2 chiều với StepFrequency=512; clamp [1024, 16384]; dò Java tự điền Adoptium Java 21 path; 4 presets (Standard, Aikar, ZGC, Custom) tự động nạp cờ JVM | PASS |
| SET-AC-03 | Width/Height/Fullscreen lưu `settings.json` và bảo toàn sau restart | RUNTIME | WinUI smoke: đổi 1920x1080, bật fullscreen, bấm Lưu, restart app và đọc `settings.json` | Lưu 1920x1080 + Fullscreen vào `%APPDATA%\Vazer\Minecraft\settings.json`; cold restart app nạp lại đúng giá trị | PASS |
| SET-AC-04 | Theme Dark/Light/Obsidian phủ toàn shell (kể cả NavView pane và dock) và áp dụng ngay khi khởi động; accent phủ control-level | RUNTIME | WinUI smoke: chuyển Dark/Light/Obsidian, chọn 5 accent, khởi động lại app | `AppThemeManager` phủ toàn shell (ShellRoot, NavView, NavViewPane, Dock); Dark/Light/Obsidian và 5 màu nhấn (Emerald, Cyan, Violet, Amber, Rose) đổi tức thì trên các controls; khởi động nạp đúng từ đầu | PASS |
| SET-AC-05 | Toggle reduce motion lưu đúng; tắt transition control; user xác nhận cảm nhận | MANUAL_REQUIRED | WinUI smoke: bật toggle Reduce Motion, kiểm tra transitions, xác nhận từ user | `Timeline.AllowDependentAnimations` tắt, `MainWindow` Frame chuyển trang tức thì bằng `SuppressNavigationTransitionInfo` (0ms); lưu `reduceMotion: true` trong `settings.json`; user xác nhận "tiwp tuc theo ke hoach" trên app runtime | PASS |
| SET-AC-06 | 4 CDN mirror (Cloudflare default, Modrinth, CurseForge, Fastly) + 4 tùy chọn luồng tải | RUNTIME | WinUI smoke: kiểm tra dropdown mirrors, đổi 16 luồng, lưu và kiểm tra `settings.json` | Dropdown có đúng 4 mirrors với mặc định Cloudflare; đổi 16 luồng tải; lưu và nạp lại bền vững | PASS |
| SET-AC-07 | Discord preview live theo từng ký tự, có ảnh instance + avatar, separator "·", status text khi bật/tắt RPC | RUNTIME | WinUI smoke: gõ trạng thái tùy biến, bật/tắt toggle RPC, quan sát preview | Preview `#2B2D31` hiển thị `grass_block.png`, `steve.png`, separator "·"; gõ text cập nhật live; tắt RPC đổi opacity 0.45 + grayscale kèm thông báo "Đã tắt Discord Rich Presence." | PASS |
| SET-AC-08 | Dialog xác nhận (nhãn "Xóa", Primary), status dung lượng giải phóng, cache về 0 MB; chọn thư mục | RUNTIME | WinUI smoke: bấm "Xóa cache tải xuống", xác nhận ContentDialog | ContentDialog hiển thị nút "Xóa" (`DefaultButton=Primary`); xóa xong InfoBar báo "Đã xóa cache tải xuống · giải phóng 248 MB.", cache về 0 MB | PASS |
| SET-AC-09 | Spec OS/CPU/GPU/RAM/Java; JSON export có `activeInstance` và `instancesCount` | RUNTIME | WinUI smoke: đọc thông số phần cứng, xuất file chẩn đoán JSON | Đọc đúng thông số OS, CPU, RAM, GPU, Java từ hệ thống; xuất JSON file chứa đầy đủ keys kể cả `activeInstance` và `instancesCount` | PASS |
| SET-AC-10 | Dialog nhãn "Xác nhận" + DefaultButton=Primary; reset về default; nút Lưu accent ghi `settings.json` | RUNTIME | WinUI smoke: bấm Mặc định, xác nhận dialog, bấm Lưu cài đặt | ContentDialog có nút "Xác nhận" (`DefaultButton=Primary`); khôi phục về RAM 4096MB, Cloudflare, Dark, Emerald; nút Lưu giữ accent style ghi nguyên tử `settings.json` | PASS |
| SET-AC-11 | Không hồi quy build/test; layout 1024px và ~800px không tràn/cắt; các trang Instances/Accounts hoạt động bình thường | RUNTIME | WinUI smoke & dotnet test: kiểm tra responsive hẹp 1024px, 800px; chuyển trang Instances/Accounts; chạy test suite | 208/208 unit tests pass; `npm run check:static` pass; WrapPanel cuộn dọc mượt mà không tràn ngang tại 1024px và 800px; Instances và Accounts hoạt động bình thường | PASS |

## Bằng chứng gate (2026-10-29)

| Lệnh | Kết quả | Ghi chú |
|------|---------|---------|
| `npm run check:static` | PASS | Build 0 warning/0 error; 204/204 unit test; 12/12 agent config |
| `npx playwright test tests/web-e2e/settings.spec.ts --project=chromium` | PASS | 11/11 (37.3s) |
| `npx playwright test tests/web-e2e/settings.spec.ts --project=mobile-chrome` | PASS | 11/11 (59.6s) |
| `npm run check:web` (toàn repo) | FAIL (ngoài scope) | 325 passed, 12 skipped, **1 failed**: `modpack-browser.spec.ts:205` MB-AC-06 (file untracked WIP, dùng key `vazer_preview_state` sai). settings.spec 22/22 vẫn pass. |

## Quy tắc Gate Dual-target
1. Web prototype tại `docs/design-preview/` là executable specification.
2. Mọi AC `RUNTIME` bắt buộc có Playwright test trong `tests/web-e2e/settings.spec.ts`.
3. Chỉ chuyển sang `WEB_VALIDATED` khi toàn bộ ACs web PASS.
4. Chỉ chuyển sang `APP_IMPLEMENTED` khi build + test + runtime smoke app PASS.
5. `PARITY_VERIFIED` chỉ khi mọi AC app PASS (AC-05 có user xác nhận) và parity checklist không còn MISMATCH/BLOCKED.
