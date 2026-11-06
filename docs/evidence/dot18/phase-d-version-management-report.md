# Đợt 18.6 Phase D — Version Management Tab (Quản lý phiên bản đã cài trong Settings)

## 1. Tổng quan

Thêm tab **"Phiên bản"** trong Settings cho phép người dùng xem danh sách, thống kê dung lượng và xóa các phiên bản Minecraft đã cài đặt nhằm giải phóng dung lượng đĩa:

- **Core service**: `VersionManagementService : IVersionManagementService` (tách riêng vào Core, không inline trong ViewModel):
  - `ListInstalledVersionsAsync`: quét thư mục `%APPDATA%/Vazer/Minecraft/instances/versions/`, đọc JSON metadata, tính tổng kích thước files đệ quy, kiểm tra tham chiếu từ các instance (`_instanceManager.ListAsync()`).
  - `DeleteVersionAsync`: chặn xóa nếu phiên bản đang được dùng bởi ít nhất 1 instance (`DeleteResult.Blocked`); nếu orphan thì xóa toàn bộ thư mục và trả về `FreedBytes`.
  - `GetDiskUsageAsync`: thống kê tổng dung lượng `versions/`, `libraries/`, `assets/`.
  - `CleanupOrphanVersionsAsync`: tìm và xóa hàng loạt tất cả các phiên bản orphan kèm dọn dẹp các file `.tmp` / `.partial`.
- **UI (WinUI 3)**:
  - UserControl `src/Vaz.WinUI/Views/Settings/VersionsTab.xaml(.cs)` tích hợp vào `SettingsPage.xaml` qua category chip `"versions"` ("Phiên bản", icon `&#xE8F1;`).
  - Header hiển thị tổng dung lượng: `"Tổng dung lượng: X GB (versions: Y MB, libraries: Z MB, assets: W MB)"`.
  - Bảng danh sách phiên bản: Version ID | Loại (Vanilla / Fabric / Forge / Quilt) | Dung lượng | Ngày cài đặt | Tình trạng sử dụng | Thao tác.
  - Nút **"Xóa"**: tự động **disabled** kèm ToolTip giải thích khi phiên bản đang được sử dụng; khi click vào phiên bản orphan thì hiển thị `ContentDialog` xác nhận: *"Xóa version '{id}'? Thao tác này sẽ giải phóng {size}."*.
  - Nút **"Dọn dẹp"**: quét orphan, hiển thị `ContentDialog` xác nhận: *"Xóa X version không dùng? Giải phóng Y MB."*, sau đó dọn dẹp hàng loạt và cập nhật dung lượng.
  - Nút **"Làm mới"**: quét lại đĩa và nạp lại thống kê.
- **100% ThemeResource**: tương thích hoàn hảo Light Theme & Dark Theme (`CardBackgroundFillColorDefaultBrush`, `CardStrokeColorDefaultBrush`, `SubtleFillColorSecondaryBrush`, `VazAccentBrush`, v.v.).

## 2. Commits (đúng 1 commit / Việc)

| Việc | Commit | Nội dung |
|:---:|---|---|
| 1 | `c049e0f` | `feat(versions): add VersionManagementService with scan logic` |
| 2 | `d2cb16f` | `test(versions): add unit tests for VersionManagementService` |
| 3 | `f76dbcb` | `feat(settings): add Versions tab with list and delete` |
| 4 | `7c886a9` | `feat(versions): add cleanup and disk usage stats` |
| 5 | `22f59ce` | `fix(settings): add missing card styles to VersionsTab resources` |
| — | *(hiện tại)* | `docs(evidence): add version management (Phase D) report and evidence` |

## 3. Danh sách file (Tạo / Sửa)

**Tạo mới:**
- `src/Vazer.Core/Versions/IVersionManagementService.cs` — Interface quản lý phiên bản (list, delete, stats, cleanup).
- `src/Vazer.Core/Versions/InstalledVersionInfo.cs` — Models: `InstalledVersionInfo`, `DeleteResult`, `DiskUsageSummary`, `CleanupResult`.
- `src/Vazer.Core/Versions/VersionManagementService.cs` — Triển khai logic quét thư mục, tính dung lượng, kiểm tra tham chiếu instance, xóa và dọn dẹp.
- `src/Vaz.WinUI/Views/Settings/VersionsTab.xaml(.cs)` — UserControl giao diện tab Versions (bảng danh sách, action bar, dialogs).
- `tests/Vazer.Core.Tests/VersionManagementServiceTests.cs` — 7 unit tests (empty list, 3 versions, used by instances, delete blocked, delete orphan, cleanup orphan, disk usage stats).
- `scripts/record-phase-d-evidence.ps1`, `scripts/record-phase-d-video.ps1` — Automation scripts chụp ảnh & quay video.
- Evidence: 6 ảnh chụp màn hình + 1 video runtime 30s.

**Sửa đổi:**
- `src/Vazer.Core/Extensions/MinecraftServiceCollectionExtensions.cs` — Đăng ký DI singleton `IVersionManagementService, VersionManagementService`.
- `src/Vaz.WinUI/ViewModels/SettingsViewModel.cs` — Inject `IVersionManagementService`, bổ sung category `versions`, commands & properties quản lý phiên bản.
- `src/Vaz.WinUI/Views/SettingsPage.xaml` — Nhúng `VersionsTab` với binding visibility theo `SelectedCategoryKey == "versions"`.

**Không sửa** (đúng phạm vi): `InstanceManager`, `LoaderInstallers`, `InstanceMigrationService`.

## 4. Test & kiểm tra

| Mốc | Kết quả |
|---|---|
| Baseline trước đợt (Phase C/18.5b) | **413/413** pass (`dotnet test Vaz.sln`) |
| Sau đợt 18.6 Phase D | **420/420** pass, 0 skip (`dotnet test Vaz.sln`) — tăng 7 test |
| `npm run check:static` / `check.ps1 -Scope static` | **PASSED** (build + test + agent config) |
| Warnings mới introduced | 0 |

**7 Unit Test Cases (`VersionManagementServiceTests.cs`):**
1. `ListInstalledVersionsAsync_EmptyFolder_ReturnsEmptyList`: **PASS** — Thư mục trống trả về list rỗng.
2. `ListInstalledVersionsAsync_ThreeVersions_ReturnsThreeCorrectItems`: **PASS** — Nhận diện đúng 3 version (Vanilla, Fabric, Forge), tính đúng size > 0.
3. `ListInstalledVersionsAsync_UsedByInstances_MapsInstancesCorrectly`: **PASS** — 1 version được dùng bởi 2 instance → `UsedByInstances.Count == 2`, `IsInUse == true`.
4. `DeleteVersionAsync_VersionInUse_ReturnsBlockedAndDoesNotDeleteFolder`: **PASS** — Phiên bản đang được instance dùng → trả về `DeleteResult.Blocked`, thư mục KHÔNG bị xóa.
5. `DeleteVersionAsync_OrphanVersion_DeletesFolderAndReturnsSuccess`: **PASS** — Phiên bản orphan → xóa thư mục thành công, trả về `DeleteResult.Success` và giải phóng dung lượng.
6. `CleanupOrphanVersionsAsync_CleansOnlyUnusedVersions`: **PASS** — Chỉ xóa các version orphan, bảo toàn nguyên vẹn version đang được sử dụng.
7. `GetDiskUsageAsync_CalculatesTotalAndSubdirectoriesAccurately`: **PASS** — Tính chính xác dung lượng từng phần (`versions/`, `libraries/`, `assets/`) và tổng dung lượng.

## 5. Bằng chứng (Evidence)

**Video 30s:**
- `docs/evidence/dot18/videos/version-management-delete-orphan.mp4` — **32.3s** (1100x670, libx264): Mở app → Settings → click tab "Phiên bản" → danh sách phiên bản hiển thị kèm dung lượng → click nút "Xóa" trên version orphan `1.18.2-orphan-demo` → ContentDialog xác nhận *"Xóa version '1.18.2-orphan-demo'? Thao tác này sẽ giải phóng 10.0 MB."* → bấm "Xóa" → version biến mất khỏi danh sách, InfoBar thông báo thành công, tổng dung lượng giảm → bấm "Làm mới".

**Screenshots (6 ảnh trong `docs/evidence/dot18/screenshots/`):**
1. `settings-versions-dark.png` — Tab Versions giao diện Dark Theme (đầy đủ header tổng dung lượng, nút Dọn dẹp, Làm mới, bảng danh sách).
2. `settings-versions-light.png` — Tab Versions giao diện Light Theme (100% ThemeResource, độ tương phản chuẩn).
3. `settings-versions-delete-confirm.png` — ContentDialog xác nhận xóa phiên bản orphan: *"Xóa version '1.19.4-orphan-demo'? Thao tác này sẽ giải phóng 15.0 MB."*.
4. `settings-versions-in-use.png` — Cột "Tình trạng sử dụng" hiển thị *"Đang dùng bởi 2 instance (Forge 1.20.1 #2, Forge 1.20.1 #1)"*, nút "Xóa" bị **disabled** an toàn.
5. `settings-versions-cleanup-confirm.png` — ContentDialog dọn dẹp hàng loạt: *"Xóa 3 version không dùng? Giải phóng 69.6 MB."* với nút "Dọn dẹp ngay" và "Hủy".
6. `settings-versions-refresh.png` — Trạng thái sau khi xóa: InfoBar thông báo *"Đã xóa version '1.19.4-orphan-demo', giải phóng 15.0 MB."*, dung lượng cập nhật và danh sách thu gọn.

## 6. Known issues / Hạn chế

1. **Khóa thư mục khi game đang chạy**: Nếu một instance đang chạy và người dùng cố xóa một version orphan khác không liên quan, xóa vẫn bình thường; nhưng nếu OS khóa file trong thư mục do tiến trình khác, service sẽ bắt ngoại lệ và trả về `DeleteResult.Failed` với thông báo lỗi rõ ràng.
2. **Assets/Libraries dùng chung**: Các thư mục `libraries/` và `assets/` chứa hash/artifact dùng chung giữa nhiều phiên bản (Mojang deduplication), do đó service chỉ xóa thư mục của chính `versions/{versionId}` chứ không xóa thư viện dùng chung (tránh làm hỏng các bản cài khác).
3. **Cột bảng trên màn hình cực hẹp (<600px)**: Bảng sử dụng ListView Grid với thanh cuộn ngang/dọc, trên màn hình nhỏ có thể cần cuộn ngang để xem hết cột "Tình trạng sử dụng".

## 7. Đề xuất (sau đợt này)

1. **Dọn dẹp thư viện mồ côi (Orphan Libraries Cleanup)**: Trong tương lai có thể bổ sung thuật toán quét hash các jar trong `libraries/` không còn được JSON nào trong `versions/` tham chiếu để giải phóng thêm dung lượng.
2. **Lọc và tìm kiếm phiên bản**: Bổ sung ô tìm kiếm hoặc bộ lọc loại (Chỉ hiện Vanilla / Fabric / Forge hoặc chỉ hiện Orphan) khi danh sách phiên bản quá dài.

## 8. Self-review & CHƯA KIỂM TRA

- Không tự nhận PASS; toàn bộ bằng chứng video và ảnh chụp đều được thu thập từ runtime thực tế trên Windows qua script tự động.
- **ĐÃ KIỂM TRA**:
  - Quét thư mục `versions/` thật và tính đúng dung lượng trên đĩa (~1.11 GB).
  - Khóa nút xóa cho version đang được dùng bởi instance (`1.20.1`, `1.20.1-forge-47.4.28`).
  - Mở ContentDialog xóa 1 bản và xóa thành công, giải phóng đúng dung lượng.
  - Mở ContentDialog dọn dẹp hàng loạt và tính đúng số lượng orphan.
  - Chuyển đổi Dark/Light theme mượt mà không lỗi màu.
- **CHƯA KIỂM TRA**:
  - Xóa phiên bản khi file jar bên trong đang bị một tiến trình Java bên ngoài giữ lock (service có try/catch trả về Failed, chưa ép file lock tại runtime).
