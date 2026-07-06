using AshfallCamp.Editor.FigmaImport;
using AshfallCamp.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Tests.EditMode.FigmaImport
{
    public sealed class AshfallFigmaImporterTests
    {
        [Test]
        public void PayloadParserReadsFrameTree()
        {
            const string json = "{\"source\":\"figma-console-mcp\",\"frames\":[{\"key\":\"survivors\",\"slug\":\"06-survivors-elementwise\",\"frameId\":\"72:21614\",\"frameName\":\"06 Survivors Elementwise\",\"width\":1920,\"height\":1080,\"tree\":{\"id\":\"72:21614\",\"name\":\"Root\",\"type\":\"FRAME\",\"renderMode\":\"container\",\"bounds\":{\"x\":0,\"y\":0,\"width\":1920,\"height\":1080},\"children\":[{\"id\":\"1\",\"name\":\"Screen Title\",\"type\":\"TEXT\",\"renderMode\":\"text\",\"bounds\":{\"x\":310,\"y\":140,\"width\":300,\"height\":30},\"text\":{\"characters\":\"8 SURVIVORS\",\"fontSize\":20}}]}}]}";

            var document = FigmaImportDocument.FromJson(json);
            var frame = document.FindFrame("survivors");

            Assert.NotNull(frame);
            Assert.AreEqual("72:21614", frame.frameId);
            Assert.NotNull(frame.tree);
            Assert.AreEqual(1, frame.tree.children.Count);
            Assert.AreEqual("8 SURVIVORS", frame.tree.children[0].text.characters);
        }

        [Test]
        public void NameCleanupProducesSemanticUnityNames()
        {
            Assert.AreEqual("Button_ViewDetails", FigmaImportNameUtility.SanitizeUnityName("Button - VIEW DETAILS"));
            Assert.AreEqual("Slider_Hp", FigmaImportNameUtility.SanitizeUnityName("Slider - HP"));
            Assert.AreEqual("Panel_SelectedSurvivor", FigmaImportNameUtility.SanitizeUnityName("Panel - Selected Survivor"));
            Assert.AreEqual("SurvivorCard_Jade", FigmaImportNameUtility.SanitizeUnityName("Survivor Card - JADE"));
        }

        [Test]
        public void SliderDetectorUsesFillWidthOverTrackWidth()
        {
            var slider = new FigmaImportNode
            {
                name = "Slider - HP",
                bounds = new FigmaBounds(10f, 20f, 120f, 16f)
            };
            slider.children.Add(new FigmaImportNode
            {
                name = "Track",
                bounds = new FigmaBounds(10f, 26f, 100f, 8f)
            });
            slider.children.Add(new FigmaImportNode
            {
                name = "Fill",
                bounds = new FigmaBounds(10f, 26f, 37f, 8f)
            });

            FigmaSliderDefinition definition;
            Assert.IsTrue(FigmaSliderDetector.TryCreate(slider, out definition));
            Assert.AreEqual(0.37f, definition.value, 0.001f);
        }

        [Test]
        public void SurvivorsBindingMapperCreatesCompleteRuntimeBindings()
        {
            var root = new GameObject("SurvivorsScreen", typeof(RectTransform), typeof(SurvivorsPanelView));
            try
            {
                var roster = CreateRect("RosterScreen", root.transform);
                var detail = CreateRect("SurvivorDetailScreen", root.transform);
                CreateText("ScreenTitle", roster.transform);
                CreateButton("Button_ViewDetails", roster.transform);

                var selected = CreateRect("Panel_SelectedSurvivor", roster.transform);
                selected.gameObject.AddComponent<Image>();
                CreateRawImage("SelectedPortrait", selected.transform);
                CreateText("Name", selected.transform);
                CreateText("Role", selected.transform);
                CreateText("Level", selected.transform);
                CreateText("Power", selected.transform);
                CreateText("Summary", selected.transform);

                for (var i = 0; i < 8; i++)
                {
                    var card = CreateRect("SurvivorCard_" + i.ToString("00"), roster.transform);
                    card.anchoredPosition = new Vector2((i % 4) * 100f, -Mathf.Floor(i / 4f) * 120f);
                    card.gameObject.AddComponent<Image>();
                    card.gameObject.AddComponent<Button>();
                    CreateRawImage("Portrait", card.transform);
                    CreateText("Name", card.transform);
                    CreateText("Role", card.transform);
                    CreateText("LevelLabel", card.transform);
                    CreateText("Power", card.transform);
                    CreateText("HpValue", card.transform);
                    CreateText("FatigueValue", card.transform);
                    FigmaUgUiBuilder.CreatePassiveSlider("Slider_Hp", card, 0f, 0f, 100f, 8f, Color.gray, Color.green, 0.8f);
                    FigmaUgUiBuilder.CreatePassiveSlider("Slider_Fatigue", card, 0f, 12f, 100f, 8f, Color.gray, Color.yellow, 0.2f);
                }

                var profile = CreateRect("ProfilePanel", detail.transform);
                profile.gameObject.AddComponent<Image>();
                CreateRawImage("JadePortrait", profile.transform);
                CreateText("Name", profile.transform);
                CreateText("Level", profile.transform);
                CreateText("Xp", profile.transform);
                CreateText("ValueCurrentState", profile.transform);
                CreateText("ValueHealth", profile.transform);
                CreateText("ValueFatigue", profile.transform);
                CreateText("ValueMorale", profile.transform);
                CreateText("TraitName", profile.transform);
                CreateText("TraitDesc", profile.transform);
                FigmaUgUiBuilder.CreatePassiveSlider("Slider_Health", profile, 0f, 0f, 100f, 8f, Color.gray, Color.green, 0.9f);
                FigmaUgUiBuilder.CreatePassiveSlider("Slider_Fatigue", profile, 0f, 12f, 100f, 8f, Color.gray, Color.yellow, 0.25f);
                FigmaUgUiBuilder.CreatePassiveSlider("Slider_Morale", profile, 0f, 24f, 100f, 8f, Color.gray, Color.green, 0.75f);

                var actions = CreateRect("Panel_Actions", detail.transform);
                CreateButton("Button_Treat", actions.transform);
                CreateButton("Button_X", detail.transform);

                SurvivorsFigmaBindingMapper.Bind(root, roster, detail);

                var serialized = new SerializedObject(root.GetComponent<SurvivorsPanelView>());
                Assert.NotNull(serialized.FindProperty("title").objectReferenceValue);
                Assert.NotNull(serialized.FindProperty("rosterRoot").objectReferenceValue);
                Assert.NotNull(serialized.FindProperty("detailRoot").objectReferenceValue);
                Assert.NotNull(serialized.FindProperty("viewDetailsButton").objectReferenceValue);
                Assert.NotNull(serialized.FindProperty("closeDetailButton").objectReferenceValue);
                Assert.NotNull(serialized.FindProperty("detailHealthSlider").objectReferenceValue);
                Assert.NotNull(serialized.FindProperty("detailFatigueSlider").objectReferenceValue);
                Assert.NotNull(serialized.FindProperty("detailMoraleSlider").objectReferenceValue);
                Assert.That(serialized.FindProperty("cards").arraySize, Is.EqualTo(8));
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        private static RectTransform CreateRect(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.sizeDelta = new Vector2(100f, 40f);
            return rect;
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent)
        {
            var rect = CreateRect(name, parent);
            var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
            text.text = string.Empty;
            return text;
        }

        private static RawImage CreateRawImage(string name, Transform parent)
        {
            var rect = CreateRect(name, parent);
            return rect.gameObject.AddComponent<RawImage>();
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var rect = CreateRect(name, parent);
            var image = rect.gameObject.AddComponent<Image>();
            var button = rect.gameObject.AddComponent<Button>();
            button.targetGraphic = image;
            return button;
        }
    }
}
