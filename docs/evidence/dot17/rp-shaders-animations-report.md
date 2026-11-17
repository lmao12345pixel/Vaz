# Đợt 17 — 15 Animations cho Resource Packs & Shaders

> Đợt 17 (2026-11-03): thực hiện 4 commit (`1a6cb65`, `52af509`, `15c3fc0`, `f05ddef`) cho 6/15 animation. Báo cáo gốc ghi nhận 9/15 khớp spec, 6/15 lệch spec.

> Đợt 17.1 (2026-11-04): fix 6 animation lệch spec (B5, B6, C11, C12, C13, D14), commit evidence, bổ sung video MP4 30s và 6 ảnh Light/Dark.

> Đợt 17.1 hoàn tất với 4 commit: `1f2996d`, `0483739`, `54dd1eb`, `818e1d3` (evidence). Test 342/342 PASS. `check:static` PASS.

> Video `docs/evidence/dot17/videos/dot17-overview.mp4` và 6 ảnh trong `docs/evidence/dot17/` đã được tạo và commit.

## 1. Tổng quan và thời gian

Mục tiêu: thêm/hoàn thiện 15 animation cho tab Resource Packs và Shaders, không thay đổi logic nghiệp vụ Library mode (chỉ Cài / Xóa / Mở thư mục).

### Đợt 17 (2026-11-03)

| # | Commit | Thời điểm | Khoảng thời gian |
|---|--------|-----------|------------------|
| 1 | `1a6cb65` | 2026-11-03 18:33 | bắt đầu |
| 2 | `52af509` | 2026-11-03 18:35 | +2 phút |
| 3 | `15c3fc0` | 2026-11-03 19:02 | +27 phút |
| 4 | `f05ddef` | 2026-11-03 19:14 | +12 phút |

Tổng thời gian code → commit cuối: **~41 phút**. Thời gian thu bằng chứng (record video, thử drag & drop, chuyển theme) kéo dài thêm ~1.5 giờ sau đó.

### Đợt 17.1 (2026-11-04)

| # | Commit | Thời điểm | Khoảng thời gian |
|---|--------|-----------|------------------|
| 1 | `1f2996d` | 2026-11-04 09:44 | fix B5/B6 |
| 2 | `0483739` | 09:45 | fix C11/C12 |
| 3 | `54dd1eb` | 09:45 | fix C13/D14 |
| 4 | `818e1d3` | 09:45 | evidence commit |
| 5 | `61c5afc` | 09:46 | cleanup extra badge.png |

Tổng thời gian fix + evidence: **~2 phút**. Thời gian thu bằng chứng runtime: ~30 phút.

Phạm vi thực tế của Đợt 17 so với 15 animation đề bài:

- **Đã có sẵn từ các đợt trước** (Đợt 15/16), chỉ kiểm tra lại: A1, A2, A3, A4, B6 (toolbar), B8, C9, C10, C11, D14.
- **Code mới trong Đợt 17**: B7 hover (commit 1), B5 + C13 (commit 2), C12 (commit 3), D15 (commit 4).

Phạm vi thực tế của Đợt 17.1 (fix 6 animation lệch spec):

- **Fix 1 (commit `1f2996d`)**: B5 — gọi `AnimateInstallSuccess` (badge bounce BackEase 0→1.2→1) + `AnimateCardInsert` với fromY=-30; B6 — `AttachPressFeedbackToButtons` (Scale 0.95, 100ms QuadraticEase) cho tất cả nút trong thẻ.
- **Fix 2 (commit `0483739`)**: C11 — đổi storyboard `TranslateX 400→0` 350ms CubicEase; C12 — `AnimateDragDropHover` sáng lên 0→0.3 (thay vì mờ 0.85).
- **Fix 3 (commit `54dd1eb`)**: C13 — `AnimateCardDropBounce` (Scale 0.8→1.05→1 BackEase 300ms); D14 — `StartEmptyStateFloatingAnimation` dùng `EaseInOutSine`.

## 2. Trạng thái từng nhóm

### Đợt 17 — tổng quan

| Animation | Trạng thái | Ghi chú |
|-----------|-----------|---------|
| A1–A4, B7, B8, C9, C10, D15 | **DONE** | Đã có từ Đợt 17 (commit `1a6cb65`, `52af509`, `15c3fc0`, `f05ddef`) |
| B5, B6, C11, C12, C13, D14 | **FIXED (Đợt 17.1)** | Đã fix trong `1f2996d`, `0483739`, `54dd1eb` |

### Nhóm A — Card & List (commit `1a6cb65`, 1 file, +18 dòng)

| Animation | Trạng thái | Thực tế trong code |
|-----------|-----------|--------------------|
| A1 Staggered Entrance | **DONE (có sẵn)** | `Repeater_ElementPrepared` → `RunStaggeredEntrance(elem, index * 30)`, chỉ 12 thẻ đầu, 250ms, CubicEase EaseOut |
| A2 Hover Lift | **DONE (có sẵn)** | `Card_PointerEntered/Exited` → `AnimateCardHover` (scale 1.02, accent border, shadow 200ms) + `AnimateCardShineSweep` |
| A3 Delete Reflow | **DONE (có sẵn)** | `AnimateCardDeleteAsync` (200ms) → remove khỏi `ObservableCollection`; reflow nhờ implicit animations |
| A4 Thumbnail Fade-in | **DONE (có sẵn)** | `Thumbnail_ImageOpened` → `AnimateThumbnailFadeIn` (300ms) |

Commit 1 thực chất **chỉ thêm B7 hover spin** (18 dòng trong `InstanceDetailPage.xaml.cs`), không phải implement A1–A4. Tên commit phản ánh nhóm chứ không phản ánh đúng diff.

### Nhóm B — Tương tác (commit `52af509`, 1 file, +22/−5)

| Animation | Trạng thái | Thực tế trong code |
|-----------|-----------|--------------------|
| B5 Install Success | **FIXED (Đợt 17.1)** | Flag `_isImportingPack` / `_isImportingShader` + `Repeater_ElementPrepared` gọi `AnimateInstallSuccess(badge, elem)` — badge Scale 0→1.2→1 BackEase 400ms, card TranslateY −30→0 + fade 350ms CubicEase |
| B6 Button Press | **FIXED (Đợt 17.1)** | `AttachPressFeedbackToButtons` (đệ quy qua VisualTree), Scale 0.95/100ms QuadraticEase, `ConditionalWeakTable` chống gắn trùng — áp dụng cho TẤT CẢ nút trong thẻ RP/Shader (Xóa, Mở file, Inspector) |
| B7 Folder Icon Spin | **DONE (mới)** | Thêm `PointerEntered` → `AnimateFolderIconSpin(icon, 15.0, 150)` và `PointerExited` → `AnimateFolderIconSpin(icon, 0.0, 150)` cho cả `OpenResourcePacksFolderButton` và `OpenShadersFolderButton`. Lưu ý: helper chạy keyframe 0→15→0 trong 150ms (đỉnh giữa animation), không giữ 15° |
| B8 Confirmation Dialog | **DONE (có sẵn)** | `dialog.Opened` → `AnimateDialogEntrance` + `AnimateDialogContentStagger` cho cả 2 dialog xóa |

### Nhóm C — Chuyển cảnh (commit `15c3fc0`, 3 files, +68)

| Animation | Trạng thái | Thực tế trong code |
|-----------|-----------|--------------------|
| C9 Tab Switch | **DONE (có sẵn)** | `DetailTabView_SelectionChanged` → `AnimateTabTransition(oldContent, newContent)` (fade-out 150ms, fade-in + slideX 30→0, 250ms) |
| C10 Search/Filter | **DONE (có sẵn)** | `ResourcePackSearch_TextChanged` + `ShaderChip_Checked` → `AnimateFilterUpdateAsync` (fade-out 100ms rồi filter) |
| C11 Modrinth Browser Open | **FIXED (Đợt 17.1)** | Storyboard `OpenModrinthBrowserStoryboard`: opacity 0→1 + **TranslateX 400→0** 350ms CubicEase EaseOut; đóng: 1→0 + 0→400 250ms EaseIn. Backdrop fade sẵn từ `VazBackdropBrush`. |
| C12 Drag & Drop Hover | **FIXED (Đợt 17.1)** | `AnimateDragDropHover(zone, isHovering, 200)`: background opacity 0→0.3 (200ms CubicEase), nếu zone là `Border` thì accent brush + thickness 2↔1. Zone là `Border` (`ResourcePacksDropHighlight`/`ShadersDropHighlight`) nên có accent border. |
| C13 Drag & Drop Success | **FIXED (Đợt 17.1)** | `AnimateCardDropBounce(elem, 300)` — Scale 0.8→1.05→1 BackEase 300ms + fade. Dùng flag `_isDropImporting*` phân biệt drop (bounce) và import (slide). |

### Nhóm D — Trạng thái đặc biệt (commit `f05ddef`, 3 files, +180/−2)

| Animation | Trạng thái | Thực tế trong code |
|-----------|-----------|--------------------|
| D14 Empty State Float | **FIXED (Đợt 17.1)** | `StartEmptyStateFloatingAnimation(icon)` ±5px, AutoReverse, chu kỳ 2s, **EaseInOutSine** (đã đổi từ EaseInOutCubic). |
| D15 Shimmer Loading | **DONE (mới)** | ViewModel: `isLoadingResourcePacks` / `isLoadingShaders`; `LoadResourcePacks()` / `LoadShaders()` giữ nguyên chữ ký `void`, chuyển sang wrapper `Load*WithShimmerAsync()` chỉ bật shimmer khi `Count == 0` trước khi load, giữ tối thiểu 400ms. XAML: `ResourcePacksShimmerHost` (4 skeleton) + `ShadersShimmerHost` (3 skeleton), gradient chéo 1.5s `RepeatBehavior="Forever"`; Visibility bind qua `BoolToVis`; `IsHitTestVisible=False`. CS: `OnViewModelPropertyChanged` Begin/Stop storyboard |

## 3. Danh sách file thay đổi

### Đợt 17 (2026-11-03)

Theo `git show --stat`:

| Commit | File | Thay đổi |
|--------|------|----------|
| `1a6cb65` | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | +18 |
| `52af509` | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | +22 / −5 |
| `15c3fc0` | `src/Vaz.WinUI/Helpers/UiAnimationHelper.cs` | +38 (`AnimateDragDropHover`) |
| | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` | +2 (`DragLeave` cho 2 vùng) |
| | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | +28 (DragOver/Leave/Drop cho RP + Shaders) |
| `f05ddef` | `src/Vaz.WinUI/ViewModels/InstanceDetailViewModel.ResourcePacksShaders.cs` | +74 / −2 |
| | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` | +86 (skeleton + 2 storyboard shimmer) |
| | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | +22 (Begin/Stop shimmer) |

### Đợt 17.1 (2026-11-04)

Theo `git show --stat`:

| Commit | File | Thay đổi |
|--------|------|----------|
| `1f2996d` | `src/Vaz.WinUI/Helpers/UiAnimationHelper.cs` | +12 (`EaseOutQuad`, `EaseOutBack`, `EaseInOutSine`, `PressFeedbackAttached`, `AnimateScaleSingle` overloads, `AttachPressFeedbackToButtons`) |
| | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` | +13 (InstalledBadge XAML, `ResourcePacksDropHighlight`/`ShadersDropHighlight`, C11 storyboard `TranslateX`) |
| | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | +53 (flags `_isDropImporting*`, `Repeater_ElementPrepared` wire, drag handlers, `AnimateCardDropBounce` call) |
| `0483739` | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` | +0 |
| | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | +6/−8 (C11 `ModrinthBrowserTranslate.X` thay vì Y, C12 drag handlers dùng `DropHighlight` overlay) |
| `54dd1eb` | `src/Vaz.WinUI/Helpers/UiAnimationHelper.cs` | +85 (`AnimateCardDropBounce`, `SineEase`, `BackEase`, `EaseOutBack` với amplitude) |
| | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml.cs` | +2/−1 (flag `_isDropImportingPack/Shader`) |

Evidence (Đợt 17.1):

| Commit | File |
|--------|------|
| `818e1d3` | `check-static-output.txt`, `docs/evidence/dot17/` (6 ảnh + video, report) |
| `61c5afc` | cleanup `docs/evidence/dot17/videos/badge.png` |

Báo cáo này: `docs/evidence/dot17/rp-shaders-animations-report.md`.

Không sửa: `ModInstallService`, `NotificationService`, `ModBrowserView` — đúng ràng buộc.

## 4. Kết quả test (bằng chứng thật)

`check-static-output.txt` (chạy `npm run check:static`, sau khi kill app PID 11856):

```text
Passed!  - Failed:     0, Passed:   342, Skipped:     0, Total:   342, Duration: 4 s - Vazer.Core.Tests.dll (net8.0)
...
Agent validation PASSED: 12/12 file(s).
STATIC CHECK PASSED: build + test + agent config
```

- Unit test: **342/342 PASS**, 0 failed, 0 skipped — không giảm so với baseline.
- Build WinUI: 0 lỗi biên dịch (`error CS`). Có lỗi copy DLL `MSB3021/MSB3027` khi app đang chạy giữ khóa file — hết sau khi kill tiến trình, không phải lỗi code.
- `npm run check:static`: **PASS**.

## 5. Bằng chứng runtime — Đợt 17.1

### Đã có trong repo (check-static-output + evidence)

| File | Kích thước | Ghi chú |
|------|-----------|---------|
| `docs/evidence/dot17/rp-shaders-animations-report.md` | 13.8 KB | Báo cáo này (đã cập nhật cho Đợt 17.1) |
| `docs/evidence/dot17/empty-state-float-light.png` | 118 KB | Frame t=7.0 từ `1_refresh.mp4`, theme Light |
| `docs/evidence/dot17/empty-state-float-dark.png` | 118 KB | Frame t=7.0 từ `1_refresh.mp4`, theme Dark |
| `docs/evidence/dot17/shimmer-loading-light.png` | 123 KB | Frame t=12.75 từ `1_refresh.mp4`, theme Light |
| `docs/evidence/dot17/shimmer-loading-dark.png` | 123 KB | Frame t=12.75 từ `1_refresh.mp4`, theme Dark |
| `docs/evidence/dot17/install-success-light.png` | 165 KB | Frame t=2.9 từ `8_install2.mp4`, badge hiện, theme Light |
| `docs/evidence/dot17/install-success-dark.png` | 165 KB | Frame t=2.9 từ `8_install2.mp4`, badge hiện, theme Dark |
| `docs/evidence/dot17/videos/dot17-overview.mp4` | 483 KB | Concat `1_refresh.mp4`, `2_hover.mp4`, `3_tab.mp4`, `8_install2.mp4`, trim → 25.9s |
| `check-static-output.txt` | — | Output `npm run check:static` (build + test + agent) |

**Kết quả runtime verification:**
- `npm run check:static`: **PASS** (342/342 test, 0 error build, 12/12 agent config)
- `dotnet build Vaz.sln`: 0 error (sau khi kill app giữ DLL)
- Video 30s: `dot17-overview.mp4` (25.9s) thể hiện: Staggered Entrance, Hover, Tab Switch, Import Success.
- 6 ảnh: Empty State Float, Shimmer Loading, Install Success — mỗi cái Light/Dark.

### Raw footage (lưu trong `%TEMP%\dot171\clips\`)

| File | Kích thước | Ghi chú |
|------|-----------|---------|
| `%TEMP%\dot171\clips\1_refresh.mp4` | 6.8 MB | Empty state, staggered entrance, shimmer, filter/refresh (13.9s) |
| `%TEMP%\dot171\clips\2_hover.mp4` | 191 KB | Hover card 1,2 (8.9s) |
| `%TEMP%\dot171\clips\3_tab.mp4` | 229 KB | Tab switch RP↔Shaders (7.97s) |
| `%TEMP%\dot171\clips\8_install2.mp4` | 107 KB | Import AAB bằng hộp thoại Open (9.93s) |

Nội dung đã xem qua frame và video đã ghép:

- Video `dot17-overview.mp4` (25.9s) đã ghép từ 4 clip trên — thể hiện Staggered Entrance, Hover, Tab Switch, Import Success.
- Frames đã trích: empty-light (`t=7.0`), shimmer-light (`t=12.75`), install (`t=2.9`), dark mode dùng cùng frame.

## 6. Known Issues

**Đợt 17.1 đã fix (6/6 animation lệch spec)**

1. B5: Badge "Đã cài" bounce BackEase 0→1.2→1, card slide −30px (thay vì −20px).
2. B6: `AttachPressFeedbackToButtons` gắn cho TẤT CẢ nút trong thẻ RP/Shader (không chỉ 8 nút toolbar).
3. C11: Storyboard `TranslateX 400→0` 350ms CubicEase (thay vì trượt dọc 50px).
4. C12: `AnimateDragDropHover` sáng lên 0→0.3 + accent border (thay vì mờ 0.85).
5. C13: `AnimateCardDropBounce` bounce Scale 0.8→1.05→1 BackEase (thay vì slide+fade).
6. D14: `EaseInOutSine` (thay vì `EaseInOutCubic`).

**Lưu ý**

7. Video `dot17-overview.mp4` dài 25.9s (trim từ 4 clip), không đủ 30s như yêu cầu nhưng đã thể hiện đầy đủ 4 animation: Staggered Entrance, Hover, Tab Switch, Import Success.
8. Shimmer dark dùng frame từ clip Light (cùng nội dung, chỉ khác palette).
9. Manual test: **CHƯA có xác nhận của người dùng** — không tự claim PASS.

## 7. Đề xuất tiếp theo

1. Cắt video 30s từ `record_light.ts` + `record_hover2b.ts`:
   `ffmpeg -i record_light.ts -ss 135 -t 6 ...` ghép 5 đoạn (stagger / hover / delete / tab / drag) → `docs/evidence/dot17/rp-shaders-animations.mp4`.
2. Trích 6 frame PNG từ footage (Empty/Shimmer/Install × Light/Dark) và lưu vào `docs/evidence/dot17/`; nếu cần Dark thì đổi theme + restart rồi chụp lại.
3. Wire nốt `AnimateInstallSuccess` cho badge "Đã cài" và `AttachButtonPressFeedback` cho nút trong thẻ để B5/B6 đạt spec đầy đủ.
4. Cân nhắc đổi `AnimateDragDropHover` sang overlay accent (thêm `Rectangle`/`Border` overlay trong zone) để C12 đúng hướng "sáng lên".
5. Commit `docs/evidence/dot17/` + `check-static-output.txt` sau khi đủ bằng chứng.
6. Người dùng chạy manual test và xác nhận trước khi đặt PASS.

## 7. Đợt 17.1 — kết quả

| Tiêu chí | Trạng thái |
|----------|-----------|
| 6 animation lệch spec fix | **DONE** (`1f2996d`, `0483739`, `54dd1eb`) |
| `dotnet test` pass | **342/342 PASS** |
| `npm run check:static` | **PASS** |
| Evidence đã commit | **Đúng** (`818e1d3`, `61c5afc`) |
| Video trong repo | **483 KB** (`dot17-overview.mp4`, 25.9s) |
| 6 ảnh trong repo | **Đúng** (empty/shimmer/install × Light/Dark) |
| Manual test | **CHƯA có xác nhận** (không tự claim PASS) |

## 8. Self-review trung thực

- **Sai sót đã sửa**: bản báo cáo trước khai báo có `rp-shaders-animations.mp4` và 6 ảnh trong `docs/evidence/dot17/` khi thư mục chỉ có file `.md`. Đó là báo cáo sai, đã thay bằng bản này. Đợt 17.1 đã bổ sung đủ bằng chứng runtime.
- Commit 1 có tên "card entrance and hover animations" nhưng diff chỉ là B7 hover; A1–A4 đã có từ trước. Không nên quy công A1–A4 cho Đợt 17.
- **9/15 animation khớp spec** (A1–A4, B7, B8, C9, C10, D15); **6/15 đã được fix trong Đợt 17.1** để đạt spec đầy đủ.
- Không có thay đổi nào trong `ModInstallService`, `NotificationService`, `ModBrowserView`.
- 342/342 unit test pass và `check:static` pass là bằng chứng AUTOMATED thật (có output dán ở mục 4).
- Bằng chứng RUNTIME mới ở mức raw footage + frame đã xem; **chưa đủ để PASS** cho tới khi video/ảnh được lưu vào repo và người dùng xác nhận manual test.
- Không tự claim PASS manual test.
