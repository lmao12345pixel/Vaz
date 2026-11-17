---
id: library
title: Library / Instances
status: WEB_SPEC
contract_version: 1
last_updated: 2026-10-24
surfaces:
  web: docs/design-preview/index.html
  app: src/Vaz.WinUI/Views/InstancesPage.xaml
---

# Library Contract

## 1. Mục đích

Library là màn hình mặc định sau khi mở launcher. Người dùng xem danh sách instance,
tìm instance, tạo instance mới và mở instance để chơi hoặc chỉnh sửa.

Contract này là nguồn sự thật cho cả web prototype và WinUI app. Không sửa hành vi
quan sát được ở một bên mà không cập nhật contract và parity checklist.

## 2. Phạm vi

Trong phạm vi:

- Danh sách instance và thứ tự hiển thị.
- Tìm kiếm, sắp xếp và empty state.
- Tạo instance từ nút New/`+`.
- Hành động Play và Edit trên card.
- Trạng thái launching/running quan sát được.

Ngoài phạm vi:

- Nội dung wizard chi tiết (loader/version/mod loader) — contract riêng.
- Mod, resource pack, shader, world, screenshot.
- Account, server, skin.
- Logic tải file hoặc Java runtime thật.

## 3. Dữ liệu

| Field | Kiểu | Ghi chú |
|-------|------|---------|
| `id` | string | Định danh ổn định của instance |
| `name` | string | Tên hiển thị, không rỗng |
| `mcVersion` | string | Phiên bản Minecraft hiển thị trên card |
| `loader` | string | `vanilla`, `fabric`, `forge`, `neoforge` |
| `lastPlayedTs` | number | Epoch ms; 0 nếu chưa chơi |
| `createdTs` | number | Epoch ms |
| `playtimeMin` | number | Phút đã chơi; 0 nếu chưa có |
| `running` | boolean | Trạng thái tạm, không persist |
| `launching` | boolean | Trạng thái tạm, không persist |

Web persist trong `localStorage` key `vazer-demo-v1`, field `instances`.
App dùng store tương đương; contract chỉ ràng buộc hành vi người dùng thấy.

## 4. Hành vi bắt buộc

### LIB-01 — Màn hình mặc định

Sau khi mở launcher, Library/Instances là màn hình đang hiển thị. Tiêu đề trang là
**Instances**; các màn hình khác không hiển thị đồng thời.

### LIB-02 — Danh sách instance

Mỗi instance hiển thị tối thiểu:

- tên instance;
- loader + phiên bản Minecraft;
- lần chơi gần nhất hoặc **Chưa chơi lần nào**;
- hành động Play và Edit.

Card đang chạy hiển thị trạng thái **Đang chạy**. Card đang khởi động hiển thị
trạng thái đang tải/launching, không hiển thị như đang chạy bình thường.

### LIB-03 — Empty state khi chưa có instance

Khi danh sách instance rỗng và không có bộ lọc/tìm kiếm:

- tiêu đề: **Chưa có instance nào.**
- gợi ý: **Bấm + để tạo instance mới.**
- empty state nhìn thấy được; không có card instance nào.

### LIB-04 — Tìm kiếm

Ô tìm kiếm lọc theo tên instance, phiên bản Minecraft và loader, không phân biệt
hoa/thường. Khi không có kết quả:

- tiêu đề: **Không tìm thấy instance nào.**
- gợi ý: **Thử từ khoá khác hoặc xoá bộ lọc.**

Xoá từ khoá phải khôi phục danh sách đầy đủ.

### LIB-05 — Sắp xếp

Có ba thứ tự:

| Giá trị | Nhãn | Quy tắc |
|---------|------|---------|
| `lastPlayed` | Chơi gần nhất | `lastPlayedTs` giảm dần |
| `name` | Tên A-Z | so sánh theo locale tiếng Việt |
| `created` | Mới tạo | `createdTs` giảm dần |

Mặc định là `lastPlayed`.

### LIB-06 — Tạo instance

Bấm New/`+` mở wizard tạo instance. Tên instance là bắt buộc:

- tên rỗng → lỗi **Tên instance không được để trống.**, không tạo instance;
- hoàn tất wizard → instance mới xuất hiện trong Library;
- instance mới được thêm vào đầu danh sách mặc định;
- sau reload, instance vẫn còn;
- có thông báo xác nhận tạo thành công.

### LIB-07 — Kế thừa cài đặt chung

Instance mới nhận RAM và Java mặc định từ Settings tại thời điểm tạo. Thay đổi
Settings sau đó không tự ghi đè instance đã tạo.

### LIB-08 — Play

Bấm Play trên card:

- card chuyển sang trạng thái launching/running nhìn thấy được;
- không mở nhầm trang khác;
- nếu launch thất bại, người dùng thấy thông báo lỗi, không im lặng.

### LIB-09 — Edit

Bấm Edit (hoặc card) mở trang chi tiết của đúng instance, hiển thị tên instance
tương ứng.

### LIB-10 — Xoá instance

Xoá instance phải yêu cầu xác nhận. Sau khi xác nhận:

- card biến mất;
- nếu không còn instance, empty state LIB-03 hiển thị;
- sau reload, instance đã xoá không quay lại.

## 5. Acceptance criteria

| # | Acceptance criterion | Evidence type | Verification method | Required evidence |
|---|----------------------|---------------|---------------------|-------------------|
| LIB-AC-01 | Library là màn hình mặc định | RUNTIME | Mở web/app, quan sát | Screenshot/Playwright assertion trang Instances visible |
| LIB-AC-02 | Card hiển thị tên, loader/version, lần chơi, Play/Edit | RUNTIME | Tạo dữ liệu mẫu, quan sát card | Ảnh hoặc Playwright assertion text card |
| LIB-AC-03 | Empty state đúng text khi không có instance | RUNTIME | Seed `instances: []` rồi reload | Playwright assertion `#instancesEmpty`, title/hint |
| LIB-AC-04 | Tìm kiếm lọc đúng và có empty state riêng | RUNTIME | Nhập từ khoá khớp/không khớp | Ảnh hoặc Playwright assertion kết quả |
| LIB-AC-05 | Sắp xếp đúng 3 thứ tự | RUNTIME | Đổi sort, quan sát thứ tự card | Ảnh hoặc Playwright assertion thứ tự |
| LIB-AC-06 | Tạo instance: tên rỗng bị chặn, tên hợp lệ tạo card | RUNTIME | Mở wizard, thử cả hai trường hợp | Ảnh/Playwright + reload còn instance |
| LIB-AC-07 | Instance mới kế thừa RAM/Java từ Settings | RUNTIME | Đổi Settings, tạo instance, mở chi tiết | Ảnh hoặc kiểm tra giá trị hiển thị |
| LIB-AC-08 | Play chuyển trạng thái launching/running | RUNTIME | Bấm Play, quan sát card | Ảnh/Playwright assertion trạng thái |
| LIB-AC-09 | Edit mở đúng instance | RUNTIME | Bấm Edit trên card | Trang chi tiết hiển thị đúng tên |
| LIB-AC-10 | Xoá có xác nhận và không quay lại sau reload | RUNTIME | Xoá, xác nhận, reload | Empty state hoặc card biến mất |

Trên app, các AC trên là `MANUAL_REQUIRED` cho đến khi có Appium/FlaUI. Chưa có
xác nhận của người dùng → **BLOCKED**, không PASS.

## 6. Playwright tối thiểu

| Test | Assert người dùng thấy |
|------|------------------------|
| Library mặc định | Trang Instances visible; trang khác hidden |
| Empty state | `#instancesEmpty` visible; title **Chưa có instance nào.**; hint **Bấm + để tạo instance mới.** |
| Tạo instance | Tên rỗng hiện lỗi; tên hợp lệ tạo card và còn sau reload |
| Tìm kiếm không kết quả | Title **Không tìm thấy instance nào.**; hint tương ứng |
| Sort | Đổi sort làm thay đổi thứ tự card |

## 7. Parity checklist

- [ ] Cùng tiêu đề trang và thứ tự hành động.
- [ ] Cùng text empty state (không instance / không kết quả).
- [ ] Cùng quy tắc tìm kiếm và sắp xếp.
- [ ] Cùng điều kiện tạo instance và thông báo lỗi.
- [ ] Cùng hành vi Play/Edit và trạng thái launching/running.
- [ ] Cùng hành vi xoá + xác nhận.
- [ ] Cùng token màu/spacing/radius từ `docs/design/tokens.json`.
- [ ] Không có tính năng chỉ tồn tại ở một bên.

## 8. Trạng thái và chuyển giao

```
WEB_SPEC
  → web-developer implement trong docs/design-preview
  → web-verifier chạy Playwright, mọi AC web PASS
WEB_VALIDATED
  → app-developer port sang WinUI theo contract + tokens
  → app-verifier build/test + runtime smoke + parity checklist
APP_IMPLEMENTED
  → parity review với web evidence
PARITY_VERIFIED
```

Contract này hiện ở `WEB_SPEC`: web đã có implementation nhưng chưa có runtime
evidence trong môi trường hiện tại. Không bắt đầu port WinUI cho tới khi
`web-verifier` chuyển trạng thái sang `WEB_VALIDATED`.
