# V.A.Z — Agent Workflow

## Tổng quan

Hệ thống multi-agent cho V.A.Z gồm 6 vai trò, chạy tuần tự với feedback loop tự động.
Mục tiêu: tránh báo hoàn thành ảo, đảm bảo chất lượng qua kiểm tra chéo.

## Luồng chính

```
User → Product Owner → Planner → Developer → Tester → Reviewer → QA Lead
                                                                      │
                                                              ┌───────┤
                                                              │ PASS  │ FAIL
                                                              ▼       ▼
                                                            DONE    Feedback
                                                                      │
                                                              Developer ← ┘
                                                                      │
                                                              Tester → Reviewer → QA Lead
                                                              (lặp tối đa 3 vòng)
```

## Chi tiết từng bước

### Bước 1: Product Owner

**Input**: Yêu cầu thô từ user.
**Output**: User story, phạm vi, non-goals, acceptance criteria, Definition of Done.
**Quy tắc**: Read-only. Hỏi lại user nếu yêu cầu mơ hồ.

### Bước 2: Planner

**Input**: User story + acceptance criteria từ Product Owner.
**Output**: Danh sách file ảnh hưởng, kế hoạch triển khai theo bước, rủi ro.
**Quy tắc**: Read-only. Ưu tiên thay đổi nhỏ, an toàn, dễ rollback.

### Bước 3: Developer

**Input**: Kế hoạch từ Planner + acceptance criteria.
**Output**: Code thay đổi, test mới/cập nhật, kết quả build/test.
**Quy tắc**: Được edit/shell. Phải chạy `npm run check` (hoặc `.\scripts\check.ps1`). Không tự nghiệm thu.

### Bước 4: Tester

**Input**: Danh sách file thay đổi + acceptance criteria.
**Output**: Test plan, kết quả chạy, lỗi phát hiện, verdict PASS/FAIL.
**Quy tắc**: Được shell (chạy test). KHÔNG được edit. Tìm edge case, regression.

### Bước 5: Reviewer

**Input**: Diff + acceptance criteria.
**Output**: Vấn đề code quality/architecture/security, verdict APPROVE/REQUEST CHANGES.
**Quy tắc**: Read-only. Phân loại: Critical / Major / Minor / Nit.

### Bước 6: QA Lead

**Input**: Báo cáo Tester + Reviewer + acceptance criteria.
**Output**: Verdict cuối PASS/FAIL/BLOCKED + feedback cụ thể nếu FAIL.
**Quy tắc**: Read-only. Tối đa 3 vòng sửa. Sau 3 vòng → báo user.

## Feedback Loop

```
Vòng 1: QA Lead FAIL → feedback → Developer sửa → Tester kiểm tra → Reviewer xem → QA Lead
Vòng 2: QA Lead FAIL → feedback → Developer sửa → Tester kiểm tra → Reviewer xem → QA Lead
Vòng 3: QA Lead FAIL → DỪNG → Báo user danh sách vấn đề còn lại
```

### Quy tắc feedback

1. Feedback phải cụ thể: file, dòng (nếu có), mô tả lỗi, cách sửa đề xuất.
2. Developer chỉ sửa những gì trong feedback — không mở rộng scope.
3. Tester chỉ kiểm tra lại phần sửa + regression — không thiết kế test mới ngoài scope.
4. Mỗi vòng phải có verdict rõ ràng từ QA Lead.

## Handoff Template

Khi chuyển việc giữa các agent, dùng template sau:

```
## Handoff: [Agent nguồn] → [Agent đích]

### Context
- Task: [mô tả ngắn]
- Vòng: [1/2/3]

### Input
[Dán output của agent trước]

### Acceptance Criteria
1. [Tiêu chí 1]
2. [Tiêu chí 2]

### Lệnh kiểm tra
- npm run check (gộp build + test + validate agent)
- dotnet build Vaz.sln
- dotnet test Vaz.sln
```

## Khi nào dùng workflow đầy đủ vs. rút gọn

| Tình huống | Workflow |
|------------|----------|
| Thay đổi kiến trúc, feature mới | Đầy đủ 6 agent |
| Bug fix nhỏ, rõ ràng | Developer → Tester → QA Lead |
| Thay đổi chỉ docs/config | Developer → Reviewer |
| Thay đổi web prototype HTML/CSS | Developer → Tester (visual check) |

## Escalation

- Nếu Developer bị BLOCKED: báo Planner để đánh giá lại kế hoạch.
- Nếu Tester không thể chạy test: báo QA Lead với verdict BLOCKED.
- Nếu QA Lead FAIL 3 vòng: dừng toàn bộ, báo user.
