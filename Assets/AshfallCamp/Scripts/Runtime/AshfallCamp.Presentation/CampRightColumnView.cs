using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CampRightColumnView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI overviewTitle;
        [SerializeField] private TextMeshProUGUI activeExpeditionsTitle;
        [SerializeField] private TextMeshProUGUI radioTitle;
        [SerializeField] private TextMeshProUGUI radioBody;
        [SerializeField] private Button radioButton;
        [SerializeField] private TextMeshProUGUI radioButtonLabel;
        [SerializeField] private List<BuildingPinBinding> buildingPins = new List<BuildingPinBinding>();
        [SerializeField] private List<ExpeditionCardBinding> expeditionCards = new List<ExpeditionCardBinding>();

        private readonly Dictionary<string, BuildingPinBinding> _pinLookup = new Dictionary<string, BuildingPinBinding>(StringComparer.Ordinal);
        private bool _pinLookupDirty = true;
        private Action<ExpeditionLaunchViewRequest> _launchRequested;
        private Action _broadcastRequested;
        private bool _radioButtonWired;

        private void OnDestroy()
        {
            ClearBroadcastHandler();
            foreach (var card in expeditionCards)
            {
                if (card != null) card.ClearLaunchHandler();
            }
        }

        public void ConfigureBindings(
            TextMeshProUGUI overviewTitleLabel,
            TextMeshProUGUI activeExpeditionsTitleLabel,
            TextMeshProUGUI radioTitleLabel,
            TextMeshProUGUI radioBodyLabel,
            Button radioActionButton,
            TextMeshProUGUI radioButtonText,
            IEnumerable<BuildingPinBinding> pins,
            IEnumerable<ExpeditionCardBinding> expeditions)
        {
            overviewTitle = overviewTitleLabel;
            activeExpeditionsTitle = activeExpeditionsTitleLabel;
            radioTitle = radioTitleLabel;
            radioBody = radioBodyLabel;
            radioButton = radioActionButton;
            radioButtonLabel = radioButtonText;

            buildingPins.Clear();
            if (pins != null)
            {
                buildingPins.AddRange(pins);
            }

            expeditionCards.Clear();
            if (expeditions != null)
            {
                expeditionCards.AddRange(expeditions);
            }

            _pinLookupDirty = true;
            ApplyLaunchHandler();
            WireRadioButton();
        }

        public void SetExpeditionLaunchHandler(Action<ExpeditionLaunchViewRequest> launchRequested)
        {
            _launchRequested = launchRequested;
            ApplyLaunchHandler();
        }

        public void SetBroadcastHandler(Action broadcastRequested)
        {
            _broadcastRequested = broadcastRequested;
            WireRadioButton();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            UiText.Set(overviewTitle, catalog.CampOverviewTitle);
            UiText.Set(activeExpeditionsTitle, catalog.ActiveExpeditionsTitle);
            UiText.Set(radioTitle, catalog.RadioIntelTitle);
            UiText.Set(radioBody, CampDashboardTextFormatter.FormatRadioBody(state, config, catalog));
            UiText.Set(radioButtonLabel, catalog.RadioIntelButton);
            if (radioButton != null)
            {
                radioButton.interactable = RecruitmentSystem.ValidateBroadcast(state, config).IsValid;
            }

            RenderBuildingPins(state, config, catalog);
            RenderExpeditionCards(state, config, catalog);
        }

        private void RenderBuildingPins(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            EnsurePinLookup();
            foreach (var entry in catalog.Buildings)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.BuildingId)) continue;
                BuildingPinBinding binding;
                if (!_pinLookup.TryGetValue(entry.BuildingId, out binding)) continue;

                BuildingDefinition definition;
                BuildingState building;
                var hasDefinition = config.TryGetBuilding(entry.BuildingId, out definition);
                var hasState = state.Buildings.TryGetValue(entry.BuildingId, out building);
                binding.SetActive(hasDefinition && hasState);
                if (!hasDefinition || !hasState) continue;

                var label = string.IsNullOrEmpty(definition.Name) ? string.Empty : definition.Name.Substring(0, 1).ToUpperInvariant();
                binding.ResolveReferences();
                UiText.Set(binding.Label, label);
                UiText.Set(binding.NameLabel, definition.Name);
                UiText.Set(binding.LevelLabel, FormatLevel(catalog, building.Level));
                UiText.Set(binding.StatusLabel, ResolveBuildingStatus(state, config, catalog, definition, building));
                if (binding.Pin != null)
                {
                    binding.Pin.color = building.IsUnlocked ? catalog.Theme.Teal : catalog.Theme.MutedInk;
                }

                if (binding.Icon != null && entry.Icon != null)
                {
                    binding.Icon.texture = entry.Icon;
                    binding.Icon.color = Color.white;
                }
            }
        }

        private static string ResolveBuildingStatus(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog, BuildingDefinition definition, BuildingState building)
        {
            if (BuildingSystem.IsUpgradeActive(building))
            {
                var timer = FormatTimer(BuildingSystem.GetRemainingUpgradeSeconds(building, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()));
                return building.Level <= 0
                    ? Format(Fallback(catalog.BuildingStatusBuildingFormat, "BUILDING {0}"), timer)
                    : Format(Fallback(catalog.BuildingStatusUpgradingFormat, "UPGRADING {0}"), timer);
            }

            if (building.Level <= 0)
            {
                return Fallback(catalog.BuildingStatusNotBuiltLabel, "NOT BUILT");
            }

            if (BuildingSystem.GetLevel(definition, building.Level + 1) == null)
            {
                return Fallback(catalog.BuildingStatusMaxLabel, "MAX LEVEL");
            }

            if (string.Equals(definition.Id, GameIds.Buildings.Barracks, StringComparison.Ordinal))
            {
                return string.Format("{0} / {1}", state.Survivors.Count, state.SurvivorCap);
            }

            if (string.Equals(definition.Id, GameIds.Buildings.WaterCollector, StringComparison.Ordinal) ||
                string.Equals(definition.Id, GameIds.Buildings.MushroomBeds, StringComparison.Ordinal))
            {
                var level = BuildingSystem.GetLevel(definition, building.Level);
                var perHour = level != null ? Math.Max(0, level.ResourcePerMinute * 60) : 0;
                return Format(Fallback(catalog.BuildingStatusProductionFormat, "+{0}/h"), perHour);
            }

            if (string.Equals(definition.Id, GameIds.Buildings.Infirmary, StringComparison.Ordinal))
            {
                var wounded = CountWounded(state);
                return wounded > 0
                    ? Format(Fallback(catalog.BuildingStatusWoundedFormat, "{0} Wounded"), wounded)
                    : Fallback(catalog.BuildingStatusNoWoundedLabel, "No Wounded");
            }

            if (string.Equals(definition.Id, GameIds.Buildings.Workshop, StringComparison.Ordinal))
            {
                return Fallback(catalog.BuildingStatusRepairReadyLabel, "Repair Ready");
            }

            if (string.Equals(definition.Id, GameIds.Buildings.RadioTower, StringComparison.Ordinal))
            {
                return RecruitmentSystem.ValidateBroadcast(state, config).IsValid
                    ? Fallback(catalog.BuildingStatusBroadcastReadyLabel, "Broadcast Ready")
                    : Fallback(catalog.BuildingStatusBroadcastBlockedLabel, "Broadcast Blocked");
            }

            return string.Empty;
        }

        private static int CountWounded(GameState state)
        {
            var count = 0;
            foreach (var survivor in state.Survivors)
            {
                if (survivor.State == SurvivorActivityState.Wounded) count++;
            }

            return count;
        }

        private static string FormatLevel(CampUiCatalogSO catalog, int level)
        {
            var format = string.IsNullOrWhiteSpace(catalog.LevelLabelFormat) ? "Level {0}" : catalog.LevelLabelFormat;
            return string.Format(format, level);
        }

        private static string FormatTimer(double seconds)
        {
            var whole = Math.Max(0, (int)Math.Ceiling(seconds));
            return string.Format("{0:00}:{1:00}", whole / 60, whole % 60);
        }

        private static string Format(string format, object value)
        {
            return string.Format(format, value);
        }

        private static string Fallback(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
        }

        private void RenderExpeditionCards(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var cards = CampDashboardTextFormatter.BuildExpeditionCards(state, config, catalog);
            for (var i = 0; i < expeditionCards.Count; i++)
            {
                var binding = expeditionCards[i];
                var usesDynamicEntries = cards.Count > 0;
                var hasEntry = usesDynamicEntries ? i < cards.Count : i < catalog.ExpeditionCards.Count;
                binding.SetActive(hasEntry);
                if (!hasEntry) continue;

                UiText.Set(binding.Title, usesDynamicEntries ? cards[i].Title : catalog.ExpeditionCards[i].Title);
                UiText.Set(binding.Subtitle, usesDynamicEntries ? cards[i].Subtitle : catalog.ExpeditionCards[i].Subtitle);
                UiText.Set(binding.Status, usesDynamicEntries ? cards[i].Status : catalog.ExpeditionCards[i].Status);
                if (usesDynamicEntries)
                {
                    binding.ConfigureLaunch(cards[i].ZoneId, cards[i].PolicyId, cards[i].CanLaunch);
                }
                else
                {
                    binding.ConfigureLaunch(string.Empty, string.Empty, false);
                }
            }
        }

        private void EnsurePinLookup()
        {
            if (!_pinLookupDirty) return;
            _pinLookup.Clear();
            foreach (var binding in buildingPins)
            {
                if (binding == null || string.IsNullOrWhiteSpace(binding.BuildingId)) continue;
                _pinLookup[binding.BuildingId] = binding;
            }

            _pinLookupDirty = false;
        }

        private void ApplyLaunchHandler()
        {
            foreach (var card in expeditionCards)
            {
                if (card != null)
                {
                    card.SetLaunchHandler(_launchRequested);
                }
            }
        }

        private void WireRadioButton()
        {
            if (_radioButtonWired || radioButton == null) return;
            radioButton.onClick.RemoveListener(OnRadioClicked);
            radioButton.onClick.AddListener(OnRadioClicked);
            _radioButtonWired = true;
        }

        private void ClearBroadcastHandler()
        {
            if (radioButton != null)
            {
                radioButton.onClick.RemoveListener(OnRadioClicked);
            }

            _radioButtonWired = false;
            _broadcastRequested = null;
        }

        private void OnRadioClicked()
        {
            _broadcastRequested?.Invoke();
        }

        [Serializable]
        public sealed class BuildingPinBinding
        {
            [SerializeField] private string buildingId;
            [SerializeField] private Graphic panel;
            [SerializeField] private RawImage icon;
            [SerializeField] private Image pin;
            [SerializeField] private TextMeshProUGUI label;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI levelLabel;
            [SerializeField] private TextMeshProUGUI statusLabel;

            private Transform _resolvedRoot;

            public BuildingPinBinding()
            {
            }

            public BuildingPinBinding(string buildingId, Image pin, TextMeshProUGUI label)
            {
                this.buildingId = buildingId;
                this.pin = pin;
                this.label = label;
            }

            public string BuildingId { get { return buildingId; } }
            public Graphic Panel { get { return panel; } }
            public RawImage Icon { get { return icon; } }
            public Image Pin { get { return pin; } }
            public TextMeshProUGUI Label { get { return label; } }
            public TextMeshProUGUI NameLabel { get { return nameLabel; } }
            public TextMeshProUGUI LevelLabel { get { return levelLabel; } }
            public TextMeshProUGUI StatusLabel { get { return statusLabel; } }

            public void SetActive(bool active)
            {
                ResolveReferences();
                if (panel != null)
                {
                    panel.gameObject.SetActive(active);
                }
                else
                {
                    UiText.SetActive(pin, active);
                    UiText.SetActive(label, active);
                    UiText.SetActive(nameLabel, active);
                    UiText.SetActive(levelLabel, active);
                    UiText.SetActive(statusLabel, active);
                }
            }

            public void ResolveReferences()
            {
                var root = ResolveRoot();
                if (root == null) return;

                if (panel == null) panel = root.GetComponent<Graphic>();
                if (icon == null) icon = FindRawImage(root);

                var texts = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                if (nameLabel == null) nameLabel = FindText(texts, "Name", 0);
                if (levelLabel == null) levelLabel = FindText(texts, "Level", 1);
                if (statusLabel == null) statusLabel = FindText(texts, "Status", 2) ?? FindText(texts, "Description", 2);
                if (label == null) label = FindText(texts, "PinRuntime", -1);
            }

            private Transform ResolveRoot()
            {
                if (_resolvedRoot != null) return _resolvedRoot;
                var current = label != null ? label.transform : pin != null ? pin.transform : null;
                while (current != null)
                {
                    if (current.gameObject.name.StartsWith("Callout_", StringComparison.Ordinal))
                    {
                        _resolvedRoot = current;
                        return _resolvedRoot;
                    }

                    current = current.parent;
                }

                _resolvedRoot = label != null ? label.transform.parent : pin != null ? pin.transform.parent : null;
                return _resolvedRoot;
            }

            private static RawImage FindRawImage(Transform root)
            {
                var images = root.GetComponentsInChildren<RawImage>(true);
                return images.Length > 0 ? images[0] : null;
            }

            private static TextMeshProUGUI FindText(TextMeshProUGUI[] texts, string token, int fallbackIndex)
            {
                for (var i = 0; i < texts.Length; i++)
                {
                    if (texts[i] == null) continue;
                    if (texts[i].gameObject.name.IndexOf(token, StringComparison.OrdinalIgnoreCase) >= 0)
                    {
                        return texts[i];
                    }
                }

                if (fallbackIndex < 0) return null;
                var visibleIndex = 0;
                for (var i = 0; i < texts.Length; i++)
                {
                    if (texts[i] == null) continue;
                    if (texts[i].gameObject.name.IndexOf("PinRuntime", StringComparison.OrdinalIgnoreCase) >= 0) continue;
                    if (visibleIndex == fallbackIndex) return texts[i];
                    visibleIndex++;
                }

                return null;
            }
        }

        [Serializable]
        public sealed class ExpeditionCardBinding
        {
            [SerializeField] private Graphic panel;
            [SerializeField] private Button button;
            [SerializeField] private TextMeshProUGUI title;
            [SerializeField] private TextMeshProUGUI subtitle;
            [SerializeField] private TextMeshProUGUI status;

            private Action<ExpeditionLaunchViewRequest> _launchRequested;
            private string _zoneId = string.Empty;
            private string _policyId = string.Empty;
            private bool _buttonWired;

            public ExpeditionCardBinding()
            {
            }

            public ExpeditionCardBinding(Graphic panel, Button button, TextMeshProUGUI title, TextMeshProUGUI subtitle, TextMeshProUGUI status)
            {
                this.panel = panel;
                this.button = button;
                this.title = title;
                this.subtitle = subtitle;
                this.status = status;
                WireButton();
            }

            public Graphic Panel { get { return panel; } }
            public Button Button { get { return button; } }
            public TextMeshProUGUI Title { get { return title; } }
            public TextMeshProUGUI Subtitle { get { return subtitle; } }
            public TextMeshProUGUI Status { get { return status; } }

            public void SetLaunchHandler(Action<ExpeditionLaunchViewRequest> launchRequested)
            {
                _launchRequested = launchRequested;
                WireButton();
            }

            public void ClearLaunchHandler()
            {
                if (button != null)
                {
                    button.onClick.RemoveListener(OnClicked);
                }

                _buttonWired = false;
                _launchRequested = null;
            }

            public void ConfigureLaunch(string zoneId, string policyId, bool canLaunch)
            {
                _zoneId = zoneId ?? string.Empty;
                _policyId = policyId ?? string.Empty;
                if (button != null)
                {
                    button.interactable = canLaunch;
                }
            }

            public void SetActive(bool active)
            {
                UiText.SetActive(panel, active);
            }

            private void WireButton()
            {
                if (_buttonWired || button == null) return;
                button.onClick.RemoveListener(OnClicked);
                button.onClick.AddListener(OnClicked);
                _buttonWired = true;
            }

            private void OnClicked()
            {
                if (!string.IsNullOrWhiteSpace(_zoneId))
                {
                    _launchRequested?.Invoke(new ExpeditionLaunchViewRequest(_zoneId, _policyId));
                }
            }
        }
    }
}
