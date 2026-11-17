# Sổ Theo Dõi Known Issues — VAZ Launcher

**Ngày cập nhật:** 2026-11-10  
**Tình trạng tổng quan:** Đã dọn dẹp và giải quyết toàn bộ các issue nghiêm trọng (P0/P1) từ Đợt 22 và P0.1.

---

## 1. Trạng thái Các Vấn Đề Đã Giải Quyết (Resolved)

| Mã vấn đề | Trạng thái ban đầu | Giải pháp & Trạng thái hiện tại | Commit / Ghi chú |
|---|---|---|---|
| **App tự đóng sau 45–60s** | P0 (Nghiêm trọng) | **RESOLVED:** Fix `PressScale.cs` không ném `COMException 0x800F1000` làm crash native XAML loop | `e98b18f` (chi tiết tại `docs/evidence/dot22/p01-app-autoclose-resolved.md`) |
| **crash.txt phình to 9.1MB** | P1 (Log spam) | **RESOLVED:** Đã thêm cơ chế log rotation tự động khi file vượt 1MB, giữ tối đa 5 bản sao lưu | `65d2474` |
| **_jarMetaCache không giới hạn** | P1 (Memory leak) | **RESOLVED:** Triển khai LRU eviction với LinkedList tracking, cap tối đa 200 entries | `da74651` |
| **BitmapImage không dispose** | P2 (Nghi ngờ 15 chỗ) | **RESOLVED:** Thực tế chỉ có 1 vị trí trong `NotificationToast` và đã được clear source khi Unloaded; 14 chỗ còn lại là false positive | `f3607b2` |
| **Event subscribe không unsubscribe** | P2 (Nghi ngờ ~60 chỗ) | **RESOLVED (False positive):** Toàn bộ ViewModel và View có lifecycle đều đã quản lý unsubscribe chuẩn qua MVVM Messenger / WeakReference / Dispose | Đợt 22 audit |
| **DI `? = null` 91 chỗ** | P2 (Code smell) | **RESOLVED:** Audit lại thực tế chỉ có 16 chỗ (A=7, B=3, C=6); trong đó 7 chỗ là intentional optional. 4 service batch 1 đã XML doc | `d2881c0`, `a7433bd` |

---

## 2. Các Vấn Đề Hiện Còn (Low / Design Limitation)

| Mức độ | Vấn đề | Mô tả | Hướng xử lý |
|---|---|---|---|
| **Low** | **WinUI 3 XamlCompiler CS9035 limitation** | Thuộc tính `required` trong C# 11/.NET 8 không thể áp dụng cho các ViewModel row được bind qua DataTemplate XAML (như `SearchHitRow.Hit`), do `XamlTypeInfo.g.cs` tự sinh lệnh gọi parameterless constructor `new SearchHitRow()` gây lỗi `CS9035`. Nếu gắn `[SetsRequiredMembers]` lại sinh cảnh báo `CS8618`. | Giữ `= null!` kèm comment giải thích tường minh theo Fallback 2a (đã ghi nhận tại `di-audit.md`). |
| **Low** | **Agent claim lifecycle.log P0.1 chưa implement** | Trong báo cáo P0.1 từng đề cập ghi log vòng đời ra `lifecycle.log`, tuy nhiên hiện tại hệ thống chỉ duy trì `crash.txt` và `nav_log.txt`. | Không ảnh hưởng vận hành do `crash.txt` và Event Viewer đã cung cấp đầy đủ evidence. |
