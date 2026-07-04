using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class BottomNavView : MonoBehaviour
    {
        [SerializeField] private List<NavBinding> items = new List<NavBinding>();

        private readonly Dictionary<string, NavBinding> _lookup = new Dictionary<string, NavBinding>(StringComparer.Ordinal);
        private bool _lookupDirty = true;
        private Action<string> _itemSelected;

        public void ConfigureBindings(IEnumerable<NavBinding> navBindings)
        {
            items.Clear();
            if (navBindings != null)
            {
                items.AddRange(navBindings);
            }

            _lookupDirty = true;
            WireButtons();
        }

        public void SetSelectionHandler(Action<string> itemSelected)
        {
            _itemSelected = itemSelected;
            WireButtons();
        }

        public void Render(CampUiCatalogSO catalog, string activeItemId)
        {
            if (catalog == null) return;
            EnsureLookup();

            foreach (var entry in catalog.NavItems)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) continue;
                NavBinding binding;
                if (!_lookup.TryGetValue(entry.Id, out binding)) continue;

                var isActive = string.Equals(entry.Id, activeItemId, StringComparison.Ordinal) ||
                               (string.IsNullOrWhiteSpace(activeItemId) && entry.IsActive);
                UiText.Set(binding.Label, entry.Label);
                if (binding.Panel != null)
                {
                    binding.Panel.color = isActive
                        ? catalog.Theme.Teal
                        : new Color(catalog.Theme.PaperDark.r, catalog.Theme.PaperDark.g, catalog.Theme.PaperDark.b, 0.38f);
                }

                if (binding.Label != null)
                {
                    binding.Label.color = isActive ? catalog.Theme.Paper : catalog.Theme.Ink;
                }

                if (binding.Button != null)
                {
                    binding.Button.interactable = !isActive;
                }
            }
        }

        private void EnsureLookup()
        {
            if (!_lookupDirty) return;
            _lookup.Clear();
            foreach (var binding in items)
            {
                if (binding == null || string.IsNullOrWhiteSpace(binding.Id)) continue;
                _lookup[binding.Id] = binding;
            }

            _lookupDirty = false;
            WireButtons();
        }

        private void WireButtons()
        {
            foreach (var binding in items)
            {
                if (binding != null)
                {
                    binding.Wire(_itemSelected);
                }
            }
        }

        [Serializable]
        public sealed class NavBinding
        {
            [SerializeField] private string id;
            [SerializeField] private Image panel;
            [SerializeField] private Button button;
            [SerializeField] private TextMeshProUGUI label;

            [NonSerialized] private UnityAction _cachedClick;
            [NonSerialized] private Action<string> _selectionRequested;

            public NavBinding()
            {
            }

            public NavBinding(string id, Image panel, Button button, TextMeshProUGUI label)
            {
                this.id = id;
                this.panel = panel;
                this.button = button;
                this.label = label;
            }

            public string Id { get { return id; } }
            public Image Panel { get { return panel; } }
            public Button Button { get { return button; } }
            public TextMeshProUGUI Label { get { return label; } }

            public void Wire(Action<string> selectionRequested)
            {
                if (button == null) return;
                if (_cachedClick != null)
                {
                    button.onClick.RemoveListener(_cachedClick);
                }

                _selectionRequested = selectionRequested;
                _cachedClick = OnClicked;
                button.onClick.AddListener(_cachedClick);
            }

            private void OnClicked()
            {
                if (!string.IsNullOrWhiteSpace(id))
                {
                    _selectionRequested?.Invoke(id);
                }
            }
        }
    }
}
