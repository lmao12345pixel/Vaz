# V.A.Z - Agent Workflow

## Tổng quan

Workflow multi-agent của V.A.Z dùng **Verification Contract** làm trung tâm. Mọi task
phải có contract trước khi Developer bắt đầu; Verifier chỉ nghiệm thu khi mọi
acceptance criterion có bằng chứng đúng loại.

## Luồng chính

```
User
  │
  ▼
Orchestrator tạo Verification Contract
  │
  ▼
Product Owner → Planner
  │
  ▼
Developer (Developer Report)
  │
  ▼
Tester → Reviewer
  │
  ▼
Verifier (cập nhật contract, no evidence no PASS)
  │
  ▼
QA Lead
  │
  ├─ mọi AC PASS → DONE
  └─ FAIL/BLOCKED/PENDING → feedback (tối đa 3 vòng) hoặc báo user
```

## Verification Contract

| # | Acceptance criterion | Evidence type | Verification method | Required evidence | Status |
|---|----------------------|---------------|---------------------|-------------------|--------|
| 1 | ... | AUTOMATED / RUNTIME / MANUAL_REQUIRED / BLOCKED | ... | ... | PENDING / PASS / FAIL / BLOCKED |

### Evidence type

| Loại | Dùng cho | Bằng chứng hợp lệ |
|------|----------|-------------------|
| `AUTOMATED` | build, typecheck, unit/integration test, static analysis | command + exit code + output + tên test |
| `RUNTIME` | UI, flow, navigation, play/stop, settings persistence, empty/error state, app launch | chạy app/browser + kết quả nhìn thấy, screenshot, log, URL |
| `MANUAL_REQUIRED` | không tự động hóa được | user xác nhận + bước làm + kết quả |
| `BLOCKED` | chưa có runner/môi trường | lý do + runner/môi trường còn thiếu |

### Quy tắc

- Mỗi AC có đúng một evidence type.
- AC UI/flow phải là `RUNTIME`.
- Build pass, unit test pass, code review không phải bằng chứng `RUNTIME`.
- AC `BLOCKED` không bao giờ PASS.
- Không DONE khi còn `PENDING`/`UNTESTED`.
- `check:runtime` trả `NOT_CONFIGURED` khi chưa cài runner → ghi `BLOCKED`.

## Chi tiết từng bước

### Bước 0: Orchestrator

Tạo Verification Contract từ acceptance criteria. Không gọi Developer khi chưa có
contract. Sau Developer, gọi Verifier kèm contract + Developer Report + diff.

### Bước 1: Product Owner

User story, scope, non-goals, acceptance criteria, DoD. Hỏi lại user nếu mơ hồ.

### Bước 2: Planner

File ảnh hưởng, kế hoạch từng bước, rủi ro. Ưu tiên thay đổi nhỏ, dễ rollback.

### Bước 3: Developer

Implement theo contract; chạy `npm run check:static`; cập nhật Developer Report.
Không tự đánh giá UI/runtime PASS; AC `RUNTIME` để `PENDING`/`BLOCKED` cho Verifier.

### Bước 4: Tester

Chạy test, tìm edge case/regression. Không sửa code.

### Bước 5: Reviewer

Review diff, kiến trúc, bảo mật, hiệu năng. Verdict APPROVE / REQUEST CHANGES.

### Bước 6: Verifier

Chấm từng AC trong contract. "No evidence, no PASS". AC `RUNTIME` phải có runtime
evidence thật; AC `MANUAL_REQUIRED` phải có user xác nhận. Không chạy được →
`BLOCKED`. Output là contract đã cập nhật.

### Bước 7: QA Lead

Tổng hợp Tester + Reviewer + contract từ Verifier. PASS chỉ khi mọi AC PASS.
Nếu FAIL, feedback cụ thể cho Developer; tối đa 3 vòng.

## Feedback Loop

1. QA Lead FAIL → feedback file/dòng/lỗi/cách sửa.
2. Developer chỉ sửa trong feedback.
3. Tester + Reviewer kiểm lại.
4. Verifier chấm lại contract bằng bằng chứng mới.
5. QA Lead verdict lại. Sau 3 vòng vẫn FAIL → dừng, báo user.

## Handoff Template

```
## Handoff: [Agent nguồn] → [Agent đích]

### Context
- Task: [mô tả ngắn]
- Vòng: [1/2/3]

### Verification Contract
| # | AC | Evidence type | Verification method | Required evidence | Status |
|---|----|---------------|---------------------|-------------------|--------|

### Input
[Dán output của agent trước]

### Lệnh kiểm tra
- npm run check:static   # build + test + validate agent
- npm run check:runtime  # web Playwright; NOT_CONFIGURED nếu chưa cài
- npm run check:all      # static + runtime

### Runtime environment
- Web runner: Playwright installed? yes/no
- WinUI runner: none (manual smoke only)
```

## Workflow đầy đủ vs. rút gọn

| Tình huống | Workflow |
|------------|----------|
| Feature mới, thay đổi kiến trúc | Đầy đủ: Orchestrator + 6 agent + Verifier |
| Bug fix nhỏ, rõ ràng | Developer → Tester → Verifier → QA Lead |
| Chỉ docs/config | Developer → Reviewer |
| Web prototype HTML/CSS/JS | Developer → Tester → Verifier (Playwright) |
| WinUI UI/flow | Developer → Tester → Verifier (manual smoke; BLOCKED nếu không chạy được app) |

## Escalation

- Developer bị BLOCKED: báo Planner/Orchestrator để đánh giá lại.
- Tester không chạy được test: báo QA Lead với verdict BLOCKED.
- Verifier không có runtime runner: báo user, nêu cách cài Playwright/Appium; không PASS.
- QA Lead FAIL 3 vòng: dừng, báo user danh sách vấn đề còn lại.
