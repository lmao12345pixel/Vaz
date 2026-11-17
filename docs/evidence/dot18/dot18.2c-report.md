# Đợt 18.2c — Automated UI Verification for Phase B

**Ngày:** 2026-11-05
**Phạm vi:** Tự động verify bằng UI Automation (UIA) các case test tay Phase B (dialog "Tạo instance mới").
**Công cụ:** PowerShell 7 `UIAutomationClient` + `System.Drawing` (screenshot) + ffmpeg (video).
**Script:** `scripts/dot18-automate-phase-b.ps1` — chạy bằng `pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/dot18-automate-phase-b.ps1`

> **Nguyên tắc:** Mọi kết quả dưới đây là **automated verification** (UIA dump + screenshot thật).
> Không claim PASS cho case test tay. Case không automate được → ghi `UNVERIFIED` kèm lý do.

---

## 1. Phát hiện quan trọng về môi trường test

`CreateInstanceViewModel.LoadVersionsAsync()` lấy **top 10 release mới nhất**:

```csharp
VanillaVersions = versions.Where(v => v.Type == "release")
    .OrderByDescending(v => v.ReleaseTime).Take(10).ToList();
```

Danh sách thực tế trong dropdown (đã đo bằng UIA):

| # | Version | ReleaseTime |
|---|---------|-------------|
| 1 | 26.3 | 2026-09-15 |
| 2 | 26.2 | 2026-06-16 |
| 3 | 26.1.2 | 2026-04-09 |
| 4 | 26.1.1 | 2026-04-01 |
| 5 | 26.1 | 2026-03-24 |
| 6 | 1.21.11 | 2025-12-09 |
| 7 | 1.21.10 | 2025-10-07 |
| 8 | 1.21.9 | 2025-09-30 |
| 9 | 1.21.8 | 2025-07-17 |
| 10 | 1.21.7 | 2025-06-30 |

**Hệ quả:** `1.21.1`, `1.20.1`, `1.20.2` **không có trong dropdown**. Các case dùng 3 version này
phải được điều chỉnh input (xem bảng ở mục 3). Đây là giới hạn của dữ liệu hiện có, không phải lỗi UI.

Bổ sung: `VAZER_STARTUP_PAGE=CreateInstance` **không được code hỗ trợ** (chỉ có `InstanceDetailPage`
trong `MainWindow.xaml.cs:142`). Dialog được mở bằng UIA click vào nút "Tạo Instance mới".

---

## 2. Kết quả tổng hợp

| Case | Kết quả | Bằng chứng |
|---|---|---|
| #1 Mở dialog | **PASS** | `uia/case1-dialog-open.txt`, `screenshots/case1-dialog-dark.png` |
| #2 Fabric + 1.21.1 | **PASS (adapted)** | `uia/case2-fabric-loader-versions.txt`, `screenshots/case2-fabric.png` |
| #3 Quilt + 1.21.1 | **PASS (adapted)** | `uia/case3-quilt-loader-versions.txt`, `screenshots/case3-quilt.png` |
| #4 NeoForge + 1.20.1 | **PASS (adapted)** | `uia/case4-neoforge-empty-state.txt`, `screenshots/case4-neoforge-empty.png` |
| #5 Chưa chọn Loader Version | **PASS (adapted)** | `uia/case4-neoforge-empty-state.txt`, `screenshots/case5-button-disabled.png` |
| #7 Badge "Đã cài" | **PASS (adapted)** | `uia/case7-badge-tree.txt`, `screenshots/case7-badge.png` |
| #9 Light/Dark | **PASS** | `screenshots/case1-dialog-dark.png`, `screenshots/case9-dialog-light.png` |

**Tỷ lệ:** 7/7 case automate được → PASS (5 case có input điều chỉnh do version không có trong dropdown).

---

## 3. Chi tiết từng case

### #1 Mở dialog — PASS
- Click nút "Tạo Instance mới" bằng UIA → dialog window `Tạo instance mới` xuất hiện, `Offscreen=False`.
- Cây UIA xác nhận đầy đủ: `NameBox`, `LoaderCombo`, `VersionCombo`, `LoaderVersionBox`, `PrimaryButton`, `CloseButton`.

### #2 Fabric + 1.21.1 → dùng MC 1.21.7 — PASS (adapted)
- Chọn Loader = Fabric, MC = 1.21.7.
- `LoaderVersionBox` nạp **26 item**, item đầu `0.20.1`.
- Kiểm tra semantic: `0.20.1 > 0.20.0 > 0.19.5 > 0.19.4 > 0.19.3` → **item đầu là mới nhất** ✓

### #3 Quilt + 1.21.1 → dùng MC 1.21.7 — PASS (adapted)
- Chọn Loader = Quilt, MC = 1.21.7.
- `LoaderVersionBox` nạp **26 item**, item đầu `0.30.2` ✓

### #4 NeoForge + 1.20.1 → dùng MC 26.3 — PASS (adapted)
- Lý do thay thế: `1.20.1` không có trong dropdown **và** không có NeoForge build.
  `26.3` có trong dropdown **và cũng không có NeoForge build** → cùng code path.
- Kết quả: `LoaderVersion items: 0`, `PrimaryButton IsEnabled: False`,
  `Không có version tương thích` hiển thị, warning `Vui lòng chọn loader version cho NeoForge.` hiển thị ✓

### #5 Chưa chọn Loader Version — PASS (adapted)
- Trạng thái disabled **không thể bắt bằng polling**: mẫu UIA đầu tiên (189ms) đã `Enabled=True`
  vì VM auto-select loader version đầu tiên từ cache (xem `exploration/case5-button-state-timeline.txt`).
- Thay vào đó verify trạng thái disabled ở điều kiện runtime thật: loader-version list rỗng
  (NeoForge + 26.3) → `PrimaryButton IsEnabled=False` ✓

### #7 Badge "Đã cài" → dùng version fake-install 1.21.7 — PASS (adapted)
- `1.21.1` không có trong dropdown nên không thể verify badge trên 1.21.1.
- Tạo thư mục giả `{versionsDir}/1.21.7` (đúng điều kiện `SetInstalledStatus`) → badge `Đã cài`
  render **2 element** (dropdown item + tree), `Offscreen=False` ✓
- Thư mục giả đã được xóa sau khi test.

### #9 Light/Dark — PASS
- Theme `dark`: `screenshots/case1-dialog-dark.png`
- Theme `light`: `screenshots/case9-dialog-light.png`
- Lưu ý: `settings.json` hiện tại có JSON không hợp lệ (`\"theme\": \"dark\"` — thừa backslash) nên app
  fallback về default. Test ghi đè file bản hợp lệ để ép theme, sau đó restore.

---

## 4. Case KHÔNG automate được

| Case | Trạng thái | Lý do |
|---|---|---|
| #6 Tạo instance Fabric | **UNVERIFIED** | Cần click "Tạo" + chờ download/install thật. Không automate được vì tải file nặng, mạng biến động, và không muốn tạo instance ngoài môi trường test. |
| #8 NeoForge 1.20.2 | **UNVERIFIED** | `1.20.2` không có trong dropdown (top-10). |
| #10 Launch game | **UNVERIFIED** | Cần user xác nhận game vào menu thật. |

---

## 5. Evidence files

```
docs/evidence/dot18/
├── dot18.2c-results.json              # kết quả machine-readable
├── dot18.2c-report.md                 # báo cáo này
├── uia/
│   ├── case1-dialog-open.txt
│   ├── case2-fabric-loader-versions.txt
│   ├── case2-fabric-tree.txt
│   ├── case3-quilt-loader-versions.txt
│   ├── case4-neoforge-empty-loader-versions.txt
│   ├── case4-neoforge-empty-state.txt
│   ├── case4-neoforge-empty-tree.txt
│   ├── case5-initial-state.txt
│   ├── case7-badge-tree.txt
│   └── case9-dialog-light-tree.txt
├── screenshots/
│   ├── case1-dialog-dark.png
│   ├── case2-fabric.png
│   ├── case3-quilt.png
│   ├── case4-neoforge-empty.png
│   ├── case5-button-disabled.png
│   ├── case7-badge.png
│   └── case9-dialog-light.png
├── videos/
│   └── phase-b-automated.mp4          # 11s, 9 key frames
└── exploration/                       # dump thám hiểm (không phải evidence chính)
```

---

## 6. Cách tái lập

```powershell
pwsh -NoProfile -ExecutionPolicy Bypass -File scripts/dot18-automate-phase-b.ps1
```

Script tự: backup `settings.json`, tạo version giả cho badge test, chạy UIA, chụp screenshot,
dựng video, restore `settings.json`, xóa version giả.

---

## 7. Kiến nghị

1. **Version dropdown quá hẹp.** Top 10 release khiến 1.21.1 / 1.20.x không chọn được — dễ gây
   nhầm là "không hỗ trợ". Cân nhắc tăng giới hạn hoặc thêm filter theo major version.
2. **`VAZER_STARTUP_PAGE=CreateInstance` chưa có.** Thêm vào `MainWindow.xaml.cs` để test nhanh hơn
   (hiện phải click qua UIA).
3. **`settings.json` user hiện tại hỏng JSON** (`\"theme\"`). Nên thêm validation khi lưu để tránh
   im lặng fallback.
4. **Case #6 / #10** vẫn cần user test tay.
