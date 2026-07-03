# Ashfall Camp — Production ImageGen Prompts for Unity UI

> **Project:** Ashfall Camp  
> **Use case:** production prompts only, for modular Unity UI asset generation  
> **Engine target:** Unity  
> **UI stack:** prefab-based UI, TextMeshPro text, modular art assets, clean friendly survival UI  
> **Important:** this document contains **production prompts only**. No full-screen concept prompts.

---

## 1. Core production rules

These prompts are intended for **production-ready modular assets** for Unity integration.

### Rules

- Generate **separate modular assets**, not one baked final UI screenshot.
- Do **not** bake gameplay text into production assets unless the asset is explicitly decorative.
- Use **clean readable shapes**.
- Keep **low visual noise**.
- Avoid micro-detail that will shimmer or become unreadable when scaled down.
- Design assets to be readable at **small and medium UI sizes**.
- Use **TextMeshPro** later for text in Unity.
- Keep cards, panels, buttons, icons, and frames as separate assets.
- Prefer **simple silhouettes** for icons and markers.
- Avoid excessive grime, scratches, and black-heavy grimdark styling.
- UI must feel like **friendly survival management**, not a horror bunker.

### Unity integration rules

Add these ideas to production prompts where needed:

```text
for modular Unity UI integration, isolated component, clean readable shapes, no baked labels, no paragraph text, readable at intended UI size, low-frequency texture detail
```

### Base style block

```text
Ashfall Camp, original post-apocalyptic idle RPG and survival camp management game, friendly survival UI, cozy but practical survivor camp, warm readable management dashboard, soft sand and warm paper panels, sage green success states, faded teal information accents, restrained rust orange CTA, clean card-based interface, low visual noise, clear typography structure, generous spacing, hopeful rebuilding tone, survivors tired but humane, camp made from reclaimed wood and metal, light dust and subtle wear, production-friendly game UI style
```

### Negative prompt block

```text
no grimdark, no horror UI, no black-on-black panels, no excessive dirt, no noisy scratches over text, no skulls everywhere, no aggressive red everywhere, no military bunker interface, no cluttered microtext, no mobile gacha UI, no neon cyberpunk, no unreadable grunge, no harsh contrast, no tiny unreadable details, no overtextured icons, no visual noise, no hyper-detailed micro-ornaments
```

---

## 2. Sizing guide

### Recommended aspect ratios by asset type

| Asset type | Recommended ratio |
|---|---|
| Horizontal top bars | 16:3, 8:1 |
| Side panels | 3:4, 4:5 |
| Cards | 4:3, 5:4, 3:2 |
| Portrait cards | 3:4, 4:5 |
| Buttons | 3:1, 4:1, 5:2 |
| Icons | 1:1 |
| Landscape thumbnails | 4:3 |
| Long trackers / bars | 8:1, 10:1 |
| Overlay strips | 6:1, 8:1 |

### Important readability rule for icons

Always add:

```text
simple readable icon silhouette, readable at 64 to 96 pixels, no micro-details
```

---

## 3. Naming convention for generated assets

Recommended naming for exported images:

- `ui_topbar_resource_plate`
- `ui_button_primary_large`
- `ui_panel_sidebar_status`
- `ui_card_survivor_roster`
- `ui_card_survivor_battle`
- `ui_card_enemy_battle`
- `ui_panel_expedition_progress`
- `ui_panel_expedition_log`
- `ui_panel_zone_minimap`
- `ui_map_marker_hospital`
- `ui_icon_resource_scrap`

---

## 4. Shared UI kit prompts

### 4.1 Top resource bar panel

```text
[BASE_STYLE], modular top resource bar panel for Unity UI, horizontal decorative panel plate with empty slots for 6 to 8 resource counters, no baked text, no numbers, no labels, clean parchment and soft metal frame, readable and low-noise, designed for TextMeshPro overlay, 16:3 composition
```

### 4.2 Bottom navigation panel

```text
[BASE_STYLE], bottom navigation bar panel for Unity UI, horizontal tab bar with five large tab zones, no text, no labels, teal active tab accent, clean readable structure, low-noise survival management style, 16:3 composition
```

### 4.3 Primary button

```text
[BASE_STYLE], primary action button for Unity UI, large horizontal button, soft teal and restrained rust-accented survival UI style, subtle paper texture, readable shape, no text, no icon, clean highlight and pressed state look, 4:1 composition
```

### 4.4 Secondary button

```text
[BASE_STYLE], secondary button for Unity UI, warm paper and soft steel style, low contrast but readable, no text, no icon, clean border, minimal texture, 4:1 composition
```

### 4.5 Danger button

```text
[BASE_STYLE], danger button for Unity UI, muted red action button, calm but noticeable, no text, no icon, post-apocalyptic friendly survival UI style, 4:1 composition
```

### 4.6 Blank modular card

```text
[BASE_STYLE], modular blank UI card for Unity integration, warm paper card with soft border, readable shape, slightly worn edges, no text, no icon, no baked labels, usable for survivor cards, zone cards, event cards and report cards, low visual noise, 4:3 composition
```

### 4.7 Tooltip panel

```text
[BASE_STYLE], compact tooltip panel for Unity UI, small floating panel, warm paper and teal accents, readable, no text, no label, simple border, low noise, 3:2 composition
```

### 4.8 Modal panel

```text
[BASE_STYLE], centered modal panel for a survival management game, layered parchment dialog with subtle shadow, no text, no buttons, no labels, clean readable structure, premium strategy UI, 4:3 composition
```

### 4.9 Toast panel

```text
[BASE_STYLE], compact toast notification panel, horizontal micro-card, soft parchment, subtle success accent, no text, Unity UI modular component, 4:1 composition
```

---

## 5. Camp tab production prompts

### 5.1 Camp overview background

```text
[BASE_STYLE], large illustrated camp overview scene for UI embedding, survivor settlement made from reclaimed wood, metal and tents, workshop, radio tower, water collector, infirmary, warm daylight, slightly hopeful atmosphere, readable structures, moderate detail only, no UI labels, no text, no micro-details, 16:9 composition
```

### 5.2 Building callout card

```text
[BASE_STYLE], building callout UI card set for camp overview, small modular label plates for barracks, workshop, infirmary, radio tower, water collector, empty structure for icon plus title plus level, no baked text, clean readable small-size design, 3:2 composition
```

### 5.3 Alert card

```text
[BASE_STYLE], alert card for camp dashboard, small horizontal UI card with icon slot, title slot, short description slot and CTA slot, no baked text, subtle warning accent, low visual noise, 5:2 composition
```

### 5.4 Camp status sidebar

```text
[BASE_STYLE], left sidebar status panel for strategy dashboard, vertical modular panel with space for 4 status rows, icon slots and progress bars, no baked text, warm paper and sage and teal accents, clean and readable, 3:4 composition
```

---

## 6. Survivors tab production prompts

### 6.1 Survivor roster card

```text
[BASE_STYLE], survivor roster card for Unity UI, medium card with portrait area, role area, status row, health bar, fatigue bar, small trait indicator, no baked text, readable at medium size, low-noise card-based interface, 4:3 composition
```

### 6.2 Large survivor portrait frame

```text
[BASE_STYLE], portrait card frame for a survivor management UI, empty framed portrait area with support for name, role, health, fatigue and assignment status overlays, no text, clean warm paper frame, 3:4 composition
```

### 6.3 Survivor filter bar

```text
[BASE_STYLE], filter and sorting bar for survivor roster UI, horizontal modular UI strip with tabs and dropdown slots, no baked text, clean structure for Unity overlay, low noise, 8:1 composition
```

### 6.4 Survivor empty-state illustration

```text
[BASE_STYLE], empty state illustration for a friendly survival camp roster screen, calm camp clipboard with simple survivor silhouette and tidy paper note style, hopeful tone, no text, for Unity UI, 4:3 composition
```

---

## 7. Survivor detail tab production prompts

### 7.1 Survivor portrait illustration

```text
[BASE_STYLE], survivor portrait illustration for UI, half-body portrait of a post-apocalyptic survivor, humane and grounded, practical scavenger clothing, soft light, not too grim, clean silhouette, readable facial features, suitable for card UI, no background clutter, 3:4 composition
```

### 7.2 Skill row component

```text
[BASE_STYLE], modular skill row component for Unity UI, horizontal card with icon slot, title slot, description slot and progress bar zone, no text, no labels, readable and low noise, warm paper and muted teal and green accents, 5:1 composition
```

### 7.3 Equipment slot card

```text
[BASE_STYLE], equipment slot card for a survivor detail screen, compact modular panel with icon or item preview slot, durability bar slot and item category space, no text, clean small-size readability, no micro-details, 4:1 composition
```

### 7.4 Wounds and status panel

```text
[BASE_STYLE], wounds and status panel for a survivor detail screen, compact vertical panel with health state badge area, wound slots and recovery indicators, no baked text, friendly survival UI, 3:4 composition
```

### 7.5 Action button column

```text
[BASE_STYLE], vertical stack of action button plates for a survivor detail screen, rest, treat, assign, dismiss style structure, no text, no icons baked, muted but readable color hierarchy, low visual clutter, 2:5 composition
```

---

## 8. Missions / expedition select production prompts

### 8.1 Expedition zone card

```text
[BASE_STYLE], modular expedition zone card for Unity UI, horizontal card with landscape thumbnail area, title slot, short description slot, risk badge slot, reward icon strip and recommended power slot, no baked text, readable and clean, 5:2 composition
```

### 8.2 Risk badge set

#### Low risk
```text
[BASE_STYLE], simple low-risk badge for strategy UI, readable pill shape, sage and warm neutral colors, clean icon-ready badge, no text, small-size readable, 3:1 composition
```

#### Medium risk
```text
[BASE_STYLE], simple medium-risk badge for strategy UI, warm amber pill shape, no text, readable and clean, 3:1 composition
```

#### High risk
```text
[BASE_STYLE], simple high-risk badge for strategy UI, muted red pill shape, no text, readable and clean, 3:1 composition
```

#### Extreme risk
```text
[BASE_STYLE], simple extreme-risk badge for strategy UI, muted purple pill shape, no text, readable and clean, 3:1 composition
```

### 8.3 Zone thumbnail variants

#### Abandoned Store
```text
[BASE_STYLE], location thumbnail illustration for a scavenging mission, small ruined roadside market, abandoned store front, useful scrap location, warm overcast light, readable composition, no tiny details, 4:3 composition
```

#### Dry Suburb
```text
[BASE_STYLE], location thumbnail illustration, quiet abandoned suburban street, worn houses, old cars, scavenging opportunity, subtle tension, readable layout, 4:3 composition
```

#### Ruined Clinic
```text
[BASE_STYLE], location thumbnail illustration, post-apocalyptic ruined clinic or hospital entrance, medical supplies may remain, danger implied but not horror, readable medium-detail composition, 4:3 composition
```

#### Police Outpost
```text
[BASE_STYLE], location thumbnail illustration, ruined police outpost, fortified checkpoint, useful gear and ammo potential, medium danger, readable scene, 4:3 composition
```

#### Mutant Tunnel
```text
[BASE_STYLE], location thumbnail illustration, dark tunnel entrance beneath the city, strange glow, hazardous expedition site, readable scene with strong silhouette, not horror-gore, 4:3 composition
```

---

## 9. Map tab production prompts

### 9.1 4K world map base

```text
[BASE_STYLE], illustrated regional world map for a post-apocalyptic camp management game, 4k map base, different biomes visible in one coherent map: residential ruins, city ruins, industrial fringe, forest outskirts, farmland, hazard zone, central camp safe area, roads, rivers, bridges and terrain transitions, designed as a clean readable game map background, no labels, no UI, no text, no route lines baked, 16:9 composition
```

### 9.2 Route overlay support map

```text
[BASE_STYLE], world map overlay concept with readable roads and route structure, safe routes, risky routes and dangerous routes, designed for a strategy game map, clean path readability, no text, no labels, low noise, 16:9 composition
```

### 9.3 Location marker set

```text
[BASE_STYLE], set of modular map node markers for a survival strategy map, readable at small size, clear silhouettes, categories for camp, scavenging site, hospital, police outpost, forest zone, industrial zone, mutant hazard zone, unknown location and event marker, no text, no micro-details, 1:1 composition
```

### 9.4 Selected-location info panel

```text
[BASE_STYLE], right-side location info panel for a strategy world map, vertical panel with large location thumbnail area, description area, recommended power slot, risk slot, rewards slot and two CTA button zones, no baked text, designed for Unity UI overlay, 3:4 composition
```

### 9.5 Legend and filters panel

```text
[BASE_STYLE], left legend and filters panel for a world map screen, vertical modular panel with sections for biome types, danger levels, markers and filter buttons, no text, no labels, clean readable visual grouping, low noise, 3:4 composition
```

### 9.6 Biome clusters

#### Residential ruins
```text
[BASE_STYLE], map-region cluster illustration, residential ruins biome, quiet abandoned homes, broken streets, utility poles, scavenging mood, readable from distance, simplified detail
```

#### City ruins
```text
[BASE_STYLE], map-region cluster illustration, city ruins biome, collapsed urban blocks, dark gray infrastructure, post-apocalyptic but readable, simplified from distance
```

#### Industrial fringe
```text
[BASE_STYLE], map-region cluster illustration, industrial fringe biome, warehouses, towers, fencing, salvage mood, readable silhouettes, simplified detail
```

#### Forest zone
```text
[BASE_STYLE], map-region cluster illustration, forest outskirts biome, pines, logging area, cabins, safe or low-risk exploration feel, readable shapes
```

#### Farmland
```text
[BASE_STYLE], map-region cluster illustration, farmland biome, fields, wind turbines, barns, useful supply zone, readable clean shapes
```

#### Hazard zone
```text
[BASE_STYLE], map-region cluster illustration, hazardous mutant zone, scorched or corrupted terrain, tunnel or crater entry, dangerous but readable, not horror gore, simplified silhouette
```

---

## 10. Expedition Monitor production prompts — UPDATED CARD-BATTLE VERSION

> **This section replaces the older expedition monitor prompt logic.**  
> The combat area is now based on a **card battle layout**:
>
> - player combat cards on the **left**;
> - enemy combat cards on the **right**;
> - enemy cards visually **slide in from the right side**;
> - a long **combat phase / turn progress bar** sits **above the combat cards**;
> - each card has **mini equipment slots** under the portrait;
> - each card also displays **damage, armor, and speed icons/stats**;
> - other expedition information is compact and pushed into upper and side support panels.

### 10.1 Survivor battle card (player card, left side)

```text
[BASE_STYLE], battle card for a survivor in a post-apocalyptic card combat UI, tall portrait card designed for the left side of the battlefield, large survivor portrait, readable name zone, level badge slot, hp bar zone, trait or role tag area, mini equipment slots under the portrait for weapon, armor and gear, bottom stat row with damage icon, armor icon and speed icon, clear numbers area, clean readable structure, no baked text, readable at medium size, low visual noise, production-ready Unity UI card, 3:4 composition
```

### 10.2 Enemy battle card (enemy card, right side)

```text
[BASE_STYLE], enemy battle card for a post-apocalyptic card combat UI, tall hostile character card designed for the right side of the battlefield, large enemy portrait, readable name zone, level badge slot, hp bar zone, enemy type or behavior tag area, mini equipment or mutation slots under the portrait, bottom stat row with damage icon, armor icon and speed icon, clear numbers area, no baked text, readable at medium size, slightly more hostile palette accents, production-ready Unity UI card, 3:4 composition
```

### 10.3 Player card strip / battlefield lane left

```text
[BASE_STYLE], modular battlefield strip panel for player combat cards, designed to anchor 3 to 5 survivor cards on the left side of a card battle screen, clean parchment battlefield base, subtle slot markers, no text, no labels, readable and low-noise, Unity UI integration, 16:5 composition
```

### 10.4 Enemy card strip / battlefield lane right

```text
[BASE_STYLE], modular battlefield strip panel for enemy combat cards, designed to anchor 3 to 5 enemy cards on the right side of a card battle screen, slightly darker hostile lane, subtle entry direction feeling from right side, no text, no labels, readable and low-noise, Unity UI integration, 16:5 composition
```

### 10.5 Combat phase progress bar above cards

```text
[BASE_STYLE], long combat phase progress bar for a card battle UI, horizontal tracker placed above combat cards, three readable sections for player turn, clash or resolution, and enemy turn, with a central crossed-weapons emblem, clean color transitions from green to amber to muted red, no text, no labels, readable and premium strategy UI component, 10:1 composition
```

### 10.6 Turn or phase indicator badge

```text
[BASE_STYLE], turn phase badge for a card combat UI, circular or shield-like badge indicating active combat phase, crossed weapons theme, readable at small-medium size, no text, no labels, clean and low-noise, 1:1 composition
```

### 10.7 Mini equipment slot row for battle cards

```text
[BASE_STYLE], mini equipment slot row for a battle card, compact horizontal strip with 3 to 4 item slot frames under a character portrait, designed for weapon, armor and utility gear, no baked text, readable at small size, low-noise, Unity UI modular component, 5:1 composition
```

### 10.8 Stat icon trio for damage armor speed

```text
[BASE_STYLE], compact stat icon set for battle cards, three matching game UI icons for damage, armor and speed, simple readable silhouettes, usable at very small size, no micro-details, production-ready icon family, 1:1 composition
```

### 10.9 Compact mission header panel

```text
[BASE_STYLE], compact mission header panel for expedition monitor UI, wide panel with mission thumbnail area, mission title space, current encounter slot, short objective slot and compact progress support area, no baked text, readable and low-noise, designed to occupy minimal space above the combat area, 16:5 composition
```

### 10.10 Compact time and status panel

```text
[BASE_STYLE], compact time and expedition status panel for a survival mission UI, vertical or square modular panel with slots for time remaining, threat level, distance or route state and weather, no baked text, no labels, compact readable layout, low noise, 3:4 composition
```

### 10.11 Compact zone minimap panel

```text
[BASE_STYLE], compact zone overview minimap panel for expedition monitor UI, simplified local map with route line and current position area, no text, readable at medium-small size, low visual clutter, designed to sit in the top-right information cluster, 4:3 composition
```

### 10.12 Compact squad summary strip

```text
[BASE_STYLE], compact squad summary strip for expedition monitor UI, horizontal or vertical panel showing 3 to 5 small survivor portraits with hp and fatigue bars, minimal information only, no baked text, designed as a compact support panel above the main battle cards, low visual noise, 5:2 composition
```

### 10.13 Compact combat log panel

```text
[BASE_STYLE], compact combat log panel for a card battle expedition monitor, narrow vertical or square panel with row slots for recent events and a button area to open full log, no baked text, clean paper list structure, designed to occupy minimal side space, readable and low noise, 3:4 composition
```

### 10.14 Actions command bar

```text
[BASE_STYLE], compact actions command bar for expedition monitor UI, horizontal strip with 3 to 5 action button zones such as retreat, use item, inspect enemy, defend or end turn, no baked text, no labels, clean and premium strategy UI, 8:1 composition
```

### 10.15 Energy or action points panel

```text
[BASE_STYLE], energy or action points panel for a card battle UI, compact horizontal or rounded panel with lightning icon area and 4 to 6 point pips, no baked text, readable and clean, 4:1 composition
```

### 10.16 Abilities row

```text
[BASE_STYLE], abilities row for a card battle expedition monitor, horizontal strip with 4 to 6 circular or square ability button frames, clean low-noise structure, no text, no labels, readable at small-medium size, Unity UI modular component, 8:1 composition
```

### 10.17 Loot preview compact row

```text
[BASE_STYLE], compact loot preview row for expedition monitor UI, short horizontal strip with 5 to 8 loot slot frames and optional weight area, no baked text, clean readable structure, low noise, 8:1 composition
```

### 10.18 Retreat and end-turn button pair

```text
[BASE_STYLE], pair of large control buttons for expedition monitor, one retreat style button and one end turn style button, no baked text, strong readable silhouettes, one calm teal survival button and one stronger rust-red turn resolution button, 5:2 composition
```

### 10.19 Enemy slide-in overlay cue

```text
[BASE_STYLE], subtle hostile side-entry overlay or edge cue for a card battle UI, decorative right-edge visual element suggesting enemy cards slide in from the right side, no text, no labels, low-noise, lightweight UI embellishment for Unity animation support, 3:4 composition
```

### 10.20 Battlefield center clash emblem

```text
[BASE_STYLE], central battlefield clash emblem for a card combat UI, decorative crossed-weapons insignia placed between player and enemy card groups, subtle parchment or metal medallion style, no text, readable and not too large, 1:1 composition
```

---

## 11. Workshop tab production prompts

### 11.1 Inventory item tile

```text
[BASE_STYLE], inventory item tile for a survival workshop UI, compact square tile with item preview area, small durability or quantity corner slot, clean border, readable at small size, no text, no labels, 1:1 composition
```

### 11.2 Selected item panel

```text
[BASE_STYLE], selected item detail panel for workshop UI, modular card with large item preview area, stat bar slots, durability slot and short description area, no text, clean Unity-ready component, 4:3 composition
```

### 11.3 Compare stats block

```text
[BASE_STYLE], compact compare stats panel for workshop UI, vertical stat rows with arrow direction indicators and bar zones, no text, no labels, readable low-noise component, 3:4 composition
```

### 11.4 Repair and craft materials panel

```text
[BASE_STYLE], materials requirement panel for a crafting UI, compact modular panel with 3 to 5 ingredient slots, small count area and duration slot, no text, no labels, readable and clean, 4:3 composition
```

### 11.5 Loadout sidebar

```text
[BASE_STYLE], right sidebar panel for selected survivor loadout in workshop screen, portrait area, equipment slots and quick item strip, no baked text, low visual noise, Unity-friendly structure, 3:4 composition
```

### 11.6 Category tabs

```text
[BASE_STYLE], category tab strip for workshop UI, inventory, repair, craft and equip style tabs, no text, no labels, clean large tab shapes, subtle teal selected state, 6:1 composition
```

---

## 12. Radio and recruitment production prompts

### 12.1 Radio intel message card

```text
[BASE_STYLE], radio intel message card for strategy UI, medium horizontal card with signal icon area, message summary area, urgency badge slot and action slot, no text, no labels, clean paper communication style, 5:2 composition
```

### 12.2 Recruitment candidate card

```text
[BASE_STYLE], recruitment candidate card for Unity UI, portrait area, role icon slot, traits summary area, acceptance action slot, no baked text, readable and friendly, 4:5 composition
```

### 12.3 Broadcast control panel

```text
[BASE_STYLE], broadcast control panel for a recruitment screen, modular panel with signal strength area, cost area and action button zone, no baked text, Unity-friendly UI component, 4:3 composition
```

### 12.4 Radio operator illustration

```text
[BASE_STYLE], small embedded illustration for a radio screen, survivor operating a rebuilt radio desk in camp, warm practical communication mood, readable shapes, no text, 4:3 composition
```

---

## 13. Reports production prompts

### 13.1 Report header banner

```text
[BASE_STYLE], report header banner for a strategy results screen, wide modular panel with outcome badge slot, decorative stamped-paper look, no text, no labels, readable and clean, 16:5 composition
```

### 13.2 Resource gain row

```text
[BASE_STYLE], resource gain row for a summary report UI, compact row card with icon slot, amount slot and short note space, no text, Unity-ready component, 5:1 composition
```

### 13.3 Expedition result row

```text
[BASE_STYLE], expedition result row for a summary report UI, thumbnail slot, outcome badge area, squad size slot and reward strip zone, no baked text, clean low-noise card, 5:2 composition
```

### 13.4 Camp notes panel

```text
[BASE_STYLE], camp notes panel for an offline report screen, paper note-style modular panel, subtle handwritten-notebook feeling, but no baked text, warm and hopeful survival theme, 4:3 composition
```

---

## 14. Settings and utility production prompts

### 14.1 Generic settings row

```text
[BASE_STYLE], settings row component for a survival management UI, horizontal row with icon slot, title slot, toggle or slider zone and description support area, no text, no labels, clean readable structure, 6:1 composition
```

### 14.2 Toggle switch

```text
[BASE_STYLE], toggle switch for Unity UI, clean friendly survival style, readable active and inactive states, no text, 3:1 composition
```

### 14.3 Slider plate

```text
[BASE_STYLE], slider plate for Unity UI, low-noise paper and metal slider track with handle, no text, no labels, readable, 5:1 composition
```

---

## 15. Icon family prompts

### 15.1 Generic icon family prompt

```text
[BASE_STYLE], set of clean game UI icons for Unity integration, simple readable icon silhouettes, low-frequency detail, soft worn survival style, readable at 64 to 96 pixels, no tiny scratches, no unnecessary micro-detail, premium strategy icon set, consistent lighting and outline treatment
```

### 15.2 Resource icon prompt template

```text
[BASE_STYLE], clean game UI resource icon, [RESOURCE_NAME], readable at small size, simple silhouette, no micro-details, isolated icon, 1:1 composition
```

Use for:
- scrap metal
- food
- water
- medicine
- weapon parts
- electronics
- armor parts
- fuel
- radio intel
- blueprints

### 15.3 Equipment icon prompt template

```text
[BASE_STYLE], clean game UI equipment icon, [ITEM_NAME], readable at small size, simple silhouette, no micro-details, isolated item icon, 1:1 composition
```

Use for:
- rifle
- pistol
- shotgun
- machete
- hatchet
- medkit
- backpack
- armor vest
- canteen
- ammo box
- duct tape
- radio
- flashlight

### 15.4 Status icon prompt template

```text
[BASE_STYLE], status icon for a strategy game UI, [STATUS_NAME], readable at small size, simple and clear silhouette, isolated icon, 1:1 composition
```

Use for:
- healthy
- wounded
- fatigue
- morale
- safe
- danger
- idle
- assigned
- scouting
- healing
- level up

### 15.5 Map marker prompt template

```text
[BASE_STYLE], map marker icon for a survival strategy world map, [MARKER_NAME], readable small-size silhouette, no micro-details, isolated icon, 1:1 composition
```

---

## 16. Unity import settings recommendations

These are not image prompts, but recommended post-processing rules in Unity.

### Panels / plates
- Sprite Mode: Single
- Mesh Type: Full Rect
- Compression: High Quality
- Filter Mode: Bilinear
- Mip Maps: Off
- Use 9-slice where possible

### Icons
- Sprite Mode: Single or Multiple
- Compression: High Quality
- Max size: 256 or 512
- Mip Maps: Off
- Filter Mode: Bilinear or Point depending on style consistency

### Large map base
- Max Size: 4096
- Compression: High Quality
- Use as large background image
- Consider slicing overlays separately

### Card assets
- Keep separate front frame, portrait, equipment row, and bottom stats where possible
- Avoid baking long text
- Prefer separate icon sprites for damage, armor, speed

---

## 17. QA checklist for generated assets

Reject or regenerate an asset if:

- the texture is too noisy;
- small elements are unreadable;
- icon silhouettes are muddy;
- too much text is baked into the asset;
- the palette becomes too dark or grim;
- important UI affordances are unclear;
- borders are over-decorated;
- micro-scratches appear over readable zones;
- the component does not look modular enough for Unity;
- card details become unreadable when scaled down;
- damage / armor / speed areas are unclear on battle cards;
- equipment mini-slots under cards are not obvious;
- the expedition monitor layout does not support left player cards and right enemy cards.

---

## 18. Deletion / replacement note

This document supersedes the older expedition monitor production prompts.  
Use **only this updated version** going forward, especially for the card-battle expedition monitor.

---

## 19. Quick placeholder reminder

When using prompts from this document, replace:

- `[BASE_STYLE]` with the full base style block
- `[RESOURCE_NAME]` with the target resource
- `[ITEM_NAME]` with the target item
- `[STATUS_NAME]` with the target status
- `[MARKER_NAME]` with the target map marker
