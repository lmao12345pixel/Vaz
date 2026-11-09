# Pack Manifest Schema — Resource Packs & Shader Packs

**Version:** 1.0  
**Date:** 2026-11-09  
**Files:** `resourcepacks.json`, `shaderpacks.json` (stored in instance root directory)

---

## 1. Mục tiêu
Lưu trữ metadata persistent cho Resource Packs và Shader Packs trong từng instance:
- Khắc phục lỗi mất trạng thái "Installed" sau khi restart app (V2).
- Khắc phục lỗi mất ảnh preview sau khi tải Shader (V3).
- Lưu liên kết giữa tên file zip thực tế và Modrinth ID/slug/version/thumbnail.

---

## 2. JSON Schema

### Root Object (`PacksManifest`)
| Trường | Kiểu | Mô tả |
|---|---|---|
| `schemaVersion` | `int` | Phiên bản schema (hiện tại: `1`) |
| `packType` | `string` | `"resourcepack"` hoặc `"shader"` |
| `updatedAt` | `DateTimeOffset` | Thời gian cập nhật manifest gần nhất |
| `packs` | `List<PackManifestEntry>` | Danh sách các pack đã cài đặt |

### Pack Entry (`PackManifestEntry`)
| Trường | Kiểu | Bắt buộc | Mô tả |
|---|---|---|---|
| `fileName` | `string` | **Có** | Tên file `.zip` thực tế trên đĩa (vd: `BSL_v8.2.09.zip`) |
| `projectId` | `string?` | Không | Modrinth Project ID (vd: `m0uik42B`), null nếu import thủ công/migration |
| `slug` | `string?` | Không | Modrinth slug (vd: `bsl-shaders`), null nếu migration |
| `title` | `string` | **Có** | Tên hiển thị (vd: `BSL Shaders`) |
| `versionId` | `string?` | Không | Modrinth Version ID |
| `thumbnailLocal` | `string?` | Không | Đường dẫn tương đối file ảnh cache cục bộ (vd: `thumbnails/shader-bsl-shaders.jpg`) |
| `thumbnailRemote` | `string?` | Không | URL ảnh gốc từ Modrinth CDN (fallback khi chưa cache) |
| `installedAt` | `DateTimeOffset` | **Có** | Thời điểm cài đặt / migration |
| `loader` | `string?` | Không | Loại loader tương thích (`iris`, `optifine`, `canvas`, hoặc null) |
| `author` | `string?` | Không | Tác giả pack |

---

## 3. Ví dụ File

### `shaderpacks.json`
```json
{
  "schemaVersion": 1,
  "packType": "shader",
  "updatedAt": "2026-11-09T19:45:00+07:00",
  "packs": [
    {
      "fileName": "BSL_v8.2.09.zip",
      "projectId": "m0uik42B",
      "slug": "bsl-shaders",
      "title": "BSL Shaders",
      "versionId": "n8A1gJkl",
      "thumbnailLocal": "thumbnails/shader-bsl-shaders.jpg",
      "thumbnailRemote": "https://cdn.modrinth.com/data/m0uik42B/images/xxx.png",
      "installedAt": "2026-11-09T19:40:00+07:00",
      "loader": "iris",
      "author": "Capt Tatsu"
    }
  ]
}
```

### `resourcepacks.json`
```json
{
  "schemaVersion": 1,
  "packType": "resourcepack",
  "updatedAt": "2026-11-09T19:45:00+07:00",
  "packs": [
    {
      "fileName": "Bare+Bones+1.21.zip",
      "projectId": "kJepv8hZ",
      "slug": "bare-bones",
      "title": "Bare Bones",
      "versionId": "abc12345",
      "thumbnailLocal": null,
      "thumbnailRemote": "https://cdn.modrinth.com/data/kJepv8hZ/icon.png",
      "installedAt": "2026-11-09T19:42:00+07:00",
      "loader": null,
      "author": "RobotPantaloons"
    }
  ]
}
```

---

## 4. Quy tắc Migration (Tương thích ngược)
1. Khi mở instance, nếu `resourcepacks.json` hoặc `shaderpacks.json` chưa tồn tại:
   - Quét toàn bộ file `*.zip` trong folder `resourcepacks/` và `shaderpacks/`.
   - Với mỗi zip, tạo `PackManifestEntry` với:
     - `fileName`: tên file zip.
     - `title`: tên file bỏ đuôi `.zip`.
     - `projectId`: `null` (không đoán mò).
     - `slug`: `null`.
     - `installedAt`: ngày sửa đổi file (`LastWriteTimeUtc`).
   - Lưu file manifest mới vào instance.
2. Khi người dùng cài đặt lại một pack qua Modrinth Browser:
   - Tìm entry có `fileName` hoặc `projectId` tương ứng trong manifest -> cập nhật đầy đủ `projectId`, `slug`, `thumbnailRemote`, `thumbnailLocal`.
