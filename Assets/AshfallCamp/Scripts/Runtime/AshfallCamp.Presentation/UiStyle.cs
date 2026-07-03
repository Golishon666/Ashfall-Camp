using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshfallCamp.Presentation
{
    internal static class UiStyle
    {
        public static VisualElement Panel(string title, CampUiTheme theme)
        {
            var panel = new VisualElement();
            panel.style.backgroundColor = new Color(theme.Paper.r, theme.Paper.g, theme.Paper.b, 0.86f);
            panel.style.paddingLeft = 14;
            panel.style.paddingRight = 14;
            panel.style.paddingTop = 12;
            panel.style.paddingBottom = 12;
            SetBorder(panel, theme.Line, 1);
            SetRadius(panel, 5);

            if (!string.IsNullOrEmpty(title))
            {
                var label = new Label(title);
                label.style.fontSize = 18;
                label.style.color = theme.Teal;
                label.style.unityFontStyleAndWeight = FontStyle.Bold;
                label.style.marginBottom = 4;
                panel.Add(label);
            }

            return panel;
        }

        public static Button CommandButton(string text, bool primary, Action clicked, CampUiTheme theme)
        {
            var button = new Button(clicked);
            button.text = text;
            button.style.height = 36;
            button.style.backgroundColor = primary ? theme.Rust : new Color(theme.Paper.r, theme.Paper.g, theme.Paper.b, 0.72f);
            button.style.color = primary ? theme.Paper : theme.Ink;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            SetBorder(button, primary ? new Color(0.44f, 0.14f, 0.08f, 0.95f) : theme.Line, 1);
            SetRadius(button, 3);
            return button;
        }

        public static Label SmallText(string text, CampUiTheme theme)
        {
            var label = new Label(text);
            label.style.fontSize = 13;
            label.style.color = theme.Ink;
            label.style.whiteSpace = WhiteSpace.Normal;
            label.style.marginTop = 8;
            label.style.marginBottom = 10;
            return label;
        }

        public static VisualElement MeterRow(string title, float value, string text, CampUiTheme theme)
        {
            var row = new VisualElement();
            row.style.height = 32;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;

            var label = new Label(title);
            label.style.width = 82;
            label.style.color = theme.Ink;
            label.style.fontSize = 12;
            row.Add(label);

            var meter = new VisualElement();
            meter.style.flexGrow = 1;
            meter.style.height = 8;
            meter.style.backgroundColor = new Color(0.24f, 0.21f, 0.17f, 0.22f);
            SetRadius(meter, 4);
            row.Add(meter);

            var fill = new VisualElement();
            fill.style.width = Length.Percent(Mathf.Clamp01(value) * 100);
            fill.style.height = Length.Percent(100);
            fill.style.backgroundColor = theme.Sage;
            SetRadius(fill, 4);
            meter.Add(fill);

            var state = new Label(text);
            state.style.width = 52;
            state.style.fontSize = 11;
            state.style.color = theme.Sage;
            state.style.marginLeft = 8;
            row.Add(state);
            return row;
        }

        public static Label SummaryValueRow(VisualElement parent, string title, string value, CampUiTheme theme)
        {
            var row = new VisualElement();
            row.style.height = 34;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            SetBottomBorder(row);
            parent.Add(row);

            var name = new Label(title);
            name.style.flexGrow = 1;
            name.style.color = theme.Ink;
            name.style.fontSize = 14;
            row.Add(name);

            var label = new Label(value);
            label.style.width = 90;
            label.style.color = theme.Ink;
            label.style.fontSize = 15;
            label.style.unityTextAlign = TextAnchor.MiddleRight;
            label.style.unityFontStyleAndWeight = FontStyle.Bold;
            row.Add(label);
            return label;
        }

        public static void TrySetBackground(VisualElement element, Texture2D texture)
        {
            if (texture == null) return;
            element.style.backgroundImage = new StyleBackground(texture);
        }

        public static void SetBorder(VisualElement element, Color color, float width)
        {
            element.style.borderTopWidth = width;
            element.style.borderBottomWidth = width;
            element.style.borderLeftWidth = width;
            element.style.borderRightWidth = width;
            element.style.borderTopColor = color;
            element.style.borderBottomColor = color;
            element.style.borderLeftColor = color;
            element.style.borderRightColor = color;
        }

        public static void SetBottomBorder(VisualElement element)
        {
            element.style.borderBottomWidth = 1;
            element.style.borderBottomColor = new Color(0.43f, 0.33f, 0.22f, 0.16f);
        }

        public static void SetRadius(VisualElement element, float radius)
        {
            element.style.borderTopLeftRadius = radius;
            element.style.borderTopRightRadius = radius;
            element.style.borderBottomLeftRadius = radius;
            element.style.borderBottomRightRadius = radius;
        }
    }
}
