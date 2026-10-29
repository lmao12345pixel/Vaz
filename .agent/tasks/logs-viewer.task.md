# Task: Logs Viewer & Export

- **Feature**: `logs-viewer`
- **Contract**: `docs/contracts/logs-viewer.contract.md`
- **Feature Doc**: `docs/product/features/logs-viewer.feature.md`
- **Phase**: `WEB_VALIDATED`
- **Owner**: Web Team
- **Date**: 2026-10-28

## Acceptance Criteria Checklist
- [x] LV-AC-01: Hiển thị log stream với syntax highlighting theo dòng (Timestamp, Thread, Log Level `[INFO]`, `[WARN]`, `[ERROR]`, message) và line numbers (RUNTIME - PASS)
- [x] LV-AC-02: Bộ lọc log theo cấp độ (`ALL`, `INFO`, `WARN`, `ERROR`) hiển thị đúng số lượng dòng và cập nhật `#logCount` (RUNTIME - PASS)
- [x] LV-AC-03: Tìm kiếm trong log (`#logSearch`) lọc danh sách dòng khớp và cập nhật số lượng đếm (RUNTIME - PASS)
- [x] LV-AC-04: Điều hướng nhanh: Nút "Lỗi đầu tiên" (`#jumpFirstErrorBtn`) cuộn đến dòng ERROR đầu tiên kèm hiệu ứng flash; nút "Cuối log" (`#logJumpBtn`) cuộn xuống dòng mới nhất (RUNTIME - PASS)
- [x] LV-AC-05: Auto-scroll checkbox (`#autoScrollChk`): khi bật tự động cuộn xuống đáy khi có dòng log mới; khi tắt giữ nguyên vị trí scroll (RUNTIME - PASS)
- [x] LV-AC-06: Xuất log: Nút "Copy to Clipboard" (`#copyLogBtn`) sao chép toàn bộ log (hoặc log đã lọc) kèm toast thông báo; nút "Tải về .log" (`#exportLogBtn`) tải file `latest.log` về máy (RUNTIME - PASS)
- [x] LV-AC-07: Nút "Xóa log" (`#clearLogBtn`) làm sạch khung log hiển thị empty state `#logsEmpty`; nút "Tạo log mẫu" (`#generateLogBtn`) sinh log mẫu phong phú (khởi động, tải mod, cảnh báo, lỗi) để kiểm thử (RUNTIME - PASS)
- [x] LV-AC-08: Nút "Mở thư mục logs" (`#openLogsFolderBtn`) hiển thị toast thông báo mở thư mục `.minecraft/logs` (RUNTIME - PASS)
- [x] LV-AC-09: Responsive (390px, 620px) không tràn ngang; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s (RUNTIME - PASS)
- [x] LV-AC-10: Cảm giác tương tác trực quan, cuộn log và tô màu cú pháp mượt mà theo design tokens (MANUAL_REQUIRED - PASS: "xac nhan" 2026-10-28)
- [x] LV-AC-11: Không hồi quy: Web suite 100%, static check 100%, 0 `transition: all` (AUTOMATED - PASS)

## Tiến độ thực hiện
1. [x] **Orchestrator**: Tạo Verification Contract và Feature Document (`WEB_SPEC`).
2. [x] **Web Developer**: Triển khai UI/UX web prototype + bộ test Playwright `tests/web-e2e/logs-viewer.spec.ts` + 4 ảnh screenshot bằng chứng.
3. [x] **Web Verifier**: Thẩm định runtime evidence độc lập, kiểm tra Playwright (20/20 test pass).
4. [x] **User**: Nghiệm thu trực quan LV-AC-10 -> Nâng phase lên `WEB_VALIDATED`.
