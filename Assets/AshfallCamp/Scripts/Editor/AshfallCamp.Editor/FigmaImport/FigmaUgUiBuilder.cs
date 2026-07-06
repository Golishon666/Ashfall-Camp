using System;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Editor.FigmaImport
{
    public sealed class FigmaUgUiBuilder
    {
        public FigmaBuiltFrame BuildFrame(FigmaImportFrame frame, Transform parent, FigmaFrameBuildOptions options)
        {
            if (frame == null) throw new ArgumentNullException(nameof(frame));
            if (frame.tree == null) throw new ArgumentException("Frame payload has no tree.", nameof(frame));
            if (parent == null) throw new ArgumentNullException(nameof(parent));

            var rootBounds = options.useCrop
                ? options.crop
                : new FigmaBounds(0f, 0f, Mathf.Max(1f, frame.width), Mathf.Max(1f, frame.height));

            var root = CreateRect(
                string.IsNullOrWhiteSpace(options.rootName) ? FigmaImportNameUtility.SanitizeUnityName(frame.frameName) : options.rootName,
                parent,
                rootBounds,
                rootBounds);
            root.gameObject.SetActive(options.active);

            if (options.addRootMask)
            {
                root.gameObject.AddComponent<RectMask2D>();
            }

            if (options.includeRootBackground)
            {
                AddGraphicForNode(frame.tree, root, forceSimple: true);
            }

            var stats = new FigmaImportBuildStats();
            if (frame.tree.children != null)
            {
                for (var i = 0; i < frame.tree.children.Count; i++)
                {
                    BuildNode(frame.tree.children[i], root, rootBounds, options, stats);
                }
            }

            return new FigmaBuiltFrame(root, stats);
        }

        public void PopulateSurvivorsRoot(GameObject root, FigmaImportDocument document, bool productionCrop)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (document == null) throw new ArgumentNullException(nameof(document));

            ClearChildren(root.transform);

            var rosterFrame = document.FindFrame("survivors") ?? document.FindFrame("06-survivors-elementwise");
            if (rosterFrame == null)
            {
                throw new InvalidOperationException("Survivors Figma payload does not contain the roster frame.");
            }

            var detailFrame = document.FindFrame("survivorDetail") ?? document.FindFrame("07-survivor-detail-elementwise");
            var rootRect = EnsureRectTransform(root);
            rootRect.anchorMin = new Vector2(0f, 1f);
            rootRect.anchorMax = new Vector2(0f, 1f);
            rootRect.pivot = new Vector2(0f, 1f);

            if (!productionCrop)
            {
                rootRect.anchoredPosition = Vector2.zero;
                rootRect.sizeDelta = new Vector2(rosterFrame.width, rosterFrame.height);
            }

            var crop = productionCrop
                ? new FigmaBounds(40f, 108f, Mathf.Max(1f, rootRect.sizeDelta.x), Mathf.Max(1f, rootRect.sizeDelta.y))
                : new FigmaBounds(0f, 0f, Mathf.Max(1f, rosterFrame.width), Mathf.Max(1f, rosterFrame.height));

            var roster = BuildFrame(rosterFrame, root.transform, new FigmaFrameBuildOptions
            {
                rootName = "RosterScreen",
                active = true,
                useCrop = productionCrop,
                crop = crop,
                includeRootBackground = true,
                addRootMask = false
            });

            RectTransform detailRoot = null;
            if (detailFrame != null)
            {
                var detail = BuildFrame(detailFrame, root.transform, new FigmaFrameBuildOptions
                {
                    rootName = "SurvivorDetailScreen",
                    active = false,
                    useCrop = productionCrop,
                    crop = crop,
                    includeRootBackground = true,
                    addRootMask = false
                });
                detailRoot = detail.root;
            }
            else
            {
                detailRoot = CreateRect("SurvivorDetailScreen", root.transform, crop, crop);
                detailRoot.gameObject.SetActive(false);
            }

            SurvivorsFigmaBindingMapper.Bind(root, roster.root, detailRoot);
        }

        public static Slider CreatePassiveSlider(string name, RectTransform parent, float x, float y, float width, float height, Color trackColor, Color fillColor, float value)
        {
            var root = CreateRect(name, parent, new FigmaBounds(x, y, width, height), new FigmaBounds(0f, 0f, 0f, 0f));
            return CreateSliderOnRect(root, width, height, trackColor, fillColor, Mathf.Clamp01(value), null);
        }

        public static void ClearChildren(Transform parent)
        {
            for (var i = parent.childCount - 1; i >= 0; i--)
            {
                UnityEngine.Object.DestroyImmediate(parent.GetChild(i).gameObject);
            }
        }

        private static void BuildNode(FigmaImportNode node, RectTransform parent, FigmaBounds parentBounds, FigmaFrameBuildOptions options, FigmaImportBuildStats stats)
        {
            if (node == null || string.Equals(node.renderMode, "skip", StringComparison.OrdinalIgnoreCase)) return;
            if (options.useCrop && !options.crop.ContainsCenterOf(node.bounds) && !IsEssentialOutsideCropNode(node)) return;

            FigmaSliderDefinition slider;
            if (FigmaSliderDetector.TryCreate(node, out slider))
            {
                CreateSliderNode(slider, parent, parentBounds, stats);
                return;
            }

            var rect = CreateRect(FigmaImportNameUtility.SanitizeUnityName(node.name), parent, node.bounds, parentBounds);
            var graphic = CreateVisualForNode(node, rect);

            if (node.IsText)
            {
                CreateTextForNode(node, rect);
                stats.texts++;
            }
            else if (graphic != null)
            {
                stats.graphics++;
            }

            if (node.clipsContent)
            {
                rect.gameObject.AddComponent<RectMask2D>();
            }

            AddButtonIfNeeded(node, rect, graphic);

            if (node.children != null)
            {
                for (var i = 0; i < node.children.Count; i++)
                {
                    BuildNode(node.children[i], rect, node.bounds, options, stats);
                }
            }
        }

        private static void CreateSliderNode(FigmaSliderDefinition definition, RectTransform parent, FigmaBounds parentBounds, FigmaImportBuildStats stats)
        {
            var node = definition.node;
            var rect = CreateRect(FigmaImportNameUtility.SanitizeUnityName(node.name), parent, node.bounds, parentBounds);

            Color trackColor;
            if (!FigmaImportStyleUtility.TryGetSolidColor(definition.track, out trackColor))
            {
                trackColor = new Color32(216, 197, 166, 255);
            }

            Color fillColor;
            if (!FigmaImportStyleUtility.TryGetSolidColor(definition.fill, out fillColor))
            {
                fillColor = new Color32(76, 129, 71, 255);
            }

            var height = Mathf.Max(2f, definition.track.bounds.height);
            var slider = CreateSliderOnRect(rect, Mathf.Max(1f, node.bounds.width), height, trackColor, fillColor, definition.value, definition);
            slider.name = rect.name;
            stats.sliders++;
        }

        private static Slider CreateSliderOnRect(RectTransform rect, float width, float height, Color trackColor, Color fillColor, float value, FigmaSliderDefinition? source)
        {
            var trackBounds = source.HasValue ? source.Value.track.bounds : new FigmaBounds(0f, 0f, width, height);
            var localTrack = source.HasValue
                ? new FigmaBounds(trackBounds.x - source.Value.node.bounds.x, trackBounds.y - source.Value.node.bounds.y, trackBounds.width, trackBounds.height)
                : trackBounds;

            var track = CreateRect("Track", rect, localTrack, new FigmaBounds(0f, 0f, 0f, 0f));
            var trackImage = track.gameObject.AddComponent<Image>();
            trackImage.color = trackColor;
            trackImage.raycastTarget = false;

            var fillArea = CreateRect("Fill Area", rect, localTrack, new FigmaBounds(0f, 0f, 0f, 0f));
            fillArea.anchorMin = new Vector2(0f, 1f);
            fillArea.anchorMax = new Vector2(0f, 1f);

            var fill = CreateRect("Fill", fillArea, new FigmaBounds(0f, 0f, localTrack.width, localTrack.height), new FigmaBounds(0f, 0f, 0f, 0f));
            var fillImage = fill.gameObject.AddComponent<Image>();
            fillImage.color = fillColor;
            fillImage.raycastTarget = false;

            var handleArea = CreateRect("Handle Slide Area", rect, localTrack, new FigmaBounds(0f, 0f, 0f, 0f));
            var handleSize = Mathf.Max(10f, localTrack.height + 6f);
            var handle = CreateRect("Handle", handleArea, new FigmaBounds(value * localTrack.width - handleSize * 0.5f, (localTrack.height - handleSize) * 0.5f, handleSize, handleSize), new FigmaBounds(0f, 0f, 0f, 0f));
            var handleImage = handle.gameObject.AddComponent<Image>();
            handleImage.color = new Color(fillColor.r, fillColor.g, fillColor.b, 0f);
            handleImage.raycastTarget = false;

            var slider = rect.gameObject.AddComponent<Slider>();
            slider.transition = Selectable.Transition.None;
            slider.interactable = false;
            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.fillRect = fill;
            slider.handleRect = handle;
            slider.targetGraphic = handleImage;
            ApplySliderValue(slider, value);
            return slider;
        }

        private static void ApplySliderValue(Slider slider, float value)
        {
            var clamped = Mathf.Clamp01(value);
            slider.SetValueWithoutNotify(clamped);

            if (slider.fillRect != null)
            {
                slider.fillRect.anchorMin = Vector2.zero;
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

        private static Graphic CreateVisualForNode(FigmaImportNode node, RectTransform rect)
        {
            if (node.IsText)
            {
                return null;
            }

            var isVisual = string.Equals(node.renderMode, "visual", StringComparison.OrdinalIgnoreCase) ||
                           string.Equals(node.renderMode, "background", StringComparison.OrdinalIgnoreCase);
            var isSimple = FigmaImportStyleUtility.IsSimpleSolid(node);
            if (!isVisual && !isSimple) return null;

            return AddGraphicForNode(node, rect, forceSimple: isSimple);
        }

        private static Graphic AddGraphicForNode(FigmaImportNode node, RectTransform rect, bool forceSimple)
        {
            if (node == null || rect == null) return null;

            Color color;
            var hasSolid = FigmaImportStyleUtility.TryGetSolidColor(node, out color);
            if (forceSimple || FigmaImportStyleUtility.IsSimpleSolid(node) || string.IsNullOrWhiteSpace(node.assetPath))
            {
                if (!hasSolid && string.IsNullOrWhiteSpace(node.assetPath)) return null;

                var image = rect.gameObject.AddComponent<Image>();
                image.color = hasSolid ? color : Color.white;
                image.raycastTarget = false;
                return image;
            }

            var rawImage = rect.gameObject.AddComponent<RawImage>();
            rawImage.texture = AssetDatabase.LoadAssetAtPath<Texture2D>(node.assetPath);
            rawImage.color = rawImage.texture == null ? Color.clear : new Color(1f, 1f, 1f, FigmaImportStyleUtility.NodeOpacity(node));
            rawImage.raycastTarget = false;
            return rawImage;
        }

        private static void CreateTextForNode(FigmaImportNode node, RectTransform rect)
        {
            var label = rect.gameObject.AddComponent<TextMeshProUGUI>();
            label.text = node.text != null ? node.text.characters ?? string.Empty : string.Empty;
            label.fontSize = node.text != null && node.text.fontSize > 0f ? node.text.fontSize : 16f;
            label.fontStyle = ResolveFontStyle(node.text != null ? node.text.fontName : null);
            label.alignment = FigmaImportStyleUtility.TextAlignment(node.text);
            label.color = FigmaImportStyleUtility.TextColor(node);
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;
            label.margin = Vector4.zero;
            label.raycastTarget = false;
            if (node.text != null && node.text.lineHeight != null && node.text.lineHeight.value > 0f)
            {
                label.lineSpacing = 0f;
            }
        }

        private static FontStyles ResolveFontStyle(FigmaFontName fontName)
        {
            if (fontName == null || string.IsNullOrWhiteSpace(fontName.style)) return FontStyles.Normal;

            var style = fontName.style.ToLowerInvariant();
            var result = FontStyles.Normal;
            if (style.Contains("bold") || style.Contains("black") || style.Contains("heavy") || style.Contains("semibold"))
            {
                result |= FontStyles.Bold;
            }

            if (style.Contains("italic"))
            {
                result |= FontStyles.Italic;
            }

            return result;
        }

        private static void AddButtonIfNeeded(FigmaImportNode node, RectTransform rect, Graphic graphic)
        {
            var name = FigmaImportNameUtility.NormalizeForSearch(node.name);
            if (!name.StartsWith("button", StringComparison.Ordinal) && !name.Contains("button")) return;

            var button = rect.gameObject.GetComponent<Button>();
            if (button == null)
            {
                button = rect.gameObject.AddComponent<Button>();
            }

            var target = graphic;
            if (target == null)
            {
                var image = rect.gameObject.AddComponent<Image>();
                image.color = new Color(1f, 1f, 1f, 0f);
                target = image;
            }

            target.raycastTarget = true;
            button.targetGraphic = target;
            button.transition = Selectable.Transition.ColorTint;
        }

        private static bool IsEssentialOutsideCropNode(FigmaImportNode node)
        {
            var name = FigmaImportNameUtility.NormalizeForSearch(node != null ? node.name : string.Empty);
            return name == "buttonx" || name == "closebutton" || name == "buttonclose";
        }

        private static RectTransform CreateRect(string name, Transform parent, FigmaBounds nodeBounds, FigmaBounds parentBounds)
        {
            var go = new GameObject(UniqueChildName(parent, name), typeof(RectTransform));
            var rect = go.GetComponent<RectTransform>();
            rect.SetParent(parent, false);
            rect.anchorMin = new Vector2(0f, 1f);
            rect.anchorMax = new Vector2(0f, 1f);
            rect.pivot = new Vector2(0f, 1f);
            rect.anchoredPosition = new Vector2(nodeBounds.x - parentBounds.x, -(nodeBounds.y - parentBounds.y));
            rect.sizeDelta = new Vector2(Mathf.Max(0f, nodeBounds.width), Mathf.Max(0f, nodeBounds.height));
            rect.localRotation = Quaternion.identity;
            rect.localScale = Vector3.one;
            return rect;
        }

        private static RectTransform EnsureRectTransform(GameObject target)
        {
            var rect = target.GetComponent<RectTransform>();
            return rect != null ? rect : target.AddComponent<RectTransform>();
        }

        private static string UniqueChildName(Transform parent, string desired)
        {
            desired = string.IsNullOrWhiteSpace(desired) ? "Node" : desired;
            if (parent == null || parent.Find(desired) == null) return desired;

            var index = 2;
            string candidate;
            do
            {
                candidate = desired + "_" + index.ToString("00");
                index++;
            } while (parent.Find(candidate) != null);

            return candidate;
        }
    }

    public struct FigmaFrameBuildOptions
    {
        public string rootName;
        public bool active;
        public bool useCrop;
        public FigmaBounds crop;
        public bool includeRootBackground;
        public bool addRootMask;
    }

    public readonly struct FigmaBuiltFrame
    {
        public readonly RectTransform root;
        public readonly FigmaImportBuildStats stats;

        public FigmaBuiltFrame(RectTransform root, FigmaImportBuildStats stats)
        {
            this.root = root;
            this.stats = stats;
        }
    }

    public struct FigmaImportBuildStats
    {
        public int texts;
        public int graphics;
        public int sliders;
    }
}
