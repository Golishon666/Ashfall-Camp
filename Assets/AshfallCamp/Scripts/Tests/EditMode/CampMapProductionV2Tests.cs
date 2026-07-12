using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AshfallCamp.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class CampMapProductionV2Tests
    {
        private static readonly string[] BuildingIds =
        {
            "barracks", "workshop", "water_collector", "mushroom_beds", "infirmary", "radio_tower",
        };

        private static readonly string[] ZoneIds =
        {
            "abandoned_store", "dry_suburb", "ruined_clinic", "police_outpost", "mutant_tunnel",
        };

        private static readonly string[] FutureLocationIds =
        {
            "ruined_homestead", "abandoned_greenhouse", "flooded_cellar", "scavenger_hideout", "collapsed_mine", "marauder_camp",
            "mutant_nest", "military_depot", "underground_laboratory", "biohazard_plant", "reactor_complex", "warlord_fortress",
        };

        [Test]
        public void ManifestContainsExactlyNinetySixUniqueTilesAndRequiredContent()
        {
            Assert.AreEqual(96, CampMapProductionV2Manifest.Specs.Count);
            Assert.AreEqual(96, CampMapProductionV2Manifest.Specs.Select(spec => spec.Id).Distinct(StringComparer.Ordinal).Count());
            Assert.False(CampMapProductionV2Manifest.Specs.Any(spec => spec.Id.Contains("road")), "Production V5 must not contain road tiles.");
            Assert.AreEqual(16, CampMapProductionV2Manifest.Sheets.Count);
            Assert.AreEqual(96, CampMapProductionV2Manifest.Sheets.SelectMany(sheet => sheet.TileIds).Distinct(StringComparer.Ordinal).Count());

            foreach (var sheet in CampMapProductionV2Manifest.Sheets)
            {
                Assert.AreEqual(6, sheet.TileIds.Count, sheet.TexturePath);
                Assert.That(File.Exists(sheet.TexturePath), "Missing 3x2 sprite sheet: " + sheet.TexturePath);
            }

            foreach (var buildingId in BuildingIds)
            {
                Assert.That(CampMapProductionV2Manifest.Specs.Any(spec => spec.Id == buildingId + "_basic"));
                Assert.That(CampMapProductionV2Manifest.Specs.Any(spec => spec.Id == buildingId + "_upgraded"));
                Assert.That(CampMapProductionV2Manifest.Specs.Any(spec => spec.Id == buildingId + "_ruined"));
            }

            foreach (var zoneId in ZoneIds)
            {
                Assert.That(CampMapProductionV2Manifest.Specs.Any(spec => spec.Id == zoneId));
            }

            CollectionAssert.AreEqual(
                FutureLocationIds,
                CampMapProductionV2Manifest.Specs.Where(spec => spec.Category == "future_location").Select(spec => spec.Id).ToArray(),
                "Future expedition locations must remain ordered from threat 1 to threat 12.");
            Assert.AreEqual(6, CampMapProductionV2Manifest.Specs.Count(spec => spec.Category == "grass"));
            Assert.AreEqual(6, CampMapProductionV2Manifest.Specs.Count(spec => spec.Category == "winter_extra"));
            Assert.AreEqual(6, CampMapProductionV2Manifest.Specs.Count(spec => spec.Category == "grass_location"));
            Assert.AreEqual(6, CampMapProductionV2Manifest.Specs.Count(spec => spec.Category == "winter_location"));
        }

        [Test]
        public void MarkerManifestCoversEveryLocationExactlyOnce()
        {
            var locationIds = CampMapProductionV2Manifest.Specs
                .Where(spec => spec.Category == "expedition" || spec.Category == "future_location"
                               || spec.Category == "grass_location" || spec.Category == "winter_location")
                .Select(spec => spec.Id).ToArray();
            Assert.AreEqual(29, CampMapProductionV2Manifest.Markers.Count);
            CollectionAssert.AreEquivalent(locationIds, CampMapProductionV2Manifest.Markers.Select(marker => marker.LocationId).ToArray());
            Assert.AreEqual(29, CampMapProductionV2Manifest.Markers.Select(marker => marker.LocationId).Distinct(StringComparer.Ordinal).Count());
        }

        [Test]
        public void ProductionTexturesAndTileAssetsExistAndUseRuntimeSize()
        {
            foreach (var spec in CampMapProductionV2Manifest.Specs)
            {
                Assert.That(File.Exists(spec.TexturePath), "Missing source PNG: " + spec.TexturePath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.TexturePath);
                Assert.NotNull(texture, "Missing imported texture: " + spec.TexturePath);
                Assert.AreEqual(CampMapProductionV2Manifest.RuntimeTextureSize, texture.width, spec.TexturePath);
                Assert.AreEqual(CampMapProductionV2Manifest.RuntimeTextureSize, texture.height, spec.TexturePath);

                var tile = AssetDatabase.LoadAssetAtPath<Tile>(spec.TileAssetPath);
                Assert.NotNull(tile, "Missing Tile asset: " + spec.TileAssetPath);
                Assert.NotNull(tile.sprite, "Tile has no sprite: " + spec.TileAssetPath);
                Assert.AreEqual(Tile.ColliderType.None, tile.colliderType, spec.TileAssetPath);
            }
        }

        [Test]
        public void TileSourcesContainNoChromaKeyAndSharePixelIdenticalFrame()
        {
            Color32[] expectedFrame = null;
            foreach (var spec in CampMapProductionV2Manifest.Specs)
            {
                var texture = LoadSourceTexture(spec.TexturePath);
                try
                {
                    Assert.Greater(texture.GetPixel(texture.width / 2, texture.height / 2).a, 0.98f, spec.Id + " center must be opaque.");
                    var pixels = texture.GetPixels32();
                    if (expectedFrame == null)
                    {
                        expectedFrame = pixels;
                    }

                    for (var y = 0; y < texture.height; y++)
                    {
                        for (var x = 0; x < texture.width; x++)
                        {
                            var pixel = texture.GetPixel(x, y);
                            if (pixel.a < 0.01f)
                            {
                                Assert.Less(pixel.r + pixel.g + pixel.b, 0.01f,
                                    spec.Id + " contains chroma RGB in a transparent pixel.");
                            }

                            var isFramePixel = x < CampMapProductionV2Builder.SharedFrameBand
                                               || y < CampMapProductionV2Builder.SharedFrameBand
                                               || x >= texture.width - CampMapProductionV2Builder.SharedFrameBand
                                               || y >= texture.height - CampMapProductionV2Builder.SharedFrameBand;
                            if (isFramePixel)
                            {
                                Assert.False(IsMagenta(pixel), spec.Id + " contains residual chroma-key magenta in the shared frame.");
                                Assert.AreEqual(expectedFrame[y * texture.width + x], pixels[y * texture.width + x],
                                    spec.Id + " frame differs at " + x + "," + y);
                            }
                        }
                    }
                }
                finally
                {
                    UnityEngine.Object.DestroyImmediate(texture);
                }
            }
        }

        [Test]
        public void CatalogPrefabContainsAllNinetySixCellsAndLocationMarkers()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampMapProductionV2Manifest.CatalogPrefabPath);
            Assert.NotNull(prefab);
            var grid = prefab.GetComponentInChildren<GridLayoutGroup>(true);
            Assert.NotNull(grid);
            Assert.AreEqual(CampMapProductionV2Builder.CatalogColumns, grid.constraintCount);
            Assert.AreEqual(96, grid.transform.childCount);
            foreach (Transform cell in grid.transform)
            {
                Assert.NotNull(cell.Find("Content"), cell.name);
                Assert.IsNull(cell.Find("SharedFrame"), cell.name);
                var id = cell.name.Substring("Tile_".Length);
                var expectsMarker = CampMapProductionV2Manifest.Markers.Any(marker => marker.LocationId == id);
                Assert.AreEqual(expectsMarker, cell.Find("LocationMarker") != null, cell.name);
            }
        }

        [Test]
        public void MarkerAssetsPaletteAndCatalogAreComplete()
        {
            foreach (var marker in CampMapProductionV2Manifest.Markers)
            {
                Assert.That(File.Exists(marker.TexturePath), marker.TexturePath);
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(marker.TexturePath);
                Assert.NotNull(texture, marker.TexturePath);
                Assert.AreEqual(CampMapProductionV2Manifest.MarkerTextureSize, texture.width);
                Assert.AreEqual(CampMapProductionV2Manifest.MarkerTextureSize, texture.height);
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(marker.TileAssetPath);
                Assert.NotNull(tile, marker.TileAssetPath);
                Assert.NotNull(tile.sprite, marker.TileAssetPath);
                Assert.AreEqual(Tile.ColliderType.None, tile.colliderType);
            }

            var palette = AssetDatabase.LoadAssetAtPath<GameObject>(CampMapProductionV2Manifest.MarkerPalettePath);
            Assert.NotNull(palette);
            Assert.AreEqual(new Vector3(5f, 5f, 1f), palette.GetComponent<Grid>().cellSize);
            var tilemap = palette.GetComponentInChildren<Tilemap>();
            Assert.AreEqual(29, tilemap.GetTilesBlock(tilemap.cellBounds).Count(tile => tile != null));

            var catalog = AssetDatabase.LoadAssetAtPath<GameObject>(CampMapProductionV2Manifest.MarkerCatalogPrefabPath);
            Assert.NotNull(catalog);
            var grid = catalog.GetComponentInChildren<GridLayoutGroup>(true);
            Assert.AreEqual(29, grid.transform.childCount);
        }

        [Test]
        public void PaletteGridCellSizeMatchesFiveUnitTileSprites()
        {
            var palette = AssetDatabase.LoadAssetAtPath<GameObject>(CampMapProductionV2Manifest.PalettePath);
            Assert.NotNull(palette);
            var grid = palette.GetComponent<Grid>();
            Assert.NotNull(grid);
            Assert.AreEqual(CampMapProductionV2Manifest.WorldTileCellSize, grid.cellSize.x, 0.001f);
            Assert.AreEqual(CampMapProductionV2Manifest.WorldTileCellSize, grid.cellSize.y, 0.001f);
            Assert.AreEqual(1f, grid.cellSize.z, 0.001f);
            Assert.AreEqual(Vector3.zero, grid.cellGap);
            var paletteTilemap = palette.GetComponentInChildren<Tilemap>();
            Assert.NotNull(paletteTilemap);
            Assert.AreEqual(96, paletteTilemap.GetTilesBlock(paletteTilemap.cellBounds).Count(tile => tile != null));

            var categoryOrders = CampMapProductionV2Manifest.PaletteSpecs
                .Select(spec => CampMapProductionV2Manifest.GetPaletteCategoryOrder(spec.Category)).ToArray();
            Assert.AreEqual(96, CampMapProductionV2Manifest.PaletteSpecs.Count);
            Assert.That(categoryOrders, Is.Ordered, "Palette categories must follow the declared production order.");

            foreach (var spec in CampMapProductionV2Manifest.Specs)
            {
                var tile = AssetDatabase.LoadAssetAtPath<Tile>(spec.TileAssetPath);
                Assert.NotNull(tile?.sprite, spec.TileAssetPath);
                Assert.AreEqual(CampMapProductionV2Manifest.WorldTileCellSize, tile.sprite.bounds.size.x, 0.001f, spec.Id);
                Assert.AreEqual(CampMapProductionV2Manifest.WorldTileCellSize, tile.sprite.bounds.size.y, 0.001f, spec.Id);
            }
        }

        [Test]
        public void ProductionBrushContainsAllTilesAndPaintsTheSelectedTile()
        {
            var brush = AssetDatabase.LoadAssetAtPath<CampMapProductionV2Brush>(CampMapProductionV2Manifest.BrushPath);
            Assert.NotNull(brush, "Missing Production V2 Tile Palette brush asset.");
            Assert.AreEqual(96, brush.Tiles.Count);
            Assert.AreEqual(96, brush.TileLabels.Count);
            Assert.False(brush.Tiles.Any(tile => tile == null), "The production brush contains a missing Tile asset.");
            CollectionAssert.AreEqual(
                CampMapProductionV2Manifest.PaletteSpecs.Select(spec => spec.Category + " / " + spec.Id).ToArray(),
                brush.TileLabels.ToArray(),
                "Brush order must match the sorted production palette.");

            var previousIndex = brush.SelectedIndex;
            var gridObject = new GameObject("ProductionBrushTestGrid", typeof(Grid));
            var tilemapObject = new GameObject("ProductionBrushTestTilemap", typeof(Tilemap), typeof(TilemapRenderer));
            tilemapObject.transform.SetParent(gridObject.transform, false);
            try
            {
                brush.SelectTile(brush.Tiles.Count - 1);
                var position = new Vector3Int(3, 4, 0);
                brush.Paint(gridObject.GetComponent<Grid>(), tilemapObject, position);
                Assert.AreSame(brush.SelectedTile, tilemapObject.GetComponent<Tilemap>().GetTile(position));
            }
            finally
            {
                brush.SelectTile(previousIndex);
                UnityEngine.Object.DestroyImmediate(gridObject);
            }
        }

        [Test]
        public void BootSceneUsesTheTwelveByTwelveProductionMapWithoutASecondFrameLayer()
        {
            var previousPath = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene(CampMapProductionV2Manifest.BootScenePath, OpenSceneMode.Single);
            try
            {
                var root = scene.GetRootGameObjects().Single(gameObject => gameObject.name == "WorldTileMap");
                var terrain = root.transform.Find("Tilemap")?.GetComponent<Tilemap>();
                var frame = root.transform.Find("TileFrameOverlay")?.GetComponent<Tilemap>();
                var markers = root.transform.Find("LocationMarkerOverlay")?.GetComponent<Tilemap>();
                Assert.NotNull(terrain);
                Assert.AreEqual(new Vector3(0.5f, 0.5f, 0f), terrain.tileAnchor);
                Assert.AreEqual(144, terrain.GetTilesBlock(terrain.cellBounds).Count(tile => tile != null));
                if (frame != null)
                {
                    Assert.AreEqual(0, frame.GetTilesBlock(frame.cellBounds).Count(tile => tile != null),
                        "The V5 sprites already contain the shared frame and must not be double-framed.");
                    Assert.IsNull(frame.GetComponent<TilemapCollider2D>(), "Legacy frame overlay must not create colliders.");
                }
                Assert.NotNull(markers);
                Assert.AreEqual(new Vector3(0.5f, 0.5f, 0f), markers.tileAnchor);
                Assert.AreEqual(5, markers.GetTilesBlock(markers.cellBounds).Count(tile => tile != null));
                Assert.AreEqual(3, markers.GetComponent<TilemapRenderer>().sortingOrder);
                Assert.IsNull(markers.GetComponent<TilemapCollider2D>());
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousPath) && previousPath != CampMapProductionV2Manifest.BootScenePath)
                {
                    EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
                }
            }
        }

        private static Texture2D LoadSourceTexture(string assetPath)
        {
            var texture = new Texture2D(2, 2, TextureFormat.RGBA32, false);
            Assert.True(ImageConversion.LoadImage(texture, File.ReadAllBytes(assetPath), false), assetPath);
            return texture;
        }

        private static bool IsMagenta(Color pixel)
        {
            return pixel.r > 0.82f && pixel.b > 0.70f && pixel.g < 0.32f
                   && pixel.r - pixel.g > 0.58f && pixel.b - pixel.g > 0.46f;
        }
    }
}
