# Task ledger

Mỗi task UI/flow dùng một file riêng: `.agent/tasks/<feature>.task.md`.

## Template

```markdown
# Task: <feature>

- Contract: docs/contracts/<feature>.contract.md
- Phase: WEB_SPEC | WEB_VALIDATED | APP_IMPLEMENTED | PARITY_VERIFIED
- Owner: web-developer | web-verifier | app-developer | app-verifier
- Started: YYYY-MM-DD

## Verification Contract

| # | Acceptance criterion | Evidence type | Method | Required evidence | Status |
|---|----------------------|---------------|--------|-------------------|--------|
| 1 | ... | AUTOMATED/RUNTIME/MANUAL_REQUIRED/BLOCKED | ... | ... | PENDING/PASS/FAIL/BLOCKED |

## Evidence

- Web runtime:
- App build/test:
- App runtime smoke:
- Manual confirmation:

## Blockers

- ...
```

## Quy tắc

- Chỉ Orchestrator tạo/sửa contract; chỉ Verifier cập nhật trạng thái cuối.
- Developer Report không thay thế contract.
- Không đổi phase nếu còn AC `PENDING`/`UNTESTED` hoặc `BLOCKED`.
- Web chưa `WEB_VALIDATED` thì không mở task app.
