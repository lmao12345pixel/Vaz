# Parity: Mod Manager Deep Flow — Web ↔ WinUI

- Contract: `docs/contracts/mod-manager.contract.md` (`PARITY_VERIFIED`, 2026-10-30)
- Web phase evidence: `tests/web-e2e/mod-manager.spec.ts` (9/9 pass trên chromium và mobile-chrome)
- App phase evidence: `dotnet build Vaz.sln` (0 warning, 0 error), `dotnet test Vaz.sln` (208 pass), static check PASS (build + 208 test + 12 agent configs), WinUI runtime desktop smoke PASS (9/9 ACs).
- Verifier: app-verifier (Verdict: MATCH 9/9 — 2026-10-30)

| Contract AC | Web evidence (hành vi) | App evidence (hành vi) | Status |
|-------------|------------------------|------------------------|--------|
| MM-AC-01 | Click mod row mở `#modInspector` hiển thị icon, tên, version, file, tác giả, mô tả, category, dependencies | Click mod row mở Mod Inspector (drawer 360px) đầy đủ icon, tên, version, file .jar, tác giả, mô tả, category, link Modrinth/Source | MATCH |
| MM-AC-02 | Badge `.badge.is-warning` "Thiếu dependency", nút "Cài dependency thiếu" (1-click resolve) cài Fabric API/Sodium và xóa badge | Badge warning "Thiếu dependency", Inspector có nút "Cài dependency thiếu" tự động cài/bật dependency và xóa badge | MATCH |
| MM-AC-03 | Chips Category (Tất cả, Hiệu năng, Đồ họa, Công nghệ, Sinh tồn/QoL, Phép thuật) lọc danh sách và cập nhật bộ đếm | Category chips WrapPanel lọc danh sách mod theo nhóm, cập nhật text bộ đếm "Hiển thị X / Y mods" | MATCH |
| MM-AC-04 | Cài đặt từ Catalog tự động phát hiện dependency và hiển thị dialog "Cài đặt kèm dependencies?" | Catalog (`ModBrowserDialog`) phát hiện mod có dependency thiếu, hiển thị ContentDialog gợi ý cài kèm và cài đặt đồng thời | MATCH |
| MM-AC-05 | Tắt/gỡ mod đang là dependency của mod khác hiển thị cảnh báo ảnh hưởng | Tắt ToggleSwitch hoặc gỡ mod là dependency (e.g., Sodium khi Sodium Extra bật) hiển thị ContentDialog/InfoBar cảnh báo ảnh hưởng | MATCH |
| MM-AC-06 | Hỗ trợ thêm file mod .jar từ bên ngoài qua input file và kéo thả | Toolbar có nút "+ Thêm .jar" (FileOpenPicker) và hỗ trợ Drag & Drop file .jar trực tiếp vào danh sách mod | MATCH |
| MM-AC-07 | Responsive 390px/620px không tràn ngang, phím Escape đóng Inspector, focus ring rõ | Responsive co giãn cửa sổ không tràn ngang, phím Escape đóng Inspector, visual focus rõ ràng | MATCH |
| MM-AC-08 | Chuyển động mở/đóng Inspector và modal mượt mà theo design tokens | Hiệu ứng chuyển động mượt mà theo tokens và cờ `ReduceMotion` của WinUI | MATCH |
| MM-AC-09 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100% | `npm run check:static` PASS, unit test 208/208 PASS, không lỗi biên dịch; các trang Instances, Accounts, Settings hoạt động ổn định | MATCH |
