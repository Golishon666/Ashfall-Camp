using System;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ReportsPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private Image emptyPanel;
        [SerializeField] private RawImage emptyPanelArtwork;
        [SerializeField] private TextMeshProUGUI emptyTitle;
        [SerializeField] private TextMeshProUGUI emptyBody;
        [SerializeField] private Image afterActionPanel;
        [SerializeField] private RawImage afterActionPanelArtwork;
        [SerializeField] private TextMeshProUGUI afterActionTitle;
        [SerializeField] private TextMeshProUGUI afterActionOutcome;
        [SerializeField] private TextMeshProUGUI afterActionLoot;
        [SerializeField] private TextMeshProUGUI afterActionXp;
        [SerializeField] private TextMeshProUGUI afterActionWounds;
        [SerializeField] private TextMeshProUGUI afterActionEnemies;
        [SerializeField] private TextMeshProUGUI afterActionEvents;
        [SerializeField] private Button afterActionSendAgainButton;
        [SerializeField] private TextMeshProUGUI afterActionSendAgainButtonLabel;
        [SerializeField] private Image campEventPanel;
        [SerializeField] private RawImage campEventPanelArtwork;
        [SerializeField] private TextMeshProUGUI campEventPanelTitle;
        [SerializeField] private TextMeshProUGUI campEventTitle;
        [SerializeField] private TextMeshProUGUI campEventBody;
        [SerializeField] private Image offlinePanel;
        [SerializeField] private RawImage offlinePanelArtwork;
        [SerializeField] private TextMeshProUGUI offlineTitle;
        [SerializeField] private TextMeshProUGUI offlineSummary;
        [SerializeField] private TextMeshProUGUI offlineResources;
        [SerializeField] private TextMeshProUGUI offlineCompleted;
        [SerializeField] private TextMeshProUGUI offlineHealing;
        [SerializeField] private TextMeshProUGUI offlineWarnings;

        private Action<ExpeditionLaunchViewRequest> _sendAgainRequested;
        private UnityAction _sendAgainClick;
        private ExpeditionLaunchViewRequest _afterActionSendAgainRequest;
        private bool _afterActionCanSendAgain;
        private RectTransform _afterActionPanelRect;
        private RectTransform _campEventPanelRect;
        private Vector2 _afterActionDefaultPosition;
        private Vector2 _campEventDefaultPosition;
        private bool _layoutDefaultsCached;

        private const float ReportPanelStackGap = 14f;

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            Image emptyStatePanel,
            TextMeshProUGUI emptyStateTitle,
            TextMeshProUGUI emptyStateBody,
            Image afterActionStatePanel,
            TextMeshProUGUI afterActionStateTitle,
            TextMeshProUGUI afterActionStateOutcome,
            TextMeshProUGUI afterActionStateLoot,
            TextMeshProUGUI afterActionStateXp,
            TextMeshProUGUI afterActionStateWounds,
            TextMeshProUGUI afterActionStateEnemies,
            TextMeshProUGUI afterActionStateEvents,
            Button afterActionStateSendAgainButton,
            TextMeshProUGUI afterActionStateSendAgainButtonLabel,
            Image campEventStatePanel,
            TextMeshProUGUI campEventStatePanelTitle,
            TextMeshProUGUI campEventStateTitle,
            TextMeshProUGUI campEventStateBody,
            Image offlineStatePanel,
            TextMeshProUGUI offlineStateTitle,
            TextMeshProUGUI offlineStateSummary,
            TextMeshProUGUI offlineStateResources,
            TextMeshProUGUI offlineStateCompleted,
            TextMeshProUGUI offlineStateHealing,
            TextMeshProUGUI offlineStateWarnings)
        {
            ConfigureBindings(
                titleLabel,
                emptyStatePanel,
                emptyStateTitle,
                emptyStateBody,
                afterActionStatePanel,
                afterActionStateTitle,
                afterActionStateOutcome,
                afterActionStateLoot,
                afterActionStateXp,
                afterActionStateWounds,
                afterActionStateEnemies,
                afterActionStateEvents,
                afterActionStateSendAgainButton,
                afterActionStateSendAgainButtonLabel,
                campEventStatePanel,
                campEventStatePanelTitle,
                campEventStateTitle,
                campEventStateBody,
                offlineStatePanel,
                offlineStateTitle,
                offlineStateSummary,
                offlineStateResources,
                offlineStateCompleted,
                offlineStateHealing,
                offlineStateWarnings,
                null,
                null,
                null,
                null);
        }

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            Image emptyStatePanel,
            TextMeshProUGUI emptyStateTitle,
            TextMeshProUGUI emptyStateBody,
            Image afterActionStatePanel,
            TextMeshProUGUI afterActionStateTitle,
            TextMeshProUGUI afterActionStateOutcome,
            TextMeshProUGUI afterActionStateLoot,
            TextMeshProUGUI afterActionStateXp,
            TextMeshProUGUI afterActionStateWounds,
            TextMeshProUGUI afterActionStateEnemies,
            TextMeshProUGUI afterActionStateEvents,
            Button afterActionStateSendAgainButton,
            TextMeshProUGUI afterActionStateSendAgainButtonLabel,
            Image campEventStatePanel,
            TextMeshProUGUI campEventStatePanelTitle,
            TextMeshProUGUI campEventStateTitle,
            TextMeshProUGUI campEventStateBody,
            Image offlineStatePanel,
            TextMeshProUGUI offlineStateTitle,
            TextMeshProUGUI offlineStateSummary,
            TextMeshProUGUI offlineStateResources,
            TextMeshProUGUI offlineStateCompleted,
            TextMeshProUGUI offlineStateHealing,
            TextMeshProUGUI offlineStateWarnings,
            RawImage emptyStateArtwork,
            RawImage afterActionStateArtwork,
            RawImage campEventStateArtwork,
            RawImage offlineStateArtwork)
        {
            title = titleLabel;
            emptyPanel = emptyStatePanel;
            emptyPanelArtwork = emptyStateArtwork;
            emptyTitle = emptyStateTitle;
            emptyBody = emptyStateBody;
            afterActionPanel = afterActionStatePanel;
            afterActionPanelArtwork = afterActionStateArtwork;
            afterActionTitle = afterActionStateTitle;
            afterActionOutcome = afterActionStateOutcome;
            afterActionLoot = afterActionStateLoot;
            afterActionXp = afterActionStateXp;
            afterActionWounds = afterActionStateWounds;
            afterActionEnemies = afterActionStateEnemies;
            afterActionEvents = afterActionStateEvents;
            afterActionSendAgainButton = afterActionStateSendAgainButton;
            afterActionSendAgainButtonLabel = afterActionStateSendAgainButtonLabel;
            campEventPanel = campEventStatePanel;
            campEventPanelArtwork = campEventStateArtwork;
            campEventPanelTitle = campEventStatePanelTitle;
            campEventTitle = campEventStateTitle;
            campEventBody = campEventStateBody;
            offlinePanel = offlineStatePanel;
            offlinePanelArtwork = offlineStateArtwork;
            offlineTitle = offlineStateTitle;
            offlineSummary = offlineStateSummary;
            offlineResources = offlineStateResources;
            offlineCompleted = offlineStateCompleted;
            offlineHealing = offlineStateHealing;
            offlineWarnings = offlineStateWarnings;
            WireSendAgainButton();
        }

        private void Awake()
        {
            WireSendAgainButton();
        }

        private void OnDestroy()
        {
            ClearSendAgainButton();
        }

        public void SetSendAgainHandler(Action<ExpeditionLaunchViewRequest> sendAgainRequested)
        {
            _sendAgainRequested = sendAgainRequested;
            WireSendAgainButton();
            ApplySendAgainButtonState();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            var report = CampDashboardTextFormatter.BuildReports(state, config, catalog);
            UiText.Set(title, report.Title);
            ApplyArtwork(emptyPanelArtwork, catalog.ReportsEmptyPanelTexture);
            ApplyArtwork(afterActionPanelArtwork, catalog.ReportsAfterActionPanelTexture);
            ApplyArtwork(campEventPanelArtwork, catalog.ReportsCampEventPanelTexture);
            ApplyArtwork(offlinePanelArtwork, catalog.ReportsOfflinePanelTexture);
            UiText.SetActive(emptyPanel, !report.HasAnyReport);
            UiText.Set(emptyTitle, report.EmptyTitle);
            UiText.Set(emptyBody, report.EmptyBody);

            UiText.SetActive(afterActionPanel, report.HasAfterAction);
            if (report.HasAfterAction)
            {
                UiText.Set(afterActionTitle, report.AfterActionTitle);
                UiText.Set(afterActionOutcome, report.AfterActionOutcome);
                UiText.Set(afterActionLoot, report.AfterActionLoot);
                UiText.Set(afterActionXp, report.AfterActionXp);
                UiText.Set(afterActionWounds, report.AfterActionWounds);
                UiText.Set(afterActionEnemies, report.AfterActionEnemies);
                UiText.Set(afterActionEvents, report.AfterActionEvents);
                UiText.Set(afterActionSendAgainButtonLabel, report.AfterActionSendAgainButton);
                _afterActionSendAgainRequest = report.AfterActionSendAgainRequest;
                _afterActionCanSendAgain = report.AfterActionCanSendAgain;
                ApplySendAgainButtonState();
            }
            else
            {
                _afterActionSendAgainRequest = null;
                _afterActionCanSendAgain = false;
                ApplySendAgainButtonState();
            }

            UiText.SetActive(campEventPanel, report.HasCampEvent);
            if (report.HasCampEvent)
            {
                UiText.Set(campEventPanelTitle, report.CampEventPanelTitle);
                UiText.Set(campEventTitle, report.CampEventTitle);
                UiText.Set(campEventBody, report.CampEventBody);
            }

            UiText.SetActive(offlinePanel, report.HasOfflineReport);
            if (report.HasOfflineReport)
            {
                UiText.Set(offlineTitle, report.OfflineReportTitle);
                UiText.Set(offlineSummary, report.OfflineSummary);
                UiText.Set(offlineResources, report.OfflineResources);
                UiText.Set(offlineCompleted, report.OfflineCompleted);
                UiText.Set(offlineHealing, report.OfflineHealing);
                UiText.Set(offlineWarnings, report.OfflineWarnings);
            }

            ApplyPanelLayout(report.HasAfterAction, report.HasCampEvent);
        }

        private void WireSendAgainButton()
        {
            if (afterActionSendAgainButton == null || _sendAgainClick != null) return;
            _sendAgainClick = OnSendAgainClicked;
            afterActionSendAgainButton.onClick.AddListener(_sendAgainClick);
        }

        private void ClearSendAgainButton()
        {
            if (afterActionSendAgainButton != null && _sendAgainClick != null)
            {
                afterActionSendAgainButton.onClick.RemoveListener(_sendAgainClick);
            }

            _sendAgainClick = null;
        }

        private void ApplySendAgainButtonState()
        {
            if (afterActionSendAgainButton != null)
            {
                afterActionSendAgainButton.interactable = _afterActionCanSendAgain && _sendAgainRequested != null && _afterActionSendAgainRequest != null;
            }
        }

        private void OnSendAgainClicked()
        {
            if (_afterActionSendAgainRequest == null) return;
            _sendAgainRequested?.Invoke(_afterActionSendAgainRequest);
        }

        private static void ApplyArtwork(RawImage target, Texture2D texture)
        {
            if (target == null) return;
            var hasTexture = texture != null;
            target.gameObject.SetActive(hasTexture);
            if (hasTexture)
            {
                target.texture = texture;
                target.color = Color.white;
            }
        }

        private void ApplyPanelLayout(bool hasAfterAction, bool hasCampEvent)
        {
            CacheLayoutDefaults();

            if (_campEventPanelRect != null)
            {
                _campEventPanelRect.anchoredPosition = _campEventDefaultPosition;
            }

            if (_afterActionPanelRect == null) return;
            if (hasAfterAction && hasCampEvent && _campEventPanelRect != null)
            {
                var eventHeight = _campEventPanelRect.rect.height > 0f
                    ? _campEventPanelRect.rect.height
                    : _campEventPanelRect.sizeDelta.y;
                _afterActionPanelRect.anchoredPosition = new Vector2(
                    _afterActionDefaultPosition.x,
                    _campEventDefaultPosition.y - eventHeight - ReportPanelStackGap);
                return;
            }

            _afterActionPanelRect.anchoredPosition = _afterActionDefaultPosition;
        }

        private void CacheLayoutDefaults()
        {
            if (_layoutDefaultsCached) return;

            _afterActionPanelRect = afterActionPanel != null ? afterActionPanel.rectTransform : null;
            _campEventPanelRect = campEventPanel != null ? campEventPanel.rectTransform : null;
            if (_afterActionPanelRect != null)
            {
                _afterActionDefaultPosition = _afterActionPanelRect.anchoredPosition;
            }

            if (_campEventPanelRect != null)
            {
                _campEventDefaultPosition = _campEventPanelRect.anchoredPosition;
            }

            _layoutDefaultsCached = true;
        }
    }
}
