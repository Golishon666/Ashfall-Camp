using TMPro;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    internal static class UiText
    {
        public static void Set(TextMeshProUGUI target, string value)
        {
            if (target != null)
            {
                target.text = value ?? string.Empty;
            }
        }

        public static void SetActive(Component target, bool active)
        {
            if (target != null)
            {
                target.gameObject.SetActive(active);
            }
        }
    }
}
