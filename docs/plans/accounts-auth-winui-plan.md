# Kế hoạch Triển khai: Quản lý Tài khoản & Microsoft Auth (WinUI Parity)

## Bối cảnh & Mục tiêu
Tính năng Tài khoản (`accounts-auth`) đã đạt trạng thái `WEB_VALIDATED` trên Web Prototype (`docs/contracts/accounts-auth.contract.md`, 10/10 ACs PASS).
Kế hoạch này đưa tính năng từ `WEB_VALIDATED` sang `APP_IMPLEMENTED` và cuối cùng đạt `PARITY_VERIFIED` trên ứng dụng desktop WinUI 3 (`Vaz.WinUI`), tuân thủ nghiêm ngặt quy trình Dual-Target của dự án V.A.Z.

---

## Chi tiết các bước thực hiện

### Bước 1: Khởi tạo Parity Checklist & Cập nhật Task Ledger
1. Tạo `.agent/parity/accounts-auth.parity.md` ánh xạ 10 ACs từ `docs/contracts/accounts-auth.contract.md` sang tiêu chí kiểm thử trên WinUI.
2. Cập nhật `docs/contracts/accounts-auth.contract.md` bổ sung phần `App-side evidence mapping`.
3. Cập nhật `.agent/tasks/accounts-auth.task.md` chuyển sang phase porting WinUI.

### Bước 2: Nâng cấp ViewModel & Services (`Vaz.WinUI`)
1. **Nâng cấp `AccountRow`**:
   - `ShortUuid`: Cắt UUID rút gọn dạng `c069...c3d4` hoặc 8 ký tự đầu.
   - `TokenStatus`: Đối với Microsoft hiển thị `"Token: Hợp lệ (14 ngày)"` hoặc thời hạn thực tế; Offline hiển thị `"Offline"`.
   - `CapeText`: Hiển thị tên áo choàng hoặc `"Không có"`.
   - Bổ sung property xác định có phải Microsoft hay không để ẩn/hiện nút "Làm mới".
2. **Nâng cấp `AccountViewModel`**:
   - **Offline Validation (AUTH-AC-03)**:
     - Kiểm tra rỗng: *"Tên người chơi không được để trống"*.
     - Độ dài 3–16 ký tự: *"Tên người chơi phải từ 3 đến 16 ký tự"*.
     - Ký tự hợp lệ `^[a-zA-Z0-9_]+$`: *"Tên người chơi chỉ được chứa chữ cái, số và dấu gạch dưới"*.
     - Kiểm tra trùng lặp: *"Tài khoản này đã tồn tại"*.
   - **Xóa tài khoản & Tự động chuyển Active (AUTH-AC-07)**:
     - Khi xóa tài khoản đang active, tự động chọn tài khoản đầu tiên còn lại làm active (`_store.SetActiveAsync`).
     - Đồng bộ sự kiện `ActiveAccountChanged` để `MainViewModel` cập nhật Play Dock ngay lập tức.
   - **Đồng bộ Active Account sang Bottom Dock (AUTH-AC-05)**:
     - Kích hoạt cập nhật Play Dock khi đổi active account.
   - **Device Code Flow (AUTH-AC-01, AUTH-AC-02)**:
     - Tạo `DeviceCodeSession` sinh mã định dạng `[A-Z0-9]{4}-[A-Z0-9]{4}`, timer 15:00 đếm ngược.
     - Hỗ trợ copy mã vào Clipboard kèm thông báo.
     - Nút mở link `https://microsoft.com/link` trên trình duyệt mặc định.
     - Hỗ trợ hoàn tất xác thực (thật nếu có MSAL credentials hoặc mock simulation).
   - **Làm mới Token (AUTH-AC-06)**:
     - Gọi refresh token cho tài khoản Microsoft, cập nhật trạng thái hợp lệ và thông báo toast/status.

### Bước 3: Hoàn thiện Giao diện WinUI (`AccountPage.xaml` & Dialogs)
1. **Thẻ tài khoản (`AccountCard`)**:
   - Bổ sung UUID rút gọn và trạng thái Token.
   - Bổ sung Cape chip với icon/badge tương ứng.
   - Nút "Làm mới" hiển thị cho tài khoản Microsoft.
   - Nút "Xóa" hiển thị ContentDialog xác nhận: Tiêu đề `Xóa tài khoản "<Name>"?`, nút "Xóa" (Primary), nút "Hủy".
2. **Offline Dialog (`AddOfflineDialog`)**:
   - Input username, TextBlock thông báo lỗi validation trực tiếp màu đỏ (`SystemFillColorCriticalBrush`).
   - Validate ngay khi gõ hoặc khi bấm nút "Thêm".
3. **Microsoft Device Code Dialog (`DeviceCodeDialog`)**:
   - Khung mã lớn viền accent, định dạng `XXXX-XXXX`.
   - Nút sao chép mã 1-click vào Windows Clipboard.
   - Đồng hồ đếm ngược `mm:ss` chạy từng giây.
   - Nút mở liên kết `https://microsoft.com/link`.
   - Nút hoàn tất xác thực để tạo tài khoản.

### Bước 4: Kiểm tra Static & Đảm bảo Không Hồi quy
1. Chạy `dotnet build Vaz.sln` (0 error, 0 warning).
2. Chạy `dotnet test Vaz.sln` (đảm bảo toàn bộ test pass).
3. Chạy `npm run check:static` (PASS 100%).

### Bước 5: Runtime Desktop Smoke & Nghiệm thu
1. Khởi chạy app WinUI bằng `winarm`.
2. Thực hiện thao tác kiểm tra trực tiếp:
   - Thêm tài khoản Offline với validation (rỗng, ngắn, ký tự sai, trùng).
   - Mở Device Code dialog, copy mã, quan sát timer đếm ngược, hoàn tất xác thực.
   - Kiểm tra hiển thị UUID, Token status, Cape chip trên thẻ.
   - Đổi active account, kiểm tra Play Dock dưới đáy cập nhật tức thì.
   - Làm mới token tài khoản Microsoft.
   - Xóa tài khoản active, xác nhận dialog, kiểm tra tự động chuyển active sang tài khoản kế tiếp.
3. Cập nhật contract sang `PARITY_VERIFIED`, parity checklist sang `MATCH`, task sang `DONE`.
