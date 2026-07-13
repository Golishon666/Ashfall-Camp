using System;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    public sealed class CampMapFogView : MonoBehaviour
    {
        [Header("Map")]
        [SerializeField] private Tilemap contentTilemap;
        [SerializeField] private Tilemap frontierFogTilemap;
        [SerializeField] private Tilemap deepFogTilemap;
        [SerializeField] private TileBase campCoreTile;
        [SerializeField] private TileBase radioactiveSeaTile;
        [SerializeField] private TileBase frontierFogTile;
        [SerializeField] private TileBase deepFogTile;
        [SerializeField] private Camera worldCamera;

        [Header("Unlock popup")]
        [SerializeField] private Canvas rootCanvas;
        [SerializeField] private RectTransform popupRoot;
        [SerializeField] private Button openButton;
        [SerializeField] private TextMeshProUGUI actionLabel;
        [SerializeField] private TextMeshProUGUI priceLabel;
        [SerializeField] private int popupSortingOrder = 100;
        [SerializeField] private Vector2 popupScreenOffset = new Vector2(0f, 72f);
        [SerializeField] private Color affordablePriceColor = new Color32(244, 213, 119, 255);
        [SerializeField] private Color unaffordablePriceColor = new Color32(220, 93, 65, 255);

        private GameState _state;
        private GameConfigSnapshot _config;
        private MapFogTopology _topology;
        private MapCellCoordinate _displayedCell;
        private MapCellCoordinate _pinnedCell;
        private bool _hasDisplayedCell;
        private bool _hasPinnedCell;
        private bool _interactionEnabled;
        private Canvas _popupCanvas;
        private Func<Vector2, bool> _externalPointerBlocker;

        public event Action<MapCellCoordinate> RevealRequested;
        public event Action<MapCellCoordinate> RevealedCellClicked;
        public event Action<MapCellCoordinate> FrontierCellClicked;

        public MapFogTopology Topology => _topology;
        public Tilemap FrontierFogTilemap => frontierFogTilemap;
        public Tilemap DeepFogTilemap => deepFogTilemap;
        public bool IsPopupVisible => popupRoot != null && popupRoot.gameObject.activeSelf;
        public MapCellCoordinate DisplayedCell => _displayedCell;

        public void Configure(
            Tilemap terrain,
            Tilemap frontierOverlay,
            Tilemap deepOverlay,
            TileBase coreTile,
            TileBase seaTile,
            TileBase frontierTile,
            TileBase deepTile,
            Camera camera,
            Canvas canvas,
            RectTransform popup,
            Button button,
            TextMeshProUGUI action,
            TextMeshProUGUI price)
        {
            contentTilemap = terrain;
            frontierFogTilemap = frontierOverlay;
            deepFogTilemap = deepOverlay;
            campCoreTile = coreTile;
            radioactiveSeaTile = seaTile;
            frontierFogTile = frontierTile;
            deepFogTile = deepTile;
            worldCamera = camera;
            rootCanvas = canvas;
            popupRoot = popup;
            openButton = button;
            actionLabel = action;
            priceLabel = price;
            EnsurePopupCanvas();
            BindButton();
            HidePopup();
        }

        private void Awake()
        {
            EnsurePopupCanvas();
            BindButton();
            HidePopup();
        }

        private void OnDestroy()
        {
            if (openButton != null) openButton.onClick.RemoveListener(OnOpenClicked);
        }

        public bool TryBuildTopology(out string error)
        {
            error = string.Empty;
            _topology = null;
            if (contentTilemap == null || campCoreTile == null || radioactiveSeaTile == null)
            {
                error = "Camp map fog references are incomplete.";
                return false;
            }

            var topology = new MapFogTopology();
            var coreCount = 0;
            foreach (var position in contentTilemap.cellBounds.allPositionsWithin)
            {
                var tile = contentTilemap.GetTile(position);
                if (tile == null) continue;
                var cell = new MapCellCoordinate(position.x, position.y);
                if (tile == radioactiveSeaTile)
                {
                    topology.RadioactiveSeaCells.Add(cell);
                    continue;
                }

                topology.RevealableCells.Add(cell);
                if (tile == campCoreTile)
                {
                    topology.Core = cell;
                    coreCount++;
                }
            }

            if (coreCount != 1)
            {
                error = coreCount == 0
                    ? "Camp map fog requires exactly one Camp Core tile, but none was found."
                    : "Camp map fog requires exactly one Camp Core tile, but " + coreCount + " were found.";
                return false;
            }

            _topology = topology;
            _interactionEnabled = true;
            return true;
        }

        public void DisableInteraction(string reason)
        {
            _interactionEnabled = false;
            HidePopup();
            if (!string.IsNullOrWhiteSpace(reason)) Debug.LogError(reason, this);
        }

        public void SetExternalPointerBlocker(Func<Vector2, bool> blocker)
        {
            _externalPointerBlocker = blocker;
        }

        public void HideUnlockPopup()
        {
            HidePopup();
        }

        public void Render(GameState state, GameConfigSnapshot config)
        {
            _state = state;
            _config = config;
            if (_topology == null || frontierFogTilemap == null || deepFogTilemap == null) return;

            frontierFogTilemap.ClearAllTiles();
            deepFogTilemap.ClearAllTiles();
            foreach (var cell in _topology.RevealableCells) RenderCell(cell);
            foreach (var cell in _topology.RadioactiveSeaCells) RenderCell(cell);
            frontierFogTilemap.CompressBounds();
            deepFogTilemap.CompressBounds();

            if (_hasDisplayedCell && MapFogSystem.GetVisibility(_state, _topology, _displayedCell) != MapFogVisibility.Frontier)
            {
                HidePopup();
            }
            else if (_hasDisplayedCell)
            {
                RefreshPopup();
            }
        }

        private void RenderCell(MapCellCoordinate cell)
        {
            var position = new Vector3Int(cell.X, cell.Y, 0);
            switch (MapFogSystem.GetVisibility(_state, _topology, cell))
            {
                case MapFogVisibility.Frontier:
                    frontierFogTilemap.SetTile(position, frontierFogTile);
                    break;
                case MapFogVisibility.Deep:
                    deepFogTilemap.SetTile(position, deepFogTile);
                    break;
            }
        }

        private void Update()
        {
            if (!_interactionEnabled || _topology == null || _state == null || _config == null) return;
            if (worldCamera == null) worldCamera = Camera.main;
            if (worldCamera == null) return;

            // The dashboard contains full-screen raycast targets. Using
            // EventSystem.IsPointerOverGameObject here blocks every map click,
            // even when the pointer is visually over a tile. Only the popup
            // itself must retain the pointer so its OPEN button can be pressed.
            if (IsPointerOverPopup())
            {
                if (_hasDisplayedCell) PositionPopup(_displayedCell);
                return;
            }

            if (_externalPointerBlocker != null && _externalPointerBlocker(Input.mousePosition)) return;

            var mouseWorld = worldCamera.ScreenToWorldPoint(Input.mousePosition);
            var tilePosition = contentTilemap.WorldToCell(mouseWorld);
            var hovered = new MapCellCoordinate(tilePosition.x, tilePosition.y);
            var isFrontier = MapFogSystem.GetVisibility(_state, _topology, hovered) == MapFogVisibility.Frontier;

            if (Input.GetMouseButtonDown(0))
            {
                HandleMapClick(hovered);
                return;
            }

            if (_hasPinnedCell)
            {
                ShowPopup(_pinnedCell);
            }
            else if (isFrontier)
            {
                ShowPopup(hovered);
            }
            else
            {
                HidePopup();
            }
        }

        public void HandleMapClick(MapCellCoordinate cell)
        {
            if (!_interactionEnabled || _topology == null || _state == null || contentTilemap == null) return;

            var visibility = MapFogSystem.GetVisibility(_state, _topology, cell);
            if (visibility == MapFogVisibility.Frontier)
            {
                FrontierCellClicked?.Invoke(cell);
                _hasPinnedCell = true;
                _pinnedCell = cell;
                ShowPopup(cell);
                return;
            }

            HidePopup();
            if (visibility != MapFogVisibility.Revealed) return;
            if (contentTilemap.GetTile(new Vector3Int(cell.X, cell.Y, 0)) == null) return;
            RevealedCellClicked?.Invoke(cell);
        }

        private void ShowPopup(MapCellCoordinate cell)
        {
            _displayedCell = cell;
            _hasDisplayedCell = true;
            if (popupRoot != null)
            {
                EnsurePopupCanvas();
                if (!popupRoot.gameObject.activeSelf) popupRoot.gameObject.SetActive(true);
                EnsurePopupVisualsEnabled();
                popupRoot.SetAsLastSibling();
            }
            RefreshPopup();
            PositionPopup(cell);
        }

        private void HidePopup()
        {
            _hasDisplayedCell = false;
            _hasPinnedCell = false;
            if (popupRoot != null) popupRoot.gameObject.SetActive(false);
        }

        private void RefreshPopup()
        {
            if (!_hasDisplayedCell || _config == null || _state == null) return;
            var balance = _config.Balance;
            var distance = MapFogSystem.ChebyshevDistance(_displayedCell, _topology.Core);
            var cost = MapFogSystem.CalculateCost(balance, distance);
            var available = 0;
            _state.Resources.TryGetValue(balance.MapRevealResourceId, out available);
            var affordable = available >= cost;
            if (actionLabel != null) actionLabel.text = affordable ? "OPEN" : "NOT ENOUGH SCRAP";
            if (priceLabel != null)
            {
                priceLabel.text = cost + " SCRAP";
                priceLabel.color = affordable ? affordablePriceColor : unaffordablePriceColor;
            }
            if (openButton != null) openButton.interactable = affordable;
        }

        private void PositionPopup(MapCellCoordinate cell)
        {
            if (popupRoot == null || rootCanvas == null || contentTilemap == null || worldCamera == null) return;
            var cellCenter = contentTilemap.GetCellCenterWorld(new Vector3Int(cell.X, cell.Y, 0));
            var cellTop = cellCenter + Vector3.up * contentTilemap.layoutGrid.cellSize.y * 0.5f;
            var screen = (Vector2)worldCamera.WorldToScreenPoint(cellTop) + popupScreenOffset;
            var canvasRect = popupRoot.parent as RectTransform;
            var eventCamera = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : rootCanvas.worldCamera;
            Vector2 localPoint;
            if (canvasRect != null && RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRect, screen, eventCamera, out localPoint))
            {
                var halfWidth = popupRoot.rect.width * 0.5f;
                var halfHeight = popupRoot.rect.height * 0.5f;
                localPoint.x = Mathf.Clamp(localPoint.x, canvasRect.rect.xMin + halfWidth, canvasRect.rect.xMax - halfWidth);
                localPoint.y = Mathf.Clamp(localPoint.y, canvasRect.rect.yMin + halfHeight, canvasRect.rect.yMax - halfHeight);
                popupRoot.localPosition = localPoint;
            }
        }

        private void BindButton()
        {
            if (openButton == null) return;
            openButton.onClick.RemoveListener(OnOpenClicked);
            openButton.onClick.AddListener(OnOpenClicked);
        }

        private void EnsurePopupCanvas()
        {
            if (popupRoot == null || _popupCanvas != null) return;

            var staleRaycaster = popupRoot.GetComponent<GraphicRaycaster>();
            if (staleRaycaster != null)
            {
                staleRaycaster.enabled = false;
                if (Application.isPlaying) Destroy(staleRaycaster);
                else DestroyImmediate(staleRaycaster);
            }
            var staleCanvas = popupRoot.GetComponent<Canvas>();
            if (staleCanvas != null)
            {
                staleCanvas.enabled = false;
                if (Application.isPlaying) Destroy(staleCanvas);
                else DestroyImmediate(staleCanvas);
            }

            var canvasObject = new GameObject(
                "MapFogPopupCanvas",
                typeof(RectTransform),
                typeof(Canvas),
                typeof(CanvasScaler),
                typeof(GraphicRaycaster));
            _popupCanvas = canvasObject.GetComponent<Canvas>();
            _popupCanvas.renderMode = rootCanvas != null ? rootCanvas.renderMode : RenderMode.ScreenSpaceOverlay;
            _popupCanvas.worldCamera = rootCanvas != null ? rootCanvas.worldCamera : null;
            _popupCanvas.sortingOrder = popupSortingOrder;

            var popupScaler = canvasObject.GetComponent<CanvasScaler>();
            var sourceScaler = rootCanvas != null ? rootCanvas.GetComponent<CanvasScaler>() : null;
            if (sourceScaler != null)
            {
                popupScaler.uiScaleMode = sourceScaler.uiScaleMode;
                popupScaler.referenceResolution = sourceScaler.referenceResolution;
                popupScaler.screenMatchMode = sourceScaler.screenMatchMode;
                popupScaler.matchWidthOrHeight = sourceScaler.matchWidthOrHeight;
                popupScaler.referencePixelsPerUnit = sourceScaler.referencePixelsPerUnit;
            }

            popupRoot.SetParent(canvasObject.transform, false);
            popupRoot.SetAsLastSibling();
        }

        private void EnsurePopupVisualsEnabled()
        {
            if (popupRoot == null) return;
            foreach (var graphic in popupRoot.GetComponentsInChildren<Graphic>(true))
            {
                graphic.gameObject.SetActive(true);
                graphic.enabled = true;
            }
            if (openButton != null)
            {
                openButton.gameObject.SetActive(true);
                openButton.enabled = true;
            }
        }

        private bool IsPointerOverPopup()
        {
            if (popupRoot == null || !popupRoot.gameObject.activeInHierarchy) return false;
            var eventCamera = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;
            return RectTransformUtility.RectangleContainsScreenPoint(popupRoot, Input.mousePosition, eventCamera);
        }

        private void OnOpenClicked()
        {
            if (!_hasDisplayedCell || openButton == null || !openButton.interactable) return;
            RevealRequested?.Invoke(_displayedCell);
        }
    }
}
