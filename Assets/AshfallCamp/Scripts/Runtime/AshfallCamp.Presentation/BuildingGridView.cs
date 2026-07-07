using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BuildingGridView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private Transform cardContainer;
        [SerializeField] private BuildingCardView cardPrefab;
        [SerializeField] private GridLayoutGroup gridLayout;
        [SerializeField] private List<FilterBinding> filters = new List<FilterBinding>();
        [SerializeField] private List<BuildingCardView> cards = new List<BuildingCardView>();

        private readonly Dictionary<string, FilterBinding> _filterLookup = new Dictionary<string, FilterBinding>(StringComparer.Ordinal);
        private readonly Dictionary<string, BuildingCardView> _cardLookup = new Dictionary<string, BuildingCardView>(StringComparer.Ordinal);
        private bool _lookupDirty = true;
        private bool _dynamicCardsBuilt;
        private Action<string> _upgradeRequested;

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            IEnumerable<FilterBinding> filterBindings,
            IEnumerable<BuildingCardView> buildingCards,
            Transform buildingCardContainer = null,
            BuildingCardView buildingCardPrefab = null,
            GridLayoutGroup layout = null)
        {
            title = titleLabel;
            cardContainer = buildingCardContainer;
            cardPrefab = buildingCardPrefab;
            gridLayout = layout;
            filters.Clear();
            if (filterBindings != null)
            {
                filters.AddRange(filterBindings);
            }

            cards.Clear();
            if (buildingCards != null)
            {
                cards.AddRange(buildingCards);
            }

            _lookupDirty = true;
            ApplyUpgradeHandler();
        }

        public void SetUpgradeHandler(Action<string> upgradeRequested)
        {
            _upgradeRequested = upgradeRequested;
            ApplyUpgradeHandler();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;
            EnsureLookup();

            UiText.Set(title, catalog.BuildingScreenTitle);
            RenderFilters(catalog);
            EnsureCards(catalog);
            EnsureLookup();

            foreach (var entry in catalog.Buildings)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.BuildingId)) continue;
                BuildingCardView card;
                if (_cardLookup.TryGetValue(entry.BuildingId, out card) && card != null)
                {
                    card.Render(catalog, entry, state, config);
                }
            }
        }

        private void EnsureCards(CampUiCatalogSO catalog)
        {
            ConfigureGridLayout();
            if (catalog == null || cardPrefab == null) return;
            if (_dynamicCardsBuilt) return;

            var parent = ResolveCardContainer();
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.gameObject.SetActive(false);
                }
            }

            cards.Clear();
            foreach (var entry in catalog.Buildings)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.BuildingId)) continue;

                var card = Instantiate(cardPrefab, parent);
                card.name = "BuildingCard_" + entry.BuildingId;
                card.ConfigureBuildingId(entry.BuildingId);
                card.SetUpgradeHandler(_upgradeRequested);
                cards.Add(card);
                _lookupDirty = true;
            }

            _dynamicCardsBuilt = true;
        }

        private void ConfigureGridLayout()
        {
            var parent = ResolveCardContainer();
            if (parent == null) return;

            if (gridLayout == null)
            {
                gridLayout = parent.GetComponent<GridLayoutGroup>();
            }

            var addedLayout = false;
            if (gridLayout == null)
            {
                gridLayout = parent.gameObject.AddComponent<GridLayoutGroup>();
                addedLayout = true;
            }

            gridLayout.constraint = GridLayoutGroup.Constraint.FixedColumnCount;
            gridLayout.constraintCount = 3;
            if (gridLayout.spacing == Vector2.zero)
            {
                gridLayout.spacing = new Vector2(12f, 34f);
            }

            if (gridLayout.padding == null)
            {
                gridLayout.padding = new RectOffset();
            }

            if (addedLayout || gridLayout.cellSize == Vector2.zero || gridLayout.cellSize.x <= 100f || gridLayout.cellSize.y <= 100f)
            {
                var sample = cards.Count > 0 && cards[0] != null ? cards[0].GetComponent<RectTransform>() : null;
                gridLayout.cellSize = sample != null && sample.sizeDelta != Vector2.zero
                    ? sample.sizeDelta
                    : new Vector2(540f, 306f);
            }
        }

        private Transform ResolveCardContainer()
        {
            if (cardContainer != null) return cardContainer;
            if (cards.Count > 0 && cards[0] != null && cards[0].transform.parent != null)
            {
                cardContainer = cards[0].transform.parent;
                return cardContainer;
            }

            cardContainer = transform;
            return cardContainer;
        }

        private BuildingCardView FindCard(string buildingId)
        {
            foreach (var card in cards)
            {
                if (card != null && string.Equals(card.BuildingId, buildingId, StringComparison.Ordinal)) return card;
            }

            return null;
        }

        private void EnsureLookup()
        {
            if (!_lookupDirty) return;
            _filterLookup.Clear();
            foreach (var filter in filters)
            {
                if (filter == null || string.IsNullOrWhiteSpace(filter.Id)) continue;
                _filterLookup[filter.Id] = filter;
            }

            _cardLookup.Clear();
            foreach (var card in cards)
            {
                if (card == null || string.IsNullOrWhiteSpace(card.BuildingId)) continue;
                _cardLookup[card.BuildingId] = card;
            }

            _lookupDirty = false;
        }

        private void RenderFilters(CampUiCatalogSO catalog)
        {
            foreach (var entry in catalog.BuildingFilters)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) continue;
                FilterBinding binding;
                if (!_filterLookup.TryGetValue(entry.Id, out binding)) continue;

                UiText.Set(binding.Label, entry.Label);
                if (binding.Panel != null)
                {
                    binding.Panel.color = entry.IsActive
                        ? catalog.Theme.Teal
                        : catalog.Theme.WithAlpha(catalog.Theme.PaperDark, catalog.Theme.BuildingFilterInactivePanelAlpha);
                }

                if (binding.Label != null)
                {
                    binding.Label.color = entry.IsActive ? catalog.Theme.Paper : catalog.Theme.Ink;
                }
            }
        }

        private void ApplyUpgradeHandler()
        {
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.SetUpgradeHandler(_upgradeRequested);
                }
            }
        }

        [Serializable]
        public sealed class FilterBinding
        {
            [SerializeField] private string id;
            [SerializeField] private Image panel;
            [SerializeField] private TextMeshProUGUI label;

            public FilterBinding()
            {
            }

            public FilterBinding(string id, Image panel, TextMeshProUGUI label)
            {
                this.id = id;
                this.panel = panel;
                this.label = label;
            }

            public string Id { get { return id; } }
            public Image Panel { get { return panel; } }
            public TextMeshProUGUI Label { get { return label; } }
        }
    }
}
