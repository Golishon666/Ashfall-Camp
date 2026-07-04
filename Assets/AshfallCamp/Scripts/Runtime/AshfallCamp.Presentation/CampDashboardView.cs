using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CampDashboardView : MonoBehaviour, IUiRootView
    {
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
        [SerializeField] private CampRightColumnView rightColumn;
        [SerializeField] private BottomNavView bottomNav;
        [SerializeField] private List<ScreenBinding> screens = new List<ScreenBinding>();

        private bool _isBound;
        private bool _missingCatalogLogged;
        private string _activeScreenId = string.Empty;
        private GameState _lastState;
        private GameConfigSnapshot _lastConfig;

        public event Action<string> UpgradeRequested;
        public event Action<ExpeditionLaunchViewRequest> ExpeditionLaunchRequested;
        public event Action RecruitRequested;
        public event Action<RepairItemRequest> RepairItemRequested;
        public event Action<EquipItemRequest> EquipItemRequested;

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
            RadioPanelView radioPanelView = null)
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

            rightColumn = rightColumnView;
            bottomNav = bottomNavView;
            _isBound = false;
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

            rightColumn.Render(state, config, catalog);
            EnsureActiveScreenId();
            ApplyScreenVisibility();
            bottomNav.Render(catalog, _activeScreenId);

            _lastState = state;
            _lastConfig = config;
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

            if (header == null || resourceBar == null || statusPanel == null || summaryPanel == null ||
                alertsPanel == null || buildingGrid == null || rightColumn == null || bottomNav == null)
            {
                Debug.LogError("CampDashboardView requires all dashboard subviews.", this);
                return false;
            }

            buildingGrid.SetUpgradeHandler(id => UpgradeRequested?.Invoke(id));
            if (expeditionsPanel != null)
            {
                expeditionsPanel.SetLaunchHandler(request => ExpeditionLaunchRequested?.Invoke(request));
            }

            if (radioPanel != null)
            {
                radioPanel.SetBroadcastHandler(() => RecruitRequested?.Invoke());
            }

            if (workshopPanel != null)
            {
                workshopPanel.SetRepairHandler(request => RepairItemRequested?.Invoke(request));
                workshopPanel.SetEquipHandler(request => EquipItemRequested?.Invoke(request));
            }

            rightColumn.SetExpeditionLaunchHandler(request => ExpeditionLaunchRequested?.Invoke(request));
            rightColumn.SetRecruitHandler(() => RecruitRequested?.Invoke());
            bottomNav.SetSelectionHandler(OnScreenSelected);
            _isBound = true;
            return true;
        }

        private void OnScreenSelected(string screenId)
        {
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

            foreach (var entry in catalog.NavItems)
            {
                if (entry != null && entry.IsActive && HasScreenBinding(entry.Id))
                {
                    _activeScreenId = entry.Id;
                    return;
                }
            }

            foreach (var entry in catalog.NavItems)
            {
                if (entry != null && HasScreenBinding(entry.Id))
                {
                    _activeScreenId = entry.Id;
                    return;
                }
            }
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
            foreach (var screen in screens)
            {
                if (screen == null) continue;

                var isActive = string.Equals(screen.Id, _activeScreenId, StringComparison.Ordinal);
                screen.SetActive(isActive);
            }
        }

        [Serializable]
        public sealed class ScreenBinding
        {
            [SerializeField] private string id;
            [SerializeField] private List<GameObject> roots = new List<GameObject>();

            public ScreenBinding()
            {
            }

            public ScreenBinding(string id, IEnumerable<GameObject> screenRoots)
            {
                this.id = id;
                if (screenRoots != null)
                {
                    roots.AddRange(screenRoots);
                }
            }

            public string Id { get { return id; } }

            public void SetActive(bool active)
            {
                foreach (var root in roots)
                {
                    if (root != null)
                    {
                        root.SetActive(active);
                    }
                }
            }
        }
    }
}
