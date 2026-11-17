# Task: Fluid UI & Animation Polish

- **Feature**: `fluid-ui-animation`
- **Contract**: `docs/contracts/fluid-ui-animation.contract.md`
- **Feature Doc**: `docs/product/features/fluid-ui-animation.feature.md`
- **Phase**: `WEB_VALIDATED`
- **Owner**: Web Team
- **Date**: 2026-10-29

## Acceptance Criteria Checklist
- [x] FA-AC-01: Phản hồi xúc giác tức thì (:active scale) trên toàn bộ các phần tử tương tác (.btn, .chip, .tab, .nav-item, .instance-card, .shop-card, .server-card, .skin-card, .screenshot-card, .world-card) (RUNTIME)
- [x] FA-AC-02: Chuyển động thanh trượt Nav Glider & Tab Glider mượt mà theo đường cong spring tự nhiên, không giật vị trí (RUNTIME)
- [x] FA-AC-03: Chất liệu kính mờ (Frosted glass) và viền phản quang bắt sáng (light rim) trên modal, sidebar và dialog host (RUNTIME)
- [x] FA-AC-04: Chuẩn hóa hiệu ứng mở/đóng Modal & Dialog: phóng to mượt mà (scale 0.96 -> 1) với backdrop scrim fade mượt mà (RUNTIME)
- [x] FA-AC-05: Hiệu ứng nâng thẻ (Card Hover Elevation): các thẻ có bóng đổ đa tầng và nâng nhẹ (translateY) mượt mà khi hover (RUNTIME)
- [x] FA-AC-06: Phân cấp thị giác quang học (Optical Typography): tiêu đề lớn negative tracking (-0.02em đến -0.03em), nhãn nhỏ positive tracking (+0.02em đến +0.04em) (RUNTIME)
- [x] FA-AC-07: Tuyệt đối KHÔNG sử dụng `transition: all` hoặc `transition-property: all` trên toàn bộ CSS (AUTOMATED)
- [x] FA-AC-08: Hỗ trợ Reduced Motion: khi prefers-reduced-motion: reduce, toàn bộ animations/transitions <= 0.05s (hoặc 0.01ms), focus outline >= 2px (RUNTIME)
- [x] FA-AC-09: Responsive hoàn hảo tại 390px và 620px: không có thanh cuộn ngang ngoài ý muốn (scrollWidth <= clientWidth + 1) (RUNTIME)
- [x] FA-AC-10: Trải nghiệm trực quan mượt mà, sống động, tinh tế theo chuẩn Apple Fluid Design; toàn bộ test suite pass 100% (MANUAL_REQUIRED)

## Tiến độ thực hiện
1. [x] **Orchestrator**: Tạo Verification Contract và Feature Document (`WEB_SPEC`).
2. [x] **Web Developer**: Tinh chỉnh CSS tokens, styles, glider logic, và tạo bộ test Playwright `tests/web-e2e/fluid-ui-animation.spec.ts` (18/18 PASS).
3. [x] **Screenshots**: Chụp ảnh visual evidence (`docs/design-preview/screenshots/saas-2026-*.png`).
4. [x] **User**: Nghiệm thu trực quan FA-AC-10 -> Hoàn tất `WEB_VALIDATED`.
