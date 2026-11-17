# Task Ledger: Worlds Manager Deep Flow

## Thông tin chung
- **Feature**: Worlds Manager (Quản lý thế giới chơi đơn)
- **Feature Doc**: `docs/product/features/worlds-manager.feature.md`
- **Contract**: `docs/contracts/worlds-manager.contract.md`
- **Phase hiện tại**: `WEB_VALIDATED` (user xác nhận WM-AC-10 ngày 2026-10-27)
- **Ngày tạo**: 2026-10-27
- **Verifier**: web-verifier

## Các bước thực hiện
- [x] **Bước 1 (Orchestrator)**: Lập Feature Spec, Verification Contract và Task Ledger (Status: `WEB_SPEC`).
- [x] **Bước 2 (Web Developer)**: Cập nhật Web Prototype (`index.html`, `styles.css`, `app.js`) và viết bộ kiểm thử Playwright e2e (`tests/web-e2e/worlds-manager.spec.ts`).
- [x] **Bước 3 (Web Verifier)**: Kiểm tra độc lập Playwright (18/18 tests pass), static analysis & regression (176/176 tests pass), cập nhật contract.
- [ ] **Bước 4 (User Sign-off)**: Chụp ảnh bằng chứng trực quan, chờ user xác nhận WM-AC-10 để đạt `WEB_VALIDATED`.
- [ ] **Bước 5 (Port WinUI)**: Sẽ thực hiện sau khi web đạt `WEB_VALIDATED`.

## Tiêu chí nghiệm thu (Checklist)
- [x] WM-AC-01: Thẻ World đầy đủ metadata (icon, tên, folder, game mode, size, seed, last played) — PASS
- [x] WM-AC-02: Tìm kiếm `#worldSearch` và lọc theo chip Game Mode — PASS
- [x] WM-AC-03: Sao lưu (Backup) tạo file zip với progress bar và tăng bộ đếm — PASS
- [x] WM-AC-04: Khôi phục (Restore) từ danh sách backup timestamped — PASS
- [x] WM-AC-05: Đổi tên (Rename) và Nhân bản (Duplicate) — PASS
- [x] WM-AC-06: Xóa thế giới (Delete) an toàn với dialog xác nhận — PASS
- [x] WM-AC-07: Nhập thế giới từ bên ngoài qua file input hoặc kéo thả — PASS
- [x] WM-AC-08: Level.dat Inspector NBT preview kèm copy seed — PASS
- [x] WM-AC-09: Responsive 390px/620px, Escape đóng dialog, focus outline, reduced motion — PASS
- [ ] WM-AC-10: Cảm giác mượt mà, phản hồi tokens (`MANUAL_REQUIRED`) — BLOCKED (chờ user xác nhận)
- [x] WM-AC-11: Không hồi quy toàn bộ test suite (`AUTOMATED`) — PASS
