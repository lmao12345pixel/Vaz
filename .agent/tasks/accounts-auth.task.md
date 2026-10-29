# Task: Quản lý Tài khoản & Microsoft Device Code Authentication (Web Prototype)

## Trạng thái
- Phase: `WEB_VALIDATED`
- Contract: `docs/contracts/accounts-auth.contract.md`
- Feature spec: `docs/product/features/accounts-auth.feature.md`

## Bảng tiêu chí nghiệm thu
| # | Acceptance criterion | Evidence type | Cách kiểm | Status |
|---|----------------------|---------------|-----------|--------|
| AUTH-AC-01 | Modal Device Code Flow hiển thị mã, link, countdown và polling | RUNTIME | Playwright assertion trên `#msAuthModalHost` | PASS |
| AUTH-AC-02 | Sao chép mã Device Code và hoàn tất xác thực thêm tài khoản | RUNTIME | Playwright click sao chép và xác thực | PASS |
| AUTH-AC-03 | Modal Offline Account có validation tên Minecraft hợp lệ | RUNTIME | Playwright test validation form | PASS |
| AUTH-AC-04 | Thẻ tài khoản hiển thị đầy đủ avatar, badge, UUID, token status | RUNTIME | Playwright assert cấu trúc thẻ | PASS |
| AUTH-AC-05 | Set Active đồng bộ sang thanh bottom `#userBtn` | RUNTIME | Playwright test đổi active account | PASS |
| AUTH-AC-06 | Refresh token mô phỏng làm mới token thành công | RUNTIME | Playwright test refresh token | PASS |
| AUTH-AC-07 | Xóa tài khoản an toàn kèm dialog xác nhận | RUNTIME | Playwright test xóa tài khoản | PASS |
| AUTH-AC-08 | Responsive 390px/620px, phím Escape, focus ring >= 2px, reduced motion | RUNTIME | Playwright scrollWidth và outline checks | PASS |
| AUTH-AC-09 | Cảm giác chuyển động mượt mà khi thao tác | MANUAL_REQUIRED | User xác nhận nguyên văn: "xac nhan" (2026-10-27) | PASS |
| AUTH-AC-10 | Không hồi quy: Web test suite và Static check pass 100% | AUTOMATED | `npm run check:web` và `npm run check:static` | PASS |

## Các bước triển khai
1. [x] Tạo Feature Spec & Verification Contract (`WEB_SPEC`).
2. [x] Dispatch `@web-developer` triển khai HTML, CSS, JS cho Accounts & Microsoft Auth và bộ test Playwright `tests/web-e2e/accounts-auth.spec.ts`.
3. [x] Dispatch `@web-verifier` chạy Playwright runtime và kiểm tra các AC.
4. [x] Báo cáo kết quả, chụp ảnh màn hình bằng chứng và user đã xác nhận AUTH-AC-09 ("xac nhan"), nâng phase sang `WEB_VALIDATED`.
