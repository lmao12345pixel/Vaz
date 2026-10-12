# Vazer Launcher (`Vaz`)

Standalone Minecraft launcher + mod management project, extracted from the
`UltraPcOptimizer` solution. This repository is fully independent — it has no
project reference to, and no dependency on, UltraPcOptimizer.

## Layout

```
Vaz.sln
├── src/Vazer.Core          # Core domain library (all launcher + mod logic)
├── tests/Vazer.Core.Tests  # xUnit test suite (119 tests)
└── appsettings.json        # Sample runtime configuration
```

## Vazer.Core

| Area | Folder | What it does |
| --- | --- | --- |
| Accounts & auth | `Abstractions/`, `Auth/`, `Store/` | Microsoft (MSAL) auth, offline/demo auth, account persistence |
| Instances | `Instances/` | Create, list, update, delete Minecraft installations |
| Java runtime | `Java/` | Adoptium API lookup + local JRE resolution (8–21) |
| Launch | `Launch/` | Game process launch (CmlLib) |
| Loaders | `Loaders/` | NeoForge + OptiFine installer services |
| Mods | `Mods/`, `Modrinth/` | Modrinth search/install, dependency resolution |
| Modpacks | `Modpacks/` | `.mrpack` import |
| Markdown | `Markdown/` | Lightweight markdown parser used for mod descriptions |
| DI | `Extensions/` | `AddMinecraftLauncher(...)` registration extension |

## Build & test

```powershell
dotnet build Vaz.sln
dotnet test  Vaz.sln
```

## Using the library

```csharp
using Microsoft.Extensions.DependencyInjection;
using Vazer.Core.Extensions;

var services = new ServiceCollection();
services.AddMinecraftLauncher(opt =>
{
    opt.RootPath = @"%APPDATA%/Vazer/Minecraft";
    opt.OfflineDemoMode = true;
});

var provider = services.BuildServiceProvider();
var launcher = provider.GetRequiredService<IGameLauncher>();
```

## Configuration

`MinecraftOptions` defaults live in `src/Vazer.Core/MinecraftOptions.cs`; data is
stored under `%APPDATA%/Vazer/Minecraft` by default. Copy `appsettings.json` next
to your host executable and bind the `Minecraft` section to override.

> Microsoft sign-in requires your own Azure AD application client id
> (`MsalClientId`). Leave `OfflineDemoMode` on for local development.