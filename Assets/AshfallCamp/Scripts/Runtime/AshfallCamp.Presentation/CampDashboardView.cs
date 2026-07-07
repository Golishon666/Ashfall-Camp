using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using DG.Tweening;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CampDashboardView : MonoBehaviour, IUiRootView
    {
        private const string DashboardScreenId = "camp";
        private const string LegacyDashboardScreenId = "buildings";

        [SerializeField] private Transform root;
        [SerializeField] private CampUiCatalogSO catalog;
        [SerializeField] private CampHeaderView header;
        [SerializeField] private ResourceBarView resourceBar;
        [SerializeField] private CampStatusPanelView statusPanel;
        [SerializeField] private CampSummaryPanelView summaryPanel;
        [SerializeField] private CampAlertsPanelView alertsPanel;
        [SerializeField] private BuildingGridView buildingGrid;
        [SerializeField] private SurvivorsPanelView survivorsPanel;
        [SerializeField] private ExpeditionsPanelView expeditionsPanel;
        [SerializeField] private WorkshopPanelView workshopPanel;
        [SerializeField] private ReportsPanelView reportsPanel;
        [SerializeField] private RadioPanelView radioPanel;
        [SerializeField] private SettingsPanelView settingsPanel;
        [SerializeField] private ToastPanelView toastPanel;
        [SerializeField] private CampRightColumnView rightColumn;
        [SerializeField] private BottomNavView bottomNav;
        [SerializeField] private List<ScreenBinding> screens = new List<ScreenBinding>();

        private bool _isBound;
        private bool _missingCatalogLogged;
        private bool _invalidCatalogLogged;
        private string _activeScreenId = string.Empty;
        private string _appliedScreenId = string.Empty;
        private GameState _lastState;
        private GameConfigSnapshot _lastConfig;

        public event Action<string> UpgradeRequested;
        public event Action<ExpeditionLaunchViewRequest> ExpeditionLaunchRequested;
        public event Action BroadcastRecruitmentRequested;
        public event Action<RecruitSurvivorViewRequest> RecruitRequested;
        public event Action RecruitmentCandidatesSkipRequested;
        public event Action<RepairItemRequest> RepairItemRequested;
        public event Action<EquipItemRequest> EquipItemRequested;
        public event Action<UseMedicineRequest> UseMedicineRequested;
        public event Action<StartRestRequest> StartRestRequested;
        public event Action<StopRestRequest> StopRestRequested;
        public event Action EmergencyScavengeRequested;
        public event Action<bool> AutosaveChanged;
        public event Action ManualSaveRequested;

        public Transform Root
        {
            get { return root != null ? root : transform; }
        }

        private void Awake()
        {
            EnsureBound();
        }

        public void SetCatalog(CampUiCatalogSO uiCatalog)
        {
            catalog = uiCatalog;
            _isBound = false;
            _invalidCatalogLogged = false;
        }

        public void ConfigureReferences(
            CampUiCatalogSO uiCatalog,
            CampHeaderView headerView,
            ResourceBarView resourceBarView,
            CampStatusPanelView statusPanelView,
            CampSummaryPanelView summaryPanelView,
            CampAlertsPanelView alertsPanelView,
            BuildingGridView buildingGridView,
            CampRightColumnView rightColumnView,
            BottomNavView bottomNavView,
            ExpeditionsPanelView expeditionsPanelView = null,
            WorkshopPanelView workshopPanelView = null,
            ReportsPanelView reportsPanelView = null,
            RadioPanelView radioPanelView = null,
            SettingsPanelView settingsPanelView = null,
            ToastPanelView toastPanelView = null)
        {
            catalog = uiCatalog;
            header = headerView;
            resourceBar = resourceBarView;
            statusPanel = statusPanelView;
            summaryPanel = summaryPanelView;
            alertsPanel = alertsPanelView;
            buildingGrid = buildingGridView;
            if (expeditionsPanelView != null)
            {
                expeditionsPanel = expeditionsPanelView;
            }

            if (workshopPanelView != null)
            {
                workshopPanel = workshopPanelView;
            }

            if (reportsPanelView != null)
            {
                reportsPanel = reportsPanelView;
            }

            if (radioPanelView != null)
            {
                radioPanel = radioPanelView;
            }

            if (settingsPanelView != null)
            {
                settingsPanel = settingsPanelView;
            }

            if (toastPanelView != null)
            {
                toastPanel = toastPanelView;
            }

            rightColumn = rightColumnView;
            bottomNav = bottomNavView;
            _isBound = false;
            _invalidCatalogLogged = false;
            EnsureBound();
        }

        public void Render(GameState state, GameConfigSnapshot config)
        {
            if (state == null || config == null) return;
            if (!EnsureBound()) return;

            header.Render(catalog);
            resourceBar.Render(state, config, catalog);
            statusPanel.Render(state, config, catalog);
            summaryPanel.Render(state, config, catalog);
            alertsPanel.Render(state, config, catalog);
            buildingGrid.Render(state, config, catalog);
            if (survivorsPanel != null)
            {
                survivorsPanel.Render(state, config, catalog);
            }

            if (workshopPanel != null)
            {
                workshopPanel.Render(state, config, catalog);
            }

            if (expeditionsPanel != null)
            {
                expeditionsPanel.Render(state, config, catalog);
            }

            if (reportsPanel != null)
            {
                reportsPanel.Render(state, config, catalog);
            }

            if (radioPanel != null)
            {
                radioPanel.Render(state, config, catalog);
            }

            if (settingsPanel != null)
            {
                settingsPanel.Render(state, config, catalog);
            }

            rightColumn.Render(state, config, catalog);
            EnsureActiveScreenId();
            ApplyScreenVisibility();
            bottomNav.Render(catalog, _activeScreenId);

            _lastState = state;
            _lastConfig = config;
        }

        public void ShowToast(CampToastRequest request)
        {
            if (toastPanel == null || catalog == null || request == null) return;
            toastPanel.Show(request, catalog);
        }

        public void OpenReports()
        {
            if (!EnsureBound() || catalog == null) return;
            OpenScreen(catalog.ReportsScreenId);
        }

        private bool EnsureBound()
        {
            if (_isBound) return true;
            if (catalog == null)
            {
                if (!_missingCatalogLogged)
                {
                    Debug.LogError("CampDashboardView requires a CampUiCatalogSO reference.", this);
                    _missingCatalogLogged = true;
                }

                return false;
            }

            var validation = CampUiCatalogValidator.Validate(catalog);
            if (!validation.IsValid)
            {
                if (!_invalidCatalogLogged)
                {
                    Debug.LogError("CampDashboardView has an invalid CampUiCatalogSO: " + string.Join("; ", validation.Errors), this);
                    _invalidCatalogLogged = true;
                }

                return false;
            }

            if (header == null || resourceBar == null || statusPanel == null || summaryPanel == null ||
                alertsPanel == null || buildingGrid == null || rightColumn == null || bottomNav == null)
            {
                Debug.LogError("CampDashboardView requires all dashboard subviews.", this);
                return false;
            }

            buildingGrid.SetUpgradeHandler(id => UpgradeRequested?.Invoke(id));
            alertsPanel.SetEmergencyScavengeHandler(() => EmergencyScavengeRequested?.Invoke());
            if (expeditionsPanel != null)
            {
                expeditionsPanel.SetLaunchHandler(request => ExpeditionLaunchRequested?.Invoke(request));
            }

            if (radioPanel != null)
            {
                radioPanel.SetBroadcastHandler(() => BroadcastRecruitmentRequested?.Invoke());
                radioPanel.SetRecruitHandler(request => RecruitRequested?.Invoke(request));
                radioPanel.SetSkipHandler(() => RecruitmentCandidatesSkipRequested?.Invoke());
            }

            if (workshopPanel != null)
            {
                workshopPanel.SetRepairHandler(request => RepairItemRequested?.Invoke(request));
                workshopPanel.SetEquipHandler(request => EquipItemRequested?.Invoke(request));
            }

            if (survivorsPanel != null)
            {
                survivorsPanel.SetUseMedicineHandler(request => UseMedicineRequested?.Invoke(request));
                survivorsPanel.SetRestHandlers(
                    request => StartRestRequested?.Invoke(request),
                    request => StopRestRequested?.Invoke(request));
            }

            if (settingsPanel != null)
            {
                settingsPanel.SetAutosaveChangedHandler(enabled => AutosaveChanged?.Invoke(enabled));
                settingsPanel.SetManualSaveHandler(() => ManualSaveRequested?.Invoke());
            }

            if (reportsPanel != null)
            {
                reportsPanel.SetSendAgainHandler(request => ExpeditionLaunchRequested?.Invoke(request));
            }

            rightColumn.SetExpeditionLaunchHandler(request => ExpeditionLaunchRequested?.Invoke(request));
            rightColumn.SetBroadcastHandler(() => BroadcastRecruitmentRequested?.Invoke());
            bottomNav.SetSelectionHandler(OnScreenSelected);
            _isBound = true;
            return true;
        }

        private void OnScreenSelected(string screenId)
        {
            OpenScreen(screenId);
        }

        private void OpenScreen(string screenId)
        {
            if (!EnsureBound()) return;
            if (string.IsNullOrWhiteSpace(screenId) || !HasScreenBinding(screenId)) return;

            _activeScreenId = screenId;
            if (_lastState != null && _lastConfig != null)
            {
                Render(_lastState, _lastConfig);
            }
            else
            {
                ApplyScreenVisibility();
                bottomNav.Render(catalog, _activeScreenId);
            }
        }

        private void EnsureActiveScreenId()
        {
            if (HasScreenBinding(_activeScreenId)) return;

            if (TrySetActiveScreen(DashboardScreenId)) return;
            if (TrySetActiveScreen(LegacyDashboardScreenId)) return;

            foreach (var entry in catalog.NavItems)
            {
                if (entry != null && entry.IsActive && TrySetActiveScreen(entry.Id))
                {
                    return;
                }
            }

            foreach (var entry in catalog.NavItems)
            {
                if (entry != null && TrySetActiveScreen(entry.Id))
                {
                    return;
                }
            }
        }

        private bool TrySetActiveScreen(string screenId)
        {
            if (!HasScreenBinding(screenId)) return false;
            _activeScreenId = screenId;
            return true;
        }

        private bool HasScreenBinding(string screenId)
        {
            if (string.IsNullOrWhiteSpace(screenId)) return false;
            foreach (var screen in screens)
            {
                if (screen != null && string.Equals(screen.Id, screenId, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private void ApplyScreenVisibility()
        {
            var animate = !string.IsNullOrWhiteSpace(_appliedScreenId) &&
                          !string.Equals(_appliedScreenId, _activeScreenId, StringComparison.Ordinal);
            foreach (var screen in screens)
            {
                if (screen == null) continue;

                var isActive = string.Equals(screen.Id, _activeScreenId, StringComparison.Ordinal);
                screen.SetActive(isActive, catalog.ScreenTransition, animate);
            }

            _appliedScreenId = _activeScreenId;
        }

        [Serializable]
        public sealed class ScreenBinding
        {
            [SerializeField] private string id;
            [SerializeField] private List<GameObject> roots = new List<GameObject>();
            [SerializeField] private List<CanvasGroup> canvasGroups = new List<CanvasGroup>();

            public ScreenBinding()
            {
            }

            public ScreenBinding(string id, IEnumerable<GameObject> screenRoots)
                : this(id, screenRoots, null)
            {
            }

            public ScreenBinding(string id, IEnumerable<GameObject> screenRoots, IEnumerable<CanvasGroup> screenCanvasGroups)
            {
                this.id = id;
                if (screenRoots != null)
                {
                    roots.AddRange(screenRoots);
                }

                if (screenCanvasGroups != null)
                {
                    canvasGroups.AddRange(screenCanvasGroups);
                }
            }

            public string Id { get { return id; } }

            public void SetActive(bool active)
            {
                SetActive(active, null, false);
            }

            public void SetActive(bool active, CampUiScreenTransition transition, bool animate)
            {
                for (var i = 0; i < roots.Count; i++)
                {
                    var root = roots[i];
                    if (root != null)
                    {
                        SetRootActive(root, ResolveCanvasGroup(i, root), active, transition, animate);
                    }
                }
            }

            private CanvasGroup ResolveCanvasGroup(int index, GameObject root)
            {
                if (index >= 0 && index < canvasGroups.Count && canvasGroups[index] != null)
                {
                    return canvasGroups[index];
                }

                return root != null ? root.GetComponent<CanvasGroup>() : null;
            }

            private static void SetRootActive(GameObject root, CanvasGroup group, bool active, CampUiScreenTransition transition, bool animate)
            {
                if (root == null) return;
                if (group != null)
                {
                    DOTween.Kill(group);
                }

                var canAnimate = animate && transition != null && transition.Enabled && transition.DurationSeconds > 0 && group != null;
                if (!canAnimate)
                {
                    root.SetActive(active);
                    ApplyCanvasState(group, active);
                    return;
                }

                if (active)
                {
                    root.SetActive(true);
                    ApplyCanvasState(group, true);
                }
                else
                {
                    group.interactable = false;
                    group.blocksRaycasts = false;
                    DOTween.To(() => group.alpha, value => group.alpha = value, 0, transition.DurationSeconds)
                        .SetEase(transition.Ease)
                        .SetUpdate(transition.UseUnscaledTime)
                        .SetTarget(group)
                        .SetLink(root)
                        .OnComplete(() => root.SetActive(false));
                }
            }

            private static void ApplyCanvasState(CanvasGroup group, bool active)
            {
                if (group == null) return;
                group.alpha = active ? 1 : 0;
                group.interactable = active;
                group.blocksRaycasts = active;
            }
        }
    }
}
