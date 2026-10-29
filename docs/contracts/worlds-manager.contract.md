---
feature: worlds-manager
status: WEB_VALIDATED
created: 2026-10-27
updated: 2026-10-27
verifier: web-verifier
---

# Verification Contract: Worlds Manager (Quản lý thế giới chơi đơn)

## Bối cảnh & Mục tiêu
Tính năng Worlds Manager trong Instance Detail cho phép người chơi quản lý toàn diện các thế giới chơi đơn (Minecraft saves): xem danh sách trực quan, tìm kiếm/lọc game mode, sao lưu nén zip, khôi phục backup, đổi tên, nhân bản, xuất zip, xóa an toàn, nhập world từ bên ngoài và soi dữ liệu Level.dat NBT.

## Verification Contract Table

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế | Status |
|---|----------------------|---------------|-----------|------------------|--------|
| WM-AC-01 | Danh sách World Cards hiển thị đầy đủ icon thế giới, tên, folder name, Game Mode badge (Survival/Creative/Hardcore), Difficulty, Last played, Size MB và Seed | RUNTIME | Playwright: mở tab Worlds, assert danh sách thẻ world hiển thị icon, tên, folder, badge game mode, size MB và seed | Playwright `worlds-manager.spec.ts` (test 1 & 10) PASS cả chromium (3.3s) & mobile-chrome (5.5s): 5 world cards, icon, badges, seeds hiển thị chính xác | PASS |
| WM-AC-02 | Tìm kiếm thế giới theo tên/seed (`#worldSearch`) và lọc theo Game Mode chip (Tất cả, Survival, Creative, Hardcore) | RUNTIME | Playwright: nhập search, click các chip filter, assert danh sách thế giới lọc chính xác và `#worldsSummary` cập nhật số lượng | Playwright `worlds-manager.spec.ts` (test 2 & 11) PASS cả chromium (5.4s) & mobile-chrome (6.4s): search "Creative" (1/5), seed "-771928401928", chips survival (2), creative (1), hardcore (1), all (5) | PASS |
| WM-AC-03 | Sao lưu thế giới (Backup) tạo bản nén .zip kèm thanh tiến trình và tăng bộ đếm backup của world | RUNTIME | Playwright: click nút Backup trên card hoặc "Backup all", theo dõi progress bar, assert toast thông báo và số backup tăng | Playwright `worlds-manager.spec.ts` (test 3 & 12) PASS cả chromium (5.7s) & mobile-chrome (7.3s): toast "Đã backup Survival 1.21", count tăng 0->1, backup all toast PASS | PASS |
| WM-AC-04 | Khôi phục thế giới (Restore) mở dialog chọn các bản sao lưu đã có với timestamp & dung lượng, khôi phục thành công | RUNTIME | Playwright: click nút Khôi phục, chọn bản backup trong dialog, bấm Khôi phục, assert toast thành công | Playwright `worlds-manager.spec.ts` (test 4 & 13) PASS cả chromium (4.5s) & mobile-chrome (6.8s): dialog mở hiển thị 2 bản backup, khôi phục thành công, last played "Vừa xong" | PASS |
| WM-AC-05 | Đổi tên thế giới (Rename) và Nhân bản thế giới (Duplicate/Clone) cập nhật tên, folder và state danh sách | RUNTIME | Playwright: đổi tên world qua dialog rename; bấm nhân bản world -> assert world mới xuất hiện kèm hậu tố "(Copy)" | Playwright `worlds-manager.spec.ts` (test 5 & 14) PASS cả chromium (4.3s) & mobile-chrome (5.4s): rename cập nhật tên & folder "/saves/the_gioi_sinh_ton_moi", duplicate tạo card "Creative Test (Copy)" | PASS |
| WM-AC-06 | Xóa thế giới (Delete) yêu cầu dialog xác nhận nguy hiểm; xóa thành công loại bỏ world khỏi instance và cập nhật bộ đếm summary | RUNTIME | Playwright: click Xóa world, assert dialog cảnh báo, xác nhận xóa -> assert thẻ world biến mất và summary giảm | Playwright `worlds-manager.spec.ts` (test 6 & 15) PASS cả chromium (4.4s) & mobile-chrome (5.4s): dialog cảnh báo vĩnh viễn, thẻ "Adventure Dungeon" bị xóa, summary giảm từ 5 xuống 4 | PASS |
| WM-AC-07 | Nhập thế giới từ bên ngoài (Import World `.zip` hoặc kéo thả file zip) tự động giải nén và thêm world mới vào instance kèm toast | RUNTIME | Playwright: set file zip qua input `#worldFileInput` hoặc trigger drop event, assert world mới xuất hiện trong danh sách | Playwright `worlds-manager.spec.ts` (test 7 & 16) PASS cả chromium (3.2s) & mobile-chrome (4.8s): import qua input tạo "SkyBlock Warrior", dropzone drag/drop tạo "Castle Siege Adventure" | PASS |
| WM-AC-08 | Level.dat Inspector hiển thị đầy đủ NBT data (Seed, Spawn coords, GameTime, Version, Generator, Cheats) kèm nút sao chép seed 1-click | RUNTIME | Playwright: click "Xem Level.dat", assert dialog inspector chứa đầy đủ seed, spawn X/Y/Z, version; click copy seed assert toast | Playwright `worlds-manager.spec.ts` (test 8 & 17) PASS cả chromium (4.5s) & mobile-chrome (6.5s): inspector hiển thị seed, spawn X/Y/Z, Minecraft 1.21.1, copy seed toast xuất hiện | PASS |
| WM-AC-09 | Responsive (390px, 620px) không tràn ngang; phím Escape đóng dialog; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s | RUNTIME | Playwright: đo scrollWidth <= clientWidth tại 390px và 620px; outline focus >= 2px; Escape đóng dialog; reduced motion | Playwright `worlds-manager.spec.ts` (test 9 & 18) PASS cả chromium (3.4s) & mobile-chrome (5.5s): scrollWidth <= clientWidth tại 390/620px, Escape đóng dialog, outline >= 2px, duration <= 0.05s | PASS |
| WM-AC-10 | Cảm giác tương tác trực quan, thanh tiến trình sao lưu và chuyển đổi dialog mượt mà theo design tokens | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, chuyển sang tab Worlds, thao tác sao lưu, đổi tên, chi tiết và chụp ảnh nghiệm thu | User xác nhận nguyên văn: "xac nhan" (2026-10-27) | PASS |
| WM-AC-11 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100% | AUTOMATED | Chạy `npm run check:web` và `npm run check:static` | `check:static` (exit 0): 0 err/warn, 192 tests pass, 12 agent configs OK. `check:web` (exit 0): 176/176 tests pass (chromium + mobile-chrome) | PASS |

## Quy tắc Gate Dual-target
1. Prototype Web là executable specification; WinUI implement sau theo contract.
2. Không chuyển sang `WEB_VALIDATED` nếu bất kỳ AC nào chưa PASS (WM-AC-10 đang BLOCKED chờ user sign-off).
3. Không port WinUI khi contract chưa đạt `WEB_VALIDATED`.
