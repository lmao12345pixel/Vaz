# Vazer Launcher — UI prototype (web)

**Bản hiện tại: Wave 8.1 · v14** (Minecraft Launcher Dock, 3D Block Icons, Emerald Tokens).

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
| **Instances** | Grid card 240×152, search + sort (Chơi gần nhất / Tên A-Z / Mới tạo), New Instance wizard 3 bước, Play/Stop có trạng thái + chip "Đang chạy", context menu chuột phải (Play/Stop / Edit / Rename / Duplicate / Open folder / Delete), thời gian chơi cộng dồn trên card |
| **Instance Detail** | Header + Launch/Stop, 4 tab **Mods / Worlds / Logs / Settings**, toolbar phân nhóm có divider, mod list có toggle + badge update + menu `⋯` |
| **Mods** | Filter bar (search tên/file, chip trạng thái + count, sort A–Z/Z–A, xóa bộ lọc, “Hiển thị X / Y mod”), Check updates / Update all, menu từng mod: cập nhật riêng, chọn phiên bản (dialog radio), gỡ mod có xác nhận |
| **Worlds** | Danh sách world (size, lần chơi cuối, số backup), Backup từng world + Backup all có progress, khôi phục từ backup cũ, Open worlds folder |
| **Logs** | Live log giả lập theo kiểu `latest.log` (có dòng JVM args/RAM/resolution), Clear Log, Copy to Clipboard, Auto-scroll |
| **Settings** | Form thật: RAM slider + số (1024–16384 MB), JVM args, Java path, độ phân giải, Lưu/Mặc định; đường dẫn instance, Delete instance |
| **Accounts** | Card tài khoản + avatar Minotar 52×52 bo góc, Set active, Xóa, Add Offline, Add Microsoft; chip skin đang dùng |
| **Discover** | 4 modpack tuyển chọn (Performance Fabric, Create Adventure, NeoForge Essentials, Cozy Fabric), search theo tên/tác giả, lọc theo loader (Fabric/NeoForge), cài đặt tạo instance mới kèm dependency |
| **Shaders** | Màn hình riêng trên thanh nav; 8 shaderpack đồ họa (ảnh preview thật lưu cục bộ), search thời gian thực, lọc phong cách (Cân bằng / Chân thực / Fantasy / Tối ưu FPS), xem chi tiết, chọn instance mục tiêu & tự động resolve Iris dependency |
| **Servers** | Server mock có search/lọc, CRUD, yêu thích, copy địa chỉ, làm mới trạng thái và vào server mô phỏng |
| **Skins** | Chọn tài khoản, xem trước skin 3D đúng texture game (kéo xoay/zoom), 6 preset Classic/Slim, áp dụng/mặc định/ngẫu nhiên; lưu theo tài khoản |
| **Mod Browser** | Dialog rộng, search debounce 300 ms (Enter để tìm ngay), filter Loader/MC/Sort, kết quả dạng list, cài mod có progress % và tự cài dependency (Fabric API) |
| **Install flows** | Cài mod → xuất hiện ngay trong tab Mods + reload vẫn còn; Check updates/Update all đổi version thật; Install .mrpack và OptiFine thêm mod vào instance |
| **Crash demo** | Nút **Simulate crash** trên thanh demo: dừng instance đang chạy, ghi log stack trace, mở dialog crash (exit code, đường dẫn crash report, nút mở report) |
| **Activity Center** | Nút hoạt động trên titlebar + badge; drawer theo dõi task đang chạy/lịch sử (launch, cài mod, update, backup, restore, tạo instance, crash), filter Tất cả / Đang chạy / Lịch sử, xóa lịch sử; mở bằng Ctrl+Shift+T, đóng bằng Esc |

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

## Motion v4 — "Tactile update"

- **Nút bấm**: ripple theo vị trí con trỏ (pointerdown), icon nhích/nảy khi hover (icon Sync xoay 180°), nhấn lún 0.97 rồi bật lại bằng spring.
- **Update flow**: nút Check updates / Update all chuyển sang trạng thái busy (spinner + glow thở); từng dòng mod quét sáng khi kiểm tra; dòng đang cập nhật có vệt progress chạy ở đáy; badge cập nhật pop-in, biến mất bằng fade-scale; dòng vừa cập nhật flash teal; progress bar có fill mượt và glow khi xong; status pulse khi hoàn tất.
- **Đồng bộ DOM**: `commitRowUpdate()` + `syncUpdatesBar()` cập nhật đúng dòng/thanh đếm mà không re-render cả list, nên animation không bị cắt.
- Tất cả tự tắt khi `prefers-reduced-motion: reduce` (ripple cũng không được tạo).
- Code chính: `attachRipple`, `setButtonBusy`, `markRowChecking`, `setRowUpdating`, `commitRowUpdate` trong `app.js`; block `v4` cuối `styles.css`.

## Motion v5 — "Wave 1"

- **Card ↔ Detail morph**: card bay thành header (WAAPI ghost 380ms), header fade/scale nhẹ; nút Back hoặc nav Instances chạy ngược lại đúng đường.
- **Tab chuyển hướng**: panel cũ trượt 16px theo hướng đi, panel mới vào từ phía đối diện; tab glider vẫn trượt theo.
- **Install pipeline**: dòng mod trong Mod Browser bung ra 3 bước **Tải → Kiểm tra → Cài** (dot pulse/check), chip dependency hiện lần lượt, xong thì gập lại thành "Đã cài".
- **Log line motion**: dòng mới trượt 6px + fade; WARN/ERROR có nền màu; nút **Cuối log** hiện khi cuộn lên.
- **Command palette**: `Ctrl+K` mở palette fuzzy — mở/launch instance, Accounts, tạo instance, đổi theme, Download Mods; ↑/↓ + Enter, Esc đóng.
- **Log intelligence**: ô tìm trong log, chip ALL/INFO/WARN/ERROR, nút **Lỗi đầu tiên** (scroll + flash), nút Copy chỉ copy các dòng đang lọc.
- **Multi-select mods**: nút **Select** bật checkbox; click thường chọn từng mod, Shift-click chọn khoảng; thanh bulk Enable / Disable / Uninstall / Bỏ chọn.
- Tất cả hiệu ứng tôn trọng `prefers-reduced-motion` (morph/ripple/tab slide tự tắt).

## Motion v6 — "Wave 2"

- **Launch choreography**: progress chạy mượt qua 4 bước **Java → Metadata → Assets → Process** (dot pulse/check, status theo từng bước); xong tự chuyển sang tab Logs. Reduced motion rút ngắn từng bước và bỏ lerp.
- **Live count-up**: pill số instance (kể cả `1 / 3` khi search) và số kết quả Mod Browser đếm lên thay vì nhảy số.
- **Update queue**: panel hàng đợi khi Update all — từng mod `Chờ → % → Xong`, progress riêng + thanh tổng; hoàn tất thì fade rồi biến mất.
- **Backup fill + restore**: row world có thanh tiến trình inline + % khi nén; nút restore mở dialog chọn bản backup (mới nhất trước), khôi phục có progress và cập nhật "Chơi lần cuối".
- **Account switch**: card mới pop nhẹ, avatar nảy, mark ✓ Active trượt vào; crossfade trạng thái active.
- **Dialog origin**: dialog scale/blur từ đúng vị trí con trỏ (transform-origin theo pointer).
- **Aurora reactive**: lớp aurora dịch nhẹ theo con trỏ bằng CSS `translate` (throttle bằng rAF), tắt hoàn toàn khi reduced motion.
- **Clone instance**: context menu **Duplicate** copy settings, mods và worlds sang instance mới `(copy)`.
- **Playtime**: mỗi phiên Launch → Stop/crash cộng dồn phút chơi, hiển thị cạnh "Chơi lần cuối".
- Code chính: `countUp`, `animateProgressTo`/`setLaunchStep`, `showUpdateQueue`/`setQueueItem`, `openRestoreDialog`, `duplicateInstance`, `initAmbient`; block `v6` cuối `styles.css`.

## Wave 3 — "Activity Center & Create wizard"

- **Activity Center**: nút trên titlebar (kèm badge số task đang chạy, flash ✓/! khi xong) mở drawer bên phải. Task có tiến trình, trạng thái, thời gian tương đối; filter **Tất cả / Đang chạy / Lịch sử**, clear history. `Ctrl+Shift+T` mở/đóng, `Esc` đóng.
- **Task hooks**: launch (4 phase), cài mod (kể cả dependency), Update all / update từng mod, backup world, restore, tạo instance và crash đều tạo task thật trong Activity Center. Task lỗi (crash) có màu danger và giữ report path.
- **Persistence**: chỉ task đã kết thúc được lưu vào `localStorage` (tối đa 20); task đang chạy reset khi reload.
- **Create Instance wizard**: 3 bước **Thông tin → Phiên bản → Xem lại**; chọn loader bằng card (Vanilla/Fabric/NeoForge), chọn MC version, nhập loader version, xem lại ID/đường dẫn trước khi tạo; ID tự dedupe (`-2`, `-3`) kể cả khi tên trùng.
- **Tạo instance có task**: dialog hiển thị tiến trình resolve manifest → cài loader → tạo thư mục; xong task `Tạo ...` xuất hiện trong Activity Center.
- Code chính: `createTask`/`updateTask`/`finishTask`, `renderTasks`, `openTasks`, `openNewInstanceDialog` (wizard), `uniqueInstanceId`; block `v7` cuối `styles.css`.
- Tất cả hiệu ứng tôn trọng `prefers-reduced-motion`; console không có lỗi.

## Wave 4 — "Mod workspace"

- **Filter bar** ngay dưới toolbar Mods: search theo **tên project hoặc tên file** (không phân biệt hoa thường), chip trạng thái **Tất cả / Đang bật / Đang tắt / Có cập nhật** kèm số đếm live, sort **Tên A–Z / Tên Z–A**, nút **Xóa bộ lọc** và dòng summary **“Hiển thị X / Y mod”**.
- **Hai empty state**: instance chưa có mod (“Chưa có mod nào.”) và không có kết quả với bộ lọc hiện tại; empty state thứ hai có nút xóa bộ lọc và hiển thị từ khóa bằng text thuần.
- **Row identity**: mỗi dòng giữ `data-mod-row` = index gốc trong `state.mods`. Menu, toggle, update từng mod, uninstall, shift-select và các helper animation (`rowAt`, `commitRowUpdate`, `setRowUpdating`, `markRowChecking`) đều resolve qua attribute này — filter/sort đổi thứ tự DOM không làm lệch thao tác.
- **Selection an toàn**: đổi filter hoặc sort xóa selection; Shift-click chỉ chọn các dòng đang hiển thị; bulk count chỉ tính mod đang hiển thị.
- **Live recompute**: counts/summary cập nhật sau toggle, check updates, update từng mod, Update all (vẫn áp dụng **toàn bộ** mod, không chỉ mod đang hiển thị), uninstall và cài từ Mod Browser; flash/animation và task trong Activity Center giữ nguyên.
- **An toàn + a11y**: query chỉ render bằng `textContent` (không chèn HTML), chip là `<button>` với `aria-pressed`, Enter/Space hoạt động, ô search giữ focus khi lọc. Layout wrap sạch ở 1280 / 900 / 620 px, không tràn ngang.
- Code chính: `visibleMods`, `modMatchesFilter`, `syncModFilterControls`, `renderMods`, `rowAt`, `flashModRow`; block `v8` cuối `styles.css`.

## Wave 5 — "Discover, Servers & Global Settings"

- **Nav mở rộng**: thanh nav chính giờ có **5 chức năng** — Instances / Discover / Servers / Accounts / Settings — thay vì chỉ 2 mục.
- **Discover**: 4 modpack tuyển chọn (Performance Fabric, Create Adventure, NeoForge Essentials, Cozy Fabric) với search + lọc loader. Bấm **Cài modpack** sẽ tạo instance thật trong demo, cài kèm mod và dependency đã chọn, cộng task trong Activity Center; instance có `packId` và giữ trạng thái sau reload.
- **Servers**: danh sách server mock (Aurora SMP, Creative Hub, Modded Survival) với search, lọc **Tất cả / Yêu thích / Online**, thêm/sửa/xóa server, bật/tắt yêu thích, copy địa chỉ và **Vào** server mô phỏng bằng task. Server được lưu trong `localStorage`.
- **Settings toàn cục**: Java path, RAM mặc định cho instance mới, theme, giảm chuyển động, thư mục dữ liệu và xóa cache mô phỏng; có **Lưu cài đặt** / **Mặc định**. Cài đặt được lưu và áp dụng lại sau reload.
- **Reduced motion thủ công**: bật trong Settings sẽ thêm class `.reduce-motion`, tắt animation/transition toàn giao diện; vẫn tôn trọng `prefers-reduced-motion` của hệ điều hành.
- Code chính: `renderDiscover`, `installDiscoverPack`, `renderServers`, `openServerDialog`, `joinServer`, `syncGlobalSettingsUI`, `applyGlobalSettings`; block `v9` cuối `styles.css`.

## Wave 6 — "Skins, pack details & polish"

- **Nav 6 chức năng**: thêm **Skins** giữa Servers và Accounts; tổng cộng Instances / Discover / Servers / Skins / Accounts / Settings.
- **Skins**: chọn tài khoản (mặc định tài khoản active), xem trước **mô hình 3D đúng texture game** bằng `skinview3d` (kéo xoay, lăn chuột zoom, tự xoay chậm khi không giảm chuyển động), 6 preset **Steve / Alex / Ari / Efe / Kai / Makena** với model Classic/Slim. **Áp dụng skin** lưu vào tài khoản trong `localStorage`; **Mặc định** trả về skin gốc; **Ngẫu nhiên** gợi ý preset khác. Card Accounts hiển thị chip mặt skin thật. Texture default lưu tại `assets/skins/`; thư viện tại `vendor/`.
- **Discover pack details**: nút **Chi tiết** mở dialog rộng với mô tả, loader/MC, danh sách mod chính, phiên bản và dependency tự động. Cài trực tiếp từ dialog; số mod trong status/task giờ tính cả dependency thay vì chỉ mod chính.
- **Server refresh**: nút **Làm mới** mô phỏng kiểm tra lại online/ping/players cho toàn bộ server, lưu `localStorage` và cập nhật status ngay; có spinner trên icon.
- **Command palette**: thêm lệnh điều hướng nhanh tới Discover, Servers, Skins và Settings.
- **Polish**: empty/disabled states cho Skins khi chưa có tài khoản, skin card có `aria-pressed`, status `aria-live`, layout responsive 1280/900/620, tôn trọng `prefers-reduced-motion` và công tắc giảm chuyển động.
- Code chính: `SKIN_PRESETS`, `renderSkins`, `renderSkinPreview`, `ensureSkinViewer`, `syncSkinViewer`, `applySkinSelection`, `openPackDetails`, `packDependencyProjects`, `refreshServerStatuses`; block `v11` cuối `styles.css`.

## Wave 7 — "Shader discovery & visualizer"

- **Tách riêng Shaders trên Navigation bar**: thanh nav chính hiện có **7 chức năng** — Instances / Discover / Shaders / Servers / Skins / Accounts / Settings.
- **Discover**: trở về vai trò thuần túy cho Modpack tuyển chọn, search tên/tác giả và lọc theo loader.
- **Shaders (màn hình độc lập)**:
  - 8 Shaderpack tuyển chọn: Complementary Reimagined, BSL Shaders, Photon, Solas, Bliss, MakeUp - Ultra Fast, AstraLex, Miniature Shader.
  - Tài nguyên ảnh preview cục bộ: tất cả ảnh chụp ingame chất lượng cao (1000px JPG) lưu trong `assets/shaders/`, đường dẫn trực tiếp, offline 100%.
  - Tìm kiếm tức thời theo tên, tác giả, mô tả hoặc từ khóa phong cách.
  - Bộ lọc phong cách: **Tất cả / Cân bằng / Chân thực / Fantasy / Hiệu năng**.
- **Dialog chi tiết Shader**:
  - Hero image khổ rộng sắc nét kèm badge phong cách và thông tin bản dựng (version, dung lượng, lượt tải, Minecraft tương thích).
  - Danh sách tính năng chính dạng chip tags và mô tả chuyên sâu.
  - **Chọn Instance mục tiêu**: cho phép cài shader vào bất kỳ instance nào hiện có.
  - **Tự động kiểm tra & resolve Iris Shaders**: nếu instance Fabric được chọn chưa có Iris, launcher hiển thị cảnh báo và tự động cài kèm Iris Shaders + Fabric API trong cùng một tác vụ.
- **Activity Center & Trạng thái**:
  - Tác vụ cài shader hiển thị tiến trình mượt mà trong drawer Hoạt động.
  - Thẻ shader hiển thị badge **Đang dùng** hoặc **Đã cài** tương ứng với instance hiện tại.
  - Phím tắt `Ctrl+K` có thêm lệnh nhanh **Shaders**.
- Code chính: `SHADER_PACKS`, `renderShaders`, `filteredDiscoverShaders`, `openShaderDetails`, `installShader`, `isShaderInstalled`; block `v12` cuối `styles.css`.

## Wave 7.1 — "UX Flow & Quick Launch Optimization"

- **Titlebar Quick Launch & Bộ chọn Instance toàn cục**:
  - Bộ chọn instance trực tiếp trên thanh tiêu đề (`#globalActiveInstance`), luôn hiển thị instance đang chọn và phiên bản MC tương ứng. Đổi instance tại đây sẽ đồng bộ tức thì sang trang Instances, Details và Shaders.
  - Nút **Chơi / Dừng** toàn cục (`#globalPlayBtn`) kèm trạng thái động (Đang chạy, Chơi, Dừng), cho phép khởi chạy hoặc dừng instance từ bất kỳ màn hình nào mà không cần quay lại trang chủ.
- **Bộ chọn Instance đích ngay trên trang Shaders**:
  - Toolbar trang Shaders có thêm menu chọn instance (`#shaderTargetInstance`), cho phép người dùng kiểm tra hoặc đổi instance cần nhận shader pack ngay tại chỗ.
- **Hệ thống phím tắt nhanh toàn diện**:
  - `Ctrl+P`: Khởi chạy hoặc dừng instance đang chọn từ bất kỳ đâu.
  - `Alt+1` đến `Alt+7`: Chuyển nhanh giữa 7 màn hình chính (Instances, Discover, Shaders, Servers, Skins, Accounts, Settings).
  - `Ctrl+F` hoặc `/` (khi không gõ văn bản): Tự động focus vào ô tìm kiếm của màn hình hiện tại (Instances, Discover, Shaders, Servers, Mods trong Detail).
  - `Escape`: Nếu đang gõ trong ô tìm kiếm có nội dung, tự động xóa nội dung tìm kiếm và blur; nếu không thì đóng context menu, command palette hoặc Activity Center.
- **Nâng cấp ô tìm kiếm chuẩn HTML5**:
  - Tất cả ô tìm kiếm chuyển sang `type="search"` với nút xóa nhanh và placeholder gợi ý phím tắt `(Ctrl+F)`.
- Code chính: `updateGlobalQuickPlay`, enhanced `keydown` listener trong `wireEvents`; CSS trong block titlebar và header-actions.

## Wave 8 — "Skins Studio Pro"

- **Custom PNG Upload & Drag-and-Drop**:
  - Tải lên skin tùy chỉnh bằng nút **Tải skin (.png)** hoặc kéo thả trực tiếp file PNG vào khung xem 3D (`#skinStage`).
  - Hỗ trợ và kiểm tra kích thước chuẩn Minecraft: 64×64 hoặc 64×32 pixel.
  - Dialog cho phép đặt tên skin và chọn model: **Classic (4px)** hoặc **Slim (3px)**.
  - Lưu vào `state.customSkins` trong `localStorage`, hiển thị ngay trong danh sách với nút xóa riêng biệt.
- **Áo choàng (Capes)**:
  - Tích hợp 4 áo choàng Minecraft đặc sắc kèm texture pixel thật lưu cục bộ: **Migrator Cape**, **Vanilla Cape**, **15th Anniversary Cape**, **Cherry Blossom Cape** (cùng tùy chọn Không dùng áo choàng).
  - Tự động gắn vào nhân vật 3D qua `skinViewer.loadCape()` / `skinViewer.resetCape()`.
  - Áo choàng được lưu và ghi nhớ theo từng tài khoản (`account.capeId`).
- **Điều khiển Chuyển động & Góc nhìn 3D**:
  - Bộ thanh công cụ nổi ngay trên sân khấu 3D:
    - 4 tư thế chuyển động sống động: **Đứng yên (Idle)**, **Đi bộ (Walk)**, **Chạy (Run)**, **Bay lượn (Fly)** bằng `skinview3d`.
    - Bật/tắt tự động xoay 360 độ (`skinViewer.autoRotate`).
    - Nút đặt lại góc nhìn mặc định (`skinViewer.resetCameraPose()`).
- **Lấy skin theo tên người chơi (Grab Skin by Username)**:
  - Ô nhập tên nhân vật Minecraft (ví dụ: `Technoblade`, `Dream`, `Jeb_`, `MumboJumbo`) + nút **Lấy skin**.
  - Tự động tải texture từ Minotar API, có cơ chế tạo skin thủ tục dự phòng ngoại tuyến 100% bằng canvas nếu mất mạng.
  - Tự động thêm vào bộ sưu tập skin tùy chỉnh và hiển thị ngay trên người mẫu 3D.
- Code chính: `CAPE_PRESETS`, `applySkinViewerAnimation`, `toggleSkinAutoRotate`, `resetSkinCamera`, `renderCapeSelector`, `selectCape`, `handleSkinFileInput`, `promptAddCustomSkin`, `grabSkinByUsername`, `deleteCustomSkin`; block `v13` cuối `styles.css`.

## Wave 8.1 — "Authentic Minecraft Launcher Look & Feel"

- **Thanh Minecraft Play Dock (`.mc-play-bar`) ở cạnh dưới cửa sổ**:
  - Tái hiện bố cục kinh điển của launcher Minecraft chính thức:
    - **Cánh trái**: Thẻ instance đang chọn kèm icon khối cỏ 3D isometric (`.mc-block-icon`), menu dropdown `#globalActiveInstance`, và phụ đề phiên bản/loader (`#mcInstanceSub`).
    - **Trung tâm**: Nút **CHƠI / PLAY** xanh ngọc lục bảo lớn (`#globalPlayBtn`), viền nổi 3D, chuyển sang đỏ **DỪNG** khi game đang chạy và hiển thị thanh tiến trình sọc chuyển động khi đang tải.
    - **Cánh phải**: Thẻ hồ sơ người chơi (`#mcAccountBadge`) với avatar đầu vuông pixelated (`#mcAccountFace`), tên người chơi, trạng thái kết nối, bấm vào để chuyển nhanh tới màn hình Skins.
- **Biểu tượng khối 3D Isometric trên thẻ Instance**:
  - Khối Cỏ (Grass Block) cho Fabric / Mặc định.
  - Khối Lò Luyện (Furnace / Flame) cho NeoForge.
  - Khối Bàn Chế Tạo (Crafting Table) cho Vanilla.
  - Tag phân loại loader có màu riêng biệt (Fabric xanh băng, NeoForge cam lửa, Vanilla xanh lá cây).
  - Viền phát sáng ngọc lục bảo bao quanh instance đang là mục tiêu khởi chạy (`.is-active-target`).
- **Dọn sạch rác demo, tối ưu trải nghiệm chân thực**:
  - Xóa bỏ thanh nổi demo lơ lửng (`.demo-bar`) và huy hiệu `PROTOTYPE` trên thanh tiêu đề.
  - Tích hợp menu công cụ phát triển kín đáo (`#mcDevPopover`) qua nút cờ-lê (`#mcDevToolsBtn`), giữ nguyên ID `#simulateCrashBtn`, `#themeToggle`, `#resetDemo` để tương thích toàn diện.
  - Tinh chỉnh các câu văn từ "trong demo" sang văn phong chuẩn của Minecraft Launcher.
  - Chuẩn hóa token màu `--accent` sang sắc xanh ngọc lục bảo Minecraft (`#22c55e` / `#16a34a`).

## Wave 8.2 — "Deep Diagnostics, Modrinth Hub & Live Social Identity"

- **Pillar 1: Diagnostics & Deep Instance Suite**:
  - **Saves & Screenshots**: Bổ sung tab `Screenshots` vào Instance Detail (`TAB_ORDER` gồm `mods`, `worlds`, `screenshots`, `logs`, `settings`). Gallery ảnh chụp kèm Lightbox phóng to toàn màn hình. Tab `Worlds` bổ sung badge Seed, sao chép Seed và NBT Level.dat inspector.
  - **Crash Analyzer & 1-Click Quick Fix**: Banner chẩn đoán `#crashAnalyzerBanner` tự động phát hiện 3 kịch bản lỗi kinh điển (Tràn RAM OOM -> cấp 6GB; Sai Java -> chuyển Java 21; Xung đột Mixin -> tắt mod xung đột) kèm nút Quick Fix xử lý tức thì. Bộ chọn kịch bản crash trong Dev popover.
  - **Export & Clone Instance**: Xuất gói `.mrpack` (Modrinth) hoặc `.zip` (Full) qua Activity Center task; nhân bản instance với tùy chọn giữ nguyên hoặc làm sạch saves.
- **Pillar 2: Content Ecosystem & Modrinth-grade Experience**:
  - **Universal Discover Matrix**: Bộ chuyển 5 loại nội dung (`Mods`, `Resource Packs`, `Shader Packs`, `Data Packs`, `Modpacks`), Category Tag Chips, bộ lọc đa chiều (tìm kiếm tức thời, Loader, MC version, Sort).
  - **Modrinth-grade Content Detail**: Modal chi tiết với Markdown rendering, Thư viện ảnh Gallery + Lightbox, Bảng Version Matrix hiển thị độ tương thích và changelog.
  - **1-Click Dependency Resolution Engine**: Cây phụ thuộc tương tác trực quan (Required vs Optional, Đã có vs Sẽ cài mới) cho các mod phức tạp (Iris Shaders -> Sodium + Fabric API, Continuity -> Indium).
  - **Update Checker & Changelog Diff**: Badge số lượng mod có cập nhật trên tab Mods, xem nhanh diff thay đổi và nút "Cập nhật tất cả" bảo lưu trạng thái enabled/disabled.
- **Pillar 3: Social, Identity & Live Community**:
  - **3D Wardrobe Studio**: Chuyển đổi mô hình Slim (Alex 3px) ↔ Classic (Steve 4px), 5 tư thế chuyển động (Walk, Fly, Sneak, Wave, Bow Aim), hiệu ứng vạt áo choàng bay phất phơ (Cape Flutter Physics) và nạp áo choàng tùy chỉnh từ file PNG.
  - **Multiplayer Server Ping & MOTD**: Cột sóng ping Minecraft 5 vạch màu, parser mã màu MOTD Minecraft (§0-§f) an toàn chống XSS, tooltip avatar danh sách người chơi và nút Direct Connect tự quét tìm instance tương thích.
  - **Discord Rich Presence Visualizer**: Khung xem trước Discord Dark mode mô phỏng trạng thái thời gian thực với tên game, instance, avatar và tùy biến ghi chú.
  - **Minecraft News Carousel**: Băng chuyền 3 slide tin tức chính thức kiểu Minecraft Launcher tự trượt 6s, tạm dừng khi hover và modal đọc bài viết chi tiết.

## Wave 8.3 — "Dedicated Navigation & Smart Instance Target Selection"

- **Tách riêng các tab nội dung trên Sidebar Navigation**:
  - Tách `Discover` thành 5 màn hình chuyên biệt trực tiếp trên thanh điều hướng: **Mods** (`#page-mods`), **Resource Packs** (`#page-resourcepacks`), **Shaders** (`#page-shaders`), **Data Packs** (`#page-datapacks`), **Modpacks** (`#page-modpacks`).
  - Hệ thống phím tắt `Alt+1` đến `Alt+0` bao quát toàn bộ 10 phân hệ chính; `Ctrl+F` và `/` tự động focus vào ô tìm kiếm của màn hình đang mở.
- **Hộp thoại Chọn Instance Mục tiêu (Smart Target Instance Picker)**:
  - Khi cài đặt gói nội dung hoặc dependency ("Cài đặt tất cả"), hệ thống mở popup danh sách các instance có sẵn, hiển thị icon khối 3D isometric (`grass_block.png`, `furnace.png`, `crafting_table.png`), loader, phiên bản Minecraft và chip tương thích.
  - Tùy chọn checkbox tự động đặt làm Instance đang chơi (Active Instance) đồng bộ ngay xuống Minecraft Play Bar.
- **Chuẩn hóa 100% Asset & Texture Cục bộ**:
  - Thay thế toàn bộ ảnh placeholder bằng tài nguyên thật lưu trong `assets/mods/`, `assets/resourcepacks/`, `assets/datapacks/`, `assets/modpacks/`, `assets/gallery/` và `assets/blocks/`. 100% request HTTP 200, offline không phụ thuộc internet.

## Wave 9 — "In-game Settings & options.txt Visual Editor"

- **Khu vực Cấu hình In-game trong tab Settings của Instance (`#tab-settings`)**:
  - Tích hợp trình quản lý cấu hình Minecraft toàn diện trực tiếp trong trang Instance Detail.
  - Hỗ trợ xem và chỉnh sửa song song hai chế độ: **Giao diện trực quan (Visual Editor)** và **Văn bản thô (`options.txt` Raw Editor)**.
- **Trình chỉnh sửa Trực quan (Visual Editor) với 4 nhóm thiết lập**:
  - **Đồ họa**: Render Distance (2–32 chunks), Simulation Distance (5–32 chunks), Max FPS (10–260/Unlimited), Graphics Mode (Fast/Fancy/Fabulous), Clouds (Off/Fast/Fancy), GUI Scale (Auto/1x/2x/3x/4x), Brightness (0–100%), VSync và Entity Shadows.
  - **Âm thanh**: Thanh trượt âm lượng trực quan 0–100% cho Master, Music, Weather, Blocks, Hostiles, Players, Ambient.
  - **Điều khiển**: Mouse Sensitivity, Invert Mouse (đảo trục Y), Auto-Jump, Sneak Mode (Giữ/Bật tắt), Sprint Mode (Giữ/Bật tắt) và FOV (30°–110° Quake Pro).
  - **Phím bấm (Keybinds)**: 12 phím hành động kinh điển (W/A/S/D, Space, Shift, Ctrl, Attack, Use, Inventory, Drop, Chat). Tích hợp **chế độ ghi phím tương tác** (Click nút -> nhấn phím bất kỳ hoặc click chuột -> cập nhật phím mới tức thì, nhấn Esc để hủy).
- **Đồng bộ hai chiều (Bidirectional Sync) & Bảo toàn dữ liệu**:
  - Thay đổi trên Visual Editor tự động cập nhật ngay sang `options.txt` thô.
  - Chỉnh sửa trực tiếp trên Raw Text Editor tự động phân tích và phản ánh ngay lên các thanh trượt/công tắc của Visual Editor.
  - **Bảo toàn 100% các khóa tùy chỉnh / khóa lạ**: các dòng cấu hình mod hoặc tùy chọn phiên bản mới không bị ghi đè hay thất thoát khi đồng bộ và xuất file.
- **3 Cấu hình Mẫu Nhanh (Presets)**:
  - **Tối ưu FPS**: Render distance 6, fast graphics, tắt mây/bóng, 260 FPS (Unlimited), gamma 100%.
  - **Cân bằng**: Render distance 12, fancy graphics, mây nhanh, 120 FPS, gamma 50%.
  - **Đồ họa cao**: Render distance 20, fabulous graphics, mây đẹp, VSync bật, 144 FPS.
- **Xuất file `options.txt` & Nhật ký Khởi chạy**:
  - Nút **Xuất options.txt**: Tải file `options.txt` chuẩn về máy tính qua Blob download.
  - Ghi log chi tiết trong `launchInstance()` (`[Launcher/INFO]: Loaded options.txt (...)`) hiển thị trên tab Logs và `latest.log`.
  - Lưu trữ độc lập theo từng instance trong `state.instances[id].mcOptions` và bền vững qua `localStorage`.

### Wave 9.1 — "Motion & responsive polish"

- **Motion tokens dùng chung** (`:root`): `--dur-fast` 140ms, `--dur-base` 220ms, `--dur-slow` 380ms,
  `--ease-standard` `cubic-bezier(0.2, 0, 0, 1)`; giữ alias cũ `--ease-out` / `--ease-in-out` /
  `--ease-spring` để không vỡ style cũ. Mọi `transition: all` đã được thay bằng danh sách property
  tường minh (hover chỉ animate màu/viền/bóng/transform, không animate layout).
- **Bàn phím**: `:focus-visible` có vòng `2px` + `outline-offset: 2px` cho nav item, button, input,
  select, tab, textbox; token `--focus-ring` đổi theo theme (light theme dùng `--accent-2`).
- **Toast xếp chồng**: toast được gom vào `.toast-stack` (flex column, `bottom: 56px`, trên mobile
  `bottom: 148px` để không đè play dock) nên nhiều toast không chồng lên nhau; mỗi toast sống ~1.9s,
  nhận `is-out` rồi bị remove.
- **Dialog**: `.dialog-host` đóng dứt khoát sau 170ms, `hidden` + dọn `innerHTML` + bỏ
  `data-dialog-id` nên không còn `is-closing` treo.
- **Sidebar mobile (≤620px)**: nav thu gọn thành **rail 52px** (icon vẫn bấm được, content chừa
  `padding-left: 52px`); nút `.nav-mobile-toggle` trên titlebar mở rộng thành drawer 272px kèm
  `.nav-scrim`; đóng bằng **Escape**, click ra ngoài hoặc chọn mục; `#content` và `body` bị khoá
  scroll khi mở. Play dock chuyển 2 hàng để vừa 390px.
- **Reduced motion**: `prefers-reduced-motion: reduce` và công tắc **Giảm chuyển động** trong
  Settings (`html.reduce-motion`) đều tắt animation/transition toàn cục, ripple không được tạo,
  pulse/reveal hiện tức thì.
- **Không tràn ngang**: layout không vượt `scrollWidth` ở 620px và 390px (đã có Playwright assert).
- Code chính: `.toast-stack`, `.nav-mobile-toggle`, `.nav-scrim`, `setMobileNavOpen`,
  `initMobileNav`, `lockContentScroll`, `toastStack`; block `WAVE 9.1` cuối `styles.css`.

### Ảnh chụp

- [Activity Center — đang chạy](screenshots/wave3-tasks-running.png)
- [Activity Center — lịch sử](screenshots/wave3-tasks-history.png)
- [Create Instance wizard](screenshots/wave3-create-wizard.png)

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

- Không gọi Modrinth/Mojang/Microsoft thật; chỉ tải icon mod qua CDN công khai. Avatar tài khoản lấy từ texture skin cục bộ.
- Không mở game thật — Launch chỉ mô phỏng tiến trình/PID và stream log.
- Backup world / crash report chỉ là mô phỏng UI; không tạo file thật.
- Thay đổi tên chỉ đổi tên hiển thị (app thật sẽ đổi tên thư mục).
- Task Center chỉ lưu lịch sử task đã kết thúc trong `localStorage` (tối đa 20); task đang chạy sẽ mất khi reload.
- Trạng thái mock lưu trong `localStorage`; bấm **Reset** trên thanh demo để quay lại dữ liệu gốc.
- Skins dùng texture default thật và render 3D cục bộ; chưa upload/parse file skin PNG của người dùng.
- `vendor/skinview3d.bundle.js` là bản 3.4.2 đã vá sample bias FXAA (`-100` → `-16`) để tránh cảnh báo shader trên Windows/ANGLE; FXAA tắt trong viewer.
- Không có dark/light theo hệ thống — dùng nút **Dark/Light** ở thanh demo dưới cùng bên phải.
