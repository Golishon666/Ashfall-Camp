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
    public sealed class CampMapFogPresenter : IStartable, IDisposable
    {
        private readonly CampMapFogView _view;
        private readonly IUiRootView _uiRoot;
        private readonly IGameStateReader _reader;
        private readonly IGameConfigProvider _configs;
        private readonly IMapFogUseCase _useCase;
        private IDisposable _stateSubscription;
        private bool _initializing;
        private bool _revealing;

        public CampMapFogPresenter(CampMapFogView view, IUiRootView uiRoot, IGameStateReader reader, IGameConfigProvider configs, IMapFogUseCase useCase)
        {
            _view = view;
            _uiRoot = uiRoot;
            _reader = reader;
            _configs = configs;
            _useCase = useCase;
        }

        public void Start()
        {
            string topologyError;
            if (!_view.TryBuildTopology(out topologyError))
            {
                _view.DisableInteraction(topologyError);
                return;
            }

            _view.RevealRequested += OnRevealRequested;
            _stateSubscription = _reader.State.Subscribe(OnStateChanged);
            OnStateChanged(_reader.State.CurrentValue);
        }

        public void Dispose()
        {
            _view.RevealRequested -= OnRevealRequested;
            _stateSubscription?.Dispose();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == null) return;
            if (!state.MapFogInitialized)
            {
                if (!_initializing) InitializeAsync().Forget();
                return;
            }

            if (_configs.Current != null) _view.Render(state, _configs.Current);
        }

        private void OnRevealRequested(MapCellCoordinate cell)
        {
            if (!_revealing) RevealAsync(cell).Forget();
        }

        private async UniTaskVoid InitializeAsync()
        {
            _initializing = true;
            try
            {
                var validation = await _useCase.InitializeAsync(_view.Topology, CancellationToken.None);
                if (!validation.IsValid)
                {
                    _view.DisableInteraction(FormatValidation(validation));
                    return;
                }

                if (_configs.Current != null) _view.Render(_reader.State.CurrentValue, _configs.Current);
            }
            catch (Exception ex)
            {
                _view.DisableInteraction("Failed to initialize camp map fog: " + ex.Message);
                Debug.LogException(ex);
            }
            finally
            {
                _initializing = false;
            }
        }

        private async UniTaskVoid RevealAsync(MapCellCoordinate cell)
        {
            _revealing = true;
            try
            {
                var result = await _useCase.RevealAsync(_view.Topology, cell, CancellationToken.None);
                if (!result.Validation.IsValid)
                {
                    _uiRoot.ShowToast(new CampToastRequest(CampToastIds.ActionBlocked, FormatValidation(result.Validation)));
                }
                else if (_configs.Current != null)
                {
                    _view.Render(_reader.State.CurrentValue, _configs.Current);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _revealing = false;
            }
        }

        private static string FormatValidation(ValidationResult validation)
        {
            return validation == null || validation.Errors == null || validation.Errors.Count == 0
                ? "Map reveal action is unavailable."
                : string.Join(" ", validation.Errors);
        }
    }
}
