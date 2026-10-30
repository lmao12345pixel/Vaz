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
- B2-fix-traversal | PENDING | chặn `..` + khóa host cdn.modrinth.com trong ModInstallService
- B2-fix-bulkupdate | PENDING | thêm `POST /version_files` vào ModrinthClient + CheckUpdates dùng Bulk
- B2-fix-jarmeta | PENDING | parse fabric.mod.json / mods.toml / neoforge.mods.toml từ jar khi import & hiển thị
- B4-evidence | PENDING | chụp app 1025x533 + 960x530
- UNVERIFIED-429/timeout | UNVERIFIED | không chủ độngTrigger 429; code có retry 3, chờ Retry-After
