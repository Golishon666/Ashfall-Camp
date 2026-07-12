using AshfallCamp.Editor;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class CampMapTestTileBuilderTests
    {
        [Test]
        public void GeneratedTestTileTexturesAreSquareAndSameSize()
        {
            foreach (var spec in CampMapTestTileBuilder.TileSpecs)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AssetPath);

                Assert.NotNull(texture, "Missing generated test tile: " + spec.AssetPath);
                Assert.AreEqual(CampMapTestTileBuilder.TileSize, texture.width, spec.AssetPath);
                Assert.AreEqual(CampMapTestTileBuilder.TileSize, texture.height, spec.AssetPath);
            }
        }

        [Test]
        public void TestTileMapPrefabUsesSquareGridCells()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampMapTestTileBuilder.PrefabPath);

            Assert.NotNull(prefab, "Missing generated test tile prefab: " + CampMapTestTileBuilder.PrefabPath);

            var grid = prefab.GetComponentInChildren<GridLayoutGroup>(true);
            Assert.NotNull(grid, "Test tile prefab must contain a GridLayoutGroup.");
            Assert.AreEqual(grid.cellSize.x, grid.cellSize.y, 0.001f, "Grid cell size must stay square.");
            Assert.AreEqual(CampMapTestTileBuilder.PreviewColumns, grid.constraintCount);

            var tileImages = prefab.GetComponentsInChildren<RawImage>(true);
            Assert.AreEqual(CampMapTestTileBuilder.TileSpecs.Count, tileImages.Length);

            foreach (var image in tileImages)
            {
                Assert.NotNull(image.texture, image.name + " has no texture.");
                Assert.AreEqual(CampMapTestTileBuilder.TileSize, image.texture.width, image.name);
                Assert.AreEqual(CampMapTestTileBuilder.TileSize, image.texture.height, image.name);
            }
        }

        [Test]
        public void ImageGenTileTexturesAreSquareAndSameSize()
        {
            foreach (var path in CampMapImageGenTileBuilder.PreviewPaths)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(path);

                Assert.NotNull(texture, "Missing ImageGen test asset: " + path);
                Assert.AreEqual(CampMapImageGenTileBuilder.TileSize, texture.width, path);
                Assert.AreEqual(CampMapImageGenTileBuilder.TileSize, texture.height, path);
            }
        }

        [Test]
        public void ImageGenTileMapPrefabUsesSquareGridCells()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampMapImageGenTileBuilder.PrefabPath);

            Assert.NotNull(prefab, "Missing ImageGen test tile prefab: " + CampMapImageGenTileBuilder.PrefabPath);

            var grid = prefab.GetComponentInChildren<GridLayoutGroup>(true);
            Assert.NotNull(grid, "ImageGen test tile prefab must contain a GridLayoutGroup.");
            Assert.AreEqual(grid.cellSize.x, grid.cellSize.y, 0.001f, "Grid cell size must stay square.");
            Assert.AreEqual(CampMapImageGenTileBuilder.PreviewColumns, grid.constraintCount);

            var tileImages = prefab.GetComponentsInChildren<RawImage>(true);
            Assert.AreEqual(CampMapImageGenTileBuilder.PreviewPaths.Count, tileImages.Length);

            foreach (var image in tileImages)
            {
                Assert.NotNull(image.texture, image.name + " has no texture.");
                Assert.AreEqual(CampMapImageGenTileBuilder.TileSize, image.texture.width, image.name);
                Assert.AreEqual(CampMapImageGenTileBuilder.TileSize, image.texture.height, image.name);
            }
        }

        [Test]
        public void BiomeTileMapUsesASeamlessSquareGrid()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampMapImageGenTileBuilder.BiomeMapPrefabPath);

            Assert.NotNull(prefab, "Missing generated biome tile map: " + CampMapImageGenTileBuilder.BiomeMapPrefabPath);
            var grid = prefab.GetComponentInChildren<GridLayoutGroup>(true);
            Assert.NotNull(grid, "Biome tile map must contain a GridLayoutGroup.");
            Assert.AreEqual(CampMapImageGenTileBuilder.BiomeMapCellSize, grid.cellSize.x, 0.001f);
            Assert.AreEqual(CampMapImageGenTileBuilder.BiomeMapCellSize, grid.cellSize.y, 0.001f);
            Assert.AreEqual(Vector2.zero, grid.spacing, "Terrain tiles must touch with no gaps or overlap.");
            Assert.AreEqual(CampMapImageGenTileBuilder.BiomeMapColumns, grid.constraintCount);

            var tiles = prefab.GetComponentsInChildren<RawImage>(true);
            Assert.AreEqual(CampMapImageGenTileBuilder.BiomeMapColumns * CampMapImageGenTileBuilder.BiomeMapRows, tiles.Length);
            foreach (var tile in tiles)
            {
                Assert.NotNull(tile.texture, tile.name + " has no tile texture.");
                Assert.AreEqual(CampMapImageGenTileBuilder.TileSize, tile.texture.width, tile.name);
                Assert.AreEqual(CampMapImageGenTileBuilder.TileSize, tile.texture.height, tile.name);
            }
        }
    }
}
