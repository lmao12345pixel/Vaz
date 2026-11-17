# Task: Quản lý Resource Packs & Shaders chuyên sâu (Port WinUI)

## Trạng thái
- Phase: `PARITY_PARTIAL (REMEDIATION COMPLETE)`
- Contract: `docs/contracts/resource-packs-shaders.contract.md`
- Feature spec: `docs/product/features/resource-packs-shaders.feature.md`
- Parity checklist: `.agent/parity/resource-packs-shaders.parity.md`
- Visual Evidence Report: `docs/evidence/resource-packs-shaders-report.md`
- Owner: app-developer, app-verifier

## Bảng tiêu chí nghiệm thu (WinUI)
| # | Acceptance criterion | Evidence type | Cách kiểm trên WinUI | Status |
|---|----------------------|---------------|----------------------|--------|
| RPS-AC-01 | Tab Resource Packs hiển thị danh sách packs với tên, icon, độ phân giải, dung lượng, toggle on/off | RUNTIME | TabViewItem Resource Packs, 2 cột (Khả dụng vs Đang dùng), ListView card đầy đủ metadata | PARTIAL |
| RPS-AC-02 | Đổi thứ tự ưu tiên (Priority Reorder) bằng nút Move Up/Down, lưu vào state | RUNTIME | Nút Move Up/Down (`^`, `v`), vô hiệu hóa nút biên, menu flyout `...` | PASS |
| RPS-AC-03 | Cảnh báo tương thích (Incompatible Warning badge) nếu format không khớp MC version | RUNTIME | Badge cảnh báo `⚠ Không tương thích` trên Dokucraft Light (format 15) | PASS |
| RPS-AC-04 | Tab Shaders hiển thị danh sách shaderpack, chọn active shader hoặc tắt shader, preview ảnh | RUNTIME | TabViewItem Shaders, 8 shader cards, highlight active, nút Vanilla disabled khi không có shader active | PASS |
| RPS-AC-05 | Chuyển đổi Profile Shader (Low, Medium, High, Ultra) tức thì | RUNTIME | Category filter chips lọc shaders kèm counter cập nhật tức thì | PARTIAL |
| RPS-AC-06 | Tự động phát hiện thiếu Shader Loader, nút 1-click cài đặt Iris và chuyển sang Sẵn sàng | RUNTIME | Shader Loader Banner kèm 1-click cài đặt Iris, 19 unit tests trong `ShaderLoaderResolverTests.cs`, Inspector drawer overlay 380px | PASS |
| RPS-AC-07 | Hỗ trợ thêm file `.zip` qua file input và kéo thả drag & drop | RUNTIME | Nút "+ Thêm .zip", drag-drop file zip vào tab, kiểm tra file zip hỏng bằng `ZipFile.OpenRead` | PASS |
| RPS-AC-08 | Responsive 1024px/800px không tràn ngang; focus rõ ràng | RUNTIME | Min size 960x530 (`WM_GETMINMAXINFO`), Inspector drawer overlay không che ép layout. Ảnh kiểm chứng tại 1025x533, 1280x720, 1366x768 (1600x900: UNVERIFIED). | PARTIAL |
| RPS-AC-09 | Cảm giác tương tác trực quan, mượt mà khi thao tác | MANUAL_REQUIRED | Nghiệm thu tương tác mượt mà, empty state, reduce motion; chờ xác nhận trực quan từ user. | PARTIAL |
| RPS-AC-10 | Không hồi quy: Web test suite và Static check pass 100% | AUTOMATED | `dotnet test Vaz.sln` (227/227 test pass), `dotnet build Vaz.sln` (0 error, 0 warning), `npm run check:static` pass, `npm run check:web` pass | PASS |

## Các bước triển khai
1. [x] Tạo Feature Spec & Verification Contract (`WEB_SPEC`).
2. [x] Triển khai Web Prototype & Playwright tests (`tests/web-e2e/resource-packs-shaders.spec.ts`).
3. [x] Kiểm định Playwright runtime đạt `WEB_VALIDATED` (16/16 test pass).
4. [x] Tạo `ResourcePackShaderData.cs` và mở rộng `InstanceDetailViewModel.cs`.
5. [x] Cập nhật giao diện `InstanceDetailPage.xaml` với 2 TabViewItem: Resource Packs & Shaders.
6. [x] Đợt 1 Remediation: Chuyển Inspector Drawer sang Overlay, focus restoration, bảo vệ layout không ép 2 cột.
7. [x] Đợt 2 Remediation: Bổ sung Resource Pack Searchbox và Shader Loader Prerequisite banner kèm 1-click install Iris.
8. [x] Đợt 3 Remediation: Hoàn thiện logic biên (disable Up ở `# 1`, Down ở cuối), action overflow menu `...`, empty state, corrupted zip validation, domain resolver + 19 unit tests (227 tests pass), chụp đầy đủ 18 ảnh minh chứng `<AC>-<mô tả>-<WxH>.png` (bảo toàn 6 ảnh gốc), hoàn thiện báo cáo minh chứng `resource-packs-shaders-report.md`.
9. [ ] Nghiệm thu cuối cùng (`PARITY_VERIFIED`): Chạy kiểm thử màn hình >= 1600x900 và nhận xác nhận trực quan của user.
