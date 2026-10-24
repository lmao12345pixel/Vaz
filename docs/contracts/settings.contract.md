---
id: settings
status: WEB_SPEC
owner: team/product-owner
surfaces: [web, app]
web_route: "sidebar: Settings"
app_route: "Settings page"
design_tokens: docs/design/tokens.json
updated: 2026-10-24
---

# Contract: Settings

## 1. Mục đích

Cho người dùng cấu hình chung cho launcher: Java, RAM, theme, giảm chuyển động và
đường dẫn dữ liệu. Web prototype là **executable specification**; WinUI chỉ được port
sau khi contract này đạt `WEB_VALIDATED`.

## 2. Phạm vi

### Trong phạm vi

- Mở trang Settings từ sidebar.
- Đọc/ghi các trường cài đặt chung.
- Áp dụng theme và reduce-motion ngay khi lưu.
- Hiển thị phản hồi lưu / khôi phục mặc định.
- Giữ cài đặt sau reload web; app phải giữ sau khi mở lại.
- Reset về mặc định có xác nhận.

### Ngoài phạm vi

- Phát hiện Java thật hoặc tải Java.
- Ghi file/thư mục thật trên disk.
- Đồng bộ cài đặt giữa nhiều máy.
- Cài đặt riêng của từng instance (thuộc contract instance-settings).

## 3. Dữ liệu

`globalSettings` là một object dùng chung cho mọi instance mới.

| Field | Kiểu | Mặc định | Quy tắc |
|-------|------|----------|---------|
| `javaPath` | string | `""` | Trim khoảng trắng; rỗng = dùng Java mặc định |
| `ramMb` | number | `4096` | Clamp `1024..16384`, làm tròn số nguyên |
| `theme` | `"dark" \| "light"` | theme hiện tại | Giá trị khác bị coi là không hợp lệ |
| `reduceMotion` | boolean | `false` | Bật/tắt hiệu ứng chuyển động |
| `dataPath` | string | đường dẫn demo | Chỉ đọc trong contract này |

Web lưu vào `localStorage` key `vazer-demo-v1` dưới `globalSettings`.
App phải lưu vào settings store tương đương và giữ nguyên ngữ nghĩa field.

## 4. Hành vi người dùng thấy

| ID | Hành vi | Kết quả mong đợi |
|----|---------|------------------|
| SET-01 | Click `Settings` ở sidebar | Trang `Cài đặt` hiển thị; trang Instances ẩn |
| SET-02 | Kéo slider RAM hoặc nhập số | Hai control đồng bộ; giá trị ngoài khoảng bị kẹp về `1024..16384` |
| SET-03 | Click `Lưu cài đặt` | Status hiện `Đã lưu cài đặt chung.` |
| SET-04 | Reload web / mở lại app | Giá trị đã lưu vẫn còn, gồm RAM, Java path, theme, reduce motion |
| SET-05 | Chọn theme và lưu | Toàn bộ giao diện đổi theme ngay; reload vẫn giữ theme |
| SET-06 | Bật `Giảm chuyển động` và lưu | Hiệu ứng chuyển động giảm ngay; reload vẫn giữ |
| SET-07 | Nhập Java path có khoảng trắng rồi lưu | Giá trị được trim; rỗng quay về Java mặc định |
| SET-08 | Click `Mặc định` | Có hộp xác nhận; đồng ý thì reset và status `Đã khôi phục cài đặt mặc định.` |
| SET-09 | Dữ liệu lưu bị thiếu/hỏng | Không crash; dùng mặc định và clamp RAM hợp lệ |

## 5. Acceptance criteria và bằng chứng

> `WEB_SPEC` chỉ được chuyển `WEB_VALIDATED` khi **mọi** AC web có runtime evidence.
> App chỉ được bắt đầu sau `WEB_VALIDATED`. UI app chưa automation phải là
> `MANUAL_REQUIRED`; chưa có user xác nhận thì `BLOCKED`, không PASS.

| # | Acceptance criterion | Web evidence | Cách kiểm web | App evidence | Cách kiểm app | Status |
|---|----------------------|--------------|---------------|--------------|---------------|--------|
| SET-AC-01 | Sidebar mở đúng trang Settings | `RUNTIME` | Playwright: click nav, assert heading + page visible, Instances hidden | `MANUAL_REQUIRED` | Smoke: mở app, click Settings, quan sát | PENDING |
| SET-AC-02 | RAM sync và clamp đúng | `RUNTIME` | Playwright: nhập 8192, assert slider; nhập 99999, assert 16384 | `MANUAL_REQUIRED` | Smoke: nhập cả hai chiều | PENDING |
| SET-AC-03 | Lưu hiện đúng phản hồi | `RUNTIME` | Playwright: click lưu, assert status text | `MANUAL_REQUIRED` | Smoke: click lưu, đọc status | PENDING |
| SET-AC-04 | Giá trị giữ sau reload/restart | `RUNTIME` | Playwright: lưu 8192, reload, mở Settings, assert 8192 | `MANUAL_REQUIRED` | Smoke: lưu, đóng/mở app, kiểm tra | PENDING |
| SET-AC-05 | Theme áp dụng và giữ | `RUNTIME` | Playwright: đổi theme, assert `data-theme`, reload, assert lại | `MANUAL_REQUIRED` | Smoke: đổi theme, quan sát, mở lại | PENDING |
| SET-AC-06 | Reduce motion áp dụng và giữ | `RUNTIME` | Playwright: bật, assert class `reduce-motion`, reload, assert lại | `MANUAL_REQUIRED` | Smoke: bật, quan sát animation, mở lại | PENDING |
| SET-AC-07 | Java path trim, rỗng dùng mặc định | `RUNTIME` | Playwright: nhập chuỗi có spaces, lưu, reload, assert đã trim | `MANUAL_REQUIRED` | Smoke: nhập, lưu, mở lại | PENDING |
| SET-AC-08 | Reset mặc định có xác nhận | `RUNTIME` | Playwright: click Mặc định, assert dialog, xác nhận, assert status + defaults | `MANUAL_REQUIRED` | Smoke: reset, xác nhận | PENDING |
| SET-AC-09 | Storage thiếu/hỏng không crash | `AUTOMATED` + `RUNTIME` | Automated normalization test; Playwright seed JSON hỏng rồi reload | `AUTOMATED` + `MANUAL_REQUIRED` | Unit test normalize + smoke | PENDING |

## 6. Playwright tối thiểu

Các test bắt buộc trong `tests/web-e2e/vaz.spec.ts`:

1. `sidebar mở Settings và ẩn trang Instances` — SET-AC-01.
2. `Settings lưu RAM và giữ giá trị sau reload` — SET-AC-02/03/04.
3. `Settings áp dụng theme và giữ sau reload` — SET-AC-05.
4. `Settings reset về mặc định sau xác nhận` — SET-AC-08.

Chỉ assert hành vi người dùng thấy (heading, text, value, page ẩn/hiện). Không assert
CSS selector nội bộ, class trang trí, hay biến JavaScript.

## 7. Parity checklist (web → app)

- [ ] Cùng label `Cài đặt`, `Lưu cài đặt`, `Mặc định`.
- [ ] Cùng default `ramMb = 4096`, range `1024..16384`.
- [ ] Cùng status text `Đã lưu cài đặt chung.`
- [ ] Cùng status text `Đã khôi phục cài đặt mặc định.`
- [ ] Theme và reduce motion áp dụng ngay, giữ sau restart.
- [ ] Java path trim; rỗng dùng mặc định.
- [ ] Reset có xác nhận.
- [ ] Token màu/nền/radius lấy từ `docs/design/tokens.json`.

## 8. Điều kiện chuyển trạng thái

| Từ | Sang | Điều kiện |
|----|------|-----------|
| `WEB_SPEC` | `WEB_VALIDATED` | Tất cả AC web `PASS` bằng Playwright runtime thật; không còn `PENDING`/`BLOCKED` |
| `WEB_VALIDATED` | `APP_IMPLEMENTED` | app-developer port xong; build/test pass; Developer Report đầy đủ |
| `APP_IMPLEMENTED` | `PARITY_VERIFIED` | app-verifier chạy build/test + runtime smoke + parity checklist; AC app `PASS` |
