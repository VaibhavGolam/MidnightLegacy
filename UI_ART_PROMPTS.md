# Midnight Legacy: UI art prompt pack

19 core pictures plus about 40 optional extras for the menu screens (sections D to G). Every one is optional: the game already has a plain version of each, and the painted one replaces it the moment the file is in the project. So you can generate them in any order and drop them in as they finish.

Where they go: `Assets/_Project/Resources/Art/`. File names must match exactly. After copying, run **Midnight Legacy > Reimport Art** once.

## How to generate them

1. Use the same image tool you used for the cars and characters, so the look stays consistent.
2. Start every prompt with the **style line** below.
3. For the transparent ones (everything starting with `ui_`), put the object on a **flat solid magenta background (#FF00FF)** and say so in the prompt. AI tools can't make real transparency. Send me the files afterwards and I'll cut the magenta out and size them to the right resolution. You don't have to remove any backgrounds yourself.
4. Ask for a **square image** for icons and buttons, **portrait 9:16** for scenes.
5. If the first result is wrong, change one thing at a time. Don't rewrite the whole prompt.

### Style line (paste at the start of every prompt)

```
Game UI art for a night street racing story game, painted anime look with clean shapes, deep navy and midnight blue base, periwinkle blue (#6B77D6) accents, small touches of warm gold, soft glow, slightly glossy like a racing game menu, no text, no letters, no numbers, no watermark.
```

### Negative line (add at the end if your tool supports it)

```
text, letters, words, logo, watermark, signature, photo, realistic, cluttered, noisy, blurry, extra buttons, cropped
```

---

## A. Story scenes (JPG, 1080 x 1920, portrait)

These fix the first screen you sent (the box with the skyline). Each fills the whole phone behind the story text. **Keep the bottom 40% darker and quiet**, because the text box sits there.

### 1. `scene_room.jpg` (the opening scene, Jun's room)

```
[STYLE LINE] Portrait 9:16 illustration of a small student bedroom at night in a harbour town. A large window on the upper half shows the lit town of Port Alder, a harbour with boats, hills and a calm dark sea under a starry sky. Dim room, a narrow bed with a rumpled blanket, a desk with a warm desk lamp and a closed laptop, a jacket on a chair. Lonely, quiet, a little sad mood. The lower 40 percent of the picture is dark and calm with very little detail so text can sit on it. Soft warm lamp light against cool blue night.
```

### 2. `scene_phone.jpg` (the phone call and message scenes)

```
[STYLE LINE] Portrait 9:16 close view of a smartphone lying on a dark wooden desk at night, the screen glowing pale blue with abstract soft shapes instead of any text. A small desk lamp glow at the side, shallow depth of field, a set of car keys next to the phone. The phone is in the upper half of the picture. The lower 40 percent is dark desk surface with almost no detail.
```

### 3. `scene_city.jpg` (town streets)

```
[STYLE LINE] Portrait 9:16 view down a wet harbour town street at night in Port Alder, warm shop lights and a few neon signs without readable letters, reflections on the road, old low buildings, a few parked cars, a streetlight haze, hills and the sea behind. The lower 40 percent is a dark wet road with very little detail.
```

### 4. `scene_road.jpg` (talking scenes on the road)

```
[STYLE LINE] Portrait 9:16 low angle view from a coastal mountain road at night, empty asphalt with white edge lines, stone guard wall, pine trees, the lit harbour town and the sea far below, a few stars, faint warm headlight glow coming round a bend. The lower 40 percent is dark road and shadow with very little detail.
```

### 5. `scene_pass.jpg` (the mountain pass scenes)

```
[STYLE LINE] Portrait 9:16 mountain pass road at night under a big pale moon, winding asphalt climbing between dark rocky slopes and pines, a thin mist in the valley, cold blue light with a little warm light from one far lamp. The lower 40 percent is dark rock and road with very little detail.
```

---

## B. Buttons and panels (PNG, magenta background)

These are drawn in nine pieces by the game, so the **corners keep their shape** while the middle stretches. That means: **a plain flat middle, no ornament or logo in the centre, same border all the way round.** Ask for them flat and front-on.

### 6. `ui_panel.png` (every card and info box), 512 x 512

```
[STYLE LINE] A single flat front-view UI card frame, square with rounded corners, dark navy glass fill with a subtle vertical gradient, a thin periwinkle blue glowing border line with a faint inner highlight along the top edge, soft shadow. The centre is a plain flat dark fill with no pattern, no icon, no text. Perfectly symmetrical, centred, on a flat solid magenta (#FF00FF) background.
```

### 7. `ui_button_primary.png` (main buttons), 768 x 288

```
[STYLE LINE] A single wide rounded game button, front view, periwinkle blue (#6B77D6) glossy surface with a lighter top half and a slightly darker bottom half, thin white inner edge highlight, soft dark drop shadow underneath. Plain flat centre with no icon and no text, same rounded corner size on all four corners. Centred on a flat solid magenta (#FF00FF) background.
```

### 8. `ui_button_secondary.png` (second choice buttons), 768 x 288

```
[STYLE LINE] A single wide rounded game button, front view, dark navy glass surface, thin blue-grey glowing outline, subtle lighter top edge, soft drop shadow. Plain flat centre with no icon and no text, same corner size on all four corners. Centred on a flat solid magenta (#FF00FF) background.
```

### 9. `ui_button_gold.png` (rewards, ad bonus), 768 x 288

```
[STYLE LINE] A single wide rounded game button, front view, warm gold and amber glossy surface with a bright top half and a deeper amber bottom half, thin pale yellow inner edge highlight, soft drop shadow. Plain flat centre with no icon and no text, same corner size on all four corners. Centred on a flat solid magenta (#FF00FF) background.
```

### 10. `ui_button_danger.png` (quit, leave), 768 x 288

```
[STYLE LINE] A single wide rounded game button, front view, deep crimson red glossy surface with a lighter top half, thin pink inner edge highlight, soft drop shadow. Plain flat centre with no icon and no text, same corner size on all four corners. Centred on a flat solid magenta (#FF00FF) background.
```

### 11. `ui_pad.png` (the two steering pads), 768 x 384

```
[STYLE LINE] A single wide rounded touch pad, front view, translucent dark glass with a faint inner glow, a thin periwinkle blue border, very subtle diagonal light streaks near the edges only. The centre is completely empty and plain (an arrow will be drawn on top by the game). Same corner size on all four corners. Centred on a flat solid magenta (#FF00FF) background.
```

### 12. `ui_btn_round.png` (base of the round drive buttons), 512 x 512

This one is **grey on purpose**. The game tints it red, blue, orange or silver for each button.

```
[STYLE LINE] A single round game control button seen from the front, a perfect circle, light neutral grey metallic rim with a thin bevel, dark smoked glass centre with a soft highlight on the upper half, subtle inner shadow. Colourless: only greys and white, no colour tint, no icon, no text. Centred with a little space round it on a flat solid magenta (#FF00FF) background.
```

---

## C. Icons (PNG, magenta background, 512 x 512)

All icons are **plain white on magenta**, flat, thick and simple, so the game can tint them. They sit inside the round buttons, so they must read at about 60 pixels.

Shared line for all six icons. In each prompt below, replace `[ICON LINE]` with this:

```
Flat white game icon, thick rounded shapes, no outline, no shading, no text, centred with generous space round it, on a flat solid magenta (#FF00FF) background.
```

### 13. `ui_icon_brake.png`

```
[ICON LINE] A brake symbol: a bold white circle outline with a solid white stop square inside, and two short curved lines either side like a brake disc sign.
```

### 14. `ui_icon_nitro.png`

```
[ICON LINE] A nitro flame symbol: one bold white flame shape with a smaller flame cut out inside it, leaning slightly forward like it is rushing right.
```

### 15. `ui_icon_drift.png`

```
[ICON LINE] A drift symbol: a simple top view car shape sliding sideways with two curved skid mark swooshes behind it.
```

### 16. `ui_icon_reverse.png`

```
[ICON LINE] A reverse symbol: one bold white U-turn arrow, the arrowhead pointing down at the bottom left.
```

### 17. `ui_icon_arrow.png`

```
[ICON LINE] A single bold chevron pointing to the RIGHT, like a play arrow made of two thick rounded strokes. The game flips it for the left pad.
```

### 18. `ui_icon_pause.png`

```
[ICON LINE] A pause symbol: two thick rounded vertical bars side by side.
```

### 19. `ui_medal.png` (results screen medals), 512 x 512

Grey on purpose. The game tints it gold, silver or bronze.

```
[STYLE LINE] A single round medal with a ribbon at the top, front view, a star embossed in the centre, simple clean shapes, colourless: only greys and white, no colour tint, no text. Centred on a flat solid magenta (#FF00FF) background.
```

---

## D. Menu icons (PNG, magenta background, 512 x 512)

These are for the bottom tab bar and the Career tiles, to match your mockups. They are drawn in white and tinted by the game (blue when selected, grey when not). Use the same `[ICON LINE]` as section C.

| File | Prompt (after `[ICON LINE]`) |
|---|---|
| `ui_tab_story.png` | An open book with a small bookmark ribbon. |
| `ui_tab_events.png` | A chequered racing flag on a short pole, waving. |
| `ui_tab_garage.png` | A front view garage door with a car silhouette inside. |
| `ui_tab_mail.png` | A closed envelope with a simple flap. |
| `ui_tab_shop.png` | A shopping bag with two short handles. |
| `ui_tab_more.png` | Three bold horizontal lines (menu symbol). |
| `ui_stat_races.png` | A stopwatch with a small flag beside it. |
| `ui_stat_wins.png` | A trophy cup with two handles. |
| `ui_stat_distance.png` | A winding road seen from above running into the distance. |
| `ui_stat_speed.png` | A speedometer dial with a needle pointing high. |
| `ui_stat_near.png` | Two car silhouettes passing very close side by side with a spark between. |
| `ui_stat_corners.png` | A road bending in a sharp curve with an arrow following it. |
| `ui_stat_drift.png` | A car sliding sideways with curved skid marks. |
| `ui_stat_cash.png` | A stack of three coins. |
| `ui_check.png` | A bold tick mark inside a circle. |
| `ui_lock.png` | A padlock, closed. |
| `ui_gear.png` | A cog wheel with eight teeth. |
| `ui_coin.png` | Not white: a gold coin with a bright rim and a dollar sign, front view, soft shine. Keep the magenta background. |

## E. Event pictures (JPG, 1024 x 448)

One wide picture per area. It sits on the right side of the event cards and the story card, and fades into the card on its left, so **keep the left third calm and dark**. Name must match the area: `thumb_forest.jpg`, `thumb_pass.jpg`, `thumb_coast.jpg`, `thumb_city.jpg`.

```
[STYLE LINE] Wide 16:7 night scene, a sports car on an empty road seen from a low three quarter view. The left third of the picture is dark and quiet. Add soft glow, light trails and a bit of mist.
```

Then swap the setting sentence:

- **forest:** narrow road through tall dark pines, mist, warm lamps far ahead.
- **pass:** winding mountain road with guard rail under a big pale moon.
- **coast:** coastal road with the lit harbour and sea behind, wet asphalt reflections.
- **city:** wet town street, neon signs without readable letters, reflections.

## F. Shop pictures (PNG or JPG, 512 x 512, square)

These can have a full background (no magenta). Names: `shop_removeads`, `shop_supporter`, `shop_paint`, `shop_wheels`, `shop_decals`.

```
[STYLE LINE] Square product picture for a game shop, dark navy background with a soft blue glow behind the object, the object large in the centre.
```

- **removeads:** a bold crossed-out speaker or megaphone shape in red, no text.
- **supporter:** a glossy periwinkle blue heart with a small triangle badge on it, gold sparkle.
- **paint:** a spray can and three bright colour splashes (violet, pink, gold).
- **wheels:** a front view of a three-spoke racing wheel with a blue neon rim.
- **decals:** a car side panel with twin gold racing stripes and a small lightning bolt.

## G. Part pictures (PNG or JPG, 512 x 512, square)

Used on the Garage part rows. There are 8 slots: `part_engine`, `part_tires`, `part_suspension`, `part_brakes`, `part_lightweight`, `part_aero`, `part_nitro`, `part_frame`. Any slot without a picture keeps the plain tile.

```
[STYLE LINE] Square product picture of a single car part, dark navy background with a soft blue glow, the part large in the centre, glossy metal with blue light on the edges.
```

Swap in the part: engine = an engine block with a turbo; tires = a performance tyre; suspension = a coilover spring and shock; brakes = a brake disc with a red caliper; lightweight = a carbon fibre panel; aero = a rear wing; nitro = a blue nitro bottle with a valve; frame = a roll cage section.

---

## What to do with the files

1. Generate them and put all the images in one folder or zip.
2. Send them to me. I'll remove the magenta, trim and resize them, rebuild the project zip and tell you what to copy.
3. If you want to do it yourself instead: remove the background, save as PNG (the scenes as JPG), copy into `Assets/_Project/Resources/Art/`, then run **Midnight Legacy > Reimport Art**.

## If something looks wrong

| Problem | Fix |
|---|---|
| Button corners look squashed or stretched | The corners in the picture are too big or too small. Regenerate with "smaller rounded corners" or "larger rounded corners". |
| Buttons look fine but the centre has a pattern that stretches | Regenerate and stress "plain flat centre". |
| Round buttons look the wrong colour | `ui_btn_round` must be grey. If it already has colour, the tint mixes with it. |
| Icon looks tiny inside the button | Regenerate with more space removed around it ("fills most of the frame"). |
| Scenes have text hard to read over them | The bottom of the scene is too bright or busy. Ask for "dark, calm, empty lower third". |
