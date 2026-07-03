using AshfallCamp.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshfallCamp.Presentation
{
    internal sealed class BuildingCostRowView
    {
        private readonly CampUiCatalogSO _catalog;
        private VisualElement _costs;

        public BuildingCostRowView(CampUiCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public void Build(VisualElement card, Button button)
        {
            var theme = _catalog.Theme;
            var costRow = new VisualElement();
            costRow.style.height = 48;
            costRow.style.flexDirection = FlexDirection.Row;
            costRow.style.alignItems = Align.Center;
            costRow.style.marginTop = 12;
            UiStyle.SetBorder(costRow, new Color(0.43f, 0.33f, 0.22f, 0.18f), 1);
            UiStyle.SetRadius(costRow, 3);
            card.Add(costRow);

            var costLabel = new Label(_catalog.UpgradeCostLabel);
            costLabel.style.width = 104;
            costLabel.style.fontSize = 10;
            costLabel.style.color = theme.Ink;
            costLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            costLabel.style.marginLeft = 8;
            costRow.Add(costLabel);

            _costs = new VisualElement();
            _costs.style.flexGrow = 1;
            _costs.style.flexDirection = FlexDirection.Row;
            _costs.style.alignItems = Align.Center;
            costRow.Add(_costs);
            costRow.Add(button);
        }

        public void Render(BuildingDefinition definition, BuildingState building)
        {
            _costs.Clear();
            var next = BuildingSystem.GetLevel(definition, building.Level + 1);
            if (next == null || next.Cost.Count == 0)
            {
                _costs.Add(CostChip(_catalog.MaxCostLabel, null));
                return;
            }

            foreach (var cost in next.Cost)
            {
                _costs.Add(CostChip(cost.Value.ToString(), FindResourceIcon(cost.Key)));
            }
        }

        private VisualElement CostChip(string value, Texture2D icon)
        {
            var theme = _catalog.Theme;
            var chip = new VisualElement();
            chip.style.flexDirection = FlexDirection.Row;
            chip.style.alignItems = Align.Center;
            chip.style.marginRight = 10;

            var visual = new VisualElement();
            visual.style.width = 22;
            visual.style.height = 22;
            visual.style.marginRight = 4;
            visual.style.backgroundColor = new Color(0.16f, 0.14f, 0.11f, 0.35f);
            UiStyle.TrySetBackground(visual, icon);
            UiStyle.SetRadius(visual, 11);
            chip.Add(visual);

            var label = new Label(value);
            label.style.fontSize = 14;
            label.style.color = theme.Ink;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            chip.Add(label);
            return chip;
        }

        private Texture2D FindResourceIcon(string resourceId)
        {
            foreach (var entry in _catalog.ResourceBar)
            {
                if (entry.Id == resourceId) return entry.Icon;
            }

            return null;
        }
    }
}
