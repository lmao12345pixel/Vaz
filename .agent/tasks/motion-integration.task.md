---
id: motion-integration
contract: docs/contracts/motion-integration.contract.md
feature: docs/product/features/motion-integration.feature.md
status: WEB_VALIDATED
owner: orchestrator
created: 2026-10-26
updated: 2026-10-26
---

# Task Ledger: Motion Integration (Main Prototype)

## Trạng thái hiện tại
Phase: `WEB_VALIDATED`
Mục tiêu tiếp theo: Đã hoàn tất validation trên Web prototype; sẵn sàng làm spec cho phase port WinUI app nếu cần.

## Bảng tiêu chí nghiệm thu

| # | Tiêu chí | Loại bằng chứng | Trạng thái | Ghi chú |
|---|----------|-----------------|------------|---------|
| MI-AC-01 | Shared element FLIP transition card -> detail | RUNTIME | PASS | Playwright runtime test pass (chromium + mobile-chrome) |
| MI-AC-02 | Reverse FLIP transition detail -> card | RUNTIME | PASS | Playwright runtime test pass (chromium + mobile-chrome) |
| MI-AC-03 | Khởi chạy instance trực tiếp không popup modal | RUNTIME | PASS | Playwright runtime test pass (chromium + mobile-chrome) |
| MI-AC-04 | Khởi chạy hoàn tất đánh dấu running (Đang chạy), nút đổi sang Stop | RUNTIME | PASS | Playwright runtime test pass (chromium + mobile-chrome) |
| MI-AC-05 | Click Stop dừng instance an toàn và trả về bình thường | RUNTIME | PASS | Playwright runtime test pass (chromium + mobile-chrome) |
| MI-AC-06 | Reduce motion chạy tức thì <= 0.05s | RUNTIME | PASS | Playwright runtime test pass (chromium + mobile-chrome) |
| MI-AC-07 | Responsive 390px, 620px, focus-visible | RUNTIME | PASS | Playwright runtime test pass (chromium + mobile-chrome) |
| MI-AC-08 | Chuyển động mượt mà | MANUAL_REQUIRED | PASS | User xác nhận nguyên văn: "xac mhan" (2026-10-26) |
| MI-AC-09 | Không hồi quy (check:web, check:static) | AUTOMATED | PASS | npm run check:web (108 passed) + check:static (192 passed) |
