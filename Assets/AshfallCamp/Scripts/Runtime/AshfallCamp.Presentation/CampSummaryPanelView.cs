using System;
using AshfallCamp.Domain;
using TMPro;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    [DisallowMultipleComponent]
    public sealed class CampSummaryPanelView : MonoBehaviour
    {
        [SerializeField] private TextMeshProUGUI populationValue;
        [SerializeField] private TextMeshProUGUI idleSurvivorsValue;
        [SerializeField] private TextMeshProUGUI buildingsValue;
        [SerializeField] private TextMeshProUGUI productionValue;
        [SerializeField] private TextMeshProUGUI populationLabel;
        [SerializeField] private TextMeshProUGUI idleSurvivorsLabel;
        [SerializeField] private TextMeshProUGUI buildingsLabel;
        [SerializeField] private TextMeshProUGUI productionLabel;

        public void ConfigureBindings(
            TextMeshProUGUI populationValueText,
            TextMeshProUGUI idleSurvivorsValueText,
            TextMeshProUGUI buildingsValueText,
            TextMeshProUGUI productionValueText,
            TextMeshProUGUI populationLabelText,
            TextMeshProUGUI idleSurvivorsLabelText,
            TextMeshProUGUI buildingsLabelText,
            TextMeshProUGUI productionLabelText)
        {
            populationValue = populationValueText;
            idleSurvivorsValue = idleSurvivorsValueText;
            buildingsValue = buildingsValueText;
            productionValue = productionValueText;
            populationLabel = populationLabelText;
            idleSurvivorsLabel = idleSurvivorsLabelText;
            buildingsLabel = buildingsLabelText;
            productionLabel = productionLabelText;
        }

        public void Render(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return;

            UiText.Set(populationLabel, catalog.PopulationLabel);
            UiText.Set(idleSurvivorsLabel, catalog.IdleSurvivorsLabel);
            UiText.Set(buildingsLabel, catalog.BuildingsLabel);
            UiText.Set(productionLabel, catalog.ProductionMetricLabel);

            UiText.Set(populationValue, state.Survivors.Count + " / " + Math.Max(1, state.SurvivorCap));
            UiText.Set(idleSurvivorsValue, UiStateQueries.CountIdleSurvivors(state).ToString());
            UiText.Set(buildingsValue, UiStateQueries.CountUnlockedBuildings(state) + " / " + state.Buildings.Count);

            var productionPerHour = string.IsNullOrWhiteSpace(catalog.ProductionMetricResourceId)
                ? 0
                : UiStateQueries.CalculateProductionPerHour(state, config, catalog.ProductionMetricResourceId);
            UiText.Set(productionValue, "+" + productionPerHour + catalog.PerHourSuffixLabel);
        }
    }
}
