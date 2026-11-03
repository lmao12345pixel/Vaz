# Feature: Fluid UI & Animation Polish (Tối ưu giao diện & Chuyển động chuẩn Apple)

## 1. Mục tiêu & Bối cảnh
Theo nguyên lý thiết kế Apple WWDC ("Designing Fluid Interfaces") và hướng dẫn UI/UX cao cấp:
Giao diện ứng dụng chỉ thực sự "sống động" khi phản hồi ngay lập tức, chuyển động mượt mà liên tục, có chiều sâu vật lý và tôn trọng cơ học thế giới thực:
1. **Phản hồi tức thì (Instant Response)**: Nút bấm, tab, chip, card phản hồi ngay khi bấm (:active scale) thay vì đợi nhả chuột, loại bỏ hoàn toàn độ trễ cảm giác.
2. **Chuyển động tự nhiên (Spring Physics)**: Thanh trượt điều hướng (Navigation glider) và thanh trượt tab (Tab glider) lướt với gia tốc vật lý tự nhiên, mượt mà và dứt khoát.
3. **Chất liệu & Chiều sâu (Frosted Glass & Depth)**: Áp dụng kính mờ đa tầng (`backdrop-filter: blur`), bóng đổ ánh sáng thực tế và viền bắt sáng (light rim reflection) ở mép trên của các thẻ và dialog.
4. **Phân cấp thị giác quang học (Optical Typography)**: Tiêu đề lớn co hẹp khoảng cách chữ (negative tracking) để thanh thoát, chữ nhỏ và nhãn huy hiệu giãn nhẹ (positive tracking) để dễ đọc.
5. **Hiệu năng & Khả năng tiếp cận**: 100% thuộc tính chuyển động chạy trên compositor (`transform`, `opacity`), tuyệt đối không có `transition: all`, tuân thủ hoàn hảo `prefers-reduced-motion`.

## 2. Phạm vi (Scope)
- **Styles & Tokens (`docs/design-preview/styles.css`)**:
  - Bổ sung motion tokens: `--ease-spring-smooth`, `--ease-spring-snappy`, `--shadow-ambient`, `--shadow-elevated`.
  - Phản hồi xúc giác `:active` đồng bộ trên `.btn`, `.chip`, `.tab`, `.nav-item`, `.card-grid .instance-card`, `.shop-card`, `.server-card`, `.skin-card`, `.screenshot-card`, `.world-card`.
  - Chuẩn hóa `.modal-card` cho toàn bộ các modal (`#modpackInstallModalHost`, `#shopTryOnModalHost`, `#msAuthModalHost`, `#offlineModalHost`, `#dialogHost`).
  - Nâng cấp `.nav-glider` và `.tab-glider` với đường cong gia tốc tự nhiên.
  - Tinh chỉnh optical tracking cho typography: `.page-title`, `.modal-title`, `.section-title`, `.badge`, `.chip`, `.caption`.
  - Đảm bảo 0 `transition: all` hoặc `transition-property: all`.
  - Đảm bảo reduced-motion `<= 0.05s`.
- **Prototype Logic (`docs/design-preview/app.js`)**:
  - Tối ưu `moveNavGlider()` và `moveTabGlider()` đảm bảo căn chỉnh tọa độ chính xác, không giật khung hình.
- **Kiểm thử (`tests/web-e2e/fluid-ui-animation.spec.ts`)**:
  - Test đầy đủ các tiêu chí từ FA-AC-01 đến FA-AC-10 trên cả Desktop Chromium và Mobile Chrome.
