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
        [SerializeField] private RawImage emptyPanelArtwork;
        [SerializeField] private TextMeshProUGUI emptyTitle;
        [SerializeField] private TextMeshProUGUI emptyBody;
        [SerializeField] private GameObject rosterRoot;
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Button viewDetailsButton;
        [SerializeField] private Button closeDetailButton;
        [SerializeField] private Image selectedSummaryPanel;
        [SerializeField] private RawImage selectedSummaryPortrait;
        [SerializeField] private TextMeshProUGUI selectedSummaryAvatar;
        [SerializeField] private TextMeshProUGUI selectedSummaryName;
        [SerializeField] private TextMeshProUGUI selectedSummaryRole;
        [SerializeField] private TextMeshProUGUI selectedSummaryLevel;
        [SerializeField] private TextMeshProUGUI selectedSummaryPower;
        [SerializeField] private TextMeshProUGUI selectedSummaryNote;
        [SerializeField] private List<SurvivorCardBinding> cards = new List<SurvivorCardBinding>();
        [SerializeField] private Image detailPanel;
        [SerializeField] private RawImage detailPanelArtwork;
        [SerializeField] private TextMeshProUGUI detailTitle;
        [SerializeField] private TextMeshProUGUI detailLevel;
        [SerializeField] private TextMeshProUGUI detailXp;
        [SerializeField] private TextMeshProUGUI detailStatus;
        [SerializeField] private TextMeshProUGUI detailBackground;
        [SerializeField] private TextMeshProUGUI detailTraits;
        [SerializeField] private TextMeshProUGUI detailWeapon;
        [SerializeField] private TextMeshProUGUI detailTreatment;
        [SerializeField] private TextMeshProUGUI detailStats;
        [SerializeField] private Slider detailHealthSlider;
        [SerializeField] private Slider detailFatigueSlider;
        [SerializeField] private Slider detailMoraleSlider;
        [SerializeField] private TextMeshProUGUI detailHealthValue;
        [SerializeField] private TextMeshProUGUI detailFatigueValue;
        [SerializeField] private TextMeshProUGUI detailMoraleValue;
        [SerializeField] private TextMeshProUGUI detailMedicineCost;
        [SerializeField] private RawImage detailPortrait;
        [SerializeField] private Button useMedicineButton;
        [SerializeField] private TextMeshProUGUI useMedicineButtonLabel;

        private string _selectedSurvivorId = string.Empty;
        private SurvivorDetailActionKind _selectedActionKind;
        private Action<UseMedicineRequest> _useMedicineRequested;
        private Action<StartRestRequest> _startRestRequested;
        private Action<StopRestRequest> _stopRestRequested;
        private UnityAction _useMedicineClick;
        private UnityAction _viewDetailsClick;
        private UnityAction _closeDetailClick;

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
            RawImage selectedDetailPortrait = null,
            Button selectedUseMedicineButton = null,
            TextMeshProUGUI selectedUseMedicineButtonLabel = null,
            RawImage emptyStateArtwork = null,
            RawImage selectedDetailArtwork = null,
            GameObject rosterScreenRoot = null,
            GameObject detailScreenRoot = null,
            Button selectedViewDetailsButton = null,
            Button selectedCloseDetailButton = null,
            TextMeshProUGUI selectedDetailLevel = null,
            TextMeshProUGUI selectedDetailXp = null,
            TextMeshProUGUI selectedDetailStatus = null,
            Slider selectedDetailHealthSlider = null,
            Slider selectedDetailFatigueSlider = null,
            Slider selectedDetailMoraleSlider = null,
            TextMeshProUGUI selectedDetailHealthValue = null,
            TextMeshProUGUI selectedDetailFatigueValue = null,
            TextMeshProUGUI selectedDetailMoraleValue = null,
            RawImage selectedRosterPortrait = null,
            Image selectedRosterPanel = null,
            TextMeshProUGUI selectedRosterAvatar = null,
            TextMeshProUGUI selectedRosterName = null,
            TextMeshProUGUI selectedRosterRole = null,
            TextMeshProUGUI selectedRosterLevel = null,
            TextMeshProUGUI selectedRosterPower = null,
            TextMeshProUGUI selectedRosterNote = null)
        {
            title = titleLabel;
            countLabel = survivorCountLabel;
            emptyPanel = emptyStatePanel;
            if (emptyStateArtwork != null)
            {
                emptyPanelArtwork = emptyStateArtwork;
            }

            emptyTitle = emptyStateTitle;
            emptyBody = emptyStateBody;
            cards.Clear();
            if (cardBindings != null)
            {
                cards.AddRange(cardBindings);
            }

            detailPanel = selectedDetailPanel;
            if (selectedDetailArtwork != null)
            {
                detailPanelArtwork = selectedDetailArtwork;
            }

            detailTitle = selectedDetailTitle;
            if (selectedDetailLevel != null)
            {
                detailLevel = selectedDetailLevel;
            }

            if (selectedDetailXp != null)
            {
                detailXp = selectedDetailXp;
            }

            if (selectedDetailStatus != null)
            {
                detailStatus = selectedDetailStatus;
            }

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

            if (selectedDetailPortrait != null)
            {
                detailPortrait = selectedDetailPortrait;
            }

            if (selectedUseMedicineButton != null)
            {
                useMedicineButton = selectedUseMedicineButton;
            }

            if (selectedUseMedicineButtonLabel != null)
            {
                useMedicineButtonLabel = selectedUseMedicineButtonLabel;
            }

            if (rosterScreenRoot != null)
            {
                rosterRoot = rosterScreenRoot;
            }

            if (detailScreenRoot != null)
            {
                detailRoot = detailScreenRoot;
            }

            if (selectedViewDetailsButton != null)
            {
                viewDetailsButton = selectedViewDetailsButton;
            }

            if (selectedCloseDetailButton != null)
            {
                closeDetailButton = selectedCloseDetailButton;
            }

            if (selectedDetailHealthSlider != null)
            {
                detailHealthSlider = selectedDetailHealthSlider;
            }

            if (selectedDetailFatigueSlider != null)
            {
                detailFatigueSlider = selectedDetailFatigueSlider;
            }

            if (selectedDetailMoraleSlider != null)
            {
                detailMoraleSlider = selectedDetailMoraleSlider;
            }

            if (selectedDetailHealthValue != null)
            {
                detailHealthValue = selectedDetailHealthValue;
            }

            if (selectedDetailFatigueValue != null)
            {
                detailFatigueValue = selectedDetailFatigueValue;
            }

            if (selectedDetailMoraleValue != null)
            {
                detailMoraleValue = selectedDetailMoraleValue;
            }

            if (selectedRosterPortrait != null)
            {
                selectedSummaryPortrait = selectedRosterPortrait;
            }

            if (selectedRosterPanel != null)
            {
                selectedSummaryPanel = selectedRosterPanel;
            }

            if (selectedRosterAvatar != null)
            {
                selectedSummaryAvatar = selectedRosterAvatar;
            }

            if (selectedRosterName != null)
            {
                selectedSummaryName = selectedRosterName;
            }

            if (selectedRosterRole != null)
            {
                selectedSummaryRole = selectedRosterRole;
            }

            if (selectedRosterLevel != null)
            {
                selectedSummaryLevel = selectedRosterLevel;
            }

            if (selectedRosterPower != null)
            {
                selectedSummaryPower = selectedRosterPower;
            }

            if (selectedRosterNote != null)
            {
                selectedSummaryNote = selectedRosterNote;
            }

            detailStats = selectedDetailStats;
            WireCards();
            WireUseMedicineButton();
            WireDetailNavigation();
        }

        private void Awake()
        {
            WireCards();
            WireUseMedicineButton();
            WireDetailNavigation();
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
            ClearDetailNavigation();
        }

        public void SetUseMedicineHandler(Action<UseMedicineRequest> useMedicineRequested)
        {
            _useMedicineRequested = useMedicineRequested;
            WireUseMedicineButton();
        }

        public void SetRestHandlers(Action<StartRestRequest> startRestRequested, Action<StopRestRequest> stopRestRequested)
        {
            _startRestRequested = startRestRequested;
            _stopRestRequested = stopRestRequested;
            WireUseMedicineButton();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            UiText.Set(title, catalog.SurvivorsScreenTitle);
            UiText.Set(countLabel, CampDashboardTextFormatter.Format(catalog.SurvivorsCountFormat, state.Survivors.Count, Math.Max(1, state.SurvivorCap)));

            var hasSurvivors = state.Survivors.Count > 0;
            UiText.SetActive(emptyPanel, !hasSurvivors);
            ApplyArtwork(emptyPanelArtwork, hasSurvivors ? null : catalog.SurvivorsEmptyPanelTexture);
            UiText.Set(emptyTitle, catalog.SurvivorEmptyTitle);
            UiText.Set(emptyBody, catalog.SurvivorEmptyBody);

            if (hasSurvivors && !HasSelectedSurvivor(state))
            {
                _selectedSurvivorId = state.Survivors[0].Id;
                ShowRoster();
            }

            var cardPresentations = CampDashboardTextFormatter.BuildSurvivorCards(state, catalog);
            CampSurvivorCardPresentation selectedCard = null;
            for (var i = 0; i < cards.Count; i++)
            {
                var card = i < cardPresentations.Count ? cardPresentations[i] : null;
                var isSelected = string.Equals(_selectedSurvivorId, card != null ? card.SurvivorId : string.Empty, StringComparison.Ordinal);
                if (isSelected)
                {
                    selectedCard = card;
                }

                cards[i].Render(card, catalog, isSelected);
            }

            RenderDetail(FindSelectedSurvivor(state), selectedCard, state, config, catalog);
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

        private void ShowRoster()
        {
            SetActive(rosterRoot, true);
            SetActive(detailRoot, false);
        }

        private void ShowDetail()
        {
            if (string.IsNullOrWhiteSpace(_selectedSurvivorId)) return;
            SetActive(rosterRoot, false);
            SetActive(detailRoot, true);
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

        private void RenderDetail(SurvivorState survivor, CampSurvivorCardPresentation selectedCard, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            UiText.SetActive(detailPanel, survivor != null);
            ApplyArtwork(detailPanelArtwork, null);
            UiText.SetActive(viewDetailsButton, survivor != null);
            RenderSelectedSummary(null, null, false);
            if (survivor == null) return;

            var detail = CampDashboardTextFormatter.BuildSurvivorDetail(survivor, state, config, catalog);
            RenderSelectedSummary(selectedCard, detail, true);
            UiText.Set(detailTitle, detail.Title);
            UiText.Set(detailLevel, detail.LevelText);
            UiText.Set(detailXp, detail.XpText);
            UiText.Set(detailStatus, detail.StatusText);
            UiText.Set(detailBackground, detail.Background);
            UiText.Set(detailTraits, detail.Traits);
            UiText.Set(detailWeapon, detail.Weapon);
            UiText.Set(detailTreatment, detail.Treatment);
            UiText.Set(detailStats, detail.Stats);
            SetSlider(detailHealthSlider, detail.HealthValue);
            SetSlider(detailFatigueSlider, detail.FatigueValue);
            SetSlider(detailMoraleSlider, detail.MoraleValue);
            UiText.Set(detailHealthValue, detail.HealthText);
            UiText.Set(detailFatigueValue, detail.FatigueText);
            UiText.Set(detailMoraleValue, detail.MoraleText);
            UiText.Set(detailMedicineCost, detail.ActionCost);
            UiText.Set(useMedicineButtonLabel, detail.ActionButton);
            ApplyPortrait(detailPortrait, detail.Portrait);
            _selectedActionKind = detail.ActionKind;
            UiText.SetActive(detailMedicineCost, detail.ShowAction && !string.IsNullOrWhiteSpace(detail.ActionCost));
            if (useMedicineButton != null)
            {
                UiText.SetActive(useMedicineButton, detail.ShowAction);
                useMedicineButton.interactable = detail.ShowAction && detail.CanUseAction && HasSelectedActionHandler(detail.ActionKind);
            }
        }

        private void RenderSelectedSummary(CampSurvivorCardPresentation selectedCard, CampSurvivorDetailPresentation detail, bool hasSurvivor)
        {
            UiText.SetActive(selectedSummaryPanel, hasSurvivor);
            if (!hasSurvivor)
            {
                ApplyPortrait(selectedSummaryPortrait, null);
                UiText.SetActive(selectedSummaryAvatar, false);
                UiText.Set(selectedSummaryName, string.Empty);
                UiText.Set(selectedSummaryRole, string.Empty);
                UiText.Set(selectedSummaryLevel, string.Empty);
                UiText.Set(selectedSummaryPower, string.Empty);
                UiText.Set(selectedSummaryNote, string.Empty);
                return;
            }

            var name = selectedCard != null ? selectedCard.Name : string.Empty;
            var hasPortrait = ApplyPortrait(selectedSummaryPortrait, detail != null ? detail.Portrait : null);
            UiText.SetActive(selectedSummaryAvatar, !hasPortrait);
            UiText.Set(selectedSummaryAvatar, string.IsNullOrWhiteSpace(name) ? string.Empty : name.Substring(0, 1).ToUpperInvariant());
            UiText.Set(selectedSummaryName, name);
            UiText.Set(selectedSummaryRole, selectedCard != null ? selectedCard.Skill : string.Empty);
            UiText.Set(selectedSummaryLevel, detail != null ? detail.LevelText : string.Empty);
            UiText.Set(selectedSummaryPower, selectedCard != null ? "POWER\n" + selectedCard.PowerText : string.Empty);
            UiText.Set(selectedSummaryNote, detail != null ? detail.Background : string.Empty);
        }

        private void WireDetailNavigation()
        {
            if (viewDetailsButton != null && _viewDetailsClick == null)
            {
                _viewDetailsClick = ShowDetail;
                viewDetailsButton.onClick.AddListener(_viewDetailsClick);
            }

            if (closeDetailButton != null && _closeDetailClick == null)
            {
                _closeDetailClick = ShowRoster;
                closeDetailButton.onClick.AddListener(_closeDetailClick);
            }
        }

        private void ClearDetailNavigation()
        {
            if (viewDetailsButton != null && _viewDetailsClick != null)
            {
                viewDetailsButton.onClick.RemoveListener(_viewDetailsClick);
            }

            if (closeDetailButton != null && _closeDetailClick != null)
            {
                closeDetailButton.onClick.RemoveListener(_closeDetailClick);
            }

            _viewDetailsClick = null;
            _closeDetailClick = null;
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
            if (string.IsNullOrWhiteSpace(_selectedSurvivorId)) return;

            if (_selectedActionKind == SurvivorDetailActionKind.UseMedicine)
            {
                _useMedicineRequested?.Invoke(new UseMedicineRequest { SurvivorId = _selectedSurvivorId });
            }
            else if (_selectedActionKind == SurvivorDetailActionKind.StartRest)
            {
                _startRestRequested?.Invoke(new StartRestRequest { SurvivorId = _selectedSurvivorId });
            }
            else if (_selectedActionKind == SurvivorDetailActionKind.StopRest)
            {
                _stopRestRequested?.Invoke(new StopRestRequest { SurvivorId = _selectedSurvivorId });
            }
        }

        private bool HasSelectedActionHandler(SurvivorDetailActionKind actionKind)
        {
            if (actionKind == SurvivorDetailActionKind.UseMedicine) return _useMedicineRequested != null;
            if (actionKind == SurvivorDetailActionKind.StartRest) return _startRestRequested != null;
            if (actionKind == SurvivorDetailActionKind.StopRest) return _stopRestRequested != null;
            return false;
        }

        [Serializable]
        public sealed class SurvivorCardBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private RawImage cardArtwork;
            [SerializeField] private Button button;
            [SerializeField] private RawImage portrait;
            [SerializeField] private TextMeshProUGUI avatarLabel;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI stateLabel;
            [SerializeField] private TextMeshProUGUI skillLabel;
            [SerializeField] private TextMeshProUGUI levelLabel;
            [SerializeField] private TextMeshProUGUI powerLabel;
            [SerializeField] private Slider healthSlider;
            [SerializeField] private Slider fatigueSlider;
            [SerializeField] private TextMeshProUGUI healthValueLabel;
            [SerializeField] private TextMeshProUGUI fatigueValueLabel;

            [NonSerialized] private string _survivorId = string.Empty;
            [NonSerialized] private UnityAction _cachedClick;
            [NonSerialized] private Action<string> _selected;

            public SurvivorCardBinding()
            {
            }

            public SurvivorCardBinding(Image panel, Button button, TextMeshProUGUI avatarLabel, TextMeshProUGUI nameLabel, TextMeshProUGUI stateLabel, TextMeshProUGUI skillLabel)
                : this(panel, button, null, avatarLabel, nameLabel, stateLabel, skillLabel)
            {
            }

            public SurvivorCardBinding(Image panel, Button button, RawImage portrait, TextMeshProUGUI avatarLabel, TextMeshProUGUI nameLabel, TextMeshProUGUI stateLabel, TextMeshProUGUI skillLabel)
                : this(panel, button, portrait, avatarLabel, nameLabel, stateLabel, skillLabel, null, null, null, null, null, null)
            {
            }

            public SurvivorCardBinding(
                Image panel,
                Button button,
                RawImage portrait,
                TextMeshProUGUI avatarLabel,
                TextMeshProUGUI nameLabel,
                TextMeshProUGUI stateLabel,
                TextMeshProUGUI skillLabel,
                TextMeshProUGUI levelLabel,
                TextMeshProUGUI powerLabel,
                Slider healthSlider,
                Slider fatigueSlider,
                TextMeshProUGUI healthValueLabel,
                TextMeshProUGUI fatigueValueLabel)
                : this(panel, null, button, portrait, avatarLabel, nameLabel, stateLabel, skillLabel, levelLabel, powerLabel, healthSlider, fatigueSlider, healthValueLabel, fatigueValueLabel)
            {
            }

            public SurvivorCardBinding(
                Image panel,
                RawImage cardArtwork,
                Button button,
                RawImage portrait,
                TextMeshProUGUI avatarLabel,
                TextMeshProUGUI nameLabel,
                TextMeshProUGUI stateLabel,
                TextMeshProUGUI skillLabel,
                TextMeshProUGUI levelLabel,
                TextMeshProUGUI powerLabel,
                Slider healthSlider,
                Slider fatigueSlider,
                TextMeshProUGUI healthValueLabel,
                TextMeshProUGUI fatigueValueLabel)
            {
                this.panel = panel;
                this.cardArtwork = cardArtwork;
                this.button = button;
                this.portrait = portrait;
                this.avatarLabel = avatarLabel;
                this.nameLabel = nameLabel;
                this.stateLabel = stateLabel;
                this.skillLabel = skillLabel;
                this.levelLabel = levelLabel;
                this.powerLabel = powerLabel;
                this.healthSlider = healthSlider;
                this.fatigueSlider = fatigueSlider;
                this.healthValueLabel = healthValueLabel;
                this.fatigueValueLabel = fatigueValueLabel;
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
                if (survivor == null)
                {
                    ApplyArtwork(cardArtwork, null);
                    return;
                }

                ApplyArtwork(cardArtwork, null);
                if (panel != null)
                {
                    panel.color = isSelected
                        ? catalog.Theme.WithAlpha(catalog.Theme.Teal, catalog.Theme.SurvivorSelectedPanelAlpha)
                        : catalog.Theme.WithAlpha(catalog.Theme.PaperDark, catalog.Theme.SurvivorInactivePanelAlpha);
                }

                var textColor = catalog.Theme.Ink;
                var hasPortrait = ApplyPortrait(portrait, survivor.Portrait);
                UiText.SetActive(avatarLabel, !hasPortrait);
                UiText.Set(avatarLabel, survivor.Avatar);
                UiText.Set(nameLabel, survivor.Name);
                UiText.Set(stateLabel, survivor.State);
                UiText.Set(skillLabel, survivor.Skill);
                UiText.Set(levelLabel, survivor.LevelText);
                UiText.Set(powerLabel, survivor.PowerText);
                SetSlider(healthSlider, survivor.HealthValue);
                SetSlider(fatigueSlider, survivor.FatigueValue);
                UiText.Set(healthValueLabel, survivor.HealthText);
                UiText.Set(fatigueValueLabel, survivor.FatigueText);
                if (avatarLabel != null) avatarLabel.color = catalog.Theme.Paper;
                if (nameLabel != null) nameLabel.color = textColor;
                if (stateLabel != null) stateLabel.color = catalog.Theme.MutedInk;
                if (skillLabel != null) skillLabel.color = catalog.Theme.MutedInk;
                if (levelLabel != null) levelLabel.color = catalog.Theme.MutedInk;
                if (powerLabel != null) powerLabel.color = textColor;
                if (healthValueLabel != null) healthValueLabel.color = catalog.Theme.MutedInk;
                if (fatigueValueLabel != null) fatigueValueLabel.color = catalog.Theme.MutedInk;
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

        private static bool ApplyPortrait(RawImage target, Texture2D portrait)
        {
            if (target == null) return false;

            var hasPortrait = portrait != null;
            target.gameObject.SetActive(hasPortrait);
            if (hasPortrait)
            {
                target.texture = portrait;
                target.color = Color.white;
            }

            return hasPortrait;
        }

        private static bool ApplyArtwork(RawImage target, Texture2D texture)
        {
            if (target == null) return false;

            var hasTexture = texture != null;
            target.gameObject.SetActive(hasTexture);
            if (hasTexture)
            {
                target.texture = texture;
                target.color = Color.white;
            }

            return hasTexture;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider == null) return;
            var clamped = Mathf.Clamp01(value);
            slider.SetValueWithoutNotify(clamped);
            if (slider.fillRect != null)
            {
                slider.fillRect.anchorMin = new Vector2(0f, 0f);
                slider.fillRect.anchorMax = new Vector2(clamped, 1f);
                slider.fillRect.offsetMin = Vector2.zero;
                slider.fillRect.offsetMax = Vector2.zero;
            }

            if (slider.handleRect != null)
            {
                slider.handleRect.anchorMin = new Vector2(clamped, 0.5f);
                slider.handleRect.anchorMax = new Vector2(clamped, 0.5f);
                slider.handleRect.anchoredPosition = Vector2.zero;
            }
        }
    }
}
