---
feature: logs-viewer
status: WEB_VALIDATED
created: 2026-10-28
updated: 2026-10-28
verifier: web-verifier
---

# Logs Viewer & Export Contract

Contract quy định hành vi giao diện cho tính năng Logs Viewer & Export trên Web Prototype (`docs/design-preview/`), dùng làm executable specification trước khi port sang WinUI 3 desktop.

## Bảng tiêu chí nghiệm thu (Acceptance Criteria)

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế | Status |
|---|----------------------|---------------|-----------|------------------|--------|
| LV-AC-01 | Hiển thị log stream với syntax highlighting theo dòng (Timestamp, Thread, Log Level `[INFO]`, `[WARN]`, `[ERROR]`, message) và line numbers | RUNTIME | Playwright: mở tab Logs khi có dữ liệu, assert các dòng log có class cấu trúc tương ứng (`.log-timestamp`, `.log-level`, `.log-msg`, `.log-num`) | `tests/web-e2e/logs-viewer.spec.ts` (pass chromium + mobile-chrome) + screenshot `evidence-logs-01-viewer.png` | PASS |
| LV-AC-02 | Bộ lọc log theo cấp độ (`ALL`, `INFO`, `WARN`, `ERROR`) hiển thị đúng số lượng dòng và cập nhật `#logCount` | RUNTIME | Playwright: click các chip lọc mức log, assert số lượng dòng hiển thị và nội dung text `#logCount` tương ứng | `tests/web-e2e/logs-viewer.spec.ts` (pass chromium + mobile-chrome) | PASS |
| LV-AC-03 | Tìm kiếm trong log (`#logSearch`) lọc danh sách dòng khớp và cập nhật số lượng đếm | RUNTIME | Playwright: nhập từ khóa tìm kiếm (VD: "Sodium" hoặc "ERROR"), assert danh sách dòng lọc đúng và `#logCount` cập nhật | `tests/web-e2e/logs-viewer.spec.ts` (pass chromium + mobile-chrome) + screenshot `evidence-logs-03-search.png` | PASS |
| LV-AC-04 | Điều hướng nhanh: Nút "Lỗi đầu tiên" (`#jumpFirstErrorBtn`) cuộn đến dòng ERROR đầu tiên kèm hiệu ứng flash; nút "Cuối log" (`#logJumpBtn`) cuộn xuống dòng mới nhất | RUNTIME | Playwright: click `#jumpFirstErrorBtn`, assert dòng lỗi đầu tiên nhận class `.is-flash` và scrollIntoView; scroll lên trên click `#logJumpBtn` assert scroll xuống đáy | `tests/web-e2e/logs-viewer.spec.ts` (pass chromium + mobile-chrome) + screenshot `evidence-logs-02-filter-error.png` | PASS |
| LV-AC-05 | Auto-scroll checkbox (`#autoScrollChk`): khi bật tự động cuộn xuống đáy khi có dòng log mới; khi tắt giữ nguyên vị trí scroll | RUNTIME | Playwright: toggle `#autoScrollChk`, thêm dòng log mới, kiểm tra vị trí scrollTop của `#logView` | `tests/web-e2e/logs-viewer.spec.ts` (pass chromium + mobile-chrome) | PASS |
| LV-AC-06 | Xuất log: Nút "Copy to Clipboard" (`#copyLogBtn`) sao chép toàn bộ log (hoặc log đã lọc) kèm toast thông báo; nút "Tải về .log" (`#exportLogBtn`) tải file `latest.log` về máy | RUNTIME | Playwright: click copy assert toast "Đã sao chép"; click export assert toast hoặc tải file | `tests/web-e2e/logs-viewer.spec.ts` (pass chromium + mobile-chrome) | PASS |
| LV-AC-07 | Nút "Xóa log" (`#clearLogBtn`) làm sạch khung log hiển thị empty state `#logsEmpty`; nút "Tạo log mẫu" (`#generateLogBtn`) sinh log mẫu phong phú (khởi động, tải mod, cảnh báo, lỗi) để kiểm thử | RUNTIME | Playwright: click Xóa log assert `#logsEmpty` visible; click Tạo log mẫu assert danh sách dòng xuất hiện lại kèm toast | `tests/web-e2e/logs-viewer.spec.ts` (pass chromium + mobile-chrome) | PASS |
| LV-AC-08 | Nút "Mở thư mục logs" (`#openLogsFolderBtn`) hiển thị toast thông báo mở thư mục `.minecraft/logs` | RUNTIME | Playwright: click nút mở thư mục logs, assert toast thông báo xuất hiện | `tests/web-e2e/logs-viewer.spec.ts` (pass chromium + mobile-chrome) | PASS |
| LV-AC-09 | Responsive (390px, 620px) không tràn ngang; `:focus-visible` rõ ràng; Reduced motion duration <= 0.05s | RUNTIME | Playwright: kiểm tra scrollWidth <= clientWidth tại 390px và 620px; outline focus >= 2px; duration <= 0.05s | `tests/web-e2e/logs-viewer.spec.ts` (pass chromium + mobile-chrome) + screenshot `evidence-logs-04-mobile.png` | PASS |
| LV-AC-10 | Cảm giác tương tác trực quan, cuộn log và tô màu cú pháp mượt mà theo design tokens | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, chuyển sang tab Logs, lọc log, cuộn xem và chụp ảnh kiểm chứng | User xác nhận nguyên văn: "xac nhan" (2026-10-28) qua ảnh chụp Review pane (`evidence-logs-01` -> `04`) | PASS |
| LV-AC-11 | Không hồi quy: Web test suite (`check:web`) và Static check (`check:static`) pass 100%, 0 `transition: all` | AUTOMATED | Chạy `npm run check:static`, `npx playwright test`, grep CSS rule | `npm run check:static` pass, 0 regex matches `transition: all` | PASS |
