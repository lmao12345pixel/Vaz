# Mods Manual Verification Checklist

Tài liệu này liệt kê các kịch bản kiểm thử cần thao tác thủ công trên máy thật hoặc phần cứng thực tế (chưa thể tự động hóa 100% bằng script/Playwright/winarm). Người dùng hoặc QA tick `[x]` khi hoàn thành kiểm chứng.

---

## 1. TÌM MOD (Modrinth Search & Browser)

- [ ] **MC-SM-01: Drag and Drop jar file từ File Explorer**
  - **Cách thử**: Mở Windows Explorer, kéo 1 hoặc nhiều file `.jar` thả trực tiếp vào cửa sổ App tại tab Mods.
  - **Kỳ vọng**: App nhận diện sự kiện Drop, hiển thị tiến trình sao chép, quét metadata và nạp vào danh sách mà không bị crash.

- [ ] **MC-SM-02: Hủy tải mod giữa chừng (User Cancel)**
  - **Cách thử**: Trong `ModBrowserDialog`, bấm "Cài" một mod có dung lượng lớn (ví dụ: Iris hoặc Physics Mod), ngay lập tức bấm nút "Hủy" bên cạnh ProgressBar.
  - **Kỳ vọng**: Tải dừng ngay lập tức, thông báo "Đã hủy tải", file tạm `.partial` trong thư mục `mods/` bị xóa sạch hoàn toàn, không để lại rác trên đĩa.

- [ ] **MC-SM-03: Rút dây mạng / Tắt Wi-Fi khi đang tải mod**
  - **Cách thử**: Bấm "Cài" mod, ngắt kết nối Internet trong lúc thanh tiến trình đang chạy.
  - **Kỳ vọng**: App hiển thị thông báo lỗi mạng rõ ràng trên InfoBar, không bị treo UI, file `.partial` được dọn dẹp sạch sẽ.

- [ ] **MC-SM-04: Cài đặt gói .mrpack thực tế qua Picker**
  - **Cách thử**: Bấm nút "Cài đặt Modpack (.mrpack)", chọn một file `.mrpack` tải từ Modrinth.
  - **Kỳ vọng**: Quá trình giải nén, kiểm tra zip-slip, kiểm tra host cdn.modrinth.com, tải các mod phụ thuộc và ghi manifest `mods.json` hoạt động trơn tru.

---

## 2. LOAD MOD (Quản lý mod đã cài)

- [ ] **MC-LD-01: Thao tác Bật/Tắt (Toggle) mod khi Game đang chạy**
  - **Cách thử**: Khởi chạy Minecraft từ instance, trong khi game đang chạy (file jar bị tiến trình `javaw.exe` khóa độc quyền), bấm toggle tắt hoặc xóa mod trong App.
  - **Kỳ vọng**: App xử lý ngoại lệ `IOException` an toàn, hiển thị thông báo lỗi thân thiện "File đang bị khóa bởi tiến trình khác (game đang chạy)", không sập app.

- [ ] **MC-LD-02: Xóa mod có hộp thoại xác nhận**
  - **Cách thử**: Bấm icon Thùng rác (Xóa) tại một dòng mod trong danh sách.
  - **Kỳ vọng**: Hiển thị ContentDialog yêu cầu xác nhận xóa file jar khỏi đĩa; bấm "Xóa" thì file bị xóa và biến mất khỏi danh sách; bấm "Hủy" thì giữ nguyên.

- [ ] **MC-LD-03: Kéo thả nhiều jar có mod trùng lặp**
  - **Cách thử**: Kéo 2 file `.jar` có cùng nội dung (cùng hash sha512) vào thư mục mods hoặc qua UI.
  - **Kỳ vọng**: App chỉ import 1 file và thông báo đã bỏ qua file trùng lặp.

- [ ] **MC-LD-04: Hiển thị mượt mà trên màn hình độ phân giải cao (HiDPI / Scaling 125%, 150%)**
  - **Cách thử**: Đổi Windows Display Scaling sang 125% hoặc 150%, quan sát giao diện tab Mods và dialog Mod Browser.
  - **Kỳ vọng**: Các icon, badge "Đã cài", text cảnh báo, inspector drawer co giãn sắc nét, không bị vỡ layout hoặc tràn viền.

---

## Ghi chú kiểm thử
- Người kiểm thử: _______________________
- Ngày kiểm thử: _______________________
- Phiên bản App: Vaz.WinUI 1.0.0
- Môi trường: Windows 11 / .NET 8 / WinUI 3
