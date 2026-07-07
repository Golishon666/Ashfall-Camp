using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CampAlertsPanelView : MonoBehaviour
    {
        private const float DefaultFirstRowTopOffset = 67f;
        private const float DefaultRowLeftOffset = 26f;
        private const float DefaultRowRightOffset = 26f;
        private const float DefaultRowHeight = 52f;
        private const float DefaultRowSpacing = 7f;
        private const float DefaultBottomPadding = 12f;

        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private List<AlertBinding> alerts = new List<AlertBinding>();
        [SerializeField] private GameObject alertRowPrefab;
        [SerializeField] private RectTransform alertContentRoot;
        [SerializeField] private ScrollRect scrollRect;
        [SerializeField] private float firstRowTopOffset = DefaultFirstRowTopOffset;
        [SerializeField] private float rowLeftOffset = DefaultRowLeftOffset;
        [SerializeField] private float rowRightOffset = DefaultRowRightOffset;
        [SerializeField] private float rowHeight = DefaultRowHeight;
        [SerializeField] private float rowSpacing = DefaultRowSpacing;
        [SerializeField] private float bottomPadding = DefaultBottomPadding;

        private readonly List<AlertBinding> _runtimeAlerts = new List<AlertBinding>();
        private Action _emergencyScavengeRequested;
        private Action<CampAlertPresentation> _alertActionRequested;
        private RectTransform _runtimeContentRoot;
        private bool _runtimePoolInitialized;

        private void OnDestroy()
        {
            foreach (var binding in alerts)
            {
                if (binding != null) binding.ClearActionHandler();
            }

            foreach (var binding in _runtimeAlerts)
            {
                if (binding != null) binding.ClearActionHandler();
            }
        }

        public void ConfigureBindings(TextMeshProUGUI titleLabel, IEnumerable<AlertBinding> alertBindings)
        {
            title = titleLabel;
            alerts.Clear();
            if (alertBindings != null)
            {
                alerts.AddRange(alertBindings);
            }

            _runtimePoolInitialized = false;
            _runtimeAlerts.Clear();
        }

        public void ConfigureRowPrefab(GameObject rowPrefab, RectTransform contentRoot = null, ScrollRect selectedScrollRect = null)
        {
            alertRowPrefab = rowPrefab;
            alertContentRoot = contentRoot;
            scrollRect = selectedScrollRect;
            _runtimePoolInitialized = false;
            _runtimeAlerts.Clear();
        }

        public void SetEmergencyScavengeHandler(Action emergencyScavengeRequested)
        {
            _emergencyScavengeRequested = emergencyScavengeRequested;
        }

        public void SetAlertActionHandler(Action<CampAlertPresentation> alertActionRequested)
        {
            _alertActionRequested = alertActionRequested;
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            UiText.Set(title, catalog.RecentAlertsTitle);
            var dynamicAlerts = CampDashboardTextFormatter.BuildAlerts(state, config, catalog);
            var entries = dynamicAlerts.Count > 0 ? dynamicAlerts : BuildStaticAlerts(catalog);
            var bindings = ResolveBindings(entries.Count);
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                var hasEntry = i < entries.Count;
                binding.SetActive(hasEntry);
                if (!hasEntry) continue;

                var presentation = entries[i];
                var tone = presentation.ToneColor;
                var panelColor = catalog.Theme.WithAlpha(tone, catalog.Theme.AlertPanelAlpha);
                binding.ApplyArtwork(catalog.CampAlertCardTexture);
                if (binding.Panel != null) binding.Panel.color = panelColor;
                binding.ApplyIcon(presentation.Icon, tone);
                UiText.Set(binding.Title, presentation.Title);
                UiText.Set(binding.Body, presentation.Body);
                binding.SetAction(presentation, _alertActionRequested, _emergencyScavengeRequested);
            }

            LayoutRows(bindings, entries.Count);
        }

        private static List<CampAlertPresentation> BuildStaticAlerts(CampUiCatalogSO catalog)
        {
            var entries = new List<CampAlertPresentation>();
            if (catalog == null || catalog.Alerts == null) return entries;

            foreach (var entry in catalog.Alerts)
            {
                if (entry == null) continue;
                var canInvoke = entry.Action != CampAlertAction.None && entry.ButtonView != CampAlertButtonView.Hidden;
                entries.Add(new CampAlertPresentation(
                    entry.Id,
                    entry.Title,
                    entry.Body,
                    entry.ToneColor,
                    entry.Severity,
                    entry.Priority,
                    entry.Category,
                    entry.Action,
                    entry.TargetScreenId,
                    entry.ButtonLabel,
                    entry.ButtonView,
                    entry.Icon,
                    entry.ActionIcon,
                    canInvoke));
            }

            return entries;
        }

        private List<AlertBinding> ResolveBindings(int requiredCount)
        {
            if (alertRowPrefab == null)
            {
                return alerts;
            }

            EnsureRuntimePool();
            var content = ResolveContentRoot();
            while (_runtimeAlerts.Count < requiredCount)
            {
                var instance = Instantiate(alertRowPrefab, content != null ? content : transform);
                instance.name = alertRowPrefab.name + "_" + _runtimeAlerts.Count;
                _runtimeAlerts.Add(AlertBinding.FromRoot(instance));
            }

            return _runtimeAlerts;
        }

        private void EnsureRuntimePool()
        {
            if (_runtimePoolInitialized) return;
            _runtimeAlerts.Clear();
            var content = ResolveContentRoot();
            foreach (var binding in alerts)
            {
                if (binding == null) continue;
                binding.Reparent(content);
                _runtimeAlerts.Add(binding);
            }

            _runtimePoolInitialized = true;
        }

        private RectTransform ResolveContentRoot()
        {
            if (alertContentRoot != null) return alertContentRoot;
            if (_runtimeContentRoot != null) return _runtimeContentRoot;

            var go = new GameObject("AlertScrollContent", typeof(RectTransform));
            go.transform.SetParent(transform, false);
            _runtimeContentRoot = go.GetComponent<RectTransform>();
            _runtimeContentRoot.anchorMin = new Vector2(0f, 1f);
            _runtimeContentRoot.anchorMax = new Vector2(1f, 1f);
            _runtimeContentRoot.pivot = new Vector2(0.5f, 1f);
            _runtimeContentRoot.anchoredPosition = Vector2.zero;
            _runtimeContentRoot.sizeDelta = Vector2.zero;

            if (scrollRect == null)
            {
                scrollRect = GetComponent<ScrollRect>();
            }

            if (scrollRect == null)
            {
                scrollRect = gameObject.AddComponent<ScrollRect>();
            }

            scrollRect.content = _runtimeContentRoot;
            scrollRect.viewport = transform as RectTransform;
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollRect.scrollSensitivity = 18f;
            return _runtimeContentRoot;
        }

        private void LayoutRows(IReadOnlyList<AlertBinding> bindings, int activeCount)
        {
            if (alertRowPrefab == null || bindings == null) return;

            var content = ResolveContentRoot();
            var safeRowHeight = Mathf.Max(1f, rowHeight);
            var safeSpacing = Mathf.Max(0f, rowSpacing);
            for (var i = 0; i < bindings.Count; i++)
            {
                var rect = bindings[i] != null ? bindings[i].Root : null;
                if (rect == null) continue;

                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(1f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(rowLeftOffset, -firstRowTopOffset - i * (safeRowHeight + safeSpacing));
                rect.sizeDelta = new Vector2(-rowLeftOffset - rowRightOffset, safeRowHeight);
            }

            if (content != null)
            {
                var height = firstRowTopOffset + activeCount * safeRowHeight + Mathf.Max(0, activeCount - 1) * safeSpacing + bottomPadding;
                content.sizeDelta = new Vector2(0f, Mathf.Max(0f, height));
            }
        }

        [Serializable]
        public sealed class AlertBinding
        {
            [SerializeField] private Graphic panel;
            [SerializeField] private RawImage cardArtwork;
            [SerializeField] private Graphic dot;
            [SerializeField] private TextMeshProUGUI title;
            [SerializeField] private TextMeshProUGUI body;
            [SerializeField] private Button actionButton;
            [SerializeField] private TextMeshProUGUI actionButtonLabel;

            private Action<CampAlertPresentation> _alertActionRequested;
            private Action _emergencyScavengeRequested;
            private CampAlertPresentation _presentation;
            private bool _actionButtonWired;

            public AlertBinding()
            {
            }

            public AlertBinding(Graphic panel, Graphic dot, TextMeshProUGUI title, TextMeshProUGUI body)
            {
                this.panel = panel;
                this.dot = dot;
                this.title = title;
                this.body = body;
            }

            public Graphic Panel { get { return panel; } }
            public RectTransform Root { get { return panel != null ? panel.transform as RectTransform : null; } }
            public Graphic Dot { get { return dot; } }
            public TextMeshProUGUI Title { get { return title; } }
            public TextMeshProUGUI Body { get { return body; } }
            public Button ActionButton { get { return actionButton; } }
            public TextMeshProUGUI ActionButtonLabel { get { return actionButtonLabel; } }

            public static AlertBinding FromRoot(GameObject root)
            {
                if (root == null) return new AlertBinding();

                return new AlertBinding
                {
                    panel = root.GetComponent<Graphic>(),
                    cardArtwork = root.GetComponent<RawImage>(),
                    dot = FindChildComponent<Graphic>(root.transform, "Image_Icon"),
                    title = FindChildComponent<TextMeshProUGUI>(root.transform, "Text_Title"),
                    body = FindChildComponent<TextMeshProUGUI>(root.transform, "Text_Description"),
                    actionButton = FindChildComponent<Button>(root.transform, "Button_View"),
                    actionButtonLabel = FindChildComponent<TextMeshProUGUI>(root.transform, "Text_Label")
                };
            }

            public void Reparent(RectTransform parent)
            {
                if (parent == null || panel == null) return;
                panel.transform.SetParent(parent, false);
            }

            public void ApplyArtwork(Texture2D texture)
            {
                if (cardArtwork == null) return;

                var hasTexture = texture != null;
                cardArtwork.gameObject.SetActive(hasTexture);
                if (!hasTexture) return;

                cardArtwork.texture = texture;
                cardArtwork.color = Color.white;
            }

            public void ApplyIcon(Texture2D texture, Color fallbackColor)
            {
                if (dot == null) return;

                var rawImage = dot as RawImage;
                if (rawImage != null && texture != null)
                {
                    rawImage.texture = texture;
                    rawImage.color = Color.white;
                    rawImage.gameObject.SetActive(true);
                    return;
                }

                if (rawImage != null)
                {
                    rawImage.texture = null;
                }

                dot.color = fallbackColor;
                dot.gameObject.SetActive(true);
            }

            public void SetAction(
                CampAlertPresentation presentation,
                Action<CampAlertPresentation> alertActionRequested,
                Action emergencyScavengeRequested)
            {
                var hasAction = presentation != null && presentation.HasAction;
                _presentation = hasAction ? presentation : null;
                _alertActionRequested = hasAction ? alertActionRequested : null;
                _emergencyScavengeRequested = hasAction ? emergencyScavengeRequested : null;
                UiText.SetActive(actionButton, hasAction);
                var showText = hasAction && presentation.ButtonView != CampAlertButtonView.Icon;
                UiText.SetActive(actionButtonLabel, showText);
                UiText.Set(actionButtonLabel, showText ? presentation.ActionLabel : string.Empty);
                if (actionButton != null)
                {
                    actionButton.interactable = hasAction &&
                                                (alertActionRequested != null ||
                                                 (presentation.CanStartEmergencyScavenge && emergencyScavengeRequested != null));
                }

                WireActionButton();
            }

            public void ClearActionHandler()
            {
                if (actionButton != null)
                {
                    actionButton.onClick.RemoveListener(OnActionClicked);
                }

                _actionButtonWired = false;
                _alertActionRequested = null;
                _emergencyScavengeRequested = null;
                _presentation = null;
            }

            public void SetActive(bool active)
            {
                UiText.SetActive(panel, active);
            }

            private void WireActionButton()
            {
                if (_actionButtonWired || actionButton == null) return;
                actionButton.onClick.RemoveListener(OnActionClicked);
                actionButton.onClick.AddListener(OnActionClicked);
                _actionButtonWired = true;
            }

            private void OnActionClicked()
            {
                if (_presentation == null) return;
                if (_alertActionRequested != null)
                {
                    _alertActionRequested.Invoke(_presentation);
                    return;
                }

                if (_presentation.CanStartEmergencyScavenge)
                {
                    _emergencyScavengeRequested?.Invoke();
                }
            }

            private static T FindChildComponent<T>(Transform root, string childName) where T : Component
            {
                if (root == null) return null;
                var children = root.GetComponentsInChildren<T>(true);
                for (var i = 0; i < children.Length; i++)
                {
                    if (children[i] != null && children[i].name == childName)
                    {
                        return children[i];
                    }
                }

                return null;
            }
        }
    }
}
