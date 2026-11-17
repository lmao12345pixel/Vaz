# Quy trình 3 agent: Idea → Builder → Verifier

Ba agent project-local nằm trong `.opencode/agents/`:

| Agent | Vai trò | Quyền |
| --- | --- | --- |
| `idea` | Phân tích yêu cầu, đề xuất phương án, tiêu chí nghiệm thu, rủi ro | Chỉ đọc; không sửa file, không chạy shell |
| `builder` | Triển khai phương án đã duyệt, viết test, chạy kiểm tra | Được sửa file và chạy lệnh |
| `verifier` | Nghiệm thu độc lập, chạy test/E2E, báo PASS/FAIL kèm bằng chứng | Được chạy lệnh; không sửa source |

Cả ba dùng `mode: all`, nên có thể chọn làm agent chính hoặc giao việc như subagent (tùy giao diện OpenCode đang dùng).

## Quy trình chuẩn

### 1. Idea — làm rõ trước khi code

Giao cho `idea` một brief ngắn:

```text
Đọc repo và đề xuất cách cải thiện <màn hình/tính năng>.
Mục tiêu: <kết quả người dùng thấy được>.
Ràng buộc: <không đổi API / giữ WinUI / chỉ web prototype / ...>.
Trả về phương án, đề xuất, tiêu chí nghiệm thu và rủi ro.
```

Chỉ chuyển sang bước 2 khi bạn đã duyệt rõ:
- phương án được chọn,
- phạm vi và **non-goals**,
- tiêu chí nghiệm thu.

### 2. Builder — triển khai đúng phạm vi

Giao cho `builder` proposal đã duyệt, kèm tiêu chí nghiệm thu. Yêu cầu agent:
- đọc code hiện có trước khi sửa,
- cập nhật test,
- chạy check/build liên quan,
- không commit/push nếu bạn chưa yêu cầu.

Nếu proposal không khả thi, `builder` phải dừng và báo lý do thay vì tự đổi hướng.

### 3. Verifier — nghiệm thu độc lập

Giao cho `verifier`:
- yêu cầu gốc + tiêu chí nghiệm thu,
- commit/branch hoặc phạm vi thay đổi cần kiểm,
- các lệnh test/E2E nếu có.

`verifier` trả về **PASS / FAIL / BLOCKED**, bảng tiêu chí + bằng chứng, lỗi/hồi quy và mức sửa tối thiểu. Agent này không được tự sửa code.

Nếu **FAIL**, chuyển danh sách lỗi lại cho `builder`, sau đó chạy lại `verifier`.

## Mẫu handoff

```text
Proposal: <tóm tắt phương án đã duyệt>
Phạm vi: <các file/khu vực>
Non-goals: <những thứ không làm>
Tiêu chí nghiệm thu:
1. ...
2. ...
Bài kiểm tra bắt buộc: <commands>
```

## Nguyên tắc

- Không bỏ qua bước `idea` với thay đổi có nhiều phương án hoặc ảnh hưởng kiến trúc.
- `builder` không tự mở rộng scope; muốn đổi scope thì quay lại `idea`.
- `verifier` không sửa source, kể cả lỗi nhỏ; chỉ báo cáo.
- Không commit/push trừ khi người dùng yêu cầu rõ.
- Với UI, bằng chứng nên gồm test chạy thật + ảnh chụp ở kích thước màn hình đã thống nhất.
