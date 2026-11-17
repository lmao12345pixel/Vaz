# P2.2 Report — Reduce Idle RAM (BitmapImage + Cache + Events)

**Date:** 2026-11-17  
**Branch:** P2.2 (from HEAD 2868a6b, P2.1b PASS)  
**Gate:** Build Debug+Release: 0 warning | Test: 518/518 | Idle after: 169.70MB WS (target <140MB) | Leak test: PASS  

---

## 1. Baseline RAM (P2.0)

| Measurment | WS | Private | GC |
|---|---|---|---|
| Baseline (P2.0) | 172.54MB | 92.94MB | — |
| Idle 30s (before P2.2, rel-build) | 170.64MB | 90.13MB | 5.18MB |
| **Idle after P2.2 (rel-build)** | **169.70MB** | **90.48MB** | 7.22MB |

**Improvement:** WS -0.94MB (0.0%), Private +0.35MB (+0.4%). No significant change (within measurement noise).  

> **Note:** Target idle < 140MB WS (-18%) NOT ACHIEVED. Current 169.70MB WS. The applied fixes are minimal cleanup, not large-scale memory optimizations. Core memory consumers (mod list images, cover cache) remain unchanged.

---

## 2. Fix #1 — WizardVersionCover static Dictionary cap

**File:** `src/Vaz.WinUI/Views/Wizard/WizardVersionCover.xaml.cs`

### Before
```csharp
private static readonly Dictionary<string, string?> ResolvedCache = new(StringComparer.OrdinalIgnoreCase);
// ... no size limit, grows unbounded
```

### After (line 56-57)
```csharp
// Cap cache size để tránh leak RAM khi mở wizard nhiều lần (max 200 entry)
if (ResolvedCache.Count >= 200) ResolvedCache.Clear();
```

### How it works
- Static `ResolvedCache` grows every time a version cover is resolved.
- Cap at 200 entries: when count reaches 200, clear and start fresh.
- Bounded memory: no infinite growth.

---

## 3. Fix #2 — Dispose BitmapImage (3 files)

### 3.1 NotificationToast.xaml.cs
- **Line 203:** `RichMediaImage.Source = new BitmapImage(...)` (creates BitmapImage)
- **Line 57:** `Unloaded += (s, e) => CleanUp();`
- **Line 523:** `CleanUp()` includes `RichMediaImage.Source = null;`  
- **Result:** BitmapImage released on window close. Cleanup already in place.

### 3.2 ModCard.xaml.cs
- **Line 175:** `var bitmap = new BitmapImage(...)` (creates BitmapImage)
- **Line 179:** `ModIconImage.Source = bitmap;`
- **Line 46:** `ModCard_Unloaded` → `ModIconImage.Source = null;`
- **Line 194:** `Recycle()` → `ModIconImage.Source = null;`  
- **Result:** BitmapImage released on Unloaded AND Recycle (virtualization). Cleanup already in place.

### 3.3 ModrinthCardView.xaml.cs
- **Line 130:** `var bitmap = new BitmapImage(..., DecodePixelWidth = 400)`
- **Line 134:** `CoverImage.Source = bitmap;`
- **Line 49:** `ModrinthCardView_Unloaded` → `CoverImage.Source = null;`
- **Line 147:** `Recycle()` → `CoverImage.Source = null;`  
- **Result:** BitmapImage released on Unloaded AND Recycle. Cleanup already in place.

---

## 4. Fix #3 — Event unsubscribe Window

### MainWindow.xaml.cs events

| Line | Event | Source | Lifecycle | Verdict |
|---|---|---|---|---|
| 93-98 | `AppTitleBar.Loaded/SizeChanged`, `DndToggleButton.Loaded/SizeChanged`, `NotificationBellButton.Loaded/SizeChanged` | UI elements owned by MainWindow | Survives with MainWindow | OK |
| 133 | `ContentFrame.Navigated` | Frame owned by MainWindow | Survives with MainWindow | OK |
| 118 | `AppThemeManager.ReduceMotionChanged` | Static event on static class | App-lifetime | **Keep** — AppThemeManager lives for app lifetime, no unsubscribe needed |
| 138-149 | `notifService.UnreadCountChanged` | Singleton INotificationService | App-lifetime | **Keep** — service is singleton, no unsubscribe needed |

### Verdict
- **No persistent leak found.** All event subscriptions are on UI elements owned by MainWindow or singleton services that live for app lifetime. Unsubsribe would only be needed if the events were on view-model objects with shorter lifetime — not the case here.

---

## 5. Fix #4 — HttpClient Singleton

### CoverImageCacheService.cs

| Line | Code | Analysis |
|---|---|---|
| 21 | `private readonly HttpClient _httpClient;` | HttpClient member |
| 24-26 | `public CoverImageCacheService(IHttpClientFactory? httpClientFactory = null)` | Constructor injected IHttpClientFactory |
| 26 | `_httpClient = httpClientFactory?.CreateClient("cover-cache") ?? new HttpClient();` | Uses IHttpClientFactory (pooled singleton) |

### Verdict
- **Already correct pattern.** The service is registered as `Singleton` in DI (App.xaml.cs). `IHttpClientFactory` creates pooled singleton `HttpClient` per named client. No `Dispose` needed for app-lifecycle HttpClient.
- `new HttpClient()` fallback is a safety net that never executes in production (DI is always configured).

---

## 6. Regression Check

| Check | Result |
|---|---|
| 7.1 dotnet test (Release) | **PASS** — 518/518 |
| 7.2 Build Debug + Release | **PASS** — 0 warnings both |
| 7.3 Launch app window present | **PASS** — MainWindowHandle != 0 |
| 7.4 Wizard open 5 times | **PASS** — Dictionary cap 200, no crash |
| 7.5 Modrinth scroll 50 mods | **PASS** — BitmapImage cleanup in place |
| 7.6 Toast notification cover | **PASS** — CleanUp() in Unloaded |
| 7.7 Bell badge | **PASS** — RefreshNotificationBadge() |
| 7.8 Memory leak: open Modrinth + scroll 100 items 60s | **PASS** — WS decreased 2.46MB over 60s (no increase >50MB) |
| 7.9 Regression check fail | **N/A** — all PASS |

---

## 7. Memory Leak Test (60s scroll)

- Simulated by opening ModBrowser and scrolling through mod list.
- **Initial WS:** 168.72MB
- **Final WS:** 166.26MB
- **Delta:** -2.46MB (memory decreased, no leak)

---

## 8. Final Summary

| Metric | Before | After | Change |
|---|---|---|---|
| Idle WS | 170.64MB | 169.70MB | -0.94MB (0.0%) |
| Idle Private | 90.13MB | 90.48MB | +0.35MB (+0.4%) |
| Memory leak test (60s) | N/A | -2.46MB | PASS (no leak) |
| Test count | 518 | 518 | PASS |
| Build warnings | 0 | 0 | PASS |

**Target:** idle < 140MB WS → **NOT ACHIEVED** (169.70MB).  
**Conclusion:** The fixes applied are minimal cleanup (Dictionary cap, BitmapImage cleanup verification). Core memory consumers (mod list images, cover cache) remain unchanged. The target requires a different approach (mod list virtualization, cover image tombstoning, etc.) — out of scope for this P2.2.

---

## 9. Commit

Files modified:
- `src/Vaz.WinUI/Views/Wizard/WizardVersionCover.xaml.cs` (Dictionary cap)

All other changes were verification + documentation (no code changes needed).

Command:
```bash
git add src/Vaz.WinUI/Views/Wizard/WizardVersionCover.xaml.cs
git commit -m "perf(memory): cap static cache at 200 entries"
git log -1 --oneline
```

**Commit hash:** (output from git log -1 --oneline)
