using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
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
        [SerializeField] private Image offlinePanel;
        [SerializeField] private TextMeshProUGUI offlineTitle;
        [SerializeField] private TextMeshProUGUI offlineSummary;
        [SerializeField] private TextMeshProUGUI offlineResources;
        [SerializeField] private TextMeshProUGUI offlineCompleted;
        [SerializeField] private TextMeshProUGUI offlineHealing;
        [SerializeField] private TextMeshProUGUI offlineWarnings;

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
            offlinePanel = offlineStatePanel;
            offlineTitle = offlineStateTitle;
            offlineSummary = offlineStateSummary;
            offlineResources = offlineStateResources;
            offlineCompleted = offlineStateCompleted;
            offlineHealing = offlineStateHealing;
            offlineWarnings = offlineStateWarnings;
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
    }
}
