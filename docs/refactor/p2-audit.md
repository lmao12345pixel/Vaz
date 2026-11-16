# P2.0 — Performance Baseline + Audit (VAZER LAUNCHER)

Ngày: 2026-11-16
Đợt: P2.0 (Audit performance — KHÔNG code)
Tag: v0.19.0 local tại 6ecf9d7 (N3.3b fix), 518 test, 0 warning.

---

## 1. CẢM NANG (Baseline)

Đo trên máy: Win11, .NET 8, WinUI 3.

| Metric | COLD | WARM |
|---|---|---|
| cold_start_ms / warm_start_ms | **1371 ms** | **1531 ms** |
| RAM idle (30s) WorkingSet | 172.54 MB | 161.39 MB |
| RAM idle (30s) Private | 92.94 MB | 82.82 MB |

**Phân tích:**
- cold/WARM khác biệt nhỏ (1371 vs 1531 ms) → không rõ có cache OS hóc búa.
- RAM idle ~161-173 MB WS — khá cao cho app không có đồ họa nặng. Nguồn chính: event subscribers (MainWindow), unclosed BitmapImage, DI singleton graph (NotificationService + cover-cache HttpClient + instance graph).

---

## 2. AUDIT STARTUP

### 2.1 App.xaml.cs — OnLaunched

1. `App()` constructor:
   - `InitializeComponent()` — parse XAML (fast)
   - Build config (`ConfigurationBuilder` + `appsettings.json`)
   - Build DI container (`ServiceCollection` → `BuildServiceProvider`) — registers all singletons + viewmodels
   - `GpuTierDetector.Current = Services.GetRequiredService<GpuTierDetector>()` — constructor: sync-read `GpuTierStore.Load()` (file ~KB) + optional Registry read. **~1-5 ms**

2. `OnLaunched`:
   - UnhandledException hook
   - AppData dir + lifecycle log
   - `Dispatcher = DispatcherQueue.GetForCurrentThread()`
   - **BLOCK: `settings.LoadAsync().GetAwaiter().GetResult()`** (line 120-123)
   - `AppThemeManager.Initialize(settings)`
   - `MainWindow` create + `Activate()`
   - `INotificationService.InitializeAsync()` → `NotificationHistoryStore.LoadAsync()` (disk read ~KB) + UI dispatch

### 2.2 Hotspot chính

| # | File:Line | Giây ước tính | Lazy được? | Rủi ro |
|---|---|---|---|---|
| 1 | `App.xaml.cs:120-123` | 100-500 ms | Có — `settings` không cần trước window hiện | Sync-over-async; UI thread block; easy fix |
| 2 | `App.xaml.cs:38-73` | ~20-50 ms | No — DI graph settled toàn cục | Low |
| 3 | `App.xaml.cs:76` | ~1-5 ms | No — GpuTierDetector sync-load | Low |
| 4 | `MainWindow.xaml.cs:139-143` | ~100-300 ms (async) | No — NotificationHistoryStore.LoadAsync đầu tải | Low-Medium; defer to background |

### 2.3 Sync-over-async hiện có

- `App.xaml.cs:120-123`: `settings.LoadAsync().GetAwaiter().GetResult()` — classic sync-over-async; blocks UI thread. Fix: `await settings.LoadAsync().ConfigureAwait(false)`.

---

## 3. AUDIT RAM

### 3.1 BitmapImage (WinUI-managed native memory)

`new BitmapImage(...)` được tạo ở nhiều nơi, **không dispose rõ ràng**:

| File:Line | Context |
|---|---|
| `WizardVersionCover.xaml.cs:97` | `CoverImage.Source = new BitmapImage(...)` — on panel open |
| `NotificationToast.xaml.cs:203` | `RichMediaImage.Source = new BitmapImage(...)` — toast showing |
| `ModCard.xaml.cs:175`, `ModrinthCardView.xaml.cs:130` | card image load |
| `InstanceDetailViewModel.cs:96`, `1260` | mod icon from jar |
| `ResourcePacksShaders.cs:43,61,71,131,885` | resource pack shader preview |

- **Worst case:** panel mở/tắt nhiều lần → nhiều BitmapImage không được release → native memory grow.
- **Fix P2.2:** wrap BitmapImage in `using` or clear binding in `Unloaded` event / `Dispose()`.

### 3.2 HttpClient

- `CoverImageCacheService` (Singleton) — `new HttpClient()` in constructor (line 26). Since service is singleton, HttpClient is also singleton — acceptable, but no `Dispose()` → native socket/connection pool never released.
- Not a leak per se (singleton) but no dispose on app exit.

### 3.3 Cache unbounded

| Cache | File:Line | Size | Fixable? |
|---|---|---|---|
| `WizardVersionCover.ResolvedCache` | `WizardVersionCover.xaml.cs:17` | **static Dictionary, unbounded** | Yes — add size cap / eviction |
| `InstanceDetailViewModel._jarMetaCache` | `InstanceDetailViewModel.cs:149` | LRU cap 200 (good) | No — already bounded |
| `CoverImageCacheService` disk cache | `CoverImageCacheService.cs:18` | **no size cap** on disk dir | No — disk, manageable |

### 3.4 Event subscription leaks (long-lived)

| File:Line | Event | Subscribed? | Unsubscribed? |
|---|---|---|---|
| `MainWindow.xaml.cs:143-149` | `notifService.UnreadCountChanged` | + | **No** (panel/dialog close doesn't unsubscribe) |
| `MainWindow.xaml.cs:157` | `_settingsService.SettingsChanged` | + | **No** |
| `MainWindow.xaml.cs:119` | `AppThemeManager.ReduceMotionChanged` | + | **No** |
| `InstanceDetailViewModel.cs:251-252` | `_activeInstance.CurrentChanged`, `_mainViewModel.PropertyChanged` | + | **Yes** (in `Dispose()` line 1791) |
| `InstanceDetailViewModel.cs:1724-1725` | `_logWatcher.Changed/Created` | + | **Yes** (in `StopLogWatcher()` line 1739-1741) |
| `InstanceDetailViewModel.ResourcePacksShaders.cs:920-937` | `_resourcePacksWatcher._shaderPacksWatcher` | + | **Yes** (in `DisposeFileSystemWatchers()` line 984-992) |

**Note:** These are mostly on long-lived services (MainWindow, App) — the leak is **persistent** but not per-instance. Tốt nhất: unsubscribe in `mainWindow.Closed` or `App.OnExit`.

### 3.5 FileSystemWatcher

- `InstanceDetailViewModel._logWatcher` — properly disposed.
- `ResourcePacksShaders` watchers — properly disposed.
- No leak here.

### 3.6 LargeObjectHeap risk

- No unbounded byte array allocations found. `new byte[20]` in `InstanceDetailViewModel.cs:969` — small, transient, not LOH.
- No `LargeObjectHeap` risk.

---

## 4. ĐỀ XUẤT P2.1 / P2.2 / P2.3

### P2.1 (P1) — Startup fast — lazy init service không cần trước window hiện

**Scope:** Chỉ `App.xaml.cs` và `MainWindow.xaml.cs`.

**Phương án A (recommended):**
- `App.xaml.cs:120-123` → async/await (không `.GetResult()`)
- `MainWindow.xaml.cs:139-143` → defer `INotificationService.InitializeAsync()` cho lần đầu mở bell (thông qua lazy `NotificationCenterOverlay` open)
- Expected gain: **reduce cold_start_ms 20-40%** (1371 → ~800-1000)

**Rủi ro:** service lazy null khi race condition → cần null check trong từng consumer.

**File thay đổi:** `App.xaml.cs`, `MainWindow.xaml.cs`.

**Verify metric:** cold_start_ms, p95.

---

### P2.2 (P1) — RAM reduce — dispose BitmapImage, LRU cache, HttpClient Singleton

**Scope:** `NotificationToast.xaml.cs`, `ModCard.xaml.cs`, `ModrinthCardView.xaml.cs`, `WizardVersionCover.xaml.cs`, `CoverImageCacheService.cs`, `WizardVersionCover.xaml.cs`.

**Phương án A (recommended):**
- Wrap BitmapImage creation/use với `using` or clear binding on `Unloaded`
- Add bounded LRU cap to `WizardVersionCover.ResolvedCache`
- `CoverImageCacheService` → register `IHttpClientFactory` in DI (singleton HttpClient) — hoặc add `Dispose()`

**Expected gain:** **reduce idle RAM 10-30%** (161-173 → ~110-140 MB), **reduce peak RAM 15-40%** during mod browsing.

**Rủi ro:** cache miss tăng → load chậm hơn (trade-off). LRU cap cần tuning.

**File thay đổi:** `NotificationToast.xaml.cs`, `ModCard.xaml.cs`, `ModrinthCardView.xaml.cs`, `WizardVersionCover.xaml.cs`, `CoverImageCacheService.cs`.

**Verify metric:** idle RAM WS/Private, peak RAM.

---

### P2.3 (P2) — Micro-optimize — Settings load async, mod list virtualization, defer NotificationCenter init

**Scope:** `MainWindow.xaml.cs`, `InstancesPage.xaml`, `InstancesViewModel.cs`.

**Phương án:**
- `InstancesViewModel.LoadAsync()` → `INotificationService.InitializeAsync()` background only if unread count > 0
- Mod list virtualization (`ItemsPanelTemplate` = `VirtualizingStackPanel` on large lists)
- Defer `NotificationCenter` overlay init đến lần đầu click bell

**Expected gain:** nhỏ hơn (10-20% aggregate), không rủi ro đáng kể.

**Rủi ro:** low.

**Verify metric:** any startup micro-benchmark.

---

## 5. CÂU HỎI MỞ

1. `MainWindow.Closed` event có được subscribe từ `App.xaml.cs` để unsubscribe?
2. `NotificationCenterOverlay` panel — `Unloaded` handler để clear BitmapImage binding?
3. `WizardVersionCover.ResolvedCache` — cap cũ 1000? Tiềm năng memory leak.
4. Should `INotificationService` be disposed on app exit?

---

## 6. Ghi chú

- Không sửa code. Chỉ đo, audit, ghi doc.
- BUILD: 0 warning (không đổi code).
- TEST: 518/518 (không đổi).
- KHÔNG tag, KHÔNG push.
