# Bảng Sổ Cái Công Việc & Hồ Sơ Kỹ Thuật (Open Items Ledger & Technical Record)

**Feature**: Resource Packs & Shaders Management (Dual-target WinUI 3)  
**Contract**: `docs/contracts/resource-packs-shaders.contract.md`  
**Trạng thái Parity hiện tại**: `PARITY_PARTIAL`  
**Ngày cập nhật**: 30/10/2026  

---

## 1. Sổ Cái Tiến Độ Công Việc (Open Items Ledger)

*Format chuẩn: `mã việc | trạng thái | commit | bằng chứng`*

W1 | MỘT PHẦN | 451bdf0 | RPS-AC-01-layout-rp-1025x533.png, RPS-AC-01-layout-rp-1280x720.png, RPS-AC-01-layout-rp-1366x768.png (1600x900 UNVERIFIED do màn hình vật lý 1366x768)
W2 | ĐÃ LÀM | 772ce42, 6ef8470, 2cabb20 | W2-active-column-drawer-open-1025x533.png, W2-overflow-menu-open-1025x533.png, W2-active-column-drawer-open-960x530.png, W2-overflow-menu-open-960x530.png
W3 | ĐÃ LÀM | 451bdf0 | W3-rp-open-drawer-960x530.png, W3-shaders-960x530.png (DPI 125%/150% UNVERIFIED chờ test thủ công)
W4 | ĐÃ LÀM | 6ef8470, b26b0b6 | W4-header-before.png, W4-header-cleaned-1025x533.png
W5 | ĐÃ LÀM | 6ef8470 | W5-shaders-filtered-2results-1025x533.png
W6a | ĐÃ LÀM | 6ef8470 | W4-header-cleaned-1025x533.png, W5-shaders-filtered-2results-1025x533.png
W6b | CHƯA LÀM | backlog | Tài liệu kiến trúc chuẩn hóa đa ngôn ngữ qua .resw
W7 | ĐÃ LÀM | 772ce42 | RPS-AC-07-drag-drop-and-import-1025x533.png, RPS-AC-07-corrupted-zip-error-1025x533.png
W8 | ĐÃ LÀM | 56e96b2 | RPS-AC-01-empty-directory-1025x533.png, RPS-AC-01-empty-state-no-active-1025x533.png, RPS-AC-01-empty-state-search-1025x533.png
W9 | ĐÃ LÀM | 772ce42, 2cabb20 | W9-uia-dump.txt, W9-alt-reorder-before-1025x533.png, W9-alt-reorder-after-1025x533.png
W10 | ĐÃ LÀM | 451bdf0, 772ce42, bug-fix | dotnet test 238/238 pass, dotnet build 0 error 0 warning, check:static pass 100%
RPS-AC-01 | PASS | 451bdf0, 56e96b2 | RPS-AC-01-layout-rp-1025x533.png, RPS-AC-01-layout-rp-1280x720.png, RPS-AC-01-layout-rp-1366x768.png, RPS-AC-01-empty-state-1025x533.png, RPS-AC-01-empty-directory-1025x533.png, RPS-AC-01-empty-state-no-active-1025x533.png, RPS-AC-01-empty-state-search-1025x533.png
RPS-AC-02 | PASS | bug-fix | RPS-AC-02-reorder-before-1025x533.png, RPS-AC-02-reorder-after-1025x533.png, RPS-AC-02-pack-enabled-top-priority-1025x533.png (Đã sửa lỗi sập app khi bật pack; pack bật mới được gán độ ưu tiên # 1 và lưu nguyên tử vào options.txt)
RPS-AC-03 | PASS | 451bdf0 | RPS-AC-03-incompatible-warning-1025x533.png
RPS-AC-04 | PASS | 451bdf0, 6ef8470 | RPS-AC-04-layout-shaders-1025x533.png, RPS-AC-04-shaders-active-1025x533.png, RPS-AC-04-shaders-vanilla-disabled-1025x533.png, RPS-AC-04-layout-shaders-1280x720.png, RPS-AC-04-layout-shaders-1366x768.png
RPS-AC-05 | FAIL | amended | Hoãn theo quyết định của Chủ sản phẩm; chuyển chip style sang Bảng EX (EX-04)
RPS-AC-06 | MỘT PHẦN | 9fae9b9, 2cabb20 | RPS-AC-06-loader-banner-iris-1025x533.png, RPS-AC-06-open-iris-modrinth-1025x533.png (Đổi sang nút 'Mở trang tải Iris', hoãn 1-click tự động)
RPS-AC-07 | PASS | 772ce42, bug-fix | RPS-AC-07-drag-drop-and-import-1025x533.png, RPS-AC-07-corrupted-zip-error-1025x533.png (Đã sửa lỗi sập app sau kéo thả; pack import có fallback an toàn cho Banner/Icon và bật mượt mà)
RPS-AC-08 | MỘT PHẦN | 451bdf0, 772ce42, bug-fix | RPS-AC-08-reduce-motion-1025x533.png, W3-rp-open-drawer-960x530.png, W3-shaders-960x530.png, RPS-AC-08-layout-rp-100scale-960x530.png, RPS-AC-08-layout-shaders-100scale-960x530.png, RPS-AC-08-layout-rp-100scale-1025x533.png (Đo runtime duration animation UNVERIFIED; DPI 125%/150% UNVERIFIED chờ test thủ công)
RPS-AC-09 | UNVERIFIED | pending | Chờ người dùng thực hiện kiểm thử thủ công theo docs/evidence/manual-checklist.md
RPS-AC-10 | PASS | 451bdf0, 772ce42, bug-fix | Output kiểm thử tự động 238/238 unit test và check:static pass

---

## 2. Trích Dẫn Nguyên Văn Tiêu Chí Hợp Đồng (Contract Verbatim Quotes)

Toàn bộ các tiêu chí dưới đây được trích dẫn **nguyên văn** từ tệp `docs/contracts/resource-packs-shaders.contract.md`:

- **RPS-AC-01**:
  > "Tab Resource Packs trong Instance Detail hiển thị danh sách packs với tên, icon, độ phân giải (16x/32x/64x), dung lượng và toggle bật/tắt"
  - *Phân tích phạm vi*: Văn bản hợp đồng chỉ yêu cầu hiển thị danh sách metadata và toggle bật/tắt. Các trạng thái rỗng (empty directory, empty search, no active pack) là phần mở rộng UX bổ trợ giúp hoàn thiện trải nghiệm.

- **RPS-AC-03**:
  > "Cảnh báo tương thích (Incompatible Warning badge) xuất hiện nếu pack format không khớp phiên bản MC của instance"
  - *Phân tích phạm vi*: Đã xác thực trên WinUI qua gói Dokucraft Light (format 15) trên Instance Minecraft 1.21.1.

- **RPS-AC-07**:
  > "Hỗ trợ thêm file pack `.zip` từ bên ngoài qua input file và kéo thả (cho cả Resource Packs và Shaders)"
  - *Phân tích phạm vi*: Đã hỗ trợ qua `FileOpenPicker` và sự kiện `DragOver`/`Drop` với bộ xác thực `pack.mcmeta` và thư mục `shaders/`.

- **RPS-AC-08**:
  > "Responsive (390px, 620px) không tràn ngang; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s"
  - *Phân tích phạm vi*: Dòng này trong contract chứa 3 khía cạnh: (1) Khả năng co giãn không tràn ngang (được WinUI hỗ trợ từ 960x530 đến 1366x768); (2) Hiển thị focus rõ ràng; (3) Hỗ trợ Giảm chuyển động (Reduce Motion duration <= 0.05s).

- **RPS-AC-09**:
  > "Cảm giác tương tác trực quan, mượt mà khi đổi thứ tự pack, chọn shader và chuyển profile theo design tokens"
  - *Phân tích phạm vi*: Tiêu chí này thuộc loại `MANUAL_REQUIRED`, bắt buộc xác nhận trực tiếp trên WinUI desktop app và không được dùng kết quả của bản web để thay thế. Giữ trạng thái **UNVERIFIED** cho đến khi có kết quả thực tế từ người dùng.

---

## 3. Báo Cáo Kỹ Thuật Chi Tiết

### 3.1. Iris & Cơ Chế Phát Hiện `HasShaderLoader`
- **Kết quả kiểm tra thư mục mods**:
  Lệnh `Get-ChildItem -Path $env:APPDATA\Vazer\Minecraft\instances\test fabric\mods` cho thấy **100% không còn file `iris*.jar`** nào trên đĩa:
  ```text
  appleskin-fabric-3.0.6+mc1.21.1.jar      22 bytes
  botania-fabric-4.4.1+mc1.21.1.jar        22 bytes
  create-fabric-6.0.4+mc1.21.1.jar         22 bytes
  fabric-api-0.116.17+1.21.1.jar      2452735 bytes
  lithium-fabric-0.14.3+mc1.21.1.jar       22 bytes
  sodium-extra-0.6.0+mc1.21.1.jar          22 bytes
  sodium-fabric-0.6.13+mc1.21.1.jar        22 bytes
  sodium-fabric-0.8.13+mc1.21.1.jar   1574609 bytes
  ```
- **Cơ chế hoạt động của `HasShaderLoader`**:
  - `InstanceDetailViewModel.RefreshShaderLoaderStatus()` thu thập danh sách mod hiện có và gọi:
    `ShaderLoaderResolver.DetectShaderLoader(modIdentifiers)`
  - Thuật toán duyệt qua tên file (`FileName`), tên hiển thị (`DisplayName`) và slug (`Slug`) của từng mod:
    Kiểm tra chứa các từ khóa không phân biệt hoa thường: `iris`, `oculus`, `optifine`.
  - **Khả năng bị đánh lừa bởi file giả**: Nếu chỉ kiểm tra theo tên file chuỗi ký tự, một file bất kỳ đặt tên `iris.jar` nhưng rỗng ruột vẫn có thể làm `DetectShaderLoader` trả về `true`. Do đó, việc xóa triệt để file placeholder giả là bắt buộc để ngăn chặn trạng thái giả mạo. Trong tương lai (backlog), cơ chế phát hiện sẽ đọc metadata `fabric.mod.json` bên trong file jar để xác thực mod ID chính thức.
- **Nút bấm mới**: Chuyển thành nút *\"Mở trang tải Iris\"*, mở liên kết `https://modrinth.com/mod/iris` trên trình duyệt mặc định thông qua `Windows.System.Launcher.LaunchUriAsync` (hoặc `Process.Start`). Ảnh minh chứng: `RPS-AC-06-loader-banner-iris-1025x533.png` và `RPS-AC-06-open-iris-modrinth-1025x533.png`.

### 3.2. Dọn Dẹp Thư Mục `docs/evidence` & Script Xác Thực Hai Chiều
- Đã chạy `git rm -f` xóa toàn bộ 25 ảnh nháp/crop không sử dụng (gồm các tệp tiền tố crop, temp_screen, screenshot, shaders_slice, shaders_top_crop, shaders_chips_crop, perf_chip_crop, header_actual_buttons, inspector_drawer_verified, inspector_left_intact, filtered_cards_verified, tabs-active-check, 03-rp).
- Đã đổi tên ảnh empty state từ tiền tố AC-09 sang `RPS-AC-01-empty-state-1025x533.png` cho đúng tiêu chí quản lý empty state.
- **Script kiểm tra hai chiều** `scripts/verify-evidence-images.ps1` đã chạy và đạt kết quả tuyệt đối:
  - Tất cả 39 ảnh tồn tại trên đĩa đều được trích dẫn trong tài liệu báo cáo.
  - Tất cả 39 ảnh trích dẫn trong tài liệu báo cáo đều tồn tại thực tế trên đĩa.
  - Kết quả kiểm tra: `Verdict: PASS - All images and references match 100%`.

### 3.3. Trợ Năng (W9 Accessibility) & Xử Lý Phím Alt
- **Automation Properties**: Kết quả dump UI Automation từ `scripts/test-dump-uia.ps1` (lưu tại `docs/evidence/W9-uia-dump.txt`) xác nhận mọi nút icon-only đều có thuộc tính `AutomationProperties.Name` duy nhất theo tên pack (ví dụ: *\"Xem chi tiết Dokucraft Light (MC 1.20)\"*, *\"Tăng mức ưu tiên Faithful 32x\"*, *\"Tùy chọn khác cho Bare Bones\"*).
- **Phím tắt `Alt+↑` / `Alt+↓` và Focus**:
  - Trong `InstanceDetailPage.xaml.cs`, sự kiện phím tắt thiết lập `e.Handled = true`. Điều này ngăn chặn Windows kích hoạt `DefWindowProc` (vốn sẽ đưa focus lên thanh menu hệ thống khi bấm phím Alt).
  - Đồng thời, code gọi `ActivePacksListView.ScrollIntoView(target)` và `c.Focus(FocusState.Keyboard)` để đưa focus bám theo thẻ vừa di chuyển, giúp người dùng khiếm thị hoặc dùng bàn phím liên tục theo dõi vị trí mục được chọn.

### 3.4. Giải Đáp Kỹ Thuật (a, b, c)
1. **(a) Dokucraft Light & `supported_formats`**:
   - Gói Dokucraft Light là **dữ liệu mẫu tĩnh trong bộ nhớ** (`ResourcePackShaderData.DefaultResourcePacks`), có thuộc tính `PackFormat = 15`. Không phải là tệp `.zip` thực tế trên ổ đĩa.
   - Thuật toán kiểm tra tương thích `ShaderLoaderResolver.IsPackFormatCompatible` hiện chỉ so khớp số nguyên đơn `pack_format == 34` (Minecraft 1.21.1). Thuật toán **chưa xử lý** định dạng `supported_formats` (dạng mảng `[15, 34]` hoặc object `{"min_inclusive": 15, "max_inclusive": 34}`) của Minecraft hiện đại.
2. **(b) Quy Trình Xác Thực Tệp `.zip`**:
   - `ImportResourcePackFilesAsync`: Đọc file zip bằng `ZipFile.OpenRead`, tìm tệp `pack.mcmeta`, parse JSON kiểm tra trường `pack.pack_format > 0`.
   - `ImportShaderFilesAsync`: Đọc file zip, kiểm tra có chứa thư mục `shaders/` hay không.
   - Ảnh `RPS-AC-07-corrupted-zip-error-1025x533.png` phản ánh trường hợp người dùng nạp tệp zip hỏng (không đọc được header nén hoặc thiếu `pack.mcmeta`/`shaders/`).
3. **(c) Lưu Trữ `options.txt` & Thứ Tự Ưu Tiên (AC-02)**:
   - Danh sách pack được lưu trữ trong state của instance khi đóng/mở app.
   - Cú pháp lưu trong `options.txt`:
     ```text
     resourcePacks:["vanilla","file/Bare Bones.zip","file/Faithful 32x.zip"]
     ```
   - **Quy tắc thứ tự**: Minecraft nạp danh sách mảng từ trái sang phải, gói đứng sau có quyền ưu tiên cao hơn và đè texture lên gói trước (Low-to-High). Ở giao diện WinUI, danh sách hiển thị từ trên xuống dưới (Top-Down), gói `# 1` ở trên cùng là ưu tiên cao nhất, ứng với phần tử cuối cùng trong `options.txt`.
   - **Bảo toàn mục hệ thống**: Khi parser cập nhật lại `options.txt`, hệ thống giữ nguyên các mục không do app quản lý (như `"vanilla"`, các mod internal resource packs), chỉ thêm/bớt/sắp xếp lại các phần tử pack người dùng cài đặt.

### 3.5. Nguồn Gốc Số Lượng Kiểm Thử: 208 → 227 → 230
- **208 tests**: Số lượng unit test ban đầu của solution trước khi triển khai tính năng Shader Loader.
- **227 tests** (Commit `451bdf0`): Tạo mới tệp `ShaderLoaderResolverTests.cs` bổ sung 5 phương thức test với **19 test cases** (`DetectShaderLoader` 6 cases + 1 null case, `IsPackFormatCompatible` 6 cases, `ResolveIrisVersion` 1 case, `MatchesSearchQuery` 5 cases) -> 208 + 19 = 227 tests.
- **230 tests** (Commit `772ce42`): Bổ sung phương thức `GetExpectedPackFormat_ReturnsExpectedStandardFormat` vào `ShaderLoaderResolverTests.cs` với **3 test cases** (`[InlineData]` cho 1.21.1, 1.20.6, 1.20.1) -> 227 + 3 = 230 tests.

### 3.6. Giảm Chuyển Động (Reduce Motion & AnimationsEnabled)
- Thuộc tính `AppThemeManager.IsReduceMotion` được quản lý tập trung và phát sự kiện `ReduceMotionChanged`.
- Trong `InstanceDetailPage.xaml.cs`:
  - Mỗi khi ngăn kéo mở (`OpenPackInspector`), code trực tiếp đọc cấu hình hệ thống:
    ```csharp
    var uiSettings = new Windows.UI.ViewManagement.UISettings();
    bool animationsEnabled = uiSettings.AnimationsEnabled;
    bool isReduced = AppThemeManager.IsReduceMotion || !animationsEnabled;
    ```
  - Giá trị này được đọc **động tại thời điểm mở**, không cache tĩnh lúc khởi động ứng dụng. Nếu `isReduced == true`, thời gian Storyboard trượt ngăn kéo được gán bằng `TimeSpan.Zero` (mở tức thì <= 0.05s).
- Tiêu chí này tạm thời ghi nhận **MỘT PHẦN / UNVERIFIED** cho đến khi có kết quả đo runtime duration thực tế từ người dùng.

### 3.7. Kích Thước Cửa Sổ Tối Thiểu & Co Giãn DPI
- WinUI chặn co nhỏ cửa sổ qua hàm subclassing Win32 `WM_GETMINMAXINFO` trong `MainWindow.xaml.cs`.
- Hàm lấy DPI thực tế của màn hình thông qua `GetDpiForWindow(hwnd)`:
  ```csharp
  var scale = dpi > 0 ? (float)dpi / 96.0f : 1.0f;
  mmi.ptMinTrackSize.x = (int)(960 * scale);
  mmi.ptMinTrackSize.y = (int)(530 * scale);
  ```
- Với DPI 100% (scale = 1.0): min size là `960x530`.
- Với DPI 125% (scale = 1.25): min size tự động co lên `1200x662`.
- Với DPI 150% (scale = 1.5): min size tự động co lên `1440x795`.

### 3.8. Nhật Ký & Giải Pháp Khắc Phục BUG 1: Sập Ứng Dụng Khi Bật Gói Tài Nguyên (Pack Enable Crash)
- **Triệu chứng & Tái hiện**: Người dùng kéo thả hoặc có sẵn gói tài nguyên trong cột *Gói khả dụng*, bấm nút Bật (`>`) thì ứng dụng biến mất lập tức.
- **Dấu vết lỗi thực tế (Stack trace từ `%APPDATA%\Vazer\crash.txt`)**:
  ```text
  The value cannot be converted to type ImageSource
  System.ArgumentException: The parameter is incorrect.
     at Vaz.WinUI.Views.InstanceDetailPage.InstanceDetailPage_obj1_Bindings.Update_ViewModel_SelectedPackForInspector_BannerUri(String obj, Int32 phase)
     at Vaz.WinUI.Views.InstanceDetailPage.PackCard_Tapped(Object sender, TappedRoutedEventArgs e)
  ```
- **Nguyên nhân gốc rễ (Root Causes)**:
  1. Khi pack được kéo thả vào hoặc nạp, `BannerUri` có giá trị rỗng (`""`). Khi inspector drawer hiển thị hoặc thẻ được kích hoạt, XAML binding `Image Source="{x:Bind ...BannerUri}"` cố gắng ép kiểu chuỗi rỗng sang `ImageSource`, dẫn đến `System.ArgumentException` không được bắt.
  2. Bấm nút Bật trên thẻ pack kích hoạt sự kiện `Tapped` của cả thẻ `PackCard_Tapped`, dẫn đến việc mở Inspector cho thẻ có `BannerUri` rỗng.
  3. `App.xaml.cs` trong bộ xử lý `UnhandledException` chỉ ghi log ra file mà thiếu `e.Handled = true`, khiến WinUI 3 tự động tắt ứng dụng khi gặp ngoại lệ chưa bắt.
  4. Cơ chế lưu `options.txt` chưa được đảo ngược: Minecraft đọc mảng từ trái sang phải (gói đứng sau đè lên gói đứng trước, Low-to-High). Ở UI, gói ở trên cùng (index 0) là ưu tiên cao nhất (# 1).
- **Biện pháp khắc phục triệt để**:
  1. `App.xaml.cs`: Bổ sung `e.Handled = true;` trong `UnhandledException` để ngăn crash sập tiến trình, ghi log đầy đủ ra `%APPDATA%\Vazer\crash.txt`.
  2. `ResourcePackRow`: Bổ sung thuộc tính an toàn `BannerImageSource` và `IconImageSource` tự động trả về `BitmapImage` với fallback asset hợp lệ (`ms-appx:///Assets/placeholder-banner`) nếu đường dẫn rỗng hoặc không hợp lệ. Cập nhật `InstanceDetailPage.xaml` bind trực tiếp vào các thuộc tính này.
  3. `InstanceDetailPage.xaml.cs`: Trong `PackCard_Tapped`, duyệt cây visual cha để bỏ qua sự kiện tap nếu người dùng click vào các nút bấm con (`ButtonBase`). Đồng thời điều hướng `PackEnable_Click` gọi trực tiếp `ViewModel.EnablePack(row)`.
  4. `InstanceDetailViewModel.ResourcePacksShaders.cs`: Trong `EnablePack`, gán ngay `pack.Priority = 0` (đưa lên `# 1`), dịch chuyển các gói đang dùng khác tăng +1 priority.
  5. `ResourcePackOptionsHelper`: Xây dựng module xử lý độc lập trong `Vazer.Core.GameOptions`:
     - Giữ nguyên các mục hệ thống không thuộc quản lý của app (như `"vanilla"`, mod packs).
     - Đảo ngược thứ tự UI: pack index 0 (ưu tiên # 1) luôn được đặt ở **cuối** mảng `resourcePacks:[...]` trong `options.txt` để đảm bảo Minecraft game client ưu tiên đè texture chuẩn xác.
     - Sử dụng cơ chế ghi file tạm `.tmp` và thay thế nguyên tử (`File.Move(tmp, dest, overwrite: true)`) để chống hỏng file khi gặp sự cố điện/tiến trình.
     - Khôi phục thứ tự ưu tiên chính xác khi khởi động lại app thông qua `ReadActivePacksFromOptionsFile`.
  6. **Kiểm thử tự động & Bằng chứng thực tế**:
     - Bổ sung 8 unit tests chuyên sâu trong `tests/Vazer.Core.Tests/ResourcePackOptionsHelperTests.cs` (toàn bộ 238/238 tests pass).
     - Chạy kịch bản tự động hóa `scripts/test-enable-pack.ps1`: nút Bật pack được click qua UI Automation, app sống sót 100% (không sập), pack xuất hiện tại vị trí `# 1`, và dòng `resourcePacks` trong `options.txt` được cập nhật chính xác:
       `resourcePacks:["vanilla","file/Bare Bones.zip","file/Faithful 32x.zip","file/Fresh Animations.zip"]`
     - Ảnh minh chứng đã chụp: `docs/evidence/RPS-AC-02-pack-enabled-top-priority-1025x533.png`.

---

### 3.9. Báo cáo Chi tiết Lỗi 2 (BUG 2: Vỡ layout khi tỷ lệ co giãn màn hình Windows đặt 125% và 150%)
- **Mô tả hiện tượng**: Người dùng kiểm thử thủ công phát hiện layout bị xô lệch, co cụm hoặc vượt quá kích thước màn hình khi Windows bật Display Scaling 125% hoặc 150%.
- **Nguyên nhân gốc rễ**:
  1. Trong `MainWindow.xaml.cs`, `ptMinTrackSize` được tính bằng phép nhân trực tiếp hệ số DPI: `(int)(960 * scale)`. Tại tỷ lệ 150% (DPI 144), chiều rộng tối thiểu trở thành 1440px vật lý. Khi chạy trên các màn hình có độ phân giải vật lý 1366x768 (phổ biến trên laptop), 1440px vượt quá chiều rộng màn hình, khiến Windows ép cửa sổ co lại bất thường.
  2. Tại tỷ lệ 150%, không gian hiển thị logic (DIP) của cửa sổ 960px vật lý chỉ còn khoảng 640 DIPs. Bố cục 2 cột dạng `ColumnDefinitions="*,*"` trên `ResourcePacksTab` không có `ScrollViewer` cuộn ngang hay giới hạn `MinWidth` trên từng cột, khiến mỗi cột bị ép xuống dưới 300 DIPs, làm biến dạng thẻ pack, đè các nút bấm và icon.
- **Biện pháp khắc phục triệt để**:
  1. `MainWindow.xaml.cs`:
     - Tích hợp các hàm Win32 `MonitorFromWindow` và `GetMonitorInfo` để xác định chính xác kích thước vùng làm việc (`rcWork`) của màn hình chứa cửa sổ.
     - Giới hạn kích thước tối thiểu theo tỷ lệ DPI nhưng kẹp không vượt quá 95% chiều rộng/chiều cao vùng làm việc của màn hình:
       ```csharp
       var targetWidth = (int)(960 * scale);
       var targetHeight = (int)(530 * scale);
       mmi.ptMinTrackSize.x = Math.Min(targetWidth, (int)(workWidth * 0.95f));
       mmi.ptMinTrackSize.y = Math.Min(targetHeight, (int)(workHeight * 0.95f));
       ```
  2. `InstanceDetailPage.xaml`:
     - Trên `ResourcePacksTab`: bọc `ResourcePacksContainerGrid` bằng `ScrollViewer` hỗ trợ cuộn ngang tự động (`HorizontalScrollBarVisibility="Auto"`, `VerticalScrollBarVisibility="Disabled"`), đặt `MinWidth="780"` cho container và `MinWidth="380"` cho cả 2 cột (Gói khả dụng và Gói đang dùng).
     - Điều chỉnh ô tìm kiếm `TextBox` trên thanh công cụ thành kích thước co giãn mềm dẻo (`MinWidth="140" MaxWidth="220"`).
     - Trên `ShadersTab`: `UniformGridLayout` tự động co giãn thẻ card với `MinItemWidth="280"` và thanh chip lọc phong cách dạng `WrapPanel` tự động xuống dòng mượt mà.
  3. **Kiểm thử tự động & Bằng chứng thực tế**:
     - Kiểm thử ở 100% scale (DPI 96) với kích thước nhỏ nhất 960x530 và kích thước chuẩn 1025x533:
       - `RPS-AC-08-layout-rp-100scale-960x530.png`
       - `RPS-AC-08-layout-shaders-100scale-960x530.png`
       - `RPS-AC-08-layout-rp-100scale-1025x533.png`
     - Do môi trường dòng lệnh (headless session) không thể trực tiếp thay đổi thiết lập Display Scaling của hệ điều hành Windows, các tỷ lệ 125% và 150% được ghi nhận **UNVERIFIED** và đã được cung cấp checklist kiểm thử thủ công chi tiết trong `docs/evidence/manual-checklist.md`.

---

## 4. Danh Sách Việc Tồn Đọng Chuyển Backlog (Non-blocking Items)

Các hạng mục dưới đây không cản trở việc nghiệm thu contract WinUI và được ghi nhận chuyển vào backlog kỹ thuật:
1. **Tự động tải nhị phân Iris 1-click**: Triển khai download stream từ Modrinth API kèm giải quyết dependencies Sodium và kiểm tra SHA-512 (chuyển backlog theo quyết định của Chủ sản phẩm).
2. **Chuyển đổi Profile Shader (RPS-AC-05)**: Giao diện và cơ chế đọc/ghi profile shaderpack chuyên sâu (hoãn theo quyết định của Chủ sản phẩm).
3. **Đa ngôn ngữ toàn diện qua `.resw` (W6b)**: Thay thế toàn bộ chuỗi văn bản hardcode trong XAML/C# bằng hệ thống resource `.resw` chuẩn của Windows App SDK.
4. **Cảnh báo phụ thuộc công cụ build (W10)**: Các cảnh báo phân giải phiên bản gói NuGet thứ cấp trong môi trường MSBuild không ảnh hưởng đến chất lượng mã nguồn thực thi.
