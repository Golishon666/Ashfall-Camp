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
        public void MushroomBedsUpgradeRaisesCapAndProducesFood()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>("Assets/AshfallCamp/Configs/GameConfigDatabase.asset");
            Assert.NotNull(database);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 49;

            var result = BuildingSystem.Upgrade(state, config, "mushroom_beds");
            Assert.IsTrue(result.Validation.IsValid, string.Join(", ", result.Validation.Errors));
            Assert.AreEqual(1, state.Buildings["mushroom_beds"].Level);
            Assert.AreEqual(70, state.ResourceCaps["food"]);

            BuildingSystem.TickProduction(state, config, 60);

            Assert.AreEqual(50, state.Resources["food"]);
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
        public void ScriptableObjectConfigValidationUsesConfiguredScrapResourceId()
        {
            var database = CreateValidDatabase();
            RenameResource(database, "scrap", "metal", "Metal");

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            var state = GameStateFactory.CreateNew(config, 0);
            var recruitmentCost = RecruitmentSystem.CalculateCost(state, config);

            Assert.IsTrue(config.Resources.ContainsKey("metal"));
            Assert.IsFalse(config.Resources.ContainsKey("scrap"));
            Assert.AreEqual("metal", config.Balance.RecruitmentScrapResourceId);
            Assert.IsTrue(recruitmentCost.ContainsKey("metal"));
            Assert.IsFalse(recruitmentCost.ContainsKey("scrap"));
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
            Assert.GreaterOrEqual(config.Enemies.Count, 6);
            Assert.GreaterOrEqual(config.Items.Count, 10);
            Assert.GreaterOrEqual(config.Buildings.Count, 6);
            Assert.GreaterOrEqual(CountBuildingUpgradeLevels(config), 24);
            Assert.AreEqual(60, config.Balance.EmergencyScavengeDurationSeconds);
            Assert.AreEqual(300, config.Balance.EmergencyScavengeCooldownSeconds);
            Assert.AreEqual(3, config.Balance.EmergencyScavengeRewards["scrap"]);
            Assert.AreEqual(2, config.Balance.EmergencyScavengeRewards["food"]);
            Assert.AreEqual(2, config.Balance.EmergencyScavengeRewards["water"]);
            Assert.AreEqual(5, config.Balance.ExpeditionCompletionXp);
            Assert.AreEqual("survival", config.Balance.ExpeditionCompletionSkillId);
            Assert.AreEqual(3, config.Balance.ExpeditionCompletionSkillXp);
            Assert.AreEqual(300, config.Balance.CampUpkeepIntervalSeconds);
            Assert.AreEqual("food", config.Balance.CampUpkeepFoodResourceId);
            Assert.AreEqual(1, config.Balance.CampUpkeepFoodPerSurvivor);
            Assert.AreEqual("water", config.Balance.CampUpkeepWaterResourceId);
            Assert.AreEqual(1, config.Balance.CampUpkeepWaterPerSurvivor);
            Assert.AreEqual(4, config.Balance.CampUpkeepShortageMoralePenalty);
            Assert.AreEqual(2, config.Balance.CampUpkeepShortageFatigue);
            Assert.AreEqual(10, config.Balance.RestFatigueRecoveryPerMinute);
            Assert.AreEqual(50, config.Balance.SurvivorXpThresholdBase);
            Assert.AreEqual(1.55, config.Balance.SurvivorXpThresholdExponent);
            Assert.AreEqual(20, config.Balance.SkillXpThresholdBase);
            Assert.AreEqual(1.35, config.Balance.SkillXpThresholdExponent);
            Assert.AreEqual("durability_loss", config.Balance.DurabilityTraitModifierId);
            Assert.AreEqual(1, config.Zones["abandoned_store"].DurabilityPressure);
            Assert.AreEqual(4, config.Zones["mutant_tunnel"].DurabilityPressure);
            Assert.AreEqual(-1, config.Policies["cautious"].DurabilityModifier);
            Assert.AreEqual(1, config.Policies["aggressive"].DurabilityModifier);
            Assert.AreEqual(1, config.Traits["clumsy"].StatModifiers["durability_loss"]);
            Assert.IsTrue(config.RecruitableSurvivors.ContainsKey("elias"));
            Assert.IsTrue(config.Buildings.ContainsKey("barracks"));
            Assert.IsTrue(config.Buildings.ContainsKey("workshop"));
            Assert.IsTrue(config.Buildings.ContainsKey("water_collector"));
            Assert.IsTrue(config.Buildings.ContainsKey("mushroom_beds"));
            Assert.IsTrue(config.Buildings.ContainsKey("infirmary"));
            Assert.IsTrue(config.Buildings.ContainsKey("radio_tower"));
            Assert.IsTrue(config.Zones.ContainsKey("abandoned_store"));
            Assert.IsTrue(config.Zones.ContainsKey("dry_suburb"));
            Assert.IsTrue(config.Zones.ContainsKey("ruined_clinic"));
            Assert.IsTrue(config.Zones.ContainsKey("police_outpost"));
            Assert.IsTrue(config.Zones.ContainsKey("mutant_tunnel"));
        }

        [Test]
        public void WeaponCatalogAssetContainsImportedIconsAndCombatRules()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>("Assets/AshfallCamp/Configs/GameConfigDatabase.asset");
            Assert.NotNull(database);
            Assert.NotNull(database.Weapons);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(94, config.Weapons.Count);
            Assert.IsTrue(config.Weapons.ContainsKey("weapon_melee_advanced_10_crusher_maul"));
            Assert.IsTrue(config.Weapons.ContainsKey("weapon_firearm_advanced_30_elite_wasteland_sniper"));

            var meleeCount = 0;
            var rangedCount = 0;
            var explosiveCount = 0;
            foreach (var weapon in database.Weapons.Weapons)
            {
                Assert.NotNull(weapon.Icon, weapon.Id + " icon is missing.");
                if (weapon.Type == WeaponCombatType.Melee)
                {
                    meleeCount++;
                    Assert.AreEqual(WeaponTargetingRule.FrontlineOnly, weapon.TargetingRule, weapon.Id);
                }
                else if (weapon.Type == WeaponCombatType.Ranged)
                {
                    rangedCount++;
                    Assert.AreEqual(WeaponTargetingRule.AnyEnemy, weapon.TargetingRule, weapon.Id);
                }
                else if (weapon.Type == WeaponCombatType.Explosive)
                {
                    explosiveCount++;
                    Assert.AreEqual(WeaponTargetingRule.AreaAnyEnemies, weapon.TargetingRule, weapon.Id);
                }
            }

            Assert.AreEqual(40, meleeCount);
            Assert.AreEqual(51, rangedCount);
            Assert.AreEqual(3, explosiveCount);
        }

        [Test]
        public void ScriptableObjectConfigValidationRejectsUnknownProgressionSkill()
        {
            var database = CreateValidDatabase();
            database.Balance.Balance.ExpeditionCompletionSkillId = "ghost_skill";

            var ex = Assert.Throws<InvalidOperationException>(() => new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult());

            StringAssert.Contains("unknown skill", ex.Message);
            DestroyDatabase(database);
        }

        [Test]
        public void ScriptableObjectConfigValidationRejectsNegativeDurabilityPressure()
        {
            var database = CreateValidDatabase();
            database.Zones.Zones[0].DurabilityPressure = -1;

            var ex = Assert.Throws<InvalidOperationException>(() => new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult());

            StringAssert.Contains("durability pressure", ex.Message);
            DestroyDatabase(database);
        }

        [Test]
        public void ScriptableObjectConfigValidationRejectsUnknownCampUpkeepResource()
        {
            var database = CreateValidDatabase();
            database.Balance.Balance.CampUpkeepFoodResourceId = "ghost_food";

            var ex = Assert.Throws<InvalidOperationException>(() => new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult());

            StringAssert.Contains("Camp upkeep references unknown resource", ex.Message);
            DestroyDatabase(database);
        }

        [Test]
        public void ScriptableObjectConfigValidationRejectsInvalidRestRecovery()
        {
            var database = CreateValidDatabase();
            database.Balance.Balance.RestFatigueRecoveryPerMinute = 0;

            var ex = Assert.Throws<InvalidOperationException>(() => new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult());

            StringAssert.Contains("Rest fatigue recovery", ex.Message);
            DestroyDatabase(database);
        }

        private static GameConfigDatabaseSO CreateValidDatabase()
        {
            var database = ScriptableObject.CreateInstance<GameConfigDatabaseSO>();
            database.Resources = ScriptableObject.CreateInstance<ResourceCatalogSO>();
            database.Resources.Resources.Add(new ResourceConfigData { Id = "scrap", Name = "Scrap", HasCap = false, StartAmount = 15 });
            database.Resources.Resources.Add(new ResourceConfigData { Id = "food", Name = "Food", HasCap = true, StartAmount = 8, StartCap = 50 });
            database.Resources.Resources.Add(new ResourceConfigData { Id = "water", Name = "Water", HasCap = true, StartAmount = 6, StartCap = 40 });
            database.Resources.Resources.Add(new ResourceConfigData { Id = "medicine", Name = "Medicine", HasCap = true, StartAmount = 1, StartCap = 20 });
            database.Resources.Resources.Add(new ResourceConfigData { Id = "weapon_parts", Name = "Weapon Parts", HasCap = false, StartAmount = 0 });

            database.Survivors = ScriptableObject.CreateInstance<SurvivorCatalogSO>();
            database.Survivors.StartingSurvivor = new StartingSurvivorConfigData
            {
                Name = "Mara",
                BackgroundId = "scavenger",
                TraitIds = new List<string> { "careful" },
                WeaponItemId = "rusty_knife",
                Skills = new List<IntPairData> { new IntPairData("scavenging", 4), new IntPairData("melee", 1), new IntPairData("survival", 2) }
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

            database.Weapons = ScriptableObject.CreateInstance<WeaponCatalogSO>();
            database.Weapons.Weapons.Add(new WeaponConfigData
            {
                Id = "rusty_knife",
                Name = "Rusty Knife",
                Description = "A dull camp knife kept sharp enough for close work.",
                Type = WeaponCombatType.Melee,
                Rarity = WeaponRarity.Common,
                Attack = 4,
                AttacksPerTurn = 1,
                TargetCount = 1,
                TargetingRule = WeaponTargetingRule.FrontlineOnly,
                HitChance = 0.86,
                ArmorPenetration = 0.05,
                CriticalChance = 0.04,
                MaxDurability = 80,
                RepairCostMultiplier = 1.0,
                AttackSoundId = "melee_blade"
            });

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
            database.Buildings.Buildings.Add(new BuildingConfigData
            {
                Id = "workshop",
                Name = "Workshop",
                StartsUnlocked = true,
                Levels = new List<BuildingLevelConfigData>
                {
                    new BuildingLevelConfigData { Level = 0 },
                    new BuildingLevelConfigData { Level = 1, Cost = new List<IntPairData> { new IntPairData("scrap", 35) } }
                }
            });
            database.Buildings.Buildings.Add(new BuildingConfigData
            {
                Id = "infirmary",
                Name = "Infirmary",
                StartsUnlocked = true,
                AffectedResourceId = "medicine",
                Levels = new List<BuildingLevelConfigData>
                {
                    new BuildingLevelConfigData { Level = 0, ResourceCap = 20 },
                    new BuildingLevelConfigData { Level = 1, Cost = new List<IntPairData> { new IntPairData("scrap", 50), new IntPairData("medicine", 2) }, ResourceCap = 30 }
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
            database.Balance.Balance.EmergencyScavengeRewards.Add(new IntPairData("scrap", 3));
            database.Balance.Balance.EmergencyScavengeRewards.Add(new IntPairData("food", 2));
            database.Balance.Balance.EmergencyScavengeRewards.Add(new IntPairData("water", 2));
            return database;
        }

        private static void RenameResource(GameConfigDatabaseSO database, string oldId, string newId, string newName)
        {
            foreach (var resource in database.Resources.Resources)
            {
                if (resource.Id == oldId)
                {
                    resource.Id = newId;
                    resource.Name = newName;
                }
            }

            foreach (var building in database.Buildings.Buildings)
            {
                if (building.AffectedResourceId == oldId) building.AffectedResourceId = newId;
                if (building.ProducedResourceId == oldId) building.ProducedResourceId = newId;
                foreach (var level in building.Levels)
                {
                    RenamePairs(level.Cost, oldId, newId);
                }
            }

            foreach (var zone in database.Zones.Zones)
            {
                foreach (var loot in zone.LootTable)
                {
                    if (loot.ResourceId == oldId)
                    {
                        loot.ResourceId = newId;
                    }
                }
            }

            var balance = database.Balance.Balance;
            if (balance.RecruitmentScrapResourceId == oldId) balance.RecruitmentScrapResourceId = newId;
            if (balance.RecruitmentFoodResourceId == oldId) balance.RecruitmentFoodResourceId = newId;
            if (balance.RecruitmentWaterResourceId == oldId) balance.RecruitmentWaterResourceId = newId;
            if (balance.ExpeditionFoodResourceId == oldId) balance.ExpeditionFoodResourceId = newId;
            if (balance.ExpeditionWaterResourceId == oldId) balance.ExpeditionWaterResourceId = newId;
            if (balance.CampUpkeepFoodResourceId == oldId) balance.CampUpkeepFoodResourceId = newId;
            if (balance.CampUpkeepWaterResourceId == oldId) balance.CampUpkeepWaterResourceId = newId;
            if (balance.WorkshopRepairResourceId == oldId) balance.WorkshopRepairResourceId = newId;
            if (balance.HealingMedicineResourceId == oldId) balance.HealingMedicineResourceId = newId;
            RenamePairs(balance.EmergencyScavengeRewards, oldId, newId);
        }

        private static void RenamePairs(IEnumerable<IntPairData> pairs, string oldId, string newId)
        {
            foreach (var pair in pairs)
            {
                if (pair.Id == oldId)
                {
                    pair.Id = newId;
                }
            }
        }

        private static void AssertResource(GameConfigSnapshot config, string id, int startAmount, bool hasCap, int startCap)
        {
            Assert.IsTrue(config.Resources.ContainsKey(id), "Missing resource: " + id);
            var resource = config.Resources[id];
            Assert.AreEqual(startAmount, resource.StartAmount, id + " start amount");
            Assert.AreEqual(hasCap, resource.HasCap, id + " cap flag");
            Assert.AreEqual(startCap, resource.StartCap, id + " start cap");
        }

        private static int CountBuildingUpgradeLevels(GameConfigSnapshot config)
        {
            var count = 0;
            foreach (var building in config.Buildings.Values)
            {
                for (var i = 0; i < building.Levels.Count; i++)
                {
                    if (building.Levels[i].Level > 0)
                    {
                        count++;
                    }
                }
            }

            return count;
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
            UnityEngine.Object.DestroyImmediate(database.Weapons);
            UnityEngine.Object.DestroyImmediate(database.Buildings);
            UnityEngine.Object.DestroyImmediate(database.Zones);
            UnityEngine.Object.DestroyImmediate(database.Balance);
            UnityEngine.Object.DestroyImmediate(database);
        }
    }
}
