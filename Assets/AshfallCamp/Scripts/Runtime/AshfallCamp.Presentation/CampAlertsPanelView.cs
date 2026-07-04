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
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private List<AlertBinding> alerts = new List<AlertBinding>();

        private Action _emergencyScavengeRequested;

        private void OnDestroy()
        {
            foreach (var binding in alerts)
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
        }

        public void SetEmergencyScavengeHandler(Action emergencyScavengeRequested)
        {
            _emergencyScavengeRequested = emergencyScavengeRequested;
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            UiText.Set(title, catalog.RecentAlertsTitle);
            var dynamicAlerts = CampDashboardTextFormatter.BuildAlerts(state, config, catalog);
            for (var i = 0; i < alerts.Count; i++)
            {
                var binding = alerts[i];
                var usesDynamicEntries = dynamicAlerts.Count > 0;
                var hasEntry = usesDynamicEntries ? i < dynamicAlerts.Count : i < catalog.Alerts.Count;
                binding.SetActive(hasEntry);
                if (!hasEntry) continue;

                var tone = usesDynamicEntries ? dynamicAlerts[i].ToneColor : catalog.Alerts[i].ToneColor;
                var panelColor = new Color(tone.r, tone.g, tone.b, 0.08f);
                if (binding.Panel != null) binding.Panel.color = panelColor;
                if (binding.Dot != null) binding.Dot.color = tone;
                UiText.Set(binding.Title, usesDynamicEntries ? dynamicAlerts[i].Title : catalog.Alerts[i].Title);
                UiText.Set(binding.Body, usesDynamicEntries ? dynamicAlerts[i].Body : catalog.Alerts[i].Body);
                binding.SetAction(usesDynamicEntries ? dynamicAlerts[i] : null, _emergencyScavengeRequested);
            }
        }

        [Serializable]
        public sealed class AlertBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private Image dot;
            [SerializeField] private TextMeshProUGUI title;
            [SerializeField] private TextMeshProUGUI body;
            [SerializeField] private Button actionButton;
            [SerializeField] private TextMeshProUGUI actionButtonLabel;

            private Action _emergencyScavengeRequested;
            private bool _actionButtonWired;

            public AlertBinding()
            {
            }

            public AlertBinding(Image panel, Image dot, TextMeshProUGUI title, TextMeshProUGUI body)
            {
                this.panel = panel;
                this.dot = dot;
                this.title = title;
                this.body = body;
            }

            public Image Panel { get { return panel; } }
            public Image Dot { get { return dot; } }
            public TextMeshProUGUI Title { get { return title; } }
            public TextMeshProUGUI Body { get { return body; } }
            public Button ActionButton { get { return actionButton; } }
            public TextMeshProUGUI ActionButtonLabel { get { return actionButtonLabel; } }

            public void SetAction(CampAlertPresentation presentation, Action emergencyScavengeRequested)
            {
                var hasAction = presentation != null && presentation.CanStartEmergencyScavenge;
                _emergencyScavengeRequested = hasAction ? emergencyScavengeRequested : null;
                UiText.SetActive(actionButton, hasAction);
                UiText.Set(actionButtonLabel, hasAction ? presentation.ActionLabel : string.Empty);
                if (actionButton != null)
                {
                    actionButton.interactable = hasAction && emergencyScavengeRequested != null;
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
                _emergencyScavengeRequested = null;
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
                _emergencyScavengeRequested?.Invoke();
            }
        }
    }
}
