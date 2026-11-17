# Đợt 19.1 — Quality Gate P1 (editorconfig + analyzer + warning baseline)

## 1. Tổng quan

**Mục tiêu:** dựng 3 lớp phòng ngự chất lượng tối thiểu để chặn regression trong PR tương lai:
quy ước code (`.editorconfig`), analyzer build-time (`Directory.Build.props`), và baseline
build sạch warning (0 warning) + nâng sàn test CI.

**Kết quả:** 463/463 test pass (không giảm), **18 → 0 warning** ở cả Debug và Release,
`check:static` PASSED, `MIN_TESTS` 320 → 460.

**Nguyên tắc đã giữ:**
- Không sửa logic nghiệp vụ; chỉ nullability/async signature + config.
- Không sửa test file.
- Không bật `TreatWarningsAsErrors` toàn bộ (để 19.2).

## 2. Trạng thái từng việc

| Việc | Nội dung | Trạng thái | Commit |
|:---:|---|:---:|---|
| 1 | Tạo `.editorconfig` root | ✅ DONE | `ea604d2` |
| 2 | Tạo `Directory.Build.props` (AnalysisLevel latest, EnforceCodeStyleInBuild) | ✅ DONE | `e33b087` |
| 3 | Nâng `MIN_TESTS` 320 → 460 | ✅ DONE | `8083562` |
| 4 | Fix toàn bộ 18 warning (baseline sạch) | ✅ DONE | `bb61010` |
| 5 | Verify test + build + static check | ✅ DONE | *(docs commit)* |

## 3. Danh sách file

**Tạo mới:**
- `.editorconfig` — charset/indent/final-newline/trim; style C# mức `suggestion`;
  `CS4014` + `VSTHRD110` mức `warning`.
- `Directory.Build.props` — `LangVersion=latest`, `Nullable=enable`,
  `AnalysisLevel=latest`, `EnforceCodeStyleInBuild=true`; **không** bật
  `TreatWarningsAsErrors`.
- `docs/evidence/dot19/quality-gate-p1-report.md` — báo cáo này.

**Sửa:**
- `.github/workflows/ci.yml` — `MIN_TESTS: '320'` → `'460'`.
- `src/Vaz.WinUI/Views/Controls/WizardStep2Loader.xaml.cs` — null-safe `Application.Current?.Resources`.
- `src/Vaz.WinUI/Views/Controls/WizardStep3Confirm.xaml.cs` — null-safe resource lookup + throw có message.
- `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` — async lambda cho `ChangeVersionAction`
  (`Task<MinecraftInstance>` → `Task<MinecraftInstance?>`).
- `src/Vazer.Core/Loaders/FabricInstaller.cs` — lọc entry thiếu `Version` trước `SelectLatestStable`.
- `src/Vazer.Core/Loaders/QuiltInstaller.cs` — như trên.
- `src/Vazer.Core/Versions/MinecraftVersionService.cs` — `ClearCacheAsync` bỏ `async` +
  `Task.FromResult`; `UnconditionalSuppressMessage(IL2026)` có justification.

**Không sửa:** test file, `LoaderHelpers.cs` (đã thử đổi signature nhưng revert vì làm phát
sinh CS8620 trong test — giữ nguyên API), logic nghiệp vụ.

## 4. Test + warning (trước → sau)

| Mốc | Trước 19.1 | Sau 19.1 |
|---|---|---|
| `dotnet test Vaz.sln` | 463/463 pass | **463/463 pass**, 0 fail/skip |
| `dotnet build -c Debug` | 18 warning | **0 warning**, 0 error |
| `dotnet build -c Release` | 18 warning | **0 warning**, 0 error |
| `check:static` | PASSED | **PASSED** |
| CI `MIN_TESTS` | 320 | **460** |

**Phân loại warning và cách xử lý** (18 theo MSBuild summary; 14 vị trí source duy nhất;
36 dòng log thô do multi-target + message wrap):

| Code | Vị trí | Cách xử lý |
|---|:---:|---|
| CS8602 | 3 | Null-safe `Application.Current?.Resources` (WizardStep2Loader ×2, WizardStep3Confirm ×1) |
| CS8619 | 1 | Async lambda target-typed `Task<MinecraftInstance?>` (InstanceDetailPage) |
| CS8620 | 2 | Lọc `Version` null/rỗng tại call site (Fabric/Quilt installer) |
| CS1998 | 1 | Bỏ `async`, trả `Task.FromResult` (MinecraftVersionService.ClearCacheAsync) |
| IL2026 | 7 | `UnconditionalSuppressMessage` + justification (xem §6) |

## 5. Bằng chứng

**Build sạch (Release, no-incremental):**
```
dotnet build Vaz.sln -c Release --no-incremental
    0 Warning(s)
    0 Error(s)
```

**Build sạch (Debug, no-incremental):**
```
dotnet build Vaz.sln -c Debug --no-incremental
    0 Warning(s)
    0 Error(s)
```

**Test (Release, no-build):**
```
dotnet test Vaz.sln -c Release --no-build
Passed! - Failed: 0, Passed: 463, Skipped: 0, Total: 463
```

**Static gate:**
```
check.ps1 -Scope static
STATIC CHECK PASSED: build + test + agent config
CHECK PASSED: static
```

**CI:**
```
grep -n "MIN_TESTS" .github/workflows/ci.yml
50:          MIN_TESTS: '460'
```

## 6. Known issues

1. **IL2026 đang được suppress, chưa fix gốc.** `MinecraftVersionService` dùng
   `JsonSerializer.Deserialize<T>` reflection cho các DTO private. Justification: `Vazer.Core`
   không đánh dấu `IsTrimmable`, WinUI publish `TrimMode=partial`, nên assembly này không bị trim.
   **P2:** chuyển 5 DTO (`VanillaVersionManifest`, `FabricLoaderEntry`, `QuiltLoaderEntry`,
   `NeoForgeMavenVersions`, `List<LoaderVersionInfo>`) sang `VazJsonContext` source-gen.
2. **`VSTHRD110` chưa có hiệu lực.** Rule này thuộc
   `Microsoft.VisualStudio.Threading.Analyzers`, chưa cài package → khai báo severity trong
   `.editorconfig` hiện là no-op. Cần thêm analyzer ở P2.
3. **`.editorconfig` đặt `end_of_line = lf`** trong khi repo hiện dùng CRLF. Chưa chạy
   `dotnet format` nên chưa có churn; nếu bật format gate ở P2 sẽ tạo diff lớn một lần.
4. **`AnalysisLevel=latest`** đi theo SDK; khi nâng .NET SDK có thể xuất hiện CA warning mới.
   Cần chốt `AnalysisLevel` theo version (ví dụ `8.0`) nếu muốn ổn định tuyệt đối.
5. **`MIN_TESTS=460`** thấp hơn actual 463 (buffer 3 theo yêu cầu). Khi thêm test nên nâng tiếp.

## 7. Đề xuất tiếp theo (P2)

1. Bật `TreatWarningsAsErrors=true` **riêng `Vazer.Core.csproj`**; sau khi ổn định mở rộng cho WinUI.
2. Thêm `Microsoft.VisualStudio.Threading.Analyzers` + `Microsoft.CodeAnalysis.NetAnalyzers`
   để `VSTHRD110`/`CS4014` thực sự hoạt động.
3. Migrate JSON sang source-gen (`VazJsonContext`) để xoá suppression IL2026.
4. CI: thêm `dotnet format --verify-no-changes`, coverage threshold (Core ≥80%), và build
   `-warnaserror` cho Core.
5. Chốt `AnalysisLevel` theo SDK version; thêm `dotnet_diagnostic` cho các rule quan trọng
   (async, dispose, nullable) ở mức `warning`.

## 8. Self-review

- [x] `dotnet test`: 463/463 pass, không giảm, không skip.
- [x] Debug + Release build: 0 warning, 0 error.
- [x] `check:static` PASSED.
- [x] Không sửa test file.
- [x] Không đổi logic nghiệp vụ; chỉ nullability/async signature.
- [x] Mỗi việc 1 commit, message đúng quy ước.
- [x] `MIN_TESTS` không còn `320`.
- [x] Không bật `TreatWarningsAsErrors` ở phase này.
- [x] IL2026 suppression có justification + kế hoạch P2 rõ ràng.
