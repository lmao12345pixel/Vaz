# Mods Open-Items Ledger — TÌM MOD (Modrinth) & LOAD MOD

**Task riêng**: cải tiến + kiểm thử thật TÌM MOD / LOAD MOD từ Modrinth — không thay đổi status contract `mod-manager.contract.md` (đã PARITY_VERIFIED) hay `resource-packs-shaders` (PARITY_PARTIAL).
**Format**: `mã | trạng thái | bằng chứng/ghi chú`. Ảnh đặt tên `<mã>-<mô tả>-<WxH>.png`, KHÔNG ghi đè ảnh cũ.
**Chat**: ≤10 dòng/tin; chi tiết đưa vào các file docs này.

## 0. Bước 0 — Mô tả code hiện tại (30/10/2026, đọc qua toàn bộ code)

### TÌM MOD (ModBruj: Modrinth search + download)
- UI: `src/Vaz.WinUI/Views/ModBrowserDialog.xaml(.cs)` — ContentDialog "Download Mods": AutoSuggestBox search, 3 ComboBox (Loader/MC version/Sort), InfoBar + ProgressBar, ListView results, Load more, overlay xác nhận dependency (`DependencyPromptOverlay`). Mở từ `DownloadMods_Click` (InstanceDetailPage L250-255). `ProjectDetailDialog.xaml.cs` gọi `_modrinth.GetProjectAsync(slug)`, render body qua `MarkdownLiteParser`.
- ViewModel: `ModBrowserViewModel.cs` — `Results` (ObservableCollection<SearchHitRow>), PageSize=20, debounce 300ms `OnSearchTextChangedDebounced` (L120-140), sort map Downloads/Updated/Newest/Relevance (L168-174), loader/version "Auto" rút từ instance active (L221-243), `InstallModAsync` → `_modInstall.InstallFromModrinthAsync(id, slug, versionId:null, resolveDependencies:true)` (L294). DI: ModBrowserViewModel là Transient (App.xaml.cs L49).
- HTTP: `src/Vazer.Core/Modrinth/ModrinthClient.cs` — BaseUrl `https://api.modrinth.com/v2`; header UA = `Vazer/1.0.0 (+https://vazer.app)` (MinecraftOptions L46); cache GET 5 phút (ConcurrentDictionary keyed URL); retry 3 lần khi 429 (đợi Retry-After, mặc định 1s); timeout client `modrinth-api` 30s, `modrinth-download` 300s (MinecraftServiceCollectionExtensions L62-70). Endpoint đã dùng: `GET /search` (facets mảng lồng `[[...]]` đúng chuẩn — BuildFacets L146-163), `GET /project/{slug}`, `GET /project/{slug}/version?loaders=[..]&game_versions=[..]`, `GET /version/{id}`, `GET {fileUrl}` (buffer vào MemoryStream trước khi trả). **Chưa có** `POST /version_files` (tra cứu theo hash) — cần cho cập nhật hàng loạt.
- Download: `src/Vazer.Core/Mods/ModInstallService.cs` — ghi `{target}.partial` rồi `File.Move(overwrite:true)` (L160-172, 211); **sha512 verify** trước khi move, sai → xóa `.partial` + throw (L174-192); trùng ProjectId → thay jar, giữ Enabled (L138, 194-230); tên file trùng → thêm `-1`,`-2` (L147-157). **Vấn đề**: dùng `file.Filename` từ API trực tiếp cho `Path.Combine` (không re-check traversal, L140-161); chấp nhận mọi URL tải (khóa host `cdn.modrinth.com`); dependency install đệ quy depth<=5 + visited cycle guard (L232-260); chọn release→beta (banner warning)→error; primary file ưu tiên.
- .mrpack: `src/Vazer.Core/Modpacks/MrpackImporter.cs` — đọc `modrinth.index.json` (format=1, game=minecraft), tải file file-by-file từ `Downloads[0]` (chưa failover mirror) + sha512 xác minh, **zip-slip guard** `SafeResolvePath` (L344-359), đồng bộ `mods.json` Source="mrpack"; Forge bị chặn.
- Manifest: `mods.json` atomic write `.tmp`+Move (L522-550).

### LOAD MOD (danh sách mod instance)
- `InstanceDetailViewModel.cs` `LoadAsync` (L160-282): quét `{instance}/mods/*.jar` không có trong `mods.json` → tự thêm entry Source="custom", `ProjectId = ModCatalogData.GetSlug(...)` (heuristic theo tên file), VersionId="1.0.0"; rồi `_modInstall.ListAsync`.
- **Không parse metadata trong jar**: không có fabric.mod.json / META-INF/mods.toml / neoforge.mods.toml — metadata hiển thị lấy từ `ModCatalogData` (27 mod hardcoded, L90-534; GetSlug L536-567); mod lạ chỉ hiện tên file. **Synthetic data**: mọi mod có `ProjectUrl = https://modrinth.com/mod/{slug}` và `SourceUrl = https://github.com/vazer/{slug}` (giả cho mod thật) — L246-247.
- Toggle = `SetModEnabledAsync` (rename `*.disabled`, l332-367 có cảnh báo mod phụ thuộc); xóa = `UninstallAsync` (XIconfirm dialog InstanceDetailPage L465-485); error guard app `e.Handled=true` (docs rps 3.8).
- Cập nhật: `CheckUpdatesAsync`/`UpdateAllAsync` gọi per-mod `GetVersionsAsync` (chưa dùng bulk hash lookup).
- ListView chuẩn WinUI trên tab Mods (default UI virtualization) — chưa đo 500+ mod.

## 1. Ledger (đang chạy)

- B0-mota | ĐÃ LÀM | mục 0 ở trên + explore ses_e72f1e79dffez80VvN4Aipqnfh
- SM (search real API) | ĐÃ LÀM | 14/14 HTTP 200 — `docs/evidence/mods-test-report.md` §2, log `.agent/mods-test-raw.txt`
- HA (hash lookup real API) | ĐÃ LÀM | `POST /version_files` real jar → resolved=1, version_number đúng; fake hash → `{}`; sha512 tải == API — `mods-test-report.md` §3, log `.agent/mods-hash-raw.txt`
- B2-fix-traversal | ĐÃ LÀM | `ModInstallService`: `IsAllowedDownloadUrl` (chỉ https://cdn.modrinth.com) chặn trước download; `SanitizeModFileName` từ chối tên file API chứa traversal/path/control → throw + không tải; unit test `ModDownloadGuardTests.cs` (14 case) — fusion-fixed trong group 2
- B2-fix-bulkupdate | ĐÃ LÀM | `IModrinthClient.GetVersionsFromHashesAsync` (POST /version_files, Model `VersionFilesRequest`, JSON context thêm serializable types); `ModInstallService.CheckUpdatesByHashAsync` (chunk ≤100/hash, hash từ manifest hoặc băm đĩa, so Id/VersionNumber); WinUI `CheckUpdatesAsync` gọi bulk; unit test `ModInstallBulkUpdateTests.cs` (8 case, mock POST)
- B2-fix-jarmeta | ĐÃ LÀM | `JarMetadataReader` (fabric/quilt JSON + mods.toml/neoforge.mods.toml, corrupt → null); WinUI: auto-discovery đọc metadata khi LoadAsync, import .jar điền ProjectId/VersionId/Sha512, hiển thị + cảnh báo loader mismatch/jar hỏng (LoadWarning), row Version dùng metadata thay "1.0.0" giả; unit test `JarMetadataReaderTests.cs` (9 case zip thật)
- B2-dup-skip | ĐÃ LÀM | Modrinth install: trùng sha512 trong manifest → bỏ qua (Warning "Duplicate skipped…"), xóa .partial; import .jar: trùng hash/nội dung → bỏ qua có tổng kết; name collision → -1/-2, không ghi đè
- B2-synthetic-url | ĐÃ LÀM | bỏ `SourceUrl = https://github.com/vazer/{slug}` + link "Mã nguồn" giả trong XAML; ProjectUrl chỉ hiện khi Source=modrinth/có catalog entry
- B3-tests | ĐÃ LÀM | 31 test mới: JarMetadataReaderTests 9 · ModDownloadGuardTests 14 · ModInstallBulkUpdateTests 8; `dotnet test Vaz.sln` = 269/269 PASS (đã là 238) — 0 skip 0 fail
- B4-evidence | ĐÃ LÀM | 8 bộ ảnh 1025×533 + 960×530 chụp app chạy thật trên Windows (SM-01 browse, SM-01 search sodium, SM-02 no-results, SM-03 tiếng Việt có dấu, LD-01/04/05/06 corrupt/mismatch warnings, LD-03 500+ mods đo 2698ms, UPD-01 bulk check updates); lưu tại `docs/evidence/`
- B2-UPD-round2 | ĐÃ LÀM | POST /version_files/update (algorithm: sha512, loaders, game_versions). CheckUpdatesByHashAsync kiểm tra cả mod custom (nếu khớp hash Modrinth thì nâng cấp Source=modrinth và gán ProjectId/Sha512). Script test-modrinth-update.ps1 xác minh Sodium 0.6.0 (f7b9...) -> 0.8.13 (SMxNOGZ6) HTTP 200. Chạy thật trên WinUI: InfoBar '1 mod có bản mới.' hiện rõ ràng; chụp UPD-01-update-available-1025x533.png & 960x530.png. Commit 50bf5f5.
- B2-stream-dl | ĐÃ LÀM | NetworkResponseStream truyền trực tiếp từ response stream ra file đĩa không buffer MemoryStream. Kiểm tra redirect không thoát cdn.modrinth.com. Xóa sạch .partial khi hủy/lỗi. MrpackImporter bổ sung kiểm tra host và dọn .partial. ModBrowserViewModel hỗ trợ CancelInstallCommand + nút Hủy trong UI. Commit 6b1ec4f (ghi chú trước đó ghi nhầm hash 04f9382 khi rebase nháp; hash chính thức trên git là 6b1ec4f).
- B3-tests-round2 | ĐÃ LÀM | 17 unit test mới trong ModNetworkAndDependencyTests.cs (ER-1..4, DP-1, PH-2..3, DEP required/incompatible/optional, DL redirect guard, DL partial cleanup, Mrpack host guard). Tổng test: 286/286 PASS. Commit 6b1ec4f.
- B2-LD06-warnings | ĐÃ LÀM | Cảnh báo trùng mod ID giữa các jar + cảnh báo sai phiên bản Minecraft (IsMinecraftVersionCompatible hỗ trợ exact, ranges, wildcard, tilde). Cảnh báo nêu rõ hậu quả crash. 11 unit test mới trong JarMetadataReaderTests.cs (tổng test: 297/297 PASS). Commit db9b8a9.
- B2-LD03-perf-cache | ĐÃ LÀM | Đọc metadata jar chạy trên background Task.Run không block UI thread. Cache tuần tự hóa ra file đĩa .jar-metadata-cache.json. Đo 520 mods: Cold load 650ms, Warm load 104ms (<1s). Commit db9b8a9.
- B2-SM08-hints-badge | ĐÃ LÀM | Tìm kiếm rỗng được cho phép khi duyệt mod ban đầu (đóng: không phải lỗi). 0 kết quả hiển thị gợi ý 'Thử từ khóa tiếng Anh...'. Mod đã cài hiển thị badge xanh lá 'Đã cài'. Commit db9b8a9.
- B4-evidence-round2 | ĐÃ LÀM | Chụp đầy đủ các bộ ảnh mới ở cả 2 độ phân giải 1025x533 và 960x530: LD-06 duplicate mod ID, LD-06 MC version mismatch, LD-04 corrupt jar (960x530), SM-08 no results hint, SM-08 installed badge, UPD-01 update available.
- `B5-manual-checklist` | ĐÃ LÀM | Chuyển toàn bộ các kịch bản cần thiết bị vật lý / tương tác thật của người dùng sang docs/evidence/mods-manual-checklist.md để người dùng tick kiểm chứng: MC-SM-01 (kéo thả file ngoài), MC-SM-02 (bấm Hủy tải trên UI), MC-SM-03 (ngắt mạng vật lý), MC-SM-04 (.mrpack picker), MC-LD-01 (khóa file khi game đang chạy), MC-LD-02 (hộp thoại xác nhận xóa), MC-LD-04 (scale màn hình 125%/150%).
- `C4-G1-search-error` | ĐÃ LÀM | SM-08 query rỗng trả danh sách rỗng không gọi HTTP; SM-13/14 limit được kẹp Math.Clamp(1, 100); ER-1..4 ModrinthErrorMapper (0 auto-retry cho 429 ném ModrinthRateLimitException kèm Retry-After, 1 auto-retry cho timeout/5xx); DP-1 debounce 300ms hủy request cũ, test 10 phím gõ nhanh; PH-2/3 PaginationState { offset, limit, hasMore, total, isLoadingMore }, sort mapping; 13 unit test mới. Commit e086c7c.
- `C4-G2-safe-download-dep` | ĐÃ LÀM | DL-1..3 tải qua file tạm, sha512 kiểm tra trước File.Move, CancellationToken hủy hoặc lỗi mạng/sai hash lập tức xóa file tạm, tiến trình báo % và byte; DEP required vs optional dependencies, UI checkbox 'Tải kèm dependencies', mặc định release mới nhất tương thích MC + loader; 3 unit test mới. Commit efa6830.
- `B6-browse-popular-mods` | ĐÃ LÀM | GetFeaturedModsAsync gọi /v2/search?query= (sort downloads, facets loader/MC) khi mở dialog hoặc xóa từ khóa; SearchAsync query rỗng tiếp tục trả rỗng không gọi HTTP. _currentRequestId chống race condition request cũ đè request mới. 6 unit test mới. Commit ba21505.
- `B7-pr-review-hardening` | ĐÃ LÀM | Tải file tạm đổi sang .partial cùng thư mục đích modsDir (atomic rename trên cùng ổ đĩa, dọn sạch trong finally). Thêm CleanOrphanPartials dọn file .partial mồ côi khi khởi động/nạp instance. Badge 'Đã cài' bỏ suy diễn ModCatalogData, kiểm tra file thật trên đĩa (File.Exists). Ẩn nút 'Tải thêm' khi Results.Count >= TotalHits. Thêm unit test rớt mạng, hủy tải, sai hash, dọn dẹp file mồ côi, 429 Retry-After. Chạy 3 lần liên tiếp dotnet test: 322/322 PASS. CI .github/workflows/ci.yml kiểm chứng trên windows-latest.
- `B8-missing-file-badge` | ĐÃ LÀM | Quét file jar thật trên đĩa; nếu có trong mods.json nhưng thiếu file jar thì hiện badge đỏ 'Thiếu file', khóa ToggleSwitch, mở ContentDialog cho phép 'Gỡ bỏ' khỏi mods.json. HasShaderLoader loại bỏ mod thiếu file và mod disabled. Unit test ShaderLoaderResolverTests + ảnh LD-07 (1025x533 và 960x530). Commit b7d79c7.
- `B8-duplicate-game-crash-warning` | ĐÃ LÀM | Khi có nhiều jar cùng mod id, cảnh báo ghi rõ 'game có thể không chạy' do DuplicateModException. Tái hiện và chụp ảnh trên instance tạm rt-mods-test (LD-06 ở cả 1025x533 và 960x530). Commit e52f492.
- `B8-update-replaces-jar` | ĐÃ LÀM | ModInstallService khi cập nhật sẽ tìm và xóa triệt để file cũ (kể cả file .disabled), thay thế bằng file mới thay vì đặt song song; bản cũ chỉ bị xóa SAU KHI bản mới tải xong và sha512 khớp. Unit test InstallFromModrinthAsync_UpdateReplacesOldJar_DoesNotKeepBothSideBySide. Commit b7d79c7.
- `B8-update-failure-rollback` | ĐÃ LÀM | Cập nhật mod khi tải bị lỗi hoặc hủy thì bản cũ giữ nguyên vẹn trên đĩa và trong manifest. Unit test InstallFromModrinthAsync_UpdateFailsOrCancels_PreservesOldJarAndManifest. Commit d20d692.
- `B8-active-partial-protection` | ĐÃ LÀM | CleanOrphanPartials kiểm tra danh sách ActivePartials (ConcurrentDictionary); các lượt tải đang chạy sẽ được giữ nguyên, chỉ xóa các file .partial mồ côi thật sự. Unit test DL_CleanOrphanPartials_DoesNotDeleteActiveDownloads. Commit b7d79c7.
- `B8-metadata-cache-appdata` | ĐÃ LÀM | Chuyển file cache metadata .jar-metadata-cache.json ra ngoài thư mục instance, đặt tại %AppData%/Vazer/cache/metadata/{instanceName}-{hash}.json; tự động di chuyển và dọn dẹp cache cũ. Commit b7d79c7.
- `B8-test-fabric-iris-missing-file` | ĐÃ LÀM | Chụp màn hình tab Mods của instance thật 'test fabric' trên app bản mới nhất, hiển thị Iris Shaders với badge 'Thiếu file'. Ảnh LD-08-test-fabric-iris-missing-file-1025x533.png và LD-08-test-fabric-iris-missing-file-960x530.png.
- `CI-status-github` | ĐÃ LÀM | Đã chạy 6 lần trên GitHub Actions (workflow ci.yml trên nhánh feat/browse-popular-mods), 100% XANH (tổng 327 tests pass, sàn MIN_TESTS 320):
  - Run [41170099637](https://github.com/lmao12345pixel/Vaz/actions/runs/41170099637) (commit `655595f`, 327 tests)
  - Run [41168784317](https://github.com/lmao12345pixel/Vaz/actions/runs/41168784317) (commit `05a08f0`, 327 tests)
  - Run [41159049111](https://github.com/lmao12345pixel/Vaz/actions/runs/41159049111) (commit `373bc51`, 326 tests - commit nằm 100% trong phạm vi PR #1)
  - Run [41156024313](https://github.com/lmao12345pixel/Vaz/actions/runs/41156024313) (commit `65af265`, 325 tests)
  - Run [41148553563](https://github.com/lmao12345pixel/Vaz/actions/runs/41148553563) (commit `93610fa`, 322 tests)
  - Run [41103856268](https://github.com/lmao12345pixel/Vaz/actions/runs/41103856268) (commit `ba21505`, 322 tests)
- `UNVERIFIED-manual` | UNVERIFIED | Các kịch bản phụ thuộc phần cứng / tương tác vật lý thật trong docs/evidence/mods-manual-checklist.md: MC-SM-01 (mạng chập chờn), MC-SM-02 (bấm Cancel khi tải), MC-SM-03 (kéo thả .mrpack ngoài Explorer), MC-LD-01 (kéo thả jar ngoài Explorer), MC-LD-02 (kéo thả jar trùng hash), MC-LD-03 (khóa file khi game đang chạy), MC-LD-04 (xác nhận gỡ mod bình thường), MC-LD-05 (gỡ mod thiếu file), MC-LD-06 (scale màn hình 125%/150%). Giữ nguyên UNVERIFIED theo quy định không báo hoàn thành ảo.

---

## 2. Giải trình chi tiết 7 hạng mục bổ sung (31/10/2026)

1. **Iris không có file jar trong tab Mods (Phantom Mod)**:
   - *Nguyên nhân*: `mods.json` chứa mục Iris nhưng file `iris-fabric-1.8.8+mc1.21.1.jar` đã bị xóa khỏi đĩa. Code cũ không kiểm tra `File.Exists`, dẫn đến việc toggle không đổi được file thật và `HasShaderLoader` vẫn nhận diện nhầm.
   - *Khắc phục*: Thêm thuộc tính `IsMissingFile` và `CanToggle` vào `InstalledModRow`. Khi thiếu file, hiển thị badge đỏ 'Thiếu file', vô hiệu hóa `ToggleSwitch`. Cập nhật `HasShaderLoader` chỉ duyệt các mod `!IsMissingFile && Enabled`. Thêm nút gỡ với ContentDialog xác nhận 'Gỡ {tên mod} khỏi danh sách?'.
   - *Bằng chứng*: Unit test `DetectShaderLoader_ExcludesFilteredOutMissingOrDisabledMods` (PASS); ảnh chụp màn hình `docs/evidence/LD-07-missing-file-badge-1025x533.png` (1025x533 px, 96 DPI / 100% display scale) và `LD-07-missing-file-badge-960x530.png` (960x530 px, 96 DPI / 100% display scale) trên instance tạm `rt-mods-test`, cùng ảnh `docs/evidence/LD-08-test-fabric-iris-missing-file-1025x533.png` (1025x533 px, 96 DPI / 100% display scale) và `LD-08-test-fabric-iris-missing-file-960x530.png` (960x530 px, 96 DPI / 100% display scale) trên instance thật `test fabric`.
   - *Ghi chú mã ảnh*: Tiền tố `LD-07` trong ảnh `LD-07-missing-file-badge-*` và `LD-08` trong `LD-08-test-fabric-iris-missing-file-*` là mã file ảnh bằng chứng màn hình cho hạng mục Item 1 (thiếu file jar). Mã này độc lập và không thay thế mã kịch bản thủ công `MC-LD-03` (khóa file khi game đang chạy, trước đây từng được gọi tạm là LD-07 trong dự thảo cũ). Cả 4 file ảnh đều có kích thước thực tế đúng 1025x533 px và 960x530 px, độ phân giải 96 DPI (100% display scale). Giữ nguyên file ảnh hiện tại không đổi tên hay ghi đè theo quy tắc an toàn.

2. **Cảnh báo trùng lặp Mod (Duplicate Mod Jars) & Nút cập nhật**:
   - *Cảnh báo*: Cập nhật thông điệp cảnh báo trong `InstanceDetailViewModel.cs`: `Trùng mod id '{group.Key}' với file '{otherFiles}'. Hậu quả: Loader sẽ crash khi khởi động do DuplicateModException (game có thể không chạy).`
   - *Tái hiện*: Tạo bản sao trên instance tạm `rt-mods-test`, chứa 2 file `sodium-fabric-0.6.0+mc1.21.1.jar` và `sodium-fabric-0.8.13+mc1.21.1.jar`. App cảnh báo chính xác tại cả 2 độ phân giải (1025x533 và 960x530).
   - *Cập nhật thay thế*: Trong `ModInstallService.InstallFromModrinthInternalAsync`, bản mới được tải về `.partial` và kiểm tra SHA512 trước. Chỉ sau khi SHA512 khớp, app mới xóa các bản cũ (`existingMods`) và di chuyển file mới vào. Nếu tải lỗi hoặc hủy, bản cũ và manifest giữ nguyên vẹn. Unit test `InstallFromModrinthAsync_UpdateReplacesOldJar_DoesNotKeepBothSideBySide`, `InstallFromModrinthAsync_UpdateFailsOrCancels_PreservesOldJarAndManifest`, và `InstallFromModrinthAsync_UpdateSha512Mismatch_PreservesOldJarAndManifest` (đều PASS).
   - *Bằng chứng*: Ảnh chụp `docs/evidence/LD-06-duplicate-sodium-warning-1025x533.png` (1025x533 px, 96 DPI / 100% display scale) và `LD-06-duplicate-sodium-warning-960x530.png` (960x530 px, 96 DPI / 100% display scale).
   - *Ghi chú HiDPI*: Mức hiển thị 96 DPI / 100% không đại diện cho HiDPI; kịch bản `MC-LD-06` (scale màn hình 125%/150%) vẫn giữ nguyên trạng thái `UNVERIFIED` cho đến khi kiểm thử trên màn hình thật.

3. **Nguồn gốc file sodium-fabric-0.8.13 trong instance "test fabric"**:
   - **Không rõ nguồn gốc**. File `sodium-fabric-0.8.13+mc1.21.1.jar` trên đĩa có thời gian sửa đổi là 18/10/2026 10:16 PM, trước ngày repository được khởi tạo (23/10/2026). Không có bất kỳ lệnh, log hay commit nào trong lịch sử chứng minh việc tải file này.
   - Thời điểm 2026-10-29T20:04:05 ghi trong `mods.json` chỉ là thời điểm app tự động quét thư mục `mods`, phát hiện file chưa được khai báo và tự thêm vào manifest với thuộc tính `source: custom`. Không suy đoán nguồn gốc file ngoài thực tế.
   - Chủ sản phẩm đã dọn file ra Desktop. Toàn bộ test hiện tại và tương lai được cô lập 100% trên instance tạm (`rt-*`).

4. **Sửa mã commit 04f9382 thành 6b1ec4f**:
   - Kiểm tra `git log --oneline -50` cho thấy commit `04f9382` không tồn tại trong lịch sử git của repository (đây là mã hash nháp cục bộ sinh ra trong quá trình rebase / amend trước khi push).
   - Commit chính thức trên nhánh `feat/browse-popular-mods` chứa toàn bộ phần stream-dl, network resilience và 17 unit tests là `6b1ec4f` ("test(mods): network resilience, stream downloads, dependency resolution + 17 tests"). Đã sửa lại toàn bộ tài liệu dẫn chiếu.

5. **Đánh số cố định mã kiểm thử thủ công MC-SM-xx và MC-LD-xx**:
   - Toàn bộ danh mục kiểm thử thủ công đã được quy chuẩn và lưu trữ tại `docs/evidence/mods-manual-checklist.md` làm Single Source of Truth:
     - `MC-SM-01`: Tải mod khi mạng chập chờn / độ trễ cao.
     - `MC-SM-02`: Bấm Hủy tải trên UI khi file đang tải dở.
     - `MC-SM-03`: Kéo thả file `.mrpack` từ Windows Explorer.
     - `MC-LD-01`: Kéo thả file `.jar` từ Windows Explorer vào tab Mods.
     - `MC-LD-02`: Kéo thả file `.jar` trùng sha512.
     - `MC-LD-03`: Toggle bật/tắt mod khi game đang chạy (khóa file io).
     - `MC-LD-04`: Xóa mod bình thường có hộp thoại xác nhận.
     - `MC-LD-05`: Gỡ bỏ mục mod thiếu file jar khỏi danh sách.
     - `MC-LD-06`: Kiểm tra giao diện ở tỷ lệ thu phóng HiDPI (125%, 150%).

6. **Trạng thái GitHub Actions CI**:
   - Workflow `.github/workflows/ci.yml` trên nhánh `feat/browse-popular-mods` đã có 6 lần chạy, tất cả đều XANH (100% pass, MIN_TESTS sàn 320, hiện tại 327 test pass):
     - Run `41170099637`: commit `655595f`, XANH (Success), 327/327 unit tests passed. Link: `https://github.com/lmao12345pixel/Vaz/actions/runs/41170099637`.
     - Run `41168784317`: commit `05a08f0`, XANH (Success), 327/327 unit tests passed. Link: `https://github.com/lmao12345pixel/Vaz/actions/runs/41168784317`.
     - Run `41159049111`: commit `373bc51`, XANH (Success), 326/326 unit tests passed. Commit `373bc51` nằm 100% trong phạm vi PR #1. Link: `https://github.com/lmao12345pixel/Vaz/actions/runs/41159049111`. File log lưu tại `docs/evidence/ci-run-41159049111.txt`.
     - Run `41156024313`: commit `65af265`, XANH (Success), 325/325 unit tests passed. Link: `https://github.com/lmao12345pixel/Vaz/actions/runs/41156024313`.
     - Run `41148553563`: commit `93610fa`, XANH (Success), 322/322 unit tests passed. Link: `https://github.com/lmao12345pixel/Vaz/actions/runs/41148553563`.
     - Run `41103856268`: commit `ba21505`, XANH (Success), 322/322 unit tests passed. Link: `https://github.com/lmao12345pixel/Vaz/actions/runs/41103856268`.

7. **Bảo vệ active download trong CleanOrphanPartials & Di chuyển cache**:
   - `CleanOrphanPartials`: Bổ sung cơ chế `RegisterActivePartial` quản lý bằng `ConcurrentDictionary`. Trong lúc tải file, đường dẫn file `.partial` được đăng ký bảo vệ và giải phóng khi hoàn tất; hàm dọn dẹp bỏ qua mọi file đang hoạt động, tránh xóa nhầm khi tải song song nhiều mod. Unit test `DL_CleanOrphanPartials_DoesNotDeleteActiveDownloads` (PASS).
   - `.jar-metadata-cache.json`: Đã chuyển toàn bộ cơ chế lưu cache ra khỏi thư mục instance vào `%AppData%/Vazer/cache/metadata/{instanceName}-{hash}.json`. Tự động phát hiện và xóa file cache cũ nếu còn tồn tại trong thư mục instance.
