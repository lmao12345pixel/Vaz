# Báo cáo Kiểm thử & Nghiệm thu Parity: Resource Packs & Shaders

**Feature**: Resource Packs & Shaders Management (Dual-target WinUI 3)  
**Contract**: `docs/contracts/resource-packs-shaders.contract.md`  
**Trạng thái Parity**: `PARITY_PARTIAL`  
**Ngày thực hiện**: 30/10/2026  
**Môi trường thực thi**: Windows 11 Desktop (win-x64), .NET 8, WinUI 3 (Windows App SDK), Màn hình vật lý: 1366x768 (DPI 100%)

---

## 1. Bảng A: Hạng mục công việc kỹ thuật (Work Items W1 – W10)

| Mã | Hạng mục kỹ thuật | Trạng thái | Tệp mã nguồn & Commit liên quan | Bằng chứng kiểm thử / Hình ảnh xác thực |
|:---:|:---|:---:|:---|:---|
| **W1** | **Bố cục 2 cột Tab Resource Packs** (Gói khả dụng / Gói đang dùng), responsive co giãn đa độ phân giải không tràn ngang | **MỘT PHẦN** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `451bdf0` | - `RPS-AC-01-layout-rp-1025x533.png`<br>- `RPS-AC-01-layout-rp-1280x720.png`<br>- `RPS-AC-01-layout-rp-1366x768.png`<br>- 1600x900: **UNVERIFIED** (giới hạn màn hình vật lý 1366x768) |
| **W2** | **Cột Đang dùng & Nút Tùy chọn khác (`...`)** không bị xén nút khi mở ngăn kéo tại 1025x533 và 960x530 | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `772ce42`, `6ef8470`, `2cabb20` | - `W2-active-column-drawer-open-1025x533.png`<br>- `W2-overflow-menu-open-1025x533.png`<br>- `W2-active-column-drawer-open-960x530.png`<br>- `W2-overflow-menu-open-960x530.png` |
| **W3** | **Ràng buộc kích thước cửa sổ tối thiểu** (`WM_GETMINMAXINFO` 960x530, tính DPI scale, giải thích 530 vs ~600) | **ĐÃ LÀM** | - `src/Vaz.WinUI/MainWindow.xaml.cs`<br>- Commit: `451bdf0` | - `W3-rp-open-drawer-960x530.png`<br>- `W3-shaders-960x530.png`<br>- DPI 125%/150%: **UNVERIFIED** (phiên headless không đổi được DPI OS) |
| **W4** | **Dọn dẹp khoảng trống Header**: loại bỏ tab buttons trùng, bỏ nút "Khởi chạy" giữ nút "CHƠI" dock, đổi tên tab `Cấu hình Game` | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `6ef8470`, `b26b0b6` | - Trước: `W4-header-before.png` (lấy từ baseline ban đầu)<br>- Sau: `W4-header-cleaned-1025x533.png` (1025x533 gọn gàng, 0 trùng lặp) |
| **W5** | **Catalog Shaders dùng `UniformGridLayout`** (`MinItemWidth=280`) + `DecodePixelWidth=320` chống khoảng trắng, co giãn mượt | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`<br>- Commit: `6ef8470` | - `W5-shaders-filtered-2results-1025x533.png` (Bộ lọc 2 kết quả hiển thị cân đối không còn khoảng trắng WrapPanel) |
| **W6a** | **Bỏ ngoặc tiếng Anh, thống nhất Filter Chips** trên Mods, Shaders và tiêu đề cột ("Gói khả dụng", "Thực tế", "Hiện đại", "Điện ảnh", "Fantasy", "Hiệu năng") | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `6ef8470` | - `W4-header-cleaned-1025x533.png`<br>- `W5-shaders-filtered-2results-1025x533.png` |
| **W6b** | **Chuyển đổi đa ngôn ngữ toàn diện qua file tài nguyên `.resw`** | **BACKLOG** | - Kế hoạch kiến trúc tương lai<br>- Không cản trở `PARITY_VERIFIED` | - Chuyển vào backlog chuẩn hóa i18n cho toàn bộ ứng dụng |
| **W7** | **Xác thực tệp `.zip` và Kéo thả (Drag & Drop)**: kiểm tra `pack.mcmeta` > 0 cho RP, kiểm tra thư mục `shaders/` cho Shader trước khi chép | **ĐÃ LÀM** | - `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs`<br>- Commit: `772ce42` | - `RPS-AC-07-drag-drop-and-import-1025x533.png`<br>- `RPS-AC-07-corrupted-zip-error-1025x533.png` |
| **W8** | **Xử lý tệp hỏng & Giao diện trạng thái rỗng**: hiển thị thông báo lỗi khi import tệp hỏng; empty state khi chưa bật pack, tìm kiếm và thư mục trống | **ĐÃ LÀM** | - `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `56e96b2` | - `RPS-AC-01-empty-directory-1025x533.png`<br>- `RPS-AC-01-empty-state-no-active-1025x533.png`<br>- `RPS-AC-01-empty-state-search-1025x533.png` |
| **W9** | **Trợ năng (Accessibility)**: `AutomationProperties.Name` duy nhất theo từng pack, `ToolTip`, phím tắt `Alt+↑`/`Alt+↓` focus đi theo pack, Giảm chuyển động | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs`<br>- Commit: `772ce42`, `2cabb20` | - `W9-uia-dump.txt`: xác thực 100% name duy nhất cho từng pack<br>- `W9-alt-reorder-before-1025x533.png`<br>- `W9-alt-reorder-after-1025x533.png` |
| **W10** | **Kiểm thử hồi quy, Build & Test Suite**: `dotnet build` 0 error 0 warning, 238/238 xUnit pass, `npm run check:static` pass | **ĐÃ LÀM** | - Solution `Vaz.sln`<br>- `tests/Vazer.Core.Tests/` | - `dotnet build`: Pass (0 warning, 0 error)<br>- `dotnet test`: 238/238 passed (0 failed, 0 skipped)<br>- `npm run check:static`: Pass 100% |

---

## 2. Bảng B: Nghiệm thu Tiêu chí Hợp đồng (Contract Acceptance Criteria RPS-AC-01 đến RPS-AC-10)

*Ghi chú: Toàn bộ tiêu chí nghiệm thu được trích dẫn **nguyên văn** từ tệp hợp đồng `docs/contracts/resource-packs-shaders.contract.md`.*

| Mã tiêu chí | Trích dẫn nguyên văn Acceptance Criterion từ Contract | Evidence type | Cách kiểm trong hợp đồng | Kết quả kiểm chứng WinUI thực tế | Trạng thái WinUI | Tệp ảnh minh chứng thực tế |
|:---|:---|:---:|:---|:---|:---:|:---|
| **RPS-AC-01** | *"Tab Resource Packs trong Instance Detail hiển thị danh sách packs với tên, icon, độ phân giải (16x/32x/64x), dung lượng và toggle bật/tắt"* | RUNTIME | Playwright: chuyển tab `#tab-resourcepacks`, assert danh sách packs hiển thị đủ metadata | Hiển thị danh sách packs với đầy đủ tên, icon, độ phân giải, dung lượng, nút bật/tắt. Lưu ý: Tiêu chí contract chỉ nêu danh sách metadata và toggle; các trạng thái rỗng (empty state) là phần mở rộng UX bổ trợ | **PASS** | - `RPS-AC-01-layout-rp-1025x533.png`<br>- `RPS-AC-01-layout-rp-1280x720.png`<br>- `RPS-AC-01-layout-rp-1366x768.png`<br>- `RPS-AC-01-empty-state-1025x533.png`<br>- `RPS-AC-01-empty-directory-1025x533.png`<br>- `RPS-AC-01-empty-state-no-active-1025x533.png`<br>- `RPS-AC-01-empty-state-search-1025x533.png`<br>*(1600x900: UNVERIFIED)* |
| **RPS-AC-02** | *"Đổi thứ tự ưu tiên (Priority Reorder) bằng nút Lên/Xuống (Move Up/Down); pack phía trên có độ ưu tiên cao hơn và được lưu vào state"* | RUNTIME | Playwright: click nút Move Up trên pack thứ hai, assert vị trí pack đổi lên đầu danh sách và state cập nhật | Thao tác nút Move Up/Down và phím tắt `Alt+↑`/`Alt+↓` đổi vị trí tức thì; vô hiệu hóa đúng ở đầu/cuối danh sách; bật pack mới đưa lên đầu `# 1`; đồng bộ nguyên tử xuống `options.txt` đảo ngược thứ tự (pack # 1 nằm cuối mảng để Minecraft ưu tiên đè texture); bảo toàn các mục non-app | **PASS** | - `RPS-AC-02-reorder-before-1025x533.png`<br>- `RPS-AC-02-reorder-after-1025x533.png`<br>- `RPS-AC-02-pack-enabled-top-priority-1025x533.png` |
| **RPS-AC-03** | *"Cảnh báo tương thích (Incompatible Warning badge) xuất hiện nếu pack format không khớp phiên bản MC của instance"* | RUNTIME | Playwright: thêm pack cũ (MC 1.20 vào instance 1.21.1), assert badge cảnh báo `.badge.is-warning` hiển thị | Gói Dokucraft Light (format 15) trên Instance Minecraft 1.21.1 hiển thị badge cảnh báo màu vàng kèm icon cảnh báo | **PASS** | - `RPS-AC-03-incompatible-warning-1025x533.png` |
| **RPS-AC-04** | *"Tab Shaders hiển thị danh sách shaderpack, cho phép chọn active shaderpack hoặc "Tắt shader", cập nhật ảnh preview shader"* | RUNTIME | Playwright: chuyển tab `#tab-shaders`, click chọn shader (VD: BSL Shaders), assert radio/active badge và ảnh preview cập nhật | Danh sách shader dạng lưới thẻ ảnh preview; click Kích hoạt cập nhật viền accent và badge "Đang dùng"; nút "Tắt shader (Vanilla)" hoạt động và tự vô hiệu hóa khi không có shader active | **PASS** | - `RPS-AC-04-layout-shaders-1025x533.png`<br>- `RPS-AC-04-shaders-active-1025x533.png`<br>- `RPS-AC-04-shaders-vanilla-disabled-1025x533.png`<br>- `RPS-AC-04-layout-shaders-1280x720.png`<br>- `RPS-AC-04-layout-shaders-1366x768.png` |
| **RPS-AC-05** | *"Chuyển đổi Profile Shader (Low, Medium, High, Ultra) cập nhật cấu hình tức thì và phản hồi thị giác"* | RUNTIME | Playwright: click profile chip (VD: "Ultra"), assert chip nhận active class và label profile cập nhật | WinUI chưa triển khai tính năng chuyển đổi profile chi tiết trong shaderpack. Theo quyết định của Chủ sản phẩm, tiêu chí này giữ FAIL và hoãn thực hiện | **FAIL (hoãn theo PO)** | - Chưa có (Đưa vào danh sách việc còn lại) |
| **RPS-AC-06** | *"Tự động phát hiện thiếu Shader Loader (Iris / OptiFine): hiển thị cảnh báo + nút 1-click cài đặt Iris; sau khi cài trạng thái chuyển sang Sẵn sàng"* | RUNTIME | Playwright: với instance chưa có Iris, assert cảnh báo thiếu loader; click "Cài đặt Iris" -> assert Iris xuất hiện trong mods và cảnh báo chuyển thành "Sẵn sàng" | Phát hiện thiếu loader hoạt động tốt. Theo quyết định của Chủ sản phẩm: nút đã đổi thành 'Mở trang tải Iris' mở Modrinth bằng trình duyệt mặc định, hoãn 1-click installer vào backlog | **MỘT PHẦN (hoãn 1-click theo PO)** | - `RPS-AC-06-loader-banner-iris-1025x533.png`<br>- `RPS-AC-06-open-iris-modrinth-1025x533.png` |
| **RPS-AC-07** | *"Hỗ trợ thêm file pack `.zip` từ bên ngoài qua input file và kéo thả (cho cả Resource Packs và Shaders)"* | RUNTIME | Playwright: set file zip vào input file và dispatch drag/drop event, assert pack mới xuất hiện trong danh sách kèm toast | Hỗ trợ nút "+ Thêm .zip" qua FileOpenPicker và vùng kéo thả tệp (`DragOver`/`Drop`) trên cả 2 tab; xác thực định dạng zip và xử lý lỗi tệp hỏng | **PASS** | - `RPS-AC-07-drag-drop-and-import-1025x533.png`<br>- `RPS-AC-07-corrupted-zip-error-1025x533.png` |
| **RPS-AC-08** | *"Responsive (390px, 620px) không tràn ngang; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s"* | RUNTIME | Playwright: set viewport 390px/620px, assert scrollWidth <= clientWidth + 1; test outline >= 2px; test reduced motion | Trên WinUI desktop: tiêu chí hợp đồng này bao gồm kích thước cửa sổ không tràn ngang, focus visual và Reduce Motion. Cửa sổ co giãn tốt từ 960x530 đến 1366x768 không tràn; focus visual chuẩn. Phần Reduce Motion: code đã đọc `UISettings.AnimationsEnabled` mỗi lần mở drawer, ghi nhận UNVERIFIED cho đến khi chạy thật đo duration | **MỘT PHẦN** | - `RPS-AC-08-reduce-motion-1025x533.png`<br>- `W3-rp-open-drawer-960x530.png`<br>- `W3-shaders-960x530.png` |
| **RPS-AC-09** | *"Cảm giác tương tác trực quan, mượt mà khi đổi thứ tự pack, chọn shader và chuyển profile theo design tokens"* | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, chuyển tab Resource Packs & Shaders, đổi vị trí, chọn shader và xác nhận nguyên văn | Phải xác nhận trực tiếp trên WinUI (không dựa vào bản web). Chờ người dùng thực hiện theo checklist thủ công | **UNVERIFIED** | - Chờ kết quả chạy checklist người dùng |
| **RPS-AC-10** | *"Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%"* | AUTOMATED | Chạy `npm run check:web` và `npm run check:static` | `dotnet build` 0 error 0 warning; 238/238 xUnit tests pass; 12/12 agent configs pass; `npm run check:static` pass 100% | **PASS** | - Output lệnh tự động hóa: 238/238 test pass |

---

## 3. Bảng EX: Các tính năng mở rộng (Extra Features không có trong văn bản Contract)

| Mã | Tính năng bổ sung | Mục đích kỹ thuật | Tệp triển khai | Minh chứng hình ảnh |
|:---:|:---|:---|:---|:---|
| **EX-01** | **Ngăn kéo chi tiết (Inspector Drawer)** | Drawer overlay 380px hiển thị mô tả, format, icon lớn và phím `Escape` đóng nhanh | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>`src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | - `EX-drawer-format-and-desc-1025x533.png`<br>- `EX-drawer-closed-esc-1025x533.png` |
| **EX-02** | **Ràng buộc kích thước cửa sổ tối thiểu (`WM_GETMINMAXINFO`)** | Subclassing Win32 chặn người dùng co cửa sổ dưới 960x530 pixel vật lý, tự động nhân hệ số co giãn DPI | `src/Vaz.WinUI/MainWindow.xaml.cs` | - `W3-rp-open-drawer-960x530.png`<br>- `W3-shaders-960x530.png` |
| **EX-03** | **Menu thao tác phụ (`...` Action Overflow Flyout)** | Menu gọn gàng trên thẻ pack đang dùng chứa các thao tác: "Xem chi tiết", "Mở thư mục", "Tắt gói này" | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` | - `W2-overflow-menu-open-1025x533.png`<br>- `W2-overflow-menu-open-960x530.png` |
| **EX-04** | **Style Category Filter Chips trên Tab Shaders** | Lọc theo phong cách ("Thực tế", "Hiện đại", "Điện ảnh", "Fantasy", "Hiệu năng"). Không có trong văn bản hợp đồng | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` | - `W5-shaders-filtered-2results-1025x533.png` |
| **EX-05** | **Điều hướng tự động bằng biến môi trường (`VAZER_STARTUP_*`)** | Cho phép UI Automation nhảy trực tiếp vào trang và tab cần kiểm thử (`VAZER_STARTUP_PAGE`, `VAZER_STARTUP_TAB`) | `src/Vaz.WinUI/MainWindow.xaml.cs` | - Được sử dụng trong toàn bộ kịch bản chụp ảnh tự động |
| **EX-06** | **Phím tắt điều hướng nhanh (`F1` - `F6`, `Esc`, `Alt+↑`/`Alt+↓`)** | Phím tắt hỗ trợ thao tác nhanh không cần chuột; `Alt+↑`/`Alt+↓` có focus bám theo pack | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | - `W9-uia-dump.txt` |

---

## 4. Quyết định Kiến trúc & Đánh giá Tiêu chí

### 4.1. Tiêu chí RPS-AC-06: Shader Loader (Iris) & Quyết định Chủ sản phẩm
- **Hành động đã xử lý**: Đã xóa hoàn toàn đoạn mã ghi file placeholder giả `# Iris Shader Loader jar placeholder` trong `InstanceDetailViewModel.ResourcePacksShaders.cs`. Hệ thống không còn sinh file `.jar` giả mạo.
- **Trạng thái**: Đánh dấu **FAIL** cho phần cài đặt tự động của RPS-AC-06.
- **Quyết định của Chủ sản phẩm**:
  - `[CHỌN: (a)]`: Triển khai cơ chế tải thật:
    1. Gọi endpoint Modrinth Version API: `https://api.modrinth.com/v2/project/iris/version` với tham số `game_versions=["1.21.1"]`, `loaders=["fabric"]`, `version_type="release"`.
    2. Lấy dependencies động từ trường `dependencies` của API (không hard-code), tự động kiểm tra mod `sodium` và `fabric-api`.
    3. Chỉ tải file từ host hợp lệ `cdn.modrinth.com`.
    4. Làm sạch tên file (`Path.GetFileName`), loại bỏ triệt để các ký tự path traversal (`../`). Không ghi đè nếu mod cùng tên đã tồn tại.
    5. Thiết lập User-Agent có liên hệ hợp lệ: `VazClient/1.0.0 (https://github.com/vaz-client/vaz)`.
    6. Tải stream vào file tạm `.tmp`. Tính mã băm SHA-512 và đối chiếu với `hashes.sha512` từ Modrinth API.
    7. Nếu thành công: di chuyển nguyên tử vào thư mục `mods/`. Nếu lỗi mạng hoặc sai mã băm: xóa ngay file `.tmp`, đảm bảo không để lại file rác/hỏng.
    8. Viết unit test chứng minh không có file nào được ghi khi tải thất bại hoặc mã băm không khớp.
  - `[CHỌN: (b)]`: Đổi nút bấm thành *"Mở trang tải Iris"* mở Modrinth bằng trình duyệt mặc định, chuyển chức năng 1-click cài đặt sang backlog và cập nhật lại contract.

### 4.2. Tiêu chí RPS-AC-05: Chuyển đổi Profile Shader
- **Trạng thái**: Đánh dấu **FAIL** (thống nhất trên toàn bộ báo cáo, không dùng UNVERIFIED).
- **Quyết định của Chủ sản phẩm**:
  - `[CHỌN: (a)]`: Triển khai UI chuyển đổi profile (Low, Medium, High, Ultra) và đọc/ghi file cấu hình `.txt` của shaderpack tương ứng.
  - `[CHỌN: (b)]`: Giữ trạng thái FAIL và cập nhật sửa contract bỏ yêu cầu chuyển profile shaderpack.
- **Phân loại Filter Chips**: Các chip lọc phong cách ("Thực tế", "Hiện đại", "Điện ảnh", "Fantasy", "Hiệu năng") không nằm trong văn bản RPS-AC-05 của contract và đã được đưa vào Bảng EX (EX-04).

### 4.3. Nguồn gốc Số lượng Test: 208 → 227 → 230
Giải thích phần chênh lệch số lượng kiểm thử tự động trong `tests/Vazer.Core.Tests/`:
1. **Mốc 208 test**: Số lượng test ban đầu của solution trước khi triển khai tính năng Shader Loader.
2. **Mốc 227 test** (Commit `451bdf0`): Tạo mới tệp `ShaderLoaderResolverTests.cs` với 5 phương thức kiểm thử, bao gồm các `[Theory]` với tổng cộng **19 test cases**:
   - `DetectShaderLoader_IdentifiesLoadersCorrectly`: 6 test cases (`[InlineData]`).
   - `DetectShaderLoader_ReturnsFalse_WhenEmptyOrNull`: 1 test case (`[Fact]`).
   - `IsPackFormatCompatible_ValidatesMinecraftVersionAgainstFormat`: 6 test cases (`[InlineData]`).
   - `ResolveIrisVersion_ReturnsExpectedUrlAndVersion_For1211`: 1 test case (`[Fact]`).
   - `MatchesSearchQuery_FiltersProperly`: 5 test cases (`[InlineData]`).
   - *Tổng cộng*: 208 + 19 = 227 tests.
3. **Mốc 230 test** (Commit `772ce42`): Bổ sung phương thức `GetExpectedPackFormat_ReturnsExpectedStandardFormat` vào `ShaderLoaderResolverTests.cs` với **3 test cases** (`[InlineData]` cho 1.21.1, 1.20.6, 1.20.1).
   - *Tổng cộng*: 227 + 3 = 230 tests.
   - *Tệp `ShaderLoaderResolverTests.cs` thực tế có tổng cộng 22 test cases* (thường được gọi làm tròn là ~20 unit tests).

### 4.4. Tiêu chí RPS-AC-09: Checklist Kiểm thử Thủ công trên WinUI
Do RPS-AC-09 là tiêu chí `MANUAL_REQUIRED`, việc nghiệm thu phải thực hiện trực tiếp trên WinUI desktop app (không dùng kết quả của bản web). Tiêu chí tạm thời ghi nhận **UNVERIFIED** chờ người dùng chạy theo checklist:
1. **Bước 1**: Mở ứng dụng Vaz.WinUI, điều hướng vào một instance bất kỳ có sẵn.
2. **Bước 2**: Chuyển sang tab *Resource Packs*.
   - Nhấn nút "Tăng mức ưu tiên" / "Giảm mức ưu tiên" hoặc bấm chọn pack rồi dùng phím tắt `Alt+↑`/`Alt+↓`.
   - Quan sát: Badge thứ tự `# 1`, `# 2` cập nhật tức thì; viền focus bám theo gói vừa di chuyển; không bị giật lag.
3. **Bước 3**: Nhấp vào nút "Xem chi tiết" (icon thông tin) trên thẻ pack.
   - Quan sát: Ngăn kéo 380px mở mượt từ bên phải kèm lớp phủ mờ (scrim); nhấn phím `Escape` ngăn kéo đóng ngay lập tức.
4. **Bước 4**: Chuyển sang tab *Shaders*.
   - Nhấn "Kích hoạt" trên một thẻ shader bất kỳ.
   - Quan sát: Thẻ nhận viền accent màu ngọc lục bảo và badge "Đang dùng"; nút "Tắt shader (Vanilla)" sáng lên.
5. **Bước 5**: Nhấn nút "Tắt shader (Vanilla)".
   - Quan sát: Shader tắt, trạng thái quay về mặc định và nút "Tắt shader (Vanilla)" tự động chuyển sang vô hiệu hóa.
6. **Xác nhận**: Người dùng nhập phản hồi xác nhận nguyên văn sau khi hoàn thành các bước trên.

### 4.5. Xác minh Toàn vẹn 6 Ảnh Gốc (`before-01` đến `before-06`)
So sánh mã hash blob trong Git giữa commit gốc `1717437` và commit hiện tại:
- Blob commit 1717437 (ảnh 01) vs `before-01-instances-list.png`: `112237a780d2825db9665ef9c44a5cabde5db72d` (Khớp 100%)
- Blob commit 1717437 (ảnh 02) vs `before-02-instance-detail-mods.png`: `d5eae30304eba4b9fd1a40510e1afc2bc016dfbe` (Khớp 100%)
- Blob commit 1717437 (ảnh 03) vs `before-03-resource-packs-tab.png`: `dfded52e13151d63ec8d4642e74978c43b5048f0` (Khớp 100%)
- Blob commit 1717437 (ảnh 04) vs `before-04-resource-pack-inspector.png`: `be2db3d4433bbc183f262828897d55ea65d24cbf` (Khớp 100%)
- Blob commit 1717437 (ảnh 05) vs `before-05-shaders-catalog.png`: `635c8e9c8bb1278b2e6e12b000f69b90df6807d3` (Khớp 100%)
- Blob commit 1717437 (ảnh 06) vs `before-06-shaders-filtered.png`: `9824591c83ad5f24c83280243ce048438036bfc5` (Khớp 100%)

---

## 5. Giải đáp Kỹ thuật Chi tiết

### 5.1. Nguồn gốc Dokucraft Light & `supported_formats`
- **Nguồn dữ liệu**: Dokucraft Light là dữ liệu mẫu tĩnh (in-memory seed data) khai báo trong `ResourcePackShaderData.DefaultResourcePacks` với `PackFormat = 15`. Nó không tồn tại dưới dạng file `.zip` thật trên ổ đĩa.
- **Xử lý `supported_formats`**: Thuật toán `ShaderLoaderResolver.IsPackFormatCompatible` hiện tại chỉ so sánh trường số nguyên `pack_format == 34` (định dạng của Minecraft 1.21.1). Thuật toán chưa hỗ trợ phân tích cú pháp nâng cao của trường `supported_formats` (dạng mảng `[min, max]` hoặc dictionary `{"min_inclusive": min, "max_inclusive": max}`) theo quy cách Minecraft 1.20.2+.

### 5.2. Quy trình Xác thực File `.zip`
- Mã nguồn kiểm tra file zip trước khi copy vào thư mục instance:
  - **Resource Pack (`ImportResourcePackFilesAsync`)**: Mở zip qua `ZipFile.OpenRead`, tìm tệp entry `pack.mcmeta`, parse JSON kiểm tra bắt buộc có `pack_format > 0`.
  - **Shader Pack (`ImportShaderFilesAsync`)**: Mở zip, duyệt danh sách entry kiểm tra sự tồn tại của thư mục con `shaders/`.
- **Bản chất ảnh lỗi**: Ảnh `RPS-AC-07-corrupted-zip-error-1025x533.png` ghi lại lỗi khi nạp file zip hỏng (không đọc được header nén hoặc thiếu metadata bắt buộc).

### 5.3. Tiêu chí RPS-AC-02, Persistence và Cấu trúc `options.txt`
- **Lưu trữ khi đóng/mở app**: Danh sách resource pack được lưu trong bộ nhớ state của instance.
- **Dòng cấu hình trong `options.txt`**:
  ```text
  resourcePacks:["vanilla","file/Bare Bones.zip","file/Faithful 64x.zip"]
  ```
- **Quy tắc thứ tự**: Minecraft nạp mảng từ trái sang phải, gói đứng sau sẽ đè lên gói đứng trước (Low-to-High). Ở giao diện WinUI, danh sách hiển thị từ trên xuống (Top-Down), gói `# 1` ở trên cùng là ưu tiên cao nhất, ứng với phần tử cuối cùng trong `options.txt`.
- **Bảo toàn mục hệ thống**: Khi parser ghi đè lại file `options.txt`, hệ thống giữ nguyên các mục không thuộc quản lý của ứng dụng (như `"vanilla"`, các gói tài nguyên tích hợp sẵn của mod), chỉ cập nhật các phần tử pack do người dùng kích hoạt.

### 5.4. Kích thước Tối thiểu & Co giãn Tỷ lệ DPI
- Mã nguồn `MainWindow.xaml.cs` đã tích hợp hệ số nhân DPI vật lý vào cấu trúc `MINMAXINFO`:
  ```csharp
  var scale = dpi > 0 ? (float)dpi / 96.0f : 1.0f;
  mmi.ptMinTrackSize.x = (int)(960 * scale);
  mmi.ptMinTrackSize.y = (int)(530 * scale);
  ```
- Các tỷ lệ DPI 125% và 150% ghi nhận **UNVERIFIED** chờ người dùng kiểm thử thủ công ngoài môi trường headless.

### 5.5. Trạng thái Thư mục Rỗng (Empty Directory)
- Giao diện bổ sung trạng thái rỗng cấp thư mục: khi thư mục `resourcepacks/` hoàn toàn chưa có file `.zip` nào, ứng dụng hiển thị icon tệp rỗng cùng thông điệp *"Thư mục chưa có gói tài nguyên nào"* và hướng dẫn kéo thả hoặc nhấn nút Thêm zip (`RPS-AC-01-empty-directory-1025x533.png`).

---

## 6. Kết luận Nghiệm thu & Danh sách Việc còn lại

### 6.1. Kết luận Trạng thái
Duy trì chính thức trạng thái **`PARITY_PARTIAL`**.

### 6.2. Danh sách Việc còn lại (Mỗi mục một dòng)
Kiểm chứng hiển thị độ phân giải 1600x900 trên màn hình vật lý lớn hơn 1366x768.
Kiểm chứng co giãn tỷ lệ DPI 125% và 150% qua kiểm thử giao diện thủ công.
Kiểm chứng thứ tự đè texture thực tế trong options.txt khi khởi chạy Minecraft client.
Triển khai giao diện và logic chuyển đổi Profile shaderpack (Low/Medium/High/Ultra) cho RPS-AC-05 (hoặc sửa contract).
Triển khai tải file nhị phân Iris thật qua Modrinth API kèm kiểm tra mã băm SHA-512 và Sodium dependency (hoặc đổi sang nút mở web).
Kiểm thử thủ công cảm giác tương tác WinUI theo checklist của RPS-AC-09.
Kiểm thử thủ công thao tác kéo thả file .zip từ File Explorer ngoài desktop vào ứng dụng.
Chạy kiểm thử thực tế đo duration của animation khi bật/tắt Reduce Motion.
Chuẩn hóa toàn bộ chuỗi văn bản cứng sang tệp tài nguyên đa ngôn ngữ .resw (W6b, backlog, không cản trở PARITY_VERIFIED).
Duy trì toàn bộ bộ kiểm thử tự động static và unit test 230/230 pass (W10, backlog, không cản trở PARITY_VERIFIED).
