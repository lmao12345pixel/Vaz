---
feature: resource-packs-shaders
status: WEB_VALIDATED
last_updated: 2026-10-26
contract_version: 1.0.0
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


## 5. Web Verifier Report (Độc lập)

- **Verifier:** web-verifier
- **Thời gian kiểm định:** 2026-10-26
- **Kết luận:** BLOCKED (RPS-AC-01 -> RPS-AC-08 RUNTIME PASS; RPS-AC-10 AUTOMATED PASS; RPS-AC-09 MANUAL_REQUIRED giữ BLOCKED do chưa có xác nhận từ user).
- **Phase:** Giữ `WEB_SPEC` (chưa đủ điều kiện chuyển sang `WEB_VALIDATED`).
