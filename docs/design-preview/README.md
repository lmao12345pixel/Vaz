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
| **Instances** | Grid card 220×130, New Instance dialog, Play (có trạng thái), context menu chuột phải (Play / Edit / Open folder / Delete) |
| **Instance Detail** | Header + Launch, 3 tab **Mods / Logs / Settings**, toolbar phân nhóm có divider, mod list có toggle + badge update |
| **Logs** | Live log giả lập theo kiểu `latest.log`, Clear Log, Copy to Clipboard, Auto-scroll |
| **Settings** | InfoBar RAM/JVM (đúng trạng thái hiện tại), đường dẫn instance, Delete instance |
| **Accounts** | Card tài khoản + avatar Minotar 48×48, Set active, Xóa, Add Offline, Add Microsoft |
| **Mod Browser** | Dialog rộng, search debounce 300 ms (Enter để tìm ngay), filter Loader/MC/Sort, kết quả dạng list, cài mod có progress % và tự cài dependency (Fabric API) |
| **Install flows** | Cài mod → xuất hiện ngay trong tab Mods + reload vẫn còn; Check updates/Update all đổi version thật; Install .mrpack và OptiFine thêm mod vào instance |

## Chỉnh sửa

- **Mock data** — đầu file `app.js`, object `MOCK`: instances, mods, accounts, catalog Modrinth.
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
- Trạng thái mock lưu trong `localStorage`; bấm **Reset** trên thanh demo để quay lại dữ liệu gốc.
- Không có dark/light theo hệ thống — dùng nút **Dark/Light** ở thanh demo dưới cùng bên phải.
