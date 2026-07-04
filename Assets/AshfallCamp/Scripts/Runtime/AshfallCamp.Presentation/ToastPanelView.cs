using DG.Tweening;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class ToastPanelView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup canvasGroup;
        [SerializeField] private Image accent;
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI body;

        private Sequence _sequence;

        public void ConfigureBindings(CanvasGroup group, Image accentImage, TextMeshProUGUI titleLabel, TextMeshProUGUI bodyLabel)
        {
            canvasGroup = group;
            accent = accentImage;
            title = titleLabel;
            body = bodyLabel;
        }

        private void OnDestroy()
        {
            KillSequence();
        }

        public void HideImmediate()
        {
            KillSequence();
            ApplyAlpha(0);
            gameObject.SetActive(false);
        }

        public void Show(CampToastRequest request, CampUiCatalogSO catalog)
        {
            if (request == null || catalog == null || catalog.Toast == null) return;
            var entry = ResolveEntry(request.Id, catalog);
            if (entry == null) return;

            UiText.Set(title, CampDashboardTextFormatter.Format(entry.TitleFormat, request.Args.ToArray()));
            UiText.Set(body, CampDashboardTextFormatter.Format(entry.BodyFormat, request.Args.ToArray()));
            if (accent != null)
            {
                accent.color = entry.ToneColor;
            }

            KillSequence();
            gameObject.SetActive(true);
            if (canvasGroup == null)
            {
                return;
            }

            canvasGroup.interactable = false;
            canvasGroup.blocksRaycasts = false;

            if (!catalog.Toast.Enabled || catalog.Toast.FadeSeconds <= 0)
            {
                ApplyAlpha(1);
                return;
            }

            ApplyAlpha(0);
            _sequence = DOTween.Sequence()
                .SetEase(catalog.Toast.Ease)
                .SetUpdate(catalog.Toast.UseUnscaledTime)
                .SetTarget(canvasGroup)
                .SetLink(gameObject);
            _sequence.Append(DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 1, catalog.Toast.FadeSeconds));
            _sequence.AppendInterval(catalog.Toast.VisibleSeconds);
            _sequence.Append(DOTween.To(() => canvasGroup.alpha, value => canvasGroup.alpha = value, 0, catalog.Toast.FadeSeconds));
            _sequence.OnComplete(() => gameObject.SetActive(false));
        }

        private void ApplyAlpha(float alpha)
        {
            if (canvasGroup != null)
            {
                canvasGroup.alpha = alpha;
            }
        }

        private void KillSequence()
        {
            if (_sequence != null && _sequence.IsActive())
            {
                _sequence.Kill();
            }

            _sequence = null;
            if (canvasGroup != null)
            {
                DOTween.Kill(canvasGroup);
            }
        }

        private static CampToastUiEntry ResolveEntry(string id, CampUiCatalogSO catalog)
        {
            if (catalog.ToastMessages == null || string.IsNullOrWhiteSpace(id)) return null;
            foreach (var entry in catalog.ToastMessages)
            {
                if (entry != null && entry.Id == id)
                {
                    return entry;
                }
            }

            return null;
        }
    }
}
