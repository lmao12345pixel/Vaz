## Tổng quan PR
PR bao gồm toàn bộ các commit trên nhánh `feat/browse-popular-mods` cải tiến, củng cố độ bền vững và hoàn thiện các module cốt lõi của V.A.Z Launcher.

---

### Trạng thái tính năng theo chuẩn hóa:

1. **Cài đặt (Settings)**:
   - **Nhãn**: `PARTIAL`
   - Phạm vi: Cấu hình RAM, thư mục game/Java, theme, options.txt, lưu trữ cấu hình. Đã kiểm thử unit test logic và cấu hình; hạ từ nhãn cũ xuống `PARTIAL` do chưa có log/video runtime hoàn chỉnh cho mọi tương tác UI.

2. **Tài khoản (Accounts)**:
   - **Nhãn**: `CHƯA KIỂM TAY`
   - Phạm vi: Microsoft OAuth2 / Xbox Live flow và tài khoản Offline. Chế độ Offline đã kiểm; Microsoft OAuth2 chưa kiểm tra đăng nhập bằng tài khoản Microsoft thật trên môi trường thực tế.

3. **Quản lý Mod (Mod Manager & Load Mod)**:
   - **Nhãn**: `CÓ LỖI ĐANG MỞ`
   - Phạm vi: Bật/tắt mod, xóa mod, quét metadata jar, cảnh báo trùng mod ID kèm `(game có thể không chạy)`, thay thế jar cũ khi cập nhật và rollback an toàn khi lỗi/hủy/sai hash SHA512.
   - Trạng thái lỗi: Đã vá lỗi phantom Iris bằng badge "Thiếu file", khóa toggle và bổ sung nút "Gỡ" khỏi danh sách; loại trừ mod thiếu file khỏi `HasShaderLoader`. Bổ sung ảnh bằng chứng màn hình `LD-07` và `LD-08` (1025x533 và 960x530 ở 96 DPI / 100% display scale). Các kịch bản khóa file khi game chạy (MC-LD-03) và HiDPI (MC-LD-06) tiếp tục theo dõi trong checklist thủ công.

4. **Tìm và Tải Mod (Modrinth Search & Download)**:
   - **Nhãn**: `CHƯA KIỂM TAY`
   - Phạm vi: Duyệt mod nổi tiếng khi ô tìm trống, tìm kiếm debounce 300ms, tải an toàn qua stream `.partial` trong thư mục đích (bảo vệ lượt tải đang chạy khỏi orphan cleanup), badge "Đã cài" dựa trên file thật trên đĩa, cập nhật hàng loạt bằng hash.
   - Trạng thái kiểm thử: Đã pass 327 test tự động (.NET 8); bỏ tuyên bố runtime smoke do chưa có log/bằng chứng runtime riêng; chờ kiểm thử thủ công theo `MC-SM-01..06`.

5. **Gói tài nguyên & Shaders (Resource Packs / Shaders)**:
   - **Nhãn**: `CHƯA KIỂM TAY`
   - Trạng thái contract: `PARITY_PARTIAL`.
   - Các mục hoãn / chờ hoàn thiện: AC-05 shader profile (Low/Medium/High/Ultra), nút 1-click cài đặt Iris tự động từ Modrinth, sắp xếp thứ tự ưu tiên in-game, kéo thả nhiều gói và kiểm tra HiDPI.

---

### Tình trạng CI (GitHub Actions)
- **Số lần chạy trên nhánh `feat/browse-popular-mods`**: 6 lần.
- **Trạng thái**: XANH (GREEN - Success 100%, sàn MIN_TESTS: 320, tổng 327 tests pass).
- **Phạm vi commit**: Toàn bộ nhánh `feat/browse-popular-mods` theo dõi vào `main` thuộc phạm vi PR #1 (bao gồm cả commit `373bc51` và run `41159049111`).
  1. Run [#41170099637](https://github.com/lmao12345pixel/Vaz/actions/runs/41170099637) (commit `655595f`, 327 tests passed): **Thành công (Success)**.
  2. Run [#41168784317](https://github.com/lmao12345pixel/Vaz/actions/runs/41168784317) (commit `05a08f0`, 327 tests passed): **Thành công (Success)**.
  3. Run [#41159049111](https://github.com/lmao12345pixel/Vaz/actions/runs/41159049111) (commit `373bc51`, 326 tests passed - trong phạm vi PR): **Thành công (Success)**.
  4. Run [#41156024313](https://github.com/lmao12345pixel/Vaz/actions/runs/41156024313) (commit `65af265`, 325 tests passed): **Thành công (Success)**.
  5. Run [#41148553563](https://github.com/lmao12345pixel/Vaz/actions/runs/41148553563) (commit `93610fa`, 322 tests passed): **Thành công (Success)**.
  6. Run [#41103856268](https://github.com/lmao12345pixel/Vaz/actions/runs/41103856268) (commit `ba21505`, 322 tests passed): **Thành công (Success)**.

---

### Lỗi đã biết / Việc chưa xong (Checklist thủ công Đợt 3):
- Kịch bản gạt toggle khi game đang chạy (`javaw.exe` khóa file) cần test thủ công trên máy thật (`MC-LD-03`).
- Đăng nhập Microsoft OAuth2 với tài khoản Microsoft thật (`CHƯA KIỂM TAY`).
- Khả năng co giãn giao diện hiển thị ở mức phóng to HiDPI 125%, 150% (`MC-LD-06`).
- Thử nghiệm kéo thả tệp từ ngoài Explorer trên desktop cảm ứng/tablet (`MC-LD-01`, `MC-SM-03`).
- Tắt hẳn mạng khi tìm mod (`MC-SM-04`), gõ tiếng Việt Unikey/Telex (`MC-SM-05`), duyệt mod phổ biến khi ô tìm trống (`MC-SM-06`).

---

### Liên kết tài liệu kiểm chứng (Evidence Links):
- [Sổ theo dõi chi tiết (mods-open-items.md)](https://github.com/lmao12345pixel/Vaz/blob/feat/browse-popular-mods/docs/evidence/mods-open-items.md)
- [Checklist kiểm thử thủ công (mods-manual-checklist.md)](https://github.com/lmao12345pixel/Vaz/blob/feat/browse-popular-mods/docs/evidence/mods-manual-checklist.md)
- [Log xuất CI Run 41159049111 (ci-run-41159049111.txt)](https://github.com/lmao12345pixel/Vaz/blob/feat/browse-popular-mods/docs/evidence/ci-run-41159049111.txt)
- [Diff 4 commit củng cố (diff-4-commits.txt)](https://github.com/lmao12345pixel/Vaz/blob/feat/browse-popular-mods/docs/evidence/diff-4-commits.txt)

---
**AI self-review, đã đồng bộ bằng chứng và số liệu thực tế, chờ chủ sản phẩm duyệt.**
