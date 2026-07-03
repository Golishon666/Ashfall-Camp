using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using Names = AshfallCamp.Presentation.CampDashboardElementNames;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CampDashboardView : MonoBehaviour, IUiRootView
    {
        [SerializeField] private Transform root;
        [SerializeField] private CampUiCatalogSO catalog;

        private readonly Dictionary<string, ResourceBinding> _resourceBindings = new Dictionary<string, ResourceBinding>(StringComparer.Ordinal);
        private readonly Dictionary<string, BuildingBinding> _buildingBindings = new Dictionary<string, BuildingBinding>(StringComparer.Ordinal);
        private SummaryBinding _summaryBinding;
        private StatusBinding _statusBinding;
        private bool _isBound;
        private bool _missingCatalogLogged;

        public event Action<string> UpgradeRequested;

        public Transform Root
        {
            get { return root != null ? root : transform; }
        }

        private void Awake()
        {
            EnsureBound();
        }

        public void SetCatalog(CampUiCatalogSO uiCatalog)
        {
            catalog = uiCatalog;
            _isBound = false;
            _resourceBindings.Clear();
            _buildingBindings.Clear();
            _summaryBinding = default;
            _statusBinding = default;
        }

        public void Render(GameState state, GameConfigSnapshot config)
        {
            if (state == null || config == null) return;
            if (!EnsureBound()) return;

            RenderResources(state, config);
            RenderStatus(state);
            RenderSummary(state, config);
            RenderBuildings(state, config);
        }

        private bool EnsureBound()
        {
            if (_isBound) return true;
            if (catalog == null)
            {
                if (!_missingCatalogLogged)
                {
                    Debug.LogError("CampDashboardView requires a CampUiCatalogSO reference.", this);
                    _missingCatalogLogged = true;
                }

                return false;
            }

            BindResources();
            BindStatus();
            BindSummary();
            BindBuildings();
            _isBound = true;
            return true;
        }

        private void BindResources()
        {
            _resourceBindings.Clear();
            foreach (var entry in catalog.ResourceBar)
            {
                if (string.IsNullOrWhiteSpace(entry.Id)) continue;
                var resourceRoot = FindDeep(Root, Names.ResourceGroup(entry.Id)) ?? Root;
                var binding = new ResourceBinding(
                    FindComponent<TextMeshProUGUI>(resourceRoot, Names.ResourceLabel(entry.Id)),
                    FindComponent<TextMeshProUGUI>(resourceRoot, Names.ResourceValue(entry.Id)),
                    FindComponent<RawImage>(resourceRoot, Names.ResourceIcon(entry.Id)));

                if (binding.Value == null)
                {
                    Debug.LogWarning("CampDashboardView resource value text is missing for id: " + entry.Id, this);
                }

                _resourceBindings[entry.Id] = binding;
            }
        }

        private void BindStatus()
        {
            _statusBinding = new StatusBinding(
                FindComponent<TextMeshProUGUI>(Root, Names.CampStatusLabel),
                FindComponent<TextMeshProUGUI>(Root, Names.CampStatusBody),
                FindComponent<TextMeshProUGUI>(Root, Names.CampStatusBadgeText));
        }

        private void BindSummary()
        {
            _summaryBinding = new SummaryBinding(
                FindSummaryValue(Names.SummaryPopulation),
                FindSummaryValue(Names.SummaryIdleSurvivors),
                FindSummaryValue(Names.SummaryBuildings),
                FindSummaryValue(Names.SummaryProduction),
                FindSummaryLabel(Names.SummaryPopulation),
                FindSummaryLabel(Names.SummaryIdleSurvivors),
                FindSummaryLabel(Names.SummaryBuildings),
                FindSummaryLabel(Names.SummaryProduction));
        }

        private void BindBuildings()
        {
            _buildingBindings.Clear();
            foreach (var entry in catalog.Buildings)
            {
                if (string.IsNullOrWhiteSpace(entry.BuildingId)) continue;
                var cardRoot = FindDeep(Root, Names.BuildingCard(entry.BuildingId));
                if (cardRoot == null) continue;

                var buttonRoot = FindDeep(cardRoot, Names.BuildingUpgradeButton(entry.BuildingId));
                var button = BindButton(buttonRoot, entry.BuildingId);
                var costRow = FindDeep(cardRoot, Names.BuildingCostRow(entry.BuildingId));
                var binding = new BuildingBinding(
                    cardRoot,
                    FindComponent<TextMeshProUGUI>(cardRoot, Names.BuildingImageLetter(entry.BuildingId)),
                    FindComponent<RawImage>(cardRoot, Names.BuildingIcon(entry.BuildingId)),
                    FindComponent<TextMeshProUGUI>(cardRoot, Names.BuildingName(entry.BuildingId)),
                    FindComponent<TextMeshProUGUI>(cardRoot, Names.BuildingLevel(entry.BuildingId)),
                    FindComponent<TextMeshProUGUI>(cardRoot, Names.BuildingDescription(entry.BuildingId)),
                    FindComponent<TextMeshProUGUI>(cardRoot, Names.BuildingEffect(entry.BuildingId)),
                    costRow,
                    FindComponent<TextMeshProUGUI>(costRow, Names.BuildingCostMax(entry.BuildingId)),
                    button,
                    FindComponent<TextMeshProUGUI>(buttonRoot, Names.BuildingUpgradeLabel(entry.BuildingId)),
                    FindCostBindings(costRow, entry.BuildingId));

                _buildingBindings[entry.BuildingId] = binding;
            }
        }

        private Button BindButton(Transform buttonRoot, string buildingId)
        {
            if (buttonRoot == null) return null;
            var button = buttonRoot.GetComponent<Button>();
            if (button == null)
            {
                button = buttonRoot.gameObject.AddComponent<Button>();
            }

            var image = buttonRoot.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = true;
                button.targetGraphic = image;
            }

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(() => UpgradeRequested?.Invoke(buildingId));
            return button;
        }

        private void RenderResources(GameState state, GameConfigSnapshot config)
        {
            foreach (var entry in catalog.ResourceBar)
            {
                if (string.IsNullOrWhiteSpace(entry.Id)) continue;
                ResourceBinding binding;
                if (!_resourceBindings.TryGetValue(entry.Id, out binding)) continue;

                SetText(binding.Label, entry.Label);
                if (binding.Icon != null && entry.Icon != null)
                {
                    binding.Icon.texture = entry.Icon;
                    binding.Icon.color = Color.white;
                }

                SetText(binding.Value, FormatResourceValue(state, config, entry));
            }
        }

        private void RenderStatus(GameState state)
        {
            var strained = false;
            if (!string.IsNullOrWhiteSpace(catalog.StatusResourceId))
            {
                int amount;
                strained = state.Resources.TryGetValue(catalog.StatusResourceId, out amount) &&
                           amount < catalog.StatusStrainedBelowAmount;
            }

            SetText(_statusBinding.Label, strained ? catalog.CampStatusStrainedLabel : catalog.CampStatusHealthyLabel);
            SetText(_statusBinding.Body, catalog.CampStatusBody);
            SetText(_statusBinding.Badge, catalog.CampStatusBadgeLabel);
        }

        private void RenderSummary(GameState state, GameConfigSnapshot config)
        {
            SetText(_summaryBinding.PopulationLabel, catalog.PopulationLabel);
            SetText(_summaryBinding.IdleSurvivorsLabel, catalog.IdleSurvivorsLabel);
            SetText(_summaryBinding.BuildingsLabel, catalog.BuildingsLabel);
            SetText(_summaryBinding.ProductionLabel, catalog.ProductionMetricLabel);

            SetText(_summaryBinding.PopulationValue, state.Survivors.Count + " / " + Math.Max(1, state.SurvivorCap));
            SetText(_summaryBinding.IdleSurvivorsValue, UiStateQueries.CountIdleSurvivors(state).ToString());
            SetText(_summaryBinding.BuildingsValue, UiStateQueries.CountUnlockedBuildings(state) + " / " + state.Buildings.Count);

            var productionPerHour = string.IsNullOrWhiteSpace(catalog.ProductionMetricResourceId)
                ? 0
                : UiStateQueries.CalculateProductionPerHour(state, config, catalog.ProductionMetricResourceId);
            SetText(_summaryBinding.ProductionValue, "+" + productionPerHour + catalog.PerHourSuffixLabel);
        }

        private void RenderBuildings(GameState state, GameConfigSnapshot config)
        {
            foreach (var entry in catalog.Buildings)
            {
                if (string.IsNullOrWhiteSpace(entry.BuildingId)) continue;

                BuildingBinding binding;
                BuildingDefinition definition;
                BuildingState building;
                var hasBinding = _buildingBindings.TryGetValue(entry.BuildingId, out binding);
                var hasDefinition = config.Buildings.TryGetValue(entry.BuildingId, out definition);
                var hasState = state.Buildings.TryGetValue(entry.BuildingId, out building);
                if (!hasBinding) continue;

                binding.SetActive(hasDefinition && hasState);
                if (!hasDefinition || !hasState) continue;

                SetText(binding.ImageLetter, string.IsNullOrEmpty(definition.Name) ? string.Empty : definition.Name.Substring(0, 1).ToUpperInvariant());
                if (binding.Icon != null && entry.Icon != null)
                {
                    binding.Icon.texture = entry.Icon;
                    binding.Icon.color = Color.white;
                }

                SetText(binding.Name, definition.Name);
                SetText(binding.Level, string.Format(catalog.LevelLabelFormat, building.Level));
                SetText(binding.Description, entry.Description);
                SetText(binding.Effect, BuildingEffectTextFormatter.Format(catalog, definition, building));

                RenderBuildingCost(binding, definition, building);
                RenderUpgradeButton(binding, state, config, definition, building);
            }
        }

        private void RenderBuildingCost(BuildingBinding binding, BuildingDefinition definition, BuildingState building)
        {
            var nextLevel = BuildingSystem.GetLevel(definition, building.Level + 1);
            var hasNextCost = nextLevel != null && nextLevel.Cost.Count > 0;
            SetActive(binding.CostMax, nextLevel == null);
            SetText(binding.CostMax, catalog.MaxCostLabel);

            foreach (var cost in binding.Costs)
            {
                var amount = 0;
                var isVisible = hasNextCost && nextLevel.Cost.TryGetValue(cost.ResourceId, out amount);
                cost.SetActive(isVisible);
                if (!isVisible) continue;

                var resource = FindResourceEntry(cost.ResourceId);
                if (cost.Icon != null && resource != null && resource.Icon != null)
                {
                    cost.Icon.texture = resource.Icon;
                    cost.Icon.color = Color.white;
                }

                SetText(cost.Value, amount.ToString());
            }
        }

        private void RenderUpgradeButton(BuildingBinding binding, GameState state, GameConfigSnapshot config, BuildingDefinition definition, BuildingState building)
        {
            var nextLevel = BuildingSystem.GetLevel(definition, building.Level + 1);
            var label = catalog.UpgradeButtonLabel;
            var interactable = false;

            if (nextLevel == null)
            {
                label = catalog.MaxButtonLabel;
            }
            else if (!building.IsUnlocked)
            {
                label = catalog.LockedButtonLabel;
            }
            else
            {
                var validation = BuildingSystem.ValidateUpgrade(state, config, definition.Id);
                if (validation.IsValid)
                {
                    interactable = true;
                    label = catalog.UpgradeButtonLabel;
                }
                else
                {
                    label = catalog.NeedResourcesButtonLabel;
                }
            }

            SetText(binding.UpgradeLabel, label);
            if (binding.UpgradeButton != null)
            {
                binding.UpgradeButton.interactable = interactable;
            }
        }

        private TextMeshProUGUI FindSummaryValue(string label)
        {
            return FindComponent<TextMeshProUGUI>(Root, Names.SummaryValue(label));
        }

        private TextMeshProUGUI FindSummaryLabel(string label)
        {
            return FindComponent<TextMeshProUGUI>(Root, Names.SummaryLabel(label));
        }

        private List<CostBinding> FindCostBindings(Transform costRow, string buildingId)
        {
            var bindings = new List<CostBinding>();
            if (costRow == null) return bindings;

            var prefix = Names.BuildingCostValuePrefix(buildingId);
            var values = costRow.GetComponentsInChildren<TextMeshProUGUI>(true);
            foreach (var value in values)
            {
                if (!value.name.StartsWith(prefix, StringComparison.Ordinal)) continue;
                var resourceId = value.name.Substring(prefix.Length);
                var icon = FindComponent<RawImage>(costRow, Names.BuildingCostIcon(buildingId, resourceId));
                bindings.Add(new CostBinding(resourceId, icon, value));
            }

            return bindings;
        }

        private ResourceUiEntry FindResourceEntry(string resourceId)
        {
            foreach (var entry in catalog.ResourceBar)
            {
                if (string.Equals(entry.Id, resourceId, StringComparison.Ordinal)) return entry;
            }

            return null;
        }

        private static string FormatResourceValue(GameState state, GameConfigSnapshot config, ResourceUiEntry entry)
        {
            if (entry.UsesSurvivorCapacity)
            {
                return state.Survivors.Count + " / " + Math.Max(1, state.SurvivorCap);
            }

            int amount;
            state.Resources.TryGetValue(entry.Id, out amount);

            ResourceDefinition definition;
            if (config.Resources.TryGetValue(entry.Id, out definition) && definition.HasCap)
            {
                int cap;
                if (!state.ResourceCaps.TryGetValue(entry.Id, out cap))
                {
                    cap = definition.StartCap;
                }

                return amount + " / " + cap;
            }

            return amount.ToString();
        }

        private static void SetText(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        private static void SetActive(Component target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }

        private static T FindComponent<T>(Transform parent, string objectName) where T : Component
        {
            var child = FindDeep(parent, objectName);
            return child != null ? child.GetComponent<T>() : null;
        }

        private static Transform FindDeep(Transform parent, string objectName)
        {
            if (parent == null) return null;
            if (parent.name == objectName) return parent;

            for (var i = 0; i < parent.childCount; i++)
            {
                var found = FindDeep(parent.GetChild(i), objectName);
                if (found != null) return found;
            }

            return null;
        }

        private readonly struct ResourceBinding
        {
            public readonly TextMeshProUGUI Label;
            public readonly TextMeshProUGUI Value;
            public readonly RawImage Icon;

            public ResourceBinding(TextMeshProUGUI label, TextMeshProUGUI value, RawImage icon)
            {
                Label = label;
                Value = value;
                Icon = icon;
            }
        }

        private readonly struct StatusBinding
        {
            public readonly TextMeshProUGUI Label;
            public readonly TextMeshProUGUI Body;
            public readonly TextMeshProUGUI Badge;

            public StatusBinding(TextMeshProUGUI label, TextMeshProUGUI body, TextMeshProUGUI badge)
            {
                Label = label;
                Body = body;
                Badge = badge;
            }
        }

        private readonly struct SummaryBinding
        {
            public readonly TextMeshProUGUI PopulationValue;
            public readonly TextMeshProUGUI IdleSurvivorsValue;
            public readonly TextMeshProUGUI BuildingsValue;
            public readonly TextMeshProUGUI ProductionValue;
            public readonly TextMeshProUGUI PopulationLabel;
            public readonly TextMeshProUGUI IdleSurvivorsLabel;
            public readonly TextMeshProUGUI BuildingsLabel;
            public readonly TextMeshProUGUI ProductionLabel;

            public SummaryBinding(
                TextMeshProUGUI populationValue,
                TextMeshProUGUI idleSurvivorsValue,
                TextMeshProUGUI buildingsValue,
                TextMeshProUGUI productionValue,
                TextMeshProUGUI populationLabel,
                TextMeshProUGUI idleSurvivorsLabel,
                TextMeshProUGUI buildingsLabel,
                TextMeshProUGUI productionLabel)
            {
                PopulationValue = populationValue;
                IdleSurvivorsValue = idleSurvivorsValue;
                BuildingsValue = buildingsValue;
                ProductionValue = productionValue;
                PopulationLabel = populationLabel;
                IdleSurvivorsLabel = idleSurvivorsLabel;
                BuildingsLabel = buildingsLabel;
                ProductionLabel = productionLabel;
            }
        }

        private sealed class BuildingBinding
        {
            public readonly Transform Root;
            public readonly TextMeshProUGUI ImageLetter;
            public readonly RawImage Icon;
            public readonly TextMeshProUGUI Name;
            public readonly TextMeshProUGUI Level;
            public readonly TextMeshProUGUI Description;
            public readonly TextMeshProUGUI Effect;
            public readonly Transform CostRow;
            public readonly TextMeshProUGUI CostMax;
            public readonly Button UpgradeButton;
            public readonly TextMeshProUGUI UpgradeLabel;
            public readonly List<CostBinding> Costs;

            public BuildingBinding(
                Transform root,
                TextMeshProUGUI imageLetter,
                RawImage icon,
                TextMeshProUGUI name,
                TextMeshProUGUI level,
                TextMeshProUGUI description,
                TextMeshProUGUI effect,
                Transform costRow,
                TextMeshProUGUI costMax,
                Button upgradeButton,
                TextMeshProUGUI upgradeLabel,
                List<CostBinding> costs)
            {
                Root = root;
                ImageLetter = imageLetter;
                Icon = icon;
                Name = name;
                Level = level;
                Description = description;
                Effect = effect;
                CostRow = costRow;
                CostMax = costMax;
                UpgradeButton = upgradeButton;
                UpgradeLabel = upgradeLabel;
                Costs = costs;
            }

            public void SetActive(bool active)
            {
                if (Root != null)
                {
                    Root.gameObject.SetActive(active);
                }
            }
        }

        private readonly struct CostBinding
        {
            public readonly string ResourceId;
            public readonly RawImage Icon;
            public readonly TextMeshProUGUI Value;

            public CostBinding(string resourceId, RawImage icon, TextMeshProUGUI value)
            {
                ResourceId = resourceId;
                Icon = icon;
                Value = value;
            }

            public void SetActive(bool active)
            {
                if (Icon != null)
                {
                    Icon.gameObject.SetActive(active);
                }

                if (Value != null)
                {
                    Value.gameObject.SetActive(active);
                }
            }
        }
    }
}
