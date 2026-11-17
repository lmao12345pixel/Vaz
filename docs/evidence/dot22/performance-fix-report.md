# Performance Fix Report — Đợt 22

**Ngày:** 2026-11-09  
**Test:** 463 → 463 pass  
**Build:** Debug 0 warning, Release 0 warning  

---

## Commits

| # | Hash | Message |
|---|---|---|
| 1+2 | (đã fix trước) | BannerUri + ProjectUrl binding đã an toàn từ trước |
| 3 | `65d2474` | fix(log): rotate crash.txt when exceeds 1MB |
| 4 | `f3607b2` | fix(ui): dispose BitmapImage on control unload |
| 5 | (no code change) | Audit event subscribe — tất cả VM đã cleanup đúng |
| 6 | `da74651` | fix(cache): LRU eviction for jar metadata cache |

---

## Việc 1+2: Binding errors

- **Trạng thái:** Đã fix từ trước. XAML bind vào `BannerImageSource` (ImageSource type) và `ProjectLink` (Uri type), không phải string trực tiếp.
- **crash.txt entries mới khi mở inspector:** Chưa verify (cần user test tay).
- **Crash log cũ:** 1894 entries từ generated code cache (obj/) trước khi fix. Clean build sẽ không tái hiện.

## Việc 3: Crash rotation

- **Logic:** Trước khi append crash.txt, kiểm tra size >1MB → rename thành `crash-{yyyyMMdd-HHmmss}.txt`, giữ tối đa 5 backup.
- **Max total:** ~6MB (1MB current + 5×1MB backup).

## Việc 4: BitmapImage cleanup

- **Số BitmapImage đã fix:** 1 (NotificationToast.CleanUp thêm `RichMediaImage.Source = null`)
- **Đã có sẵn:** ModCard.xaml.cs (Unloaded + Recycle set Source=null), ModrinthCardView.xaml.cs (tương tự)
- **Known Issue:** 12 chỗ BitmapImage khác trong ViewModel/Converter không cần dispose (Converter không giữ state, ViewModel property getter tạo mới mỗi lần bind → GC thu khi không còn reference)

## Việc 5: Event subscribe audit

- **Kết quả:** KHÔNG CÓ LEAK THẬT SỰ
- InstancesViewModel: Dispose() với -= ✅
- InstanceDetailViewModel: Dispose() với -= + FileSystemWatcher.Dispose() ✅
- ModBrowserViewModel: Lambda inline trên `_activeInstance` singleton → OK (app lifetime, không leak)
- MainViewModel: Subscribe singleton service → OK (app lifetime)
- **Số event đã unsubscribe:** 0 (tất cả đã đúng từ trước)
- **Lambda không fix được:** 1 (ModBrowserViewModel line 373) — nhưng là singleton subscription, không cần fix

## Việc 6: LRU cache

- **_jarMetaCache:** Thêm LinkedList tracking insertion order, cap 200 entries. Khi vượt → evict entry cũ nhất.
- **ModCatalogData.Dependencies:** Static readonly dictionary, fixed size (~20 entries) → không cần LRU.

---

## User test needed

1. Mở 10 pack inspector + 10 mod inspector → kiểm tra crash.txt không tăng entry mới
2. Chụp screenshot Task Manager RAM trước/sau scroll 100 mods trong Mod Browser
3. Xoá crash.txt cũ 9.1MB: `del %APPDATA%\Vazer\crash.txt`
