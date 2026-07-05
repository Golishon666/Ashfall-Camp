using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CampStatusPanelView : MonoBehaviour
    {
        [SerializeField] private RawImage panelArtwork;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI label;
        [SerializeField] private TextMeshProUGUI body;
        [SerializeField] private TextMeshProUGUI badge;
        [SerializeField] private TextMeshProUGUI moraleLabel;
        [SerializeField] private TextMeshProUGUI safetyLabel;
        [SerializeField] private TextMeshProUGUI suppliesLabel;
        [SerializeField] private TextMeshProUGUI moraleValue;
        [SerializeField] private TextMeshProUGUI safetyValue;
        [SerializeField] private TextMeshProUGUI suppliesValue;
        [SerializeField] private Image moraleFill;
        [SerializeField] private Image safetyFill;
        [SerializeField] private Image suppliesFill;

        public void ConfigureBindings(
            TextMeshProUGUI statusTitle,
            TextMeshProUGUI statusLabel,
            TextMeshProUGUI statusBody,
            TextMeshProUGUI statusBadge,
            TextMeshProUGUI moraleLabelText,
            TextMeshProUGUI safetyLabelText,
            TextMeshProUGUI suppliesLabelText,
            TextMeshProUGUI moraleValueText,
            TextMeshProUGUI safetyValueText,
            TextMeshProUGUI suppliesValueText,
            Image moraleFillImage,
            Image safetyFillImage,
            Image suppliesFillImage,
            RawImage statusPanelArtwork = null)
        {
            panelArtwork = statusPanelArtwork;
            title = statusTitle;
            label = statusLabel;
            body = statusBody;
            badge = statusBadge;
            moraleLabel = moraleLabelText;
            safetyLabel = safetyLabelText;
            suppliesLabel = suppliesLabelText;
            moraleValue = moraleValueText;
            safetyValue = safetyValueText;
            suppliesValue = suppliesValueText;
            moraleFill = moraleFillImage;
            safetyFill = safetyFillImage;
            suppliesFill = suppliesFillImage;
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            var status = CampDashboardTextFormatter.BuildStatus(state, config, catalog);

            ApplyArtwork(panelArtwork, catalog.CampStatusPanelTexture);
            UiText.Set(title, catalog.CampStatusTitle);
            UiText.Set(label, status.Label);
            UiText.Set(body, status.Body);
            UiText.Set(badge, status.Badge);
            UiText.Set(moraleLabel, status.MoraleLabel);
            UiText.Set(safetyLabel, status.SafetyLabel);
            UiText.Set(suppliesLabel, status.SuppliesLabel);
            UiText.Set(moraleValue, status.MoraleValue);
            UiText.Set(safetyValue, status.SafetyValue);
            UiText.Set(suppliesValue, status.SuppliesValue);
            ApplyFill(moraleFill, moraleLabel, moraleValue, status.MoralePercent, catalog);
            ApplyFill(safetyFill, safetyLabel, safetyValue, status.SafetyPercent, catalog);
            ApplyFill(suppliesFill, suppliesLabel, suppliesValue, status.SuppliesPercent, catalog);
        }

        private static void ApplyFill(Image target, TextMeshProUGUI label, TextMeshProUGUI value, int percent, CampUiCatalogSO catalog)
        {
            var color = ColorForPercent(percent, catalog);
            var slider = FindSlider(target, label, value);
            if (slider != null)
            {
                slider.minValue = 0f;
                slider.maxValue = 1f;
                slider.wholeNumbers = false;
                slider.SetValueWithoutNotify(Mathf.Clamp01(percent / 100f));

                var fillGraphic = slider.fillRect != null ? slider.fillRect.GetComponent<Graphic>() : null;
                if (fillGraphic != null)
                {
                    fillGraphic.color = color;
                }
            }

            if (target == null) return;

            target.fillAmount = Mathf.Clamp01(percent / 100f);
            target.color = color;
        }

        private static Slider FindSlider(params Component[] sources)
        {
            for (var i = 0; i < sources.Length; i++)
            {
                var source = sources[i];
                if (source == null) continue;

                var parentSlider = source.GetComponentInParent<Slider>(true);
                if (parentSlider != null) return parentSlider;

                var parent = source.transform.parent;
                if (parent == null) continue;

                var childSlider = parent.GetComponentInChildren<Slider>(true);
                if (childSlider != null) return childSlider;
            }

            return null;
        }

        private static Color ColorForPercent(int percent, CampUiCatalogSO catalog)
        {
            if (catalog.LowResourceAlertPercentThreshold > 0 && percent <= catalog.LowResourceAlertPercentThreshold)
            {
                return catalog.Theme.Rust;
            }

            if (catalog.StatusWarningPercentThreshold > 0 && percent <= catalog.StatusWarningPercentThreshold)
            {
                return catalog.Theme.Amber;
            }

            return catalog.Theme.Teal;
        }

        private static void ApplyArtwork(RawImage target, Texture2D texture)
        {
            if (target == null) return;

            var hasTexture = texture != null;
            target.gameObject.SetActive(hasTexture);
            if (!hasTexture) return;

            target.texture = texture;
            target.color = Color.white;
        }
    }
}
