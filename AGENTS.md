# V.A.Z — Agent System

## Bối cảnh

V.A.Z là Minecraft client/launcher lấy cảm hứng từ Luna Client, Valiant Client, Prism Launcher.
Hiện tại gồm:
- **Vazer.Core** (.NET 8, C#): domain logic — instances, mods, auth, Java resolver, loaders.
- **Vaz.WinUI** (WinUI 3): GUI desktop app.
- **Web prototype** (HTML/CSS/JS): demo UI tại `docs/design-preview/`.
- **Test suite** (xUnit): 181 tests tại `tests/Vazer.Core.Tests/` (đã chạy và xác nhận).

## Stack thực tế

| Thành phần | Công nghệ |
|------------|-----------|
| Language | C# (.NET 8) |
| UI Framework | WinUI 3 (Windows App SDK) |
| Architecture | MVVM (CommunityToolkit.Mvvm) |
| DI | Microsoft.Extensions.DependencyInjection |
| Test | xUnit 2.9.3, FluentAssertions 8.11.0, Moq 4.21.0, coverlet |
| Web demo | HTML/CSS/JS thuần (không framework) |
| Build | dotnet CLI / MSBuild |
| VCS | Git |

## Lệnh kiểm tra thực tế

```bash
# Static: build + test + validate agent config
npm run check:static
# hoặc không cần npm:
.\scripts\check.ps1
.\check.cmd

# Runtime web: Playwright (NOT_CONFIGURED nếu chưa cài runner)
npm run check:runtime

# Static + runtime
npm run check:all

# Build toàn bộ solution
dotnet build Vaz.sln

# Chạy unit test
dotnet test Vaz.sln

# Build chỉ Core (nhanh hơn nếu chỉ sửa logic)
dotnet build src/Vazer.Core/Vazer.Core.csproj

# Test chỉ Core
dotnet test tests/Vazer.Core.Tests/Vazer.Core.Tests.csproj

# Kiểm tra frontmatter/quyền/testing-rule của 8 agent
.\scripts\validate-agents.ps1
```

> **Không có lệnh lint/typecheck riêng** — C# compiler đã kiểm tra type. Build pass = typecheck pass.
> `npm run check` là alias cũ của static check; nó **không** chứng minh UI/flow hoạt động.
> `package.json` là script runner. Static check không cần npm install; runtime Playwright cần
> `npm install` + `npx playwright install chromium`.

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
10. `npm run check:static` không thay thế `npm run check:runtime` cho AC UI/flow.
11. Không gọi Developer khi chưa có Verification Contract.
12. Không nghiệm thu khi còn AC `PENDING`/`UNTESTED`.
13. Không đổi `BLOCKED`/`PENDING` thành `PASS` vì các AC khác đạt.
14. Mọi task phải có Developer Report và contract đã được Verifier cập nhật.

## Định nghĩa DONE bắt buộc

Một task chỉ được coi là DONE khi:

1. ✅ Có Verification Contract cho mọi acceptance criterion.
2. ✅ Mọi AC có trạng thái cuối `PASS`; không còn `PENDING`/`UNTESTED`/`BLOCKED`.
3. ✅ `npm run check:static` (hoặc `.\scripts\check.ps1`) — pass toàn bộ.
4. ✅ `dotnet build Vaz.sln` — pass, 0 error; `dotnet test Vaz.sln` — tất cả pass, không skip.
5. ✅ Mọi AC `AUTOMATED` có command + output + tên test thật.
6. ✅ Mọi AC `RUNTIME` có runtime evidence thật (web Playwright/app chạy thật).
7. ✅ Mọi AC `MANUAL_REQUIRED` có xác nhận của user.
8. ✅ Không regression: chức năng cũ vẫn hoạt động.
9. ✅ Reviewer APPROVE — không còn Critical/Major issue.
10. ✅ Verifier trả contract đã cập nhật, mọi AC `PASS`.
11. ✅ QA Lead verdict PASS.
12. ✅ Code đã commit (nếu user yêu cầu).

> Có bất kỳ AC `PENDING`, `UNTESTED`, hoặc `BLOCKED` → task **chưa DONE**.
> Build/test pass không thay thế được runtime evidence cho UI/flow.

## Hệ thống Agent

### Đội ngũ agent

| Agent | File | Vai trò | Quyền |
|-------|------|---------|-------|
| Orchestrator | `.opencode/agents/orchestrator.md` | Tạo Verification Contract, điều phối, gate trước Verifier | Read-only + shell + subagent |
| Product Owner | `.opencode/agents/team/product-owner.md` | User story, scope, acceptance criteria, DoD | Read-only |
| Planner | `.opencode/agents/team/planner.md` | Phân tích code, lập kế hoạch triển khai | Read-only |
| Developer | `.opencode/agents/team/developer.md` | Implement code, cập nhật Developer Report | Edit + Shell |
| Tester | `.opencode/agents/team/tester.md` | Chạy test, tìm lỗi, edge case | Shell only (no edit) |
| Reviewer | `.opencode/agents/team/reviewer.md` | Review diff, kiến trúc, bảo mật, hiệu năng | Read-only |
| Verifier | `.opencode/agents/verifier.md` | Chấm Verification Contract; no evidence, no PASS | Shell only (no edit) |
| QA Lead | `.opencode/agents/team/qa-lead.md` | Tổng hợp, verdict cuối, feedback loop | Read-only |

6 agent team dùng `mode: subagent`; Orchestrator là `mode: primary` (điều phối workflow);
Verifier là `mode: subagent` (kiểm chứng độc lập). Tất cả có `temperature` riêng và khai báo
quyền bằng `permissions` array của OpenCode V2.
Agent ID: `orchestrator`, `team/product-owner`, `team/planner`, `team/developer`,
`team/tester`, `team/reviewer`, `team/qa-lead`, `verifier`.

### Quy tắc phân quyền

- **Không agent nào được vừa code vừa tự nghiệm thu.**
- Orchestrator: `edit: deny`, `shell: allow`, `subagent: allow`.
- Product Owner, Planner, Reviewer, QA Lead: `edit: deny`, `shell: deny`.
- Tester: `edit: deny`, `shell: allow` (chỉ chạy lệnh kiểm tra).
- Developer: `edit: allow`, `shell: allow`.
- Verifier: `edit: deny`, `shell: allow` (chạy runtime/static check, không sửa).
- Verifier và Orchestrator không được edit; Developer không được tự verdict PASS.

## Quy trình làm task

```
User yêu cầu
    │
    ▼
┌─────────────────┐
│  Orchestrator   │  → Tạo Verification Contract: AUTOMATED / RUNTIME / MANUAL_REQUIRED / BLOCKED
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│  Product Owner   │  → User story + Acceptance criteria + DoD
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Planner       │  → Kế hoạch triển khai + File ảnh hưởng
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Developer      │  → Code + Developer Report; KHÔNG tự verdict UI
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Tester        │  → Chạy test + Tìm lỗi
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│   Reviewer       │  → Review code quality
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    Verifier      │  → Chấm Verification Contract; no evidence, no PASS
└────────┬────────┘
         │
         ▼
┌─────────────────┐
│    QA Lead       │  → Verdict: PASS / FAIL / BLOCKED
└────────┬────────┘
         │
    ┌────┴────┐
    │  PASS?  │
    └────┬────┘
     Yes │    No (FAIL/BLOCKED)
         │      │
         ▼      ▼
      DONE    Feedback → Developer → Tester → Reviewer → Verifier → QA Lead
                         (tối đa 3 vòng, sau đó báo user)
```

Nếu có AC `RUNTIME` mà Verifier không chạy được → verdict cuối là **BLOCKED**,
không được coi là DONE.

## Cách gọi từng agent

Dùng subagent trong OpenCode. Agent ID đầy đủ là `team/<tên>` vì file nằm trong
thư mục con `.opencode/agents/team/`. Riêng Orchestrator và Verifier nằm trực tiếp
trong `.opencode/agents/`.

```
# Orchestrator — chọn làm primary agent (không gọi bằng @)
Chọn agent `orchestrator`, rồi yêu cầu:
Tạo Verification Contract cho yêu cầu sau: [mô tả yêu cầu].
Mỗi AC ghi evidence type AUTOMATED / RUNTIME / MANUAL_REQUIRED / BLOCKED.

# Product Owner — phân tích yêu cầu
@team/product-owner Phân tích yêu cầu: [mô tả ngắn]. Trả về user story, scope, acceptance criteria.

# Planner — lập kế hoạch
@team/planner Dựa trên user story sau, lập kế hoạch triển khai:
[Dán user story + acceptance criteria từ Product Owner]

# Developer — triển khai
@team/developer Triển khai theo kế hoạch sau:
[Dán kế hoạch từ Planner + Verification Contract]
Cập nhật Developer Report với trạng thái/bằng chứng từng AC.

# Tester — kiểm thử
@team/tester Kiểm tra thay đổi sau:
[Dán danh sách file thay đổi + Verification Contract]
Chạy: npm run check:static

# Reviewer — review code
@team/reviewer Review diff sau:
[Dán danh sách file thay đổi + Verification Contract]

# Verifier — chấm lại contract bằng bằng chứng thật
@verifier Chấm Verification Contract sau:
[Dán contract + Developer Report + môi trường runtime]
No evidence, no PASS. Thiếu runtime evidence → BLOCKED.

# QA Lead — verdict
@team/qa-lead Tổng hợp kết quả:
Tester: [Dán báo cáo Tester]
Reviewer: [Dán báo cáo Reviewer]
Verifier: [Dán contract đã cập nhật]
Acceptance criteria: [Dán từ Product Owner]
```

## Checklist tự review (cho Developer)

Trước khi báo hoàn thành, Developer phải tự kiểm:

- [ ] Đã nhận Verification Contract từ Orchestrator và cập nhật đúng evidence type (`AUTOMATED` / `RUNTIME` / `MANUAL_REQUIRED` / `BLOCKED`)?
- [ ] Đã chạy `npm run check:static` (hoặc `.\scripts\check.ps1`) — pass?
- [ ] Đã chạy `dotnet build Vaz.sln` — pass?
- [ ] Đã chạy `dotnet test Vaz.sln` — tất cả pass?
- [ ] AC `RUNTIME`: đã chạy `npm run check:runtime` nếu runner có? Nếu chưa có, đã ghi `NOT_CONFIGURED`/`BLOCKED`?
- [ ] AC `MANUAL_REQUIRED`: đã ghi bước user cần làm, không tự PASS?
- [ ] Không tự đánh giá UI/runtime PASS?
- [ ] Developer Report đã có trạng thái và bằng chứng cho từng AC?
- [ ] Code theo đúng pattern hiện có?
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
10. **Không coi `npm run check:static` là đã kiểm thử UI/flow.**
11. **Không nghiệm thu khi contract còn AC `PENDING`/`UNTESTED`.**
12. **No evidence, no PASS.** Không có bằng chứng đúng loại thì không có PASS.

## Format báo cáo bắt buộc

Mỗi agent có format output riêng trong file `.md` của mình. Tóm tắt:

| Agent | Output chính |
|-------|-------------|
| Orchestrator | Verification Contract, điều phối, verdict tổng hợp |
| Product Owner | User Story, Scope, Acceptance Criteria, DoD |
| Planner | File ảnh hưởng, Kế hoạch triển khai, Rủi ro |
| Developer | Developer Report, Tệp thay đổi, Kiểm tra đã chạy, Runtime còn thiếu |
| Tester | Test plan, Kết quả chạy, Lỗi phát hiện, Verdict |
| Reviewer | Vấn đề phát hiện, Mức độ, Verdict |
| Verifier | Verification Contract đã cập nhật, Đã chạy, Lỗi/hồi quy, Runtime chưa kiểm được |
| QA Lead | Tổng hợp, Verdict cuối, Feedback (nếu FAIL) |

## Agent cũ (tương thích ngược)

Các agent gốc vẫn hoạt động tại `.opencode/agents/`:
- `idea.md` — tương đương Product Owner + Planner gộp.
- `builder.md` — tương đương Developer.

`verifier.md` **không còn** là agent cũ — đây là Verifier độc lập, bắt buộc dùng
trong quy trình mới và không được PASS khi thiếu runtime evidence.

Dùng hệ thống mới (`orchestrator` + `team/` + `verifier`) cho quy trình đầy đủ.
Dùng `idea`/`builder` cho task nhỏ, nhanh — nhưng vẫn phải theo
"Quy tắc kiểm thử bắt buộc" ở trên.
