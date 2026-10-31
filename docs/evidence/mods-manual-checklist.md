# Danh mục kiểm thử thủ công Mods (Mods Manual Verification Checklist)

Tài liệu này là nguồn duy nhất (Single Source of Truth) định nghĩa các kịch bản kiểm thử thủ công cho tính năng TÌM MOD và LOAD MOD, đòi hỏi thao tác tương tác người dùng, phần cứng hoặc tiến trình ngoài (chưa tự động hóa 100% bằng unit test hoặc headless runner).

---

## 1. TÌM MOD (Mod Browser & Modrinth)

- [ ] **MC-SM-01: Tải mod khi mạng chập chờn hoặc độ trễ cao**
  - **Bước thử**: Kết nối mạng có độ trễ cao (hoặc giới hạn băng thông), mở `ModBrowserDialog`, tìm và nhấn "Cài đặt" một mod.
  - **Kết quả mong đợi**: Thanh tiến trình cập nhật phần trăm và dung lượng mượt mà, thông báo trạng thái rõ ràng, UI không bị đơ giật.

- [ ] **MC-SM-02: Bấm nút Hủy trên giao diện trong lúc đang tải tệp mod**
  - **Bước thử**: Trong `ModBrowserDialog`, bấm "Cài đặt" một mod có dung lượng tương đối (ví dụ Create hoặc Sodium), bấm ngay nút "Hủy" cạnh thanh tiến trình.
  - **Kết quả mong đợi**: Quá trình tải dừng ngay lập tức, thông báo "Đã hủy tải", file tạm `.partial` trong thư mục `mods/` bị xóa sạch hoàn toàn, không tạo file jar rác.

- [ ] **MC-SM-03: Kéo thả gói .mrpack từ File Explorer vào cửa sổ ứng dụng**
  - **Bước thử**: Mở Windows Explorer, kéo file `.mrpack` hợp lệ thả vào vùng cửa sổ App.
  - **Kết quả mong đợi**: App nhận diện file modpack, hiển thị hộp thoại xác nhận thông tin gói (tên, phiên bản Minecraft, tóm tắt) và cho phép người dùng bấm "Cài" hoặc "Hủy".

---

## 2. LOAD MOD (Quản lý danh sách Mod)

- [ ] **MC-LD-01: Kéo thả tệp .jar từ Windows File Explorer vào tab Mods**
  - **Bước thử**: Kéo 1 hoặc nhiều file `.jar` từ Explorer thả vào vùng danh sách tab Mods của instance.
  - **Kết quả mong đợi**: App xử lý sự kiện Drag & Drop, sao chép file an toàn qua file tạm `.tmp-import`, quét metadata và hiển thị ngay trên danh sách.

- [ ] **MC-LD-02: Kéo thả tệp .jar trùng lặp nội dung từ File Explorer**
  - **Bước thử**: Kéo 2 file `.jar` có nội dung giống nhau (cùng sha512) hoặc trùng lặp với mod đã có sẵn vào app.
  - **Kết quả mong đợi**: App nhận diện trùng hash sha512, bỏ qua file trùng và thông báo cảnh báo thân thiện, không nhân bản file.

- [ ] **MC-LD-03: Thao tác Bật/Tắt (Toggle) mod khi Game đang chạy**
  - **Bước thử**: Bấm "Chơi" để khởi chạy game từ instance; trong khi tiến trình `javaw.exe` đang chạy và khóa file `.jar`, bấm gạt Toggle hoặc xóa mod trong app.
  - **Kết quả mong đợi**: App xử lý ngoại lệ `IOException` an toàn, hiển thị thông báo lỗi thân thiện "File đang bị khóa bởi tiến trình khác (game đang chạy)", app không bị crash.

- [ ] **MC-LD-04: Xóa mod có hộp thoại xác nhận**
  - **Bước thử**: Nhấn nút Thùng rác (Xóa) trên dòng mod bình thường trong danh sách.
  - **Kết quả mong đợi**: Hiển thị ContentDialog yêu cầu xác nhận "Gỡ cài đặt {Tên mod}?"; bấm "Xóa" thì file jar bị xóa khỏi đĩa và gỡ khỏi danh sách; bấm "Hủy" thì giữ nguyên.

- [ ] **MC-LD-05: Gỡ bỏ mục mod thiếu file ("Iris ma")**
  - **Bước thử**: Khi danh sách hiển thị mod có badge "Thiếu file" (ví dụ Iris có trong `mods.json` nhưng không có file jar trên đĩa), bấm nút Gỡ.
  - **Kết quả mong đợi**: Hiển thị hộp thoại "Gỡ {Tên mod} khỏi danh sách? Mục này không có file trên đĩa và sẽ được xóa khỏi cấu hình mods.json"; bấm "Gỡ bỏ" thì mod được xóa sạch khỏi `mods.json` và giao diện.

- [ ] **MC-LD-06: Hiển thị giao diện trên màn hình độ phân giải cao (HiDPI / Scaling 125%, 150%)**
  - **Bước thử**: Thiết lập Windows Display Scaling sang 125% và 150%, mở App, vào tab Mods và mở dialog Mod Browser.
  - **Kết quả mong đợi**: Các icon, badge "Thiếu file", "Đã cài", "Thiếu dependency", nhãn danh mục, inspector drawer và layout danh sách hiển thị sắc nét, co giãn đúng tỷ lệ, không bị vỡ layout hoặc tràn màn hình.
