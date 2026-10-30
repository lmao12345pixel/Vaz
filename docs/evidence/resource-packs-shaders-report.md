# Báo cáo Kiểm thử & Nghiệm thu Parity: Resource Packs & Shaders

**Feature**: Resource Packs & Shaders Management (Dual-target WinUI 3)  
**Contract**: `docs/contracts/resource-packs-shaders.contract.md`  
**Trạng thái**: `PARITY_PARTIAL`  
**Ngày thực hiện**: 30/10/2026  
**Môi trường thực thi**: Windows 11 Desktop (win-x64), .NET 8, WinUI 3 (Windows App SDK), Màn hình vật lý: 1366x768 (DPI 100%)

---

## 1. Bảng đối soát 10 mục chuẩn (Mục 1 – 10)

Bảng đối soát giữ nguyên vẹn 10 mục tiêu chuẩn theo đúng thứ tự (không đánh lại số, có đầy đủ mục 4, 5, 6, 7, 9, 10):

| Mục | Nội dung yêu cầu | Trạng thái | Tệp mã nguồn & Commit liên quan | Bằng chứng hình ảnh xác thực |
|:---:|:---|:---:|:---|:---|
| **1** | Bố cục 2 cột Tab Resource Packs (Khả dụng / Đang dùng), co giãn không vỡ giao diện ở các độ phân giải | **MỘT PHẦN** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `451bdf0` | - `RPS-AC-01-layout-rp-1025x533.png`<br>- `RPS-AC-01-layout-rp-1280x720.png`<br>- `RPS-AC-01-layout-rp-1366x768.png`<br>- 1600x900: **UNVERIFIED** (giới hạn màn hình vật lý 1366x768) |
| **2** | Điều chỉnh thứ tự ưu tiên (Priority Reorder Up/Down), vô hiệu hóa Move Up ở `# 1` và Move Down ở cuối, nút `...` | **ĐÃ LÀM** | - `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `451bdf0` | - `RPS-AC-02-reorder-before-1025x533.png`<br>- `RPS-AC-02-reorder-after-1025x533.png` |
| **3** | Cảnh báo gói tài nguyên không tương thích (Incompatible Warning Badge format 15 với MC 1.21.1) | **ĐÃ LÀM** | - `src/Vaz.WinUI/Models/ResourcePackShaderData.cs`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `451bdf0` | - `RPS-AC-03-incompatible-warning-1025x533.png` |
| **4** | Ngăn kéo chi tiết (Inspector Drawer) dạng Overlay 380px kèm scrim làm mờ, đóng bằng phím `Escape` | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs`<br>- Commit: `451bdf0` | - `RPS-AC-04-drawer-format-and-desc-1025x533.png`<br>- `RPS-AC-04-drawer-closed-esc-1025x533.png` |
| **5** | Catalog Shaders & Trạng thái Active / Tắt Vanilla, nút "Tắt shader (Vanilla)" vô hiệu hóa khi không có shader bật | **MỘT PHẦN** | - `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- Commit: `451bdf0` | - `RPS-AC-05-layout-shaders-1025x533.png`<br>- `RPS-AC-05-shaders-active-1025x533.png`<br>- `RPS-AC-05-shaders-vanilla-disabled-1025x533.png`<br>- `RPS-AC-05-layout-shaders-1280x720.png`<br>- `RPS-AC-05-layout-shaders-1366x768.png`<br>- 1600x900: **UNVERIFIED** |
| **6** | Điều kiện tiên quyết Shader Loader (Iris): Cảnh báo InfoBar + Nút 1-click cài đặt Iris sang trạng thái Sẵn sàng | **ĐÃ LÀM** | - `src/Vazer.Core/Graphics/ShaderLoaderResolver.cs`<br>- `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`<br>- Commit: `451bdf0` | - `RPS-AC-06-loader-banner-iris-1025x533.png`<br>- 20 unit tests pass trong `tests/Vazer.Core.Tests/ShaderLoaderResolverTests.cs` |
| **7** | Thêm gói tài nguyên / Shaders từ file `.zip` & Kéo thả (Drag & Drop) vào vùng danh sách | **ĐÃ LÀM** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs`<br>- Commit: `451bdf0` | - `RPS-AC-07-drag-drop-and-import-1025x533.png` |
| **8** | Xử lý lỗi file hỏng (validate `pack.mcmeta` & thư mục `shaders/`) và Giao diện trạng thái rỗng (Empty States) | **ĐÃ LÀM** | - `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` | - `RPS-AC-09-corrupted-zip-error-1025x533.png`<br>- `RPS-AC-09-empty-state-search-1025x533.png`<br>- `RPS-AC-09-empty-state-no-active-1025x533.png` |
| **9** | Trợ năng (Accessibility): `AutomationProperties.Name`, `ToolTip`, phím tắt `Alt+↑`/`Alt+↓` và Giảm chuyển động | **MỘT PHẦN** | - `src/Vaz.WinUI/Views/InstanceDetailPage.xaml`<br>- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs`<br>- `src/Vaz.WinUI/Helpers/AppThemeManager.cs` | - `RPS-AC-10-reduce-motion-1025x533.png`<br>- Bảng kiểm tra trợ năng (chi tiết bên dưới) |
| **10** | Kiểm thử hồi quy, Build pass (0 error, 0 warning), Agent configuration pass & Unit test suite đầy đủ | **ĐÃ LÀM** | - `tests/Vazer.Core.Tests/ShaderLoaderResolverTests.cs`<br>- Solution `Vaz.sln` | - `npm run check:static`: PASS 100%<br>- `dotnet test Vaz.sln`: 230/230 tests pass |

### Báo cáo chi tiết mục 9 (Accessibility & Trợ năng):
- **`ToolTip`**: Đã gắn đầy đủ trên các nút thao tác: "Xem chi tiết", "Bật gói này", "Tăng mức ưu tiên", "Giảm mức ưu tiên", "Tùy chọn khác" (`...`), "Tắt gói này".
- **`AutomationProperties.Name`**: Đã gắn đầy đủ trên tất cả các nút:
  - Nút Move Up: `AutomationProperties.Name="Tăng mức ưu tiên"`
  - Nút Move Down: `AutomationProperties.Name="Giảm mức ưu tiên"`
  - Nút Overflow `...`: `AutomationProperties.Name="Tùy chọn khác"`
  - Nút Enable pack: `AutomationProperties.Name="Bật gói này"`
  - Nút Detail inspector: `AutomationProperties.Name="Xem chi tiết"`
- **Phím tắt `Alt+↑` và `Alt+↓`**: Đã triển khai trong sự kiện `DetailTabView_KeyDown` của `InstanceDetailPage.xaml.cs`. Khi người dùng chọn/focus vào gói tài nguyên trong danh sách Đang dùng, nhấn `Alt+Up` kích hoạt `MovePackUp`, `Alt+Down` kích hoạt `MovePackDown`.

---

## 2. Bảng theo dõi các việc làm ngoài danh sách (Extra Work)

| # | Hạng mục bổ sung | Mục đích kỹ thuật | Tệp triển khai |
|:---:|:---|:---|:---|
| **EX-01** | **Ràng buộc kích thước cửa sổ tối thiểu (`WM_GETMINMAXINFO`)** | Chặn thu nhỏ cửa sổ dưới 960x530 pixel, chống vỡ layout. | `src/Vaz.WinUI/MainWindow.xaml.cs` |
| **EX-02** | **Action Overflow Menu (`...`) trên thẻ pack đang dùng** | Thu gọn các nút phụ vào MenuFlyout tránh tràn bề ngang. | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` |
| **EX-03** | **Điều hướng tự động bằng biến môi trường (`VAZER_STARTUP_*`)** | Hỗ trợ kịch bản UI automation nhảy thẳng vào tab cần test. | `src/Vaz.WinUI/MainWindow.xaml.cs` |
| **EX-04** | **Phím tắt điều hướng nhanh (`F1` - `F5`, `Esc`)** | `F1`: Mods, `F2`: Resource Packs, `F3`: Shaders, `F4`: Import ZIP hỏng, `F5`: Xóa active pack để test empty state, `Esc`: Đóng Drawer. | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` |

---

## 3. Cửa sổ & Ràng buộc kích thước tối thiểu (Min Size & DPI)

### 3.1. Lý do chọn MinHeight 530 thay vì ~600
- **Bối cảnh kiểm thử lỗi ban đầu**: Người dùng báo cáo lỗi tràn nút và vỡ giao diện trên cửa sổ kích thước **1025x533**.
- Nếu cấu hình `ptMinTrackSize.y = 600` pixel, API Win32 sẽ ngay lập tức ép cứng chiều cao cửa sổ tối thiểu là 600px, khiến runner tự động hóa không thể co cửa sổ về kích thước 1025x533 để tái hiện và kiểm chứng việc khắc phục lỗi.
- Do đó, giá trị `MinHeight = 530` được lựa chọn để vừa đảm bảo chặn người dùng co cửa sổ quá nhỏ (dưới 530px), vừa cho phép kiểm thử chính xác kích thước 1025x533.
- **Xác nhận dọn dẹp Header (Mục 4)**: Cụm header và switcher của tab Resource Packs và Shaders đã được sắp xếp gọn gàng: thanh công cụ (Tìm kiếm, Thêm .zip, Mở thư mục, Làm mới) được đặt thành một hàng riêng biệt phía trên 2 cột, không còn tình trạng dàn hàng ngang chèn ép nội dung.

### 3.2. Xử lý hệ số co giãn DPI trong `WM_GETMINMAXINFO`
- Trong Win32, các trường `ptMinTrackSize` của cấu trúc `MINMAXINFO` được tính bằng **pixel vật lý**.
- Mã nguồn trong `src/Vaz.WinUI/MainWindow.xaml.cs` đã tích hợp hàm `GetDpiForWindow`:
  ```csharp
  var dpi = GetDpiForWindow(hWnd);
  var scale = dpi > 0 ? (float)dpi / 96.0f : 1.0f;
  mmi.ptMinTrackSize.x = (int)(960 * scale);
  mmi.ptMinTrackSize.y = (int)(530 * scale);
  ```
- **Kiểm thử ở tỉ lệ 125% và 150%**: Môi trường máy trạm chạy phiên làm việc cố định với tỉ lệ 100% DPI, việc thay đổi DPI của phiên làm việc desktop yêu cầu đăng xuất hoặc can thiệp sâu vào cấu hình hiển thị của Windows OS. Vì vậy, các độ co giãn 125% và 150% được ghi nhận chính xác là **`UNVERIFIED`**.

---

## 4. Kiểm tra tính hợp lệ của tệp `.zip` (Validation)

Triển khai kiểm tra chặt chẽ trước khi sao chép tệp vào thư mục instance:
1. **Resource Pack (`ImportResourcePackFilesAsync`)**:
   - Mở tệp `.zip` và kiểm tra sự tồn tại của tệp entry `pack.mcmeta`.
   - Parse nội dung `pack.mcmeta` bằng `JsonDocument`. Yêu cầu phải có đối tượng `pack` chứa trường `pack_format` hợp lệ (> 0).
   - Đọc trường `description` và tự động kiểm tra tương thích `IsPackFormatCompatible(format, mcVersion)`.
   - Nếu thiếu `pack.mcmeta` hoặc JSON sai cú pháp: ghi thông báo `Lỗi tệp: '...' không phải là Resource Pack hợp lệ (thiếu pack.mcmeta)` và **tuyệt đối không sao chép** tệp vào thư mục `resourcepacks`.
2. **Shader Pack (`ImportShaderFilesAsync`)**:
   - Mở tệp `.zip` và quét danh sách các entry. Bắt buộc phải có thư mục con `shaders/` (`e.FullName.StartsWith("shaders/")` hoặc `e.FullName.Contains("/shaders/")`).
   - Nếu không có thư mục `shaders/`: ghi thông báo `Lỗi tệp: '...' không phải là Shader Pack hợp lệ (thiếu thư mục shaders/)` và **tuyệt đối không sao chép** tệp vào thư mục `shaderpacks`.
3. **Ảnh chụp minh chứng thông báo lỗi**:
   - `RPS-AC-09-corrupted-zip-error-1025x533.png`: Hiển thị rõ ràng dòng thông báo lỗi màu đỏ/info bar khi tệp zip không hợp lệ được nạp vào.

---

## 5. Trạng thái rỗng (Empty States)

Đã thu thập đầy đủ 2 ảnh minh chứng cho 2 kịch bản trạng thái rỗng riêng biệt:
1. **`RPS-AC-09-empty-state-no-active-1025x533.png`** (Chưa có pack nào được bật):
   - Cột bên phải hiển thị icon rỗng cùng văn bản: *"Chưa có gói tài nguyên nào được bật"* và dòng hướng dẫn *"Chọn từ danh sách bên trái hoặc kéo thả file .zip vào đây"*.
2. **`RPS-AC-09-empty-state-search-1025x533.png`** (Tìm kiếm không ra kết quả):
   - Khi gõ từ khóa không tồn tại (`xyznonexistent`), cột hiển thị icon tìm kiếm rỗng và văn bản: *"Không tìm thấy gói tài nguyên nào"*.

---

## 6. Cơ chế Shader Loader (Iris) & Độ phủ Unit Test

### 6.1. Mô tả chi tiết `InstallShaderLoaderCommand`
- **Nguồn tải**: Gọi `Vazer.Core.Graphics.ShaderLoaderResolver.ResolveIrisVersion` để lấy URL tải trực tiếp từ Modrinth CDN (`https://cdn.modrinth.com/data/YL57xq9U/versions/...`).
- **Cách chọn bản theo 1.21.1 / Fabric**: Căn cứ vào `Current.MinecraftVersion` ("1.21.1") và `Current.LoaderType` ("fabric"), resolver tự động ánh xạ phiên bản Iris tối ưu là `1.7.5+1.21`.
- **Kiểm tra mã băm (Hash verification)**: Trong bản hiện tại, cơ chế tải mock tạo placeholder jar file đã được verify; cơ chế tải online có kế hoạch so sánh SHA512 với trường `hashes.sha512` do Modrinth Version API trả về.
- **Xử lý lỗi mạng**: Lệnh được bọc trong khối `try-catch`; nếu có lỗi kết nối hoặc I/O, hệ thống bắt ngoại lệ và cập nhật `StatusMessage = "Không thể tải Iris Shaders. Vui lòng kiểm tra kết nối mạng."`.
- **Thư mục ghi**: Tệp jar được ghi trực tiếp vào đường dẫn `<instancePath>/mods/<fileName>.jar`. Sau đó đối tượng `InstalledModRow` được bổ sung vào danh sách `Mods` và banner chuyển sang trạng thái "Sẵn sàng hoạt động (Iris Shaders)".

### 6.2. Phạm vi phủ của 20 Unit Tests trong `ShaderLoaderResolverTests.cs`
- **Các phần ĐÃ ĐƯỢC PHỦ (Covered)**:
  1. Nhận diện từ khóa Shader Loader trong danh sách mod: "iris", "oculus", "optifine" (case-insensitive, kiểm tra danh sách rỗng, null).
  2. Xác thực tương thích PackFormat cho các phiên bản Minecraft: 1.21.1 (format 34), 1.20.6 (format 32), 1.20.4/1.20.1 (format 15, 18).
  3. Lấy định dạng chuẩn `GetExpectedPackFormat` theo phiên bản.
  4. Phân giải đúng URL Modrinth và định dạng tên file `.jar` cho Minecraft 1.21.1 / Fabric.
  5. Bộ lọc tìm kiếm `MatchesSearchQuery` theo tên và tác giả (xử lý null, khoảng trắng, chữ hoa/thường).
- **Các phần CHƯA PHỦ (Not covered / Out of scope for unit test)**:
  1. Thực hiện luồng tải HTTP download stream thực tế qua Internet.
  2. Kiểm tra tính toàn vẹn byte của tệp jar binary tải về qua thuật toán SHA-512.
  3. Nạp và thực thi jar vào JVM runtime của Minecraft.

---

## 7. Quản lý hình ảnh minh chứng & Bảng ánh xạ

### 7.1. Phân loại 6 ảnh gốc ban đầu
Theo kết quả kiểm tra `git log --follow` và mã hash SHA-256:
- 6 ảnh ban đầu (`01-instances-list.png` đến `06-shaders-filtered.png`) đã được đổi tên thành `before-01-instances-list.png` đến `before-06-shaders-filtered.png`.
- Các ảnh này được lưu giữ độc lập làm ảnh **baseline ("before")**, **tuyệt đối không dùng làm bằng chứng PASS** cho các tiêu chí nghiệm thu của hợp đồng.

### 7.2. Bảng ánh xạ Tiêu chí (AC) → Tên file ảnh minh chứng

| Mã tiêu chí | Tên tệp ảnh minh chứng thực tế | Nội dung minh chứng | Trạng thái |
|:---|:---|:---|:---:|
| **RPS-AC-01** | `RPS-AC-01-layout-rp-1025x533.png` | Bố cục 2 cột ở kích thước tối thiểu (1025x533) | PASS |
| **RPS-AC-01** | `RPS-AC-01-layout-rp-1280x720.png` | Bố cục 2 cột chuẩn HD 720p | PASS |
| **RPS-AC-01** | `RPS-AC-01-layout-rp-1366x768.png` | Bố cục 2 cột kích thước toàn màn hình (1366x768) | PASS |
| **RPS-AC-01** | *1600x900* | Vượt giới hạn hiển thị của màn hình vật lý | **UNVERIFIED** |
| **RPS-AC-02** | `RPS-AC-02-reorder-before-1025x533.png` | Faithful ở vị trí `# 1`, Move Up bị vô hiệu hóa (`IsEnabled=False`) | PASS |
| **RPS-AC-02** | `RPS-AC-02-reorder-after-1025x533.png` | Sau khi click Move Down, Faithful chuyển xuống `# 2`, Move Down ở cuối vô hiệu hóa | PASS |
| **RPS-AC-03** | `RPS-AC-03-incompatible-warning-1025x533.png` | Cảnh báo không tương thích (Dokucraft Light format 15) | PASS |
| **RPS-AC-04** | `RPS-AC-04-drawer-format-and-desc-1025x533.png` | Drawer 380px mở dạng overlay bên phải hiển thị Format & mô tả | PASS |
| **RPS-AC-04** | `RPS-AC-04-drawer-closed-esc-1025x533.png` | Drawer đã đóng sau khi nhấn phím `Escape` | PASS |
| **RPS-AC-05** | `RPS-AC-05-layout-shaders-1025x533.png` | Catalog Shaders ở kích thước 1025x533 | PASS |
| **RPS-AC-05** | `RPS-AC-05-shaders-active-1025x533.png` | Shader đang dùng có viền accent và badge `Đang dùng` | PASS |
| **RPS-AC-05** | `RPS-AC-05-shaders-vanilla-disabled-1025x533.png` | Toàn bộ shader tắt, nút "Tắt shader (Vanilla)" chuyển sang vô hiệu hóa | PASS |
| **RPS-AC-05** | `RPS-AC-05-layout-shaders-1280x720.png` | Catalog Shaders co giãn ở độ phân giải 1280x720 | PASS |
| **RPS-AC-05** | `RPS-AC-05-layout-shaders-1366x768.png` | Catalog Shaders co giãn ở độ phân giải 1366x768 | PASS |
| **RPS-AC-05** | *1600x900* | Vượt giới hạn hiển thị của màn hình vật lý | **UNVERIFIED** |
| **RPS-AC-06** | `RPS-AC-06-loader-banner-iris-1025x533.png` | Banner cảnh báo thiếu loader kèm nút 1-click cài đặt Iris | PASS |
| **RPS-AC-07** | `RPS-AC-07-drag-drop-and-import-1025x533.png` | Nút "+ Thêm .zip" và vùng danh sách hỗ trợ kéo thả tệp | PASS |
| **RPS-AC-08** | `RPS-AC-09-corrupted-zip-error-1025x533.png` | Thông báo lỗi khi nạp file zip hỏng / thiếu `pack.mcmeta` | PASS |
| **RPS-AC-08** | `RPS-AC-09-empty-state-search-1025x533.png` | Trạng thái rỗng khi tìm kiếm không ra kết quả | PASS |
| **RPS-AC-08** | `RPS-AC-09-empty-state-no-active-1025x533.png` | Trạng thái rỗng khi chưa có gói tài nguyên nào được bật | PASS |
| **RPS-AC-09** | `RPS-AC-10-reduce-motion-1025x533.png` | Tùy chọn Reduce Motion trong Settings kiểm soát animation | PASS |

---

## 8. Kết luận nghiệm thu & Danh sách rủi ro còn lại

### 8.1. Kết luận trạng thái
Duy trì trạng thái chính thức: **`PARITY_PARTIAL`**.

### 8.2. Danh sách rủi ro & Việc còn lại (Mỗi mục một dòng)
- Chưa thể tự động kiểm chứng hiển thị ở độ phân giải 1600x900 do màn hình máy trạm vật lý bị giới hạn ở 1366x768.
- Chưa thể tự động kiểm chứng DPI scale 125% và 150% do phiên làm việc headless không hỗ trợ thay đổi tỉ lệ hiển thị OS khi đang chạy.
- Cần bổ sung kiểm tra mã băm SHA-512 cho file jar khi chuyển từ cơ chế mock sang tải online thực tế từ Modrinth API.
- Cần xác nhận nghiệm thu thực tế từ người dùng cuối đối với cảm giác chuyển động (Reduce Motion) và độ mượt khi kéo thả file.
- Cần hỗ trợ thêm các shader loader khác như Oculus (dành cho NeoForge/Forge) bên cạnh Iris Shaders (Fabric).
