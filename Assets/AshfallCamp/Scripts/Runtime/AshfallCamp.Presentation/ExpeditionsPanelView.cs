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
        [SerializeField] private List<RouteMapNodeBinding> mapNodes = new List<RouteMapNodeBinding>();
        [SerializeField] private List<RouteCardBinding> routeCards = new List<RouteCardBinding>();
        [SerializeField] private TextMeshProUGUI squadTitle;
        [SerializeField] private List<SquadMemberBinding> squadMembers = new List<SquadMemberBinding>();
        [SerializeField] private TextMeshProUGUI policyTitle;
        [SerializeField] private List<PolicyBinding> policies = new List<PolicyBinding>();
        [SerializeField] private Image detailPanel;
        [SerializeField] private RawImage detailPanelArtwork;
        [SerializeField] private TextMeshProUGUI detailTitle;
        [SerializeField] private TextMeshProUGUI detailStats;
        [SerializeField] private TextMeshProUGUI detailLoot;
        [SerializeField] private TextMeshProUGUI detailEnemies;
        [SerializeField] private TextMeshProUGUI detailWarnings;
        [SerializeField] private Button launchButton;
        [SerializeField] private TextMeshProUGUI launchButtonLabel;
        [SerializeField] private Image monitorPanel;
        [SerializeField] private RawImage monitorPanelArtwork;
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
            IEnumerable<RouteMapNodeBinding> routeMapNodes,
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
            ConfigureBindings(
                titleLabel,
                routeMapNodes,
                routes,
                expeditionSquadTitle,
                expeditionSquadMembers,
                expeditionPolicyTitle,
                expeditionPolicies,
                selectedDetailPanel,
                selectedDetailTitle,
                selectedDetailStats,
                selectedDetailLoot,
                selectedDetailEnemies,
                selectedDetailWarnings,
                launchActionButton,
                launchActionLabel,
                activeMonitorPanel,
                activeMonitorTitle,
                activeMonitorHeader,
                activeMonitorProgress,
                activeMonitorLoot,
                activeMonitorNoise,
                activeMonitorLog,
                null,
                null);
        }

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            IEnumerable<RouteMapNodeBinding> routeMapNodes,
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
            TextMeshProUGUI activeMonitorLog,
            RawImage selectedDetailArtwork,
            RawImage activeMonitorArtwork)
        {
            title = titleLabel;
            mapNodes.Clear();
            if (routeMapNodes != null)
            {
                mapNodes.AddRange(routeMapNodes);
            }

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
            detailPanelArtwork = selectedDetailArtwork;
            detailTitle = selectedDetailTitle;
            detailStats = selectedDetailStats;
            detailLoot = selectedDetailLoot;
            detailEnemies = selectedDetailEnemies;
            detailWarnings = selectedDetailWarnings;
            launchButton = launchActionButton;
            launchButtonLabel = launchActionLabel;
            monitorPanel = activeMonitorPanel;
            monitorPanelArtwork = activeMonitorArtwork;
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

            foreach (var node in mapNodes)
            {
                if (node != null)
                {
                    node.Clear();
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

            RenderMapNodes(screen, catalog);
            RenderSquad(screen, catalog);
            RenderPolicies(screen, catalog);
            RenderDetail(screen.Selected, catalog);
            RenderMonitor(screen.Monitor, catalog);
        }

        private void RenderMapNodes(CampExpeditionScreenPresentation screen, CampUiCatalogSO catalog)
        {
            for (var i = 0; i < mapNodes.Count; i++)
            {
                mapNodes[i].Render(i < screen.Routes.Count ? screen.Routes[i] : null, catalog);
            }
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

            ApplyArtwork(detailPanelArtwork, catalog.ExpeditionDetailPanelTexture);
            _selectedPolicyId = detail.PolicyId;
            UiText.Set(detailTitle, detail.Title);
            UiText.Set(detailStats, detail.Details);
            UiText.Set(detailLoot, detail.Loot);
            UiText.Set(detailEnemies, detail.Enemies);
            UiText.Set(detailWarnings, detail.Warnings);
            UiText.Set(launchButtonLabel, detail.LaunchButton);

            if (detailPanel != null)
            {
                detailPanel.color = catalog.Theme.WithAlpha(catalog.Theme.Paper, catalog.Theme.ExpeditionDetailPanelAlpha);
            }

            if (detailTitle != null) detailTitle.color = catalog.Theme.Ink;
            if (detailStats != null) detailStats.color = catalog.Theme.MutedInk;
            if (detailLoot != null) detailLoot.color = catalog.Theme.MutedInk;
            if (detailEnemies != null) detailEnemies.color = catalog.Theme.MutedInk;
            if (detailWarnings != null) detailWarnings.color = detail.CanLaunch ? catalog.Theme.Sage : catalog.Theme.Rust;
            if (launchButtonLabel != null) launchButtonLabel.color = catalog.Theme.Paper;
            if (launchButton != null) launchButton.interactable = detail.CanLaunch && _launchRequested != null;
        }

        private void RenderMonitor(CampExpeditionMonitorPresentation monitor, CampUiCatalogSO catalog)
        {
            UiText.SetActive(monitorPanel, monitor != null && monitor.HasActiveExpedition);
            if (monitor == null || !monitor.HasActiveExpedition) return;

            ApplyArtwork(monitorPanelArtwork, catalog.ExpeditionMonitorPanelTexture);
            UiText.Set(monitorTitle, monitor.Title);
            UiText.Set(monitorHeader, monitor.Header);
            UiText.Set(monitorProgress, monitor.Progress);
            UiText.Set(monitorLoot, monitor.Loot);
            UiText.Set(monitorNoise, monitor.Noise);
            UiText.Set(monitorLog, monitor.Log);
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

        private void ApplyHandlers()
        {
            foreach (var card in routeCards)
            {
                if (card != null)
                {
                    card.Wire(SelectRoute);
                }
            }

            foreach (var node in mapNodes)
            {
                if (node != null)
                {
                    node.Wire(SelectRoute);
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
        public sealed class RouteMapNodeBinding
        {
            [SerializeField] private Image node;
            [SerializeField] private Button button;
            [SerializeField] private TextMeshProUGUI label;

            [NonSerialized] private string _zoneId = string.Empty;
            [NonSerialized] private Action<string> _selected;
            [NonSerialized] private UnityAction _click;

            public RouteMapNodeBinding()
            {
            }

            public RouteMapNodeBinding(Image nodeImage, Button nodeButton, TextMeshProUGUI nodeLabel)
            {
                node = nodeImage;
                button = nodeButton;
                label = nodeLabel;
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
                UiText.SetActive(node, route != null);
                if (button != null) button.interactable = route != null && route.CanSelect && !route.IsSelected;
                if (route == null) return;

                UiText.Set(label, route.Title);

                if (node != null)
                {
                    node.color = route.IsSelected
                        ? catalog.Theme.WithAlpha(catalog.Theme.Teal, catalog.Theme.ExpeditionRouteSelectedPanelAlpha)
                        : catalog.Theme.WithAlpha(catalog.Theme.PaperDark, route.CanSelect ? catalog.Theme.ExpeditionRouteAvailablePanelAlpha : catalog.Theme.ExpeditionRouteBlockedPanelAlpha);
                }

                if (label != null)
                {
                    label.color = route.IsSelected ? catalog.Theme.Paper : route.CanSelect ? catalog.Theme.Ink : catalog.Theme.Rust;
                }
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
        public sealed class RouteCardBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private RawImage thumbnail;
            [SerializeField] private RawImage riskBadge;
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
                if (route == null)
                {
                    ApplyArtwork(thumbnail, null);
                    ApplyArtwork(riskBadge, null);
                    return;
                }

                UiText.Set(title, route.Title);
                UiText.Set(subtitle, route.Subtitle);
                UiText.Set(status, route.Status);
                ApplyArtwork(thumbnail, route.Thumbnail);
                ApplyArtwork(riskBadge, route.RiskBadge);

                if (panel != null)
                {
                    panel.color = route.IsSelected
                        ? catalog.Theme.WithAlpha(catalog.Theme.Teal, catalog.Theme.ExpeditionRouteSelectedPanelAlpha)
                        : catalog.Theme.WithAlpha(catalog.Theme.PaperDark, route.CanSelect ? catalog.Theme.ExpeditionRouteAvailablePanelAlpha : catalog.Theme.ExpeditionRouteBlockedPanelAlpha);
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
            [SerializeField] private RawImage cardArtwork;
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
                if (member == null)
                {
                    ApplyArtwork(cardArtwork, null);
                    return;
                }

                UiText.Set(title, member.Name);
                UiText.Set(meta, member.Meta);
                ApplyArtwork(cardArtwork, catalog.ExpeditionSquadMemberCardTexture);

                if (panel != null)
                {
                    panel.color = member.IsSelected
                        ? catalog.Theme.WithAlpha(catalog.Theme.Amber, catalog.Theme.ExpeditionSquadSelectedPanelAlpha)
                        : catalog.Theme.WithAlpha(catalog.Theme.PaperDark, member.CanSelect ? catalog.Theme.ExpeditionSquadAvailablePanelAlpha : catalog.Theme.ExpeditionSquadBlockedPanelAlpha);
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
            [SerializeField] private RawImage cardArtwork;
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
                if (policy == null)
                {
                    ApplyArtwork(cardArtwork, null);
                    return;
                }

                UiText.Set(title, policy.Title);
                UiText.Set(details, policy.Details);
                ApplyArtwork(cardArtwork, catalog.ExpeditionPolicyCardTexture);

                if (panel != null)
                {
                    panel.color = policy.IsSelected
                        ? catalog.Theme.WithAlpha(catalog.Theme.Teal, catalog.Theme.ExpeditionPolicySelectedPanelAlpha)
                        : catalog.Theme.WithAlpha(catalog.Theme.PaperDark, catalog.Theme.ExpeditionPolicyInactivePanelAlpha);
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
