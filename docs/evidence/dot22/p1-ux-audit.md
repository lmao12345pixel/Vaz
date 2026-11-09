# Báo cáo Audit 3 Vấn đề UX Install — VAZ Launcher

**Ngày:** 2026-11-09  
**Người thực hiện:** Raez  
**Đợt:** P1.1 (Audit UX install — KHÔNG sửa code)  
**Tiền đề:** P0.1 PASSED (commit `e98b18f`). C2.2 PASSED. Tag `v0.19.0-rc1`.

---

## 1. Tổng quan 3 vấn đề & Mức độ nghiêm trọng

| Mã | Vấn đề | Hiện tượng | Mức độ |
|---|---|---|---|
| **V1** | **Feedback cài đặt không đồng bộ** | Mod có progress bar; Resource Pack có toast to; Shader chỉ có toast "đã hoàn thành" rất bé, dễ miss. | **P1 (UX Inconsistency)** |
| **V2** | **Mất trạng thái cài đặt sau restart** | RP & Shader cài từ Modrinth catalog, sau khi restart app: tab trong Instance Detail báo chưa cài, nút trên Modrinth Catalog quay lại "Install" thay vì "Installed". | **P0 (Critical Persistence Bug)** |
| **V3** | **Shader mất ảnh preview sau khi cài** | Trước khi cài: Modrinth browser có ảnh minh họa 16:9. Sau khi cài: thẻ shader trong thư viện trống trơn, khung xám rỗng, xảy ra với toàn bộ shader. | **P1 (Visual Defect)** |

---

## 2. V1 — Audit Feedback UX Install

### 2.1. Phân tích luồng cài đặt từng loại
1. **Mod (`ModBrowserView.xaml`):**
   - **Vị trí code:** `ModBrowserView.xaml:211-230`, `ModBrowserViewModel.cs:891-933`.
   - **Thành phần:** Top bar có `ProgressBar` (Height=4, Maximum=100, bind `ViewModel.ProgressPercent`), `TextBlock` hiển thị `%` + trạng thái tải, nút "Hủy" bind `CancelInstallCommand`.
   - **Notification:** Đồng thời phát `_notificationService.ShowProgress` hiển thị toast tiến trình ở góc dưới màn hình. Khi xong, hiện toast `ShowSuccess` kèm icon mod (44x44px).
2. **Resource Pack (`ModrinthCatalogView.xaml`):**
   - **Vị trí code:** `ModrinthCatalogView.xaml:157-162`, `ModrinthCardView.xaml:239-252`.
   - **Thành phần:** Header **hoàn toàn không có ProgressBar**, chỉ có một `ProgressRing` bé (14x14px) nằm ở thanh phân trang. Trên từng thẻ (`ModrinthCardView`), nút chuyển sang `InstallingPanel` (chứa `ProgressRing` 14x14 và chữ "Đang tải...").
   - **Notification:** Phát toast qua `_notificationService.ShowProgress`. Khi xong, phát `ShowSuccess`. Resource Pack thường có `icon_url` từ Modrinth nên toast kích hoạt `RichMediaContainer` (44x44px), hiển thị to rõ ràng.
3. **Shader (`ModrinthCatalogView.xaml`):**
   - **Vị trí code:** `ModrinthCatalogView.xaml:157-162`, `ModBrowserViewModel.cs:930-933`, `NotificationToast.xaml:44-53`.
   - **Lý do toast Shader "rất bé, dễ miss":**
     - Modrinth API cho Shader **hầu như không có trường `icon_url`** (trả về `null`/rỗng, chỉ có mảng `gallery` chứa ảnh chụp màn hình).
     - Khi `row.IconUrl` rỗng, trong `NotificationToast.xaml:44-53`, `RichMediaContainer` bị ẩn hoàn toàn (`Visibility="Collapsed"`).
     - Chiều cao toast co lại mức tối thiểu (chỉ còn icon glyph 16px và 2 dòng chữ ngắn).
     - Text hoàn tất bị hardcode: `"Mod đã sẵn sàng trong instance."` (thay vì "Shader").
     - Toàn bộ giao diện duyệt không có bất kỳ thanh ProgressBar tiến độ nào hiển thị phần trăm dung lượng tải.

### 2.2. Bảng so sánh Feedback hiện tại

| Loại item | Feedback tiến độ trong trang | Feedback Toast | Component Toast | File:Line | Đề xuất |
|---|---|---|---|---|---|
| **Mod** | `ProgressBar` 4px + % + nút Hủy | Progress Toast + Success Toast (có icon 44x44) | `NotificationToast.xaml` | `ModBrowserView.xaml:218`, `ModBrowserViewModel.cs:891` | Giữ nguyên (chuẩn mực). |
| **Resource Pack** | Chỉ có `ProgressRing` 14x14 trên Card | Progress Toast + Success Toast (thường có icon) | `NotificationToast.xaml` | `ModrinthCatalogView.xaml:157`, `ModrinthCardView.xaml:245` | Bổ sung `ProgressBar` vào Header của `ModrinthCatalogView` như Mod. |
| **Shader** | Chỉ có `ProgressRing` 14x14 trên Card | Toast co nhỏ do thiếu icon, không có ảnh | `NotificationToast.xaml` | `ModBrowserViewModel.cs:930`, `NotificationToast.xaml:50` | Giữ nguyên Notification (không dùng progress bar trong catalog theo Đề xuất C của Master), nhưng toast phải to: fallback lấy ảnh gallery/cover vào `RichMediaContainer` + sửa text "Shader đã sẵn sàng". |

---

## 3. V2 — Audit Persistence (Ưu tiên cao nhất)

### 3.1. Tìm nơi lưu installed state
- **Thực tế:** **HOÀN TOÀN KHÔNG CÓ CODE LƯU MANIFEST CHO RP VÀ SHADER.**
- Bằng chứng code: `src/Vaz.WinUI/Services/ModDownloadManager.cs:185-271` (`InstallPackOrShaderAsync`).
  - Hàm chỉ thực hiện: tải file zip về `instance.Path/resourcepacks` hoặc `instance.Path/shaderpacks`, đổi tên từ `.partial` sang `.zip`.
  - Không gọi bất kỳ hàm lưu manifest nào (khác hoàn toàn với Mod: Mod gọi `ModInstallService.InstallFromModrinthAsync` ghi vào `instancePath/mods.json`).
  - Toàn bộ metadata Modrinth (`ProjectId`, `Slug`, `VersionId`, `Title`, `IconUrl`) bị bỏ rơi ngay sau khi hàm hoàn tất.

### 3.2. Tìm nơi đọc installed state khi mở app / restart
- **Trong Browser / Catalog (`ModBrowserViewModel.cs:827-848`):**
  - Gọi `_downloadManager.CheckInstallStateAsync` -> `IsModInstalledOnDiskAsync` (`ModDownloadManager.cs:51-95`).
- **Logic kiểm tra trên đĩa (`ModDownloadManager.cs:80-88`):**
  ```csharp
  var slugLower = slug.Trim().ToLowerInvariant();
  var projIdLower = projectId?.Trim().ToLowerInvariant();
  var files = Directory.GetFiles(targetFolder, "*.zip");
  foreach (var file in files)
  {
      var fileName = Path.GetFileName(file).ToLowerInvariant();
      if (fileName.Contains(slugLower) || (!string.IsNullOrEmpty(projIdLower) && fileName.Contains(projIdLower)))
      {
          return true;
      }
  }
  ```

### 3.3. Root Cause chính xác của V2
1. **Heuristic Filename Match bị sai:**
   - File zip tải từ Modrinth giữ nguyên tên gốc do tác giả đặt trên Modrinth CDN (`file.Filename`).
   - Ví dụ:
     - Shader `BSL Shaders`: slug là `bsl-shaders`, ID là `m0uik42B`. Tên file tải về là `BSL_v8.2.09.zip`.
     - Shader `Complementary Reimagined`: slug là `complementary-reimagined`. Tên file tải về là `ComplementaryReimagined_r5.2.2.zip`.
     - Resource Pack `Bare Bones`: slug là `bare-bones`. Tên file là `Bare+Bones+1.21.zip`.
   - Tên file zip thực tế **hoàn toàn không chứa chuỗi `slug`** (do slug có dấu gạch ngang `-`, file zip dùng camelCase/gạch dưới) và **không chứa `projectId`** (ID là chuỗi hash ngẫu nhiên).
   - Vì vậy, câu lệnh `fileName.Contains(slugLower) || fileName.Contains(projIdLower)` **luôn trả về `false`**.
   - **Kết quả:** Sau khi restart app hoặc mở lại browser, launcher kiểm tra lại đĩa không thấy file khớp -> nút lập tức reset về "Cài đặt" (Install) thay vì "Đã cài" (Installed).
2. **Trong Tab Resource Packs / Shaders của Instance Detail:**
   - Khi khởi động, `LoadResourcePacksCore()` (`ResourcePacksShaders.cs:214`) và `LoadShadersCore()` (`ResourcePacksShaders.cs:518`) **luôn chèn cứng danh sách mẫu** `DefaultResourcePacks` và `DefaultShaders` từ `ResourcePackShaderData.cs`.
   - File zip thực tế được quét thêm vào với `Id = Path.GetFileNameWithoutExtension(fName).ToLowerInvariant()`. Không có liên kết metadata, xem như item tự do.
   - Thêm vào đó, badge `InstalledBadge` trên mỗi card (`InstanceDetailPage.xaml:1034, 1387`) bị set cứng `Visibility="Collapsed"`, chỉ được bật bằng animation nhất thời khi vừa import trong cùng phiên chạy; khi restart app tải lại từ đĩa, badge này không bao giờ hiển thị.

---

## 4. V3 — Audit Shader Preview

### 4.1. Nguồn nạp preview trước và sau khi cài
- **Trước khi cài (Modrinth Catalog):**
  - Đọc từ Modrinth Search API (`SearchHitRow.IconUrl` hoặc gallery). URL online dạng `https://cdn.modrinth.com/...` được truyền trực tiếp vào `BitmapImage` trong `ModrinthCardView.xaml:101`. Hiển thị đầy đủ ảnh minh họa.
- **Sau khi cài (Instance Detail - Tab Shaders):**
  - XAML hiển thị: `InstanceDetailPage.xaml:1367-1370` bind vào `ShaderPackRow.ImageUriSource`.
  - Code nạp từ đĩa: `InstanceDetailViewModel.ResourcePacksShaders.cs:538-550`:
    ```csharp
    var item = new ShaderPackItem
    {
        Id = Path.GetFileNameWithoutExtension(fName).ToLowerInvariant(),
        Name = Path.GetFileNameWithoutExtension(fName),
        Author = "Tùy biến",
        Description = "Shader pack đã cài đặt.",
        Style = "realistic",
        StyleLabel = "Thực tế",
        Version = "1.0.0",
        Size = sizeStr,
        AccentColor = "#3b82f6"
        // TRƯỜNG ImageUri BỊ BỎ RỖNG HOÀN TOÀN!
    };
    ```

### 4.2. Root Cause của V3
- **Không lưu URL và không cache ảnh:**
  - Shader pack zip không có quy chuẩn file ảnh đại diện bên trong file zip (không có `pack.png` như Resource Pack). Ảnh của shader chỉ nằm trên Modrinth CDN.
  - Khi tải shader, `ModDownloadManager.InstallPackOrShaderAsync` chỉ tải file zip, không tải ảnh thumbnail/cover về đĩa, cũng không lưu URL ảnh vào bất kỳ đâu.
  - Khi app quét thư mục `shaderpacks/*.zip`, nó tạo `ShaderPackItem` với `ImageUri = string.Empty` -> `ImageUriSource` trả về `null` -> Thẻ shader trên giao diện hoàn toàn không có ảnh, biến thành khung xám rỗng.

---

## 5. Đối chiếu chéo V2 và V3: Độc lập hay Cùng gốc?

- **Kết luận:** **V2 và V3 CÙNG MỘ NGUYÊN NHÂN GỐC (Single Root Cause): Thiếu hệ thống Manifest Persistence cho Resource Packs và Shaders.**
- **So sánh với Mod:**
  - Mod có `mods.json` lưu: `FileName`, `ProjectId`, `VersionId`, `Source`.
  - Nhờ có manifest, Mod kiểm tra cài đặt chính xác 100% bằng `ProjectId`, không phụ thuộc vào việc tên file `.jar` có chứa slug hay không.
- **Với RP và Shader:**
  - Hiện tại hoàn toàn không có file manifest (`resourcepacks.json` hay `shaderpacks.json`).
  - Dẫn đến V2: Phải đoán mò tên file zip -> đoán sai -> mất trạng thái "Installed".
  - Dẫn đến V3: Không có nơi lưu URL ảnh cover / đường dẫn thumbnail cache -> mất ảnh preview sau khi tải.
- **Giải pháp tối ưu:** **Gộp chung xử lý V2 và V3 vào cùng một giải pháp Manifest.**

---

## 6. Đề xuất Hướng xử lý & Phân kỳ đợt tiếp theo

### 6.1. Kiến trúc Manifest đề xuất cho RP/Shader (Giải quyết triệt để V2 + V3)
Tạo cơ chế lưu manifest (tương tự `ModsManifest` của `ModInstallService`):
- File lưu: `instancePath/resourcepacks.json` và `instancePath/shaderpacks.json` (hoặc gộp trong `packs.json`).
- Schema item lưu:
  ```json
  {
    "fileName": "BSL_v8.2.09.zip",
    "projectId": "m0uik42B",
    "slug": "bsl-shaders",
    "title": "BSL Shaders",
    "versionId": "...",
    "imageUri": "https://cdn.modrinth.com/..." (hoặc đường dẫn cache local),
    "author": "Capt Tatsu",
    "installedAt": "2026-11-09T..."
  }
  ```
- **Khi tải (P1.2):** Lưu metadata vào manifest; tải kèm ảnh thumbnail lưu vào cache local `instancePath/shaderpacks/.cache/<slug>.jpg`.
- **Khi kiểm tra cài đặt (V2):** Tra cứu trực tiếp theo `projectId` hoặc `slug` trong manifest. Khớp 100%, không bị ảnh hưởng bởi tên file zip.
- **Khi hiển thị thư viện (V3):** Đọc `imageUri` từ manifest để bind lên card. Giữ nguyên 100% ảnh minh họa sau restart.
- **Dọn dẹp:** Bỏ nạp cứng `DefaultShaders` / `DefaultResourcePacks` mẫu vào instance thật của người dùng.

### 6.2. Phân kỳ kế hoạch

| Đợt | Trọng tâm | Phạm vi | Ước lượng |
|---|---|---|---|
| **P1.2 (P0)** | **Manifest & Persistence (Fix triệt để V2 + V3)** | Tạo `PacksManifest`, cập nhật `ModDownloadManager.InstallPackOrShaderAsync`, sửa `IsModInstalledOnDiskAsync` tra cứu manifest, cập nhật `LoadShadersCore` nạp `ImageUri` từ manifest. | 1 phiên |
| **P1.3 (P1)** | **Đồng bộ UX Feedback & Toast (Fix V1 theo Đề xuất C)** | Thêm `ProgressBar` vào Header `ModrinthCatalogView` cho Resource Pack; Cải tiến `NotificationService.ShowSuccess` cho Shader (lấy ảnh cover, sửa message, điều chỉnh chiều cao toast không bị co bé). | 0.5 phiên |
| **P1.4 (P2)** | **Verify & Clean up** | Xóa mock data `DefaultShaders/DefaultResourcePacks`, chạy full test suite + kịch bản test tay của Master. | 0.5 phiên |

---

## 7. Câu hỏi mở cho Master (Không tự đoán)

1. **Về vị trí lưu Manifest:** Master muốn lưu thành 2 file riêng biệt `resourcepacks.json` và `shaderpacks.json` nằm trong thư mục instance, hay gộp chung vào 1 file `installed-packs.json`? (Khuyên: 2 file riêng biệt hoặc 1 file `packs.json` theo chuẩn `mods.json`).
2. **Về lưu ảnh Thumbnail Shader:** Khi tải Shader về, launcher nên lưu URL online của Modrinth vào manifest để load trực tiếp qua internet, hay tải luôn file ảnh `.jpg` về thư mục cache cục bộ của instance (để xem được cả khi offline)? (Khuyên: tải cache local để offline vẫn thấy ảnh).
