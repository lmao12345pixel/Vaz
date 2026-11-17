# Feature Spec: Motion Integration (Main Prototype)

## 1. Bối cảnh & Mục tiêu

Motion Lab (`docs/design-preview/motion-lab.html`) đã đạt trạng thái `WEB_VALIDATED`.
Sau khi thử nghiệm ban đầu và tiếp nhận phản hồi từ người dùng (*"bỏ cái modal 4 bước chạy tuần tự cái này nhìn quá AI và thực sự không cần thiết"*), phạm vi tích hợp được tối giản hóa theo nguyên lý YAGNI:
1. **Shared Element Transition (FLIP)**: chuyển cảnh mượt giữa thẻ (Card) trong danh sách `#instanceGrid` và màn hình chi tiết (`#page-detail`) mà không làm "nhảy" layout.
2. **Direct Inline Launch**: Khởi chạy trực tiếp ngay tại chỗ (inline spinner & chip trạng thái), không popup modal 4 bước thừa thãi, cho phép dừng an toàn bằng nút Stop.

Mục tiêu đợt này là **ghép nối trải nghiệm Shared Element FLIP vào giao diện chính (`index.html`)** của V.A.Z web prototype:
- Khi người dùng nhấp vào một Instance Card trong thư viện, icon và tên instance sẽ bay và phóng to/thu nhỏ chuyển tiếp liên tục vào header của trang chi tiết (`#page-detail`) bằng kỹ thuật FLIP.
- Khi người dùng bấm nút quay lại ("&larr; Thư viện" hoặc phím Escape/Back), transition đảo ngược lại vị trí thẻ ban đầu.
- Khởi chạy instance trực tiếp và nhanh chóng, hiển thị trạng thái `Đang chạy` và nút `Stop`.

## 2. User Stories

- **US-MI-01 (Shared Element Entry)**: Là người dùng duyệt danh sách instances, khi tôi click vào một card, tôi muốn thấy icon và tiêu đề của card chuyển tiếp mượt mà vào đầu trang chi tiết để tạo cảm giác liên tục về không gian, không bị giật trang đột ngột.
- **US-MI-02 (Shared Element Exit)**: Là người dùng ở trang chi tiết, khi tôi bấm nút quay lại danh sách instances, tôi muốn phần tử tiêu đề và icon thu nhỏ lướt trở về đúng vị trí card ban đầu.
- **US-MI-03 (Direct Clean Launch)**: Là người chơi, khi bấm Play tôi muốn game khởi chạy nhanh gọn trực tiếp, nút chuyển sang Stop để dừng game khi cần, không bị gián đoạn bởi popup modal che khuất màn hình.
- **US-MI-04 (Accessibility & Reduced Motion)**: Là người dùng bật chế độ giảm chuyển động (hoặc OS prefers-reduced-motion), các thao tác chuyển trang và khởi chạy diễn ra ngay lập tức (thời gian <= 50ms), không gây chóng mặt hay giật lag.

## 3. Phạm vi

### Trong phạm vi (Web Prototype)
- `docs/design-preview/index.html`: Cấu trúc header `#page-detail` và thẻ `#instanceGrid .instance-card` hỗ trợ FLIP transition.
- `docs/design-preview/styles.css`: Styling cho shared element FLIP transitions; tuân thủ tokens `--dur-*`, `--ease-*`, tuyệt đối không có `transition: all`.
- `docs/design-preview/app.js`: Tích hợp FLIP transition vào `openDetail()` và `closeDetail()`; giữ `launchInstance()` khởi chạy inline trực tiếp, mượt mà và an toàn.
- `tests/web-e2e/motion-integration.spec.ts`: Bộ test Playwright kiểm thử toàn diện MI-AC-01 đến MI-AC-07 và MI-AC-09 trên chromium và mobile-chrome.

### Ngoài phạm vi
- Chưa chỉnh sửa C# WinUI desktop (`src/Vaz.WinUI`).
- Không làm thay đổi selector mà các spec cũ phụ thuộc (`.toast`, `.mod-row`, `.tab`, `#crashAnalyzerBanner`, `#modInspectorHost`).
