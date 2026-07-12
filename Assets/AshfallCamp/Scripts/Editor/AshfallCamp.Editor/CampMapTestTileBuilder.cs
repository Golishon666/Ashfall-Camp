using System;
using System.Collections.Generic;
using System.IO;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using Object = UnityEngine.Object;
using Random = System.Random;

namespace AshfallCamp.Editor
{
    public static class CampMapTestTileBuilder
    {
        public const int TileSize = 512;
        public const int PreviewColumns = 5;
        public const int PrefabCellSize = 132;
        public const string TileFolder = "Assets/AshfallCamp/Art/UI/CampMap/TestTiles";
        public const string PreviewPath = TileFolder + "/test_tile_map_preview.png";
        public const string PrefabPath = "Assets/AshfallCamp/Prefabs/UI/Main/PF_TestTileMap.prefab";

        private static readonly TestTileSpec[] Specs =
        {
            new TestTileSpec("camp_core", "tile_camp_core.png", TileKind.CampCore, C(0x7E, 0x73, 0x49), C(0xC0, 0xA0, 0x69), 11),
            new TestTileSpec("camp_buildable", "tile_camp_buildable.png", TileKind.Buildable, C(0x83, 0x75, 0x50), C(0xB7, 0x9B, 0x6C), 12),
            new TestTileSpec("barracks", "tile_barracks.png", TileKind.Barracks, C(0x7B, 0x72, 0x4E), C(0xB3, 0x86, 0x5C), 13),
            new TestTileSpec("workshop", "tile_workshop.png", TileKind.Workshop, C(0x73, 0x6A, 0x56), C(0xA2, 0x83, 0x64), 14),
            new TestTileSpec("water_collector", "tile_water_collector.png", TileKind.WaterCollector, C(0x6B, 0x76, 0x5A), C(0x6E, 0x98, 0x9A), 15),
            new TestTileSpec("infirmary", "tile_infirmary.png", TileKind.Infirmary, C(0x78, 0x73, 0x57), C(0xB8, 0x97, 0x74), 16),
            new TestTileSpec("radio_tower", "tile_radio_tower.png", TileKind.RadioTower, C(0x6F, 0x73, 0x58), C(0x8A, 0x9B, 0x83), 17),
            new TestTileSpec("road", "tile_road.png", TileKind.Road, C(0x78, 0x6B, 0x4B), C(0xC2, 0xA3, 0x70), 18),
            new TestTileSpec("field", "tile_field.png", TileKind.Field, C(0x91, 0x86, 0x4F), C(0xD2, 0xBD, 0x70), 19),
            new TestTileSpec("forest", "tile_forest.png", TileKind.Forest, C(0x43, 0x5A, 0x35), C(0x7B, 0x8A, 0x54), 20),
            new TestTileSpec("ruins_buffer", "tile_ruins_buffer.png", TileKind.Ruins, C(0x6D, 0x68, 0x5B), C(0xA8, 0x94, 0x78), 21),
            new TestTileSpec("wasteland", "tile_wasteland.png", TileKind.Wasteland, C(0x96, 0x7B, 0x54), C(0xD0, 0xA9, 0x72), 22),
            new TestTileSpec("hazard", "tile_hazard.png", TileKind.Hazard, C(0x50, 0x48, 0x3E), C(0xB4, 0x63, 0x3A), 23),
            new TestTileSpec("abandoned_store", "tile_abandoned_store.png", TileKind.Store, C(0x73, 0x6B, 0x5C), C(0xB8, 0x97, 0x6D), 24),
            new TestTileSpec("mutant_tunnel", "tile_mutant_tunnel.png", TileKind.Tunnel, C(0x45, 0x42, 0x38), C(0x9D, 0x65, 0x3D), 25),
        };

        public static IReadOnlyList<TestTileSpec> TileSpecs
        {
            get { return Specs; }
        }

        [MenuItem("Tools/Ashfall Camp/Camp Map/Generate Test Tiles")]
        public static void GenerateTestTiles()
        {
            EnsureFolder(TileFolder);

            foreach (var spec in Specs)
            {
                var texture = BuildTexture(spec);
                SaveTexture(spec.AssetPath, texture);
                Object.DestroyImmediate(texture);
                ConfigureTextureImporter(spec.AssetPath);
            }

            GeneratePreview();
            GeneratePrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Generated Ashfall Camp test tile set: " + Specs.Length + " square " + TileSize + "x" + TileSize + " tiles, preview, and prefab.");
        }

        private static Texture2D BuildTexture(TestTileSpec spec)
        {
            var texture = new Texture2D(TileSize, TileSize, TextureFormat.RGBA32, false);
            FillTerrain(texture, spec);
            DrawTileFrame(texture, spec);
            DrawKindDetails(texture, spec);
            AddDirtAndWear(texture, spec);
            texture.Apply(false, false);
            return texture;
        }

        private static void FillTerrain(Texture2D texture, TestTileSpec spec)
        {
            for (var y = 0; y < TileSize; y++)
            {
                var v = y / (float)(TileSize - 1);
                for (var x = 0; x < TileSize; x++)
                {
                    var u = x / (float)(TileSize - 1);
                    var dx = Mathf.Abs(u - 0.5f) * 2f;
                    var dy = Mathf.Abs(v - 0.5f) * 2f;
                    var edge = Mathf.Max(dx, dy);
                    var grain = (Hash01(x / 3, y / 3, spec.Seed) - 0.5f) * 0.16f;
                    var coarse = (Hash01(x / 18, y / 18, spec.Seed + 101) - 0.5f) * 0.18f;
                    var sun = Mathf.Clamp01(0.18f + v * 0.16f + (1f - edge) * 0.14f + grain + coarse);
                    var color = Color.Lerp(spec.BaseColor, spec.AccentColor, sun);

                    if (edge > 0.9f)
                    {
                        color = Color.Lerp(color, Darken(spec.BaseColor, 0.58f), Mathf.InverseLerp(0.9f, 1f, edge) * 0.78f);
                    }

                    texture.SetPixel(x, y, color);
                }
            }
        }

        private static void DrawKindDetails(Texture2D texture, TestTileSpec spec)
        {
            switch (spec.Kind)
            {
                case TileKind.CampCore:
                    DrawRoadCross(texture, C(0xB7, 0x96, 0x66), 0.68f);
                    DrawCircle(texture, 256, 252, 58, C(0x55, 0x48, 0x37), 0.88f);
                    DrawCircle(texture, 256, 252, 37, C(0x2E, 0x2A, 0x24), 0.72f);
                    DrawCircle(texture, 256, 252, 18, C(0xC9, 0x63, 0x3A), 0.74f);
                    DrawBenches(texture);
                    break;
                case TileKind.Buildable:
                    DrawBuildSlot(texture, C(0xE0, 0xD1, 0xB1), 0.64f);
                    break;
                case TileKind.Barracks:
                    DrawBuildSlot(texture, C(0xE0, 0xD1, 0xB1), 0.42f);
                    DrawBuilding(texture, new Rect(150, 190, 210, 118), C(0x75, 0x4D, 0x36), C(0xC0, 0x93, 0x61));
                    DrawRect(texture, 190, 236, 52, 34, C(0x43, 0x35, 0x2A), 0.9f);
                    DrawRect(texture, 270, 236, 52, 34, C(0x43, 0x35, 0x2A), 0.9f);
                    break;
                case TileKind.Workshop:
                    DrawBuildSlot(texture, C(0xD2, 0xC2, 0xA4), 0.36f);
                    DrawBuilding(texture, new Rect(148, 184, 220, 130), C(0x5E, 0x5C, 0x53), C(0x9C, 0x78, 0x58));
                    DrawRect(texture, 182, 218, 62, 54, C(0x31, 0x32, 0x30), 0.9f);
                    DrawCircle(texture, 310, 246, 28, C(0x5B, 0x61, 0x63), 0.88f);
                    DrawLine(texture, 318, 160, 318, 228, 10, C(0x54, 0x4A, 0x3C), 0.94f);
                    break;
                case TileKind.WaterCollector:
                    DrawBuildSlot(texture, C(0xD2, 0xC2, 0xA4), 0.3f);
                    DrawCircle(texture, 256, 258, 88, C(0x4E, 0x78, 0x7D), 0.82f);
                    DrawCircle(texture, 256, 258, 64, C(0x79, 0xB0, 0xB8), 0.64f);
                    DrawLine(texture, 172, 190, 338, 326, 14, C(0x74, 0x58, 0x40), 0.75f);
                    DrawLine(texture, 340, 190, 174, 326, 14, C(0x74, 0x58, 0x40), 0.75f);
                    break;
                case TileKind.Infirmary:
                    DrawBuildSlot(texture, C(0xE0, 0xD1, 0xB1), 0.35f);
                    DrawBuilding(texture, new Rect(164, 184, 190, 136), C(0x7A, 0x6D, 0x58), C(0xC7, 0xAE, 0x8A));
                    DrawRect(texture, 238, 210, 36, 100, C(0xA7, 0x6F, 0x5E), 0.86f);
                    DrawRect(texture, 206, 242, 100, 36, C(0xA7, 0x6F, 0x5E), 0.86f);
                    break;
                case TileKind.RadioTower:
                    DrawBuildSlot(texture, C(0xD2, 0xC2, 0xA4), 0.28f);
                    DrawLine(texture, 256, 340, 256, 132, 12, C(0x46, 0x49, 0x42), 0.95f);
                    DrawLine(texture, 212, 340, 256, 132, 8, C(0x46, 0x49, 0x42), 0.82f);
                    DrawLine(texture, 300, 340, 256, 132, 8, C(0x46, 0x49, 0x42), 0.82f);
                    DrawLine(texture, 206, 188, 306, 188, 8, C(0x55, 0x5F, 0x58), 0.9f);
                    DrawCircleOutline(texture, 256, 126, 46, C(0x8A, 0x9B, 0x83), 0.64f, 5);
                    DrawCircleOutline(texture, 256, 126, 74, C(0x8A, 0x9B, 0x83), 0.34f, 4);
                    break;
                case TileKind.Road:
                    DrawRoad(texture);
                    break;
                case TileKind.Field:
                    DrawFieldRows(texture, spec);
                    break;
                case TileKind.Forest:
                    DrawForest(texture, spec);
                    break;
                case TileKind.Ruins:
                    DrawRuins(texture, spec, false);
                    break;
                case TileKind.Wasteland:
                    DrawWastelandCracks(texture, spec);
                    break;
                case TileKind.Hazard:
                    DrawWastelandCracks(texture, spec);
                    DrawHazardGlow(texture, spec);
                    break;
                case TileKind.Store:
                    DrawRuins(texture, spec, true);
                    DrawBuilding(texture, new Rect(146, 174, 220, 132), C(0x6B, 0x5A, 0x4A), C(0xB2, 0x8C, 0x61));
                    DrawRect(texture, 184, 222, 144, 48, C(0x42, 0x3A, 0x32), 0.88f);
                    break;
                case TileKind.Tunnel:
                    DrawWastelandCracks(texture, spec);
                    DrawCircle(texture, 256, 286, 126, C(0x31, 0x2D, 0x29), 0.92f);
                    DrawCircle(texture, 256, 294, 84, C(0x16, 0x17, 0x17), 0.96f);
                    DrawCircleOutline(texture, 256, 286, 132, C(0x9D, 0x65, 0x3D), 0.54f, 9);
                    break;
            }
        }

        private static void DrawTileFrame(Texture2D texture, TestTileSpec spec)
        {
            var dark = Darken(spec.BaseColor, 0.46f);
            var light = Color.Lerp(spec.AccentColor, Color.white, 0.14f);
            DrawRect(texture, 0, 0, TileSize, 10, dark, 0.72f);
            DrawRect(texture, 0, TileSize - 10, TileSize, 10, light, 0.28f);
            DrawRect(texture, 0, 0, 10, TileSize, dark, 0.6f);
            DrawRect(texture, TileSize - 10, 0, 10, TileSize, dark, 0.5f);
            DrawLine(texture, 25, 25, TileSize - 25, 25, 4, C(0xF0, 0xE2, 0xC4), 0.28f);
            DrawLine(texture, 25, TileSize - 25, TileSize - 25, TileSize - 25, 4, C(0x3A, 0x31, 0x27), 0.28f);
        }

        private static void DrawBuildSlot(Texture2D texture, Color color, float alpha)
        {
            DrawRectOutline(texture, 104, 112, 304, 288, color, alpha, 7);
            DrawLine(texture, 120, 128, 392, 128, 3, C(0xF1, 0xE7, 0xCC), 0.22f);
            DrawLine(texture, 120, 384, 392, 384, 4, C(0x42, 0x34, 0x26), 0.22f);
            DrawScatteredStones(texture, 108, 116, 300, 280, 26, C(0xB9, 0xAB, 0x8A));
        }

        private static void DrawRoad(Texture2D texture)
        {
            for (var y = 0; y < TileSize; y++)
            {
                for (var x = 0; x < TileSize; x++)
                {
                    var distance = Mathf.Abs(y - (x * 0.48f + 132f));
                    if (distance < 82f)
                    {
                        var alpha = Mathf.Clamp01(1f - distance / 82f) * 0.92f;
                        BlendPixel(texture, x, y, C(0xC8, 0xA4, 0x72), alpha);
                    }
                }
            }

            DrawLine(texture, 10, 128, 502, 366, 6, C(0x69, 0x57, 0x3C), 0.2f);
            DrawLine(texture, 10, 174, 502, 410, 4, C(0x69, 0x57, 0x3C), 0.18f);
        }

        private static void DrawRoadCross(Texture2D texture, Color color, float alpha)
        {
            DrawLine(texture, 0, 246, TileSize, 282, 74, color, alpha);
            DrawLine(texture, 246, 0, 280, TileSize, 70, color, alpha * 0.88f);
        }

        private static void DrawFieldRows(Texture2D texture, TestTileSpec spec)
        {
            for (var y = 86; y < 444; y += 36)
            {
                DrawLine(texture, 40, y, 472, y + 22, 7, C(0x6D, 0x6A, 0x3F), 0.34f);
                DrawLine(texture, 40, y + 13, 472, y + 35, 3, C(0xD7, 0xC8, 0x75), 0.3f);
            }

            DrawScatteredStones(texture, 30, 40, 452, 420, 20, Darken(spec.BaseColor, 0.72f));
        }

        private static void DrawForest(Texture2D texture, TestTileSpec spec)
        {
            var random = new Random(spec.Seed * 31);
            for (var i = 0; i < 38; i++)
            {
                var x = random.Next(44, 468);
                var y = random.Next(46, 466);
                var radius = random.Next(24, 46);
                DrawCircle(texture, x, y, radius, C(0x2F, 0x49, 0x2D), 0.86f);
                DrawCircle(texture, x - radius * 0.22f, y + radius * 0.16f, radius * 0.62f, C(0x68, 0x7B, 0x48), 0.45f);
                DrawRect(texture, x - 4, y - radius / 2, 8, radius, C(0x4A, 0x35, 0x22), 0.52f);
            }
        }

        private static void DrawRuins(Texture2D texture, TestTileSpec spec, bool sparse)
        {
            var random = new Random(spec.Seed * 37);
            var count = sparse ? 9 : 18;
            for (var i = 0; i < count; i++)
            {
                var w = random.Next(36, 96);
                var h = random.Next(18, 58);
                var x = random.Next(38, TileSize - w - 38);
                var y = random.Next(42, TileSize - h - 42);
                DrawRect(texture, x, y, w, h, C(0x8C, 0x83, 0x72), 0.52f);
                DrawRectOutline(texture, x, y, w, h, C(0x46, 0x3D, 0x35), 0.36f, 3);
            }

            DrawCracks(texture, spec, sparse ? 10 : 20, C(0x3C, 0x36, 0x2F), 0.46f);
        }

        private static void DrawWastelandCracks(Texture2D texture, TestTileSpec spec)
        {
            DrawCracks(texture, spec, 26, C(0x45, 0x38, 0x2B), 0.5f);
            DrawScatteredStones(texture, 24, 28, 464, 454, 42, C(0x6B, 0x5B, 0x45));
        }

        private static void DrawHazardGlow(Texture2D texture, TestTileSpec spec)
        {
            var random = new Random(spec.Seed * 41);
            for (var i = 0; i < 9; i++)
            {
                var x = random.Next(70, 444);
                var y = random.Next(80, 432);
                DrawLine(texture, x, y, x + random.Next(-56, 64), y + random.Next(-50, 58), 7, C(0xC9, 0x63, 0x3A), 0.46f);
                DrawLine(texture, x, y, x + random.Next(-56, 64), y + random.Next(-50, 58), 3, C(0xE1, 0xB4, 0x6A), 0.35f);
            }
        }

        private static void DrawBuilding(Texture2D texture, Rect rect, Color wall, Color roof)
        {
            DrawRect(texture, Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height), wall, 0.86f);
            DrawLine(texture, rect.x - 12, rect.y + rect.height, rect.center.x, rect.y + rect.height + 54, 24, roof, 0.92f);
            DrawLine(texture, rect.x + rect.width + 12, rect.y + rect.height, rect.center.x, rect.y + rect.height + 54, 24, roof, 0.92f);
            DrawRectOutline(texture, Mathf.RoundToInt(rect.x), Mathf.RoundToInt(rect.y), Mathf.RoundToInt(rect.width), Mathf.RoundToInt(rect.height), C(0x36, 0x2F, 0x27), 0.42f, 4);
        }

        private static void DrawBenches(Texture2D texture)
        {
            DrawRect(texture, 160, 188, 78, 16, C(0x68, 0x4D, 0x34), 0.76f);
            DrawRect(texture, 276, 318, 78, 16, C(0x68, 0x4D, 0x34), 0.76f);
            DrawRect(texture, 318, 190, 16, 78, C(0x68, 0x4D, 0x34), 0.76f);
        }

        private static void DrawCracks(Texture2D texture, TestTileSpec spec, int count, Color color, float alpha)
        {
            var random = new Random(spec.Seed * 101);
            for (var i = 0; i < count; i++)
            {
                var x = random.Next(44, 468);
                var y = random.Next(44, 468);
                var length = random.Next(28, 95);
                var angle = random.NextDouble() * Math.PI * 2.0;
                var x1 = x + Mathf.Cos((float)angle) * length;
                var y1 = y + Mathf.Sin((float)angle) * length;
                DrawLine(texture, x, y, x1, y1, random.Next(2, 5), color, alpha);
            }
        }

        private static void AddDirtAndWear(Texture2D texture, TestTileSpec spec)
        {
            var random = new Random(spec.Seed * 131);
            for (var i = 0; i < 80; i++)
            {
                var x = random.Next(20, 492);
                var y = random.Next(20, 492);
                var radius = random.Next(3, 14);
                var color = random.NextDouble() > 0.5 ? C(0x3B, 0x31, 0x25) : C(0xE5, 0xD3, 0xA8);
                DrawCircle(texture, x, y, radius, color, 0.04f + (float)random.NextDouble() * 0.08f);
            }
        }

        private static void DrawScatteredStones(Texture2D texture, int x, int y, int width, int height, int count, Color color)
        {
            var random = new Random(x * 17 + y * 31 + width * 7 + count);
            for (var i = 0; i < count; i++)
            {
                var px = random.Next(x, x + width);
                var py = random.Next(y, y + height);
                var radius = random.Next(3, 9);
                DrawCircle(texture, px, py, radius, color, 0.3f);
            }
        }

        private static void DrawRect(Texture2D texture, int x, int y, int width, int height, Color color, float alpha)
        {
            var minX = Mathf.Clamp(x, 0, TileSize - 1);
            var minY = Mathf.Clamp(y, 0, TileSize - 1);
            var maxX = Mathf.Clamp(x + width, 0, TileSize);
            var maxY = Mathf.Clamp(y + height, 0, TileSize);
            for (var py = minY; py < maxY; py++)
            {
                for (var px = minX; px < maxX; px++)
                {
                    BlendPixel(texture, px, py, color, alpha);
                }
            }
        }

        private static void DrawRectOutline(Texture2D texture, int x, int y, int width, int height, Color color, float alpha, int thickness)
        {
            DrawRect(texture, x, y, width, thickness, color, alpha);
            DrawRect(texture, x, y + height - thickness, width, thickness, color, alpha);
            DrawRect(texture, x, y, thickness, height, color, alpha);
            DrawRect(texture, x + width - thickness, y, thickness, height, color, alpha);
        }

        private static void DrawCircle(Texture2D texture, float centerX, float centerY, float radius, Color color, float alpha)
        {
            var minX = Mathf.Clamp(Mathf.FloorToInt(centerX - radius), 0, TileSize - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(centerX + radius), 0, TileSize - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(centerY - radius), 0, TileSize - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(centerY + radius), 0, TileSize - 1);
            var r2 = radius * radius;
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    if (dx * dx + dy * dy <= r2)
                    {
                        BlendPixel(texture, x, y, color, alpha);
                    }
                }
            }
        }

        private static void DrawCircleOutline(Texture2D texture, float centerX, float centerY, float radius, Color color, float alpha, int thickness)
        {
            var inner = radius - thickness;
            var minX = Mathf.Clamp(Mathf.FloorToInt(centerX - radius), 0, TileSize - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(centerX + radius), 0, TileSize - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(centerY - radius), 0, TileSize - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(centerY + radius), 0, TileSize - 1);
            var r2 = radius * radius;
            var inner2 = inner * inner;
            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var dx = x - centerX;
                    var dy = y - centerY;
                    var d2 = dx * dx + dy * dy;
                    if (d2 <= r2 && d2 >= inner2)
                    {
                        BlendPixel(texture, x, y, color, alpha);
                    }
                }
            }
        }

        private static void DrawLine(Texture2D texture, float x0, float y0, float x1, float y1, float thickness, Color color, float alpha)
        {
            var minX = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(x0, x1) - thickness), 0, TileSize - 1);
            var maxX = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(x0, x1) + thickness), 0, TileSize - 1);
            var minY = Mathf.Clamp(Mathf.FloorToInt(Mathf.Min(y0, y1) - thickness), 0, TileSize - 1);
            var maxY = Mathf.Clamp(Mathf.CeilToInt(Mathf.Max(y0, y1) + thickness), 0, TileSize - 1);
            var dx = x1 - x0;
            var dy = y1 - y0;
            var length2 = dx * dx + dy * dy;
            if (length2 <= 0.001f) return;

            for (var y = minY; y <= maxY; y++)
            {
                for (var x = minX; x <= maxX; x++)
                {
                    var t = Mathf.Clamp01(((x - x0) * dx + (y - y0) * dy) / length2);
                    var px = x0 + t * dx;
                    var py = y0 + t * dy;
                    var distance = Vector2.Distance(new Vector2(x, y), new Vector2(px, py));
                    if (distance <= thickness)
                    {
                        BlendPixel(texture, x, y, color, alpha * Mathf.Clamp01(1f - distance / (thickness + 0.001f) * 0.45f));
                    }
                }
            }
        }

        private static void BlendPixel(Texture2D texture, int x, int y, Color color, float alpha)
        {
            if (x < 0 || x >= TileSize || y < 0 || y >= TileSize) return;
            var current = texture.GetPixel(x, y);
            texture.SetPixel(x, y, Color.Lerp(current, color, Mathf.Clamp01(alpha)));
        }

        private static void GeneratePreview()
        {
            var rows = Mathf.CeilToInt(Specs.Length / (float)PreviewColumns);
            var preview = new Texture2D(PreviewColumns * TileSize, rows * TileSize, TextureFormat.RGBA32, false);
            var background = C(0x2B, 0x2E, 0x33);
            for (var y = 0; y < preview.height; y++)
            {
                for (var x = 0; x < preview.width; x++)
                {
                    preview.SetPixel(x, y, background);
                }
            }

            for (var i = 0; i < Specs.Length; i++)
            {
                var texture = BuildTexture(Specs[i]);
                var column = i % PreviewColumns;
                var row = rows - 1 - i / PreviewColumns;
                preview.SetPixels(column * TileSize, row * TileSize, TileSize, TileSize, texture.GetPixels());
                Object.DestroyImmediate(texture);
            }

            preview.Apply(false, false);
            SaveTexture(PreviewPath, preview);
            Object.DestroyImmediate(preview);
            ConfigureTextureImporter(PreviewPath, 4096);
        }

        private static void GeneratePrefab()
        {
            var root = new GameObject("PF_TestTileMap", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            var rootRect = root.GetComponent<RectTransform>();
            rootRect.sizeDelta = new Vector2(760, 486);

            var backplate = root.GetComponent<Image>();
            backplate.color = new Color32(0x2B, 0x2E, 0x33, 0xE6);

            var grid = new GameObject("SquareTileGrid", typeof(RectTransform), typeof(GridLayoutGroup));
            grid.transform.SetParent(root.transform, false);
            var gridRect = grid.GetComponent<RectTransform>();
            gridRect.anchorMin = Vector2.zero;
            gridRect.anchorMax = Vector2.one;
            gridRect.offsetMin = new Vector2(22, 22);
            gridRect.offsetMax = new Vector2(-22, -22);

            var layout = grid.GetComponent<GridLayoutGroup>();
            layout.cellSize = new Vector2(PrefabCellSize, PrefabCellSize);
            layout.spacing = new Vector2(10, 10);
            layout.padding = new RectOffset(0, 0, 0, 0);
            layout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            layout.constraintCount = PreviewColumns;
            layout.childAlignment = TextAnchor.MiddleCenter;

            foreach (var spec in Specs)
            {
                var cell = new GameObject("Tile_" + spec.Id, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
                cell.transform.SetParent(grid.transform, false);
                var rawImage = cell.GetComponent<RawImage>();
                rawImage.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(spec.AssetPath);
                rawImage.color = Color.white;
                rawImage.raycastTarget = false;

                var rect = cell.GetComponent<RectTransform>();
                rect.sizeDelta = new Vector2(PrefabCellSize, PrefabCellSize);
            }

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            Object.DestroyImmediate(root);
            AssetDatabase.ImportAsset(PrefabPath, ImportAssetOptions.ForceUpdate);
        }

        private static void SaveTexture(string assetPath, Texture2D texture)
        {
            var absolutePath = Path.GetFullPath(assetPath);
            var directory = Path.GetDirectoryName(absolutePath);
            if (!string.IsNullOrEmpty(directory))
            {
                Directory.CreateDirectory(directory);
            }

            File.WriteAllBytes(absolutePath, texture.EncodeToPNG());
            AssetDatabase.ImportAsset(assetPath, ImportAssetOptions.ForceUpdate);
        }

        private static void ConfigureTextureImporter(string assetPath, int maxTextureSize = 1024)
        {
            var importer = AssetImporter.GetAtPath(assetPath) as TextureImporter;
            if (importer == null) return;

            importer.textureType = TextureImporterType.Default;
            importer.alphaSource = TextureImporterAlphaSource.None;
            importer.mipmapEnabled = false;
            importer.wrapMode = TextureWrapMode.Clamp;
            importer.filterMode = FilterMode.Bilinear;
            importer.maxTextureSize = maxTextureSize;
            importer.textureCompression = TextureImporterCompression.CompressedHQ;
            importer.SaveAndReimport();
        }

        private static void EnsureFolder(string assetFolder)
        {
            var parts = assetFolder.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next))
                {
                    AssetDatabase.CreateFolder(current, parts[i]);
                }

                current = next;
            }
        }

        private static Color C(byte r, byte g, byte b)
        {
            return new Color32(r, g, b, 0xFF);
        }

        private static Color Darken(Color color, float amount)
        {
            return new Color(color.r * amount, color.g * amount, color.b * amount, color.a);
        }

        private static float Hash01(int x, int y, int salt)
        {
            unchecked
            {
                var n = (uint)(x * 374761393 + y * 668265263 + salt * 1442695041);
                n = (n ^ (n >> 13)) * 1274126177u;
                n ^= n >> 16;
                return (n & 0x00FFFFFF) / 16777215f;
            }
        }

        public sealed class TestTileSpec
        {
            public readonly string Id;
            public readonly string FileName;
            public readonly TileKind Kind;
            public readonly Color BaseColor;
            public readonly Color AccentColor;
            public readonly int Seed;

            public TestTileSpec(string id, string fileName, TileKind kind, Color baseColor, Color accentColor, int seed)
            {
                Id = id;
                FileName = fileName;
                Kind = kind;
                BaseColor = baseColor;
                AccentColor = accentColor;
                Seed = seed;
            }

            public string AssetPath
            {
                get { return TileFolder + "/" + FileName; }
            }
        }

        public enum TileKind
        {
            CampCore,
            Buildable,
            Barracks,
            Workshop,
            WaterCollector,
            Infirmary,
            RadioTower,
            Road,
            Field,
            Forest,
            Ruins,
            Wasteland,
            Hazard,
            Store,
            Tunnel
        }
    }
}
