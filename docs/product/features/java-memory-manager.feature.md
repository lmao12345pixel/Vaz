# Feature: Java Resolver & JVM Memory Manager

## 1. Mục tiêu & Bối cảnh
Trong Minecraft launcher, việc cấu hình sai phiên bản Java (ví dụ chạy Minecraft 1.21.1 bằng Java 8 hoặc Java 17) hoặc cấp phát RAM không hợp lý (thiếu RAM gây lag giật, thừa RAM gây nghẽn Garbage Collector hoặc tràn RAM hệ thống) là nguyên nhân hàng đầu dẫn đến crash khi khởi chạy.

Tính năng **Java Resolver & JVM Memory Manager** mang lại trải nghiệm chuyên nghiệp tương đương Prism Launcher và Modrinth App:
- Tự động phát hiện (Auto-detect) các bản cài đặt Java trên hệ thống (Java 8, 17, 21), hiển thị đầy đủ Vendor, Version, Path và Architecture.
- Tự động gợi ý bản Java tương thích nhất theo phiên bản Minecraft của instance (VD: MC 1.21.1 -> Java 21; MC 1.20 -> Java 17; MC 1.12.2 -> Java 8).
- Cảnh báo rõ ràng khi Java đã chọn không tương thích kèm nút 1-click chuyển về Java chuẩn.
- Hỗ trợ tải tự động (Auto-download) Java từ Adoptium Temurin với thanh tiến trình trực quan.
- Nhập Java tùy biến kèm tính năng kiểm tra (Test Java) hiển thị kết quả probe version.
- Điều khiển RAM trực quan: thanh trượt Min/Max RAM, các preset nhanh (2 GB, 4 GB, 8 GB, 12 GB), biểu đồ thanh RAM hệ thống trực quan cảnh báo khi vượt 85% RAM vật lý.
- Bộ chọn Garbage Collector (GC) tối ưu: G1GC, Generational ZGC (Java 21), Shenandoah, và Custom flags.

## 2. User Stories

### US-1: Tự động phát hiện & Lựa chọn Java
- **Là** một người chơi Minecraft,
- **Tôi muốn** launcher tự động tìm thấy các bản Java đã cài trên máy và đánh dấu bản phù hợp nhất cho instance,
- **Để** tôi không phải tự tìm đường dẫn `javaw.exe` thủ công.

### US-2: Cảnh báo & Tự động sửa Java không tương thích
- **Là** một người chơi,
- **Tôi muốn** nhận cảnh báo ngay khi phiên bản Java được chọn không chạy được phiên bản Minecraft này,
- **Để** tôi khắc phục ngay trước khi bấm nút Play gây crash game.

### US-3: Cài đặt Java 1-Click
- **Là** một người dùng mới chưa có Java 21 trên máy,
- **Tôi muốn** có nút tải và cài đặt tự động Java 21 (Adoptium Temurin),
- **Để** tôi có thể vào chơi game ngay lập tức mà không cần tự tải cài đặt từ web ngoài.

### US-4: Điều khiển RAM & Cảnh báo an toàn
- **Là** một người chơi modpack,
- **Tôi muốn** chỉnh dung lượng RAM tối thiểu và tối đa bằng thanh trượt hoặc chọn preset nhanh (4 GB / 8 GB), đồng thời thấy biểu đồ RAM hệ thống,
- **Để** tránh cấp phát quá mức làm treo máy hoặc thiếu RAM làm đơ game.

### US-5: Tối ưu Garbage Collector (GC)
- **Là** một người chơi muốn tối ưu FPS và giảm giật khung hình (stutter),
- **Tôi muốn** chọn nhanh preset GC (G1GC hoặc Generational ZGC cho Java 21),
- **Để** game chạy mượt mà nhất mà không cần học thuộc các cờ lệnh phức tạp của JVM.

## 3. Phạm vi (Scope)
- **Web Prototype**: Triển khai tại tab Cài đặt (`#tab-settings`) của Instance Detail và tích hợp với modal Java Selector / Memory Manager.
- **CSS**: Scoped class, tuân thủ design tokens (`--dur-*`, `--ease-*`, `--accent`, `--bg-*`), responsive 390px/620px, không dùng `transition: all`.
- **E2E Tests**: Viết bộ test Playwright `tests/web-e2e/java-memory-manager.spec.ts` kiểm thử toàn bộ hành vi trên Chromium & Mobile Chrome.
