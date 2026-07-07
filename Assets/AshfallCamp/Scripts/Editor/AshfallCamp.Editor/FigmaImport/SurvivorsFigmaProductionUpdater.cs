using System;
using System.IO;
using AshfallCamp.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;

namespace AshfallCamp.Editor.FigmaImport
{
    public static class SurvivorsFigmaProductionUpdater
    {
        public const string PayloadPath = "Assets/AshfallCamp/Art/UI/FigmaImports/SurvivorsElementwise/payload.json";
        public const string PreviewPrefabPath = "Assets/AshfallCamp/Prefabs/UI/FigmaExports/PF_SurvivorsElementwise.prefab";
        public const string SurvivorsPrefabPath = "Assets/AshfallCamp/Prefabs/UI/SurvivorsScreen.prefab";
        public const string ExpeditionsPrefabPath = "Assets/AshfallCamp/Prefabs/UI/ExpeditionsScreen.prefab";
        public const string WorkshopPrefabPath = "Assets/AshfallCamp/Prefabs/UI/WorkshopScreen.prefab";
        public const string ReportsPrefabPath = "Assets/AshfallCamp/Prefabs/UI/ReportsScreen.prefab";
        public const string RadioPrefabPath = "Assets/AshfallCamp/Prefabs/UI/RadioScreen.prefab";
        public const string DashboardPrefabPath = "Assets/AshfallCamp/Prefabs/UI/PF_CampDashboard.prefab";

        private const string FigmaVisualRootName = "FigmaImportedVisualRoot";
        private const string RuntimeBindingsRootName = "RuntimeBindings";

        private static readonly FigmaPanelIntegration[] ProductionPanels =
        {
            new FigmaPanelIntegration("worldMap", ExpeditionsPrefabPath, "ExpeditionsScreen", "expeditions", "expeditionsPanel", typeof(ExpeditionsPanelView), "WorldMapFigma"),
            new FigmaPanelIntegration("workshop", WorkshopPrefabPath, "WorkshopScreen", "workshop", "workshopPanel", typeof(WorkshopPanelView), "WorkshopFigma"),
            new FigmaPanelIntegration("reports", ReportsPrefabPath, "ReportsScreen", "reports", "reportsPanel", typeof(ReportsPanelView), "ReportsFigma"),
            new FigmaPanelIntegration("radioRecruitment", RadioPrefabPath, "RadioScreen", "radio", "radioPanel", typeof(RadioPanelView), "RadioRecruitmentFigma")
        };

        public static FigmaImportDocument LoadPayload()
        {
            var fullPath = Path.GetFullPath(PayloadPath);
            if (!File.Exists(fullPath))
            {
                throw new FileNotFoundException("Figma UI payload is missing. Run Import All Panels first.", PayloadPath);
            }

            var document = FigmaImportDocument.FromJson(File.ReadAllText(fullPath));
            if (document == null || document.frames == null || document.frames.Count == 0)
            {
                throw new InvalidOperationException("Figma UI payload is empty or invalid: " + PayloadPath);
            }

            return document;
        }

        public static void RebuildFromPayload(FigmaImportDocument document)
        {
            if (document == null) throw new ArgumentNullException(nameof(document));

            EnsureFolder("Assets/AshfallCamp/Prefabs/UI", "FigmaExports");
            BuildPreviewPrefabs(document);
            BuildSurvivorsProductionPrefab(document);
            for (var i = 0; i < ProductionPanels.Length; i++)
            {
                BuildProductionPanelPrefab(document, ProductionPanels[i]);
            }

            UpdateDashboardInstance(document);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
        }

        private static void BuildPreviewPrefabs(FigmaImportDocument document)
        {
            BuildSurvivorsPreviewPrefab(document);

            for (var i = 0; i < document.frames.Count; i++)
            {
                var frame = document.frames[i];
                if (frame == null || frame.tree == null || string.Equals(frame.key, "survivors", StringComparison.OrdinalIgnoreCase))
                {
                    continue;
                }

                BuildGenericPreviewPrefab(frame);
            }
        }

        private static void BuildSurvivorsPreviewPrefab(FigmaImportDocument document)
        {
            var root = new GameObject("PF_SurvivorsElementwise", typeof(RectTransform), typeof(CanvasGroup), typeof(SurvivorsPanelView));
            try
            {
                new FigmaUgUiBuilder().PopulateSurvivorsRoot(root, document, productionCrop: false);
                PrefabUtility.SaveAsPrefabAsset(root, PreviewPrefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildGenericPreviewPrefab(FigmaImportFrame frame)
        {
            var prefabName = "PF_" + ToPreviewName(frame.slug) + "Elementwise";
            var prefabPath = "Assets/AshfallCamp/Prefabs/UI/FigmaExports/" + prefabName + ".prefab";
            var root = new GameObject(prefabName, typeof(RectTransform), typeof(CanvasGroup));
            try
            {
                var rect = root.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(frame.width, frame.height);

                new FigmaUgUiBuilder().BuildFrame(frame, root.transform, new FigmaFrameBuildOptions
                {
                    rootName = ToPascalName(frame.slug),
                    active = true,
                    useCrop = false,
                    includeRootBackground = true,
                    addRootMask = false
                });

                PrefabUtility.SaveAsPrefabAsset(root, prefabPath);
            }
            finally
            {
                UnityEngine.Object.DestroyImmediate(root);
            }
        }

        private static void BuildSurvivorsProductionPrefab(FigmaImportDocument document)
        {
            var root = PrefabUtility.LoadPrefabContents(SurvivorsPrefabPath);
            try
            {
                ConfigureScreenRect(root);

                if (root.GetComponent<SurvivorsPanelView>() == null)
                {
                    root.AddComponent<SurvivorsPanelView>();
                }

                new FigmaUgUiBuilder().PopulateSurvivorsRoot(root, document, productionCrop: true);
                PrefabUtility.SaveAsPrefabAsset(root, SurvivorsPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void BuildProductionPanelPrefab(FigmaImportDocument document, FigmaPanelIntegration panel)
        {
            var frame = document.FindFrame(panel.frameKey);
            if (frame == null)
            {
                Debug.LogWarning("Skipping Figma production panel. Payload frame is missing: " + panel.frameKey);
                return;
            }

            if (!File.Exists(panel.prefabPath))
            {
                Debug.LogWarning("Skipping Figma production panel. Prefab is missing: " + panel.prefabPath);
                return;
            }

            var root = PrefabUtility.LoadPrefabContents(panel.prefabPath);
            try
            {
                root.name = panel.rootName;
                ConfigureScreenRect(root);
                if (root.GetComponent(panel.panelType) == null)
                {
                    root.AddComponent(panel.panelType);
                }

                var runtimeRoot = EnsureChildRect(root.transform, RuntimeBindingsRootName);
                MoveExistingChildrenToRuntimeBindings(root.transform, runtimeRoot);
                runtimeRoot.gameObject.SetActive(false);
                ClearRuntimeText(runtimeRoot);

                var visualRoot = EnsureChildRect(root.transform, FigmaVisualRootName);
                visualRoot.gameObject.SetActive(true);
                visualRoot.SetAsFirstSibling();
                ConfigureFullRootRect(visualRoot, root.GetComponent<RectTransform>().sizeDelta);
                FigmaUgUiBuilder.ClearChildren(visualRoot);

                var crop = new FigmaBounds(40f, 108f, 1840f, 790f);
                new FigmaUgUiBuilder().BuildFrame(frame, visualRoot, new FigmaFrameBuildOptions
                {
                    rootName = panel.figmaRootName,
                    active = true,
                    useCrop = true,
                    crop = crop,
                    includeRootBackground = true,
                    addRootMask = false
                });

                PrefabUtility.SaveAsPrefabAsset(root, panel.prefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }
        }

        private static void UpdateDashboardInstance(FigmaImportDocument document)
        {
            var dashboardRoot = PrefabUtility.LoadPrefabContents(DashboardPrefabPath);
            try
            {
                var dashboard = dashboardRoot.GetComponent<CampDashboardView>();
                if (dashboard == null)
                {
                    throw new InvalidOperationException("CampDashboardView is missing from " + DashboardPrefabPath);
                }

                ReplaceDashboardPanel<SurvivorsPanelView>(dashboardRoot, SurvivorsPrefabPath, "SurvivorsScreen", "survivors", "survivorsPanel");
                for (var i = 0; i < ProductionPanels.Length; i++)
                {
                    var panel = ProductionPanels[i];
                    if (document.FindFrame(panel.frameKey) == null)
                    {
                        continue;
                    }

                    ReplaceDashboardPanel(dashboardRoot, panel);
                }

                PrefabUtility.SaveAsPrefabAsset(dashboardRoot, DashboardPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(dashboardRoot);
            }
        }

        private static void ReplaceDashboardPanel<TPanel>(GameObject dashboardRoot, string prefabPath, string rootName, string screenId, string panelProperty)
            where TPanel : Component
        {
            ReplaceDashboardPanel(dashboardRoot, new FigmaPanelIntegration(string.Empty, prefabPath, rootName, screenId, panelProperty, typeof(TPanel), string.Empty));
        }

        private static void ReplaceDashboardPanel(GameObject dashboardRoot, FigmaPanelIntegration panel)
        {
            var dashboard = dashboardRoot.GetComponent<CampDashboardView>();
            var oldPanel = dashboardRoot.GetComponentInChildren(panel.panelType, true);
            var oldRoot = oldPanel != null ? oldPanel.gameObject : null;
            var parent = oldRoot != null && oldRoot.transform.parent != null ? oldRoot.transform.parent : dashboardRoot.transform;
            var siblingIndex = oldRoot != null ? oldRoot.transform.GetSiblingIndex() : parent.childCount;
            var activeSelf = oldRoot != null && oldRoot.activeSelf;
            var oldCanvasGroup = oldRoot != null ? oldRoot.GetComponent<CanvasGroup>() : null;

            var source = AssetDatabase.LoadAssetAtPath<GameObject>(panel.prefabPath);
            if (source == null)
            {
                throw new InvalidOperationException("Built prefab could not be loaded: " + panel.prefabPath);
            }

            var newRoot = (GameObject)PrefabUtility.InstantiatePrefab(source, dashboardRoot.scene);
            newRoot.name = panel.rootName;
            newRoot.transform.SetParent(parent, false);
            newRoot.transform.SetSiblingIndex(siblingIndex);
            newRoot.SetActive(activeSelf);

            var newCanvasGroup = newRoot.GetComponent<CanvasGroup>();
            if (newCanvasGroup == null)
            {
                newCanvasGroup = newRoot.AddComponent<CanvasGroup>();
            }

            CopyCanvasState(oldCanvasGroup, newCanvasGroup);

            if (oldRoot != null)
            {
                UnityEngine.Object.DestroyImmediate(oldRoot);
            }

            BindDashboardScreen(dashboard, panel.screenId, panel.panelProperty, newRoot, newRoot.GetComponent(panel.panelType), newCanvasGroup);
        }

        private static void BindDashboardScreen(CampDashboardView dashboard, string screenId, string panelProperty, GameObject screenRoot, Component panel, CanvasGroup canvasGroup)
        {
            var serialized = new SerializedObject(dashboard);
            var property = serialized.FindProperty(panelProperty);
            if (property != null)
            {
                property.objectReferenceValue = panel;
            }

            var screens = serialized.FindProperty("screens");
            var found = false;
            for (var i = 0; i < screens.arraySize; i++)
            {
                var item = screens.GetArrayElementAtIndex(i);
                if (!string.Equals(item.FindPropertyRelative("id").stringValue, screenId, StringComparison.Ordinal)) continue;

                SetScreenBinding(item, screenRoot, canvasGroup);
                found = true;
                break;
            }

            if (!found)
            {
                var index = screens.arraySize;
                screens.arraySize++;
                var item = screens.GetArrayElementAtIndex(index);
                item.FindPropertyRelative("id").stringValue = screenId;
                SetScreenBinding(item, screenRoot, canvasGroup);
            }

            serialized.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetScreenBinding(SerializedProperty item, GameObject screenRoot, CanvasGroup canvasGroup)
        {
            var roots = item.FindPropertyRelative("roots");
            roots.arraySize = 1;
            roots.GetArrayElementAtIndex(0).objectReferenceValue = screenRoot;

            var groups = item.FindPropertyRelative("canvasGroups");
            groups.arraySize = 1;
            groups.GetArrayElementAtIndex(0).objectReferenceValue = canvasGroup;
        }

        private static void ConfigureScreenRect(GameObject root)
        {
            var rect = root.GetComponent<RectTransform>();
            if (rect == null)
            {
                rect = root.AddComponent<RectTransform>();
            }

            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(40f, -108f);
            rect.sizeDelta = new Vector2(1840f, 790f);

            if (root.GetComponent<CanvasGroup>() == null)
            {
                root.AddComponent<CanvasGroup>();
            }
        }

        private static RectTransform EnsureChildRect(Transform parent, string childName)
        {
            var child = parent.Find(childName);
            if (child != null)
            {
                var existingRect = child.GetComponent<RectTransform>();
                return existingRect != null ? existingRect : child.gameObject.AddComponent<RectTransform>();
            }

            var go = new GameObject(childName, typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            return rect;
        }

        private static void MoveExistingChildrenToRuntimeBindings(Transform root, RectTransform runtimeRoot)
        {
            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child == runtimeRoot ||
                    string.Equals(child.name, FigmaVisualRootName, StringComparison.Ordinal) ||
                    string.Equals(child.name, RuntimeBindingsRootName, StringComparison.Ordinal))
                {
                    continue;
                }

                child.SetParent(runtimeRoot, false);
            }
        }

        private static void ConfigureFullRootRect(RectTransform rect, Vector2 size)
        {
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = Vector2.zero;
            rect.sizeDelta = size;
        }

        private static void ClearRuntimeText(RectTransform runtimeRoot)
        {
            var labels = runtimeRoot.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (var i = 0; i < labels.Length; i++)
            {
                labels[i].text = string.Empty;
            }
        }

        private static void CopyCanvasState(CanvasGroup oldCanvasGroup, CanvasGroup newCanvasGroup)
        {
            if (oldCanvasGroup != null)
            {
                newCanvasGroup.alpha = oldCanvasGroup.alpha;
                newCanvasGroup.interactable = oldCanvasGroup.interactable;
                newCanvasGroup.blocksRaycasts = oldCanvasGroup.blocksRaycasts;
                newCanvasGroup.ignoreParentGroups = oldCanvasGroup.ignoreParentGroups;
                return;
            }

            newCanvasGroup.alpha = 0f;
            newCanvasGroup.interactable = false;
            newCanvasGroup.blocksRaycasts = false;
        }

        private static string ToPascalName(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return "Frame";

            var result = string.Empty;
            var makeUpper = true;
            for (var i = 0; i < value.Length; i++)
            {
                var ch = value[i];
                if (!char.IsLetterOrDigit(ch))
                {
                    makeUpper = true;
                    continue;
                }

                result += makeUpper ? char.ToUpperInvariant(ch) : ch;
                makeUpper = false;
            }

            return string.IsNullOrWhiteSpace(result) ? "Frame" : result;
        }

        private static string ToPreviewName(string slug)
        {
            if (string.IsNullOrWhiteSpace(slug)) return "Frame";

            var result = string.Empty;
            var parts = slug.Split(new[] { '-' }, StringSplitOptions.RemoveEmptyEntries);
            for (var i = 0; i < parts.Length; i++)
            {
                var part = parts[i];
                if (i == 0 && IsAllDigits(part)) continue;
                if (string.Equals(part, "elementwise", StringComparison.OrdinalIgnoreCase)) continue;

                result += ToPascalName(part);
            }

            return string.IsNullOrWhiteSpace(result) ? ToPascalName(slug) : result;
        }

        private static bool IsAllDigits(string value)
        {
            if (string.IsNullOrWhiteSpace(value)) return false;
            for (var i = 0; i < value.Length; i++)
            {
                if (!char.IsDigit(value[i])) return false;
            }

            return true;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (AssetDatabase.IsValidFolder(path)) return;
            AssetDatabase.CreateFolder(parent, child);
        }

        private sealed class FigmaPanelIntegration
        {
            public readonly string frameKey;
            public readonly string prefabPath;
            public readonly string rootName;
            public readonly string screenId;
            public readonly string panelProperty;
            public readonly Type panelType;
            public readonly string figmaRootName;

            public FigmaPanelIntegration(string frameKey, string prefabPath, string rootName, string screenId, string panelProperty, Type panelType, string figmaRootName)
            {
                this.frameKey = frameKey;
                this.prefabPath = prefabPath;
                this.rootName = rootName;
                this.screenId = screenId;
                this.panelProperty = panelProperty;
                this.panelType = panelType;
                this.figmaRootName = figmaRootName;
            }
        }
    }
}
