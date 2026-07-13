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
        private const string GameConfigDatabasePath = "Assets/AshfallCamp/Configs/Core/GameConfigDatabase.asset";

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

            var first = TestBuildingUpgrades.UpgradeAndComplete(state, config, "barracks");
            Assert.IsTrue(first.Validation.IsValid);
            Assert.AreEqual(1, state.Buildings["barracks"].Level);
            Assert.AreEqual(75, state.Resources["scrap"]);
            Assert.AreEqual(16, state.Resources["food"]);
            Assert.AreEqual(2, state.SurvivorCap);
            Assert.AreEqual(1, state.SquadSize);

            var second = TestBuildingUpgrades.UpgradeAndComplete(state, config, "barracks");
            Assert.IsTrue(second.Validation.IsValid);
            Assert.AreEqual(2, state.Buildings["barracks"].Level);
            Assert.AreEqual(15, state.Resources["scrap"]);
            Assert.AreEqual(8, state.Resources["food"]);
            Assert.AreEqual(3, state.SurvivorCap);
            Assert.AreEqual(2, state.SquadSize);
        }

        [Test]
        public void BuildingUpgradeStartsTimerAndBlocksDuplicateUpgrade()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;

            var result = BuildingSystem.Upgrade(state, config, "barracks", 1000);

            Assert.IsTrue(result.Validation.IsValid);
            Assert.IsTrue(result.Started);
            Assert.AreEqual(1, result.TargetLevel);
            Assert.Greater(result.DurationSeconds, 0);
            Assert.AreEqual(0, state.Buildings["barracks"].Level);
            Assert.AreEqual(75, state.Resources["scrap"]);
            Assert.AreEqual(16, state.Resources["food"]);
            Assert.AreEqual(1000, state.Buildings["barracks"].UpgradeStartedAtUnixMs);
            Assert.Greater(state.Buildings["barracks"].UpgradeFinishedAtUnixMs, state.Buildings["barracks"].UpgradeStartedAtUnixMs);

            var duplicate = BuildingSystem.ValidateUpgrade(state, config, "barracks");
            Assert.IsFalse(duplicate.IsValid);
            Assert.Contains("Building upgrade is already in progress.", duplicate.Errors);
        }

        [Test]
        public void BuildingUpgradeCompletesOnlyAfterTimerFinishes()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;

            BuildingSystem.Upgrade(state, config, "barracks", 1000);
            var finishAt = state.Buildings["barracks"].UpgradeFinishedAtUnixMs;

            var early = BuildingSystem.CompleteReadyUpgrades(state, config, finishAt - 1);
            Assert.AreEqual(0, early.Count);
            Assert.AreEqual(0, state.Buildings["barracks"].Level);

            var completed = BuildingSystem.CompleteReadyUpgrades(state, config, finishAt);
            Assert.AreEqual(1, completed.Count);
            Assert.AreEqual("barracks", completed[0]);
            Assert.AreEqual(1, state.Buildings["barracks"].Level);
            Assert.AreEqual(0, state.Buildings["barracks"].UpgradeStartedAtUnixMs);
            Assert.AreEqual(0, state.Buildings["barracks"].UpgradeFinishedAtUnixMs);
            Assert.AreEqual(2, state.SurvivorCap);
        }

        [Test]
        public void WaterCollectorUpgradeRaisesCapAndProducesWater()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;

            var result = TestBuildingUpgrades.UpgradeAndComplete(state, config, "water_collector");
            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(60, state.ResourceCaps["water"]);

            BuildingSystem.TickProduction(state, config, 60);

            Assert.AreEqual(7, state.Resources["water"]);
        }

        [Test]
        public void MushroomBedsUpgradeRaisesCapAndProducesFood()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);
            Assert.NotNull(database);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 49;

            var result = TestBuildingUpgrades.UpgradeAndComplete(state, config, "mushroom_beds");
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

            var result = TestBuildingUpgrades.UpgradeAndComplete(state, config, "workshop");

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
            BuildingSystem.CompleteReadyUpgrades(store.State.CurrentValue, config, long.MaxValue);
            Assert.AreEqual(1, store.State.CurrentValue.Buildings["barracks"].Level);
        }

        [Test]
        public void BuildingConfigAssetsDefineLevelsZeroThroughTen()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);
            Assert.NotNull(database);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            foreach (var building in config.Buildings.Values)
            {
                for (var level = 0; level <= 10; level++)
                {
                    var definition = BuildingSystem.GetLevel(building, level);
                    Assert.NotNull(definition, building.Id + " level " + level + " is missing.");
                    if (level > 0)
                    {
                        Assert.Greater(definition.UpgradeDurationSeconds, 0, building.Id + " level " + level + " duration");
                    }
                }
            }
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
            database.Balance.Balance.RecruitmentBroadcastCost.Clear();

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
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);
            Assert.NotNull(database);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            AssertResource(config, "scrap", 15, false, 0);
            AssertResource(config, "food", 8, true, 50);
            AssertResource(config, "water", 6, true, 40);
            AssertResource(config, "medicine", 1, true, 20);
            Assert.AreEqual("Asha", config.StartingSurvivor.Name);
            Assert.IsFalse(string.IsNullOrWhiteSpace(config.StartingSurvivor.PortraitId));
            Assert.AreEqual("rusty_knife", config.StartingSurvivor.WeaponItemId);
            Assert.IsTrue(config.Weapons.ContainsKey(config.StartingSurvivor.WeaponConfigId));
            Assert.IsTrue(config.Armor.ContainsKey(config.StartingSurvivor.ArmorConfigId));
            Assert.IsTrue(config.Utilities.ContainsKey(config.StartingSurvivor.UtilityConfigId));
            Assert.AreEqual(43, config.RecruitableSurvivors.Count);
            Assert.AreEqual(61, config.Enemies.Count);
            Assert.GreaterOrEqual(config.Items.Count, 10);
            Assert.AreEqual(124, config.Weapons.Count);
            Assert.AreEqual(60, config.Armor.Count);
            Assert.AreEqual(20, config.Utilities.Count);
            Assert.GreaterOrEqual(config.Buildings.Count, 6);
            Assert.GreaterOrEqual(CountBuildingUpgradeLevels(config), 24);
            Assert.AreEqual(60, config.Balance.EmergencyScavengeDurationSeconds);
            Assert.AreEqual(300, config.Balance.EmergencyScavengeCooldownSeconds);
            Assert.AreEqual(3, config.Balance.EmergencyScavengeRewards["scrap"]);
            Assert.AreEqual(8, config.Balance.EmergencyScavengeRewards["food"]);
            Assert.AreEqual(8, config.Balance.EmergencyScavengeRewards["water"]);
            Assert.AreEqual(1, config.Balance.EmergencyScavengeRewards["weapon_parts"]);
            Assert.AreEqual(1, config.Balance.EmergencyScavengeRewards["medicine"]);
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
            Assert.IsTrue(config.RecruitableSurvivors.ContainsKey("survivor_02"));
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
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);
            Assert.NotNull(database);
            Assert.NotNull(database.Weapons);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(124, config.Weapons.Count);
            Assert.IsTrue(config.Weapons.ContainsKey("weapon_melee_advanced_10_crusher_maul"));
            Assert.IsTrue(config.Weapons.ContainsKey("weapon_firearm_advanced_30_elite_wasteland_sniper"));
            Assert.IsTrue(config.Weapons.ContainsKey("weapon_creature_boss_apex_claw_titan_apex_titan_claws"));

            var meleeCount = 0;
            var rangedCount = 0;
            var explosiveCount = 0;
            foreach (var weapon in database.Weapons.Weapons)
            {
                Assert.NotNull(weapon.Icon, weapon.Id + " icon is missing.");
                Assert.GreaterOrEqual(weapon.Durability, 0f, weapon.Id);
                Assert.LessOrEqual(weapon.Durability, 1f, weapon.Id);
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

            Assert.AreEqual(62, meleeCount);
            Assert.AreEqual(56, rangedCount);
            Assert.AreEqual(6, explosiveCount);
        }

        [Test]
        public void ArmorAndUtilityCatalogAssetsContainImportedIconsAndProgressionStats()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);
            Assert.NotNull(database);
            Assert.NotNull(database.Armor);
            Assert.NotNull(database.Utilities);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(60, config.Armor.Count);
            Assert.AreEqual(20, config.Utilities.Count);
            Assert.IsTrue(config.Armor.ContainsKey("armor_30_legendary_plated_survival_armor"));
            Assert.IsTrue(config.Armor.ContainsKey("armor_midgame_30_veteran_scavenger_armor"));
            Assert.IsTrue(config.Utilities.ContainsKey("utility_medkit_tier_05_advanced_trauma_case"));
            Assert.IsTrue(config.Utilities.ContainsKey("utility_backpack_tier_05_elite_cargo_pack"));

            var lightCount = 0;
            var mediumCount = 0;
            var heavyCount = 0;
            foreach (var armor in database.Armor.Armor)
            {
                Assert.NotNull(armor.Icon, armor.Id + " icon is missing.");
                Assert.GreaterOrEqual(armor.Defense, 0, armor.Id);
                Assert.GreaterOrEqual(armor.Durability, 0f, armor.Id);
                Assert.LessOrEqual(armor.Durability, 1f, armor.Id);
                Assert.GreaterOrEqual(armor.MaxDurability, 1, armor.Id);
                if (armor.Type == ArmorType.Light) lightCount++;
                if (armor.Type == ArmorType.Medium) mediumCount++;
                if (armor.Type == ArmorType.Heavy) heavyCount++;
            }

            Assert.Greater(lightCount, 0);
            Assert.Greater(mediumCount, 0);
            Assert.Greater(heavyCount, 0);

            var medkits = 0;
            var toolkits = 0;
            var ammoPacks = 0;
            var backpacks = 0;
            foreach (var utility in database.Utilities.Utilities)
            {
                Assert.NotNull(utility.Icon, utility.Id + " icon is missing.");
                Assert.GreaterOrEqual(utility.Tier, 1, utility.Id);
                Assert.LessOrEqual(utility.Tier, 5, utility.Id);
                if (utility.Type == UtilityEquipmentType.Medkit) medkits++;
                if (utility.Type == UtilityEquipmentType.Toolkit) toolkits++;
                if (utility.Type == UtilityEquipmentType.AmmoPack) ammoPacks++;
                if (utility.Type == UtilityEquipmentType.Backpack) backpacks++;
            }

            Assert.AreEqual(5, medkits);
            Assert.AreEqual(5, toolkits);
            Assert.AreEqual(5, ammoPacks);
            Assert.AreEqual(5, backpacks);
        }

        [Test]
        public void CharacterCatalogAssetsContainPortraitsAndEquipmentLinks()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);
            Assert.NotNull(database);
            Assert.NotNull(database.Survivors);
            Assert.NotNull(database.Enemies);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.NotNull(database.Survivors.StartingSurvivor.Portrait);
            var usedPortraits = new HashSet<Sprite>();
            Assert.IsTrue(usedPortraits.Add(database.Survivors.StartingSurvivor.Portrait), "Starting survivor portrait is reused.");
            Assert.AreEqual(43, database.Survivors.RecruitableSurvivors.Count);
            Assert.AreEqual(61, database.Enemies.Enemies.Count);
            Assert.IsTrue(config.Weapons.ContainsKey(config.StartingSurvivor.WeaponConfigId));
            Assert.IsTrue(config.Armor.ContainsKey(config.StartingSurvivor.ArmorConfigId));
            Assert.IsTrue(config.Utilities.ContainsKey(config.StartingSurvivor.UtilityConfigId));

            foreach (var survivor in database.Survivors.RecruitableSurvivors)
            {
                Assert.NotNull(survivor.Portrait, survivor.Id + " portrait is missing.");
                Assert.IsTrue(usedPortraits.Add(survivor.Portrait), survivor.Id + " portrait is reused.");
                Assert.IsTrue(config.Weapons.ContainsKey(survivor.WeaponConfigId), survivor.Id + " weapon config is missing.");
                Assert.IsTrue(config.Armor.ContainsKey(survivor.ArmorConfigId), survivor.Id + " armor config is missing.");
                Assert.IsTrue(config.Utilities.ContainsKey(survivor.UtilityConfigId), survivor.Id + " utility config is missing.");
            }

            var humanEnemyCount = 0;
            var creatureEnemyCount = 0;
            foreach (var enemy in database.Enemies.Enemies)
            {
                Assert.NotNull(enemy.Portrait, enemy.Id + " portrait is missing.");
                Assert.IsTrue(usedPortraits.Add(enemy.Portrait), enemy.Id + " portrait is reused.");
                Assert.IsTrue(config.Weapons.ContainsKey(enemy.WeaponConfigId), enemy.Id + " weapon config is missing.");
                if (enemy.Kind != EnemyKind.Human)
                {
                    creatureEnemyCount++;
                    continue;
                }

                humanEnemyCount++;
                Assert.IsTrue(config.Armor.ContainsKey(enemy.ArmorConfigId), enemy.Id + " armor config is missing.");
                Assert.IsTrue(config.Utilities.ContainsKey(enemy.UtilityConfigId), enemy.Id + " utility config is missing.");
            }

            Assert.Greater(humanEnemyCount, 0);
            Assert.Greater(creatureEnemyCount, 0);
        }

        [Test]
        public void GameIdsMatchProductionConfigCatalogs()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);
            Assert.NotNull(database);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            AssertContainsRequiredIds("resources", config.Resources.Keys, GameIds.Resources.All);
            AssertIdsMatch("skills", GameStateFactory.SkillIds, GameIds.Skills.All);
            AssertIdsMatch("buildings", config.Buildings.Keys, GameIds.Buildings.All);
            AssertIdsMatch("zones", config.Zones.Keys, GameIds.Zones.All);
            AssertIdsMatch("policies", config.Policies.Keys, GameIds.Policies.All);
            AssertIdsMatch("backgrounds", config.Backgrounds.Keys, GameIds.Backgrounds.All);
            AssertIdsMatch("traits", config.Traits.Keys, GameIds.Traits.All);
            AssertIdsMatch("recruitable survivors", config.RecruitableSurvivors.Keys, GameIds.RecruitableSurvivors.All);
            AssertIdsMatch("enemies", config.Enemies.Keys, GameIds.Enemies.All);
            AssertIdsMatch("weapons", config.Weapons.Keys, GameIds.Weapons.All);
            AssertIdsMatch("armor", config.Armor.Keys, GameIds.Armor.All);
            AssertIdsMatch("utilities", config.Utilities.Keys, GameIds.Utilities.All);
            AssertIdsMatch("legacy items", config.Items.Keys, GameIds.Items.All);
            AssertIdsMatch("status effects", new[] { config.Balance.HealingDefaultWoundId }, GameIds.StatusEffects.All);
            AssertIdsMatch("sounds", CollectUsedSoundIds(config), GameIds.Sounds.All);
        }

        [Test]
        public void GameConfigLookupExtensionsResolveProductionIds()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);
            Assert.NotNull(database);

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            ResourceDefinition resource;
            Assert.IsTrue(config.TryGetResource(GameIds.Resources.Scrap, out resource));
            Assert.AreSame(resource, config.RequireResource(GameIds.Resources.Scrap));

            BackgroundDefinition background;
            Assert.IsTrue(config.TryGetBackground(GameIds.Backgrounds.Scavenger, out background));
            Assert.AreSame(background, config.RequireBackground(GameIds.Backgrounds.Scavenger));

            TraitDefinition trait;
            Assert.IsTrue(config.TryGetTrait(GameIds.Traits.Careful, out trait));
            Assert.AreSame(trait, config.RequireTrait(GameIds.Traits.Careful));

            ExpeditionPolicyDefinition policy;
            Assert.IsTrue(config.TryGetPolicy(GameIds.Policies.Balanced, out policy));
            Assert.AreSame(policy, config.RequirePolicy(GameIds.Policies.Balanced));

            ZoneDefinition zone;
            Assert.IsTrue(config.TryGetZone(GameIds.Zones.AbandonedStore, out zone));
            Assert.AreSame(zone, config.RequireZone(GameIds.Zones.AbandonedStore));

            EnemyDefinition enemy;
            Assert.IsTrue(config.TryGetEnemy(GameIds.Enemies.CreatureWeakRadiatedHound, out enemy));
            Assert.AreSame(enemy, config.RequireEnemy(GameIds.Enemies.CreatureWeakRadiatedHound));

            ItemDefinition item;
            Assert.IsTrue(config.TryGetItem(GameIds.Items.RustyKnife, out item));
            Assert.AreSame(item, config.RequireItem(GameIds.Items.RustyKnife));

            WeaponDefinition weapon;
            Assert.IsTrue(config.TryGetWeapon(GameIds.Weapons.Melee01SurvivalKnife, out weapon));
            Assert.AreSame(weapon, config.RequireWeapon(GameIds.Weapons.Melee01SurvivalKnife));

            ArmorDefinition armor;
            Assert.IsTrue(config.TryGetArmor(GameIds.Armor.PatchedClothJacket, out armor));
            Assert.AreSame(armor, config.RequireArmor(GameIds.Armor.PatchedClothJacket));

            UtilityDefinition utility;
            Assert.IsTrue(config.TryGetUtility(GameIds.Utilities.MedkitTier01RagBundle, out utility));
            Assert.AreSame(utility, config.RequireUtility(GameIds.Utilities.MedkitTier01RagBundle));

            BuildingDefinition building;
            Assert.IsTrue(config.TryGetBuilding(GameIds.Buildings.Barracks, out building));
            Assert.AreSame(building, config.RequireBuilding(GameIds.Buildings.Barracks));

            RecruitableSurvivorDefinition survivor;
            Assert.IsTrue(config.TryGetRecruitableSurvivor(GameIds.RecruitableSurvivors.Survivor02, out survivor));
            Assert.AreSame(survivor, config.RequireRecruitableSurvivor(GameIds.RecruitableSurvivors.Survivor02));

            Assert.IsFalse(config.TryGetWeapon("missing_weapon", out weapon));
            Assert.IsNull(weapon);
            var ex = Assert.Throws<InvalidOperationException>(() => config.RequireWeapon("missing_weapon"));
            StringAssert.Contains("Weapon config is missing: missing_weapon", ex.Message);
        }

        [Test]
        public void ScriptableObjectConfigAllowsAdditionalRuntimeTestResources()
        {
            var database = CreateValidDatabase();
            database.Resources.Resources.Add(new ResourceConfigData
            {
                Id = "test_runtime_resource",
                Name = "Test Runtime Resource",
                HasCap = true,
                StartAmount = 7,
                StartCap = 12
            });

            var config = new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
            var state = GameStateFactory.CreateNew(config, 0);

            Assert.IsTrue(config.Resources.ContainsKey("test_runtime_resource"));
            Assert.IsFalse(Array.IndexOf(GameIds.Resources.All, "test_runtime_resource") >= 0);
            Assert.AreEqual(7, state.Resources["test_runtime_resource"]);
            Assert.AreEqual(12, state.ResourceCaps["test_runtime_resource"]);
            DestroyDatabase(database);
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

        private static void AssertIdsMatch(string label, IEnumerable<string> configIds, string[] gameIds)
        {
            var configSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in configIds)
            {
                if (!string.IsNullOrWhiteSpace(id)) configSet.Add(id);
            }

            var gameIdSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in gameIds)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(id), label + " contains an empty GameIds entry.");
                Assert.IsTrue(gameIdSet.Add(id), label + " contains duplicate GameIds entry: " + id);
            }

            foreach (var id in configSet)
            {
                Assert.IsTrue(gameIdSet.Contains(id), label + " missing GameIds entry: " + id);
            }

            foreach (var id in gameIdSet)
            {
                Assert.IsTrue(configSet.Contains(id), label + " has stale GameIds entry: " + id);
            }
        }

        private static void AssertContainsRequiredIds(string label, IEnumerable<string> configIds, string[] requiredIds)
        {
            var configSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in configIds)
            {
                if (!string.IsNullOrWhiteSpace(id)) configSet.Add(id);
            }

            var requiredSet = new HashSet<string>(StringComparer.Ordinal);
            foreach (var id in requiredIds)
            {
                Assert.IsFalse(string.IsNullOrWhiteSpace(id), label + " contains an empty required id.");
                Assert.IsTrue(requiredSet.Add(id), label + " contains duplicate required id: " + id);
            }

            foreach (var id in requiredSet)
            {
                Assert.IsTrue(configSet.Contains(id), label + " missing required id: " + id);
            }
        }

        private static IEnumerable<string> CollectUsedSoundIds(GameConfigSnapshot config)
        {
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var weapon in config.Weapons.Values)
            {
                AddId(ids, weapon.AttackSoundId);
            }

            foreach (var armor in config.Armor.Values)
            {
                AddId(ids, armor.EquipSoundId);
            }

            foreach (var utility in config.Utilities.Values)
            {
                AddId(ids, utility.UseSoundId);
            }

            return ids;
        }

        private static void AddId(HashSet<string> ids, string id)
        {
            if (!string.IsNullOrWhiteSpace(id)) ids.Add(id);
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
            database.Resources.Resources.Add(new ResourceConfigData { Id = "radio_intel", Name = "Radio Intel", HasCap = false, StartAmount = 3 });

            database.Survivors = ScriptableObject.CreateInstance<SurvivorCatalogSO>();
            database.Survivors.StartingSurvivor = new StartingSurvivorConfigData
            {
                Name = "Mara",
                PortraitId = "ui_character_battle_survivor_01",
                BackgroundId = "scavenger",
                TraitIds = new List<string> { "careful" },
                WeaponItemId = "rusty_knife",
                WeaponConfigId = "rusty_knife",
                ArmorConfigId = "leather_jacket",
                UtilityConfigId = "field_medkit",
                Skills = new List<IntPairData> { new IntPairData("scavenging", 4), new IntPairData("melee", 1), new IntPairData("survival", 2) }
            };
            database.Survivors.RecruitableSurvivors.Add(new RecruitableSurvivorConfigData
            {
                Id = "survivor_02",
                Name = "Bram",
                PortraitId = "ui_character_battle_survivor_02",
                BackgroundId = "scavenger",
                TraitIds = new List<string> { "careful" },
                WeaponItemId = "rusty_knife",
                WeaponConfigId = "rusty_knife",
                ArmorConfigId = "leather_jacket",
                UtilityConfigId = "field_medkit",
                Skills = new List<IntPairData> { new IntPairData("scavenging", 1), new IntPairData("melee", 1) }
            });

            database.Backgrounds = ScriptableObject.CreateInstance<BackgroundCatalogSO>();
            database.Backgrounds.Backgrounds.Add(new BackgroundConfigData { Id = "scavenger", Name = "Scavenger" });

            database.Traits = ScriptableObject.CreateInstance<TraitCatalogSO>();
            database.Traits.Traits.Add(new TraitConfigData { Id = "careful", Name = "Careful" });

            database.Policies = ScriptableObject.CreateInstance<ExpeditionPolicyCatalogSO>();
            database.Policies.Policies.Add(new ExpeditionPolicyConfigData { Id = "balanced", Name = "Balanced" });

            database.Enemies = ScriptableObject.CreateInstance<EnemyCatalogSO>();
            database.Enemies.Enemies.Add(new EnemyConfigData { Id = "creature_weak_radiated_hound", Name = "Radiated Hound", Kind = EnemyKind.Creature, PortraitId = "ui_character_creature_weak_radiated_hound_01", MaxHealth = 14, BaseDamage = 3, Accuracy = 0.75 });

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
                Durability = 1f,
                MaxDurability = 80,
                RepairCostMultiplier = 1.0,
                AttackSoundId = "melee_blade"
            });

            database.Armor = ScriptableObject.CreateInstance<ArmorCatalogSO>();
            database.Armor.Armor.Add(new ArmorConfigData
            {
                Id = "leather_jacket",
                Name = "Leather Jacket",
                Description = "A worn jacket with stitched padding.",
                Type = ArmorType.Light,
                Rarity = WeaponRarity.Common,
                Defense = 1,
                EvasionChance = 0.02,
                BonusHealth = 2,
                BonusStamina = 1,
                SpeedModifier = 0,
                Durability = 1f,
                MaxDurability = 70,
                RepairCostMultiplier = 1.0,
                EquipSoundId = "armor_light"
            });

            database.Utilities = ScriptableObject.CreateInstance<UtilityCatalogSO>();
            database.Utilities.Utilities.Add(new UtilityConfigData
            {
                Id = "field_medkit",
                Name = "Field Medkit",
                Description = "Basic bandages and disinfectant.",
                Type = UtilityEquipmentType.Medkit,
                Rarity = WeaponRarity.Common,
                Tier = 1,
                HealAmount = 12,
                MaxDurability = 1,
                RepairCostMultiplier = 0,
                UseSoundId = "utility_medkit"
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
                    new BuildingLevelConfigData { Level = 1, Cost = new List<IntPairData> { new IntPairData("scrap", 25) }, UpgradeDurationSeconds = 30, SurvivorCap = 2, SquadSize = 1 }
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
                    new BuildingLevelConfigData { Level = 1, Cost = new List<IntPairData> { new IntPairData("scrap", 35) }, UpgradeDurationSeconds = 30 }
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
                    new BuildingLevelConfigData { Level = 1, Cost = new List<IntPairData> { new IntPairData("scrap", 50), new IntPairData("medicine", 2) }, UpgradeDurationSeconds = 30, ResourceCap = 30 }
                }
            });
            EnsureBuildingLevelsZeroThroughTen(database.Buildings);

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
                EnemyTable = new List<WeightedEntryData> { new WeightedEntryData("creature_weak_radiated_hound", 100) },
                LootTable = new List<LootTableEntryData> { new LootTableEntryData("scrap", 4, 10, 100) }
            });

            database.Balance = ScriptableObject.CreateInstance<BalanceConfigSO>();
            database.Balance.Balance = new BalanceConfigData();
            database.Balance.Balance.EmergencyScavengeRewards.Add(new IntPairData("scrap", 3));
            database.Balance.Balance.EmergencyScavengeRewards.Add(new IntPairData("food", 2));
            database.Balance.Balance.EmergencyScavengeRewards.Add(new IntPairData("water", 2));
            return database;
        }

        private static void EnsureBuildingLevelsZeroThroughTen(BuildingCatalogSO catalog)
        {
            foreach (var building in catalog.Buildings)
            {
                for (var level = 0; level <= 10; level++)
                {
                    if (HasBuildingLevel(building, level)) continue;

                    var previous = building.Levels.Count > 0 ? building.Levels[building.Levels.Count - 1] : new BuildingLevelConfigData();
                    building.Levels.Add(new BuildingLevelConfigData
                    {
                        Level = level,
                        UpgradeDurationSeconds = level > 0 ? 30 : 0,
                        SurvivorCap = previous.SurvivorCap,
                        SquadSize = previous.SquadSize,
                        ResourceCap = previous.ResourceCap,
                        ResourcePerMinute = previous.ResourcePerMinute
                    });
                }
            }
        }

        private static bool HasBuildingLevel(BuildingConfigData building, int level)
        {
            foreach (var candidate in building.Levels)
            {
                if (candidate.Level == level) return true;
            }

            return false;
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
            if (balance.MapRevealResourceId == oldId) balance.MapRevealResourceId = newId;
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
            UnityEngine.Object.DestroyImmediate(database.Armor);
            UnityEngine.Object.DestroyImmediate(database.Utilities);
            UnityEngine.Object.DestroyImmediate(database.Buildings);
            UnityEngine.Object.DestroyImmediate(database.Zones);
            UnityEngine.Object.DestroyImmediate(database.Balance);
            UnityEngine.Object.DestroyImmediate(database);
        }
    }
}
