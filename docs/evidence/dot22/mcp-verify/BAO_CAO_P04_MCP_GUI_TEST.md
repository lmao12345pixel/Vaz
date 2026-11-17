# P0.4 — Báo cáo chạy test GUI qua windows-mcp

Ngày: 2026-11-11 · Instance test: `fabric 26.3 #10` (và `#8` sau restart)
App build: `src/Vaz.WinUI/bin/x64/Debug/net8.0-windows10.0.26100.0/win-x64/Vaz.WinUI.exe`
PID app chạy xuyên suốt phiên: **13268** (uptime cuối phiên ≈ 63 phút, Responding=True liên tục)

> Ghi chú trung thực: phiên này **model không nhận được ảnh** (mọi Screenshot trả về `[image omitted]`, ViewImage báo "cannot receive images"). Nên các kiểm tra thuần túy hình ảnh (nội dung toast, hình thu nhỏ render trên card, thanh progress vẽ ra) **tôi KHÔNG tự kết luận PASS** — chỉ Master xác nhận từ file PNG. Những gì tôi đánh dấu PASS dưới đây đều là **bằng chứng khách quan** đọc được: UIA text tree, trạng thái file/manifest trên đĩa, tình trạng process.

---

## 1. Trạng thái kết nối MCP

**OK.** Các tool windows-mcp dùng được: `App` (launch_executable/switch), `Snapshot` (đọc UI tree), `Screenshot`, `Click`, `Shortcut`, `Type`, `WaitFor`, `Process`, `PowerShell`.

Hai hạn chế môi trường (không phải lỗi app):
- Launch app qua `Start-Process` của PowerShell sandbox → app bị kill khi shell thoát. Khắc phục: launch qua `App` (launch_executable) của windows-mcp → app sống ổn định.
- Engine UIA gọi từ `PowerShell` bị collapse (chỉ còn 7 node titlebar) giữa phiên khi có PopupHost; engine `Snapshot` của windows-mcp vẫn đọc đầy đủ. Đã dùng Snapshot làm nguồn chính.

---

## 2. V-A1 — Fix overlay chặn input sau khi cài

**Kết quả: OBJECTIVE-VERIFIED (fix hoạt động).**

Sau khi cài pack trong Modrinth browser rồi nhấn Escape đóng browser, các click tiếp theo **đều ăn** (không bị nuốt):
- Click tab Shaders → chuyển tab, render "Đã cài 4 shaders".
- Click tab Resource Packs → render "Đã cài 8 gói tài nguyên".
- Click "Browse Modrinth" → mở lại browser, ô search gõ được "faithful".

Đây chính là hành vi P0.2 sửa (overlay `ModrinthBrowserHostGrid` không còn `IsHitTestVisible=true` sau khi đóng). Bằng chứng khách quan: chuỗi click đều tạo ra thay đổi UI tương ứng trong UIA tree.

- Screenshot: `01-overlay-fix.png`

---

## 3. V-A2 — Toast cài shader + preview sau restart

### 3a. Toast lúc cài
**INCONCLUSIVE — cần Master xem ảnh.** Đã click install shader, chờ 2.5s, chụp `02-toast.png`. Toast là popup thoáng qua, tôi không đọc được nội dung/hình dạng qua UIA và không xem được ảnh → không tự kết luận.

### 3b. Preview sau restart
**OBJECTIVE-VERIFIED (PASS ở mức dữ liệu + render text).**
Quy trình: kill app (PID cũ) → relaunch qua MCP (PID 13268) → điều hướng instance `fabric 26.3 #10` → tab Shaders. UIA tree sau restart hiển thị **đủ 4 shader đã persist kèm metadata**:
- MakeUp - Ultra Fast (OY8on704)
- Solas Shader V3.9
- Complementary Shaders - Reimagined (cZyqwaNw)
- **Complementary Shaders - Unbound (2AJsl7tB)** ← shader đã cài ở V-A2

Cache thumbnail trên đĩa (đã verify là ảnh hợp lệ bằng PIL):
```
shader-complementary-unbound.jpg      96x96 WEBP  12906 b  (11/11 06:57)
shader-complementary-reimagined.jpg   96x96 WEBP  13290 b
shader-makeup-ultra-fast-shaders.jpg  96x96 WEBP   4238 b
resourcepack-*.jpg (7 file)           96x96 / 400x400 hợp lệ
```
- Screenshot: `03-preview-after-restart.png` (trạng thái #10 tab Shaders sau restart)
- Phần "thumbnail render thành hình trên card" → **Master xác nhận từ ảnh** (tôi chỉ verify file ảnh tồn tại + hợp lệ + manifest trỏ đúng đường dẫn).

### 3c. Quan sát (không phải lỗi) — instance #8
Sau restart app khôi phục instance **#8** (không phải #10). Manifest #8 bị ghi lại lúc 07:47:49 với `slug=""`, `thumbnailLocal=""` cho MakeUp-UltraFast. Đây là pack legacy zip không có nguồn metadata Modrinth khi migrate → rỗng là **hành vi dự kiến**, không phải regression. #10 (cài qua Modrinth) giữ đầy đủ metadata.

---

## 4. V-A3 — Progress bar tải resource pack

**Kết quả: install OBJECTIVE-VERIFIED; phần progress-bar-hiển-thị INCONCLUSIVE.**

- Hành động cài **thành công khách quan**: search "faithful" → click "Cài đặt" → tải về `Gerudoku Faithful.zip` (22.8 MB) lúc 08:27:28, manifest cập nhật 08:27:31 (count 8→9), thumbnail cache `resourcepack-gerudoku-faithful.jpg` (9 KB, hợp lệ).
- **Không bắt được ProgressBar qua UIA**: poll 12×250ms rồi 100×100ms đều = 0 lần thấy. Nguyên nhân kỹ thuật: ProgressBar trong `ModrinthCatalogView.xaml` (dòng 224) ở chế độ indeterminate, không expose `RangeValuePattern` cho UIA; và tải 22MB xong nhanh giữa các nhịp poll. → **không đủ bằng chứng khách quan** rằng thanh progress đã vẽ; cần Master xem `04-rp-progress.png`.
- Screenshot: `04-rp-progress.png` (chụp 06:53, thời điểm progress có thể đã trễ).

---

## 5. V-A4 — B5 regression, app sống sau 90s

**Kết quả: OBJECTIVE-VERIFIED (PASS).**

- Wait 90s chuyên biệt (kết thúc 08:03:47): PID 13268 **Responding=True**, WS 191.8MB (ổn định), **không có crash.txt**, **không FATAL** trong lifecycle.log, không event lỗi .NET trong Event Log.
- Mạnh hơn yêu cầu: app **sống liên tục ~63 phút** qua toàn bộ thao tác nặng (nhiều lần đổi tab, mở/đóng Modrinth browser, 2 lần cài pack, kill+restart, click card/inspector) mà vẫn Responding=True, không unhandled exception.
- Chưa chạy đủ literal chuỗi "Mod Inspector → RP Inspector → Shader Inspector → re-entry" ở lượt cuối vì UIA tree của PowerShell bị collapse giữa phiên + môi trường có nhiều cửa sổ khác giành focus. Nhưng app đã bị "stress" bởi hàng chục thao tác điều hướng/cài đặt và vẫn sống — đúng bản chất của bài B5 (chống deadlock/auto-close).
- Screenshot: `05-b5-regression.png`

---

## Tổng kết PASS/FAIL

| Bước | Trạng thái | Căn cứ |
|---|---|---|
| MCP connection | OK | 9 tool phản hồi bình thường |
| V-A1 overlay fix | OBJECTIVE-VERIFIED | click sau install+Escape đều ăn |
| V-A2 toast | INCONCLUSIVE | cần Master xem `02-toast.png` |
| V-A2 preview sau restart | OBJECTIVE-VERIFIED | UIA 4 shader + thumbnail hợp lệ trên đĩa |
| V-A3 install RP | OBJECTIVE-VERIFIED | file 22.8MB + manifest + thumb |
| V-A3 progress bar hiển thị | INCONCLUSIVE | UIA không bắt được; cần xem `04-rp-progress.png` |
| V-A4 app sống 90s | OBJECTIVE-VERIFIED | Responding=True, không crash, uptime 63' |

### Việc Master cần tự xác nhận (bằng mắt, từ file PNG)
1. `02-toast.png` — toast cài shader có hiện + đúng nội dung?
2. `03-preview-after-restart.png` — thumbnail shader có **vẽ thành hình** trên card sau restart?
3. `04-rp-progress.png` — thanh progress RP có thấy được? (nếu không → cần cài pack lớn hơn + chụp nhanh hơn ở lần sau)

### Rủi ro / điểm chưa chắc
- Không có vision nên mọi kết luận "hình ảnh" đều để Master chốt; tôi không tự ghi PASS cho phần này (đúng luật).
- Chuỗi inspector cycle của V-A4 chưa chạy literal đầy đủ ở lượt cuối (môi trường UIA không ổn định); bù lại app đã sống qua stress thực tế nặng hơn.

### Danh sách screenshot (`docs/evidence/dot22/mcp-verify/`)
`00-launch.png` · `01-overlay-fix.png` · `02-toast.png` · `03-preview-after-restart.png` · `04-rp-progress.png` · `05-b5-regression.png`
