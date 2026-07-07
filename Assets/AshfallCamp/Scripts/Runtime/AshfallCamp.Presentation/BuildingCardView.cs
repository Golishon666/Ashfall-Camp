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
        [SerializeField] private RawImage thumbnail;
        [SerializeField] private TextMeshProUGUI workersLabel;
        [SerializeField] private TextMeshProUGUI conditionValueLabel;
        [SerializeField] private Slider conditionSlider;
        [SerializeField] private Image conditionFill;
        [SerializeField] private CanvasGroup mainContent;
        [SerializeField] private Graphic timerOverlay;
        [SerializeField] private TextMeshProUGUI timerLabel;
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
            AutoBindMissingReferences();
            WireButton();
        }

        public void ConfigureBuildingId(string id)
        {
            buildingId = id ?? string.Empty;
            AutoBindMissingReferences();
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
            AutoBindMissingReferences();
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

            if (thumbnail != null)
            {
                var hasImage = entry.Image != null;
                thumbnail.gameObject.SetActive(hasImage);
                if (hasImage)
                {
                    thumbnail.texture = entry.Image;
                    thumbnail.color = Color.white;
                }
            }

            UiText.Set(nameLabel, definition.Name);
            UiText.Set(levelLabel, string.Format(string.IsNullOrWhiteSpace(catalog.LevelLabelFormat) ? "Level {0}" : catalog.LevelLabelFormat, building.Level));
            UiText.Set(descriptionLabel, entry.Description);
            UiText.Set(effectLabel, BuildingEffectTextFormatter.Format(catalog, definition, building));
            RenderWorkers(definition, building);
            RenderCondition(definition, building);

            if (costRow != null)
            {
                costRow.Render(catalog, definition, building);
            }
            else
            {
                RenderFallbackCostSlots(catalog, definition, building);
            }

            RenderUpgradeButton(catalog, state, config, definition, building);
            RenderTimerOverlay(building);
        }

        private void RenderUpgradeButton(CampUiCatalogSO catalog, GameState state, GameConfigSnapshot config, BuildingDefinition definition, BuildingState building)
        {
            var nextLevel = BuildingSystem.GetLevel(definition, building.Level + 1);
            var label = GetUpgradeLabel(catalog, building);
            var interactable = false;

            if (nextLevel == null)
            {
                label = Fallback(catalog.MaxButtonLabel, "MAX");
            }
            else if (BuildingSystem.IsUpgradeActive(building))
            {
                label = building.Level <= 0
                    ? Fallback(catalog.BuildButtonLabel, "BUILD")
                    : Fallback(catalog.UpgradeButtonLabel, "UPGRADE");
            }
            else if (!building.IsUnlocked)
            {
                label = Fallback(catalog.LockedButtonLabel, "LOCKED");
            }
            else
            {
                var validation = BuildingSystem.ValidateUpgrade(state, config, definition.Id);
                if (validation.IsValid)
                {
                    interactable = true;
                    label = GetUpgradeLabel(catalog, building);
                }
                else
                {
                    label = Fallback(catalog.NeedResourcesButtonLabel, "NEED");
                }
            }

            UiText.Set(upgradeLabel, label);
            if (upgradeButton != null)
            {
                upgradeButton.interactable = interactable;
            }
        }

        private void RenderWorkers(BuildingDefinition definition, BuildingState building)
        {
            var level = BuildingSystem.GetLevel(definition, building.Level);
            var capacity = level != null ? Math.Max(0, level.WorkerCapacity) : 0;
            var workers = capacity > 0 ? Mathf.Clamp(building.AssignedWorkers, 0, capacity) : Math.Max(0, building.AssignedWorkers);
            if (workersLabel != null)
            {
                workersLabel.text = capacity > 0
                    ? string.Format("{0} {1} / {2}", FormatWorkerDots(workers, capacity), workers, capacity)
                    : "0 / 0";
            }
        }

        private void RenderCondition(BuildingDefinition definition, BuildingState building)
        {
            var level = BuildingSystem.GetLevel(definition, building.Level);
            var condition = building.ConditionPercent > 0
                ? building.ConditionPercent
                : level != null ? level.DefaultConditionPercent : 0;
            condition = Mathf.Clamp(condition, 0, 100);

            UiText.Set(conditionValueLabel, condition + "%");
            if (conditionSlider != null)
            {
                conditionSlider.minValue = 0;
                conditionSlider.maxValue = 100;
                conditionSlider.value = condition;
            }

            if (conditionFill != null)
            {
                conditionFill.fillAmount = condition / 100f;
            }
        }

        private void RenderFallbackCostSlots(CampUiCatalogSO catalog, BuildingDefinition definition, BuildingState building)
        {
            var nextLevel = BuildingSystem.GetLevel(definition, building.Level + 1);
            var labels = new[]
            {
                FindChildComponent<TextMeshProUGUI>("Text_CostScrapValue"),
                FindChildComponent<TextMeshProUGUI>("Text_CostWaterValue"),
                FindChildComponent<TextMeshProUGUI>("Text_CostPartsValue")
            };
            var icons = new[]
            {
                FindChildComponent<RawImage>("Image_CostScrap"),
                FindChildComponent<RawImage>("Image_CostWater"),
                FindChildComponent<RawImage>("Image_CostParts")
            };

            var slotIndex = 0;
            if (nextLevel != null)
            {
                foreach (var cost in nextLevel.Cost)
                {
                    if (slotIndex >= labels.Length) break;
                    SetFallbackCostSlot(catalog, icons[slotIndex], labels[slotIndex], cost.Key, cost.Value, true);
                    slotIndex++;
                }
            }

            for (var i = slotIndex; i < labels.Length; i++)
            {
                SetFallbackCostSlot(catalog, icons[i], labels[i], string.Empty, 0, false);
            }
        }

        private static void SetFallbackCostSlot(CampUiCatalogSO catalog, RawImage icon, TextMeshProUGUI label, string resourceId, int value, bool active)
        {
            UiText.SetActive(icon, active);
            UiText.SetActive(label, active);
            if (!active) return;

            var resource = FindResourceEntry(catalog, resourceId);
            if (icon != null && resource != null && resource.Icon != null)
            {
                icon.texture = resource.Icon;
                icon.color = Color.white;
            }

            UiText.Set(label, value.ToString());
        }

        private static ResourceUiEntry FindResourceEntry(CampUiCatalogSO catalog, string resourceId)
        {
            foreach (var entry in catalog.ResourceBar)
            {
                if (string.Equals(entry.Id, resourceId, StringComparison.Ordinal)) return entry;
            }

            return null;
        }

        private void RenderTimerOverlay(BuildingState building)
        {
            var active = BuildingSystem.IsUpgradeActive(building);
            if (active)
            {
                EnsureTimerOverlay();
            }

            if (mainContent != null)
            {
                mainContent.alpha = active ? 0.48f : 1f;
            }

            if (timerOverlay != null)
            {
                timerOverlay.gameObject.SetActive(active);
            }

            UiText.SetActive(timerLabel, active);
            if (!active) return;

            var remaining = BuildingSystem.GetRemainingUpgradeSeconds(building, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
            UiText.Set(timerLabel, FormatTimer(remaining));
        }

        private void AutoBindMissingReferences()
        {
            if (thumbnail == null) thumbnail = FindChildComponent<RawImage>("Image_Thumbnail");
            if (workersLabel == null) workersLabel = FindChildComponent<TextMeshProUGUI>("Text_Workers");
            if (conditionValueLabel == null) conditionValueLabel = FindChildComponent<TextMeshProUGUI>("Text_ConditionValue");
            if (conditionSlider == null) conditionSlider = FindChildComponent<Slider>("Slider_Condition");
            if (conditionFill == null) conditionFill = FindChildComponent<Image>("Slider_Condition_Fill");
            if (timerLabel == null) timerLabel = FindChildComponent<TextMeshProUGUI>("Text_Timer");
            if (timerOverlay == null) timerOverlay = FindChildComponent<Graphic>("TimerOverlay");
            if (mainContent == null) mainContent = GetComponent<CanvasGroup>();

            if (nameLabel == null) nameLabel = FindNamedOrSuffixedText("Text_Title", "BuildingName_");
            if (levelLabel == null) levelLabel = FindNamedOrSuffixedText("Text_Level", "BuildingLevel_");
            if (descriptionLabel == null) descriptionLabel = FindNamedOrSuffixedText("Text_Description", "BuildingDescription_");
            if (effectLabel == null) effectLabel = FindNamedOrSuffixedText("Text_Effects", "BuildingEffect_");
            if (upgradeButton == null) upgradeButton = FindNamedOrSuffixedButton("Button_Upgrade", "BuildingUpgradeButton_");
            if (upgradeLabel == null) upgradeLabel = FindNamedOrSuffixedText("Text_UpgradeLabel", "BuildingUpgradeLabel_");
            if (costRow == null) costRow = GetComponentInChildren<BuildingCostRowView>(true);
        }

        private TextMeshProUGUI FindNamedOrSuffixedText(string exactName, string prefix)
        {
            var exact = FindChildComponent<TextMeshProUGUI>(exactName);
            if (exact != null) return exact;
            return string.IsNullOrWhiteSpace(buildingId) ? null : FindChildComponent<TextMeshProUGUI>(prefix + buildingId);
        }

        private Button FindNamedOrSuffixedButton(string exactName, string prefix)
        {
            var exact = FindChildComponent<Button>(exactName);
            if (exact != null) return exact;
            return string.IsNullOrWhiteSpace(buildingId) ? null : FindChildComponent<Button>(prefix + buildingId);
        }

        private T FindChildComponent<T>(string childName) where T : Component
        {
            if (string.IsNullOrWhiteSpace(childName)) return null;
            var children = GetComponentsInChildren<T>(true);
            for (var i = 0; i < children.Length; i++)
            {
                if (string.Equals(children[i].gameObject.name, childName, StringComparison.Ordinal))
                {
                    return children[i];
                }
            }

            return null;
        }

        private void EnsureTimerOverlay()
        {
            if (timerOverlay != null && timerLabel != null) return;

            var overlayObject = new GameObject("TimerOverlay", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            overlayObject.transform.SetParent(transform, false);
            var rect = overlayObject.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;
            var image = overlayObject.GetComponent<Image>();
            image.color = new Color(0f, 0f, 0f, 0.52f);
            timerOverlay = image;

            var labelObject = new GameObject("Text_Timer", typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            labelObject.transform.SetParent(overlayObject.transform, false);
            var labelRect = labelObject.GetComponent<RectTransform>();
            labelRect.anchorMin = new Vector2(0.08f, 0.2f);
            labelRect.anchorMax = new Vector2(0.92f, 0.8f);
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;
            timerLabel = labelObject.GetComponent<TextMeshProUGUI>();
            timerLabel.alignment = TextAlignmentOptions.Center;
            timerLabel.fontSize = 28f;
            timerLabel.enableAutoSizing = true;
            timerLabel.fontSizeMin = 14f;
            timerLabel.fontStyle = FontStyles.Bold;
            timerLabel.color = Color.white;
        }

        private static string FormatWorkerDots(int workers, int capacity)
        {
            if (capacity <= 0) return string.Empty;
            var result = string.Empty;
            var visibleCapacity = Math.Min(capacity, 10);
            var visibleWorkers = Math.Min(workers, visibleCapacity);
            for (var i = 0; i < visibleCapacity; i++)
            {
                result += i < visibleWorkers ? "●" : "○";
            }

            return result;
        }

        private static string FormatTimer(double seconds)
        {
            var whole = Math.Max(0, (int)Math.Ceiling(seconds));
            return string.Format("{0:00}:{1:00}", whole / 60, whole % 60);
        }

        private static string GetUpgradeLabel(CampUiCatalogSO catalog, BuildingState building)
        {
            return building.Level <= 0
                ? Fallback(catalog.BuildButtonLabel, "BUILD")
                : Fallback(catalog.UpgradeButtonLabel, "UPGRADE");
        }

        private static string Fallback(string value, string fallback)
        {
            return string.IsNullOrWhiteSpace(value) ? fallback : value;
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
