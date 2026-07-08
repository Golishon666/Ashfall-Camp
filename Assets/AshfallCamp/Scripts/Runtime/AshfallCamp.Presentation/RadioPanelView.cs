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
        private const float DefaultTargetFrequencyMhz = 107.45f;
        private const float SignalLockThreshold = 0.75f;
        private const float SignalNormalThreshold = 0.35f;
        private const float SignalGoodThreshold = 0.75f;
        private const double GoodSignalCandidateChanceMultiplier = 1.0;
        private const double NormalSignalCandidateChanceMultiplier = 0.5;
        private const double BadSignalCandidateChanceMultiplier = 0.25;
        private const float SignalGraphWindowMhz = 5f;
        private const int DefaultSignalGraphBarCount = 48;

        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private RawImage intelPanelArtwork;
        [SerializeField] private TextMeshProUGUI intelTitle;
        [SerializeField] private TextMeshProUGUI intelBody;
        [SerializeField] private RawImage broadcastPanelArtwork;
        [SerializeField] private TextMeshProUGUI broadcastTitle;
        [SerializeField] private TextMeshProUGUI broadcastCost;
        [SerializeField] private TextMeshProUGUI broadcastRadioIntelCostValue;
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
        [SerializeField] private TextMeshProUGUI callStrengthStateLabel;
        [SerializeField] private Image signalStrengthFillImage;
        [SerializeField] private TextMeshProUGUI scanRangeLabel;
        [SerializeField] private Slider scanRangeSlider;
        [SerializeField] private Image scanRangeFillImage;
        [SerializeField] private RectTransform tunerGraphContainer;
        [SerializeField] private Image tunerGraphBarTemplate;
        [SerializeField] private List<Image> tunerSignalBars = new List<Image>();
        [SerializeField] private int tunerGraphBarCount = DefaultSignalGraphBarCount;
        [SerializeField] private bool randomizeSignalOnEnable = true;
        [SerializeField] private bool resetFrequencyToTargetOnOpen = true;
        [SerializeField] private float targetFrequencyMhz = DefaultTargetFrequencyMhz;
        [SerializeField] private float currentFrequencyMhz = DefaultTargetFrequencyMhz;

        private Action<RecruitSurvivorViewRequest> _recruitRequested;
        private Action<BroadcastRecruitmentRequest> _broadcastRequested;
        private Action _skipRequested;
        private UnityAction _broadcastClick;
        private UnityAction _frequencyDownClick;
        private UnityAction _frequencyUpClick;
        private bool _useSkipAction;
        private bool _signalInitialized;
        private bool _lastCanBroadcast;
        private bool _lastCanSkipCandidates;
        private string _lastBroadcastStatusText = string.Empty;
        private CampUiCatalogSO _lastCatalog;
        private readonly List<RecruitCardView> _dynamicCards = new List<RecruitCardView>();
        private readonly List<Image> _runtimeSignalBars = new List<Image>();

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
            IEnumerable<Image> signalBars,
            TextMeshProUGUI scanRange = null,
            RectTransform graphContainer = null,
            Image graphBarTemplate = null,
            TextMeshProUGUI callStrengthState = null,
            Image signalStrengthFill = null,
            Slider rangeSlider = null,
            Image rangeFill = null)
        {
            ClearFrequencyButtons();
            frequencyDownButton = frequencyDown;
            frequencyUpButton = frequencyUp;
            frequencyLabel = frequencyValueLabel;
            signalStateLabel = signalState;
            signalStrengthSlider = signalStrength;
            if (callStrengthState != null)
            {
                callStrengthStateLabel = callStrengthState;
            }

            if (signalStrengthFill != null)
            {
                signalStrengthFillImage = signalStrengthFill;
            }

            if (scanRange != null)
            {
                scanRangeLabel = scanRange;
            }

            if (rangeSlider != null)
            {
                scanRangeSlider = rangeSlider;
            }

            if (rangeFill != null)
            {
                scanRangeFillImage = rangeFill;
            }

            if (graphContainer != null)
            {
                tunerGraphContainer = graphContainer;
            }

            if (graphBarTemplate != null)
            {
                tunerGraphBarTemplate = graphBarTemplate;
            }

            tunerSignalBars.Clear();
            if (signalBars != null)
            {
                tunerSignalBars.AddRange(signalBars);
            }

            ResetRuntimeSignalBars();
            WireFrequencyButtons();
            RenderTuner();
        }

        private void Awake()
        {
            WireBroadcastButton();
            WireFrequencyButtons();
            EnsureSignalInitialized();
            ResolveSliderFillBindings();
            RenderTuner();
        }

        private void OnEnable()
        {
            if (randomizeSignalOnEnable)
            {
                StartNewSignalTarget();
            }
            else
            {
                EnsureSignalInitialized();
            }

            RenderTuner();
        }

        private void OnDestroy()
        {
            ClearBroadcastButton();
            ClearFrequencyButtons();
            ResetRuntimeSignalBars();
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
            SetBroadcastHandler(broadcastRequested == null ? (Action<BroadcastRecruitmentRequest>)null : _ => broadcastRequested());
        }

        public void SetBroadcastHandler(Action<BroadcastRecruitmentRequest> broadcastRequested)
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
            _lastCatalog = catalog;
            EnsureSignalInitialized();

            var radio = CampDashboardTextFormatter.BuildRadioScreen(state, config, catalog);
            ApplyArtwork(intelPanelArtwork, catalog.RadioIntelPanelTexture);
            ApplyArtwork(broadcastPanelArtwork, catalog.RadioBroadcastPanelTexture);
            UiText.Set(title, radio.Title);
            UiText.Set(intelTitle, radio.IntelTitle);
            UiText.Set(intelBody, radio.IntelBody);
            UiText.Set(broadcastTitle, radio.BroadcastTitle);
            UiText.Set(broadcastCost, radio.BroadcastCost);
            RenderBroadcastRadioIntelCost(state, config);
            UiText.Set(broadcastStatus, radio.BroadcastStatus);
            _lastBroadcastStatusText = radio.BroadcastStatus;
            UiText.Set(broadcastButtonLabel, radio.BroadcastButton);
            UiText.Set(candidateListTitle, radio.CandidateListTitle);
            UiText.Set(emptyTitle, radio.EmptyTitle);
            UiText.Set(emptyBody, radio.EmptyBody);

            _useSkipAction = radio.CanSkipCandidates;
            _lastCanBroadcast = radio.CanBroadcast;
            _lastCanSkipCandidates = radio.CanSkipCandidates;
            if (broadcastButton != null)
            {
                RefreshBroadcastActionState(catalog);
            }

            if (broadcastStatus != null)
            {
                RefreshBroadcastStatus(catalog);
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

        private void RenderBroadcastRadioIntelCost(GameState state, GameConfigSnapshot config)
        {
            if (broadcastRadioIntelCostValue == null) return;
            var cost = RecruitmentSystem.CalculateCost(state, config);
            var amount = 0;
            var hasRadioIntelCost = cost != null && cost.TryGetValue(GameIds.Resources.RadioIntel, out amount) && amount > 0;
            broadcastRadioIntelCostValue.gameObject.SetActive(hasRadioIntelCost);
            UiText.Set(
                broadcastRadioIntelCostValue,
                hasRadioIntelCost ? amount.ToString(CultureInfo.InvariantCulture) : string.Empty);
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

            _broadcastRequested?.Invoke(CreateBroadcastRequest());
        }

        private void AdjustFrequency(float deltaMhz)
        {
            currentFrequencyMhz = Mathf.Clamp(currentFrequencyMhz + deltaMhz, MinimumFrequencyMhz, MaximumFrequencyMhz);
            RenderTuner();
            RefreshBroadcastActionState(_lastCatalog);
            RefreshBroadcastStatus(_lastCatalog);
        }

        private bool IsSignalLocked()
        {
            return CalculateSignalStrength() >= SignalLockThreshold;
        }

        private BroadcastRecruitmentRequest CreateBroadcastRequest()
        {
            return new BroadcastRecruitmentRequest
            {
                CandidateChanceMultiplier = ResolveCandidateChanceMultiplier()
            };
        }

        private double ResolveCandidateChanceMultiplier()
        {
            var tier = ResolveSignalTier(CalculateSignalStrength());
            if (tier == SignalStrengthTier.Good) return GoodSignalCandidateChanceMultiplier;
            return tier == SignalStrengthTier.Normal ? NormalSignalCandidateChanceMultiplier : BadSignalCandidateChanceMultiplier;
        }

        private void EnsureSignalInitialized()
        {
            if (_signalInitialized) return;
            targetFrequencyMhz = Mathf.Clamp(targetFrequencyMhz, MinimumFrequencyMhz, MaximumFrequencyMhz);
            currentFrequencyMhz = Mathf.Clamp(currentFrequencyMhz, MinimumFrequencyMhz, MaximumFrequencyMhz);
            _signalInitialized = true;
        }

        private void StartNewSignalTarget()
        {
            targetFrequencyMhz = GenerateRandomTargetFrequency();
            if (resetFrequencyToTargetOnOpen)
            {
                currentFrequencyMhz = targetFrequencyMhz;
            }

            _signalInitialized = true;
        }

        private static float GenerateRandomTargetFrequency()
        {
            var stepCount = Mathf.RoundToInt((MaximumFrequencyMhz - MinimumFrequencyMhz) / FrequencyStepMhz);
            var step = UnityEngine.Random.Range(0, stepCount + 1);
            return MinimumFrequencyMhz + step * FrequencyStepMhz;
        }

        private void RenderTuner()
        {
            EnsureSignalInitialized();
            ResolveSliderFillBindings();
            currentFrequencyMhz = Mathf.Clamp(currentFrequencyMhz, MinimumFrequencyMhz, MaximumFrequencyMhz);
            targetFrequencyMhz = Mathf.Clamp(targetFrequencyMhz, MinimumFrequencyMhz, MaximumFrequencyMhz);
            UiText.Set(frequencyLabel, currentFrequencyMhz.ToString("0.00", CultureInfo.InvariantCulture) + " MHz");
            UiText.Set(scanRangeLabel, string.Format(
                CultureInfo.InvariantCulture,
                "SCAN RANGE {0:0}-{1:0} MHz",
                MinimumFrequencyMhz,
                MaximumFrequencyMhz));
            UpdateScanRangeSlider();

            var strength = CalculateSignalStrength();
            if (signalStrengthSlider != null)
            {
                signalStrengthSlider.SetValueWithoutNotify(strength);
            }

            ApplySignalStrengthPresentation(strength);
            UiText.Set(signalStateLabel, IsSignalLocked() ? "SIGNAL LOCKED" : "TUNING...");
            UpdateSignalBars(strength);
        }

        private float CalculateSignalStrength()
        {
            return EvaluateSignalFunction(currentFrequencyMhz);
        }

        private float EvaluateSignalFunction(float frequencyMhz)
        {
            var primary = Gaussian(frequencyMhz, targetFrequencyMhz, 0.34f);
            var lowerEcho = Gaussian(frequencyMhz, targetFrequencyMhz - 1.05f, 0.2f) * 0.32f;
            var upperEcho = Gaussian(frequencyMhz, targetFrequencyMhz + 0.88f, 0.24f) * 0.26f;
            var lowNoise = 0.08f + Mathf.Abs(Mathf.Sin(frequencyMhz * 3.91f)) * 0.08f;
            var echo = Mathf.Clamp01(lowNoise + lowerEcho + upperEcho);
            return Mathf.Clamp01(primary + echo * (1f - primary));
        }

        private static float Gaussian(float x, float center, float sigma)
        {
            if (sigma <= 0f) return 0f;
            var normalized = (x - center) / sigma;
            return Mathf.Exp(-0.5f * normalized * normalized);
        }

        private void ResolveSliderFillBindings()
        {
            if (signalStrengthFillImage == null && signalStrengthSlider != null && signalStrengthSlider.fillRect != null)
            {
                signalStrengthFillImage = signalStrengthSlider.fillRect.GetComponent<Image>();
            }

            if (scanRangeFillImage == null && scanRangeSlider != null && scanRangeSlider.fillRect != null)
            {
                scanRangeFillImage = scanRangeSlider.fillRect.GetComponent<Image>();
            }
        }

        private void UpdateScanRangeSlider()
        {
            if (scanRangeSlider != null)
            {
                scanRangeSlider.minValue = 0f;
                scanRangeSlider.maxValue = 1f;
                scanRangeSlider.SetValueWithoutNotify(1f);
            }

            if (scanRangeFillImage != null)
            {
                scanRangeFillImage.color = _lastCatalog != null ? _lastCatalog.Theme.Teal : new Color32(0x2F, 0x6A, 0x72, 0xFF);
            }
        }

        private void ApplySignalStrengthPresentation(float strength)
        {
            var tier = ResolveSignalTier(strength);
            var color = ResolveSignalColor(tier);
            if (signalStrengthFillImage != null)
            {
                signalStrengthFillImage.color = color;
            }

            UiText.Set(callStrengthStateLabel, ResolveSignalLabel(tier));
            if (callStrengthStateLabel != null)
            {
                callStrengthStateLabel.color = color;
            }
        }

        private static SignalStrengthTier ResolveSignalTier(float strength)
        {
            if (strength >= SignalGoodThreshold) return SignalStrengthTier.Good;
            return strength >= SignalNormalThreshold ? SignalStrengthTier.Normal : SignalStrengthTier.Bad;
        }

        private Color ResolveSignalColor(SignalStrengthTier tier)
        {
            var theme = _lastCatalog != null ? _lastCatalog.Theme : null;
            if (tier == SignalStrengthTier.Good) return theme != null ? theme.Sage : new Color32(0x53, 0x78, 0x3F, 0xFF);
            if (tier == SignalStrengthTier.Normal) return new Color32(0xC6, 0x9A, 0x35, 0xFF);
            return theme != null ? theme.Rust : new Color32(0xC9, 0x63, 0x3A, 0xFF);
        }

        private static string ResolveSignalLabel(SignalStrengthTier tier)
        {
            if (tier == SignalStrengthTier.Good) return "GOOD";
            return tier == SignalStrengthTier.Normal ? "NORMAL" : "BAD";
        }

        private void ApplyBroadcastButtonVisual(bool enabled, CampUiCatalogSO catalog)
        {
            if (broadcastButton == null || catalog == null) return;
            var target = broadcastButton.targetGraphic as Graphic;
            if (target != null)
            {
                target.color = enabled
                    ? catalog.Theme.Teal
                    : catalog.Theme.WithAlpha(catalog.Theme.MutedInk, 0.45f);
            }

            if (broadcastButtonLabel != null)
            {
                broadcastButtonLabel.color = enabled
                    ? catalog.Theme.Paper
                    : catalog.Theme.WithAlpha(catalog.Theme.PaperDark, 0.65f);
            }
        }

        private void RefreshBroadcastActionState(CampUiCatalogSO catalog)
        {
            if (broadcastButton == null) return;
            var enabled = _lastCanSkipCandidates
                ? _skipRequested != null
                : _lastCanBroadcast && _broadcastRequested != null;
            broadcastButton.interactable = enabled;
            ApplyBroadcastButtonVisual(enabled, catalog);
        }

        private void RefreshBroadcastStatus(CampUiCatalogSO catalog)
        {
            if (broadcastStatus == null || catalog == null) return;

            var canAct = _lastCanSkipCandidates || _lastCanBroadcast;
            UiText.Set(broadcastStatus, _lastBroadcastStatusText);

            broadcastStatus.color = canAct ? catalog.Theme.Sage : catalog.Theme.Rust;
        }

        private void UpdateSignalBars(float strength)
        {
            EnsureRuntimeSignalBars();
            if (_runtimeSignalBars.Count == 0) return;

            var windowStart = Mathf.Clamp(
                currentFrequencyMhz - SignalGraphWindowMhz * 0.5f,
                MinimumFrequencyMhz,
                MaximumFrequencyMhz - SignalGraphWindowMhz);
            var windowEnd = windowStart + SignalGraphWindowMhz;

            for (var i = 0; i < _runtimeSignalBars.Count; i++)
            {
                var bar = _runtimeSignalBars[i];
                if (bar == null) continue;

                var sample = _runtimeSignalBars.Count > 1 ? i / (float)(_runtimeSignalBars.Count - 1) : 0f;
                var sampleFrequency = Mathf.Lerp(windowStart, windowEnd, sample);
                var signal = EvaluateSignalFunction(sampleFrequency);
                var currentMarker = Mathf.Clamp01(1f - Mathf.Abs(sampleFrequency - currentFrequencyMhz) / 0.22f);
                var rect = bar.rectTransform;
                var size = rect.sizeDelta;
                size.y = Mathf.Lerp(6f, 56f, signal);
                rect.sizeDelta = size;

                var intensity = Mathf.Clamp01(signal * 0.85f + currentMarker * 0.35f);
                var alpha = Mathf.Lerp(0.25f, 1f, Mathf.Max(signal, currentMarker * strength));
                bar.color = new Color(
                    Mathf.Lerp(0.34f, 0.66f, intensity),
                    Mathf.Lerp(0.52f, 0.9f, intensity),
                    Mathf.Lerp(0.26f, 0.42f, intensity),
                    alpha);
            }
        }

        private void EnsureRuntimeSignalBars()
        {
            ResolveTunerGraphBindings();
            if (tunerGraphContainer == null) return;

            var count = Mathf.Clamp(tunerGraphBarCount, 8, 96);
            if (_runtimeSignalBars.Count == count) return;

            ResetRuntimeSignalBars();
            HideSourceSignalBars();

            for (var i = 0; i < count; i++)
            {
                var bar = CreateRuntimeSignalBar(i, count);
                if (bar != null)
                {
                    _runtimeSignalBars.Add(bar);
                }
            }
        }

        private void ResolveTunerGraphBindings()
        {
            if (tunerGraphBarTemplate == null)
            {
                foreach (var bar in tunerSignalBars)
                {
                    if (bar != null)
                    {
                        tunerGraphBarTemplate = bar;
                        break;
                    }
                }
            }

            if (tunerGraphContainer == null && tunerGraphBarTemplate != null)
            {
                tunerGraphContainer = tunerGraphBarTemplate.transform.parent as RectTransform;
            }
        }

        private Image CreateRuntimeSignalBar(int index, int count)
        {
            GameObject barObject;
            if (tunerGraphBarTemplate != null)
            {
                barObject = Instantiate(tunerGraphBarTemplate.gameObject, tunerGraphContainer);
            }
            else
            {
                barObject = new GameObject("SignalGraphBar", typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
                barObject.transform.SetParent(tunerGraphContainer, false);
            }

            barObject.name = "SignalGraphBar_" + index.ToString("00", CultureInfo.InvariantCulture);
            barObject.SetActive(true);
            var rect = barObject.GetComponent<RectTransform>();
            if (rect != null)
            {
                var x = count > 1 ? index / (float)(count - 1) : 0f;
                rect.anchorMin = new Vector2(x, 0.5f);
                rect.anchorMax = new Vector2(x, 0.5f);
                rect.pivot = new Vector2(0.5f, 0.5f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(4f, 12f);
            }

            var image = barObject.GetComponent<Image>();
            if (image != null)
            {
                image.raycastTarget = false;
            }

            return image;
        }

        private void HideSourceSignalBars()
        {
            foreach (var bar in tunerSignalBars)
            {
                if (bar != null)
                {
                    bar.gameObject.SetActive(false);
                }
            }
        }

        private void ResetRuntimeSignalBars()
        {
            for (var i = _runtimeSignalBars.Count - 1; i >= 0; i--)
            {
                var bar = _runtimeSignalBars[i];
                if (bar != null)
                {
                    DestroySignalBar(bar.gameObject);
                }
            }

            _runtimeSignalBars.Clear();
        }

        private static void DestroySignalBar(GameObject gameObject)
        {
            if (gameObject == null) return;
            if (Application.isPlaying)
            {
                Destroy(gameObject);
            }
            else
            {
                DestroyImmediate(gameObject);
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

        private enum SignalStrengthTier
        {
            Bad,
            Normal,
            Good
        }
    }
}
