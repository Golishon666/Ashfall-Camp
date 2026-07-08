using AshfallCamp.Domain;
using AshfallCamp.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class ResourceBarViewTests
    {
        private const string DashboardPrefabPath = "Assets/AshfallCamp/Prefabs/UI/PF_CampDashboard.prefab";

        [Test]
        public void CampDashboardResourceBindingsHaveRateLabels()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");

            var resourceBar = prefab.GetComponentInChildren<ResourceBarView>(true);
            Assert.NotNull(resourceBar, DashboardPrefabPath + " does not contain ResourceBarView.");

            var serialized = new SerializedObject(resourceBar);
            var resources = serialized.FindProperty("resources");
            Assert.That(resources.arraySize, Is.GreaterThan(0), "ResourceBarView has no resource bindings.");

            for (var i = 0; i < resources.arraySize; i++)
            {
                var resource = resources.GetArrayElementAtIndex(i);
                var id = resource.FindPropertyRelative("id").stringValue;
                Assert.NotNull(resource.FindPropertyRelative("rate").objectReferenceValue, "Missing rate label for resource bar id: " + id);
            }
        }

        [Test]
        public void ResourceRateShowsOnlyPositiveProduction()
        {
            var root = new GameObject("ResourceBar");
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();

            try
            {
                var view = root.AddComponent<ResourceBarView>();
                var label = CreateText(root.transform, "Label");
                var value = CreateText(root.transform, "Value");
                var rate = CreateText(root.transform, "Rate");
                view.ConfigureBindings(new[]
                {
                    new ResourceBarView.ResourceBinding(GameIds.Resources.Food, label, value, rate, null)
                });

                catalog.PerHourSuffixLabel = "/h";
                catalog.ResourceBar.Add(new ResourceUiEntry { Id = GameIds.Resources.Food, Label = "FOOD" });

                var config = CreateProductionConfig(GameIds.Resources.Food, GameIds.Buildings.MushroomBeds, 2);
                var state = new GameState();
                state.Resources[GameIds.Resources.Food] = 8;
                state.ResourceCaps[GameIds.Resources.Food] = 50;
                state.Buildings[GameIds.Buildings.MushroomBeds] = new BuildingState
                {
                    Id = GameIds.Buildings.MushroomBeds,
                    IsUnlocked = true,
                    Level = 1
                };

                view.Render(state, config, catalog);

                Assert.That(rate.gameObject.activeSelf, Is.True);
                Assert.AreEqual("+120/h", rate.text);

                state.Buildings[GameIds.Buildings.MushroomBeds].IsUnlocked = false;
                view.Render(state, config, catalog);

                Assert.That(rate.gameObject.activeSelf, Is.False);
                Assert.AreEqual(string.Empty, rate.text);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void SurvivorCapacityRateShowsOnlyPositiveIdleCount()
        {
            var root = new GameObject("ResourceBar");
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();

            try
            {
                var view = root.AddComponent<ResourceBarView>();
                var label = CreateText(root.transform, "Label");
                var value = CreateText(root.transform, "Value");
                var rate = CreateText(root.transform, "Rate");
                view.ConfigureBindings(new[]
                {
                    new ResourceBarView.ResourceBinding("survivors", label, value, rate, null)
                });

                catalog.IdleSuffixLabel = "idle";
                catalog.ResourceBar.Add(new ResourceUiEntry
                {
                    Id = "survivors",
                    Label = "CAPACITY",
                    UsesSurvivorCapacity = true
                });

                var state = new GameState { SurvivorCap = 3 };
                state.Survivors.Add(new SurvivorState { State = SurvivorActivityState.Idle });
                state.Survivors.Add(new SurvivorState { State = SurvivorActivityState.Idle });
                state.Survivors.Add(new SurvivorState { State = SurvivorActivityState.OnExpedition });

                view.Render(state, new GameConfigSnapshot(), catalog);

                Assert.That(rate.gameObject.activeSelf, Is.True);
                Assert.AreEqual("2 idle", rate.text);

                state.Survivors[0].State = SurvivorActivityState.Resting;
                state.Survivors[1].State = SurvivorActivityState.OnExpedition;
                view.Render(state, new GameConfigSnapshot(), catalog);

                Assert.That(rate.gameObject.activeSelf, Is.False);
                Assert.AreEqual(string.Empty, rate.text);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ResourceBarCreatesRuntimeCellsForConfiguredResources()
        {
            var root = new GameObject("ResourceBar");
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();

            try
            {
                var view = root.AddComponent<ResourceBarView>();
                var template = CreateResourceCell(root.transform, "ResourceCell_Template");
                var label = template.transform.Find("Text_Label").GetComponent<TextMeshProUGUI>();
                var value = template.transform.Find("Text_Value").GetComponent<TextMeshProUGUI>();
                var rate = template.transform.Find("Text_Rate").GetComponent<TextMeshProUGUI>();
                var icon = template.transform.Find("Image_Icon").GetComponent<RawImage>();
                view.ConfigureBindings(new[]
                {
                    new ResourceBarView.ResourceBinding(GameIds.Resources.Food, label, value, rate, icon)
                });

                catalog.ResourceBar.Add(new ResourceUiEntry { Id = GameIds.Resources.Food, Label = "FOOD" });

                var config = new GameConfigSnapshot();
                config.Resources[GameIds.Resources.Food] = new ResourceDefinition { Id = GameIds.Resources.Food, Name = "Food" };
                config.Resources["test_runtime_resource"] = new ResourceDefinition { Id = "test_runtime_resource", Name = "Test Runtime Resource" };
                var state = new GameState();
                state.Resources[GameIds.Resources.Food] = 8;
                state.Resources["test_runtime_resource"] = 7;

                view.Render(state, config, catalog);

                var runtimeCell = root.transform.Find("ResourceCell_test_runtime_resource");
                Assert.NotNull(runtimeCell, "Resource bar should create a runtime cell for config-only resources.");
                Assert.AreEqual("TEST RUNTIME RESOURCE", runtimeCell.Find("Text_Label").GetComponent<TextMeshProUGUI>().text);
                Assert.AreEqual("7", runtimeCell.Find("Text_Value").GetComponent<TextMeshProUGUI>().text);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(root);
            }
        }

        private static TextMeshProUGUI CreateText(Transform parent, string name)
        {
            var gameObject = new GameObject(name);
            gameObject.transform.SetParent(parent);
            return gameObject.AddComponent<TextMeshProUGUI>();
        }

        private static GameObject CreateResourceCell(Transform parent, string name)
        {
            var cell = new GameObject(name, typeof(RectTransform));
            cell.transform.SetParent(parent, false);
            var rect = cell.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(160, 80);
            CreateText(cell.transform, "Text_Label");
            CreateText(cell.transform, "Text_Value");
            CreateText(cell.transform, "Text_Rate");
            var icon = new GameObject("Image_Icon");
            icon.transform.SetParent(cell.transform, false);
            icon.AddComponent<RawImage>();
            return cell;
        }

        private static GameConfigSnapshot CreateProductionConfig(string resourceId, string buildingId, int resourcePerMinute)
        {
            var config = new GameConfigSnapshot();
            config.Resources[resourceId] = new ResourceDefinition
            {
                Id = resourceId,
                HasCap = true,
                StartCap = 50
            };
            config.Buildings[buildingId] = new BuildingDefinition
            {
                Id = buildingId,
                ProducedResourceId = resourceId,
                Levels =
                {
                    new BuildingLevelDefinition
                    {
                        Level = 1,
                        ResourcePerMinute = resourcePerMinute
                    }
                }
            };

            return config;
        }
    }
}
