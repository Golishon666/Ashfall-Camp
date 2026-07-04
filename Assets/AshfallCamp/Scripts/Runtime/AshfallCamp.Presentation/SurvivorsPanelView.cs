using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SurvivorsPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI countLabel;
        [SerializeField] private Image emptyPanel;
        [SerializeField] private TextMeshProUGUI emptyTitle;
        [SerializeField] private TextMeshProUGUI emptyBody;
        [SerializeField] private List<SurvivorCardBinding> cards = new List<SurvivorCardBinding>();
        [SerializeField] private Image detailPanel;
        [SerializeField] private TextMeshProUGUI detailTitle;
        [SerializeField] private TextMeshProUGUI detailBackground;
        [SerializeField] private TextMeshProUGUI detailTraits;
        [SerializeField] private TextMeshProUGUI detailWeapon;
        [SerializeField] private TextMeshProUGUI detailStats;

        private string _selectedSurvivorId = string.Empty;

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            TextMeshProUGUI survivorCountLabel,
            Image emptyStatePanel,
            TextMeshProUGUI emptyStateTitle,
            TextMeshProUGUI emptyStateBody,
            IEnumerable<SurvivorCardBinding> cardBindings,
            Image selectedDetailPanel,
            TextMeshProUGUI selectedDetailTitle,
            TextMeshProUGUI selectedDetailBackground,
            TextMeshProUGUI selectedDetailTraits,
            TextMeshProUGUI selectedDetailWeapon,
            TextMeshProUGUI selectedDetailStats)
        {
            title = titleLabel;
            countLabel = survivorCountLabel;
            emptyPanel = emptyStatePanel;
            emptyTitle = emptyStateTitle;
            emptyBody = emptyStateBody;
            cards.Clear();
            if (cardBindings != null)
            {
                cards.AddRange(cardBindings);
            }

            detailPanel = selectedDetailPanel;
            detailTitle = selectedDetailTitle;
            detailBackground = selectedDetailBackground;
            detailTraits = selectedDetailTraits;
            detailWeapon = selectedDetailWeapon;
            detailStats = selectedDetailStats;
            WireCards();
        }

        private void Awake()
        {
            WireCards();
        }

        private void OnDestroy()
        {
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.Clear();
                }
            }
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            UiText.Set(title, catalog.SurvivorsScreenTitle);
            UiText.Set(countLabel, CampDashboardTextFormatter.Format(catalog.SurvivorsCountFormat, state.Survivors.Count, Math.Max(1, state.SurvivorCap)));

            var hasSurvivors = state.Survivors.Count > 0;
            UiText.SetActive(emptyPanel, !hasSurvivors);
            UiText.Set(emptyTitle, catalog.SurvivorEmptyTitle);
            UiText.Set(emptyBody, catalog.SurvivorEmptyBody);

            if (hasSurvivors && !HasSelectedSurvivor(state))
            {
                _selectedSurvivorId = state.Survivors[0].Id;
            }

            for (var i = 0; i < cards.Count; i++)
            {
                var survivor = i < state.Survivors.Count ? state.Survivors[i] : null;
                cards[i].Render(survivor, config, catalog, string.Equals(_selectedSurvivorId, survivor != null ? survivor.Id : string.Empty, StringComparison.Ordinal));
            }

            RenderDetail(FindSelectedSurvivor(state), state, config, catalog);
        }

        private void WireCards()
        {
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.Wire(SelectSurvivor);
                }
            }
        }

        private void SelectSurvivor(string survivorId)
        {
            _selectedSurvivorId = survivorId ?? string.Empty;
        }

        private bool HasSelectedSurvivor(GameState state)
        {
            return FindSelectedSurvivor(state) != null;
        }

        private SurvivorState FindSelectedSurvivor(GameState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(_selectedSurvivorId)) return null;
            foreach (var survivor in state.Survivors)
            {
                if (string.Equals(survivor.Id, _selectedSurvivorId, StringComparison.Ordinal))
                {
                    return survivor;
                }
            }

            return null;
        }

        private void RenderDetail(SurvivorState survivor, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            UiText.SetActive(detailPanel, survivor != null);
            if (survivor == null) return;

            UiText.Set(detailTitle, CampDashboardTextFormatter.Format(catalog.SurvivorDetailTitle, survivor.Name, survivor.Level));
            UiText.Set(detailBackground, CampDashboardTextFormatter.Format(catalog.SurvivorDetailBackgroundFormat, GetBackgroundName(survivor, config)));
            UiText.Set(detailTraits, CampDashboardTextFormatter.Format(catalog.SurvivorDetailTraitsFormat, FormatTraits(survivor, config, catalog)));
            UiText.Set(detailWeapon, FormatWeapon(survivor, state, config, catalog));
            UiText.Set(detailStats, CampDashboardTextFormatter.Format(
                catalog.SurvivorDetailStatsFormat,
                survivor.Health,
                survivor.MaxHealth,
                survivor.Morale,
                survivor.Fatigue,
                survivor.Xp));
        }

        private static string GetBackgroundName(SurvivorState survivor, GameConfigSnapshot config)
        {
            BackgroundDefinition background;
            return config.Backgrounds.TryGetValue(survivor.BackgroundId, out background) ? background.Name : survivor.BackgroundId;
        }

        private static string FormatTraits(SurvivorState survivor, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (survivor.TraitIds.Count == 0) return catalog.SurvivorNoTraitsLabel;

            var names = new List<string>();
            foreach (var traitId in survivor.TraitIds)
            {
                TraitDefinition trait;
                names.Add(config.Traits.TryGetValue(traitId, out trait) ? trait.Name : traitId);
            }

            return string.Join(", ", names);
        }

        private static string FormatWeapon(SurvivorState survivor, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var item = FindEquippedWeapon(survivor, state);
            if (item == null)
            {
                return CampDashboardTextFormatter.Format(catalog.SurvivorDetailWeaponFormat, catalog.SurvivorNoWeaponLabel, 0, 0);
            }

            ItemDefinition definition;
            var itemName = config.Items.TryGetValue(item.ItemId, out definition) ? definition.Name : item.ItemId;
            return CampDashboardTextFormatter.Format(catalog.SurvivorDetailWeaponFormat, itemName, item.Durability, item.MaxDurability);
        }

        private static InventoryItemState FindEquippedWeapon(SurvivorState survivor, GameState state)
        {
            if (survivor == null || state == null || string.IsNullOrWhiteSpace(survivor.Equipment.WeaponItemUid)) return null;
            foreach (var item in state.Inventory)
            {
                if (string.Equals(item.Uid, survivor.Equipment.WeaponItemUid, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private static string FormatTopSkill(SurvivorState survivor, CampUiCatalogSO catalog)
        {
            if (survivor == null || survivor.Skills.Count == 0) return string.Empty;

            var bestId = string.Empty;
            var bestValue = int.MinValue;
            foreach (var pair in survivor.Skills)
            {
                if (pair.Value < bestValue) continue;
                if (pair.Value == bestValue && string.CompareOrdinal(pair.Key, bestId) >= 0) continue;

                bestId = pair.Key;
                bestValue = pair.Value;
            }

            return CampDashboardTextFormatter.Format(catalog.SurvivorCardSkillFormat, GetSkillLabel(bestId, catalog), Math.Max(0, bestValue));
        }

        private static string GetSkillLabel(string skillId, CampUiCatalogSO catalog)
        {
            foreach (var entry in catalog.SurvivorSkillLabels)
            {
                if (entry != null && string.Equals(entry.Id, skillId, StringComparison.Ordinal))
                {
                    return entry.Label;
                }
            }

            return skillId;
        }

        [Serializable]
        public sealed class SurvivorCardBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private Button button;
            [SerializeField] private TextMeshProUGUI avatarLabel;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI stateLabel;
            [SerializeField] private TextMeshProUGUI skillLabel;

            [NonSerialized] private string _survivorId = string.Empty;
            [NonSerialized] private UnityAction _cachedClick;
            [NonSerialized] private Action<string> _selected;

            public SurvivorCardBinding()
            {
            }

            public SurvivorCardBinding(Image panel, Button button, TextMeshProUGUI avatarLabel, TextMeshProUGUI nameLabel, TextMeshProUGUI stateLabel, TextMeshProUGUI skillLabel)
            {
                this.panel = panel;
                this.button = button;
                this.avatarLabel = avatarLabel;
                this.nameLabel = nameLabel;
                this.stateLabel = stateLabel;
                this.skillLabel = skillLabel;
            }

            public void Wire(Action<string> selected)
            {
                if (button == null) return;
                Clear();
                _selected = selected;
                _cachedClick = OnClicked;
                button.onClick.AddListener(_cachedClick);
            }

            public void Clear()
            {
                if (button != null && _cachedClick != null)
                {
                    button.onClick.RemoveListener(_cachedClick);
                }

                _cachedClick = null;
            }

            public void Render(SurvivorState survivor, GameConfigSnapshot config, CampUiCatalogSO catalog, bool isSelected)
            {
                _survivorId = survivor != null ? survivor.Id : string.Empty;
                UiText.SetActive(panel, survivor != null);
                if (survivor == null) return;

                if (panel != null)
                {
                    panel.color = isSelected
                        ? new Color(catalog.Theme.Teal.r, catalog.Theme.Teal.g, catalog.Theme.Teal.b, 0.86f)
                        : new Color(catalog.Theme.PaperDark.r, catalog.Theme.PaperDark.g, catalog.Theme.PaperDark.b, 0.64f);
                }

                var textColor = isSelected ? catalog.Theme.Paper : catalog.Theme.Ink;
                UiText.Set(avatarLabel, string.IsNullOrEmpty(survivor.Name) ? string.Empty : survivor.Name.Substring(0, 1).ToUpperInvariant());
                UiText.Set(nameLabel, survivor.Name);
                UiText.Set(stateLabel, CampDashboardTextFormatter.Format(catalog.SurvivorCardStateFormat, survivor.State, survivor.Level));
                UiText.Set(skillLabel, FormatTopSkill(survivor, catalog));
                if (avatarLabel != null) avatarLabel.color = catalog.Theme.Paper;
                if (nameLabel != null) nameLabel.color = textColor;
                if (stateLabel != null) stateLabel.color = isSelected ? catalog.Theme.PaperDark : catalog.Theme.MutedInk;
                if (skillLabel != null) skillLabel.color = isSelected ? catalog.Theme.PaperDark : catalog.Theme.MutedInk;
                if (button != null) button.interactable = !isSelected;
            }

            private void OnClicked()
            {
                if (!string.IsNullOrWhiteSpace(_survivorId))
                {
                    _selected?.Invoke(_survivorId);
                }
            }
        }
    }
}
