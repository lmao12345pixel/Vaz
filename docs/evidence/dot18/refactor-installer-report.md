# Dọt 18.2g — Refactor hệ thống tạo instance & cài loader (Strategy + Facade)

## 1. Tổng quan

Refactor toàn bộ flow "tạo instance" thành kiến trúc Strategy (`ILoaderInstaller`) +
Facade (`IInstanceCreationService`) + Progress model dùng chung (`InstallerProgress`).
UI/UX giữ nguyên hành vi; chỉ thay đổi bên trong.

Commits: `c31f018` → `46e7a1f` → `b125f1d` → `586aaab` → `332464f` → (cuối cùng)

## 2. Trạng thái từng việc

| Việc | Trạng thái | Ghi chú |
|---|---|---|
| 1. Interface + model | DONE (`c31f018`) | `ILoaderInstaller`, `InstallerStage`/`InstallerProgress`, `InstanceInfo`, `IInstanceCreationService` |
| 2. Các installer | DONE (`46e7a1f`) | Vanilla/Fabric/Quilt/NeoForge/Forge(placeholder). NeoForgeInstaller implements cả `INeoForgeInstaller` (compat MrpackImporter) |
| 3. Facade | DONE (`b125f1d`) | `InstanceCreationService` — chọn installer, resolve loader version qua `IMinecraftVersionService`, ghi instance.json |
| 4. ViewModel | DONE (`586aaab`) | Inject `IInstanceCreationService`; expose `IsInstalling`, `InstallerProgressPercent`, `InstallMessage`, `InstallStage` |
| 5. Dialog XAML | DONE (`332464f`) | ProgressBar (IsIndeterminate khi Percent=-1) + TextBlock message; nút Tạo disable khi đang cài; Hủy bị chặn bằng CloseButtonClick cancel |
| 6. Cleanup + test | DONE | Smoke test pass; report này |

## 3. File thay đổi

**Tạo mới:**
- `src/Vazer.Core/Loaders/ILoaderInstaller.cs`, `InstallerProgress.cs`, `LoaderHelpers.cs`
- `src/Vazer.Core/Loaders/VanillaInstaller.cs`, `FabricInstaller.cs`, `QuiltInstaller.cs`, `NeoForgeInstaller.cs` (renamed từ `NeoForgeInstallerService.cs`), `ForgeInstaller.cs`
- `src/Vazer.Core/Instances/IInstanceCreationService.cs`, `InstanceCreationService.cs`
- `src/Vazer.Core/Abstractions/InstanceInfo.cs`
- `tests/Vazer.Core.Tests/LoaderInstallersTests.cs`, `InstanceCreationServiceTests.cs`

**Sửa:**
- `src/Vazer.Core/Loaders/INeoForgeInstaller.cs` (renamed từ `INeoForgeInstallerService.cs`, interface đổi tên `INeoForgeInstaller`)
- `src/Vazer.Core/Instances/InstanceManager.cs` (đổi tên tham chiếu)
- `src/Vazer.Core/Versions/MinecraftVersionService.cs` (McToNeoForgePrefix ref)
- `src/Vazer.Core/Extensions/MinecraftServiceCollectionExtensions.cs` (DI: 5 installer + facade)
- `src/Vaz.WinUI/ViewModels/CreateInstanceViewModel.cs`, `Views/CreateInstanceDialog.xaml(.cs)`
- `tests/Vazer.Core.Tests/NeoForgeInstallerTests.cs`, `InstanceManagerTests.cs` (rename refs)

**Xóa:** không có (InstanceManager.Create*Async vẫn dùng bởi MrpackImporter).

## 4. Test

- **Trước**: 368/368 pass. **Sau**: **390/390 pass** (+22 test mới: 10 loader installer + 8 facade + 4 NeoForge helper giữ nguyên).
- `npm run check:static`: **PASSED**.
- Smoke test: app mở được, dialog "Tạo instance mới" mở được (UIA), không crash.

## 5. Evidence

- Build WinUI: 0 error, 0 warning.
- Smoke run: `MainWindow` xuất hiện (UIA dump), dialog open = True.
- UIA dump dialog: `docs/evidence/dot18/uia/` (18.2d artifacts vẫn còn giá trị).
- ProgressBar/TextBlock chỉ hiển thị khi IsInstalling=true → chỉ thấy trong lúc cài → **chưa chụp được screenshot UI progress** (xem Known issues).

## 6. Known issues / hạn chế

1. **Progress determinate chỉ có ở Vanilla** (qua CmlLib fileProgress). Fabric/Quilt/NeoForge vẫn indeterminate (-1) vì CmlLib không report bytes cho loader install. Consistency of *model* OK, consistency of *percent* chưa đạt.
2. **NeoForge cài xong không trả version name** → facade phải quét `instancePath/versions/` (`InstanceManager.DiscoverVersionName`) để lấy LaunchVersionName — heuristic OK nhưng không check được trong unit test (cast fake installer).
3. **MrpackImporter vẫn dùng `InstanceManager.Create*Async`** (chưa chuyển sang facade) — hành vi giữ nguyên vì test mock `IInstanceManager` dày đặc; nên chuyển sang facade ở task sau để một đường code.
4. **WinUI 3 ContentDialog không có `IsCloseButtonEnabled`** (đã kiểm tra winmd) → không thể gray-out nút Hủy; thay bằng `CloseButtonClick` cancel khi `IsInstalling` (click không đóng dialog). Nút Tạo disable bình thường.
5. **UI Progress Bar/TextBlock + badge progress chưa verify qua cài đặt thật qua UI** (cần download lớn ~300MB) → MANUAL_REQUIRED.

## 7. Đề xuất

- Chuyển MrpackImporter sang `IInstanceCreationService` (Đợt 18.3/18.4).
- Forge installer thật ở Đợt 18.3 (registry đã có chỗ).
- Nếu cần percent cho Fabric/Quilt: dùng fileProgress của CmlLib installer (nếu có overload) hoặc báo ước lượng theo stage.

## 8. Self-review

- [x] 368 test cũ giữ nguyên coverage (chỉ rename class/interface, không xóa test nào).
- [x] Không đổi behavior user-facing (Create flow giữ nguyên thứ tự bước, metadata format giữ nguyên, instance.json format giữ nguyên).
- [x] Không claim PASS manual test (progress UI chưa chạy end-to-end qua UI thật).
- [x] WinUI build sạch (0 error).
- [x] DI chỉ thêm đăng ký — không đổi đăng ký cũ (INeoForgeInstaller giữ nguyên qua concrete instance chung).
- Bài học: blanket rename (NeoForgeInstallerService → NeoForgeInstaller) cũng đổi luôn `INeoForgeInstallerService` thành `INeoForgeInstaller` ngoài ý muốn → phải rename file interface theo (đã làm), mọi thứ nhất quán lại sau build+test.
