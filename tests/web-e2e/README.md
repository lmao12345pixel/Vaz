# V.A.Z — Web E2E Tests (Playwright)

## Mục đích

Đây là runtime evidence cho **executable specification** tại `docs/design-preview/`.
Web phải được chứng minh bằng Playwright trước khi WinUI port, theo contract trong
`docs/contracts/`. Test chỉ được coi là bằng chứng khi nó thực sự chạy và assert
kết quả người dùng nhìn thấy.

- Web AC PASS → web-verifier đặt contract `WEB_VALIDATED`.
- Runner thiếu → `RUNTIME CHECK NOT_CONFIGURED` → `BLOCKED`, không PASS.

## Trạng thái hiện tại

Runner **chưa được cài** trong môi trường này. Vì vậy:

```bash
npm run check:runtime
```

sẽ trả `RUNTIME CHECK NOT_CONFIGURED` (exit 2) — đây là hành vi đúng, không phải PASS.

## Cài đặt (một lần)

```bash
npm install
npx playwright install chromium
```

Sau đó chạy:

```bash
npm run check:runtime
# hoặc toàn bộ static + web + runtime:
npm run check:all
```

## 3 test ưu tiên

| # | Test | Assert người dùng nhìn thấy |
|---|------|-----------------------------|
| 1 | Sidebar → Settings | Click mục Settings; trang Settings hiển thị, trang Instances ẩn |
| 2 | Settings lưu giá trị | RAM 8192 được lưu, status hiện thông báo, reload vẫn giữ 8192 |
| 3 | Empty state Library | Không có instance; `#instancesEmpty` hiện `Chưa có instance nào.` và `Bấm + để tạo instance mới.` |

## Quy tắc viết test

- Assert `toBeVisible`, `toHaveText`, `toHaveValue`, URL, hoặc nội dung dialog — không chỉ `click()`.
- Không dùng `waitForTimeout` thay cho assertion.
- Mỗi test phải độc lập; `beforeEach` xoá localStorage.
- Không thêm test chỉ để tăng coverage; mỗi test phải gắn với một acceptance criterion runtime.

## WinUI

WinUI runtime chưa có runner tự động. Khi task có AC UI/flow của WinUI:

- Chạy app thật, xác nhận cửa sổ khởi động và log không có lỗi nghiêm trọng; hoặc
- Đánh dấu `BLOCKED` nếu không chạy được.
- E2E sau này: Appium + Windows driver hoặc FlaUI; xem `docs/testing-strategy.md`.
