---
id: crash-analyzer
status: WEB_VALIDATED
owner: team/product-owner
surfaces: [web, app]
web_route: "instance detail: tab logs"
app_route: "instance detail: tab logs"
design_tokens: docs/design/tokens.json
updated: 2026-10-24
---

# Contract: Crash Analyzer & Auto Diagnostic Assistant

## 1. Mục đích

Tự động phân tích stack trace và log lỗi khi game crash, nhận diện nguyên nhân gốc rễ
trong 4 nhóm lỗi phổ biến (Tràn RAM, Sai Java Runtime, Thiếu Dependency, Xung đột Mixin),
và cung cấp nút 1-Click Quick Fix để khắc phục cấu hình tức thì kèm tính năng sao chép
báo cáo chẩn đoán. Web prototype là **executable specification**; WinUI chỉ được port
sau khi contract này đạt `WEB_VALIDATED`.

## 2. Phạm vi

### Trong phạm vi

- Tự động phát hiện crash khi instance kết thúc bất thường hoặc log chứa lỗi nghiêm trọng.
- Hiển thị banner `#crashAnalyzerBanner` trên đầu tab Logs.
- Nhận diện 4 chữ ký crash:
  1. `oom`: `OutOfMemoryError` hoặc `Java heap space`.
  2. `java`: `UnsupportedClassVersionError` hoặc sai lệch class file version.
  3. `dependency`: `requires fabric-api`, `missing dependency`, hoặc mod yêu cầu mod phụ thuộc.
  4. `mixin`: `MixinApplyError` hoặc `MixinTransformerError`.
- Hành động 1-Click Quick Fix:
  - `oom`: Tăng RAM thêm 2048 MB (làm tròn step 512, clamp `4096..16384` MB).
  - `java`: Đổi `javaPath` thành Java 21 LTS (`C:\Program Files\Java\jdk-21\bin\javaw.exe`).
  - `dependency`: Cài đặt mod thiếu vào instance (`Fabric API` v0.92.0, `enabled: true`).
  - `mixin`: Vô hiệu hóa mod xung đột (`enabled: false`).
- Nút "Sao chép báo cáo" (`#crashCopyReportBtn`): Lưu format tóm tắt chẩn đoán vào clipboard.
- Nút "Bỏ qua" (`#crashIgnoreBtn`) và đóng banner: Ẩn banner.
- Chuyển banner sang trạng thái "Đã sửa lỗi" (`is-fixed`) sau khi áp dụng Quick Fix.

### Ngoài phạm vi

- Gửi crash report ra server cloud bên thứ 3.
- Decompile hay vá bytecode runtime của Java mod.

## 3. Dữ liệu & Quy tắc xử lý

| Trường | Kiểu | Mô tả |
|--------|------|-------|
| `type` | `"oom" \| "java" \| "dependency" \| "mixin"` | Loại chữ ký lỗi |
| `badge` | string | Nhãn ngắn gọn hiển thị trên banner |
| `title` | string | Tiêu đề lỗi |
| `desc` | string | Giải thích nguyên nhân thân thiện người dùng |
| `snippet` | string | Trích đoạn dòng log lỗi chính xác |
| `actionLabel` | string | Nhãn nút Quick Fix |

## 4. Hành vi người dùng thấy

| ID | Hành vi | Kết quả mong đợi |
|----|---------|------------------|
| CRASH-01 | Instance crash do OOM | Banner hiện nhãn `Tràn RAM`, snippet `OutOfMemoryError`, nút Quick Fix nâng RAM |
| CRASH-02 | Click Quick Fix OOM | RAM instance tăng +2048 MB, banner chuyển `is-fixed` "Đã sửa lỗi", toast thông báo |
| CRASH-03 | Instance crash do sai Java | Banner hiện nhãn `Sai Java`, snippet `UnsupportedClassVersionError`, nút Quick Fix đổi Java 21 LTS |
| CRASH-04 | Click Quick Fix Java | `javaPath` đổi sang Java 21 LTS, banner chuyển `is-fixed`, toast thông báo |
| CRASH-05 | Instance crash do thiếu Dependency | Banner hiện nhãn `Thiếu Dependency`, snippet `requires fabric-api`, nút Quick Fix cài Fabric API |
| CRASH-06 | Click Quick Fix Dependency | Mod `fabric-api` được thêm vào instance với `enabled: true`, banner chuyển `is-fixed`, toast thông báo |
| CRASH-07 | Instance crash do Mixin | Banner hiện nhãn `Xung đột Mixin`, snippet `MixinApplyError`, nút Quick Fix tắt mod xung đột |
| CRASH-08 | Click Quick Fix Mixin | Mod xung đột bị tắt (`enabled: false`), banner chuyển `is-fixed`, toast thông báo |
| CRASH-09 | Click Sao chép báo cáo | Diagnostic report được copy vào clipboard, toast hiển thị xác nhận |
| CRASH-10 | Click Bỏ qua hoặc đóng | Banner `#crashAnalyzerBanner` bị ẩn (`hidden = true`) |

## 5. Acceptance criteria và bằng chứng

| # | Acceptance criterion | Web evidence | Cách kiểm web | App evidence | Cách kiểm app | Status |
|---|----------------------|--------------|---------------|--------------|---------------|--------|
| CRASH-AC-01 | Auto-detect OOM & Quick Fix nâng RAM +2GB | `RUNTIME` | Playwright: simulate OOM, assert banner, click quick fix, assert RAM tăng +2048 MB, assert banner is-fixed | `MANUAL_REQUIRED` | Smoke: chạy crash OOM, click fix, kiểm tra RAM | PENDING |
| CRASH-AC-02 | Auto-detect Sai Java & Quick Fix Java 21 | `RUNTIME` | Playwright: simulate java, assert banner, click quick fix, assert javaPath đổi sang Java 21 LTS | `MANUAL_REQUIRED` | Smoke: chạy crash Java, click fix, kiểm tra path | PENDING |
| CRASH-AC-03 | Auto-detect Thiếu Dependency & Quick Fix cài mod | `RUNTIME` | Playwright: simulate dependency, assert banner, click quick fix, assert fabric-api có trong mods và enabled | `MANUAL_REQUIRED` | Smoke: chạy crash dependency, click fix, kiểm tra mod list | PENDING |
| CRASH-AC-04 | Auto-detect Mixin & Quick Fix tắt mod | `RUNTIME` | Playwright: simulate mixin, assert banner, click quick fix, assert mod xung đột enabled = false | `MANUAL_REQUIRED` | Smoke: chạy crash mixin, click fix, kiểm tra mod status | PENDING |
| CRASH-AC-05 | Nút Sao chép báo cáo chẩn đoán | `RUNTIME` | Playwright: click copy report, assert toast thông báo | `MANUAL_REQUIRED` | Smoke: click copy report, dán xem nội dung | PENDING |
| CRASH-AC-06 | Nút Bỏ qua ẩn banner | `RUNTIME` | Playwright: click Bỏ qua, assert banner hidden | `MANUAL_REQUIRED` | Smoke: click Bỏ qua, banner biến mất | PENDING |
