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
        [SerializeField] private List<FilterBinding> filters = new List<FilterBinding>();
        [SerializeField] private List<BuildingCardView> cards = new List<BuildingCardView>();

        private readonly Dictionary<string, FilterBinding> _filterLookup = new Dictionary<string, FilterBinding>(StringComparer.Ordinal);
        private readonly Dictionary<string, BuildingCardView> _cardLookup = new Dictionary<string, BuildingCardView>(StringComparer.Ordinal);
        private bool _lookupDirty = true;
        private Action<string> _upgradeRequested;

        public void ConfigureBindings(TextMeshProUGUI titleLabel, IEnumerable<FilterBinding> filterBindings, IEnumerable<BuildingCardView> buildingCards)
        {
            title = titleLabel;
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
                        : new Color(catalog.Theme.PaperDark.r, catalog.Theme.PaperDark.g, catalog.Theme.PaperDark.b, 0.55f);
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
