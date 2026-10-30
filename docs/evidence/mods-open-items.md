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
- UNVERIFIED-runtime | UNVERIFIED | ER-1..4 (mất mạng/timeout/429/5xx), DP-1 (debounce rapid typing), PH-2..3 (pagination/sort dropdown), DL-1..3 (download progress/cancel UI dialog), LD-07 (lock file khi game running), LD-09 (.mrpack import dialog), DEP (dependencies suggest) — giữ nguyên UNVERIFIED theo quy định không báo hoàn thành ảo
