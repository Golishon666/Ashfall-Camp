using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AshfallCamp.Editor
{
    public static class CampMapImageGenTileBuilder
    {
        public const int TileSize = 512;
        public const int PreviewColumns = 6;
        public const int PrefabCellSize = 104;
        public const int PrefabCellSpacing = 8;
        public const int BiomeMapColumns = 10;
        public const int BiomeMapRows = 8;
        public const int BiomeMapCellSize = 112;
        public const float WorldTileCellSize = 5f;
        public const int WorldTileBorderCropPixels = 16;
        public const int GeneratedSheetEdgeTrimPixels = 14;
        public const string RootFolder = "Assets/AshfallCamp/Art/UI/CampMap";
        public const string PrefabPath = "Assets/AshfallCamp/Prefabs/UI/Main/PF_TestTileMapImageGen.prefab";
        public const string BiomeMapPrefabPath = "Assets/AshfallCamp/Prefabs/UI/Main/PF_CampBiomeTileMap.prefab";
        public const string WorldTileAssetFolder = "Assets/AshfallCamp/Tiles/BiomeWorld";
        public const string WorldTileTextureFolder = RootFolder + "/SeamlessWorldTiles";
        public const string RegeneratedTileRootFolder = RootFolder + "/TestTilesRegenerated";
        public const string RegeneratedTileFolder = RegeneratedTileRootFolder + "/Tiles";
        public const string RegeneratedTileSheetPath = RegeneratedTileRootFolder + "/biome_tileset_magenta_source.png";
        public const string RegeneratedNoChromaSheetPath = RegeneratedTileRootFolder + "/biome_tileset_no_chromakey.png";
        public const string ForestOverlayPath = RootFolder + "/ForestOverlays/forest_canopy_cluster.png";
        public const string ForestOverlayTilePath = WorldTileAssetFolder + "/forest_canopy_overlay.asset";

        private const string SimpleTilesFolder = RootFolder + "/TestTilesImageGenSimple/Tiles";
        private const string SimpleBuildingsFolder = RootFolder + "/TestTilesImageGenSimple/Buildings";
        private const string WinterTilesFolder = RootFolder + "/TestTilesImageGenNuclearWinter/Tiles";
        private const string RadiationTilesFolder = RootFolder + "/TestTilesImageGenRadiation/Tiles";

        private static readonly string[] TilePathsInternal =
        {
            SimpleTilesFolder + "/tile_camp_core.png",
            SimpleTilesFolder + "/tile_camp_buildable.png",
            SimpleTilesFolder + "/tile_camp_barracks.png",
            SimpleTilesFolder + "/tile_forest.png",
            SimpleTilesFolder + "/tile_wheat_field.png",
            SimpleTilesFolder + "/tile_desert.png",
            SimpleTilesFolder + "/tile_abandoned_store.png",
            SimpleTilesFolder + "/tile_dry_suburb.png",
            SimpleTilesFolder + "/tile_ruined_clinic.png",
            SimpleTilesFolder + "/tile_police_outpost.png",
            SimpleTilesFolder + "/tile_mutant_tunnel.png",
            SimpleTilesFolder + "/tile_city.png",
            SimpleTilesFolder + "/tile_green_field.png",
            SimpleTilesFolder + "/tile_ruins_buffer.png",
            SimpleTilesFolder + "/tile_wasteland.png",
            SimpleTilesFolder + "/tile_hazard.png",
            SimpleTilesFolder + "/tile_marsh_water.png",
            SimpleTilesFolder + "/tile_rocky_hills.png",

            WinterTilesFolder + "/tile_camp_core_winter.png",
            WinterTilesFolder + "/tile_camp_buildable_winter.png",
            WinterTilesFolder + "/tile_barracks_winter.png",
            WinterTilesFolder + "/tile_dead_forest_winter.png",
            WinterTilesFolder + "/tile_frost_wheat_field.png",
            WinterTilesFolder + "/tile_ash_wasteland.png",
            WinterTilesFolder + "/tile_abandoned_store_winter.png",
            WinterTilesFolder + "/tile_dry_suburb_winter.png",
            WinterTilesFolder + "/tile_ruined_clinic_winter.png",
            WinterTilesFolder + "/tile_police_outpost_winter.png",
            WinterTilesFolder + "/tile_mutant_tunnel_winter.png",
            WinterTilesFolder + "/tile_city_winter.png",
            WinterTilesFolder + "/tile_green_field_thaw.png",
            WinterTilesFolder + "/tile_ruins_buffer_winter.png",
            WinterTilesFolder + "/tile_ash_wasteland_buffer.png",
            WinterTilesFolder + "/tile_irradiated_hazard_winter.png",
            WinterTilesFolder + "/tile_frozen_marsh.png",
            WinterTilesFolder + "/tile_rocky_hills_winter.png",

            RadiationTilesFolder + "/tile_nuclear_crater.png",
            RadiationTilesFolder + "/tile_irradiated_wasteland.png",
            RadiationTilesFolder + "/tile_radioactive_pool.png",
            RadiationTilesFolder + "/tile_dead_forest_radiation.png",
            RadiationTilesFolder + "/tile_contaminated_ruins.png",
            RadiationTilesFolder + "/tile_waste_barrels_zone.png",
        };

        private static readonly string[] BuildingPathsInternal =
        {
            SimpleBuildingsFolder + "/building_barracks.png",
            SimpleBuildingsFolder + "/building_workshop.png",
            SimpleBuildingsFolder + "/building_water_collector.png",
            SimpleBuildingsFolder + "/building_mushroom_beds.png",
            SimpleBuildingsFolder + "/building_infirmary.png",
            SimpleBuildingsFolder + "/building_radio_tower.png",
        };

        public static IReadOnlyList<string> TilePaths => TilePathsInternal;

        public static IReadOnlyList<string> BuildingPaths => BuildingPathsInternal;

        public static IReadOnlyList<string> PreviewPaths
        {
            get
            {
                var paths = new string[TilePathsInternal.Length + BuildingPathsInternal.Length];
                Array.Copy(TilePathsInternal, paths, TilePathsInternal.Length);
                Array.Copy(BuildingPathsInternal, 0, paths, TilePathsInternal.Length, BuildingPathsInternal.Length);
                return paths;
            }
        }

        // The map is deliberately a fixed grid.  Do not add decorative spacing here: every
        // terrain image occupies precisely one grid cell, so adjacent tiles cannot leave a
        // transparent seam or overlap when the prefab is scaled by its parent canvas.
        private static readonly string[,] BiomeMapPaths =
        {
            { SimpleTilesFolder + "/tile_marsh_water.png", SimpleTilesFolder + "/tile_marsh_water.png", SimpleTilesFolder + "/tile_forest.png", SimpleTilesFolder + "/tile_forest.png", SimpleTilesFolder + "/tile_green_field.png", SimpleTilesFolder + "/tile_green_field.png", SimpleTilesFolder + "/tile_wheat_field.png", SimpleTilesFolder + "/tile_wheat_field.png", SimpleTilesFolder + "/tile_desert.png", SimpleTilesFolder + "/tile_rocky_hills.png" },
            { SimpleTilesFolder + "/tile_marsh_water.png", SimpleTilesFolder + "/tile_forest.png", SimpleTilesFolder + "/tile_forest.png", SimpleTilesFolder + "/tile_green_field.png", SimpleTilesFolder + "/tile_green_field.png", SimpleTilesFolder + "/tile_wheat_field.png", SimpleTilesFolder + "/tile_wheat_field.png", SimpleTilesFolder + "/tile_desert.png", SimpleTilesFolder + "/tile_rocky_hills.png", WinterTilesFolder + "/tile_frozen_marsh.png" },
            { SimpleTilesFolder + "/tile_forest.png", SimpleTilesFolder + "/tile_forest.png", SimpleTilesFolder + "/tile_camp_buildable.png", SimpleTilesFolder + "/tile_camp_core.png", SimpleTilesFolder + "/tile_camp_buildable.png", SimpleTilesFolder + "/tile_wheat_field.png", SimpleTilesFolder + "/tile_ruins_buffer.png", SimpleTilesFolder + "/tile_dry_suburb.png", WinterTilesFolder + "/tile_ash_wasteland_buffer.png", WinterTilesFolder + "/tile_dead_forest_winter.png" },
            { SimpleTilesFolder + "/tile_forest.png", SimpleTilesFolder + "/tile_camp_buildable.png", SimpleTilesFolder + "/tile_camp_barracks.png", SimpleTilesFolder + "/tile_camp_core.png", SimpleTilesFolder + "/tile_abandoned_store.png", SimpleTilesFolder + "/tile_dry_suburb.png", SimpleTilesFolder + "/tile_ruined_clinic.png", SimpleTilesFolder + "/tile_city.png", WinterTilesFolder + "/tile_city_winter.png", WinterTilesFolder + "/tile_dead_forest_winter.png" },
            { SimpleTilesFolder + "/tile_green_field.png", SimpleTilesFolder + "/tile_ruins_buffer.png", SimpleTilesFolder + "/tile_abandoned_store.png", SimpleTilesFolder + "/tile_dry_suburb.png", SimpleTilesFolder + "/tile_city.png", SimpleTilesFolder + "/tile_ruins_buffer.png", SimpleTilesFolder + "/tile_wasteland.png", WinterTilesFolder + "/tile_ash_wasteland.png", WinterTilesFolder + "/tile_irradiated_hazard_winter.png", RadiationTilesFolder + "/tile_dead_forest_radiation.png" },
            { SimpleTilesFolder + "/tile_desert.png", SimpleTilesFolder + "/tile_rocky_hills.png", SimpleTilesFolder + "/tile_wasteland.png", SimpleTilesFolder + "/tile_wasteland.png", SimpleTilesFolder + "/tile_hazard.png", SimpleTilesFolder + "/tile_wasteland.png", WinterTilesFolder + "/tile_ash_wasteland.png", WinterTilesFolder + "/tile_ash_wasteland_buffer.png", RadiationTilesFolder + "/tile_irradiated_wasteland.png", RadiationTilesFolder + "/tile_contaminated_ruins.png" },
            { SimpleTilesFolder + "/tile_rocky_hills.png", WinterTilesFolder + "/tile_frost_wheat_field.png", WinterTilesFolder + "/tile_green_field_thaw.png", WinterTilesFolder + "/tile_dead_forest_winter.png", WinterTilesFolder + "/tile_frozen_marsh.png", WinterTilesFolder + "/tile_ash_wasteland.png", WinterTilesFolder + "/tile_irradiated_hazard_winter.png", RadiationTilesFolder + "/tile_dead_forest_radiation.png", RadiationTilesFolder + "/tile_radioactive_pool.png", RadiationTilesFolder + "/tile_irradiated_wasteland.png" },
            { WinterTilesFolder + "/tile_frozen_marsh.png", WinterTilesFolder + "/tile_rocky_hills_winter.png", WinterTilesFolder + "/tile_ash_wasteland_buffer.png", WinterTilesFolder + "/tile_irradiated_hazard_winter.png", RadiationTilesFolder + "/tile_irradiated_wasteland.png", RadiationTilesFolder + "/tile_radioactive_pool.png", RadiationTilesFolder + "/tile_waste_barrels_zone.png", RadiationTilesFolder + "/tile_contaminated_ruins.png", RadiationTilesFolder + "/tile_nuclear_crater.png", RadiationTilesFolder + "/tile_nuclear_crater.png" },
        };

        private static readonly string[] RegeneratedTileNames =
        {
            "forest", "farmland", "wasteland", "snow_dead_forest", "frozen_marsh", "radioactive_crater",
        };

        [MenuItem("Tools/Ashfall Camp/Camp Map/Build ImageGen Tile Prefab")]
        public static void BuildPrefab()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporters();

            var paths = PreviewPaths;
            var rowCount = Mathf.CeilToInt(paths.Count / (float)PreviewColumns);
            var gridWidth = PreviewColumns * PrefabCellSize + (PreviewColumns - 1) * PrefabCellSpacing;
            var gridHeight = rowCount * PrefabCellSize + (rowCount - 1) * PrefabCellSpacing;

            var root = new GameObject("PF_TestTileMapImageGen", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(gridWidth + 44, gridHeight + 44);

            var backplate = root.GetComponent<Image>();
            backplate.color = new Color32(0x1E, 0x1F, 0x1C, 0xF2);
            backplate.raycastTarget = false;

            var grid = new GameObject("SquareImageGenTileGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(root.transform, false);
            var gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(22, 22);
            gridRect.offsetMax = new Vector2(-22, -22);

            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(PrefabCellSize, PrefabCellSize);
            layout.spacing = new Vector2(PrefabCellSpacing, PrefabCellSpacing);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = PreviewColumns;
            layout.childAlignment = TextAnchor.UpperLeft;

            foreach (var path in paths)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                var cellName = "Asset_" + System.IO.Path.GetFileNameWithoutExtension(path);
                var cell = new GameObject(cellName, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                cell.transform.SetParent(grid.transform, false);

                var rawImage = cell.GetComponent<RawImage>();
                rawImage.texture = texture;
                rawImage.color = Color.white;
                rawImage.raycastTarget = false;

                var rect = cell.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(PrefabCellSize, PrefabCellSize);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            Debug.Log("Built Ashfall Camp ImageGen test tile prefab: " + PrefabPath);
        }

        [MenuItem("Tools/Ashfall Camp/Camp Map/Build Biome Tile Map")]
        public static void BuildBiomeTileMap()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporters();

            var root = new GameObject("PF_CampBiomeTileMap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(BiomeMapColumns * BiomeMapCellSize + 32, BiomeMapRows * BiomeMapCellSize + 98);
            var backplate = root.GetComponent<Image>();
            backplate.color = new Color32(0x1A, 0x1C, 0x1A, 0xFF);
            backplate.raycastTarget = false;

            var gridObject = new GameObject("BiomeTileGrid_NoGaps", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.transform.SetParent(root.transform, false);
            var gridRect = gridObject.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(16, 16);
            gridRect.offsetMax = new Vector2(-16, -82);

            var grid = gridObject.GetComponent<GridLayoutGroup>();
            grid.cellSize = new Vector2(BiomeMapCellSize, BiomeMapCellSize);
            grid.spacing = Vector2.zero;
            grid.padding = new RectOffset(0, 0, 0, 0);
            grid.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            grid.constraintCount = BiomeMapColumns;
            grid.childAlignment = TextAnchor.UpperLeft;

            AddMapText(root.transform, "Title", "ASHFALL CAMP — BIOME EXPEDITION MAP", new Vector2(16, -12), new Vector2(-16, -42), 24, FontStyle.Bold, new Color32(232, 211, 159, 255));
            AddMapText(root.transform, "Legend", "MARSH  •  FOREST  •  FARMLAND  •  CAMP  •  RUINS  •  WASTELAND  •  NUCLEAR WINTER  •  RADIATION ZONE", new Vector2(16, -44), new Vector2(-16, -72), 12, FontStyle.Normal, new Color32(176, 188, 164, 255));

            for (var row = 0; row < BiomeMapRows; row++)
            {
                for (var column = 0; column < BiomeMapColumns; column++)
                {
                    var path = BiomeMapPaths[row, column];
                    var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);
                    if (texture == null)
                    {
                        throw new InvalidOperationException("Missing biome-map tile: " + path);
                    }

                    var cell = new GameObject("Biome_" + GetBiomeName(path) + "_" + row + "_" + column,
                        typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                    cell.transform.SetParent(gridObject.transform, false);
                    var image = cell.GetComponent<RawImage>();
                    image.texture = texture;
                    image.color = Color.white;
                    image.raycastTarget = false;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, BiomeMapPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(BiomeMapPrefabPath, ImportAssetOptions.ForceUpdate);
            AssetDatabase.SaveAssets();
            Debug.Log("Built seamless Ashfall Camp biome tile map: " + BiomeMapPrefabPath);
        }

        [MenuItem("Tools/Ashfall Camp/Camp Map/Apply Biomes To World Tilemap")]
        public static void ApplyBiomesToWorldTilemap()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ConfigureTextureImporters();
            EnsureFolder(WorldTileAssetFolder);
            EnsureFolder(WorldTileTextureFolder);
            EnsureFolder(RegeneratedTileFolder);
            GenerateRegeneratedBiomeTiles();

            var root = GameObject.Find("WorldTileMap");
            if (root == null)
            {
                throw new InvalidOperationException("WorldTileMap was not found in the active scene.");
            }

            var grid = root.GetComponent<Grid>();
            if (grid == null)
            {
                throw new InvalidOperationException("WorldTileMap requires a Grid component.");
            }

            grid.cellSize = new Vector3(WorldTileCellSize, WorldTileCellSize, 1f);
            grid.cellGap = Vector3.zero;

            var renderedMap = root.transform.Find("Tilemap")?.GetComponent<Tilemap>();
            if (renderedMap == null)
            {
                throw new InvalidOperationException("WorldTileMap/Tilemap was not found or has no Tilemap component.");
            }

            root.GetComponent<Tilemap>()?.ClearAllTiles();
            renderedMap.ClearAllTiles();
            var canopyMap = GetOrCreateForestCanopyMap(root.transform);
            canopyMap.ClearAllTiles();
            var canopyTile = GetOrCreateForestCanopyTile();

            var tilesBySourcePath = new Dictionary<string, Tile>();
            for (var row = 0; row < BiomeMapRows; row++)
            {
                for (var column = 0; column < BiomeMapColumns; column++)
                {
                    var sourcePath = BiomeMapPaths[row, column];
                    if (!tilesBySourcePath.TryGetValue(sourcePath, out var tile))
                    {
                        tile = GetOrCreateWorldTile(sourcePath);
                        tilesBySourcePath.Add(sourcePath, tile);
                    }

                    var cell = new Vector3Int(column - BiomeMapColumns / 2, BiomeMapRows / 2 - row - 1, 0);
                    renderedMap.SetTile(cell, tile);
                    if (Path.GetFileNameWithoutExtension(sourcePath).Contains("forest") && (row + column) % 2 == 0)
                    {
                        canopyMap.SetTile(cell, canopyTile);
                    }
                }
            }

            renderedMap.CompressBounds();
            canopyMap.CompressBounds();
            EditorSceneManager.MarkSceneDirty(SceneManager.GetActiveScene());
            EditorSceneManager.SaveScene(SceneManager.GetActiveScene());
            AssetDatabase.SaveAssets();
            Debug.Log("Applied " + (BiomeMapColumns * BiomeMapRows) + " biome tiles to WorldTileMap/Tilemap.");
        }

        [MenuItem("Tools/Ashfall Camp/Camp Map/Prepare Regenerated Tile Palette")]
        public static void PrepareRegeneratedTilePalette()
        {
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            EnsureFolder(WorldTileAssetFolder);
            GenerateRegeneratedBiomeTiles();

            foreach (var tileName in RegeneratedTileNames)
            {
                var sprite = GetRegeneratedTileSprite(tileName);
                var tilePath = WorldTileAssetFolder + "/palette_" + tileName + ".asset";
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
                if (tile == null)
                {
                    tile = ScriptableObject.CreateInstance<Tile>();
                    AssetDatabase.CreateAsset(tile, tilePath);
                }

                tile.sprite = sprite;
                tile.colliderType = Tile.ColliderType.None;
                EditorUtility.SetDirty(tile);
            }

            const string paletteName = "AshfallBiomePalette";
            var palettePath = WorldTileAssetFolder + "/" + paletteName + ".prefab";
            if (AssetDatabase.LoadAssetAtPath<GameObject>(palettePath) != null)
            {
                AssetDatabase.DeleteAsset(palettePath);
            }

            GridPaletteUtility.CreateNewPalette(
                WorldTileAssetFolder,
                paletteName,
                GridLayout.CellLayout.Rectangle,
                GridPalette.CellSizing.Manual,
                Vector3.one,
                GridLayout.CellSwizzle.XYZ);

            var paletteRoot = PrefabUtility.LoadPrefabContents(palettePath);
            try
            {
                var paletteTilemap = paletteRoot.GetComponentInChildren<Tilemap>();
                for (var index = 0; index < RegeneratedTileNames.Length; index++)
                {
                    var paletteTile = AssetDatabase.LoadAssetAtPath<Tile>(
                        WorldTileAssetFolder + "/palette_" + RegeneratedTileNames[index] + ".asset");
                    paletteTilemap.SetTile(new Vector3Int(index % 3, 1 - index / 3, 0), paletteTile);
                }

                paletteTilemap.CompressBounds();
                PrefabUtility.SaveAsPrefabAsset(paletteRoot, palettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(paletteRoot);
            }

            AssetDatabase.SaveAssets();
            Debug.Log("Prepared AshfallBiomePalette with six chroma-free tiles from: " + RegeneratedNoChromaSheetPath);
        }

        private static string GetBiomeName(string path)
        {
            if (path.Contains("Radiation")) return "Radiation";
            if (path.Contains("NuclearWinter")) return "Winter";
            if (path.Contains("forest")) return "Forest";
            if (path.Contains("field")) return "Fields";
            if (path.Contains("camp")) return "Camp";
            if (path.Contains("ruin") || path.Contains("city") || path.Contains("suburb")) return "Ruins";
            return "Wasteland";
        }

        private static void AddMapText(Transform parent, string name, string value, Vector2 topLeft, Vector2 bottomRight, int fontSize, FontStyle style, Color color)
        {
            var label = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Text), typeof(Outline));
            label.transform.SetParent(parent, false);
            var rect = label.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(1, 1);
            rect.offsetMin = new Vector2(topLeft.x, bottomRight.y);
            rect.offsetMax = new Vector2(bottomRight.x, topLeft.y);

            var text = label.GetComponent<Text>();
            text.font = Resources.GetBuiltinResource<Font>("LegacyRuntime.ttf");
            text.text = value;
            text.fontSize = fontSize;
            text.fontStyle = style;
            text.color = color;
            text.alignment = TextAnchor.MiddleCenter;
            text.raycastTarget = false;

            var outline = label.GetComponent<Outline>();
            outline.effectColor = new Color32(20, 23, 20, 230);
            outline.effectDistance = new Vector2(1, -1);
        }

        private static Tile GetOrCreateWorldTile(string spritePath)
        {
            var tileName = GetRegeneratedTileName(spritePath);
            var sprite = GetRegeneratedTileSprite(tileName);
            if (sprite == null)
            {
                throw new InvalidOperationException("Missing regenerated world tile sprite: " + tileName);
            }

            var tilePath = WorldTileAssetFolder + "/" + Path.GetFileNameWithoutExtension(spritePath) + ".asset";
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(tilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, tilePath);
            }

            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static string GetRegeneratedTileName(string originalPath)
        {
            var fileName = Path.GetFileNameWithoutExtension(originalPath).ToLowerInvariant();
            var tileName = originalPath.Contains("Radiation") ? "radioactive_crater"
                : fileName.Contains("frozen") ? "frozen_marsh"
                : originalPath.Contains("NuclearWinter") ? "snow_dead_forest"
                : fileName.Contains("forest") ? "forest"
                : fileName.Contains("field") || fileName.Contains("green") ? "farmland"
                : "wasteland";
            return tileName;
        }

        private static Sprite GetRegeneratedTileSprite(string tileName)
        {
            var sprites = AssetDatabase.LoadAllAssetRepresentationsAtPath(RegeneratedNoChromaSheetPath);
            foreach (var asset in sprites)
            {
                if (asset is Sprite sprite && sprite.name == "tile_" + tileName)
                {
                    return sprite;
                }
            }

            throw new InvalidOperationException("Missing sliced sprite tile_" + tileName + " in " + RegeneratedNoChromaSheetPath);
        }

        private static Tilemap GetOrCreateForestCanopyMap(Transform root)
        {
            var overlay = root.Find("ForestCanopyOverlay");
            if (overlay == null)
            {
                var gameObject = new GameObject("ForestCanopyOverlay", typeof(Tilemap), typeof(TilemapRenderer));
                overlay = gameObject.transform;
                overlay.SetParent(root, false);
            }

            var renderer = overlay.GetComponent<TilemapRenderer>();
            renderer.sortingOrder = 1;
            return overlay.GetComponent<Tilemap>();
        }

        private static Tile GetOrCreateForestCanopyTile()
        {
            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ForestOverlayPath);
            if (sprite == null)
            {
                throw new InvalidOperationException("Missing transparent forest overlay: " + ForestOverlayPath);
            }

            if (AssetImporter.GetAtPath(ForestOverlayPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = 128f;
                importer.alphaIsTransparency = true;
                importer.mipmapEnabled = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
                sprite = AssetDatabase.LoadAssetAtPath<Sprite>(ForestOverlayPath);
            }

            var tile = AssetDatabase.LoadAssetAtPath<Tile>(ForestOverlayTilePath);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, ForestOverlayTilePath);
            }

            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void GenerateRegeneratedBiomeTiles()
        {
            if (AssetImporter.GetAtPath(RegeneratedNoChromaSheetPath) is not TextureImporter importer)
            {
                throw new InvalidOperationException("Missing chroma-free regenerated tileset: " + RegeneratedNoChromaSheetPath);
            }

            var wasReadable = importer.isReadable;
            importer.textureType = TextureImporterType.Default;
            if (!wasReadable)
            {
                importer.isReadable = true;
                importer.SaveAndReimport();
            }

            var sheet = AssetDatabase.LoadAssetAtPath<Texture2D>(RegeneratedNoChromaSheetPath);
            var columns = FindOpaqueSpans(sheet, true);
            var rows = FindOpaqueSpans(sheet, false);
            if (columns.Count != 3 || rows.Count != 2)
            {
                throw new InvalidOperationException("Chroma-free tileset must contain a 3 by 2 grid.");
            }

            rows.Reverse();
            var spriteMetaData = new SpriteMetaData[RegeneratedTileNames.Length];
            var index = 0;
            for (var row = 0; row < rows.Count; row++)
            {
                for (var column = 0; column < columns.Count; column++)
                {
                    spriteMetaData[index] = new SpriteMetaData
                    {
                        name = "tile_" + RegeneratedTileNames[index],
                        rect = new Rect(columns[column].start, rows[row].start, columns[column].length, rows[row].length),
                        pivot = new Vector2(0.5f, 0.5f),
                        alignment = (int)SpriteAlignment.Center,
                    };
                    index++;
                }
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Multiple;
            importer.spritePixelsPerUnit = 90f;
            importer.alphaIsTransparency = true;
            importer.mipmapEnabled = false;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.spritesheet = spriteMetaData;
            importer.isReadable = wasReadable;
            importer.SaveAndReimport();
        }

        private static List<(int start, int length)> FindOpaqueSpans(Texture2D texture, bool horizontal)
        {
            var extent = horizontal ? texture.width : texture.height;
            var crossExtent = horizontal ? texture.height : texture.width;
            var spans = new List<(int start, int length)>();
            var start = -1;
            for (var i = 0; i < extent; i++)
            {
                var hasOpaquePixels = false;
                for (var j = 0; j < crossExtent; j += 4)
                {
                    var pixel = horizontal ? texture.GetPixel(i, j) : texture.GetPixel(j, i);
                    if (pixel.a > 0.05f)
                    {
                        hasOpaquePixels = true;
                        break;
                    }
                }

                if (hasOpaquePixels && start < 0) start = i;
                if (!hasOpaquePixels && start >= 0)
                {
                    spans.Add((start, i - start));
                    start = -1;
                }
            }

            if (start >= 0) spans.Add((start, extent - start));
            return spans;
        }

        private static List<(int start, int length)> FindContentSpans(Texture2D texture, bool horizontal)
        {
            var extent = horizontal ? texture.width : texture.height;
            var crossExtent = horizontal ? texture.height : texture.width;
            var spans = new List<(int start, int length)>();
            var start = -1;
            for (var i = 0; i < extent; i++)
            {
                var hasContent = false;
                for (var j = 0; j < crossExtent; j += 4)
                {
                    var pixel = horizontal ? texture.GetPixel(i, j) : texture.GetPixel(j, i);
                    if (!IsChromaKeyPixel(pixel))
                    {
                        hasContent = true;
                        break;
                    }
                }

                if (hasContent && start < 0) start = i;
                if (!hasContent && start >= 0)
                {
                    spans.Add((start, i - start));
                    start = -1;
                }
            }

            if (start >= 0) spans.Add((start, extent - start));
            return spans;
        }

        private static bool IsChromaKeyPixel(Color pixel)
        {
            return pixel.r > 0.45f && pixel.b > 0.45f && pixel.g < 0.45f
                   && pixel.r - pixel.g > 0.25f && pixel.b - pixel.g > 0.25f;
        }

        private static void SaveResampledTile(Texture2D source, RectInt sourceRect, string outputPath)
        {
            var result = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
            for (var y = 0; y < TileSize; y++)
            {
                for (var x = 0; x < TileSize; x++)
                {
                    var safeWidth = Mathf.Max(1, sourceRect.width - GeneratedSheetEdgeTrimPixels * 2);
                    var safeHeight = Mathf.Max(1, sourceRect.height - GeneratedSheetEdgeTrimPixels * 2);
                    var u = (sourceRect.x + GeneratedSheetEdgeTrimPixels + (x + 0.5f) * safeWidth / TileSize) / source.width;
                    var v = (sourceRect.y + GeneratedSheetEdgeTrimPixels + (y + 0.5f) * safeHeight / TileSize) / source.height;
                    var color = source.GetPixelBilinear(u, v);
                    result.SetPixel(x, y, IsChromaKeyPixel(color) ? new Color32(181, 151, 101, 255) : color);
                }
            }

            result.Apply(false, false);
            File.WriteAllBytes(outputPath, result.EncodeToPNG());
            Object.DestroyImmediate(result);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);
            if (AssetImporter.GetAtPath(outputPath) is TextureImporter importer)
            {
                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = TileSize / WorldTileCellSize;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = false;
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.SaveAndReimport();
            }
        }

        private static Sprite GetOrCreateSeamlessWorldSprite(string sourcePath)
        {
            var sourceImporter = AssetImporter.GetAtPath(sourcePath) as TextureImporter;
            if (sourceImporter == null)
            {
                throw new InvalidOperationException("Missing texture importer: " + sourcePath);
            }

            var wasReadable = sourceImporter.isReadable;
            if (!wasReadable)
            {
                sourceImporter.isReadable = true;
                sourceImporter.SaveAndReimport();
            }

            var source = AssetDatabase.LoadAssetAtPath<Texture2D>(sourcePath);
            if (source == null)
            {
                throw new InvalidOperationException("Missing world tile texture: " + sourcePath);
            }

            var outputPath = WorldTileTextureFolder + "/" + Path.GetFileNameWithoutExtension(sourcePath) + "_seamless.png";
            var result = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
            var sourceSpan = source.width - WorldTileBorderCropPixels * 2;
            for (var y = 0; y < TileSize; y++)
            {
                var v = (WorldTileBorderCropPixels + (y + 0.5f) * sourceSpan / TileSize) / source.height;
                for (var x = 0; x < TileSize; x++)
                {
                    var u = (WorldTileBorderCropPixels + (x + 0.5f) * sourceSpan / TileSize) / source.width;
                    result.SetPixel(x, y, source.GetPixelBilinear(u, v));
                }
            }

            result.Apply(false, false);
            File.WriteAllBytes(outputPath, result.EncodeToPNG());
            Object.DestroyImmediate(result);
            AssetDatabase.ImportAsset(outputPath, ImportAssetOptions.ForceUpdate);

            if (AssetImporter.GetAtPath(outputPath) is TextureImporter outputImporter)
            {
                outputImporter.textureType = TextureImporterType.Sprite;
                outputImporter.spriteImportMode = SpriteImportMode.Single;
                outputImporter.spritePixelsPerUnit = TileSize / WorldTileCellSize;
                outputImporter.mipmapEnabled = false;
                outputImporter.alphaIsTransparency = false;
                outputImporter.textureCompression = TextureImporterCompression.Uncompressed;
                outputImporter.SaveAndReimport();
            }

            if (!wasReadable)
            {
                sourceImporter.isReadable = false;
                sourceImporter.SaveAndReimport();
            }

            return AssetDatabase.LoadAssetAtPath<Sprite>(outputPath);
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (string.IsNullOrEmpty(parent) || string.IsNullOrEmpty(name))
            {
                throw new InvalidOperationException("Invalid asset folder: " + folder);
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private static void ConfigureTextureImporters()
        {
            foreach (var path in PreviewPaths)
            {
                if (AssetImporter.GetAtPath(path) is not TextureImporter importer)
                {
                    continue;
                }

                importer.textureType = TextureImporterType.Sprite;
                importer.spriteImportMode = SpriteImportMode.Single;
                importer.spritePixelsPerUnit = TileSize / WorldTileCellSize;
                importer.mipmapEnabled = false;
                importer.alphaIsTransparency = path.Contains("/Buildings/");
                importer.textureCompression = TextureImporterCompression.Uncompressed;
                importer.maxTextureSize = TileSize;
                importer.SaveAndReimport();
            }
        }
    }
}
