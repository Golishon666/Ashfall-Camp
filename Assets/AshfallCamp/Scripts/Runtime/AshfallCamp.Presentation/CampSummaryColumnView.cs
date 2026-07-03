using AshfallCamp.Domain;
using UnityEngine;
using UnityEngine.UIElements;

namespace AshfallCamp.Presentation
{
    internal sealed class CampSummaryColumnView
    {
        private readonly CampUiCatalogSO _catalog;
        private Label _populationLabel;
        private Label _idleLabel;
        private Label _buildingLabel;
        private Label _productionMetricLabel;
        private Label _statusLabel;

        public CampSummaryColumnView(CampUiCatalogSO catalog)
        {
            _catalog = catalog;
        }

        public void Build(VisualElement main)
        {
            var theme = _catalog.Theme;
            var parent = new VisualElement();
            parent.style.width = 320;
            parent.style.marginRight = 18;
            main.Add(parent);

            var status = UiStyle.Panel(_catalog.CampStatusTitle, theme);
            status.style.height = 230;
            parent.Add(status);

            var statusRow = new VisualElement();
            statusRow.style.flexDirection = FlexDirection.Row;
            statusRow.style.alignItems = Align.Center;
            statusRow.style.marginTop = 8;
            status.Add(statusRow);

            var badge = new Label(_catalog.CampStatusBadgeLabel);
            badge.style.width = 62;
            badge.style.height = 62;
            badge.style.backgroundColor = theme.Sage;
            badge.style.color = theme.Paper;
            badge.style.unityTextAlign = TextAnchor.MiddleCenter;
            badge.style.unityFontStyleAndWeight = FontStyle.Bold;
            UiStyle.SetRadius(badge, 31);
            statusRow.Add(badge);

            _statusLabel = new Label(_catalog.CampStatusHealthyLabel);
            _statusLabel.style.fontSize = 22;
            _statusLabel.style.color = theme.Sage;
            _statusLabel.style.unityFontStyleAndWeight = FontStyle.Bold;
            _statusLabel.style.marginLeft = 14;
            statusRow.Add(_statusLabel);

            status.Add(UiStyle.SmallText(_catalog.CampStatusBody, theme));
            status.Add(UiStyle.MeterRow(_catalog.MoraleLabel, 0.72f, _catalog.MoraleValueLabel, theme));
            status.Add(UiStyle.MeterRow(_catalog.SafetyLabel, 0.66f, _catalog.SafetyValueLabel, theme));
            status.Add(UiStyle.MeterRow(_catalog.SuppliesLabel, 0.58f, _catalog.SuppliesValueLabel, theme));

            var summary = UiStyle.Panel(_catalog.CampSummaryTitle, theme);
            summary.style.height = 246;
            summary.style.marginTop = 14;
            parent.Add(summary);
            _populationLabel = UiStyle.SummaryValueRow(summary, _catalog.PopulationLabel, "-", theme);
            _idleLabel = UiStyle.SummaryValueRow(summary, _catalog.IdleSurvivorsLabel, "-", theme);
            _buildingLabel = UiStyle.SummaryValueRow(summary, _catalog.BuildingsLabel, "-", theme);
            _productionMetricLabel = UiStyle.SummaryValueRow(summary, _catalog.ProductionMetricLabel, "-", theme);

            var note = new Label(_catalog.CampSummaryNote);
            note.style.marginTop = 18;
            note.style.paddingLeft = 16;
            note.style.paddingRight = 16;
            note.style.paddingTop = 14;
            note.style.paddingBottom = 14;
            note.style.backgroundColor = new Color(0.86f, 0.78f, 0.62f, 0.28f);
            note.style.color = theme.MutedInk;
            note.style.whiteSpace = WhiteSpace.Normal;
            note.style.unityTextAlign = TextAnchor.MiddleCenter;
            UiStyle.SetRadius(note, 2);
            summary.Add(note);

            var alerts = UiStyle.Panel(_catalog.RecentAlertsTitle, theme);
            alerts.style.flexGrow = 1;
            alerts.style.marginTop = 14;
            parent.Add(alerts);

            foreach (var alert in _catalog.Alerts)
            {
                AddAlert(alerts, alert);
            }
        }

        public void Render(GameState state, GameConfigSnapshot config)
        {
            _populationLabel.text = state.Survivors.Count + " / " + Mathf.Max(1, state.SurvivorCap);
            _idleLabel.text = UiStateQueries.CountIdleSurvivors(state).ToString();
            _buildingLabel.text = UiStateQueries.CountUnlockedBuildings(state) + " / " + Mathf.Max(1, state.Buildings.Count);
            _productionMetricLabel.text = "+" + UiStateQueries.CalculateProductionPerHour(state, config, _catalog.ProductionMetricResourceId) + _catalog.PerHourSuffixLabel;

            var trackedAmount = string.IsNullOrWhiteSpace(_catalog.StatusResourceId) || !state.Resources.ContainsKey(_catalog.StatusResourceId)
                ? _catalog.StatusStrainedBelowAmount + 1
                : state.Resources[_catalog.StatusResourceId];
            var isHealthy = trackedAmount > _catalog.StatusStrainedBelowAmount;
            _statusLabel.text = isHealthy ? _catalog.CampStatusHealthyLabel : _catalog.CampStatusStrainedLabel;
            _statusLabel.style.color = isHealthy ? _catalog.Theme.Sage : _catalog.Theme.Rust;
        }

        private void AddAlert(VisualElement parent, AlertUiEntry alert)
        {
            var theme = _catalog.Theme;
            var row = new VisualElement();
            row.style.minHeight = 66;
            row.style.flexDirection = FlexDirection.Row;
            row.style.alignItems = Align.Center;
            row.style.marginTop = 8;
            row.style.paddingLeft = 8;
            row.style.paddingRight = 8;
            row.style.backgroundColor = new Color(alert.ToneColor.r, alert.ToneColor.g, alert.ToneColor.b, 0.08f);
            UiStyle.SetBorder(row, new Color(alert.ToneColor.r, alert.ToneColor.g, alert.ToneColor.b, 0.22f), 1);
            UiStyle.SetRadius(row, 4);
            parent.Add(row);

            var dot = new Label("!");
            dot.style.width = 34;
            dot.style.height = 34;
            dot.style.backgroundColor = alert.ToneColor;
            dot.style.color = theme.Paper;
            dot.style.unityTextAlign = TextAnchor.MiddleCenter;
            dot.style.unityFontStyleAndWeight = FontStyle.Bold;
            dot.style.marginRight = 10;
            UiStyle.SetRadius(dot, 17);
            row.Add(dot);

            var stack = new VisualElement();
            stack.style.flexGrow = 1;
            row.Add(stack);

            var heading = new Label(alert.Title);
            heading.style.color = alert.ToneColor;
            heading.style.fontSize = 13;
            heading.style.unityFontStyleAndWeight = FontStyle.Bold;
            stack.Add(heading);

            var body = new Label(alert.Body);
            body.style.color = theme.Ink;
            body.style.fontSize = 12;
            body.style.whiteSpace = WhiteSpace.Normal;
            stack.Add(body);
        }
    }
}
