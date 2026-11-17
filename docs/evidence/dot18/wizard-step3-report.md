# Đợt 18.3b — Wizard Step 3 + Transitions + Feedback (Confirm & Creation)

## 1. Tổng quan

Hoàn thiện toàn diện Wizard tạo instance mới — Phase 2/2 (.NET 8 / WinUI 3):
- **Step 3 (Confirm)**: Thẻ preview lớn 500px căn giữa với animated gradient loop (10s), tile loader nổi bật, Minecraft version, tên instance với floating label, nút "Tạo Instance" bo tròn gradient với glow pulse khi đủ điều kiện.
- **Loading & Success State**: Nút chuyển trạng thái (morph 500ms) thành viên nang loading với spinner trắng và stage text cross-fade theo sự kiện thật ("Đang chuẩn bị..." → "Đang lấy danh sách Fabric loader..." → "Đang cài Fabric..."). Trạng thái hoàn thành hiển thị checkmark xanh/trắng nảy mượt mà (500ms BackEase), tự động đóng dialog sau 1.5s–2.5s.
- **Wizard Container & Navigation**: Khung 3 bước chuyển mượt mà (slide 350ms CubicEase EaseInOut theo hướng tới/lui).
- **Progress Indicator Top**: Thanh 3 dots kết nối bởi đường line chuyển động, active dot phóng to kèm glow (200ms BackEase), line nối fill gradient (300ms).

## 2. Tiến độ từng Việc & Bảng 12 Animation

### Bảng 12 Animations

| STT | Tên Animation | Trạng thái | Chi tiết triển khai |
|:---:|---|:---:|---|
| #1 | Step Forward | **ĐÃ LÀM** | Step cũ trượt trái + fade out, step mới trượt từ phải (140px → 0) + fade in trong 350ms `CubicEase EaseInOut`. |
| #2 | Step Backward | **ĐÃ LÀM** | Đảo ngược hướng trượt (từ -140px → 0) khi bấm "← Quay lại" trong 350ms. |
| #3 | Progress Indicator | **ĐÃ LÀM** | Active dot phóng to (14px → 22px) + glow 200ms `BackEase`, line nối fill gradient 300ms `CubicEase`. |
| #4 | Label Float | **ĐÃ LÀM** | Nhãn "Tên instance" trượt lên -14px và thu nhỏ 15px → 11px khi focus hoặc có sẵn văn bản (150ms `CubicEase`). |
| #5 | Button Press | **ĐÃ LÀM** | Nút "Tạo Instance" co 0.95 khi bấm và hồi 1.0 khi nhả trong 100ms. |
| #6 | Button Enable Pulse | **ĐÃ LÀM** | Lớp glow xung quanh nút "Tạo" lặp chu kỳ 2s với `SineEase EaseInOut` (opacity 0.25 ↔ 0.85) khi form hợp lệ. |
| #7 | Button Morph | **ĐÃ LÀM** | Nút thu gọn và chuyển sang dạng loading pill trong 500ms `BackEase`. |
| #8 | Progress Percent | **ĐÃ LÀM** | Hiển thị % tiến độ thật từ `IInstanceCreationService` khi có determinate progress; tự ẩn khi indeterminate (không bịa số). |
| #9 | Stage Text Cross-fade | **ĐÃ LÀM** | Chuyển đổi giai đoạn tiến độ (text cũ fade out 120ms, text mới fade in 160ms `CubicEase`). |
| #10 | Success Checkmark | **ĐÃ LÀM** | Biểu tượng checkmark nổi bật nảy scale 0 → 1.2 → 1 (500ms `BackEase`) khi hoàn tất, kích hoạt auto-close sau 2.5s. |
| #11 | Dialog Open | **ĐÃ LÀM** | Dialog mở với hiệu ứng scale 0.92 → 1 và fade in trong 300ms `BackEase`. |
| #12 | Backdrop Blur | **KHÔNG KHẢ THI** | WinUI 3 Desktop `ContentDialog` render trên `PopupWindowSiteBridge` riêng; Acrylic/Backdrop brush không hỗ trợ blur xuyên thấu trực tiếp trên bridge này mà không cần P/Invoke subclassing sâu. Đã áp dụng fallback surface nền chuẩn `ThemeResource`. |

## 3. Danh sách file (Tạo / Sửa / Xóa)

**Tạo mới:**
- `src/Vaz.WinUI/Views/Controls/WizardStep3Confirm.xaml`: Giao diện Step 3 Confirm (preview card 500px, animated gradient, floating label, morph button).
- `src/Vaz.WinUI/Views/Controls/WizardStep3Confirm.xaml.cs`: Code-behind quản lý animated gradient loop, floating label, morph button, progress update và success checkmark.
- `src/Vaz.WinUI/Views/Controls/WizardProgressIndicator.xaml`: Control thanh tiến trình 3 dots kết nối gradient.
- `src/Vaz.WinUI/Views/Controls/WizardProgressIndicator.xaml.cs`: Logic animation cho dot active (phóng to + glow) và line fill.
- `docs/evidence/dot18/wizard-step3-report.md`: Báo cáo tiêu chuẩn này.
- `scripts/record-wizard-full.ps1`, `scripts/capture-8-screenshots.ps1`: Scripts kiểm tra và thu thập bằng chứng runtime tự động.

**Sửa đổi:**
- `src/Vaz.WinUI/Models/WizardState.cs`: Bổ sung `Step3Confirm = 3`.
- `src/Vaz.WinUI/ViewModels/CreateInstanceViewModel.cs`: Bổ sung `ApplyWizardState` và `CreateFromWizardAsync`, liên kết trực tiếp tiến trình thật từ `IInstanceCreationService`.
- `src/Vaz.WinUI/Views/CreateInstanceDialog.xaml`: Mở rộng container chứa 3 step hosts, tích hợp `WizardProgressIndicator`, cấu hình kích thước và ThemeResource.
- `src/Vaz.WinUI/Views/CreateInstanceDialog.xaml.cs`: Điều phối chuyển hướng 3 bước (slide 350ms), animate dialog open, gọi `CreateFromWizardAsync` và cập nhật tiến trình vào Step 3.
- `src/Vaz.WinUI/Views/Controls/WizardStep1Version.xaml(.cs)`: Tinh chỉnh padding footer, chọn phiên bản mới nhất mặc định.
- `src/Vaz.WinUI/Views/Controls/WizardStep2Loader.xaml(.cs)`: Cập nhật breadcrumb "Bước 2/3", nút điều hướng "Tiếp tục →", chọn Fabric mặc định.
- `src/Vaz.WinUI/Views/Controls/WizardVersionCard.xaml`: Đặt `IsHitTestVisible="False"` cho nội dung trong thẻ để nút card nhận click tin cậy.
- `scripts/capture-winui.ps1`: Tích hợp Win32 `BringToFront` (HWND_TOPMOST) đảm bảo chụp cửa sổ app không bị che khuất.

**Xóa:** Không xóa file logic cốt lõi nào.

## 4. Kết quả test

- **Mốc trước đợt (baseline)**: 390 passed, 0 failed, 0 skipped.
- **Sau đợt**: **390 passed, 0 failed, 0 skipped** (toàn bộ unit test suite giữ nguyên vẹn).
- **Kiểm tra tĩnh**: `npm run check:static` — **PASSED** (build pass, unit test pass, 12/12 agent config pass).

## 5. Bằng chứng

### Video demo hoàn chỉnh (~46s)
- **Đường dẫn**: `docs/evidence/dot18/videos/wizard-step3.mp4` (323 frames @ 7fps, 46.1s, 625 KB).
- **Kịch bản thực tế**:
  1. Khởi động app → Bấm "Tạo Instance mới" (Dialog open scale 0.92→1).
  2. Rê chuột qua các thẻ phiên bản (Card Hover Lift).
  3. Nhập tìm kiếm "1.21" (Search focus glow + search filter fade).
  4. Xóa tìm kiếm, chọn thẻ phiên bản 26.3 (Card click bounce + checkmark bounce).
  5. Bấm "Tiếp tục →" → Chuyển sang Step 2 (Slide transition + progress indicator dot 2 active).
  6. Chọn Fabric loader (Logo spin 360° + dropdown slide-down + live preview panel).
  7. Bấm "Tiếp tục →" → Chuyển sang Step 3 (Slide transition + progress indicator dot 3 active).
  8. Step 3 hiển thị: Thẻ preview lớn với gradient chuyển màu chậm (10s loop), tên tự sinh "Fabric 26.3 #x", nút "Tạo Instance" phát sáng (glow pulse).
  9. Bấm "Tạo Instance" → Nút morph sang loading pill, hiện spinner trắng và text tiến trình thật ("Đang lấy danh sách Fabric loader..." → "Đang cài Fabric...").
  10. Cài đặt xong → Hiển thị checkmark nảy "Đã tạo thành công!" trong 2.5s rồi dialog tự đóng.
  11. Màn hình chính xuất hiện instance mới tạo trong danh sách Instances.

### 8 Ảnh chụp bằng chứng
1. `docs/evidence/dot18/screenshots/wizard-step3-dark.png`: Step 3 ở Dark theme với thẻ preview 500px, logo Fabric, floating label "Tên instance" và nút Tạo phát sáng.
2. `docs/evidence/dot18/screenshots/wizard-step3-light.png`: Step 3 ở Light theme với nền trắng sáng, viền và thanh progress indicator đồng bộ.
3. `docs/evidence/dot18/screenshots/wizard-step3-loading.png`: Trạng thái loading (nút morph thành viên nang, spinner trắng, stage text từ installer thật).
4. `docs/evidence/dot18/screenshots/wizard-step3-success.png`: Trạng thái tạo thành công và cập nhật lên màn hình chính.
5. `docs/evidence/dot18/screenshots/wizard-transition-step1-to-2.png`: Khoảnh khắc chuyển bước từ Step 1 sang Step 2 (dot 1 và dot 2 kết nối).
6. `docs/evidence/dot18/screenshots/wizard-transition-step2-to-3.png`: Khoảnh khắc chuyển bước từ Step 2 sang Step 3 (cả 3 dots được nối đầy đủ).
7. `docs/evidence/dot18/screenshots/wizard-step3-confirm-card.png`: Cận cảnh preview card trung tâm với animated gradient.
8. `docs/evidence/dot18/screenshots/wizard-step3-pulse-glow.png`: Cận cảnh nút "Tạo Instance" với hiệu ứng pulse glow xanh ngọc.

## 6. Known issues

1. **Backdrop Blur**: Do giới hạn kiến trúc WinUI 3 `ContentDialog` trên Windows App SDK 1.5, backdrop của dialog không thể áp dụng blur xuyên thấu màn hình nền mà không cần can thiệp window subclassing/DwmEnableBlurBehindWindow cấp Win32.
2. **Dung lượng ước tính ở Step 3**: Được tính dựa trên profile loader đã chọn (Vanilla ~214MB, Fabric ~226MB, Quilt ~231MB, NeoForge ~282MB). Khi tải thật, dung lượng có thể thay đổi tùy thuộc vào cache Mojang hiện có trên máy.

## 7. Đề xuất bước tiếp theo

1. Bổ sung Step tùy chọn: Cấu hình RAM & JVM nâng cao ngay trước khi bấm Tạo (hoặc mở rộng vào Step 3).
2. Thêm hiệu ứng âm thanh click nhẹ (UI sound feedback) khi chuyển bước và khi tạo thành công.
3. Hỗ trợ phím tắt bàn phím (Enter để tiếp tục, Esc/Backspace để quay lại).

## 8. Self-review

- Đã tuân thủ nghiêm ngặt nguyên tắc không làm hỏng 390 test hiện tại và 100% dùng ThemeResource.
- Rủi ro nhỏ: Trên máy có độ phân giải rất thấp (dưới 720p chiều cao), dialog 540px có thể chiếm phần lớn màn hình; đã thiết lập MaxHeight 700px và ScrollViewer tự động để đảm bảo luôn cuộn được.
- **CHƯA KIỂM TRA**: Trải nghiệm thực tế bằng mắt trên màn hình người dùng và cảm nhận độ mượt chuyển động tay.

### Kịch bản kiểm tra thủ công (Manual Test Script)
1. Mở launcher V.A.Z.
2. Nhấn nút "Tạo Instance mới" ở góc trên bên phải → Dialog mở ra với hiệu ứng nảy nhẹ (Step 1).
3. Di chuột qua các thẻ phiên bản → Thẻ hơi nâng lên (scale 1.05) và viền sáng màu accent.
4. Gõ "1.21" vào ô tìm kiếm → Danh sách mượt mà lọc lại chỉ còn các bản 1.21.
5. Xóa ô tìm kiếm, nhấp chọn bản "26.3" → Xuất hiện dấu tích xanh lá ở góc trên thẻ, nút "Tiếp tục →" sáng lên.
6. Bấm "Tiếp tục →" → Giao diện trượt sang trái, thanh tiến trình trên đỉnh sáng dot số 2 (Step 2).
7. Nhấp vào thẻ "Fabric" → Logo xoay 360 độ, ô chọn version loader trượt nhẹ xuống từ trên.
8. Bấm "Tiếp tục →" → Giao diện trượt tiếp sang Step 3, thanh tiến trình sáng trọn vẹn cả 3 dots (Step 3).
9. Xem thẻ xem trước: Logo Fabric cam lớn, dòng "Minecraft 26.3", tên gợi ý sẵn "Fabric 26.3 #x", nút "Tạo Instance" phát sáng nhịp thở.
10. Bấm "Tạo Instance" → Nút thu gọn lại, hiện vòng quay trắng và chữ "Đang cài Fabric...", sau đó báo "Đã tạo thành công!" và tự động đóng, instance mới xuất hiện trên danh sách chính.
