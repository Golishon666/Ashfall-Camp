using AshfallCamp.Domain;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    internal static class BuildingEffectTextFormatter
    {
        public static string Format(CampUiCatalogSO catalog, BuildingDefinition definition, BuildingState building)
        {
            var level = BuildingSystem.GetLevel(definition, building.Level);
            if (level == null) return string.Empty;
            if (level.SurvivorCap > 0) return string.Format(catalog.SurvivorEffectFormat, level.SurvivorCap, Mathf.Max(1, level.SquadSize));
            if (!string.IsNullOrWhiteSpace(definition.AffectedResourceId) && level.ResourceCap > 0)
            {
                if (level.ResourcePerMinute > 0)
                {
                    return string.Format(catalog.ResourceCapEffectFormat, definition.AffectedResourceId, level.ResourceCap, level.ResourcePerMinute * 60);
                }

                return string.Format(catalog.ResourceCapOnlyEffectFormat, definition.AffectedResourceId, level.ResourceCap);
            }

            return catalog.RouteUnlockEffectLabel;
        }
    }
}
