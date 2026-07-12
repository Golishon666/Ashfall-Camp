using System;
using System.Collections.Generic;
using System.Threading;
using AshfallCamp.Domain;
using Cysharp.Threading.Tasks;
using R3;

namespace AshfallCamp.Application
{
    public interface IGameConfigProvider
    {
        UniTask<GameConfigSnapshot> LoadAsync(CancellationToken ct);
        GameConfigSnapshot Current { get; }
    }

    public interface IGameStateReader
    {
        ReadOnlyReactiveProperty<GameState> State { get; }
    }

    public interface IGameStateWriter
    {
        UniTask MutateAsync(Func<GameState, GameState> mutation, CancellationToken ct);
    }

    public interface ILaunchExpeditionUseCase
    {
        UniTask<LaunchExpeditionResult> ExecuteAsync(LaunchExpeditionRequest request, CancellationToken ct);
    }

    public interface IUpgradeBuildingUseCase
    {
        UniTask<BuildingUpgradeResult> ExecuteAsync(string buildingId, CancellationToken ct);
    }

    public interface IRecruitSurvivorUseCase
    {
        UniTask<RecruitSurvivorResult> ExecuteAsync(RecruitSurvivorRequest request, CancellationToken ct);
    }

    public interface IBroadcastRecruitmentUseCase
    {
        UniTask<BroadcastRecruitmentResult> ExecuteAsync(BroadcastRecruitmentRequest request, CancellationToken ct);
    }

    public interface ISkipRecruitmentCandidatesUseCase
    {
        UniTask<SkipRecruitmentCandidatesResult> ExecuteAsync(CancellationToken ct);
    }

    public interface IRepairItemUseCase
    {
        UniTask<RepairItemResult> ExecuteAsync(RepairItemRequest request, CancellationToken ct);
    }

    public interface IEquipItemUseCase
    {
        UniTask<EquipItemResult> ExecuteAsync(EquipItemRequest request, CancellationToken ct);
    }

    public interface IUseMedicineUseCase
    {
        UniTask<UseMedicineResult> ExecuteAsync(UseMedicineRequest request, CancellationToken ct);
    }

    public interface IStartRestUseCase
    {
        UniTask<RestSurvivorResult> ExecuteAsync(StartRestRequest request, CancellationToken ct);
    }

    public interface IStopRestUseCase
    {
        UniTask<RestSurvivorResult> ExecuteAsync(StopRestRequest request, CancellationToken ct);
    }

    public interface IStartEmergencyScavengeUseCase
    {
        UniTask<EmergencyScavengeResult> ExecuteAsync(EmergencyScavengeRequest request, CancellationToken ct);
    }

    public interface ISetAutosaveUseCase
    {
        UniTask<SetAutosaveResult> ExecuteAsync(bool enabled, CancellationToken ct);
    }

    public interface ITickGameUseCase
    {
        UniTask<TickGameResult> ExecuteAsync(double deltaSeconds, CancellationToken ct);
    }

    public interface IOfflineProgressUseCase
    {
        UniTask<OfflineProgressReport> ExecuteAsync(long nowUnixMs, CancellationToken ct);
    }

    public interface ISaveLoadUseCase
    {
        UniTask<LoadGameResult> LoadOrCreateAsync(CancellationToken ct);
        UniTask SaveAsync(CancellationToken ct);
    }

    public interface ISaveRepository
    {
        UniTask<SaveRepositoryLoadResult> LoadAsync(CancellationToken ct);
        UniTask SaveAsync(GameState state, CancellationToken ct);
    }

    public interface IUnixTimeProvider
    {
        long NowUnixMs { get; }
    }

    public sealed class SystemUnixTimeProvider : IUnixTimeProvider
    {
        public long NowUnixMs
        {
            get { return DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(); }
        }
    }

    public sealed class LoadGameResult
    {
        public GameState State;
        public bool CreatedNew;
        public bool UsedBackup;
        public string Message = string.Empty;
    }

    public sealed class SaveRepositoryLoadResult
    {
        public GameState State;
        public bool UsedBackup;
    }

    public sealed class SetAutosaveResult
    {
        public bool AutosaveEnabled;
    }

    public sealed class TickGameResult
    {
        public List<string> CompletedExpeditionIds = new List<string>();
        public List<string> FinishedExpeditionIds = new List<string>();
        public List<string> RestCompletedSurvivorIds = new List<string>();
        public List<string> CompletedBuildingIds = new List<string>();

        public bool HasCriticalProgress
        {
            get { return FinishedExpeditionIds.Count > 0 || RestCompletedSurvivorIds.Count > 0 || CompletedBuildingIds.Count > 0; }
        }
    }

    public sealed class GameStateStore : IGameStateReader, IGameStateWriter, IDisposable
    {
        private readonly ReactiveProperty<GameState> _state;
        private readonly ReadOnlyReactiveProperty<GameState> _readOnlyState;

        public GameStateStore()
        {
            _state = new ReactiveProperty<GameState>(new GameState());
            _readOnlyState = _state.ToReadOnlyReactiveProperty();
        }

        public ReadOnlyReactiveProperty<GameState> State
        {
            get { return _readOnlyState; }
        }

        public UniTask MutateAsync(Func<GameState, GameState> mutation, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (mutation == null) throw new ArgumentNullException("mutation");
            var current = _state.Value;
            var next = mutation(current) ?? current;
            if (ReferenceEquals(next, current))
            {
                _state.ForceNotify();
            }
            else
            {
                _state.Value = next;
            }

            return UniTask.CompletedTask;
        }

        public void Dispose()
        {
            _readOnlyState.Dispose();
            _state.Dispose();
        }
    }

    public sealed class LaunchExpeditionUseCase : ILaunchExpeditionUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private LaunchExpeditionResult _lastResult;

        public LaunchExpeditionUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
        }

        public async UniTask<LaunchExpeditionResult> ExecuteAsync(LaunchExpeditionRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            await EnsureConfigAsync(ct);
            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = ExpeditionLauncher.Launch(state, _configs.Current, request);
                return state;
            }, ct);
            return _lastResult;
        }

        private async UniTask EnsureConfigAsync(CancellationToken ct)
        {
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }
        }
    }

    public sealed class UpgradeBuildingUseCase : IUpgradeBuildingUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private readonly IUnixTimeProvider _clock;
        private BuildingUpgradeResult _lastResult;

        public UpgradeBuildingUseCase(IGameStateWriter writer, IGameConfigProvider configs, IUnixTimeProvider clock = null)
        {
            _writer = writer;
            _configs = configs;
            _clock = clock ?? new SystemUnixTimeProvider();
        }

        public async UniTask<BuildingUpgradeResult> ExecuteAsync(string buildingId, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = BuildingSystem.Upgrade(state, _configs.Current, buildingId, _clock.NowUnixMs);
                return state;
            }, ct);
            return _lastResult;
        }
    }

    public sealed class BroadcastRecruitmentUseCase : IBroadcastRecruitmentUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private BroadcastRecruitmentResult _lastResult;

        public BroadcastRecruitmentUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
        }

        public async UniTask<BroadcastRecruitmentResult> ExecuteAsync(BroadcastRecruitmentRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = RecruitmentSystem.Broadcast(state, _configs.Current, request);
                return state;
            }, ct);
            return _lastResult;
        }
    }

    public sealed class SkipRecruitmentCandidatesUseCase : ISkipRecruitmentCandidatesUseCase
    {
        private readonly IGameStateWriter _writer;
        private SkipRecruitmentCandidatesResult _lastResult;

        public SkipRecruitmentCandidatesUseCase(IGameStateWriter writer)
        {
            _writer = writer;
        }

        public async UniTask<SkipRecruitmentCandidatesResult> ExecuteAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = RecruitmentSystem.SkipCandidates(state);
                return state;
            }, ct);
            return _lastResult;
        }
    }

    public sealed class RecruitSurvivorUseCase : IRecruitSurvivorUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private RecruitSurvivorResult _lastResult;

        public RecruitSurvivorUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
        }

        public async UniTask<RecruitSurvivorResult> ExecuteAsync(RecruitSurvivorRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = RecruitmentSystem.Recruit(state, _configs.Current, request);
                return state;
            }, ct);
            return _lastResult;
        }
    }

    public sealed class RepairItemUseCase : IRepairItemUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private RepairItemResult _lastResult;

        public RepairItemUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
        }

        public async UniTask<RepairItemResult> ExecuteAsync(RepairItemRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = WorkshopSystem.Repair(state, _configs.Current, request);
                return state;
            }, ct);
            return _lastResult;
        }
    }

    public sealed class EquipItemUseCase : IEquipItemUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private EquipItemResult _lastResult;

        public EquipItemUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
        }

        public async UniTask<EquipItemResult> ExecuteAsync(EquipItemRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = WorkshopSystem.Equip(state, _configs.Current, request);
                return state;
            }, ct);
            return _lastResult;
        }
    }

    public sealed class UseMedicineUseCase : IUseMedicineUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private UseMedicineResult _lastResult;

        public UseMedicineUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
        }

        public async UniTask<UseMedicineResult> ExecuteAsync(UseMedicineRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = HealingSystem.UseMedicine(state, _configs.Current, request);
                return state;
            }, ct);
            return _lastResult;
        }
    }

    public sealed class StartRestUseCase : IStartRestUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private RestSurvivorResult _lastResult;

        public StartRestUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
        }

        public async UniTask<RestSurvivorResult> ExecuteAsync(StartRestRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = RestSystem.StartRest(state, _configs.Current, request);
                return state;
            }, ct);
            return _lastResult;
        }
    }

    public sealed class StopRestUseCase : IStopRestUseCase
    {
        private readonly IGameStateWriter _writer;
        private RestSurvivorResult _lastResult;

        public StopRestUseCase(IGameStateWriter writer)
        {
            _writer = writer;
        }

        public async UniTask<RestSurvivorResult> ExecuteAsync(StopRestRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = RestSystem.StopRest(state, request);
                return state;
            }, ct);
            return _lastResult;
        }
    }

    public sealed class StartEmergencyScavengeUseCase : IStartEmergencyScavengeUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private EmergencyScavengeResult _lastResult;

        public StartEmergencyScavengeUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
        }

        public async UniTask<EmergencyScavengeResult> ExecuteAsync(EmergencyScavengeRequest request, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            _lastResult = null;
            await _writer.MutateAsync(state =>
            {
                _lastResult = RecoverySystem.StartEmergencyScavenge(state, _configs.Current, request);
                return state;
            }, ct);
            return _lastResult;
        }
    }

    public sealed class SetAutosaveUseCase : ISetAutosaveUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly ISaveRepository _repository;
        private GameState _snapshot;

        public SetAutosaveUseCase(IGameStateWriter writer, ISaveRepository repository)
        {
            _writer = writer;
            _repository = repository;
        }

        public async UniTask<SetAutosaveResult> ExecuteAsync(bool enabled, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            _snapshot = null;
            await _writer.MutateAsync(state =>
            {
                if (state.Settings == null)
                {
                    state.Settings = new GameSettings();
                }

                state.Settings.AutosaveEnabled = enabled;
                _snapshot = state;
                return state;
            }, ct);

            if (_snapshot != null)
            {
                await _repository.SaveAsync(_snapshot, ct);
            }

            return new SetAutosaveResult { AutosaveEnabled = enabled };
        }
    }

    public sealed class TickGameUseCase : ITickGameUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private readonly IUnixTimeProvider _clock;

        public TickGameUseCase(IGameStateWriter writer, IGameConfigProvider configs, IUnixTimeProvider clock = null)
        {
            _writer = writer;
            _configs = configs;
            _clock = clock ?? new SystemUnixTimeProvider();
        }

        public async UniTask<TickGameResult> ExecuteAsync(double deltaSeconds, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            var result = new TickGameResult();
            await _writer.MutateAsync(state =>
            {
                var dt = Math.Max(0, deltaSeconds);
                var activeBefore = ActiveExpeditionIds(state);
                state.TotalPlayTimeSeconds += dt;
                result.CompletedBuildingIds.AddRange(BuildingSystem.CompleteReadyUpgrades(state, _configs.Current, _clock.NowUnixMs));
                CampUpkeepSystem.Tick(state, _configs.Current, dt);
                BuildingSystem.TickProduction(state, _configs.Current, dt);
                ExpeditionSimulator.TickAll(state, _configs.Current, dt);
                HealingSystem.Tick(state, _configs.Current, dt);
                result.RestCompletedSurvivorIds.AddRange(RestSystem.Tick(state, _configs.Current, dt));
                RecoverySystem.Tick(state, _configs.Current, dt);
                FillCompletedExpeditions(activeBefore, state, result.CompletedExpeditionIds);
                FillFinishedExpeditions(activeBefore, state, result.FinishedExpeditionIds);
                return state;
            }, ct);
            return result;
        }

        private static HashSet<string> ActiveExpeditionIds(GameState state)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var expedition in state.Expeditions)
            {
                if (expedition.Status == ExpeditionStatus.Active) result.Add(expedition.Id);
            }

            return result;
        }

        private static void FillCompletedExpeditions(HashSet<string> activeBefore, GameState state, List<string> output)
        {
            foreach (var expedition in state.Expeditions)
            {
                if (activeBefore.Contains(expedition.Id) && expedition.Status == ExpeditionStatus.Completed)
                {
                    output.Add(expedition.Id);
                }
            }
        }

        private static void FillFinishedExpeditions(HashSet<string> activeBefore, GameState state, List<string> output)
        {
            foreach (var expedition in state.Expeditions)
            {
                if (activeBefore.Contains(expedition.Id) && expedition.Status != ExpeditionStatus.Active && expedition.Status != ExpeditionStatus.Returning)
                {
                    output.Add(expedition.Id);
                }
            }
        }
    }

    public sealed class OfflineProgressUseCase : IOfflineProgressUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private OfflineProgressReport _report;

        public OfflineProgressUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
        }

        public async UniTask<OfflineProgressReport> ExecuteAsync(long nowUnixMs, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            _report = new OfflineProgressReport();
            await _writer.MutateAsync(state =>
            {
                var beforeResources = CopyResources(state.Resources);
                var beforeSpent = CopyResources(state.Statistics.TotalResourcesSpent);
                var activeBefore = ActiveExpeditionIds(state);
                var woundedBefore = WoundedSurvivorIds(state);
                var elapsedMs = Math.Max(0, nowUnixMs - state.LastSaveAtUnixMs);
                var offlineSeconds = GameMath.Clamp(elapsedMs / 1000.0, 0, _configs.Current.Balance.MaxOfflineSeconds);
                _report.AppliedSeconds = offlineSeconds;
                var remaining = offlineSeconds;
                var cursorUnixMs = state.LastSaveAtUnixMs;
                while (remaining > 0)
                {
                    var dt = Math.Min(10, remaining);
                    cursorUnixMs += (long)Math.Round(dt * 1000);
                    state.TotalPlayTimeSeconds += dt;
                    AddUnique(_report.CompletedBuildingIds, BuildingSystem.CompleteReadyUpgrades(state, _configs.Current, cursorUnixMs));
                    CampUpkeepSystem.Tick(state, _configs.Current, dt);
                    BuildingSystem.TickProduction(state, _configs.Current, dt);
                    ExpeditionSimulator.TickAll(state, _configs.Current, dt);
                    HealingSystem.Tick(state, _configs.Current, dt);
                    RestSystem.Tick(state, _configs.Current, dt);
                    RecoverySystem.Tick(state, _configs.Current, dt);
                    remaining -= dt;
                }

                AddUnique(_report.CompletedBuildingIds, BuildingSystem.CompleteReadyUpgrades(state, _configs.Current, nowUnixMs));
                state.LastSaveAtUnixMs = nowUnixMs;
                FillResourceDelta(beforeResources, state.Resources, _report.ResourcesGained);
                FillResourceDelta(beforeSpent, state.Statistics.TotalResourcesSpent, _report.ResourcesSpent);
                FillCompletedExpeditions(activeBefore, state, _report.CompletedExpeditionIds);
                FillWounded(state, _report.WoundedSurvivorIds);
                FillHealed(woundedBefore, state, _report.HealedSurvivorIds);
                state.LastOfflineReport = _report;
                return state;
            }, ct);
            return _report;
        }

        private static Dictionary<string, int> CopyResources(Dictionary<string, int> resources)
        {
            if (resources == null) return new Dictionary<string, int>(StringComparer.Ordinal);
            return new Dictionary<string, int>(resources, StringComparer.Ordinal);
        }

        private static HashSet<string> ActiveExpeditionIds(GameState state)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var expedition in state.Expeditions)
            {
                if (expedition.Status == ExpeditionStatus.Active) result.Add(expedition.Id);
            }
            return result;
        }

        private static HashSet<string> WoundedSurvivorIds(GameState state)
        {
            var result = new HashSet<string>(StringComparer.Ordinal);
            foreach (var survivor in state.Survivors)
            {
                if (survivor.State == SurvivorActivityState.Wounded) result.Add(survivor.Id);
            }

            return result;
        }

        private static void FillResourceDelta(Dictionary<string, int> before, Dictionary<string, int> after, Dictionary<string, int> output)
        {
            foreach (var pair in after)
            {
                int oldValue;
                before.TryGetValue(pair.Key, out oldValue);
                var delta = pair.Value - oldValue;
                if (delta > 0) output[pair.Key] = delta;
            }
        }

        private static void FillCompletedExpeditions(HashSet<string> activeBefore, GameState state, List<string> output)
        {
            foreach (var expedition in state.Expeditions)
            {
                if (activeBefore.Contains(expedition.Id) && expedition.Status == ExpeditionStatus.Completed)
                {
                    output.Add(expedition.Id);
                }
            }
        }

        private static void FillWounded(GameState state, List<string> output)
        {
            foreach (var survivor in state.Survivors)
            {
                if (survivor.State == SurvivorActivityState.Wounded) output.Add(survivor.Id);
            }
        }

        private static void FillHealed(HashSet<string> woundedBefore, GameState state, List<string> output)
        {
            foreach (var survivor in state.Survivors)
            {
                if (woundedBefore.Contains(survivor.Id) && survivor.State != SurvivorActivityState.Wounded)
                {
                    output.Add(survivor.Id);
                }
            }
        }

        private static void AddUnique(List<string> output, List<string> values)
        {
            foreach (var value in values)
            {
                if (!output.Contains(value)) output.Add(value);
            }
        }
    }

    public sealed class SaveLoadUseCase : ISaveLoadUseCase
    {
        private readonly ISaveRepository _repository;
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;

        public SaveLoadUseCase(ISaveRepository repository, IGameStateWriter writer, IGameConfigProvider configs)
        {
            _repository = repository;
            _writer = writer;
            _configs = configs;
        }

        public async UniTask<LoadGameResult> LoadOrCreateAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var config = await _configs.LoadAsync(ct);
            var repositoryResult = await _repository.LoadAsync(ct);
            var loaded = repositoryResult != null ? repositoryResult.State : null;
            var result = new LoadGameResult();
            if (loaded == null)
            {
                loaded = GameStateFactory.CreateNew(config, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                result.CreatedNew = true;
                result.Message = "Created a new Ashfall Camp save.";
            }
            else
            {
                result.UsedBackup = repositoryResult != null && repositoryResult.UsedBackup;
                Migrate(loaded, config);
                EnsureConfiguredState(loaded, config);
                result.Message = result.UsedBackup ? "Loaded Ashfall Camp backup save." : "Loaded Ashfall Camp save.";
            }

            BuildingSystem.CompleteReadyUpgrades(loaded, config, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            BuildingSystem.ApplyAllBuildingEffects(loaded, config);
            UnlockSystem.RefreshZoneUnlocks(loaded, config);
            ProgressionSystem.RefreshDemoCompletion(loaded, config, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            result.State = loaded;
            await _writer.MutateAsync(_ => loaded, ct);
            return result;
        }

        public async UniTask SaveAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            GameState snapshot = null;
            await _writer.MutateAsync(state =>
            {
                state.LastSaveAtUnixMs = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                snapshot = state;
                return state;
            }, ct);
            await _repository.SaveAsync(snapshot, ct);
        }

        private static void Migrate(GameState state, GameConfigSnapshot config)
        {
            if (string.IsNullOrEmpty(state.Version))
            {
                state.Version = GameConstants.CurrentSaveVersion;
            }

            state.Version = GameConstants.CurrentSaveVersion;
            if (state.SurvivorCap <= 0) state.SurvivorCap = 1;
            if (state.SquadSize <= 0) state.SquadSize = 1;
            if (state.Resources == null) state.Resources = new Dictionary<string, int>(StringComparer.Ordinal);
            if (state.ResourceCaps == null) state.ResourceCaps = new Dictionary<string, int>(StringComparer.Ordinal);
            if (state.ResourceProductionRemainders == null) state.ResourceProductionRemainders = new Dictionary<string, double>(StringComparer.Ordinal);
            if (state.Survivors == null) state.Survivors = new List<SurvivorState>();
            if (state.Inventory == null) state.Inventory = new List<InventoryItemState>();
            if (state.Buildings == null) state.Buildings = new Dictionary<string, BuildingState>(StringComparer.Ordinal);
            if (state.Zones == null) state.Zones = new Dictionary<string, ZoneState>(StringComparer.Ordinal);
            if (state.Upgrades == null) state.Upgrades = new Dictionary<string, UpgradeState>(StringComparer.Ordinal);
            if (state.Expeditions == null) state.Expeditions = new List<ExpeditionState>();
            if (state.Recovery == null) state.Recovery = new RecoveryActionState();
            if (state.Progress == null) state.Progress = new GameProgressState();
            if (state.Settings == null) state.Settings = new GameSettings();
            if (state.Statistics == null) state.Statistics = new GameStatistics();

            foreach (var survivor in state.Survivors)
            {
                if (survivor == null) continue;
                if (survivor.Equipment == null) survivor.Equipment = new SurvivorEquipmentState();
                if (!string.IsNullOrWhiteSpace(survivor.Equipment.BackpackItemUid) ||
                    string.IsNullOrWhiteSpace(survivor.Equipment.UtilityItemUid) ||
                    config == null)
                {
                    continue;
                }

                var utilityItem = WorkshopSystem.FindItem(state, survivor.Equipment.UtilityItemUid);
                ItemDefinition definition;
                if (utilityItem != null && config.TryGetItem(utilityItem.ItemId, out definition) && definition.Slot == ItemSlot.Backpack)
                {
                    survivor.Equipment.BackpackItemUid = survivor.Equipment.UtilityItemUid;
                    survivor.Equipment.UtilityItemUid = string.Empty;
                }
            }
        }

        private static void EnsureConfiguredState(GameState state, GameConfigSnapshot config)
        {
            foreach (var resource in config.Resources.Values)
            {
                if (!state.Resources.ContainsKey(resource.Id))
                {
                    state.Resources[resource.Id] = Math.Max(0, resource.StartAmount);
                }

                if (resource.HasCap && !state.ResourceCaps.ContainsKey(resource.Id))
                {
                    state.ResourceCaps[resource.Id] = Math.Max(0, resource.StartCap);
                }
            }

            foreach (var building in config.Buildings.Values)
            {
                if (!state.Buildings.ContainsKey(building.Id))
                {
                    var startLevel = BuildingSystem.GetLevel(building, building.StartingLevel);
                    state.Buildings[building.Id] = new BuildingState
                    {
                        Id = building.Id,
                        Level = building.StartingLevel,
                        IsUnlocked = building.StartsUnlocked,
                        AssignedWorkers = startLevel != null ? startLevel.DefaultWorkers : 0,
                        ConditionPercent = startLevel != null ? startLevel.DefaultConditionPercent : 0
                    };
                }
                else
                {
                    BuildingSystem.ApplyConfiguredDisplayDefaults(state.Buildings[building.Id], building);
                }
            }

            foreach (var zone in config.Zones.Values)
            {
                if (!state.Zones.ContainsKey(zone.Id))
                {
                    state.Zones[zone.Id] = new ZoneState
                    {
                        Id = zone.Id,
                        IsUnlocked = zone.UnlockConditions.Count == 0
                    };
                }
            }
        }
    }
}
