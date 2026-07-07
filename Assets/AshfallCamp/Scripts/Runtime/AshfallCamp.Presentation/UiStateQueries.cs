using AshfallCamp.Domain;

namespace AshfallCamp.Presentation
{
    internal static class UiStateQueries
    {
        public static int CountIdleSurvivors(GameState state)
        {
            var count = 0;
            foreach (var survivor in state.Survivors)
            {
                if (survivor.State == SurvivorActivityState.Idle) count++;
            }
            return count;
        }

        public static int CountUnlockedBuildings(GameState state)
        {
            var count = 0;
            foreach (var building in state.Buildings.Values)
            {
                if (building.IsUnlocked) count++;
            }
            return count;
        }

        public static int CalculateProductionPerHour(GameState state, GameConfigSnapshot config, string resourceId)
        {
            var perMinute = 0;
            foreach (var building in state.Buildings.Values)
            {
                if (!building.IsUnlocked) continue;
                BuildingDefinition definition;
                if (!config.TryGetBuilding(building.Id, out definition)) continue;
                if (definition.ProducedResourceId != resourceId) continue;
                var level = BuildingSystem.GetLevel(definition, building.Level);
                if (level != null) perMinute += level.ResourcePerMinute;
            }
            return perMinute * 60;
        }
    }
}
