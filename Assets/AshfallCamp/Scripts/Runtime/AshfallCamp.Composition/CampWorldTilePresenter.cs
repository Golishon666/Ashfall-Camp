using System;
using System.Collections.Generic;
using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using AshfallCamp.Presentation;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using UnityEngine.Tilemaps;
using VContainer.Unity;

namespace AshfallCamp.Composition
{
    public sealed class CampWorldTilePresenter : IStartable, IDisposable
    {
        private static readonly MapCellCoordinate[] Directions =
        {
            new MapCellCoordinate(-1,-1), new MapCellCoordinate(0,-1), new MapCellCoordinate(1,-1),
            new MapCellCoordinate(-1,0),                                  new MapCellCoordinate(1,0),
            new MapCellCoordinate(-1,1),  new MapCellCoordinate(0,1),  new MapCellCoordinate(1,1)
        };

        private readonly CampWorldTileView _view;
        private readonly CampMapFogView _fogView;
        private readonly GameConfigDatabaseSO _database;
        private readonly IGameStateReader _reader;
        private readonly IGameConfigProvider _configs;
        private readonly ILaunchExpeditionUseCase _launch;
        private readonly IUnixTimeProvider _clock;
        private readonly IUiRootView _uiRoot;
        private IDisposable _subscription;
        private GameState _state;
        private bool _launching;

        public CampWorldTilePresenter(CampWorldTileView view, CampMapFogView fogView, GameConfigDatabaseSO database, IGameStateReader reader, IGameConfigProvider configs, ILaunchExpeditionUseCase launch, IUnixTimeProvider clock, IUiRootView uiRoot)
        {
            _view = view;
            _fogView = fogView;
            _database = database;
            _reader = reader;
            _configs = configs;
            _launch = launch;
            _clock = clock;
            _uiRoot = uiRoot;
        }

        public void Start()
        {
            if (_database == null || _database.WorldTiles == null || _view.ContentTilemap == null || _fogView == null)
            {
                _view.SetInteractionEnabled(false);
                Debug.LogError("World tile interaction references are incomplete.");
                return;
            }
            _fogView.RevealedCellClicked += OnRevealedCellClicked;
            _fogView.FrontierCellClicked += OnFrontierCellClicked;
            _fogView.SetExternalPointerBlocker(_view.IsPointerOverUi);
            _view.LaunchRequested += OnLaunchRequested;
            _subscription = _reader.State.Subscribe(OnStateChanged);
            OnStateChanged(_reader.State.CurrentValue);
        }

        public void Dispose()
        {
            _fogView.RevealedCellClicked -= OnRevealedCellClicked;
            _fogView.FrontierCellClicked -= OnFrontierCellClicked;
            _fogView.SetExternalPointerBlocker(null);
            _view.LaunchRequested -= OnLaunchRequested;
            _subscription?.Dispose();
        }

        private void OnStateChanged(GameState state)
        {
            if (state == null) return;
            _state = state;
            RefreshMarkers();
        }

        private void OnRevealedCellClicked(MapCellCoordinate cell)
        {
            if (_state == null || _configs.Current == null || !_state.RevealedMapCells.Contains(cell)) return;
            var tile = _view.ContentTilemap.GetTile(new Vector3Int(cell.X, cell.Y, 0));
            WorldTileConfigSO asset;
            if (!_database.WorldTiles.TryGetByTile(tile, out asset) || asset == null)
            {
                Debug.LogError("No world tile config is assigned to revealed cell " + cell + ".");
                return;
            }
            if (asset.Type == WorldTileType.Camp || asset.Type == WorldTileType.Impassable) return;
            WorldTileDefinition definition;
            if (!_configs.Current.TryGetWorldTile(asset.Id, out definition))
            {
                Debug.LogError("World tile definition is missing for " + asset.Id + ".");
                return;
            }
            var route = FindRouteTileIds(cell);
            _fogView.HideUnlockPopup();
            _view.Show(definition, cell, route, _state, _configs.Current);
        }

        private void OnFrontierCellClicked(MapCellCoordinate cell)
        {
            _view.Hide();
        }

        private void OnLaunchRequested(WorldTileLaunchViewRequest request)
        {
            if (!_launching) LaunchAsync(request).Forget();
        }

        private async UniTaskVoid LaunchAsync(WorldTileLaunchViewRequest request)
        {
            _launching = true;
            try
            {
                var result = await _launch.ExecuteAsync(new LaunchExpeditionRequest
                {
                    ZoneId = request.ZoneId,
                    WorldTileId = request.TileId,
                    TargetCell = request.Cell,
                    RouteTileIds = new List<string>(request.RouteTileIds),
                    SurvivorIds = new List<string>(request.SurvivorIds),
                    PolicyId = GameIds.Policies.Balanced,
                    Seed = (uint)Math.Max(1, _clock.NowUnixMs.GetHashCode()),
                    NowUnixMs = _clock.NowUnixMs,
                    ConfirmWarnings = true
                }, CancellationToken.None);
                if (!result.Validation.IsValid)
                {
                    _uiRoot.ShowToast(new CampToastRequest(CampToastIds.ActionBlocked, string.Join(" ", result.Validation.Errors)));
                }
                else
                {
                    _uiRoot.ShowToast(new CampToastRequest(CampToastIds.ExpeditionLaunched, "Team sent to " + request.TileId.Replace('_', ' ') + "."));
                }
            }
            catch (Exception exception)
            {
                Debug.LogException(exception);
            }
            finally
            {
                _launching = false;
            }
        }

        private void RefreshMarkers()
        {
            var markers = _view.MarkerTilemap;
            if (markers == null || _state == null) return;
            markers.gameObject.SetActive(true);
            markers.ClearAllTiles();
            foreach (var cell in _state.RevealedMapCells)
            {
                var position = new Vector3Int(cell.X, cell.Y, 0);
                var content = _view.ContentTilemap.GetTile(position);
                WorldTileConfigSO config;
                if (!_database.WorldTiles.TryGetByTile(content, out config) || config.Type != WorldTileType.Expedition || config.MarkerTile == null) continue;
                markers.SetTile(position, config.MarkerTile);
            }
            markers.CompressBounds();
        }

        private List<string> FindRouteTileIds(MapCellCoordinate target)
        {
            var core = FindCoreCell();
            var revealed = new HashSet<MapCellCoordinate>(_state.RevealedMapCells);
            var queue = new Queue<MapCellCoordinate>();
            var previous = new Dictionary<MapCellCoordinate, MapCellCoordinate>();
            queue.Enqueue(core);
            previous[core] = core;
            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current.Equals(target)) break;
                foreach (var direction in Directions)
                {
                    var next = new MapCellCoordinate(current.X + direction.X, current.Y + direction.Y);
                    if (!revealed.Contains(next) || previous.ContainsKey(next)) continue;
                    previous[next] = current;
                    queue.Enqueue(next);
                }
            }
            if (!previous.ContainsKey(target)) return new List<string>();
            var cells = new List<MapCellCoordinate>();
            var cursor = target;
            while (!cursor.Equals(core))
            {
                cells.Add(cursor);
                cursor = previous[cursor];
            }
            cells.Reverse();
            var route = new List<string>();
            foreach (var cell in cells)
            {
                var tile = _view.ContentTilemap.GetTile(new Vector3Int(cell.X, cell.Y, 0));
                WorldTileConfigSO config;
                if (_database.WorldTiles.TryGetByTile(tile, out config) && (config.Type == WorldTileType.Normal || config.Type == WorldTileType.Expedition)) route.Add(config.Id);
            }
            return route;
        }

        private MapCellCoordinate FindCoreCell()
        {
            foreach (var position in _view.ContentTilemap.cellBounds.allPositionsWithin)
            {
                var tile = _view.ContentTilemap.GetTile(position);
                WorldTileConfigSO config;
                if (_database.WorldTiles.TryGetByTile(tile, out config) && config.Id == "camp_core") return new MapCellCoordinate(position.x, position.y);
            }
            return default(MapCellCoordinate);
        }
    }
}
