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

---

## Đợt 18.2 — Investigate & Expand Loader Support (2026-11-04)

### Việc 1 — Giải trình test count 351 → 349

Tiền đề "351 → 349" **không đúng**. Số liệu thực tế:

| Thời điểm | Tổng test | Nguồn |
|-----------|-----------|-------|
| Trước Phase B (`f84a605` = `769cd75^`) | **342** | đo bằng `dotnet test --list-tests` trong worktree tạm (đã xóa) |
| Sau Phase B (`769cd75`) | **349** | `dotnet test --list-tests` |
| Sau khi thêm 2 test NeoForge (`1adcba7`) | **351** | `dotnet test` full suite |

- Chênh lệch 342 → 349 = **+7 test mới, 0 test bị mất/đổi tên/skip**. Cả 7 đều nằm trong `MinecraftVersionServiceTests.cs` (ClearCacheAsync_ClearsAllFiles, GetCacheTimestampAsync_Exists, GetCacheTimestampAsync_Missing, GetLoaderVersionsAsync_Fabric, GetLoaderVersionsAsync_Quilt, GetLoaderVersionsAsync_UnsupportedLoader_EmptyList, GetVanillaVersionsAsync_FetchesFromMojang).
- Không có test nào bị xóa trong commit Phase B (`git show 769cd75 -- tests/` → `MinecraftVersionServiceTests.cs` là **file mới**, 155 insertions).
- Số "351" không xuất hiện ở đâu trong repo/docs; giả định ban đầu sai. Sau Việc 3, tổng test lại đúng bằng 351.

### Việc 2 — Review diff 2 test đã sửa ở Phase A/B

- `ClearCacheAsync_ClearsAllFiles` và `GetCacheTimestampAsync_Exists`: file test gốc ghi dummy cache vào **RootPath**; service thực tế dùng `{RootPath}/cache/` (`_cacheRoot` trong ctor). Hai test gốc fail deterministic (không flaky, không liên quan UI).
- Fix đã commit: ghi dummy vào `{RootPath}/cache/` (đúng layout thật của service).
- **Coverage không mất**: cả 2 test vẫn verify đúng hành vi mong muốn (ClearCache xóa file cache + giữ RootPath; GetCacheTimestamp trả non-null khi file tồn tại). Service logic không đổi.
- **Kết luận: Chấp nhận được.** Không cần khôi phục test gốc.

### Việc 3 — NeoForge metadata source — DONE (commit `1adcba7`)

- `MinecraftVersionService.FetchNeoForgeVersionsAsync`: tính prefix qua `NeoForgeInstallerService.McToNeoForgePrefix` (yêu cầu MC ≥ 1.20, strip pre-release suffix); MC < 1.20 / snapshot → log warning, trả rỗng **không gọi HTTP**.
- GET `https://maven.neoforged.net/api/maven/versions/releases/net/neoforged/neoforge` (shape `{isSnapshot, versions[]}`), filter `StartsWith(prefix)`, map → `LoaderVersionInfo` (stable / beta (`-beta`) / alpha (các suffix `-` khác, không installable)), URL = Maven installer template.
- Cache `%AppData%/Vazer/cache/loader-versions/neoforge/{mcVersion}.json` (TTL 24h).
- **Không có `20.1.x` trên Maven → MC 1.20.1 KHÔNG có NeoForge builds** (1.20.1 là bản Forge); sớm nhất là `20.2.x` (MC 1.20.2+); MC 1.21.1 → prefix `21.1.` ✓.
- Unit tests mới: `GetLoaderVersionsAsync_NeoForge` (filter prefix, stable/beta) và `GetLoaderVersionsAsync_NeoForge_UnsupportedMc_ReturnsEmpty` (MC 1.19.4 → rỗng, không HTTP). **351/351 pass.**

### Việc 4 — Quilt trong UI — DONE (commit `b38a200`)

- `CreateInstanceViewModel.Loaders` = `["Fabric", "Quilt", "NeoForge", "Vanilla"]`; `CreateAsync` switch có arm Quilt.
- Thêm `CreateQuiltAsync` vào `IInstanceManager` + `InstanceManager` (mirror Fabric: `QuiltInstaller` từ CmlLib.Core 4.0.6, `LoaderType="quilt"`, launch version name do installer trả về, Quilt đã được `LaunchVersionResolver` hỗ trợ sẵn).
- UI bind danh sách Loaders tự động qua `LoaderCombo.ItemsSource` (code-behind) — không cần sửa XAML.

### Gates đã chạy (18.2)

- `dotnet build Vaz.sln -c Debug` — Succeeded, 0 warning, 0 error (sau khi thêm Quilt).
- `dotnet test` — **351/351 pass, 0 skipped** (sau Việc 3 và sau Việc 4).
- `npm run check:static` — **PASSED** (chạy trước commit b38a200).

### Còn tồn đọng / lưu ý

- **UI chưa runtime-verify** — chưa mở dialog kiểm tra Quilt/NeoForge dropdown, nút Tạo disable, badge, tạo instance thật. Không được coi là PASS manual test.
- NeoForge: dropdown đã có dữ liệu (stable/beta) từ 18.2; alpha không hiển thị/installable. Installer vẫn tự resolve nếu `loaderVersion = null`; UI giờ luôn gửi selected version (hoặc null nếu danh sách rỗng — nhưng danh sách rỗng thì `CanCreateInstance = false`, không tạo được).
- ~~VM sort loader versions bằng ordinal string compare~~ **(đã sửa trong Đợt 18.2b — xem mục dưới).**
- Forge vẫn **không hỗ trợ**.
- Commits của Đợt 18.2: `1adcba7 feat(versions): add NeoForge loader version metadata source`, `b38a200 feat(versions): add Quilt to instance creation UI`.

---

## Đợt 18.2b — Fix sort version & update doc (2026-11-04)

### Giới hạn NeoForge (bổ sung ghi chú)

- **NeoForge chỉ hỗ trợ MC ≥ 1.20.2** (build sớm nhất trên Maven: `20.2.x`). **MC 1.20.1 KHÔNG có NeoForge** (1.20.1 là thời Forge).
- **Forge: chưa hỗ trợ** (sẽ làm trong Đợt 18.3).
- **Quilt: đã hỗ trợ trong UI** (dropdown loader + tạo instance qua QuiltInstaller / CmlLib.Core 4.0.6).

### Việc 1 — Fix sort Loader Version — DONE

- Thêm `src/Vazer.Core/Versions/LoaderVersionComparer.cs` — comparer SemVer-like (bỏ build metadata `+...`, tách core/prerelease tại `-`, so sánh số từng segment; stable > prerelease cùng core; prerelease so sánh theo identifier: số < chữ, số so sánh số, chữ ordinal; prerelease ngắn hơn xếp thấp hơn).
- `CreateInstanceViewModel` dùng `OrderByDescending(v => v.Version, new LoaderVersionComparer())` cho cả stable và beta lists — `21.1.206` nay > `21.1.99`.
- Không sửa service, không thêm package mới.
- Thêm `tests/Vazer.Core.Tests/LoaderVersionComparerTests.cs` (11 case `[Theory]` + 1 `[Fact]` OrderByDescending_GivesHighestFirst = 12 test).

### Gates đã chạy (18.2b)

- `dotnet build Vaz.sln -c Debug` — Succeeded (8 warning đều pre-existing trong `MinecraftVersionService.cs`: CS1998, IL2026; không warning từ code mới).
- `dotnet test` — **363/363 pass, 0 skipped** (từ 351, +12 test comparer).
- `npm run check:static` — **PASSED** (build + test + agent config 12/12; lần đầu chạy báo `4 Lỗi(s)` trong build nhưng không tái hiện được và artifact output đã mất; chạy lại sạch 0 error — xem ghi chú bên dưới).

---

## D?t 18.2d/e - Dropdown MC Version khong group duoc + settings.json recovery (2026-11-05)

### Vi?c 1 - Fix dropdown MC Version - DONE (commit `565f0c8`)

- `CreateInstanceViewModel.LoadVersionsAsync`: bo `.Take(10)` + filter release-only; hien thi TAT CA version (~1300: release + snapshot + old_beta + old_alpha), sap xep release truoc roi snapshot/old (TypeRank); default selection van la release moi nhat. Verify UIA: dropdown select duoc 1.21.1, 1.20.1, 1.20.2.
- Fallback tu Cach C -> Cach A: **Cach C (CollectionViewSource grouping + GroupStyle) crash tren WinUI 3 ComboBox**, 2 cach deu that bai (xem Known Limitations). **Cach B cung bat kha thi** vi WinUI 3 ComboBox khong co `IsEditable`.

### Vi?c 2 - settings.json hong - DONE (commit `31b084a`)

- File hong thuc te: `%APPDATA%\Vazer\Minecraft\settings.json` dong 14 (`\"theme\": \"dark\"`).
- Recovery double-escape (co tu truoc) nay **ghi lai file da fix** atomically + tao `.bak`; parse fail hoan toan -> backup `.bak` + **ghi file mac dinh moi** (truoc do chi backup, khong tao file moi).
- Da chay recovery tren file that: app mo duoc, file parse OK (theme=dark, accent=violet, ramMb=8192 - giu gia tri user, khong reset). `.bak` giu ban hong.

### Known Limitations (WinUI 3 ComboBox)

1. **WinUI 3 ComboBox khong ho tro CollectionViewSource grouping + GroupStyle** - da thu 2 cach, deu crash khi mo dialog (khong phai A/B test cosmetic):
   - Set `ItemsSource = CollectionViewSource.View` truc tiep: `E_NOINTERFACE` (crash.txt 10:16:19).
   - Bind qua XAML `{Binding Source={StaticResource VersionGroups}}` + `GroupStyle.HeaderTemplate`: `InvalidCastException` trong `MeasureOverride`, app chet hang (crash.txt 10:18:31).
2. **WinUI 3 ComboBox khong co `IsEditable`** -> khong the searchable/filter-as-you-type dropdown truc tiep; muon search thi phai doi sang `AutoSuggestBox` (xem danh gia ben duoi).
3. **Hien tai dung Cach A**: flat list ~1300 version, scrollable, release nam dau. Du dung nhung UX chua toi uu (phai scroll xa; snapshot nam sau release). Group header bi chac chan khong co thay cho den khi doi control.

### Danh gia AutoSuggestBox cho dropdown MC Version (chi bao cao - chua lam)

| Khia canh | Danh gia |
|---|---|
| Uu diem | User go tim version nhanh; filter-as-you-type (vd go "1.20" -> thay 1.20.1/1.20.2); giai quyet triet de ~1300 item scroll; giu duoc badge "Da cai" trong item template |
| Nhuoc diem | Phai rewrite bind + selection: AutoSuggestBox dung `Text` + `QuerySubmitted`/`SuggestionChosen` thay vi `SelectedItem`/`ItemsSource` kieu Selector; phai map text -> MinecraftVersionInfo (validation truoc khi enable nut Tao); ItemTemplate khac ComboBox (van giu duoc DataTemplate nhung selection model khac han) |
| Rui ro | Hanh vi focus/keyboard khac ComboBox; co the anh huong validation CanCreateInstance va flow loader-version reload (OnSelectedMinecraftVersionChanged); phan kiem tra regression moi thu dung duoc |
| Uoc luong | ~2-3 gio (code + build + runtime verify UIA + screenshot) |

**Ke luan de xuat: GIU Cach A cho Phase B.** Neu user feedback can search version, lam task rieng (Dot 18.4) - khong tron vao dot nay. Cach C grouping coi nhu BO qua (khong work tren WinUI 3 ComboBox).

### Gates (18.2d/e)

- `dotnet test Vaz.sln` - **364/364 pass** (363 + 1 test settings moi), 0 skipped.
- `npm run check:static` - **PASSED**.
- `dotnet build src/Vaz.WinUI` - 0 error, 0 warning.
- Runtime: app mo dialog OK, settings real file tu recovery OK. **Chua claim manual test PASS** - user can test tay (cuon dropdown, chon 1.20.1/1.20.2, tao instance, mo settings).
