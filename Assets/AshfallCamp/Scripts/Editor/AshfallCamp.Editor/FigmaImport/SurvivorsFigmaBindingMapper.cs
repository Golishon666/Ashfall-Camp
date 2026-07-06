using System;
using System.Collections.Generic;
using AshfallCamp.Presentation;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Editor.FigmaImport
{
    public static class SurvivorsFigmaBindingMapper
    {
        public static void Bind(GameObject root, RectTransform rosterRoot, RectTransform detailRoot)
        {
            if (root == null) throw new ArgumentNullException(nameof(root));
            if (rosterRoot == null) throw new ArgumentNullException(nameof(rosterRoot));
            if (detailRoot == null) throw new ArgumentNullException(nameof(detailRoot));

            var view = root.GetComponent<SurvivorsPanelView>();
            if (view == null)
            {
                view = root.AddComponent<SurvivorsPanelView>();
            }

            var runtime = RuntimeBindings.Create(root.transform);
            var rosterCards = BuildCardBindings(rosterRoot, runtime);
            var selectedPanel = FindRect(rosterRoot, "PanelSelectedSurvivor");
            var selectedPortrait = FindRawImage(selectedPanel != null ? selectedPanel : rosterRoot, "SelectedPortrait", "Portrait");
            var viewDetailsButton = FindButton(selectedPanel != null ? selectedPanel : rosterRoot, "ButtonViewDetails", "ViewDetails");

            var profile = FindRect(detailRoot, "ProfilePanel");
            var wounds = FindRect(detailRoot, "PanelWoundsStatus", "WoundsStatus");
            var actions = FindRect(detailRoot, "PanelActions", "Actions");
            var equipment = FindRect(detailRoot, "PanelEquipment", "Equipment");

            var healthSlider = FindSlider(profile, "SliderHealth", "Health");
            var fatigueSlider = FindSlider(profile, "SliderFatigue", "Fatigue");
            var moraleSlider = FindSlider(profile, "SliderMorale", "Morale");
            var actionButton = FindButton(actions, "ButtonTreat", "Treat") ?? FindButton(actions, "ButtonRest", "Rest") ?? runtime.Button("UseMedicineButton", string.Empty);

            view.ConfigureBindings(
                FindText(rosterRoot, "ScreenTitle") ?? runtime.Text("Title"),
                runtime.Text("CountLabel"),
                runtime.Image("EmptyPanel"),
                runtime.Text("EmptyTitle"),
                runtime.Text("EmptyBody"),
                rosterCards,
                EnsureImage(profile != null ? profile : detailRoot),
                FindText(profile, "Name") ?? runtime.Text("DetailTitle"),
                FindText(wounds, "Note", "Desc") ?? runtime.Text("DetailBackground"),
                CombineTexts(runtime, "DetailTraits", FindText(profile, "TraitName"), FindText(profile, "TraitDesc")),
                FindEquipmentText(equipment, "Weapon") ?? runtime.Text("DetailWeapon"),
                runtime.Text("DetailStats"),
                FindEquipmentText(equipment, "Supplies") ?? runtime.Text("DetailTreatment"),
                runtime.Text("DetailActionCost"),
                FindRawImage(profile, "JadePortrait", "Portrait") ?? runtime.RawImage("DetailPortrait"),
                actionButton,
                FindText(actionButton != null ? actionButton.transform : null, "Label") ?? runtime.Text("UseMedicineButtonLabel"),
                runtime.RawImage("EmptyPanelArtwork"),
                runtime.RawImage("DetailPanelArtwork"),
                rosterRoot.gameObject,
                detailRoot.gameObject,
                viewDetailsButton ?? runtime.Button("ViewDetailsButton", string.Empty),
                FindButton(detailRoot, "ButtonX", "CloseButton") ?? runtime.Button("CloseDetailButton", string.Empty),
                FindText(profile, "Level") ?? runtime.Text("DetailLevel"),
                FindText(profile, "XP", "Xp") ?? runtime.Text("DetailXp"),
                FindText(profile, "ValueCurrentState", "CurrentState") ?? runtime.Text("DetailStatus"),
                healthSlider ?? runtime.Slider("DetailHealthSlider"),
                fatigueSlider ?? runtime.Slider("DetailFatigueSlider"),
                moraleSlider ?? runtime.Slider("DetailMoraleSlider"),
                FindText(profile, "ValueHealth") ?? runtime.Text("DetailHealthValue"),
                FindText(profile, "ValueFatigue") ?? runtime.Text("DetailFatigueValue"),
                FindText(profile, "ValueMorale") ?? runtime.Text("DetailMoraleValue"),
                selectedPortrait ?? runtime.RawImage("SelectedSummaryPortrait"),
                EnsureImage(selectedPanel),
                runtime.Text("SelectedSummaryAvatar"),
                FindText(selectedPanel, "Name") ?? runtime.Text("SelectedSummaryName"),
                FindText(selectedPanel, "Role") ?? runtime.Text("SelectedSummaryRole"),
                FindText(selectedPanel, "Level") ?? runtime.Text("SelectedSummaryLevel"),
                FindText(selectedPanel, "Power") ?? runtime.Text("SelectedSummaryPower"),
                FindText(selectedPanel, "Summary") ?? runtime.Text("SelectedSummaryNote"));
        }

        private static IEnumerable<SurvivorsPanelView.SurvivorCardBinding> BuildCardBindings(RectTransform rosterRoot, RuntimeBindings runtime)
        {
            var cards = FindRects(rosterRoot, "SurvivorCard");
            cards.Sort(CompareRectsTopLeft);

            var result = new List<SurvivorsPanelView.SurvivorCardBinding>();
            for (var i = 0; i < cards.Count; i++)
            {
                var card = cards[i];
                if (card == null) continue;

                var panel = EnsureImage(card);
                var button = card.GetComponent<Button>();
                if (button == null)
                {
                    button = card.gameObject.AddComponent<Button>();
                    button.transition = Selectable.Transition.ColorTint;
                }

                button.targetGraphic = panel;
                panel.raycastTarget = true;

                result.Add(new SurvivorsPanelView.SurvivorCardBinding(
                    panel,
                    runtime.RawImage("CardArtwork_" + i.ToString("00")),
                    button,
                    FindRawImage(card, "Portrait") ?? runtime.RawImage("CardPortrait_" + i.ToString("00")),
                    runtime.Text("CardAvatar_" + i.ToString("00")),
                    FindText(card, "Name") ?? runtime.Text("CardName_" + i.ToString("00")),
                    runtime.Text("CardState_" + i.ToString("00")),
                    FindText(card, "Role") ?? runtime.Text("CardSkill_" + i.ToString("00")),
                    FindText(card, "LevelLabel", "Level") ?? runtime.Text("CardLevel_" + i.ToString("00")),
                    FindText(card, "Power") ?? runtime.Text("CardPower_" + i.ToString("00")),
                    FindSlider(card, "SliderHp", "SliderHealth", "HP") ?? runtime.Slider("CardHealthSlider_" + i.ToString("00")),
                    FindSlider(card, "SliderFatigue", "Fatigue") ?? runtime.Slider("CardFatigueSlider_" + i.ToString("00")),
                    FindText(card, "HpValue", "HealthValue") ?? runtime.Text("CardHealthValue_" + i.ToString("00")),
                    FindText(card, "FatigueValue") ?? runtime.Text("CardFatigueValue_" + i.ToString("00"))));
            }

            return result;
        }

        private static TextMeshProUGUI CombineTexts(RuntimeBindings runtime, string name, TextMeshProUGUI first, TextMeshProUGUI second)
        {
            if (first != null && second == null) return first;
            if (first == null && second != null) return second;
            if (first == null) return runtime.Text(name);

            first.text = first.text + "\n" + second.text;
            return first;
        }

        private static TextMeshProUGUI FindEquipmentText(Transform equipment, string label)
        {
            if (equipment == null) return null;

            var rows = FindRects(equipment, "EquipmentRow" + label, label);
            if (rows.Count == 0) return null;

            return FindText(rows[0], "Title") ?? FindText(rows[0], "Detail") ?? rows[0].GetComponentInChildren<TextMeshProUGUI>(true);
        }

        private static int CompareRectsTopLeft(RectTransform a, RectTransform b)
        {
            var ay = a != null ? a.anchoredPosition.y : 0f;
            var by = b != null ? b.anchoredPosition.y : 0f;
            if (!Mathf.Approximately(ay, by)) return by.CompareTo(ay);

            var ax = a != null ? a.anchoredPosition.x : 0f;
            var bx = b != null ? b.anchoredPosition.x : 0f;
            return ax.CompareTo(bx);
        }

        private static RectTransform FindRect(Transform root, params string[] tokens)
        {
            var all = FindRects(root, tokens);
            return all.Count > 0 ? all[0] : null;
        }

        private static List<RectTransform> FindRects(Transform root, params string[] tokens)
        {
            var result = new List<RectTransform>();
            if (root == null || tokens == null || tokens.Length == 0) return result;

            var rects = root.GetComponentsInChildren<RectTransform>(true);
            for (var i = 0; i < rects.Length; i++)
            {
                var rect = rects[i];
                if (rect == null || rect == root) continue;
                if (FigmaImportNameUtility.Matches(rect, tokens))
                {
                    result.Add(rect);
                }
            }

            return result;
        }

        private static TextMeshProUGUI FindText(Transform root, params string[] tokens)
        {
            if (root == null || tokens == null || tokens.Length == 0) return null;

            var labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
            for (var exactPass = 0; exactPass < 2; exactPass++)
            {
                for (var i = 0; i < labels.Length; i++)
                {
                    var label = labels[i];
                    if (label == null) continue;

                    var labelName = FigmaImportNameUtility.NormalizeForSearch(label.transform);
                    for (var t = 0; t < tokens.Length; t++)
                    {
                        var token = FigmaImportNameUtility.NormalizeForSearch(tokens[t]);
                        if (token.Length == 0) continue;
                        if ((exactPass == 0 && labelName == token) || (exactPass == 1 && labelName.Contains(token)))
                        {
                            return label;
                        }
                    }
                }
            }

            return null;
        }

        private static RawImage FindRawImage(Transform root, params string[] tokens)
        {
            if (root == null) return null;

            var images = root.GetComponentsInChildren<RawImage>(true);
            for (var exactPass = 0; exactPass < 2; exactPass++)
            {
                for (var i = 0; i < images.Length; i++)
                {
                    var image = images[i];
                    if (image == null) continue;

                    var imageName = FigmaImportNameUtility.NormalizeForSearch(image.transform);
                    for (var t = 0; t < tokens.Length; t++)
                    {
                        var token = FigmaImportNameUtility.NormalizeForSearch(tokens[t]);
                        if (token.Length == 0) continue;
                        if ((exactPass == 0 && imageName == token) || (exactPass == 1 && imageName.Contains(token)))
                        {
                            return image;
                        }
                    }
                }
            }

            return null;
        }

        private static Slider FindSlider(Transform root, params string[] tokens)
        {
            if (root == null) return null;

            var sliders = root.GetComponentsInChildren<Slider>(true);
            for (var exactPass = 0; exactPass < 2; exactPass++)
            {
                for (var i = 0; i < sliders.Length; i++)
                {
                    var slider = sliders[i];
                    if (slider == null) continue;

                    var name = FigmaImportNameUtility.NormalizeForSearch(slider.transform);
                    for (var t = 0; t < tokens.Length; t++)
                    {
                        var token = FigmaImportNameUtility.NormalizeForSearch(tokens[t]);
                        if (token.Length == 0) continue;
                        if ((exactPass == 0 && name == token) || (exactPass == 1 && name.Contains(token)))
                        {
                            return slider;
                        }
                    }
                }
            }

            return null;
        }

        private static Button FindButton(Transform root, params string[] tokens)
        {
            if (root == null) return null;

            var buttons = root.GetComponentsInChildren<Button>(true);
            for (var exactPass = 0; exactPass < 2; exactPass++)
            {
                for (var i = 0; i < buttons.Length; i++)
                {
                    var button = buttons[i];
                    if (button == null) continue;

                    var name = FigmaImportNameUtility.NormalizeForSearch(button.transform);
                    for (var t = 0; t < tokens.Length; t++)
                    {
                        var token = FigmaImportNameUtility.NormalizeForSearch(tokens[t]);
                        if (token.Length == 0) continue;
                        if ((exactPass == 0 && name == token) || (exactPass == 1 && name.Contains(token)))
                        {
                            return button;
                        }
                    }
                }
            }

            return null;
        }

        private static Image EnsureImage(Transform target)
        {
            if (target == null) return null;

            var image = target.GetComponent<Image>();
            if (image != null) return image;

            image = target.gameObject.AddComponent<Image>();
            image.color = new Color(1f, 1f, 1f, 0f);
            image.raycastTarget = false;
            return image;
        }

        private sealed class RuntimeBindings
        {
            private readonly RectTransform _root;
            private readonly Dictionary<string, Component> _cache = new Dictionary<string, Component>(StringComparer.Ordinal);

            private RuntimeBindings(RectTransform root)
            {
                _root = root;
            }

            public static RuntimeBindings Create(Transform parent)
            {
                var existing = parent.Find("RuntimeBindings");
                if (existing != null)
                {
                    UnityEngine.Object.DestroyImmediate(existing.gameObject);
                }

                var go = new GameObject("RuntimeBindings", typeof(RectTransform), typeof(CanvasGroup));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(parent, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = Vector2.zero;

                var group = go.GetComponent<CanvasGroup>();
                group.alpha = 0f;
                group.interactable = false;
                group.blocksRaycasts = false;

                return new RuntimeBindings(rect);
            }

            public TextMeshProUGUI Text(string name)
            {
                return GetOrCreate(name, () =>
                {
                    var rect = Child(name, 120f, 24f);
                    var text = rect.gameObject.AddComponent<TextMeshProUGUI>();
                    text.text = string.Empty;
                    text.fontSize = 12f;
                    text.textWrappingMode = TextWrappingModes.Normal;
                    text.overflowMode = TextOverflowModes.Truncate;
                    text.raycastTarget = false;
                    return text;
                });
            }

            public RawImage RawImage(string name)
            {
                return GetOrCreate(name, () =>
                {
                    var rect = Child(name, 32f, 32f);
                    var image = rect.gameObject.AddComponent<RawImage>();
                    image.color = Color.clear;
                    image.raycastTarget = false;
                    return image;
                });
            }

            public Image Image(string name)
            {
                return GetOrCreate(name, () =>
                {
                    var rect = Child(name, 32f, 32f);
                    var image = rect.gameObject.AddComponent<Image>();
                    image.color = Color.clear;
                    image.raycastTarget = false;
                    return image;
                });
            }

            public Button Button(string name, string label)
            {
                return GetOrCreate(name, () =>
                {
                    var image = Image(name + "_Graphic");
                    var button = image.gameObject.AddComponent<Button>();
                    button.targetGraphic = image;
                    button.transition = Selectable.Transition.None;
                    if (!string.IsNullOrEmpty(label))
                    {
                        Text(name + "_Label").text = label;
                    }

                    return button;
                });
            }

            public Slider Slider(string name)
            {
                return GetOrCreate(name, () => FigmaUgUiBuilder.CreatePassiveSlider(name, _root, 0f, 0f, 100f, 8f, new Color32(216, 197, 166, 255), new Color32(76, 129, 71, 255), 0.5f));
            }

            private RectTransform Child(string name, float width, float height)
            {
                var go = new GameObject(name, typeof(RectTransform));
                var rect = go.GetComponent<RectTransform>();
                rect.SetParent(_root, false);
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = Vector2.zero;
                rect.sizeDelta = new Vector2(width, height);
                return rect;
            }

            private T GetOrCreate<T>(string name, Func<T> factory) where T : Component
            {
                Component component;
                if (_cache.TryGetValue(name, out component))
                {
                    return component as T;
                }

                var created = factory();
                _cache[name] = created;
                return created;
            }
        }
    }
}
