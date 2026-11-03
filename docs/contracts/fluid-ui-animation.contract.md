---
feature: fluid-ui-animation
status: WEB_VALIDATED
created: 2026-10-28
updated: 2026-10-29
verifier: web-verifier
---

# Fluid UI & Animation Polish Contract

Contract quy định các tiêu chuẩn chuyển động mượt mà, phản hồi xúc giác, vật liệu kính mờ và phân tầng thị giác trên Web Prototype (`docs/design-preview/`), dùng làm executable specification theo chuẩn Apple Fluid Design.

## Bảng tiêu chí nghiệm thu (Acceptance Criteria)

| # | Acceptance criterion | Evidence type | Cách kiểm | Evidence thực tế | Status |
|---|----------------------|---------------|-----------|------------------|--------|
| FA-AC-01 | Phản hồi xúc giác tức thì (:active scale) trên toàn bộ các phần tử tương tác (.btn, .chip, .tab, .nav-item, .instance-card, .shop-card, .server-card, .skin-card) | RUNTIME | Playwright: mô phỏng pointerdown/hover trên các controls, assert transform scale active < 1.0 và transition duration phản hồi <= 150ms | `tests/web-e2e/fluid-ui-animation.spec.ts:17` PASS (18/18 Chromium + Mobile Chrome) | PASS |
| FA-AC-02 | Chuyển động thanh trượt Nav Glider & Tab Glider mượt mà theo đường cong spring tự nhiên, không giật vị trí | RUNTIME | Playwright: chuyển đổi màn hình và tab, assert vị trí .nav-glider và .tab-glider bám dính đúng phần tử đang chọn với opacity = 1 | `tests/web-e2e/fluid-ui-animation.spec.ts:49` PASS (18/18 Chromium + Mobile Chrome) | PASS |
| FA-AC-03 | Chất liệu kính mờ (Frosted glass) và viền phản quang bắt sáng (light rim) trên modal, sidebar và dialog host | RUNTIME | Playwright: kiểm tra computed style backdrop-filter có blur và saturate trên .modal-scrim, .dialog-host và các thẻ nổi | `tests/web-e2e/fluid-ui-animation.spec.ts:76` PASS (18/18 Chromium + Mobile Chrome) | PASS |
| FA-AC-04 | Chuẩn hóa hiệu ứng mở/đóng Modal & Dialog: phóng to mượt mà (scale 0.96 -> 1) với backdrop scrim fade mượt mà | RUNTIME | Playwright: mở dialog/modal, assert animation dialog-in và fade-in kích hoạt, không giật bố cục | `tests/web-e2e/fluid-ui-animation.spec.ts:97` PASS (18/18 Chromium + Mobile Chrome) | PASS |
| FA-AC-05 | Hiệu ứng nâng thẻ (Card Hover Elevation): các thẻ có bóng đổ đa tầng và nâng nhẹ (translateY) mượt mà khi hover | RUNTIME | Playwright: hover trên .instance-card, .shop-card, assert transform translateY < 0 và box-shadow gia tăng | `tests/web-e2e/fluid-ui-animation.spec.ts:121` PASS (18/18 Chromium + Mobile Chrome) | PASS |
| FA-AC-06 | Phân cấp thị giác quang học (Optical Typography): tiêu đề lớn negative tracking (-0.02em đến -0.03em), nhãn nhỏ positive tracking (+0.02em đến +0.04em) | RUNTIME | Playwright: kiểm tra computed letter-spacing của .page-title, .modal-title, .badge, .chip | `tests/web-e2e/fluid-ui-animation.spec.ts:137` PASS (18/18 Chromium + Mobile Chrome) | PASS |
| FA-AC-07 | Tuyệt đối KHÔNG sử dụng `transition: all` hoặc `transition-property: all` trên toàn bộ CSS | AUTOMATED | Ripgrep quét `styles.css`, assert 0 kết quả khớp `transition:\s*all` hoặc `transition-property:\s*all` | `tests/web-e2e/fluid-ui-animation.spec.ts:152` PASS (0 kết quả, regex scan sạch) | PASS |
| FA-AC-08 | Hỗ trợ Reduced Motion: khi prefers-reduced-motion: reduce, toàn bộ animations/transitions <= 0.05s (hoặc 0.01ms), focus outline >= 2px | RUNTIME | Playwright: kích hoạt reduced-motion, assert mọi transition/animation duration <= 0.05s, kiểm tra focus-visible outline >= 2px | `tests/web-e2e/fluid-ui-animation.spec.ts:160` PASS (18/18 Chromium + Mobile Chrome) | PASS |
| FA-AC-09 | Responsive hoàn hảo tại 390px và 620px: không có thanh cuộn ngang ngoài ý muốn (scrollWidth <= clientWidth + 1) | RUNTIME | Playwright: đo scrollWidth và clientWidth trên các viewport 390px và 620px | `tests/web-e2e/fluid-ui-animation.spec.ts:190` PASS (18/18 Chromium + Mobile Chrome) | PASS |
| FA-AC-10 | Trải nghiệm trực quan mượt mà, sống động, tinh tế theo chuẩn Apple Fluid Design; toàn bộ test suite pass 100% | MANUAL_REQUIRED | Mở `http://127.0.0.1:5173/`, tương tác các màn hình, quay video/chụp ảnh nghiệm thu trực quan | Đã chụp ảnh và xác nhận trực quan bởi user: ĐẠT chuẩn SaaS 2026 & Apple Fluid Design; 18/18 test pass | PASS |
