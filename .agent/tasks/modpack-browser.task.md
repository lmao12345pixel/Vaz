# Task: Modpack Browser & 1-Click Install

- **Feature**: `modpack-browser`
- **Contract**: `docs/contracts/modpack-browser.contract.md`
- **Feature Doc**: `docs/product/features/modpack-browser.feature.md`
- **Phase**: `WEB_VALIDATED`
- **Owner**: Web Team
- **Date**: 2026-10-28

## Acceptance Criteria Checklist
- [x] MB-AC-01: Danh sách Modpack Cards hiển thị đầy đủ cover art/banner, icon, tên, tác giả, số lượng mod (vd: "45 mods"), dung lượng MB, lượt tải, loader badge và phiên bản MC (RUNTIME - PASS)
- [x] MB-AC-02: Bộ lọc Category chips (Tất cả, Hiệu năng, Phiêu lưu, Kỹ thuật, QoL) và các dropdown (Loader, Version, Sắp xếp) lọc chính xác danh sách và cập nhật `#modpacksCount` (RUNTIME - PASS)
- [x] MB-AC-03: Tìm kiếm modpack (`#modpacksSearch`) theo tên/tác giả/từ khóa, hiển thị empty state khi không có kết quả (RUNTIME - PASS)
- [x] MB-AC-04: Click "Chi tiết" mở Modpack Inspector Modal với đầy đủ Hero banner, mô tả chi tiết, danh sách mod trong pack và selector chọn phiên bản modpack (RUNTIME - PASS)
- [x] MB-AC-05: 1-Click "Cài đặt & Tạo Instance" hiển thị Modal tiến trình cài đặt (tải metadata, nạp loader, tải mods) và tự động tạo instance mới trong thư viện (RUNTIME - PASS)
- [x] MB-AC-06: Nút Hủy trong quá trình cài đặt modpack dừng tiến trình an toàn và đóng modal (RUNTIME - PASS)
- [x] MB-AC-07: Hỗ trợ nhập modpack từ file bên ngoài (.mrpack / .zip) qua nút "Nhập .mrpack" hoặc kéo thả file vào `#page-modpacks` (RUNTIME - PASS)
- [x] MB-AC-08: Responsive (390px, 620px) không tràn ngang; phím Escape đóng các modal; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s (RUNTIME - PASS)
- [x] MB-AC-09: Cảm giác tương tác trực quan, thanh tiến trình tải modpack và chuyển cảnh modal mượt mà theo design tokens (MANUAL_REQUIRED - PASS: User xác nhận "xac nhan" 2026-10-28)
- [x] MB-AC-10: Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%, 0 `transition: all` (AUTOMATED - PASS)

## Tiến độ thực hiện
1. [x] **Orchestrator**: Tạo Verification Contract và Feature Document (`WEB_SPEC`).
2. [x] **Web Developer**: Triển khai UI/UX web prototype + bộ test Playwright `tests/web-e2e/modpack-browser.spec.ts` + khắc phục responsive 390px/620px (20/20 test pass).
3. [x] **Web Verifier**: Thẩm định runtime evidence độc lập, kiểm tra Playwright.
4. [x] **User**: Nghiệm thu trực quan MB-AC-09 -> Nâng phase lên `WEB_VALIDATED`.
