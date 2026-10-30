# Mods Test Report — TÌM MOD & LOAD MOD (runtime thật)

Task riêng, không ảnh hưởng PARITY contract khác. Mỗi dòng: mã | kịch bản | cách tái hiện | kết quả | mức độ | bằng chứng.
Chưa chạy được → ghi `UNVERIFIED`, không bỏ qua. Logs: `.agent/mods-test-raw.txt`, `.agent/mods-hash-raw.txt`.

## 1. Kịch bản kế hoạch

TÌM MOD: SM-01..14 (từ khóa), DP (debounce/reorder), PH (pagination/sort), ER (mất mạng/timeout/429/5xx + retry), DL (download: progress/cancel/partial/sha512/traversal/cdn/remplacement/trùng), DEP (phụ thuộc/release).
LOAD MOD: LD-01 rỗng, LD-02 1 mod, LD-03 500+ (đo ms), LD-04 jar hỏng, LD-05 jar không metadata, LD-06 trùng id/loader sai, LD-07 toggle/uninstall, LD-08 .jar picker/drag-drop, LD-09 .mrpack (zip-slip, hash), UPD (bulk hash update).

## 2. Kết quả RUNTIME THẬT — TÌM MOD / search API (30/10/2026)

Cách chạy: `powershell scripts/test-modrinth-search.ps1` → HTTP GET thật tới `api.modrinth.com/v2/search`, UA `Vazer/1.0.0 (+https://vazer.app)`, facets dạng mảng lồng gần giống code `BuildFacets` (client C# đã đúng format này).

| Mã | Kịch bản | Kết quả | Mức độ | Bằng chứng |
|---|---|---|---|---|
| SM-01 | từ khóa có kết quả (`sodium`) | HTTP 200, hits=20, first=sodium, 3287ms | — | log raw |
| SM-02 | từ khóa không có kết quả | HTTP 200, hits=0 (UI phải trống + InfoBar) | Đã fix empty-state sẵn (ModBrowserViewModel) | log raw |
| SM-03 | tiếng Việt có dấu `tối ưu hiệu năng` | HTTP 200, hits=0 (chuẩn hóa escape đúng) | — | log raw |
| SM-04 | ký tự đặc biệt `a%b&c=d+f` | HTTP 200, hits=0 | — | log raw |
| SM-05 | từ khóa 500 ký tự | HTTP 200, hits=0, không lỗi 4xx/414 | — | log raw |
| SM-06 | facets loader `categories:fabric` | HTTP 200, hits=20 | — | log raw |
| SM-07 | facets phiên bản `versions:1.21.1` | HTTP 200, hits=20 | — | log raw |
| SM-08 | query rỗng | HTTP 200, hits=20 (API cho phép; App dùng AutoSuggestBox — cần kiểm UI không call rỗng) | LOW | log raw |
| SM-09 | phân trang offset=100 | HTTP 200, hits=8 (pagination hoạt động) | — | log raw |
| SM-10 | offset=100000 (vượt tổng) | HTTP 200, hits=0 | — | log raw |
| SM-11 | `index=downloads` | HTTP 200 (sort hoạt động) | — | log raw |
| SM-12 | `limit=100` (max) | HTTP 200, hits=100 | — | log raw |
| SM-13 | `limit=101` (>max) | HTTP 200 nhưng server trả hits=100 → client tự clamp 1..100 là an toàn | LOW | log raw |
| SM-14 | `limit=0` | HTTP 200, server trả hits=1 → client clamp tốt | LOW | log raw |

**Phát hiện script**: facet PHẲNG `["project_type:mod"]` → HTTP 400 "deserializing JSON data"; facet mảng LỒNG `[["project_type:mod"]]` → HTTP 200. Client C# `BuildFacets` đã build mảng lồng — PASS.

## 3. Kết quả RUNTIME THẬT — download + hash lookup (30/10/2026)

Cách chạy: `powershell scripts/test-modrinth-hash.ps1` → lấy file đầu `sodium-fabric-0.8.13+mc1.21.1.jar` (slug=sodium, primary file, host `cdn.modrinth.com`), tải về → sha512 local so với API → `POST /version_files`.

| Mã | Kịch bản | Kết quả | Mức độ | Bằng chứng |
|---|---|---|---|---|
| HA-01 | GET versions → primary file | 200; file=sodium-fabric-0.8.13+mc1.21.1.jar; api_sha512 len=128; downloads=2114228; url-host=cdn.modrinth.com | — | log raw |
| HA-02 | Tải file thật + so sha512 | size=1574609B, **match=True** (sha512 local == API) | — | log raw |
| HA-03 | `POST /version_files` (bulk) 1 hash thật | HTTP 200, resolved=1, version_number trả về tương ứng (`mc1.21.1-0.8.13-fabric`), project_id được map hash → version object | BLOCK (chưa có trong code — sẽ thêm) | Télé raw |
| HA-04 | hash giả → `POST /version_files` | HTTP 200 body `{}` (không map bất kỳ) — chuẩn cho "không trùng mod tải lại" | — | log inline |

## 4. Kết quả kế hoạch — chưa chạy hoặc đã chạy tại nhánh khác (cập nhật thực hành)

- ER-1 mất mạng / ER-2 timeout 30s / ER-3 HTTP 429 / ER-4 HTTP 5xx | UNVERIFIED (không chủ động ghi hỏng mạng hoặc 429 — không được phép ransom server; code đã có retry 3 + Retry-After; sẽ verify bằng unit test mock HTTP với FakeHttpMessageHandler)
- DP-1 debounce/gõ nhanh (request cũ không ghi đè mới) — sẽ verify bằng unit test với nhiều lần searchAsync lần lượt
- PH-2 load more, PH-3 sort | cần chạy qua WinUI App runtime (automation) — PENDING Bước 4
- DL-1 progress/cancel, DL-2 .partial → rename, DL-3 sha512 sai → xóa temp | runtime UI còn UNVERIFIED (logic .partial + sha512 đã có, test cũ checksum-mismatch pass)
- DL-4 path traversal, DL-5 cdn.modrinth.com allowlist, DL-6 trùng hash | ĐÃ FIX + unit test — xem §4b
- DEP-1 dependencies, DEP-2 release default | code có (InstallFromModrinthAsync resolveDependencies) — test cũ dep/alpha/beta pass; runtime UI UNVERIFIED
- LD-04/05/06/08 logic | ĐÃ FIX (JarMetadataReader + ImportJarFilesAsync) — xem §4b; LD-07, LD-09 runtime UI UNVERIFIED
- UPD-1 cập nhật hàng loạt bằng hash | ĐÃ FIX (POST /version_files + CheckUpdatesByHashAsync) — xem §4b

## 4b. FIXED sau Bước 2/3 (30/10/2026) — code + unit test tự động

`dotnet test Vaz.sln` sau fix: **269/269 PASS** (baseline 238 + 31 mới), 0 stuck/0 skip/0 fail.

| Mã | Đã sửa gì | Bằng chứng |
|---|---|---|
| DL-4 | `ModInstallService`: `SanitizeModFileName` từ chối tên file API chứa `..`/path/ ký tự điều khiển → throw "unsafe filename from API", không download; `SanitizeModFileName`/`IsSafeModFileName` internal static | `ModDownloadGuardTests.cs` — traversal/pathed/Kontrol → null; Install unsafe-filename → throw + `DownloadAsync` Times.Never |
| DL-5 | `IsAllowedDownloadUrl`: chỉ https://cdn.modrinth.com (host case-insensitive); URL khác → throw trước khi tải | `ModDownloadGuardTests.cs` — cdn TRUE; http/other-host/relative → FALSE; Install non-cdn → throw "*not allowed*" + never download |
| DL-6 (trùng) | Trùng sha512 trong manifest → "Duplicate skipped", xóa `.partial`, không cài lại; `Sha512` của bản thật được lưu vào manifest (sha512 tính ra từ file hoặc từ API); name collision → `-1`/`-2` không ghi đè (đã có trước, vẫn PASS 24 test cũ) | install flow trong `ModInstallService.cs`; test cũ happy/collision vẫn pass (269 total) |
| LD-04/05/06 | `JarMetadataReader` mới: đọc fabric.mod.json/quilt.mod.json/META-INF/mods.toml/neoforge.mods.toml từ zip (đã cache key name+mtime+size); corrupt → null (không crash); WinUI thêm LoadWarning cho từng mod khi jar hỏng/ko metadata/loader mismatch | `JarMetadataReaderTests.cs` 9 case (zip thật: fabric/quilt/forge/neoforge/corrupt/no-meta/stream) |
| LD-08 | `ImportJarFilesAsync` mới: per-file try/catch (1 file hỏng khỏi bỏ dụng), copy → `.tmp-import` → sha512, tên trùng → `-1`/`-2` (không ghi đè), trùng hash → bỏ qua có tổng kết, manifest ghi atomic | code `InstanceDetailViewModel.cs`; runtime UI còn UNVERIFIED |
| UPD-1 | `IModrinthClient.GetVersionsFromHashesAsync` (POST /version_files) + `ModInstallService.CheckUpdatesByHashAsync` (chunk 100, hash từ manifest hoặc băm file trên đĩa, so Id/VersionNumber) + `InstanceDetailViewModel.CheckUpdatesAsync` dùng bulk | `ModInstallBulkUpdateTests.cs` 8 case: resolved thêm mới → HasUpdate; same Id/VersionNumber → false; hash mới không resolve → fallback; Source=custom loại; hash-disk từ file; 150 mods → đúng 2 CHUNK; hash trùng → gửi distinct |
| Synthetic URL | Xóa `SourceUrl = https://github.com/vazer/{slug}` + nút "Mã nguồn" giả; `Version` row hiện truly VersionId thay "1.0.0" | XAML/VM diff; sẽ chụp runtime ở B4 |

### Test tự động mới (Bước 3) — theo file

- `tests/Vazer.Core.Tests/JarMetadataReaderTests.cs` — 9 case (zip thật dựng bằng ZipArchive)
- `tests/Vazer.Core.Tests/ModDownloadGuardTests.cs` — 14 case (4 URL round + 3 Theory + 4 sanitize + 1 safe + 2 install блок)
- `tests/Vazer.Core.Tests/ModInstallBulkUpdateTests.cs` — 8 case (mock `POST /version_files`)
- Tổng mới: **31**; `dotnet test Vaz.sln` → Okay: Passed 269, Failed 0, Skipped 0 (run 30/10/2026, Duration 2s)

## 4c. RUNTIME UI EVIDENCE CHỤP THẬT TRÊN APP VAZ.WINUI (30/10/2026)

App chạy thật trên Windows desktop (`Vaz.WinUI.exe`, PID 10004), cửa sổ điều khiển qua Windows API + winarm, chụp bằng `scripts/capture-app-window.ps1` (SetWindowPos + CopyFromScreen crop đúng pixel). Đã kiểm thử trên 2 độ phân giải quy chuẩn 1025×533 và 960×530.

| Mã kịch bản | Mô tả kiểm thử runtime | Ảnh bằng chứng (1025×533) | Ảnh bằng chứng (960×530) | Kết quả quan sát |
|---|---|---|---|---|
| SM-01 (browse) | Mở ModBrowserDialog từ nút `+ Download Mods` | `docs/evidence/SM-01-browse-mods-1025x533.png` | `docs/evidence/SM-01-browse-mods-960x530.png` | Dialog mở mượt, AutoSuggestBox focused, nạp danh sách mod ban đầu từ Modrinth (Sodium, Iris, Lithium...) |
| SM-01 (search) | Gõ từ khóa `sodium` vào AutoSuggestBox | `docs/evidence/SM-01-search-sodium-1025x533.png` | `docs/evidence/SM-01-search-sodium-960x530.png` | Modrinth trả kết quả sodium, UI hiển thị các dòng `SearchHitRow` |
| SM-02 (no-results) | Tìm từ khóa không tồn tại `asdfghjkl_khong_co_mod_nay_9999` | `docs/evidence/SM-02-search-no-results-1025x533.png` | `docs/evidence/SM-02-search-no-results-960x530.png` | Danh sách rỗng, không sập app, không lỗi unhandled |
| SM-03 (vietnamese) | Gõ tiếng Việt có dấu `tối ưu hiệu năng` qua clipboard paste | `docs/evidence/SM-03-search-vietnamese-1025x533.png` | `docs/evidence/SM-03-search-vietnamese-960x530.png` | Không lỗi encode, chuỗi tiếng Việt truyền đúng, kết quả 0 hits an toàn |
| LD-01 / LD-04 / LD-05 / LD-06 | Load instance `rt-mods-test` (4 jars: 1 valid fabric, 1 forge mismatch, 1 corrupt zip, 1 no-meta) | `docs/evidence/LD-01-load-mods-rt-test-1025x533.png`, `docs/evidence/LD-04-inspect-row1-1025x533.png`, `docs/evidence/LD-06-forge-mismatch-and-valid-1025x533.png` | `docs/evidence/LD-06-forge-mismatch-and-valid-960x530.png` | Hiển thị 4/4 mods; JarMetadataReader đọc metadata an toàn; jar hỏng/sai loader không làm crash app, hiển thị đúng tên file và gắn warning |
| LD-03 (500+ mods) | Mở instance `rt-mods-500` chứa 520 fabric jars | `docs/evidence/LD-03-load-500-mods-1025x533.png` | `docs/evidence/LD-03-load-500-mods-960x530.png` | Thời gian load: **2698 ms**; UI không đứng (virtualized ListView); hiển thị danh sách đầy đủ |
| UPD-01 | Bấm nút `Check updates` trên toolbar | `docs/evidence/UPD-01-bulk-check-updates-1025x533.png` | `docs/evidence/UPD-01-bulk-check-updates-960x530.png` | Gọi `CheckUpdatesByHashAsync` bulk qua `POST /version_files`, thanh progress chạy, UI giữ responsive |

## 5. Báo cáo tỷ số (cập nhật sau Bước 4)

- **PASS (có bằng chứng thật)**:
  - API HTTP: SM-01..14 (14), HA-01..04 (4)
  - Unit tests: 269/269 pass (31 tests mới cho JarMetadataReader, ModDownloadGuard, ModInstallBulkUpdate)
  - Runtime UI screenshots (1025×533 + 960×530): SM-01 browse, SM-01 search, SM-02 no-result, SM-03 tiếng Việt, LD-01, LD-03 (500+ mods), LD-04/05/06 (corrupt/mismatch), UPD-01 bulk update check.
- **FIXED (code + tests)**:
  - DL-4 (path traversal guard)
  - DL-5 (allowlist cdn.modrinth.com)
  - DL-6-trùng (skip duplicate sha512)
  - LD-04/05/06 (JarMetadataReader multi-loader + corrupt resilience)
  - LD-08 (ImportJarFilesAsync safe staging + collision rename)
  - UPD-1 (bulk hash lookup POST /version_files)
  - Synthetic URL (xóa source url giả, hiện version thật)
- **UNVERIFIED (chưa chạy runtime hoặc cần mock đặc thù)**:
  - ER-1..4 (mất mạng / timeout 30s / HTTP 429 / HTTP 5xx — cần network proxy mock)
  - DP-1 (debounce rapid typing UI automation)
  - PH-2..3 (pagination scroll & sort dropdown UI automation)
  - DL-1..3 (tiến trình download / cancel / partial file UI automation)
  - LD-07 (toggle / delete mod khi game lock file)
  - LD-09 (.mrpack import dialog UI automation)
  - DEP-1..2 (dependency install prompt UI automation)
