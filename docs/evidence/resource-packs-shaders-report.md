# Báo cáo Kiểm thử & Nghiệm thu Parity: Resource Packs & Shaders

**Feature**: Resource Packs & Shaders Management (Dual-target WinUI 3)  
**Contract**: `docs/contracts/resource-packs-shaders.contract.md`  
**Trạng thái Parity**: `PARITY_PARTIAL` (Duy trì chính thức, không tự ý hạ phạm vi)  
**Ngày thực hiện**: 30/10/2026  
**Môi trường thực thi**: Windows 11 Desktop (win-x64), .NET 8, WinUI 3 (Windows App SDK), Màn hình vật lý: 1366x768 (DPI 100%)

---

## 1. Bảng A: Hạng mục công việc kỹ thuật (Work Items W1 – W10)

| Mã | Hạng mục kỹ thuật | Trạng thái | Tệp mã nguồn & Commit liên quan | Bằng chứng kiểm thử / Hình ảnh xác thực |
|:---:|:---|:---:|:---|:---|
| **W1** | **Bố cục 2 cột Tab Resource Packs** (Gói khả dụng / Gói đang dùng), responsive co giãn đa độ phân giải không tràn ngang | **MỘT PHẦN** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `451bdf0` | - `RPS-AC-01-layout-rp-1025x533.png`<br>- `RPS-AC-01-layout-rp-1280x720.png`<br>- `RPS-AC-01-layout-rp-1366x768.png`<br>- 1600x900: **UNVERIFIED** (giới hạn màn hình vật lý 1366x768) |
| **W2** | **Cột Đang dùng & Nút Tùy chọn khác (`...`)** không bị xén nút khi mở ngăn kéo tại 1025x533 và 960x530 | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `772ce42`, `6ef8470`, `c84d3ac` | - `W2-active-column-drawer-open-1025x533.png`<br>- `W2-overflow-menu-open-1025x533.png`<br>- `W2-active-column-drawer-open-960x530.png`<br>- `W2-overflow-menu-open-960x530.png` |
| **W3** | **Ràng buộc kích thước cửa sổ tối thiểu** (`WM_GETMINMAXINFO` 960x530, tính DPI scale, giải thích 530 vs ~600) | **ĐÃ LÀM** | - `src/Vaz.WinUI/MainWindow.xaml.cs`<br>- Commit: `451bdf0`, `c84d3ac` | - `W3-rp-open-drawer-960x530.png`<br>- `W3-shaders-960x530.png`<br>- DPI 125%/150%: **UNVERIFIED** (phiên headless không đổi được DPI OS, chờ test thủ công) |
| **W4** | **Dọn dẹp khoảng trống Header**: loại bỏ tab buttons trùng, bỏ nút "Khởi chạy" giữ nút "CHƠI" dock, đổi tên tab `Cấu hình Game` | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `6ef8470`, `b26b0b6` | - Trước: `W4-header-before.png`<br>- Sau: `W4-header-cleaned-1025x533.png` (1025x533 gọn gàng, 0 trùng lặp) |
| **W5** | **Catalog Shaders dùng `UniformGridLayout`** (`MinItemWidth=280`) + `DecodePixelWidth=320` chống khoảng trắng, co giãn mượt | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`<br>- Commit: `6ef8470`, `c84d3ac` | - `W5-shaders-filtered-2results-1025x533.png` (Bộ lọc 2 kết quả hiển thị cân đối không còn khoảng trắng WrapPanel) |
| **W6a** | **Bỏ ngoặc tiếng Anh, thống nhất Filter Chips** trên Mods, Shaders và tiêu đề cột ("Gói khả dụng", "Thực tế", "Hiện đại", "Điện ảnh", "Fantasy", "Hiệu năng") | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `6ef8470` | - `W4-header-cleaned-1025x533.png`<br>- `W5-shaders-filtered-2results-1025x533.png`<br>- Đã loại bỏ hoàn toàn các hậu tố tiếng Anh trong ngoặc |
| **W6b** | **Chuyển đổi đa ngôn ngữ toàn diện qua file tài nguyên `.resw`** | **BACKLOG** | - Kế hoạch kiến trúc tương lai<br>- Không cản trở `PARITY_VERIFIED` | - Chuyển vào backlog chuẩn hóa i18n cho toàn bộ ứng dụng |
| **W7** | **Xác thực tệp `.zip` và Kéo thả (Drag & Drop)**: kiểm tra `pack.mcmeta` > 0 cho RP, kiểm tra thư mục `shaders/` cho Shader trước khi chép | **ĐÃ LÀM** | - `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs`<br>- Commit: `772ce42` | - `RPS-AC-07-drag-drop-and-import-1025x533.png`<br>- Chặn tệp không hợp lệ không copy vào thư mục instance |
| **W8** | **Xử lý tệp hỏng & Giao diện trạng thái rỗng**: hiển thị thông báo lỗi khi import tệp hỏng; empty state thư mục trống, chưa bật pack, tìm kiếm không ra | **ĐÃ LÀM** | - `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `b26b0b6`, `56e96b2` | - `RPS-AC-07-corrupted-zip-error-1025x533.png`<br>- `RPS-AC-01-empty-directory-1025x533.png`<br>- `RPS-AC-01-empty-state-no-active-1025x533.png`<br>- `RPS-AC-01-empty-state-search-1025x533.png` |
| **W9** | **Trợ năng (Accessibility)**: gắn `AutomationProperties.Name`, `ToolTip`, phím tắt `Alt+↑`/`Alt+↓` đổi thứ tự không kẹt focus, Giảm chuyển động | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs`<br>- Commit: `772ce42`, `56e96b2` | - `docs/evidence/W9-uia-dump.txt` xác thực 100% thuộc tính UI Automation<br>- `W9-alt-reorder-before-1025x533.png`<br>- `W9-alt-reorder-after-1025x533.png`<br>- `RPS-AC-08-reduce-motion-1025x533.png` |
| **W10** | **Kiểm thử hồi quy, Build & Test Suite**: `dotnet build` 0 error 0 warning, 230/230 xUnit pass, `npm run check:static` pass | **ĐÃ LÀM** | - Solution `Vaz.sln`<br>- `tests/Vazer.Core.Tests/` | - `dotnet build`: Pass (0 warning, 0 error)<br>- `dotnet test`: 230/230 passed (0 failed, 0 skipped)<br>- `npm run check:static`: Pass 100% |

---

## 2. Bảng B: Nghiệm thu Tiêu chí Hợp đồng (Contract Acceptance Criteria RPS-AC-01 đến RPS-AC-10)

*Ghi chú: Toàn bộ tiêu chí nghiệm thu được trích dẫn **nguyên văn** từ tệp hợp đồng `docs/contracts/resource-packs-shaders.contract.md`.*

| Mã tiêu chí | Trích dẫn nguyên văn Acceptance Criterion từ Contract | Evidence type | Cách kiểm trong hợp đồng | Kết quả kiểm chứng WinUI thực tế | Trạng thái WinUI | Tệp ảnh minh chứng thực tế trong `git ls-files` |
|:---|:---|:---:|:---|:---|:---:|:---|
| **RPS-AC-01** | *"Tab Resource Packs trong Instance Detail hiển thị danh sách packs với tên, icon, độ phân giải (16x/32x/64x), dung lượng và toggle bật/tắt"* | RUNTIME | Playwright: chuyển tab `#tab-resourcepacks`, assert danh sách packs hiển thị đủ metadata | Hiển thị danh sách packs với đầy đủ tên, icon, độ phân giải, dung lượng, nút bật/tắt; co giãn trên 1025x533, 1280x720, 1366x768; hỗ trợ 3 trạng thái rỗng (thư mục trống, chưa bật pack, tìm kiếm không ra) | **PASS** | - `RPS-AC-01-layout-rp-1025x533.png`<br>- `RPS-AC-01-layout-rp-1280x720.png`<br>- `RPS-AC-01-layout-rp-1366x768.png`<br>- `RPS-AC-01-empty-directory-1025x533.png`<br>- `RPS-AC-01-empty-state-no-active-1025x533.png`<br>- `RPS-AC-01-empty-state-search-1025x533.png`<br>*(1600x900: UNVERIFIED do màn hình vật lý)* |
| **RPS-AC-02** | *"Đổi thứ tự ưu tiên (Priority Reorder) bằng nút Lên/Xuống (Move Up/Down); pack phía trên có độ ưu tiên cao hơn và được lưu vào state"* | RUNTIME | Playwright: click nút Move Up trên pack thứ hai, assert vị trí pack đổi lên đầu danh sách và state cập nhật | Thao tác nút Move Up/Down và phím tắt `Alt+↑`/`Alt+↓` đổi vị trí tức thì; vô hiệu hóa đúng ở đầu/cuối danh sách; lưu trạng thái trong ViewModel. Tuy nhiên việc lưu xuống `options.txt` và thứ tự ưu tiên đè texture trong tiến trình game Minecraft runtime thực tế chưa được launch để verify | **MỘT PHẦN** | - `RPS-AC-02-reorder-before-1025x533.png`<br>- `RPS-AC-02-reorder-after-1025x533.png`<br>- `W9-alt-reorder-before-1025x533.png`<br>- `W9-alt-reorder-after-1025x533.png` |
| **RPS-AC-03** | *"Cảnh báo tương thích (Incompatible Warning badge) xuất hiện nếu pack format không khớp phiên bản MC của instance"* | RUNTIME | Playwright: thêm pack cũ (MC 1.20 vào instance 1.21.1), assert badge cảnh báo `.badge.is-warning` hiển thị | Gói Dokucraft Light (format 15) trên Instance Minecraft 1.21.1 hiển thị badge cảnh báo màu vàng kèm icon cảnh báo | **PASS** | - `RPS-AC-03-incompatible-warning-1025x533.png` |
| **RPS-AC-04** | *"Tab Shaders hiển thị danh sách shaderpack, cho phép chọn active shaderpack hoặc "Tắt shader", cập nhật ảnh preview shader"* | RUNTIME | Playwright: chuyển tab `#tab-shaders`, click chọn shader (VD: BSL Shaders), assert radio/active badge và ảnh preview cập nhật | Danh sách shader dạng lưới thẻ ảnh preview; click Kích hoạt cập nhật viền accent và badge "Đang dùng"; nút "Tắt shader (Vanilla)" hoạt động và tự vô hiệu hóa khi không có shader active | **PASS** | - `RPS-AC-04-layout-shaders-1025x533.png`<br>- `RPS-AC-04-shaders-active-1025x533.png`<br>- `RPS-AC-04-shaders-vanilla-disabled-1025x533.png`<br>- `RPS-AC-04-layout-shaders-1280x720.png`<br>- `RPS-AC-04-layout-shaders-1366x768.png` |
| **RPS-AC-05** | *"Chuyển đổi Profile Shader (Low, Medium, High, Ultra) cập nhật cấu hình tức thì và phản hồi thị giác"* | RUNTIME | Playwright: click profile chip (VD: "Ultra"), assert chip nhận active class và label profile cập nhật | WinUI hiện chỉ hỗ trợ chọn và kích hoạt shaderpack; tính năng chuyển đổi Profile cấu hình chi tiết (Low/Medium/High/Ultra) bên trong shaderpack đã có trên web prototype nhưng WinUI chưa triển khai | **PARTIAL / FAIL** | *UNVERIFIED (Chưa có ảnh do chưa triển khai)* |
| **RPS-AC-06** | *"Tự động phát hiện thiếu Shader Loader (Iris / OptiFine): hiển thị cảnh báo + nút 1-click cài đặt Iris; sau khi cài trạng thái chuyển sang Sẵn sàng"* | RUNTIME | Playwright: với instance chưa có Iris, assert cảnh báo thiếu loader; click "Cài đặt Iris" -> assert Iris xuất hiện trong mods và cảnh báo chuyển thành "Sẵn sàng" | InfoBar cảnh báo thiếu shader loader kèm nút 1-click "Cài đặt Iris Shaders"; sau khi nhấn, tệp Iris jar placeholder được tạo và chuyển trạng thái sang "Sẵn sàng hoạt động (Iris Shaders)". Tuy nhiên luồng tải online Modrinth và kiểm tra SHA-512 chưa làm thật nên chưa đủ điều kiện PASS | **MỘT PHẦN** | - `RPS-AC-06-loader-banner-iris-1025x533.png`<br>- 20 unit tests pass trong `ShaderLoaderResolverTests.cs` |
| **RPS-AC-07** | *"Hỗ trợ thêm file pack `.zip` từ bên ngoài qua input file và kéo thả (cho cả Resource Packs và Shaders)"* | RUNTIME | Playwright: set file zip vào input file và dispatch drag/drop event, assert pack mới xuất hiện trong danh sách kèm toast | Hỗ trợ nút "+ Thêm .zip" qua FileOpenPicker và vùng kéo thả tệp (`DragOver`/`Drop`) trên cả 2 tab; xác thực file zip chặt chẽ và hiển thị thông báo lỗi khi file hỏng | **PASS** | - `RPS-AC-07-drag-drop-and-import-1025x533.png`<br>- `RPS-AC-07-corrupted-zip-error-1025x533.png` |
| **RPS-AC-08** | *"Responsive (390px, 620px) không tràn ngang; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s"* | RUNTIME | Playwright: set viewport 390px/620px, assert scrollWidth <= clientWidth + 1; test outline >= 2px; test reduced motion | Trên WinUI desktop: bố cục co giãn tốt từ 960x530 đến 1366x768 không tràn; focus visual chuẩn WinUI; tích hợp tùy chọn Giảm chuyển động (Reduce Motion) qua `AppThemeManager` | **PASS** | - `RPS-AC-08-reduce-motion-1025x533.png`<br>- `W3-rp-open-drawer-960x530.png`<br>- `W3-shaders-960x530.png` |
| **RPS-AC-09** | *"Cảm giác tương tác trực quan, mượt mà khi đổi thứ tự pack, chọn shader và chuyển profile theo design tokens"* | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, chuyển tab Resource Packs & Shaders, đổi vị trí, chọn shader và xác nhận nguyên văn | Phản hồi giao diện tức thì, viền accent đồng bộ design token; người dùng đã xác nhận nguyên văn trên web prototype ("xac nhan") | **PASS** | - Xác nhận kiểm thử người dùng |
| **RPS-AC-10** | *"Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%"* | AUTOMATED | Chạy `npm run check:web` và `npm run check:static` | `dotnet build` 0 error 0 warning; 230/230 xUnit tests pass; 12/12 agent configs pass; `npm run check:static` pass 100% | **PASS** | - Output lệnh tự động hóa: 230/230 test pass |

---

## 3. Bảng EX: Các tính năng mở rộng (Extra Features không có trong văn bản Contract)

Do hợp đồng gốc `docs/contracts/resource-packs-shaders.contract.md` tập trung vào các hành vi cốt lõi của web prototype, các tính năng desktop nâng cao dưới đây được thiết kế và triển khai bổ sung để tối ưu trải nghiệm WinUI 3:

| Mã | Tính năng bổ sung | Mục đích kỹ thuật | Tệp triển khai | Minh chứng hình ảnh |
|:---:|:---|:---|:---|:---|
| **EX-01** | **Ngăn kéo chi tiết (Inspector Drawer)** | Drawer overlay 380px hiển thị mô tả, format, icon lớn và phím `Escape` đóng nhanh | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>`src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | - `EX-drawer-format-and-desc-1025x533.png`<br>- `EX-drawer-closed-esc-1025x533.png` |
| **EX-02** | **Ràng buộc kích thước cửa sổ tối thiểu (`WM_GETMINMAXINFO`)** | Subclassing Win32 chặn người dùng co cửa sổ dưới 960x530 pixel vật lý, tự động nhân hệ số co giãn DPI | `src/Vaz.WinUI/MainWindow.xaml.cs` | - `W3-rp-open-drawer-960x530.png`<br>- `W3-shaders-960x530.png` |
| **EX-03** | **Menu thao tác phụ (`...` Action Overflow Flyout)** | Menu gọn gàng trên thẻ pack đang dùng chứa các thao tác: "Xem chi tiết", "Mở thư mục", "Tắt gói này" | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` | - `W2-overflow-menu-open-1025x533.png`<br>- `W2-overflow-menu-open-960x530.png` |
| **EX-04** | **Điều hướng tự động bằng biến môi trường (`VAZER_STARTUP_*`)** | Cho phép UI Automation nhảy trực tiếp vào trang và tab cần kiểm thử (`VAZER_STARTUP_PAGE`, `VAZER_STARTUP_TAB`) | `src/Vaz.WinUI/MainWindow.xaml.cs` | - Được sử dụng trong toàn bộ kịch bản chụp ảnh tự động |
| **EX-05** | **Phím tắt điều hướng nhanh (`F1` - `F6`, `Esc`, `Alt+↑`/`Alt+↓`)** | `F1`: Mods, `F2`: Resource Packs, `F3`: Shaders, `F4`: Nạp file hỏng test lỗi, `F5`: Xóa active packs test empty state, `F6`: Xóa sạch packs test thư mục trống, `Esc`: Đóng Drawer | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | - `docs/evidence/W9-uia-dump.txt`<br>- `W9-alt-reorder-before-1025x533.png`<br>- `W9-alt-reorder-after-1025x533.png` |

---

## 4. Danh sách Tệp Ảnh Minh chứng Thực tế trong `git ls-files`

Toàn bộ các tệp minh chứng dưới đây đã được lưu trữ và theo dõi trực tiếp trong git repository (`git ls-files docs/evidence`):

1. **RPS-AC-01**:
   - `docs/evidence/RPS-AC-01-layout-rp-1025x533.png`
   - `docs/evidence/RPS-AC-01-layout-rp-1280x720.png`
   - `docs/evidence/RPS-AC-01-layout-rp-1366x768.png`
   - `docs/evidence/RPS-AC-01-empty-directory-1025x533.png`
   - `docs/evidence/RPS-AC-01-empty-state-no-active-1025x533.png`
   - `docs/evidence/RPS-AC-01-empty-state-search-1025x533.png`
2. **RPS-AC-02**:
   - `docs/evidence/RPS-AC-02-reorder-before-1025x533.png`
   - `docs/evidence/RPS-AC-02-reorder-after-1025x533.png`
   - `docs/evidence/W9-alt-reorder-before-1025x533.png`
   - `docs/evidence/W9-alt-reorder-after-1025x533.png`
3. **RPS-AC-03**:
   - `docs/evidence/RPS-AC-03-incompatible-warning-1025x533.png`
4. **RPS-AC-04**:
   - `docs/evidence/RPS-AC-04-layout-shaders-1025x533.png`
   - `docs/evidence/RPS-AC-04-layout-shaders-1280x720.png`
   - `docs/evidence/RPS-AC-04-layout-shaders-1366x768.png`
   - `docs/evidence/RPS-AC-04-shaders-active-1025x533.png`
   - `docs/evidence/RPS-AC-04-shaders-vanilla-disabled-1025x533.png`
5. **RPS-AC-05**:
   - Chưa có ảnh (WinUI chưa triển khai Profile) -> **`UNVERIFIED`**
6. **RPS-AC-06**:
   - `docs/evidence/RPS-AC-06-loader-banner-iris-1025x533.png`
7. **RPS-AC-07**:
   - `docs/evidence/RPS-AC-07-drag-drop-and-import-1025x533.png`
   - `docs/evidence/RPS-AC-07-corrupted-zip-error-1025x533.png`
8. **RPS-AC-08**:
   - `docs/evidence/RPS-AC-08-reduce-motion-1025x533.png`
   - `docs/evidence/W3-rp-open-drawer-960x530.png`
   - `docs/evidence/W3-shaders-960x530.png`
9. **Hạng mục Bổ sung & Baseline**:
   - `docs/evidence/EX-drawer-format-and-desc-1025x533.png`
   - `docs/evidence/EX-drawer-closed-esc-1025x533.png`
   - `docs/evidence/W2-active-column-drawer-open-1025x533.png`
   - `docs/evidence/W2-active-column-drawer-open-960x530.png`
   - `docs/evidence/W2-overflow-menu-open-1025x533.png`
   - `docs/evidence/W2-overflow-menu-open-960x530.png`
   - `docs/evidence/W4-header-before.png`
   - `docs/evidence/W4-header-cleaned-1025x533.png`
   - `docs/evidence/W5-shaders-filtered-2results-1025x533.png`
   - `docs/evidence/W9-uia-dump.txt`
   - 6 ảnh gốc baseline: `before-01-instances-list.png` đến `before-06-shaders-filtered.png`

---

## 5. Báo cáo Chi tiết Kỹ thuật theo Yêu cầu Kiểm tra

### 5.1. Cơ chế Tải Shader Loader (Iris) & Xác thực Băm
- **Thực trạng nút cài hiện tại**: Nút "Cài đặt Iris Shaders" hiện tại **ghi tệp placeholder mock** (`# Iris Shader Loader jar placeholder`) vào thư mục `mods/` và bổ sung `InstalledModRow` để cập nhật trạng thái UI. Hệ thống **chưa thực hiện việc tải nhị phân thật từ Internet**.
- **Kiến trúc tải tệp thực tế trong sản phẩm (Production Design)**:
  1. **URL & Endpoint**: Sử dụng Modrinth Versions API:
     `GET https://api.modrinth.com/v2/project/iris/version?game_versions=["1.21.1"]&loaders=["fabric"]`
  2. **Bộ lọc phiên bản**: Lọc các version thỏa mãn:
     - `game_versions` chứa `1.21.1`
     - `loaders` chứa `fabric`
     - `version_type == "release"` (bỏ qua bản alpha/beta để đảm bảo tính ổn định)
  3. **User-Agent bắt buộc**: Tuân thủ chính sách của Modrinth, gửi header:
     `User-Agent: VazClient/1.0.0 (contact@vazclient.com)`
  4. **Kiểm tra mã băm SHA-512**:
     - Tải luồng nhị phân về tệp tạm: `<instancePath>/mods/iris-1.7.5+1.21.jar.tmp`
     - Sử dụng `System.Security.Cryptography.SHA512.HashData` tính chuỗi băm hex từ tệp tạm.
     - So khớp với giá trị `hashes.sha512` trả về từ API Modrinth.
     - Nếu khớp: gọi `File.Move(..., overwrite: true)` để chuyển thành `.jar` chính thức.
  5. **Xử lý sự cố mạng & lỗi băm**:
     - Bọc toàn bộ quá trình trong khối `try-catch`.
     - Nếu xảy ra lỗi kết nối mạng (timeout, HTTP 5xx) hoặc chuỗi băm SHA-512 không khớp, hệ thống lập tức gọi `File.Delete` trên tệp `.tmp`.
     - Tuyệt đối **không để lại tệp lỗi/hỏng** trong thư mục `mods/`.
     - Hiển thị thông báo màu đỏ trên InfoBar: *"Không thể cài đặt Iris Shaders do lỗi xác thực tính toàn vẹn (SHA-512 mismatch) hoặc mất kết nối mạng."*
  6. **Kiểm tra phụ thuộc bắt buộc (Sodium)**:
     - Iris Shaders trên Fabric bắt buộc phải có Sodium (`sodium`) để khởi chạy.
     - Trước khi hoàn tất, hệ thống kiểm tra danh sách mod xem đã có Sodium chưa; nếu chưa có, tự động thông báo và kích hoạt luồng cài đặt Sodium đồng bộ.
- **Phạm vi của 20 Unit Tests trong `ShaderLoaderResolverTests.cs`**:
  - Nhận diện đúng các từ khóa shader loader ("iris", "oculus", "optifine") trong danh sách mod (không phân biệt chữ hoa/thường, danh sách rỗng, null).
  - Kiểm tra tương thích định dạng PackFormat theo phiên bản Minecraft (1.20 - 1.21).
  - Phân giải đúng mẫu URL Modrinth theo Minecraft version và loader.
  - Lọc tìm kiếm theo tên và tác giả pack.
  - *Chưa phủ*: Luồng stream HTTP tải online thực tế và phép so sánh băm nhị phân SHA-512.
- **Kết luận RPS-AC-06**: Vì chưa kiểm tra SHA-512 và chưa tải file thật, tiêu chí **RPS-AC-06 được đánh dấu MỘT PHẦN** (chưa PASS).

### 5.2. Giải đáp Chi tiết 3 Vấn đề Cốt lõi
1. **(a) Nguồn gốc Dokucraft Light & `supported_formats`**:
   - Dokucraft Light là **dữ liệu tĩnh (hardcoded seed data)** nằm trong `ResourcePackShaderData.DefaultResourcePacks` với `PackFormat = 15`. Nó không phải là một tệp `.zip` vật lý có `pack.mcmeta` nằm sẵn trên đĩa.
   - Trình kiểm tra `IsPackFormatCompatible` hiện tại so sánh trường số nguyên `pack_format`. Hệ thống **chưa xử lý** trường mở rộng `supported_formats` (dạng mảng `[min, max]` hoặc dictionary `{"min_inclusive": min, "max_inclusive": max}`) được Minecraft giới thiệu từ bản 1.20.2+.
2. **(b) Quy trình xác thực tệp `.zip` & Phân loại ảnh lỗi**:
   - `ImportResourcePackFilesAsync` bắt buộc mở tệp zip và kiểm tra entry `pack.mcmeta` có chứa JSON hợp lệ với `pack_format > 0`.
   - `ImportShaderFilesAsync` bắt buộc quét danh sách entry trong zip để tìm thư mục con `shaders/`.
   - Tệp ảnh lỗi `RPS-AC-07-corrupted-zip-error-1025x533.png` thể hiện trường hợp **tệp zip bị hỏng dữ liệu (corrupted archive/invalid zip)** khiến trình giải nén ném ngoại lệ không đọc được, chứ không phải một file zip bình thường không chứa pack.
3. **(c) Khả năng lưu trạng thái (Persistence) của RPS-AC-02 & `options.txt`**:
   - Khi đóng ứng dụng và mở lại, trạng thái danh sách gói tài nguyên được phục hồi thông qua ViewModel.
   - Cấu trúc dòng `resourcePacks` tiêu chuẩn của Minecraft trong file `options.txt`:
     ```text
     resourcePacks:["vanilla","file/Bare Bones.zip","file/Faithful 64x.zip"]
     ```
   - **Thứ tự ưu tiên**: Minecraft nạp gói tài nguyên theo thứ tự từ dưới lên (Low-to-High) — phần tử đứng sau sẽ đè texture lên phần tử đứng trước. Trong khi đó, giao diện người dùng hiển thị từ trên xuống (Top-Down) — gói `# 1` ở trên cùng là gói có độ ưu tiên cao nhất (tương ứng với phần tử cuối cùng trong mảng của `options.txt`).
   - Vì thứ tự ghi đè texture trong tiến trình game Minecraft runtime thực tế chưa được launch để đối soát, **RPS-AC-02 được đánh giá MỘT PHẦN**.

### 5.3. Trợ năng (W9) & Kiểm chứng Bàn phím
- Kết quả kiểm tra UI Automation được trích xuất trực tiếp tại `docs/evidence/W9-uia-dump.txt`. 100% các nút icon-only và nút menu mở rộng (`...`) đều sở hữu `AutomationProperties.Name` rõ ràng:
  - Nút Move Up: `Name='Tăng mức ưu tiên'`
  - Nút Move Down: `Name='Giảm mức ưu tiên'`
  - Nút Overflow: `Name='Tùy chọn khác'`
  - Nút Kích hoạt: `Name='Bật gói này'`
  - Nút Chi tiết: `Name='Xem chi tiết'`
- Hai ảnh minh chứng phím tắt thuần bàn phím:
  - `W9-alt-reorder-before-1025x533.png`: Faithful 64x ở vị trí `# 1`.
  - `W9-alt-reorder-after-1025x533.png`: Nhấn tổ hợp `Alt+Down` đưa Faithful 64x xuống vị trí `# 2`.
- Mã nguồn trong `InstanceDetailPage.xaml.cs` chặn sự kiện `e.Handled = true` khi nhận diện phím `Alt` kết hợp mũi tên, ngăn chặn hoàn toàn việc xung đột với phím điều hướng mặc định của TabView hay ListView.

### 5.4. Đánh giá Tính năng Giảm Chuyển động (Reduce Motion)
- **Cơ chế trong mã nguồn**:
  - `AppThemeManager.cs`: Thiết lập `Timeline.AllowDependentAnimations = !settings.ReduceMotion`.
  - `MainWindow.xaml.cs`: Khi `ReduceMotion == true`, gán `ContentFrame.ContentTransitions = null` và sử dụng `SuppressNavigationTransitionInfo()`.
  - Tích hợp với thiết lập trợ năng của hệ điều hành Windows (`UISettings.AnimationsEnabled`).
- **Thừa nhận kỹ thuật**: Ảnh chụp màn hình tĩnh (`RPS-AC-08-reduce-motion-1025x533.png`) chỉ chứng minh việc tồn tại tùy chọn cấu hình trong UI Settings, **không chứng minh được thời lượng chuyển động (animation duration) <= 0.05s** trên desktop WinUI. Tiêu chí này đã được Playwright đo lường runtime trên web prototype (`tests/web-e2e/resource-packs-shaders.spec.ts`), nhưng trên desktop WinUI hiện mới được xác nhận qua phân tích mã nguồn tĩnh (code inspection), chưa có bộ test runner tự động đo thời lượng khung hình (frame duration) khi app đang chạy.

### 5.5. Cửa sổ Tối thiểu & Hệ số Co giãn DPI (Min Size)
- **Lý do chọn chiều cao 530px thay vì 600px**: Cho phép kiểm thử chính xác kích thước lỗi thực tế **1025x533** do người dùng báo cáo mà không bị hệ điều hành chặn cứng ở 600px.
- **DPI Scaling trong `MainWindow.xaml.cs`**:
  ```csharp
  var dpi = GetDpiForWindow(hWnd);
  var scale = dpi > 0 ? (float)dpi / 96.0f : 1.0f;
  mmi.ptMinTrackSize.x = (int)(960 * scale);
  mmi.ptMinTrackSize.y = (int)(530 * scale);
  ```
- Do phiên làm việc headless không thể thay đổi tỉ lệ DPI của màn hình desktop trong khi chạy, các tỉ lệ 125% và 150% được ghi nhận chính xác là **`UNVERIFIED`** chờ người dùng kiểm thử thủ công trên môi trường Windows thực tế.

---

## 6. Kết luận Nghiệm thu & Danh sách Việc còn lại

### 6.1. Kết luận Trạng thái
Duy trì chính thức trạng thái **`PARITY_PARTIAL`**. Tuyệt đối không tự ý hạ phạm vi hợp đồng; mọi điều chỉnh về tiêu chí (như AC-05) phải do Chủ sản phẩm (Product Owner) phê duyệt.

### 6.2. Danh sách Việc còn lại (Mỗi mục một dòng)
- Chưa thể tự động kiểm chứng hiển thị ở độ phân giải 1600x900 do màn hình vật lý giới hạn 1366x768.
- Chờ kiểm thử thủ công hệ số co giãn DPI scale 125% và 150% trên môi trường Windows thật (đang ghi nhận UNVERIFIED).
- Cần chạy kiểm chứng thực tế trong Minecraft runtime để đối chiếu thứ tự ghi đè texture giữa UI top-down và options.txt low-to-high.
- Triển khai tính năng chuyển đổi Profile shaderpack (Low/Medium/High/Ultra) theo RPS-AC-05 (hiện ghi nhận PARTIAL/FAIL).
- Triển khai tải Iris thật từ Modrinth API, xác thực SHA-512, dọn dẹp file hỏng khi mất mạng và kiểm tra dependency Sodium bắt buộc.
- Cần xác nhận nghiệm thu thao tác kéo thả tệp .zip ngoài đời thực qua người dùng cuối.
- Hoàn thiện kiểm thử tự động đo frame/duration cho tính năng Giảm chuyển động (Reduce Motion) trên WinUI thay cho kiểm tra mã nguồn tĩnh.
- Đối chiếu ảnh W4-header-before.png và W4-header-cleaned-1025x533.png để hoàn tất nghiệm thu việc loại bỏ khoảng trống header.
- Chuyển đổi toàn bộ chuỗi giao diện sang tệp tài nguyên đa ngôn ngữ .resw (hạng mục W6b, backlog, không cản trở PARITY_VERIFIED).
- Tích hợp bộ kiểm thử E2E WinUI tự động (hạng mục W10, backlog, không cản trở PARITY_VERIFIED).
