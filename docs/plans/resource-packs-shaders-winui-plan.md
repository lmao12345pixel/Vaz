# Kế hoạch triển khai WinUI: Resource Packs & Shaders Management

**Feature:** `resource-packs-shaders`
**Contract:** `docs/contracts/resource-packs-shaders.contract.md` (status: `WEB_VALIDATED`)
**Mục tiêu:** Đạt `PARITY_VERIFIED` trên WinUI app với đầy đủ runtime desktop evidence cho RPS-AC-01 -> RPS-AC-10.

---

## 1. Phân tích tiêu chí nghiệm thu (Acceptance Criteria)

| # | Tiêu chí | Evidence Type | Triển khai trên WinUI |
|---|----------|---------------|------------------------|
| RPS-AC-01 | Tab Resource Packs hiển thị danh sách packs với tên, icon, độ phân giải (16x/32x/64x), dung lượng và toggle bật/tắt | RUNTIME | TabViewItem "Resource Packs" trong `InstanceDetailPage.xaml`, `ListView` hiển thị các packs với icon, tên, resolution badge, size, ToggleSwitch. |
| RPS-AC-02 | Đổi thứ tự ưu tiên (Priority Reorder) bằng nút Lên/Xuống (Move Up/Down); pack phía trên có độ ưu tiên cao hơn | RUNTIME | Nút Move Up và Move Down trên từng pack card; đổi vị trí trong `ObservableCollection<ResourcePackRow>` và lưu `resourcepacks.json`. |
| RPS-AC-03 | Cảnh báo tương thích (Incompatible Warning badge) xuất hiện nếu format không khớp MC version | RUNTIME | Phân tích `pack_format` hoặc file name so với phiên bản instance; hiển thị badge `⚠ Không tương thích` (amber). |
| RPS-AC-04 | Tab Shaders hiển thị danh sách shaderpack, cho phép chọn active shaderpack hoặc "Tắt shader", cập nhật ảnh preview shader | RUNTIME | TabViewItem "Shaders" với Grid 2 cột: danh sách Shader cards (có card "Tắt shader") và khung Preview ảnh lớn độ phân giải cao + badge trạng thái. |
| RPS-AC-05 | Chuyển đổi Profile Shader (Low, Medium, High, Ultra) cập nhật cấu hình tức thì | RUNTIME | Profile chips WrapPanel (Low, Medium, High, Ultra), click đổi profile, cập nhật label và lưu config. |
| RPS-AC-06 | Tự động phát hiện thiếu Shader Loader (Iris / OptiFine): hiển thị cảnh báo + nút 1-click cài đặt Iris | RUNTIME | Kiểm tra xem instance đã có `Iris Shaders` trong danh sách mods chưa. Nếu chưa: hiện InfoBar cảnh báo kèm nút "Cài đặt Iris". Bấm cài sẽ tự động add Iris vào mods và chuyển banner sang "Sẵn sàng hoạt động (Iris)". |
| RPS-AC-07 | Hỗ trợ thêm file pack `.zip` từ bên ngoài qua nút Open file picker và kéo thả (Drag & Drop) | RUNTIME | Nút "+ Thêm pack / shader (.zip)" mở FileOpenPicker và sự kiện `Drop` trên tab sao chép file `.zip` vào thư mục `resourcepacks/` hoặc `shaderpacks/`. |
| RPS-AC-08 | Responsive 1024px và ~800px không tràn ngang; phím tắt / tương tác mượt | RUNTIME | Grid & WrapPanel co giãn linh hoạt, ScrollViewer cuộn dọc mượt mà. |
| RPS-AC-09 | Cảm giác tương tác trực quan, mượt mà theo design tokens | MANUAL_REQUIRED | Thao tác đổi vị trí pack, chọn shader, chuyển profile, user xác nhận trực quan. |
| RPS-AC-10 | Không hồi quy: build/test solution pass 100% | AUTOMATED | `npm run check:static` (0 err, 208 test pass, 12 agent config pass). |

---

## 2. Các bước triển khai cụ thể

- [ ] **Bước 1**: Tạo Model `ResourcePackShaderData.cs` trong `src/Vaz.WinUI/Models/` chứa mock catalog cho Resource Packs (Faithful 32x, Bare Bones, Stay True, Fresh Animations) và Shaders (BSL Shaders, Complementary Reimagined, Photon, Solas, Bliss, MakeUp, AstraLex, Miniature).
- [ ] **Bước 2**: Mở rộng `InstanceDetailViewModel.cs`:
  - Thêm `ObservableCollection<ResourcePackRow> ResourcePacks`.
  - Thêm `ObservableCollection<ShaderPackRow> ShaderPacks`.
  - Quản lý `ActiveShader`, `ShaderProfile` (Low, Medium, High, Ultra), `HasShaderLoader`, `ShaderWarningMessage`.
  - Thao tác: `MovePackUpAsync`, `MovePackDownAsync`, `TogglePackEnabledAsync`, `SelectShaderAsync`, `SelectProfileAsync`, `InstallIrisLoaderAsync`, `ImportResourcePackAsync`, `ImportShaderPackAsync`.
- [ ] **Bước 3**: Cập nhật `InstanceDetailPage.xaml` và `InstanceDetailPage.xaml.cs`:
  - Thêm TabViewItem `Resource Packs` (glyph `&#xE790;` hoặc `&#xF133;`).
  - Thêm TabViewItem `Shaders` (glyph `&#xE706;`).
  - Thiết kế UI chuẩn Fluent design tokens.
  - Hỗ trợ Drag & Drop file `.zip`.
- [ ] **Bước 4**: Kiểm tra tự động (`npm run check:static`, `dotnet test`).
- [ ] **Bước 5**: Chạy WinUI runtime desktop smoke test qua WinArm, kiểm chứng RPS-AC-01 đến RPS-AC-10, cập nhật parity docs và task ledger sang `PARITY_VERIFIED`.
