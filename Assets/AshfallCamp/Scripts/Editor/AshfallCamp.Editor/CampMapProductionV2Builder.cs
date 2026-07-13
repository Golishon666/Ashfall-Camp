using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;
using Object = UnityEngine.Object;

namespace AshfallCamp.Editor
{
    public static class CampMapProductionV2Builder
    {
        public const int CatalogColumns = 10;
        public const int CatalogCellSize = 104;
        public const int CatalogCellSpacing = 8;
        public const int SharedFrameBand = 28;
        public const int MarkerColumns = 6;
        public const int MarkerRows = 5;

        [MenuItem("Tools/Ashfall Camp/Camp Map/Production V2/Build All")]
        public static void BuildAll()
        {
            EnsureOutputFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            GenerateTilesFromSheets();
            GenerateMarkersFromSheet();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ValidateSourceFiles();
            ConfigureImporters();
            BuildTileAssets();
            BuildMarkerTileAssets();
            BuildBrush();
            BuildPalette();
            BuildMarkerPalette();
            ActivateBrush();
            BuildCatalogPrefab();
            BuildMarkerCatalogPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("Built Ashfall Camp Production V2 assets: sixteen 3x2 source sheets, one standalone map-border tile, 97 location/terrain tiles, 29 location markers, two palettes, and catalog previews. The manually authored Tilemap was not modified.");
        }

        [MenuItem("Tools/Ashfall Camp/Camp Map/Production V2/Build Assets And Palette")]
        public static void BuildAssetsAndPalette()
        {
            EnsureOutputFolders();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            GenerateTilesFromSheets();
            GenerateMarkersFromSheet();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            ValidateSourceFiles();
            ConfigureImporters();
            BuildTileAssets();
            BuildMarkerTileAssets();
            BuildBrush();
            BuildPalette();
            BuildMarkerPalette();
            ActivateBrush();
            BuildCatalogPrefab();
            BuildMarkerCatalogPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
        }

        [MenuItem("Tools/Ashfall Camp/Camp Map/Production V2/Rebuild Sorted Palettes")]
        public static void RebuildSortedPalettes()
        {
            EnsureOutputFolders();
            BuildBrush();
            BuildPalette();
            BuildMarkerPalette();
            ActivateBrush();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceUpdate);
            Debug.Log("Rebuilt sorted Production V2 content and marker palettes.");
        }

        [MenuItem("Tools/Ashfall Camp/Camp Map/Production V2/Activate Tile Brush")]
        public static void ActivateBrush()
        {
            var brush = AssetDatabase.LoadAssetAtPath<CampMapProductionV2Brush>(CampMapProductionV2Manifest.BrushPath);
            if (brush == null)
            {
                throw new InvalidOperationException("Missing Production V2 brush asset: " + CampMapProductionV2Manifest.BrushPath);
            }

            GridPaintingState.gridBrush = brush;
            var palette = AssetDatabase.LoadAssetAtPath<GameObject>(CampMapProductionV2Manifest.PalettePath);
            if (palette != null)
            {
                GridPaintingState.palette = palette;
            }

            Selection.activeObject = brush;
            EditorGUIUtility.PingObject(brush);
        }

        public static void ValidateSourceFiles()
        {
            if (CampMapProductionV2Manifest.Specs.Count != 97)
            {
                throw new InvalidOperationException("Production V2 manifest must contain exactly 97 tiles.");
            }

            if (CampMapProductionV2Manifest.Sheets.Count != 16)
            {
                throw new InvalidOperationException("Production V2 must contain exactly sixteen 3x2 sprite sheets.");
            }

            var sheetIds = CampMapProductionV2Manifest.Sheets.SelectMany(sheet => sheet.TileIds).ToArray();
            if (sheetIds.Length != 96 || sheetIds.Distinct(StringComparer.Ordinal).Count() != 96)
            {
                throw new InvalidOperationException("Production V2 sheets must map exactly 96 unique tile ids.");
            }

            if (CampMapProductionV2Manifest.Markers.Count != 29
                || CampMapProductionV2Manifest.Markers.Select(marker => marker.LocationId).Distinct(StringComparer.Ordinal).Count() != 29)
            {
                throw new InvalidOperationException("Production V2 must define exactly 29 unique location markers.");
            }

            if (!File.Exists(CampMapProductionV2Manifest.MarkerSheetPath))
            {
                throw new FileNotFoundException("Missing Production V2 marker sprite sheet.", CampMapProductionV2Manifest.MarkerSheetPath);
            }

            foreach (var sheet in CampMapProductionV2Manifest.Sheets)
            {
                if (sheet.TileIds.Count != 6)
                {
                    throw new InvalidOperationException("Each Production V2 sheet must map exactly six tiles: " + sheet.TexturePath);
                }

                if (!File.Exists(sheet.TexturePath))
                {
                    throw new FileNotFoundException("Missing Production V2 sprite sheet.", sheet.TexturePath);
                }
            }

            var duplicate = CampMapProductionV2Manifest.Specs
                .GroupBy(spec => spec.Id, StringComparer.Ordinal)
                .FirstOrDefault(group => group.Count() > 1);
            if (duplicate != null)
            {
                throw new InvalidOperationException("Duplicate Production V2 tile id: " + duplicate.Key);
            }

            foreach (var spec in CampMapProductionV2Manifest.Specs)
            {
                if (!File.Exists(spec.TexturePath))
                {
                    throw new FileNotFoundException("Missing Production V2 tile texture.", spec.TexturePath);
                }
            }

            foreach (var marker in CampMapProductionV2Manifest.Markers)
            {
                if (!File.Exists(marker.TexturePath))
                {
                    throw new FileNotFoundException("Missing Production V2 marker texture.", marker.TexturePath);
                }
            }

            var generatedSheetIds = new HashSet<string>(
                CampMapProductionV2Manifest.Specs
                    .Where(spec => !string.Equals(spec.Category, "map_border", StringComparison.Ordinal))
                    .Select(spec => spec.Id),
                StringComparer.Ordinal);
            if (!generatedSheetIds.SetEquals(sheetIds))
            {
                throw new InvalidOperationException("Production V2 sheet ids do not match the generated tile manifest.");
            }
        }

        public static void GenerateTilesFromSheets()
        {
            Color32[] sharedFramePixels = null;

            foreach (var sheet in CampMapProductionV2Manifest.Sheets)
            {
                if (!File.Exists(sheet.TexturePath))
                {
                    throw new FileNotFoundException("Missing Production V2 sprite sheet.", sheet.TexturePath);
                }

                var source = LoadPng(sheet.TexturePath);
                try
                {
                    var sourcePixels = source.GetPixels32();
                    var columns = DetectSpans(sourcePixels, source.width, source.height, true);
                    var rows = DetectSpans(sourcePixels, source.width, source.height, false);
                    if (columns.Count != 3 || rows.Count != 2)
                    {
                        throw new InvalidOperationException(
                            $"Expected a 3x2 magenta-separated sprite sheet at {sheet.TexturePath}, detected {columns.Count}x{rows.Count}.");
                    }

                    for (var tileIndex = 0; tileIndex < sheet.TileIds.Count; tileIndex++)
                    {
                        var column = tileIndex % 3;
                        var topRow = tileIndex / 3;
                        var sourceRow = rows[rows.Count - 1 - topRow];
                        var pixels = ResampleTile(sourcePixels, source.width, columns[column], sourceRow);

                        if (sharedFramePixels == null)
                        {
                            sharedFramePixels = (Color32[])pixels.Clone();
                        }

                        ApplyPixelIdenticalFrame(pixels, sharedFramePixels);
                        SaveTilePng(CampMapProductionV2Manifest.GetTexturePath(sheet.TileIds[tileIndex]), pixels);
                    }
                }
                finally
                {
                    Object.DestroyImmediate(source);
                }
            }
        }

        public static void GenerateMarkersFromSheet()
        {
            if (!File.Exists(CampMapProductionV2Manifest.MarkerSheetPath))
            {
                throw new FileNotFoundException("Missing Production V2 marker sprite sheet.", CampMapProductionV2Manifest.MarkerSheetPath);
            }

            var source = LoadPng(CampMapProductionV2Manifest.MarkerSheetPath);
            try
            {
                var sourcePixels = source.GetPixels32();
                for (var index = 0; index < CampMapProductionV2Manifest.Markers.Count; index++)
                {
                    var column = index % MarkerColumns;
                    var topRow = index / MarkerColumns;
                    var xMin = Mathf.FloorToInt(column * source.width / (float)MarkerColumns);
                    var xMaxExclusive = Mathf.FloorToInt((column + 1) * source.width / (float)MarkerColumns);
                    var yMin = source.height - Mathf.FloorToInt((topRow + 1) * source.height / (float)MarkerRows);
                    var yMaxExclusive = source.height - Mathf.FloorToInt(topRow * source.height / (float)MarkerRows);
                    var pixels = ExtractMarker(
                        sourcePixels,
                        source.width,
                        new PixelSpan(xMin, xMaxExclusive - 1),
                        new PixelSpan(yMin, yMaxExclusive - 1));
                    SaveMarkerPng(CampMapProductionV2Manifest.Markers[index].TexturePath, pixels);
                }
            }
            finally
            {
                Object.DestroyImmediate(source);
            }
        }

        private static Texture2D LoadPng(string path)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false, true);
            if (!ImageConversion.LoadImage(texture, File.ReadAllBytes(path), false))
            {
                Object.DestroyImmediate(texture);
                throw new InvalidOperationException("Unable to decode sprite sheet: " + path);
            }

            return texture;
        }

        private static List<PixelSpan> DetectSpans(Color32[] pixels, int width, int height, bool columns)
        {
            var length = columns ? width : height;
            var crossLength = columns ? height : width;
            var active = new bool[length];

            for (var axis = 0; axis < length; axis++)
            {
                var nonMagenta = 0;
                for (var cross = 0; cross < crossLength; cross++)
                {
                    var x = columns ? axis : cross;
                    var y = columns ? cross : axis;
                    if (!IsSeparatorMagenta(pixels[y * width + x]))
                    {
                        nonMagenta++;
                    }
                }

                active[axis] = nonMagenta > crossLength / 5;
            }

            var spans = new List<PixelSpan>();
            var start = -1;
            for (var axis = 0; axis <= length; axis++)
            {
                var isActive = axis < length && active[axis];
                if (isActive && start < 0)
                {
                    start = axis;
                }
                else if (!isActive && start >= 0)
                {
                    spans.Add(new PixelSpan(start, axis - 1));
                    start = -1;
                }
            }

            return spans;
        }

        private static Color32[] ResampleTile(Color32[] source, int sourceWidth, PixelSpan xSpan, PixelSpan ySpan)
        {
            var size = CampMapProductionV2Manifest.RuntimeTextureSize;
            var output = new Color32[size * size];
            for (var y = 0; y < size; y++)
            {
                var sourceY = ySpan.Start + Mathf.Min(ySpan.Length - 1, Mathf.FloorToInt((y + 0.5f) * ySpan.Length / size));
                for (var x = 0; x < size; x++)
                {
                    var sourceX = xSpan.Start + Mathf.Min(xSpan.Length - 1, Mathf.FloorToInt((x + 0.5f) * xSpan.Length / size));
                    var pixel = source[sourceY * sourceWidth + sourceX];
                    if (IsMagenta(pixel))
                    {
                        pixel = new Color32(0, 0, 0, 0);
                    }

                    output[y * size + x] = pixel;
                }
            }

            return output;
        }

        private static Color32[] ExtractMarker(Color32[] source, int sourceWidth, PixelSpan cellX, PixelSpan cellY)
        {
            var minX = cellX.End;
            var maxX = cellX.Start;
            var minY = cellY.End;
            var maxY = cellY.Start;
            for (var y = cellY.Start; y <= cellY.End; y++)
            {
                for (var x = cellX.Start; x <= cellX.End; x++)
                {
                    if (IsSeparatorMagenta(source[y * sourceWidth + x]))
                    {
                        continue;
                    }

                    minX = Mathf.Min(minX, x);
                    maxX = Mathf.Max(maxX, x);
                    minY = Mathf.Min(minY, y);
                    maxY = Mathf.Max(maxY, y);
                }
            }

            if (maxX < minX || maxY < minY)
            {
                throw new InvalidOperationException("A marker cell in the Production V2 sheet is empty.");
            }

            minX = Mathf.Max(cellX.Start, minX - 3);
            maxX = Mathf.Min(cellX.End, maxX + 3);
            minY = Mathf.Max(cellY.Start, minY - 3);
            maxY = Mathf.Min(cellY.End, maxY + 3);

            var sourceWidthInCell = maxX - minX + 1;
            var sourceHeightInCell = maxY - minY + 1;
            var size = CampMapProductionV2Manifest.MarkerTextureSize;
            const int contentSize = 224;
            var scale = Mathf.Min(contentSize / (float)sourceWidthInCell, contentSize / (float)sourceHeightInCell);
            var destinationWidth = Mathf.Max(1, Mathf.RoundToInt(sourceWidthInCell * scale));
            var destinationHeight = Mathf.Max(1, Mathf.RoundToInt(sourceHeightInCell * scale));
            var destinationX = (size - destinationWidth) / 2;
            var destinationY = (size - destinationHeight) / 2;
            var output = new Color32[size * size];

            for (var y = 0; y < destinationHeight; y++)
            {
                var sourceY = minY + Mathf.Min(sourceHeightInCell - 1, Mathf.FloorToInt((y + 0.5f) * sourceHeightInCell / destinationHeight));
                for (var x = 0; x < destinationWidth; x++)
                {
                    var sourceX = minX + Mathf.Min(sourceWidthInCell - 1, Mathf.FloorToInt((x + 0.5f) * sourceWidthInCell / destinationWidth));
                    var pixel = source[sourceY * sourceWidth + sourceX];
                    if (IsSeparatorMagenta(pixel))
                    {
                        pixel = new Color32(0, 0, 0, 0);
                    }

                    output[(destinationY + y) * size + destinationX + x] = pixel;
                }
            }

            return output;
        }

        private static void ApplyPixelIdenticalFrame(Color32[] pixels, Color32[] frameSource)
        {
            var size = CampMapProductionV2Manifest.RuntimeTextureSize;
            for (var y = 0; y < size; y++)
            {
                for (var x = 0; x < size; x++)
                {
                    if (x < SharedFrameBand || y < SharedFrameBand || x >= size - SharedFrameBand || y >= size - SharedFrameBand)
                    {
                        pixels[y * size + x] = frameSource[y * size + x];
                    }
                }
            }
        }

        private static void SaveTilePng(string path, Color32[] pixels)
        {
            var size = CampMapProductionV2Manifest.RuntimeTextureSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static void SaveMarkerPng(string path, Color32[] pixels)
        {
            var size = CampMapProductionV2Manifest.MarkerTextureSize;
            var texture = new Texture2D(size, size, TextureFormat.RGBA32, false, true);
            try
            {
                texture.SetPixels32(pixels);
                texture.Apply(false, false);
                File.WriteAllBytes(path, texture.EncodeToPNG());
            }
            finally
            {
                Object.DestroyImmediate(texture);
            }
        }

        private static bool IsMagenta(Color32 pixel)
        {
            return pixel.r > 210 && pixel.b > 180 && pixel.g < 80
                   && pixel.r - pixel.g > 150 && pixel.b - pixel.g > 120;
        }

        private static bool IsSeparatorMagenta(Color32 pixel)
        {
            return pixel.r > 115 && pixel.b > 115 && pixel.g < 115
                   && pixel.r - pixel.g > 65 && pixel.b - pixel.g > 65;
        }

        private static void EnsureOutputFolders()
        {
            EnsureFolder(CampMapProductionV2Manifest.ArtRoot);
            EnsureFolder(CampMapProductionV2Manifest.SheetFolder);
            EnsureFolder(CampMapProductionV2Manifest.TileTextureFolder);
            EnsureFolder(CampMapProductionV2Manifest.PreviewFolder);
            EnsureFolder(CampMapProductionV2Manifest.MarkerRoot);
            EnsureFolder(CampMapProductionV2Manifest.MarkerTextureFolder);
            EnsureFolder(CampMapProductionV2Manifest.TileAssetFolder);
            EnsureFolder(CampMapProductionV2Manifest.MarkerTileAssetFolder);
            EnsureFolder(CampMapProductionV2Manifest.BrushFolder);
        }

        private static void ConfigureImporters()
        {
            foreach (var spec in CampMapProductionV2Manifest.Specs)
            {
                ConfigureImporter(spec.TexturePath, true);
            }

            foreach (var marker in CampMapProductionV2Manifest.Markers)
            {
                ConfigureMarkerImporter(marker.TexturePath);
            }
        }

        private static void ConfigureImporter(string assetPath, bool alpha)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                throw new InvalidOperationException("Texture importer was not found for " + assetPath);
            }

            importer.GetSourceTextureWidthAndHeight(out var sourceWidth, out var sourceHeight);
            if (sourceWidth != sourceHeight)
            {
                throw new InvalidOperationException("Production V2 textures must be square: " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
#pragma warning disable CS0618
            importer.spritesheet = Array.Empty<SpriteMetaData>();
#pragma warning restore CS0618
            importer.spritePixelsPerUnit = sourceWidth / CampMapProductionV2Manifest.WorldTileCellSize;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = alpha;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = CampMapProductionV2Manifest.RuntimeTextureSize;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static void ConfigureMarkerImporter(string assetPath)
        {
            if (AssetImporter.GetAtPath(assetPath) is not TextureImporter importer)
            {
                throw new InvalidOperationException("Marker texture importer was not found for " + assetPath);
            }

            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
#pragma warning disable CS0618
            importer.spritesheet = Array.Empty<SpriteMetaData>();
#pragma warning restore CS0618
            importer.spritePixelsPerUnit = CampMapProductionV2Manifest.MarkerTextureSize / 2f;
            importer.mipmapEnabled = false;
            importer.alphaIsTransparency = true;
            importer.textureCompression = TextureImporterCompression.Uncompressed;
            importer.maxTextureSize = CampMapProductionV2Manifest.MarkerTextureSize;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.SaveAndReimport();
        }

        private static void BuildTileAssets()
        {
            foreach (var spec in CampMapProductionV2Manifest.Specs)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.TexturePath);
                if (sprite == null)
                {
                    throw new InvalidOperationException("Missing imported sprite: " + spec.TexturePath);
                }

                CreateOrUpdateTile(spec.TileAssetPath, sprite);
            }

        }

        private static void BuildMarkerTileAssets()
        {
            foreach (var marker in CampMapProductionV2Manifest.Markers)
            {
                var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(marker.TexturePath);
                if (sprite == null)
                {
                    throw new InvalidOperationException("Missing imported marker sprite: " + marker.TexturePath);
                }

                CreateOrUpdateTile(marker.TileAssetPath, sprite);
            }
        }

        private static Tile CreateOrUpdateTile(string path, Sprite sprite)
        {
            var tile = AssetDatabase.LoadAssetAtPath<Tile>(path);
            if (tile == null)
            {
                tile = ScriptableObject.CreateInstance<Tile>();
                AssetDatabase.CreateAsset(tile, path);
            }

            tile.sprite = sprite;
            tile.colliderType = Tile.ColliderType.None;
            tile.flags = TileFlags.LockAll;
            EditorUtility.SetDirty(tile);
            return tile;
        }

        private static void BuildBrush()
        {
            var brush = AssetDatabase.LoadAssetAtPath<CampMapProductionV2Brush>(CampMapProductionV2Manifest.BrushPath);
            if (brush == null)
            {
                brush = ScriptableObject.CreateInstance<CampMapProductionV2Brush>();
                AssetDatabase.CreateAsset(brush, CampMapProductionV2Manifest.BrushPath);
            }

            brush.Configure(CampMapProductionV2Manifest.PaletteSpecs);
            EditorUtility.SetDirty(brush);
        }

        private static void BuildPalette()
        {
            const string paletteName = "AshfallCampProductionV2SortedPalette";
            var paletteCellSize = new Vector3(
                CampMapProductionV2Manifest.WorldTileCellSize,
                CampMapProductionV2Manifest.WorldTileCellSize,
                1f);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CampMapProductionV2Manifest.PalettePath) == null)
            {
                GridPaletteUtility.CreateNewPalette(
                    CampMapProductionV2Manifest.TileAssetFolder,
                    paletteName,
                    GridLayout.CellLayout.Rectangle,
                    GridPalette.CellSizing.Manual,
                    paletteCellSize,
                    GridLayout.CellSwizzle.XYZ);
            }

            var root = PrefabUtility.LoadPrefabContents(CampMapProductionV2Manifest.PalettePath);
            try
            {
                var paletteGrid = root.GetComponent<Grid>();
                if (paletteGrid == null)
                {
                    throw new InvalidOperationException("Production V2 palette requires a Grid component.");
                }

                paletteGrid.cellSize = paletteCellSize;
                paletteGrid.cellGap = Vector3.zero;
                var tilemap = RecreatePaletteTilemap(root.transform, "ContentTiles");
                var placements = BuildCategoryPlacements(
                    CampMapProductionV2Manifest.PaletteSpecs,
                    spec => spec.Category,
                    CatalogColumns);
                var rows = placements.Max(placement => placement.Row) + 1;
                tilemap.origin = Vector3Int.zero;
                tilemap.size = new Vector3Int(CatalogColumns, rows, 1);
                tilemap.ResizeBounds();
                foreach (var placement in placements)
                {
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(placement.Item.TileAssetPath);
                    if (tile == null)
                    {
                        throw new InvalidOperationException("Missing palette Tile asset: " + placement.Item.TileAssetPath);
                    }

                    tilemap.SetTile(new Vector3Int(placement.Column, rows - 1 - placement.Row, 0), tile);
                }

                tilemap.CompressBounds();
                var placedTileCount = tilemap.GetTilesBlock(tilemap.cellBounds).Count(tile => tile != null);
                if (placedTileCount != CampMapProductionV2Manifest.PaletteSpecs.Count)
                {
                    throw new InvalidOperationException(
                        $"Production palette placed {placedTileCount} of {CampMapProductionV2Manifest.PaletteSpecs.Count} tiles.");
                }
                EditorUtility.SetDirty(tilemap);
                EditorUtility.SetDirty(paletteGrid);
                EditorUtility.SetDirty(root);
                PrefabUtility.SaveAsPrefabAsset(root, CampMapProductionV2Manifest.PalettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildMarkerPalette()
        {
            const string paletteName = "AshfallCampLocationMarkerSortedPalette";
            var paletteCellSize = new Vector3(
                CampMapProductionV2Manifest.WorldTileCellSize,
                CampMapProductionV2Manifest.WorldTileCellSize,
                1f);
            if (AssetDatabase.LoadAssetAtPath<GameObject>(CampMapProductionV2Manifest.MarkerPalettePath) == null)
            {
                GridPaletteUtility.CreateNewPalette(
                    CampMapProductionV2Manifest.MarkerTileAssetFolder,
                    paletteName,
                    GridLayout.CellLayout.Rectangle,
                    GridPalette.CellSizing.Manual,
                    paletteCellSize,
                    GridLayout.CellSwizzle.XYZ);
            }

            var root = PrefabUtility.LoadPrefabContents(CampMapProductionV2Manifest.MarkerPalettePath);
            try
            {
                var paletteGrid = root.GetComponent<Grid>();
                paletteGrid.cellSize = paletteCellSize;
                paletteGrid.cellGap = Vector3.zero;
                var tilemap = RecreatePaletteTilemap(root.transform, "LocationMarkers");
                var placements = BuildCategoryPlacements(
                    CampMapProductionV2Manifest.PaletteMarkers,
                    marker => CampMapProductionV2Manifest.GetSpec(marker.LocationId).Category,
                    MarkerColumns);
                var rows = placements.Max(placement => placement.Row) + 1;
                tilemap.origin = Vector3Int.zero;
                tilemap.size = new Vector3Int(MarkerColumns, rows, 1);
                tilemap.ResizeBounds();
                foreach (var placement in placements)
                {
                    var tile = AssetDatabase.LoadAssetAtPath<Tile>(placement.Item.TileAssetPath);
                    if (tile == null)
                    {
                        throw new InvalidOperationException("Missing marker palette Tile asset: " + placement.Item.TileAssetPath);
                    }

                    tilemap.SetTile(new Vector3Int(placement.Column, rows - 1 - placement.Row, 0), tile);
                }

                tilemap.CompressBounds();
                EditorUtility.SetDirty(tilemap);
                EditorUtility.SetDirty(paletteGrid);
                EditorUtility.SetDirty(root);
                PrefabUtility.SaveAsPrefabAsset(root, CampMapProductionV2Manifest.MarkerPalettePath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildCatalogPrefab()
        {
            var rows = Mathf.CeilToInt(CampMapProductionV2Manifest.Specs.Count / (float)CatalogColumns);
            var width = CatalogColumns * CatalogCellSize + (CatalogColumns - 1) * CatalogCellSpacing;
            var height = rows * CatalogCellSize + (rows - 1) * CatalogCellSpacing;

            var root = new GameObject("PF_CampTileCatalogV2", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(width + 32, height + 32);
            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color32(24, 24, 21, 255);
            rootImage.raycastTarget = false;

            var gridObject = new GameObject("ProductionV2Grid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.transform.SetParent(root.transform, false);
            var gridRect = gridObject.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(16, 16);
            gridRect.offsetMax = new Vector2(-16, -16);
            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(CatalogCellSize, CatalogCellSize);
            layout.spacing = new Vector2(CatalogCellSpacing, CatalogCellSpacing);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = CatalogColumns;
            layout.childAlignment = TextAnchor.UpperLeft;

            foreach (var spec in CampMapProductionV2Manifest.Specs)
            {
                var cell = new GameObject("Tile_" + spec.Id, typeof(RectTransform));
                cell.transform.SetParent(gridObject.transform, false);

                var content = new GameObject("Content", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                content.transform.SetParent(cell.transform, false);
                Stretch(content.GetComponent<RectTransform>());
                var contentImage = content.GetComponent<Image>();
                contentImage.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(spec.TexturePath);
                contentImage.preserveAspect = true;
                contentImage.raycastTarget = false;

                var markerSprite = AssetDatabase.LoadAssetAtPath<Sprite>(CampMapProductionV2Manifest.GetMarkerTexturePath(spec.Id));
                if (markerSprite != null)
                {
                    var marker = new GameObject("LocationMarker", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                    marker.transform.SetParent(cell.transform, false);
                    var markerRect = marker.GetComponent<RectTransform>();
                    markerRect.anchorMin = new Vector2(1f, 1f);
                    markerRect.anchorMax = new Vector2(1f, 1f);
                    markerRect.pivot = new Vector2(1f, 1f);
                    markerRect.anchoredPosition = new Vector2(-5f, -5f);
                    markerRect.sizeDelta = new Vector2(40f, 40f);
                    var markerImage = marker.GetComponent<Image>();
                    markerImage.sprite = markerSprite;
                    markerImage.preserveAspect = true;
                    markerImage.raycastTarget = false;
                }
            }

            PrefabUtility.SaveAsPrefabAsset(root, CampMapProductionV2Manifest.CatalogPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(CampMapProductionV2Manifest.CatalogPrefabPath, ImportAssetOptions.ForceUpdate);
        }

        private static void BuildMarkerCatalogPrefab()
        {
            const int cellSize = 92;
            const int spacing = 10;
            var rows = Mathf.CeilToInt(CampMapProductionV2Manifest.Markers.Count / (float)MarkerColumns);
            var width = MarkerColumns * cellSize + (MarkerColumns - 1) * spacing;
            var height = rows * cellSize + (rows - 1) * spacing;

            var root = new GameObject("PF_CampLocationMarkerCatalogV2", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            root.GetComponent<RectTransform>().sizeDelta = new Vector2(width + 32, height + 32);
            var rootImage = root.GetComponent<Image>();
            rootImage.color = new Color32(24, 24, 21, 255);
            rootImage.raycastTarget = false;

            var gridObject = new GameObject("LocationMarkerGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            gridObject.transform.SetParent(root.transform, false);
            var gridRect = gridObject.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(16, 16);
            gridRect.offsetMax = new Vector2(-16, -16);
            var layout = gridObject.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(cellSize, cellSize);
            layout.spacing = new Vector2(spacing, spacing);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = MarkerColumns;
            layout.childAlignment = TextAnchor.UpperLeft;

            foreach (var markerSpec in CampMapProductionV2Manifest.Markers)
            {
                var cell = new GameObject("Marker_" + markerSpec.LocationId, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                cell.transform.SetParent(gridObject.transform, false);
                var image = cell.GetComponent<Image>();
                image.sprite = AssetDatabase.LoadAssetAtPath<Sprite>(markerSpec.TexturePath);
                image.preserveAspect = true;
                image.raycastTarget = false;
            }

            PrefabUtility.SaveAsPrefabAsset(root, CampMapProductionV2Manifest.MarkerCatalogPrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(CampMapProductionV2Manifest.MarkerCatalogPrefabPath, ImportAssetOptions.ForceUpdate);
        }

        private static void Stretch(RectTransform rect)
        {
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
        }

        private static List<PalettePlacement<T>> BuildCategoryPlacements<T>(
            IReadOnlyList<T> items,
            Func<T, string> getCategory,
            int columns)
        {
            var placements = new List<PalettePlacement<T>>(items.Count);
            var cursor = 0;
            string previousCategory = null;
            foreach (var item in items)
            {
                var category = getCategory(item);
                if (previousCategory != null && !string.Equals(previousCategory, category, StringComparison.Ordinal)
                    && cursor % columns != 0)
                {
                    cursor += columns - cursor % columns;
                }

                placements.Add(new PalettePlacement<T>(item, cursor % columns, cursor / columns));
                cursor++;
                previousCategory = category;
            }

            return placements;
        }

        private static Tilemap RecreatePaletteTilemap(Transform paletteRoot, string layerName)
        {
            var existingTilemaps = paletteRoot.GetComponentsInChildren<Tilemap>(true);
            foreach (var existing in existingTilemaps)
            {
                Object.DestroyImmediate(existing.gameObject);
            }

            var layer = new GameObject(layerName, typeof(Tilemap), typeof(TilemapRenderer));
            layer.transform.SetParent(paletteRoot, false);
            var tilemap = layer.GetComponent<Tilemap>();
            tilemap.tileAnchor = new Vector3(0.5f, 0.5f, 0f);
            return tilemap;
        }

        private static void EnsureFolder(string folder)
        {
            if (AssetDatabase.IsValidFolder(folder))
            {
                return;
            }

            var parent = Path.GetDirectoryName(folder)?.Replace('\\', '/');
            var name = Path.GetFileName(folder);
            if (string.IsNullOrWhiteSpace(parent) || string.IsNullOrWhiteSpace(name))
            {
                throw new InvalidOperationException("Invalid Unity asset folder: " + folder);
            }

            EnsureFolder(parent);
            AssetDatabase.CreateFolder(parent, name);
        }

        private readonly struct PixelSpan
        {
            public PixelSpan(int start, int end)
            {
                Start = start;
                End = end;
            }

            public int Start { get; }

            public int End { get; }

            public int Length => End - Start + 1;
        }

        private readonly struct PalettePlacement<T>
        {
            public PalettePlacement(T item, int column, int row)
            {
                Item = item;
                Column = column;
                Row = row;
            }

            public T Item { get; }
            public int Column { get; }
            public int Row { get; }
        }
    }
}
