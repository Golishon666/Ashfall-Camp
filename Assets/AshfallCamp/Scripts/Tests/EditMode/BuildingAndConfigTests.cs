using System;
using System.Collections.Generic;
using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class BuildingAndConfigTests
    {
        [Test]
        public void BuildingUpgradeValidationBlocksUnknownLockedAndUnaffordable()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);

            var unknown = BuildingSystem.ValidateUpgrade(state, config, "missing");
            Assert.IsFalse(unknown.IsValid);
            Assert.Contains("Unknown building.", unknown.Errors);

            state.Buildings["workshop"].IsUnlocked = false;
            var locked = BuildingSystem.ValidateUpgrade(state, config, "workshop");
            Assert.IsFalse(locked.IsValid);
            Assert.Contains("Building is locked.", locked.Errors);

            var unaffordable = BuildingSystem.ValidateUpgrade(state, config, "barracks");
            Assert.IsFalse(unaffordable.IsValid);
            Assert.Contains("Not enough resources.", unaffordable.Errors);
        }

        [Test]
        public void BuildingUpgradeSpendsResourcesAndRaisesCampLimits()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;

            var first = BuildingSystem.Upgrade(state, config, "barracks");
            Assert.IsTrue(first.Validation.IsValid);
            Assert.AreEqual(1, state.Buildings["barracks"].Level);
            Assert.AreEqual(75, state.Resources["scrap"]);
            Assert.AreEqual(16, state.Resources["food"]);
            Assert.AreEqual(2, state.SurvivorCap);
            Assert.AreEqual(1, state.SquadSize);

            var second = BuildingSystem.Upgrade(state, config, "barracks");
            Assert.IsTrue(second.Validation.IsValid);
            Assert.AreEqual(2, state.Buildings["barracks"].Level);
            Assert.AreEqual(15, state.Resources["scrap"]);
            Assert.AreEqual(8, state.Resources["food"]);
            Assert.AreEqual(3, state.SurvivorCap);
            Assert.AreEqual(2, state.SquadSize);
        }

        [Test]
        public void WaterCollectorUpgradeRaisesCapAndProducesWater()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;

            var result = BuildingSystem.Upgrade(state, config, "water_collector");
            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(60, state.ResourceCaps["water"]);

            BuildingSystem.TickProduction(state, config, 60);

            Assert.AreEqual(7, state.Resources["water"]);
        }

        [Test]
        public void BuildingUpgradeRefreshesZoneUnlocks()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;

            Assert.IsFalse(state.Zones["police_outpost"].IsUnlocked);

            var result = BuildingSystem.Upgrade(state, config, "workshop");

            Assert.IsTrue(result.Validation.IsValid);
            Assert.IsTrue(state.Zones["police_outpost"].IsUnlocked);
        }

        [Test]
        public void UpgradeBuildingUseCaseMutatesStore()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new UpgradeBuildingUseCase(store, new StaticConfigProvider(config));

            var result = useCase.ExecuteAsync("barracks", CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(1, store.State.CurrentValue.Buildings["barracks"].Level);
        }

        [Test]
        public void ScriptableObjectConfigValidationRejectsDuplicateIds()
        {
            var database = CreateValidDatabase();
            database.Resources.Resources.Add(new ResourceConfigData { Id = "scrap", Name = "Duplicate Scrap", StartAmount = 0 });

            var ex = Assert.Throws<InvalidOperationException>(() => new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult());

            StringAssert.Contains("duplicate id", ex.Message);
            DestroyDatabase(database);
        }

        [Test]
        public void ScriptableObjectConfigValidationRejectsInvalidReferences()
        {
            var database = CreateValidDatabase();
            database.Zones.Zones[0].LootTable[0].ResourceId = "ghost_resource";

            var ex = Assert.Throws<InvalidOperationException>(() => new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult());

            StringAssert.Contains("unknown resource", ex.Message);
            DestroyDatabase(database);
        }

        [Test]
        public void StarterConfigAssetMatchesBootCoreDefaults()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>("Assets/AshfallCamp/Configs/GameConfigDatabase.asset");
            Assert.NotNull(database);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            AssertResource(config, "scrap", 15, false, 0);
            AssertResource(config, "food", 8, true, 50);
            AssertResource(config, "water", 6, true, 40);
            AssertResource(config, "medicine", 1, true, 20);
            Assert.AreEqual("Mara", config.StartingSurvivor.Name);
            Assert.AreEqual("rusty_knife", config.StartingSurvivor.WeaponItemId);
            Assert.GreaterOrEqual(config.RecruitableSurvivors.Count, 6);
            Assert.IsTrue(config.RecruitableSurvivors.ContainsKey("elias"));
            Assert.IsTrue(config.Zones.ContainsKey("abandoned_store"));
            Assert.IsTrue(config.Zones.ContainsKey("dry_suburb"));
            Assert.IsTrue(config.Zones.ContainsKey("ruined_clinic"));
            Assert.IsTrue(config.Zones.ContainsKey("police_outpost"));
            Assert.IsTrue(config.Zones.ContainsKey("mutant_tunnel"));
        }

        private static GameConfigDatabaseSO CreateValidDatabase()
        {
            var database = ScriptableObject.CreateInstance<GameConfigDatabaseSO>();
            database.Resources = ScriptableObject.CreateInstance<ResourceCatalogSO>();
            database.Resources.Resources.Add(new ResourceConfigData { Id = "scrap", Name = "Scrap", HasCap = false, StartAmount = 15 });
            database.Resources.Resources.Add(new ResourceConfigData { Id = "food", Name = "Food", HasCap = true, StartAmount = 8, StartCap = 50 });
            database.Resources.Resources.Add(new ResourceConfigData { Id = "water", Name = "Water", HasCap = true, StartAmount = 6, StartCap = 40 });
            database.Resources.Resources.Add(new ResourceConfigData { Id = "medicine", Name = "Medicine", HasCap = true, StartAmount = 1, StartCap = 20 });

            database.Survivors = ScriptableObject.CreateInstance<SurvivorCatalogSO>();
            database.Survivors.StartingSurvivor = new StartingSurvivorConfigData
            {
                Name = "Mara",
                BackgroundId = "scavenger",
                TraitIds = new List<string> { "careful" },
                WeaponItemId = "rusty_knife",
                Skills = new List<IntPairData> { new IntPairData("scavenging", 4), new IntPairData("melee", 1) }
            };
            database.Survivors.RecruitableSurvivors.Add(new RecruitableSurvivorConfigData
            {
                Id = "elias",
                Name = "Elias",
                BackgroundId = "scavenger",
                TraitIds = new List<string> { "careful" },
                WeaponItemId = "rusty_knife",
                Skills = new List<IntPairData> { new IntPairData("scavenging", 1), new IntPairData("melee", 1) }
            });

            database.Backgrounds = ScriptableObject.CreateInstance<BackgroundCatalogSO>();
            database.Backgrounds.Backgrounds.Add(new BackgroundConfigData { Id = "scavenger", Name = "Scavenger" });

            database.Traits = ScriptableObject.CreateInstance<TraitCatalogSO>();
            database.Traits.Traits.Add(new TraitConfigData { Id = "careful", Name = "Careful" });

            database.Policies = ScriptableObject.CreateInstance<ExpeditionPolicyCatalogSO>();
            database.Policies.Policies.Add(new ExpeditionPolicyConfigData { Id = "balanced", Name = "Balanced" });

            database.Enemies = ScriptableObject.CreateInstance<EnemyCatalogSO>();
            database.Enemies.Enemies.Add(new EnemyConfigData { Id = "feral_dog", Name = "Feral Dog", MaxHealth = 14, BaseDamage = 3, Accuracy = 0.75 });

            database.Items = ScriptableObject.CreateInstance<ItemCatalogSO>();
            database.Items.Items.Add(new ItemConfigData { Id = "rusty_knife", Name = "Rusty Knife", Slot = ItemSlot.Weapon, WeaponType = WeaponType.Melee, BaseDamage = 4, MaxDurability = 80 });

            database.Buildings = ScriptableObject.CreateInstance<BuildingCatalogSO>();
            database.Buildings.Buildings.Add(new BuildingConfigData
            {
                Id = "barracks",
                Name = "Barracks",
                StartsUnlocked = true,
                Levels = new List<BuildingLevelConfigData>
                {
                    new BuildingLevelConfigData { Level = 0, SurvivorCap = 1, SquadSize = 1 },
                    new BuildingLevelConfigData { Level = 1, Cost = new List<IntPairData> { new IntPairData("scrap", 25) }, SurvivorCap = 2, SquadSize = 1 }
                }
            });
            database.Buildings.Buildings.Add(new BuildingConfigData
            {
                Id = "radio_tower",
                Name = "Radio Tower",
                StartsUnlocked = true,
                Levels = new List<BuildingLevelConfigData>
                {
                    new BuildingLevelConfigData { Level = 0 }
                }
            });

            database.Zones = ScriptableObject.CreateInstance<ZoneCatalogSO>();
            database.Zones.Zones.Add(new ZoneConfigData
            {
                Id = "abandoned_store",
                Name = "Abandoned Store",
                BaseDurationSeconds = 45,
                MinDurationSeconds = 30,
                MaxDurationSeconds = 90,
                FoodCostPerSurvivor = 1,
                RecommendedPower = 5,
                EnemyTable = new List<WeightedEntryData> { new WeightedEntryData("feral_dog", 100) },
                LootTable = new List<LootTableEntryData> { new LootTableEntryData("scrap", 4, 10, 100) }
            });

            database.Balance = ScriptableObject.CreateInstance<BalanceConfigSO>();
            database.Balance.Balance = new BalanceConfigData();
            return database;
        }

        private static void AssertResource(GameConfigSnapshot config, string id, int startAmount, bool hasCap, int startCap)
        {
            Assert.IsTrue(config.Resources.ContainsKey(id), "Missing resource: " + id);
            var resource = config.Resources[id];
            Assert.AreEqual(startAmount, resource.StartAmount, id + " start amount");
            Assert.AreEqual(hasCap, resource.HasCap, id + " cap flag");
            Assert.AreEqual(startCap, resource.StartCap, id + " start cap");
        }

        private static void DestroyDatabase(GameConfigDatabaseSO database)
        {
            UnityEngine.Object.DestroyImmediate(database.Resources);
            UnityEngine.Object.DestroyImmediate(database.Survivors);
            UnityEngine.Object.DestroyImmediate(database.Backgrounds);
            UnityEngine.Object.DestroyImmediate(database.Traits);
            UnityEngine.Object.DestroyImmediate(database.Policies);
            UnityEngine.Object.DestroyImmediate(database.Enemies);
            UnityEngine.Object.DestroyImmediate(database.Items);
            UnityEngine.Object.DestroyImmediate(database.Buildings);
            UnityEngine.Object.DestroyImmediate(database.Zones);
            UnityEngine.Object.DestroyImmediate(database.Balance);
            UnityEngine.Object.DestroyImmediate(database);
        }
    }
}
