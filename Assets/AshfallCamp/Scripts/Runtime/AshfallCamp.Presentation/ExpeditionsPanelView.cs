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
    public sealed class ExpeditionsPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private List<RouteCardBinding> routeCards = new List<RouteCardBinding>();
        [SerializeField] private Image detailPanel;
        [SerializeField] private TextMeshProUGUI detailTitle;
        [SerializeField] private TextMeshProUGUI detailStats;
        [SerializeField] private TextMeshProUGUI detailLoot;
        [SerializeField] private TextMeshProUGUI detailEnemies;
        [SerializeField] private TextMeshProUGUI detailWarnings;
        [SerializeField] private Button launchButton;
        [SerializeField] private TextMeshProUGUI launchButtonLabel;
        [SerializeField] private Image monitorPanel;
        [SerializeField] private TextMeshProUGUI monitorTitle;
        [SerializeField] private TextMeshProUGUI monitorHeader;
        [SerializeField] private TextMeshProUGUI monitorProgress;
        [SerializeField] private TextMeshProUGUI monitorLoot;
        [SerializeField] private TextMeshProUGUI monitorNoise;
        [SerializeField] private TextMeshProUGUI monitorLog;

        private string _selectedZoneId = string.Empty;
        private string _selectedPolicyId = string.Empty;
        private Action<ExpeditionLaunchViewRequest> _launchRequested;
        private UnityAction _launchClick;

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            IEnumerable<RouteCardBinding> routes,
            Image selectedDetailPanel,
            TextMeshProUGUI selectedDetailTitle,
            TextMeshProUGUI selectedDetailStats,
            TextMeshProUGUI selectedDetailLoot,
            TextMeshProUGUI selectedDetailEnemies,
            TextMeshProUGUI selectedDetailWarnings,
            Button launchActionButton,
            TextMeshProUGUI launchActionLabel,
            Image activeMonitorPanel,
            TextMeshProUGUI activeMonitorTitle,
            TextMeshProUGUI activeMonitorHeader,
            TextMeshProUGUI activeMonitorProgress,
            TextMeshProUGUI activeMonitorLoot,
            TextMeshProUGUI activeMonitorNoise,
            TextMeshProUGUI activeMonitorLog)
        {
            title = titleLabel;
            routeCards.Clear();
            if (routes != null)
            {
                routeCards.AddRange(routes);
            }

            detailPanel = selectedDetailPanel;
            detailTitle = selectedDetailTitle;
            detailStats = selectedDetailStats;
            detailLoot = selectedDetailLoot;
            detailEnemies = selectedDetailEnemies;
            detailWarnings = selectedDetailWarnings;
            launchButton = launchActionButton;
            launchButtonLabel = launchActionLabel;
            monitorPanel = activeMonitorPanel;
            monitorTitle = activeMonitorTitle;
            monitorHeader = activeMonitorHeader;
            monitorProgress = activeMonitorProgress;
            monitorLoot = activeMonitorLoot;
            monitorNoise = activeMonitorNoise;
            monitorLog = activeMonitorLog;
            ApplyHandlers();
        }

        private void Awake()
        {
            ApplyHandlers();
        }

        private void OnDestroy()
        {
            ClearLaunchButton();
            foreach (var card in routeCards)
            {
                if (card != null)
                {
                    card.Clear();
                }
            }
        }

        public void SetLaunchHandler(Action<ExpeditionLaunchViewRequest> launchRequested)
        {
            _launchRequested = launchRequested;
            ApplyHandlers();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            var screen = CampDashboardTextFormatter.BuildExpeditionScreen(state, config, catalog, _selectedZoneId);
            _selectedZoneId = screen.SelectedZoneId;
            UiText.Set(title, screen.Title);

            for (var i = 0; i < routeCards.Count; i++)
            {
                routeCards[i].Render(i < screen.Routes.Count ? screen.Routes[i] : null, catalog);
            }

            RenderDetail(screen.Selected, catalog);
            RenderMonitor(screen.Monitor);
        }

        private void RenderDetail(CampExpeditionDetailPresentation detail, CampUiCatalogSO catalog)
        {
            UiText.SetActive(detailPanel, detail != null && !string.IsNullOrWhiteSpace(detail.ZoneId));
            if (detail == null || string.IsNullOrWhiteSpace(detail.ZoneId)) return;

            _selectedPolicyId = detail.PolicyId;
            UiText.Set(detailTitle, detail.Title);
            UiText.Set(detailStats, detail.Details);
            UiText.Set(detailLoot, detail.Loot);
            UiText.Set(detailEnemies, detail.Enemies);
            UiText.Set(detailWarnings, detail.Warnings);
            UiText.Set(launchButtonLabel, detail.LaunchButton);

            if (detailPanel != null)
            {
                detailPanel.color = new Color(catalog.Theme.Paper.r, catalog.Theme.Paper.g, catalog.Theme.Paper.b, 0.86f);
            }

            if (detailTitle != null) detailTitle.color = catalog.Theme.Ink;
            if (detailStats != null) detailStats.color = catalog.Theme.MutedInk;
            if (detailLoot != null) detailLoot.color = catalog.Theme.MutedInk;
            if (detailEnemies != null) detailEnemies.color = catalog.Theme.MutedInk;
            if (detailWarnings != null) detailWarnings.color = detail.CanLaunch ? catalog.Theme.Sage : catalog.Theme.Rust;
            if (launchButtonLabel != null) launchButtonLabel.color = catalog.Theme.Paper;
            if (launchButton != null) launchButton.interactable = detail.CanLaunch && _launchRequested != null;
        }

        private void RenderMonitor(CampExpeditionMonitorPresentation monitor)
        {
            UiText.SetActive(monitorPanel, monitor != null && monitor.HasActiveExpedition);
            if (monitor == null || !monitor.HasActiveExpedition) return;

            UiText.Set(monitorTitle, monitor.Title);
            UiText.Set(monitorHeader, monitor.Header);
            UiText.Set(monitorProgress, monitor.Progress);
            UiText.Set(monitorLoot, monitor.Loot);
            UiText.Set(monitorNoise, monitor.Noise);
            UiText.Set(monitorLog, monitor.Log);
        }

        private void ApplyHandlers()
        {
            foreach (var card in routeCards)
            {
                if (card != null)
                {
                    card.Wire(SelectRoute);
                }
            }

            WireLaunchButton();
        }

        private void SelectRoute(string zoneId)
        {
            _selectedZoneId = zoneId ?? string.Empty;
        }

        private void WireLaunchButton()
        {
            if (launchButton == null || _launchClick != null) return;
            _launchClick = OnLaunchClicked;
            launchButton.onClick.AddListener(_launchClick);
        }

        private void ClearLaunchButton()
        {
            if (launchButton != null && _launchClick != null)
            {
                launchButton.onClick.RemoveListener(_launchClick);
            }

            _launchClick = null;
        }

        private void OnLaunchClicked()
        {
            if (!string.IsNullOrWhiteSpace(_selectedZoneId))
            {
                _launchRequested?.Invoke(new ExpeditionLaunchViewRequest(_selectedZoneId, _selectedPolicyId));
            }
        }

        [Serializable]
        public sealed class RouteCardBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private Button button;
            [SerializeField] private TextMeshProUGUI title;
            [SerializeField] private TextMeshProUGUI subtitle;
            [SerializeField] private TextMeshProUGUI status;

            [NonSerialized] private string _zoneId = string.Empty;
            [NonSerialized] private Action<string> _selected;
            [NonSerialized] private UnityAction _click;

            public RouteCardBinding()
            {
            }

            public RouteCardBinding(Image panel, Button button, TextMeshProUGUI title, TextMeshProUGUI subtitle, TextMeshProUGUI status)
            {
                this.panel = panel;
                this.button = button;
                this.title = title;
                this.subtitle = subtitle;
                this.status = status;
            }

            public void Wire(Action<string> selected)
            {
                Clear();
                _selected = selected;
                if (button == null) return;
                _click = OnClicked;
                button.onClick.AddListener(_click);
            }

            public void Clear()
            {
                if (button != null && _click != null)
                {
                    button.onClick.RemoveListener(_click);
                }

                _click = null;
            }

            public void Render(CampExpeditionRoutePresentation route, CampUiCatalogSO catalog)
            {
                _zoneId = route != null ? route.ZoneId : string.Empty;
                UiText.SetActive(panel, route != null);
                if (route == null) return;

                UiText.Set(title, route.Title);
                UiText.Set(subtitle, route.Subtitle);
                UiText.Set(status, route.Status);

                if (panel != null)
                {
                    panel.color = route.IsSelected
                        ? new Color(catalog.Theme.Teal.r, catalog.Theme.Teal.g, catalog.Theme.Teal.b, 0.84f)
                        : new Color(catalog.Theme.PaperDark.r, catalog.Theme.PaperDark.g, catalog.Theme.PaperDark.b, route.CanSelect ? 0.72f : 0.38f);
                }

                var textColor = route.IsSelected ? catalog.Theme.Paper : catalog.Theme.Ink;
                if (title != null) title.color = textColor;
                if (subtitle != null) subtitle.color = route.IsSelected ? catalog.Theme.PaperDark : catalog.Theme.MutedInk;
                if (status != null) status.color = route.CanLaunch ? catalog.Theme.Sage : catalog.Theme.Rust;
                if (button != null) button.interactable = route.CanSelect && !route.IsSelected;
            }

            private void OnClicked()
            {
                if (!string.IsNullOrWhiteSpace(_zoneId))
                {
                    _selected?.Invoke(_zoneId);
                }
            }
        }
    }
}
