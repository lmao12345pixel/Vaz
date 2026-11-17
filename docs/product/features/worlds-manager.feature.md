# Feature: Worlds Manager — Quản lý thế giới chơi đơn (Saves)

## Bối cảnh & Mục tiêu
Trong Minecraft, thế giới chơi đơn (saves/worlds) chứa toàn bộ tiến trình chơi, công trình xây dựng và dữ liệu địa hình của người chơi.
Launcher chuyên nghiệp (Prism, Modrinth) cung cấp bộ công cụ quản lý thế giới hoàn chỉnh:
- Xem danh sách thế giới với icon, game mode, độ khó, dung lượng và ngày chơi.
- Sao lưu tự động/thủ công (Backup .zip) và khôi phục (Restore).
- Đổi tên (Rename), nhân bản (Duplicate/Clone), xuất (Export .zip) và xóa an toàn (Delete).
- Nhập thế giới mới từ file `.zip` hoặc kéo thả trực tiếp.
- Kiểm tra chi tiết dữ liệu `level.dat` (NBT preview: Seed, Spawn, Game Rules, Version).

## Phạm vi (Scope)
- **Web Prototype**: Triển khai tại `#tab-worlds` trong `docs/design-preview/index.html`, `styles.css`, `app.js`.
- **E2E Automation**: Viết test suite `tests/web-e2e/worlds-manager.spec.ts` cho Chromium và Mobile Chrome.
- **Không chạm WinUI**: Toàn bộ logic desktop sẽ được port sau khi web đạt `WEB_VALIDATED`.

## User Stories & Acceptance Criteria

### User Story 1: Danh sách thế giới trực quan & Thống kê
Là người chơi, tôi muốn nhìn thấy danh sách các thế giới với icon trực quan, chế độ chơi (Survival, Creative, Hardcore), độ khó, dung lượng và thời gian chơi gần nhất để dễ dàng quản lý.

- **WM-AC-01**: Mỗi thẻ world hiển thị icon thế giới, tên thế giới, folder name, Game Mode badge, Difficulty, kích thước MB, lần chơi cuối và seed.

### User Story 2: Tìm kiếm & Lọc theo chế độ chơi
Là người chơi có nhiều thế giới, tôi muốn tìm kiếm theo tên hoặc lọc theo Game Mode để nhanh chóng tìm thấy thế giới mong muốn.

- **WM-AC-02**: Nhập từ khóa vào `#worldSearch` lọc danh sách theo tên/seed; bấm các chip lọc `#worldFilterChips` (Tất cả, Survival, Creative, Hardcore) lọc đúng danh sách và cập nhật số lượng thế giới hiển thị.

### User Story 3: Sao lưu & Khôi phục thế giới (Backup & Restore)
Là người chơi, tôi muốn sao lưu thế giới của mình thành file nén zip trước khi thử mod mới hoặc chuyển bản game, và có thể khôi phục lại bất kỳ lúc nào.

- **WM-AC-03**: Bấm nút Backup trên thẻ world hoặc "Backup all" hiển thị thanh tiến trình nén, tạo bản backup .zip và tăng bộ đếm backup của world.
- **WM-AC-04**: Bấm nút Khôi phục mở dialog danh sách các bản backup theo mốc thời gian và dung lượng; chọn bản backup và bấm Khôi phục cập nhật lại trạng thái world.

### User Story 4: Quản lý vòng đời (Đổi tên, Nhân bản, Xuất, Xóa)
Là người chơi, tôi muốn đổi tên thế giới, nhân bản để chơi thử nhánh mới, xuất ra file zip để chia sẻ hoặc xóa bỏ thế giới không còn dùng.

- **WM-AC-05**: Bấm "Đổi tên" cho phép sửa tên hiển thị; bấm "Nhân bản" tạo bản sao thế giới mới với hậu tố "(Copy)" và seed tương đương.
- **WM-AC-06**: Bấm "Xóa" mở dialog xác nhận cảnh báo mất dữ liệu vĩnh viễn; sau khi xác nhận world bị xóa khỏi danh sách và tổng dung lượng summary được cập nhật.

### User Story 5: Nhập thế giới từ bên ngoài (Import World)
Là người chơi tải save map từ mạng về, tôi muốn thêm nhanh file `.zip` bằng nút "Import World" hoặc kéo thả file vào launcher.

- **WM-AC-07**: Hỗ trợ input file `.zip` qua `#worldFileInput` hoặc kéo thả vào vùng dropzone, tạo world mới và hiển thị toast thông báo thành công.

### User Story 6: Level.dat NBT Inspector
Là người chơi kỹ thuật, tôi muốn xem các thông số hạt giống (Seed), tọa độ hồi sinh (Spawn X/Y/Z), phiên bản game tạo map và sao chép seed 1-click.

- **WM-AC-08**: Dialog Level.dat Inspector hiển thị đầy đủ Seed, Spawn, Version, GameType, Cheats và có nút sao chép seed 1-click.

### User Story 7: Trải nghiệm & Phản hồi thị giác
Giao diện Worlds Manager mượt mà, responsive trên mobile, tuân thủ design tokens.

- **WM-AC-09**: Responsive ở 390px và 620px không tràn ngang; phím Escape đóng dialog; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s.
- **WM-AC-10**: Cảm giác tương tác trực quan, thanh tiến trình backup và chuyển đổi dialog mượt mà theo design tokens. (`MANUAL_REQUIRED`)
- **WM-AC-11**: Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%. (`AUTOMATED`)
