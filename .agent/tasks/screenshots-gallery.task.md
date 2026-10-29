# Task: Screenshots Gallery & Lightbox Viewer

- **Feature**: `screenshots-gallery`
- **Contract**: `docs/contracts/screenshots-gallery.contract.md`
- **Feature Doc**: `docs/product/features/screenshots-gallery.feature.md`
- **Phase**: `WEB_VALIDATED`
- **Owner**: Web Team
- **Date**: 2026-10-27

## Acceptance Criteria Checklist
- [x] SG-AC-01: Danh sách Screenshots hiển thị dạng lưới thẻ ảnh (Grid) với thumbnail, tiêu đề, tên file, ngày chụp, tag thế giới và độ phân giải (RUNTIME - PASS)
- [x] SG-AC-02: Tìm kiếm ảnh theo tên/tiêu đề/thế giới và sắp xếp mới nhất / cũ nhất (RUNTIME - PASS)
- [x] SG-AC-03: Nhấp vào ảnh hoặc nút Zoom mở Lightbox toàn màn hình hiển thị ảnh chất lượng cao và metadata (RUNTIME - PASS)
- [x] SG-AC-04: Điều hướng trong Lightbox: Next/Prev, phím mũi tên Trái/Phải, Escape đóng (RUNTIME - PASS)
- [x] SG-AC-05: Hành động trong Lightbox: Sao chép, Đặt làm ảnh bìa instance, Tải về (RUNTIME - PASS)
- [x] SG-AC-06: Xóa ảnh chụp màn hình an toàn có dialog xác nhận (RUNTIME - PASS)
- [x] SG-AC-07: Thêm ảnh chụp màn hình mới qua Import file / kéo thả / Chụp demo (F2) (RUNTIME - PASS)
- [x] SG-AC-08: Nút "Mở thư mục screenshots" hiển thị toast thông báo (RUNTIME - PASS)
- [x] SG-AC-09: Responsive 390px/620px, Escape đóng dialog/lightbox, focus ring, reduced-motion <= 0.05s (RUNTIME - PASS)
- [x] SG-AC-10: Cảm giác tương tác trực quan mượt mà (MANUAL_REQUIRED - PASS: "xac nhan" 2026-10-27)
- [x] SG-AC-11: Không hồi quy: Web suite 100%, static check 100%, 0 `transition: all` (AUTOMATED - PASS)

## Tiến độ thực hiện
1. [x] **Orchestrator**: Tạo Verification Contract và Feature Document (`WEB_SPEC`).
2. [x] **Web Developer**: Triển khai UI/UX web prototype + bộ test Playwright `tests/web-e2e/screenshots-gallery.spec.ts` (pass 20/20 test).
3. [x] **Web Verifier**: Thẩm định runtime evidence độc lập, 20/20 Playwright pass, cập nhật contract.
4. [x] **User**: Nghiệm thu trực quan SG-AC-10 -> Nâng phase lên `WEB_VALIDATED`.
