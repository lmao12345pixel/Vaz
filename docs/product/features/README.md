# Feature specs

Mỗi feature có một spec ngắn và một behavior contract dùng chung cho web và WinUI.

## Cấu trúc

```
docs/product/features/<feature>.feature.md   # user story, scope, out-of-scope
docs/contracts/<feature>.contract.md         # behavior contract + Verification Contract
.agent/tasks/<feature>.task.md               # task ledger (phase, owner, evidence)
.agent/parity/<feature>.parity.md            # parity checklist web -> app
```

## Quy tắc

- Web prototype là **executable specification**; behavior phải được chốt trên web trước.
- Không bắt đầu WinUI khi contract chưa đạt `WEB_VALIDATED`.
- Contract là nguồn sự thật duy nhất; web và app không được tự đặt behavior riêng.
- Mọi thay đổi behavior phải cập nhật contract, spec và test tương ứng.

## Contracts hiện có

| Feature | Contract | Web surface | App surface |
|---------|----------|-------------|-------------|
| Settings | `docs/contracts/settings.contract.md` | `docs/design-preview/index.html#page-settings` | `src/Vaz.WinUI` (Settings screen) |
| Library | `docs/contracts/library.contract.md` | `docs/design-preview/index.html#page-instances` | `src/Vaz.WinUI` (Instances screen) |

## Trạng thái

`WEB_SPEC` → `WEB_VALIDATED` → `APP_IMPLEMENTED` → `PARITY_VERIFIED`

Không dùng trạng thái khác. Nếu thiếu bằng chứng runtime, giữ nguyên trạng thái trước
và ghi `BLOCKED`; không được nhảy trạng thái.
