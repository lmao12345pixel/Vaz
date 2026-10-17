# BUILD-NOTES — Vaz.WinUI

Ghi chú build/run cho `src/Vaz.WinUI` (WinUI 3, unpackaged, self-contained x64).

## Môi trường dev
- **Visual Studio Build Tools 2026 (BuildTools 18)** — có sẵn **AppxPackage tooling** (`Microsoft.AppxPackage.*` DLLs) → PRI/Appx targets chạy thật, không cần workaround nào.
- **.NET 8 SDK** — cho `net8.0-windows10.0.26100.0`.
- Windows SDK tools (`Microsoft.Windows.SDK.BuildTools` NuGet 10.0.26100.4654; `mt.exe` từ Windows Kits 10.0.26100.0 khi cần kiểm tra manifest).
- Build hoàn toàn bằng MSBuild CLI, không cần IDE.

## Lệnh build (BuildTools 18 MSBuild)
```powershell
$msb = "C:\Program Files (x86)\Microsoft Visual Studio\18\BuildTools\MSBuild\Current\Bin\MSBuild.exe"
# Restore (chỉ khi NuGet cache trống / nguồn NuGet offline của máy bị hỏng):
& $msb Vaz.sln -t:Restore -p:RestoreSources=https://api.nuget.org/v3/index.json -v:m
# Build:
& $msb Vaz.sln -t:Build -p:Configuration=Release -v:m
```
Lưu ý: dùng `-p:Configuration=Release` — `-c` gây lỗi MSB1001 với MSBuild này.

## Runtime — máy đích không cần cài gì
App là **unpackaged + self-contained**:
- **Không cần .NET Desktop Runtime** (`SelfContained=true` → runtime .NET nằm trong output).
- **Không cần Windows App Runtime MSIX** (`WindowsAppSDKSelfContained=true` → WinAppSDK runtime nằm trong output; bootstrapper tắt, UndockedRegFreeWinRT tự kích hoạt).
- Copy nguyên thư mục output là chạy được:

```
src\Vaz.WinUI\bin\x64\Release\net8.0-windows10.0.26100.0\win-x64\
```

Smoke test: chạy `Vaz.WinUI.exe` → cửa sổ **"Vazer Launcher"** (NavigationView 3 mục: Mod Browser / Instances / Accounts).

## Thuộc tính csproj then chốt
| Thuộc tính | Giá trị | Vì sao |
|---|---|---|
| `OutputType` | `WinExe` | app GUI, không console |
| `TargetFramework` | `net8.0-windows10.0.26100.0` | TFM của WinUI 3 + WinAppSDK |
| `UseWinUI` | `true` | kích hoạt XAML compiler/targets |
| `WindowsPackageType` | `None` | unpackaged (chạy trực tiếp .exe, không MSIX) |
| `EnableMsixTooling` | `true` | bắt buộc kể cả với unpackaged — nạp đúng logic resource + sinh `resources.pri` |
| `SelfContained` | `true` | đóng gói .NET runtime vào output |
| `WindowsAppSDKSelfContained` | `true` | đóng gói WinAppSDK runtime vào output |
| `WindowsAppSdkBootstrapInitialize` | `false` | self-contained không dùng bootstrapper (tránh đòi WinAppRuntime MSIX, lỗi 0x80670016) |
| `RuntimeIdentifier` | `win-x64` | self-contained WinAppSDK yêu cầu RID cụ thể |
| `Platforms` + fallback | `x64` | tự map `""` / `AnyCPU` / `"Any CPU"` → `x64` (self-contained không nhận AnyCPU) |
| `ApplicationManifest` | `app.manifest` | chứa `maxversiontested=10.0.19041.0` — WASDK yêu cầu; thiếu → app "defaults to Windows 8 behavior" và crash |

## Mapping platform trong `Vaz.sln`
Vaz.WinUI (GUID `{3D0F0758-73F6-49E9-8AD5-39E7E0ACD968}`): `Any CPU` → **x64** ở cả `Debug|x64` và `Release|x64`.

## App.xaml — bắt buộc `XamlControlsResources`
`<XamlControlsResources/>` phải nằm trong `Application.Resources` (merged dictionary). Thiếu nó → `XamlParseException: Cannot find a Resource with the Name/Key ...` ngay khi parse window đầu tiên → crash `0xC000027B` không có log (đã gặp và fix trong quá trình scaffold).

## CI/CD
- Máy build cần: .NET 8 SDK + **VS Build Tools có AppxPackage tooling** (hoặc MSBuild tương đương). Thiếu AppxPackage targets → không sinh `resources.pri` → app crash lúc khởi động (`0xC000027B`).
- Artifact = cả thư mục `bin\x64\Release\net8.0-windows10.0.26100.0\win-x64\` (self-contained — deploy target không cần cài runtime).
- `dotnet build` có thể dùng nếu môi trường có đủ WinUI tooling; đường đã kiểm chứng trong repo này là MSBuild của BuildTools 18.
