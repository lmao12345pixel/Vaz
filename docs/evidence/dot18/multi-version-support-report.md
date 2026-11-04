# Đợt 18 — Multi-Version Support — Báo cáo Evidence

## Phase B — UI tạo instance (2026-11-04)

### Trạng thái các hạng mục

| Hạng mục | Trạng thái | Ghi chú |
|----------|-----------|---------|
| Badge "Đã cài" | Done | `MinecraftVersionInfo.IsInstalled` + `MinecraftVersionService.SetInstalledStatus()` (kiểm tra `versions/{id}` dưới RootPath). `CreateInstanceDialog` hiển thị badge xanh cạnh version đã cài qua `BoolToVis` converter. |
| Loader Version dropdown động | Done (Fabric/Quilt) | `LoadLoaderVersionsAsync` gọi `GetLoaderVersionsAsync` khi đổi MC version hoặc Loader; stable trước, beta sau; tự chọn item đầu. `LoaderVersions` bind vào `LoaderVersionBox.ItemsSource` (code-behind reassign; `IReadOnlyList<string>` không raise CollectionChanged). Rỗng → ẩn ComboBox, hiện text "Không có version tương thích". Có ProgressRing khi đang tải. Build XAML không crash sau khi loại bỏ `<x:Array>` (không được WinUI 3 XamlCompiler hỗ trợ trong cấu hình này). |
| Validation warning | Done | `CanCreateInstance` = MC version đã chọn AND (Loader == "Vanilla" OR LoaderVersion đã chọn). Nút "Tạo" (`IsPrimaryButtonEnabled`) bind `CanCreateInstance`; InfoBar `CreateWarningBar` (Severity=Warning, IsOpen = NOT CanCreateInstance, Message = `CreateWarningMessage`) hiện "Vui lòng chọn loader version cho {loader}." / "Chọn Minecraft version.". |
| Commit | Done | Xem commit `feat(versions): phase B - instance creation UI`. |

### Forge / NeoForge — hỗ trợ hay không?

**Forge: KHÔNG hỗ trợ.** Core không có Forge installer; `MrpackImporter` chặn Forge với NotSupportedException.

**NeoForge: hỗ trợ một phần (installer saja), KHÔNG hỗ trợ dropdown version.**
- NeoForge installer qua `NeoForgeInstallerService` (Maven chính thức, headless JAR, cần MC ≥ 1.20.1, tự chọn build mới nhất) — hoạt động nếu `loaderVersion` = null.
- `MinecraftVersionService.GetLoaderVersionsAsync` **chỉ hỗ trợ Fabric và Quilt**; NeoForge (và Forge) trả về danh sách rỗng → UI sẽ hiện "Không có version tương thích" và `CanCreateInstance = false` cho NeoForge cho đến khi thêm nguồn metadata NeoForge.
- Quilt cũng không xuất hiện trong UI (UI Loaders = Fabric / NeoForge / Vanilla), mặc dù service có fetch Quilt.

### Validation có hoạt động không?

Logic đã triển khai và build/bind thành công (`CanCreateInstance`, `CreateWarningMessage`, update khi đổi version/loader/loader-version). **Chưa runtime-verify UI** — chưa mở dialog và quan sát trạng thái disabled/warning. Không được coi là PASS manual test.

### Loader version có bind động chưa?

Có (Fabric; Quilt trong service; NeoForge chưa có nguồn dữ liệu). Binding qua code-behind (`LoaderVersionBox.ItemsSource`) với re-assign trong `PropertyChanged` handler; không tự thay đổi khi danh sách thay đổi vì `LoaderVersions` là `IReadOnlyList<string>`.

### Gates đã chạy

- `dotnet build src/Vaz.WinUI/Vaz.WinUI.csproj -c Debug --no-restore` — Succeeded, 0 errors, 0 warnings.
- `dotnet test` — **349/349 pass, 0 skipped** (349 total).
- `npm run check:static` — **PASSED** (build + test + agent config).
- Ghi chú: trước đó 2 test Phase A (`GetCacheTimestampAsync_Exists`, `ClearCacheAsync_ClearsAllFiles`) fail do giả định sai về vị trí cache; đã sửa **file test** (ghi dummy vào `{RootPath}/cache/` thay vì RootPath). Không thay đổi logic service.

### Còn tồn đọng / việc tiếp theo

- Runtime-verify UI (dialog mở, badge hiện đúng, dropdown loader load, nút "Tạo" disable + warning khi chưa chọn loader version).
- Thêm nguồn metadata NeoForge nếu muốn UI chọn được NeoForge version (hoặc để `loaderVersion = null` = latest opt-in).
