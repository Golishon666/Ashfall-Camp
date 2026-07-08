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
        private readonly List<ResourceBinding> _runtimeBindings = new List<ResourceBinding>();
        private readonly List<ResourceUiEntry> _renderEntries = new List<ResourceUiEntry>();
        private bool _lookupDirty = true;

        public void ConfigureBindings(IEnumerable<ResourceBinding> resourceBindings)
        {
            ResetRuntimeBindings();
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
            BuildRenderEntries(config, catalog);
            HideAllBindings();

            for (var i = 0; i < _renderEntries.Count; i++)
            {
                var entry = _renderEntries[i];
                if (string.IsNullOrWhiteSpace(entry.Id)) continue;
                var binding = ResolveBinding(entry.Id, i);
                if (binding == null) continue;

                binding.SetActive(true);
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
                ApplyRateText(binding, FormatRateValue(state, config, catalog, entry));
                ApplyCapacitySlider(binding, state, entry);
            }
        }

        private void OnDestroy()
        {
            ResetRuntimeBindings();
        }

        private void BuildRenderEntries(GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            _renderEntries.Clear();
            var ids = new HashSet<string>(StringComparer.Ordinal);
            if (catalog.ResourceBar != null)
            {
                foreach (var entry in catalog.ResourceBar)
                {
                    if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) continue;
                    _renderEntries.Add(entry);
                    ids.Add(entry.Id);
                }
            }

            foreach (var resource in config.Resources.Values)
            {
                if (resource == null || string.IsNullOrWhiteSpace(resource.Id) || ids.Contains(resource.Id)) continue;
                _renderEntries.Add(new ResourceUiEntry
                {
                    Id = resource.Id,
                    Label = FormatDefaultResourceLabel(resource)
                });
                ids.Add(resource.Id);
            }
        }

        private static string FormatDefaultResourceLabel(ResourceDefinition resource)
        {
            var label = !string.IsNullOrWhiteSpace(resource.Name) ? resource.Name : resource.Id;
            return label.Replace('_', ' ').ToUpperInvariant();
        }

        private ResourceBinding ResolveBinding(string id, int renderIndex)
        {
            ResourceBinding binding;
            if (_lookup.TryGetValue(id, out binding))
            {
                return binding;
            }

            binding = CreateRuntimeBinding(id, renderIndex);
            if (binding == null) return null;
            _runtimeBindings.Add(binding);
            _lookup[id] = binding;
            return binding;
        }

        private ResourceBinding CreateRuntimeBinding(string id, int renderIndex)
        {
            var template = FindTemplateBinding();
            var templateRoot = template != null ? template.FindRoot() : null;
            if (templateRoot == null || templateRoot.parent == null) return null;

            var clone = Instantiate(templateRoot.gameObject, templateRoot.parent);
            clone.name = "ResourceCell_" + SanitizeName(id);
            var cloneRect = clone.transform as RectTransform;
            var templateRect = templateRoot as RectTransform;
            if (cloneRect != null && templateRect != null)
            {
                var width = templateRect.rect.width > 0f ? templateRect.rect.width : templateRect.sizeDelta.x;
                if (width <= 0f) width = 160f;
                cloneRect.anchoredPosition = new Vector2(
                    templateRect.anchoredPosition.x + width * renderIndex,
                    templateRect.anchoredPosition.y);
            }

            return ResourceBinding.FromRoot(id, clone.transform);
        }

        private ResourceBinding FindTemplateBinding()
        {
            foreach (var binding in resources)
            {
                if (binding != null && binding.FindRoot() != null)
                {
                    return binding;
                }
            }

            return null;
        }

        private static string SanitizeName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Resource";
            var chars = value.ToCharArray();
            for (var i = 0; i < chars.Length; i++)
            {
                if (!char.IsLetterOrDigit(chars[i]) && chars[i] != '_')
                {
                    chars[i] = '_';
                }
            }

            return new string(chars);
        }

        private void HideAllBindings()
        {
            foreach (var binding in resources)
            {
                if (binding != null) binding.SetActive(false);
            }

            foreach (var binding in _runtimeBindings)
            {
                if (binding != null) binding.SetActive(false);
            }
        }

        private void ResetRuntimeBindings()
        {
            for (var i = _runtimeBindings.Count - 1; i >= 0; i--)
            {
                var binding = _runtimeBindings[i];
                var root = binding != null ? binding.FindRoot() : null;
                if (root == null) continue;

                if (Application.isPlaying)
                {
                    Destroy(root.gameObject);
                }
                else
                {
                    DestroyImmediate(root.gameObject);
                }
            }

            _runtimeBindings.Clear();
            _lookupDirty = true;
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

            foreach (var binding in _runtimeBindings)
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

        private static string FormatRateValue(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog, ResourceUiEntry entry)
        {
            if (entry.UsesSurvivorCapacity)
            {
                var idle = UiStateQueries.CountIdleSurvivors(state);
                if (idle <= 0) return string.Empty;

                var suffix = string.IsNullOrWhiteSpace(catalog.IdleSuffixLabel) ? "idle" : catalog.IdleSuffixLabel;
                return idle + " " + suffix;
            }

            var perHour = UiStateQueries.CalculateProductionPerHour(state, config, entry.Id);
            if (perHour <= 0) return string.Empty;

            var perHourSuffix = string.IsNullOrWhiteSpace(catalog.PerHourSuffixLabel) ? "/h" : catalog.PerHourSuffixLabel;
            return "+" + perHour + perHourSuffix;
        }

        private static void ApplyRateText(ResourceBinding binding, string value)
        {
            if (binding == null || binding.Rate == null) return;

            var hasValue = !string.IsNullOrWhiteSpace(value);
            binding.Rate.gameObject.SetActive(hasValue);
            UiText.Set(binding.Rate, hasValue ? value : string.Empty);
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
            [SerializeField] private TextMeshProUGUI rate;
            [SerializeField] private RawImage icon;

            public ResourceBinding()
            {
            }

            public ResourceBinding(string id, TextMeshProUGUI label, TextMeshProUGUI value, RawImage icon)
                : this(id, label, value, null, icon)
            {
            }

            public ResourceBinding(string id, TextMeshProUGUI label, TextMeshProUGUI value, TextMeshProUGUI rate, RawImage icon)
            {
                this.id = id;
                this.label = label;
                this.value = value;
                this.rate = rate;
                this.icon = icon;
            }

            public string Id { get { return id; } }
            public TextMeshProUGUI Label { get { return label; } }
            public TextMeshProUGUI Value { get { return value; } }
            public TextMeshProUGUI Rate { get { return rate; } }
            public RawImage Icon { get { return icon; } }

            public void SetActive(bool active)
            {
                var root = FindRoot();
                if (root != null)
                {
                    root.gameObject.SetActive(active);
                }
            }

            public RectTransform FindRoot()
            {
                var source = icon != null ? icon.transform : label != null ? label.transform : value != null ? value.transform : rate != null ? rate.transform : null;
                return source != null && source.parent != null ? source.parent as RectTransform : null;
            }

            public Slider FindSlider()
            {
                var source = icon != null ? icon.transform : label != null ? label.transform : value != null ? value.transform : null;
                return source != null && source.parent != null ? source.parent.GetComponentInChildren<Slider>(true) : null;
            }

            public static ResourceBinding FromRoot(string id, Transform root)
            {
                if (root == null) return null;
                return new ResourceBinding(
                    id,
                    FindText(root, "Text_Label"),
                    FindText(root, "Text_Value"),
                    FindText(root, "Text_Rate"),
                    FindIcon(root));
            }

            private static TextMeshProUGUI FindText(Transform root, string name)
            {
                foreach (var text in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                {
                    if (string.Equals(text.name, name, StringComparison.Ordinal))
                    {
                        return text;
                    }
                }

                return null;
            }

            private static RawImage FindIcon(Transform root)
            {
                foreach (var image in root.GetComponentsInChildren<RawImage>(true))
                {
                    if (string.Equals(image.name, "Image_Icon", StringComparison.Ordinal))
                    {
                        return image;
                    }
                }

                return root.GetComponentInChildren<RawImage>(true);
            }
        }
    }
}
