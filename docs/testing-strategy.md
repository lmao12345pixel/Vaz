# V.A.Z — Testing Strategy

## Tổng quan

Chiến lược test 4 tầng cho V.A.Z, từ nhanh/rẻ đến chậm/đắt.

## Cách chạy kiểm tra bắt buộc

```bash
npm run check
# hoặc không cần npm:
.\scripts\check.ps1
```

`npm run check` chạy tuần tự: `dotnet build Vaz.sln` → `dotnet test Vaz.sln`
→ validate frontmatter/quyền của 6 agent. Dừng ngay khi có bước fail.

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

### Test hiện có (17 file test + 2 helper, 176 test)

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

## Tầng 3: Component Test (Web Prototype)

**Mục đích**: Kiểm tra UI component của web demo tại `docs/design-preview/`.
**Hiện trạng**: Chưa có test framework cho web demo.

### Cách kiểm tra hiện tại

1. Mở `docs/design-preview/index.html` trong browser.
2. Kiểm tra thủ công: sidebar navigation, nút Play, settings form, modal.
3. Kiểm tra responsive: thu nhỏ browser window.
4. Kiểm tra dark/light theme nếu có.

### Bổ sung sau (khi cần)

Nếu web prototype phát triển phức tạp hơn:

```bash
# Option 1: Playwright (đã có .playwright-mcp/ trong repo)
npm init -y
npm install --save-dev @playwright/test
npx playwright install

# Option 2: Vitest + jsdom (nếu chuyển sang component framework)
```

> **Hiện tại không thêm dependency cho web demo** — nó là static HTML, kiểm tra thủ công đủ dùng.

## Tầng 4: E2E Test (WinUI App)

**Hiện trạng**: Chưa có E2E test cho WinUI app.
**Lý do**: WinUI E2E cần Windows App Testing framework, chi phí setup cao.

### 5 luồng E2E ưu tiên (thiết kế trước, implement sau)

| # | Luồng | Bước | Kết quả mong đợi |
|---|-------|------|-------------------|
| 1 | Library → Instance → Play | Mở app → Chọn instance → Nhấn Play | Game launch hoặc hiện log |
| 2 | Settings lưu cấu hình | Mở Settings → Đổi RAM/Java → Save | Giá trị lưu đúng, persist sau restart |
| 3 | Mods bật/tắt | Mở instance → Tab Mods → Toggle mod | Mod state thay đổi, file .disabled rename |
| 4 | Auth flow | Chưa login → Nhấn Play → Auth dialog | Dialog hiện, auth success → Play |
| 5 | Import modpack | Kéo .mrpack vào app | Import thành công, instance mới xuất hiện |

### Cấu trúc thư mục E2E (chuẩn bị)

```
tests/
├── Vazer.Core.Tests/          # Unit test hiện có
└── Vaz.E2E.Tests/             # E2E test (tạo khi cần)
    ├── Vaz.E2E.Tests.csproj
    ├── Fixtures/
    │   └── AppFixture.cs      # Setup/teardown app
    ├── Tests/
    │   ├── InstanceFlowTests.cs
    │   ├── SettingsFlowTests.cs
    │   ├── ModToggleTests.cs
    │   ├── AuthFlowTests.cs
    │   └── ImportFlowTests.cs
    └── Helpers/
        └── UIAutomation.cs    # Windows UI Automation helpers
```

### Cách bổ sung E2E

1. Tạo project xUnit mới: `dotnet new xunit -o tests/Vaz.E2E.Tests`
2. Thêm reference: `Microsoft.Windows.Apps.Test` hoặc `FlaUI.UIA3`
3. Thêm vào solution: `dotnet sln add tests/Vaz.E2E.Tests/Vaz.E2E.Tests.csproj`
4. Viết fixture để launch/kill app.
5. Implement từng flow theo bảng trên.

## Quy tắc chung

1. **Không viết test vô nghĩa** chỉ để tăng coverage (vd: test getter/setter).
2. **Test phải có assertion có ý nghĩa** — kiểm tra behavior, không kiểm tra implementation.
3. **Tên test phải mô tả scenario**: `MethodName_WhenCondition_ShouldResult`.
4. **Mỗi thay đổi logic phải đi kèm test** — Developer tự viết, Tester kiểm chứng.
5. **Test fail = chặn merge.** Không bỏ qua test đỏ.
6. **Coverage là tham khảo, không phải mục tiêu.** Focus vào critical path.
