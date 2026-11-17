# Bảng theo dõi kiểm thử thủ công Mods — Đợt 3 (Mods Manual Verification Run)

Tài liệu này dùng cho người dùng / kiểm thử viên thực hiện test tay trên môi trường máy thật ở Đợt 3. Toàn bộ các ca ban đầu đều ở trạng thái `UNVERIFIED`. Người kiểm thử chỉ đổi sang `PASS` hoặc `FAIL` khi đã trực tiếp quan sát và đính kèm bằng chứng thực tế.

---

## Bảng kiểm thử 8 ca thủ công (Đợt 3)

| Mã ca | Mô tả kịch bản | Môi trường (OS, DPI, phiên bản app) | Kết quả (PASS/FAIL/UNVERIFIED) | Ảnh/log đính kèm | Ghi chú | Người xác nhận | Ngày |
|---|---|---|:---:|---|---|---|---|
| **MC-SM-01** | Tải mod khi mạng chập chờn / độ trễ cao; thanh tiến trình % và byte mượt mà, không đơ UI | | UNVERIFIED | | | | |
| **MC-SM-02** | Bấm nút "Hủy" khi file mod đang tải dở; dừng tức thì, xóa sạch file `.partial` trong `mods/` | | UNVERIFIED | | | | |
| **MC-SM-03** | Kéo thả gói `.mrpack` từ Windows File Explorer vào cửa sổ ứng dụng | | UNVERIFIED | | | | |
| **MC-SM-04** | Tắt hẳn mạng (ngắt Wi-Fi/cáp) rồi tìm mod; báo lỗi mạng gọn gàng, có nút "Thử lại" | | UNVERIFIED | | | | |
| **MC-SM-05** | Gõ tiếng Việt bằng Unikey/EVKey Telex vào ô tìm kiếm; không mất chữ, không nhân đôi ký tự | | UNVERIFIED | | | | |
| **MC-SM-06** | Mở Download Mods lúc ô tìm kiếm trống; nạp mod phổ biến đúng loader và phiên bản MC của instance | | UNVERIFIED | | | | |
| **MC-LD-03** | Bật/tắt mod khi game đang chạy (`javaw.exe` khóa file jar); bắt IOException an toàn, không crash app | | UNVERIFIED | | | | |
| **MC-LD-06** | Co giãn giao diện trên màn hình HiDPI (bắt buộc test cả hai mức **125%** và **150%**); layout không vỡ/tràn | *(Bắt buộc test cả 125% và 150%)* | UNVERIFIED | | Cần ảnh/log ở 125% và 150% mới đóng được ca | | |

---

## Hướng dẫn ghi nhận kết quả:
1. **Kết quả**: Chỉ ghi `PASS` khi mọi bước thực hiện đúng kết quả mong đợi; ghi `FAIL` nếu phát hiện lỗi hoặc vỡ layout; giữ `UNVERIFIED` nếu chưa kiểm tra.
2. **Ảnh/log đính kèm**: Lưu ảnh chụp màn hình hoặc file log vào thư mục `docs/evidence/` với tên quy chuẩn (ví dụ: `MC-LD-06-hidpi-125.png`, `MC-LD-06-hidpi-150.png`).
3. **Người xác nhận & Ngày**: Ghi rõ người thao tác thực tế và ngày giờ thực hiện.
