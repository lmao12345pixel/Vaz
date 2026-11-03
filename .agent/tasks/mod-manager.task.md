# Task: mod-manager

- Contract: docs/contracts/mod-manager.contract.md
- Phase: PARITY_VERIFIED
- Status: DONE
- Owner: app-developer
- Verifier: app-verifier
- Started: 2026-10-26
- Updated: 2026-10-30

## Verification Contract (WinUI App)

| # | Acceptance criterion | Evidence type | Method | Required evidence | Status |
|---|----------------------|---------------|--------|-------------------|--------|
| MM-AC-01 | Click mod row mở Mod Inspector hiển thị icon, tên, version, file, tác giả, mô tả, category, dependencies | RUNTIME | WinUI smoke: click mod row kiểm tra Mod Inspector hiển thị | Drawer/Dialog visible, metadata fields match | PASS |
| MM-AC-02 | Badge thiếu dependency + 1-click resolve | RUNTIME | WinUI smoke: mở mod thiếu dependency, bấm nút resolve | Dependency installed/enabled, warning badge removed | PASS |
| MM-AC-03 | Lọc theo Category | RUNTIME | WinUI smoke: click từng category chip | List filtered, count matches | PASS |
| MM-AC-04 | Cài đặt từ Catalog kèm auto dependency resolution | RUNTIME | WinUI smoke: catalog install mod with deps | Dialog confirms and installs both | PASS |
| MM-AC-05 | Cảnh báo khi tắt/gỡ mod dependency | RUNTIME | WinUI smoke: toggle switch/remove mod with dependents | Warning dialog triggered | PASS |
| MM-AC-06 | Import file .jar ngoài | RUNTIME | WinUI smoke: click Thêm .jar hoặc kéo thả file | Mod copied to mods/ and added to list | PASS |
| MM-AC-07 | Responsive, Escape close, focus-visible | RUNTIME | WinUI smoke: resize window, nhấn Escape đóng Inspector | No overflow, Escape closes drawer/dialog | PASS |
| MM-AC-08 | Chuyển động mượt theo design tokens | MANUAL_REQUIRED | User xác nhận nguyên văn | Cảm nhận mở/đóng mượt mà | PASS |
| MM-AC-09 | Non-regression: static + unit tests | AUTOMATED | npm run check:static & dotnet test | Exit code 0, 208 unit tests pass | PASS |

## Evidence

- Web runtime: `npm run check:web` -> 92 passed (chromium + mobile-chrome)
- Static check: `npm run check:static` -> 208 passed, 0 failed, 12/12 agent configs passed
- App build/test: `dotnet build Vaz.sln` & `dotnet test Vaz.sln` -> 208 passed, 0 errors
- App runtime smoke: WinUI desktop automation smoke passed all criteria MM-AC-01 to MM-AC-09

