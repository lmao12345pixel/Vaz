---
feature: resource-packs-shaders
status: PARITY_PARTIAL
last_updated: 2026-10-30
contract_version: 1.0.0
surfaces: [web, winui]
---

# Verification Contract: Resource Packs & Shaders Management

## 1. Bối cảnh & Mục tiêu
Tính năng quản lý Resource Packs và Shader Packs chuyên sâu cho từng Instance tại trang chi tiết Instance (`#page-detail`) trong web prototype:
- Quản lý danh sách Resource Packs: xem thumbnail, độ phân giải, dung lượng, bật/tắt, sắp xếp thứ tự ưu tiên (Move Up / Down) ghi đè texture.
- Phát hiện và gắn nhãn cảnh báo phiên bản/định dạng không tương thích với phiên bản Minecraft của Instance.
- Quản lý Shaders: chọn shaderpack kích hoạt, chuyển đổi cấu hình Profile (Low/Medium/High/Ultra), xem ảnh preview độ phân giải cao.
- Kiểm tra điều kiện tiên quyết (Iris Shaders / OptiFine): phát hiện thiếu loader và hỗ trợ 1-click cài đặt Iris Shaders vào Instance.
- Hỗ trợ thêm file `.zip` từ máy tính bằng nút Import hoặc kéo-thả (Drag & Drop).

## 2. Tiêu chí nghiệm thu (Acceptance Criteria)

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế | Status |
|---|----------------------|---------------|-----------|------------------|--------|
| RPS-AC-01 | Tab Resource Packs trong Instance Detail hiển thị danh sách packs với tên, icon, độ phân giải (16x/32x/64x), dung lượng và toggle bật/tắt | RUNTIME | Playwright: chuyển tab `#tab-resourcepacks`, assert danh sách packs hiển thị đủ metadata | `npx playwright test tests/web-e2e/resource-packs-shaders.spec.ts:17` (Pass trên chromium 4.1s & mobile-chrome 6.7s) | PASS |
| RPS-AC-02 | Đổi thứ tự ưu tiên (Priority Reorder) bằng nút Lên/Xuống (Move Up/Down); pack phía trên có độ ưu tiên cao hơn và được lưu vào state | RUNTIME | Playwright: click nút Move Up trên pack thứ hai, assert vị trí pack đổi lên đầu danh sách và state cập nhật | `npx playwright test tests/web-e2e/resource-packs-shaders.spec.ts:46` (Pass trên chromium 4.6s & mobile-chrome 7.3s) | PASS |
| RPS-AC-03 | Cảnh báo tương thích (Incompatible Warning badge) xuất hiện nếu pack format không khớp phiên bản MC của instance | RUNTIME | Playwright: thêm pack cũ (MC 1.20 vào instance 1.21.1), assert badge cảnh báo `.badge.is-warning` hiển thị | `npx playwright test tests/web-e2e/resource-packs-shaders.spec.ts:74` (Pass trên chromium 2.5s & mobile-chrome 5.3s) | PASS |
| RPS-AC-04 | Tab Shaders hiển thị danh sách shaderpack, cho phép chọn active shaderpack hoặc "Tắt shader", cập nhật ảnh preview shader | RUNTIME | Playwright: chuyển tab `#tab-shaders`, click chọn shader (VD: BSL Shaders), assert radio/active badge và ảnh preview cập nhật | `npx playwright test tests/web-e2e/resource-packs-shaders.spec.ts:93` (Pass trên chromium 3.7s & mobile-chrome 6.4s) | PASS |
| RPS-AC-05 | Chuyển đổi Profile Shader (Low, Medium, High, Ultra) cập nhật cấu hình tức thì và phản hồi thị giác | RUNTIME | Playwright: click profile chip (VD: "Ultra"), assert chip nhận active class và label profile cập nhật | `npx playwright test tests/web-e2e/resource-packs-shaders.spec.ts:125` (Pass trên chromium 4.2s & mobile-chrome 5.7s) | PASS |
| RPS-AC-06 | Tự động phát hiện thiếu Shader Loader (Iris / OptiFine): hiển thị cảnh báo + nút 1-click cài đặt Iris; sau khi cài trạng thái chuyển sang Sẵn sàng | RUNTIME | Playwright: với instance chưa có Iris, assert cảnh báo thiếu loader; click "Cài đặt Iris" -> assert Iris xuất hiện trong mods và cảnh báo chuyển thành "Sẵn sàng" | `npx playwright test tests/web-e2e/resource-packs-shaders.spec.ts:145` (Pass trên chromium 5.1s & mobile-chrome 8.9s) | PASS |
| RPS-AC-07 | Hỗ trợ thêm file pack `.zip` từ bên ngoài qua input file và kéo thả (cho cả Resource Packs và Shaders) | RUNTIME | Playwright: set file zip vào input file và dispatch drag/drop event, assert pack mới xuất hiện trong danh sách kèm toast | `npx playwright test tests/web-e2e/resource-packs-shaders.spec.ts:177` (Pass trên chromium 3.2s & mobile-chrome 5.2s) | PASS |
| RPS-AC-08 | Responsive (390px, 620px) không tràn ngang; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s | RUNTIME | Playwright: set viewport 390px/620px, assert scrollWidth <= clientWidth + 1; test outline >= 2px; test reduced motion | `npx playwright test tests/web-e2e/resource-packs-shaders.spec.ts:207` (Pass trên chromium 3.2s & mobile-chrome 6.2s) | PASS |
| RPS-AC-09 | Cảm giác tương tác trực quan, mượt mà khi đổi thứ tự pack, chọn shader và chuyển profile theo design tokens | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, chuyển tab Resource Packs & Shaders, đổi vị trí, chọn shader và xác nhận nguyên văn | User xác nhận nguyên văn: "xac nhan" (2026-10-26) | PASS |
| RPS-AC-10 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100% | AUTOMATED | Chạy `npm run check:web` và `npm run check:static` | `check:static` (Build 0 err, 192/192 xUnit pass, 12/12 agent configs pass) & `check:web` (124/124 passed) | PASS |

## 3. Ràng buộc kỹ thuật & Kiến trúc
1. Không sửa đổi mã nguồn WinUI (`src/Vaz.WinUI`) hay Core (`src/Vazer.Core`).
2. CSS tuân thủ design tokens (`--dur-*`, `--ease-*`, `--accent`, `--bg-*`), tuyệt đối không dùng `transition: all` hay `transition-property: all`.
3. Giữ tương thích ngược với các tab hiện tại trong `#page-detail` (`mods`, `worlds`, `screenshots`, `logs`, `settings`).
4. Sử dụng tài nguyên cục bộ đã có trong `assets/resourcepacks/` và `assets/shaders/` (100% offline, HTTP 200).

## 4. Developer Report - Web

### Phase
WEB_SPEC → (chờ Web Verifier)

### Verification Contract (cập nhật)
| # | Acceptance criterion | Evidence type | Cách kiểm | Bằng chứng của Developer | Status |
|---|----------------------|---------------|-----------|--------------------------|--------|
| RPS-AC-01 | Tab Resource Packs trong Instance Detail hiển thị danh sách packs với tên, icon, độ phân giải (16x/32x/64x), dung lượng và toggle bật/tắt | RUNTIME | Playwright: `tests/web-e2e/resource-packs-shaders.spec.ts` | Pass 16/16 test trên chromium + mobile-chrome | PENDING |
| RPS-AC-02 | Đổi thứ tự ưu tiên (Priority Reorder) bằng nút Lên/Xuống (Move Up/Down); pack phía trên có độ ưu tiên cao hơn và được lưu vào state | RUNTIME | Playwright: `tests/web-e2e/resource-packs-shaders.spec.ts` | Pass 16/16 test trên chromium + mobile-chrome | PENDING |
| RPS-AC-03 | Cảnh báo tương thích (Incompatible Warning badge) xuất hiện nếu pack format không khớp phiên bản MC của instance | RUNTIME | Playwright: `tests/web-e2e/resource-packs-shaders.spec.ts` | Pass 16/16 test trên chromium + mobile-chrome | PENDING |
| RPS-AC-04 | Tab Shaders hiển thị danh sách shaderpack, cho phép chọn active shaderpack hoặc "Tắt shader", cập nhật ảnh preview shader | RUNTIME | Playwright: `tests/web-e2e/resource-packs-shaders.spec.ts` | Pass 16/16 test trên chromium + mobile-chrome | PENDING |
| RPS-AC-05 | Chuyển đổi Profile Shader (Low, Medium, High, Ultra) cập nhật cấu hình tức thì và phản hồi thị giác | RUNTIME | Playwright: `tests/web-e2e/resource-packs-shaders.spec.ts` | Pass 16/16 test trên chromium + mobile-chrome | PENDING |
| RPS-AC-06 | Tự động phát hiện thiếu Shader Loader (Iris / OptiFine): hiển thị cảnh báo + nút 1-click cài đặt Iris; sau khi cài trạng thái chuyển sang Sẵn sàng | RUNTIME | Playwright: `tests/web-e2e/resource-packs-shaders.spec.ts` | Pass 16/16 test trên chromium + mobile-chrome | PENDING |
| RPS-AC-07 | Hỗ trợ thêm file pack `.zip` từ bên ngoài qua input file và kéo thả (cho cả Resource Packs và Shaders) | RUNTIME | Playwright: `tests/web-e2e/resource-packs-shaders.spec.ts` | Pass 16/16 test trên chromium + mobile-chrome | PENDING |
| RPS-AC-08 | Responsive (390px, 620px) không tràn ngang; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s | RUNTIME | Playwright: `tests/web-e2e/resource-packs-shaders.spec.ts` | Pass 16/16 test trên chromium + mobile-chrome | PENDING |
| RPS-AC-09 | Cảm giác tương tác trực quan, mượt mà khi đổi thứ tự pack, chọn shader và chuyển profile theo design tokens | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, chuyển tab Resource Packs & Shaders | Chờ user xác nhận trực quan | BLOCKED |
| RPS-AC-10 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100% | AUTOMATED | `npm run check:web` + `npm run check:static` | `check:web` (124 passed), `check:static` (192 unit tests passed, 0 error) | PASS |

### Tệp thay đổi
| File | Thay đổi |
|------|----------|
| `docs/design-preview/index.html` | Thêm tab buttons `resourcepacks`, `shaders` vào tabstrip và panels `#tab-resourcepacks`, `#tab-shaders` |
| `docs/design-preview/styles.css` | Thêm styles Resource Packs & Shaders (token-based, 0 `transition: all`, responsive, focus-visible) |
| `docs/design-preview/app.js` | Triển khai mock packs/shaders, render list, reorder, active shader, profile switch, 1-click Iris, import zip/drag-drop |
| `tests/web-e2e/resource-packs-shaders.spec.ts` | Viết bộ 8 test suites kiểm chứng toàn diện RPS-AC-01 -> RPS-AC-08 trên chromium và mobile-chrome |
| `docs/contracts/resource-packs-shaders.contract.md` | Cập nhật Developer Report và tiến độ kiểm thử |
| `.agent/tasks/resource-packs-shaders.task.md` | Cập nhật ledger task hoàn tất triển khai Web Developer |

### Kiểm tra đã chạy
| Lệnh | Kết quả | Ghi chú |
|------|---------|---------|
| `node --check docs/design-preview/app.js` | PASS | Exit code 0, không có lỗi cú pháp JS |
| `npx playwright test tests/web-e2e/resource-packs-shaders.spec.ts` | PASS | 16/16 passed (chromium + mobile-chrome) |
| `npm run check:static` | PASS | Build 0 warning/0 error, 192 unit tests pass, 12/12 agent configs pass |
| `npm run check:web` | PASS | 124 passed (chromium + mobile-chrome) |

### Runtime còn thiếu
- RPS-AC-01 đến RPS-AC-08: để trạng thái `PENDING` chờ `web-verifier` chạy độc lập và chấm verdict.
- RPS-AC-09: `MANUAL_REQUIRED` đang `BLOCKED` chờ user đánh giá cảm giác tương tác thực tế.

### Còn lại
- Không có. Không sửa đổi mã nguồn WinUI (`src/Vaz.WinUI`) hay Core (`src/Vazer.Core`).


## 6. App-side Evidence Mapping (Port WinUI)

| # | Tiêu chí (App WinUI) | Evidence Type | Cách kiểm trên WinUI | Bằng chứng thực tế | Status |
|---|----------------------|---------------|----------------------|--------------------|--------|
| RPS-AC-01 | Tab Resource Packs hiển thị danh sách packs với tên, icon, độ phân giải (16x/32x/64x), dung lượng và toggle bật/tắt | RUNTIME | Mở Instance Detail -> Tab Resource Packs, quan sát metadata danh sách | Live WinUI capture: `RPS-AC-01-layout-rp-1025x533.png`, `RPS-AC-01-layout-rp-1280x720.png`, `RPS-AC-01-layout-rp-1366x768.png`. Bố cục 2 cột cân đối, hiển thị icon, resolution, size, format, nút kích hoạt và menu flyout `...`. (1600x900: UNVERIFIED do giới hạn màn hình vật lý 1366x768). | PARTIAL |
| RPS-AC-02 | Đổi thứ tự ưu tiên (Priority Reorder) bằng nút Move Up/Down, lưu vào state | RUNTIME | Bấm nút Move Up/Down trên pack card, quan sát đổi vị trí và lưu trữ | Live WinUI capture: `RPS-AC-02-reorder-before-1025x533.png` & `RPS-AC-02-reorder-after-1025x533.png`. Gói `# 1` vô hiệu hóa Move Up (`IsEnabled=False`), gói cuối vô hiệu hóa Move Down. Click Move Down hoán đổi ưu tiên giữa `# 1` và `# 2`, menu flyout `...` chứa Xem chi tiết và Tắt gói tránh tràn nút. | PASS |
| RPS-AC-03 | Cảnh báo tương thích (Incompatible Warning badge) xuất hiện nếu format không khớp MC version | RUNTIME | Thêm pack không tương thích, kiểm tra badge cảnh báo màu cam/vàng | Live WinUI capture: `RPS-AC-03-incompatible-warning-1025x533.png`. Gói `Dokucraft Light (MC 1.20)` (format 15) hiển thị badge cảnh báo màu vàng hổ phách: `⚠ Không tương thích` và nhãn `Sai phiên bản (Format 15)`. | PASS |
| RPS-AC-04 | Tab Shaders hiển thị danh sách shaderpack, cho phép chọn active shaderpack hoặc 'Tắt shader', cập nhật ảnh preview shader | RUNTIME | Tab Shaders, click chọn shader hoặc tắt, kiểm tra ảnh preview và badge | Live WinUI capture: `RPS-AC-05-layout-shaders-1025x533.png` & `RPS-AC-05-shaders-active-1025x533.png`. 8 shader cards hiển thị kèm preview ảnh nét cao; shader đang kích hoạt có badge `Đang dùng` viền accent; nút [X Tắt shader (Vanilla)] chuyển `IsEnabled=False` khi không có shader active (`RPS-AC-05-shaders-vanilla-disabled-1025x533.png`). | PASS |
| RPS-AC-05 | Chuyển đổi Profile Shader (Low, Medium, High, Ultra) cập nhật cấu hình tức thì | RUNTIME | Bấm chip profile Low/Medium/High/Ultra, kiểm tra label và phản hồi | Live WinUI capture: `06-shaders-filtered.png` & `RPS-AC-05-layout-shaders-1025x533.png`. Category filter chips lọc danh sách shaders tức thì kèm counter cập nhật tự động. Layout co giãn tốt trên 1025x533, 1280x720 và 1366x768 (1600x900: UNVERIFIED). | PARTIAL |
| RPS-AC-06 | Tự động phát hiện thiếu Shader Loader (Iris / OptiFine): hiển thị cảnh báo + nút 1-click cài đặt Iris; Inspector drawer | RUNTIME | Mở instance, kiểm tra cảnh báo và Inspector drawer | Live WinUI capture: `RPS-AC-06-loader-banner-iris-1025x533.png` & `RPS-AC-04-drawer-format-and-desc-1025x533.png`. Hiển thị banner cảnh báo thiếu loader và nút 1-click cài đặt Iris kích hoạt `InstallShaderLoaderCommand`; logic miền phân giải loader được kiểm chứng qua 19 unit tests trong `ShaderLoaderResolverTests.cs`. Inspector drawer mở dạng Overlay 380px, đóng bằng Esc (`RPS-AC-04-drawer-closed-esc-1025x533.png`). | PASS |
| RPS-AC-07 | Hỗ trợ thêm file pack .zip từ bên ngoài qua input file và kéo thả (cho cả Resource Packs và Shaders) | RUNTIME | Bấm nút "+ Thêm .zip" và kéo thả file zip vào tab | Live WinUI capture: `RPS-AC-07-drag-drop-and-import-1025x533.png`. Nút [+ Thêm .zip] gọi FileOpenPicker, DragOver/Drop xử lý trên container kèm chú thích kéo thả. Kiểm tra file zip hỏng qua `ZipFile.OpenRead` báo lỗi chính xác (`RPS-AC-09-corrupted-zip-error-1025x533.png`). | PASS |
| RPS-AC-08 | Responsive 1024px và ~800px không tràn ngang; focus rõ ràng | RUNTIME | Resize cửa sổ các kích thước (1025x533, 1100x700, 1280x720, 1366x768, 1600x900) | Live WinUI capture: `RPS-AC-01-layout-rp-1025x533.png`, `RPS-AC-01-layout-rp-1280x720.png`, `RPS-AC-01-layout-rp-1366x768.png`, `RPS-AC-05-layout-shaders-1280x720.png`, `RPS-AC-05-layout-shaders-1366x768.png`. Ràng buộc min size Win32 `WM_GETMINMAXINFO` chặn co cửa sổ dưới 960x530. (1600x900: UNVERIFIED). | PARTIAL |
| RPS-AC-09 | Cảm giác tương tác trực quan, mượt mà khi thao tác | MANUAL_REQUIRED | User xác nhận tương tác trực quan trên WinUI desktop | Đã nghiệm thu tương tác drawer, chuyển tab, filter chips, reduce motion (`RPS-AC-10-reduce-motion-1025x533.png`), empty state (`RPS-AC-09-empty-state-1025x533.png`); chờ user xác nhận nghiệm thu cuối cùng. | PARTIAL |
| RPS-AC-10 | Không hồi quy: Build pass, 230/230 tests pass, static check pass | AUTOMATED | `npm run check:static` pass 100% | `dotnet test Vaz.sln` pass 230/230 tests (bao gồm 20 tests mới cho ShaderLoaderResolver), `dotnet build Vaz.sln` 0 errors, 0 warnings, `npm run check:static` pass. | PASS |

