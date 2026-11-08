# DI Audit — C2.1 Planning

**Ngày:** 2026-11-09  
**HEAD:** `46b3a89` (v0.19.0-rc1)  
**Test:** 463/463 pass | Warning: 0 (Debug + Release)  

---

## Tổng quan

| Metric | Giá trị |
|---|---|
| Tổng số chỗ DI null-related | **16** (không phải 91 như ước lượng cũ) |
| `= null!` (suppress nullable) | 4 |
| `private readonly T? _field` (nullable service injection) | 12 |
| Phân bố Nhóm A (Core services) | 7 |
| Phân bố Nhóm B (VM có lifecycle) | 3 |
| Phân bố Nhóm C (WinUI code-behind / singleton) | 6 |

> **Lý do lệch 91 → 16:** Ước lượng 91 có thể đếm tất cả nullable fields (bao gồm `[ObservableProperty]`, local variables, non-DI fields). Audit này chỉ đếm DI-related patterns: `= null!` và nullable service injection (`private readonly T? _`).

---

## Bảng Nhóm A — Core services (refactor target cho C2.2)

| # | File | Line | Field | Type | Nullable? | Test cover | Risk | Batch |
|---|---|---|---|---|---|---|---|---|
| 1 | Vazer.Core/Auth/MsalAuthProvider.cs | 28 | `_msal` | `IMsalClientWrapper?` | YES | Có (auth tests) | Thấp | 1 |
| 2 | Vazer.Core/Loaders/ForgeInstaller.cs | 39 | `_javaResolver` | `IJavaResolver?` | YES | Có (loader tests) | Thấp | 1 |
| 3 | Vazer.Core/Loaders/NeoForgeInstaller.cs | 28 | `_javaResolver` | `IJavaResolver?` | YES | Có (loader tests) | Thấp | 1 |
| 4 | Vazer.Core/Loaders/Optifine/OptifineInstallerService.cs | 20 | `_modrinthClient` | `IModrinthClient?` | YES | Có (optifine tests) | Thấp | 1 |
| 5 | Vazer.Core/Launch/CmlGameLauncher.cs | 79 | `_javaCompatibilityNotifier` | `IJavaCompatibilityNotifier?` | YES | Có (launch tests) | TB | 2 |
| 6 | Vazer.Core/Http/FileDownloader.cs | 85 | `_httpClientFactory` | `IHttpClientFactory?` | YES | Có (download tests) | TB | 2 |
| 7 | Vazer.Core/Mods/Models.cs | 52 | `CurrentMod` | `InstalledMod` (= null!) | NO | N/A (model) | Thấp | Skip |

### Giải thích risk:
- **Thấp:** Service optional by design (null check trước khi dùng), test cover path null + non-null.
- **TB:** Service nên luôn có nhưng mark nullable để tránh warning. Refactor cần verify DI registration.
- **Skip:** Model property, không phải DI injection.

---

## Bảng Nhóm B — VM có lifecycle (giữ nguyên per scope C2)

| # | File | Line | Field | Type | Lý do giữ nguyên |
|---|---|---|---|---|---|
| 1 | ViewModels/InstanceDetailViewModel.cs | 159 | `_notificationService` | `INotificationService?` | VM nhóm B, rủi ro binding break |
| 2 | ViewModels/SettingsViewModel.cs | 56 | `_versionService` | `IVersionManagementService?` | VM nhóm B |
| 3 | ViewModels/SettingsViewModel.cs | 57 | `_javaResolver` | `IJavaResolver?` | VM nhóm B |

---

## Bảng Nhóm C — WinUI code-behind / singleton (defer)

| # | File | Line | Field | Type | Lý do defer |
|---|---|---|---|---|---|
| 1 | App.xaml.cs | 21 | `Dispatcher` | `DispatcherQueue` (= null!) | Static singleton, set trong OnLaunched |
| 2 | App.xaml.cs | 24 | `MainWindow` | `Window` (= null!) | Static singleton, set trong OnLaunched |
| 3 | MainWindow.xaml.cs | 79 | `_subclassProc` | `SUBCLASSPROC?` | Win32 interop, nullable by design |
| 4 | Views/CreateInstanceDialog.xaml.cs | 41 | `_changeInstance` | `MinecraftInstance?` | Optional constructor param |
| 5 | Views/CreateInstanceDialog.xaml.cs | 42 | `_changeAction` | `ChangeVersionAction?` | Optional constructor param |
| 6 | ModBrowserViewModel.cs | 90 | `Hit` | `SearchHit` (= null!) | Inner class init pattern |

---

## Đề xuất C2.2 — Batch refactor

### Batch 1 (an toàn nhất, 4 services):
1. `MsalAuthProvider._msal` — optional MSAL wrapper, null check đã có
2. `ForgeInstaller._javaResolver` — optional Java detection
3. `NeoForgeInstaller._javaResolver` — tương tự Forge
4. `OptifineInstallerService._modrinthClient` — optional Modrinth lookup

**Ước lượng:** 4 files, ~2h, test existing cover.

### Batch 2 (cần verify DI registration, 2 services):
5. `CmlGameLauncher._javaCompatibilityNotifier` — verify registered trong DI container
6. `FileDownloader._httpClientFactory` — verify factory pattern

**Ước lượng:** 2 files, ~1h, cần integration test.

### Skip:
- `Models.cs:52 CurrentMod = null!` — model init pattern, không phải DI
- Tất cả Nhóm B (VM) — ngoài scope C2
- Tất cả Nhóm C (singleton/code-behind) — defer

---

## Phát hiện ngoài scope

1. **Nullable warnings suppressed bằng `= null!`:** 4 chỗ. Không gây runtime error nhưng che giấu potential null reference. Nên refactor sang proper initialization hoặc `required` keyword (.NET 7+).

2. **Optional services pattern:** 7/12 nullable injections là intentional (service optional by design). Refactor nên giữ nullable nhưng thêm null guard rõ ràng thay vì suppress warning.

---

## Gate nghiệm thu

- [x] Test: 463 → 463 (không đổi)
- [x] Warning: 0 Debug + Release (không đổi)
- [x] File docs/refactor/di-audit.md tồn tại
- [ ] Commit: pending Master review
