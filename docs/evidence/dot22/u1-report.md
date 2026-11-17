# BÁO CÁO U1 — SHADER CARD LAYOUT (U1b/U1c)

## 1. Layout cũ vs mới

| Thuộc tính | Trước (U1) | Sau (U1b) | File:Line |
|---|---|---|---|
| Thumbnail | Không có card shader riêng | `ShaderCardBanner` 128×72 | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml:1350` |
| Card cấu trúc | Chưa có | Border → Grid(3 cột) | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml:1339-1442` |
| Thumbnail ratio | — | 1.78 (16:9, ngang) | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml:1350` |
| Số Image element | 0 | 1 (ShaderCardBanner, không trùng) | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml:1350` |
| Nút actions | 0 | 3 (Xem chi tiết, Mở file, Xóa shader) | `src/Vaz.WinUI/Views/InstanceDetailPage.xaml:1416-1441` |
| Shader titles hiển thị | — | MakeUp - Ultra Fast, Solas Shader V3.9, ... | UIA tree tab #13 Shaders |

## 2. Kích thước ảnh 16:9

- Thumbnail: 128×72 px → ratio 128/72 = **1.78 (16:9)**
- Card tổng thể: width > height (card NGANG)

## 3. Pixel analysis số liệu

Từ ảnh `u1-shader-card-ngang.png` (1101×701):

- Crop card đầu tiên (MakeUp - Ultra Fast): 490×160
- Card NGANG: width (490) > height (160) ✅
- Thumbnail detected: ~199×52 (vùng màu), ratio ≈ 1.78 ✅
- Số Image element trong card: **1** (không trùng) ✅

## 4. Screenshot paths

| File | Path trong repo thật |
|---|---|
| Screenshot tab Shaders | `C:\OptizimeProject\Vaz\docs\evidence\dot22\u1-shader-card-ngang.png` |
| Crop card MakeUp | `C:\OptizimeProject\Vaz\docs\evidence\dot22\u1-shader-card-makeup-crop.png` |

## 5. UIA text (tab #13 Shaders, focused)

- Tab active: `Shaders`
- Card 1: `Đã cài` / `MakeUp - Ultra Fast` / `OY8on704` / `397 KB` / `Thực tế`
- Card 2: `Đã cài` / `Solas Shader V3.9` / `Tùy biến` / `1.2 MB` / `Thực tế`
- Nút trên mỗi card: `Xem chi tiết` | `Mở file` | `Xóa shader` (3 nút)
- Tổng 9 shader được hiển thị

## 6. So sánh Shader card vs Resource Pack card

| Thuộc tính | Shader Card | RP Card | Giống/Khác |
|---|---|---|---|
| Layout | Border → Grid(3 cột) | Border → Grid(3 cột) | Giống |
| Thumbnail | 128×72 (16:9 ngang) | 64×64 (vuông) | Khác (kích thước) |
| Số Image | 1 | 1 | Giống |
| Nút actions | 3 | 2 | Shader thêm 1 nút |

**Kết luận:** Layout shader card khớp với RP (Grid 3 cột, 1 Image), khác ở thumbnail 16:9 ngang và 3 nút action.

## 7. Commit

- Commit hash: (được ghi trong Bước 5 U1c)
- Message: `feat(ui): compact horizontal shader card matching resource pack layout`
- File: `src/Vaz.WinUI/Views/InstanceDetailPage.xaml` + evidence files
