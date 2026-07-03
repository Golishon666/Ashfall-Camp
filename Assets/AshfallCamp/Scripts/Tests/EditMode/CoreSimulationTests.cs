using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class CoreSimulationTests
    {
        [Test]
        public void ResourcesRespectSpendAndCaps()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);

            Assert.IsFalse(ResourceSystem.TrySpend(state, new Dictionary<string, int> { { "scrap", 999 } }));
            Assert.AreEqual(15, state.Resources["scrap"]);

            ResourceSystem.Add(state, "food", 999);
            Assert.AreEqual(50, state.Resources["food"]);

            ResourceSystem.Add(state, "scrap", 999);
            Assert.AreEqual(1014, state.Resources["scrap"]);
        }

        [Test]
        public void LaunchValidationBlocksLockedWoundedAndUnaffordableRuns()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);

            var locked = ExpeditionValidator.Validate(state, config, Request("dry_suburb", 11));
            Assert.IsFalse(locked.IsValid);
            Assert.Contains("Zone is locked.", locked.Errors);

            state.Survivors[0].State = SurvivorActivityState.Wounded;
            var wounded = ExpeditionValidator.Validate(state, config, Request("abandoned_store", 11));
            Assert.IsFalse(wounded.IsValid);
            Assert.Contains("All survivors must be idle.", wounded.Errors);

            state.Survivors[0].State = SurvivorActivityState.Idle;
            state.Resources["food"] = 0;
            var noResources = ExpeditionValidator.Validate(state, config, Request("abandoned_store", 11));
            Assert.IsFalse(noResources.IsValid);
            Assert.Contains("Not enough food or water.", noResources.Errors);
        }

        [Test]
        public void LaunchSpendsSuppliesAndMarksSurvivorBusy()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);

            var result = ExpeditionLauncher.Launch(state, config, Request("abandoned_store", 123));

            Assert.IsTrue(result.Validation.IsValid);
            Assert.NotNull(result.Expedition);
            Assert.AreEqual(7, state.Resources["food"]);
            Assert.AreEqual(SurvivorActivityState.OnExpedition, state.Survivors[0].State);
            Assert.AreEqual(result.Expedition.Id, state.Survivors[0].CurrentExpeditionId);
        }

        [Test]
        public void ExpeditionSimulationIsDeterministicForSameSeed()
        {
            var config = TestConfigFactory.Create();
            var first = GameStateFactory.CreateNew(config, 0);
            var second = GameStateFactory.CreateNew(config, 0);
            ExpeditionLauncher.Launch(first, config, Request("abandoned_store", 999));
            ExpeditionLauncher.Launch(second, config, Request("abandoned_store", 999));

            ExpeditionSimulator.TickAll(first, config, 60);
            ExpeditionSimulator.TickAll(second, config, 60);

            Assert.AreEqual(first.Resources["scrap"], second.Resources["scrap"]);
            Assert.AreEqual(first.Resources["food"], second.Resources["food"]);
            Assert.AreEqual(first.Expeditions[0].Log.Count, second.Expeditions[0].Log.Count);
            Assert.AreEqual(first.Expeditions[0].Status, second.Expeditions[0].Status);
        }

        [Test]
        public void CombatMathClampsDamageAndFirearmsAddNoise()
        {
            var rng = new SeededRandom(5);
            Assert.AreEqual(0.15, CombatResolver.CalculateHitChance(-1, 0, 0, 0, 0.15, 0.95));
            Assert.AreEqual(0.95, CombatResolver.CalculateHitChance(5, 0, 0, 0, 0.15, 0.95));
            Assert.GreaterOrEqual(CombatResolver.CalculateDamage(1, 0, 1, 1.75, false, 999, ref rng), 1);

            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var revolver = new InventoryItemState { Uid = "item_revolver", ItemId = "rusty_revolver", Durability = 60, MaxDurability = 60, EquippedBySurvivorId = "survivor_1" };
            state.Inventory.Add(revolver);
            state.Survivors[0].Equipment.WeaponItemUid = revolver.Uid;
            var expedition = new ExpeditionState { Id = "expedition_test", ZoneId = "abandoned_store", RandomState = 77 };
            expedition.SurvivorIds.Add("survivor_1");

            CombatResolver.ResolveCombat(state, config, expedition, "feral_dog");

            Assert.Greater(expedition.Noise, 0);
        }

        [Test]
        public void ExpeditionCompletionGrantsLootXpAndFamiliarity()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var result = ExpeditionLauncher.Launch(state, config, Request("abandoned_store", 123));
            result.Expedition.AccumulatedLoot["scrap"] = 8;

            ExpeditionSimulator.Complete(state, config, result.Expedition);

            Assert.AreEqual(23, state.Resources["scrap"]);
            Assert.AreEqual(SurvivorActivityState.Idle, state.Survivors[0].State);
            Assert.Greater(state.Survivors[0].Xp, 0);
            Assert.AreEqual(1, state.Zones["abandoned_store"].Completions);
            Assert.Greater(state.Zones["abandoned_store"].Familiarity, 0);
        }

        [Test]
        public void OfflineProgressCapsAndCompletesActiveExpeditions()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var result = ExpeditionLauncher.Launch(state, config, Request("abandoned_store", 123));
            result.Expedition.ExpectedDurationSeconds = 10;
            result.Expedition.AccumulatedLoot["scrap"] = 5;
            state.LastSaveAtUnixMs = 0;

            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new OfflineProgressUseCase(store, new StaticConfigProvider(config));

            var report = useCase.ExecuteAsync((long)(config.Balance.MaxOfflineSeconds + 1000) * 1000, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(config.Balance.MaxOfflineSeconds, report.AppliedSeconds);
            Assert.Contains(result.Expedition.Id, report.CompletedExpeditionIds);
            Assert.GreaterOrEqual(report.ResourcesGained["scrap"], 5);
            Assert.GreaterOrEqual(store.State.CurrentValue.Resources["scrap"], 20);
        }

        [Test]
        public void SaveLoadPreservesStateAndFallsBackToBackup()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var result = ExpeditionLauncher.Launch(state, config, Request("abandoned_store", 321));
            var folder = Path.Combine(Path.GetTempPath(), "AshfallCampTests_" + Guid.NewGuid().ToString("N"));
            var repository = new JsonSaveRepository(folder);

            repository.SaveAsync(state, CancellationToken.None).GetAwaiter().GetResult();
            var loaded = repository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.NotNull(loaded);
            Assert.AreEqual(7, loaded.Resources["food"]);
            Assert.AreEqual(result.Expedition.Id, loaded.Expeditions[0].Id);
            Assert.AreEqual(ExpeditionStatus.Active, loaded.Expeditions[0].Status);

            File.WriteAllText(Path.Combine(folder, "save.json"), "{corrupt");
            var fromBackup = repository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.NotNull(fromBackup);
            Assert.AreEqual(result.Expedition.Id, fromBackup.Expeditions[0].Id);
            Directory.Delete(folder, true);
        }

        [Test]
        public void SaveLoadUseCaseMigratesOldVersion()
        {
            var oldState = new GameState { Version = string.Empty };
            var store = new GameStateStore();
            var useCase = new SaveLoadUseCase(new FakeSaveRepository(oldState), store, new StaticConfigProvider(TestConfigFactory.Create()));

            var result = useCase.LoadOrCreateAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(GameConstants.CurrentSaveVersion, result.State.Version);
            Assert.NotNull(result.State.Resources);
            Assert.NotNull(result.State.Expeditions);
        }

        private static LaunchExpeditionRequest Request(string zoneId, uint seed)
        {
            return new LaunchExpeditionRequest
            {
                ZoneId = zoneId,
                PolicyId = "balanced",
                Seed = seed,
                NowUnixMs = 0,
                ConfirmWarnings = true,
                SurvivorIds = new List<string> { "survivor_1" }
            };
        }
    }

    internal sealed class StaticConfigProvider : IGameConfigProvider
    {
        public StaticConfigProvider(GameConfigSnapshot current)
        {
            Current = current;
        }

        public GameConfigSnapshot Current { get; private set; }

        public UniTask<GameConfigSnapshot> LoadAsync(CancellationToken ct)
        {
            return UniTask.FromResult(Current);
        }
    }

    internal sealed class FakeSaveRepository : ISaveRepository
    {
        private readonly GameState _state;

        public FakeSaveRepository(GameState state)
        {
            _state = state;
        }

        public UniTask<GameState> LoadAsync(CancellationToken ct)
        {
            return UniTask.FromResult(_state);
        }

        public UniTask SaveAsync(GameState state, CancellationToken ct)
        {
            return UniTask.CompletedTask;
        }
    }

    internal static class TestConfigFactory
    {
        public static GameConfigSnapshot Create()
        {
            var config = new GameConfigSnapshot();
            config.Resources["scrap"] = new ResourceDefinition { Id = "scrap", Name = "Scrap", StartAmount = 15, HasCap = false };
            config.Resources["food"] = new ResourceDefinition { Id = "food", Name = "Food", StartAmount = 8, HasCap = true, StartCap = 50 };
            config.Resources["water"] = new ResourceDefinition { Id = "water", Name = "Water", StartAmount = 6, HasCap = true, StartCap = 40 };
            config.Resources["weapon_parts"] = new ResourceDefinition { Id = "weapon_parts", Name = "Weapon Parts", StartAmount = 0, HasCap = false };
            config.Resources["medicine"] = new ResourceDefinition { Id = "medicine", Name = "Medicine", StartAmount = 1, HasCap = true, StartCap = 20 };

            config.Backgrounds["scavenger"] = new BackgroundDefinition
            {
                Id = "scavenger",
                Name = "Scavenger",
                SkillBonuses = new Dictionary<string, int> { { "scavenging", 3 }, { "survival", 1 } },
                StatBonuses = new Dictionary<string, int> { { "carry_capacity", 5 } }
            };
            config.Traits["careful"] = new TraitDefinition { Id = "careful", Name = "Careful" };
            config.StartingSurvivor.Name = "Mara";
            config.StartingSurvivor.BackgroundId = "scavenger";
            config.StartingSurvivor.TraitIds.Add("careful");
            config.StartingSurvivor.WeaponItemId = "rusty_knife";
            config.StartingSurvivor.Skills["scavenging"] = 4;
            config.StartingSurvivor.Skills["melee"] = 1;
            config.StartingSurvivor.Skills["firearms"] = 0;
            config.StartingSurvivor.Skills["survival"] = 2;
            config.StartingSurvivor.Skills["mechanics"] = 0;
            config.StartingSurvivor.Skills["medicine"] = 0;

            config.Policies["balanced"] = new ExpeditionPolicyDefinition { Id = "balanced", Name = "Balanced" };
            config.Items["rusty_knife"] = new ItemDefinition { Id = "rusty_knife", Name = "Rusty Knife", Slot = ItemSlot.Weapon, WeaponType = WeaponType.Melee, BaseDamage = 4, AccuracyBonus = 0.02, CritBonus = 0.01, MaxDurability = 80 };
            config.Items["rusty_revolver"] = new ItemDefinition { Id = "rusty_revolver", Name = "Rusty Revolver", Slot = ItemSlot.Weapon, WeaponType = WeaponType.Firearm, BaseDamage = 10, AccuracyBonus = -0.03, CritBonus = 0.04, NoisePerAttack = 3, MaxDurability = 60 };
            config.Enemies["feral_dog"] = new EnemyDefinition { Id = "feral_dog", Name = "Feral Dog", MaxHealth = 14, Armor = 0, Evasion = 0.08, BaseDamage = 3, AttackType = WeaponType.Melee, Accuracy = 0.75, XpReward = 4 };
            config.Zones["abandoned_store"] = Zone("abandoned_store", 45, 1, 0, 5, Array.Empty<UnlockCondition>());
            config.Zones["dry_suburb"] = Zone("dry_suburb", 90, 1, 1, 8, new[] { new UnlockCondition { Type = "zone_completions", Id = "abandoned_store", Value = 2 } });
            config.Zones["police_outpost"] = Zone("police_outpost", 120, 1, 1, 8, new[] { new UnlockCondition { Type = "building_level", Id = "workshop", Value = 1 } });
            config.Buildings["barracks"] = Building("barracks", true, string.Empty, string.Empty,
                Level(0, Array.Empty<IntPairData>(), 1, 1, 0, 0),
                Level(1, new[] { new IntPairData("scrap", 25), new IntPairData("food", 4) }, 2, 1, 0, 0),
                Level(2, new[] { new IntPairData("scrap", 60), new IntPairData("food", 8) }, 3, 2, 0, 0));
            config.Buildings["workshop"] = Building("workshop", true, string.Empty, string.Empty,
                Level(0, Array.Empty<IntPairData>(), 0, 0, 0, 0),
                Level(1, new[] { new IntPairData("scrap", 35) }, 0, 0, 0, 0));
            config.Buildings["water_collector"] = Building("water_collector", true, "water", "water",
                Level(0, Array.Empty<IntPairData>(), 0, 0, 40, 0),
                Level(1, new[] { new IntPairData("scrap", 30) }, 0, 0, 60, 1));
            config.Buildings["infirmary"] = Building("infirmary", true, "medicine", string.Empty,
                Level(0, Array.Empty<IntPairData>(), 0, 0, 20, 0),
                Level(1, new[] { new IntPairData("scrap", 50), new IntPairData("medicine", 2) }, 0, 0, 30, 0));
            return config;
        }

        private static ZoneDefinition Zone(string id, int duration, int food, int water, int power, IEnumerable<UnlockCondition> unlocks)
        {
            var zone = new ZoneDefinition
            {
                Id = id,
                Name = id,
                RiskTier = RiskTier.Safe,
                BaseDurationSeconds = duration,
                MinDurationSeconds = Math.Max(1, duration / 2),
                MaxDurationSeconds = duration * 2,
                FoodCostPerSurvivor = food,
                WaterCostPerSurvivor = water,
                RecommendedPower = power
            };
            zone.EnemyTable.Add(new WeightedEntry { Id = "feral_dog", Weight = 100 });
            zone.LootTable.Add(new LootTableEntry { ResourceId = "scrap", Min = 4, Max = 10, Weight = 100 });
            zone.LootTable.Add(new LootTableEntry { ResourceId = "food", Min = 1, Max = 4, Weight = 70 });
            foreach (var unlock in unlocks) zone.UnlockConditions.Add(unlock);
            return zone;
        }

        private static BuildingDefinition Building(string id, bool unlocked, string affectedResourceId, string producedResourceId, params BuildingLevelDefinition[] levels)
        {
            return new BuildingDefinition
            {
                Id = id,
                Name = id,
                StartsUnlocked = unlocked,
                AffectedResourceId = affectedResourceId,
                ProducedResourceId = producedResourceId,
                Levels = new List<BuildingLevelDefinition>(levels)
            };
        }

        private static BuildingLevelDefinition Level(int level, IEnumerable<IntPairData> cost, int survivorCap, int squadSize, int resourceCap, int resourcePerMinute)
        {
            var result = new BuildingLevelDefinition
            {
                Level = level,
                SurvivorCap = survivorCap,
                SquadSize = squadSize,
                ResourceCap = resourceCap,
                ResourcePerMinute = resourcePerMinute
            };

            foreach (var pair in cost)
            {
                result.Cost[pair.Id] = pair.Value;
            }

            return result;
        }
    }
}
