# V.A.Z — Testing Strategy

## Mục tiêu

Web prototype là **executable specification**; WinUI app là bản implement theo cùng
behavior contract và design tokens. Feature UI chỉ được port sang WinUI sau khi web
đã được runtime-verify và contract đạt `WEB_VALIDATED`.

## 4 trạng thái feature UI

| Trạng thái | Ý nghĩa | Bằng chứng bắt buộc |
|------------|---------|---------------------|
| `WEB_SPEC` | Contract + feature doc + web implementation có, chưa runtime-verify | Contract, feature doc, web files |
| `WEB_VALIDATED` | Web khớp contract và user-visible behavior đã PASS | Playwright output + screenshot/trace |
| `APP_IMPLEMENTED` | WinUI đã port, build/test pass, runtime smoke đã chạy | Build/test output + smoke log/observation |
| `PARITY_VERIFIED` | App khớp contract + tokens + parity checklist | Build/test + smoke + parity checklist PASS |

Chỉ `web-verifier` được đặt `WEB_VALIDATED`; chỉ `app-verifier` được đặt
`APP_IMPLEMENTED` và `PARITY_VERIFIED`. Developer không tự đặt phase cuối.

## Cách chạy kiểm tra

```bash
# Static: dotnet build + dotnet test + validate 12 agent
npm run check:static
# hoặc
.\scripts\check.ps1

# Web contract/token/syntax check (KHÔNG phải runtime)
npm run check:web

# Runtime web: Playwright (NOT_CONFIGURED nếu chưa cài runner)
npm run check:runtime

# Static + web + runtime
npm run check:all
```

- `check:static` không chứng minh UI/flow hoạt động.
- `check:web` kiểm tra sự tồn tại/format của contract, `docs/design/tokens.json` và
  cú pháp JS; nó không thay thế Playwright.
- `check:runtime` in `RUNTIME CHECK NOT_CONFIGURED` (exit 2) nếu thiếu
  `node_modules/@playwright/test` hoặc config; khi đó AC `RUNTIME` là **BLOCKED**.
- Không có warning nào được bỏ qua hoặc tắt.

## Phân loại bằng chứng (bắt buộc)

| Loại | Dùng cho | Bằng chứng hợp lệ | Không phải bằng chứng |
|------|----------|-------------------|------------------------|
| `AUTOMATED` | build, typecheck, lint, unit/integration test, static analysis | command + exit code + output + tên test | đọc code, "test có vẻ đúng" |
| `RUNTIME` | UI, flow, navigation, play/stop, settings persistence, empty/error state, app launch | chạy app/browser + kết quả nhìn thấy, screenshot, log, URL | build pass, unit test pass, code review |
| `MANUAL_REQUIRED` | không tự động hóa được | user xác nhận + bước làm + kết quả | lời khai của Developer |
| `BLOCKED` | chưa có runner/môi trường | lý do + runner/môi trường còn thiếu | mọi thứ khác |

Quy tắc: AC UI/flow phải có `RUNTIME` evidence. Không chạy được runtime test →
**BLOCKED**, không được **PASS**. `MANUAL_REQUIRED` chưa có user xác nhận → **BLOCKED**.
AC `BLOCKED` không bao giờ PASS. Không DONE khi còn `PENDING`/`UNTESTED`.

## Verification Contract

Mọi task phải có contract trước khi Developer bắt đầu:

| # | Acceptance criterion | Evidence type | Verification method | Required evidence | Status |
|---|----------------------|---------------|---------------------|-------------------|--------|
| 1 | ... | AUTOMATED / RUNTIME / MANUAL_REQUIRED / BLOCKED | ... | ... | PENDING / PASS / FAIL / BLOCKED |

Contract feature UI nằm tại `docs/contracts/<feature>.contract.md`, có thêm phase
`WEB_SPEC` / `WEB_VALIDATED` / `APP_IMPLEMENTED` / `PARITY_VERIFIED`.
Contract mẫu: `docs/contracts/settings.contract.md`, `docs/contracts/library.contract.md`.

## Tầng 1: Static + unit test

```bash
dotnet build Vaz.sln
dotnet test Vaz.sln
```

Bằng chứng: exit code, số warning/error, tên test, tổng pass/fail/skip.
Build pass **không** phải runtime evidence.

## Tầng 2: Web contract/token check

```bash
npm run check:web
```

Kiểm tra:

- `docs/design-preview/index.html`, `app.js`, `styles.css` tồn tại.
- `docs/design/tokens.json` parse được và có đủ token bắt buộc:
  `color.surface`, `color.surfaceElevated`, `color.primary`, `color.danger`
  (dark + light), `spacing.xs/sm/md/lg`, `radius.sm/md/lg`.
- Có ít nhất 2 contract trong `docs/contracts/` với `id` và status hợp lệ.
- `node --check` cho `docs/design-preview/app.js` và `tests/web-e2e/serve.mjs`.

Đây là `AUTOMATED`, không phải `RUNTIME`.

## Tầng 3: Web runtime — Playwright

**Vị trí**: `tests/web-e2e/` (config, `serve.mjs`, `vaz.spec.ts`).

### 3 test ưu tiên

| # | Test | Assert người dùng nhìn thấy |
|---|------|-----------------------------|
| 1 | Sidebar → Settings | Click mục Settings; trang Settings hiển thị, trang Instances ẩn |
| 2 | Settings lưu giá trị | RAM 8192 được lưu, status hiện thông báo, reload vẫn giữ 8192 |
| 3 | Empty state Library | Không có instance; `#instancesEmpty` hiện `Chưa có instance nào.` và `Bấm + để tạo instance mới.` |

Test phải assert user-visible result (text, visibility, value), không assert CSS class
nội bộ hay biến JavaScript.

### Cài đặt và chạy

```bash
npm install
npx playwright install chromium
npm run check:runtime
```

`NOT_CONFIGURED` = chưa có runner → ghi **BLOCKED**. Không được thay bằng
"đọc code thấy đúng" hoặc coi static/web check là runtime evidence.

## Tầng 4: WinUI app — build/test + runtime smoke + parity

**Hiện trạng**: Chưa có E2E automation cho WinUI; chưa cài Appium/FlaUI.
**E2E sau này**: Appium + Windows driver hoặc FlaUI.UIA3.

### Runtime smoke checklist (bắt buộc khi task có AC UI/flow WinUI)

1. Launch app thật (Visual Studio hoặc `dotnet run --project src/Vaz.WinUI`).
2. Xác nhận cửa sổ chính/main screen mở được, không crash ngay.
3. Kiểm tra log khởi động: không có exception/lỗi nghiêm trọng.
4. Thực hiện đúng flow trong acceptance criterion; ghi lại kết quả nhìn thấy.
5. Nếu không chạy được app → **BLOCKED**, không PASS.
6. UI chưa automation được → `MANUAL_REQUIRED`; chỉ PASS khi user xác nhận.

### Parity checklist

Mỗi feature có `.agent/parity/<feature>.parity.md`. App verifier phải đối chiếu:

- Cùng screen/route và entry point với web.
- Cùng user-visible text/trạng thái (empty/error/loading/success).
- Cùng validation và persistence behavior.
- Cùng design tokens: màu, spacing, radius, typography.
- Cùng interaction outcomes; khác biệt platform phải được ghi rõ và chấp thuận.
- Không regression chức năng cũ.

`PARITY_VERIFIED` chỉ khi mọi mục parity PASS và runtime evidence đúng loại.

## Quy tắc chống báo hoàn thành ảo

1. Không PASS nếu chưa chạy lệnh thật.
2. Không bỏ qua warning.
3. Không tự nghiệm thu; Developer không tự đặt phase cuối.
4. Không đoán kết quả test.
5. `BLOCKED` phải nêu runner/môi trường còn thiếu.
6. Feedback loop tối đa 3 vòng.
7. Không dùng code review/build/unit test thay runtime evidence.
8. Không đổi `BLOCKED`/`PENDING` thành `PASS`.
9. Không port WinUI trước `WEB_VALIDATED`.
10. Không đặt `PARITY_VERIFIED` khi parity còn fail/blocked.
