using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Composition;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using AshfallCamp.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class SurvivorsReferenceUiTests
    {
        private const string DashboardPath = "Assets/AshfallCamp/Prefabs/UI/PF_CampDashboard.prefab";
        private const string SurvivorsPath = "Assets/AshfallCamp/Prefabs/UI/SurvivorsScreen.prefab";
        private const string NavigationPath = "Assets/AshfallCamp/Prefabs/UI/Main/BottomNavigation.prefab";
        private const string CatalogPath = "Assets/AshfallCamp/UI/CampUiCatalog.asset";
        private const string DatabasePath = "Assets/AshfallCamp/Configs/Core/GameConfigDatabase.asset";

        [Test]
        public void ReferenceProfileContainsSixSurvivorsAndEveryInventoryGroup()
        {
            var config = LoadProductionConfig();
            var state = CampUiPreviewStateFactory.CreateReferenceProfile(config, 0);

            Assert.AreEqual(6, state.Survivors.Count);
            Assert.AreEqual("Asha", state.Survivors[0].Name);
            Assert.AreEqual(3, state.Survivors[0].Level);
            Assert.AreEqual(420, state.Survivors[0].Xp);
            Assert.AreEqual(9, state.Inventory.Count);
            Assert.IsFalse(string.IsNullOrWhiteSpace(state.Survivors[0].Equipment.BackpackItemUid));

            var catalog = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CatalogPath);
            var items = CampDashboardTextFormatter.BuildSurvivorInventory(state, config, catalog, state.Survivors[0].Id);
            Assert.IsTrue(items.Any(item => item.Category == SurvivorInventoryCategory.Weapons));
            Assert.IsTrue(items.Any(item => item.Category == SurvivorInventoryCategory.Armor));
            Assert.IsTrue(items.Any(item => item.Category == SurvivorInventoryCategory.Utility));
            Assert.AreEqual(4, items.Count(item => item.Category == SurvivorInventoryCategory.Materials));
        }

        [Test]
        public void BackpackEquipsWithoutReplacingUtility()
        {
            var config = LoadProductionConfig();
            var state = CampUiPreviewStateFactory.CreateReferenceProfile(config, 0);
            var survivor = state.Survivors[0];
            var oldUtility = survivor.Equipment.UtilityItemUid;
            var backpack = state.Inventory.First(item => item.ItemId == GameIds.Items.Backpack);
            survivor.Equipment.BackpackItemUid = string.Empty;
            backpack.EquippedBySurvivorId = string.Empty;

            var result = WorkshopSystem.Equip(state, config, new EquipItemRequest { SurvivorId = survivor.Id, ItemUid = backpack.Uid });

            Assert.IsTrue(result.Validation.IsValid, string.Join("; ", result.Validation.Errors));
            Assert.AreEqual(backpack.Uid, survivor.Equipment.BackpackItemUid);
            Assert.AreEqual(oldUtility, survivor.Equipment.UtilityItemUid);
        }

        [Test]
        public void MigrationMovesLegacyBackpackOutOfUtilitySlot()
        {
            var config = LoadProductionConfig();
            var state = CampUiPreviewStateFactory.CreateReferenceProfile(config, 0);
            var survivor = state.Survivors[0];
            var backpackUid = survivor.Equipment.BackpackItemUid;
            survivor.Equipment.BackpackItemUid = string.Empty;
            survivor.Equipment.UtilityItemUid = backpackUid;

            var migrate = typeof(SaveLoadUseCase).GetMethod("Migrate", BindingFlags.NonPublic | BindingFlags.Static);
            Assert.NotNull(migrate);
            migrate.Invoke(null, new object[] { state, config });

            Assert.AreEqual(backpackUid, survivor.Equipment.BackpackItemUid);
            Assert.IsEmpty(survivor.Equipment.UtilityItemUid);
        }

        [Test]
        public void ProductionPrefabsExposeReferenceBindingsAndSixNavigationTabs()
        {
            var survivorsPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(SurvivorsPath);
            var survivors = survivorsPrefab.GetComponent<SurvivorsPanelView>();
            Assert.NotNull(survivors);
            var survivorsSerialized = new SerializedObject(survivors);
            Assert.AreEqual(6, survivorsSerialized.FindProperty("cards").arraySize);
            Assert.AreEqual(6, survivorsSerialized.FindProperty("skillRows").arraySize);
            Assert.AreEqual(4, survivorsSerialized.FindProperty("equipmentSlots").arraySize);
            Assert.AreEqual(5, survivorsSerialized.FindProperty("inventoryFilters").arraySize);
            Assert.AreEqual(4, survivorsSerialized.FindProperty("inventorySections").arraySize);
            var scrollRects = survivorsPrefab.GetComponentsInChildren<ScrollRect>(true);
            Assert.AreEqual(6, scrollRects.Length);
            Assert.AreEqual(2, scrollRects.Count(scroll => scroll.vertical && !scroll.horizontal));
            Assert.AreEqual(4, scrollRects.Count(scroll => scroll.horizontal && !scroll.vertical));

            var navPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(NavigationPath);
            var nav = navPrefab.GetComponent<BottomNavView>();
            Assert.NotNull(nav);
            var navItems = new SerializedObject(nav).FindProperty("items");
            var expected = new[] { "camp", "survivors", "inventory", "expeditions", "radio", "reports" };
            Assert.AreEqual(expected.Length, navItems.arraySize);
            for (var i = 0; i < expected.Length; i++)
            {
                Assert.AreEqual(expected[i], navItems.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue);
            }

            var dashboard = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPath).GetComponent<CampDashboardView>();
            var screens = new SerializedObject(dashboard).FindProperty("screens");
            Assert.IsTrue(Enumerable.Range(0, screens.arraySize).Any(i => screens.GetArrayElementAtIndex(i).FindPropertyRelative("id").stringValue == "inventory"));
        }

        [Test]
        public void CatalogUsesReferenceResourceAndNavigationOrder()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CatalogPath);
            CollectionAssert.AreEqual(
                new[] { "scrap", "food", "water", "weapon_parts", "medicine", "radio_intel" },
                catalog.ResourceBar.Select(entry => entry.Id).ToArray());
            CollectionAssert.AreEqual(
                new[] { "camp", "survivors", "inventory", "expeditions", "radio", "reports" },
                catalog.NavItems.Select(entry => entry.Id).ToArray());
        }

        private static GameConfigSnapshot LoadProductionConfig()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(DatabasePath);
            Assert.NotNull(database);
            return new ScriptableObjectGameConfigProvider(database).LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
        }
    }
}
