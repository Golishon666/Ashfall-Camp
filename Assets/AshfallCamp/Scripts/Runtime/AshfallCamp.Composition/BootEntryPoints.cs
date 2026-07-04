using System;
using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Domain;
using AshfallCamp.Presentation;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace AshfallCamp.Composition
{
    public sealed class GameBootstrapper : IAsyncStartable
    {
        private readonly ISaveLoadUseCase _saveLoad;
        private readonly IOfflineProgressUseCase _offlineProgress;
        private readonly IUnixTimeProvider _clock;

        public GameBootstrapper(ISaveLoadUseCase saveLoad, IOfflineProgressUseCase offlineProgress, IUnixTimeProvider clock)
        {
            _saveLoad = saveLoad;
            _offlineProgress = offlineProgress;
            _clock = clock;
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            var load = await _saveLoad.LoadOrCreateAsync(cancellation);
            if (load == null || load.CreatedNew || load.State == null) return;

            var report = await _offlineProgress.ExecuteAsync(_clock.NowUnixMs, cancellation);
            if (report != null && report.AppliedSeconds > 0)
            {
                await _saveLoad.SaveAsync(cancellation);
            }
        }
    }

    public sealed class GameClockLoop : ITickable
    {
        private readonly ITickGameUseCase _tickGame;
        private readonly IGameConfigProvider _configs;
        private double _accumulator;
        private bool _isTicking;

        public GameClockLoop(ITickGameUseCase tickGame, IGameConfigProvider configs)
        {
            _tickGame = tickGame;
            _configs = configs;
        }

        public void Tick()
        {
            if (_configs.Current == null) return;
            _accumulator += Time.unscaledDeltaTime;
            var step = Math.Max(0.1, _configs.Current.Balance.SimulationTickSeconds);
            if (_accumulator < step || _isTicking) return;
            _accumulator -= step;
            TickAsync(step).Forget();
        }

        private async UniTaskVoid TickAsync(double step)
        {
            _isTicking = true;
            try
            {
                await _tickGame.ExecuteAsync(step, CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isTicking = false;
            }
        }
    }

    public sealed class AutoSaveLoop : ITickable
    {
        private readonly ISaveLoadUseCase _saveLoad;
        private readonly IGameConfigProvider _configs;
        private readonly IGameStateReader _reader;
        private double _accumulator;
        private bool _isSaving;

        public AutoSaveLoop(ISaveLoadUseCase saveLoad, IGameConfigProvider configs, IGameStateReader reader)
        {
            _saveLoad = saveLoad;
            _configs = configs;
            _reader = reader;
        }

        public void Tick()
        {
            if (_configs.Current == null || !_reader.State.CurrentValue.Settings.AutosaveEnabled) return;
            _accumulator += Time.unscaledDeltaTime;
            if (_accumulator < _configs.Current.Balance.AutosaveSeconds || _isSaving) return;
            _accumulator = 0;
            SaveAsync().Forget();
        }

        private async UniTaskVoid SaveAsync()
        {
            _isSaving = true;
            try
            {
                await _saveLoad.SaveAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isSaving = false;
            }
        }
    }

    public sealed class CampHudPresenter : IStartable, ITickable, IDisposable
    {
        private readonly IUiRootView _view;
        private readonly IGameStateReader _reader;
        private readonly IGameConfigProvider _configs;
        private readonly IUpgradeBuildingUseCase _upgradeBuilding;
        private readonly ILaunchExpeditionUseCase _launchExpedition;
        private readonly IRecruitSurvivorUseCase _recruitSurvivor;
        private IDisposable _stateSubscription;
        private double _accumulator;
        private bool _isUpgrading;
        private bool _isLaunching;
        private bool _isRecruiting;

        public CampHudPresenter(IUiRootView view, IGameStateReader reader, IGameConfigProvider configs, IUpgradeBuildingUseCase upgradeBuilding, ILaunchExpeditionUseCase launchExpedition, IRecruitSurvivorUseCase recruitSurvivor)
        {
            _view = view;
            _reader = reader;
            _configs = configs;
            _upgradeBuilding = upgradeBuilding;
            _launchExpedition = launchExpedition;
            _recruitSurvivor = recruitSurvivor;
        }

        public void Start()
        {
            _view.UpgradeRequested += OnUpgradeRequested;
            _view.ExpeditionLaunchRequested += OnExpeditionLaunchRequested;
            _view.RecruitRequested += OnRecruitRequested;
            _stateSubscription = _reader.State.Subscribe(_ => RenderCurrent());
            RenderCurrent();
        }

        public void Tick()
        {
            if (_configs.Current == null) return;
            _accumulator += Time.unscaledDeltaTime;
            if (_accumulator < 0.2f) return;
            _accumulator = 0;
            RenderCurrent();
        }

        public void Dispose()
        {
            _view.UpgradeRequested -= OnUpgradeRequested;
            _view.ExpeditionLaunchRequested -= OnExpeditionLaunchRequested;
            _view.RecruitRequested -= OnRecruitRequested;
            _stateSubscription?.Dispose();
        }

        private void OnUpgradeRequested(string buildingId)
        {
            if (_isUpgrading) return;
            UpgradeAsync(buildingId).Forget();
        }

        private void OnExpeditionLaunchRequested(ExpeditionLaunchViewRequest request)
        {
            if (_isLaunching || request == null) return;
            LaunchExpeditionAsync(request).Forget();
        }

        private void OnRecruitRequested()
        {
            if (_isRecruiting) return;
            RecruitAsync().Forget();
        }

        private async UniTaskVoid UpgradeAsync(string buildingId)
        {
            _isUpgrading = true;
            try
            {
                var result = await _upgradeBuilding.ExecuteAsync(buildingId, CancellationToken.None);
                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Building upgrade blocked: " + string.Join(", ", result.Validation.Errors));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isUpgrading = false;
            }
        }

        private async UniTaskVoid LaunchExpeditionAsync(ExpeditionLaunchViewRequest request)
        {
            _isLaunching = true;
            try
            {
                if (_configs.Current == null) return;

                var state = _reader.State.CurrentValue;
                var survivorIds = SelectIdleSurvivors(state);
                if (survivorIds.Count == 0)
                {
                    Debug.LogWarning("Expedition launch blocked: no idle survivors.");
                    return;
                }

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var result = await _launchExpedition.ExecuteAsync(new LaunchExpeditionRequest
                {
                    ZoneId = request.ZoneId,
                    PolicyId = ResolvePolicyId(request.PolicyId, _configs.Current),
                    SurvivorIds = survivorIds,
                    Seed = CreateLaunchSeed(state, now),
                    NowUnixMs = now,
                    ConfirmWarnings = true
                }, CancellationToken.None);

                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Expedition launch blocked: " + string.Join(", ", result.Validation.Errors));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isLaunching = false;
            }
        }

        private async UniTaskVoid RecruitAsync()
        {
            _isRecruiting = true;
            try
            {
                if (_configs.Current == null) return;

                var state = _reader.State.CurrentValue;
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var result = await _recruitSurvivor.ExecuteAsync(new RecruitSurvivorRequest
                {
                    Seed = CreateLaunchSeed(state, now),
                    NowUnixMs = now
                }, CancellationToken.None);

                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Recruitment blocked: " + string.Join(", ", result.Validation.Errors));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isRecruiting = false;
            }
        }

        private static System.Collections.Generic.List<string> SelectIdleSurvivors(AshfallCamp.Domain.GameState state)
        {
            var result = new System.Collections.Generic.List<string>();
            var maxCount = Math.Max(1, state.SquadSize);
            foreach (var survivor in state.Survivors)
            {
                if (result.Count >= maxCount) break;
                if (survivor.State == AshfallCamp.Domain.SurvivorActivityState.Idle)
                {
                    result.Add(survivor.Id);
                }
            }

            return result;
        }

        private static string ResolvePolicyId(string requestedPolicyId, AshfallCamp.Domain.GameConfigSnapshot config)
        {
            if (!string.IsNullOrWhiteSpace(requestedPolicyId) && config.Policies.ContainsKey(requestedPolicyId))
            {
                return requestedPolicyId;
            }

            foreach (var policyId in config.Policies.Keys)
            {
                return policyId;
            }

            return string.Empty;
        }

        private static uint CreateLaunchSeed(AshfallCamp.Domain.GameState state, long nowUnixMs)
        {
            unchecked
            {
                return ((uint)nowUnixMs) ^ ((uint)Math.Max(1, state.NextId) * 2654435761u);
            }
        }

        private void RenderCurrent()
        {
            if (_configs.Current == null) return;
            _view.Render(_reader.State.CurrentValue, _configs.Current);
        }
    }
}
