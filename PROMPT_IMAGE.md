# PROMPT_IMAGE — Sprite Generation Guide

> Tài liệu prompt dùng cho các model sinh ảnh AI (gpt-image-1, DALL·E 3, Midjourney, SDXL...) để tạo sprite cho dự án **Cổ Sử Việt Hùng** (Tower Defense Việt Nam).

## Quy ước chung

### Style toàn cục (paste vào mọi prompt)

```
Pixel art, 2D top-down isometric tower defense game asset, Vietnamese folklore theme,
flat colors with subtle dithering, palette inspired by Đông Sơn bronze drum
(deep bronze, warm ochre, jade green, vermilion red, ivory white),
crisp 1px outline in dark brown (#2a1a10), no anti-aliasing,
transparent background (PNG with alpha), centered subject,
no text, no watermark, no UI elements.
```

### Cấu hình kỹ thuật

- **Format**: PNG, transparent background (alpha channel)
- **Color mode**: RGBA 8-bit
- **Pixels Per Unit (PPU) trong Unity**: 100
- **Pivot**: Center (0.5, 0.5) trừ khi ghi chú khác

### Quy ước negative prompt (cho model SDXL/Midjourney)

```
no 3D rendering, no realistic photo, no gradient mesh, no smooth shading,
no text overlay, no watermark, no signature, no border, no frame,
no cluttered background, no multiple subjects, no anime face details
```


---

## 1. Quái (Enemies)

### File path & naming
Đặt tại: `Assets/CSVH/Game/Sprites/Enemies/`

Quy ước: `Enemy_{IdSnakeCase}.png` — Id trùng với `EnemyConfig.Id` trong `enemies.json`.

| File | Size | Subject |
|---|---|---|
| `Enemy_Ho_Tinh.png` | 64×64 | Hồ Tinh (cáo chín đuôi) |
| `Enemy_Quan_Tong.png` | 64×64 | Quân Tống |
| `Enemy_Quan_Nguyen_Mong.png` | 64×64 | Quân Nguyên Mông |
| `Enemy_Moc_Tinh.png` | 64×64 | Mộc Tinh (yêu cây) |
| `Enemy_Thuong_Luong.png` | 80×80 | Thuồng Luồng (giao long) |
| `Enemy_Quy_Mot_Gio.png` | 128×128 | Quỷ Một Giò (boss) |

### Prompt — Hồ_Tinh

```
Pixel art enemy sprite, 64x64, Vietnamese fox spirit "Hồ Tinh",
nine slender tails fanning behind, sleek crimson and white fur,
glowing amber eyes, mischievous expression, agile lean pose facing camera,
tiny clawed paws, jade-green spirit aura wisp at base,
flat colors, 1px dark outline, transparent background, centered, no shadow.
```

### Prompt — Quân_Tống

```
Pixel art enemy sprite, 64x64, ancient Song dynasty Chinese soldier "Quân Tống",
lacquered bamboo armor in faded crimson and indigo, conical iron helmet
with red plume, holding a short curved dao saber low at side,
stern face, marching pose, leather boots, weathered cloth banner on back,
flat colors, 1px dark outline, transparent background, centered.
```

### Prompt — Quân_Nguyên_Mông

```
Pixel art enemy sprite, 64x64, Mongol Yuan warrior "Quân Nguyên Mông",
fur-trimmed steel helmet, dark leather lamellar armor, recurve composite bow
slung on back, wolfskin cloak, fierce slanted eyes, mounted-archer build but
on foot, charging pose, dust kick at boots,
flat colors, 1px dark outline, transparent background, centered.
```


### Prompt — Mộc_Tinh

```
Pixel art enemy sprite, 64x64, Vietnamese tree spirit "Mộc Tinh",
gnarled humanoid figure made of bamboo and old banyan wood, mossy bark skin,
glowing emerald eye-knots, twiggy arms with sharp leafy claws,
rooted lumbering stance, small white plumeria flowers blooming on shoulder,
flat colors, 1px dark outline, transparent background, centered.
```

### Prompt — Thuồng_Luồng

```
Pixel art enemy sprite, 80x80, Vietnamese water serpent "Thuồng Luồng",
long sinuous dragon-eel body with sapphire blue scales and golden belly,
horned snout, whisker-barbels, four small clawed limbs, dripping water droplets,
fierce coiled charging pose, lotus-pad ripple at base,
flat colors, 1px dark outline, transparent background, centered.
```

### Prompt — Quỷ_Một_Giò (BOSS)

```
Pixel art boss sprite, 128x128, Vietnamese one-legged demon "Quỷ Một Giò",
hulking ogre with single massive bare leg balanced on a clawed foot,
charcoal-black skin with red tribal markings, twin crooked horns,
single huge bloodshot eye, brass nose ring, holding a chipped stone club,
ferocious open-mouth roar showing fangs, taller than enemies,
flat colors, 1px dark outline, transparent background, centered, imposing.
```

---

## 2. Thành (Tower)

### File path & naming
Đặt tại: `Assets/CSVH/Game/Sprites/Tower/`

| File | Size | Subject |
|---|---|---|
| `Tower_Base.png` | 96×96 | Thân Thành (nền móng) |
| `Tower_Cannon.png` | 64×64 | Đại bác Đông Sơn (overlay quay theo target) |

### Prompt — Tower_Base

```
Pixel art tower sprite, 96x96, ancient Vietnamese citadel turret "Thành cổ",
stacked stone blocks with terracotta tile roof curving like Đại Việt pagoda,
small lotus motif carved on front wall, two narrow arrow-slit windows glowing
warm yellow, base wrapped with woven bamboo fence, jade-green moss accents,
flat colors with Đông Sơn bronze and warm ochre palette, 1px dark outline,
transparent background, centered, top-down isometric 3/4 view.
```

### Prompt — Tower_Cannon (rotates toward target)

```
Pixel art weapon sprite, 64x64, traditional Vietnamese ballista "Nỏ thần"
mounted on a swivel base, polished dark teakwood frame with bronze fittings
inscribed with Đông Sơn spiral patterns, drawn bowstring, single bamboo bolt
ready, profile side view oriented to the right (so it can be rotated by code),
flat colors, 1px dark outline, transparent background, pivot at base center.
```

> **Pivot note**: Trong Unity Sprite Editor, set Pivot = Custom (0.5, 0.2) — gốc xoay nằm ở base ballista, không phải center.


---

## 3. Đạn (Projectiles)

### File path & naming
Đặt tại: `Assets/CSVH/Game/Sprites/Projectiles/`

Quy ước: `Projectile_{IdSnakeCase}.png`

| File | Size | Subject |
|---|---|---|
| `Projectile_Mui_Ten_Tre.png` | 32×16 | Mũi Tên Tre |
| `Projectile_Dan_Da.png` | 24×24 | Đạn Đá |

### Prompt — Mũi_Tên_Tre

```
Pixel art projectile sprite, 32x16, traditional Vietnamese bamboo arrow
"Mũi tên tre", split-bamboo shaft with iron arrowhead and three white feather
fletches, side profile pointing to the right, slight motion blur trail of dust,
flat colors with bamboo green and bronze tip, 1px dark outline,
transparent background, pivot at left-center for projectile motion.
```

> **Pivot note**: Custom (0.1, 0.5) — gốc nằm ở đuôi tên để direction vector đúng hướng.

### Prompt — Đạn_Đá

```
Pixel art projectile sprite, 24x24, weathered round stone shot "Đạn đá",
dark grey granite with cracked surface texture, faint orange ember glow,
small dust particles trailing, top-down circular view,
flat colors, 1px dark outline, transparent background, centered pivot.
```

---

## 4. Special FX (chiêu đặc biệt)

### File path & naming
Đặt tại: `Assets/CSVH/Game/Sprites/Special/`

| File | Size | Subject |
|---|---|---|
| `Special_Trong_Dong_Dong_Son.png` | 256×256 | Trống Đồng Đông Sơn (hiệu ứng nổ tròn) |
| `Special_Luoi_Guom_Le_Loi.png` | 256×128 | Lưỡi Gươm Lê Lợi (chém ngang) |
| `Special_Mui_Ten_An_Duong_Vuong.png` | 256×128 | Mũi Tên An Dương Vương (rãnh sét xuyên thẳng) |

### Prompt — Trống_Đồng_Đông_Sơn

```
Pixel art VFX sprite, 256x256, Vietnamese Đông Sơn bronze drum shockwave,
top-down circular view of a full bronze drum surface with concentric rings,
star at center, dancers/birds frieze patterns, expanding golden energy ring
radiating outward, semi-transparent at edges, jade green and bronze tones,
1px dark outline on solid parts, transparent background, centered pivot.
```


### Prompt — Lưỡi_Gươm_Lê_Lợi

```
Pixel art VFX sprite, 256x128, sweeping katana-style slash arc "Lưỡi gươm
Lê Lợi" from Vietnamese Lê dynasty hero legend, glowing white-blue crescent
trail with golden particles, motion blur from left to right,
ornate jade-handled sword silhouette barely visible at trail origin,
semi-transparent gradient edges, no background,
1px dark outline on opaque core, transparent PNG, centered.
```

### Prompt — Mũi_Tên_An_Dương_Vương

```
Pixel art VFX sprite, 256x128, magical arrow strike "Nỏ thần An Dương Vương",
brilliant golden lightning bolt of an arrow piercing horizontally left to right,
crackling electric blue sparks along shaft, large white flash at impact tip,
turtle-shell motif (golden turtle Kim Quy) faint at trail origin,
semi-transparent edges, transparent PNG, centered horizontal pivot.
```

---

## 5. HUD Icons

### File path & naming
Đặt tại: `Assets/CSVH/Game/Sprites/HUD/`

| File | Size | Subject |
|---|---|---|
| `Hud_Icon_Cong.png` | 64×64 | Icon nhánh Công (Attack) |
| `Hud_Icon_Giap.png` | 64×64 | Icon nhánh Giáp (Armor) |
| `Hud_Icon_Special.png` | 64×64 | Icon nhánh Special |
| `Hud_Icon_Exp.png` | 64×64 | Icon nhánh EXP / Cấp_Thành |
| `Hud_Avatar_Tower.png` | 96×96 | Avatar Thành ở vùng Dưới Phải |
| `Hud_Avatar_Enemy.png` | 96×96 | Avatar Quái ở vùng Trên Trái |
| `Hud_Frame_Bronze.png` | 256×64 | Khung viền hoa văn Đông Sơn (9-slice) |

### Prompt — Hud_Icon_Cong

```
Pixel art UI icon, 64x64, "Công" upgrade icon (attack damage),
crossed Đại Việt war saber and bamboo arrow on a circular bronze medallion
with Đông Sơn spiral border, deep red core glow,
flat colors with bronze and crimson, 1px dark outline,
transparent background, square centered composition, no text.
```

### Prompt — Hud_Icon_Giap

```
Pixel art UI icon, 64x64, "Giáp" upgrade icon (armor),
ornate Đại Việt bronze breastplate with embossed lotus and Đông Sơn star,
small leather strap details, polished sheen,
flat colors with bronze and jade green, 1px dark outline,
transparent background, square centered composition, no text.
```

### Prompt — Hud_Icon_Special

```
Pixel art UI icon, 64x64, "Đặc biệt" upgrade icon (special ability),
Đông Sơn bronze drum top view with central star and concentric heron pattern,
golden energy aura radiating, mystical glow,
flat colors with bronze, gold, and ivory, 1px dark outline,
transparent background, square centered composition, no text.
```

### Prompt — Hud_Icon_Exp

```
Pixel art UI icon, 64x64, "Kinh nghiệm" upgrade icon (experience),
upward-pointing chevron crowned with a stylized lotus blossom,
glowing teal-jade gradient core, soft particle sparkle around,
flat colors with jade green and ivory, 1px dark outline,
transparent background, square centered composition, no text.
```


### Prompt — Hud_Frame_Bronze (9-slice border)

```
Pixel art UI frame border, 256x64, ornate horizontal panel border in Đông Sơn
bronze drum style, repeating spiral and heron motifs along the length,
weathered bronze-gold color, dark inner shadow, flat center area meant to be
9-sliced (corners ornate, edges seamless tile, center empty/solid),
flat colors, 1px dark outline, transparent background, no text.
```

> **9-slice setup trong Unity**: Sprite Editor → Border = (32, 32, 32, 32) cho 4 góc; mode = Sliced trong Image component.

---

## 6. Background / Tilemap

### File path & naming
Đặt tại: `Assets/CSVH/Game/Sprites/Tiles/`

| File | Size | Subject |
|---|---|---|
| `Tile_Grass_Plain.png` | 64×32 | Tile cỏ Việt Nam (isometric) |
| `Tile_Grass_Lotus.png` | 64×32 | Tile cỏ có hoa sen (variant) |
| `Tile_Path_Stone.png` | 64×32 | Tile đường đá (đường Quái đi) |
| `Tile_Water_Pond.png` | 64×32 | Tile ao nước (decoration) |

### Prompt — Tile_Grass_Plain

```
Pixel art isometric tile, 64x32, lush green Vietnamese village grass tile,
diamond-shaped 2:1 isometric ratio, short grass blades and tiny clovers texture,
seamless tileable on all four sides, top-down 30-degree isometric perspective,
deep jade green and warm yellow-green palette,
flat colors with subtle dithering, no outline (tiles connect cleanly),
transparent background only at corners outside the diamond.
```

### Prompt — Tile_Grass_Lotus

```
Pixel art isometric tile, 64x32, Vietnamese grass tile with single lotus
blossom decoration in center, diamond 2:1 isometric ratio, pink lotus with
green pad on grass background, seamless tileable,
flat colors, palette of jade green / soft pink / ivory,
transparent background only at corners outside the diamond.
```

### Prompt — Tile_Path_Stone

```
Pixel art isometric tile, 64x32, weathered stone path tile for Vietnamese
ancient citadel approach, irregular grey cobblestones with moss in cracks,
diamond 2:1 isometric ratio, seamless tileable on all four edges,
flat colors with dithering, palette of slate grey / dark moss green / sand,
transparent background only at corners outside the diamond.
```

### Prompt — Tile_Water_Pond

```
Pixel art isometric tile, 64x32, calm Vietnamese village pond water tile,
diamond 2:1 isometric ratio, light ripples and a tiny lotus pad floating,
seamless tileable, semi-translucent shimmer highlight,
flat colors with dithering, palette of aqua teal / deep navy / pale ivory ripple,
transparent background only at corners outside the diamond.
```


---

## 7. Cấu hình Sprite trong Unity (sau khi import PNG)

Sau khi drop PNG vào folder, chọn từng asset và set Inspector:

### Sprite (Single)

| Field | Value |
|---|---|
| Texture Type | Sprite (2D and UI) |
| Sprite Mode | Single |
| Pixels Per Unit | 100 |
| Pivot | Center *(trừ trường hợp có ghi chú khác — ví dụ Tower_Cannon dùng Custom 0.5/0.2, Mui_Ten_Tre dùng 0.1/0.5)* |
| Mesh Type | Tight |
| Filter Mode | Point (no filter) — giữ pixel art sắc |
| Compression | None — tránh artifact nén trên sprite nhỏ |
| Wrap Mode | Clamp |

### Sprite 9-Sliced (cho `Hud_Frame_Bronze`)

| Field | Value |
|---|---|
| Sprite Mode | Single |
| Pivot | Center |
| Border | Left 32, Right 32, Top 32, Bottom 32 *(điều chỉnh theo viền thật)* |
| → Image component | Image Type = Sliced |

### Tilemap tile (cho `Tile_*`)

| Field | Value |
|---|---|
| Filter Mode | Point |
| Pixels Per Unit | 100 |
| Pivot | Center |
| → Tạo Tile asset: Right-click trong Project → Create → 2D → Tiles → Rule Tile (hoặc Tile thường) |

---

## 8. Naming convention tổng hợp

| Loại | Folder | Pattern | Ví dụ |
|---|---|---|---|
| Quái | `Sprites/Enemies/` | `Enemy_{IdSnakeCase}.png` | `Enemy_Ho_Tinh.png` |
| Thành | `Sprites/Tower/` | `Tower_{Part}.png` | `Tower_Base.png` |
| Đạn | `Sprites/Projectiles/` | `Projectile_{IdSnakeCase}.png` | `Projectile_Mui_Ten_Tre.png` |
| Special FX | `Sprites/Special/` | `Special_{IdSnakeCase}.png` | `Special_Trong_Dong_Dong_Son.png` |
| HUD icon | `Sprites/HUD/` | `Hud_Icon_{Track}.png` | `Hud_Icon_Cong.png` |
| HUD avatar | `Sprites/HUD/` | `Hud_Avatar_{Subject}.png` | `Hud_Avatar_Tower.png` |
| HUD frame | `Sprites/HUD/` | `Hud_Frame_{Style}.png` | `Hud_Frame_Bronze.png` |
| Tile | `Sprites/Tiles/` | `Tile_{Theme}_{Variant}.png` | `Tile_Grass_Lotus.png` |

### Quy tắc Id chuyển đổi

- Tên Quái có dấu (Hồ_Tinh, Quân_Tống, Quỷ_Một_Giò...) → trong filename dùng **không dấu, snake_case**:
  - `Hồ_Tinh` → `Ho_Tinh`
  - `Quân_Tống` → `Quan_Tong`
  - `Quân_Nguyên_Mông` → `Quan_Nguyen_Mong`
  - `Mộc_Tinh` → `Moc_Tinh`
  - `Thuồng_Luồng` → `Thuong_Luong`
  - `Quỷ_Một_Giò` → `Quy_Mot_Gio`
  - `Trống_Đồng_Đông_Sơn` → `Trong_Dong_Dong_Son`
  - `Lưỡi_Gươm_Lê_Lợi` → `Luoi_Guom_Le_Loi`
  - `Mũi_Tên_An_Dương_Vương` → `Mui_Ten_An_Duong_Vuong`
  - `Mũi_Tên_Tre` → `Mui_Ten_Tre`
  - `Đạn_Đá` → `Dan_Da`

- File C# / `enemies.json` vẫn giữ Id có dấu (`"Hồ_Tinh"`); chỉ sprite filename mới chuyển sang ASCII để Unity asset path không bị lỗi encoding trên Windows.


---

## 9. Bảng tham chiếu màu Đông Sơn (palette)

Dùng paste vào prompt khi muốn nhất quán màu sắc qua nhiều sprite:

```
Color palette (use these exact tones):
- Bronze gold:      #B58A40
- Bronze dark:      #6B4A1F
- Vermilion red:    #C8362C
- Crimson deep:     #7A1818
- Jade green:       #3F7A56
- Forest deep:      #1F4731
- Ivory white:      #F2E6C9
- Lacquer black:    #2A1A10
- Sky teal:         #4A9DAA
- Lotus pink:       #E5849C
```

---

## 10. Workflow gợi ý với gpt-image-1 / DALL·E 3

1. **Generate**: paste "Style toàn cục" + prompt riêng của sprite.
2. **Crop & resize**: model thường trả 1024×1024 — cần crop chuẩn kích thước rồi resize xuống size đã ghi (64×64, 96×96…) bằng nearest-neighbor (Photoshop "Nearest Neighbor (hard edges)" hoặc Aseprite Image → Resize → Algorithm: nearest).
3. **Background removal**: nếu model trả nền không trong, dùng `remove.bg` hoặc Photoshop Magic Wand + alpha mask.
4. **Palette quantize** (optional, để giữ pixel-art feel): chạy qua Aseprite "Color → Palette → Sort by hue" hoặc dùng plugin Pixaki.
5. **Save**: PNG-24 alpha, no interlacing.
6. **Drop vào Unity** đúng folder → Inspector apply settings ở mục 7.
7. **Test trong scene**: mở SampleScene, gán sprite vào prefab tương ứng (`EnemyPrefab.SpriteRenderer.Sprite`, etc.).

---

## 11. Checklist trước khi commit sprite

- [ ] PNG có alpha channel, không nền trắng đặc
- [ ] Đặt đúng folder + đúng naming pattern
- [ ] Pixels Per Unit = 100 (hoặc đồng bộ toàn dự án)
- [ ] Filter Mode = Point (no filter)
- [ ] Compression = None (cho sprite ≤ 256×256)
- [ ] Pivot đúng theo bảng (đặc biệt Tower_Cannon, Mui_Ten_Tre)
- [ ] Palette tuân quy ước Đông Sơn (warm bronze + jade)
- [ ] Không text/watermark/border thừa
- [ ] Test render trong scene: không bị blur, không bị ngược chiều

---

## 12. Reference legend (cảm hứng văn hóa)

| Asset | Câu chuyện / nguồn cảm hứng |
|---|---|
| Hồ Tinh | Hồ ly chín đuôi trong truyền thuyết Hồ Tây — "Sự tích Hồ Tây" |
| Quân Tống | Quân xâm lược nhà Tống thời Lý Thường Kiệt (1075–1077) |
| Quân Nguyên Mông | Quân Nguyên Mông xâm lược 3 lần thời Trần (1258, 1285, 1287–88) |
| Mộc Tinh | Yêu tinh cây cổ thụ trong truyền thuyết "Sự tích Hồ Gươm" |
| Thuồng Luồng | Giao long sông nước Việt cổ — "Sự tích Lạc Long Quân" |
| Quỷ Một Giò | Yêu ma một chân trong dân gian Việt — boss đợt 5 |
| Trống Đồng Đông Sơn | Bảo vật văn hóa Đông Sơn ~700 TCN — Bắc Bộ |
| Lưỡi Gươm Lê Lợi | Thuận Thiên kiếm Hoàn Kiếm — Lê Lợi đánh quân Minh |
| Mũi Tên An Dương Vương | Nỏ thần Kim Quy thời An Dương Vương — Cổ Loa |

---

*Tài liệu này là hướng dẫn sinh asset. Không cần commit sprite cùng spec, có thể tách commit riêng khi designer hoàn thành.*
