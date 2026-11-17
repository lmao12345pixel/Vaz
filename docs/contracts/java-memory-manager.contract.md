---
feature: java-memory-manager
status: WEB_VALIDATED
last_updated: 2026-10-27
contract_version: 1.0.0
---

# Verification Contract: Java Resolver & JVM Memory Manager

## 1. Bối cảnh & Mục tiêu
Cung cấp giao diện quản lý Java Runtime Resolver và JVM Memory chuyên sâu trong Instance Settings (`#tab-settings`):
- Tự động phát hiện các runtime Java (Java 8, 17, 21) kèm Vendor, Architecture, Version và đánh dấu phiên bản khuyến nghị theo bản Minecraft của instance.
- Cảnh báo trực quan khi cấu hình Java không tương thích (vd: MC 1.21.1 chạy Java 8/17) kèm nút 1-click chuyển đổi sang Java khuyến nghị.
- Tự động download và cài đặt Java 21 từ Adoptium Temurin với thanh tiến trình trực quan.
- Nhập đường dẫn Java tùy biến kèm tính năng Test Java probe kết quả tức thì.
- Điều khiển Min RAM / Max RAM kèm các preset nhanh (2 GB, 4 GB, 8 GB, 12 GB), biểu đồ thanh phân bổ RAM hệ thống, và cảnh báo khi RAM vượt quá 85% dung lượng máy.
- Cấu hình Garbage Collector (GC Presets: G1GC, Generational ZGC, Shenandoah) tự động sinh cờ tối ưu vào ô JVM arguments.

## 2. Tiêu chí nghiệm thu (Acceptance Criteria)

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế | Status |
|---|----------------------|---------------|-----------|------------------|--------|
| JMM-AC-01 | Danh sách Java Runtime tự động phát hiện (Java 8, 17, 21) hiển thị đầy đủ Vendor, Version, Path, Arch và badge "Khuyến nghị" cho Java phù hợp phiên bản MC của instance | RUNTIME | Playwright: mở tab Settings, assert danh sách runtime trong selector, kiểm tra badge "Khuyến nghị" khớp phiên bản MC | `npx playwright test tests/web-e2e/java-memory-manager.spec.ts` (18/18 pass chromium + mobile-chrome) | PASS |
| JMM-AC-02 | Cảnh báo Java không tương thích (Incompatible Warning) xuất hiện khi chọn runtime sai (VD: MC 1.21.1 chọn Java 17 hoặc 8); có nút 1-click chọn ngay Java tương thích | RUNTIME | Playwright: chọn runtime Java 17 cho instance 1.21.1, assert banner cảnh báo; click nút "Dùng Java khuyến nghị" -> trạng thái trở về tương thích | `npx playwright test tests/web-e2e/java-memory-manager.spec.ts` (18/18 pass chromium + mobile-chrome) | PASS |
| JMM-AC-03 | Nhập đường dẫn Java tùy biến (`Custom Java Path`) với nút "Kiểm tra" (Test Java) hiển thị kết quả probe version (vd: Java 21.0.4 64-bit) hoặc báo lỗi nếu path không hợp lệ | RUNTIME | Playwright: nhập đường dẫn hợp lệ, click Test -> assert kết quả probe thành công; nhập path sai -> assert thông báo lỗi | `npx playwright test tests/web-e2e/java-memory-manager.spec.ts` (18/18 pass chromium + mobile-chrome) | PASS |
| JMM-AC-04 | 1-Click Cài đặt Java: nút "Tải Java 21 (Temurin)" hiển thị tiến trình tải/cài đặt và tự động thêm runtime vào danh sách sẵn sàng | RUNTIME | Playwright: click nút tải Java, assert progress bar hoạt động, sau khi hoàn tất runtime mới xuất hiện và được chọn | `npx playwright test tests/web-e2e/java-memory-manager.spec.ts` (18/18 pass chromium + mobile-chrome) | PASS |
| JMM-AC-05 | Điều chỉnh RAM cấp phát (Min RAM & Max RAM) đồng bộ giữa thanh trượt và ô nhập số; bấm các preset nhanh (2 GB, 4 GB, 8 GB, 12 GB) cập nhật đúng giá trị | RUNTIME | Playwright: kéo slider RAM hoặc click các chip preset nhanh, assert giá trị ô input và thanh trượt thay đổi đồng bộ | `npx playwright test tests/web-e2e/java-memory-manager.spec.ts` (18/18 pass chromium + mobile-chrome) | PASS |
| JMM-AC-06 | Biểu đồ thanh RAM hệ thống (System Memory Bar) hiển thị tỉ lệ phân bổ; cảnh báo an toàn xuất hiện khi Max RAM vượt quá 85% tổng RAM hệ thống hoặc dưới 1024 MB | RUNTIME | Playwright: tăng Max RAM lên quá 85% tổng RAM (vd: 14 GB trên máy 16 GB), assert banner cảnh báo quá tải RAM; giảm xuống dưới 1024 MB assert cảnh báo thiếu RAM | `npx playwright test tests/web-e2e/java-memory-manager.spec.ts` (18/18 pass chromium + mobile-chrome) | PASS |
| JMM-AC-07 | Chọn GC Preset (G1GC, Generational ZGC, Shenandoah) tự động điền cờ JVM tối ưu tương ứng vào ô JVM Arguments | RUNTIME | Playwright: click chip GC "ZGC (Generational)", assert ô textarea chứa `-XX:+UseZGC -XX:+ZGenerational`; chọn G1GC assert chứa `-XX:+UseG1GC` | `npx playwright test tests/web-e2e/java-memory-manager.spec.ts` (18/18 pass chromium + mobile-chrome) | PASS |
| JMM-AC-08 | Bấm "Lưu" lưu cấu hình vào state instance và localStorage kèm toast thông báo; bấm "Mặc định" khôi phục về giá trị khuyến nghị | RUNTIME | Playwright: chỉnh sửa cấu hình, bấm Lưu -> reload trang assert state được giữ; bấm Mặc định -> assert giá trị khôi phục | `npx playwright test tests/web-e2e/java-memory-manager.spec.ts` (18/18 pass chromium + mobile-chrome) | PASS |
| JMM-AC-09 | Responsive (390px, 620px) không tràn ngang; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s | RUNTIME | Playwright: kiểm tra scrollWidth <= clientWidth tại 390px và 620px; outline focus >= 2px; reduced motion | `npx playwright test tests/web-e2e/java-memory-manager.spec.ts` (18/18 pass chromium + mobile-chrome) | PASS |
| JMM-AC-10 | Cảm giác tương tác trực quan, kéo trượt mượt mà, phản hồi chuyển đổi runtime & GC theo design tokens | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, vào tab Settings, thao tác chọn runtime, kéo slider RAM, chọn preset GC và kiểm tra ảnh chụp | User xác nhận nguyên văn: "xac nhan" (2026-10-27) dựa trên ảnh chụp evidence-java-01 đến 04 | PASS |
| JMM-AC-11 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100% | AUTOMATED | Chạy `npm run check:web` và `npm run check:static` | `npm run check:static` PASS (0 errors, 192 unit tests pass, agent validation 12/12). `npm run check:web` PASS (158/158 pass chromium + mobile-chrome). | PASS |

## 3. Ràng buộc kỹ thuật & Kiến trúc
1. Không sửa đổi mã nguồn WinUI (`src/Vaz.WinUI`) hay Core (`src/Vazer.Core`).
2. CSS tuân thủ design tokens (`--dur-*`, `--ease-*`, `--accent`, `--bg-*`), tuyệt đối không dùng `transition: all` hay `transition-property: all`.
3. Tương thích dữ liệu với model `JavaInstallation` của Vazer.Core (`MajorVersion`, `Vendor`, `Architecture`, `Path`, `IsJdk`).
4. Lưu trữ state instance trong `state.instances` và đồng bộ qua `localStorage` (`vazer-demo-v1`).
