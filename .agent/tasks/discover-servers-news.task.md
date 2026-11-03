# Task: Discover (Server Browser & News Feed)

- **Feature**: `discover-servers-news`
- **Contract**: `docs/contracts/discover-servers-news.contract.md`
- **Feature Doc**: `docs/product/features/discover-servers-news.feature.md`
- **Phase**: `WEB_VALIDATED`
- **Owner**: Web Team
- **Date**: 2026-10-27

## Acceptance Criteria Checklist
- [x] DSN-AC-01: Danh sách Server Cards hiển thị icon, tên, IP (kèm copy), badge, MOTD đa màu, players, ping bars (RUNTIME - PASS)
- [x] DSN-AC-02: Bộ lọc Category chips & dropdown lọc (Tất cả, Yêu thích, Online) (RUNTIME - PASS)
- [x] DSN-AC-03: Tìm kiếm server theo tên/IP/version và empty state (RUNTIME - PASS)
- [x] DSN-AC-04: Thêm server mới qua Dialog với kiểm tra ping (RUNTIME - PASS)
- [x] DSN-AC-05: Sửa server, xóa server an toàn, toggle favorite (RUNTIME - PASS)
- [x] DSN-AC-06: Nút Chơi ngay (Join) và Sao chép IP (RUNTIME - PASS)
- [x] DSN-AC-07: News Carousel với Next/Prev, dots, auto-play, pause on hover (RUNTIME - PASS)
- [x] DSN-AC-08: Dialog đọc bài viết tin tức chi tiết kèm nút hành động (RUNTIME - PASS)
- [x] DSN-AC-09: Responsive 390px/620px, Escape đóng dialog, focus-visible >= 2px, reduced-motion <= 0.05s (RUNTIME - PASS)
- [x] DSN-AC-10: Cảm giác tương tác trực quan mượt mà (MANUAL_REQUIRED - PASS: "xac nhan" 2026-10-27)
- [x] DSN-AC-11: Không hồi quy: Web suite 100%, static check 100%, 0 `transition: all` (AUTOMATED - PASS)

## Tiến độ thực hiện
1. [x] **Orchestrator**: Tạo Verification Contract và Feature Document (`WEB_SPEC`).
2. [x] **Web Developer**: Triển khai UI/UX web prototype + bộ test Playwright `tests/web-e2e/discover-servers-news.spec.ts` (20/20 test pass).
3. [x] **Web Verifier**: Thẩm định runtime evidence độc lập, kiểm tra Playwright (20/20 passed, AC-01 đến AC-09 và AC-11 PASS; DSN-AC-10 BLOCKED; giữ phase `WEB_SPEC`).
4. [x] **User**: Nghiệm thu trực quan DSN-AC-10 -> Nâng phase lên `WEB_VALIDATED`.
