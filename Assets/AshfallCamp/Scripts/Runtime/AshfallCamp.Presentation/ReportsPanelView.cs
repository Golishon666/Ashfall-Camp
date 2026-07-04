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
        [SerializeField] private TextMeshProUGUI emptyTitle;
        [SerializeField] private TextMeshProUGUI emptyBody;
        [SerializeField] private Image afterActionPanel;
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
        [SerializeField] private TextMeshProUGUI campEventPanelTitle;
        [SerializeField] private TextMeshProUGUI campEventTitle;
        [SerializeField] private TextMeshProUGUI campEventBody;
        [SerializeField] private Image offlinePanel;
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
            title = titleLabel;
            emptyPanel = emptyStatePanel;
            emptyTitle = emptyStateTitle;
            emptyBody = emptyStateBody;
            afterActionPanel = afterActionStatePanel;
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
            campEventPanelTitle = campEventStatePanelTitle;
            campEventTitle = campEventStateTitle;
            campEventBody = campEventStateBody;
            offlinePanel = offlineStatePanel;
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
            ApplySendAgainButtonState();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            var report = CampDashboardTextFormatter.BuildReports(state, config, catalog);
            UiText.Set(title, report.Title);
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
    }
}
