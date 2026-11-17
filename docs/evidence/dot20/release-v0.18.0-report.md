# Đợt 20 — Release V.A.Z v0.18.0 (Multi-Version)

**Ngày:** 2026-11-06 · **Tag:** `v0.18.0` (annotated, local) · **Trạng thái:** XONG.

## 1. Tổng quan

Đóng gói dòng **Multi-Version** thành release `v0.18.0`: đồng bộ version identity,
viết lịch sử thay đổi cho toàn bộ Đợt 18 (A–E, 18.4 Forge, 18.7 Java), cập nhật
README với feature list + ảnh evidence, tạo annotated tag và báo cáo tổng kết.

**Không làm** đúng yêu cầu: không refactor, không thêm feature, không sửa behavior đã PASS.

## 2. Trạng thái từng việc

| Việc | Nội dung | Kết quả | Commit |
|:---:|---|:---:|---|
| 1 | Version → 0.18.0 | ✅ | `7a6f9c1` |
| 2 | `CHANGELOG.md` | ✅ | `1529bd7` |
| 3 | `README.md` (features + screenshots) | ✅ | `f78ac8b` |
| 4 | Annotated tag `v0.18.0` | ✅ *(local, chưa push)* | — |
| 5 | GitHub Release workflow | ⏭️ **SKIP** — không có workflow | — |
| 6 | Report tổng kết | ✅ | *(commit này)* |

## 3. Version identity & artifacts

Repo là **unpackaged WinUI** (`WindowsPackageType=None`) nên không có
`Package.appxmanifest`. Version được đồng bộ ở 4 nơi tương đương:

| File | Thay đổi |
|---|---|
| `src/Vaz.WinUI/app.manifest` | `assemblyIdentity version` `1.0.0.0` → **`0.18.0.0`** |
| `src/Vaz.WinUI/Vaz.WinUI.csproj` | Thêm `<Version>0.18.0</Version>`, `<AssemblyVersion>`, `<FileVersion>` |
| `src/Vaz.WinUI/ViewModels/SettingsViewModel.cs` | `VersionLabel`, 2 chuỗi "kiểm tra cập nhật", `version` trong diagnostic JSON → `0.18.0` |
| `README.md` / `CHANGELOG.md` | Header + release history |

`grep "2.4.0" src/` → **0 hit**. Version cũ 2.4.0 là số nội bộ của Settings,
không phải package version; đã thay để tránh hiển thị sai sau release.

**Docs mới:**
- `CHANGELOG.md` — Added/Changed/Fixed/Verification/Known issues cho 18.1→19.1,
  kèm link tới 8 báo cáo evidence.
- `README.md` — release header, 6 highlight, gallery 4 ảnh
  (`wizard-step1-dark.png`, `wizard-step2-5cards-forge.png`,
  `wizard-step3-confirm-card.png`, `settings-versions-dark.png`), layout cập nhật
  (`src/Vaz.WinUI`, `tests/web-e2e`, `CHANGELOG`, `Directory.Build.props`),
  số test 119 → 463, Java 8–21 → 8–25.

## 4. Verification (trước → sau)

| Gate | Trước release | Sau release |
|---|---|---|
| `dotnet test Vaz.sln` | 463/463 pass | **463/463 pass**, 0 fail/skip |
| `dotnet build -c Debug` | 0 warning | **0 warning**, 0 error |
| `dotnet build -c Release` | 0 warning | **0 warning**, 0 error |
| `check.ps1 -Scope static` | PASSED | **PASSED** |
| CI `MIN_TESTS` | 460 | 460 |

```
Passed! - Failed: 0, Passed: 463, Skipped: 0, Total: 463
    0 Warning(s) / 0 Error(s)   (Debug + Release, --no-incremental)
CHECK PASSED: static
```

## 5. Commits & tag

- **Phạm vi release:** từ `769cd75` (`feat(versions): phase B - instance creation UI`,
  commit đầu Đợt 18, 2026-11-04) đến tag `v0.18.0`.
- **Số commit trong phạm vi:** **75** (71 commit Đợt 18 + 19.1 trước khi release,
  cộng 4 commit Đợt 20). Tổng repo: **211** commit.
- **Tag:** annotated `v0.18.0`, message tóm tắt Multi-Version + 463 test + 0 warning.
  Tag **chưa được push** lên `origin` (external action — chờ user xác nhận).
- **GitHub Release workflow:** `.github/workflows/` chỉ có `ci.yml`; không có
  `release.yml` → không trigger được (Việc 5 optional, bỏ qua có chủ đích).

## 6. Known issues còn lại

1. **GPU Intel HD 4600 (driver 2016)** — Forge/Fabric thoát im lặng sau GL/Mixin,
   không verify được "vào menu" trên máy test. Nguồn:
   `docs/evidence/dot18/forge-installer-report.md` §5.
2. **Legacy Forge ≤ 1.12.2 (gồm 1.8.9)** — artifact không có trên
   `maven.minecraftforge.net`; UI chặn chủ động + installer guard. Cần legacy
   Maven source riêng.
3. **`.bak` sau Change Version** — `instance.json.bak` / `mods.json.bak` giữ lại có
   chủ đích để user tự khôi phục; chưa có dọn tự động.
4. **Runtime UI evidence cho Java section** — môi trường chặn launch GUI nên ảnh/video
   ở Phase E là BLOCKED; có manual test script 10 bước.
5. **IL2026 suppression** — `MinecraftVersionService` vẫn dùng reflection JSON;
   đã suppress có justification, kế hoạch source-gen ở 19.2.
6. **Chưa có installer/MSIX artifact** — app unpackaged; task này chỉ đóng gói metadata
   + tag, output build nằm ở `src/Vaz.WinUI/bin/x64/Release/...`.

## 7. Hướng dẫn publish (chờ user)

```bash
git push origin v0.18.0          # push tag (external action, chưa thực hiện)
# GitHub Release: tạo Release từ tag v0.18.0, dán nội dung CHANGELOG.md mục 0.18.0
```

Nếu muốn tự động hóa ở 19.2: thêm `.github/workflows/release.yml` chạy
`dotnet publish -c Release` + upload artifact + `softprops/action-gh-release`.

## 8. Self-review

- [x] Version đồng bộ 4 nơi; không còn `2.4.0`.
- [x] CHANGELOG cover 18.1→18.7 + 18.7b/c + 19.1, có link evidence.
- [x] README có feature list + 4 ảnh evidence tồn tại thật.
- [x] 463/463 test, 0 warning Debug/Release, static check PASSED.
- [x] Không refactor, không thêm feature, không sửa behavior đã PASS.
- [x] Tag annotated, chưa push; workflow release không tồn tại → disclose rõ.
- [x] Known issues 3 mục yêu cầu + 3 mục bổ sung, có nguồn.
