# V.A.Z — Testing Strategy

## Tổng quan

Chiến lược test 4 tầng cho V.A.Z, từ nhanh/rẻ đến chậm/đắt.

## Cách chạy kiểm tra bắt buộc

```bash
# Static: build + unit test + validate agent config
npm run check:static
# hoặc
.\scripts\check.ps1

# Runtime web (Playwright). Trả NOT_CONFIGURED nếu runner chưa cài.
npm run check:runtime

# Cả hai
npm run check:all
```

`npm run check:static` chạy tuần tự: `dotnet build Vaz.sln` → `dotnet test Vaz.sln`
→ validate frontmatter/quyền/testing-rule của 8 agent. Dừng ngay khi có bước fail.

> `npm run check` là alias cũ của static check. Nó **không** chứng minh UI/flow hoạt động.

## Phân loại bằng chứng (bắt buộc)

| Loại | Dùng cho | Bằng chứng hợp lệ | Không phải bằng chứng |
|------|----------|-------------------|------------------------|
| `AUTOMATED` | build, typecheck, lint, unit/integration test, static analysis | command + exit code + output + tên test | đọc code, "test có vẻ đúng" |
| `RUNTIME` | UI, flow, browser/app | chạy app + kết quả nhìn thấy, screenshot, log, URL | build pass, unit test pass, code review |
| `MANUAL_REQUIRED` | không tự động hóa được | user xác nhận + bước làm + kết quả | lời khai của Developer |
| `BLOCKED` | chưa có runner/môi trường | lý do + runner/môi trường còn thiếu | mọi thứ khác |

Quy tắc: AC UI/flow phải có `RUNTIME` evidence. Không chạy được runtime test →
**BLOCKED**, không được **PASS**. `MANUAL_REQUIRED` chưa có user xác nhận → **BLOCKED**.
AC `BLOCKED` không bao giờ PASS. Không DONE khi còn `PENDING`/`UNTESTED`.

## Verification Contract

Mọi task phải có contract trước khi Developer bắt đầu:

| # | Acceptance criterion | Evidence type | Verification method | Required evidence | Status |
|---|----------------------|---------------|---------------------|-------------------|--------|
| 1 | ... | AUTOMATED / RUNTIME / MANUAL_REQUIRED / BLOCKED | ... | ... | PENDING / PASS / FAIL / BLOCKED |

Chỉ Verifier cập nhật trạng thái cuối. Contract mẫu đầy đủ: `docs/agent-workflow.md`.

## Tầng 1: Static Check (Build)

**Công cụ**: `dotnet build`
**Mục đích**: Type check, syntax check, dependency resolution.
**Chạy**: Mọi thay đổi code.

```bash
dotnet build Vaz.sln
```

C# compiler đã kiểm tra type — không cần tool lint riêng.
Build pass = typecheck pass.

## Tầng 2: Unit Test

**Công cụ**: xUnit 2.9.3 + FluentAssertions 8.11.0 + Moq 4.21.0
**Vị trí**: `tests/Vazer.Core.Tests/`
**Chạy**: Mọi thay đổi logic.

```bash
dotnet test Vaz.sln
# hoặc chỉ core:
dotnet test tests/Vazer.Core.Tests/Vazer.Core.Tests.csproj
```

### Test hiện có (17 file test + 2 helper, 181 test)

| File | Khu vực |
|------|---------|
| `AccountStoreTests.cs` | Auth / lưu tài khoản |
| `CompositeAuthProviderTests.cs` | Auth / composite provider |
| `MsalAuthProviderTests.cs` | Auth / MSAL |
| `OfflineUuidTests.cs` | Auth / offline UUID |
| `InstanceManagerTests.cs` | Instances / quản lý instance |
| `JavaResolverTests.cs` | Java / tìm Java |
| `JavaResolverIOTests.cs` | Java / IO operations |
| `AdoptiumClientTests.cs` | Java / Adoptium API |
| `ModInstallServiceTests.cs` | Mods / cài mod |
| `ModrinthClientTests.cs` | Mods / Modrinth API |
| `MrpackImporterTests.cs` | Modpacks / import mrpack |
| `NeoForgeInstallerTests.cs` | Loaders / NeoForge |
| `OptifineInstallerTests.cs` | Loaders / Optifine |
| `FileDownloaderTests.cs` | Http / download file |
| `MarkdownLiteParserTests.cs` | Markdown / parser |
| `ModelsTests.cs` | Models / data models |
| `MinecraftServiceCollectionExtensionsTests.cs` | DI / service registration |
| `TestHelpers/FakeHttpMessageHandler.cs` | Test helper |
| `TestHelpers/FakeHttpClientFactory.cs` | Test helper |

### 10 test case ưu tiên cho V.A.Z

| # | Test case | Khu vực | Loại | Lý do ưu tiên |
|---|-----------|---------|------|----------------|
| 1 | Instance tạo mới với tên hợp lệ/không hợp lệ | Instances | Unit | Core feature — quản lý instance |
| 2 | Instance liệt kê đúng danh sách | Instances | Unit | Hiển thị trên UI chính |
| 3 | Mod install từ Modrinth thành công/thất bại | Mods | Unit | Tính năng chính — cài mod |
| 4 | Mod bật/tắt toggle state đúng | Mods | Unit | UX quan trọng |
| 5 | Java resolver tìm đúng version | Java | Unit | Prerequisite cho launch |
| 6 | Auth offline tạo UUID deterministic | Auth | Unit | Login flow cơ bản |
| 7 | RAM allocation validate range hợp lệ | Settings | Unit | Config an toàn |
| 8 | FileDownloader xử lý 404/timeout | Http | Unit | Resilience |
| 9 | NeoForge installer parse version đúng | Loaders | Unit | Loader support |
| 10 | Mrpack import file hợp lệ/corrupt | Modpacks | Unit | Import tính năng |

**Trạng thái hiện tại**: 8/10 case đã có test tự động. Case #1 mới có test cho
`CreateNeoForgeAsync` (Create Fabric/Vanilla cần network nên chưa test). Case #7
chưa có logic validate RAM range trong Core — `LaunchParameters.MaxRamMb` chỉ có
giá trị mặc định 4096 và Settings RAM chưa implement. Đây là gap đã biết, không
được coi là đã kiểm thử.

### Convention viết test mới

```csharp
// File: tests/Vazer.Core.Tests/{Feature}Tests.cs
// Naming: MethodName_Scenario_ExpectedResult
// Arrange-Act-Assert pattern
// Dùng FluentAssertions cho assertion
// Dùng Moq cho mock dependency

[Fact]
public async Task MethodName_WhenCondition_ShouldExpectedResult()
{
    // Arrange
    var sut = new SystemUnderTest();

    // Act
    var result = await sut.DoSomething();

    // Assert
    result.Should().BeTrue();
}
```

## Tầng 3: Runtime Test — Web Prototype (Playwright)

**Mục đích**: Kiểm tra UI/flow thật của web demo tại `docs/design-preview/`, không chỉ đọc code.
**Trạng thái**: Đã tạo runner + 3 test tại `tests/runtime/web/`; dependency **chưa cài** trong môi trường hiện tại, nên `npm run check:runtime` trả `RUNTIME CHECK NOT_CONFIGURED` (exit 2).

### 3 test ưu tiên

| # | Test | Assert người dùng nhìn thấy |
|---|------|-----------------------------|
| 1 | Sidebar → Settings | Click mục Settings trong sidebar; trang Settings hiển thị, trang Instances ẩn |
| 2 | Settings lưu giá trị | RAM 8192 được lưu, status hiện thông báo, reload vẫn giữ 8192 |
| 3 | Empty state Library | Không còn instance; `#instancesEmpty` hiện tiêu đề `Chưa có instance nào.` và hint `Bấm + để tạo instance mới.` |

### Cài đặt và chạy

```bash
npm install
npx playwright install chromium
npm run check:runtime
```

Test nằm trong `tests/runtime/web/vaz.spec.ts`; server tĩnh dùng `serve.mjs`.
Chi tiết: `tests/runtime/web/README.md`.

> `NOT_CONFIGURED` = chưa có runner, phải ghi **BLOCKED**. Không được thay bằng
> "đọc code thấy đúng" hoặc coi static check là runtime evidence.

## Tầng 4: Runtime/E2E — WinUI App (plan only)

**Hiện trạng**: Chưa có E2E test cho WinUI app; chưa cài Appium/FlaUI.
**Quyết định**: Chỉ lập kế hoạch, không thêm dependency ở giai đoạn này.
**E2E sau này**: Appium + Windows driver hoặc FlaUI.UIA3.

### Smoke test thủ công (bắt buộc khi task có AC UI/flow WinUI)

1. Launch app thật (Visual Studio hoặc `dotnet run --project src/Vaz.WinUI`).
2. Xác nhận cửa sổ chính/main screen mở được, không crash ngay.
3. Kiểm tra log khởi động: không có exception/lỗi nghiêm trọng.
4. Thực hiện đúng flow trong acceptance criterion và ghi lại kết quả nhìn thấy.
5. Nếu không chạy được app → **BLOCKED**, không PASS.

### 5 luồng E2E ưu tiên (thiết kế trước, implement sau)

| # | Luồng | Bước | Kết quả mong đợi |
|---|-------|------|-------------------|
| 1 | Library → Instance → Play | Mở app → Chọn instance → Nhấn Play | Game launch hoặc hiện log |
| 2 | Settings lưu cấu hình | Mở Settings → Đổi RAM/Java → Save | Giá trị lưu đúng, persist sau restart |
| 3 | Mods bật/tắt | Mở instance → Tab Mods → Toggle mod | Mod state thay đổi, file .disabled rename |
| 4 | Auth flow | Chưa login → Nhấn Play → Auth dialog | Dialog hiện, auth success → Play |
| 5 | Import modpack | Kéo .mrpack vào app | Import thành công, instance mới xuất hiện |

### Cấu trúc dự kiến

```
tests/
├── Vazer.Core.Tests/          # Unit test hiện có
├── runtime/web/               # Playwright web runtime (hiện có)
└── Vaz.E2E.Tests/             # WinUI E2E (tạo khi cần)
    ├── Vaz.E2E.Tests.csproj
    ├── Fixtures/
    │   └── AppFixture.cs
    ├── Tests/
    │   ├── InstanceFlowTests.cs
    │   ├── SettingsFlowTests.cs
    │   ├── ModToggleTests.cs
    │   ├── AuthFlowTests.cs
    │   └── ImportFlowTests.cs
    └── Helpers/
        └── UIAutomation.cs
```

### Cách bổ sung E2E sau này

1. Chọn Appium + Windows driver hoặc FlaUI.UIA3; đánh giá độ ổn định trước khi thêm dependency.
2. Tạo project xUnit `tests/Vaz.E2E.Tests`.
3. Viết fixture launch/kill app và chụp log.
4. Implement từng flow; mỗi flow phải assert kết quả người dùng nhìn thấy.
5. Chỉ đánh dấu PASS khi flow thực sự chạy; nếu không → BLOCKED.

## Quy tắc chung

1. **Không viết test vô nghĩa** chỉ để tăng coverage (vd: test getter/setter).
2. **Test phải có assertion có ý nghĩa** — kiểm tra behavior, không kiểm tra implementation.
3. **Tên test phải mô tả scenario**: `MethodName_WhenCondition_ShouldResult`.
4. **Mỗi thay đổi logic phải đi kèm test** — Developer tự viết, Tester kiểm chứng.
5. **Test fail = chặn merge.** Không bỏ qua test đỏ.
6. **Coverage là tham khảo, không phải mục tiêu.** Focus vào critical path.
