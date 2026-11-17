# V.A.Z — Agent System

## Bối cảnh

V.A.Z là Minecraft client/launcher lấy cảm hứng từ Luna Client, Valiant Client, Prism Launcher.
Hiện tại gồm:

- **Vazer.Core** (.NET 8, C#): domain logic — instances, mods, auth, Java resolver, loaders.
- **Vaz.WinUI** (WinUI 3): GUI desktop app.
- **Web prototype** (HTML/CSS/JS): executable specification tại `docs/design-preview/`.
- **Test suite** (xUnit): `tests/Vazer.Core.Tests/` — chạy `dotnet test` để lấy số test hiện tại.

## Stack thực tế

| Thành phần | Công nghệ |
|------------|-----------|
| Language | C# (.NET 8) |
| UI Framework | WinUI 3 (Windows App SDK) |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| DI | Microsoft.Extensions.DependencyInjection |
| Test | xUnit 2.9.3, FluentAssertions 8.11.0, Moq 4.21.0, coverlet |
| Web demo | HTML/CSS/JS thuần (không framework) |
| Web runtime | Playwright (`tests/web-e2e/`) |
| Build | dotnet CLI / MSBuild |
| VCS | Git |

## Mô hình dual-target

**Web prototype là executable specification; WinUI app là bản implement theo contract.**
Feature UI phải được thiết kế, chốt behavior và kiểm chứng runtime trên web **trước khi**
port sang WinUI.

### Bốn trạng thái bắt buộc

| Trạng thái | Ý nghĩa | Agent được đặt |
|------------|---------|----------------|
| `WEB_SPEC` | Contract + feature doc + web implementation đã có, chưa được runtime-verify | Orchestrator |
| `WEB_VALIDATED` | Mọi AC web `RUNTIME` đã PASS bằng Playwright, user-visible behavior khớp contract | `web-verifier` |
| `APP_IMPLEMENTED` | WinUI đã port, build + test pass, runtime smoke đã chạy | `app-verifier` |
| `PARITY_VERIFIED` | App khớp contract + design tokens, parity checklist và runtime evidence đều PASS | `app-verifier` |

### Cổng bắt buộc

1. **Không bắt đầu WinUI trước khi contract đạt `WEB_VALIDATED`.**
2. Không có trạng thái nào được đặt nếu thiếu bằng chứng đúng loại.
3. Web và app dùng chung `docs/contracts/<feature>.contract.md` và `docs/design/tokens.json`.
4. Web thay đổi sau `WEB_VALIDATED` → đưa contract về `WEB_SPEC` và verify lại.
5. App thay đổi sau `PARITY_VERIFIED` → đưa contract về `APP_IMPLEMENTED` và parity lại.
6. UI app chưa automation được → `MANUAL_REQUIRED` hoặc `BLOCKED`, tuyệt đối không `PASS`.
7. Task core/backend không có UI dùng agent generic (`team/developer`, `verifier`); dual-target
   chỉ bắt buộc cho feature UI.

### Cấu trúc làm việc

| Đường dẫn | Vai trò |
|-----------|---------|
| `docs/product/features/` | Feature doc, user story, phạm vi |
| `docs/contracts/` | Behavior contract dùng chung web + app |
| `docs/design/tokens.json` | Design tokens dùng chung web + app |
| `.agent/tasks/` | Task ledger, trạng thái phase, evidence |
| `.agent/parity/` | Parity checklist theo feature |
| `tests/web-e2e/` | Playwright runtime tests cho web |

## Lệnh kiểm tra thực tế

```bash
# Static: build + unit test + validate agent config
npm run check:static
# hoặc không cần npm:
.\scripts\check.ps1 -Scope static
.\check.cmd

# Web gate: contract/token/JS + Playwright (chromium + mobile-chrome)
npm run check:web

# Alias runtime web (delegate sang check:web)
npm run check:runtime

# Static + web gate
npm run check:all

# Build toàn bộ solution
dotnet build Vaz.sln

# Chạy unit test
dotnet test Vaz.sln

# Build/test chỉ Core (nhanh hơn nếu chỉ sửa logic)
dotnet build src/Vazer.Core/Vazer.Core.csproj
dotnet test tests/Vazer.Core.Tests/Vazer.Core.Tests.csproj

# Kiểm tra frontmatter/quyền/testing-rule của agent
.\scripts\validate-agents.ps1
```

> **Không có lệnh lint/typecheck riêng** — C# compiler đã kiểm tra type. Build pass = typecheck pass.
> `npm run check` là alias cũ của static check; nó **không** chứng minh UI/flow hoạt động.
> Static check không cần npm install; runtime Playwright cần `npm install` +
> `npx playwright install chromium`.

## Quy tắc kiểm thử bắt buộc

### 4 loại bằng chứng

| Loại | Dùng cho | Bằng chứng hợp lệ | Không phải bằng chứng |
|------|----------|-------------------|------------------------|
| `AUTOMATED` | build, typecheck, lint, unit/integration test, static analysis | command + exit code + output + tên test | đọc code, "test có vẻ đúng" |
| `RUNTIME` | UI, flow, navigation, play/stop, settings persistence, empty/error state, app launch | chạy app/browser + kết quả nhìn thấy, screenshot, log, URL | build pass, unit test pass, code review |
| `MANUAL_REQUIRED` | không tự động hóa được | user xác nhận + bước làm + kết quả quan sát | lời khai của Developer |
| `BLOCKED` | chưa có runner/môi trường để kiểm | lý do + runner/môi trường còn thiếu | mọi thứ khác |

### Verification Contract

**Mọi task phải có Verification Contract** trước khi giao Developer. Mỗi acceptance
criterion phải có đúng một evidence type (`AUTOMATED`, `RUNTIME`, `MANUAL_REQUIRED`,
hoặc `BLOCKED`), cách kiểm, bằng chứng bắt buộc, và trạng thái
(`PENDING`, `PASS`, `FAIL`, `BLOCKED`).

- Không được DONE nếu còn AC `PENDING`/`UNTESTED`.
- AC `BLOCKED` không bao giờ được PASS.
- Chỉ Verifier được cập nhật trạng thái cuối của contract.
- Contract của feature UI phải ghi thêm phase `WEB_SPEC` / `WEB_VALIDATED` /
  `APP_IMPLEMENTED` / `PARITY_VERIFIED`.

### Quy tắc cứng

1. **Code review không phải kiểm thử.** Đọc diff chỉ để hiểu thay đổi.
2. **Build pass không phải kiểm thử.** Build chỉ chứng minh code biên dịch được.
3. **Unit test pass không tự động chứng minh UI/flow hoạt động.**
4. Mọi acceptance criterion `RUNTIME` phải có runtime evidence.
5. Không chạy được runtime test → **BLOCKED**, tuyệt đối không **PASS**.
6. `MANUAL_REQUIRED` chỉ PASS khi user đã xác nhận; chưa xác nhận → **BLOCKED**.
7. Verifier không được PASS dựa trên mô tả của Developer/Orchestrator.
8. Không dùng "đọc code thấy đúng" làm bằng chứng cho AC `RUNTIME`.
9. Nếu runtime runner chưa có, ghi `NOT_CONFIGURED`, nêu cách cài, và dừng ở **BLOCKED**.
10. `npm run check:static` không thay thế `npm run check:web` cho AC UI/flow.
11. Không gọi Developer khi chưa có Verification Contract.
12. Không nghiệm thu khi còn AC `PENDING`/`UNTESTED`.
13. Không đổi `BLOCKED`/`PENDING` thành `PASS` vì các AC khác đạt.
14. Mọi task phải có Developer Report và contract đã được Verifier cập nhật.
15. **Không gọi `app-developer` khi contract chưa `WEB_VALIDATED`.**
16. **Không đặt `WEB_VALIDATED` nếu chưa có Playwright runtime evidence.**
17. **Không đặt `PARITY_VERIFIED` nếu parity checklist còn mục fail/blocked.**
18. **Developer không được tự đặt phase cuối;** chỉ verifier tương ứng được đặt.

## Định nghĩa DONE bắt buộc

Một task chỉ được coi là DONE khi:

1. ✅ Có Verification Contract cho mọi acceptance criterion.
2. ✅ Mọi AC có trạng thái cuối `PASS`; không còn `PENDING`/`UNTESTED`/`BLOCKED`.
3. ✅ `npm run check:static` (hoặc `.\scripts\check.ps1`) — pass toàn bộ.
4. ✅ `npm run check:web` — pass (Playwright chromium + mobile-chrome, 3 test ưu tiên).
5. ✅ `dotnet build Vaz.sln` — pass, 0 error; `dotnet test Vaz.sln` — tất cả pass, không skip.
6. ✅ Mọi AC `AUTOMATED` có command + output + tên test thật.
7. ✅ Mọi AC `RUNTIME` có runtime evidence thật (web Playwright/app chạy thật).
8. ✅ Mọi AC `MANUAL_REQUIRED` có xác nhận của user.
9. ✅ Feature UI đạt `PARITY_VERIFIED`; web đã `WEB_VALIDATED` trước khi port app.
10. ✅ Không regression: chức năng cũ vẫn hoạt động.
11. ✅ Reviewer APPROVE — không còn Critical/Major issue.
12. ✅ Verifier trả contract đã cập nhật, mọi AC `PASS`.
13. ✅ QA Lead verdict PASS.
14. ✅ Code đã commit (nếu user yêu cầu).

> Có bất kỳ AC `PENDING`, `UNTESTED`, hoặc `BLOCKED` → task **chưa DONE**.
> Build/test pass không thay thế được runtime evidence cho UI/flow.

## Hệ thống Agent

### Đội ngũ agent

| Agent | File | Vai trò | Quyền |
|-------|------|---------|-------|
| Orchestrator | `.opencode/agents/orchestrator.md` | Tạo contract, điều phối web trước app, gate phase | Read-only + shell + subagent |
| Product Owner | `.opencode/agents/team/product-owner.md` | User story, scope, acceptance criteria, DoD | Read-only |
| Planner | `.opencode/agents/team/planner.md` | Phân tích code, lập kế hoạch triển khai | Read-only |
| Developer (generic) | `.opencode/agents/team/developer.md` | Core/backend, không phải UI dual-target | Edit + Shell |
| Tester | `.opencode/agents/team/tester.md` | Chạy test, tìm lỗi, edge case | Shell only (no edit) |
| Reviewer | `.opencode/agents/team/reviewer.md` | Review diff, kiến trúc, bảo mật, hiệu năng | Read-only |
| Verifier (generic) | `.opencode/agents/verifier.md` | Chấm contract cho task không UI | Shell only (no edit) |
| QA Lead | `.opencode/agents/team/qa-lead.md` | Tổng hợp, verdict cuối, feedback loop | Read-only |
| Web Developer | `.opencode/agents/web-developer.md` | Implement web prototype + Playwright tests | Edit + Shell |
| Web Verifier | `.opencode/agents/web-verifier.md` | Playwright runtime; đặt `WEB_VALIDATED` | Shell only (no edit) |
| App Developer | `.opencode/agents/app-developer.md` | Port WinUI sau `WEB_VALIDATED`; parity | Edit + Shell |
| App Verifier | `.opencode/agents/app-verifier.md` | Build/test + smoke + parity; đặt `PARITY_VERIFIED` | Shell only (no edit) |

Các agent web/app là `mode: subagent`; Orchestrator là `mode: primary`. Tất cả có
`temperature` riêng và khai báo quyền bằng `permissions` array của OpenCode V2.
Agent ID: `orchestrator`, `web-developer`, `web-verifier`, `app-developer`,
`app-verifier`, `team/product-owner`, `team/planner`, `team/developer`, `team/tester`,
`team/reviewer`, `team/qa-lead`, `verifier`.

### Quy tắc phân quyền

- **Không agent nào được vừa code vừa tự nghiệm thu.**
- Orchestrator: `edit: deny`, `shell: allow`, `subagent: allow`.
- Product Owner, Planner, Reviewer, QA Lead: `edit: deny`, `shell: deny`.
- Tester, Web Verifier, App Verifier: `edit: deny`, `shell: allow`.
- Generic Developer, Web Developer, App Developer: `edit: allow`, `shell: allow`.
- Verifier: `edit: deny`, `shell: allow`.
- Developer không được tự verdict PASS hoặc tự đặt phase cuối.

## Quy trình làm task

### Feature UI (dual-target)

```
User
  │
  ▼
Orchestrator tạo Verification Contract + feature doc
  │  status: WEB_SPEC
  ▼
Web Developer (web prototype + Playwright)
  │
  ▼
Web Verifier (Playwright runtime, user-visible)
  │  ├─ PASS hết web AC → WEB_VALIDATED
  │  └─ NOT_CONFIGURED/FAIL → BLOCKED, dừng
  ▼
App Developer (chỉ sau WEB_VALIDATED)
  │
  ▼
App Verifier (build/test + runtime smoke + parity)
  │  ├─ build/test pass → APP_IMPLEMENTED
  │  └─ parity + runtime pass → PARITY_VERIFIED
  ▼
Tester → Reviewer → QA Lead
  │
  ├─ mọi AC PASS + PARITY_VERIFIED → DONE
  └─ FAIL/BLOCKED/PENDING → feedback (tối đa 3 vòng) hoặc báo user
```

### Task core/backend (không UI)

Dùng workflow generic: Orchestrator → Product Owner → Planner → Developer → Tester →
Reviewer → Verifier → QA Lead. Vẫn phải có Verification Contract và evidence đúng loại.

Nếu có AC `RUNTIME` mà Verifier không chạy được → verdict cuối là **BLOCKED**,
không được coi là DONE.

## Cách gọi từng agent

Dùng subagent trong OpenCode. Agent ID đầy đủ là `team/<tên>` với agent trong thư mục
`team/`; các agent web/app/orchestrator/verifier nằm trực tiếp trong `.opencode/agents/`.

```
# Orchestrator
Chọn agent `orchestrator`, rồi yêu cầu:
Tạo Verification Contract + feature doc cho: [mô tả].
Contract ghi phase WEB_SPEC và evidence type từng AC.

# Web Developer
@web-developer Implement web prototype theo contract:
[Dán contract + feature doc + design tokens]
Cập nhật Developer Report; không tự đặt WEB_VALIDATED.

# Web Verifier
@web-verifier Chấm contract web:
[Dán contract + Developer Report + môi trường Playwright]
No evidence, no PASS. Thiếu Playwright/runtime evidence → BLOCKED.

# App Developer
@app-developer Port WinUI theo contract đã WEB_VALIDATED:
[Dán contract + parity checklist + tokens]
Cập nhật Developer Report + .agent/parity/<feature>.parity.md.

# App Verifier
@app-verifier Chấm contract app:
[Dán contract + Developer Report + parity checklist + môi trường app]
Build/test + runtime smoke + parity. UI chưa automation → MANUAL_REQUIRED/BLOCKED.

# Generic Developer/Verifier (core/backend)
@team/developer Triển khai theo contract: [contract]
@verifier Chấm contract: [contract + Developer Report]

# QA Lead
@team/qa-lead Tổng hợp Tester + Reviewer + contract đã cập nhật.
```

## Checklist tự review (cho Developer)

Trước khi báo hoàn thành, Developer phải tự kiểm:

- [ ] Đã nhận Verification Contract và đúng phase (`WEB_SPEC`/`WEB_VALIDATED`/`APP_IMPLEMENTED`/`PARITY_VERIFIED`)?
- [ ] Web: đã thêm/cập nhật Playwright test trong `tests/web-e2e/`, assert user-visible behavior?
- [ ] App: contract đã `WEB_VALIDATED` trước khi sửa WinUI?
- [ ] App: đã cập nhật `.agent/parity/<feature>.parity.md`?
- [ ] Đã chạy `npm run check:static` (hoặc `.\scripts\check.ps1`) — pass?
- [ ] Đã chạy `npm run check:web` — pass?
- [ ] Đã chạy `dotnet build Vaz.sln` — pass?
- [ ] Đã chạy `dotnet test Vaz.sln` — tất cả pass?
- [ ] AC `RUNTIME`: đã chạy `npm run check:web` (hoặc `npx playwright test`) và có output thật? Nếu runner chưa cấu hình, đã ghi `NOT_CONFIGURED`/`BLOCKED`?
- [ ] AC `MANUAL_REQUIRED`: đã ghi bước user cần làm, không tự PASS?
- [ ] Không tự đánh giá UI/runtime/parity PASS?
- [ ] Developer Report đã có trạng thái và bằng chứng cho từng AC?
- [ ] Code theo đúng pattern hiện có, dùng `docs/design/tokens.json`?
- [ ] Không hardcode secret/token?
- [ ] Error handling đầy đủ?
- [ ] Test cover phần thay đổi?
- [ ] Không sửa file ngoài scope?

## Quy tắc chống báo hoàn thành ảo

1. **Không báo PASS nếu chưa chạy lệnh thực tế.** Phải có output thật.
2. **Không bỏ qua warning.** Warning phải giải thích hoặc sửa.
3. **Không tự nghiệm thu.** Developer KHÔNG BAO GIỜ tự đánh giá PASS/FAIL.
4. **Không đoán kết quả test.** Chạy thật, dán output thật.
5. **Nếu bị BLOCKED**, ghi rõ lý do và KHÔNG báo hoàn thành.
6. **Feedback loop tối đa 3 vòng.** Sau 3 vòng FAIL → dừng, báo user.
7. **Mọi agent phải dùng format output đã định nghĩa.** Không trả lời tự do.
8. **Không dùng code review, build pass, hoặc unit test pass làm bằng chứng cho AC `RUNTIME`.**
9. **Không đổi `BLOCKED`/`PENDING` thành `PASS`** chỉ vì các AC khác đạt hoặc "code trông đúng".
10. **Không coi `npm run check:static` là đã kiểm thử UI/flow; `check:web` chỉ có giá trị khi Playwright thật sự chạy và pass.**
11. **Không nghiệm thu khi contract còn AC `PENDING`/`UNTESTED`.**
12. **No evidence, no PASS.** Không có bằng chứng đúng loại thì không có PASS.
13. **Không port WinUI trước `WEB_VALIDATED`.**
14. **Không đặt `WEB_VALIDATED`/`PARITY_VERIFIED` nếu thiếu runtime evidence.**

## Format báo cáo bắt buộc

Mỗi agent có format output riêng trong file `.md` của mình. Tóm tắt:

| Agent | Output chính |
|-------|-------------|
| Orchestrator | Verification Contract, phase gate, điều phối, verdict tổng hợp |
| Product Owner | User Story, Scope, Acceptance Criteria, DoD |
| Planner | File ảnh hưởng, Kế hoạch triển khai, Rủi ro |
| Web Developer | Developer Report, web files, Playwright tests, runtime còn thiếu |
| Web Verifier | Contract đã cập nhật, Playwright evidence, `WEB_VALIDATED`/`BLOCKED` |
| App Developer | Developer Report, WinUI files, parity checklist, runtime còn thiếu |
| App Verifier | Contract đã cập nhật, build/test/smoke evidence, parity verdict |
| Developer (generic) | Developer Report, Tệp thay đổi, Kiểm tra đã chạy, Runtime còn thiếu |
| Tester | Test plan, Kết quả chạy, Lỗi phát hiện, Verdict |
| Reviewer | Vấn đề phát hiện, Mức độ, Verdict |
| Verifier (generic) | Verification Contract đã cập nhật, Đã chạy, Lỗi/hồi quy |
| QA Lead | Tổng hợp, Verdict cuối, Feedback (nếu FAIL) |

## Agent cũ (tương thích ngược)

Các agent gốc vẫn hoạt động tại `.opencode/agents/`:
- `idea.md` — tương đương Product Owner + Planner gộp.
- `builder.md` — tương đương Developer.

`verifier.md` là Verifier độc lập cho task generic, bắt buộc dùng trong quy trình mới
và không được PASS khi thiếu runtime evidence.

Dùng hệ thống dual-target (`orchestrator` + `web-*` + `app-*`) cho feature UI.
Dùng `team/` + `verifier` cho task core/backend. Dùng `idea`/`builder` cho task nhỏ,
nhanh — nhưng vẫn phải theo "Quy tắc kiểm thử bắt buộc" ở trên.
