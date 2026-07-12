using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using AshfallCamp.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Editor
{
    public static class SurvivorsReferenceUiBuilder
    {
        public const string SurvivorsPrefabPath = "Assets/AshfallCamp/Prefabs/UI/SurvivorsScreen.prefab";
        public const string BottomNavigationPrefabPath = "Assets/AshfallCamp/Prefabs/UI/Main/BottomNavigation.prefab";
        public const string DashboardPrefabPath = "Assets/AshfallCamp/Prefabs/UI/PF_CampDashboard.prefab";
        public const string CatalogPath = "Assets/AshfallCamp/UI/CampUiCatalog.asset";

        private static readonly Color Paper = new Color32(0xE8, 0xD8, 0xB8, 0xFF);
        private static readonly Color PaperLight = new Color32(0xF0, 0xE4, 0xCB, 0xFF);
        private static readonly Color Ink = new Color32(0x29, 0x27, 0x21, 0xFF);
        private static readonly Color MutedInk = new Color32(0x65, 0x59, 0x47, 0xFF);
        private static readonly Color Dark = new Color32(0x18, 0x1D, 0x1A, 0xF2);
        private static readonly Color Sage = new Color32(0x63, 0x76, 0x45, 0xFF);
        private static readonly Color Amber = new Color32(0xC7, 0x8A, 0x2A, 0xFF);
        private static readonly Color Line = new Color32(0x72, 0x60, 0x43, 0x88);

        [MenuItem("Tools/Ashfall Camp/UI/Rebuild Survivors Reference Screen")]
        public static void BuildAll()
        {
            ConfigureCatalog();
            BuildSurvivorsPrefab();
            BuildBottomNavigationPrefab();
            UpdateDashboardPrefab();
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Rebuilt Ashfall Camp survivors reference UI and six-tab navigation.");
        }

        public static void BuildSurvivorsPrefab()
        {
            var root = new GameObject("SurvivorsScreen", typeof(RectTransform), typeof(CanvasGroup), typeof(SurvivorsPanelView));
            try
            {
                SetRect(root.GetComponent<RectTransform>(), 0f, 0f, 1840f, 790f);
                var background = CreateRawImage(root.transform, "ParchmentBackground", 0f, 0f, 1840f, 790f, LoadTexture("Assets/AshfallCamp/Art/UI/Production/ScreenBackplatesClean/ui_backplate_clean_parchment_01.png"));
                background.color = Color.white;

                var left = CreatePanel(root.transform, "RosterColumn", 0f, 0f, 530f, 790f, PaperLight);
                var center = CreatePanel(root.transform, "SurvivorColumn", 540f, 0f, 590f, 790f, PaperLight);
                var right = CreatePanel(root.transform, "InventoryColumn", 1140f, 0f, 700f, 790f, PaperLight);
                AddOutline(left.gameObject);
                AddOutline(center.gameObject);
                AddOutline(right.gameObject);

                var title = CreateText(left.transform, "Title", "Survivors", 26f, 18f, 300f, 38f, 31, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
                var count = CreateText(left.transform, "Count", "6 / 12", 400f, 22f, 105f, 30f, 18, FontStyles.Normal, MutedInk, TextAlignmentOptions.Right);
                var rosterContent = CreateVerticalScrollContent(left.transform, "RosterScroll", 14f, 62f, 502f, 708f, 770f);
                var cards = new List<SurvivorsPanelView.SurvivorCardBinding>();
                for (var i = 0; i < 6; i++)
                {
                    cards.Add(CreateSurvivorCard(rosterContent, i, i * 112f));
                }
                var recruit = CreateButton(rosterContent, "RecruitSurvivorButton", 0f, 686f, 502f, 58f, "+  Recruit Survivor", 18);
                recruit.interactable = false;

                var portrait = CreateRawImage(center.transform, "SelectedPortrait", 18f, 18f, 220f, 236f, null);
                AddOutline(portrait.gameObject);
                var detailTitle = CreateText(center.transform, "SelectedName", "Asha", 258f, 22f, 300f, 46f, 38, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
                var level = CreateText(center.transform, "SelectedLevel", "Level 3", 258f, 72f, 140f, 32f, 24, FontStyles.Bold, new Color32(0x2F, 0x70, 0x74, 0xFF), TextAlignmentOptions.Left);
                var xp = CreateText(center.transform, "SelectedXp", "420 / 900", 430f, 78f, 130f, 24f, 16, FontStyles.Normal, Ink, TextAlignmentOptions.Right);
                var xpSlider = CreateSlider(center.transform, "XpSlider", 258f, 107f, 302f, 12f, new Color32(0x2F, 0x79, 0x79, 0xFF));
                var backgroundLabel = CreateText(center.transform, "Background", "Background\nMechanic", 258f, 132f, 300f, 50f, 17, FontStyles.Normal, Ink, TextAlignmentOptions.TopLeft);
                var traits = CreateText(center.transform, "Traits", "Traits\nResourceful    Quick Learner", 258f, 188f, 300f, 58f, 17, FontStyles.Normal, Ink, TextAlignmentOptions.TopLeft);

                CreateRule(center.transform, 18f, 268f, 554f);
                var skills = new List<SurvivorsPanelView.SurvivorSkillBinding>();
                var skillNames = new[] { "Scavenging", "Melee", "Firearms", "Survival", "Mechanics", "Medicine" };
                for (var i = 0; i < skillNames.Length; i++)
                {
                    var y = 282f + i * 38f;
                    var skillLabel = CreateText(center.transform, "SkillLabel_" + skillNames[i], skillNames[i], 28f, y, 132f, 28f, 17, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
                    var skillSlider = CreateSlider(center.transform, "SkillSlider_" + skillNames[i], 168f, y + 8f, 324f, 12f, Sage);
                    var skillValue = CreateText(center.transform, "SkillValue_" + skillNames[i], "0", 505f, y, 50f, 28f, 17, FontStyles.Bold, Ink, TextAlignmentOptions.Right);
                    skills.Add(new SurvivorsPanelView.SurvivorSkillBinding(skillLabel, skillSlider, skillValue));
                }

                CreateRule(center.transform, 18f, 522f, 554f);
                var loadout = new List<SurvivorsPanelView.EquipmentSlotBinding>();
                var slotLabels = new[] { "Weapon", "Armor", "Utility", "Backpack" };
                for (var i = 0; i < slotLabels.Length; i++)
                {
                    var slot = CreatePanel(center.transform, "Loadout_" + slotLabels[i], 18f + i * 138f, 542f, 128f, 174f, Paper);
                    AddOutline(slot.gameObject);
                    var slotLabel = CreateText(slot.transform, "SlotLabel", slotLabels[i], 4f, 5f, 120f, 24f, 16, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
                    var icon = CreateRawImage(slot.transform, "Icon", 24f, 33f, 80f, 80f, null);
                    var itemName = CreateText(slot.transform, "Name", "Empty", 4f, 117f, 120f, 30f, 14, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
                    var durability = CreateText(slot.transform, "Durability", string.Empty, 4f, 146f, 120f, 20f, 12, FontStyles.Normal, MutedInk, TextAlignmentOptions.Center);
                    loadout.Add(new SurvivorsPanelView.EquipmentSlotBinding(slot, slotLabel, icon, itemName, durability));
                }

                var inventoryTitle = CreateText(right.transform, "InventoryTitle", "Inventory", 22f, 16f, 320f, 42f, 30, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
                var filters = new List<SurvivorsPanelView.InventoryFilterBinding>();
                var categories = new[]
                {
                    SurvivorInventoryCategory.All,
                    SurvivorInventoryCategory.Weapons,
                    SurvivorInventoryCategory.Armor,
                    SurvivorInventoryCategory.Utility,
                    SurvivorInventoryCategory.Materials
                };
                for (var i = 0; i < categories.Length; i++)
                {
                    var tab = CreateButton(right.transform, "Filter_" + categories[i], 20f + i * 132f, 60f, 124f, 38f, categories[i] == SurvivorInventoryCategory.All ? "All" : categories[i].ToString(), 15);
                    filters.Add(new SurvivorsPanelView.InventoryFilterBinding(categories[i], tab.GetComponent<Image>(), tab, tab.GetComponentInChildren<TextMeshProUGUI>()));
                }

                var inventoryContent = CreateVerticalScrollContent(right.transform, "InventoryScroll", 20f, 108f, 660f, 500f, 620f);
                var sections = new List<SurvivorsPanelView.InventorySectionBinding>
                {
                    CreateInventorySection(inventoryContent, SurvivorInventoryCategory.Weapons, 0f, 5),
                    CreateInventorySection(inventoryContent, SurvivorInventoryCategory.Armor, 144f, 5),
                    CreateInventorySection(inventoryContent, SurvivorInventoryCategory.Utility, 288f, 5),
                    CreateInventorySection(inventoryContent, SurvivorInventoryCategory.Materials, 432f, 4)
                };

                var selectedPanel = CreatePanel(right.transform, "SelectedItemPanel", 20f, 620f, 660f, 150f, Paper);
                AddOutline(selectedPanel.gameObject);
                var selectedIcon = CreateRawImage(selectedPanel.transform, "SelectedIcon", 14f, 14f, 112f, 112f, null);
                var selectedName = CreateText(selectedPanel.transform, "SelectedName", "Rusty Knife", 142f, 14f, 280f, 30f, 20, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
                var selectedDescription = CreateText(selectedPanel.transform, "SelectedDescription", string.Empty, 142f, 46f, 340f, 42f, 13, FontStyles.Normal, MutedInk, TextAlignmentOptions.TopLeft);
                var selectedStats = CreateText(selectedPanel.transform, "SelectedStats", string.Empty, 142f, 94f, 340f, 30f, 14, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
                var equip = CreateButton(selectedPanel.transform, "EquipButton", 490f, 88f, 156f, 46f, "Equip", 18);
                equip.GetComponent<Image>().color = new Color32(0x9C, 0x51, 0x2D, 0xFF);
                var equipLabel = equip.GetComponentInChildren<TextMeshProUGUI>();
                equipLabel.color = PaperLight;

                var view = root.GetComponent<SurvivorsPanelView>();
                view.ConfigureBindings(
                    title,
                    count,
                    null,
                    null,
                    null,
                    cards,
                    center,
                    detailTitle,
                    backgroundLabel,
                    traits,
                    null,
                    null,
                    selectedDetailPortrait: portrait,
                    selectedDetailLevel: level,
                    selectedDetailXp: xp);
                view.ConfigureReferenceBindings(
                    xpSlider,
                    skills,
                    loadout,
                    inventoryTitle,
                    filters,
                    sections,
                    selectedIcon,
                    selectedName,
                    selectedDescription,
                    selectedStats,
                    equip,
                    equipLabel);

                PrefabUtility.SaveAsPrefabAsset(root, SurvivorsPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        public static void BuildBottomNavigationPrefab()
        {
            var root = new GameObject("BottomNavigation", typeof(RectTransform), typeof(Image), typeof(BottomNavView));
            try
            {
                SetRect(root.GetComponent<RectTransform>(), 0f, 0f, 1120f, 72f);
                root.GetComponent<Image>().color = Dark;
                AddOutline(root);
                var ids = new[] { "camp", "survivors", "inventory", "expeditions", "radio", "reports" };
                var bindings = new List<BottomNavView.NavBinding>();
                for (var i = 0; i < ids.Length; i++)
                {
                    var button = CreateButton(root.transform, "Nav_" + ids[i], i * (1120f / 6f), 0f, 1120f / 6f, 72f, ids[i].ToUpperInvariant(), 16);
                    button.GetComponent<Image>().color = Dark;
                    var marker = CreatePanel(button.transform, "CurrentTabPointer", 12f, 0f, 162f, 4f, Amber);
                    marker.gameObject.SetActive(false);
                    var icon = CreateRawImage(button.transform, "Icon", 20f, 19f, 34f, 34f, null);
                    var label = button.GetComponentInChildren<TextMeshProUGUI>();
                    SetRect(label.rectTransform, 48f, 0f, 134f, 72f);
                    label.fontSize = 14f;
                    label.alignment = TextAlignmentOptions.MidlineLeft;
                    bindings.Add(new BottomNavView.NavBinding(ids[i], button.GetComponent<Image>(), button, icon, label));
                }

                root.GetComponent<BottomNavView>().ConfigureBindings(bindings);
                PrefabUtility.SaveAsPrefabAsset(root, BottomNavigationPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static SurvivorsPanelView.SurvivorCardBinding CreateSurvivorCard(Transform parent, int index, float y)
        {
            var panel = CreatePanel(parent, "SurvivorCard_" + (index + 1), 14f, y, 502f, 104f, Paper);
            var button = panel.gameObject.AddComponent<Button>();
            button.targetGraphic = panel;
            AddOutline(panel.gameObject);
            var portrait = CreateRawImage(panel.transform, "Portrait", 8f, 8f, 86f, 88f, null);
            var name = CreateText(panel.transform, "Name", "Survivor", 106f, 8f, 150f, 30f, 24, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
            var state = CreateText(panel.transform, "State", "Idle", 106f, 38f, 150f, 22f, 14, FontStyles.Normal, MutedInk, TextAlignmentOptions.Left);
            var hp = CreateSlider(panel.transform, "HP", 272f, 15f, 172f, 8f, new Color32(0x5F, 0x85, 0x3C, 0xFF));
            var fatigue = CreateSlider(panel.transform, "Fatigue", 272f, 46f, 172f, 8f, new Color32(0xC2, 0x88, 0x20, 0xFF));
            var morale = CreateSlider(panel.transform, "Morale", 272f, 77f, 172f, 8f, Sage);
            CreateText(panel.transform, "HpCaption", "HP", 248f, 5f, 30f, 26f, 12, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
            CreateText(panel.transform, "FatigueCaption", "F", 248f, 36f, 30f, 26f, 12, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
            CreateText(panel.transform, "MoraleCaption", "M", 248f, 67f, 30f, 26f, 12, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
            var hpValue = CreateText(panel.transform, "HpValue", string.Empty, 447f, 3f, 48f, 26f, 12, FontStyles.Normal, Ink, TextAlignmentOptions.Right);
            var fatigueValue = CreateText(panel.transform, "FatigueValue", string.Empty, 447f, 34f, 48f, 26f, 12, FontStyles.Normal, Ink, TextAlignmentOptions.Right);
            var moraleValue = CreateText(panel.transform, "MoraleValue", string.Empty, 447f, 65f, 48f, 26f, 12, FontStyles.Normal, Ink, TextAlignmentOptions.Right);
            return new SurvivorsPanelView.SurvivorCardBinding(panel, null, button, portrait, null, name, state, null, null, null, hp, fatigue, morale, hpValue, fatigueValue, moraleValue);
        }

        private static SurvivorsPanelView.InventorySectionBinding CreateInventorySection(Transform parent, SurvivorInventoryCategory category, float y, int capacity)
        {
            var root = new GameObject("Section_" + category, typeof(RectTransform));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), 0f, y, 660f, 136f);
            var title = CreateText(root.transform, "Title", category.ToString(), 0f, 0f, 300f, 24f, 18, FontStyles.Bold, Ink, TextAlignmentOptions.Left);
            var count = CreateText(root.transform, "Count", "0", 610f, 0f, 50f, 24f, 16, FontStyles.Normal, Ink, TextAlignmentOptions.Right);
            CreateRule(root.transform, 0f, 26f, 660f);
            var itemContent = CreateHorizontalScrollContent(root.transform, "ItemsScroll", 0f, 32f, 660f, 96f, Mathf.Max(780f, capacity * 130f));
            var items = new List<SurvivorsPanelView.InventoryItemBinding>();
            for (var i = 0; i < capacity; i++)
            {
                var tile = CreatePanel(itemContent, "Item_" + i, i * 130f, 0f, 122f, 84f, Paper);
                var button = tile.gameObject.AddComponent<Button>();
                button.targetGraphic = tile;
                AddOutline(tile.gameObject);
                var icon = CreateRawImage(tile.transform, "Icon", 27f, 4f, 68f, 54f, null);
                var name = CreateText(tile.transform, "Name", string.Empty, 2f, 59f, 118f, 22f, 12, FontStyles.Bold, Ink, TextAlignmentOptions.Center);
                var itemCount = CreateText(tile.transform, "Count", string.Empty, 86f, 3f, 30f, 22f, 12, FontStyles.Bold, Ink, TextAlignmentOptions.Right);
                items.Add(new SurvivorsPanelView.InventoryItemBinding(tile, button, icon, name, itemCount));
            }

            return new SurvivorsPanelView.InventorySectionBinding(category, root, title, count, items);
        }

        private static Transform CreateVerticalScrollContent(Transform parent, string name, float x, float y, float width, float height, float contentHeight)
        {
            var scrollRoot = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            scrollRoot.transform.SetParent(parent, false);
            SetRect(scrollRoot.GetComponent<RectTransform>(), x, y, width, height);

            var viewport = CreatePanel(scrollRoot.transform, "Viewport", 0f, 0f, width, height, new Color(1f, 1f, 1f, 0.001f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            SetRect(content.GetComponent<RectTransform>(), 0f, 0f, width, contentHeight);

            var scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content.GetComponent<RectTransform>();
            scroll.horizontal = false;
            scroll.vertical = true;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 32f;
            scroll.verticalNormalizedPosition = 1f;
            return content.transform;
        }

        private static Transform CreateHorizontalScrollContent(Transform parent, string name, float x, float y, float width, float height, float contentWidth)
        {
            var scrollRoot = new GameObject(name, typeof(RectTransform), typeof(ScrollRect));
            scrollRoot.transform.SetParent(parent, false);
            SetRect(scrollRoot.GetComponent<RectTransform>(), x, y, width, height);

            var viewport = CreatePanel(scrollRoot.transform, "Viewport", 0f, 0f, width, height, new Color(1f, 1f, 1f, 0.001f));
            viewport.gameObject.AddComponent<RectMask2D>();
            var content = new GameObject("Content", typeof(RectTransform));
            content.transform.SetParent(viewport.transform, false);
            SetRect(content.GetComponent<RectTransform>(), 0f, 0f, contentWidth, height);

            var scroll = scrollRoot.GetComponent<ScrollRect>();
            scroll.viewport = viewport.rectTransform;
            scroll.content = content.GetComponent<RectTransform>();
            scroll.horizontal = true;
            scroll.vertical = false;
            scroll.movementType = ScrollRect.MovementType.Clamped;
            scroll.scrollSensitivity = 28f;
            scroll.horizontalNormalizedPosition = 0f;
            return content.transform;
        }

        private static void ConfigureCatalog()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CatalogPath);
            if (catalog == null) throw new InvalidOperationException("Camp UI catalog is missing: " + CatalogPath);

            var resources = new Dictionary<string, ResourceUiEntry>(StringComparer.Ordinal);
            foreach (var entry in catalog.ResourceBar)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Id)) resources[entry.Id] = entry;
            }
            catalog.ResourceBar = new List<ResourceUiEntry>
            {
                Resource(resources, GameIds.Resources.Scrap, "SCRAP"),
                Resource(resources, GameIds.Resources.Food, "FOOD"),
                Resource(resources, GameIds.Resources.Water, "WATER"),
                Resource(resources, GameIds.Resources.WeaponParts, "WEAPON PARTS"),
                Resource(resources, GameIds.Resources.Medicine, "MEDICINE"),
                Resource(resources, GameIds.Resources.RadioIntel, "RADIO INTEL")
            };

            var nav = new Dictionary<string, NavUiEntry>(StringComparer.Ordinal);
            foreach (var entry in catalog.NavItems)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Id)) nav[entry.Id] = entry;
            }
            var inventoryIcon = nav.ContainsKey("workshop") ? nav["workshop"].Icon : null;
            catalog.NavItems = new List<NavUiEntry>
            {
                Nav(nav, "camp", "CAMP", true),
                Nav(nav, "survivors", "SURVIVORS", false),
                new NavUiEntry { Id = "inventory", Label = "INVENTORY", Icon = inventoryIcon },
                Nav(nav, "expeditions", "EXPEDITIONS", false),
                Nav(nav, "radio", "RADIO", false),
                Nav(nav, "reports", "REPORTS", false)
            };

            catalog.SurvivorInventoryTitle = "INVENTORY";
            catalog.SurvivorDetailTitle = "{0}";
            catalog.SurvivorInventoryEquipButton = "EQUIP";
            catalog.SurvivorInventoryEmptyLabel = "NO ITEMS";
            catalog.SurvivorInventoryFilters = new List<SurvivorInventoryFilterUiEntry>
            {
                new SurvivorInventoryFilterUiEntry { Id = "all", Label = "ALL" },
                new SurvivorInventoryFilterUiEntry { Id = "weapons", Label = "WEAPONS" },
                new SurvivorInventoryFilterUiEntry { Id = "armor", Label = "ARMOR" },
                new SurvivorInventoryFilterUiEntry { Id = "utility", Label = "UTILITY" },
                new SurvivorInventoryFilterUiEntry { Id = "materials", Label = "MATERIALS" }
            };
            catalog.SurvivorInventoryItems = BuildInventoryArtwork();
            EditorUtility.SetDirty(catalog);
        }

        private static List<SurvivorInventoryItemUiEntry> BuildInventoryArtwork()
        {
            return new List<SurvivorInventoryItemUiEntry>
            {
                Item(GameIds.Items.RustyKnife, "A dull blade, but better than nothing.", "Assets/AshfallCamp/Art/UI/Production/Icons/Equipment/ui_icon_equipment_machete.png"),
                Item(GameIds.Items.MetalPipe, "A heavy improvised melee weapon.", "Assets/AshfallCamp/Art/UI/Production/Icons/Equipment/ui_icon_equipment_hatchet.png"),
                Item(GameIds.Items.RustyRevolver, "Old, loud and still dangerous.", "Assets/AshfallCamp/Art/UI/Production/Icons/Equipment/ui_icon_equipment_pistol.png"),
                Item(GameIds.Items.LeatherJacket, "Worn leather offering basic protection.", "Assets/AshfallCamp/Art/UI/Production/Icons/Equipment/ui_icon_equipment_armor_vest.png"),
                Item(GameIds.Items.ScrapArmor, "Scavenged plates wired into a rough vest.", "Assets/AshfallCamp/Art/UI/Production/Icons/Equipment/ui_icon_equipment_armor_vest.png"),
                Item(GameIds.Items.Medkit, "Emergency supplies for field treatment.", "Assets/AshfallCamp/Art/UI/Production/Icons/Equipment/ui_icon_equipment_medkit.png"),
                Item(GameIds.Items.Toolkit, "Tools for repairs away from the workshop.", "Assets/AshfallCamp/Art/UI/Production/Icons/Equipment/ui_icon_equipment_duct_tape.png"),
                Item(GameIds.Items.AmmoPack, "Keeps loose ammunition dry and organized.", "Assets/AshfallCamp/Art/UI/Production/Icons/Equipment/ui_icon_equipment_ammo_box.png"),
                Item(GameIds.Items.Backpack, "Adds room for scavenged supplies.", "Assets/AshfallCamp/Art/UI/Production/Icons/Equipment/ui_icon_equipment_backpack.png")
            };
        }

        private static void UpdateDashboardPrefab()
        {
            var root = PrefabUtility.LoadPrefabContents(DashboardPrefabPath);
            try
            {
                var dashboard = root.GetComponent<CampDashboardView>();
                if (dashboard == null) throw new InvalidOperationException("CampDashboardView is missing from dashboard prefab.");
                var bottomNav = root.GetComponentInChildren<BottomNavView>(true);
                if (bottomNav != null)
                {
                    SetRect(bottomNav.GetComponent<RectTransform>(), 400f, 990f, 1120f, 72f);
                }
                var survivors = root.GetComponentInChildren<SurvivorsPanelView>(true);
                if (survivors != null)
                {
                    SetRect(survivors.GetComponent<RectTransform>(), 40f, 140f, 1840f, 790f);
                }
                var oldClock = root.transform.Find("ReferenceClock");
                if (oldClock != null) UnityEngine.Object.DestroyImmediate(oldClock.gameObject);
                var clockRoot = new GameObject("ReferenceClock", typeof(RectTransform), typeof(Image));
                clockRoot.transform.SetParent(root.transform, false);
                SetRect(clockRoot.GetComponent<RectTransform>(), 1640f, 14f, 250f, 72f);
                clockRoot.GetComponent<Image>().color = Dark;
                var clockCanvas = clockRoot.AddComponent<Canvas>();
                clockCanvas.overrideSorting = true;
                clockCanvas.sortingOrder = 100;
                var day = CreateText(clockRoot.transform, "Day", "Day 1", 12f, 0f, 118f, 72f, 20, FontStyles.Bold, PaperLight, TextAlignmentOptions.Center);
                var time = CreateText(clockRoot.transform, "Time", "08:00", 130f, 0f, 108f, 72f, 22, FontStyles.Bold, Amber, TextAlignmentOptions.Center);
                dashboard.ConfigureClock(day, time);
                var serialized = new SerializedObject(dashboard);
                var screens = serialized.FindProperty("screens");
                for (var i = 0; i < screens.arraySize; i++)
                {
                    var screen = screens.GetArrayElementAtIndex(i);
                    var id = screen.FindPropertyRelative("id");
                    if (id.stringValue == "workshop") id.stringValue = "inventory";
                }
                serialized.ApplyModifiedPropertiesWithoutUndo();
                ReorderResourceCells(root);
                PrefabUtility.SaveAsPrefabAsset(root, DashboardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static ResourceUiEntry Resource(Dictionary<string, ResourceUiEntry> entries, string id, string label)
        {
            ResourceUiEntry entry;
            if (!entries.TryGetValue(id, out entry)) entry = new ResourceUiEntry { Id = id };
            entry.Label = label;
            entry.UsesSurvivorCapacity = false;
            return entry;
        }

        private static void ReorderResourceCells(GameObject dashboardRoot)
        {
            var resourceBar = dashboardRoot.GetComponentInChildren<ResourceBarView>(true);
            if (resourceBar == null) return;
            var serialized = new SerializedObject(resourceBar);
            var resources = serialized.FindProperty("resources");
            var byId = new Dictionary<string, RectTransform>(StringComparer.Ordinal);
            var minX = float.MaxValue;
            var width = 0f;
            for (var i = 0; i < resources.arraySize; i++)
            {
                var binding = resources.GetArrayElementAtIndex(i);
                var id = binding.FindPropertyRelative("id").stringValue;
                var label = binding.FindPropertyRelative("label").objectReferenceValue as TextMeshProUGUI;
                var rect = label != null && label.transform.parent != null ? label.transform.parent as RectTransform : null;
                if (rect == null || string.IsNullOrWhiteSpace(id)) continue;
                byId[id] = rect;
                minX = Mathf.Min(minX, rect.anchoredPosition.x);
                if (width <= 0f) width = rect.sizeDelta.x > 0f ? rect.sizeDelta.x : rect.rect.width;
            }

            if (minX == float.MaxValue) return;
            if (width <= 0f) width = 180f;
            var order = new[]
            {
                GameIds.Resources.Scrap,
                GameIds.Resources.Food,
                GameIds.Resources.Water,
                GameIds.Resources.WeaponParts,
                GameIds.Resources.Medicine,
                GameIds.Resources.RadioIntel
            };
            for (var i = 0; i < order.Length; i++)
            {
                RectTransform rect;
                if (!byId.TryGetValue(order[i], out rect)) continue;
                rect.anchoredPosition = new Vector2(minX + i * width, rect.anchoredPosition.y);
                rect.SetSiblingIndex(i);
            }
        }

        private static NavUiEntry Nav(Dictionary<string, NavUiEntry> entries, string id, string label, bool active)
        {
            NavUiEntry entry;
            if (!entries.TryGetValue(id, out entry)) entry = new NavUiEntry { Id = id };
            entry.Label = label;
            entry.IsActive = active;
            return entry;
        }

        private static SurvivorInventoryItemUiEntry Item(string id, string description, string texturePath)
        {
            return new SurvivorInventoryItemUiEntry { ItemId = id, Description = description, Icon = LoadTexture(texturePath) };
        }

        private static Image CreatePanel(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(Image));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), x, y, width, height);
            var image = go.GetComponent<Image>();
            image.color = color;
            return image;
        }

        private static RawImage CreateRawImage(Transform parent, string name, float x, float y, float width, float height, Texture texture)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(RawImage));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), x, y, width, height);
            var image = go.GetComponent<RawImage>();
            image.texture = texture;
            image.color = texture != null ? Color.white : new Color(1f, 1f, 1f, 0f);
            return image;
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name, string value, float x, float y, float width, float height, float size, FontStyles style, Color color, TextAlignmentOptions alignment)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(CanvasRenderer), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            SetRect(go.GetComponent<RectTransform>(), x, y, width, height);
            var text = go.GetComponent<TextMeshProUGUI>();
            text.text = value;
            text.font = ResolveFont();
            text.fontSize = size;
            text.fontStyle = style;
            text.color = color;
            text.alignment = alignment;
            text.textWrappingMode = TextWrappingModes.Normal;
            text.raycastTarget = false;
            return text;
        }

        private static Button CreateButton(Transform parent, string name, float x, float y, float width, float height, string label, int fontSize)
        {
            var image = CreatePanel(parent, name, x, y, width, height, new Color32(0x46, 0x50, 0x3B, 0xFF));
            var button = image.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            var text = CreateText(button.transform, "Label", label, 4f, 0f, width - 8f, height, fontSize, FontStyles.Bold, PaperLight, TextAlignmentOptions.Center);
            text.raycastTarget = false;
            return button;
        }

        private static Slider CreateSlider(Transform parent, string name, float x, float y, float width, float height, Color fillColor)
        {
            var root = new GameObject(name, typeof(RectTransform), typeof(Slider));
            root.transform.SetParent(parent, false);
            SetRect(root.GetComponent<RectTransform>(), x, y, width, height);
            var track = CreatePanel(root.transform, "Track", 0f, 0f, width, height, new Color32(0x5A, 0x50, 0x3F, 0x55));
            var fill = CreatePanel(root.transform, "Fill", 0f, 0f, width, height, fillColor);
            var slider = root.GetComponent<Slider>();
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.value = 1f;
            slider.interactable = false;
            slider.targetGraphic = track;
            slider.fillRect = fill.rectTransform;
            slider.handleRect = null;
            return slider;
        }

        private static void CreateRule(Transform parent, float x, float y, float width)
        {
            CreatePanel(parent, "Rule", x, y, width, 1f, Line).raycastTarget = false;
        }

        private static void AddOutline(GameObject target)
        {
            var outline = target.GetComponent<Outline>();
            if (outline == null) outline = target.AddComponent<Outline>();
            outline.effectColor = Line;
            outline.effectDistance = new Vector2(1f, -1f);
        }

        private static void SetRect(RectTransform rect, float x, float y, float width, float height)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            rect.localScale = Vector3.one;
        }

        private static Texture2D LoadTexture(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static TMP_FontAsset ResolveFont()
        {
            if (TMP_Settings.defaultFontAsset != null) return TMP_Settings.defaultFontAsset;
            return AssetDatabase.LoadAssetAtPath<TMP_FontAsset>("Assets/TextMesh Pro/Resources/Fonts & Materials/LiberationSans SDF.asset");
        }
    }
}
