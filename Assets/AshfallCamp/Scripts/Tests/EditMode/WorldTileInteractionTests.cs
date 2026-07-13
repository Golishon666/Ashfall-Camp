using System.Linq;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using AshfallCamp.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class WorldTileInteractionTests
    {
        [Test]
        public void CatalogClassifiesEveryProductionTile()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WorldTileCatalogSO>("Assets/AshfallCamp/Configs/World/WorldTileCatalog.asset");
            Assert.NotNull(catalog);
            Assert.AreEqual(97, catalog.Tiles.Count);
            Assert.AreEqual(97, catalog.Tiles.Select(tile => tile.Id).Distinct().Count());
            Assert.AreEqual(47, catalog.Tiles.Count(tile => tile.Type == WorldTileType.Normal));
            Assert.AreEqual(29, catalog.Tiles.Count(tile => tile.Type == WorldTileType.Expedition));
            Assert.AreEqual(20, catalog.Tiles.Count(tile => tile.Type == WorldTileType.Camp));
            Assert.AreEqual(1, catalog.Tiles.Count(tile => tile.Type == WorldTileType.Impassable));
            Assert.True(catalog.Tiles.All(tile => tile.ContentTile != null));
        }

        [Test]
        public void EveryPlayableTileHasItsOwnZoneAndBalance()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>("Assets/AshfallCamp/Configs/Core/GameConfigDatabase.asset");
            Assert.NotNull(database);
            Assert.NotNull(database.WorldTiles);
            foreach (var tile in database.WorldTiles.Tiles.Where(tile => tile.Type == WorldTileType.Normal || tile.Type == WorldTileType.Expedition))
            {
                Assert.False(string.IsNullOrWhiteSpace(tile.ZoneId), tile.Id);
                var zone = database.Zones.Zones.SingleOrDefault(item => item.Id == tile.ZoneId);
                Assert.NotNull(zone, tile.Id);
                Assert.AreEqual(tile.Type == WorldTileType.Expedition, zone.ShowInExpeditionMenu, tile.Id);
                Assert.Greater(tile.EnemyTable.Count, 0, tile.Id);
                Assert.Greater(tile.ResourceRewards.Count, 0, tile.Id);
                Assert.Greater(tile.EquipmentTable.Count, 0, tile.Id);
            }
        }

        [Test]
        public void WheatIsWeakAndRadioactiveTilesAreDangerous()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<WorldTileCatalogSO>("Assets/AshfallCamp/Configs/World/WorldTileCatalog.asset");
            var wheat = catalog.Tiles.Single(tile => tile.Id == "wheat_field");
            var crater = catalog.Tiles.Single(tile => tile.Id == "nuclear_crater");
            var barrels = catalog.Tiles.Single(tile => tile.Id == "waste_barrels_zone");
            Assert.AreEqual(1, wheat.Strength);
            Assert.True(wheat.ResourceRewards.Any(entry => entry.ResourceId == "food"));
            Assert.Greater(crater.Strength, wheat.Strength);
            Assert.Greater(barrels.EncounterChance, wheat.EncounterChance);
            Assert.Greater(crater.WaterCostPerSurvivor, wheat.WaterCostPerSurvivor);
        }

        [Test]
        public void RoundTripCostCountsEveryRouteCellTwice()
        {
            var config = new GameConfigSnapshot();
            config.Balance.ExpeditionFoodResourceId = "food";
            config.Balance.ExpeditionWaterResourceId = "water";
            config.WorldTiles["forest"] = new WorldTileDefinition { Id = "forest", Type = WorldTileType.Normal, FoodCostPerSurvivor = 0, WaterCostPerSurvivor = 1 };
            config.WorldTiles["desert"] = new WorldTileDefinition { Id = "desert", Type = WorldTileType.Normal, FoodCostPerSurvivor = 1, WaterCostPerSurvivor = 3 };
            var cost = WorldTileTravelSystem.CalculateRoundTripCost(config, new[] { "forest", "desert" }, 4);
            Assert.AreEqual(8, cost["food"]);
            Assert.AreEqual(32, cost["water"]);
        }

        [Test]
        public void LaunchSpendsRouteSuppliesAndCompletionAppliesReturnRewards()
        {
            var config = new GameConfigSnapshot();
            config.Balance.ExpeditionFoodResourceId = "food";
            config.Balance.ExpeditionWaterResourceId = "water";
            config.Policies[GameIds.Policies.Balanced] = new ExpeditionPolicyDefinition();
            config.Zones["gather_test"] = new ZoneDefinition { Id = "gather_test", Name = "Test", BaseDurationSeconds = 10, MinDurationSeconds = 1, MaxDurationSeconds = 20 };
            config.WorldTiles["forest"] = new WorldTileDefinition { Id = "forest", Type = WorldTileType.Normal, WaterCostPerSurvivor = 1, EncounterChance = 0 };
            config.WorldTiles["desert"] = new WorldTileDefinition { Id = "desert", Type = WorldTileType.Normal, FoodCostPerSurvivor = 1, WaterCostPerSurvivor = 3, EncounterChance = 0 };
            config.WorldTiles["forest"].ResourceRewards.Add(new LootTableEntry { ResourceId = "food", Min = 2, Max = 2, Weight = 100 });
            var state = new GameState { SquadSize = 4 };
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            state.Zones["gather_test"] = new ZoneState { Id = "gather_test", IsUnlocked = true };
            state.Survivors.Add(new SurvivorState { Id = "s1", Name = "Asha", State = SurvivorActivityState.Idle, Health = 20, MaxHealth = 20 });
            var result = ExpeditionLauncher.Launch(state, config, new LaunchExpeditionRequest
            {
                ZoneId = "gather_test",
                WorldTileId = "forest",
                SurvivorIds = { "s1" },
                RouteTileIds = { "forest", "desert" },
                PolicyId = GameIds.Policies.Balanced,
                ConfirmWarnings = true,
                Seed = 7
            });
            Assert.True(result.Validation.IsValid, string.Join(" ", result.Validation.Errors));
            Assert.AreEqual(18, state.Resources["food"]);
            Assert.AreEqual(12, state.Resources["water"]);
            ExpeditionSimulator.Complete(state, config, result.Expedition);
            Assert.AreEqual(ExpeditionStatus.Completed, result.Expedition.Status);
            Assert.GreaterOrEqual(state.Resources["food"], 22);
            Assert.AreEqual(SurvivorActivityState.Idle, state.Survivors[0].State);
        }

        [Test]
        public void FigmaTooltipPrefabRemainsEditableUgUi()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/FIGUNITY/Prefabs/PF_11WorldTileTooltipElementwise.prefab");
            Assert.NotNull(prefab);
            Assert.NotNull(Find(prefab.transform, "WorldTileTooltip"));
            Assert.NotNull(Find(prefab.transform, "SquadRosterModal"));
            Assert.AreEqual(50, prefab.GetComponentsInChildren<TextMeshProUGUI>(true).Length);
            Assert.AreEqual(9, prefab.GetComponentsInChildren<Button>(true).Length);
            Assert.AreEqual(1, prefab.GetComponentsInChildren<Slider>(true).Length);
        }

        [Test]
        public void WorldMapPrefabHasInteractionAndMarkerLayer()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>("Assets/AshfallCamp/Scenes/WorldTileMap.prefab");
            var view = prefab.GetComponent<CampWorldTileView>();
            Assert.NotNull(view);
            var serialized = new SerializedObject(view);
            Assert.NotNull(serialized.FindProperty("contentTilemap").objectReferenceValue);
            var markerTilemap = serialized.FindProperty("markerTilemap").objectReferenceValue as UnityEngine.Tilemaps.Tilemap;
            Assert.NotNull(markerTilemap);
            Assert.True(markerTilemap.gameObject.activeSelf);
            Assert.NotNull(serialized.FindProperty("tooltipPrefab").objectReferenceValue);
            Assert.GreaterOrEqual(serialized.FindProperty("uiSortingOrder").intValue, 110);
        }

        private static Transform Find(Transform root, string name)
        {
            if (root.name == name) return root;
            foreach (Transform child in root)
            {
                var result = Find(child, name);
                if (result != null) return result;
            }
            return null;
        }
    }
}
