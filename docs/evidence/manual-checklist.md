# Checklist Kiểm thử Thủ công WinUI (Manual Testing Checklist)

Tài liệu này cung cấp các bước kiểm thử thủ công từng bước dành cho người dùng trên môi trường WinUI desktop thực tế để nghiệm thu các hạng mục `MANUAL_REQUIRED` hoặc `UNVERIFIED`.

---

## 1. Kiểm thử Tỷ lệ Co giãn DPI (125% và 150%)
- **Mục tiêu**: Xác thực cửa sổ không vỡ layout, không tràn ngang và kích thước tối thiểu tuân thủ hệ số DPI (`WM_GETMINMAXINFO`).
- **Các bước thực hiện**:
  1. Chuột phải ngoài màn hình Desktop Windows > chọn **Display settings** (Cài đặt hiển thị).
  2. Tại mục **Scale** (Tỷ lệ), chuyển từ `100%` sang `125%`.
  3. Mở ứng dụng Vaz.WinUI, điều hướng vào instance bất kỳ > mở tab Resource Packs và Shaders.
  4. Thử co nhỏ cửa sổ hết mức có thể bằng chuột.
  5. Dùng thước đo pixel hoặc kiểm tra visual: Kích thước cửa sổ tối thiểu dừng lại ở `1200x662` (tương đương `960x1.25` và `530x1.25`). Các cột không bị đè chữ, nút bấm không bị xén.
  6. Lặp lại với Scale `150%`: Kích thước cửa sổ tối thiểu dừng lại ở `1440x795` (`960x1.5` và `530x1.5`).
- **Kết quả mong đợi**: Giao diện sắc nét, font chữ phóng to tỷ lệ chuẩn, layout co giãn đáp ứng không tràn ngang.

---

## 2. Kiểm thử Giảm chuyển động (Reduce Motion / AnimationsEnabled)
- **Mục tiêu**: Xác thực khi tắt hiệu ứng chuyển động trong Windows, ngăn kéo (drawer) mở tức thì không có độ trễ animation.
- **Các bước thực hiện**:
  1. Mở Windows Settings > **Accessibility** (Trợ năng) > **Visual effects** (Hiệu ứng hình ảnh).
  2. Tắt mục **Animation effects** (Hiệu ứng động) -> chuyển sang `Off`.
  3. Mở Vaz.WinUI > vào trang Instance Detail > tab Resource Packs.
  4. Nhấn vào nút "Xem chi tiết" (icon thông tin) trên một gói bất kỳ.
  5. **Quan sát**: Ngăn kéo thông tin bên phải xuất hiện ngay lập tức (thời gian hiển thị <= 0.05s), không chạy hiệu ứng trượt trễ.
  6. Bật lại **Animation effects** -> `On` trong Windows Settings, mở lại ngăn kéo: Quan sát hiệu ứng trượt mượt mà trở lại mà không cần khởi động lại ứng dụng Vaz.WinUI.

---

## 3. Kiểm thử Kéo thả Tệp .zip (Drag & Drop)
- **Mục tiêu**: Kiểm tra tính năng kéo thả file từ File Explorer vào ứng dụng với 3 kịch bản tệp khác nhau.
- **Các bước thực hiện**:
  1. Mở cửa sổ File Explorer bên cạnh cửa sổ Vaz.WinUI.
  2. **Kịch bản A (Resource Pack hợp lệ)**:
     - Kéo 1 tệp `.zip` có chứa `pack.mcmeta` (có trường `pack_format`) thả vào vùng danh sách Resource Packs.
     - Quan sát: Con trỏ chuột hiện icon Copy; khi thả, thông báo thành công xuất hiện và gói mới được nạp vào danh sách.
  3. **Kịch bản B (Shader Pack hợp lệ)**:
     - Chuyển sang tab Shaders. Kéo 1 tệp `.zip` có chứa thư mục `shaders/` thả vào danh sách Shaders.
     - Quan sát: Tệp được sao chép vào `shaderpacks/`, thẻ shader mới xuất hiện trong catalog.
  4. **Kịch bản C (Tệp zip hỏng / không đúng định dạng)**:
     - Tạo 1 file zip rỗng hoặc file văn bản đổi đuôi `.zip` (không có `pack.mcmeta` và không có thư mục `shaders/`).
     - Kéo thả vào ứng dụng.
     - Quan sát: Ứng dụng chặn sao chép, hiển thị InfoBar/thông báo lỗi tệp không hợp lệ hoặc thiếu metadata bắt buộc, không làm crash ứng dụng.

---

## 4. Kiểm thử Lưu trữ options.txt và Thứ tự Ưu tiên (AC-02 Persistence)
- **Mục tiêu**: Xác thực thứ tự gói tài nguyên được lưu đúng chuẩn Low-to-High trong `options.txt` và không làm mất các gói vanilla/mod.
- **Các bước thực hiện**:
  1. Trong tab Resource Packs, sắp xếp các gói đang dùng theo thứ tự từ trên xuống:
     - Vị trí `# 1` (cao nhất): Faithful 32x.
     - Vị trí `# 2`: Bare Bones.
  2. Đóng hoàn toàn ứng dụng Vaz.WinUI.
  3. Mở tệp `<InstancePath>/options.txt` bằng Notepad.
  4. Tìm dòng bắt đầu bằng `resourcePacks:`.
  5. **Quan sát cú pháp**:
     `resourcePacks:["vanilla","file/Bare Bones.zip","file/Faithful 32x.zip"]`
     (Gói ưu tiên `# 1` trong UI phải nằm ở cuối mảng để đè texture theo chuẩn nạp Minecraft). Mục `"vanilla"` và các mod packs có sẵn không bị xóa.
  6. Khởi động lại Vaz.WinUI: Thứ tự hiển thị vẫn giữ nguyên `# 1` Faithful 32x và `# 2` Bare Bones.

---

## 5. Kiểm thử Trải nghiệm Tương tác (RPS-AC-09 UX Feel)
- **Mục tiêu**: Cảm nhận trực quan độ mượt mà, phản hồi thị giác và âm thanh/focus theo design tokens.
- **Các bước thực hiện**:
  1. Dùng bàn phím: Bấm phím Tab di chuyển vào danh sách gói đang dùng > dùng `Alt+Down` và `Alt+Up` đổi chỗ các pack.
     - Quan sát: Focus bám theo thẻ đang chọn, không bị mất tiêu điểm, badge `# 1`, `# 2` đổi màu accent lập tức.
  2. Bấm vào một shader > Quan sát viền xanh ngọc lục bảo (Emerald accent `#10b981`) sáng lên, badge "Đang dùng" kích hoạt tức thì.
  3. Bấm "Tắt shader (Vanilla)" > Shader tắt mượt mà, nút tự chuyển sang trạng thái disabled (mờ đi).
  4. Đánh giá tổng thể: Phản hồi tức thì, không giật lag layout, màu sắc tuân thủ bảng thiết kế.
