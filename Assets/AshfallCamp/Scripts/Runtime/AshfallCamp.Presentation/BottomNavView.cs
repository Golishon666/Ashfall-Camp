using UnityEngine;
using UnityEngine.UIElements;

namespace AshfallCamp.Presentation
{
    internal sealed class BottomNavView
    {
        private readonly CampUiCatalogSO _catalog;

        public BottomNavView(CampUiCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public void Build(VisualElement parent)
        {
            var theme = _catalog.Theme;
            var nav = new VisualElement();
            nav.style.height = 104;
            nav.style.flexDirection = FlexDirection.Row;
            nav.style.alignItems = Align.Center;
            nav.style.justifyContent = Justify.Center;
            nav.style.marginTop = 16;
            nav.style.paddingLeft = 40;
            nav.style.paddingRight = 40;
            nav.style.backgroundColor = new Color(0.23f, 0.16f, 0.11f, 0.12f);
            UiStyle.TrySetBackground(nav, _catalog.BottomNavPlate);
            parent.Add(nav);

            foreach (var item in _catalog.NavItems)
            {
                AddNavItem(nav, item, theme);
            }
        }

        private static void AddNavItem(VisualElement parent, NavUiEntry item, CampUiTheme theme)
        {
            var button = new Label(item.Label);
            button.style.flexGrow = 1;
            button.style.height = 62;
            button.style.marginLeft = 8;
            button.style.marginRight = 8;
            button.style.backgroundColor = item.IsActive ? theme.Teal : new Color(theme.Paper.r, theme.Paper.g, theme.Paper.b, 0.44f);
            button.style.color = item.IsActive ? theme.Paper : theme.Ink;
            button.style.fontSize = 20;
            button.style.unityTextAlign = TextAnchor.MiddleCenter;
            button.style.unityFontStyleAndWeight = FontStyle.Bold;
            UiStyle.SetBorder(button, theme.Line, 1);
            UiStyle.SetRadius(button, 5);
            parent.Add(button);
        }
    }
}
