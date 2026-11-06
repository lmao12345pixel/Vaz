# Đợt 18.7 Phase E — Java Compatibility (auto map Java theo MC version)

## 1. Tổng quan

**Mục tiêu:** app tự xác định Java tối thiểu cho từng phiên bản Minecraft (8 / 17 / 21) và
cảnh báo khi Java đang dùng không phù hợp — tránh silent failure khi chơi MC cũ bằng Java 21.

**Nguyên tắc đã áp dụng:**

| # | Nguyên tắc | Kết quả |
|---|---|---|
| 1 | Baseline 420 test → sau ≥ 426 | **442 pass**, 0 fail, 0 skip |
| 2 | Chỉ warning khi mismatch, KHÔNG tự tải Java | Đã bỏ auto-download trong `CmlGameLauncher`; chỉ log + notification, launch tiếp tục |
| 3 | Tách logic ra Core service | `JavaVersionMapper` + `IJavaVersionMapper` (DI), `JavaRequirements` model |
| 4 | 100% ThemeResource, không hard-code | UI mới dùng `ThemeResource`/`StaticResource`; xoá hardcode `DetectedJavaPath` và `"Minecraft 1.21.1"` trong Settings |
| 5 | Mỗi Việc 1 commit | 6 commit code + 1 commit docs |
| 6 | DỪNG nếu sửa file ngoài phạm vi | 2 file ngoài danh sách bắt buộc để nối Core → UI (disclose ở §3) |

**Thay đổi hành vi quan trọng:** trước đây `CmlGameLauncher` tự gọi `IJavaResolver.EnsureAsync`
(tải Adoptium) khi Java hiện tại thấp hơn yêu cầu. Theo nguyên tắc 2, đoạn này đã được thay
bằng kiểm tra + cảnh báo. `EnsureAsync` vẫn còn trong codebase và vẫn được dùng bởi
`ForgeInstaller`, `NeoForgeInstaller`, `MainViewModel` — chỉ luồng launch game không tự tải nữa.

## 2. Commits (đúng 1 commit / Việc)

| Việc | Commit | Nội dung |
|:---:|---|---|
| 1 | `74f9e56` | `feat(java): add JavaVersionMapper for MC version to Java mapping` |
| 2 | `7604a39` | `test(java): add unit tests for JavaVersionMapper` |
| 3 | `f61b722` | `feat(launcher): warn on Java version mismatch` |
| 4 | `db85cd3` | `feat(settings): add java version detection and selection` |
| 5 | `bc7e51c` | `refactor: remove hardcoded minecraft version references` |
| 6 | `4189c60` | `refactor(java): route launcher warning through JavaVersionMapper` (fix: dùng đúng instance mapper đã inject, để unit test chạy đúng path production) |
| — | *(commit docs)* | `docs(evidence): add Phase E Java compatibility report` |

## 3. Danh sách file (Tạo / Sửa)

**Tạo mới (đúng phạm vi):**
- `src/Vazer.Core/Java/JavaRequirements.cs` — model `JavaRequirements` (`Unknown = 0`, `IsKnown`,
  `IsMismatch`, `BuildMismatchMessage`) + interface `IJavaCompatibilityNotifier`.
- `src/Vazer.Core/Java/JavaVersionMapper.cs` — `IJavaVersionMapper` + `JavaVersionMapper`
  (parse semantic prefix, bỏ hậu tố pre/rc; snapshot/custom → `Unknown`; `WarnIfMismatch`).
- `tests/Vazer.Core.Tests/JavaVersionMapperTests.cs` — 22 test case mới.

**Sửa (đúng phạm vi):**
- `src/Vazer.Core/Java/JavaResolver.cs` — `RequiredJava` delegate sang `JavaVersionMapper`
  (Unknown → fallback 17 cho luồng chọn Java cũ); thêm scan `%ProgramFiles%\Microsoft\jdk-*`
  (top-level only, tránh quét Edge/Office).
- `src/Vazer.Core/Launch/CmlGameLauncher.cs` — inject `IJavaVersionMapper?` +
  `IJavaCompatibilityNotifier?`; probe Java khi `MajorVersion <= 0`; mismatch → log warning +
  notification, **không block**, **không auto-download**.
- `src/Vaz.WinUI/ViewModels/SettingsViewModel.cs` — inject `IJavaResolver?`; `DetectedJavaItem`;
  `DetectedJavaVersions`; `DetectJavaAsync` (scan thật, chạy ngoài UI thread); `SelectJava`;
  warning thiếu Java 21; xoá `DetectedJavaPath` hardcode + default `"Minecraft 1.21.1"`.
- `src/Vaz.WinUI/Views/SettingsPage.xaml` — InfoBar cảnh báo + bảng Java (Version | Path | Chọn),
  toàn bộ màu dùng `ThemeResource`.
- `src/Vaz.WinUI/Views/SettingsPage.xaml.cs` — `SelectJava_Click` (DataContext → command).
- `src/Vazer.Core/Extensions/MinecraftServiceCollectionExtensions.cs` — đăng ký
  `IJavaVersionMapper` → `JavaVersionMapper`.
- `tests/Vazer.Core.Tests/JavaResolverTests.cs` — sửa kỳ vọng `1.17.1`: 16 → 17 theo mapping mới
  (đây là test cũ khẳng định Java 16, trái với quy tắc Phase E "1.17–1.20.4 → Java 17").

**Ngoài danh sách PHẠM VI (bắt buộc, đã disclose):**
- `src/Vaz.WinUI/Services/JavaCompatibilityNotifier.cs` — adapter `IJavaCompatibilityNotifier`
  → `INotificationService`. Core không được tham chiếu WinUI nên cần bridge này để toast thật.
- `src/Vaz.WinUI/App.xaml.cs` — 2 dòng đăng ký adapter trong DI (không sửa logic khác).

**Không sửa (đúng yêu cầu):** `LoaderInstallers` (Fabric/Quilt/Forge/NeoForge/Vanilla),
`InstanceCreationService`, `InstanceMigrationService`.

## 4. Test & kiểm tra

| Mốc | Kết quả |
|---|---|
| Baseline trước đợt | **420/420** pass (`dotnet test Vaz.sln`) |
| Sau Phase E | **442/442** pass, 0 fail, 0 skip |
| Test mới | **+22 test case** (6 mapping bắt buộc + 6 boundary/suffix + 5 Unknown + 5 warning/requirements) |
| `check.ps1 -Scope static` | **PASSED** (build + test + agent config 12/12) |
| `dotnet build Vaz.sln` | **0 error** (warning IL2026/CS8620 pre-existing) |
| `dotnet build src/Vaz.WinUI` | **0 error**, 18 warning (pre-existing) |
| `check:web` | Không chạy — đợt này không sửa web prototype/contract (xem §8) |

**6 test case mapping bắt buộc:**

| MC version | Expected | Kết quả |
|---|:---:|:---:|
| `1.8.9` | 8 | ✅ PASS |
| `1.16.5` | 8 | ✅ PASS |
| `1.17` | 17 | ✅ PASS |
| `1.20.4` | 17 | ✅ PASS |
| `1.20.5` | 21 | ✅ PASS |
| `1.21.1` | 21 | ✅ PASS |
| `snapshot-24w14a` (phụ) | Unknown | ✅ PASS |

Case bổ sung: `1.17.1`, `1.18.2`, `1.19.4`, `1.20`, `1.21`, `1.21.1-pre1`, `24w14a`,
`custom-build`, rỗng/null → tất cả PASS.

**Kết quả Java detect trên máy chạy (theo đúng logic `DiscoverAsync`, lọc 8–21):**
6 `java.exe` tồn tại; sau lọc `MaxJavaVersion = 21` còn **4 bản**: Java 8 (Oracle jre1.8.0_491),
Java 8 (Oracle java8path), Java 17 (bundled Temurin), **Java 21 (bundled Temurin 21.0.12.1)**.
Hai bản Java 22 (`jdk-22`, `javapath`) bị loại bởi `MaxJavaVersion` — xem §6.

## 5. Bằng chứng (Evidence)

**Automated (PASS):**
```
dotnet test Vaz.sln
Passed! - Failed: 0, Passed: 442, Skipped: 0, Total: 442

check.ps1 -Scope static
STATIC CHECK PASSED: build + test + agent config
CHECK PASSED: static
```

**Runtime UI (BLOCKED — không đủ bằng chứng):**
- Ảnh Settings Java section: **BLOCKED**. Môi trường chạy lệnh chặn launch GUI
  (`Process.Start`/`Start-Process` bị security policy từ chối), nên không thể mở app để chụp.
- Video 30s (launch Fabric 1.21.1 + thử instance cũ): **BLOCKED** cùng lý do.
- Ảnh toast mismatch: **BLOCKED** cùng lý do.
- Theo quy tắc dự án: UI chưa automation được → **BLOCKED**, không đánh PASS.

**Manual test script (10 bước) cho user:**

1. `dotnet build Vaz.sln` rồi chạy `src\Vaz.WinUI\bin\x64\Debug\net8.0-windows10.0.26100.0\win-x64\Vaz.WinUI.exe`.
2. Mở **Settings → Java & RAM**.
3. Bấm **Dò Java**; chờ progress ring tắt.
4. Kiểm tra bảng: mỗi dòng có badge `Java 8/17/21`, full version + vendor, đường dẫn `java.exe`.
5. Nếu có Java 21: **không** thấy InfoBar cảnh báo; nếu không có: InfoBar vàng
   "Bạn chưa có Java 21 — cần cài để chơi MC 1.20.5+.".
6. Bấm **Chọn** ở dòng Java 21 → ô "Đường dẫn Java Runtime" đổi đúng path; bấm **Lưu**.
7. Tạo/ chọn instance **MC 1.8.9** (hoặc 1.16.5), giữ Java mặc định là 21.
8. Bấm **Chơi**.
9. Kỳ vọng: toast vàng "Java version không phù hợp" với nội dung
   "Instance … cần Java 8, nhưng app đang dùng Java 21. Có thể gây lỗi.".
10. Kỳ vọng: launch **không bị chặn** (không có dialog block, không tự tải Java); game có thể
    lỗi do Java sai — đó là quyết định của user.

## 6. Known issues / Hạn chế

1. **Java 22+ bị lọc khỏi detect** (`MinecraftOptions.MaxJavaVersion = 21`). Máy chỉ có Java 22
   sẽ hiện cảnh báo "chưa có Java 21" dù Java 22 chạy được MC 1.20.5+. Cần nâng `MaxJavaVersion`
   hoặc so sánh `>= required` thay vì `== required` (ngoài phạm vi đợt).
2. **Bỏ auto-download ở launch** là thay đổi hành vi có chủ đích theo nguyên tắc 2. Nếu user
   không có Java phù hợp, game sẽ fail thay vì tự cài — đúng yêu cầu "user tự cài".
3. **Message dùng `InstanceId`**, không phải tên hiển thị: `LaunchParameters` chưa có `Name`.
4. **Detect chỉ quét path phổ biến** (bundled, PATH, Java, Eclipse Adoptium, Microsoft JDK,
   LocalAppData Adoptium). Không đọc registry `JDK_HOME`/`JAVA_HOME`; Java cài ở ổ khác sẽ không thấy.
5. **Hardcode `1.21.1` còn 35 hit** ngoài `ModCatalogData`: 20 comment/doc, 11 demo/catalog/
   design-time data, **4 fallback/default thật** ngoài phạm vi
   (`InstanceDetailViewModel.MinecraftVersion`, `ResourcePacksShaders.CompatibilityText`,
   `WizardStep2Loader ?? "1.21.1"`, `CreateInstanceDialog ?? "1.21.1"`).
   Đã sửa hit trong `SettingsViewModel` (Việc 5).
6. `SystemSpecsProvider.Java("")` vẫn trả "Tự động (Java 21 LTS)" — text tĩnh, không phải logic
   chọn Java, nhưng nên đổi thành "Tự động" khi có dịp.

## 7. Đề xuất (sau đợt này)

1. Nâng `MaxJavaVersion` lên 25 và đổi `DiscoverAsync` filter thành `>= MinJavaVersion`.
2. Thêm `JAVA_HOME` + registry scan (`HKLM\SOFTWARE\JavaSoft\JDK`) để detect đủ hơn.
3. Đưa `InstanceName` vào `LaunchParameters` để message thân thiện hơn `InstanceId`.
4. Xoá 4 fallback `"1.21.1"` còn lại: thay bằng `string.Empty` + guard "chưa chọn version".
5. Cập nhật web prototype + contract `settings`/`java-compatibility` để giữ mô hình dual-target
   (đợt này task chỉ định app/core nên chưa làm).
6. Cân nhắc cho user override "vẫn chơi" / "tự tải Java" bằng setting rõ ràng.

## 8. Self-review & CHƯA KIỂM TRA

**Đã tự kiểm:**
- [x] Mapper là nguồn logic duy nhất; `JavaResolver.RequiredJava` delegate, không còn 2 bảng mapping.
- [x] Không auto-download ở `LaunchAsync`; không block launch.
- [x] `Unknown`/snapshot không cảnh báo (có test).
- [x] Java thực tế `<= 0` không cảnh báo (tránh false positive).
- [x] UI dùng `ThemeResource`/`StaticResource`, không hard-code màu.
- [x] Không sửa `LoaderInstallers`, `InstanceCreationService`, `InstanceMigrationService`.
- [x] Build Core + WinUI 0 error; test 442/442; static check PASSED.
- [x] Không commit secret/token.

**CHƯA KIỂM TRA (BLOCKED / MANUAL_REQUIRED):**
- [ ] Runtime Settings Java section (ảnh) — BLOCKED do môi trường chặn launch GUI.
- [ ] Runtime toast mismatch (ảnh/video 30s) — BLOCKED cùng lý do.
- [ ] Launch thật instance 1.8.9/1.16.5 bằng Java 21 — MANUAL_REQUIRED (script 10 bước §5).
- [ ] Parity web ↔ app cho Settings Java section — chưa làm (ngoài phạm vi task).
