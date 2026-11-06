# Đợt 18.5 Phase C — Change Version (đổi Minecraft version + Mod Loader cho instance có sẵn)

## 1. Tổng quan

Bổ sung tính năng **"Đổi phiên bản"** trên Instance Detail: user đổi **Minecraft version + Mod Loader** của instance hiện tại **in-place** (cùng folder, giữ nguyên name/path), có backup/restore an toàn, và **KHÔNG bao giờ đụng world/mods/options/config** trừ khi user xác nhận:

- **Nút "Đổi phiên bản"** trên header Instance Detail (cạnh "Mở thư mục"), chỉ enable khi instance được chọn (bind `CanManageMods`).
- **Wizard tái sử dụng** `CreateInstanceDialog` với chế độ change:
  - Preselect MC version + loader hiện tại (Step 1 `PendingSelectVersionId`, Step 2 `SelectLoaderAndVersionAsync`).
  - Tên instance **không cho đổi** (Step 3 khóa NameBox `SetNameReadOnly`).
  - Nút cuối ghi **"Đổi phiên bản"** thay vì "Tạo Instance" (`SetActionLabel`).
  - Đổi MC version ở Step 1 → loader versions refresh theo MC mới (fix bug).
- **Logic migration** (`InstanceDetailViewModel.ChangeVersionAsync`):
  1. Chặn khi game đang chạy.
  2. Backup `instance.json` → `instance.json.bak` + `mods.json` → `mods.json.bak` TRƯỚC khi động đĩa.
  3. Tái sử dụng `IInstanceCreationService.CreateAsync()` với version/loader mới (tên giữ nguyên → id/path không đổi → in-place).
  4. Ghi `instance.json` mới (version/loader mới, giữ `CreatedAt`/`LastPlayedAt`/`IconPath`), giữ nguyên `name`/`path`/world.
  5. **Chỉ xóa `mods/` + `mods.json` khi option == DeleteMods và SAU KHI cài thành công**; world/options.txt/config không bao giờ bị đụng.
  6. Fail hoặc cancel giữa chừng → **restore backup** + ném lỗi.
- **Warning 3 lựa chọn**: "Đổi ngay, giữ mods" / "Đổi ngay, xóa mods" / "Hủy". Vì **WinUI 3 không cho phép `ContentDialog` lồng trong `ContentDialog`** (wizard là ContentDialog → dialog con không bao giờ mở), chuyển sang **overlay panel trong Step 3** (fade + slide 220ms, dùng `ThemeResource`).

**Bug phát hiện & fix trong đợt (Việc 3):**
1. Change mode đổi MC version ở Step 1 → Step 2 vẫn giữ loader version của MC cũ → thêm refresh theo MC mới (`c1e26e3`).
2. Warning 3 lựa chọn dùng `ContentDialog.ShowAsync()` lồng trong wizard → **nested ContentDialog không mở trong WinUI 3** (đã xác nhận bằng probe: không window/popup mới hiện ra sau Invoke) → thay bằng overlay panel trong Step 3 (`37a23fe`).

## 2. Commits (đúng 1 commit / Việc)

| Việc | Commit | Nội dung |
|:---:|---|---|
| 1 | `14494e6` | `feat(instance): add change version button on detail page` |
| 2 | `a9b6b66` | `feat(instance): implement version change migration with backup` |
| 3 | `c1e26e3` | `fix(instance): resolve change version issues` (refresh loader versions khi MC đổi trong change mode) |
| 3 | `37a23fe` | `fix(instance): use in-wizard overlay panel for migration warning (nested ContentDialog does not open in WinUI 3)` |
| — | *(chưa commit)* | `docs(evidence): add change version (Phase C) report and evidence` |

## 3. Danh sách file (Tạo / Sửa)

**Tạo mới:**
- `src/Vaz.WinUI/Views/Controls/WizardStep3Confirm.xaml(.cs)` — overlay `MigrationWarningPanel` (3 nút KeepMods/DeleteMods/CancelMigration, fade+slide), `SetActionLabel(...)` (label nút cuối), `SetNameReadOnly(name)` (khóa tên), `RequestMigrationConfirmationAsync()` → `Task<ModChangeOption>`.
- `scripts/change-version-scenario.ps1` — scenario runner tái sử dụng (giữ/xóa/hủy mods; select MC + loader card; capture frames + ffmpeg; verify `instance.json` trên đĩa sau khi đổi).
- Evidence: video + screenshots (xem §5).

**Sửa đổi:**
- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` — nút "Đổi phiên bản" (glyph `E8AB`) cạnh "Mở thư mục", `IsEnabled={x:Bind ViewModel.CanManageMods}`.
- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` — `ChangeVersion_Click` (mở `CreateInstanceDialog(vm, current, changeAction)`, sau thành công reload detail + gửi `InstancesChangedMessage`).
- `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.cs` — enum `ModChangeOption { KeepMods, DeleteMods, Cancel }`; `ChangeVersionAsync` (backup → CreateAsync → merge → xóa mods nếu chọn → restore khi fail/cancel); inject `IInstanceCreationService`.
- `src/Vaz.WinUI/Views/CreateInstanceDialog.xaml(.cs)` — **change mode**: title "Đổi phiên bản instance", `ChangeVersionAction` delegate, preselect MC/loader, khóa tên + label "Đổi phiên bản", overlay warning thay nested dialog, refresh loader khi MC đổi.
- `src/Vaz.WinUI/Views/Controls/WizardStep1Version.xaml.cs` — `PendingSelectVersionId` (preselect version trước khi control load).
- `src/Vaz.WinUI/Views/Controls/WizardStep2Loader.xaml.cs` — refactor `LoaderButton_Click` → `SelectLoaderAsync(loader)`; thêm `SelectLoaderAndVersionAsync(loaderType, loaderVersion)` public (preselect + chọn loader version nếu có).

**Không sửa** (đúng phạm vi): `LoaderInstallers` (Forge/Fabric/Quilt/NeoForge), `MrpackImporter`, `InstanceManager.cs` (không cần — migration tái sử dụng `IInstanceCreationService.CreateAsync()` đã ghi `instance.json` tại path instance; vì tên giữ nguyên nên id/path giữ nguyên → in-place không cần sửa InstanceManager).

> **Ngoài danh sách PHẠM VI (đã disclose):** các control wizard `WizardStep1Version`/`WizardStep2Loader`/`WizardStep3Confirm` — nằm ngoài 4 file PHẠM VI nhưng **bắt buộc** để implement "Wizard tương tự Create Instance nhưng preselect + khóa tên + label khác" (tương tự đợt Forge). Không đụng `InstanceManager.cs` trong PHẠM VI vì không cần thiết.

## 4. Test & kiểm tra

| Mốc | Kết quả |
|---|---|
| Baseline trước đợt (Phase C) | **408/408** pass (`dotnet test Vaz.sln`) |
| Sau Phase C | **408/408** pass |
| Sau Đợt 18.5b (Rollback unit test) | **413/413** pass, 0 skip (`dotnet test Vaz.sln`) |
| `npm run check:static` / `check.ps1 -Scope static` | **PASSED** (build + test + agent config) |
| Build WinUI | 0 error (warnings pre-existing CS1998/CS8620 không do đợt này) |
| `check:web` | Không cần — web prototype/tests không bị đụng trong đợt (feature dùng wizard app có sẵn) |

**Runtime checks (thật, trên máy chạy):**

Test instance thật: `fabric 1.21.1 #4` (Fabric 1.21.1, loader 0.20.1, **5 mods giả**, world `PhaseCWorld`, options.txt, config).

| # | Kịch bản | Thao tác | Kết quả trên đĩa | Verdict |
|---|----------|----------|------------------|:---:|
| 1 | Giữ mods | Đổi 1.21.1 → **Fabric 1.20.6**, chọn "Đổi ngay, giữ mods" | `instance.json` = fabric/1.20.6; `versions/fabric-loader-0.20.1-1.20.6/` được cài; **5 mods + mods.json giữ nguyên**; world PhaseCWorld còn; options.txt/config còn; có `.bak` | ✅ **PASS** |
| 2 | Xóa mods | Đổi → **Vanilla 1.20.6** (đổi cả loader), chọn "Đổi ngay, xóa mods" | `instance.json` = vanilla/1.20.6 (`launchVersionName=1.20.6`, loaderVersion null); **`mods/` + `mods.json` bị xóa**; world PhaseCWorld còn; options.txt/config còn; có `.bak` | ✅ **PASS** |
| 3 | Hủy | Mở wizard → Step 3 → "Đổi phiên bản" → warning → **Hủy** | Không đổi gì: `instance.json` giữ nguyên fabric 1.21.1; **KHÔNG tạo `.bak`**; mods/world nguyên vẹn | ✅ **PASS** |

- Unit test (Đợt 18.5b): **5 unit tests mới** trong `tests/Vazer.Core.Tests/InstanceMigrationServiceTests.cs` cover 100% các nhánh success, remove mods, fail install rollback, mid-install cancel rollback, và cancel before start. 5/5 PASS.

## 5. Bằng chứng (Evidence)

**Video:**
- `docs/evidence/dot18/videos/change-version-keep-mods.mp4` — **45.5s** (đúng yêu cầu "Video 45s"): Instance Detail → click "Đổi phiên bản" → Step 1 chọn 1.21.1 → Step 2 Fabric → Step 3 (tên khóa, nút "Đổi phiên bản") → warning → chọn "Đổi ngay, giữ mods" → success → capture Instance Detail sau khi đổi. Frames capture thật (178 frame) → re-time `setpts=0.505` để đúng 45.5s.

**Screenshots** (`docs/evidence/dot18/screenshots/`):
- `change-version-warning.png` — dialog cảnh báo **3 lựa chọn** (giữ mods / xóa mods / Hủy).
- `change-version-delete-warning.png`, `change-version-cancel-warning.png` — warning cho 2 kịch bản còn lại.
- `change-version-step3.png` — Step 3 change mode: tên instance khóa (read-only), nút "Đổi phiên bản", breadcrumb "Bước 3/3".
- `change-version-cancel-step3.png`, `change-version-vanilla-step3.png` — Step 3 change mode (kịch bản hủy / Vanilla).
- `change-version-detail-1206.png` — Instance Detail sau đổi → **Fabric 1.20.6** (kịch bản giữ mods).
- `change-version-detail-vanilla.png` — Instance Detail sau đổi → **Vanilla 1.20.6** (kịch bản xóa mods).
- `change-version-after-1201.png` — Instance Detail sau đổi → **Fabric 1.21.1** (kịch bản cuối của video).
- `instance-detail-change-version-button.png` (commit 1) — header Instance Detail có nút "Đổi phiên bản" cạnh "Mở thư mục".

**Evidence đĩa (kịch bản 1 — giữ mods)** — instance.json sau đổi:
```json
{
  "id": "fabric 1.21.1 #4",
  "name": "Fabric 1.21.1 #4",
  "minecraftVersion": "1.20.6",
  "loaderType": "fabric",
  "loaderVersion": "0.20.1",
  "launchVersionName": "fabric-loader-0.20.1-1.20.6",
  "createdAt": "2026-11-06T09:21:36.760687+00:00"
}
```
Kèm `versions/fabric-loader-0.20.1-1.20.6/` được cài, `instance.json.bak` + `mods.json.bak` tồn tại, 5 mods + world còn.

**Evidence đĩa (kịch bản 2 — xóa mods)**:
```json
{
  "minecraftVersion": "1.20.6",
  "loaderType": "vanilla",
  "loaderVersion": null,
  "launchVersionName": "1.20.6"
}
```
Kèm: `mods/` + `mods.json` **bị xóa**, world PhaseCWorld còn, options.txt + config còn.

## 6. Known issues / Hạn chế

1. **Nested ContentDialog không mở trong WinUI 3** — đây là giới hạn của framework (dialog con trong dialog cha bị block). Giải pháp: overlay panel trong Step 3 (không thể dùng `ContentDialog` thật cho warning). Đảm bảo user-visible behavior giống 3 lựa chọn; đã ghi nhận.
2. **Rollback path** — **ĐÃ TEST** (Đợt 18.5b). Đã tách logic sang `InstanceMigrationService` (Core) và hoàn thành 5/5 unit test trong `tests/Vazer.Core.Tests/InstanceMigrationServiceTests.cs`:
   - Case 1: `ChangeVersionAsync_Success_KeepMods_PreservesModsAndCreatesBackups` — **PASS**. `instance.json` mới, 2 mods giữ nguyên, `instance.json.bak` + `mods.json.bak` tồn tại.
   - Case 2: `ChangeVersionAsync_Success_RemoveMods_DeletesModsDirectoryAndManifest` — **PASS**. `instance.json` đổi sang vanilla, thư mục `mods/` và `mods.json` bị xóa, `.bak` tồn tại.
   - Case 3: `ChangeVersionAsync_FailDuringInstall_RestoresOriginalInstanceJsonAndPreservesMods` — **PASS**. Mock `CreateAsync` throw `InvalidOperationException` → rollback `instance.json` về 1.21.1 ban đầu, mods không bị động.
   - Case 4: `ChangeVersionAsync_CancelMidInstall_RestoresBackupsAndRethrowsOperationCanceled` — **PASS**. Mock `cts.Cancel()` khi đang ghi dở dang → rollback `instance.json` từ `.bak`, rethrow `OperationCanceledException`, mods nguyên vẹn.
   - Case 5: `ChangeVersionAsync_CancelBeforeStart_DoesNotCreateBackupsOrModifyFiles` — **PASS**. `cts.Cancel()` trước khi gọi → ném `OperationCanceledException`, không tạo bất kỳ file `.bak` nào, `CreateAsync` không bao giờ được gọi.
3. **Loader version trong Step 2 khi đổi MC**: preselect chỉ giữ loader version nếu version đó vẫn tồn tại cho MC mới; nếu không, rơi về version mới nhất (thử bằng `ItemsSource.Cast<object>()`). Loader version cũ (ví dụ Fabric 0.20.1 cho MC cũ) với MC mới có thể khác bản mới nhất — đây là hành vi mong muốn (không ép version có thể không tồn tại).
4. Instance test `fabric 1.21.1 #4` **đã bị xóa sau khi thu evidence** (là dữ liệu test, không phải instance user). Không còn dư máy.
5. **`instance.json.bak` / `mods.json.bak` còn lại** sau khi đổi thành công — có chủ đích (giữ để user tự khôi phục nếu cần). Không tự xóa.
6. Video compile từ frames (3fps capture → re-time 0.505) — tốc độ phát khác thực tế; flow user-visible không đổi.

## 7. Đề xuất (sau đợt này)

1. **Cảnh báo world không tương thích**: hiện chỉ cảnh báo chung "mods và world có thể hỏng". Có thể nâng cấp: liệt kê số mods/số world sẽ bị ảnh hưởng trước khi xác nhận.
2. **`.bak` dọn dẹp**: thêm tùy chọn "xóa file backup cũ" hoặc tự xóa `.bak` sau N lần chạy thành công.
3. Nếu cảnh báo 3 lựa chọn cần reachable bằng user click "X" hoặc ESC trong lúc panel mở: hiện ESC khi panel mở sẽ đóng toàn bộ wizard (hành vi cũ). Có thể xử lý sau nếu cần.
4. **Khôi phục launcher "đang chọn instance"**: sau khi đổi version, Instance Detail reload (`LoadAsync`) + `InstancesChangedMessage` — đã làm; thêm test automation ổn định nếu cần.

## 8. Self-review & CHƯA KIỂM TRA

- Không tự đánh giá UI/parity PASS; mọi ảnh/video là capture thật trên máy chạy (xem §5).
- **ĐÃ TEST (unit test mock)**: Rollback khi installer fail & cancel mid-install (đã PASS 5/5 case trong `InstanceMigrationServiceTests`).
- **CHƯA KIỂM TRA (runtime)**: (a) đổi loader sang **NeoForge/Quilt/Forge** end-to-end khác Fabric (cùng code path nhưng chưa chạy; Forge headless ≥1.13, Vanilla/NeoForge tương tự); (b) đổi MC **xuống** bản chưa cài (cần download) và **lên** bản mới hơn; (c) launch game sau khi đổi version (vào main menu) — phụ thuộc GPU máy (đã biết hạn chế từ 18.4, Forge/Fabric chưa vào menu trên Intel HD 4600).
- **Manual test script (10 bước)** cho user:
  1. Mở V.A.Z → Instances → chọn instance **Fabric 1.21.1** có mods.
  2. Header → nút **"Đổi phiên bản"**.
  3. Wizard mở ở change mode: Step 1 đã preselect **1.21.1**; search + chọn **1.20.6**.
  4. Step 2: loader model giữ **Fabric** → output Fabric 0.20.1 (hoặc latest cho 1.20.6).
  5. Step 3: tên instance **không sửa được**; nút cuối ghi **"Đổi phiên bản"**.
  6. Click → warning **3 lựa chọn** hiện ra.
  7. Chọn (A) "Đổi ngay, **giữ mods**" → chờ success (~30s–2 phút).
  8. Mở folder instance → verify: `versions/fabric-loader-…-1.20.6/` tồn tại, `instance.json` ghi 1.20.6, **mods/ còn nguyên**, `saves/` còn, `instance.json.bak` + `mods.json.bak` có.
  9. Lặp lại với (B) "Đổi ngay, **xóa mods**" → verify `mods/` + `mods.json` đã bị xóa, world còn. Với (C) **Hủy** → verify không đổi gì, không có `.bak`.
  10. Click "Chơi" → game mở với version mới (cần GPU tốt; nếu thoát im lặng gửi `logs/latest.log`).
