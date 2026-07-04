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

            Assert.IsTrue(ResourceSystem.TrySpend(state, new Dictionary<string, int> { { "scrap", 14 } }));
            Assert.AreEqual(1000, state.Resources["scrap"]);
            Assert.AreEqual(14, state.Statistics.TotalResourcesSpent["scrap"]);
        }

        [Test]
        public void CampUpkeepSpendsSuppliesAndPenalizesShortage()
        {
            var config = TestConfigFactory.Create();
            config.Balance.CampUpkeepIntervalSeconds = 60;
            config.Balance.CampUpkeepFoodPerSurvivor = 1;
            config.Balance.CampUpkeepWaterPerSurvivor = 1;
            config.Balance.CampUpkeepShortageMoralePenalty = 4;
            config.Balance.CampUpkeepShortageFatigue = 2;
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["food"] = 1;
            state.Resources["water"] = 0;
            state.Survivors[0].Morale = 50;
            state.Survivors[0].Fatigue = 0;

            CampUpkeepSystem.Tick(state, config, 60);

            Assert.AreEqual(0, state.Resources["food"]);
            Assert.AreEqual(0, state.Resources["water"]);
            Assert.AreEqual(1, state.Statistics.TotalResourcesSpent["food"]);
            Assert.IsFalse(state.Statistics.TotalResourcesSpent.ContainsKey("water"));
            Assert.AreEqual(46, state.Survivors[0].Morale);
            Assert.AreEqual(2, state.Survivors[0].Fatigue);
            Assert.AreEqual(0, state.CampUpkeepAccumulatorSeconds);
        }

        [Test]
        public void CampUpkeepIgnoresSurvivorsAwayFromCamp()
        {
            var config = TestConfigFactory.Create();
            config.Balance.CampUpkeepIntervalSeconds = 60;
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["food"] = 1;
            state.Resources["water"] = 1;
            state.Survivors[0].State = SurvivorActivityState.OnExpedition;

            CampUpkeepSystem.Tick(state, config, 60);

            Assert.AreEqual(1, state.Resources["food"]);
            Assert.AreEqual(1, state.Resources["water"]);
            Assert.AreEqual(0, state.Statistics.TotalResourcesSpent.Count);
        }

        [Test]
        public void EmergencyScavengeStartsWithoutIdleSurvivorsAndCompletesWithRewards()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Survivors[0].State = SurvivorActivityState.Wounded;
            state.Resources["food"] = 0;
            state.Resources["water"] = 0;

            var result = RecoverySystem.StartEmergencyScavenge(state, config, new EmergencyScavengeRequest { NowUnixMs = 1000 });

            Assert.IsTrue(result.Validation.IsValid);
            Assert.IsTrue(result.Started);
            Assert.IsTrue(state.Recovery.EmergencyScavengeActive);
            Assert.AreEqual(60, state.Recovery.EmergencyScavengeRemainingSeconds);

            RecoverySystem.Tick(state, config, 59);
            Assert.IsTrue(state.Recovery.EmergencyScavengeActive);
            RecoverySystem.Tick(state, config, 1);

            Assert.IsFalse(state.Recovery.EmergencyScavengeActive);
            Assert.AreEqual(18, state.Resources["scrap"]);
            Assert.AreEqual(2, state.Resources["food"]);
            Assert.AreEqual(2, state.Resources["water"]);
            Assert.AreEqual(300, state.Recovery.EmergencyScavengeCooldownRemainingSeconds);
            Assert.AreEqual(1, state.CampEvents.Count);
            Assert.AreEqual(GameEventIds.EmergencyScavengeCompleted, state.CampEvents[0].EventId);

            var cooldown = RecoverySystem.ValidateEmergencyScavenge(state, config, new EmergencyScavengeRequest { NowUnixMs = 62000 });
            Assert.IsFalse(cooldown.IsValid);
            Assert.Contains("Emergency scavenge is cooling down.", cooldown.Errors);

            RecoverySystem.Tick(state, config, 300);
            Assert.IsTrue(RecoverySystem.ValidateEmergencyScavenge(state, config, new EmergencyScavengeRequest { NowUnixMs = 362000 }).IsValid);
        }

        [Test]
        public void EmergencyScavengeUseCaseAndTickMutateStore()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["food"] = 0;
            state.Resources["water"] = 0;
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var start = new StartEmergencyScavengeUseCase(store, new StaticConfigProvider(config));
            var tick = new TickGameUseCase(store, new StaticConfigProvider(config));

            var started = start.ExecuteAsync(new EmergencyScavengeRequest { NowUnixMs = 10 }, CancellationToken.None).GetAwaiter().GetResult();
            tick.ExecuteAsync(60, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(started.Validation.IsValid);
            Assert.IsFalse(store.State.CurrentValue.Recovery.EmergencyScavengeActive);
            Assert.AreEqual(2, store.State.CurrentValue.Resources["food"]);
            Assert.AreEqual(2, store.State.CurrentValue.Resources["water"]);
        }

        [Test]
        public void SetAutosaveUseCaseMutatesStoreAndSavesImmediately()
        {
            var state = GameStateFactory.CreateNew(TestConfigFactory.Create(), 0);
            var store = new GameStateStore();
            var repository = new FakeSaveRepository(state);
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new SetAutosaveUseCase(store, repository);

            var result = useCase.ExecuteAsync(false, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(result.AutosaveEnabled);
            Assert.IsFalse(store.State.CurrentValue.Settings.AutosaveEnabled);
            Assert.NotNull(repository.SavedState);
            Assert.IsFalse(repository.SavedState.Settings.AutosaveEnabled);
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
        public void LaunchWarningsRequireExplicitConfirmation()
        {
            var config = TestConfigFactory.Create();
            config.Zones["abandoned_store"].RecommendedPower = 999;
            var state = GameStateFactory.CreateNew(config, 0);
            var request = Request("abandoned_store", 123);
            request.ConfirmWarnings = false;

            var blocked = ExpeditionLauncher.Launch(state, config, request);

            Assert.IsTrue(blocked.Validation.IsValid);
            Assert.AreEqual(1, blocked.Validation.Warnings.Count);
            Assert.IsNull(blocked.Expedition);
            Assert.AreEqual(0, state.Expeditions.Count);
            Assert.AreEqual(SurvivorActivityState.Idle, state.Survivors[0].State);

            request.ConfirmWarnings = true;
            var launched = ExpeditionLauncher.Launch(state, config, request);

            Assert.IsNotNull(launched.Expedition);
            Assert.AreEqual(1, state.Expeditions.Count);
            Assert.AreEqual(SurvivorActivityState.OnExpedition, state.Survivors[0].State);
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
        public void RecruitmentBroadcastSpendsResourcesAndSelectionAddsSurvivor()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            BuildingSystem.Upgrade(state, config, "barracks");

            var broadcast = RecruitmentSystem.Broadcast(state, config, new BroadcastRecruitmentRequest { Seed = 1, NowUnixMs = 25 });

            Assert.IsTrue(broadcast.Validation.IsValid);
            Assert.AreEqual(1, broadcast.CandidateIds.Count);
            Assert.AreEqual("elias", broadcast.CandidateIds[0]);
            Assert.AreEqual(55, state.Resources["scrap"]);
            Assert.AreEqual(14, state.Resources["food"]);
            Assert.AreEqual(18, state.Resources["water"]);
            Assert.AreEqual(1, state.Recruitment.PendingCandidateIds.Count);

            var result = RecruitmentSystem.Recruit(state, config, new RecruitSurvivorRequest { CandidateId = "elias", NowUnixMs = 50 });

            Assert.IsTrue(result.Validation.IsValid);
            Assert.NotNull(result.Survivor);
            Assert.AreEqual(2, state.Survivors.Count);
            Assert.AreEqual("Elias", state.Survivors[1].Name);
            Assert.AreEqual("rusty_knife", result.Weapon.ItemId);
            Assert.AreEqual(result.Weapon.Uid, result.Survivor.Equipment.WeaponItemUid);
            Assert.AreEqual(55, state.Resources["scrap"]);
            Assert.AreEqual(14, state.Resources["food"]);
            Assert.AreEqual(18, state.Resources["water"]);
            Assert.AreEqual(0, result.Cost.Count);
            Assert.AreEqual(0, state.Recruitment.PendingCandidateIds.Count);
            Assert.AreEqual(1, state.Statistics.SurvivorsRecruited);
            Assert.AreEqual(1, state.CampEvents.Count);
            Assert.AreEqual(GameEventIds.SurvivorJoined, state.CampEvents[0].EventId);
            Assert.AreEqual(result.Survivor.Id, state.CampEvents[0].SubjectId);
        }

        [Test]
        public void RecruitmentBroadcastUsesConfiguredCandidateCount()
        {
            var config = TestConfigFactory.Create();
            config.RecruitableSurvivors["nora"] = new RecruitableSurvivorDefinition
            {
                Id = "nora",
                Name = "Nora",
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
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            BuildingSystem.Upgrade(state, config, "barracks");

            var broadcast = RecruitmentSystem.Broadcast(state, config, new BroadcastRecruitmentRequest { Seed = 1 });

            Assert.IsTrue(broadcast.Validation.IsValid);
            Assert.AreEqual(config.Balance.RecruitmentCandidateCount, broadcast.CandidateIds.Count);
            CollectionAssert.AreEquivalent(new[] { "elias", "nora" }, broadcast.CandidateIds);
        }

        [Test]
        public void RecruitmentCanTargetSpecificCandidate()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            BuildingSystem.Upgrade(state, config, "barracks");

            var broadcast = RecruitmentSystem.Broadcast(state, config, new BroadcastRecruitmentRequest { Seed = 999 });
            var result = RecruitmentSystem.Recruit(state, config, new RecruitSurvivorRequest { CandidateId = broadcast.CandidateIds[0] });

            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual("Elias", result.Survivor.Name);
            Assert.AreEqual(2, state.Survivors.Count);
        }

        [Test]
        public void RecruitmentBlocksUnavailableCandidate()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            BuildingSystem.Upgrade(state, config, "barracks");

            var withoutBroadcast = RecruitmentSystem.Recruit(state, config, new RecruitSurvivorRequest { CandidateId = "elias" });
            var broadcast = RecruitmentSystem.Broadcast(state, config, new BroadcastRecruitmentRequest { Seed = 1 });
            var result = RecruitmentSystem.Recruit(state, config, new RecruitSurvivorRequest { CandidateId = "missing_signal" });

            Assert.IsFalse(withoutBroadcast.Validation.IsValid);
            Assert.IsFalse(result.Validation.IsValid);
            Assert.Contains("Selected survivor signal is unavailable.", result.Validation.Errors);
            Assert.AreEqual(1, state.Survivors.Count);
            Assert.AreEqual(1, broadcast.CandidateIds.Count);
            Assert.AreEqual(1, state.Recruitment.PendingCandidateIds.Count);
        }

        [Test]
        public void RecruitmentSkipClearsPendingSignals()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            BuildingSystem.Upgrade(state, config, "barracks");

            var broadcast = RecruitmentSystem.Broadcast(state, config, new BroadcastRecruitmentRequest { Seed = 1 });
            var skipped = RecruitmentSystem.SkipCandidates(state);

            Assert.IsTrue(broadcast.Validation.IsValid);
            Assert.IsTrue(skipped.Validation.IsValid);
            Assert.AreEqual("elias", skipped.SkippedCandidateIds[0]);
            Assert.AreEqual(0, state.Recruitment.PendingCandidateIds.Count);
            Assert.IsTrue(RecruitmentSystem.ValidateBroadcast(state, config).IsValid);
        }

        [Test]
        public void RecruitSurvivorUseCasesMutateStore()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            BuildingSystem.Upgrade(state, config, "barracks");
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var broadcastUseCase = new BroadcastRecruitmentUseCase(store, new StaticConfigProvider(config));
            var useCase = new RecruitSurvivorUseCase(store, new StaticConfigProvider(config));

            var broadcast = broadcastUseCase.ExecuteAsync(new BroadcastRecruitmentRequest { Seed = 1 }, CancellationToken.None).GetAwaiter().GetResult();
            var result = useCase.ExecuteAsync(new RecruitSurvivorRequest { CandidateId = broadcast.CandidateIds[0] }, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(broadcast.Validation.IsValid);
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
        public void ExpeditionCompletionDamagesEquippedItemsAndBrokenItemsStopContributingPower()
        {
            var config = TestConfigFactory.Create();
            config.Zones["abandoned_store"].DurabilityPressure = 2;
            config.Policies["aggressive"].DurabilityModifier = 1;
            config.Traits["clumsy"] = new TraitDefinition
            {
                Id = "clumsy",
                Name = "Clumsy",
                StatModifiers = new Dictionary<string, int> { { config.Balance.DurabilityTraitModifierId, 2 } }
            };
            var state = GameStateFactory.CreateNew(config, 0);
            var equipped = state.Inventory[0];
            equipped.Durability = 4;
            state.Survivors[0].TraitIds.Add("clumsy");
            var stored = new InventoryItemState { Uid = "item_stored", ItemId = "rusty_revolver", Durability = 60, MaxDurability = 60 };
            state.Inventory.Add(stored);
            var request = Request("abandoned_store", 123);
            request.PolicyId = "aggressive";
            var result = ExpeditionLauncher.Launch(state, config, request);

            ExpeditionSimulator.Complete(state, config, result.Expedition);

            Assert.AreEqual(0, equipped.Durability);
            Assert.AreEqual(60, stored.Durability);
            Assert.AreEqual(4, result.Expedition.EquipmentDurabilityLost[equipped.Uid]);
            Assert.AreEqual(equipped.Uid, result.Expedition.BrokenItemUids[0]);
            Assert.IsNull(SquadPowerSystem.GetEquippedItem(state, config, equipped.Uid));
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
        public void UseMedicineRequiresInfirmaryMedicineAndHealsWound()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            HealingSystem.ApplyWound(state, config, "survivor_1");

            var locked = HealingSystem.UseMedicine(state, config, new UseMedicineRequest { SurvivorId = "survivor_1" });
            Assert.IsFalse(locked.Validation.IsValid);
            Assert.Contains("Infirmary is not ready.", locked.Validation.Errors);

            state.Buildings["infirmary"].Level = 1;
            state.Resources["medicine"] = 0;
            var unaffordable = HealingSystem.UseMedicine(state, config, new UseMedicineRequest { SurvivorId = "survivor_1" });
            Assert.IsFalse(unaffordable.Validation.IsValid);
            Assert.Contains("Not enough medicine.", unaffordable.Validation.Errors);

            state.Resources["medicine"] = 1;
            var result = HealingSystem.UseMedicine(state, config, new UseMedicineRequest { SurvivorId = "survivor_1" });

            Assert.IsTrue(result.Validation.IsValid);
            Assert.IsTrue(result.Healed);
            Assert.AreEqual(0, state.Resources["medicine"]);
            Assert.AreEqual(SurvivorActivityState.Idle, state.Survivors[0].State);
            Assert.AreEqual(state.Survivors[0].MaxHealth, state.Survivors[0].Health);
            Assert.AreEqual(0, state.Survivors[0].StatusEffects.Count);
        }

        [Test]
        public void UseMedicineUseCaseMutatesStore()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Buildings["infirmary"].Level = 1;
            HealingSystem.ApplyWound(state, config, "survivor_1");
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new UseMedicineUseCase(store, new StaticConfigProvider(config));

            var result = useCase.ExecuteAsync(new UseMedicineRequest { SurvivorId = "survivor_1" }, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsTrue(result.Validation.IsValid);
            Assert.AreEqual(SurvivorActivityState.Idle, store.State.CurrentValue.Survivors[0].State);
            Assert.AreEqual(0, store.State.CurrentValue.Resources["medicine"]);
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
        public void TickGameUseCaseReportsCompletedExpeditions()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var launched = ExpeditionLauncher.Launch(state, config, Request("abandoned_store", 123));
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new TickGameUseCase(store, new StaticConfigProvider(config));

            var result = useCase.ExecuteAsync(9999, CancellationToken.None).GetAwaiter().GetResult();

            Assert.Contains(launched.Expedition.Id, result.FinishedExpeditionIds);
            Assert.AreNotEqual(ExpeditionStatus.Active, store.State.CurrentValue.Expeditions[0].Status);
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
        public void OfflineProgressAppliesCampUpkeepAndReportsSpentResources()
        {
            var config = TestConfigFactory.Create();
            config.Balance.CampUpkeepIntervalSeconds = 60;
            config.Balance.CampUpkeepFoodPerSurvivor = 2;
            config.Balance.CampUpkeepWaterPerSurvivor = 1;
            var state = GameStateFactory.CreateNew(config, 0);
            state.LastSaveAtUnixMs = 0;
            state.Resources["food"] = 4;
            state.Resources["water"] = 2;
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new OfflineProgressUseCase(store, new StaticConfigProvider(config));

            var report = useCase.ExecuteAsync(120000, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(120, report.AppliedSeconds);
            Assert.AreEqual(4, report.ResourcesSpent["food"]);
            Assert.AreEqual(2, report.ResourcesSpent["water"]);
            Assert.AreEqual(0, store.State.CurrentValue.Resources["food"]);
            Assert.AreEqual(0, store.State.CurrentValue.Resources["water"]);
            Assert.AreSame(report, store.State.CurrentValue.LastOfflineReport);
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
            Assert.Greater(state.Survivors[0].SkillXp[config.Balance.ExpeditionCompletionSkillId], 0);
            Assert.AreEqual(1, state.Zones["abandoned_store"].Completions);
            Assert.Greater(state.Zones["abandoned_store"].Familiarity, 0);
        }

        [Test]
        public void ExpeditionCompletionUsesConfiguredXpAndSkillLevelRules()
        {
            var config = TestConfigFactory.Create();
            config.Balance.ExpeditionCompletionXp = 8;
            config.Balance.ExpeditionCompletionSkillId = "survival";
            config.Balance.ExpeditionCompletionSkillXp = 10;
            config.Balance.SurvivorXpThresholdBase = 4;
            config.Balance.SurvivorXpThresholdExponent = 1;
            config.Balance.SkillXpThresholdBase = 2;
            config.Balance.SkillXpThresholdExponent = 1;
            var state = GameStateFactory.CreateNew(config, 0);
            var survivor = state.Survivors[0];
            var initialLevel = survivor.Level;
            var initialHealth = survivor.MaxHealth;
            var initialSurvival = survivor.Skills["survival"];
            var result = ExpeditionLauncher.Launch(state, config, Request("abandoned_store", 123));

            ExpeditionSimulator.Complete(state, config, result.Expedition);

            Assert.AreEqual(initialLevel + 1, survivor.Level);
            Assert.AreEqual(4, survivor.Xp);
            Assert.AreEqual(initialHealth + config.Balance.SurvivorHealthPerLevel, survivor.MaxHealth);
            Assert.AreEqual(initialSurvival + 1, survivor.Skills["survival"]);
            Assert.AreEqual(2, survivor.SkillXp["survival"]);
        }

        [Test]
        public void DemoCompletionUsesConfiguredAnyConditionAndRecordsEvent()
        {
            var config = TestConfigFactory.Create();
            config.Balance.DemoCompletionRequiresAnyCondition = true;
            config.Balance.DemoCompletionConditions.Add(new UnlockCondition { Type = GameConditionTypes.ZoneCompletions, Id = "abandoned_store", Value = 1 });
            config.Balance.DemoCompletionConditions.Add(new UnlockCondition { Type = GameConditionTypes.BuildingLevel, Id = "radio_tower", Value = 2 });
            var state = GameStateFactory.CreateNew(config, 0);

            Assert.IsFalse(ProgressionSystem.RefreshDemoCompletion(state, config, 100));

            state.Zones["abandoned_store"].Completions = 1;
            var changed = ProgressionSystem.RefreshDemoCompletion(state, config, 250);
            var second = ProgressionSystem.RefreshDemoCompletion(state, config, 300);

            Assert.IsTrue(changed);
            Assert.IsFalse(second);
            Assert.IsTrue(state.Progress.DemoCompleted);
            Assert.AreEqual(GameConditionTypes.ZoneCompletions + ":abandoned_store", state.Progress.DemoCompletionId);
            Assert.AreEqual(250, state.Progress.DemoCompletedAtUnixMs);
            Assert.AreEqual(1, state.CampEvents.Count);
            Assert.AreEqual(GameEventIds.DemoCompleted, state.CampEvents[0].EventId);
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
        public void OfflineProgressCompletesEmergencyScavengeAndReportsResources()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.LastSaveAtUnixMs = 0;
            state.Resources["food"] = 0;
            state.Resources["water"] = 0;
            RecoverySystem.StartEmergencyScavenge(state, config, new EmergencyScavengeRequest { NowUnixMs = 0 });
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var useCase = new OfflineProgressUseCase(store, new StaticConfigProvider(config));

            var report = useCase.ExecuteAsync(61000, CancellationToken.None).GetAwaiter().GetResult();

            Assert.IsFalse(store.State.CurrentValue.Recovery.EmergencyScavengeActive);
            Assert.AreEqual(2, report.ResourcesGained["food"]);
            Assert.AreEqual(2, report.ResourcesGained["water"]);
            Assert.AreEqual(18, store.State.CurrentValue.Resources["scrap"]);
            Assert.AreEqual(GameEventIds.EmergencyScavengeCompleted, store.State.CurrentValue.CampEvents[0].EventId);
        }

        [Test]
        public void SaveLoadPreservesStateAndFallsBackToBackup()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var result = ExpeditionLauncher.Launch(state, config, Request("abandoned_store", 321));
            state.Resources["scrap"] = 37;
            state.CampUpkeepAccumulatorSeconds = 42;
            state.Inventory[0].Durability = 44;
            state.Buildings["workshop"].Level = 1;
            state.Zones["abandoned_store"].Completions = 2;
            state.Zones["abandoned_store"].Familiarity = 42.5;
            state.Survivors[0].StatusEffects.Add(new StatusEffectState { Id = "sprained_ankle", RemainingSeconds = 120 });
            state.CampEvents.Add(new CampEventState { Id = "event_1", EventId = GameEventIds.SurvivorJoined, SubjectId = "survivor_1", SubjectName = "Mara", DetailId = "scavenger", AtUnixMs = 25 });
            state.Recruitment.PendingCandidateIds.Add("elias");
            state.Recruitment.LastBroadcastAtUnixMs = 25;
            state.Recovery.EmergencyScavengeActive = true;
            state.Recovery.EmergencyScavengeRemainingSeconds = 17;
            state.Recovery.EmergencyScavengeStartedAtUnixMs = 100;
            state.Recovery.EmergencyScavengeCooldownRemainingSeconds = 12;
            state.LastOfflineReport = new OfflineProgressReport { AppliedSeconds = 120, WasPresented = true };
            state.LastOfflineReport.ResourcesGained["food"] = 3;
            state.LastOfflineReport.ResourcesSpent["water"] = 2;
            state.LastOfflineReport.CompletedExpeditionIds.Add(result.Expedition.Id);
            state.LastOfflineReport.WoundedSurvivorIds.Add(state.Survivors[0].Id);
            state.LastOfflineReport.HealedSurvivorIds.Add(state.Survivors[0].Id);
            state.Statistics.TotalResourcesSpent["water"] = 2;
            state.Settings.AutosaveEnabled = false;
            state.Progress.DemoCompleted = true;
            state.Progress.DemoCompletionId = GameConditionTypes.BuildingLevel + ":radio_tower";
            state.Progress.DemoCompletedAtUnixMs = 500;
            result.Expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = 12.5, Message = "Mara found a sealed crate." });
            result.Expedition.EquipmentDurabilityLost[state.Inventory[0].Uid] = 3;
            result.Expedition.BrokenItemUids.Add(state.Inventory[0].Uid);
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
            Assert.AreEqual(42, loaded.State.CampUpkeepAccumulatorSeconds);
            Assert.AreEqual("rusty_knife", loaded.State.Inventory[0].ItemId);
            Assert.AreEqual(44, loaded.State.Inventory[0].Durability);
            Assert.AreEqual(1, loaded.State.Buildings["workshop"].Level);
            Assert.AreEqual(2, loaded.State.Zones["abandoned_store"].Completions);
            Assert.AreEqual(42.5, loaded.State.Zones["abandoned_store"].Familiarity);
            Assert.AreEqual(result.Expedition.Id, loaded.State.Expeditions[0].Id);
            Assert.AreEqual(ExpeditionStatus.Active, loaded.State.Expeditions[0].Status);
            Assert.AreEqual("sprained_ankle", loaded.State.Survivors[0].StatusEffects[0].Id);
            Assert.AreEqual(120, loaded.State.Survivors[0].StatusEffects[0].RemainingSeconds);
            Assert.AreEqual(1, loaded.State.CampEvents.Count);
            Assert.AreEqual(GameEventIds.SurvivorJoined, loaded.State.CampEvents[0].EventId);
            Assert.AreEqual("Mara", loaded.State.CampEvents[0].SubjectName);
            Assert.AreEqual("elias", loaded.State.Recruitment.PendingCandidateIds[0]);
            Assert.AreEqual(25, loaded.State.Recruitment.LastBroadcastAtUnixMs);
            Assert.IsTrue(loaded.State.Recovery.EmergencyScavengeActive);
            Assert.AreEqual(17, loaded.State.Recovery.EmergencyScavengeRemainingSeconds);
            Assert.AreEqual(100, loaded.State.Recovery.EmergencyScavengeStartedAtUnixMs);
            Assert.AreEqual(12, loaded.State.Recovery.EmergencyScavengeCooldownRemainingSeconds);
            Assert.NotNull(loaded.State.LastOfflineReport);
            Assert.AreEqual(120, loaded.State.LastOfflineReport.AppliedSeconds);
            Assert.IsTrue(loaded.State.LastOfflineReport.WasPresented);
            Assert.AreEqual(3, loaded.State.LastOfflineReport.ResourcesGained["food"]);
            Assert.AreEqual(2, loaded.State.LastOfflineReport.ResourcesSpent["water"]);
            Assert.AreEqual(result.Expedition.Id, loaded.State.LastOfflineReport.CompletedExpeditionIds[0]);
            Assert.AreEqual(state.Survivors[0].Id, loaded.State.LastOfflineReport.WoundedSurvivorIds[0]);
            Assert.AreEqual(state.Survivors[0].Id, loaded.State.LastOfflineReport.HealedSurvivorIds[0]);
            Assert.IsFalse(loaded.State.Settings.AutosaveEnabled);
            Assert.AreEqual("Mara found a sealed crate.", loaded.State.Expeditions[0].Log[0].Message);
            Assert.AreEqual(3, loaded.State.Expeditions[0].EquipmentDurabilityLost[state.Inventory[0].Uid]);
            Assert.AreEqual(state.Inventory[0].Uid, loaded.State.Expeditions[0].BrokenItemUids[0]);
            Assert.AreEqual("found_item_1", loaded.State.Expeditions[0].FoundItems[0].Uid);
            Assert.IsTrue(loaded.State.Progress.DemoCompleted);
            Assert.AreEqual(GameConditionTypes.BuildingLevel + ":radio_tower", loaded.State.Progress.DemoCompletionId);
            Assert.AreEqual(500, loaded.State.Progress.DemoCompletedAtUnixMs);
            Assert.AreEqual(2, loaded.State.Statistics.TotalResourcesSpent["water"]);
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

        public GameState SavedState { get; private set; }

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
            SavedState = state;
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
            config.Policies["aggressive"] = new ExpeditionPolicyDefinition { Id = "aggressive", Name = "Aggressive", RiskModifier = 1.2, LootModifier = 1.1, DurationModifier = 0.9, PowerModifier = 1.1, NoiseModifier = 1, DurabilityModifier = 1 };
            config.Items["rusty_knife"] = new ItemDefinition { Id = "rusty_knife", Name = "Rusty Knife", Slot = ItemSlot.Weapon, WeaponType = WeaponType.Melee, BaseDamage = 4, AccuracyBonus = 0.02, CritBonus = 0.01, MaxDurability = 80 };
            config.Items["rusty_revolver"] = new ItemDefinition { Id = "rusty_revolver", Name = "Rusty Revolver", Slot = ItemSlot.Weapon, WeaponType = WeaponType.Firearm, BaseDamage = 10, AccuracyBonus = -0.03, CritBonus = 0.04, NoisePerAttack = 3, MaxDurability = 60 };
            config.Enemies["feral_dog"] = new EnemyDefinition { Id = "feral_dog", Name = "Feral Dog", MaxHealth = 14, Armor = 0, Evasion = 0.08, BaseDamage = 3, AttackType = WeaponType.Melee, Accuracy = 0.75, XpReward = 4 };
            config.Zones["abandoned_store"] = Zone("abandoned_store", 45, 1, 0, 5, Array.Empty<UnlockCondition>());
            config.Zones["dry_suburb"] = Zone("dry_suburb", 90, 1, 1, 8, new[] { new UnlockCondition { Type = GameConditionTypes.ZoneCompletions, Id = "abandoned_store", Value = 2 } });
            config.Zones["police_outpost"] = Zone("police_outpost", 120, 1, 1, 8, new[] { new UnlockCondition { Type = GameConditionTypes.BuildingLevel, Id = "workshop", Value = 1 } });
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
            config.Balance.EmergencyScavengeDurationSeconds = 60;
            config.Balance.EmergencyScavengeCooldownSeconds = 300;
            config.Balance.EmergencyScavengeRewards["scrap"] = 3;
            config.Balance.EmergencyScavengeRewards["food"] = 2;
            config.Balance.EmergencyScavengeRewards["water"] = 2;
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
                RecommendedPower = power,
                DurabilityPressure = 1
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
