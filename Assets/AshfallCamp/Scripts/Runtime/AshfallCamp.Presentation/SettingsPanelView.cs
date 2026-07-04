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
        [SerializeField] private RawImage autosaveRowArtwork;
        [SerializeField] private RawImage autosaveToggleTrackArtwork;
        [SerializeField] private RawImage autosaveToggleKnobArtwork;
        [SerializeField] private TextMeshProUGUI manualSaveTitle;
        [SerializeField] private TextMeshProUGUI manualSaveBody;
        [SerializeField] private Button manualSaveButton;
        [SerializeField] private TextMeshProUGUI manualSaveButtonLabel;
        [SerializeField] private RawImage manualSaveRowArtwork;

        private Action<bool> _autosaveChanged;
        private Action _manualSaveRequested;
        private UnityAction<bool> _autosaveToggleChanged;
        private UnityAction _manualSaveClick;

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            TextMeshProUGUI autosaveTitleLabel,
            TextMeshProUGUI autosaveBodyLabel,
            TextMeshProUGUI autosaveStateLabel,
            Toggle autosaveEnabledToggle,
            TextMeshProUGUI autosaveEnabledToggleLabel,
            TextMeshProUGUI manualSaveTitleLabel = null,
            TextMeshProUGUI manualSaveBodyLabel = null,
            Button manualSaveActionButton = null,
            TextMeshProUGUI manualSaveActionButtonLabel = null,
            RawImage autosaveRow = null,
            RawImage autosaveToggleTrack = null,
            RawImage autosaveToggleKnob = null,
            RawImage manualSaveRow = null)
        {
            title = titleLabel;
            autosaveTitle = autosaveTitleLabel;
            autosaveBody = autosaveBodyLabel;
            autosaveState = autosaveStateLabel;
            autosaveToggle = autosaveEnabledToggle;
            autosaveToggleLabel = autosaveEnabledToggleLabel;
            if (autosaveRow != null) autosaveRowArtwork = autosaveRow;
            if (autosaveToggleTrack != null) autosaveToggleTrackArtwork = autosaveToggleTrack;
            if (autosaveToggleKnob != null) autosaveToggleKnobArtwork = autosaveToggleKnob;
            if (manualSaveTitleLabel != null) manualSaveTitle = manualSaveTitleLabel;
            if (manualSaveBodyLabel != null) manualSaveBody = manualSaveBodyLabel;
            if (manualSaveActionButton != null) manualSaveButton = manualSaveActionButton;
            if (manualSaveActionButtonLabel != null) manualSaveButtonLabel = manualSaveActionButtonLabel;
            if (manualSaveRow != null) manualSaveRowArtwork = manualSaveRow;
            WireToggle();
            WireManualSaveButton();
        }

        private void Awake()
        {
            WireToggle();
            WireManualSaveButton();
        }

        private void OnDestroy()
        {
            ClearToggle();
            ClearManualSaveButton();
        }

        public void SetAutosaveChangedHandler(Action<bool> autosaveChanged)
        {
            _autosaveChanged = autosaveChanged;
            WireToggle();
        }

        public void SetManualSaveHandler(Action manualSaveRequested)
        {
            _manualSaveRequested = manualSaveRequested;
            WireManualSaveButton();
            ApplyManualSaveButtonState();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            var settings = CampDashboardTextFormatter.BuildSettings(state, config, catalog);
            ApplyArtwork(autosaveRowArtwork, catalog.SettingsRowTexture);
            ApplyArtwork(
                autosaveToggleTrackArtwork,
                settings.AutosaveEnabled ? catalog.SettingsToggleTrackActiveTexture : catalog.SettingsToggleTrackInactiveTexture);
            ApplyArtwork(autosaveToggleKnobArtwork, catalog.SettingsToggleKnobTexture);
            ApplyArtwork(manualSaveRowArtwork, catalog.SettingsRowTexture);

            UiText.Set(title, settings.Title);
            UiText.Set(autosaveTitle, settings.AutosaveTitle);
            UiText.Set(autosaveBody, settings.AutosaveBody);
            UiText.Set(autosaveState, settings.AutosaveState);
            UiText.Set(autosaveToggleLabel, settings.AutosaveToggleLabel);
            UiText.Set(manualSaveTitle, settings.ManualSaveTitle);
            UiText.Set(manualSaveBody, settings.ManualSaveBody);
            UiText.Set(manualSaveButtonLabel, settings.ManualSaveButton);

            if (autosaveState != null)
            {
                autosaveState.color = settings.AutosaveEnabled ? catalog.Theme.Sage : catalog.Theme.Rust;
            }

            if (autosaveToggle != null)
            {
                autosaveToggle.SetIsOnWithoutNotify(settings.AutosaveEnabled);
                autosaveToggle.interactable = _autosaveChanged != null;
            }

            ApplyManualSaveButtonState();
        }

        private static void ApplyArtwork(RawImage target, Texture2D texture)
        {
            if (target == null) return;
            var hasTexture = texture != null;
            target.gameObject.SetActive(hasTexture);
            target.texture = texture;
            if (hasTexture)
            {
                target.color = Color.white;
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

        private void WireManualSaveButton()
        {
            if (manualSaveButton == null || _manualSaveClick != null) return;
            _manualSaveClick = OnManualSaveClicked;
            manualSaveButton.onClick.AddListener(_manualSaveClick);
        }

        private void ClearManualSaveButton()
        {
            if (manualSaveButton != null && _manualSaveClick != null)
            {
                manualSaveButton.onClick.RemoveListener(_manualSaveClick);
            }

            _manualSaveClick = null;
        }

        private void ApplyManualSaveButtonState()
        {
            if (manualSaveButton != null)
            {
                manualSaveButton.interactable = _manualSaveRequested != null;
            }
        }

        private void OnAutosaveToggleChanged(bool enabled)
        {
            _autosaveChanged?.Invoke(enabled);
        }

        private void OnManualSaveClicked()
        {
            _manualSaveRequested?.Invoke();
        }
    }
}
