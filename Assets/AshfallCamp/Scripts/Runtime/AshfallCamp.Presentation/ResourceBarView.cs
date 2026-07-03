using System.Collections.Generic;
using AshfallCamp.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshfallCamp.Presentation
{
    internal sealed class ResourceBarView
    {
        private readonly CampUiCatalogSO _catalog;
        private readonly Dictionary<string, ResourceTile> _tiles = new Dictionary<string, ResourceTile>(System.StringComparer.Ordinal);

        public ResourceBarView(CampUiCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public void Build(VisualElement parent)
        {
            var theme = _catalog.Theme;
            var top = new VisualElement();
            top.style.height = 118;
            top.style.flexDirection = FlexDirection.Row;
            top.style.alignItems = Align.Center;
            top.style.marginBottom = 14;
            parent.Add(top);

            var brand = new VisualElement();
            brand.style.width = 520;
            brand.style.flexDirection = FlexDirection.Row;
            brand.style.alignItems = Align.Center;
            top.Add(brand);

            var banner = new Label(_catalog.BrandBannerText);
            banner.style.width = 88;
            banner.style.height = 98;
            banner.style.backgroundColor = theme.Teal;
            banner.style.color = theme.Paper;
            banner.style.fontSize = 48;
            banner.style.unityFontStyleAndWeight = FontStyle.Bold;
            banner.style.unityTextAlign = TextAnchor.MiddleCenter;
            banner.style.marginRight = 22;
            UiStyle.SetRadius(banner, 3);
            brand.Add(banner);

            var brandText = new VisualElement();
            brandText.style.flexGrow = 1;
            brand.Add(brandText);

            var title = new Label(_catalog.BrandTitle);
            title.style.fontSize = 46;
            title.style.color = theme.Ink;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            title.style.letterSpacing = 0;
            brandText.Add(title);

            var subtitle = new Label(_catalog.BrandSubtitle);
            subtitle.style.fontSize = 14;
            subtitle.style.color = theme.Teal;
            subtitle.style.unityFontStyleAndWeight = FontStyle.Bold;
            subtitle.style.letterSpacing = 0;
            brandText.Add(subtitle);

            var tray = new VisualElement();
            tray.style.flexGrow = 1;
            tray.style.height = 92;
            tray.style.flexDirection = FlexDirection.Row;
            tray.style.alignItems = Align.Center;
            tray.style.justifyContent = Justify.Center;
            tray.style.paddingLeft = 28;
            tray.style.paddingRight = 28;
            tray.style.marginLeft = 18;
            tray.style.backgroundColor = new Color(0.21f, 0.18f, 0.15f, 0.2f);
            UiStyle.TrySetBackground(tray, _catalog.TopResourcePlate);
            top.Add(tray);

            foreach (var entry in _catalog.ResourceBar)
            {
                AddTile(tray, entry);
            }
        }

        public void Render(GameState state, GameConfigSnapshot config)
        {
            foreach (var entry in _catalog.ResourceBar)
            {
                ResourceTile tile;
                if (!_tiles.TryGetValue(entry.Id, out tile)) continue;

                if (entry.UsesSurvivorCapacity)
                {
                    tile.Amount.text = state.Survivors.Count + " / " + Mathf.Max(1, state.SurvivorCap);
                    tile.Rate.text = UiStateQueries.CountIdleSurvivors(state) + " " + _catalog.IdleSuffixLabel;
                    continue;
                }

                int amount;
                state.Resources.TryGetValue(entry.Id, out amount);
                int cap;
                var hasCap = state.ResourceCaps.TryGetValue(entry.Id, out cap);
                tile.Amount.text = hasCap ? amount + "/" + cap : amount.ToString();
                var perHour = UiStateQueries.CalculateProductionPerHour(state, config, entry.Id);
                tile.Rate.text = perHour > 0 ? "+" + perHour + _catalog.PerHourSuffixLabel : string.Empty;
            }
        }

        private void AddTile(VisualElement parent, ResourceUiEntry entry)
        {
            var theme = _catalog.Theme;
            var tile = new VisualElement();
            tile.style.width = entry.UsesSurvivorCapacity ? 150 : 122;
            tile.style.height = 70;
            tile.style.flexDirection = FlexDirection.Row;
            tile.style.alignItems = Align.Center;
            tile.style.paddingLeft = 8;
            tile.style.paddingRight = 8;
            tile.style.marginLeft = 4;
            tile.style.marginRight = 4;
            parent.Add(tile);

            var icon = new VisualElement();
            icon.style.width = 36;
            icon.style.height = 36;
            icon.style.marginRight = 8;
            icon.style.backgroundColor = entry.UsesSurvivorCapacity ? new Color(0.16f, 0.14f, 0.11f, 0.7f) : new Color(1f, 1f, 1f, 0f);
            UiStyle.TrySetBackground(icon, entry.Icon);
            UiStyle.SetRadius(icon, 18);
            tile.Add(icon);

            var textStack = new VisualElement();
            textStack.style.flexGrow = 1;
            tile.Add(textStack);

            var title = new Label(entry.Label);
            title.style.fontSize = 10;
            title.style.color = theme.Ink;
            title.style.unityFontStyleAndWeight = FontStyle.Bold;
            textStack.Add(title);

            var amount = new Label("-");
            amount.style.fontSize = 20;
            amount.style.color = theme.Ink;
            amount.style.unityFontStyleAndWeight = FontStyle.Bold;
            textStack.Add(amount);

            var rate = new Label(string.Empty);
            rate.style.fontSize = 11;
            rate.style.color = theme.Sage;
            textStack.Add(rate);
            _tiles[entry.Id] = new ResourceTile(amount, rate);
        }

        private sealed class ResourceTile
        {
            public readonly Label Amount;
            public readonly Label Rate;

            public ResourceTile(Label amount, Label rate)
            {
                Amount = amount;
                Rate = rate;
            }
        }
    }
}
