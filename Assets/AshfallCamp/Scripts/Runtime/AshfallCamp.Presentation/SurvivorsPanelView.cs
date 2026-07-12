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
    public sealed class SurvivorsPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI title;
        [SerializeField] private TextMeshProUGUI countLabel;
        [SerializeField] private Image emptyPanel;
        [SerializeField] private RawImage emptyPanelArtwork;
        [SerializeField] private TextMeshProUGUI emptyTitle;
        [SerializeField] private TextMeshProUGUI emptyBody;
        [SerializeField] private GameObject rosterRoot;
        [SerializeField] private GameObject detailRoot;
        [SerializeField] private Button viewDetailsButton;
        [SerializeField] private Button closeDetailButton;
        [SerializeField] private Image selectedSummaryPanel;
        [SerializeField] private RawImage selectedSummaryPortrait;
        [SerializeField] private TextMeshProUGUI selectedSummaryAvatar;
        [SerializeField] private TextMeshProUGUI selectedSummaryName;
        [SerializeField] private TextMeshProUGUI selectedSummaryRole;
        [SerializeField] private TextMeshProUGUI selectedSummaryLevel;
        [SerializeField] private TextMeshProUGUI selectedSummaryPower;
        [SerializeField] private TextMeshProUGUI selectedSummaryNote;
        [SerializeField] private List<SurvivorCardBinding> cards = new List<SurvivorCardBinding>();
        [SerializeField] private Image detailPanel;
        [SerializeField] private RawImage detailPanelArtwork;
        [SerializeField] private TextMeshProUGUI detailTitle;
        [SerializeField] private TextMeshProUGUI detailLevel;
        [SerializeField] private TextMeshProUGUI detailXp;
        [SerializeField] private TextMeshProUGUI detailStatus;
        [SerializeField] private TextMeshProUGUI detailBackground;
        [SerializeField] private TextMeshProUGUI detailTraits;
        [SerializeField] private TextMeshProUGUI detailWeapon;
        [SerializeField] private TextMeshProUGUI detailTreatment;
        [SerializeField] private TextMeshProUGUI detailStats;
        [SerializeField] private Slider detailHealthSlider;
        [SerializeField] private Slider detailFatigueSlider;
        [SerializeField] private Slider detailMoraleSlider;
        [SerializeField] private TextMeshProUGUI detailHealthValue;
        [SerializeField] private TextMeshProUGUI detailFatigueValue;
        [SerializeField] private TextMeshProUGUI detailMoraleValue;
        [SerializeField] private TextMeshProUGUI detailMedicineCost;
        [SerializeField] private RawImage detailPortrait;
        [SerializeField] private Button useMedicineButton;
        [SerializeField] private TextMeshProUGUI useMedicineButtonLabel;
        [Header("Reference screen")]
        [SerializeField] private Slider detailXpSlider;
        [SerializeField] private List<SurvivorSkillBinding> skillRows = new List<SurvivorSkillBinding>();
        [SerializeField] private List<EquipmentSlotBinding> equipmentSlots = new List<EquipmentSlotBinding>();
        [SerializeField] private TextMeshProUGUI inventoryTitle;
        [SerializeField] private List<InventoryFilterBinding> inventoryFilters = new List<InventoryFilterBinding>();
        [SerializeField] private List<InventorySectionBinding> inventorySections = new List<InventorySectionBinding>();
        [SerializeField] private RawImage selectedItemIcon;
        [SerializeField] private TextMeshProUGUI selectedItemName;
        [SerializeField] private TextMeshProUGUI selectedItemDescription;
        [SerializeField] private TextMeshProUGUI selectedItemStats;
        [SerializeField] private Button selectedItemEquipButton;
        [SerializeField] private TextMeshProUGUI selectedItemEquipButtonLabel;

        private string _selectedSurvivorId = string.Empty;
        private SurvivorDetailActionKind _selectedActionKind;
        private Action<UseMedicineRequest> _useMedicineRequested;
        private Action<StartRestRequest> _startRestRequested;
        private Action<StopRestRequest> _stopRestRequested;
        private Action<EquipItemRequest> _equipItemRequested;
        private UnityAction _useMedicineClick;
        private UnityAction _viewDetailsClick;
        private UnityAction _closeDetailClick;
        private UnityAction _equipItemClick;
        private GameState _lastState;
        private GameConfigSnapshot _lastConfig;
        private CampUiCatalogSO _lastCatalog;
        private SurvivorInventoryCategory _activeInventoryCategory = SurvivorInventoryCategory.All;
        private string _selectedItemUid = string.Empty;
        private string _selectedMaterialId = string.Empty;

        public void ConfigureBindings(
            TextMeshProUGUI titleLabel,
            TextMeshProUGUI survivorCountLabel,
            Image emptyStatePanel,
            TextMeshProUGUI emptyStateTitle,
            TextMeshProUGUI emptyStateBody,
            IEnumerable<SurvivorCardBinding> cardBindings,
            Image selectedDetailPanel,
            TextMeshProUGUI selectedDetailTitle,
            TextMeshProUGUI selectedDetailBackground,
            TextMeshProUGUI selectedDetailTraits,
            TextMeshProUGUI selectedDetailWeapon,
            TextMeshProUGUI selectedDetailStats,
            TextMeshProUGUI selectedDetailTreatment = null,
            TextMeshProUGUI selectedDetailMedicineCost = null,
            RawImage selectedDetailPortrait = null,
            Button selectedUseMedicineButton = null,
            TextMeshProUGUI selectedUseMedicineButtonLabel = null,
            RawImage emptyStateArtwork = null,
            RawImage selectedDetailArtwork = null,
            GameObject rosterScreenRoot = null,
            GameObject detailScreenRoot = null,
            Button selectedViewDetailsButton = null,
            Button selectedCloseDetailButton = null,
            TextMeshProUGUI selectedDetailLevel = null,
            TextMeshProUGUI selectedDetailXp = null,
            TextMeshProUGUI selectedDetailStatus = null,
            Slider selectedDetailHealthSlider = null,
            Slider selectedDetailFatigueSlider = null,
            Slider selectedDetailMoraleSlider = null,
            TextMeshProUGUI selectedDetailHealthValue = null,
            TextMeshProUGUI selectedDetailFatigueValue = null,
            TextMeshProUGUI selectedDetailMoraleValue = null,
            RawImage selectedRosterPortrait = null,
            Image selectedRosterPanel = null,
            TextMeshProUGUI selectedRosterAvatar = null,
            TextMeshProUGUI selectedRosterName = null,
            TextMeshProUGUI selectedRosterRole = null,
            TextMeshProUGUI selectedRosterLevel = null,
            TextMeshProUGUI selectedRosterPower = null,
            TextMeshProUGUI selectedRosterNote = null)
        {
            title = titleLabel;
            countLabel = survivorCountLabel;
            emptyPanel = emptyStatePanel;
            if (emptyStateArtwork != null)
            {
                emptyPanelArtwork = emptyStateArtwork;
            }

            emptyTitle = emptyStateTitle;
            emptyBody = emptyStateBody;
            cards.Clear();
            if (cardBindings != null)
            {
                cards.AddRange(cardBindings);
            }

            detailPanel = selectedDetailPanel;
            if (selectedDetailArtwork != null)
            {
                detailPanelArtwork = selectedDetailArtwork;
            }

            detailTitle = selectedDetailTitle;
            if (selectedDetailLevel != null)
            {
                detailLevel = selectedDetailLevel;
            }

            if (selectedDetailXp != null)
            {
                detailXp = selectedDetailXp;
            }

            if (selectedDetailStatus != null)
            {
                detailStatus = selectedDetailStatus;
            }

            detailBackground = selectedDetailBackground;
            detailTraits = selectedDetailTraits;
            detailWeapon = selectedDetailWeapon;
            if (selectedDetailTreatment != null)
            {
                detailTreatment = selectedDetailTreatment;
            }

            if (selectedDetailMedicineCost != null)
            {
                detailMedicineCost = selectedDetailMedicineCost;
            }

            if (selectedDetailPortrait != null)
            {
                detailPortrait = selectedDetailPortrait;
            }

            if (selectedUseMedicineButton != null)
            {
                useMedicineButton = selectedUseMedicineButton;
            }

            if (selectedUseMedicineButtonLabel != null)
            {
                useMedicineButtonLabel = selectedUseMedicineButtonLabel;
            }

            if (rosterScreenRoot != null)
            {
                rosterRoot = rosterScreenRoot;
            }

            if (detailScreenRoot != null)
            {
                detailRoot = detailScreenRoot;
            }

            if (selectedViewDetailsButton != null)
            {
                viewDetailsButton = selectedViewDetailsButton;
            }

            if (selectedCloseDetailButton != null)
            {
                closeDetailButton = selectedCloseDetailButton;
            }

            if (selectedDetailHealthSlider != null)
            {
                detailHealthSlider = selectedDetailHealthSlider;
            }

            if (selectedDetailFatigueSlider != null)
            {
                detailFatigueSlider = selectedDetailFatigueSlider;
            }

            if (selectedDetailMoraleSlider != null)
            {
                detailMoraleSlider = selectedDetailMoraleSlider;
            }

            if (selectedDetailHealthValue != null)
            {
                detailHealthValue = selectedDetailHealthValue;
            }

            if (selectedDetailFatigueValue != null)
            {
                detailFatigueValue = selectedDetailFatigueValue;
            }

            if (selectedDetailMoraleValue != null)
            {
                detailMoraleValue = selectedDetailMoraleValue;
            }

            if (selectedRosterPortrait != null)
            {
                selectedSummaryPortrait = selectedRosterPortrait;
            }

            if (selectedRosterPanel != null)
            {
                selectedSummaryPanel = selectedRosterPanel;
            }

            if (selectedRosterAvatar != null)
            {
                selectedSummaryAvatar = selectedRosterAvatar;
            }

            if (selectedRosterName != null)
            {
                selectedSummaryName = selectedRosterName;
            }

            if (selectedRosterRole != null)
            {
                selectedSummaryRole = selectedRosterRole;
            }

            if (selectedRosterLevel != null)
            {
                selectedSummaryLevel = selectedRosterLevel;
            }

            if (selectedRosterPower != null)
            {
                selectedSummaryPower = selectedRosterPower;
            }

            if (selectedRosterNote != null)
            {
                selectedSummaryNote = selectedRosterNote;
            }

            detailStats = selectedDetailStats;
            WireCards();
            WireUseMedicineButton();
            WireDetailNavigation();
        }

        private void Awake()
        {
            WireCards();
            WireUseMedicineButton();
            WireDetailNavigation();
            WireReferenceBindings();
        }

        private void OnDestroy()
        {
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.Clear();
                }
            }

            ClearUseMedicineButton();
            ClearDetailNavigation();
            ClearReferenceBindings();
        }

        public void ConfigureReferenceBindings(
            Slider xpSlider,
            IEnumerable<SurvivorSkillBinding> skills,
            IEnumerable<EquipmentSlotBinding> loadout,
            TextMeshProUGUI inventoryTitleLabel,
            IEnumerable<InventoryFilterBinding> filters,
            IEnumerable<InventorySectionBinding> sections,
            RawImage itemIcon,
            TextMeshProUGUI itemName,
            TextMeshProUGUI itemDescription,
            TextMeshProUGUI itemStats,
            Button equipButton,
            TextMeshProUGUI equipButtonLabel)
        {
            detailXpSlider = xpSlider;
            skillRows.Clear();
            if (skills != null) skillRows.AddRange(skills);
            equipmentSlots.Clear();
            if (loadout != null) equipmentSlots.AddRange(loadout);
            inventoryTitle = inventoryTitleLabel;
            inventoryFilters.Clear();
            if (filters != null) inventoryFilters.AddRange(filters);
            inventorySections.Clear();
            if (sections != null) inventorySections.AddRange(sections);
            selectedItemIcon = itemIcon;
            selectedItemName = itemName;
            selectedItemDescription = itemDescription;
            selectedItemStats = itemStats;
            selectedItemEquipButton = equipButton;
            selectedItemEquipButtonLabel = equipButtonLabel;
            WireReferenceBindings();
        }

        public void SetUseMedicineHandler(Action<UseMedicineRequest> useMedicineRequested)
        {
            _useMedicineRequested = useMedicineRequested;
            WireUseMedicineButton();
        }

        public void SetRestHandlers(Action<StartRestRequest> startRestRequested, Action<StopRestRequest> stopRestRequested)
        {
            _startRestRequested = startRestRequested;
            _stopRestRequested = stopRestRequested;
            WireUseMedicineButton();
        }

        public void SetEquipHandler(Action<EquipItemRequest> equipItemRequested)
        {
            _equipItemRequested = equipItemRequested;
            WireReferenceBindings();
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;
            _lastState = state;
            _lastConfig = config;
            _lastCatalog = catalog;

            UiText.Set(title, catalog.SurvivorsScreenTitle);
            UiText.Set(countLabel, CampDashboardTextFormatter.Format(catalog.SurvivorsCountFormat, state.Survivors.Count, Math.Max(1, state.SurvivorCap)));

            var hasSurvivors = state.Survivors.Count > 0;
            UiText.SetActive(emptyPanel, !hasSurvivors);
            ApplyArtwork(emptyPanelArtwork, hasSurvivors ? null : catalog.SurvivorsEmptyPanelTexture);
            UiText.Set(emptyTitle, catalog.SurvivorEmptyTitle);
            UiText.Set(emptyBody, catalog.SurvivorEmptyBody);

            if (hasSurvivors && !HasSelectedSurvivor(state))
            {
                _selectedSurvivorId = state.Survivors[0].Id;
                ShowRoster();
            }

            var cardPresentations = CampDashboardTextFormatter.BuildSurvivorCards(state, catalog);
            CampSurvivorCardPresentation selectedCard = null;
            for (var i = 0; i < cards.Count; i++)
            {
                var card = i < cardPresentations.Count ? cardPresentations[i] : null;
                var isSelected = string.Equals(_selectedSurvivorId, card != null ? card.SurvivorId : string.Empty, StringComparison.Ordinal);
                if (isSelected)
                {
                    selectedCard = card;
                }

                cards[i].Render(card, catalog, isSelected);
            }

            RenderDetail(FindSelectedSurvivor(state), selectedCard, state, config, catalog);
            RenderInventory(state, config, catalog);
        }

        private void WireCards()
        {
            foreach (var card in cards)
            {
                if (card != null)
                {
                    card.Wire(SelectSurvivor);
                }
            }
        }

        private void SelectSurvivor(string survivorId)
        {
            _selectedSurvivorId = survivorId ?? string.Empty;
            Rerender();
        }

        private void ShowRoster()
        {
            SetActive(rosterRoot, true);
            SetActive(detailRoot, false);
        }

        private void ShowDetail()
        {
            if (string.IsNullOrWhiteSpace(_selectedSurvivorId)) return;
            SetActive(rosterRoot, false);
            SetActive(detailRoot, true);
        }

        private bool HasSelectedSurvivor(GameState state)
        {
            return FindSelectedSurvivor(state) != null;
        }

        private SurvivorState FindSelectedSurvivor(GameState state)
        {
            if (state == null || string.IsNullOrWhiteSpace(_selectedSurvivorId)) return null;
            foreach (var survivor in state.Survivors)
            {
                if (string.Equals(survivor.Id, _selectedSurvivorId, StringComparison.Ordinal))
                {
                    return survivor;
                }
            }

            return null;
        }

        private void RenderDetail(SurvivorState survivor, CampSurvivorCardPresentation selectedCard, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            UiText.SetActive(detailPanel, survivor != null);
            ApplyArtwork(detailPanelArtwork, null);
            UiText.SetActive(viewDetailsButton, survivor != null);
            RenderSelectedSummary(null, null, false);
            if (survivor == null) return;

            var detail = CampDashboardTextFormatter.BuildSurvivorDetail(survivor, state, config, catalog);
            RenderSelectedSummary(selectedCard, detail, true);
            UiText.Set(detailTitle, detail.Title);
            UiText.Set(detailLevel, detail.LevelText);
            UiText.Set(detailXp, detail.XpText);
            SetSlider(detailXpSlider, detail.XpValue);
            UiText.Set(detailStatus, detail.StatusText);
            UiText.Set(detailBackground, detail.Background);
            UiText.Set(detailTraits, detail.Traits);
            UiText.Set(detailWeapon, detail.Weapon);
            UiText.Set(detailTreatment, detail.Treatment);
            UiText.Set(detailStats, detail.Stats);
            SetSlider(detailHealthSlider, detail.HealthValue);
            SetSlider(detailFatigueSlider, detail.FatigueValue);
            SetSlider(detailMoraleSlider, detail.MoraleValue);
            UiText.Set(detailHealthValue, detail.HealthText);
            UiText.Set(detailFatigueValue, detail.FatigueText);
            UiText.Set(detailMoraleValue, detail.MoraleText);
            UiText.Set(detailMedicineCost, detail.ActionCost);
            UiText.Set(useMedicineButtonLabel, detail.ActionButton);
            ApplyPortrait(detailPortrait, detail.Portrait);
            for (var i = 0; i < skillRows.Count; i++)
            {
                skillRows[i]?.Render(i < detail.Skills.Count ? detail.Skills[i] : null, catalog);
            }

            for (var i = 0; i < equipmentSlots.Count; i++)
            {
                equipmentSlots[i]?.Render(i < detail.Equipment.Count ? detail.Equipment[i] : null, catalog);
            }
            _selectedActionKind = detail.ActionKind;
            UiText.SetActive(detailMedicineCost, detail.ShowAction && !string.IsNullOrWhiteSpace(detail.ActionCost));
            if (useMedicineButton != null)
            {
                UiText.SetActive(useMedicineButton, detail.ShowAction);
                useMedicineButton.interactable = detail.ShowAction && detail.CanUseAction && HasSelectedActionHandler(detail.ActionKind);
            }
        }

        private void RenderSelectedSummary(CampSurvivorCardPresentation selectedCard, CampSurvivorDetailPresentation detail, bool hasSurvivor)
        {
            UiText.SetActive(selectedSummaryPanel, hasSurvivor);
            if (!hasSurvivor)
            {
                ApplyPortrait(selectedSummaryPortrait, null);
                UiText.SetActive(selectedSummaryAvatar, false);
                UiText.Set(selectedSummaryName, string.Empty);
                UiText.Set(selectedSummaryRole, string.Empty);
                UiText.Set(selectedSummaryLevel, string.Empty);
                UiText.Set(selectedSummaryPower, string.Empty);
                UiText.Set(selectedSummaryNote, string.Empty);
                return;
            }

            var name = selectedCard != null ? selectedCard.Name : string.Empty;
            var hasPortrait = ApplyPortrait(selectedSummaryPortrait, detail != null ? detail.Portrait : null);
            UiText.SetActive(selectedSummaryAvatar, !hasPortrait);
            UiText.Set(selectedSummaryAvatar, string.IsNullOrWhiteSpace(name) ? string.Empty : name.Substring(0, 1).ToUpperInvariant());
            UiText.Set(selectedSummaryName, name);
            UiText.Set(selectedSummaryRole, selectedCard != null ? selectedCard.Skill : string.Empty);
            UiText.Set(selectedSummaryLevel, detail != null ? detail.LevelText : string.Empty);
            UiText.Set(selectedSummaryPower, selectedCard != null ? "POWER\n" + selectedCard.PowerText : string.Empty);
            UiText.Set(selectedSummaryNote, detail != null ? detail.Background : string.Empty);
        }

        private void WireDetailNavigation()
        {
            if (viewDetailsButton != null && _viewDetailsClick == null)
            {
                _viewDetailsClick = ShowDetail;
                viewDetailsButton.onClick.AddListener(_viewDetailsClick);
            }

            if (closeDetailButton != null && _closeDetailClick == null)
            {
                _closeDetailClick = ShowRoster;
                closeDetailButton.onClick.AddListener(_closeDetailClick);
            }
        }

        private void ClearDetailNavigation()
        {
            if (viewDetailsButton != null && _viewDetailsClick != null)
            {
                viewDetailsButton.onClick.RemoveListener(_viewDetailsClick);
            }

            if (closeDetailButton != null && _closeDetailClick != null)
            {
                closeDetailButton.onClick.RemoveListener(_closeDetailClick);
            }

            _viewDetailsClick = null;
            _closeDetailClick = null;
        }

        private void WireUseMedicineButton()
        {
            if (useMedicineButton == null || _useMedicineClick != null) return;
            _useMedicineClick = OnUseMedicineClicked;
            useMedicineButton.onClick.AddListener(_useMedicineClick);
        }

        private void ClearUseMedicineButton()
        {
            if (useMedicineButton != null && _useMedicineClick != null)
            {
                useMedicineButton.onClick.RemoveListener(_useMedicineClick);
            }

            _useMedicineClick = null;
        }

        private void OnUseMedicineClicked()
        {
            if (string.IsNullOrWhiteSpace(_selectedSurvivorId)) return;

            if (_selectedActionKind == SurvivorDetailActionKind.UseMedicine)
            {
                _useMedicineRequested?.Invoke(new UseMedicineRequest { SurvivorId = _selectedSurvivorId });
            }
            else if (_selectedActionKind == SurvivorDetailActionKind.StartRest)
            {
                _startRestRequested?.Invoke(new StartRestRequest { SurvivorId = _selectedSurvivorId });
            }
            else if (_selectedActionKind == SurvivorDetailActionKind.StopRest)
            {
                _stopRestRequested?.Invoke(new StopRestRequest { SurvivorId = _selectedSurvivorId });
            }
        }

        private bool HasSelectedActionHandler(SurvivorDetailActionKind actionKind)
        {
            if (actionKind == SurvivorDetailActionKind.UseMedicine) return _useMedicineRequested != null;
            if (actionKind == SurvivorDetailActionKind.StartRest) return _startRestRequested != null;
            if (actionKind == SurvivorDetailActionKind.StopRest) return _stopRestRequested != null;
            return false;
        }

        private void RenderInventory(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            UiText.Set(inventoryTitle, catalog.SurvivorInventoryTitle);
            var items = CampDashboardTextFormatter.BuildSurvivorInventory(state, config, catalog, _selectedSurvivorId);
            EnsureSelectedInventoryItem(items);

            foreach (var filter in inventoryFilters)
            {
                filter?.Render(_activeInventoryCategory, catalog);
            }

            foreach (var section in inventorySections)
            {
                section?.Render(items, _activeInventoryCategory, _selectedItemUid, _selectedMaterialId, catalog, SelectInventoryItem);
            }

            CampInventoryItemPresentation selected = null;
            foreach (var item in items)
            {
                if (IsSelectedInventoryItem(item))
                {
                    selected = item;
                    break;
                }
            }

            RenderSelectedInventoryItem(selected, catalog);
        }

        private void EnsureSelectedInventoryItem(List<CampInventoryItemPresentation> items)
        {
            foreach (var item in items)
            {
                if (IsVisibleForFilter(item) && IsSelectedInventoryItem(item)) return;
            }

            _selectedItemUid = string.Empty;
            _selectedMaterialId = string.Empty;
            foreach (var item in items)
            {
                if (!IsVisibleForFilter(item)) continue;
                if (item.IsMaterial) _selectedMaterialId = item.ItemId;
                else _selectedItemUid = item.ItemUid;
                return;
            }
        }

        private bool IsVisibleForFilter(CampInventoryItemPresentation item)
        {
            return item != null && (_activeInventoryCategory == SurvivorInventoryCategory.All || item.Category == _activeInventoryCategory);
        }

        private bool IsSelectedInventoryItem(CampInventoryItemPresentation item)
        {
            if (item == null) return false;
            return item.IsMaterial
                ? string.Equals(item.ItemId, _selectedMaterialId, StringComparison.Ordinal)
                : string.Equals(item.ItemUid, _selectedItemUid, StringComparison.Ordinal);
        }

        private void SelectInventoryItem(CampInventoryItemPresentation item)
        {
            if (item == null) return;
            _selectedItemUid = item.IsMaterial ? string.Empty : item.ItemUid;
            _selectedMaterialId = item.IsMaterial ? item.ItemId : string.Empty;
            Rerender();
        }

        private void SelectInventoryFilter(SurvivorInventoryCategory category)
        {
            _activeInventoryCategory = category;
            _selectedItemUid = string.Empty;
            _selectedMaterialId = string.Empty;
            Rerender();
        }

        private void RenderSelectedInventoryItem(CampInventoryItemPresentation item, CampUiCatalogSO catalog)
        {
            var hasItem = item != null;
            ApplyPortrait(selectedItemIcon, hasItem ? item.Icon : null);
            UiText.Set(selectedItemName, hasItem ? item.Name : catalog.SurvivorInventoryEmptyLabel);
            UiText.Set(selectedItemDescription, hasItem ? item.Description : string.Empty);
            UiText.Set(selectedItemStats, hasItem ? item.Stats : string.Empty);
            UiText.Set(selectedItemEquipButtonLabel, catalog.SurvivorInventoryEquipButton);
            if (selectedItemEquipButton != null)
            {
                UiText.SetActive(selectedItemEquipButton, hasItem && !item.IsMaterial);
                selectedItemEquipButton.interactable = hasItem && !item.IsMaterial && item.CanEquip && _equipItemRequested != null;
            }
        }

        private void WireReferenceBindings()
        {
            foreach (var filter in inventoryFilters)
            {
                filter?.Wire(SelectInventoryFilter);
            }

            if (selectedItemEquipButton != null && _equipItemClick == null)
            {
                _equipItemClick = OnEquipItemClicked;
                selectedItemEquipButton.onClick.AddListener(_equipItemClick);
            }
        }

        private void ClearReferenceBindings()
        {
            foreach (var filter in inventoryFilters) filter?.Clear();
            foreach (var section in inventorySections) section?.Clear();
            if (selectedItemEquipButton != null && _equipItemClick != null)
            {
                selectedItemEquipButton.onClick.RemoveListener(_equipItemClick);
            }

            _equipItemClick = null;
        }

        private void OnEquipItemClicked()
        {
            if (string.IsNullOrWhiteSpace(_selectedSurvivorId) || string.IsNullOrWhiteSpace(_selectedItemUid)) return;
            _equipItemRequested?.Invoke(new EquipItemRequest { SurvivorId = _selectedSurvivorId, ItemUid = _selectedItemUid });
        }

        private void Rerender()
        {
            if (_lastState != null && _lastConfig != null && _lastCatalog != null)
            {
                Render(_lastState, _lastConfig, _lastCatalog);
            }
        }

        [Serializable]
        public sealed class SurvivorSkillBinding
        {
            [SerializeField] private TextMeshProUGUI label;
            [SerializeField] private Slider slider;
            [SerializeField] private TextMeshProUGUI value;

            public SurvivorSkillBinding(TextMeshProUGUI label, Slider slider, TextMeshProUGUI value)
            {
                this.label = label;
                this.slider = slider;
                this.value = value;
            }

            public void Render(CampSurvivorSkillPresentation skill, CampUiCatalogSO catalog)
            {
                var active = skill != null;
                if (label != null) label.transform.parent.gameObject.SetActive(active);
                if (!active) return;
                UiText.Set(label, skill.Label);
                UiText.Set(value, skill.Percent.ToString());
                SetSlider(slider, skill.Value);
            }
        }

        [Serializable]
        public sealed class EquipmentSlotBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private TextMeshProUGUI slotLabel;
            [SerializeField] private RawImage icon;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI durabilityLabel;

            public EquipmentSlotBinding(Image panel, TextMeshProUGUI slotLabel, RawImage icon, TextMeshProUGUI nameLabel, TextMeshProUGUI durabilityLabel)
            {
                this.panel = panel;
                this.slotLabel = slotLabel;
                this.icon = icon;
                this.nameLabel = nameLabel;
                this.durabilityLabel = durabilityLabel;
            }

            public void Render(CampEquippedItemPresentation item, CampUiCatalogSO catalog)
            {
                UiText.SetActive(panel, item != null);
                if (item == null) return;
                UiText.Set(slotLabel, item.SlotLabel);
                UiText.Set(nameLabel, item.Name);
                UiText.Set(durabilityLabel, item.MaxDurability > 0 ? item.Durability + "/" + item.MaxDurability : string.Empty);
                ApplyPortrait(icon, item.Icon);
            }
        }

        [Serializable]
        public sealed class InventoryFilterBinding
        {
            [SerializeField] private SurvivorInventoryCategory category;
            [SerializeField] private Image panel;
            [SerializeField] private Button button;
            [SerializeField] private TextMeshProUGUI label;
            [NonSerialized] private UnityAction _click;
            [NonSerialized] private Action<SurvivorInventoryCategory> _selected;

            public InventoryFilterBinding(SurvivorInventoryCategory category, Image panel, Button button, TextMeshProUGUI label)
            {
                this.category = category;
                this.panel = panel;
                this.button = button;
                this.label = label;
            }

            public void Wire(Action<SurvivorInventoryCategory> selected)
            {
                Clear();
                _selected = selected;
                if (button == null) return;
                _click = () => _selected?.Invoke(category);
                button.onClick.AddListener(_click);
            }

            public void Clear()
            {
                if (button != null && _click != null) button.onClick.RemoveListener(_click);
                _click = null;
            }

            public void Render(SurvivorInventoryCategory activeCategory, CampUiCatalogSO catalog)
            {
                var active = category == activeCategory;
                UiText.Set(label, category == SurvivorInventoryCategory.All ? "All" : category.ToString());
                if (panel != null) panel.color = active ? catalog.Theme.Sage : catalog.Theme.WithAlpha(catalog.Theme.PaperDark, 0.7f);
                if (button != null) button.interactable = !active;
            }
        }

        [Serializable]
        public sealed class InventorySectionBinding
        {
            [SerializeField] private SurvivorInventoryCategory category;
            [SerializeField] private GameObject root;
            [SerializeField] private TextMeshProUGUI title;
            [SerializeField] private TextMeshProUGUI count;
            [SerializeField] private List<InventoryItemBinding> items = new List<InventoryItemBinding>();

            public InventorySectionBinding(SurvivorInventoryCategory category, GameObject root, TextMeshProUGUI title, TextMeshProUGUI count, IEnumerable<InventoryItemBinding> items)
            {
                this.category = category;
                this.root = root;
                this.title = title;
                this.count = count;
                if (items != null) this.items.AddRange(items);
            }

            public void Render(List<CampInventoryItemPresentation> source, SurvivorInventoryCategory filter, string selectedUid, string selectedMaterialId, CampUiCatalogSO catalog, Action<CampInventoryItemPresentation> selected)
            {
                var visible = filter == SurvivorInventoryCategory.All || filter == category;
                var matches = new List<CampInventoryItemPresentation>();
                foreach (var item in source)
                {
                    if (item != null && item.Category == category) matches.Add(item);
                }

                if (root != null) root.SetActive(visible && matches.Count > 0);
                if (!visible) return;
                UiText.Set(title, category.ToString());
                UiText.Set(count, matches.Count.ToString());
                for (var i = 0; i < items.Count; i++)
                {
                    var item = i < matches.Count ? matches[i] : null;
                    var isSelected = item != null && (item.IsMaterial
                        ? string.Equals(item.ItemId, selectedMaterialId, StringComparison.Ordinal)
                        : string.Equals(item.ItemUid, selectedUid, StringComparison.Ordinal));
                    items[i].Render(item, isSelected, catalog, selected);
                }
            }

            public void Clear()
            {
                foreach (var item in items) item?.Clear();
            }
        }

        [Serializable]
        public sealed class InventoryItemBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private Button button;
            [SerializeField] private RawImage icon;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI countLabel;
            [NonSerialized] private UnityAction _click;
            [NonSerialized] private CampInventoryItemPresentation _item;
            [NonSerialized] private Action<CampInventoryItemPresentation> _selected;

            public InventoryItemBinding(Image panel, Button button, RawImage icon, TextMeshProUGUI nameLabel, TextMeshProUGUI countLabel)
            {
                this.panel = panel;
                this.button = button;
                this.icon = icon;
                this.nameLabel = nameLabel;
                this.countLabel = countLabel;
            }

            public void Render(CampInventoryItemPresentation item, bool isSelected, CampUiCatalogSO catalog, Action<CampInventoryItemPresentation> selected)
            {
                _item = item;
                UiText.SetActive(panel, item != null);
                Clear();
                if (item == null) return;
                _selected = selected;
                UiText.Set(nameLabel, item.Name);
                UiText.Set(countLabel, item.IsMaterial ? item.Count.ToString() : string.Empty);
                ApplyPortrait(icon, item.Icon);
                if (panel != null) panel.color = isSelected ? catalog.Theme.Amber : catalog.Theme.WithAlpha(catalog.Theme.PaperDark, 0.82f);
                if (button != null)
                {
                    _click = () => _selected?.Invoke(_item);
                    button.onClick.AddListener(_click);
                    button.interactable = !isSelected;
                }
            }

            public void Clear()
            {
                if (button != null && _click != null) button.onClick.RemoveListener(_click);
                _click = null;
            }
        }

        [Serializable]
        public sealed class SurvivorCardBinding
        {
            [SerializeField] private Image panel;
            [SerializeField] private RawImage cardArtwork;
            [SerializeField] private Button button;
            [SerializeField] private RawImage portrait;
            [SerializeField] private TextMeshProUGUI avatarLabel;
            [SerializeField] private TextMeshProUGUI nameLabel;
            [SerializeField] private TextMeshProUGUI stateLabel;
            [SerializeField] private TextMeshProUGUI skillLabel;
            [SerializeField] private TextMeshProUGUI levelLabel;
            [SerializeField] private TextMeshProUGUI powerLabel;
            [SerializeField] private Slider healthSlider;
            [SerializeField] private Slider fatigueSlider;
            [SerializeField] private Slider moraleSlider;
            [SerializeField] private TextMeshProUGUI healthValueLabel;
            [SerializeField] private TextMeshProUGUI fatigueValueLabel;
            [SerializeField] private TextMeshProUGUI moraleValueLabel;

            [NonSerialized] private string _survivorId = string.Empty;
            [NonSerialized] private UnityAction _cachedClick;
            [NonSerialized] private Action<string> _selected;

            public SurvivorCardBinding()
            {
            }

            public SurvivorCardBinding(Image panel, Button button, TextMeshProUGUI avatarLabel, TextMeshProUGUI nameLabel, TextMeshProUGUI stateLabel, TextMeshProUGUI skillLabel)
                : this(panel, button, null, avatarLabel, nameLabel, stateLabel, skillLabel)
            {
            }

            public SurvivorCardBinding(Image panel, Button button, RawImage portrait, TextMeshProUGUI avatarLabel, TextMeshProUGUI nameLabel, TextMeshProUGUI stateLabel, TextMeshProUGUI skillLabel)
                : this(panel, button, portrait, avatarLabel, nameLabel, stateLabel, skillLabel, null, null, null, null, null, null)
            {
            }

            public SurvivorCardBinding(
                Image panel,
                Button button,
                RawImage portrait,
                TextMeshProUGUI avatarLabel,
                TextMeshProUGUI nameLabel,
                TextMeshProUGUI stateLabel,
                TextMeshProUGUI skillLabel,
                TextMeshProUGUI levelLabel,
                TextMeshProUGUI powerLabel,
                Slider healthSlider,
                Slider fatigueSlider,
                TextMeshProUGUI healthValueLabel,
                TextMeshProUGUI fatigueValueLabel)
                : this(panel, null, button, portrait, avatarLabel, nameLabel, stateLabel, skillLabel, levelLabel, powerLabel, healthSlider, fatigueSlider, healthValueLabel, fatigueValueLabel)
            {
            }

            public SurvivorCardBinding(
                Image panel,
                RawImage cardArtwork,
                Button button,
                RawImage portrait,
                TextMeshProUGUI avatarLabel,
                TextMeshProUGUI nameLabel,
                TextMeshProUGUI stateLabel,
                TextMeshProUGUI skillLabel,
                TextMeshProUGUI levelLabel,
                TextMeshProUGUI powerLabel,
                Slider healthSlider,
                Slider fatigueSlider,
                TextMeshProUGUI healthValueLabel,
                TextMeshProUGUI fatigueValueLabel)
            {
                this.panel = panel;
                this.cardArtwork = cardArtwork;
                this.button = button;
                this.portrait = portrait;
                this.avatarLabel = avatarLabel;
                this.nameLabel = nameLabel;
                this.stateLabel = stateLabel;
                this.skillLabel = skillLabel;
                this.levelLabel = levelLabel;
                this.powerLabel = powerLabel;
                this.healthSlider = healthSlider;
                this.fatigueSlider = fatigueSlider;
                this.healthValueLabel = healthValueLabel;
                this.fatigueValueLabel = fatigueValueLabel;
            }

            public SurvivorCardBinding(
                Image panel,
                RawImage cardArtwork,
                Button button,
                RawImage portrait,
                TextMeshProUGUI avatarLabel,
                TextMeshProUGUI nameLabel,
                TextMeshProUGUI stateLabel,
                TextMeshProUGUI skillLabel,
                TextMeshProUGUI levelLabel,
                TextMeshProUGUI powerLabel,
                Slider healthSlider,
                Slider fatigueSlider,
                Slider moraleSlider,
                TextMeshProUGUI healthValueLabel,
                TextMeshProUGUI fatigueValueLabel,
                TextMeshProUGUI moraleValueLabel)
                : this(panel, cardArtwork, button, portrait, avatarLabel, nameLabel, stateLabel, skillLabel, levelLabel, powerLabel, healthSlider, fatigueSlider, healthValueLabel, fatigueValueLabel)
            {
                this.moraleSlider = moraleSlider;
                this.moraleValueLabel = moraleValueLabel;
            }

            public void Wire(Action<string> selected)
            {
                if (button == null) return;
                Clear();
                _selected = selected;
                _cachedClick = OnClicked;
                button.onClick.AddListener(_cachedClick);
            }

            public void Clear()
            {
                if (button != null && _cachedClick != null)
                {
                    button.onClick.RemoveListener(_cachedClick);
                }

                _cachedClick = null;
            }

            public void Render(CampSurvivorCardPresentation survivor, CampUiCatalogSO catalog, bool isSelected)
            {
                _survivorId = survivor != null ? survivor.SurvivorId : string.Empty;
                UiText.SetActive(panel, survivor != null);
                if (survivor == null)
                {
                    ApplyArtwork(cardArtwork, null);
                    return;
                }

                ApplyArtwork(cardArtwork, null);
                if (panel != null)
                {
                    panel.color = isSelected
                        ? catalog.Theme.WithAlpha(catalog.Theme.Teal, catalog.Theme.SurvivorSelectedPanelAlpha)
                        : catalog.Theme.WithAlpha(catalog.Theme.PaperDark, catalog.Theme.SurvivorInactivePanelAlpha);
                }

                var textColor = catalog.Theme.Ink;
                var hasPortrait = ApplyPortrait(portrait, survivor.Portrait);
                UiText.SetActive(avatarLabel, !hasPortrait);
                UiText.Set(avatarLabel, survivor.Avatar);
                UiText.Set(nameLabel, survivor.Name);
                UiText.Set(stateLabel, survivor.State);
                UiText.Set(skillLabel, survivor.Skill);
                UiText.Set(levelLabel, survivor.LevelText);
                UiText.Set(powerLabel, survivor.PowerText);
                SetSlider(healthSlider, survivor.HealthValue);
                SetSlider(fatigueSlider, survivor.FatigueValue);
                SetSlider(moraleSlider, survivor.MoraleValue);
                UiText.Set(healthValueLabel, survivor.HealthText);
                UiText.Set(fatigueValueLabel, survivor.FatigueText);
                UiText.Set(moraleValueLabel, survivor.MoraleText);
                if (avatarLabel != null) avatarLabel.color = catalog.Theme.Paper;
                if (nameLabel != null) nameLabel.color = textColor;
                if (stateLabel != null) stateLabel.color = catalog.Theme.MutedInk;
                if (skillLabel != null) skillLabel.color = catalog.Theme.MutedInk;
                if (levelLabel != null) levelLabel.color = catalog.Theme.MutedInk;
                if (powerLabel != null) powerLabel.color = textColor;
                if (healthValueLabel != null) healthValueLabel.color = catalog.Theme.MutedInk;
                if (fatigueValueLabel != null) fatigueValueLabel.color = catalog.Theme.MutedInk;
                if (moraleValueLabel != null) moraleValueLabel.color = catalog.Theme.MutedInk;
                if (button != null) button.interactable = !isSelected;
            }

            private void OnClicked()
            {
                if (!string.IsNullOrWhiteSpace(_survivorId))
                {
                    _selected?.Invoke(_survivorId);
                }
            }
        }

        private static bool ApplyPortrait(RawImage target, Texture2D portrait)
        {
            if (target == null) return false;

            var hasPortrait = portrait != null;
            target.gameObject.SetActive(hasPortrait);
            if (hasPortrait)
            {
                target.texture = portrait;
                target.color = Color.white;
            }

            return hasPortrait;
        }

        private static bool ApplyArtwork(RawImage target, Texture2D texture)
        {
            if (target == null) return false;

            var hasTexture = texture != null;
            target.gameObject.SetActive(hasTexture);
            if (hasTexture)
            {
                target.texture = texture;
                target.color = Color.white;
            }

            return hasTexture;
        }

        private static void SetActive(GameObject target, bool active)
        {
            if (target != null && target.activeSelf != active)
            {
                target.SetActive(active);
            }
        }

        private static void SetSlider(Slider slider, float value)
        {
            if (slider == null) return;
            var clamped = Mathf.Clamp01(value);
            slider.SetValueWithoutNotify(clamped);
            if (slider.fillRect != null)
            {
                slider.fillRect.anchorMin = new Vector2(0f, 0f);
                slider.fillRect.anchorMax = new Vector2(clamped, 1f);
                slider.fillRect.offsetMin = Vector2.zero;
                slider.fillRect.offsetMax = Vector2.zero;
            }

            if (slider.handleRect != null)
            {
                slider.handleRect.anchorMin = new Vector2(clamped, 0.5f);
                slider.handleRect.anchorMax = new Vector2(clamped, 0.5f);
                slider.handleRect.anchoredPosition = Vector2.zero;
            }
        }
    }
}
