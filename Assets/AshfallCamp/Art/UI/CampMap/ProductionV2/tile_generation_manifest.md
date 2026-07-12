# Ashfall Camp Production V2 — vivid clean V5 sprite sheets

The clipboard image is the principal style reference. The authoritative source art is sixteen PNG sprite sheets in `Sheets`, each containing exactly six tiles in a 3x2 grid. Their fixed tile order is declared in `CampMapProductionV2Manifest.Sheets`.

## Shared art direction

- Charming hand-painted post-apocalyptic board-game illustration matching the reference.
- Orthographic top-down camera with a slight angle, identical scale, soft light from upper left.
- Bright saturated strategy-game palette: vivid olive and emerald greens, warm golden ochres, rich rust orange, deep teal water, icy blue snow shadows and toxic lime hazards.
- Clean low-medium detail: broad painted terrain texture, one main object cluster, only 2-4 large supporting accents and generous quiet space.
- No dense micro-debris, pebble carpets, repeated grass specks or photorealistic noise.
- No road tiles, asphalt lanes or road markings. The former four road slots are `power_substation`, `collapsed_bunker`, `abandoned_checkpoint` and `ash_cemetery`.
- No people, readable text, labels, numbers, logos, UI or watermark.
- Every tile is surrounded by the same pale beige stone-block border; sheets use flat `#ff00ff` gutters.

## Deterministic processing

`CampMapProductionV2Builder.GenerateTilesFromSheets` detects the magenta gutters, slices every sheet in top-left reading order, resamples each tile to 512x512 and removes residual magenta from exterior corners. It then copies the outer 28-pixel frame band from the first reference tile onto every output. The exported `Tiles/tile_<id>.png` files therefore have pixel-identical edges even though their source art was generated in sixteen separate calls.

Sheets 11 and 12 add twelve future expedition locations ordered from threat 1 (`ruined_homestead`) to threat 12 (`warlord_fortress`). They are included in the palette and preview catalog but intentionally not placed on the current authored 12x12 map.

Sheets 13 and 14 add six vivid grass-covered terrain tiles and six winter terrain tiles. They are available in the palette and brush without altering the authored map.

Sheets 15 and 16 add six grass-biome locations and six winter-biome locations. Together with the five current and twelve future expedition locations, they form a set of 29 marked locations.

`Markers/location_markers_sheet_v6.png` is the authoritative 6x5 marker sheet. The builder extracts its first 29 pins into transparent 256x256 Single Sprites, creates collider-free marker Tile assets, a separate marker palette and a marker preview catalog. Location cards receive the matching pin, while the five locations on the authored map are displayed on `LocationMarkerOverlay` above terrain.

The baked common frame replaces the older `TileFrameOverlay` pass and prevents double borders. Production V2 keeps any legacy overlay empty.

## Unity import

Generated tile PNGs import as Single Sprites, without mipmaps, uncompressed at a runtime maximum of 512x512. Pixels per unit are derived from the 512-pixel source and a five-world-unit map cell. Tile assets are written under `Assets/AshfallCamp/Tiles/ProductionV2`.

The editor-only `AshfallCampProductionV2Brush.asset` appears in the Tile Palette brush selector. Its inspector exposes all 96 tiles in manifest order and paints one selected tile per cell; the builder also activates the brush and the Production V2 palette after rebuilding. Both content and marker palette Grids use 5x5 world-unit cells, matching map cells, so thumbnails and paint positions cannot overlap.

Palette entries are grouped into separate category rows: camp, buildings, current expeditions, future locations, grass content, winter content, remaining biomes, city, buffers and hazards. The production brush follows the same order; the marker palette mirrors the corresponding location groups.
