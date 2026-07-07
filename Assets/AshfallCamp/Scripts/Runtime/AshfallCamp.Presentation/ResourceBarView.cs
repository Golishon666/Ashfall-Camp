using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ResourceBarView : MonoBehaviour
    {
        [SerializeField] private List<ResourceBinding> resources = new List<ResourceBinding>();

        private readonly Dictionary<string, ResourceBinding> _lookup = new Dictionary<string, ResourceBinding>(StringComparer.Ordinal);
        private bool _lookupDirty = true;

        public void ConfigureBindings(IEnumerable<ResourceBinding> resourceBindings)
        {
            resources.Clear();
            if (resourceBindings != null)
            {
                resources.AddRange(resourceBindings);
            }

            _lookupDirty = true;
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;
            EnsureLookup();

            foreach (var entry in catalog.ResourceBar)
            {
                if (string.IsNullOrWhiteSpace(entry.Id)) continue;
                ResourceBinding binding;
                if (!_lookup.TryGetValue(entry.Id, out binding)) continue;

                UiText.Set(binding.Label, entry.Label);
                if (binding.Icon != null)
                {
                    var hasIcon = entry.Icon != null;
                    binding.Icon.gameObject.SetActive(hasIcon);
                    if (hasIcon)
                    {
                        binding.Icon.texture = entry.Icon;
                        binding.Icon.color = Color.white;
                    }
                }

                UiText.Set(binding.Value, FormatResourceValue(state, config, entry));
                ApplyCapacitySlider(binding, state, entry);
            }
        }

        private void EnsureLookup()
        {
            if (!_lookupDirty) return;
            _lookup.Clear();
            foreach (var binding in resources)
            {
                if (binding == null || string.IsNullOrWhiteSpace(binding.Id)) continue;
                _lookup[binding.Id] = binding;
            }

            _lookupDirty = false;
        }

        private static string FormatResourceValue(GameState state, GameConfigSnapshot config, ResourceUiEntry entry)
        {
            if (entry.UsesSurvivorCapacity)
            {
                return state.Survivors.Count + " / " + Math.Max(1, state.SurvivorCap);
            }

            int amount;
            state.Resources.TryGetValue(entry.Id, out amount);

            ResourceDefinition definition;
            if (config.TryGetResource(entry.Id, out definition) && definition.HasCap)
            {
                int cap;
                if (!state.ResourceCaps.TryGetValue(entry.Id, out cap))
                {
                    cap = definition.StartCap;
                }

                return amount + " / " + cap;
            }

            return amount.ToString();
        }

        private static void ApplyCapacitySlider(ResourceBinding binding, GameState state, ResourceUiEntry entry)
        {
            if (binding == null || !entry.UsesSurvivorCapacity) return;

            var slider = binding.FindSlider();
            if (slider == null) return;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;
            slider.SetValueWithoutNotify(Mathf.Clamp01(state.Survivors.Count / (float)Math.Max(1, state.SurvivorCap)));
        }

        [Serializable]
        public sealed class ResourceBinding
        {
            [SerializeField] private string id;
            [SerializeField] private TextMeshProUGUI label;
            [SerializeField] private TextMeshProUGUI value;
            [SerializeField] private RawImage icon;

            public ResourceBinding()
            {
            }

            public ResourceBinding(string id, TextMeshProUGUI label, TextMeshProUGUI value, RawImage icon)
            {
                this.id = id;
                this.label = label;
                this.value = value;
                this.icon = icon;
            }

            public string Id { get { return id; } }
            public TextMeshProUGUI Label { get { return label; } }
            public TextMeshProUGUI Value { get { return value; } }
            public RawImage Icon { get { return icon; } }

            public Slider FindSlider()
            {
                var source = icon != null ? icon.transform : label != null ? label.transform : value != null ? value.transform : null;
                return source != null && source.parent != null ? source.parent.GetComponentInChildren<Slider>(true) : null;
            }
        }
    }
}
