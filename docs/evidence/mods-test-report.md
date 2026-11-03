# Mods Test Report — TÌM MOD & LOAD MOD (runtime thật)

Task riêng, không ảnh hưởng PARITY contract khác. Mỗi dòng: mã | kịch bản | cách tái hiện | kết quả | mức độ | bằng chứng.
Chưa chạy được → ghi `UNVERIFIED`, không bỏ qua.
Logs: `.agent/mods-test-raw.txt`, `.agent/mods-hash-raw.txt`.

---

## 1. Kịch bản kế hoạch

- **TÌM MOD**: SM-01..14 (từ khóa Modrinth), DP (debounce/reorder), PH (pagination/sort), ER (mất mạng/timeout/429/5xx + retry), DL (download: progress/cancel/partial/sha512/traversal/cdn/thay thế/trùng), DEP (phụ thuộc/release).
- **LOAD MOD**: LD-01 rỗng, LD-02 1 mod, LD-03 500+ (đo ms), LD-04 jar hỏng, LD-05 jar không metadata, LD-06 trùng id/loader sai/phiên bản MC sai (kèm hậu quả), LD-07 toggle/uninstall, LD-08 .jar picker/drag-drop, LD-09 .mrpack (zip-slip, hash, cdn host), UPD (bulk hash update POST /version_files/update).

---

## 2. Kết quả RUNTIME THẬT — API Modrinth Search qua Script (30/10/2026)

> **Lưu ý**: Các test SM-01..14 dưới đây là kiểm tra trực tiếp giao thức HTTP API của Modrinth bằng PowerShell script (`scripts/test-modrinth-search.ps1`), không phải kiểm thử code giao diện ứng dụng.

Cách chạy: `powershell -NoProfile -ExecutionPolicy Bypass -File scripts/test-modrinth-search.ps1` → gửi HTTP GET trực tiếp tới `api.modrinth.com/v2/search`, Header User-Agent `Vazer/1.0.0 (+https://vazer.app)`, facets lồng `[[...]]`.

| Mã | Kịch bản | Kết quả API | Mức độ | Bằng chứng |
|---|---|---|---|---|
| SM-01 | từ khóa có kết quả (`sodium`) | HTTP 200, hits=20, first=sodium, 3287ms | — | log raw |
| SM-02 | từ khóa không có kết quả | HTTP 200, hits=0 | Đã xử lý empty-state | log raw |
| SM-03 | tiếng Việt có dấu `tối ưu hiệu năng` | HTTP 200, hits=0 (URL escape UTF-8 an toàn) | — | log raw |
| SM-04 | ký tự đặc biệt `a%b&c=d+f` | HTTP 200, hits=0 | — | log raw |
| SM-05 | từ khóa 500 ký tự | HTTP 200, hits=0, không lỗi 4xx/414 | — | log raw |
| SM-06 | facets loader `categories:fabric` | HTTP 200, hits=20 | — | log raw |
| SM-07 | facets phiên bản `versions:1.21.1` | HTTP 200, hits=20 | — | log raw |
| SM-08 | query rỗng | HTTP 200, hits=20 (hành vi đúng khi mở màn hình duyệt mod ban đầu; không phải lỗi) | — | log raw |
| SM-09 | phân trang offset=100 | HTTP 200, hits=8 (phân trang hoạt động) | — | log raw |
| SM-10 | offset=100000 (vượt tổng) | HTTP 200, hits=0 | — | log raw |
| SM-11 | `index=downloads` | HTTP 200 (sắp xếp hoạt động) | — | log raw |
| SM-12 | `limit=100` (giới hạn tối đa) | HTTP 200, hits=100 | — | log raw |
| SM-13 | `limit=101` (> tối đa) | HTTP 200 nhưng server trả hits=100 (client tự clamp 1..100) | LOW | log raw |
| SM-14 | `limit=0` | HTTP 200, server trả hits=1 (client clamp an toàn) | LOW | log raw |

**Phát hiện từ script**: Facet dạng phẳng `["project_type:mod"]` bị Modrinth trả HTTP 400; facet mảng lồng `[["project_type:mod"]]` trả HTTP 200. Hàm C# `ModrinthClient.BuildFacets` đã triển khai dạng mảng lồng — PASS.

---

## 3. Kết quả RUNTIME THẬT — Download, Hash Lookup & Bulk Update API (30/10/2026)

Cách chạy:
1. `powershell scripts/test-modrinth-hash.ps1`
2. `powershell scripts/test-modrinth-update.ps1`

| Mã | Kịch bản | Kết quả API | Mức độ | Bằng chứng |
|---|---|---|---|---|
| HA-01 | GET versions → primary file | 200; file=sodium-fabric-0.8.13+mc1.21.1.jar; sha512=128 hex; host=cdn.modrinth.com | — | log raw |
| HA-02 | Tải file thật + so sha512 | Size=1574609B, sha512 khớp tuyệt đối với API | — | log raw |
| HA-03 | `POST /version_files` (bulk) 1 hash thật | HTTP 200, resolved=1, version_number map đúng | — | log raw |
| HA-04 | hash giả → `POST /version_files` | HTTP 200 body `{}` rỗng (chuẩn an toàn) | — | log raw |
| UPD-API | `POST /version_files/update` Sodium cũ | Đưa sha512 của Sodium 0.6.0 (`f7b9...`), loader `fabric`, game_version `1.21.1` → API trả về bản mới Sodium 0.8.13 (`SMxNOGZ6`) | — | `scripts/test-modrinth-update.ps1` log |

---

## 4. Danh sách đã sửa (FIXED) — Code + Unit Test tự động

Toàn bộ test suite giải pháp: **297/297 PASS** (0 lỗi, 0 skip).

| Mã | Nội dung đã sửa | Chi tiết kỹ thuật | Bằng chứng kiểm thử |
|---|---|---|---|
| DL-4 | Path Traversal Guard | `SanitizeModFileName` từ chối tên file chứa `..`, path separator, ký tự điều khiển. Ném ngoại lệ, không tải. | `ModDownloadGuardTests.cs` (14 tests) |
| DL-5 | Allowlist cdn.modrinth.com | `IsAllowedDownloadUrl` chặn mọi URL ngoài `https://cdn.modrinth.com`. Redirect ra ngoài cũng bị chặn. | `ModDownloadGuardTests.cs`, `ModNetworkAndDependencyTests.cs` |
| DL-6 | Trùng hash / nội dung | So khớp sha512 trước khi ghi đè; trùng hash trong manifest thì bỏ qua và xóa `.partial`. | `ModInstallServiceTests.cs`, `ModNetworkAndDependencyTests.cs` |
| DL-Stream | Stream trực tiếp vào `.partial` | `NetworkResponseStream` truyền tải trực tiếp từ network stream ra file đĩa, không buffer toàn bộ vào `MemoryStream`. Xóa sạch `.partial` khi lỗi hoặc hủy. | `ModNetworkAndDependencyTests.cs` |
| DL-Cancel | Hủy tải có CancellationToken | ViewModel hỗ trợ `CancelInstallCommand`; UI có nút "Hủy" cạnh thanh tiến trình. | `ModBrowserViewModel.cs`, `ModNetworkAndDependencyTests.cs` |
| Mrpack | Guard host & partial cleanup | `MrpackImporter` kiểm tra `IsAllowedDownloadUrl` và dọn file dở dang khi tải thất bại. | `ModNetworkAndDependencyTests.cs` |
| LD-04/05 | JarMetadataReader đa nền tảng | Đọc `fabric.mod.json`, `quilt.mod.json`, `mods.toml`, `neoforge.mods.toml`. Jar hỏng trả về `null` không crash. | `JarMetadataReaderTests.cs` (20 tests) |
| LD-06 | Cảnh báo trùng mod id & sai MC version | Kiểm tra trùng mod ID giữa các jar và so khớp phiên bản Minecraft instance (`IsMinecraftVersionCompatible`). Cảnh báo nêu rõ hậu quả crash. | `JarMetadataReaderTests.cs`, screenshots runtime |
| LD-03 | Đọc metadata nền + Cache ra đĩa | Toàn bộ quét file và trích xuất metadata chạy trên `Task.Run`. Lưu cache vào `.jar-metadata-cache.json`. Lần 2 mở trong **104 ms** (mục tiêu < 1s). | Đo đạc benchmark trên instance 520 mods |
| UPD-01 | Bulk Update `POST /version_files/update` | Gửi sha512 hàng loạt kèm loader và MC version. Tự động kiểm tra cả mod custom nếu trùng hash Modrinth. | `ModInstallBulkUpdateTests.cs` (8 tests), screenshots runtime |
| SM-08 | Empty search & gợi ý tiếng Anh & badge | Cho phép tìm rỗng khi duyệt; khi 0 kết quả hiện gợi ý từ khóa tiếng Anh; mod đã cài có badge "Đã cài". | Screenshots runtime |
| UI URL | Dọn dẹp URL giả lập | Bỏ `SourceUrl` giả mạo (`github.com/vazer/...`); hiển thị `VersionId` thật từ metadata. | Code review, runtime screenshots |

---

## 5. Bảng bằng chứng chụp thật trên WinUI App (1025×533 & 960×530)

Chụp bằng `scripts/capture-app-window.ps1` trên `Vaz.WinUI` chạy thật:

| Mã kịch bản | Mô tả kiểm thử | Ảnh 1025×533 | Ảnh 960×530 | Kết quả quan sát |
|---|---|---|---|---|
| SM-01 (browse) | Mở dialog tải mod | `docs/evidence/SM-01-browse-mods-1025x533.png` | `docs/evidence/SM-01-browse-mods-960x530.png` | Dialog mở mượt, danh sách nạp ban đầu |
| SM-01 (search) | Gõ từ khóa `sodium` | `docs/evidence/SM-01-search-sodium-1025x533.png` | `docs/evidence/SM-01-search-sodium-960x530.png` | Kết quả tìm kiếm Sodium xuất hiện |
| SM-02 (no-results) | Tìm từ khóa không tồn tại | `docs/evidence/SM-02-search-no-results-1025x533.png` | `docs/evidence/SM-02-search-no-results-960x530.png` | Không crash, danh sách rỗng an toàn |
| SM-03 (tiếng Việt) | Gõ từ khóa có dấu tiếng Việt | `docs/evidence/SM-03-search-vietnamese-1025x533.png` | `docs/evidence/SM-03-search-vietnamese-960x530.png` | Encode UTF-8 chuẩn, không lỗi |
| SM-08 (hint) | Gợi ý khi 0 kết quả | `docs/evidence/SM-08-no-results-hint-1025x533.png` | `docs/evidence/SM-08-no-results-hint-960x530.png` | Hiện gợi ý "Thử từ khóa tiếng Anh..." |
| SM-08 (badge) | Badge "Đã cài" | `docs/evidence/SM-08-installed-badge-1025x533.png` | `docs/evidence/SM-08-installed-badge-960x530.png` | Badge xanh lá "Đã cài" hiển thị bên cạnh mod đã có |
| LD-01 | Nạp instance test `rt-mods-test` | `docs/evidence/LD-01-load-mods-rt-test-1025x533.png` | `docs/evidence/LD-01-load-mods-rt-test-960x530.png` | Nạp đủ danh sách mod |
| LD-03 (500+ mods) | Đo tốc độ 520 mods | `docs/evidence/LD-03-load-500-mods-1025x533.png` | `docs/evidence/LD-03-load-500-mods-960x530.png` | Nạp nền, UI ảo hóa mượt mà (650ms lạnh / 104ms ấm) |
| LD-04 (jar hỏng) | Cảnh báo jar hỏng | `docs/evidence/LD-04-corrupt-warning-1025x533.png` | `docs/evidence/LD-04-corrupt-jar-960x530.png` | Cảnh báo cam `⚠ Jar hỏng hoặc không có metadata...` |
| LD-06 (mismatch) | Cảnh báo sai loader | `docs/evidence/LD-06-forge-mismatch-and-valid-1025x533.png` | `docs/evidence/LD-06-forge-mismatch-and-valid-960x530.png` | Cảnh báo `Loader của jar là 'forge' nhưng instance đang dùng 'fabric'` |
| LD-06 (duplicate) | Cảnh báo trùng mod ID | `docs/evidence/LD-06-duplicate-mod-id-1025x533.png` | `docs/evidence/LD-06-duplicate-mod-id-960x530.png` | Cảnh báo kèm hậu quả: `DuplicateModException` |
| LD-06 (mc version) | Cảnh báo sai phiên bản MC | `docs/evidence/LD-06-mc-mismatch-1025x533.png` | `docs/evidence/LD-06-mc-mismatch-960x530.png` | Cảnh báo kèm hậu quả: `ModResolutionException` |
| UPD-01 (bulk) | Kiểm tra cập nhật hàng loạt | `docs/evidence/UPD-01-bulk-check-updates-1025x533.png` | `docs/evidence/UPD-01-bulk-check-updates-960x530.png` | Thanh tiến trình cập nhật chạy mượt |
| UPD-01 (update) | Có bản cập nhật cho Sodium cũ | `docs/evidence/UPD-01-update-available-1025x533.png` | `docs/evidence/UPD-01-update-available-960x530.png` | InfoBar hiện rõ: "1 mod có bản mới." |

---

## 6. Đo lường hiệu năng LD-03 (500+ mods)

- **Môi trường đo**: Instance `rt-mods-500` chứa 520 file `.jar` Fabric thực tế (nén cấu trúc zip chuẩn có `fabric.mod.json`).
- **Cách đo**: Đặt `Stopwatch` đo thời gian từ khi gọi `LoadAsync` đến khi toàn bộ mod được parse, hash và nạp vào danh sách hiển thị.
- **Kết quả**:
  - **Lần nạp đầu (Cold load)**: **650 ms** — Mở từng zip, parse JSON, tính toán cảnh báo và tuần tự hóa cache ghi ra đĩa.
  - **Lần nạp thứ hai (Warm load)**: **104 ms** — Đọc trực tiếp từ file đĩa `.jar-metadata-cache.json` (kiểm tra `lastWriteTime` + `fileSize`). Tốc độ tăng gần 6.5 lần, đạt tiêu chuẩn khắt khe < 1 giây.
- **Xác nhận UI Thread**: Việc quét file và đọc metadata được bọc hoàn toàn trong `await Task.Run(...)`, không block UI thread của WinUI.

---

## 7. Phân loại kịch bản còn UNVERIFIED & Bàn giao thủ công

Các kịch bản sau chưa thể mô phỏng hoàn toàn tự động vì cần sự can thiệp phần cứng hoặc hành động vật lý của người dùng. Các mục này đã được đưa vào `docs/evidence/mods-manual-checklist.md`:

1. **MC-SM-01 (Kéo thả file jar từ Explorer)**: Kéo thả tệp tin từ ngoài màn hình Windows Explorer vào vùng nhận của WinUI.
2. **MC-SM-02 (Hủy tải mod giữa chừng bằng tay)**: Người dùng bấm Cancel trên giao diện khi đang tải tệp lớn.
3. **MC-SM-03 (Ngắt kết nối mạng vật lý)**: Rút cáp mạng / ngắt Wi-Fi khi đang truyền luồng file từ server.
4. **MC-SM-04 (Cài đặt gói .mrpack qua hộp thoại FilePicker)**: Chọn file .mrpack từ ổ đĩa thật.
5. **MC-LD-01 (Khóa file khi game đang chạy)**: Bật/tắt mod khi process game đang chiếm dụng file jar.
6. **MC-LD-02 (Hộp thoại xác nhận xóa mod)**: Xác nhận xóa mod qua dialog tương tác.
7. **MC-LD-04 (HiDPI scaling 125%/150%)**: Đổi scale màn hình Windows thật để kiểm tra độ sắc nét.

---

## 8. Đợt 1 Commit 4 — Search & Safe Download Hardening

- **SM-08 (Empty query)**: Khi query rỗng hoặc toàn khoảng trắng, trả ngay `SearchResults.Empty` mà không gửi HTTP request đến Modrinth API. Đã kiểm chứng qua unit test `SearchAsync_EmptyOrWhitespaceQuery_ReturnsEmptyWithoutHttpCall`.
- **SM-13/14 (Limit clamp)**: Kẹp giới hạn kết quả `Limit = Math.Clamp(limit, 1, 100)` trong `SearchQuery` và `ModrinthClient.SearchAsync`. Đã kiểm chứng qua unit tests `SearchQuery_Limit_ClampedBetween1And100` và `SearchAsync_Limit_ClampedTo100`.
- **ER-1..4 (ModrinthErrorMapper & Auto-retry)**:
  - `ModrinthErrorMapper`: chuẩn hóa các lỗi Network (HttpRequestException), Timeout (TaskCanceledException), Rate-limit (429 với `Retry-After`), Server error (5xx).
  - Tự động thử lại: 0 lần cho lỗi 429 (ném ngay `ModrinthRateLimitException` kèm Retry-After), 1 lần cho lỗi Timeout / 5xx. Đã kiểm chứng qua các unit test retry và mapper.
- **DP-1 (Debounce 300ms & Keystrokes cancel)**: Gõ nhanh liên tục hủy CancellationToken cũ, chỉ chạy query cuối cùng. Đã kiểm chứng qua unit test `DP1_RapidTyping_CancelsPreviousQueries_OnlyLastCompletes`.
- **PH-2/3 (PaginationState)**: Quản lý trạng thái phân trang `{ Offset, Limit, HasMore, Total, IsLoadingMore }` cùng các chỉ mục sắp xếp (`relevance`, `downloads`, `follows`, `newest`, `updated`). Đã kiểm chứng qua unit tests `PaginationState_AdvancesOffsetCorrectly` và `PaginationState_HandlesEmptyResults`.
- **DL-1..3 (Tải an toàn qua file tạm hệ thống)**:
  - Tải vào file tạm hệ thống `Path.Combine(Path.GetTempPath(), $"vazer-{Guid.NewGuid():N}.tmp")`.
  - Kiểm tra SHA512 trên file tạm TRƯỚC KHI gọi `File.Move` vào thư mục `mods/`.
  - Hủy giữa chừng (CancellationToken) hoặc lỗi tải / sai SHA512 lập tức xóa file tạm, file đích không được tạo. Tiến trình báo đầy đủ số byte và %. Đã kiểm chứng qua các unit tests `DL_CancelledDownload_CleansUpTempFileImmediately`, `DL_FailedSha512_CleansUpPartialFile`, và `DL_ProgressReports_BytesAndPercent`.
- **DEP (Dependencies bắt buộc vs tùy chọn)**:
  - Thêm CheckBox "Tải kèm dependencies" (hai chiều với `ViewModel.IncludeDependencies`) trên giao diện `ModBrowserDialog`.
  - Dependency bắt buộc mặc định chọn bản release mới nhất tương thích MC + loader. Khi bỏ chọn checkbox, không tải kèm dependency. Đã kiểm chứng qua unit test `DEP_UncheckedDependencies_DoesNotInstallRequiredDep`.
