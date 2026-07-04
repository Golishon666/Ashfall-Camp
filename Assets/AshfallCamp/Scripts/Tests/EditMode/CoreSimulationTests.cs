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
            Assert.Contains("Not enough expedition resources.", noResources.Errors);
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
        public void LaunchExpeditionUseCaseMutatesStore()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new LaunchExpeditionUseCase(store, new StaticConfigProvider(config));

            var result = useCase.ExecuteAsync(Request("abandoned_store", 987), CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(1, store.State.CurrentValue.Expeditions.Count);
            Assert.AreEqual(SurvivorActivityState.OnExpedition, store.State.CurrentValue.Survivors[0].State);
        }

        [Test]
        public void RecruitmentValidationBlocksCapAndUnaffordableBroadcast()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;

            var capped = RecruitmentSystem.ValidateBroadcast(state, config);
            Assert.IsFalse(capped.IsValid);
            Assert.Contains("Survivor cap reached.", capped.Errors);

            BuildingSystem.Upgrade(state, config, "barracks");
            state.Resources["scrap"] = 0;
            var unaffordable = RecruitmentSystem.ValidateBroadcast(state, config);

            Assert.IsFalse(unaffordable.IsValid);
            Assert.Contains("Not enough resources.", unaffordable.Errors);
        }

        [Test]
        public void RecruitmentSpendsResourcesAddsSurvivorAndEquipsWeapon()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            BuildingSystem.Upgrade(state, config, "barracks");

            var result = RecruitmentSystem.Recruit(state, config, new RecruitSurvivorRequest { Seed = 1 });

            Assert.IsTrue(result.Validation.IsValid);
            Assert.NotNull(result.Survivor);
            Assert.AreEqual(2, state.Survivors.Count);
            Assert.AreEqual("Elias", state.Survivors[1].Name);
            Assert.AreEqual("rusty_knife", result.Weapon.ItemId);
            Assert.AreEqual(result.Weapon.Uid, result.Survivor.Equipment.WeaponItemUid);
            Assert.AreEqual(55, state.Resources["scrap"]);
            Assert.AreEqual(14, state.Resources["food"]);
            Assert.AreEqual(18, state.Resources["water"]);
            Assert.AreEqual(1, state.Statistics.SurvivorsRecruited);
        }

        [Test]
        public void RecruitSurvivorUseCaseMutatesStore()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            BuildingSystem.Upgrade(state, config, "barracks");
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new RecruitSurvivorUseCase(store, new StaticConfigProvider(config));

            var result = useCase.ExecuteAsync(new RecruitSurvivorRequest { Seed = 1 }, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(2, store.State.CurrentValue.Survivors.Count);
            Assert.AreEqual("Elias", store.State.CurrentValue.Survivors[1].Name);
        }

        [Test]
        public void WorkshopRepairRequiresUpgradeResourcesAndDamagedItem()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Inventory[0].Durability = 50;
            state.Resources["weapon_parts"] = 5;

            var locked = WorkshopSystem.ValidateRepair(state, config, new RepairItemRequest { ItemUid = state.Inventory[0].Uid });
            Assert.IsFalse(locked.IsValid);
            Assert.Contains("Workshop repair is locked.", locked.Errors);

            state.Resources["scrap"] = 100;
            BuildingSystem.Upgrade(state, config, "workshop");
            state.Resources["weapon_parts"] = 0;
            var unaffordable = WorkshopSystem.ValidateRepair(state, config, new RepairItemRequest { ItemUid = state.Inventory[0].Uid });
            Assert.IsFalse(unaffordable.IsValid);
            Assert.Contains("Not enough repair resources.", unaffordable.Errors);

            state.Resources["weapon_parts"] = 5;
            state.Inventory[0].Durability = state.Inventory[0].MaxDurability;
            var full = WorkshopSystem.ValidateRepair(state, config, new RepairItemRequest { ItemUid = state.Inventory[0].Uid });
            Assert.IsFalse(full.IsValid);
            Assert.Contains("Item is already fully repaired.", full.Errors);
        }

        [Test]
        public void WorkshopRepairSpendsPartsAndRestoresDurability()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["weapon_parts"] = 5;
            BuildingSystem.Upgrade(state, config, "workshop");
            state.Inventory[0].Durability = 50;

            var result = WorkshopSystem.Repair(state, config, new RepairItemRequest { ItemUid = state.Inventory[0].Uid });

            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(80, state.Inventory[0].Durability);
            Assert.AreEqual(2, state.Resources["weapon_parts"]);
            Assert.AreEqual(3, result.Cost["weapon_parts"]);
        }

        [Test]
        public void WorkshopEquipSwapsWeaponOwnership()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var knife = state.Inventory[0];
            var revolver = new InventoryItemState { Uid = "item_revolver", ItemId = "rusty_revolver", Durability = 60, MaxDurability = 60 };
            state.Inventory.Add(revolver);

            var result = WorkshopSystem.Equip(state, config, new EquipItemRequest { SurvivorId = "survivor_1", ItemUid = revolver.Uid });

            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(revolver.Uid, state.Survivors[0].Equipment.WeaponItemUid);
            Assert.AreEqual("survivor_1", revolver.EquippedBySurvivorId);
            Assert.AreEqual(string.Empty, knife.EquippedBySurvivorId);
            Assert.AreEqual(knife, result.PreviouslyEquippedItem);
        }

        [Test]
        public void RepairItemUseCaseMutatesStore()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["weapon_parts"] = 5;
            BuildingSystem.Upgrade(state, config, "workshop");
            state.Inventory[0].Durability = 50;
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new RepairItemUseCase(store, new StaticConfigProvider(config));

            var result = useCase.ExecuteAsync(new RepairItemRequest { ItemUid = state.Inventory[0].Uid }, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(80, store.State.CurrentValue.Inventory[0].Durability);
            Assert.AreEqual(2, store.State.CurrentValue.Resources["weapon_parts"]);
        }

        [Test]
        public void EquipItemUseCaseMutatesStore()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var revolver = new InventoryItemState { Uid = "item_revolver", ItemId = "rusty_revolver", Durability = 60, MaxDurability = 60 };
            state.Inventory.Add(revolver);
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new EquipItemUseCase(store, new StaticConfigProvider(config));

            var result = useCase.ExecuteAsync(new EquipItemRequest { SurvivorId = "survivor_1", ItemUid = revolver.Uid }, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(revolver.Uid, store.State.CurrentValue.Survivors[0].Equipment.WeaponItemUid);
            Assert.AreEqual("survivor_1", store.State.CurrentValue.Inventory[1].EquippedBySurvivorId);
        }

        [Test]
        public void HealingRequiresInfirmaryAndCompletesWound()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);

            HealingSystem.ApplyWound(state, config, "survivor_1");

            Assert.AreEqual(SurvivorActivityState.Wounded, state.Survivors[0].State);
            Assert.AreEqual(1, state.Survivors[0].Health);
            Assert.AreEqual("cuts", state.Survivors[0].StatusEffects[0].Id);

            HealingSystem.Tick(state, config, config.Balance.HealingDefaultWoundDurationSeconds);
            Assert.AreEqual(SurvivorActivityState.Wounded, state.Survivors[0].State);

            state.Buildings["infirmary"].Level = 1;
            HealingSystem.Tick(state, config, config.Balance.HealingDefaultWoundDurationSeconds - 1);
            Assert.AreEqual(SurvivorActivityState.Wounded, state.Survivors[0].State);

            HealingSystem.Tick(state, config, 1);
            Assert.AreEqual(SurvivorActivityState.Idle, state.Survivors[0].State);
            Assert.AreEqual(state.Survivors[0].MaxHealth, state.Survivors[0].Health);
            Assert.AreEqual(0, state.Survivors[0].StatusEffects.Count);
        }

        [Test]
        public void TickGameUseCaseHealsWoundedSurvivor()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Buildings["infirmary"].Level = 1;
            HealingSystem.ApplyWound(state, config, "survivor_1");
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new TickGameUseCase(store, new StaticConfigProvider(config));

            useCase.ExecuteAsync(config.Balance.HealingDefaultWoundDurationSeconds, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(SurvivorActivityState.Idle, store.State.CurrentValue.Survivors[0].State);
            Assert.AreEqual(store.State.CurrentValue.Survivors[0].MaxHealth, store.State.CurrentValue.Survivors[0].Health);
        }

        [Test]
        public void OfflineProgressReportsHealedSurvivors()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Buildings["infirmary"].Level = 1;
            HealingSystem.ApplyWound(state, config, "survivor_1");
            state.LastSaveAtUnixMs = 0;
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new OfflineProgressUseCase(store, new StaticConfigProvider(config));

            var report = useCase.ExecuteAsync((long)config.Balance.HealingDefaultWoundDurationSeconds * 1000, CancellationToken.None).GetAwaiter().GetResult();

            Assert.Contains("survivor_1", report.HealedSurvivorIds);
            Assert.AreEqual(SurvivorActivityState.Idle, store.State.CurrentValue.Survivors[0].State);
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
            Assert.AreSame(report, store.State.CurrentValue.LastOfflineReport);
        }

        [Test]
        public void SaveLoadPreservesStateAndFallsBackToBackup()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var result = ExpeditionLauncher.Launch(state, config, Request("abandoned_store", 321));
            state.Resources["scrap"] = 37;
            state.Inventory[0].Durability = 44;
            state.Buildings["workshop"].Level = 1;
            state.Zones["abandoned_store"].Completions = 2;
            state.Zones["abandoned_store"].Familiarity = 42.5;
            state.Survivors[0].StatusEffects.Add(new StatusEffectState { Id = "sprained_ankle", RemainingSeconds = 120 });
            result.Expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = 12.5, Message = "Mara found a sealed crate." });
            result.Expedition.FoundItems.Add(new InventoryItemState
            {
                Uid = "found_item_1",
                ItemId = "rusty_knife",
                Level = 2,
                Durability = 30,
                MaxDurability = 80
            });
            var folder = Path.Combine(Path.GetTempPath(), "AshfallCampTests_" + Guid.NewGuid().ToString("N"));
            var repository = new JsonSaveRepository(folder);

            repository.SaveAsync(state, CancellationToken.None).GetAwaiter().GetResult();
            var loaded = repository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.NotNull(loaded);
            Assert.IsFalse(loaded.UsedBackup);
            Assert.AreEqual(37, loaded.State.Resources["scrap"]);
            Assert.AreEqual(7, loaded.State.Resources["food"]);
            Assert.AreEqual("rusty_knife", loaded.State.Inventory[0].ItemId);
            Assert.AreEqual(44, loaded.State.Inventory[0].Durability);
            Assert.AreEqual(1, loaded.State.Buildings["workshop"].Level);
            Assert.AreEqual(2, loaded.State.Zones["abandoned_store"].Completions);
            Assert.AreEqual(42.5, loaded.State.Zones["abandoned_store"].Familiarity);
            Assert.AreEqual(result.Expedition.Id, loaded.State.Expeditions[0].Id);
            Assert.AreEqual(ExpeditionStatus.Active, loaded.State.Expeditions[0].Status);
            Assert.AreEqual("sprained_ankle", loaded.State.Survivors[0].StatusEffects[0].Id);
            Assert.AreEqual(120, loaded.State.Survivors[0].StatusEffects[0].RemainingSeconds);
            Assert.AreEqual("Mara found a sealed crate.", loaded.State.Expeditions[0].Log[0].Message);
            Assert.AreEqual("found_item_1", loaded.State.Expeditions[0].FoundItems[0].Uid);
            Assert.AreEqual(2, loaded.State.Expeditions[0].FoundItems[0].Level);

            File.WriteAllText(Path.Combine(folder, "save.json"), "{corrupt");
            var fromBackup = repository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.NotNull(fromBackup);
            Assert.IsTrue(fromBackup.UsedBackup);
            Assert.AreEqual(result.Expedition.Id, fromBackup.State.Expeditions[0].Id);
            Directory.Delete(folder, true);
        }

        [Test]
        public void CorruptSaveWithoutBackupCreatesNewGame()
        {
            var config = TestConfigFactory.Create();
            var folder = Path.Combine(Path.GetTempPath(), "AshfallCampTests_" + Guid.NewGuid().ToString("N"));
            Directory.CreateDirectory(folder);
            File.WriteAllText(Path.Combine(folder, "save.json"), "{corrupt");

            try
            {
                var store = new GameStateStore();
                var useCase = new SaveLoadUseCase(new JsonSaveRepository(folder), store, new StaticConfigProvider(config));

                var result = useCase.LoadOrCreateAsync(CancellationToken.None).GetAwaiter().GetResult();

                Assert.IsTrue(result.CreatedNew);
                Assert.IsFalse(result.UsedBackup);
                Assert.AreEqual(15, result.State.Resources["scrap"]);
                Assert.AreEqual("rusty_knife", result.State.Inventory[0].ItemId);
                Assert.AreEqual(GameConstants.CurrentSaveVersion, result.State.Version);
            }
            finally
            {
                Directory.Delete(folder, true);
            }
        }

        [Test]
        public void SaveLoadUseCaseReportsBackupFallback()
        {
            var state = GameStateFactory.CreateNew(TestConfigFactory.Create(), 0);
            var store = new GameStateStore();
            var useCase = new SaveLoadUseCase(new FakeSaveRepository(state, true), store, new StaticConfigProvider(TestConfigFactory.Create()));

            var result = useCase.LoadOrCreateAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(result.CreatedNew);
            Assert.IsTrue(result.UsedBackup);
            Assert.AreEqual("Loaded Ashfall Camp backup save.", result.Message);
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
        private readonly bool _usedBackup;

        public FakeSaveRepository(GameState state, bool usedBackup = false)
        {
            _state = state;
            _usedBackup = usedBackup;
        }

        public UniTask<SaveRepositoryLoadResult> LoadAsync(CancellationToken ct)
        {
            return UniTask.FromResult(_state == null
                ? null
                : new SaveRepositoryLoadResult { State = _state, UsedBackup = _usedBackup });
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
            config.RecruitableSurvivors["elias"] = new RecruitableSurvivorDefinition
            {
                Id = "elias",
                Name = "Elias",
                BackgroundId = "scavenger",
                TraitIds = new List<string> { "careful" },
                WeaponItemId = "rusty_knife",
                Skills = new Dictionary<string, int>
                {
                    { "scavenging", 1 },
                    { "melee", 1 },
                    { "firearms", 0 },
                    { "survival", 1 },
                    { "mechanics", 0 },
                    { "medicine", 0 }
                }
            };

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
            config.Buildings["radio_tower"] = Building("radio_tower", true, string.Empty, string.Empty,
                Level(0, Array.Empty<IntPairData>(), 0, 0, 0, 0),
                Level(1, new[] { new IntPairData("scrap", 45), new IntPairData("weapon_parts", 2) }, 0, 0, 0, 0));
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
