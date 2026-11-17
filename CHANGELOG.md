# Changelog

All notable changes to V.A.Z Launcher are documented here.
Format follows [Keep a Changelog](https://keepachangelog.com/en/1.1.0/); versions follow SemVer.

## [0.19.0] — 2026-11-10

Major release consolidating Minecraft Vibe UI, Performance optimizations, Resource Pack & Shader Manifest persistence, unified Install UX feedback, and critical stability fixes.

> **Note on Visual UX:** Visual UX verification has not been performed manually on the release candidate — please report any visual or interaction defects via GitHub Issues.

### Added
- **Resource Packs & Shaders Manifest Persistence (P1.2a/b/c)**
  - Dedicated `resourcepacks.json` and `shaderpacks.json` metadata manifests per instance.
  - Automatic migration for existing instances with legacy zip files (`EnsureManifestWithMigrationAsync`).
  - Offline local thumbnail caching (`thumbnails/<type>-<slug>.jpg`) with remote CDN fallback.
  - Persistent "Installed" state tracking matching Modrinth project ID/slug across application restarts.
- **Unified Install UX Feedback (P1.3)**
  - Real-time download `ProgressBar` with percentage and cancel command in `ModrinthCatalogView` for Resource Packs.
  - Rich-media Toast notifications with gallery/cover image fallback for Shaders (`InstallNotificationHelper`).
  - Customized toast ready messages per item type (`"Shader đã sẵn sàng..."`, `"Resource Pack đã sẵn sàng..."`).
- **Minecraft Vibe UI (Đợt 21)**
  - Authentic Minecraft Emerald theme tokens, card shine sweeps, micro-interactions and GPU tier gating.

### Changed
- **DI & Nullability Audit (C2.1, C2.2)**
  - Re-audited nullable injection points across services; documented intentional optional fallbacks.
  - Added XML documentation for nullable parameters in `MsalAuthProvider`, `ForgeInstaller`, `NeoForgeInstaller`, and `OptifineInstallerService`.
  - Migrated DTOs to C# 11 `required` properties where runtime JSON validation is active.

### Fixed
- **UI Block & Hit-Test Overlay on Install (P0.2)**
  - Added proactive `IsHitTestVisible = false` and 300ms fallback auto-collapse for `ModrinthBrowserHostGrid` and `ModBrowserHostGrid` to prevent transparent overlay click blocks.
  - Eliminated `.GetAwaiter().GetResult()` from `ResourcePacksShaders.cs` by converting to fully asynchronous `LoadResourcePacksCoreAsync` and `LoadShadersCoreAsync`, preventing sync-over-async dispatcher deadlocks.
  - Added `.ConfigureAwait(false)` throughout `PackManifestService` I/O operations.
- **Application Auto-Close 45–60s (P0.1)**
  - Fixed `COMException 0x800F1000` in `PressScale.cs` caused by accumulated anonymous event handlers and button unload during pointer dispatch (`e98b18f`).
- **Performance & Resource Cleanup (Đợt 22)**
  - Automated log rotation for `crash.txt` when exceeding 1MB (maximum 5 backups, `65d2474`).
  - LRU eviction cap of 200 entries for `_jarMetaCache` (`da74651`).
  - BitmapImage memory cleanup in `NotificationToast` on element unload (`f3607b2`).

## [0.18.0] — 2026-11-06 — "Multi-Version"

First packaged release of the Multi-Version line: a full instance-creation wizard,
five mod loaders, in-place version changes, installed-version management, and
automatic Java compatibility handling.

### Added

- **Multi-version foundation (18.1 / 18.2)**
  - `ILoaderInstaller` Strategy + `IInstanceCreationService` Facade + shared
    `InstallerProgress` model ([report](docs/evidence/dot18/refactor-installer-report.md)).
  - `MinecraftVersionService` with Mojang manifest, Fabric/Quilt/NeoForge/Forge
    metadata sources, semantic loader-version comparison, nested payload parsing and
    24h cache ([report](docs/evidence/dot18/multi-version-support-report.md)).
  - Full Minecraft version dropdown (no `Take(10)` cap), corrupt `settings.json`
    recovery with atomic rewrite + `.bak`, automated UIA verification
    ([18.2c](docs/evidence/dot18/dot18.2c-report.md)).
- **Create Instance Wizard (18.3)**
  - Step 1: 3-column version cards with real covers, release/snapshot badges,
    search, "Đã cài" marker and selection animation
    ([report](docs/evidence/dot18/wizard-step1-2-report.md)).
  - Step 2: Vanilla / Fabric / Quilt / NeoForge / Forge loader cards with version
    dropdown, preview panel and transitions.
  - Step 3: confirm card, animated gradient, default name, progress indicator and
    success state ([report](docs/evidence/dot18/wizard-step3-report.md)).
- **Forge support (18.4)** — headless `ForgeInstaller` for MC ≥ 1.13, Forge modpack
  (`.mrpack`) installation, Forge version metadata and proactive UI blocking for
  unsupported legacy versions ([report](docs/evidence/dot18/forge-installer-report.md)).
- **Change Version (18.5)** — in-place MC version + loader change with `instance.json` /
  `mods.json` backup, keep-or-delete mods choice, in-wizard warning overlay and
  rollback on failure/cancel ([report](docs/evidence/dot18/phase-c-change-version-report.md)).
- **Version Management (18.6)** — Settings "Phiên bản" tab: installed-version scan,
  disk usage stats, orphan cleanup and delete confirmation
  ([report](docs/evidence/dot18/phase-d-version-management-report.md)).
- **Java Compatibility (18.7 / 18.7b / 18.7c)**
  - `JavaVersionMapper` maps MC versions to Java 8 / 17 / 21; snapshots/custom
    versions resolve to `Unknown` and never warn.
  - Non-blocking launch warning on Java mismatch (no auto-download).
  - Settings Java section: real installed-JDK detection (bundled, PATH, Adoptium,
    Microsoft JDK), version/path table, "Chọn" action and missing-Java-21 warning.
  - `MaxJavaVersion` 25 (Java 22–25 detected); `IsMismatch` relaxed so newer Java
    on MC 1.20.5+ is accepted; hardcoded `1.21.1` fallbacks replaced by `Unknown`
    + wizard guards ([report](docs/evidence/dot18/phase-e-java-compat-report.md)).
- **Quality Gate P1 (19.1)** — root `.editorconfig`, `Directory.Build.props`
  (`AnalysisLevel=latest`, `EnforceCodeStyleInBuild`), CI `MIN_TESTS` 460, and a
  clean warning baseline ([report](docs/evidence/dot19/quality-gate-p1-report.md)).

### Changed

- Version identity bumped to **0.18.0** (`app.manifest`, `Vaz.WinUI.csproj`,
  Settings version label, update-check strings, diagnostic report).
- Launch no longer auto-downloads Java; users install/select Java themselves.
- `JavaResolver.RequiredJava` now delegates to the single `JavaVersionMapper` source.

### Fixed

- WinUI nested `ContentDialog` warning replaced by in-wizard overlay.
- NeoForge installer exit code 1 with a clearer error message; corrupt settings
  double-escape bug; CmlLib version resolution by launch version name.
- 18 build warnings resolved (CS8602/CS8619/CS8620/CS1998/IL2026) — 0 warnings in
  Debug and Release.
- Four hardcoded `1.21.1` fallbacks that could silently mislabel instances.

### Verification

| Gate | Result |
|---|---|
| `dotnet test Vaz.sln` | **463/463 pass**, 0 fail, 0 skip |
| `dotnet build -c Debug / -c Release` | **0 warning, 0 error** |
| `check.ps1 -Scope static` | **PASSED** (build + test + agent config 12/12) |
| CI test floor | `MIN_TESTS = 460` |

### Known issues

1. **Intel HD 4600 / driver 2016** — Forge/Fabric game exits silently after GL/Mixin
   negotiation on this GPU; cannot verify "vào menu" on this machine
   ([18.4 §5](docs/evidence/dot18/forge-installer-report.md)).
2. **Legacy Forge ≤ 1.12.2 (incl. 1.8.9)** — artifacts are not on
   `maven.minecraftforge.net`; install is proactively blocked in UI and guarded in
   the installer. Supporting it needs a separate legacy Maven source.
3. **`instance.json.bak` / `mods.json.bak`** — intentionally kept after a successful
   Change Version; no automatic cleanup yet.
4. **Runtime UI evidence for Java section** — screenshots/video blocked in the build
   environment (GUI launch restricted); covered by a 10-step manual script.
5. **IL2026 suppression** — `MinecraftVersionService` uses reflection-based JSON;
   suppressed with justification, source-gen migration planned for 19.2.

### Evidence

Full reports, 175 screenshots and 20 videos live under
[`docs/evidence/`](docs/evidence/), notably
[`docs/evidence/dot18/`](docs/evidence/dot18/) and
[`docs/evidence/dot19/`](docs/evidence/dot19/).

[0.18.0]: https://github.com/lmao12345pixel/Vaz/releases/tag/v0.18.0
