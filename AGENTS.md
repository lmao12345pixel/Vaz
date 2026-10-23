# V.A.Z — Agent System

## Bối cảnh

V.A.Z là Minecraft client/launcher lấy cảm hứng từ Luna Client, Valiant Client, Prism Launcher.
Hiện tại gồm:
- **Vazer.Core** (.NET 8, C#): domain logic — instances, mods, auth, Java resolver, loaders.
- **Vaz.WinUI** (WinUI 3): GUI desktop app.
- **Web prototype** (HTML/CSS/JS): demo UI tại `docs/design-preview/`.
- **Test suite** (xUnit): 119+ tests tại `tests/Vazer.Core.Tests/`.

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
# Build toàn bộ solution
dotnet build Vaz.sln

# Chạy unit test
dotnet test Vaz.sln

# Build chỉ Core (nhanh hơn nếu chỉ sửa logic)
dotnet build src/Vazer.Core/Vazer.Core.csproj

# Test chỉ Core
dotnet test tests/Vazer.Core.Tests/Vazer.Core.Tests.csproj
```

> **Không có lệnh lint/typecheck riêng** — C# compiler đã kiểm tra type. Build pass = typecheck pass.

## Định nghĩa DONE bắt buộc

Một task chỉ được coi là DONE khi:

1. ✅ `dotnet build Vaz.sln` — pass, 0 error.
2. ✅ `dotnet test Vaz.sln` — tất cả test pass, không skip không giải thích.
3. ✅ Không regression: chức năng cũ vẫn hoạt động.
4. ✅ Acceptance criteria từ Product Owner — tất cả đạt.
5. ✅ Reviewer APPROVE — không còn Critical/Major issue.
6. ✅ QA Lead verdict PASS.
7. ✅ Code đã commit (nếu user yêu cầu).

## Hệ thống Agent

### Đội ngũ 6 agent

| Agent | File | Vai trò | Quyền |
|-------|------|---------|-------|
| Product Owner | `.opencode/agents/team/product-owner.md` | User story, scope, acceptance criteria, DoD | Read-only |
| Planner | `.opencode/agents/team/planner.md` | Phân tích code, lập kế hoạch triển khai | Read-only |
| Developer | `.opencode/agents/team/developer.md` | Implement code, chạy build/test | Edit + Shell |
| Tester | `.opencode/agents/team/tester.md` | Chạy test, tìm lỗi, edge case | Shell only (no edit) |
| Reviewer | `.opencode/agents/team/reviewer.md` | Review diff, kiến trúc, bảo mật, hiệu năng | Read-only |
| QA Lead | `.opencode/agents/team/qa-lead.md` | Tổng hợp, verdict cuối, feedback loop | Read-only |

### Quy tắc phân quyền

- **Không agent nào được vừa code vừa tự nghiệm thu.**
- Product Owner, Planner, Reviewer, QA Lead: `edit: deny`, `shell: deny`.
- Tester: `edit: deny`, `shell: allow` (chỉ chạy lệnh kiểm tra).
- Developer: `edit: allow`, `shell: allow`.
- Tất cả agent: `subagent: deny` (orchestration do user hoặc main agent điều phối).

## Quy trình làm task

```
User yêu cầu
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
│   Developer      │  → Code + Test + Build pass
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
│    QA Lead       │  → Verdict: PASS / FAIL / BLOCKED
└────────┬────────┘
         │
    ┌────┴────┐
    │  PASS?  │
    └────┬────┘
     Yes │    No (FAIL)
         │      │
         ▼      ▼
      DONE    Feedback → Developer → Tester → Reviewer → QA Lead
                         (tối đa 3 vòng, sau đó báo user)
```

## Cách gọi từng agent

Dùng subagent trong Antigravity/OpenCode:

```
# Product Owner — phân tích yêu cầu
@product-owner Phân tích yêu cầu: [mô tả ngắn]. Trả về user story, scope, acceptance criteria.

# Planner — lập kế hoạch
@planner Dựa trên user story sau, lập kế hoạch triển khai:
[Dán user story + acceptance criteria từ Product Owner]

# Developer — triển khai
@developer Triển khai theo kế hoạch sau:
[Dán kế hoạch từ Planner + acceptance criteria]

# Tester — kiểm thử
@tester Kiểm tra thay đổi sau:
[Dán danh sách file thay đổi + acceptance criteria]
Chạy: dotnet build Vaz.sln && dotnet test Vaz.sln

# Reviewer — review code
@reviewer Review diff sau:
[Dán danh sách file thay đổi + acceptance criteria]

# QA Lead — verdict
@qa-lead Tổng hợp kết quả:
Tester: [Dán báo cáo Tester]
Reviewer: [Dán báo cáo Reviewer]
Acceptance criteria: [Dán từ Product Owner]
```

## Checklist tự review (cho Developer)

Trước khi báo hoàn thành, Developer phải tự kiểm:

- [ ] Đã chạy `dotnet build Vaz.sln` — pass?
- [ ] Đã chạy `dotnet test Vaz.sln` — tất cả pass?
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

## Format báo cáo bắt buộc

Mỗi agent có format output riêng trong file `.md` của mình. Tóm tắt:

| Agent | Output chính |
|-------|-------------|
| Product Owner | User Story, Scope, Acceptance Criteria, DoD |
| Planner | File ảnh hưởng, Kế hoạch triển khai, Rủi ro |
| Developer | Đã làm, File thay đổi, Kết quả kiểm tra |
| Tester | Test plan, Kết quả chạy, Lỗi phát hiện, Verdict |
| Reviewer | Vấn đề phát hiện, Mức độ, Verdict |
| QA Lead | Tổng hợp, Verdict cuối, Feedback (nếu FAIL) |

## Agent cũ (tương thích ngược)

Các agent gốc vẫn hoạt động tại `.opencode/agents/`:
- `idea.md` — tương đương Product Owner + Planner gộp.
- `builder.md` — tương đương Developer.
- `verifier.md` — tương đương Tester.

Dùng hệ thống mới (`team/`) cho quy trình đầy đủ.
Dùng hệ thống cũ cho task nhỏ, nhanh.
