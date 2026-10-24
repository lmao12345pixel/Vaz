# Parity records

Parity là so sánh **hành vi người dùng nhìn thấy** giữa web đã validated và app WinUI.

## Template

```markdown
# Parity: <feature>

- Contract: docs/contracts/<feature>.contract.md
- Web phase evidence: <link/output>
- App phase evidence: <link/output>
- Verifier: app-verifier

| Contract AC | Web evidence | App evidence | Status |
|-------------|--------------|--------------|--------|
| ... | ... | ... | PASS/FAIL/BLOCKED |

## UI smoke

1. Launch app:
2. Main screen visible:
3. No startup exception:
4. Flow observed:
5. Manual confirmation (user):

## Verdict

PARITY_VERIFIED / BLOCKED / FAIL
```

## Quy tắc

- Không có parity nếu app chưa `APP_IMPLEMENTED`.
- Không dùng ảnh web làm bằng chứng app.
- UI chưa automation được → `MANUAL_REQUIRED`; chưa có user xác nhận → `BLOCKED`.
- Khác label, thứ tự, trạng thái rỗng, thông báo lỗi hoặc persistence là parity fail.
- Không sửa web để che lệch app; sửa app theo contract hoặc quay lại phase web.
