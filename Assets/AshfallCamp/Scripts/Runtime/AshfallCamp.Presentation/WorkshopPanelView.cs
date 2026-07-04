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
    public sealed class WorkshopPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI status;
        [SerializeField] private Image emptyPanel;
        [SerializeField] private TextMeshProUGUI emptyTitle;
        [SerializeField] private TextMeshProUGUI emptyBody;
        [SerializeField] private List<WorkshopItemBinding> items = new List<WorkshopItemBinding>();

        private Action<RepairItemRequest> _repairRequested;
        private Action<EquipItemRequest> _equipRequested;

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            TextMeshProUGUI statusLabel,
            Image emptyStatePanel,
            TextMeshProUGUI emptyStateTitle,
            TextMeshProUGUI emptyStateBody,
            IEnumerable<WorkshopItemBinding> itemBindings)
        {
            title = titleLabel;
            status = statusLabel;
            emptyPanel = emptyStatePanel;
            emptyTitle = emptyStateTitle;
            emptyBody = emptyStateBody;

            items.Clear();
            if (itemBindings != null)
            {
                items.AddRange(itemBindings);
            }

            ApplyHandlers();
        }

        private void Awake()
        {
            ApplyHandlers();
        }

        private void OnDestroy()
        {
            foreach (var item in items)
            {
                if (item != null)
                {
                    item.Clear();
                }
            }
        }

        public void SetRepairHandler(Action<RepairItemRequest> repairRequested)
        {
            _repairRequested = repairRequested;
            ApplyHandlers();
        }

        public void SetEquipHandler(Action<EquipItemRequest> equipRequested)
        {
            _equipRequested = equipRequested;
            ApplyHandlers();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            var targetSurvivorId = ResolveTargetSurvivorId(state);
            UiText.Set(title, catalog.WorkshopScreenTitle);
            UiText.Set(status, CampDashboardTextFormatter.FormatWorkshopStatus(state, config, catalog, targetSurvivorId));
            UiText.SetActive(emptyPanel, state.Inventory.Count == 0);
            UiText.Set(emptyTitle, catalog.WorkshopEmptyTitle);
            UiText.Set(emptyBody, catalog.WorkshopEmptyBody);

            var itemPresentations = CampDashboardTextFormatter.BuildWorkshopItems(state, config, catalog, targetSurvivorId);
            for (var i = 0; i < items.Count; i++)
            {
                items[i].Render(i < itemPresentations.Count ? itemPresentations[i] : null, catalog, targetSurvivorId);
            }
        }

        private void ApplyHandlers()
        {
            foreach (var item in items)
            {
                if (item != null)
                {
                    item.Wire(_repairRequested, _equipRequested);
                }
            }
        }

        private static string ResolveTargetSurvivorId(GameState state)
        {
            if (state == null) return string.Empty;
            foreach (var survivor in state.Survivors)
            {
                if (survivor.State == SurvivorActivityState.Idle)
                {
                    return survivor.Id;
                }
            }

            return state.Survivors.Count > 0 ? state.Survivors[0].Id : string.Empty;
        }

        [Serializable]
        public sealed class WorkshopItemBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI durabilityLabel;
            [SerializeField] private TextMeshProUGUI equippedLabel;
            [SerializeField] private TextMeshProUGUI brokenLabel;
            [SerializeField] private TextMeshProUGUI repairCostLabel;
            [SerializeField] private Button repairButton;
            [SerializeField] private TextMeshProUGUI repairButtonLabel;
            [SerializeField] private Button equipButton;
            [SerializeField] private TextMeshProUGUI equipButtonLabel;

            [NonSerialized] private Action<RepairItemRequest> _repairRequested;
            [NonSerialized] private Action<EquipItemRequest> _equipRequested;
            [NonSerialized] private UnityAction _repairClick;
            [NonSerialized] private UnityAction _equipClick;
            [NonSerialized] private string _itemUid = string.Empty;
            [NonSerialized] private string _targetSurvivorId = string.Empty;

            public WorkshopItemBinding()
            {
            }

            public WorkshopItemBinding(
                Image panel,
                TextMeshProUGUI nameLabel,
                TextMeshProUGUI durabilityLabel,
                TextMeshProUGUI equippedLabel,
                TextMeshProUGUI brokenLabel,
                TextMeshProUGUI repairCostLabel,
                Button repairButton,
                TextMeshProUGUI repairButtonLabel,
                Button equipButton,
                TextMeshProUGUI equipButtonLabel)
            {
                this.panel = panel;
                this.nameLabel = nameLabel;
                this.durabilityLabel = durabilityLabel;
                this.equippedLabel = equippedLabel;
                this.brokenLabel = brokenLabel;
                this.repairCostLabel = repairCostLabel;
                this.repairButton = repairButton;
                this.repairButtonLabel = repairButtonLabel;
                this.equipButton = equipButton;
                this.equipButtonLabel = equipButtonLabel;
            }

            public void Wire(Action<RepairItemRequest> repairRequested, Action<EquipItemRequest> equipRequested)
            {
                Clear();
                _repairRequested = repairRequested;
                _equipRequested = equipRequested;

                if (repairButton != null)
                {
                    _repairClick = OnRepairClicked;
                    repairButton.onClick.AddListener(_repairClick);
                }

                if (equipButton != null)
                {
                    _equipClick = OnEquipClicked;
                    equipButton.onClick.AddListener(_equipClick);
                }
            }

            public void Clear()
            {
                if (repairButton != null && _repairClick != null)
                {
                    repairButton.onClick.RemoveListener(_repairClick);
                }

                if (equipButton != null && _equipClick != null)
                {
                    equipButton.onClick.RemoveListener(_equipClick);
                }

                _repairClick = null;
                _equipClick = null;
            }

            public void Render(CampWorkshopItemPresentation item, CampUiCatalogSO catalog, string targetSurvivorId)
            {
                _itemUid = item != null ? item.ItemUid : string.Empty;
                _targetSurvivorId = targetSurvivorId ?? string.Empty;
                UiText.SetActive(panel, item != null);
                if (item == null) return;

                UiText.Set(nameLabel, item.Name);
                UiText.Set(durabilityLabel, CampDashboardTextFormatter.Format(catalog.WorkshopItemDurabilityFormat, item.Durability, item.MaxDurability));
                UiText.Set(equippedLabel, item.Equipped);
                UiText.Set(brokenLabel, item.BrokenLabel);
                UiText.Set(repairCostLabel, item.RepairCost);
                UiText.Set(repairButtonLabel, catalog.WorkshopRepairButton);
                UiText.Set(equipButtonLabel, catalog.WorkshopEquipButton);

                if (panel != null)
                {
                    panel.color = new Color(catalog.Theme.PaperDark.r, catalog.Theme.PaperDark.g, catalog.Theme.PaperDark.b, 0.74f);
                }

                if (nameLabel != null) nameLabel.color = catalog.Theme.Ink;
                if (durabilityLabel != null) durabilityLabel.color = catalog.Theme.MutedInk;
                if (equippedLabel != null) equippedLabel.color = catalog.Theme.Teal;
                if (brokenLabel != null) brokenLabel.color = catalog.Theme.Rust;
                if (repairCostLabel != null) repairCostLabel.color = catalog.Theme.MutedInk;
                if (repairButtonLabel != null) repairButtonLabel.color = catalog.Theme.Paper;
                if (equipButtonLabel != null) equipButtonLabel.color = catalog.Theme.Paper;
                if (repairButton != null) repairButton.interactable = item.CanRepair && _repairRequested != null;
                if (equipButton != null) equipButton.interactable = item.CanEquip && _equipRequested != null;
            }

            private void OnRepairClicked()
            {
                if (!string.IsNullOrWhiteSpace(_itemUid))
                {
                    _repairRequested?.Invoke(new RepairItemRequest { ItemUid = _itemUid });
                }
            }

            private void OnEquipClicked()
            {
                if (!string.IsNullOrWhiteSpace(_targetSurvivorId) && !string.IsNullOrWhiteSpace(_itemUid))
                {
                    _equipRequested?.Invoke(new EquipItemRequest { SurvivorId = _targetSurvivorId, ItemUid = _itemUid });
                }
            }
        }
    }
}
