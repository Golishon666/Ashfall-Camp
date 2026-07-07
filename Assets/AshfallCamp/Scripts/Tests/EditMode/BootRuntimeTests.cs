using System;
using System.Collections.Generic;
using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Composition;
using AshfallCamp.Domain;
using AshfallCamp.Presentation;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using UnityEngine;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class BootRuntimeTests
    {
        [Test]
        public void BootstrapperAppliesOfflineProgressForLoadedSaveAndPersistsIt()
        {
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult
            {
                State = new GameState(),
                CreatedNew = false
            });
            var offline = new FakeOfflineProgressUseCase(new OfflineProgressReport { AppliedSeconds = 60 });
            var bootstrapper = new GameBootstrapper(saveLoad, offline, new FixedUnixTimeProvider(123456));

            bootstrapper.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, saveLoad.LoadCalls);
            Assert.AreEqual(1, offline.ExecuteCalls);
            Assert.AreEqual(123456, offline.LastNowUnixMs);
            Assert.AreEqual(1, saveLoad.SaveCalls);
        }

        [Test]
        public void BootstrapperSkipsOfflineProgressForNewSave()
        {
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult
            {
                State = new GameState(),
                CreatedNew = true
            });
            var offline = new FakeOfflineProgressUseCase(new OfflineProgressReport { AppliedSeconds = 60 });
            var bootstrapper = new GameBootstrapper(saveLoad, offline, new FixedUnixTimeProvider(123456));

            bootstrapper.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, saveLoad.LoadCalls);
            Assert.AreEqual(0, offline.ExecuteCalls);
            Assert.AreEqual(0, saveLoad.SaveCalls);
        }

        [Test]
        public void BootstrapperDoesNotPersistWhenNoOfflineSecondsApplied()
        {
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult
            {
                State = new GameState(),
                CreatedNew = false
            });
            var offline = new FakeOfflineProgressUseCase(new OfflineProgressReport());
            var bootstrapper = new GameBootstrapper(saveLoad, offline, new FixedUnixTimeProvider(123456));

            bootstrapper.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, offline.ExecuteCalls);
            Assert.AreEqual(0, saveLoad.SaveCalls);
        }

        [Test]
        public void GameClockLoopSavesAfterCompletedExpeditionTick()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult { State = state });
            var tickGame = new FakeTickGameUseCase(new TickGameResult
            {
                FinishedExpeditionIds = { "expedition_1" }
            });
            var loop = new GameClockLoop(tickGame, new StaticConfigProvider(config), saveLoad, store);

            loop.TickStepAsync(1, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, tickGame.ExecuteCalls);
            Assert.AreEqual(1, saveLoad.SaveCalls);
        }

        [Test]
        public void GameClockLoopDoesNotCriticalSaveWhenAutosaveDisabled()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Settings.AutosaveEnabled = false;
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult { State = state });
            var tickGame = new FakeTickGameUseCase(new TickGameResult
            {
                FinishedExpeditionIds = { "expedition_1" }
            });
            var loop = new GameClockLoop(tickGame, new StaticConfigProvider(config), saveLoad, store);

            loop.TickStepAsync(1, CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, tickGame.ExecuteCalls);
            Assert.AreEqual(0, saveLoad.SaveCalls);
        }

        [Test]
        public void LifecycleSaveAdapterSavesOnPauseAndQuit()
        {
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult { State = new GameState() });
            var root = new GameObject("LifecycleSaveAdapterTest");

            try
            {
                var adapter = root.AddComponent<ApplicationLifecycleSaveAdapter>();
                adapter.Construct(saveLoad);

                adapter.NotifyApplicationPaused(false);
                Assert.AreEqual(0, saveLoad.SaveCalls);

                adapter.NotifyApplicationPaused(true);
                Assert.AreEqual(1, saveLoad.SaveCalls);

                adapter.NotifyApplicationQuitting();
                Assert.AreEqual(2, saveLoad.SaveCalls);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void CampHudPresenterCriticalSavesSuccessfulStateChangingUiActions()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var view = new FakeUiRootView();
            var useCases = new FakePresenterUseCases();
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult { State = state });
            var presenter = CreatePresenter(view, store, config, useCases, saveLoad);

            presenter.Start();
            view.RaiseBroadcastRecruitmentRequested();
            view.RaiseRecruitmentCandidatesSkipRequested();
            view.RaiseRecruitRequested(new RecruitSurvivorViewRequest("survivor_02"));
            view.RaiseRepairItemRequested(new RepairItemRequest { ItemUid = "item_1" });
            view.RaiseEquipItemRequested(new EquipItemRequest { SurvivorId = "survivor_1", ItemUid = "item_2" });
            view.RaiseUseMedicineRequested(new UseMedicineRequest { SurvivorId = "survivor_1" });
            view.RaiseStartRestRequested(new StartRestRequest { SurvivorId = "survivor_1" });
            view.RaiseStopRestRequested(new StopRestRequest { SurvivorId = "survivor_1" });
            view.RaiseEmergencyScavengeRequested();
            presenter.Dispose();

            Assert.AreEqual(9, saveLoad.SaveCalls);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.RecruitmentBroadcast);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.RecruitmentSkipped);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.SurvivorRecruited);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.ItemRepaired);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.ItemEquipped);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.MedicineUsed);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.SurvivorRestStarted);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.SurvivorRestStopped);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.EmergencyScavengeStarted);
        }

        [Test]
        public void CampHudPresenterDoesNotCriticalSaveWhenAutosaveDisabled()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.Settings.AutosaveEnabled = false;
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var view = new FakeUiRootView();
            var useCases = new FakePresenterUseCases();
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult { State = state });
            var presenter = CreatePresenter(view, store, config, useCases, saveLoad);

            presenter.Start();
            view.RaiseBroadcastRecruitmentRequested();
            presenter.Dispose();

            Assert.AreEqual(0, saveLoad.SaveCalls);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.RecruitmentBroadcast);
        }

        [Test]
        public void CampHudPresenterDoesNotCriticalSaveBlockedUiActions()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var view = new FakeUiRootView();
            var useCases = new FakePresenterUseCases();
            useCases.BroadcastResult.Validation.Errors.Add("No signal.");
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult { State = state });
            var presenter = CreatePresenter(view, store, config, useCases, saveLoad);

            presenter.Start();
            view.RaiseBroadcastRecruitmentRequested();
            presenter.Dispose();

            Assert.AreEqual(0, saveLoad.SaveCalls);
            CollectionAssert.Contains(view.ToastIds, CampToastIds.ActionBlocked);
        }

        [Test]
        public void CampHudPresenterOpensReportsWhenTrackedExpeditionFinishes()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var launched = ExpeditionLauncher.Launch(state, config, new LaunchExpeditionRequest
            {
                ZoneId = "abandoned_store",
                PolicyId = "balanced",
                Seed = 123,
                NowUnixMs = 1000,
                ConfirmWarnings = true,
                SurvivorIds = new List<string> { state.Survivors[0].Id }
            });
            Assert.IsTrue(launched.Validation.IsValid, string.Join(", ", launched.Validation.Errors));
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var view = new FakeUiRootView();
            var useCases = new FakePresenterUseCases();
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult { State = state });
            var presenter = CreatePresenter(view, store, config, useCases, saveLoad);

            presenter.Start();
            ExpeditionSimulator.Complete(state, config, launched.Expedition);
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            presenter.Dispose();
            store.Dispose();

            CollectionAssert.Contains(view.ToastIds, CampToastIds.ExpeditionCompleted);
            Assert.AreEqual(1, view.OpenReportsCalls);
        }

        [Test]
        public void CampHudPresenterDoesNotReopenExistingFinishedReportOnStart()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var launched = ExpeditionLauncher.Launch(state, config, new LaunchExpeditionRequest
            {
                ZoneId = "abandoned_store",
                PolicyId = "balanced",
                Seed = 123,
                NowUnixMs = 1000,
                ConfirmWarnings = true,
                SurvivorIds = new List<string> { state.Survivors[0].Id }
            });
            Assert.IsTrue(launched.Validation.IsValid, string.Join(", ", launched.Validation.Errors));
            ExpeditionSimulator.Complete(state, config, launched.Expedition);
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var view = new FakeUiRootView();
            var useCases = new FakePresenterUseCases();
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult { State = state });
            var presenter = CreatePresenter(view, store, config, useCases, saveLoad);

            presenter.Start();
            presenter.Dispose();
            store.Dispose();

            CollectionAssert.DoesNotContain(view.ToastIds, CampToastIds.ExpeditionCompleted);
            Assert.AreEqual(0, view.OpenReportsCalls);
        }

        [Test]
        public void CampHudPresenterOpensUnpresentedOfflineReportOnStartAndMarksItPresented()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.LastOfflineReport = new OfflineProgressReport
            {
                AppliedSeconds = config.Balance.OfflineReportMinimumSeconds + 60,
                WasPresented = false
            };
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var view = new FakeUiRootView();
            var useCases = new FakePresenterUseCases();
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult { State = state });
            var presenter = CreatePresenter(view, store, config, useCases, saveLoad);

            presenter.Start();
            presenter.Dispose();
            store.Dispose();

            CollectionAssert.Contains(view.ToastIds, CampToastIds.OfflineReportReady);
            Assert.AreEqual(1, view.OpenReportsCalls);
            Assert.IsTrue(state.LastOfflineReport.WasPresented);
            Assert.AreEqual(1, saveLoad.SaveCalls);
        }

        [Test]
        public void CampHudPresenterDoesNotOpenPresentedOfflineReportOnStart()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            state.LastOfflineReport = new OfflineProgressReport
            {
                AppliedSeconds = config.Balance.OfflineReportMinimumSeconds + 60,
                WasPresented = true
            };
            var store = new GameStateStore();
            store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();
            var view = new FakeUiRootView();
            var useCases = new FakePresenterUseCases();
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult { State = state });
            var presenter = CreatePresenter(view, store, config, useCases, saveLoad);

            presenter.Start();
            presenter.Dispose();
            store.Dispose();

            CollectionAssert.DoesNotContain(view.ToastIds, CampToastIds.OfflineReportReady);
            Assert.AreEqual(0, view.OpenReportsCalls);
            Assert.AreEqual(0, saveLoad.SaveCalls);
        }

        private static CampHudPresenter CreatePresenter(
            FakeUiRootView view,
            GameStateStore store,
            GameConfigSnapshot config,
            FakePresenterUseCases useCases,
            ISaveLoadUseCase saveLoad)
        {
            return new CampHudPresenter(
                view,
                store,
                store,
                new StaticConfigProvider(config),
                useCases,
                useCases,
                useCases,
                useCases,
                useCases,
                useCases,
                useCases,
                useCases,
                useCases,
                useCases,
                useCases,
                useCases,
                saveLoad);
        }

        private sealed class FixedUnixTimeProvider : IUnixTimeProvider
        {
            public FixedUnixTimeProvider(long nowUnixMs)
            {
                NowUnixMs = nowUnixMs;
            }

            public long NowUnixMs { get; private set; }
        }

        private sealed class FakeSaveLoadUseCase : ISaveLoadUseCase
        {
            private readonly LoadGameResult _loadResult;

            public FakeSaveLoadUseCase(LoadGameResult loadResult)
            {
                _loadResult = loadResult;
            }

            public int LoadCalls { get; private set; }
            public int SaveCalls { get; private set; }

            public UniTask<LoadGameResult> LoadOrCreateAsync(CancellationToken ct)
            {
                LoadCalls++;
                return UniTask.FromResult(_loadResult);
            }

            public UniTask SaveAsync(CancellationToken ct)
            {
                SaveCalls++;
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeTickGameUseCase : ITickGameUseCase
        {
            private readonly TickGameResult _result;

            public FakeTickGameUseCase(TickGameResult result)
            {
                _result = result;
            }

            public int ExecuteCalls { get; private set; }

            public UniTask<TickGameResult> ExecuteAsync(double deltaSeconds, CancellationToken ct)
            {
                ExecuteCalls++;
                return UniTask.FromResult(_result);
            }
        }

        private sealed class FakeOfflineProgressUseCase : IOfflineProgressUseCase
        {
            private readonly OfflineProgressReport _report;

            public FakeOfflineProgressUseCase(OfflineProgressReport report)
            {
                _report = report;
            }

            public int ExecuteCalls { get; private set; }
            public long LastNowUnixMs { get; private set; }

            public UniTask<OfflineProgressReport> ExecuteAsync(long nowUnixMs, CancellationToken ct)
            {
                ExecuteCalls++;
                LastNowUnixMs = nowUnixMs;
                return UniTask.FromResult(_report);
            }
        }

        private sealed class FakeUiRootView : IUiRootView
        {
            public readonly List<string> ToastIds = new List<string>();
            public int OpenReportsCalls { get; private set; }

            public Transform Root
            {
                get { return null; }
            }

            public event Action<string> UpgradeRequested;
            public event Action<ExpeditionLaunchViewRequest> ExpeditionLaunchRequested;
            public event Action BroadcastRecruitmentRequested;
            public event Action<RecruitSurvivorViewRequest> RecruitRequested;
            public event Action RecruitmentCandidatesSkipRequested;
            public event Action<RepairItemRequest> RepairItemRequested;
            public event Action<EquipItemRequest> EquipItemRequested;
            public event Action<UseMedicineRequest> UseMedicineRequested;
            public event Action<StartRestRequest> StartRestRequested;
            public event Action<StopRestRequest> StopRestRequested;
            public event Action EmergencyScavengeRequested;
            public event Action<bool> AutosaveChanged;
            public event Action ManualSaveRequested;

            public void Render(GameState state, GameConfigSnapshot config)
            {
            }

            public void OpenReports()
            {
                OpenReportsCalls++;
            }

            public void ShowToast(CampToastRequest request)
            {
                ToastIds.Add(request != null ? request.Id : string.Empty);
            }

            public void RaiseUpgradeRequested(string buildingId)
            {
                UpgradeRequested?.Invoke(buildingId);
            }

            public void RaiseExpeditionLaunchRequested(ExpeditionLaunchViewRequest request)
            {
                ExpeditionLaunchRequested?.Invoke(request);
            }

            public void RaiseBroadcastRecruitmentRequested()
            {
                BroadcastRecruitmentRequested?.Invoke();
            }

            public void RaiseRecruitRequested(RecruitSurvivorViewRequest request)
            {
                RecruitRequested?.Invoke(request);
            }

            public void RaiseRecruitmentCandidatesSkipRequested()
            {
                RecruitmentCandidatesSkipRequested?.Invoke();
            }

            public void RaiseRepairItemRequested(RepairItemRequest request)
            {
                RepairItemRequested?.Invoke(request);
            }

            public void RaiseEquipItemRequested(EquipItemRequest request)
            {
                EquipItemRequested?.Invoke(request);
            }

            public void RaiseUseMedicineRequested(UseMedicineRequest request)
            {
                UseMedicineRequested?.Invoke(request);
            }

            public void RaiseStartRestRequested(StartRestRequest request)
            {
                StartRestRequested?.Invoke(request);
            }

            public void RaiseStopRestRequested(StopRestRequest request)
            {
                StopRestRequested?.Invoke(request);
            }

            public void RaiseEmergencyScavengeRequested()
            {
                EmergencyScavengeRequested?.Invoke();
            }

            public void RaiseAutosaveChanged(bool enabled)
            {
                AutosaveChanged?.Invoke(enabled);
            }

            public void RaiseManualSaveRequested()
            {
                ManualSaveRequested?.Invoke();
            }
        }

        private sealed class FakePresenterUseCases :
            IUpgradeBuildingUseCase,
            ILaunchExpeditionUseCase,
            IBroadcastRecruitmentUseCase,
            IRecruitSurvivorUseCase,
            ISkipRecruitmentCandidatesUseCase,
            IRepairItemUseCase,
            IEquipItemUseCase,
            IUseMedicineUseCase,
            IStartRestUseCase,
            IStopRestUseCase,
            IStartEmergencyScavengeUseCase,
            ISetAutosaveUseCase
        {
            public readonly BuildingUpgradeResult UpgradeResult = new BuildingUpgradeResult
            {
                Building = new BuildingState { Id = "barracks", Level = 1 }
            };

            public readonly LaunchExpeditionResult LaunchResult = new LaunchExpeditionResult
            {
                Expedition = new ExpeditionState { ZoneId = "abandoned_store" }
            };

            public readonly BroadcastRecruitmentResult BroadcastResult = new BroadcastRecruitmentResult();

            public readonly SkipRecruitmentCandidatesResult SkipResult = new SkipRecruitmentCandidatesResult();

            public readonly RecruitSurvivorResult RecruitResult = new RecruitSurvivorResult
            {
                Survivor = new SurvivorState { Id = "survivor_2", Name = "Bram" }
            };

            public readonly RepairItemResult RepairResult = new RepairItemResult
            {
                Item = new InventoryItemState { Uid = "item_1", ItemId = "rusty_knife" }
            };

            public readonly EquipItemResult EquipResult = new EquipItemResult
            {
                Survivor = new SurvivorState { Id = "survivor_1", Name = "Mara" },
                Item = new InventoryItemState { Uid = "item_2", ItemId = "rusty_revolver" }
            };

            public readonly UseMedicineResult UseMedicineResult = new UseMedicineResult
            {
                Survivor = new SurvivorState { Id = "survivor_1", Name = "Mara" },
                Healed = true
            };

            public readonly RestSurvivorResult StartRestResult = new RestSurvivorResult
            {
                Survivor = new SurvivorState { Id = "survivor_1", Name = "Mara" },
                Started = true
            };

            public readonly RestSurvivorResult StopRestResult = new RestSurvivorResult
            {
                Survivor = new SurvivorState { Id = "survivor_1", Name = "Mara" },
                Stopped = true
            };

            public readonly EmergencyScavengeResult EmergencyScavengeResult = new EmergencyScavengeResult
            {
                Started = true,
                DurationSeconds = 60
            };

            public FakePresenterUseCases()
            {
                BroadcastResult.CandidateIds.Add("survivor_02");
                SkipResult.SkippedCandidateIds.Add("survivor_02");
            }

            public UniTask<BuildingUpgradeResult> ExecuteAsync(string buildingId, CancellationToken ct)
            {
                return UniTask.FromResult(UpgradeResult);
            }

            public UniTask<LaunchExpeditionResult> ExecuteAsync(LaunchExpeditionRequest request, CancellationToken ct)
            {
                return UniTask.FromResult(LaunchResult);
            }

            public UniTask<BroadcastRecruitmentResult> ExecuteAsync(BroadcastRecruitmentRequest request, CancellationToken ct)
            {
                return UniTask.FromResult(BroadcastResult);
            }

            public UniTask<RecruitSurvivorResult> ExecuteAsync(RecruitSurvivorRequest request, CancellationToken ct)
            {
                return UniTask.FromResult(RecruitResult);
            }

            public UniTask<SkipRecruitmentCandidatesResult> ExecuteAsync(CancellationToken ct)
            {
                return UniTask.FromResult(SkipResult);
            }

            public UniTask<RepairItemResult> ExecuteAsync(RepairItemRequest request, CancellationToken ct)
            {
                return UniTask.FromResult(RepairResult);
            }

            public UniTask<EquipItemResult> ExecuteAsync(EquipItemRequest request, CancellationToken ct)
            {
                return UniTask.FromResult(EquipResult);
            }

            public UniTask<UseMedicineResult> ExecuteAsync(UseMedicineRequest request, CancellationToken ct)
            {
                return UniTask.FromResult(UseMedicineResult);
            }

            public UniTask<RestSurvivorResult> ExecuteAsync(StartRestRequest request, CancellationToken ct)
            {
                return UniTask.FromResult(StartRestResult);
            }

            public UniTask<RestSurvivorResult> ExecuteAsync(StopRestRequest request, CancellationToken ct)
            {
                return UniTask.FromResult(StopRestResult);
            }

            public UniTask<EmergencyScavengeResult> ExecuteAsync(EmergencyScavengeRequest request, CancellationToken ct)
            {
                return UniTask.FromResult(EmergencyScavengeResult);
            }

            public UniTask<SetAutosaveResult> ExecuteAsync(bool enabled, CancellationToken ct)
            {
                return UniTask.FromResult(new SetAutosaveResult { AutosaveEnabled = enabled });
            }
        }
    }
}
