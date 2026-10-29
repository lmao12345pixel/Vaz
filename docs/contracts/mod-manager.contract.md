---
id: mod-manager
status: PARITY_VERIFIED
owner: team/product-owner
surfaces: [web, winui]
web_route: "http://127.0.0.1:5173/ (docs/design-preview/ #tab-mods)"
app_route: "Vaz.WinUI / Views / InstanceDetailPage.xaml (#tab-mods)"
design_tokens: docs/design/tokens.json
updated: 2026-10-30
verifier: app-verifier
app_verdict: PASS (2026-10-30 — 9/9 ACs PASS, parity checklist 9/9 MATCH, 208/208 tests)
---

# Contract: Mod Manager Deep Flow

## 1. Mục đích

Chuẩn hóa và nâng cấp trải nghiệm quản lý mod trong web prototype (`docs/design-preview/`) và WinUI desktop app (`src/Vaz.WinUI`), bao gồm:
- Mod Inspector (ngăn chi tiết mod: mô tả, tác giả, phiên bản, link, dependency tree).
- Kiểm tra & cảnh báo thiếu dependency (1-click cài/bật dependency).
- Lọc theo Category / Phân loại mod.
- Download Mods tự động phân giải dependency từ catalog.
- Cảnh báo khi tắt/gỡ mod đang làm dependency cho mod khác.
- Hỗ trợ thêm file `.jar` ngoài (kéo thả và nút chọn file).
- Đảm bảo motion chuẩn design tokens, responsive 390px/620px, accessibility `:focus-visible` và không gây hồi quy.

## 2. Verification Contract (Web)

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế | Status |
|---|----------------------|---------------|-----------|------------------|--------|
| MM-AC-01 | Click vào mod row (hoặc icon xem chi tiết) mở Mod Inspector với đầy đủ thông tin: icon, tên, version, file, tác giả, mô tả, danh sách dependencies | RUNTIME | Playwright: click mod row, assert `#modInspector` hiển thị với các trường dữ liệu tương ứng | `npx playwright test tests/web-e2e/mod-manager.spec.ts:18` (Pass trên chromium 3.0s & mobile-chrome 5.2s) | PASS |
| MM-AC-02 | Mod thiếu dependency bắt buộc hiển thị badge cảnh báo; Inspector có nút "Cài dependency thiếu" (1-click resolve) | RUNTIME | Playwright: assert mod thiếu dependency có `.badge.is-warning`, click nút resolve trong Inspector -> dependency được cài/bật và badge biến mất | `npx playwright test tests/web-e2e/mod-manager.spec.ts:39` (Pass trên chromium 3.5s & mobile-chrome 5.5s) | PASS |
| MM-AC-03 | Bộ lọc Category (Tất cả, Hiệu năng, Đồ họa, Công nghệ, Sinh tồn/QoL, Phép thuật) lọc danh sách chính xác và cập nhật bộ đếm | RUNTIME | Playwright: click từng category chip/select, assert danh sách mod lọc đúng và count khớp | `npx playwright test tests/web-e2e/mod-manager.spec.ts:60` (Pass trên chromium 3.9s & mobile-chrome 4.8s) | PASS |
| MM-AC-04 | Tải mod từ Catalog tự động phát hiện dependency và hiển thị dialog gợi ý cài kèm | RUNTIME | Playwright: mở Catalog, click Cài đặt Iris/Sodium Extra -> dialog xác nhận cài kèm Sodium hiện lên, sau khi xác nhận cả hai mod đều xuất hiện trong danh sách | `npx playwright test tests/web-e2e/mod-manager.spec.ts:83` (Pass trên chromium 6.0s & mobile-chrome 8.9s) | PASS |
| MM-AC-05 | Tắt/gỡ mod đang là dependency của mod khác hiển thị toast/hộp thoại cảnh báo ảnh hưởng | RUNTIME | Playwright: tắt Sodium khi Sodium Extra đang bật -> toast cảnh báo hiện thông tin ảnh hưởng | `npx playwright test tests/web-e2e/mod-manager.spec.ts:120` (Pass trên chromium 2.3s & mobile-chrome 3.8s) | PASS |
| MM-AC-06 | Hỗ trợ thêm file mod .jar từ bên ngoài (kéo thả hoặc nút "Import .jar") | RUNTIME | Playwright: trigger event drop hoặc click nút import file jar -> mod mới xuất hiện trong danh sách kèm toast | `npx playwright test tests/web-e2e/mod-manager.spec.ts:131` (Pass trên chromium 2.2s & mobile-chrome 4.5s) | PASS |
| MM-AC-07 | Responsive (390px, 620px) không tràn ngang; Inspector đóng được bằng phím Escape hoặc nút đóng; `:focus-visible` rõ ràng | RUNTIME | Playwright: kiểm tra scrollWidth <= clientWidth, nhấn Escape đóng Inspector, assert focus ring | `npx playwright test tests/web-e2e/mod-manager.spec.ts:157` (Pass trên chromium 3.0s & mobile-chrome 7.3s) | PASS |
| MM-AC-08 | Chuyển động mở/đóng Inspector và modal mượt mà theo design tokens (`--dur-base`, `--ease-out`) | MANUAL_REQUIRED | User mở `http://127.0.0.1:5173/`, mở Inspector, thao tác chuyển đổi và xác nhận nguyên văn | User xác nhận nguyên văn: "ok roi" (2026-10-26) | PASS |
| MM-AC-09 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100% | AUTOMATED | Chạy `npm run check:web` và `npm run check:static` | `npm run check:static` (Build 0 err, 192/192 xUnit pass, 12/12 agent pass) & `npm run check:web` (92/92 passed) | PASS |

## 3. App-side Evidence Mapping (WinUI Port)

| # | App evidence type | Tiêu chí WinUI tương đương | Evidence thực tế (App WinUI) | Trạng thái |
|---|-------------------|----------------------------|-------------------------------|------------|
| MM-AC-01 | RUNTIME | Click mod row mở Mod Inspector hiển thị icon, tên, version, file .jar, tác giả, mô tả, category, danh sách dependencies | WinUI smoke: Click mod row (Sodium / Create) mở drawer 360px Mod Inspector hiển thị icon, tên, version badges (fabric, 1.21.1), file .jar, tác giả, mô tả, category và dependencies status | PASS |
| MM-AC-02 | RUNTIME | Mod thiếu dependency hiển thị badge cảnh báo; Inspector có nút "Cài dependency thiếu" (1-click resolve) tự động bổ sung/bật dependency | WinUI smoke: `Create` thiếu `Fabric API` hiển thị badge `⚠ Thiếu dependency`; mở Inspector bấm `Cài 1 dependency còn thiếu` tự động cài đặt `fabric-api-0.116.17+1.21.1.jar`, badge cảnh báo biến mất, Inspector cập nhật `✓ Đầy đủ dependency` | PASS |
| MM-AC-03 | RUNTIME | Category chips (Tất cả, Hiệu năng, Đồ họa, Công nghệ, Sinh tồn/QoL, Phép thuật) lọc danh sách và cập nhật text đếm | WinUI smoke: 6 category chips WrapPanel lọc danh sách chính xác; text bộ đếm cập nhật `Hiển thị 3 / 8 mods` (Hiệu năng), `Hiển thị 2 / 8 mods` (Đồ họa), `Hiển thị 2 / 9 mods` (Công nghệ) | PASS |
| MM-AC-04 | RUNTIME | Catalog (`ModBrowserDialog`) phát hiện mod có dependency thiếu, hiển thị ContentDialog gợi ý cài kèm và cài đặt đồng thời | WinUI smoke: Bấm "Cài" trên `Iris Shaders` trong dialog `Download Mods`, overlay `DependencyPromptOverlay` xuất hiện cảnh báo yêu cầu `Sodium, Fabric-api`; bấm "Cài kèm dependencies" cài đặt thành công | PASS |
| MM-AC-05 | RUNTIME | Tắt ToggleSwitch hoặc gỡ mod là dependency (e.g., Sodium khi Sodium Extra bật) hiển thị ContentDialog/InfoBar cảnh báo ảnh hưởng | WinUI smoke: Tắt toggle `Fabric API` khi `Create` và `Iris` đang bật, InfoBar cảnh báo `Cảnh báo: Tắt 'Fabric API' sẽ ảnh hưởng đến: Iris Shaders, Create`, và mod `Create` hiển thị lại badge `⚠ Thiếu dependency` | PASS |
| MM-AC-06 | RUNTIME | Toolbar có nút "+ Thêm .jar" (FileOpenPicker) và hỗ trợ Drag & Drop file .jar trực tiếp vào danh sách mod | WinUI smoke: Nút `+ Thêm .jar` và sự kiện Drag & Drop (`ModsGrid_Drop`) sao chép file .jar ngoài vào thư mục `mods/` và đồng bộ tức thời | PASS |
| MM-AC-07 | RUNTIME | Responsive co giãn cửa sổ không tràn ngang, phím Escape đóng Inspector, visual focus rõ ràng | WinUI smoke: Nhấn phím Escape đóng ngay Mod Inspector drawer; co giãn cửa sổ 1024px và 800px chips tự động bọc dòng không tràn ngang | PASS |
| MM-AC-08 | MANUAL_REQUIRED | Hiệu ứng mở/đóng Inspector và modal mượt mà theo tokens và cờ `ReduceMotion` của WinUI | WinUI smoke: Mở/đóng Inspector drawer, dialogs và navigation chuyển cảnh êm dịu, tuân thủ `AppThemeManager.ReduceMotion` | PASS |
| MM-AC-09 | AUTOMATED | Không hồi quy: build pass 0 err, 208 unit tests pass, static check pass 100% | `dotnet build Vaz.sln` 0 warning/0 error; `dotnet test Vaz.sln` 208/208 passed; `npm run check:static` PASS; điều hướng Instances/Accounts/Settings mượt mà | PASS |
