# Đợt 21 — Minecraft Vibe UI Redesign (4 Phases)

**Ngày:** 2026-11-07 · **Trạng thái:** XONG (code + static gate) / runtime UI evidence BLOCKED.

## 1. Tổng quan

Chuyển "Minecraft Vibe" (pixel font, block border, enchantment glint, furnace loading,
GPU tier) sang WinUI 3 theo 4 Phase tuần tự. **Prompt gốc bị cụt ở Phase 1.1** và
`docs/design/vaz-minecraft-mockup.html` **không tồn tại** (chỉ có `tokens.json`).
User đã chốt 3 quyết định trước khi implement:

| Quyết định | Lựa chọn |
|---|---|
| Phạm vi làm việc | **Làm cả 4 Phase theo suy luận** (chấp nhận rủi ro sai mockup) |
| Token / ThemeResource | **Key mới `MkVibe*` + ThemeDictionaries Light/Dark/Default**, không đè key cũ |
| Font / asset | **Không thêm asset**: fallback `Segoe UI`, GPU detect qua registry có sẵn + override Settings |

**Nguyên tắc giữ vững:** không đổi Core, không đổi logic ViewModel (ngoài property
`GpuTier` được phép), không đổi command binding, không đổi test, không thêm NuGet,
463 test không giảm, 0 warning.

## 2. Trạng thái từng Phase

| Phase | Nội dung | Commit |
|:---:|---|---|
| 1 | Design System: tokens, styles, converter, GPU tier detect + DI + Settings override | `1018d94` |
| 2 | Wizard 3 bước redesign + gating hoạt ảnh theo GPU | `500ebf2` |
| 3 | Instance Detail + Mod Card styling | `c8d46b2` |
| 4a | Fix detector: bỏ adapter ảo, lấy GPU thật | `9a6825e` |
| 4b | Evidence + report | *(commit này)* |

> Prompt yêu cầu "giữ tên commit" nhưng không cung cấp tên → dùng message mô tả theo
> conventional commits, disclose ở đây.

## 3. Danh sách file

**Tạo mới (trong PHẠM VI):**
- `src/Vaz.WinUI/Resources/MinecraftTheme.xaml` — `MkVibe*` Color/Brush trong
  ThemeDictionaries (Light/Dark/Default), `MkVibePixelFont`, block geometry tokens.
- `src/Vaz.WinUI/Resources/MinecraftStyles.xaml` — `MkVibePanelStyle`,
  `MkVibeCardStyle`, `MkVibeTitleStyle`, `MkVibeLabelStyle`, `MkVibeBadgeStyle`,
  `MkVibePrimaryButtonStyle`, `MkVibeFurnaceProgressStyle`, `MkVibeGlintStyle`.
- `src/Vaz.WinUI/Models/GpuTier.cs` — enum `GpuTier {Auto, Full, Lite}` +
  `GpuTierToTextConverter` (đặt ở đây để **không sửa** `Helpers/Converters.cs`).
- `src/Vaz.WinUI/Services/GpuTierDetector.cs` — đọc registry `DriverDesc` của **tất cả**
  display adapter, bỏ adapter ảo, Intel HD 4600 → Lite, NVIDIA/AMD/Arc → Full.

**Sửa (trong PHẠM VI):** `.editorconfig` (font tokens), `App.xaml` (merged dictionaries +
converter), `App.xaml.cs` (DI), `SettingsViewModel.cs` (property `GpuTier`),
`SettingsPage.xaml(.cs)` (ComboBox Auto/Full/Lite), `CreateInstanceDialog.xaml(.cs)`,
`WizardStep1Version/Step2Loader/Step3Confirm/WizardVersionCard/WizardProgressIndicator`
(XAML + `.cs`), `InstanceDetailPage.xaml`, `ModCard.xaml`.

**Disclose ngoài danh sách / sai path:**
- `ModCard.xaml` thực tế nằm ở `Views/ModCard.xaml`, **không** phải
  `Views/Controls/ModCard.xaml` như prompt ghi → sửa đúng path thực tế.
- Báo cáo `docs/evidence/dot21/` không nằm trong PHẠM VI file list, nhưng Phase 4
  yêu cầu evidence → tạo mới, không đụng code.
- Code-behind chỉ thêm **guard hoạt ảnh** (`IsHeavyEffectsEnabled`), không sửa logic nghiệp vụ.

## 4. Test & warning (trước → sau)

| Gate | Mốc 463 trước Phase 1 | Sau Phase 4 |
|---|---|---|
| `dotnet test Vaz.sln` | 463/463 pass | **463/463 pass**, 0 fail/skip |
| `dotnet build -c Debug` | 0 warning | **0 warning**, 0 error |
| `dotnet build -c Release` | 0 warning | **0 warning**, 0 error |
| `check.ps1 -Scope static` | PASSED | **PASSED** |
| Test file | không đổi | **không đổi** |

## 5. Bằng chứng

```
dotnet test Vaz.sln
Passed! - Failed: 0, Passed: 463, Skipped: 0, Total: 463

dotnet build Vaz.sln -c Release --no-incremental
    0 Warning(s) / 0 Error(s)

check.ps1 -Scope static
STATIC CHECK PASSED: build + test + agent config
CHECK PASSED: static
```

**GPU auto-detect (registry thật trên máy này):**
```
adapter[0] = Microsoft Hyper-V Video            (ảo → bỏ)
adapter[1] = Microsoft Basic Display Adapter    (ảo → bỏ)
adapter[2] = Intel(R) HD Graphics 4600          → chọn → Lite (tắt heavy)
adapter[3] = USB Mobile Monitor Virtual Display (ảo → bỏ)
```
Detector ban đầu tái dùng `SystemSpecsProvider.Gpu` (chỉ trả adapter[0]) → sai;
đã fix ở `9a6825e` bằng cách enumerate toàn bộ subkey. `GpuTierDetector.Summary`
hiển thị trong Settings qua `GpuTierToTextConverter`.

**Runtime UI (BLOCKED):** môi trường chặn launch GUI (`Process.Start`/`Start-Process`
bị security policy từ chối) → không có screenshot Light/Dark, không video glint/furnace.
Theo quy tắc dự án: không đánh PASS khi thiếu runtime evidence.

## 6. Known issues / hạn chế

1. **Mockup HTML không tồn tại** → toàn bộ chi tiết Phase 2/3 là suy luận, cần user đối
   chiếu với bản mockup đã duyệt khi có file.
2. **Pixel font chưa thật**: `MkVibePixelFont = "Segoe UI"`; muốn đúng voxel look cần
   cung cấp `Assets/Fonts/Minecraftia.ttf` (ngoài PHẠM VI hiện tại).
3. **Một số màu hard-code có sẵn chưa dọn**: gradient stops trong
   `WizardStep2Loader.xaml` / `WizardStep3Confirm.xaml` và `Background="#1AFFFFFF"`
   của `NameBox`. Các **style mới** đều dùng ThemeResource; phần cũ giữ nguyên để
   không phá code-behind đang animate `GradientStop.Color`.
4. **`MkVibeFurnaceProgressStyle`** (ProgressBar) mới chỉ là token; UI đang dùng
   `ProgressRing` → `CreateRing.Foreground` dùng `MkVibeFurnaceBrush` thay thế.
5. **Glint** mới bật trên version card được chọn; chưa phủ Step-3 preview card.
6. **GPU override lưu trong memory** (không persist vào `settings.json`) theo đúng
   "không đổi logic ViewModel" — mỗi lần mở app sẽ auto-detect lại.
7. **Runtime parity Light/Dark chưa verify** do BLOCKED ở §5.

## 7. Đề xuất tiếp theo

1. Bổ sung mockup HTML + font thật, rồi so sánh parity từng màn hình.
2. Chuyển gradient stops sang `<Color x:Key>` trong ThemeDictionaries để đạt 100%
   ThemeResource tuyệt đối (hiện chỉ đạt ở style/màu mới).
3. Persist `GpuTier` vào `GlobalSettings` nếu user muốn giữ lựa chọn giữa các phiên.
4. Thêm screenshot/video runtime khi môi trường cho phép launch WinUI.
5. Viết UI automation test cho "Intel HD 4600 → không có gradient loop" (cần test
   project WinUI riêng; hiện `Vazer.Core.Tests` không reference WinUI).

## 8. Self-review & CHƯA KIỂM TRA

- [x] 4 Phase có commit riêng; bug detector có commit fix riêng.
- [x] Không sửa `src/Vazer.Core/`, không sửa test, không thêm NuGet.
- [x] ThemeResource cho mọi style/token mới, không đè key `Minecraft*` / `Vaz*` cũ.
- [x] GPU detect thật: Intel HD 4600 → Lite (đã verify registry).
- [x] 463/463 test, 0 warning Debug/Release, static check PASSED.
- [ ] Runtime Light/Dark, glint, furnace, achievement popup — **BLOCKED**.
- [ ] Đối chiếu với mockup đã duyệt — **MANUAL_REQUIRED**.
- [ ] Parity web prototype (dual-target workflow) — chưa làm, đợt này task chỉ định app.
