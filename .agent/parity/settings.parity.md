# Parity: Cài đặt (Settings) — Web ↔ WinUI (Round 2)

- Contract: `docs/contracts/settings.contract.md` (`PARITY_VERIFIED`, app-verifier 2026-10-29)
- Web phase evidence: `npx playwright test tests/web-e2e/settings.spec.ts` (chromium + mobile-chrome, 11 test/project PASS, 22/22)
- App phase evidence: `dotnet build Vaz.sln` (0 warning, 0 error), `dotnet test Vaz.sln` (208 pass), static check PASS (build + 208 test + 12 agent configs), WinUI runtime desktop smoke PASS (11/11 ACs).
- Verifier: app-verifier (Verdict: MATCH 11/11 — 2026-10-29)

| Contract AC | Web evidence (hành vi) | App evidence (hành vi) | Status |
|-------------|------------------------|------------------------|--------|
| SET-AC-01 | Chip `.settings-tab` lọc `.settings-card` theo `data-settings-group`, All hiện tất cả | `ListView` chip (WrapPanel layout) + `SettingsSectionVisConverter`; chọn "Giao diện" chỉ còn thẻ Giao diện, "Tất cả" hiện đủ 7 thẻ | MATCH |
| SET-AC-02 | Slider ⇄ input 2 chiều, clamp 1024–16384, bước 512, preset điền `#settingJvmArgs` | Slider ⇄ `NumberBox` 2 chiều, StepFrequency=512, SmallChange=512; clamp [1024, 16384] và làm tròn 512 MB; 4 preset (Standard/Aikar/ZGC/Custom); nạp và điền args đúng | MATCH |
| SET-AC-03 | width/height/fullscreen lưu và bảo toàn | 1600×900 + Fullscreen → `settings.json`; restart hiển thị lại đúng | MATCH |
| SET-AC-04 | `data-theme` trên `<html>` + `--accent` đổi ngay | `AppThemeManager` phủ toàn shell (root, NavView pane, dock); nạp theme/accent ngay khi app startup; mutate `AccentFillColor*`, `AccentTextFillColor*`, `FocusStrokeColorOuterBrush`; Dark/Light/Obsidian và 5 màu nhấn áp dụng tức thì | MATCH |
| SET-AC-05 | Class `is-reduced-motion` trên `<html>` | `Timeline.AllowDependentAnimations = !ReduceMotion`; `MainWindow` dùng `SuppressNavigationTransitionInfo` chuyển trang tức thời (0ms); lưu cờ `reduceMotion` vào `settings.json`; user xác nhận "tiwp tuc theo ke hoach" trên app runtime | MATCH |
| SET-AC-06 | Select 4 mirrors (cloudflare default/modrinth/curseforge/fastly) + 2/4/8/16 threads | Đúng 4 mirror, default `cloudflare`, chuẩn hóa `auto` → `cloudflare`; 4 lựa chọn luồng tải; lưu và nạp bền vững | MATCH |
| SET-AC-07 | `input` event cập nhật `#discordCustomText` tức thời, ảnh block/skin, separator "·", status | `TextBox` binding `UpdateSourceTrigger=PropertyChanged`; Discord preview hiển thị icon block (`grass_block.png`) + avatar skin (`steve.png`); separator "·"; RPC toggle cập nhật status "Đã bật/tắt Discord Rich Presence." và chuyển đổi opacity (1.0 vs 0.45) cùng ảnh grayscale | MATCH |
| SET-AC-08 | `#clearCacheBtn` → dialog xác nhận, toast dung lượng giải phóng + status | Dialog xác nhận (`DefaultButton=Primary`, nhãn "Xóa") → InfoBar "Đã xóa cache tải xuống · giải phóng 248 MB.", cache = 0 MB; hỗ trợ chọn thư mục dữ liệu/sao lưu | MATCH |
| SET-AC-09 | Spec OS/CPU/GPU/RAM/Java + export JSON download (`vaz-diagnostic-{ts}.json`) | Spec đọc phần cứng thật của máy; `FileSavePicker` xuất file JSON với đầy đủ `app`, `version`, `timestamp`, `os`, `cpu`, `ram`, `gpu`, `settings`, `activeInstance`, `instancesCount` khớp web spec | MATCH |
| SET-AC-10 | Dialog xác nhận (nhãn "Xác nhận") → reset default; Lưu ghi `settings.json` | Dialog "Khôi phục cài đặt mặc định?" (`DefaultButton=Primary`, nhãn "Xác nhận") → reset toàn bộ thiết lập về giá trị khởi tạo; nút Lưu (style accent đồng bộ mọi trạng thái) ghi `settings.json` nguyên tử (atomic tmp + replace) | MATCH |
| SET-AC-11 | Tiêu chí app: không hồi quy build/test; layout 1024px và ~800px không tràn/cắt; các trang Instances/Accounts giữ nguyên hoạt động | 208/208 unit tests pass; `npm run check:static` pass; ScrollViewer bọc nội dung Settings, WrapPanel cho chips, thích ứng kích thước cửa sổ không bị cắt cụt; các trang cũ hoạt động bình thường | MATCH |

## Kết quả xử lý các điểm lệch từ Round 1

1. **Shell Theme & Nav Pane (P0)**: Đã giải quyết bằng `AppThemeManager`. Theme Light/Obsidian phủ toàn bộ `ShellRoot`, `NavigationView` và dock với brush nền thích ứng (`VazShellBackgroundBrush`, `VazPaneBackgroundBrush`, `VazDockBackgroundBrush`).
2. **Khởi động nạp theme ngay (P0)**: Đã gọi `AppThemeManager.Initialize(settings)` ngay trong `App.xaml.cs` trước khi tạo `MainWindow` và navigate frame.
3. **Accent Control-Level (P0)**: Đã override các brush hệ thống `AccentFillColorDefault`, `AccentFillColorSecondary`, `AccentFillColorTertiary`, `AccentTextFillColorPrimary`, `FocusStrokeColorOuterBrush` trong ThemeDictionaries lúc runtime.
4. **Discord Preview (P1)**: Đã import asset PNG `grass_block.png`, `steve.png` và các biến thể grayscale `grass_block_gray.png`, `steve_gray.png`. Separator đổi thành "·", status cập nhật khi bật/tắt RPC.
5. **Mirror & JVM Presets (P1)**:
   - Loại bỏ Shenandoah GC, giữ đúng 4 options (Standard, Aikar, ZGC, Custom).
   - Loại bỏ Mirror "auto", giữ đúng 4 options (Cloudflare default, Modrinth, CurseForge, Fastly).
6. **Báo cáo chẩn đoán JSON (P1)**: Đã bổ sung trường `activeInstance` bên cạnh `instancesCount`.
7. **Nút Dialog (P1)**: Reset Dialog có `DefaultButton=Primary` với nhãn "Xác nhận"; Xóa cache có `DefaultButton=Primary` với nhãn "Xóa".
8. **RAM Step (P2)**: Cấu hình `StepFrequency="512"` và `SmallChange="512"` trên Slider và NumberBox; `SettingsService` tự động làm tròn về bội số 512 MB.
9. **Nút Lưu (P2)**: Áp dụng `VazAccentButtonStyle` duy trì màu accent cho mọi trạng thái hover/pressed/focused.

## UI smoke & Verification (Round 2)

1. Launch app: `Vaz.WinUI.exe` nạp theme/accent đã lưu từ `settings.json` ngay tại trang Instances.
2. Nav Settings: 8 chips WrapPanel, cuộn mượt mà với 7 cards.
3. Kích thước cửa sổ hẹp: 1024px và ~800px hiển thị đầy đủ, không cắt góc.
4. Không hồi quy: Instances và Accounts navigation hoạt động bình thường, 208/208 tests pass.
