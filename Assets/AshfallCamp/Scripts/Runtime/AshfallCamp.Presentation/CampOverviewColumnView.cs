using System.Collections.Generic;
using AshfallCamp.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshfallCamp.Presentation
{
    internal sealed class CampOverviewColumnView
    {
        private readonly CampUiCatalogSO _catalog;
        private readonly Dictionary<string, BuildingPin> _pins = new Dictionary<string, BuildingPin>(System.StringComparer.Ordinal);

        public CampOverviewColumnView(CampUiCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public void Build(VisualElement main)
        {
            var parent = new VisualElement();
            parent.style.width = 340;
            parent.style.marginLeft = 18;
            main.Add(parent);

            BuildOverview(parent);
            BuildExpeditions(parent);
            BuildRadio(parent);
        }

        public void Render(GameState state, GameConfigSnapshot config)
        {
            foreach (var entry in _catalog.Buildings)
            {
                BuildingPin pin;
                if (!_pins.TryGetValue(entry.BuildingId, out pin)) continue;
                BuildingState building;
                if (!state.Buildings.TryGetValue(entry.BuildingId, out building)) continue;
                BuildingDefinition definition;
                if (config.Buildings.TryGetValue(entry.BuildingId, out definition))
                {
                    pin.Name.text = definition.Name;
                }
                pin.Level.text = string.Format(_catalog.LevelLabelFormat, building.Level);
                pin.Dot.style.backgroundColor = building.IsUnlocked ? _catalog.Theme.Teal : _catalog.Theme.MutedInk;
            }
        }

        private void BuildOverview(VisualElement parent)
        {
            var theme = _catalog.Theme;
            var overview = UiStyle.Panel(_catalog.CampOverviewTitle, theme);
            overview.style.height = 380;
            parent.Add(overview);

            var scene = new VisualElement();
            scene.style.flexGrow = 1;
            scene.style.marginTop = 8;
            scene.style.backgroundColor = new Color(0.66f, 0.76f, 0.72f, 0.25f);
            scene.style.overflow = Overflow.Hidden;
            UiStyle.SetBorder(scene, new Color(0.32f, 0.25f, 0.17f, 0.22f), 1);
            UiStyle.SetRadius(scene, 4);
            overview.Add(scene);

            var horizon = new VisualElement();
            horizon.style.position = Position.Absolute;
            horizon.style.left = 0;
            horizon.style.right = 0;
            horizon.style.top = Length.Percent(46);
            horizon.style.height = 2;
            horizon.style.backgroundColor = new Color(0.25f, 0.36f, 0.38f, 0.25f);
            scene.Add(horizon);

            foreach (var entry in _catalog.Buildings)
            {
                AddBuildingPin(scene, entry);
            }
        }

        private void BuildExpeditions(VisualElement parent)
        {
            var expeditions = UiStyle.Panel(_catalog.ActiveExpeditionsTitle, _catalog.Theme);
            expeditions.style.height = 236;
            expeditions.style.marginTop = 14;
            parent.Add(expeditions);

            foreach (var entry in _catalog.ExpeditionCards)
            {
                AddExpeditionCard(expeditions, entry);
            }
        }

        private void BuildRadio(VisualElement parent)
        {
            var theme = _catalog.Theme;
            var radio = UiStyle.Panel(_catalog.RadioIntelTitle, theme);
            radio.style.flexGrow = 1;
            radio.style.marginTop = 14;
            parent.Add(radio);

            var message = new Label(_catalog.RadioIntelBody);
            message.style.whiteSpace = WhiteSpace.Normal;
            message.style.color = theme.MutedInk;
            message.style.fontSize = 14;
            message.style.marginTop = 8;
            radio.Add(message);
            radio.Add(UiStyle.CommandButton(_catalog.RadioIntelButton, false, null, theme));
        }

        private void AddBuildingPin(VisualElement scene, BuildingUiEntry entry)
        {
            var theme = _catalog.Theme;
            var pin = new VisualElement();
            pin.style.position = Position.Absolute;
            pin.style.left = Length.Percent(entry.OverviewLeftPercent);
            pin.style.top = Length.Percent(entry.OverviewTopPercent);
            pin.style.width = 138;
            pin.style.minHeight = 58;
            pin.style.flexDirection = FlexDirection.Row;
            pin.style.alignItems = Align.Center;
            pin.style.paddingLeft = 8;
            pin.style.paddingRight = 8;
            pin.style.paddingTop = 6;
            pin.style.paddingBottom = 6;
            pin.style.backgroundColor = new Color(theme.Paper.r, theme.Paper.g, theme.Paper.b, 0.92f);
            UiStyle.SetBorder(pin, theme.Line, 1);
            UiStyle.SetRadius(pin, 4);
            scene.Add(pin);

            var dot = new VisualElement();
            dot.style.width = 28;
            dot.style.height = 28;
            dot.style.marginRight = 8;
            dot.style.backgroundColor = theme.Teal;
            UiStyle.TrySetBackground(dot, entry.Icon);
            UiStyle.SetRadius(dot, 14);
            pin.Add(dot);

            var stack = new VisualElement();
            stack.style.flexGrow = 1;
            pin.Add(stack);

            var name = new Label(string.Empty);
            name.style.fontSize = 11;
            name.style.color = theme.Ink;
            name.style.unityFontStyleAndWeight = FontStyle.Bold;
            stack.Add(name);

            var level = new Label(string.Empty);
            level.style.fontSize = 11;
            level.style.color = theme.MutedInk;
            stack.Add(level);
            _pins[entry.BuildingId] = new BuildingPin(name, level, dot);
        }

        private void AddExpeditionCard(VisualElement parent, ExpeditionUiEntry entry)
        {
            var theme = _catalog.Theme;
            var card = new VisualElement();
            card.style.height = 78;
            card.style.marginTop = 8;
            card.style.flexDirection = FlexDirection.Row;
            card.style.alignItems = Align.Center;
            card.style.backgroundColor = new Color(1f, 1f, 1f, 0.18f);
            UiStyle.SetBorder(card, theme.Line, 1);
            UiStyle.SetRadius(card, 4);
            parent.Add(card);

            var image = new VisualElement();
            image.style.width = 84;
            image.style.height = 60;
            image.style.marginLeft = 8;
            image.style.marginRight = 10;
            image.style.backgroundColor = new Color(0.42f, 0.51f, 0.48f, 0.25f);
            UiStyle.SetRadius(image, 3);
            card.Add(image);

            var stack = new VisualElement();
            stack.style.flexGrow = 1;
            card.Add(stack);

            var label = new Label(entry.Title);
            label.style.fontSize = 14;
            label.style.color = theme.Ink;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            stack.Add(label);

            var sub = new Label(entry.Subtitle);
            sub.style.fontSize = 12;
            sub.style.color = theme.MutedInk;
            stack.Add(sub);

            var state = new Label(entry.Status);
            state.style.width = 82;
            state.style.height = 26;
            state.style.marginRight = 8;
            state.style.backgroundColor = new Color(theme.Sage.r, theme.Sage.g, theme.Sage.b, 0.22f);
            state.style.color = theme.Sage;
            state.style.unityTextAlign = TextAnchor.MiddleCenter;
            state.style.unityFontStyleAndWeight = FontStyle.Bold;
            UiStyle.SetRadius(state, 2);
            card.Add(state);
        }

        private sealed class BuildingPin
        {
            public readonly Label Name;
            public readonly Label Level;
            public readonly VisualElement Dot;

            public BuildingPin(Label name, Label level, VisualElement dot)
            {
                Name = name;
                Level = level;
                Dot = dot;
            }
        }
    }
}
