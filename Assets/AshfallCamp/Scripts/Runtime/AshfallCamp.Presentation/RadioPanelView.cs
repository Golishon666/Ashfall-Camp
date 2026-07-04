using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class RadioPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI intelTitle;
        [SerializeField] private TextMeshProUGUI intelBody;
        [SerializeField] private TextMeshProUGUI broadcastTitle;
        [SerializeField] private TextMeshProUGUI broadcastCost;
        [SerializeField] private TextMeshProUGUI broadcastStatus;
        [SerializeField] private Button broadcastButton;
        [SerializeField] private TextMeshProUGUI broadcastButtonLabel;
        [SerializeField] private TextMeshProUGUI candidateListTitle;
        [SerializeField] private Image emptyPanel;
        [SerializeField] private TextMeshProUGUI emptyTitle;
        [SerializeField] private TextMeshProUGUI emptyBody;
        [SerializeField] private List<CandidateCardBinding> candidateCards = new List<CandidateCardBinding>();

        private Action<RecruitSurvivorViewRequest> _recruitRequested;
        private Action _broadcastRequested;
        private Action _skipRequested;
        private UnityAction _broadcastClick;
        private bool _useSkipAction;

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            TextMeshProUGUI intelTitleLabel,
            TextMeshProUGUI intelBodyLabel,
            TextMeshProUGUI broadcastTitleLabel,
            TextMeshProUGUI broadcastCostLabel,
            TextMeshProUGUI broadcastStatusLabel,
            Button broadcastActionButton,
            TextMeshProUGUI broadcastActionLabel,
            TextMeshProUGUI candidatesTitleLabel,
            Image emptyStatePanel,
            TextMeshProUGUI emptyStateTitle,
            TextMeshProUGUI emptyStateBody,
            IEnumerable<CandidateCardBinding> cards)
        {
            title = titleLabel;
            intelTitle = intelTitleLabel;
            intelBody = intelBodyLabel;
            broadcastTitle = broadcastTitleLabel;
            broadcastCost = broadcastCostLabel;
            broadcastStatus = broadcastStatusLabel;
            broadcastButton = broadcastActionButton;
            broadcastButtonLabel = broadcastActionLabel;
            candidateListTitle = candidatesTitleLabel;
            emptyPanel = emptyStatePanel;
            emptyTitle = emptyStateTitle;
            emptyBody = emptyStateBody;
            candidateCards.Clear();
            if (cards != null)
            {
                candidateCards.AddRange(cards);
            }

            WireBroadcastButton();
        }

        private void Awake()
        {
            WireBroadcastButton();
        }

        private void OnDestroy()
        {
            ClearBroadcastButton();
            foreach (var card in candidateCards)
            {
                if (card != null)
                {
                    card.Clear();
                }
            }
        }

        public void SetRecruitHandler(Action<RecruitSurvivorViewRequest> recruitRequested)
        {
            _recruitRequested = recruitRequested;
            WireCandidateCards();
        }

        public void SetBroadcastHandler(Action broadcastRequested)
        {
            _broadcastRequested = broadcastRequested;
            WireBroadcastButton();
        }

        public void SetSkipHandler(Action skipRequested)
        {
            _skipRequested = skipRequested;
            WireBroadcastButton();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            var radio = CampDashboardTextFormatter.BuildRadioScreen(state, config, catalog);
            UiText.Set(title, radio.Title);
            UiText.Set(intelTitle, radio.IntelTitle);
            UiText.Set(intelBody, radio.IntelBody);
            UiText.Set(broadcastTitle, radio.BroadcastTitle);
            UiText.Set(broadcastCost, radio.BroadcastCost);
            UiText.Set(broadcastStatus, radio.BroadcastStatus);
            UiText.Set(broadcastButtonLabel, radio.BroadcastButton);
            UiText.Set(candidateListTitle, radio.CandidateListTitle);
            UiText.Set(emptyTitle, radio.EmptyTitle);
            UiText.Set(emptyBody, radio.EmptyBody);

            if (broadcastButton != null)
            {
                _useSkipAction = radio.CanSkipCandidates;
                broadcastButton.interactable = radio.CanSkipCandidates
                    ? _skipRequested != null
                    : radio.CanBroadcast && _broadcastRequested != null;
            }

            if (broadcastStatus != null)
            {
                broadcastStatus.color = radio.CanBroadcast || radio.CanSkipCandidates ? catalog.Theme.Sage : catalog.Theme.Rust;
            }

            var hasCandidates = radio.Candidates.Count > 0;
            UiText.SetActive(emptyPanel, !hasCandidates);
            for (var i = 0; i < candidateCards.Count; i++)
            {
                candidateCards[i].Render(i < radio.Candidates.Count ? radio.Candidates[i] : null, catalog);
            }
        }

        private void WireBroadcastButton()
        {
            if (broadcastButton == null || _broadcastClick != null) return;
            _broadcastClick = OnBroadcastClicked;
            broadcastButton.onClick.AddListener(_broadcastClick);
        }

        private void WireCandidateCards()
        {
            foreach (var card in candidateCards)
            {
                if (card != null)
                {
                    card.Wire(OnCandidateClicked);
                }
            }
        }

        private void ClearBroadcastButton()
        {
            if (broadcastButton != null && _broadcastClick != null)
            {
                broadcastButton.onClick.RemoveListener(_broadcastClick);
            }

            _broadcastClick = null;
        }

        private void OnBroadcastClicked()
        {
            if (_useSkipAction)
            {
                _skipRequested?.Invoke();
                return;
            }

            _broadcastRequested?.Invoke();
        }

        private void OnCandidateClicked(string candidateId)
        {
            if (!string.IsNullOrWhiteSpace(candidateId))
            {
                _recruitRequested?.Invoke(new RecruitSurvivorViewRequest(candidateId));
            }
        }

        [Serializable]
        public sealed class CandidateCardBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private TextMeshProUGUI avatarLabel;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI metaLabel;
            [SerializeField] private TextMeshProUGUI skillLabel;
            [SerializeField] private TextMeshProUGUI traitsLabel;
            [SerializeField] private Button recruitButton;
            [SerializeField] private TextMeshProUGUI recruitButtonLabel;

            [NonSerialized] private string _candidateId = string.Empty;
            [NonSerialized] private Action<string> _selected;
            [NonSerialized] private UnityAction _click;

            public CandidateCardBinding()
            {
            }

            public CandidateCardBinding(
                Image panel,
                TextMeshProUGUI avatarLabel,
                TextMeshProUGUI nameLabel,
                TextMeshProUGUI metaLabel,
                TextMeshProUGUI skillLabel,
                TextMeshProUGUI traitsLabel,
                Button recruitButton,
                TextMeshProUGUI recruitButtonLabel)
            {
                this.panel = panel;
                this.avatarLabel = avatarLabel;
                this.nameLabel = nameLabel;
                this.metaLabel = metaLabel;
                this.skillLabel = skillLabel;
                this.traitsLabel = traitsLabel;
                this.recruitButton = recruitButton;
                this.recruitButtonLabel = recruitButtonLabel;
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
                _candidateId = candidate != null ? candidate.CandidateId : string.Empty;
                UiText.SetActive(panel, candidate != null);
                if (recruitButton != null) recruitButton.interactable = candidate != null && candidate.CanRecruit && _selected != null;
                if (candidate == null) return;

                UiText.Set(avatarLabel, candidate.Avatar);
                UiText.Set(nameLabel, candidate.Name);
                UiText.Set(metaLabel, candidate.Meta);
                UiText.Set(skillLabel, candidate.Skill);
                UiText.Set(traitsLabel, candidate.Traits);
                UiText.Set(recruitButtonLabel, candidate.RecruitButton);

                if (panel != null)
                {
                    panel.color = new Color(catalog.Theme.PaperDark.r, catalog.Theme.PaperDark.g, catalog.Theme.PaperDark.b, 0.68f);
                }

                if (avatarLabel != null) avatarLabel.color = catalog.Theme.Paper;
                if (nameLabel != null) nameLabel.color = catalog.Theme.Ink;
                if (metaLabel != null) metaLabel.color = catalog.Theme.MutedInk;
                if (skillLabel != null) skillLabel.color = catalog.Theme.Teal;
                if (traitsLabel != null) traitsLabel.color = catalog.Theme.MutedInk;
                if (recruitButtonLabel != null) recruitButtonLabel.color = catalog.Theme.Paper;
            }

            private void OnClicked()
            {
                if (!string.IsNullOrWhiteSpace(_candidateId))
                {
                    _selected?.Invoke(_candidateId);
                }
            }
        }
    }
}
