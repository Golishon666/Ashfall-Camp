using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BuildingCostRowView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI maxCost;
        [SerializeField] private List<CostBinding> costs = new List<CostBinding>();

        public void ConfigureBindings(TextMeshProUGUI maxCostLabel, IEnumerable<CostBinding> costBindings)
        {
            maxCost = maxCostLabel;
            costs.Clear();
            if (costBindings != null)
            {
                costs.AddRange(costBindings);
            }
        }

        public void Render(CampUiCatalogSO catalog, BuildingDefinition definition, BuildingState building)
        {
            if (catalog == null || definition == null || building == null) return;

            var nextLevel = BuildingSystem.GetLevel(definition, building.Level + 1);
            var hasNextCost = nextLevel != null && nextLevel.Cost.Count > 0;
            UiText.SetActive(maxCost, nextLevel == null);
            UiText.Set(maxCost, catalog.MaxCostLabel);

            foreach (var cost in costs)
            {
                var amount = 0;
                var isVisible = hasNextCost && nextLevel.Cost.TryGetValue(cost.ResourceId, out amount);
                cost.SetActive(isVisible);
                if (!isVisible) continue;

                var resource = FindResourceEntry(catalog, cost.ResourceId);
                if (cost.Icon != null && resource != null && resource.Icon != null)
                {
                    cost.Icon.texture = resource.Icon;
                    cost.Icon.color = Color.white;
                }

                UiText.Set(cost.Value, amount.ToString());
            }
        }

        private static ResourceUiEntry FindResourceEntry(CampUiCatalogSO catalog, string resourceId)
        {
            foreach (var entry in catalog.ResourceBar)
            {
                if (string.Equals(entry.Id, resourceId, StringComparison.Ordinal)) return entry;
            }

            return null;
        }

        [Serializable]
        public sealed class CostBinding
        {
            [SerializeField] private string resourceId;
            [SerializeField] private RawImage icon;
            [SerializeField] private TextMeshProUGUI value;

            public CostBinding()
            {
            }

            public CostBinding(string resourceId, RawImage icon, TextMeshProUGUI value)
            {
                this.resourceId = resourceId;
                this.icon = icon;
                this.value = value;
            }

            public string ResourceId { get { return resourceId; } }
            public RawImage Icon { get { return icon; } }
            public TextMeshProUGUI Value { get { return value; } }

            public void SetActive(bool active)
            {
                UiText.SetActive(icon, active);
                UiText.SetActive(value, active);
            }
        }
    }
}
