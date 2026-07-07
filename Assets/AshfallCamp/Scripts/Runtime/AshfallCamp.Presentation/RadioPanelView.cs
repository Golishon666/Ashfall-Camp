using System;
using System.Collections.Generic;
using System.Globalization;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RadioPanelView : MonoBehaviour
    {
        private const float MinimumFrequencyMhz = 100f;
        private const float MaximumFrequencyMhz = 120f;
        private const float FrequencyStepMhz = 0.05f;
        private const float TargetFrequencyMhz = 107.45f;
        private const float SignalLockThresholdMhz = 0.15f;
        private const float SignalFalloffMhz = 1.75f;

        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private RawImage intelPanelArtwork;
        [SerializeField] private TextMeshProUGUI intelTitle;
        [SerializeField] private TextMeshProUGUI intelBody;
        [SerializeField] private RawImage broadcastPanelArtwork;
        [SerializeField] private TextMeshProUGUI broadcastTitle;
        [SerializeField] private TextMeshProUGUI broadcastCost;
        [SerializeField] private TextMeshProUGUI broadcastStatus;
        [SerializeField] private Button broadcastButton;
        [SerializeField] private TextMeshProUGUI broadcastButtonLabel;
        [SerializeField] private TextMeshProUGUI candidateListTitle;
        [SerializeField] private Image emptyPanel;
        [SerializeField] private RawImage emptyPanelArtwork;
        [SerializeField] private TextMeshProUGUI emptyTitle;
        [SerializeField] private TextMeshProUGUI emptyBody;
        [SerializeField] private RecruitCardView recruitCardPrefab;
        [SerializeField] private RectTransform candidateCardContainer;
        [SerializeField] private List<CandidateCardBinding> candidateCards = new List<CandidateCardBinding>();
        [SerializeField] private Button frequencyDownButton;
        [SerializeField] private Button frequencyUpButton;
        [SerializeField] private TextMeshProUGUI frequencyLabel;
        [SerializeField] private TextMeshProUGUI signalStateLabel;
        [SerializeField] private Slider signalStrengthSlider;
        [SerializeField] private List<Image> tunerSignalBars = new List<Image>();
        [SerializeField] private float currentFrequencyMhz = TargetFrequencyMhz;

        private Action<RecruitSurvivorViewRequest> _recruitRequested;
        private Action _broadcastRequested;
        private Action _skipRequested;
        private UnityAction _broadcastClick;
        private UnityAction _frequencyDownClick;
        private UnityAction _frequencyUpClick;
        private bool _useSkipAction;
        private readonly List<RecruitCardView> _dynamicCards = new List<RecruitCardView>();
        private readonly List<float> _signalBarBaseHeights = new List<float>();

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            TextMeshProUGUI intelTitleLabel,
            TextMeshProUGUI intelBodyLabel,
            TextMeshProUGUI broadcastTitleLabel,
            TextMeshProUGUI broadcastCostLabel,
            TextMeshProUGUI broadcastStatusLabel,
            Button broadcastActionButton,
            TextMeshProUGUI broadcastActionLabel,
            TextMeshProUGUI candidatesTitleLabel,
            Image emptyStatePanel,
            TextMeshProUGUI emptyStateTitle,
            TextMeshProUGUI emptyStateBody,
            IEnumerable<CandidateCardBinding> cards,
            RawImage intelArtwork = null,
            RawImage broadcastArtwork = null,
            RawImage emptyStateArtwork = null)
        {
            title = titleLabel;
            if (intelArtwork != null)
            {
                intelPanelArtwork = intelArtwork;
            }

            intelTitle = intelTitleLabel;
            intelBody = intelBodyLabel;
            if (broadcastArtwork != null)
            {
                broadcastPanelArtwork = broadcastArtwork;
            }

            broadcastTitle = broadcastTitleLabel;
            broadcastCost = broadcastCostLabel;
            broadcastStatus = broadcastStatusLabel;
            broadcastButton = broadcastActionButton;
            broadcastButtonLabel = broadcastActionLabel;
            candidateListTitle = candidatesTitleLabel;
            emptyPanel = emptyStatePanel;
            if (emptyStateArtwork != null)
            {
                emptyPanelArtwork = emptyStateArtwork;
            }

            emptyTitle = emptyStateTitle;
            emptyBody = emptyStateBody;
            candidateCards.Clear();
            if (cards != null)
            {
                candidateCards.AddRange(cards);
            }

            WireBroadcastButton();
        }

        public void ConfigureDynamicRecruitment(RecruitCardView cardPrefab, RectTransform cardContainer)
        {
            recruitCardPrefab = cardPrefab;
            candidateCardContainer = cardContainer;
            WireDynamicCards();
        }

        public void ConfigureTunerBindings(
            Button frequencyDown,
            Button frequencyUp,
            TextMeshProUGUI frequencyValueLabel,
            TextMeshProUGUI signalState,
            Slider signalStrength,
            IEnumerable<Image> signalBars)
        {
            ClearFrequencyButtons();
            frequencyDownButton = frequencyDown;
            frequencyUpButton = frequencyUp;
            frequencyLabel = frequencyValueLabel;
            signalStateLabel = signalState;
            signalStrengthSlider = signalStrength;
            tunerSignalBars.Clear();
            if (signalBars != null)
            {
                tunerSignalBars.AddRange(signalBars);
            }

            CaptureSignalBarHeights();
            WireFrequencyButtons();
            RenderTuner();
        }

        private void Awake()
        {
            WireBroadcastButton();
            WireFrequencyButtons();
            CaptureSignalBarHeights();
            RenderTuner();
        }

        private void OnDestroy()
        {
            ClearBroadcastButton();
            ClearFrequencyButtons();
            foreach (var card in candidateCards)
            {
                if (card != null)
                {
                    card.Clear();
                }
            }

            foreach (var card in _dynamicCards)
            {
                if (card != null)
                {
                    card.Clear();
                }
            }
        }

        public void SetRecruitHandler(Action<RecruitSurvivorViewRequest> recruitRequested)
        {
            _recruitRequested = recruitRequested;
            WireCandidateCards();
            WireDynamicCards();
        }

        public void SetBroadcastHandler(Action broadcastRequested)
        {
            _broadcastRequested = broadcastRequested;
            WireBroadcastButton();
        }

        public void SetSkipHandler(Action skipRequested)
        {
            _skipRequested = skipRequested;
            WireBroadcastButton();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            var radio = CampDashboardTextFormatter.BuildRadioScreen(state, config, catalog);
            ApplyArtwork(intelPanelArtwork, catalog.RadioIntelPanelTexture);
            ApplyArtwork(broadcastPanelArtwork, catalog.RadioBroadcastPanelTexture);
            UiText.Set(title, radio.Title);
            UiText.Set(intelTitle, radio.IntelTitle);
            UiText.Set(intelBody, radio.IntelBody);
            UiText.Set(broadcastTitle, radio.BroadcastTitle);
            UiText.Set(broadcastCost, radio.BroadcastCost);
            UiText.Set(broadcastStatus, radio.BroadcastStatus);
            UiText.Set(broadcastButtonLabel, radio.BroadcastButton);
            UiText.Set(candidateListTitle, radio.CandidateListTitle);
            UiText.Set(emptyTitle, radio.EmptyTitle);
            UiText.Set(emptyBody, radio.EmptyBody);

            if (broadcastButton != null)
            {
                _useSkipAction = radio.CanSkipCandidates;
                var signalLocked = IsSignalLocked();
                broadcastButton.interactable = radio.CanSkipCandidates
                    ? _skipRequested != null
                    : radio.CanBroadcast && signalLocked && _broadcastRequested != null;
            }

            if (broadcastStatus != null)
            {
                var canAct = radio.CanSkipCandidates || (radio.CanBroadcast && IsSignalLocked());
                if (radio.CanBroadcast && !IsSignalLocked())
                {
                    UiText.Set(broadcastStatus, "Tune signal");
                }

                broadcastStatus.color = canAct ? catalog.Theme.Sage : catalog.Theme.Rust;
            }

            var hasCandidates = radio.Candidates.Count > 0;
            UiText.SetActive(emptyPanel, !hasCandidates);
            ApplyArtwork(emptyPanelArtwork, hasCandidates ? null : catalog.RadioEmptyPanelTexture);

            if (UseDynamicCards())
            {
                RenderLegacyCards(null, catalog);
                RenderDynamicCards(radio.Candidates, catalog);
            }
            else
            {
                RenderLegacyCards(radio.Candidates, catalog);
            }

            RenderTuner();
        }

        private void RenderLegacyCards(IReadOnlyList<CampRadioCandidatePresentation> candidates, CampUiCatalogSO catalog)
        {
            for (var i = 0; i < candidateCards.Count; i++)
            {
                var candidate = candidates != null && i < candidates.Count ? candidates[i] : null;
                candidateCards[i].Render(candidate, catalog);
            }
        }

        private void WireBroadcastButton()
        {
            if (broadcastButton == null || _broadcastClick != null) return;
            _broadcastClick = OnBroadcastClicked;
            broadcastButton.onClick.AddListener(_broadcastClick);
        }

        private void WireFrequencyButtons()
        {
            if (frequencyDownButton != null && _frequencyDownClick == null)
            {
                _frequencyDownClick = () => AdjustFrequency(-FrequencyStepMhz);
                frequencyDownButton.onClick.AddListener(_frequencyDownClick);
            }

            if (frequencyUpButton != null && _frequencyUpClick == null)
            {
                _frequencyUpClick = () => AdjustFrequency(FrequencyStepMhz);
                frequencyUpButton.onClick.AddListener(_frequencyUpClick);
            }
        }

        private void WireCandidateCards()
        {
            foreach (var card in candidateCards)
            {
                if (card != null)
                {
                    card.Wire(OnCandidateClicked);
                }
            }
        }

        private void WireDynamicCards()
        {
            foreach (var card in _dynamicCards)
            {
                if (card != null)
                {
                    card.Wire(OnCandidateClicked);
                }
            }
        }

        private void ClearBroadcastButton()
        {
            if (broadcastButton != null && _broadcastClick != null)
            {
                broadcastButton.onClick.RemoveListener(_broadcastClick);
            }

            _broadcastClick = null;
        }

        private void ClearFrequencyButtons()
        {
            if (frequencyDownButton != null && _frequencyDownClick != null)
            {
                frequencyDownButton.onClick.RemoveListener(_frequencyDownClick);
            }

            if (frequencyUpButton != null && _frequencyUpClick != null)
            {
                frequencyUpButton.onClick.RemoveListener(_frequencyUpClick);
            }

            _frequencyDownClick = null;
            _frequencyUpClick = null;
        }

        private void OnBroadcastClicked()
        {
            if (_useSkipAction)
            {
                _skipRequested?.Invoke();
                return;
            }

            _broadcastRequested?.Invoke();
        }

        private void AdjustFrequency(float deltaMhz)
        {
            currentFrequencyMhz = Mathf.Clamp(currentFrequencyMhz + deltaMhz, MinimumFrequencyMhz, MaximumFrequencyMhz);
            RenderTuner();
        }

        private bool IsSignalLocked()
        {
            return Mathf.Abs(currentFrequencyMhz - TargetFrequencyMhz) <= SignalLockThresholdMhz;
        }

        private void RenderTuner()
        {
            currentFrequencyMhz = Mathf.Clamp(currentFrequencyMhz, MinimumFrequencyMhz, MaximumFrequencyMhz);
            UiText.Set(frequencyLabel, currentFrequencyMhz.ToString("0.00", CultureInfo.InvariantCulture) + " MHz");

            var strength = CalculateSignalStrength();
            if (signalStrengthSlider != null)
            {
                signalStrengthSlider.SetValueWithoutNotify(strength);
            }

            UiText.Set(signalStateLabel, IsSignalLocked() ? "SIGNAL LOCKED" : "TUNING...");
            UpdateSignalBars(strength);
        }

        private float CalculateSignalStrength()
        {
            var distance = Mathf.Abs(currentFrequencyMhz - TargetFrequencyMhz);
            return Mathf.Clamp01(1f - distance / SignalFalloffMhz);
        }

        private void CaptureSignalBarHeights()
        {
            if (_signalBarBaseHeights.Count == tunerSignalBars.Count) return;

            _signalBarBaseHeights.Clear();
            foreach (var bar in tunerSignalBars)
            {
                var rect = bar != null ? bar.rectTransform : null;
                _signalBarBaseHeights.Add(rect != null ? Mathf.Max(8f, rect.sizeDelta.y) : 16f);
            }
        }

        private void UpdateSignalBars(float strength)
        {
            CaptureSignalBarHeights();
            for (var i = 0; i < tunerSignalBars.Count; i++)
            {
                var bar = tunerSignalBars[i];
                if (bar == null) continue;

                var phase = Mathf.Abs(Mathf.Sin((i + 1) * 1.73f));
                var baseHeight = i < _signalBarBaseHeights.Count ? _signalBarBaseHeights[i] : 16f;
                var rect = bar.rectTransform;
                var size = rect.sizeDelta;
                size.y = Mathf.Lerp(6f, Mathf.Max(baseHeight, 28f) * (0.7f + phase * 0.6f), strength);
                rect.sizeDelta = size;

                var alpha = Mathf.Lerp(0.25f, 1f, strength);
                bar.color = new Color(0.62f, 0.82f, 0.46f, alpha);
            }
        }

        private bool UseDynamicCards()
        {
            return recruitCardPrefab != null && candidateCardContainer != null;
        }

        private void RenderDynamicCards(IReadOnlyList<CampRadioCandidatePresentation> candidates, CampUiCatalogSO catalog)
        {
            var count = candidates != null ? Mathf.Min(candidates.Count, RecruitmentSystem.MaxCandidateCount) : 0;
            EnsureDynamicCardCount(count);
            for (var i = 0; i < _dynamicCards.Count; i++)
            {
                _dynamicCards[i].Render(i < count ? candidates[i] : null, catalog);
            }
        }

        private void EnsureDynamicCardCount(int count)
        {
            count = Mathf.Clamp(count, 0, RecruitmentSystem.MaxCandidateCount);
            while (_dynamicCards.Count < count)
            {
                var card = Instantiate(recruitCardPrefab, candidateCardContainer);
                card.name = recruitCardPrefab.name + "_" + (_dynamicCards.Count + 1).ToString("00", CultureInfo.InvariantCulture);
                card.Wire(OnCandidateClicked);
                _dynamicCards.Add(card);
            }
        }

        private void OnCandidateClicked(string candidateId)
        {
            if (!string.IsNullOrWhiteSpace(candidateId))
            {
                _recruitRequested?.Invoke(new RecruitSurvivorViewRequest(candidateId));
            }
        }

        [Serializable]
        public sealed class CandidateCardBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private RawImage cardArtwork;
            [SerializeField] private RawImage portrait;
            [SerializeField] private TextMeshProUGUI avatarLabel;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI metaLabel;
            [SerializeField] private TextMeshProUGUI skillLabel;
            [SerializeField] private TextMeshProUGUI traitsLabel;
            [SerializeField] private Button recruitButton;
            [SerializeField] private TextMeshProUGUI recruitButtonLabel;

            [NonSerialized] private string _candidateId = string.Empty;
            [NonSerialized] private Action<string> _selected;
            [NonSerialized] private UnityAction _click;

            public CandidateCardBinding()
            {
            }

            public CandidateCardBinding(
                Image panel,
                TextMeshProUGUI avatarLabel,
                TextMeshProUGUI nameLabel,
                TextMeshProUGUI metaLabel,
                TextMeshProUGUI skillLabel,
                TextMeshProUGUI traitsLabel,
                Button recruitButton,
                TextMeshProUGUI recruitButtonLabel)
                : this(panel, null, avatarLabel, nameLabel, metaLabel, skillLabel, traitsLabel, recruitButton, recruitButtonLabel)
            {
            }

            public CandidateCardBinding(
                Image panel,
                RawImage portrait,
                TextMeshProUGUI avatarLabel,
                TextMeshProUGUI nameLabel,
                TextMeshProUGUI metaLabel,
                TextMeshProUGUI skillLabel,
                TextMeshProUGUI traitsLabel,
                Button recruitButton,
                TextMeshProUGUI recruitButtonLabel)
            {
                this.panel = panel;
                this.portrait = portrait;
                this.avatarLabel = avatarLabel;
                this.nameLabel = nameLabel;
                this.metaLabel = metaLabel;
                this.skillLabel = skillLabel;
                this.traitsLabel = traitsLabel;
                this.recruitButton = recruitButton;
                this.recruitButtonLabel = recruitButtonLabel;
            }

            public void Wire(Action<string> selected)
            {
                Clear();
                _selected = selected;
                if (recruitButton == null) return;
                _click = OnClicked;
                recruitButton.onClick.AddListener(_click);
            }

            public void Clear()
            {
                if (recruitButton != null && _click != null)
                {
                    recruitButton.onClick.RemoveListener(_click);
                }

                _click = null;
            }

            public void Render(CampRadioCandidatePresentation candidate, CampUiCatalogSO catalog)
            {
                _candidateId = candidate != null ? candidate.CandidateId : string.Empty;
                UiText.SetActive(panel, candidate != null);
                if (recruitButton != null) recruitButton.interactable = candidate != null && candidate.CanRecruit && _selected != null;
                if (candidate == null)
                {
                    ApplyArtwork(cardArtwork, null);
                    return;
                }

                ApplyArtwork(cardArtwork, catalog.RadioCandidateCardTexture);
                var hasPortrait = ApplyPortrait(portrait, candidate.Portrait);
                UiText.SetActive(avatarLabel, !hasPortrait);
                UiText.Set(avatarLabel, candidate.Avatar);
                UiText.Set(nameLabel, candidate.Name);
                UiText.Set(metaLabel, candidate.Meta);
                UiText.Set(skillLabel, candidate.Skill);
                UiText.Set(traitsLabel, candidate.Traits);
                UiText.Set(recruitButtonLabel, candidate.RecruitButton);

                if (panel != null)
                {
                    panel.color = catalog.Theme.WithAlpha(catalog.Theme.PaperDark, catalog.Theme.RadioCandidatePanelAlpha);
                }

                if (avatarLabel != null) avatarLabel.color = catalog.Theme.Paper;
                if (nameLabel != null) nameLabel.color = catalog.Theme.Ink;
                if (metaLabel != null) metaLabel.color = catalog.Theme.MutedInk;
                if (skillLabel != null) skillLabel.color = catalog.Theme.Teal;
                if (traitsLabel != null) traitsLabel.color = catalog.Theme.MutedInk;
                if (recruitButtonLabel != null) recruitButtonLabel.color = catalog.Theme.Paper;
            }

            private void OnClicked()
            {
                if (!string.IsNullOrWhiteSpace(_candidateId))
                {
                    _selected?.Invoke(_candidateId);
                }
            }

            private static bool ApplyPortrait(RawImage target, Texture2D texture)
            {
                if (target == null) return false;

                var hasPortrait = texture != null;
                target.gameObject.SetActive(hasPortrait);
                if (hasPortrait)
                {
                    target.texture = texture;
                    target.color = Color.white;
                }

                return hasPortrait;
            }
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
    }
}
