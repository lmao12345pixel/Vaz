# Parity: Quản lý Tài khoản & Microsoft Auth — Web ↔ WinUI

- Contract: `docs/contracts/accounts-auth.contract.md` (`PARITY_VERIFIED`, app-verifier 2026-10-29)
- Web phase evidence: `npx playwright test tests/web-e2e/accounts-auth.spec.ts` (chromium + mobile-chrome, 8 test/project PASS, 16/16)
- App phase evidence: `dotnet build Vaz.sln` (0 warning, 0 error), `dotnet test Vaz.sln` (208/208 PASS), static check PASS (build + 208 test + 12 agent configs), WinUI runtime desktop smoke (10/10 ACs PASS).
- Verifier: app-verifier (Verdict: MATCH 10/10 — 2026-10-29)

| Contract AC | Web evidence (hành vi) | App evidence (hành vi) | Status |
|-------------|------------------------|------------------------|--------|
| AUTH-AC-01 | Click "Add Microsoft" mở Modal Device Code Flow `#msAuthModalHost` với mã, nút copy, countdown timer và link | WinUI ContentDialog Device Code hiển thị mã `XXXX-XXXX`, nút Sao chép, đếm ngược `mm:ss`, nút Mở link trình duyệt | MATCH |
| AUTH-AC-02 | Sao chép mã hiển thị toast; hoàn tất xác thực thêm tài khoản Microsoft mới và đóng modal | Nút Sao chép lưu vào Windows Clipboard kèm visual feedback; hoàn tất xác thực tạo tài khoản Microsoft mới và đóng dialog | MATCH |
| AUTH-AC-03 | Click "Add Offline" mở dialog form có validation (rỗng, 3-16 ký tự, ký tự đặc biệt, trùng lặp) | WinUI ContentDialog Offline Account có validation InfoBar báo lỗi trực quan, chặn tạo tên sai (rỗng, <3, đặc biệt, trùng) | MATCH |
| AUTH-AC-04 | Thẻ tài khoản hiển thị đầy đủ avatar face, badge loại (Microsoft/Offline), UUID rút gọn, token status, Cape chip | Thẻ tài khoản WinUI hiển thị Avatar 52x52 bo tròn 8px, badge loại, UUID rút gọn `xxxx...xxxx`, Token status, Cape chip | MATCH |
| AUTH-AC-05 | Đổi tài khoản Active cập nhật badge trên thẻ và đồng bộ ngay lập tức sang thanh điều khiển dưới đáy (`#userBtn`) | Bấm "✓ Đặt Active" cập nhật badge thẻ và đồng bộ tức thời sang Play Dock (`ActiveAccountName`, avatar, status dot) | MATCH |
| AUTH-AC-06 | Nút "Refresh token" trên tài khoản Microsoft làm mới token thành công và cập nhật trạng thái | Nút "Làm mới" trên tài khoản Microsoft gọi refresh token, hiển thị tooltip và cập nhật trạng thái hạn token | MATCH |
| AUTH-AC-07 | Xóa tài khoản yêu cầu xác nhận; xóa tài khoản đang active tự động chuyển active sang tài khoản kế tiếp | ContentDialog xác nhận (`DefaultButton=Primary`, nhãn "Xóa"); xóa active tự động chọn tài khoản kế tiếp và cập nhật Play Dock | MATCH |
| AUTH-AC-08 | Responsive không tràn ngang, phím Escape đóng dialog, outline focus rõ ràng, reduced motion | WinUI ScrollViewer thích ứng kích thước cửa sổ 800px; Escape đóng ContentDialog; hỗ trợ keyboard navigation | MATCH |
| AUTH-AC-09 | Cảm giác chuyển động mượt mà khi mở/đóng dialog và chuyển đổi tài khoản | Hiệu ứng ContentDialog Fluent và chuyển đổi Play Dock mượt mà; user đã xác nhận nghiệm thu | MATCH |
| AUTH-AC-10 | Không hồi quy: build/test pass 100%, các trang khác không bị ảnh hưởng | `dotnet test Vaz.sln` 208/208 tests pass; `npm run check:static` pass; Instances và Settings hoạt động ổn định | MATCH |
