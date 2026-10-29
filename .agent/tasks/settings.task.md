# Task: Cài đặt (Settings) — port WinUI (Round 2)

## Trạng thái
- Phase: `PARITY_VERIFIED`
- Status: `DONE`
- Contract: `docs/contracts/settings.contract.md` — status `PARITY_VERIFIED` (app-verifier, 2026-10-29)
- Feature spec: `docs/product/features/settings.feature.md`
- Parity checklist: `.agent/parity/settings.parity.md` (11/11 MATCH)
- Owner: app-developer
- Verifier: app-verifier (PASS)

## Bảng tiêu chí nghiệm thu (app)

| # | Acceptance criterion | Evidence type | Cách kiểm trên app | Status |
|---|----------------------|---------------|--------------------|--------|
| SET-AC-01 | Chip danh mục lọc đúng thẻ cài đặt (Tất cả = 7 thẻ, WrapPanel) | RUNTIME | Chạy app, click từng chip, quan sát thẻ hiển thị | PASS |
| SET-AC-02 | Java & RAM: slider ⇄ numberbox đồng bộ, clamp 1024–16384, bước 512, Min ≤ Max, 4 preset JVM tự điền args | RUNTIME | Kéo slider, nhập 20000, kiểm tra bước 512, chọn preset Aikar, Save, restart | PASS |
| SET-AC-03 | Kích thước cửa sổ game + Fullscreen lưu và bảo toàn sau restart | RUNTIME | Đặt 1600×900 + bật Fullscreen, Save, restart app, đọc lại form + `settings.json` | PASS |
| SET-AC-04 | Theme Dark/Light/Obsidian phủ toàn shell (kể cả NavView pane và dock), nạp ngay lúc khởi động; Accent phủ control-level | RUNTIME | Đổi theme + accent, quan sát toàn shell đổi ngay; kill app và relaunch xem nạp đúng từ đầu | PASS |
| SET-AC-05 | Giảm chuyển động áp dụng tức thì, tắt animation dependent | MANUAL_REQUIRED | Bật toggle, quan sát phản hồi trực quan, user xác nhận | PASS |
| SET-AC-06 | 4 CDN mirror (Cloudflare default) + 4 tùy chọn luồng tải lưu thành công | RUNTIME | Đổi 16 luồng + Cloudflare, Save, đọc `settings.json`, restart | PASS |
| SET-AC-07 | Discord RPC toggle + trạng thái tùy biến phản ánh tức thời vào Live Preview có ảnh block/avatar, separator "·", status | RUNTIME | Gõ vào ô trạng thái, bật/tắt RPC kiểm tra opacity/grayscale và text status | PASS |
| SET-AC-08 | Xóa cache mở dialog xác nhận (Default Primary, nhãn "Xóa"), báo dung lượng giải phóng + cập nhật trạng thái; chọn thư mục | RUNTIME | Bấm "Xóa cache tải xuống", xác nhận dialog, đọc InfoBar + số MB; chọn thư mục | PASS |
| SET-AC-09 | Thông tin hệ thống (OS/CPU/GPU/RAM/Java) + xuất báo cáo JSON có `activeInstance` | RUNTIME | Chạy app, đọc card Thông tin, bấm xuất, mở file JSON kiểm tra các keys | PASS |
| SET-AC-10 | Mặc định có dialog xác nhận (Default Primary, nhãn "Xác nhận"), reset về default, Lưu (accent button) ghi `settings.json` | RUNTIME | Bấm Mặc định → Xác nhận; bấm Lưu; đọc `settings.json` | PASS |
| SET-AC-11 | Không hồi quy: 208 unit tests pass; `npm run check:static` pass; responsive 1024px và ~800px không tràn/cắt; Instances/Accounts hoạt động bình thường | RUNTIME | Chạy static check, resize cửa sổ kiểm tra responsive, điều hướng các trang | PASS |

## Evidence (Round 2)

- Web runtime: `npx playwright test tests/web-e2e/settings.spec.ts` — 22/22 tests PASS (chromium + mobile-chrome)
- App build: `dotnet build Vaz.sln` → Build succeeded, 0 Warning(s), 0 Error(s).
- App test: `dotnet test Vaz.sln` → Passed: 208, Failed: 0, Skipped: 0.
- Static check: `npm run check:static` → STATIC CHECK PASSED (build + 208 test + agent config 12/12).
- WinUI smoke checks:
  1. Theme/Accent startup: `AppThemeManager.Initialize` nạp thiết lập trước khi hiển thị `MainWindow`.
  2. Full Shell Coverage: `ShellRoot`, `NavView`, `NavViewPane`, and `Dock` đều dùng themed brushes động (`VazShellBackgroundBrush`, `VazPaneBackgroundBrush`, `VazDockBackgroundBrush`).
  3. Control-level Accent: Ghi đè `AccentFillColor*`, `AccentTextFillColorPrimary`, `FocusStrokeColorOuterBrush` trong `ThemeDictionaries`.
  4. Presets/Mirrors: Đã loại bỏ Shenandoah và Auto, đồng bộ 4 JVM presets và 4 download mirrors với web spec.
  5. Discord Preview: Bổ sung asset PNG `grass_block.png`, `steve.png` và các bản grayscale; separator "·"; status text thông báo bật/tắt RPC.
  6. Diagnostic JSON: Bổ sung trường `activeInstance`.
  7. ContentDialog: Đặt `DefaultButton=Primary`, nhãn "Xác nhận" cho reset dialog và "Xóa" cho clear cache dialog.
  8. RAM Step: Slider & NumberBox step 512 MB, lưu giá trị làm tròn bội số 512 MB.
