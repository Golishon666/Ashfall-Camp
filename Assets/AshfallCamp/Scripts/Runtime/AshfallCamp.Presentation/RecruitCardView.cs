using System;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RecruitCardView : MonoBehaviour
    {
        [SerializeField] private Image panel;
        [SerializeField] private TextMeshProUGUI nameLabel;
        [SerializeField] private TextMeshProUGUI metaLabel;
        [SerializeField] private TextMeshProUGUI descriptionLabel;
        [SerializeField] private TextMeshProUGUI skillLabel;
        [SerializeField] private RawImage portrait;
        [SerializeField] private Button recruitButton;
        [SerializeField] private TextMeshProUGUI recruitButtonLabel;

        [NonSerialized] private string _candidateId = string.Empty;
        [NonSerialized] private Action<string> _selected;
        [NonSerialized] private UnityAction _click;

        public void ConfigureBindings(
            Image cardPanel,
            TextMeshProUGUI nameText,
            TextMeshProUGUI metaText,
            TextMeshProUGUI descriptionText,
            TextMeshProUGUI skillText,
            RawImage portraitImage,
            Button recruitActionButton,
            TextMeshProUGUI recruitActionLabel)
        {
            panel = cardPanel;
            nameLabel = nameText;
            metaLabel = metaText;
            descriptionLabel = descriptionText;
            skillLabel = skillText;
            portrait = portraitImage;
            recruitButton = recruitActionButton;
            recruitButtonLabel = recruitActionLabel;
        }

        private void Awake()
        {
            EnsureBindings();
        }

        private void OnDestroy()
        {
            Clear();
        }

        public void Wire(Action<string> selected)
        {
            Clear();
            _selected = selected;
            if (recruitButton == null) return;

            _click = OnClicked;
            recruitButton.onClick.AddListener(_click);
        }

        public void Clear()
        {
            if (recruitButton != null && _click != null)
            {
                recruitButton.onClick.RemoveListener(_click);
            }

            _click = null;
        }

        public void Render(CampRadioCandidatePresentation candidate, CampUiCatalogSO catalog)
        {
            EnsureBindings();

            _candidateId = candidate != null ? candidate.CandidateId : string.Empty;
            gameObject.SetActive(candidate != null);
            if (recruitButton != null)
            {
                recruitButton.interactable = candidate != null && candidate.CanRecruit && _selected != null;
            }

            if (candidate == null) return;

            var hasPortrait = ApplyPortrait(portrait, candidate.Portrait);
            UiText.Set(nameLabel, candidate.Name);
            UiText.Set(metaLabel, candidate.Meta);
            UiText.Set(descriptionLabel, candidate.Traits);
            UiText.Set(skillLabel, candidate.Skill);
            UiText.Set(recruitButtonLabel, candidate.RecruitButton);

            if (panel != null && catalog != null)
            {
                panel.color = catalog.Theme.WithAlpha(catalog.Theme.PaperDark, catalog.Theme.RadioCandidatePanelAlpha);
            }

            if (portrait != null && !hasPortrait)
            {
                portrait.gameObject.SetActive(false);
            }

            if (catalog == null) return;
            if (nameLabel != null) nameLabel.color = catalog.Theme.Ink;
            if (metaLabel != null) metaLabel.color = catalog.Theme.Teal;
            if (descriptionLabel != null) descriptionLabel.color = catalog.Theme.MutedInk;
            if (skillLabel != null) skillLabel.color = catalog.Theme.MutedInk;
            if (recruitButtonLabel != null) recruitButtonLabel.color = catalog.Theme.Paper;
        }

        private void OnClicked()
        {
            if (!string.IsNullOrWhiteSpace(_candidateId))
            {
                _selected?.Invoke(_candidateId);
            }
        }

        private void EnsureBindings()
        {
            if (panel == null)
            {
                panel = GetComponent<Image>();
            }

            if (nameLabel == null) nameLabel = FindText("Name");
            if (metaLabel == null) metaLabel = FindText("Role");
            if (descriptionLabel == null) descriptionLabel = FindText("Description");
            if (skillLabel == null) skillLabel = FindText("SkillsLabel");
            if (portrait == null) portrait = FindComponent<RawImage>("Portrait");
            if (recruitButton == null) recruitButton = FindComponent<Button>("Button_Recruit");
            if (recruitButtonLabel == null && recruitButton != null)
            {
                recruitButtonLabel = FindText(recruitButton.transform, "Label");
            }
        }

        private TextMeshProUGUI FindText(string objectName)
        {
            return FindText(transform, objectName);
        }

        private static TextMeshProUGUI FindText(Transform root, string objectName)
        {
            if (root == null || string.IsNullOrWhiteSpace(objectName)) return null;
            var match = FindChild(root, objectName);
            return match != null ? match.GetComponent<TextMeshProUGUI>() : null;
        }

        private T FindComponent<T>(string objectName) where T : Component
        {
            var match = FindChild(transform, objectName);
            return match != null ? match.GetComponent<T>() : null;
        }

        private static Transform FindChild(Transform root, string objectName)
        {
            if (root == null) return null;
            if (string.Equals(root.name, objectName, StringComparison.Ordinal)) return root;

            for (var i = 0; i < root.childCount; i++)
            {
                var found = FindChild(root.GetChild(i), objectName);
                if (found != null) return found;
            }

            return null;
        }

        private static bool ApplyPortrait(RawImage target, Texture2D texture)
        {
            if (target == null) return false;

            var hasPortrait = texture != null;
            target.gameObject.SetActive(hasPortrait);
            if (hasPortrait)
            {
                target.texture = texture;
                target.color = Color.white;
            }

            return hasPortrait;
        }
    }
}
