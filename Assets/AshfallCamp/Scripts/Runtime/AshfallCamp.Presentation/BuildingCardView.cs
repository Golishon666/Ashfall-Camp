using System;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BuildingCardView : MonoBehaviour
    {
        [SerializeField] private string buildingId;
        [SerializeField] private RawImage cardArtwork;
        [SerializeField] private TextMeshProUGUI imageLetter;
        [SerializeField] private RawImage icon;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI levelLabel;
        [SerializeField] private TextMeshProUGUI descriptionLabel;
        [SerializeField] private TextMeshProUGUI effectLabel;
        [SerializeField] private BuildingCostRowView costRow;
        [SerializeField] private Button upgradeButton;
        [SerializeField] private TextMeshProUGUI upgradeLabel;

        private Action<string> _upgradeRequested;
        private bool _buttonWired;

        public string BuildingId { get { return buildingId; } }

        private void Awake()
        {
            WireButton();
        }

        private void OnDestroy()
        {
            if (upgradeButton != null)
            {
                upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            }
        }

        public void ConfigureBindings(
            string id,
            TextMeshProUGUI imageLetterText,
            RawImage buildingIcon,
            TextMeshProUGUI buildingName,
            TextMeshProUGUI buildingLevel,
            TextMeshProUGUI buildingDescription,
            TextMeshProUGUI buildingEffect,
            BuildingCostRowView costRowView,
            Button button,
            TextMeshProUGUI buttonLabel,
            RawImage buildingCardArtwork = null)
        {
            buildingId = id;
            cardArtwork = buildingCardArtwork;
            imageLetter = imageLetterText;
            icon = buildingIcon;
            nameLabel = buildingName;
            levelLabel = buildingLevel;
            descriptionLabel = buildingDescription;
            effectLabel = buildingEffect;
            costRow = costRowView;
            upgradeButton = button;
            upgradeLabel = buttonLabel;
            _buttonWired = false;
            WireButton();
        }

        public void SetUpgradeHandler(Action<string> upgradeRequested)
        {
            _upgradeRequested = upgradeRequested;
            WireButton();
        }

        public void Render(CampUiCatalogSO catalog, BuildingUiEntry entry, GameState state, GameConfigSnapshot config)
        {
            if (catalog == null || entry == null || state == null || config == null || string.IsNullOrWhiteSpace(buildingId))
            {
                gameObject.SetActive(false);
                return;
            }

            BuildingDefinition definition;
            BuildingState building;
            var hasDefinition = config.TryGetBuilding(buildingId, out definition);
            var hasState = state.Buildings.TryGetValue(buildingId, out building);
            gameObject.SetActive(hasDefinition && hasState);
            if (!hasDefinition || !hasState) return;

            ApplyArtwork(cardArtwork, catalog.BuildingCardTexture);
            var hasIcon = icon != null && entry.Icon != null;
            UiText.SetActive(imageLetter, !hasIcon);
            if (!hasIcon)
            {
                UiText.Set(imageLetter, string.IsNullOrEmpty(definition.Name) ? string.Empty : definition.Name.Substring(0, 1).ToUpperInvariant());
            }

            FitSingleLineTitle(nameLabel);
            if (icon != null)
            {
                icon.gameObject.SetActive(hasIcon);
                if (hasIcon)
                {
                    icon.texture = entry.Icon;
                    icon.color = Color.white;
                }
            }

            UiText.Set(nameLabel, definition.Name);
            UiText.Set(levelLabel, string.Format(catalog.LevelLabelFormat, building.Level));
            UiText.Set(descriptionLabel, entry.Description);
            UiText.Set(effectLabel, BuildingEffectTextFormatter.Format(catalog, definition, building));

            if (costRow != null)
            {
                costRow.Render(catalog, definition, building);
            }

            RenderUpgradeButton(catalog, state, config, definition, building);
        }

        private void RenderUpgradeButton(CampUiCatalogSO catalog, GameState state, GameConfigSnapshot config, BuildingDefinition definition, BuildingState building)
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

            UiText.Set(upgradeLabel, label);
            if (upgradeButton != null)
            {
                upgradeButton.interactable = interactable;
            }
        }

        private static void ApplyArtwork(RawImage target, Texture2D texture)
        {
            if (target == null) return;

            var hasTexture = texture != null;
            target.gameObject.SetActive(hasTexture);
            if (!hasTexture) return;

            target.texture = texture;
            target.color = Color.white;
        }

        private static void FitSingleLineTitle(TextMeshProUGUI label)
        {
            if (label == null) return;

            label.textWrappingMode = TextWrappingModes.NoWrap;
            label.overflowMode = TextOverflowModes.Ellipsis;
            label.enableAutoSizing = true;
            label.fontSizeMax = label.fontSize;
            var readableMinimum = label.fontSize * 0.7f;
            label.fontSizeMin = label.fontSizeMin > 0f ? Mathf.Min(label.fontSizeMin, readableMinimum) : readableMinimum;
        }

        private void WireButton()
        {
            if (_buttonWired || upgradeButton == null) return;
            upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
            _buttonWired = true;
        }

        private void OnUpgradeClicked()
        {
            if (!string.IsNullOrWhiteSpace(buildingId))
            {
                _upgradeRequested?.Invoke(buildingId);
            }
        }
    }
}
