using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using UnityEditor;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AshfallCamp.Editor
{
    public static class WorldTileConfigBuilder
    {
        public const string ConfigFolder = "Assets/AshfallCamp/Configs/World/Tiles";
        public const string CatalogPath = "Assets/AshfallCamp/Configs/World/WorldTileCatalog.asset";
        private static readonly string[] WeakEnemies = { "creature_weak_ash_roach", "creature_weak_mire_leech", "creature_weak_radiated_hound", "enemy_weak_scavenger" };
        private static readonly string[] MidEnemies = { "creature_mid_stalker_rat", "creature_mid_plague_ghoul", "enemy_mid_blister_rusher", "enemy_mid_pistol_duelist" };
        private static readonly string[] EliteEnemies = { "creature_elite_acidback_lurker", "creature_elite_ironhide_ravager", "enemy_elite_auto_gunner", "enemy_elite_tire_shield_mauler" };
        private static readonly string[] BossEnemies = { "creature_boss_ash_widow", "creature_boss_tunnel_king", "enemy_boss_spine_gunner", "enemy_boss_ashfall_warlord" };

        [MenuItem("Tools/Ashfall Camp/World Map/Build World Tile Configs")]
        public static void Build()
        {
            EnsureFolder(ConfigFolder);
            var catalog = AssetDatabase.LoadAssetAtPath<WorldTileCatalogSO>(CatalogPath);
            if (catalog == null)
            {
                catalog = ScriptableObject.CreateInstance<WorldTileCatalogSO>();
                AssetDatabase.CreateAsset(catalog, CatalogPath);
            }
            catalog.Tiles.Clear();

            var zoneCatalog = AssetDatabase.LoadAssetAtPath<ZoneCatalogSO>("Assets/AshfallCamp/Configs/World/ZoneCatalog.asset");
            foreach (var spec in CampMapProductionV2Manifest.Specs)
            {
                var path = ConfigFolder + "/WorldTile_" + spec.Id + ".asset";
                var config = AssetDatabase.LoadAssetAtPath<WorldTileConfigSO>(path);
                if (config == null || MonoScript.FromScriptableObject(config) == null)
                {
                    if (File.Exists(Path.GetFullPath(path))) AssetDatabase.DeleteAsset(path);
                    config = ScriptableObject.CreateInstance<WorldTileConfigSO>();
                    AssetDatabase.CreateAsset(config, path);
                }
                Configure(config, spec);
                EditorUtility.SetDirty(config);
                catalog.Tiles.Add(config);
                if (zoneCatalog != null && (config.Type == WorldTileType.Normal || config.Type == WorldTileType.Expedition))
                {
                    ConfigureZone(zoneCatalog, config);
                }
            }

            EditorUtility.SetDirty(catalog);
            if (zoneCatalog != null) EditorUtility.SetDirty(zoneCatalog);
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>("Assets/AshfallCamp/Configs/Core/GameConfigDatabase.asset");
            if (database != null)
            {
                database.WorldTiles = catalog;
                EditorUtility.SetDirty(database);
            }
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh(ImportAssetOptions.ForceSynchronousImport);
            Debug.Log("Built " + catalog.Tiles.Count + " per-tile configs; normal=" + catalog.Tiles.Count(x => x.Type == WorldTileType.Normal) + ", expeditions=" + catalog.Tiles.Count(x => x.Type == WorldTileType.Expedition) + ".");
        }

        private static void Configure(WorldTileConfigSO output, ProductionTileSpec spec)
        {
            output.Id = spec.Id;
            output.DisplayName = Humanize(spec.Id);
            output.Category = spec.Category;
            output.Type = ResolveType(spec.Category);
            output.Strength = ResolveStrength(spec);
            output.ZoneId = output.Type == WorldTileType.Expedition ? spec.Id : output.Type == WorldTileType.Normal ? "gather_" + spec.Id : string.Empty;
            ResolveTravel(spec.Category, output.Strength, out output.FoodCostPerSurvivor, out output.WaterCostPerSurvivor);
            output.EncounterChance = output.Type == WorldTileType.Expedition ? Math.Min(.8, .12 + output.Strength * .045) : Math.Min(.42, .025 + output.Strength * .025);
            output.RareEquipmentChance = output.Type == WorldTileType.Expedition ? Math.Min(.65, .12 + output.Strength * .04) : .02;
            output.ContentTile = AssetDatabase.LoadAssetAtPath<TileBase>(spec.TileAssetPath);
            output.MarkerTile = output.Type == WorldTileType.Expedition ? AssetDatabase.LoadAssetAtPath<TileBase>(CampMapProductionV2Manifest.GetMarkerTileAssetPath(spec.Id)) : null;
            var tile = output.ContentTile as Tile;
            output.Thumbnail = tile != null ? tile.sprite : null;
            output.ResourceRewards.Clear();
            output.EnemyTable.Clear();
            output.EquipmentTable.Clear();
            AddRewards(output, spec.Category);
            AddEnemies(output);
            AddEquipment(output);
        }

        private static void ConfigureZone(ZoneCatalogSO catalog, WorldTileConfigSO tile)
        {
            var zone = catalog.Zones.FirstOrDefault(item => string.Equals(item.Id, tile.ZoneId, StringComparison.Ordinal));
            if (zone == null)
            {
                zone = new ZoneConfigData { Id = tile.ZoneId };
                catalog.Zones.Add(zone);
            }
            zone.Name = tile.DisplayName;
            zone.RiskTier = tile.Strength >= 10 ? RiskTier.DeadZone : tile.Strength >= 7 ? RiskTier.Dangerous : tile.Strength >= 4 ? RiskTier.Unstable : RiskTier.Safe;
            zone.BaseDurationSeconds = 25 + tile.Strength * (tile.Type == WorldTileType.Expedition ? 28 : 12);
            zone.MinDurationSeconds = Math.Max(15, zone.BaseDurationSeconds * .65);
            zone.MaxDurationSeconds = zone.BaseDurationSeconds * 1.65;
            zone.FoodCostPerSurvivor = 0;
            zone.WaterCostPerSurvivor = 0;
            zone.RecommendedPower = 3 + tile.Strength * (tile.Type == WorldTileType.Expedition ? 5 : 2);
            zone.BaseAmbushChance = tile.EncounterChance;
            zone.DurabilityPressure = Math.Max(0, tile.Strength / 3);
            zone.MinEnemyCount = 1;
            zone.MaxEnemyCount = Math.Max(1, Math.Min(4, 1 + tile.Strength / 4));
            zone.RequiredBuildingLevels.Clear();
            zone.UnlockConditions.Clear();
            zone.EnemyTable.Clear();
            foreach (var enemy in tile.EnemyTable) zone.EnemyTable.Add(new WeightedEntryData { Id = enemy.Id, Weight = enemy.Weight });
            zone.LootTable.Clear();
            foreach (var reward in tile.ResourceRewards)
            {
                zone.LootTable.Add(new LootTableEntryData { ResourceId = reward.ResourceId, Min = Math.Max(1, reward.Min * 2), Max = Math.Max(2, reward.Max * (tile.Type == WorldTileType.Expedition ? 3 : 2)), Weight = reward.Weight });
            }
            zone.EquipmentDropChance = tile.RareEquipmentChance;
            zone.ShowInExpeditionMenu = tile.Type == WorldTileType.Expedition;
            zone.EquipmentTable.Clear();
            foreach (var item in tile.EquipmentTable) zone.EquipmentTable.Add(new WeightedEntryData { Id = item.Id, Weight = item.Weight });
        }

        private static void AddRewards(WorldTileConfigSO output, string category)
        {
            AddReward(output, "scrap", 0, Math.Max(1, output.Strength / 2), output.Type == WorldTileType.Expedition ? 90 : 28);
            if (category.Contains("forest") || category == "grass" || output.Id == "wheat_field" || output.Id == "green_field") AddReward(output, "food", 1, 3 + output.Strength, 78);
            if (category.Contains("swamp") || output.Id.Contains("water") || output.Id.Contains("frozen")) AddReward(output, "water", 1, 2 + output.Strength / 2, 62);
            if (category == "city" || category == "buffer") AddReward(output, "weapon_parts", 1, 1 + output.Strength / 3, 35);
            if (category == "hazard" || output.Id.Contains("clinic") || output.Id.Contains("laboratory") || output.Id.Contains("research")) AddReward(output, "medicine", 1, 1 + output.Strength / 4, 32);
            if (output.Type == WorldTileType.Expedition) AddReward(output, "scrap", 2 + output.Strength, 5 + output.Strength * 2, 100);
        }

        private static void AddEnemies(WorldTileConfigSO output)
        {
            foreach (var id in WeakEnemies) output.EnemyTable.Add(new WeightedEntryData { Id = id, Weight = Math.Max(10, 80 - output.Strength * 5) });
            if (output.Strength >= 4) foreach (var id in MidEnemies) output.EnemyTable.Add(new WeightedEntryData { Id = id, Weight = 20 + output.Strength * 3 });
            if (output.Strength >= 7) foreach (var id in EliteEnemies) output.EnemyTable.Add(new WeightedEntryData { Id = id, Weight = 10 + output.Strength * 2 });
            if (output.Strength >= 10) foreach (var id in BossEnemies) output.EnemyTable.Add(new WeightedEntryData { Id = id, Weight = 4 + output.Strength });
        }

        private static void AddEquipment(WorldTileConfigSO output)
        {
            output.EquipmentTable.Add(new WeightedEntryData { Id = "rusty_knife", Weight = 35 });
            output.EquipmentTable.Add(new WeightedEntryData { Id = "metal_pipe", Weight = 30 });
            output.EquipmentTable.Add(new WeightedEntryData { Id = "leather_jacket", Weight = 25 });
            output.EquipmentTable.Add(new WeightedEntryData { Id = "medkit", Weight = 20 });
            output.EquipmentTable.Add(new WeightedEntryData { Id = "toolkit", Weight = 18 });
            if (output.Strength >= 4) output.EquipmentTable.Add(new WeightedEntryData { Id = "rusty_revolver", Weight = 16 });
            if (output.Strength >= 6) output.EquipmentTable.Add(new WeightedEntryData { Id = "scrap_armor", Weight = 14 });
            if (output.Strength >= 8) output.EquipmentTable.Add(new WeightedEntryData { Id = "sawn_off_shotgun", Weight = 10 });
            if (output.Strength >= 10) output.EquipmentTable.Add(new WeightedEntryData { Id = "hunting_rifle", Weight = 7 });
        }

        private static void ResolveTravel(string category, int strength, out int food, out int water)
        {
            food = 1; water = 1;
            if (category == "grass" || category == "forest" || category == "grass_location") food = 0;
            if (category == "desert" || category == "hazard") water = 2 + strength / 6;
            if (category == "winter" || category == "winter_extra" || category == "winter_location") food = 2;
            if (category == "swamp") water = 2;
            if (category == "city") food = 2;
        }

        private static int ResolveStrength(ProductionTileSpec spec)
        {
            var ids = CampMapProductionV2Manifest.Markers.Select(m => m.LocationId).ToList();
            if (spec.Category == "future_location") return Math.Max(1, Math.Min(12, ids.IndexOf(spec.Id) - 4));
            if (spec.Category == "grass_location") return 2 + Math.Max(0, ids.IndexOf(spec.Id) - 17);
            if (spec.Category == "winter_location") return 3 + Math.Max(0, ids.IndexOf(spec.Id) - 23);
            if (spec.Id == "abandoned_store") return 1;
            if (spec.Id == "dry_suburb") return 2;
            if (spec.Id == "ruined_clinic") return 4;
            if (spec.Id == "police_outpost") return 6;
            if (spec.Id == "mutant_tunnel") return 8;
            if (spec.Id == "wheat_field" || spec.Id == "green_field" || spec.Category == "grass") return 1;
            if (spec.Category == "forest") return spec.Id.Contains("irradiated") ? 7 : spec.Id.Contains("dead") ? 4 : 2;
            if (spec.Category == "swamp") return spec.Id.Contains("toxic") ? 8 : 4;
            if (spec.Category == "hazard") return spec.Id.Contains("crater") ? 11 : 9;
            if (spec.Category == "city") return 6;
            if (spec.Category == "desert") return 4;
            if (spec.Category.StartsWith("winter", StringComparison.Ordinal)) return 4;
            return 3;
        }

        private static WorldTileType ResolveType(string category)
        {
            if (category == "map_border") return WorldTileType.Impassable;
            if (category == "camp" || category == "building") return WorldTileType.Camp;
            if (category == "expedition" || category == "future_location" || category == "grass_location" || category == "winter_location") return WorldTileType.Expedition;
            return WorldTileType.Normal;
        }

        private static void AddReward(WorldTileConfigSO output, string id, int min, int max, int weight)
        {
            output.ResourceRewards.Add(new LootTableEntryData { ResourceId = id, Min = min, Max = max, Weight = weight });
        }

        private static string Humanize(string id)
        {
            return string.Join(" ", id.Split('_').Select(word => string.IsNullOrEmpty(word) ? word : char.ToUpperInvariant(word[0]) + word.Substring(1)));
        }

        private static void EnsureFolder(string path)
        {
            var parts = path.Split('/');
            var current = parts[0];
            for (var i = 1; i < parts.Length; i++)
            {
                var next = current + "/" + parts[i];
                if (!AssetDatabase.IsValidFolder(next)) AssetDatabase.CreateFolder(current, parts[i]);
                current = next;
            }
        }
    }
}
