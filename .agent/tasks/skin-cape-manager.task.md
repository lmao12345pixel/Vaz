# Task: Skin & Cape Manager

- **Feature**: `skin-cape-manager`
- **Contract**: `docs/contracts/skin-cape-manager.contract.md`
- **Feature Doc**: `docs/product/features/skin-cape-manager.feature.md`
- **Phase**: `WEB_SPEC`
- **Owner**: Web Team
- **Date**: 2026-10-28

## Acceptance Criteria Checklist
- [x] SCM-AC-01: Danh sách Skin Presets hiển thị dạng lưới thẻ (`.skin-card`) với thumbnail texture, tên, kiểu model (Classic/Slim) và badge "Đang dùng" (RUNTIME - PASS)
- [x] SCM-AC-02: Canvas mô hình 3D (`#skinViewer`) khởi tạo thành công, hỗ trợ bật/tắt tự động xoay (`#skinAutoRotateBtn`) (RUNTIME - PASS)
- [x] SCM-AC-03: Bộ điều khiển hoạt ảnh 3D (`#skinAnimGroup`): hỗ trợ các tư thế (Đi bộ `walk`, Bay `fly`, Rình rập `sneak`, Vẫy tay `wave`, Giương cung `bow`), cập nhật class `is-active` và hiển thị cánh Elytra khi bay (RUNTIME - PASS)
- [x] SCM-AC-04: Nút chuyển đổi Model tay (`#skinModelToggleBtn`): chuyển đổi qua lại giữa Classic (4px) và Slim (3px), cập nhật text và mô hình 3D (RUNTIME - PASS)
- [x] SCM-AC-05: Nút "Áp dụng skin" (`#applySkinBtn`) lưu skin vào tài khoản đang chọn kèm toast thông báo và cập nhật badge; nút "Mặc định" (`#resetSkinBtn`) khôi phục skin gốc (RUNTIME - PASS)
- [x] SCM-AC-06: Danh sách Áo choàng (#skinCapeBox): nhận diện tài khoản Premium có cape để hiển thị danh sách; ẩn hoàn toàn khi là tài khoản Offline hoặc tài khoản không có cape (RUNTIME - PASS)
- [x] SCM-AC-07: Thêm skin tùy chỉnh từ file PNG qua input file (`#skinFileInput`) hoặc kéo thả (`#skinDropZone`): kiểm tra kích thước 64x64/64x32, mở dialog nhập tên, thêm vào thư viện; hỗ trợ xóa custom skin (RUNTIME - PASS)
- [x] SCM-AC-08: Tải skin theo username Minecraft (`#skinGrabInput`, `#skinGrabBtn`): tải skin thành công từ tên người chơi, tự động nhận diện model, thêm vào danh sách (RUNTIME - PASS)
- [x] SCM-AC-09: Tải lên Cape tùy chỉnh (`#capeFileInput`, `#capeUploadBtn`): chọn file cape PNG, thêm vào danh sách cape kèm toast thông báo (RUNTIME - PASS)
- [x] SCM-AC-10: Responsive (390px, 620px) không tràn ngang; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s (RUNTIME - PASS)
- [ ] SCM-AC-11: Cảm giác tương tác 3D mượt mà, chuyển động xoay và đổi skin/cape trực quan theo design tokens (MANUAL_REQUIRED - BLOCKED: Chờ user nghiệm thu)
- [x] SCM-AC-12: Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%, 0 `transition: all` (AUTOMATED - PASS)

## Tiến độ thực hiện
1. [x] **Orchestrator**: Tạo Verification Contract và Feature Document (`WEB_SPEC`).
2. [x] **Web Developer**: Hoàn thiện UI/UX web prototype + bộ test Playwright `tests/web-e2e/skin-cape-manager.spec.ts` (22/22 test pass).
3. [x] **Web Verifier**: Thẩm định runtime evidence độc lập, kiểm tra Playwright.
4. [ ] **User**: Nghiệm thu trực quan SCM-AC-11 -> Nâng phase lên `WEB_VALIDATED`.
