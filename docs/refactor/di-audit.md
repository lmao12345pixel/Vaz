# DI Audit & C2.2 Refactor Report

**Ngày:** 2026-11-09  
**HEAD checkpoint:** `v0.19.0-rc1` (`46b3a89`)  
**Test gate:** 82/82 Batch 1 pass | 464/464 Total pass | Warning: 0 (Debug + Release)  

---

## 1. Tổng quan & Cập nhật Known Issue

| Metric | Giá trị trước audit | Thực tế sau C2.1 / C2.2 |
|---|---|---|
| Tổng số chỗ DI null-related | 91 (ước lượng cũ) | **16** chỗ chính xác |
| `= null!` (suppress nullable) | — | 3 (1 đã chuyển `required`, 2 WinUI static lifecycle) + 1 fallback 2a |
| `private readonly T? _field` (nullable service injection) | — | 12 |
| Phân bố Nhóm A (Core services) | — | 7 |
| Phân bố Nhóm B (VM có lifecycle) | — | 3 |
| Phân bố Nhóm C (WinUI code-behind / singleton) | — | 6 |
| **Intentional optional (giữ nguyên)** | — | **7** |

> **Cập nhật Known Issue:** "DI ? = null 91 chỗ" → "16 chỗ thực tế (A=7, B=3, C=6); trong đó 7 chỗ là intentional optional, 1 model property đã refactor sang `required`, 1 VM row giữ `= null!` do WinUI XamlTypeInfo generator, 2 static fields giữ lifecycle WinUI".

---

## 2. Intentional Optional Services — KHÔNG refactor bắt buộc non-null

7 vị trí sau đây được thiết kế nullable có chủ đích (optional by design) và có fallback path rõ ràng:

1. **`MsalAuthProvider._msal` (`IMsalClientWrapper?`)**:
   - *Lý do:* Cho phép chạy ở chế độ `OfflineDemoMode = true` (không cần `MsalClientId`) và cho phép unit test inject mock MSAL wrapper mà không cần tương tác Microsoft Identity thật.
   - *Fallback:* Khi null, login online ném lỗi rõ ràng (`InvalidOperationException`).

2. **`ForgeInstaller._javaResolver` (`IJavaResolver?`)**:
   - *Lý do:* Cho phép installer chạy độc lập mà không bắt buộc có resolver dịch vụ. Unit test kiểm thử trực tiếp nhánh null.
   - *Fallback:* Tự động quét Java runtime từ biến môi trường `PATH` hệ thống.

3. **`NeoForgeInstaller._javaResolver` (`IJavaResolver?`)**:
   - *Lý do:* Tương tự `ForgeInstaller`, hỗ trợ chạy fallback khi không có resolver hoặc trong test harness.
   - *Fallback:* Quét Java runtime từ `PATH` hệ thống.

4. **`OptifineInstallerService._modrinthClient` (`IModrinthClient?`)**:
   - *Lý do:* Cho phép cài đặt trực tiếp file OptiFine JAR độc lập mà không cần kết nối mạng đến Modrinth API.
   - *Fallback:* Khi null, bỏ qua tìm kiếm OptiFabric adapter trên Modrinth.

5. **`CmlGameLauncher._javaCompatibilityNotifier` (`IJavaCompatibilityNotifier?`)**:
   - *Lý do:* Hook UI thông báo phiên bản Java không tương thích; cho phép launcher chạy ở chế độ headless hoặc unit test không có UI.
   - *Fallback:* Bỏ qua hiển thị thông báo popup, tiếp tục launch game.

6. **`FileDownloader._httpClientFactory` (`IHttpClientFactory?`)**:
   - *Lý do:* Hỗ trợ fallback khi downloader được khởi tạo đơn lẻ không qua DI container.
   - *Fallback:* Sử dụng `HttpClient` mặc định nội bộ.

7. **`SettingsViewModel._javaResolver` / `_versionService`**:
   - *Lý do:* Tách biệt ngữ cảnh UI, tránh crash khi service chưa sẵn sàng ở design-time hoặc test runner.

---

## 3. Kết quả Refactor C2.2 — Batch 1 (4 Core Services)

### Bảng phân tích & Quyết định Bước 1 - Bước 2:

| Service | File & Line | DI Registration | Test truyền null? | Bắt buộc non-null (X)? | Quyết định C2.2 | Lý do |
|---|---|---|---|---|---|---|
| `MsalAuthProvider` | `MsalAuthProvider.cs:45` | `MinecraftServiceCollectionExtensions.cs:89` | **CÓ** (`CreateOfflineProvider`) | Không | **(Y)** Giữ nullable + XML doc | Bắt buộc X sẽ làm vỡ `OfflineDemoMode` và 1 unit test |
| `ForgeInstaller` | `ForgeInstaller.cs:48` | `MinecraftServiceCollectionExtensions.cs:100` | **CÓ** (`ForgeInstallerTests:38`) | Không | **(Y)** Giữ nullable + XML doc | Bắt buộc X sẽ làm vỡ PATH fallback và unit tests |
| `NeoForgeInstaller` | `NeoForgeInstaller.cs:34` | `MinecraftServiceCollectionExtensions.cs:99` | **CÓ** (`NeoForgeInstallerTests:60`) | Không | **(Y)** Giữ nullable + XML doc | Bắt buộc X sẽ làm vỡ unit tests |
| `OptifineInstallerService` | `OptifineInstallerService.cs:30` | `MinecraftServiceCollectionExtensions.cs:127` | **CÓ** (`OptifineInstallerTests:75`) | Không | **(Y)** Giữ nullable + XML doc | Bắt buộc X sẽ làm vỡ 14/16 unit tests |

### Thực hiện Bước 3:
- Bổ sung XML documentation đầy đủ cho constructor của cả 4 services, mô tả chi tiết tham số nullable và fallback behavior khi null.

---

## 4. Xử lý 4 vị trí `= null!` (Bước 4)

### 4a. Kiểm chứng System.Text.Json với `required`:
- Đã viết unit test `SystemTextJson_MissingRequiredProperty_ThrowsJsonException` trong `ModelsTests.cs`.
- **Kết quả:** .NET 8 `JsonSerializer.Deserialize` ném `JsonException` chuẩn xác khi JSON thiếu thuộc tính `required`.

### 4b/4c. Refactor Model & ViewModel Row:
- **`Vazer.Core/Mods/Models.cs:52` (`CurrentMod`)**:
  - Đổi từ `public InstalledMod CurrentMod { get; init; } = null!;` sang `public required InstalledMod CurrentMod { get; init; }`.
  - Compile sạch 0 warning.
- **`Vaz.WinUI/ViewModels/ModBrowserViewModel.cs:90` (`SearchHitRow.Hit`)**:
  - **Edge case WinUI 3:** Trình biên dịch XAML WinUI 3 (`XamlCompiler.exe`) tự sinh code `XamlTypeInfo.g.cs` với `Activate_..._SearchHitRow() => new SearchHitRow()`. Nếu đặt `required`, sinh lỗi `CS9035`. Nếu đặt `[SetsRequiredMembers]`, sinh cảnh báo `CS8618`.
  - **Áp dụng Fallback 2a:** Giữ `= null!`, kèm comment rõ ràng:
    `public SearchHit Hit { get; init; } = null!; // TODO: required + JsonRequired after verify — WinUI XamlTypeInfo requires parameterless instantiation without CS8618`.

### 4d. Static App Lifecycle:
- **`App.xaml.cs:21` (`Dispatcher`)** và **`App.xaml.cs:24` (`MainWindow`)**:
  - Giữ `= null!`, bổ sung inline comment: `// Assigned in OnLaunched (WinUI lifecycle)`.

---

## 5. Gate Nghiệm thu

- [x] **82/82 test riêng Batch 1:** PASS (Msal: 14, Forge: 27, NeoForge: 23, Optifine: 18).
- [x] **464/464 test tổng solution:** PASS (463 gốc + 1 test System.Text.Json `required`).
- [x] **Compiler Warnings:** 0 warning (cả Debug và Release).
- [x] **Documentation:** Hoàn thiện `docs/refactor/di-audit.md`.
