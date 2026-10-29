---
id: mod-manager
status: WEB_VALIDATED
owner: team/product-owner
surfaces: [web]
web_route: "http://127.0.0.1:5173/ (docs/design-preview/ #tab-mods)"
app_route: "n/a (chưa port WinUI)"
design_tokens: docs/design/tokens.json
updated: 2026-10-26
---

# Contract: Mod Manager Deep Flow

## 1. Mục đích

Chuẩn hóa và nâng cấp trải nghiệm quản lý mod trong web prototype (`docs/design-preview/`), bao gồm:
- Mod Inspector (ngăn chi tiết mod: mô tả, tác giả, phiên bản, link, dependency tree).
- Kiểm tra & cảnh báo thiếu dependency (1-click cài/bật dependency).
- Lọc theo Category / Phân loại mod.
- Download Mods tự động phân giải dependency từ catalog.
- Cảnh báo khi tắt/gỡ mod đang làm dependency cho mod khác.
- Hỗ trợ thêm file `.jar` ngoài (kéo thả và nút chọn file).
- Đảm bảo motion chuẩn design tokens, responsive 390px/620px, accessibility `:focus-visible` và không gây hồi quy.

## 2. Verification Contract

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
