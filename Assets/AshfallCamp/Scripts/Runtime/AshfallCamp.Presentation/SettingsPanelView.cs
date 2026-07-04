using System;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class SettingsPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI autosaveTitle;
        [SerializeField] private TextMeshProUGUI autosaveBody;
        [SerializeField] private TextMeshProUGUI autosaveState;
        [SerializeField] private Toggle autosaveToggle;
        [SerializeField] private TextMeshProUGUI autosaveToggleLabel;

        private Action<bool> _autosaveChanged;
        private UnityAction<bool> _autosaveToggleChanged;

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            TextMeshProUGUI autosaveTitleLabel,
            TextMeshProUGUI autosaveBodyLabel,
            TextMeshProUGUI autosaveStateLabel,
            Toggle autosaveEnabledToggle,
            TextMeshProUGUI autosaveEnabledToggleLabel)
        {
            title = titleLabel;
            autosaveTitle = autosaveTitleLabel;
            autosaveBody = autosaveBodyLabel;
            autosaveState = autosaveStateLabel;
            autosaveToggle = autosaveEnabledToggle;
            autosaveToggleLabel = autosaveEnabledToggleLabel;
            WireToggle();
        }

        private void Awake()
        {
            WireToggle();
        }

        private void OnDestroy()
        {
            ClearToggle();
        }

        public void SetAutosaveChangedHandler(Action<bool> autosaveChanged)
        {
            _autosaveChanged = autosaveChanged;
            WireToggle();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            var settings = CampDashboardTextFormatter.BuildSettings(state, config, catalog);
            UiText.Set(title, settings.Title);
            UiText.Set(autosaveTitle, settings.AutosaveTitle);
            UiText.Set(autosaveBody, settings.AutosaveBody);
            UiText.Set(autosaveState, settings.AutosaveState);
            UiText.Set(autosaveToggleLabel, settings.AutosaveToggleLabel);

            if (autosaveState != null)
            {
                autosaveState.color = settings.AutosaveEnabled ? catalog.Theme.Sage : catalog.Theme.Rust;
            }

            if (autosaveToggle != null)
            {
                autosaveToggle.SetIsOnWithoutNotify(settings.AutosaveEnabled);
                autosaveToggle.interactable = _autosaveChanged != null;
            }
        }

        private void WireToggle()
        {
            if (autosaveToggle == null || _autosaveToggleChanged != null) return;
            _autosaveToggleChanged = OnAutosaveToggleChanged;
            autosaveToggle.onValueChanged.AddListener(_autosaveToggleChanged);
        }

        private void ClearToggle()
        {
            if (autosaveToggle != null && _autosaveToggleChanged != null)
            {
                autosaveToggle.onValueChanged.RemoveListener(_autosaveToggleChanged);
            }

            _autosaveToggleChanged = null;
        }

        private void OnAutosaveToggleChanged(bool enabled)
        {
            _autosaveChanged?.Invoke(enabled);
        }
    }
}
