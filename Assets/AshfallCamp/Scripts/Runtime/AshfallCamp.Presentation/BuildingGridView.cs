using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshfallCamp.Presentation
{
    internal sealed class BuildingGridView
    {
        private readonly CampUiCatalogSO _catalog;
        private readonly Action<string> _upgradeRequested;
        private readonly Dictionary<string, BuildingCardView> _cards = new Dictionary<string, BuildingCardView>(StringComparer.Ordinal);

        public BuildingGridView(CampUiCatalogSO catalog, Action<string> upgradeRequested)
        {
            _catalog = catalog;
            _upgradeRequested = upgradeRequested;
        }

        public void Build(VisualElement main)
        {
            var center = new VisualElement();
            center.style.flexGrow = 1;
            center.style.flexDirection = FlexDirection.Column;
            main.Add(center);

            BuildHeader(center);

            var grid = new VisualElement();
            grid.style.flexGrow = 1;
            grid.style.flexDirection = FlexDirection.Row;
            grid.style.flexWrap = Wrap.Wrap;
            grid.style.alignContent = Align.FlexStart;
            center.Add(grid);

            for (var i = 0; i < _catalog.Buildings.Count; i++)
            {
                var card = new BuildingCardView(_catalog, _catalog.Buildings[i], i, _upgradeRequested);
                card.Build(grid);
                _cards[_catalog.Buildings[i].BuildingId] = card;
            }

            AddEmptyBuildSlot(grid);
        }

        public void Render(GameState state, GameConfigSnapshot config)
        {
            foreach (var card in _cards.Values)
            {
                card.Render(state, config);
            }
        }

        private void BuildHeader(VisualElement parent)
        {
            var theme = _catalog.Theme;
            var header = new VisualElement();
            header.style.height = 72;
            header.style.flexDirection = FlexDirection.Row;
            header.style.alignItems = Align.Center;
            parent.Add(header);

            var title = new Label(_catalog.BuildingScreenTitle);
            title.style.fontSize = 42;
            title.style.color = theme.Teal;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.marginRight = 28;
            header.Add(title);

            foreach (var filter in _catalog.BuildingFilters)
            {
                AddFilterTab(header, filter);
            }
        }

        private void AddFilterTab(VisualElement parent, FilterUiEntry filter)
        {
            var theme = _catalog.Theme;
            var tab = new Label(filter.Label);
            tab.style.width = 150;
            tab.style.height = 42;
            tab.style.marginRight = 10;
            tab.style.backgroundColor = filter.IsActive ? theme.Teal : new Color(theme.PaperDark.r, theme.PaperDark.g, theme.PaperDark.b, 0.45f);
            tab.style.color = filter.IsActive ? theme.Paper : theme.Ink;
            tab.style.unityTextAlign = TextAnchor.MiddleCenter;
            tab.style.unityFontStyleAndWeight = FontStyle.Bold;
            UiStyle.SetBorder(tab, theme.Line, 1);
            UiStyle.SetRadius(tab, 4);
            parent.Add(tab);
        }

        private void AddEmptyBuildSlot(VisualElement parent)
        {
            var theme = _catalog.Theme;
            var slot = new VisualElement();
            slot.style.width = Length.Percent(32.2f);
            slot.style.height = 292;
            slot.style.backgroundColor = new Color(0.86f, 0.78f, 0.62f, 0.16f);
            slot.style.alignItems = Align.Center;
            slot.style.justifyContent = Justify.Center;
            UiStyle.SetBorder(slot, new Color(0.43f, 0.33f, 0.22f, 0.22f), 1);
            UiStyle.SetRadius(slot, 4);
            parent.Add(slot);

            var plus = new Label("+");
            plus.style.width = 70;
            plus.style.height = 70;
            UiStyle.SetBorder(plus, theme.MutedInk, 2);
            plus.style.color = theme.MutedInk;
            plus.style.fontSize = 44;
            plus.style.unityTextAlign = TextAnchor.MiddleCenter;
            UiStyle.SetRadius(plus, 35);
            slot.Add(plus);

            var text = new Label(_catalog.EmptyBuildingTitle);
            text.style.marginTop = 14;
            text.style.color = theme.Ink;
            text.style.unityFontStyleAndWeight = FontStyle.Bold;
            slot.Add(text);
        }
    }
}
