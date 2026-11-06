# Đợt 18.4 — Forge Installer Support

## 1. Tổng quan

Bổ sung hỗ trợ cài đặt **Minecraft Forge** song song NeoForge/Fabric/Quilt/Vanilla:

- **Forge metadata**: `MinecraftVersionService.GetForgeVersionsAsync(mcVersion, ct)` — fetch từ `promotions_slim.json` của Forge (files.minecraftforge.net), filter theo `{mc}-recommended` / `{mc}-latest`, trả về `LoaderVersionInfo` (`IsRecommended=true` cho recommended). Cache 24h tại `%APPDATA%/Vazer/cache/loader-versions/forge/{mcVersion}.json`, reuse toàn bộ pipeline cache/loader của Fabric/Quilt/NeoForge (không cần code mới).
- **ForgeInstaller** (`src/Vazer.Core/Loaders/ForgeInstaller.cs`): `ILoaderInstaller` với `LoaderType => "forge"`; headless `java -jar forge-<fullVersion>-installer.jar --installClient <instancePath>`; tải installer từ `maven.minecraftforge.net`; tạo `launcher_profiles.json` trong instance dir trước khi cài (bắt buộc với Forge installer); retry/error handling theo pattern NeoForgeInstaller; viết `instance.json` khi gọi standalone; **MC < 1.13 → `NotSupportedException` với message rõ ràng** (Forge headless chỉ hỗ trợ MC ≥ 1.13).
- **Wizard**: Step 2 thêm card **Forge** (đỏ nâu, logo "Fo"), auto-fill loader version từ metadata, **disable nút "Tiếp tục" + warning "Forge headless chỉ hỗ trợ MC 1.13+."** khi MC < 1.13; Step 3 preview/loader line/size (~275 MB)/hint cập nhật; DI đăng ký `ForgeInstaller`.
- **MrpackImporter**: bỏ chặn Forge (`NotSupportedException` cũ); modpack có `dependencies.forge` giờ đi qua `InstanceManager.CreateForgeAsync` → `ForgeInstaller`. (Phụ: thêm luôn nhánh `quilt-loader` qua `CreateQuiltAsync` — trước đây modpack Quilt âm thầm rơi về Vanilla.)
- **Runtime E2E verified**: tạo instance Forge 1.20.1 từ Wizard → success state → `instance.json` hợp lệ → card xuất hiện trong "Instances" (video + screenshot bên dưới).

**Sửa đợt này (bug phát hiện trong đợt)**: tên mặc định ở Step 3 cho Forge bị rơi nhầm về "Fabric" (`LoaderBaseName` thiếu nhánh `forge`) → fix 1 dòng + commit riêng `3e2b616`.

## 2. Commits (đúng 1 commit / Việc)

| Việc | Commit | Nội dung |
|:---:|---|---|
| 1 | `9cacc46` | `feat(versions): add Forge loader version metadata source` |
| 2 | `68f3435` | `feat(loaders): implement ForgeInstaller with headless install` |
| 3 | `3fe69e8` | `feat(wizard): add Forge to instance creation UI` |
| 4 | `4851a95` | `fix(mrpack): allow Forge modpacks installation` |
| 5 | `3e2b616` | `fix(forge): default instance name uses Forge for Forge loader` (bug do đợt này) |

(Việc 5 thuần kiểm tra còn lại: `dotnet test` 408/408, `check.ps1 -Scope static` PASS — không cần commit thêm.)

## 3. Danh sách file (Tạo / Sửa)

**Tạo mới:**
- `src/Vazer.Core/Loaders/ForgeInstaller.cs` — Forge installer (rewrite placeholder cũ).
- `tests/Vazer.Core.Tests/ForgeInstallerTests.cs` — 15 test (SupportsVersion theory, headless guard, launcher_profiles, command args, process fail, version prefix).
- `docs/evidence/dot18/forge-installer-report.md` — báo cáo này.
- `scripts/record-forge-1201-success.ps1`, `scripts/record-forge-1201-full.ps1` (bản đầu, superseded), `scripts/record-forge-189-warning.ps1`, `scripts/capture-forge-step2.ps1`, `scripts/test-forge-flow.ps1`, `scripts/probe-forge-combo.ps1`.
- Evidence: `docs/evidence/dot18/videos/wizard-forge-1.20.1-create.mp4` (48.7s), `docs/evidence/dot18/videos/wizard-forge-1.8.9-warning.mp4` (17.1s), screenshots `wizard-step2-5cards-forge.png`, `wizard-forge-1.8.9-warning.png`, `wizard-step3-forge.png`, `wizard-forge-success.png`, `wizard-main-after-forge.png`, `forge-play-invoke-state.png`, `forge-after-launch-attempt.png`, `wizard-step3-forge-dark.png` (superseded, xem §7).

**Sửa đổi:**
- `src/Vazer.Core/Versions/MinecraftVersionService.cs` — `GetForgeVersionsAsync` (delegate sang pipeline loader chung) + `FetchForgeVersionsAsync` + nhánh `forge` trong `GetLoaderVersionsAsync`.
- `src/Vazer.Core/Versions/IMinecraftVersionService.cs` — thêm `GetForgeVersionsAsync(string, CancellationToken)`. **(ngoài danh sách PHẠM VI, bắt buộc để implement spec Việc 1)**
- `src/Vazer.Core/Versions/LoaderVersionInfo.cs` — thêm `IsRecommended` + alias `LoaderVersion` (spec Việc 1 yêu cầu 2 trường này). **(ngoài PHẠM VI, bắt buộc)**
- `src/Vazer.Core/Versions/IMinecraftVersionService.cs` (như trên) và fake `InstanceCreationServiceTests.FakeVersionService` được thêm 1 method tương ứng.
- `src/Vazer.Core/Extensions/MinecraftServiceCollectionExtensions.cs` — đăng ký `ForgeInstaller` (singleton + `ILoaderInstaller` + `LoaderInstallerRegistry["forge"]`).
- `src/Vazer.Core/Abstractions/IInstanceManager.cs` — thêm `CreateForgeAsync(...)`.
- `src/Vazer.Core/Instances/InstanceManager.cs` — `CreateForgeAsync` (instance.json → ForgeInstaller → DiscoverVersionName). Constructor thêm `Loaders.ForgeInstaller?` optional (tương thích backward).
- `src/Vazer.Core/Modpacks/MrpackImporter.cs` — xóa block chặn Forge; `ResolveLoader` dùng cho cả Fabric/NeoForge/Quilt/Forge; import Quilt đi qua `CreateQuiltAsync`.
- `src/Vaz.WinUI/ViewModels/CreateInstanceViewModel.cs` — `Loaders` thêm "Forge"; `UpdateCanCreateInstance` chặn Forge+MC<1.13 (disable Tạo + warning).
- `src/Vaz.WinUI/Views/Controls/WizardStep2Loader.xaml(.cs)` — 5 card (Vanilla/Fabric/Quilt/NeoForge/Forge), gradient Forge, logo spin, preview "~275 MB", validation + disable
- `src/Vaz.WinUI/Views/Controls/WizardStep3Confirm.xaml` — thêm `ForgeGradient`.
- `src/Vaz.WinUI/Views/Controls/WizardStep3Confirm.xaml.cs` — `LoaderBaseName` map `forge` (bug fix `3e2b616`).
- `tests/Vazer.Core.Tests/MinecraftVersionServiceTests.cs`, `LoaderInstallersTests.cs`, `MrpackImporterTests.cs` — test Forge (408 pass).

**Không sửa** (đúng phạm vi): `NeoForgeInstaller.cs`, `FabricInstaller.cs`, `QuiltInstaller.cs`, `InstanceCreationService.cs` (chỉ thêm đăng ký DI), `LoaderVersionComparer.cs`.

## 4. Test & kiểm tra

| Mốc | Kết quả |
|---|---|
| Baseline trước đợt | **390/390** pass |
| Sau đợt | **408/408** pass, 0 skip (`dotnet test Vaz.sln`) |
| `npm run check:static` / `check.ps1 -Scope static` | **PASSED** (build + test + agent config) |
| Warnings mới introduced | Không. (CS1998 `ClearCacheAsync`, CS8620 Fabric/Quilt — đều pre-existing, được ghi nhận nhưng **không sửa** vì ngoài phạm vi và không do đợt này.) |

Runtime checks (thật, trên máy chạy):
- Unit tests cover: Forge metadata filter/recommended-dedup/empty (MC 26.x) / cache; headless guard <1.13; command args `-jar …-installer.jar --installClient <path>`; `launcher_profiles.json` creation; error khi process exit≠0; prefix hóa `47.2.0` → `1.20.1-47.2.0`; Mrpack forge không còn throw.
- Real network: HEAD request `forge-1.20.1-47.4.28-installer.jar` → 200 (8.86 MB). `1.19.4-45.4.0` / `1.19.4-45.4.5` → 200. `1.8.9-11.15.1.2318` → **404** (artifact legacy không có ở Maven hiện tại — xem Known issues).
- App runtime: loader version `1.20.1-47.4.28` auto-loaded ở Step 2 (cache file `%APPDATA%/Vazer/cache/loader-versions/forge/1.20.1.json` chứa đúng 1 entry: recommended==latest=47.4.28 → dedupe, `IsRecommended=true`).
- Sau install: `instance.json` (`loaderType=forge`, `launchVersionName=1.20.1-forge-47.4.28`), `versions/1.20.1-forge-47.4.28/`, `versions/1.20.1/1.20.1.jar`, `libraries/net/minecraftforge/*` đầy đủ, `launcher_profiles.json` chứa profile `forge` (do chính installer ghi).

## 5. Bằng chứng (Evidence)

**Video Forge 1.20.1 tạo thành công** — `docs/evidence/dot18/videos/wizard-forge-1.20.1-create.mp4` (**48.7s**, 146 frames, ~1 fps CopyFromScreen compile 3fps): mở dialog → search "1.20.1" → chọn card → Step 2 chọn Forge → dropdown tự nạp `1.20.1-47.4.28` → Step 3 → "Tạo Instance" → success ("Đã tạo thành công!") → instance xuất hiện trong Library. **Đây là lần chạy thật**; video compile 3fps nên phát nhanh hơn thực tế (installer thực sự chạy ~2 phút, 2 instance Forge được tạo — #1 và #2, đều hợp lệ).

**Video Forge 1.8.9 bị chặn** — `docs/evidence/dot18/videos/wizard-forge-1.8.9-warning.mp4` (17.1s): chọn MC 1.8.9 → Forge → dropdown trống, warning **"Forge headless chỉ hỗ trợ MC 1.13+."**, nút "Tiếp tục →" disabled.

**Screenshots:**
- `wizard-step2-5cards-forge.png` — Step 2, 5 card (có Forge) + dropdown `1.20.1-47.4.28` + tóm tắt "Forge 1.20.1-47.4.28 · ~275 MB".
- `wizard-forge-1.8.9-warning.png` — MC 1.8.9, warning như trên.
- `wizard-step3-forge.png` — Step 3, tên mặc định "**Forge** 1.20.1 #1" (sau bug fix).
- `wizard-forge-success.png` — morph button hiển thị checkmark + "Đã tạo thành công!".
- `wizard-main-after-forge.png` — Library gồm Forge 1.20.1 #1/#2 (badge `forge`).
- `forge-play-invoke-state.png`, `forge-after-launch-attempt.png` — trạng thái launch (xem dưới).

**Launch instance Forge 1.20.1 → game vào menu: KHÔNG VERIFIED.** Chi tiết:
- `logs/latest.log` của instance #2 xác nhận: **ModLauncher chạy** (`--fml.forgeVersion 47.4.28`, Java 17.0.20.1 Temurin), GL negotiation xong ("Requested GL version 4.3 got version 4.3"), **Mixin 0.8.5 khởi động OK**, và dừng ở dòng `GL info: Intel(R) HD Graphics 4600 GL version 4.3.0` rồi JVM thoát im lặng (~10–15s). Không có `hs_err_pid*`, không có crash-reports, không có debug.log. RT flavor: **khả năng cao do GPU Intel HD 4600 + driver 20.19.15.5171 (2016)** — ngoài phạm vi code.
- Toast trong app hiển thị "Đang chuẩn bị Minecraft" + "**Đã khởi chạy Minecraft thành công.**" → launch pipeline (resolve Java 17, assetsDir, ModLauncher args `--launchTarget forgeclient`) chạy tới bước spawn JVM thành công; game không vào được menu trên máy này.
- **Manual test cần thiết** trên máy có GPU hỗ trợ: click "Chơi" trên card "Forge 1.20.1 #2" → chờ đến main menu → ghi nhận (đóng góp bởi user).

**instance.json hợp lệ** (evidence cho "tạo xong thành công"):
```json
{
  "id": "forge 1.20.1 #2",
  "name": "Forge 1.20.1 #2",
  "minecraftVersion": "1.20.1",
  "loaderType": "forge",
  "loaderVersion": "1.20.1-47.4.28",
  "launchVersionName": "1.20.1-forge-47.4.28"
}
```

## 6. Known issues / Hạn chế

1. **Game không vào được menu trên máy này** (Intel HD 4600 GL 4.3.0, driver 2016): JVM khởi động rồi thoát im lặng ngay sau giai đoạn GL/Mixin. Cần máy GPU tốt hơn để xác nhận "vào menu". Xem §5.
2. **Legacy Forge (≤1.12.2, gồm 1.8.9)**: promotions_slim có entries nhưng artifact `1.8.9-11.15.1.2318` **không tồn tại** ở `maven.minecraftforge.net` (HEAD 404) — Forge cũ đặt ở files.minecraftforge.net legacy layout. Việc chặn chủ động ở UI (button disabled) + guard `NotSupportedException` ở installer nên người dùng không tải ra file hỏng; nếu muốn hỗ trợ legacy Forge cần source riêng (ngoài phạm vi).
3. **Leftover folder** `%APPDATA%/Vazer/Minecraft/instances/fabric 1.20.1 #1` — thư mục Forge-install dở dang (có `libraries/`, `versions/`, KHÔNG có `instance.json`) do lần automation đầu tiên bị kill quá sớm (App bị force-close trước khi tạo instance.json). Nó không xuất hiện trong Library; **nên xóa thủ công** nếu không cần. Không tự xóa vì là dữ liệu trên máy user.
4. **2 instance Forge hợp lệ được tạo** (`forge 1.20.1 #1`, `forge 1.20.1 #2`) — do đợt thu evidence chạy wizard 2 lần. Cả hai đều đúng artifact; user có thể giữ #2 và xóa #1 nếu muốn.
5. **`LoaderVersionInfo` có 2 field mới** (`IsRecommended`, alias `LoaderVersion`) và `IMinecraftVersionService` có method mới — ngoài danh sách file PHẠM VI nhưng bắt buộc theo spec Việc 1. Đã ghi nhận ở đây.
6. `wizard-step3-forge-dark.png` là screenshot Step 3 **trước** bug fix default-name (hiển thị "Fabric 1.20.1 #1") — **superseded** bởi `wizard-step3-forge.png`; giữ lại trong evidence cho minh bạch, không dùng làm bằng chứng.
7. Forge 1.19.4 hoạt động theo cùng code path 1.20.1 (headless ≥1.13; installer URLs 200) nhưng **chưa chạy end-to-end**; chỉ unit-test + HEAD requests.
8. Metadata Forge cho các MC mới (ví dụ 26.x): promos **không có** → list rỗng + warning log; UI hiển thị "Không có version tương thích" (expected).
9. Video compile 3fps (48.7s) — khác tốc độ thật (~1fps frame capture); user-visible flow không thay đổi. Ngoài ra các frame có thể lẫn cửa sổ/desktop phía sau dialog (CopyFromScreen).

## 7. Đề xuất (sau đợt này)

1. **Launch thật trên máy GPU rời** để chốt AC "vào menu" cho Forge (và nên cho cả Fabric 26.x nếu chưa có evidence runtime cho 26.x).
2. **Xóa file tạm**: `fabric 1.20.1 #1` (forge dở dang) + bản thừa `forge 1.20.1 #1` (nếu không cần).
3. **Legacy Forge**: nếu cần hỗ trợ ≤1.12.2, cần fetch từ legacy Maven (`files.minecraftforge.net/maven/...`) + installer GUI mode hoặc adoptopenjdk cũ — để phase riêng.
4. **MrpackDownloader coverage**: chưa có test nhập modpack Forge *có file zip-slip* qua loader mới (nhánh forge dùng chung code path với fabric nên risk thấp; có thể thêm 2 test nếu cần).
5. **Default name**: với các loader tương lai, cân nhắc single source `LoaderBaseName` dùng chung cho Step 2 preview + Step 3 (hiện Step 2 dùng switch riêng).
6. Nếu QA muốn: thêm unit test cho `CreateForgeAsync` trong `InstanceManagerTests` (hiện test qua mock `IInstanceManager` ở MrpackImporter tests là đủ cho đợt này).

## 8. Self-review & CHƯA KIỂM TRA

- Không tự đánh giá UI/parity PASS; mọi ảnh/video đều là capture thật trên máy chạy. Launch "vào menu" **KHÔNG VERIFIED** (xem §5).
- **CHƯA KIỂM TRA**: (a) vào main menu game Forge 1.20.1; (b) Mrpack Forge end-to-end thật (chỉ unit + mock); (c) Forge end-to-end cho MC ≥1.21/26.x (không có promos); (d) Mrpack import Forge với zip-slip rollback; (e) default name "Forge …" trên luồng tạo **giữ nguyên khi user tự gõ tên** (chỉ test nhánh auto-generate).
- **Manual test script (10 bước)** cho user:
  1. Mở V.A.Z → Settings → Java: xác nhận Temurin 17 có sẵn.
  2. Instances → "Tạo Instance mới".
  3. Step 1: search `1.8.9` → chọn card → Next → **quan sát**: warning "Forge headless chỉ hỗ trợ MC 1.13+." + "Tiếp tục →" disabled → chọn lại `1.20.1`.
  4. Step 2: chọn card **Forge** → dropdown tự điền `1.20.1-47.4.28`.
  5. Đổi tên mặc định nếu muốn → Next.
  6. Step 3: kiểm tra preview (Minecraft 1.20.1, Forge badge cam) → "Tạo Instance".
  7. Chờ footer chuyển "Đã tạo thành công!" (~2 phút).
  8. Library → card "Forge 1.20.1 #N" → nút "Chơi".
  9. Game: chờ ModLauncher → menu. **Nếu thoát im lặng: gửi `logs/latest.log`.**
  10. Modpack: thả file `.mrpack` Forge → verify import không còn báo lỗi.
