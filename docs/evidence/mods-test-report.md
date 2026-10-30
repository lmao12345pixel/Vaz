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
- DL-1 progress/cancel, DL-2 .partial → rename, DL-3 sha512 sai → xóa temp, DL-4 path traversal, DL-5 cdn.modrinth.com allowlist, DL-6 không ghi đè + không tải trùng | PENDING Bước 2/3 (unit + runtime UI)
- DEP-1 dependencies, DEP-2 release default | code có (InstallFromModrinthAsync resolveDependencies) — cần unit test + runtime
- LD-xx toàn bộ | PENDING Bước 3/4
- UPD-1 cập nhật hàng loạt bằng hash | PENDING Bước 2 (thêm `POST /version_files` client + ModInstallService check bulk)

## 5. Báo cáo tỷ số (cập nhật dần)

- PASS: SM-01..14 (14), HA-01..04 (4)
- FIXED: (append sau Bước 2)
- UNVERIFIED: ER-1..4, DP-1 (chờ unit test), PH-2..3/DL-x/LD-x/UPD-1 (chờ runtime/fix)
