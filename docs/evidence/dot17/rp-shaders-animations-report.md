# Đợt 17 — 15 Animations cho Resource Packs & Shaders

> Báo cáo này được viết lại ngày 2026-11-04 sau khi kiểm tra thực tế repo và thư mục bằng chứng.
> Bản trước đây khai báo có video `.mp4` và 6 ảnh trong `docs/evidence/dot17/`; **thực tế các file đó không tồn tại**.
> Báo cáo này chỉ ghi nhận những gì đã kiểm chứng được, phần thiếu được ghi rõ ở mục 5 và 6.

## 1. Tổng quan và thời gian

Mục tiêu: thêm/hoàn thiện 15 animation cho tab Resource Packs và Shaders, không thay đổi logic nghiệp vụ Library mode (chỉ Cài / Xóa / Mở thư mục).

| # | Commit | Thời điểm | Khoảng thời gian |
|---|--------|-----------|------------------|
| 1 | `1a6cb65` | 2026-11-03 18:33 | bắt đầu |
| 2 | `52af509` | 2026-11-03 18:35 | +2 phút |
| 3 | `15c3fc0` | 2026-11-03 19:02 | +27 phút |
| 4 | `f05ddef` | 2026-11-03 19:14 | +12 phút |

Tổng thời gian code → commit cuối: **~41 phút**. Thời gian thu bằng chứng (record video, thử drag & drop, chuyển theme) kéo dài thêm ~1.5 giờ sau đó.

Phạm vi thực tế của Đợt 17 so với 15 animation đề bài:

- **Đã có sẵn từ các đợt trước** (Đợt 15/16), chỉ kiểm tra lại: A1, A2, A3, A4, B6 (toolbar), B8, C9, C10, C11, D14.
- **Code mới trong Đợt 17**: B7 hover (commit 1), B5 + C13 (commit 2), C12 (commit 3), D15 (commit 4).

## 2. Trạng thái từng nhóm

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
| B5 Install Success | **PARTIAL** | Có flag `_isImportingPack` / `_isImportingShader` bọc try/finally quanh 4 đường import (picker + drop, RP + Shaders). `Repeater_ElementPrepared` gọi `AnimateCardInsert(elem, 350)` → trượt từ **−20px** lên 0 + fade-in. **Badge "Đã cài" Scale 0→1.2→1 BackEase CHƯA được wire** (`AnimateInstallSuccess` có trong helper nhưng không được gọi ở đâu trong page) |
| B6 Button Press | **PARTIAL** | `AttachButtonPressFeedback(btn, 0.95, 100, useSpringPhysics: true)` chỉ gắn cho **8 nút toolbar**. Các nút trong thẻ (Xóa / Mở file / Xem chi tiết) **không được gắn**. Dùng spring physics thay vì QuadraticEase (không vi phạm cấm LinearEase) |
| B7 Folder Icon Spin | **DONE (mới)** | Thêm `PointerEntered` → `AnimateFolderIconSpin(icon, 15.0, 150)` và `PointerExited` → `AnimateFolderIconSpin(icon, 0.0, 150)` cho cả `OpenResourcePacksFolderButton` và `OpenShadersFolderButton`. Lưu ý: helper chạy keyframe 0→15→0 trong 150ms (đỉnh giữa animation), không giữ 15° |
| B8 Confirmation Dialog | **DONE (có sẵn)** | `dialog.Opened` → `AnimateDialogEntrance` + `AnimateDialogContentStagger` cho cả 2 dialog xóa |

### Nhóm C — Chuyển cảnh (commit `15c3fc0`, 3 files, +68)

| Animation | Trạng thái | Thực tế trong code |
|-----------|-----------|--------------------|
| C9 Tab Switch | **DONE (có sẵn)** | `DetailTabView_SelectionChanged` → `AnimateTabTransition(oldContent, newContent)` (fade-out 150ms, fade-in + slideX 30→0, 250ms) |
| C10 Search/Filter | **DONE (có sẵn)** | `ResourcePackSearch_TextChanged` + `ShaderChip_Checked` → `AnimateFilterUpdateAsync` (fade-out 100ms rồi filter) |
| C11 Modrinth Browser Open | **DONE (có sẵn, lệch spec)** | XAML storyboard `OpenModrinthBrowserStoryboard`: opacity 0→1 + **TranslateY 50→0** 250ms CubicEase; đóng đảo ngược. Spec yêu cầu TranslateX 400→0 + backdrop fade → thực tế là trượt dọc 50px, không có backdrop riêng |
| C12 Drag & Drop Hover | **DONE (mới, lệch spec)** | Method mới `AnimateDragDropHover(zone, isHovering, 200)` trong `UiAnimationHelper.cs` (dòng 580): opacity 1.0→0.85 (200ms CubicEase); nếu zone là `Border` thì accent brush + thickness 2↔1. **Zone thực tế là `Grid`** (`ResourcePacksContainerGrid`, vùng Shaders) nên không có accent border. Spec yêu cầu "sáng lên + BorderBrush accent" → thực tế là **mờ đi 15%** + caption drag của Windows |
| C13 Drag & Drop Success | **PARTIAL** | Cùng flag `_isImporting*` → `AnimateCardInsert` (trượt + fade). Spec yêu cầu bounce Scale 0.8→1.05→1 BackEase 300ms → **không phải bounce** |

### Nhóm D — Trạng thái đặc biệt (commit `f05ddef`, 3 files, +180/−2)

| Animation | Trạng thái | Thực tế trong code |
|-----------|-----------|--------------------|
| D14 Empty State Float | **DONE (có sẵn, lệch easing)** | `EmptyIcon_Loaded` → `StartEmptyStateFloatingAnimation(icon)` ±5px, AutoReverse, chu kỳ 2s. Helper dùng **EaseInOutCubic**, spec ghi SineEaseInOut |
| D15 Shimmer Loading | **DONE (mới)** | ViewModel: `isLoadingResourcePacks` / `isLoadingShaders`; `LoadResourcePacks()` / `LoadShaders()` giữ nguyên chữ ký `void`, chuyển sang wrapper `Load*WithShimmerAsync()` chỉ bật shimmer khi `Count == 0` trước khi load, giữ tối thiểu 400ms. XAML: `ResourcePacksShimmerHost` (4 skeleton) + `ShadersShimmerHost` (3 skeleton), gradient chéo 1.5s `RepeatBehavior="Forever"`; Visibility bind qua `BoolToVis`; `IsHitTestVisible=False`. CS: `OnViewModelPropertyChanged` Begin/Stop storyboard |

## 3. Danh sách file thay đổi

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

Báo cáo này: `docs/evidence/dot17/rp-shaders-animations-report.md`.

Không sửa: `ModInstallService`, `NotificationService`, `ModBrowserView` — đúng ràng buộc.

## 4. Kết quả test (bằng chứng thật)

`check-static-output.txt` (chạy `npm run check:static`):

```text
Passed!  - Failed:     0, Passed:   342, Skipped:     0, Total:   342, Duration: 5 s - Vazer.Core.Tests.dll (net8.0)
...
Agent validation PASSED: 12/12 file(s).
STATIC CHECK PASSED: build + test + agent config
```

- Unit test: **342/342 PASS**, 0 failed, 0 skipped — không giảm so với baseline.
- Build WinUI: 0 lỗi biên dịch (`error CS`). Có lỗi copy DLL `MSB3021/MSB3027` khi app đang chạy giữ khóa file — hết sau khi kill tiến trình, không phải lỗi code.
- `npm run check:static`: **PASS**.

## 5. Bằng chứng runtime — trung thực

### Đã có (kiểm chứng bằng file thật)

Raw recording nằm trong thư mục tạm, **chưa đưa vào repo**:

| File | Kích thước | Ghi chú |
|------|-----------|---------|
| `%TEMP%\opencode\dot17\record_light.ts` | 417.6 MB | Take chính, theme Light, 1312×698, 30fps |
| `%TEMP%\opencode\dot17\record_hover2b.ts` | 9.4 MB | Take drag Pack_B duration 8s (C12 hover dài + drop + toast) |
| `%TEMP%\opencode\dot17\record_hover2.ts` | 3.9 MB | Take hỏng (thu nhầm cửa sổ OpenCode) — không dùng |

Nội dung đã xem trực tiếp qua frame trích xuất (contact sheet + frame full-res):

- Light take: hover A2, stagger sau "Làm mới", dialog xóa + reflow (Faithful biến mất), tab RP↔Shaders, drag từ Explorer → drop, toast "Đã thêm 1 gói tài nguyên vào thư viện.", thẻ mới xuất hiện.
- Take 2b: caption "Thêm gói tài nguyên (.zip)" bám chuột khi kéo vào vùng drop (C12), drop thành công + toast + card insert (B5/C13). Ảnh frame full-res `h2b_hover_full.png`, `h2b_after_full.png` đã xem.

### CHƯA có (không được tính là bằng chứng)

1. **Video 30s `.mp4`: CHƯA cắt/ghép.** `docs/evidence/dot17/rp-shaders-animations.mp4` không tồn tại. Toàn bộ raw footage vẫn ở thư mục tạm.
2. **6 ảnh (Empty State Float / Shimmer Loading / Install Success × Light/Dark): CHƯA lưu.** Chỉ có ảnh chụp màn hình/contact sheet tạm thời để kiểm tra, chưa đặt tên và chưa đưa vào `docs/evidence/dot17/`.
3. **Dark mode chưa chụp ảnh.** Đã đổi `%APPDATA%\Vazer\Minecraft\settings.json` sang `"theme": "dark"` và khởi động lại app, nhưng chưa lưu ảnh nào. **Đã khôi phục lại `"theme": "light"`** (giá trị gốc) — cần khởi động lại app để áp dụng.
4. **Manual test: CHƯA có xác nhận của người dùng** — không tự claim PASS.

> Vì thiếu mục 1–3, các acceptance criterion RUNTIME của Đợt 17 **chưa đủ bằng chứng để PASS**. Trạng thái đúng là **PARTIAL / BLOCKED ở khâu bằng chứng**, không phải DONE.

## 6. Known Issues

**Thiếu bằng chứng (ưu tiên cao)**

1. Chưa có video 30s trong repo; raw `.ts` nằm ở `%TEMP%\opencode\dot17\` và có thể bị Windows dọn.
2. Chưa có 6 ảnh Light/Dark trong repo.
3. Chưa có xác nhận manual test từ người dùng.

**Lệch spec trong implementation**

4. B5: thiếu animation badge "Đã cài" (BackEase 0→1.2→1). Chỉ có card slide −20px + fade 350ms (spec: −30px).
5. B6: nút trong từng thẻ (Xóa / Mở file) không có press feedback; chỉ toolbar có.
6. C11: trượt dọc 50px thay vì ngang 400px; không có backdrop fade riêng.
7. C12: zone là `Grid` nên chỉ mờ 0.85, không có accent border; ngược chiều spec ("sáng lên").
8. C13: dùng slide+fade thay vì bounce BackEase.
9. D14: dùng CubicEase InOut thay vì SineEaseInOut.
10. B7: helper chạy 0→15→0 trong 150ms, không giữ góc 15° khi hover.
11. D15: 2 đầu gradient `#00FFFFFF` là hardcode (WinUI không có theme resource trắng alpha 0); màu giữa dùng `SystemAccentColorLight2` (theme).
12. Shimmer chỉ hiện khi danh sách trống trước khi load (đúng spec "không hiện khi append"), nên refresh sau khi đã có dữ liệu sẽ không thấy shimmer.

**Trạng thái repo**

13. `docs/evidence/dot17/` và `check-static-output.txt` đang **untracked**, chưa commit.
14. `settings.json` đã trả về `light` nhưng app đang chạy có thể vẫn ở theme dark cho tới lần khởi động lại tiếp theo.

## 7. Đề xuất tiếp theo

1. Cắt video 30s từ `record_light.ts` + `record_hover2b.ts`:
   `ffmpeg -i record_light.ts -ss 135 -t 6 ...` ghép 5 đoạn (stagger / hover / delete / tab / drag) → `docs/evidence/dot17/rp-shaders-animations.mp4`.
2. Trích 6 frame PNG từ footage (Empty/Shimmer/Install × Light/Dark) và lưu vào `docs/evidence/dot17/`; nếu cần Dark thì đổi theme + restart rồi chụp lại.
3. Wire nốt `AnimateInstallSuccess` cho badge "Đã cài" và `AttachButtonPressFeedback` cho nút trong thẻ để B5/B6 đạt spec đầy đủ.
4. Cân nhắc đổi `AnimateDragDropHover` sang overlay accent (thêm `Rectangle`/`Border` overlay trong zone) để C12 đúng hướng "sáng lên".
5. Commit `docs/evidence/dot17/` + `check-static-output.txt` sau khi đủ bằng chứng.
6. Người dùng chạy manual test và xác nhận trước khi đặt PASS.

## 8. Self-review trung thực

- **Sai sót đã sửa**: bản báo cáo trước khai báo có `rp-shaders-animations.mp4` và 6 ảnh trong `docs/evidence/dot17/` khi thư mục chỉ có file `.md`. Đó là báo cáo sai, đã thay bằng bản này.
- Commit 1 có tên "card entrance and hover animations" nhưng diff chỉ là B7 hover; A1–A4 đã có từ trước. Không nên quy công A1–A4 cho Đợt 17.
- **9/15 animation khớp spec** (A1–A4, B7, B8, C9, C10, D15); **6/15 có code chạy được nhưng lệch spec** (B5 thiếu badge bounce, B6 thiếu nút trong thẻ, C11 trượt dọc thay vì ngang, C12 mờ thay vì sáng, C13 slide thay vì bounce, D14 dùng CubicEase thay vì SineEase). Không được gọi là "đủ 15/15 đúng spec".
- Không có thay đổi nào trong `ModInstallService`, `NotificationService`, `ModBrowserView`.
- 342/342 unit test pass và `check:static` pass là bằng chứng AUTOMATED thật (có output dán ở mục 4).
- Bằng chứng RUNTIME mới ở mức raw footage + frame đã xem; **chưa đủ để PASS** cho tới khi video/ảnh được lưu vào repo và người dùng xác nhận manual test.
- Không tự claim PASS manual test.
