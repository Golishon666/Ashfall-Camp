using System;
using System.Threading;
using AshfallCamp.Application;
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

        public GameBootstrapper(ISaveLoadUseCase saveLoad)
        {
            _saveLoad = saveLoad;
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            await _saveLoad.LoadOrCreateAsync(cancellation);
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
        private IDisposable _stateSubscription;
        private double _accumulator;
        private bool _isUpgrading;

        public CampHudPresenter(IUiRootView view, IGameStateReader reader, IGameConfigProvider configs, IUpgradeBuildingUseCase upgradeBuilding)
        {
            _view = view;
            _reader = reader;
            _configs = configs;
            _upgradeBuilding = upgradeBuilding;
        }

        public void Start()
        {
            _view.UpgradeRequested += OnUpgradeRequested;
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
            _stateSubscription?.Dispose();
        }

        private void OnUpgradeRequested(string buildingId)
        {
            if (_isUpgrading) return;
            UpgradeAsync(buildingId).Forget();
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

        private void RenderCurrent()
        {
            if (_configs.Current == null) return;
            _view.Render(_reader.State.CurrentValue, _configs.Current);
        }
    }
}
