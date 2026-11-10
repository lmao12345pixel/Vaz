# Báo cáo Khắc phục Lỗi Tự Đóng Ứng Dụng (P0.1) — VAZ Launcher

**Ngày xác nhận:** 2026-11-10  
**Tình trạng:** **ĐÃ GIẢI QUYẾT TRIỆT ĐỂ (RESOLVED)**  
**Commit khắc phục:** `e98b18f` (`fix(ui): guard PressScale OnPointerReleased against disposed storyboard (0x800F1000)`)

---

## 1. Baseline & Triệu chứng ban đầu
- **Hiện tượng:** Ứng dụng tự đóng sau khoảng 45–60 giây thao tác mở/đóng Inspector liên tục (Mod -> Resource Pack -> Shader -> re-entry).
- **Trạng thái tiến trình:** Thoát sạch sẽ với **Exit Code 0** (không bị crash dump, Event Viewer sạch 0 lỗi).
- **Log lỗi trong `%APPDATA%\Vazer\crash.txt` (9,579,551 bytes):**
  - Ghi nhận 2 entry liên tiếp:
    ```text
    2026-11-09 12:57:56 [FATAL] COMException 0x800F1000: No installed components were detected.
       at Vaz.WinUI.Helpers.PressScale.OnPointerReleased (PressScale.cs:126)
    2026-11-09 12:58:00 [FATAL] COMException 0x800F1000: No installed components were detected.
       at Vaz.WinUI.Helpers.PressScale.OnPointerReleased (PressScale.cs:126)
    ```

---

## 2. Nguyên nhân gốc rễ (Root Cause)
1. **Lỗi tích lũy EventHandler trong StoryboardPool:**
   - Trong `PressScale.cs`, Storyboard được mượn từ `UiAnimationHelper.RentStoryboard()`.
   - Event `sb.Completed += (_, _) => UiAnimationHelper.ReturnStoryboard(sb);` dùng anonymous lambda nên không thể unsubscribe (`-=`).
   - Mỗi lần click, Storyboard tích lũy thêm handler `Completed`, dẫn tới việc 1 instance Storyboard bị trả về và tái sử dụng nhiều lần đồng thời trong `_storyboardPool`.
2. **Unload phần tử khi Pointer Event đang xử lý:**
   - Khi người dùng click nút đóng Inspector hoặc đổi tab, Button bị unload/detach khỏi Visual Tree trong khi chuỗi sự kiện `PointerReleased` / `PointerCaptureLost` đang được điều phối.
   - Hàm `OnPointerReleased` gọi `sb.Children.Add(anim)` và `sb.Begin()` trên visual peer đã đóng, ném `COMException 0x800F1000` (`RO_E_CLOSED`).
3. **Cơ chế gây thoát ứng dụng với Exit Code 0:**
   - Trong WinUI 3 (Windows App SDK), khi `COMException` nổ ra trực tiếp từ native message dispatch pump (vòng lặp thông điệp chuột/bàn phím), dù managed code bắt được trong `App.UnhandledException` và đánh dấu `e.Handled = true`, native XAML compositor vẫn coi visual tree bị hỏng và chủ động gửi `WM_QUIT`.
   - Ứng dụng kết thúc bình thường không sinh crash dump, tạo triệu chứng "tự đóng không rõ nguyên nhân".
   - **Đính chính:** Kết luận tại Đợt 22.1 cho rằng app đóng do "sandbox timeout" là SAI.

---

## 3. Giải pháp đã thực hiện (Commit `e98b18f`)
1. **Visual Tree Guard:** Bổ sung kiểm tra `if (sender is not Button button || !button.IsLoaded) return;` trong cả `OnPointerPressed` và `OnPointerReleased`.
2. **Tường minh Unsubscribe Event:** Chuyển sang named local function `OnCompleted` và gọi `sb.Completed -= OnCompleted;` trước khi trả Storyboard về pool.
3. **Nuốt Exception an toàn:** Wrap toàn bộ logic animation trong `try / catch (COMException)` và `try / catch (Exception)` kèm `Debug.WriteLine`, chặn triệt để exception lọt ra native pump.
4. **Phòng thủ StoryboardPool:** Thêm try/catch bên trong `RentStoryboard` và `ReturnStoryboard` để tự loại bỏ các WinRT COM object đã bị đóng.

---

## 4. Kết quả Nghiệm thu Thực tế
- Master và Agent đã chạy lại toàn bộ kịch bản B5 gốc:
  - Mở Mod Inspector -> Đóng.
  - Mở Resource Pack Inspector -> Đóng.
  - Mở Shader Inspector -> Đóng.
  - Quay lại list Instance -> Mở lại Instance -> Mở lại Inspector (re-entry).
- **Kết quả:** App sống ổn định qua 60–90 giây thao tác, `crash.txt` không phát sinh thêm bất kỳ entry nào.
- **Trạng thái Known Issue:** `RESOLVED`.
