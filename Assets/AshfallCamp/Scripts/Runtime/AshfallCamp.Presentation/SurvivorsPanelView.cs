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
        [SerializeField] private TextMeshProUGUI detailTreatment;
        [SerializeField] private TextMeshProUGUI detailStats;
        [SerializeField] private TextMeshProUGUI detailMedicineCost;
        [SerializeField] private Button useMedicineButton;
        [SerializeField] private TextMeshProUGUI useMedicineButtonLabel;

        private string _selectedSurvivorId = string.Empty;
        private Action<UseMedicineRequest> _useMedicineRequested;
        private UnityAction _useMedicineClick;

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
            TextMeshProUGUI selectedDetailStats,
            TextMeshProUGUI selectedDetailTreatment = null,
            TextMeshProUGUI selectedDetailMedicineCost = null,
            Button selectedUseMedicineButton = null,
            TextMeshProUGUI selectedUseMedicineButtonLabel = null)
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
            if (selectedDetailTreatment != null)
            {
                detailTreatment = selectedDetailTreatment;
            }

            if (selectedDetailMedicineCost != null)
            {
                detailMedicineCost = selectedDetailMedicineCost;
            }

            if (selectedUseMedicineButton != null)
            {
                useMedicineButton = selectedUseMedicineButton;
            }

            if (selectedUseMedicineButtonLabel != null)
            {
                useMedicineButtonLabel = selectedUseMedicineButtonLabel;
            }

            detailStats = selectedDetailStats;
            WireCards();
            WireUseMedicineButton();
        }

        private void Awake()
        {
            WireCards();
            WireUseMedicineButton();
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

            ClearUseMedicineButton();
        }

        public void SetUseMedicineHandler(Action<UseMedicineRequest> useMedicineRequested)
        {
            _useMedicineRequested = useMedicineRequested;
            WireUseMedicineButton();
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

            var cardPresentations = CampDashboardTextFormatter.BuildSurvivorCards(state, catalog);
            for (var i = 0; i < cards.Count; i++)
            {
                var card = i < cardPresentations.Count ? cardPresentations[i] : null;
                cards[i].Render(card, catalog, string.Equals(_selectedSurvivorId, card != null ? card.SurvivorId : string.Empty, StringComparison.Ordinal));
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

            var detail = CampDashboardTextFormatter.BuildSurvivorDetail(survivor, state, config, catalog);
            UiText.Set(detailTitle, detail.Title);
            UiText.Set(detailBackground, detail.Background);
            UiText.Set(detailTraits, detail.Traits);
            UiText.Set(detailWeapon, detail.Weapon);
            UiText.Set(detailTreatment, detail.Treatment);
            UiText.Set(detailStats, detail.Stats);
            UiText.Set(detailMedicineCost, detail.MedicineCost);
            UiText.Set(useMedicineButtonLabel, detail.MedicineButton);
            UiText.SetActive(detailMedicineCost, detail.ShowMedicineAction);
            if (useMedicineButton != null)
            {
                UiText.SetActive(useMedicineButton, detail.ShowMedicineAction);
                useMedicineButton.interactable = detail.ShowMedicineAction && detail.CanUseMedicine && _useMedicineRequested != null;
            }
        }

        private void WireUseMedicineButton()
        {
            if (useMedicineButton == null || _useMedicineClick != null) return;
            _useMedicineClick = OnUseMedicineClicked;
            useMedicineButton.onClick.AddListener(_useMedicineClick);
        }

        private void ClearUseMedicineButton()
        {
            if (useMedicineButton != null && _useMedicineClick != null)
            {
                useMedicineButton.onClick.RemoveListener(_useMedicineClick);
            }

            _useMedicineClick = null;
        }

        private void OnUseMedicineClicked()
        {
            if (!string.IsNullOrWhiteSpace(_selectedSurvivorId))
            {
                _useMedicineRequested?.Invoke(new UseMedicineRequest { SurvivorId = _selectedSurvivorId });
            }
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

            public void Render(CampSurvivorCardPresentation survivor, CampUiCatalogSO catalog, bool isSelected)
            {
                _survivorId = survivor != null ? survivor.SurvivorId : string.Empty;
                UiText.SetActive(panel, survivor != null);
                if (survivor == null) return;

                if (panel != null)
                {
                    panel.color = isSelected
                        ? new Color(catalog.Theme.Teal.r, catalog.Theme.Teal.g, catalog.Theme.Teal.b, 0.86f)
                        : new Color(catalog.Theme.PaperDark.r, catalog.Theme.PaperDark.g, catalog.Theme.PaperDark.b, 0.64f);
                }

                var textColor = isSelected ? catalog.Theme.Paper : catalog.Theme.Ink;
                UiText.Set(avatarLabel, survivor.Avatar);
                UiText.Set(nameLabel, survivor.Name);
                UiText.Set(stateLabel, survivor.State);
                UiText.Set(skillLabel, survivor.Skill);
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
