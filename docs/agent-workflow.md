# V.A.Z — Agent Workflow (dual-target)

## Mô hình

**Web prototype là executable specification; WinUI app là bản implement theo contract.**
Feature UI phải được chốt và kiểm chứng runtime trên web trước khi port sang WinUI.

```
WEB_SPEC → WEB_VALIDATED → APP_IMPLEMENTED → PARITY_VERIFIED
```

| Trạng thái | Ý nghĩa | Agent được đặt |
|------------|---------|----------------|
| `WEB_SPEC` | Contract + feature doc + web implementation đã có, chưa runtime-verify | Orchestrator |
| `WEB_VALIDATED` | Mọi AC web `RUNTIME` PASS bằng Playwright, khớp user-visible behavior | `web-verifier` |
| `APP_IMPLEMENTED` | WinUI đã port, build + test pass, runtime smoke đã chạy | `app-verifier` |
| `PARITY_VERIFIED` | App khớp contract + design tokens; parity checklist và runtime evidence PASS | `app-verifier` |

### Cổng bắt buộc

1. **Không gọi `app-developer` trước khi contract đạt `WEB_VALIDATED`.**
2. Không đặt trạng thái nào nếu thiếu bằng chứng đúng loại.
3. Web và app dùng chung `docs/contracts/<feature>.contract.md` và
   `docs/design/tokens.json`.
4. Web thay đổi sau `WEB_VALIDATED` → contract về `WEB_SPEC`, verify lại.
5. App thay đổi sau `PARITY_VERIFIED` → contract về `APP_IMPLEMENTED`, parity lại.
6. UI app chưa automation được → `MANUAL_REQUIRED` hoặc `BLOCKED`, không `PASS`.
7. Developer không được tự đặt phase cuối; chỉ verifier tương ứng được đặt.
8. Task core/backend không có UI dùng workflow generic, nhưng vẫn phải có
   Verification Contract và evidence đúng loại.

## Cấu trúc

| Đường dẫn | Vai trò |
|-----------|---------|
| `docs/product/features/` | Feature doc, user story, phạm vi, non-goals |
| `docs/contracts/` | Behavior contract dùng chung web + app |
| `docs/design/tokens.json` | Design tokens dùng chung |
| `.agent/tasks/` | Task ledger, phase hiện tại, evidence |
| `.agent/parity/` | Parity checklist theo feature |
| `tests/web-e2e/` | Playwright runtime tests cho web |

## Luồng feature UI

```
User
  │
  ▼
Orchestrator
  ├─ tạo Verification Contract
  ├─ tạo/cập nhật feature doc
  └─ đặt status: WEB_SPEC
  │
  ▼
Web Developer
  ├─ implement docs/design-preview/
  ├─ thêm Playwright test trong tests/web-e2e/
  └─ cập nhật Developer Report
  │
  ▼
Web Verifier
  ├─ chạy Playwright runtime
  ├─ assert user-visible behavior
  ├─ PASS hết web AC → WEB_VALIDATED
  └─ NOT_CONFIGURED/FAIL → BLOCKED, dừng
  │
  ▼
App Developer (chỉ sau WEB_VALIDATED)
  ├─ port WinUI theo contract + tokens
  ├─ cập nhật .agent/parity/<feature>.parity.md
  └─ cập nhật Developer Report
  │
  ▼
App Verifier
  ├─ build + test
  ├─ runtime smoke
  ├─ parity checklist
  ├─ build/test pass → APP_IMPLEMENTED
  └─ parity + runtime pass → PARITY_VERIFIED
  │
  ▼
Tester → Reviewer → QA Lead
  │
  ├─ mọi AC PASS + PARITY_VERIFIED → DONE
  └─ FAIL/BLOCKED/PENDING → feedback (tối đa 3 vòng) hoặc báo user
```

## Verification Contract

| # | Acceptance criterion | Evidence type | Verification method | Required evidence | Status |
|---|----------------------|---------------|---------------------|-------------------|--------|
| 1 | ... | AUTOMATED / RUNTIME / MANUAL_REQUIRED / BLOCKED | ... | ... | PENDING / PASS / FAIL / BLOCKED |

Metadata bắt buộc cho feature UI:

```yaml
id: <feature>
status: WEB_SPEC | WEB_VALIDATED | APP_IMPLEMENTED | PARITY_VERIFIED
owner: team/product-owner
surfaces: [web, app]
web_route: docs/design-preview/...
app_route: src/Vaz.WinUI/...
design_tokens: docs/design/tokens.json
contract_version: 1
last_updated: YYYY-MM-DD
```

### Evidence type

| Loại | Dùng cho | Bằng chứng hợp lệ |
|------|----------|-------------------|
| `AUTOMATED` | build, typecheck, unit/integration test, static analysis | command + exit code + output + tên test |
| `RUNTIME` | UI, flow, navigation, persistence, empty/error state, app launch | chạy app/browser + quan sát, screenshot, log, URL |
| `MANUAL_REQUIRED` | không tự động hóa được | user xác nhận + bước làm + kết quả |
| `BLOCKED` | chưa có runner/môi trường | lý do + runner/môi trường còn thiếu |

- AC UI/flow **phải** là `RUNTIME`.
- Build pass, unit test pass, code review không phải bằng chứng `RUNTIME`.
- `MANUAL_REQUIRED` chưa có user xác nhận → `BLOCKED`.
- AC `BLOCKED` không bao giờ `PASS`.
- Không DONE khi còn `PENDING`/`UNTESTED`.

## Chi tiết từng bước

### Bước 0 — Orchestrator

1. Nhận yêu cầu, xác định feature UI hay task core/backend.
2. Feature UI: tạo contract + feature doc, giao Product Owner và Planner.
3. Tạo task ledger trong `.agent/tasks/` với phase hiện tại.
4. Không gọi developer khi chưa có contract.
5. Sau mỗi verifier, chỉ chuyển phase khi contract đã được cập nhật bằng bằng chứng.

### Bước 1 — Product Owner

User story, scope, non-goals, acceptance criteria, DoD. Với feature UI, AC phải
mô tả behavior người dùng thấy, không mô tả CSS/internal state.

### Bước 2 — Planner

File ảnh hưởng, kế hoạch web trước/app sau, rủi ro, thứ tự test. Không lập kế hoạch
port WinUI khi contract chưa `WEB_VALIDATED`.

### Bước 3 — Web Developer

Implement `docs/design-preview/` theo contract; dùng `docs/design/tokens.json`;
thêm/cập nhật Playwright test trong `tests/web-e2e/`; chạy `npx playwright test`
(hoặc `npm run check:web`) và dán output thật. Không được xoá/sửa test để ép pass.
Cập nhật Developer Report; **không** tự đặt `WEB_VALIDATED`.

### Bước 4 — Web Verifier

Chạy Playwright, assert user-visible behavior. Không chạy được runner →
`NOT_CONFIGURED` + `BLOCKED`. Chỉ đặt `WEB_VALIDATED` khi mọi AC web `RUNTIME` PASS.
Output là contract đã cập nhật + evidence thật.

### Bước 5 — App Developer

Chỉ bắt đầu khi contract `WEB_VALIDATED`. Port WinUI theo contract + tokens; không
tự đổi behavior. Cập nhật Developer Report và `.agent/parity/<feature>.parity.md`.
Chạy `dotnet build Vaz.sln`, `dotnet test Vaz.sln`, runtime smoke hoặc ghi
`MANUAL_REQUIRED`/`BLOCKED`. **Không** tự đặt `APP_IMPLEMENTED`/`PARITY_VERIFIED`.

### Bước 6 — App Verifier

Build/test + runtime smoke + parity checklist. UI chưa automation:
`MANUAL_REQUIRED`; chưa có user xác nhận → `BLOCKED`. Đặt `APP_IMPLEMENTED` khi
port/build/test pass; chỉ đặt `PARITY_VERIFIED` khi mọi parity item và AC PASS.

### Bước 7 — Tester, Reviewer, QA Lead

Tester chạy test/edge case/regression. Reviewer review diff, kiến trúc, bảo mật,
hiệu năng. QA Lead verdict PASS/FAIL/BLOCKED dựa trên contract đã cập nhật.
PASS chỉ khi mọi AC PASS + feature UI đạt `PARITY_VERIFIED`.

## Task core/backend

```
Orchestrator → Product Owner → Planner → Developer → Tester → Reviewer
→ Verifier → QA Lead
```

Dùng `team/developer` và `verifier`. Vẫn bắt buộc Verification Contract; AC
`RUNTIME` (nếu có) phải có runtime evidence hoặc `BLOCKED`.

## Lệnh kiểm tra

```bash
npm run check:static   # dotnet build + dotnet test + validate agents
npm run check:web      # Playwright gate: chromium + mobile-chrome + contract/token checks
npm run check:runtime  # alias tương thích của check:web
npm run check:all      # static + web gate
```

`check:static` pass **không** chứng minh UI/flow hoạt động. `check:web` chỉ có giá trị
khi Playwright thực sự chạy và pass; thiếu runner → `WEB CHECK NOT_CONFIGURED`/`BLOCKED`.

## Handoff Template

```markdown
## Handoff: [Agent nguồn] → [Agent đích]

### Context
- Feature: [id]
- Phase: [WEB_SPEC/WEB_VALIDATED/APP_IMPLEMENTED/PARITY_VERIFIED]
- Vòng: [1/2/3]

### Verification Contract
| # | AC | Evidence type | Verification method | Required evidence | Status |
|---|----|---------------|---------------------|-------------------|--------|

### Input
[Dán output của agent trước]

### Evidence
- Commands:
- Output:
- Runtime/screenshot/log/URL:

### Còn thiếu
- ...
```

## Escalation

- Web runner chưa có → `BLOCKED`; hướng dẫn `npm install` +
  `npx playwright install chromium`; không chuyển sang app.
- Web AC fail → feedback cho Web Developer; tối đa 3 vòng.
- App build/test fail → feedback cho App Developer.
- App UI chưa automation → `MANUAL_REQUIRED`; user chưa xác nhận → `BLOCKED`.
- Parity fail → contract về `APP_IMPLEMENTED`; sửa và parity lại.
- QA Lead FAIL 3 vòng → dừng, báo user danh sách vấn đề còn lại.
