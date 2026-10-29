# Task: mod-manager

- Contract: docs/contracts/mod-manager.contract.md
- Phase: WEB_VALIDATED
- Owner: web-developer
- Started: 2026-10-26

## Verification Contract

| # | Acceptance criterion | Evidence type | Method | Required evidence | Status |
|---|----------------------|---------------|--------|-------------------|--------|
| MM-AC-01 | Mở Mod Inspector hiển thị chi tiết mod và dependencies | RUNTIME | Playwright assertion on #modInspector | Selector + element visible + fields match | PASS |
| MM-AC-02 | Badge thiếu dependency + 1-click resolve | RUNTIME | Playwright click resolve | Dependency installed/enabled, badge removed | PASS |
| MM-AC-03 | Lọc theo Category | RUNTIME | Playwright category chips | List filtered, count matches | PASS |
| MM-AC-04 | Cài đặt từ Catalog kèm auto dependency resolution | RUNTIME | Playwright catalog install | Dialog confirms and installs both | PASS |
| MM-AC-05 | Cảnh báo khi tắt/gỡ mod dependency | RUNTIME | Playwright toggle with dependents | Warning toast/dialog triggered | PASS |
| MM-AC-06 | Import file .jar ngoài | RUNTIME | Playwright drop / add jar | Mod added to list | PASS |
| MM-AC-07 | Responsive (390px, 620px), Escape close, focus-visible | RUNTIME | Playwright viewport & keys | scrollWidth <= clientWidth, outline >= 2px | PASS |
| MM-AC-08 | Chuyển động mượt theo design tokens | MANUAL_REQUIRED | User xác nhận nguyên văn | User xác nhận "ok roi" (2026-10-26) | PASS |
| MM-AC-09 | Non-regression: static + web tests | AUTOMATED | npm run check:static & npm run check:web | Exit code 0, 92 web + 192 unit tests pass | PASS |

## Evidence

- Web runtime: `npm run check:web` -> 92 passed (chromium + mobile-chrome)
- Static check: `npm run check:static` -> 192 passed, 0 failed, 12/12 agent configs passed
- App build/test: Chưa chạy (chưa port WinUI)
- App runtime smoke: Chưa chạy (chưa port WinUI)
- Manual confirmation: User xác nhận nguyên văn "ok roi" (2026-10-26) cho MM-AC-08.
