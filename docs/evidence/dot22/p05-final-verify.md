# P0.5 Final Verify — ProgressBar + Manifest + Commit Evidence

Ngày: 2026-11-11 · Đợt: P0.5/P0.5b · Repo: `C:\OptizimeProject\Vaz` · Branch: `feat/browse-popular-mods`
Người thực hiện: Raez (agent) · Người duyệt: Master

---

## Việc 1 — ProgressBar V-A3: CODE VERIFIED, Visual INCONCLUSIVE (không phải bug)

XAML `ModrinthCatalogView.xaml` dòng 224–229: ProgressBar determinate (`IsIndeterminate="False"`, `Maximum="100"`, `Height="4"`), `Value` bind OneWay vào `ViewModel.ProgressPercent`, màu `ModrinthAccentBrush` (#1BD96A).

Code-behind `ModBrowserViewModel.cs`:
- Dòng 330: `[ObservableProperty] private int progressPercent;` → source generator phát `PropertyChanged`, binding OneWay được cập nhật.
- Dòng 886: reset `= 0` khi bắt đầu; dòng 897: `ProgressPercent = p.PercentComplete` trong `Progress<InstallProgress>` callback, dispatch UI thread qua `App.Dispatcher.TryEnqueue`; dòng 923: `= 100` khi thành công; dòng 1059–1060 (`finally`): reset `IsBusy=false`, `ProgressPercent=0`.
- Luồng Performance Pack (dòng 1027) cùng pattern.

Visual INCONCLUSIVE vì: download 22.8MB xong <3s, thanh cao 4px, `finally` reset về 0 ngay khi hoàn tất → khó bắt kịp trong screenshot. Phân tích pixel `04-rp-progress.png`: 1398 px #1BD96A nhưng phân bố thành **badge Modrinth tĩnh + viền card** (tồn tại y hệt trong `02-toast.png`, không có trong `00/01/03/05`) — không phải fill của progress bar. Kết luận: **code verified, không phải bug**; Master chốt bằng mắt khi cần (30s nhìn `04-rp-progress.png`).

## Việc 2 — Manifest #8 vs #10: **BUG (data loss) — đã xác nhận, đã fix ở Việc 5**

Path thật: `%APPDATA%\Vazer\Minecraft\instances` (prompt ghi thiếu `Minecraft`).

### Số liệu (installedAt so từng tick với mtime file zip)

Instance `fabric 26.3 #8` (mtime manifest 11/11 07:47:49):

| fileName | slug | thumb | installedAt vs zip mtime | Kết luận |
|---|---|---|---|---|
| FreshAnimations_v1.11.2_26.1+.zip | null | null | EXACT-TICK | Legacy, migration tạo → OK (không có thumbnail mồ côi trên đĩa) |
| MakeUp-UltraFast-9.6.zip | null | null | EXACT-TICK | Legacy, migration tạo → OK |

Instance `fabric 26.3 #10` RP (mtime manifest 11/11 08:27:31): 9 entry — 7 entry Modrinth đủ slug+thumb (delta 0.12–3.12s, đúng flow), 1 legacy EXACT-TICK OK (Fullbright, không thumbnail mồ côi), và **1 BUG**:

| fileName | slug | thumb | installedAt vs zip mtime | Bằng chứng |
|---|---|---|---|---|
| **LowOnFire v26.2§8.zip** | **null** | **null** | **EXACT-TICK (−0.68s≈0 tick)** | File `thumbnails/resourcepack-low-on-fire.jpg` (2073B, mtime 11/11 06:53 — **cùng phút** với zip) tồn tại mồ côi |

Instance `fabric 26.3 #10` SHADER (mtime 11/11 06:57:22): 4 entry — 3 Modrinth đủ metadata (delta 0.41–0.85s), 1 legacy EXACT-TICK OK (Solas, không thumbnail mồ côi).

### Lập luận BUG (không thể là legacy)

1. `resourcepack-low-on-fire.jpg` **chỉ có thể** do `DownloadThumbnailAsync` tạo (định danh `{type}-{slug}.jpg`, PackManifest.cs dòng 288). Slug `low-on-fire` **không suy ra được** từ tên file `LowOnFire v26.2§8` (chứa ký tự §, có số version) → pack này **đã cài qua Modrinth thành công** lúc 06:53 (P0.4 session), entry đầy đủ đã được ghi qua `AddOrUpdateEntryAsync`.
2. Entry hiện tại có `installedAt` khớp **từng tick** với mtime zip — chữ ký của migration (`InstalledAt = fi.LastWriteTimeUtc`), vì flow Modrinth gán `UtcNow` **sau** khi tải thumbnail (delta luôn dương ≥0.12s như 8 entry kia).
3. Timeline: lifecycle.log ghi app launch 06:44:32; LowOnFire cài ~06:53; **app relaunch 07:26:19** → `EnsureManifestWithMigrationAsync` chạy lại. Giữa 2 thời điểm, entry đầy đủ đã bị thay bằng entry rỗng của migration → **metadata bị mất khi re-migrate**.
4. Cơ chế: `LoadAsync`/deserialize thất bại (file bị lock thoáng qua bởi `File.Move` khi hai luồng/tab cùng ghi — phiên 06:44–07:26 có nhiều thao tác nặng) bị code cũ coi là "manifest rỗng" → migration rebuild toàn bộ từ zip với `Slug=null`.

**Kết luận Việc 2: BUG — entry `LowOnFire v26.2§8.zip` (cài 2026-11-11 06:53, SAU P1.2 2c6b001 ngày 2026-11-09) bị mất slug/thumbnailLocal sau restart.** Các entry legacy trước P1.2 (#8 ×2, Fullbright, Solas) rỗng là **hành vi dự kiến**, không phải bug. → Chuyển Việc 5.

## Việc 3 — Commit evidence P0.4: **PASS**

- Commit `21978a7` "docs(evidence): P0.4 windows-mcp GUI verification report + 6 screenshots" (11/11 08:55 +0700), là HEAD~1 của branch.
- `git show --stat`: đủ 6 PNG (00-launch 163442B, 01-overlay-fix 176673B, 02-toast 428971B, 03-preview-after-restart 125127B, 04-rp-progress 399331B, 05-b5-regression 117897B) + BAO_CAO_P04_MCP_GUI_TEST.md (107 dòng).
- Blob PNG trong git **byte-for-byte MATCH** file trên đĩa (verify bằng `git show 21978a7:<path>` + PIL verify cả 6 blob mở được, đúng 1366×768).
- `Test-Path` báo cáo: True. `git ls-files`: 7 file tracked.

## Việc 5 — Fix P0.5b: `fix(packs): preserve manifest metadata on re-migration` — commit `fde15dc`

Thay đổi trong `src/Vazer.Core/Packs/PackManifest.cs` (3 lớp phòng vệ):

1. **`ReadManifestWithRetryAsync`** — đọc manifest retry 3 lần (60ms) khi gặp `IOException` (lock thoáng qua). Chỉ JSON hỏng thật mới bị coi là corrupt. Đây là fix gốc: lock ngắn không còn kích hoạt rebuild mất metadata.
2. **`BackupCorruptManifest`** — khi corrupt thật, copy thành `<file>.corrupt-<timestamp>.bak` trước khi ghi đè (trước đây dữ liệu bị đè mất không cứu được).
3. **`TryRecoverFromOrphanThumbnail`** — khi migration vẫn phải tạo entry mới cho zip lạ, quét `thumbnails/<type>-<slug>.jpg` mồ côi có mtime trong khoảng [zip−5s, zip+15ph] và slug chưa entry nào dùng → khôi phục `slug` + `thumbnailLocal`. Áp dụng cho cả 2 nhánh (có manifest và chưa có manifest).

Không đổi: UI, DI Core, hành vi `AddOrUpdateEntryAsync`/`SaveAsync`/`DownloadThumbnailAsync`.

Test mới (+5) trong `PackManifestServiceTests.cs`:
- `ReMigration_PreservesSlugAndThumbnail_OfExistingEntries`
- `ReMigration_AfterManifestLoss_RecoversSlugFromOrphanThumbnail` (tái hiện đúng ca LowOnFire)
- `ReMigration_CorruptManifest_BackupsFile_AndRecoversFromThumbnail`
- `ReMigration_DoesNotStealThumbnail_AlreadyUsedByAnotherEntry`
- `ReMigration_OldThumbnail_BeyondTolerance_NotAttached`

Kết quả đo:
- `dotnet test tests/Vazer.Core.Tests`: **498 passed / 0 failed / 0 skipped** (gate ≥493 khi fix: đạt; mốc cũ 442 của đợt 18.7b + các test đợt 22 + 5 test mới).
- `dotnet build Vazer.Core` và `Vaz.WinUI`: **0 warning / 0 error**.

## Việc 4 — Kết luận tổng

| Việc | Kết quả | Căn cứ |
|---|---|---|
| 1 ProgressBar | CODE VERIFIED, visual INCONCLUSIVE, không phải bug | XAML 224–229 + VM 330/886/897/923/1027/1060 + pixel analysis |
| 2 Manifest #8 vs #10 | **BUG data loss** (LowOnFire) — legacy entries OK | EXACT-TICK + orphan thumbnail 06:53 + lifecycle relaunch 07:26 |
| 3 Commit P0.4 | PASS | `21978a7`, 7 file tracked, blob MATCH |
| 5 Fix | DONE | `fde15dc`, 498 test pass, 0 warning |
| 4 Báo cáo này | DONE | commit docs(evidence) kèm theo |

**Khuyến nghị: Sẵn sàng tag v0.19.0** SAU khi Master xác nhận 2 điểm tồn dư:

1. **Dữ liệu LowOnFire đã mất không tự khôi phục** trong manifest #10 hiện tại (entry đã tồn tại nên recovery không chạy lại). Fix mới chỉ **chống mất về sau**. Nếu muốn khôi phục entry này, cần 1 thao tác nhỏ (sửa tay JSON hoặc lệnh riêng) — chờ Master quyết, Raez không tự sửa dữ liệu người dùng ngoài phạm vi.
2. Visual ProgressBar (`04-rp-progress.png`) và 2 ảnh còn lại của P0.4 chờ Master 30s confirm theo luật (Raez không tự ghi PASS phần hình ảnh).

Rủi ro còn lại: `ReadManifestWithRetryAsync` bắt mọi `Exception` không phải IO/Cancel là corrupt — hành vi giống code cũ (không tệ hơn), và nay có backup nên an toàn hơn.

## Bằng chứng kèm theo

- Script phân tích pixel: `docs/evidence/dot22/mcp-verify/p05_pixel_analysis.py` (chạy lại được: `python p05_pixel_analysis.py`).
- Ảnh P0.4: `docs/evidence/dot22/mcp-verify/00..05-*.png` (commit `21978a7`).
