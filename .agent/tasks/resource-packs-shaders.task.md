# Task: Quản lý Resource Packs & Shaders chuyên sâu (Web Prototype)

## Trạng thái
- Phase: `WEB_VALIDATED`
- Contract: `docs/contracts/resource-packs-shaders.contract.md`
- Feature spec: `docs/product/features/resource-packs-shaders.feature.md`

## Bảng tiêu chí nghiệm thu
| # | Acceptance criterion | Evidence type | Cách kiểm | Status |
|---|----------------------|---------------|-----------|--------|
| RPS-AC-01 | Tab Resource Packs hiển thị danh sách packs với tên, icon, độ phân giải, dung lượng, toggle on/off | RUNTIME | Playwright assertion trên `#tab-resourcepacks` | PASS |
| RPS-AC-02 | Đổi thứ tự ưu tiên (Priority Reorder) bằng nút Move Up/Down, lưu vào state | RUNTIME | Playwright test click Move Up/Down, assert DOM order | PASS |
| RPS-AC-03 | Cảnh báo tương thích (Incompatible Warning badge) nếu format không khớp MC version | RUNTIME | Playwright assert `.badge.is-warning` | PASS |
| RPS-AC-04 | Tab Shaders hiển thị danh sách shaderpack, chọn active shader hoặc tắt shader, preview ảnh | RUNTIME | Playwright click shader, assert active và preview | PASS |
| RPS-AC-05 | Chuyển đổi Profile Shader (Low, Medium, High, Ultra) tức thì | RUNTIME | Playwright click profile chip, assert class và text | PASS |
| RPS-AC-06 | Tự động phát hiện thiếu Shader Loader, nút 1-click cài đặt Iris và chuyển sang Sẵn sàng | RUNTIME | Playwright assert cảnh báo, click cài Iris, assert sẵn sàng | PASS |
| RPS-AC-07 | Hỗ trợ thêm file `.zip` qua file input và kéo thả drag & drop | RUNTIME | Playwright file chooser + drag/drop event | PASS |
| RPS-AC-08 | Responsive 390px/620px không tràn ngang; `:focus-visible` >= 2px; Reduced motion <= 0.05s | RUNTIME | Playwright scrollWidth và outline checks | PASS |
| RPS-AC-09 | Cảm giác tương tác trực quan, mượt mà khi thao tác | MANUAL_REQUIRED | User xác nhận trực quan: "xac nhan" (2026-10-26) | PASS |
| RPS-AC-10 | Không hồi quy: Web test suite và Static check pass 100% | AUTOMATED | `npm run check:web` và `npm run check:static` | PASS |

## Các bước triển khai
1. [x] Tạo Feature Spec & Verification Contract (`WEB_SPEC`).
2. [x] Dispatch `@web-developer` triển khai HTML, CSS, JS cho Resource Packs & Shaders và bộ test Playwright `tests/web-e2e/resource-packs-shaders.spec.ts`.
3. [x] Dispatch `@web-verifier` chạy Playwright runtime và kiểm tra các AC.
4. [x] User xác nhận trực quan RPS-AC-09 ("xac nhan"), nâng phase sang `WEB_VALIDATED`.
