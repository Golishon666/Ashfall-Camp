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
                        ? new Color32(0x32, 0x37, 0x2E, 0xFF)
                        : new Color32(0x16, 0x1C, 0x19, 0xF2);
                }

                if (binding.Label != null)
                {
                    binding.Label.color = isActive ? catalog.Theme.Amber : catalog.Theme.Paper;
                }

                if (binding.Icon != null)
                {
                    var hasIcon = entry.Icon != null;
                    binding.Icon.gameObject.SetActive(hasIcon);
                    if (hasIcon)
                    {
                        binding.Icon.texture = entry.Icon;
                        binding.Icon.color = isActive ? catalog.Theme.Amber : catalog.Theme.Paper;
                    }
                }

                if (binding.Button != null)
                {
                    binding.Button.interactable = !isActive;
                    var marker = binding.Button.transform.Find("CurrentTabPointer");
                    if (marker != null)
                    {
                        marker.gameObject.SetActive(isActive);
                    }
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
            [SerializeField] private Graphic panel;
            [SerializeField] private Button button;
            [SerializeField] private RawImage icon;
            [SerializeField] private TextMeshProUGUI label;

            [NonSerialized] private UnityAction _cachedClick;
            [NonSerialized] private Action<string> _selectionRequested;

            public NavBinding()
            {
            }

            public NavBinding(string id, Image panel, Button button, TextMeshProUGUI label)
                : this(id, panel, button, null, label)
            {
            }

            public NavBinding(string id, Image panel, Button button, RawImage icon, TextMeshProUGUI label)
            {
                this.id = id;
                this.panel = panel;
                this.button = button;
                this.icon = icon;
                this.label = label;
            }

            public string Id { get { return id; } }
            public Graphic Panel { get { return panel; } }
            public Button Button { get { return button; } }
            public RawImage Icon { get { return icon; } }
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
