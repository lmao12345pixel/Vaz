# Feature: Comprehensive Launcher Settings (Cài đặt toàn diện)

## Bối cảnh & Mục tiêu
Cung cấp trung tâm cấu hình toàn diện cho launcher V.A.Z, chuẩn hóa theo các launcher hiện đại (Prism, Modrinth, CurseForge):
1. **Phân nhóm chuyên sâu (Category Tabs)**: Điều hướng danh mục cài đặt mượt mà qua các chip / tabs:
   - Tất cả (All)
   - Khởi chạy & Chung (Launcher)
   - Java & Minecraft (Java & Runtime)
   - Giao diện & Hiệu ứng (Appearance & Accent)
   - Mạng & Tải xuống (Network & Downloads)
   - Discord & Riêng tư (Discord & Privacy)
   - Lưu trữ & Bộ nhớ (Storage & Cache)
   - Thông tin & Chẩn đoán (About & System Specs)
2. **Java & Bộ nhớ RAM tối ưu**:
   - Đồng bộ mượt mà Min RAM & Max RAM bằng slider và input số, chặn Max RAM < Min RAM hoặc ngoài dải 1024 - 16384 MB.
   - Thư viện mẫu đối số JVM (Presets: Mặc định, Aikar's Flags tối ưu GC, ZGC/Shenandoah cho máy cấu hình cao).
   - Thiết lập độ phân giải mặc định và tùy chọn khởi chạy toàn màn hình (Fullscreen).
3. **Cá nhân hóa giao diện**:
   - Chuyển đổi theme tức thời: Dark, Light, Obsidian 2026.
   - Chọn màu nhấn chủ đạo (Accent Palette): Emerald (Mặc định), Cyan, Violet, Amber, Rose.
   - Bật / tắt giảm chuyển động (Reduce Motion) và âm thanh click giao diện (UI Sounds).
4. **Mạng & Tốc độ tải**:
   - Tùy chỉnh số luồng tải đồng thời (2, 4, 8, 16 luồng).
   - Chọn máy chủ / CDN tải (Cloudflare Fastly, Modrinth CDN, CurseForge Mirror).
   - Giới hạn tốc độ băng thông tải mod / assets.
5. **Discord Rich Presence & Quyền riêng tư**:
   - Bật / tắt Discord RPC kèm tùy biến dòng trạng thái và preview thời gian thực.
   - Tùy chọn hiển thị tên instance đang chơi trên Discord.
   - Bật / tắt thu thập nhật ký crash ẩn danh.
6. **Lưu trữ, Dọn dẹp & Khôi phục**:
   - Xem vị trí thư mục dữ liệu kèm nút mở nhanh thư mục.
   - Tính toán dung lượng cache tải xuống và nút dọn dẹp bộ nhớ đệm 1-click.
   - Tùy biến đường dẫn thư mục sao lưu thế giới.
7. **Thông tin hệ thống & Xuất log chẩn đoán**:
   - Hiển thị thông số phần cứng thực tế/giả lập: OS, CPU, RAM vật lý, GPU, phiên bản Java.
   - Nút kiểm tra cập nhật launcher.
   - Nút xuất báo cáo chẩn đoán (Diagnostic Report) ra clipboard / file.
8. **Khôi phục mặc định an toàn**:
   - Dialog xác nhận trước khi hoàn tác toàn bộ thiết lập về giá trị mặc định.

## User Stories
- Là game thủ, tôi muốn điều chỉnh RAM min/max và chọn bộ cờ JVM tối ưu để Minecraft chạy mượt nhất.
- Là người dùng thích tùy biến, tôi muốn chọn theme Obsidian và màu nhấn Violet để giao diện đồng bộ với sở thích.
- Là người dùng mạng giới hạn, tôi muốn chọn số luồng tải và CDN phù hợp để tải modpack không bị nghẽn mạng.
- Khi gặp sự cố, tôi muốn xem thông tin phần cứng và xuất báo cáo chẩn đoán 1-click để gửi hỗ trợ kỹ thuật.

## Tiêu chí kỹ thuật
- Tương thích ngược với các trường cũ (`#globalJavaPath`, `#globalRam`, `#globalRamValue`, `#globalTheme`, `#globalReduceMotion`, `#globalDataPath`, `#saveGlobalSettingsBtn`, `#resetGlobalSettingsBtn`, `#globalSettingsStatus`).
- Lưu trữ bền vững trong `localStorage` dưới khóa `vazer-demo-v1` -> `globalSettings`.
- 100% không dùng `transition: all` hoặc `transition-property: all`.
- Đầy đủ accessibility, `:focus-visible` ring >= 2px, responsive 390px và 620px không tràn ngang.
