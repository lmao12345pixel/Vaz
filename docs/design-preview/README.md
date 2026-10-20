# Vazer Launcher — UI prototype (web)

Bản demo giao diện chạy trên browser để **chỉnh sửa bố cục/spacing/màu nhanh** trước khi port sang WinUI.
Đây là **prototype**, không phải production: dữ liệu là mock, mọi thao tác được mô phỏng 1:1 với app (không mở game thật).
Trạng thái mock (instance/mod/account) được lưu trong `localStorage` để reload vẫn còn; nút **Reset** xóa sạch.

## Chạy

Cách 1 — mở trực tiếp:

```
start docs\design-preview\index.html
```

Cách 2 — serve qua HTTP (khuyến nghị nếu trình duyệt chặn clipboard/file):

```powershell
python -m http.server 5173 --directory docs/design-preview
# mở http://localhost:5173
```

Không cần `npm install`, không cần build.

## Màn hình đã dựng

| Màn hình | Nội dung |
| --- | --- |
| **Instances** | Grid card 240×152, search + sort (Chơi gần nhất / Tên A-Z / Mới tạo), New Instance dialog, Play/Stop có trạng thái + chip "Đang chạy", context menu chuột phải (Play/Stop / Edit / Rename / Open folder / Delete) |
| **Instance Detail** | Header + Launch/Stop, 4 tab **Mods / Worlds / Logs / Settings**, toolbar phân nhóm có divider, mod list có toggle + badge update + menu `⋯` |
| **Mods** | Check updates / Update all, menu từng mod: cập nhật riêng, chọn phiên bản (dialog radio), gỡ mod có xác nhận |
| **Worlds** | Danh sách world (size, lần chơi cuối, số backup), Backup từng world + Backup all có progress, Open worlds folder |
| **Logs** | Live log giả lập theo kiểu `latest.log` (có dòng JVM args/RAM/resolution), Clear Log, Copy to Clipboard, Auto-scroll |
| **Settings** | Form thật: RAM slider + số (1024–16384 MB), JVM args, Java path, độ phân giải, Lưu/Mặc định; đường dẫn instance, Delete instance |
| **Accounts** | Card tài khoản + avatar Minotar 52×52 bo góc, Set active, Xóa, Add Offline, Add Microsoft |
| **Mod Browser** | Dialog rộng, search debounce 300 ms (Enter để tìm ngay), filter Loader/MC/Sort, kết quả dạng list, cài mod có progress % và tự cài dependency (Fabric API) |
| **Install flows** | Cài mod → xuất hiện ngay trong tab Mods + reload vẫn còn; Check updates/Update all đổi version thật; Install .mrpack và OptiFine thêm mod vào instance |
| **Crash demo** | Nút **Simulate crash** trên thanh demo: dừng instance đang chạy, ghi log stack trace, mở dialog crash (exit code, đường dẫn crash report, nút mở report) |

## Tính năng mới (v2)

1. **Settings thật** — RAM/JVM/Java/resolution lưu theo từng instance; log Launch in đúng `-Xmx…M` và kích thước cửa sổ.
2. **Session control** — Play ↔ Stop ở card và header; chip "Đang chạy"; chặn launch trùng; Stop ghi log `Game stopped by user.`
3. **Per-mod actions** — menu `⋯` mỗi mod: cập nhật riêng (progress), chọn phiên bản, gỡ mod.
4. **Search/sort/rename** — tìm theo tên/MC/loader, sắp xếp, đổi tên instance, hiện "Chơi lần cuối".
5. **Worlds + backup** — tab Worlds, backup giả lập có progress và đếm số backup.
6. **Crash simulation** — test UX khi game crash mà không cần chạy game thật.


## Giao diện v3 — "Aurora cockpit"

- **Palette**: nền navy `#0b0f16`, accent teal `#5eead4` → sky `#7dd3fc` → indigo `#818cf8` (gradient `--grad-accent`). Light theme dùng teal `#0f9d8f` + indigo `#6366f1`.
- **Ambient**: aurora gradient trôi chậm 38s sau shell + film grain SVG; nav/titlebar/dialog dùng `backdrop-filter` (glass).
- **Motion**: token `--ease-out` / `--ease-spring`; card vào theo stagger (`--i`), hover nhấc 4px + spotlight theo con trỏ, page fade/slide, tab indicator trượt, switch knob spring, progress shimmer, dialog scale + blur, toast spring, nút nhấn 0.97.
- **Tôn trọng người dùng**: `prefers-reduced-motion: reduce` tắt animation/transition chuyển động.
- Muốn đổi nhanh "chất": sửa `--accent`, `--accent-2`, `--grad-accent`, `--ease-out` ở đầu `styles.css`; layout/effect nằm ở block `v3` cuối file.

## Chỉnh sửa

- **Mock data** — đầu file `app.js`, object `MOCK`: instances, mods, worlds, accounts, catalog Modrinth.
- **Design tokens** — đầu file `styles.css`: màu, radius, font. Đổi `:root` là đổi toàn bộ.
- **Layout từng màn** — `index.html` (markup tĩnh) + `styles.css` (phần tương ứng).
- **Hành vi** — `app.js`, các hàm `render*`, `open*`, `launchInstance`.

## Mapping sang WinUI

| Web | WinUI |
| --- | --- |
| `#page-instances` | `src/Vaz.WinUI/Views/InstancesPage.xaml` |
| `#page-detail` | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` |
| `#page-accounts` | `src/Vaz.WinUI/Views/AccountPage.xaml` |
| Mod Browser dialog | `src/Vaz.WinUI/Views/ModBrowserDialog.xaml` |
| `.btn`, `.textbox`, token màu | Fluent resources (`CardBackgroundFillColorDefaultBrush`, `ControlFillColorDefaultBrush`, …) |

Khi một bố cục đã ổn trên web, port sang XAML theo đúng thứ tự: token → spacing → control.

## Giới hạn

- Không gọi Modrinth/Mojang/Microsoft thật; chỉ tải avatar + icon qua CDN công khai.
- Không mở game thật — Launch chỉ mô phỏng tiến trình/PID và stream log.
- Backup world / crash report chỉ là mô phỏng UI; không tạo file thật.
- Thay đổi tên chỉ đổi tên hiển thị (app thật sẽ đổi tên thư mục).
- Trạng thái mock lưu trong `localStorage`; bấm **Reset** trên thanh demo để quay lại dữ liệu gốc.
- Không có dark/light theo hệ thống — dùng nút **Dark/Light** ở thanh demo dưới cùng bên phải.
