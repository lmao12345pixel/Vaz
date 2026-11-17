# Feature: Logs Viewer & Export

- **Mã tính năng**: `logs-viewer`
- **Trạng thái**: Draft / `WEB_SPEC`
- **Phạm vi**: Web Prototype (`docs/design-preview/`) -> Port WinUI sau
- **Ngày tạo**: 2026-10-28

---

## 1. Bối cảnh & Mục tiêu

Khi khởi chạy Minecraft và các modpack lớn, hệ thống sinh ra hàng nghìn dòng nhật ký (logs) ghi lại chi tiết quá trình nạp tài nguyên, thư viện, mod và các ngoại lệ khi xảy ra lỗi.
Tab `#tab-logs` trong Instance Detail cần cung cấp một trình xem log mạnh mẽ, rõ ràng, dễ phân tích:
1. **Tô màu cú pháp thông minh (Syntax Highlighting)**: Tách bạch rõ Timestamp `[HH:mm:ss]`, Thread `[Render thread/INFO]`, Logger name và nội dung thông báo. Phân biệt màu sắc trực quan giữa INFO (xanh lục/dương nhạt), WARN (vàng hổ phách), ERROR/Exception (đỏ hồng).
2. **Lọc và tìm kiếm nhanh**: Lọc theo cấp độ nghiêm trọng (ALL, INFO, WARN, ERROR), tìm kiếm từ khóa tức thì với số lượng kết quả đếm trực quan.
3. **Điều hướng tiện lợi**: Nút nhảy đến lỗi đầu tiên ("First Error") với hiệu ứng highlight flash, nút nhảy xuống cuối log ("Jump to bottom"), hỗ trợ Auto-scroll tự động cuộn khi log mới xuất hiện.
4. **Xuất và chia sẻ**: Nút sao chép toàn bộ log vào bộ nhớ tạm (Clipboard), nút tải về file `latest.log` để người chơi dễ dàng gửi lên Discord hoặc forum hỗ trợ, nút mở thư mục logs.
5. **Dọn dẹp và thử nghiệm**: Nút xóa log, nút nạp log mẫu (Generate Demo Logs) sinh dữ liệu đa dạng để kiểm chứng giao diện.
6. **Responsive và Accessibility**: Tự động gói dòng (word-wrap) trên màn hình nhỏ (390px/620px), không tràn ngang, viền `:focus-visible` rõ ràng, tôn trọng reduced motion.

---

## 2. User Stories

### US-1: Xem log với màu sắc rõ ràng
Là người chơi Minecraft,
Tôi muốn xem log game với định dạng màu sắc trực quan (INFO, WARN, ERROR),
Để tôi nhanh chóng nhận biết game đang chạy bình thường hay gặp sự cố.

### US-2: Lọc và tìm kiếm lỗi
Là người chơi hoặc người tạo modpack,
Tôi muốn lọc riêng các dòng ERROR hoặc tìm kiếm tên mod gây lỗi,
Để tôi khoanh vùng nguyên nhân crash mà không phải cuộn thủ công hàng nghìn dòng.

### US-3: Xuất và gửi log hỗ trợ
Là người chơi,
Tôi muốn có nút 1-click để sao chép log hoặc tải file `latest.log` về máy,
Để tôi dễ dàng chia sẻ cho nhóm hỗ trợ hoặc đính kèm vào ticket báo lỗi.

---

## 3. Tiêu chí nghiệm thu (Acceptance Criteria)

- **LV-AC-01**: Hiển thị log stream với syntax highlighting theo dòng (Timestamp, Thread, Log Level `[INFO]`, `[WARN]`, `[ERROR]`, message) và line numbers.
- **LV-AC-02**: Bộ lọc log theo cấp độ (`ALL`, `INFO`, `WARN`, `ERROR`) hiển thị đúng số lượng dòng và cập nhật `#logCount`.
- **LV-AC-03**: Tìm kiếm trong log (`#logSearch`) lọc danh sách dòng khớp và cập nhật số lượng đếm.
- **LV-AC-04**: Điều hướng nhanh: Nút "Lỗi đầu tiên" (`#jumpFirstErrorBtn`) cuộn đến dòng ERROR đầu tiên kèm hiệu ứng flash; nút "Cuối log" (`#logJumpBtn`) cuộn xuống dòng mới nhất.
- **LV-AC-05**: Auto-scroll checkbox (`#autoScrollChk`): khi bật tự động cuộn xuống đáy khi có dòng log mới; khi tắt giữ nguyên vị trí scroll.
- **LV-AC-06**: Xuất log: Nút "Copy to Clipboard" (`#copyLogBtn`) sao chép toàn bộ log (hoặc log đã lọc) kèm toast thông báo; nút "Tải về .log" (`#exportLogBtn`) tải file `latest.log` về máy.
- **LV-AC-07**: Nút "Xóa log" (`#clearLogBtn`) làm sạch khung log hiển thị empty state `#logsEmpty`; nút "Tạo log mẫu" (`#generateLogBtn`) sinh log mẫu phong phú (khởi động, tải mod, cảnh báo, lỗi) để kiểm thử.
- **LV-AC-08**: Nút "Mở thư mục logs" (`#openLogsFolderBtn`) hiển thị toast thông báo mở thư mục `.minecraft/logs`.
- **LV-AC-09**: Responsive (390px, 620px) không tràn ngang; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s.
- **LV-AC-10**: Cảm giác tương tác trực quan, cuộn log và tô màu cú pháp mượt mà theo design tokens. (MANUAL_REQUIRED)
- **LV-AC-11**: Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%, 0 `transition: all`. (AUTOMATED)
