using System;
using System.Collections.Generic;
using System.Linq;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    public sealed class CampWorldTileView : MonoBehaviour
    {
        [SerializeField] private Tilemap contentTilemap;
        [SerializeField] private Tilemap markerTilemap;
        [SerializeField] private Camera worldCamera;
        [SerializeField] private GameObject tooltipPrefab;
        [SerializeField] private int uiSortingOrder = 90;

        private GameObject _uiInstance;
        private RectTransform _tooltip;
        private RectTransform _roster;
        private Button _gatherButton;
        private Button _sendButton;
        private Button _cancelButton;
        private readonly List<RosterSlot> _rosterSlots = new List<RosterSlot>();
        private readonly List<string> _selectedSurvivorIds = new List<string>();
        private readonly List<string> _routeTileIds = new List<string>();
        private WorldTileDefinition _definition;
        private MapCellCoordinate _cell;
        private GameState _state;
        private bool _interactionEnabled = true;

        public event Action<WorldTileLaunchViewRequest> LaunchRequested;

        public Tilemap ContentTilemap => contentTilemap;
        public Tilemap MarkerTilemap => markerTilemap;
        public bool IsTooltipVisible => _tooltip != null && _tooltip.gameObject.activeInHierarchy;
        public bool IsRosterVisible => _roster != null && _roster.gameObject.activeInHierarchy;

        public void Configure(Tilemap content, Tilemap markers, Camera camera, GameObject prefab)
        {
            contentTilemap = content;
            markerTilemap = markers;
            worldCamera = camera;
            tooltipPrefab = prefab;
            BuildUi();
        }

        private void Awake()
        {
            BuildUi();
        }

        private void OnDestroy()
        {
            UnwireButtons();
        }

        public void SetInteractionEnabled(bool enabled)
        {
            _interactionEnabled = enabled;
            if (!enabled) Hide();
        }

        public void Show(WorldTileDefinition definition, MapCellCoordinate cell, IReadOnlyList<string> routeTileIds, GameState state, GameConfigSnapshot config)
        {
            BuildUi();
            if (_tooltip == null || definition == null) return;
            _definition = definition;
            _cell = cell;
            _state = state;
            _routeTileIds.Clear();
            if (routeTileIds != null) _routeTileIds.AddRange(routeTileIds);
            _selectedSurvivorIds.Clear();
            _uiInstance.SetActive(true);
            _tooltip.gameObject.SetActive(true);
            _roster.gameObject.SetActive(false);
            RenderTooltip(config);
        }

        public void Hide()
        {
            _selectedSurvivorIds.Clear();
            if (_tooltip != null) _tooltip.gameObject.SetActive(false);
            if (_roster != null) _roster.gameObject.SetActive(false);
        }

        private void BuildUi()
        {
            if (_uiInstance != null) return;
            if (tooltipPrefab == null)
            {
                Debug.LogError("World tile tooltip prefab is missing.", this);
                return;
            }
            var canvasObject = new GameObject("WorldTileInteractionCanvas", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var canvas = canvasObject.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = Mathf.Max(110, uiSortingOrder);
            var scaler = canvasObject.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = .5f;

            _uiInstance = Instantiate(tooltipPrefab, canvasObject.transform, false);
            var rootRect = _uiInstance.transform as RectTransform;
            rootRect.anchorMin = Vector2.zero;
            rootRect.anchorMax = Vector2.one;
            rootRect.offsetMin = Vector2.zero;
            rootRect.offsetMax = Vector2.zero;
            _tooltip = FindRecursive(_uiInstance.transform, "WorldTileTooltip") as RectTransform;
            _roster = FindRecursive(_uiInstance.transform, "SquadRosterModal") as RectTransform;
            if (_tooltip == null || _roster == null)
            {
                Debug.LogError("World tile tooltip prefab must contain WorldTileTooltip and SquadRosterModal panels.", this);
                _interactionEnabled = false;
                return;
            }
            var designRoot = FindRecursive(_uiInstance.transform, "11WorldTileTooltipElementwise");
            if (designRoot != null)
            {
                foreach (Transform child in designRoot)
                {
                    if (child != _tooltip && child != _roster) child.gameObject.SetActive(false);
                }
            }
            _gatherButton = FindRecursive(_tooltip, "ButtonGatherresources")?.GetComponent<Button>();
            _sendButton = FindRecursive(_roster, "ButtonSendteam")?.GetComponent<Button>();
            _cancelButton = FindRecursive(_roster, "ButtonCancel")?.GetComponent<Button>();
            if (_gatherButton == null || _sendButton == null || _cancelButton == null)
            {
                Debug.LogError("World tile tooltip prefab is missing one or more required buttons.", this);
                _interactionEnabled = false;
                return;
            }
            if (_gatherButton != null) _gatherButton.onClick.AddListener(OpenRoster);
            if (_sendButton != null) _sendButton.onClick.AddListener(SendTeam);
            if (_cancelButton != null) _cancelButton.onClick.AddListener(CloseRoster);
            BuildRosterSlots();
            Hide();
        }

        private void BuildRosterSlots()
        {
            _rosterSlots.Clear();
            var names = new[] { "SurvivorAsha", "SurvivorBram", "SurvivorCora", "SurvivorDima", "SurvivorElka", "SurvivorFaro" };
            foreach (var name in names)
            {
                var root = FindRecursive(_roster, name);
                if (root == null) continue;
                var slot = new RosterSlot
                {
                    Root = root.gameObject,
                    Button = root.GetComponent<Button>(),
                    Name = FindText(root, "Name"),
                    Stats = FindText(root, "Stats")
                };
                var captured = slot;
                if (slot.Button != null) slot.Button.onClick.AddListener(() => ToggleSurvivor(captured));
                _rosterSlots.Add(slot);
            }
        }

        private void RenderTooltip(GameConfigSnapshot config)
        {
            SetText(_tooltip, "Type", _definition.Type == WorldTileType.Expedition ? "EXPEDITION LOCATION" : "NORMAL LOCATION");
            SetText(_tooltip, "Title", _definition.Name.ToUpperInvariant());
            SetText(_tooltip, "Biome", _definition.Category.ToUpperInvariant());
            SetText(_tooltip, "StrengthLabel", "THREAT " + _definition.Strength);
            SetText(_tooltip, "RiskValue", Math.Min(100, _definition.Strength * 9) + " / 100");
            SetPill(_tooltip, "TravelWater", "-" + _definition.WaterCostPerSurvivor);
            SetPill(_tooltip, "TravelFood", "-" + _definition.FoodCostPerSurvivor);
            SetPill(_tooltip, "EncounterChance", Math.Round(_definition.EncounterChance * 100) + "%");
            SetRewardPill("LootFood", "food");
            SetRewardPill("LootScrap", "scrap");
            SetRewardPill("LootWater", "water");
            SetText(_tooltip, "RareGear", "RARE EQUIPMENT  •  " + Math.Round(_definition.RareEquipmentChance * 100) + "%");
            RenderEnemies(config);
            SetText(_tooltip, "RouteValue", _routeTileIds.Count + " OPEN CELLS  •  OUT + RETURN");
            var routeFood = RouteCost(config, true);
            var routeWater = RouteCost(config, false);
            SetText(_tooltip, "RouteCost", "TOTAL PER SURVIVOR:  FOOD -" + routeFood + "  /  WATER -" + routeWater);
            SetButtonLabel(_gatherButton, _definition.Type == WorldTileType.Expedition ? "OPEN EXPEDITION" : "GATHER RESOURCES");
        }

        private void OpenRoster()
        {
            if (_definition == null || _state == null) return;
            _tooltip.gameObject.SetActive(false);
            _roster.gameObject.SetActive(true);
            _selectedSurvivorIds.Clear();
            var idle = _state.Survivors.Where(s => s != null && s.State == SurvivorActivityState.Idle).Take(_rosterSlots.Count).ToList();
            for (var i = 0; i < _rosterSlots.Count; i++)
            {
                var slot = _rosterSlots[i];
                var survivor = i < idle.Count ? idle[i] : null;
                slot.SurvivorId = survivor != null ? survivor.Id : string.Empty;
                slot.Root.SetActive(survivor != null);
                if (survivor == null) continue;
                if (slot.Name != null) slot.Name.text = survivor.Name.ToUpperInvariant();
                if (slot.Stats != null) slot.Stats.text = "LEVEL " + survivor.Level + "  •  HP " + survivor.Health + "/" + survivor.MaxHealth;
                if (slot.Button != null) slot.Button.interactable = true;
                SetSelected(slot, false);
            }
            if (idle.Count == 0 && _rosterSlots.Count > 0)
            {
                var empty = _rosterSlots[0];
                empty.Root.SetActive(true);
                empty.SurvivorId = string.Empty;
                if (empty.Name != null) empty.Name.text = "NO IDLE SURVIVORS";
                if (empty.Stats != null) empty.Stats.text = "RECRUIT A SURVIVOR OR WAIT FOR THE TEAM TO RETURN";
                if (empty.Button != null) empty.Button.interactable = false;
            }
            SetText(_roster, "Mission", _definition.Name.ToUpperInvariant() + "  •  THREAT " + _definition.Strength);
            RefreshRosterSummary();
        }

        private void ToggleSurvivor(RosterSlot slot)
        {
            if (slot == null || string.IsNullOrWhiteSpace(slot.SurvivorId)) return;
            if (_selectedSurvivorIds.Contains(slot.SurvivorId)) _selectedSurvivorIds.Remove(slot.SurvivorId);
            else if (_selectedSurvivorIds.Count < 4) _selectedSurvivorIds.Add(slot.SurvivorId);
            SetSelected(slot, _selectedSurvivorIds.Contains(slot.SurvivorId));
            RefreshRosterSummary();
        }

        private void RefreshRosterSummary()
        {
            SetText(_roster, "RosterCount", _selectedSurvivorIds.Count + " / 4");
            if (_sendButton != null) _sendButton.interactable = _selectedSurvivorIds.Count > 0;
        }

        private void SendTeam()
        {
            if (_definition == null || _selectedSurvivorIds.Count == 0) return;
            LaunchRequested?.Invoke(new WorldTileLaunchViewRequest(_definition.Id, _definition.ZoneId, _cell, _routeTileIds, _selectedSurvivorIds));
            Hide();
        }

        private void CloseRoster()
        {
            _roster.gameObject.SetActive(false);
            _tooltip.gameObject.SetActive(true);
        }

        private void RenderEnemies(GameConfigSnapshot config)
        {
            var rows = new[] { "EnemyASHROACH", "EnemyMIRELEECH", "EnemyRADIATEDHOUND" };
            for (var i = 0; i < rows.Length; i++)
            {
                var row = FindRecursive(_tooltip, rows[i]);
                if (row == null) continue;
                var entry = i < _definition.EnemyTable.Count ? _definition.EnemyTable[i] : null;
                row.gameObject.SetActive(entry != null);
                if (entry == null) continue;
                EnemyDefinition enemy;
                var label = config.TryGetEnemy(entry.Id, out enemy) ? enemy.Name : entry.Id;
                SetText(row, "Name", label.ToUpperInvariant());
                SetText(row, "Tier", _definition.Strength >= 7 ? "ELITE" : _definition.Strength >= 4 ? "MID" : "WEAK");
            }
        }

        private int RouteCost(GameConfigSnapshot config, bool food)
        {
            var total = 0;
            foreach (var id in _routeTileIds)
            {
                WorldTileDefinition tile;
                if (config.TryGetWorldTile(id, out tile)) total += food ? tile.FoodCostPerSurvivor : tile.WaterCostPerSurvivor;
            }
            return total * 2;
        }

        private void SetRewardPill(string parentName, string resourceId)
        {
            var reward = _definition.ResourceRewards.FirstOrDefault(r => r.ResourceId == resourceId);
            SetPill(_tooltip, parentName, reward != null ? reward.Min + "–" + reward.Max : "—");
        }

        private static void SetPill(Transform root, string parentName, string value)
        {
            var parent = FindRecursive(root, parentName);
            var label = parent != null ? FindText(parent, "Value") : null;
            if (label != null) label.text = value;
        }

        private static void SetText(Transform root, string name, string value)
        {
            var label = FindText(root, name);
            if (label != null) label.text = value;
        }

        private static TextMeshProUGUI FindText(Transform root, string name)
        {
            var child = FindRecursive(root, name);
            return child != null ? child.GetComponent<TextMeshProUGUI>() : null;
        }

        private static Transform FindRecursive(Transform root, string name)
        {
            if (root == null) return null;
            if (string.Equals(root.name, name, StringComparison.OrdinalIgnoreCase)) return root;
            foreach (Transform child in root)
            {
                var found = FindRecursive(child, name);
                if (found != null) return found;
            }
            return null;
        }

        private static void SetButtonLabel(Button button, string value)
        {
            if (button == null) return;
            var label = button.GetComponentInChildren<TextMeshProUGUI>(true);
            if (label != null) label.text = value;
        }

        private static void SetSelected(RosterSlot slot, bool selected)
        {
            if (slot == null || slot.Button == null || slot.Button.targetGraphic == null) return;
            slot.Button.targetGraphic.color = selected ? new Color32(199, 138, 42, 255) : new Color32(243, 232, 207, 255);
        }

        public bool IsPointerOverUi(Vector2 screen)
        {
            if (_tooltip != null && _tooltip.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(_tooltip, screen)) return true;
            return _roster != null && _roster.gameObject.activeInHierarchy && RectTransformUtility.RectangleContainsScreenPoint(_roster, screen);
        }

        private void UnwireButtons()
        {
            if (_gatherButton != null) _gatherButton.onClick.RemoveListener(OpenRoster);
            if (_sendButton != null) _sendButton.onClick.RemoveListener(SendTeam);
            if (_cancelButton != null) _cancelButton.onClick.RemoveListener(CloseRoster);
        }

        private sealed class RosterSlot
        {
            public GameObject Root;
            public Button Button;
            public TextMeshProUGUI Name;
            public TextMeshProUGUI Stats;
            public string SurvivorId = string.Empty;
        }
    }

    public sealed class WorldTileLaunchViewRequest
    {
        public WorldTileLaunchViewRequest(string tileId, string zoneId, MapCellCoordinate cell, IReadOnlyList<string> routeTileIds, IReadOnlyList<string> survivorIds)
        {
            TileId = tileId;
            ZoneId = zoneId;
            Cell = cell;
            RouteTileIds = new List<string>(routeTileIds ?? Array.Empty<string>());
            SurvivorIds = new List<string>(survivorIds ?? Array.Empty<string>());
        }
        public string TileId { get; }
        public string ZoneId { get; }
        public MapCellCoordinate Cell { get; }
        public List<string> RouteTileIds { get; }
        public List<string> SurvivorIds { get; }
    }
}
