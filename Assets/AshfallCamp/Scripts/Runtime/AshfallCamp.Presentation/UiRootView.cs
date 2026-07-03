using System;
using AshfallCamp.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshfallCamp.Presentation
{
    public interface IUiRootView
    {
        Transform Root { get; }
        event Action<string> UpgradeRequested;
        void Render(GameState state, GameConfigSnapshot config);
    }

    [RequireComponent(typeof(UIDocument))]
    public sealed class UiRootView : MonoBehaviour, IUiRootView
    {
        [SerializeField] private Transform root;
        [SerializeField] private CampUiCatalogSO catalog;

        private ResourceBarView _resourceBar;
        private CampSummaryColumnView _summaryColumn;
        private BuildingGridView _buildingGrid;
        private CampOverviewColumnView _overviewColumn;
        private VisualElement _screen;

        public event Action<string> UpgradeRequested;

        public Transform Root
        {
            get { return root != null ? root : transform; }
        }

        private void Awake()
        {
            EnsureBuilt();
        }

        public void SetCatalog(CampUiCatalogSO uiCatalog)
        {
            catalog = uiCatalog;
            _screen = null;
        }

        public void Render(GameState state, GameConfigSnapshot config)
        {
            if (state == null || config == null) return;
            if (!EnsureBuilt()) return;
            _resourceBar.Render(state, config);
            _summaryColumn.Render(state, config);
            _buildingGrid.Render(state, config);
            _overviewColumn.Render(state, config);
        }

        private bool EnsureBuilt()
        {
            if (catalog == null)
            {
                Debug.LogError("Camp UI catalog is not assigned.");
                return false;
            }

            var document = GetComponent<UIDocument>();
            if (document == null || document.rootVisualElement == null)
            {
                return false;
            }

            var documentRoot = document.rootVisualElement;
            if (_screen != null && _screen.parent == documentRoot) return true;

            _screen = null;
            documentRoot.Clear();
            documentRoot.style.flexGrow = 1;
            documentRoot.style.backgroundColor = new Color(0.05f, 0.04f, 0.03f, 1f);

            _screen = new VisualElement { name = "ashfall-camp-screen" };
            _screen.style.flexGrow = 1;
            _screen.style.flexDirection = FlexDirection.Column;
            _screen.style.backgroundColor = catalog != null ? catalog.Theme.Paper : Color.black;
            _screen.style.paddingLeft = 28;
            _screen.style.paddingRight = 28;
            _screen.style.paddingTop = 22;
            _screen.style.paddingBottom = 18;
            documentRoot.Add(_screen);

            _resourceBar = new ResourceBarView(catalog);
            _resourceBar.Build(_screen);

            var main = new VisualElement();
            main.style.flexGrow = 1;
            main.style.flexDirection = FlexDirection.Row;
            _screen.Add(main);

            _summaryColumn = new CampSummaryColumnView(catalog);
            _summaryColumn.Build(main);

            _buildingGrid = new BuildingGridView(catalog, id => UpgradeRequested?.Invoke(id));
            _buildingGrid.Build(main);

            _overviewColumn = new CampOverviewColumnView(catalog);
            _overviewColumn.Build(main);

            new BottomNavView(catalog).Build(_screen);
            return true;
        }
    }
}
