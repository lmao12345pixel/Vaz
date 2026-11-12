# P0.6 — Điều tra + fix thumbnail LowOnFire không hiện

Ngày: 2026-11-12 · Đợt: P0.6 · Repo: `C:\OptizimeProject\Vaz` · Branch: `feat/browse-popular-mods`
Người thực hiện: Raez (agent) · Người giao/duyệt: Master

Tiền đề: P0.5b PASSED (`fde15dc`, `0ca5d0a`, 498 test). Master đã sửa tay manifest #10 và restart app, nhưng card LowOnFire vẫn là khung xám. Tag v0.19.0 chờ fix này.

---

## Bước 1 — Manifest thực tế sau khi Master fix tay

File: `%APPDATA%\Vazer\Minecraft\instances\fabric 26.3 #10\resourcepacks.json` (4487 B, mtime 2026-11-11 21:11:06).

| Trường | Giá trị |
|---|---|
| `fileName` | `LowOnFire v26.2§8.zip` |
| `slug` | `low-on-fire` |
| `thumbnailLocal` | `thumbnails/resourcepack-low-on-fire.jpg` |
| `thumbnailRemote` | `null` |
| `installedAt` | `2026-11-11T06:53:48.6801617+07:00` |

- JSON **parse OK** (Python `json.loads`, 9 packs). Không BOM. Ký tự `§` mã hoá UTF-8 đúng (`\xc2\xa7`) — PowerShell **không làm hỏng** encoding.
- Cả 9 `fileName` trong manifest đều `File.Exists = True`; không có file zip nào thiếu entry.
- Thumbnail folder có `resourcepack-low-on-fire.jpg`: **2073 B**, mtime 11/11 06:53:49, định dạng **PNG 400×400** (header `89 50 4E 47`, WIC đọc được `Bgra32`) dù mang đuôi `.jpg`. Ảnh hợp lệ, 78 màu, 26% là đỏ đậm `(101,0,0)`.
- Backup Master tạo: `resourcepacks.json.bak-211042` (4446 B) — không cần restore.

**Kết luận B1: DATA ĐÚNG.** Lỗi nằm ở code.

### So sánh với Gerudoku (card đang hiện ảnh)
Gerudoku có `thumbnailRemote` = URL Modrinth **và** `pack.png` 128×128 (2410 màu). LowOnFire có `thumbnailLocal` nhưng **không có** `thumbnailRemote`, và `pack.png` chỉ **16×16** (75 màu, 114/256 pixel trong suốt). Đây là mấu chốt.

---

## Bước 2 — Root cause: **CODE**, 3 tầng chồng lên nhau

File: `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`

**Tầng 1 — dòng 286 (trước fix):** `IconUri = entry.ThumbnailRemote ?? string.Empty`.
Chỉ đọc `ThumbnailRemote`, **bỏ qua hoàn toàn `ThumbnailLocal`**. Trong khi nhánh shader (dòng 543–551) đã làm đúng: ưu tiên local, resolve relative → absolute, fallback remote. LowOnFire `remote = null` → `IconUri = ""`.

**Tầng 2 — dòng 49–64 `ResourcePackRow.IconImageSource`:** trả `CustomIconSource` **trước** `IconUri`. Mà `LoadThumbnailInBackground` (dòng 768 cũ) luôn nạp `pack.png` từ zip và gán `row.CustomIconSource`. Vì vậy **mọi** thumbnail từ manifest (local lẫn remote) đều là code chết với resource pack; ảnh trên card luôn là `pack.png`. LowOnFire `pack.png` 16×16 với 45% trong suốt, phóng lên khung 64×64 → đúng là "khung xám" Master thấy. Gerudoku hiện ảnh vì `pack.png` của nó 128×128 đủ nét.

**Tầng 3 — định dạng URI:** sửa `IconUri` thành path thô `C:\...` chưa đủ. `BitmapImage` WinUI cần URI; và `SetSourceAsync` từ stream bị từ chối với file PNG/WEBP giấu dưới đuôi `.jpg`. Cơ chế đúng đã có sẵn trong chính app: `InstanceDetailViewModel.cs` dòng 485 dùng `new Uri(iconDest).AbsoluteUri` (`file:///...`) cho mod icon.

Placeholder xám là `Assets/blocks/grass_block_gray.png` (128×128, 8314 B) — `Helpers/Converters.cs` dòng 140–143.

Không phải cache: `LoadResourcePacksCoreAsync` gọi `EnsureManifestWithMigrationAsync` mỗi lần vào tab; manifest mtime không đổi sau các lần chạy app (vẫn 21:11:06), entry persist.

---

## Bước 3 — Fix

Hai commit, tách theo từng tầng:

**`497ce8e` — `fix(packs): resolve thumbnail local path for resource pack cards`**
- Thêm `ResolvePackThumbnailUri` + `ResolveLocalThumbnailPath` (local → absolute, kiểm tra `File.Exists`, fallback remote, fallback empty).
- Dòng 286 đổi từ `ThumbnailRemote` sang helper.
- +4 test trong `PackManifestServiceTests.cs`.

**`204f69d` — `fix(packs): render cached pack thumbnail over zip pack.png`**
- `ResolvePackThumbnailUri` trả `new Uri(local).AbsoluteUri` (`file:///...`).
- `IconImageSource`: nếu `IconUri` là `file://` → ưu tiên `BitmapImage(uri)` **trước** `CustomIconSource`; sau đó fallback `CustomIconSource` (pack.png), rồi remote, rồi placeholder.
- `LoadThumbnailInBackground(row, zipPath, string? localThumbnailPath = null)`: ưu tiên đọc file cache, fallback `pack.png` trong zip. Giữ nguyên call site "+ Thêm .zip" (dòng 474) nhờ tham số optional.
- Test cập nhật assertion: `StartWith("file:///")`, `Uri.IsFile`, `LocalPath`.

**Tại sao có 2 commit:** fix v1 (`497ce8e`) là cần nhưng **chưa đủ** — Raez đã chụp pixel ngay sau khi build và thấy `thumbnail-red-px = 0`, placeholder vẫn còn. Bằng chứng trung thực lưu tại `p06-fix-v1-check.png` / `p06-card-crop-v1.png`. `204f69d` mới là fix hoàn chỉnh.

**Test:** `dotnet test tests/Vazer.Core.Tests` = **502 passed / 0 failed** (498 → 502, +4). Build `Vaz.WinUI` **0 warning / 0 error**.

### Fullbright — KHÔNG cùng root cause
- Không có thumbnail mồ côi: `thumbnails/` không chứa file nào tên `fullbright*` hay `fub*`.
- Entry Fullbright: `slug = fullbright-ub` (Master đã set), `thumbnailLocal = null`, `thumbnailRemote = null`.
- `pack.png` của chính pack này là **512×512 với 97% pixel xám** (mean RGB 46,46,46) — tức ảnh gốc của pack vốn xám. Sau fix nó vẫn được hiển thị qua fallback `CustomIconSource`, đó là hành vi đúng.
- **Không fix Fullbright**: không có dữ liệu ảnh nào để hiện và không được tự tải từ mạng trong phạm vi đợt này. Nếu Master muốn ảnh đẹp hơn, cần cài lại gói từ Modrinth để có `thumbnailLocal`/`thumbnailRemote`.

---

## Bước 4 — Verify bằng pixel analysis + UIA + restart

App được launch bằng `App(launch_executable)` của windows-mcp (PID 19268 → 19824 → restart 11576), **không** qua PowerShell sandbox.

Vùng đo: crop 74×80 quanh thumbnail card (Border 64×64 + badge "Đã cài"), toạ độ lấy từ UIA bounds.

| Chỉ số (exact-match) | Trước fix | Sau fix | Sau restart |
|---|---|---|---|
| Thumbnail LowOnFire signature (14 màu đặc trưng) | **7 px** | **2090 px (35.3%)** | **2090 px (35.3%)** |
| Placeholder `grass_block_gray` signature (12 màu) | **488 px** | **0 px** | **0 px** |
| Unique colors vùng card | 692 | **325** | **328** |

- Gate yêu cầu unique > 50 → **PASS (325 / 328)**.
- Top màu sau fix khớp trực tiếp file thumbnail: `(101,0,0)` 833 px, `(110,0,0)` 502 px, `(255,255,255)` 173, `(250,239,185)` 104, `(234,192,62)` 78.
- `placeholder-sig` về **0** → khung xám biến mất hoàn toàn.

**UIA text sau restart** (DESC=75, filter "LowOnFire"):
```
Text    'Đã cài'                 (316,414)
Text    'LowOnFire v26.2§8'      (372,365)
Button  'Mở file'                (599,380)
Button  'Xóa gói'                (633,380)
```
Card vẫn ở trạng thái installed. Master yêu cầu button "Xem chi tiết" — card RP dùng 2 nút `Mở file` / `Xóa gói` (tên "Xem chi tiết" thuộc card shader/mod), ghi rõ để tránh nhầm.

**Restart verify: PASS.** Kill PID 19824 → relaunch PID 11576 → vào lại instance #10 / tab RP / filter → chỉ số pixel y hệt (2090 / 0 / 328), manifest không bị ghi đè.

### Ảnh bằng chứng (`docs/evidence/dot22/mcp-verify/`)
`p06-baseline-before-fix.png` · `p06-fix-v1-check.png` (fix v1 chưa đủ) · `p06-card-crop-v1.png` · `06-lowonfire-after-fix.png` · `p06-card-crop-after-fix.png` · `07-lowonfire-after-restart.png` · `p07-card-crop-after-restart.png`.

### Hạn chế môi trường (không phải lỗi app)
- Model không nhận ảnh → mọi kết luận hình ảnh dựa trên đếm pixel/chữ ký màu, không phải "nhìn" ảnh. Kết luận ở mức **CÓ/KHÔNG CÓ ẢNH**, đúng ràng buộc P0.6.
- UIA tree nhiều lúc collapse còn 2–9 node (PopupHost) và cửa sổ bị Windows cascade dịch vị trí mỗi lần launch → phải lấy lại bounds qua `GetWindowRect` / UIA và `SetForegroundWindow` trước mỗi click. Đã xử lý, không ảnh hưởng kết quả.
- Build lần đầu bị `MSB3026` do app cũ (PID 13216) khoá `Vazer.Core.dll` → kill process rồi build sạch.

---

## Bước 5 — Tổng kết

| Hạng mục | Kết quả |
|---|---|
| Root cause | **CODE**, 3 tầng, `InstanceDetailViewModel.ResourcePacksShaders.cs` dòng 286 / 49–64 / 768 |
| Fix | `497ce8e` + `204f69d` |
| Test | 498 → **502 passed**, 0 warning |
| Pixel PASS | unique 325/328 > 50; thumb-sig 7 → 2090 px; placeholder 488 → 0 px |
| UIA text | PASS (card + badge "Đã cài" + 2 nút) |
| Restart | PASS (entry persist, ảnh persist) |
| Fullbright | Không cùng root cause, không có thumbnail mồ côi → không fix (báo rõ) |

**Khuyến nghị: Sẵn sàng tag v0.19.0** — với 1 lưu ý nhỏ: 3 ảnh visual của P0.4 (toast / shader thumbnail render / progress bar) vẫn chờ Master 30s confirm theo luật cũ; Raez không tự ghi PASS cho phần nhìn bằng mắt. Riêng case LowOnFire đợt này đã có bằng chứng pixel khách quan nên không cần Master confirm lại, trừ khi Master muốn tự đối chiếu `06-lowonfire-after-fix.png`.
