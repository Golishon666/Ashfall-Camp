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
        [SerializeField] private TextMeshProUGUI squadTitle;
        [SerializeField] private List<SquadMemberBinding> squadMembers = new List<SquadMemberBinding>();
        [SerializeField] private TextMeshProUGUI policyTitle;
        [SerializeField] private List<PolicyBinding> policies = new List<PolicyBinding>();
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

        private readonly List<string> _selectedSurvivorIds = new List<string>();
        private string _selectedZoneId = string.Empty;
        private string _selectedPolicyId = string.Empty;
        private bool _hasSquadSelection;
        private Action<ExpeditionLaunchViewRequest> _launchRequested;
        private UnityAction _launchClick;
        private GameState _lastState;
        private GameConfigSnapshot _lastConfig;
        private CampUiCatalogSO _lastCatalog;
        private CampExpeditionDetailPresentation _lastDetail;
        private bool _riskConfirmationPending;

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            IEnumerable<RouteCardBinding> routes,
            TextMeshProUGUI expeditionSquadTitle,
            IEnumerable<SquadMemberBinding> expeditionSquadMembers,
            TextMeshProUGUI expeditionPolicyTitle,
            IEnumerable<PolicyBinding> expeditionPolicies,
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

            squadTitle = expeditionSquadTitle;
            squadMembers.Clear();
            if (expeditionSquadMembers != null)
            {
                squadMembers.AddRange(expeditionSquadMembers);
            }

            policyTitle = expeditionPolicyTitle;
            policies.Clear();
            if (expeditionPolicies != null)
            {
                policies.AddRange(expeditionPolicies);
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

            foreach (var member in squadMembers)
            {
                if (member != null)
                {
                    member.Clear();
                }
            }

            foreach (var policy in policies)
            {
                if (policy != null)
                {
                    policy.Clear();
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

            _lastState = state;
            _lastConfig = config;
            _lastCatalog = catalog;

            var screen = CampDashboardTextFormatter.BuildExpeditionScreen(
                state,
                config,
                catalog,
                _selectedZoneId,
                _selectedPolicyId,
                _hasSquadSelection ? _selectedSurvivorIds : null,
                _riskConfirmationPending);

            _selectedZoneId = screen.SelectedZoneId;
            _selectedPolicyId = screen.SelectedPolicyId;
            SyncSelectedSurvivors(screen.SelectedSurvivorIds);

            UiText.Set(title, screen.Title);

            for (var i = 0; i < routeCards.Count; i++)
            {
                routeCards[i].Render(i < screen.Routes.Count ? screen.Routes[i] : null, catalog);
            }

            RenderSquad(screen, catalog);
            RenderPolicies(screen, catalog);
            RenderDetail(screen.Selected, catalog);
            RenderMonitor(screen.Monitor);
        }

        private void RenderSquad(CampExpeditionScreenPresentation screen, CampUiCatalogSO catalog)
        {
            UiText.Set(squadTitle, screen.SquadTitle);
            if (squadTitle != null) squadTitle.color = catalog.Theme.Ink;

            for (var i = 0; i < squadMembers.Count; i++)
            {
                squadMembers[i].Render(i < screen.SquadMembers.Count ? screen.SquadMembers[i] : null, catalog);
            }
        }

        private void RenderPolicies(CampExpeditionScreenPresentation screen, CampUiCatalogSO catalog)
        {
            UiText.Set(policyTitle, screen.PolicyTitle);
            if (policyTitle != null) policyTitle.color = catalog.Theme.Ink;

            for (var i = 0; i < policies.Count; i++)
            {
                policies[i].Render(i < screen.Policies.Count ? screen.Policies[i] : null, catalog);
            }
        }

        private void RenderDetail(CampExpeditionDetailPresentation detail, CampUiCatalogSO catalog)
        {
            _lastDetail = detail;
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

            foreach (var member in squadMembers)
            {
                if (member != null)
                {
                    member.Wire(ToggleSquadMember);
                }
            }

            foreach (var policy in policies)
            {
                if (policy != null)
                {
                    policy.Wire(SelectPolicy);
                }
            }

            WireLaunchButton();
        }

        private void SelectRoute(string zoneId)
        {
            _selectedZoneId = zoneId ?? string.Empty;
            ResetRiskConfirmation();
            RenderLast();
        }

        private void ToggleSquadMember(string survivorId)
        {
            if (string.IsNullOrWhiteSpace(survivorId)) return;

            _hasSquadSelection = true;
            var index = _selectedSurvivorIds.IndexOf(survivorId);
            if (index >= 0)
            {
                _selectedSurvivorIds.RemoveAt(index);
            }
            else if (_lastState == null || _selectedSurvivorIds.Count < Math.Max(1, _lastState.SquadSize))
            {
                _selectedSurvivorIds.Add(survivorId);
            }

            ResetRiskConfirmation();
            RenderLast();
        }

        private void SelectPolicy(string policyId)
        {
            _selectedPolicyId = policyId ?? string.Empty;
            ResetRiskConfirmation();
            RenderLast();
        }

        private void ResetRiskConfirmation()
        {
            _riskConfirmationPending = false;
        }

        private void RenderLast()
        {
            if (_lastState != null && _lastConfig != null && _lastCatalog != null)
            {
                Render(_lastState, _lastConfig, _lastCatalog);
            }
        }

        private void SyncSelectedSurvivors(IReadOnlyList<string> survivorIds)
        {
            _selectedSurvivorIds.Clear();
            if (survivorIds == null) return;

            for (var i = 0; i < survivorIds.Count; i++)
            {
                _selectedSurvivorIds.Add(survivorIds[i]);
            }
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
                if (_lastDetail != null && _lastDetail.RequiresRiskConfirmation && !_lastDetail.IsRiskConfirmationPending)
                {
                    _riskConfirmationPending = true;
                    RenderLast();
                    return;
                }

                var confirmWarnings = _lastDetail != null && _lastDetail.RequiresRiskConfirmation && _lastDetail.IsRiskConfirmationPending;
                _launchRequested?.Invoke(new ExpeditionLaunchViewRequest(_selectedZoneId, _selectedPolicyId, _selectedSurvivorIds, confirmWarnings));
                ResetRiskConfirmation();
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
                if (button != null) button.interactable = route != null && route.CanSelect && !route.IsSelected;
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
            }

            private void OnClicked()
            {
                if (!string.IsNullOrWhiteSpace(_zoneId))
                {
                    _selected?.Invoke(_zoneId);
                }
            }
        }

        [Serializable]
        public sealed class SquadMemberBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private Button button;
            [SerializeField] private TextMeshProUGUI title;
            [SerializeField] private TextMeshProUGUI meta;

            [NonSerialized] private string _survivorId = string.Empty;
            [NonSerialized] private Action<string> _selected;
            [NonSerialized] private UnityAction _click;

            public SquadMemberBinding()
            {
            }

            public SquadMemberBinding(Image panel, Button button, TextMeshProUGUI title, TextMeshProUGUI meta)
            {
                this.panel = panel;
                this.button = button;
                this.title = title;
                this.meta = meta;
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

            public void Render(CampExpeditionSquadMemberPresentation member, CampUiCatalogSO catalog)
            {
                _survivorId = member != null ? member.SurvivorId : string.Empty;
                UiText.SetActive(panel, member != null);
                if (button != null) button.interactable = member != null && member.CanSelect;
                if (member == null) return;

                UiText.Set(title, member.Name);
                UiText.Set(meta, member.Meta);

                if (panel != null)
                {
                    panel.color = member.IsSelected
                        ? new Color(catalog.Theme.Amber.r, catalog.Theme.Amber.g, catalog.Theme.Amber.b, 0.86f)
                        : new Color(catalog.Theme.PaperDark.r, catalog.Theme.PaperDark.g, catalog.Theme.PaperDark.b, member.CanSelect ? 0.66f : 0.32f);
                }

                if (title != null) title.color = member.IsSelected ? catalog.Theme.Ink : catalog.Theme.MutedInk;
                if (meta != null) meta.color = member.CanSelect ? catalog.Theme.MutedInk : catalog.Theme.Rust;
            }

            private void OnClicked()
            {
                if (!string.IsNullOrWhiteSpace(_survivorId))
                {
                    _selected?.Invoke(_survivorId);
                }
            }
        }

        [Serializable]
        public sealed class PolicyBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private Button button;
            [SerializeField] private TextMeshProUGUI title;
            [SerializeField] private TextMeshProUGUI details;

            [NonSerialized] private string _policyId = string.Empty;
            [NonSerialized] private Action<string> _selected;
            [NonSerialized] private UnityAction _click;

            public PolicyBinding()
            {
            }

            public PolicyBinding(Image panel, Button button, TextMeshProUGUI title, TextMeshProUGUI details)
            {
                this.panel = panel;
                this.button = button;
                this.title = title;
                this.details = details;
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

            public void Render(CampExpeditionPolicyPresentation policy, CampUiCatalogSO catalog)
            {
                _policyId = policy != null ? policy.PolicyId : string.Empty;
                UiText.SetActive(panel, policy != null);
                if (button != null) button.interactable = policy != null && policy.CanSelect && !policy.IsSelected;
                if (policy == null) return;

                UiText.Set(title, policy.Title);
                UiText.Set(details, policy.Details);

                if (panel != null)
                {
                    panel.color = policy.IsSelected
                        ? new Color(catalog.Theme.Teal.r, catalog.Theme.Teal.g, catalog.Theme.Teal.b, 0.82f)
                        : new Color(catalog.Theme.PaperDark.r, catalog.Theme.PaperDark.g, catalog.Theme.PaperDark.b, 0.62f);
                }

                if (title != null) title.color = policy.IsSelected ? catalog.Theme.Paper : catalog.Theme.Ink;
                if (details != null) details.color = policy.IsSelected ? catalog.Theme.PaperDark : catalog.Theme.MutedInk;
            }

            private void OnClicked()
            {
                if (!string.IsNullOrWhiteSpace(_policyId))
                {
                    _selected?.Invoke(_policyId);
                }
            }
        }
    }
}
