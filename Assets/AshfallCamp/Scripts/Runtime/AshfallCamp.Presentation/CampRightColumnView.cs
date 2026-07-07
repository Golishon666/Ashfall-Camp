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
                UiText.Set(binding.Label, label);
                if (binding.Pin != null)
                {
                    binding.Pin.color = building.IsUnlocked ? catalog.Theme.Teal : catalog.Theme.MutedInk;
                }
            }
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
            [SerializeField] private Image pin;
            [SerializeField] private TextMeshProUGUI label;

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
            public Image Pin { get { return pin; } }
            public TextMeshProUGUI Label { get { return label; } }

            public void SetActive(bool active)
            {
                UiText.SetActive(pin, active);
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
