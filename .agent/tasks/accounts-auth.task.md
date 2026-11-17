# Task: Quản lý Tài khoản & Microsoft Device Code Authentication (WinUI Port)

## Trạng thái
- Phase: `PARITY_VERIFIED`
- Status: `DONE`
- Contract: `docs/contracts/accounts-auth.contract.md` (status: `PARITY_VERIFIED`, 2026-10-29)
- Feature spec: `docs/product/features/accounts-auth.feature.md`
- Parity checklist: `.agent/parity/accounts-auth.parity.md` (10/10 MATCH)
- Owner: app-developer
- Verifier: app-verifier (PASS)

## Bảng tiêu chí nghiệm thu (WinUI App)

| # | Acceptance criterion | Evidence type | Cách kiểm trên WinUI | Status |
|---|----------------------|---------------|----------------------|--------|
| AUTH-AC-01 | Click "Add Microsoft" mở Modal Device Code Flow với mã `XXXX-XXXX`, nút copy, timer đếm ngược `mm:ss` và link | RUNTIME | Bấm "Thêm tài khoản Microsoft", assert Device Code dialog xuất hiện với đầy đủ phần tử | PASS |
| AUTH-AC-02 | Sao chép mã Device Code hiển thị thông báo; hoàn tất xác thực thêm tài khoản Microsoft mới và đóng modal | RUNTIME | Bấm sao chép, kiểm tra clipboard; bấm hoàn tất tạo tài khoản và đóng dialog | PASS |
| AUTH-AC-03 | Click "Add Offline" mở dialog form có validation (rỗng, 3-16 ký tự, ký tự đặc biệt, trùng lặp) | RUNTIME | Nhập các trường hợp lỗi assert hiển thị text thông báo đỏ; nhập tên đúng tạo tài khoản thành công | PASS |
| AUTH-AC-04 | Thẻ tài khoản hiển thị đầy đủ avatar face, badge loại (Microsoft/Offline), UUID rút gọn, token status, Cape chip | RUNTIME | Quan sát thẻ tài khoản có đủ Avatar 52x52, badge loại, UUID rút gọn, Token status, Cape chip | PASS |
| AUTH-AC-05 | Chuyển đổi tài khoản Active (`Set as Active`) cập nhật badge thẻ và đồng bộ tức thì sang Play Dock dưới đáy | RUNTIME | Bấm "Đặt Active", assert badge đổi vị trí và Play Dock cập nhật tên + avatar mới ngay lập tức | PASS |
| AUTH-AC-06 | Nút "Refresh token" trên tài khoản Microsoft làm mới token thành công và cập nhật trạng thái | RUNTIME | Bấm "Làm mới", assert InfoBar thông báo thành công và thời hạn token được cập nhật | PASS |
| AUTH-AC-07 | Xóa tài khoản yêu cầu xác nhận; nếu xóa tài khoản active thì tự động chuyển active sang tài khoản kế tiếp | RUNTIME | Xóa tài khoản active, xác nhận dialog "Xóa", assert tài khoản biến mất và tài khoản kế tiếp tự động thành active | PASS |
| AUTH-AC-08 | Thích ứng kích thước cửa sổ, phím Escape đóng các modal/dialog, outline focus rõ ràng | RUNTIME | Thử phím Escape đóng dialog, resize cửa sổ không tràn ngang, cuộn dọc mượt mà | PASS |
| AUTH-AC-09 | Cảm giác chuyển động mượt mà khi thao tác | MANUAL_REQUIRED | User xác nhận trải nghiệm chuyển động WinUI đạt chất lượng | PASS |
| AUTH-AC-10 | Không hồi quy: Build pass, unit tests pass 100%, static check pass | AUTOMATED | `dotnet test Vaz.sln` pass; `npm run check:static` pass | PASS |

## Các bước triển khai (WinUI Port)
1. [x] Lập kế hoạch `docs/plans/accounts-auth-winui-plan.md`.
2. [x] Tạo Parity Checklist `.agent/parity/accounts-auth.parity.md`.
3. [x] Cập nhật contract `docs/contracts/accounts-auth.contract.md` với App evidence mapping.
4. [x] Nâng cấp `AccountRow` & `AccountViewModel` (UUID rút gọn, Cape, Token status, Offline validation, Auto-switch active khi xóa, đồng bộ Play Dock).
5. [x] Hoàn thiện Device Code Flow dialog & Offline dialog với validation trực tiếp trên UI.
6. [x] Chạy static check & unit tests.
7. [x] Chạy runtime desktop smoke qua WinArm, nghiệm thu parity và user xác nhận.
8. [x] Đóng cổng tính năng: contract `PARITY_VERIFIED`, parity checklist `10/10 MATCH`, task ledger `DONE`.

