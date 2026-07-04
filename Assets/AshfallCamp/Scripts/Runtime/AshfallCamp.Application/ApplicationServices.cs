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

    public interface ITickGameUseCase
    {
        UniTask ExecuteAsync(double deltaSeconds, CancellationToken ct);
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
        private BuildingUpgradeResult _lastResult;

        public UpgradeBuildingUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
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
                _lastResult = BuildingSystem.Upgrade(state, _configs.Current, buildingId);
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

    public sealed class TickGameUseCase : ITickGameUseCase
    {
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;

        public TickGameUseCase(IGameStateWriter writer, IGameConfigProvider configs)
        {
            _writer = writer;
            _configs = configs;
        }

        public async UniTask ExecuteAsync(double deltaSeconds, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_configs.Current == null)
            {
                await _configs.LoadAsync(ct);
            }

            await _writer.MutateAsync(state =>
            {
                var dt = Math.Max(0, deltaSeconds);
                state.TotalPlayTimeSeconds += dt;
                BuildingSystem.TickProduction(state, _configs.Current, dt);
                ExpeditionSimulator.TickAll(state, _configs.Current, dt);
                return state;
            }, ct);
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
                var activeBefore = ActiveExpeditionIds(state);
                var elapsedMs = Math.Max(0, nowUnixMs - state.LastSaveAtUnixMs);
                var offlineSeconds = GameMath.Clamp(elapsedMs / 1000.0, 0, _configs.Current.Balance.MaxOfflineSeconds);
                _report.AppliedSeconds = offlineSeconds;
                var remaining = offlineSeconds;
                while (remaining > 0)
                {
                    var dt = Math.Min(10, remaining);
                    state.TotalPlayTimeSeconds += dt;
                    BuildingSystem.TickProduction(state, _configs.Current, dt);
                    ExpeditionSimulator.TickAll(state, _configs.Current, dt);
                    remaining -= dt;
                }

                state.LastSaveAtUnixMs = nowUnixMs;
                FillResourceDelta(beforeResources, state.Resources, _report.ResourcesGained);
                FillCompletedExpeditions(activeBefore, state, _report.CompletedExpeditionIds);
                FillWounded(state, _report.WoundedSurvivorIds);
                return state;
            }, ct);
            return _report;
        }

        private static Dictionary<string, int> CopyResources(Dictionary<string, int> resources)
        {
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
                Migrate(loaded);
                EnsureConfiguredState(loaded, config);
                result.Message = result.UsedBackup ? "Loaded Ashfall Camp backup save." : "Loaded Ashfall Camp save.";
            }

            BuildingSystem.ApplyAllBuildingEffects(loaded, config);
            UnlockSystem.RefreshZoneUnlocks(loaded, config);
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

        private static void Migrate(GameState state)
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
            if (state.Settings == null) state.Settings = new GameSettings();
            if (state.Statistics == null) state.Statistics = new GameStatistics();
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
                    state.Buildings[building.Id] = new BuildingState
                    {
                        Id = building.Id,
                        Level = building.StartingLevel,
                        IsUnlocked = building.StartsUnlocked
                    };
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
