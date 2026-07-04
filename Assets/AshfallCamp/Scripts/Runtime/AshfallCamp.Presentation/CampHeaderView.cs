using TMPro;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CampHeaderView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI brandTitle;
        [SerializeField] private TextMeshProUGUI brandSubtitle;
        [SerializeField] private TextMeshProUGUI brandBadge;

        public void ConfigureBindings(TextMeshProUGUI title, TextMeshProUGUI subtitle, TextMeshProUGUI badge)
        {
            brandTitle = title;
            brandSubtitle = subtitle;
            brandBadge = badge;
        }

        public void Render(CampUiCatalogSO catalog)
        {
            if (catalog == null) return;

            UiText.Set(brandTitle, catalog.BrandTitle);
            UiText.Set(brandSubtitle, catalog.BrandSubtitle);
            UiText.Set(brandBadge, catalog.BrandBannerText);
        }
    }
}
