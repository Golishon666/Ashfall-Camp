using System;
using AshfallCamp.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshfallCamp.Presentation
{
    internal sealed class BuildingCardView
    {
        private readonly CampUiCatalogSO _catalog;
        private readonly BuildingUiEntry _entry;
        private readonly int _index;
        private readonly Action<string> _upgradeRequested;
        private readonly BuildingCostRowView _costRow;

        private VisualElement _root;
        private VisualElement _image;
        private Label _imageLabel;
        private VisualElement _icon;
        private Label _nameLabel;
        private Label _levelLabel;
        private Label _descriptionLabel;
        private Label _effectLabel;
        private Button _button;

        public BuildingCardView(CampUiCatalogSO catalog, BuildingUiEntry entry, int index, Action<string> upgradeRequested)
        {
            _catalog = catalog;
            _entry = entry;
            _index = index;
            _upgradeRequested = upgradeRequested;
            _costRow = new BuildingCostRowView(catalog);
        }

        public void Build(VisualElement parent)
        {
            var theme = _catalog.Theme;
            _root = UiStyle.Panel(string.Empty, theme);
            _root.style.width = Length.Percent(32.2f);
            _root.style.height = 292;
            _root.style.marginRight = Length.Percent(_index % 3 == 2 ? 0 : 1.6f);
            _root.style.marginBottom = 16;
            parent.Add(_root);

            var body = new VisualElement();
            body.style.flexGrow = 1;
            body.style.flexDirection = FlexDirection.Row;
            _root.Add(body);

            BuildImage(body, theme);
            BuildInfo(body, theme);

            _button = UiStyle.CommandButton(_catalog.UpgradeButtonLabel, true, () => _upgradeRequested(_entry.BuildingId), theme);
            _button.style.width = 126;
            _button.style.marginRight = 6;
            _costRow.Build(_root, _button);
        }

        public void Render(GameState state, GameConfigSnapshot config)
        {
            BuildingDefinition definition;
            BuildingState building;
            if (!config.Buildings.TryGetValue(_entry.BuildingId, out definition) || !state.Buildings.TryGetValue(_entry.BuildingId, out building))
            {
                _root.style.display = DisplayStyle.None;
                return;
            }

            _root.style.display = DisplayStyle.Flex;
            _nameLabel.text = definition.Name;
            _levelLabel.text = string.Format(_catalog.LevelLabelFormat, building.Level);
            _descriptionLabel.text = _entry.Description;
            _effectLabel.text = BuildingEffectTextFormatter.Format(_catalog, definition, building);
            ApplyVisuals(definition);
            _costRow.Render(definition, building);
            UpdateUpgradeButton(state, config, definition, building);
        }

        private void BuildImage(VisualElement body, CampUiTheme theme)
        {
            _image = new VisualElement();
            _image.style.width = Length.Percent(44);
            _image.style.height = 150;
            _image.style.backgroundColor = new Color(0.52f, 0.62f, 0.58f, 0.22f);
            _image.style.marginRight = 14;
            _image.style.overflow = Overflow.Hidden;
            UiStyle.SetBorder(_image, theme.Line, 1);
            UiStyle.SetRadius(_image, 3);
            body.Add(_image);

            _imageLabel = new Label(string.Empty);
            _imageLabel.style.flexGrow = 1;
            _imageLabel.style.fontSize = 54;
            _imageLabel.style.color = new Color(0.18f, 0.38f, 0.4f, 0.32f);
            _imageLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _imageLabel.style.unityTextAlign = TextAnchor.MiddleCenter;
            _image.Add(_imageLabel);
        }

        private void BuildInfo(VisualElement body, CampUiTheme theme)
        {
            var info = new VisualElement();
            info.style.flexGrow = 1;
            body.Add(info);

            var row = new VisualElement();
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            info.Add(row);

            _icon = new VisualElement();
            _icon.style.width = 34;
            _icon.style.height = 34;
            _icon.style.backgroundColor = theme.Teal;
            _icon.style.marginRight = 10;
            UiStyle.SetRadius(_icon, 3);
            row.Add(_icon);

            var titleStack = new VisualElement();
            titleStack.style.flexGrow = 1;
            row.Add(titleStack);

            _nameLabel = new Label(string.Empty);
            _nameLabel.style.fontSize = 20;
            _nameLabel.style.color = theme.Teal;
            _nameLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            titleStack.Add(_nameLabel);

            _levelLabel = new Label(string.Empty);
            _levelLabel.style.fontSize = 14;
            _levelLabel.style.color = theme.Ink;
            titleStack.Add(_levelLabel);

            _descriptionLabel = new Label(string.Empty);
            _descriptionLabel.style.fontSize = 13;
            _descriptionLabel.style.whiteSpace = WhiteSpace.Normal;
            _descriptionLabel.style.color = theme.Ink;
            _descriptionLabel.style.marginTop = 10;
            info.Add(_descriptionLabel);

            _effectLabel = new Label(string.Empty);
            _effectLabel.style.fontSize = 12;
            _effectLabel.style.whiteSpace = WhiteSpace.Normal;
            _effectLabel.style.color = theme.MutedInk;
            _effectLabel.style.marginTop = 8;
            info.Add(_effectLabel);
        }

        private void ApplyVisuals(BuildingDefinition definition)
        {
            _imageLabel.text = string.IsNullOrEmpty(definition.Name) ? string.Empty : definition.Name.Substring(0, 1).ToUpperInvariant();
            if (_entry.Image != null)
            {
                UiStyle.TrySetBackground(_image, _entry.Image);
                _imageLabel.style.display = DisplayStyle.None;
            }
            else
            {
                _imageLabel.style.display = DisplayStyle.Flex;
            }

            if (_entry.Icon != null)
            {
                UiStyle.TrySetBackground(_icon, _entry.Icon);
            }
        }

        private void UpdateUpgradeButton(GameState state, GameConfigSnapshot config, BuildingDefinition definition, BuildingState building)
        {
            var validation = BuildingSystem.ValidateUpgrade(state, config, definition.Id);
            if (BuildingSystem.GetLevel(definition, building.Level + 1) == null)
            {
                _button.text = _catalog.MaxButtonLabel;
                _button.SetEnabled(false);
            }
            else if (!building.IsUnlocked)
            {
                _button.text = _catalog.LockedButtonLabel;
                _button.SetEnabled(false);
            }
            else if (!validation.IsValid)
            {
                _button.text = _catalog.NeedResourcesButtonLabel;
                _button.SetEnabled(false);
            }
            else
            {
                _button.text = _catalog.UpgradeButtonLabel;
                _button.SetEnabled(true);
            }
        }
    }
}
