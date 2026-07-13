using System;
using System.Collections.Generic;

namespace AshfallCamp.Domain
{
    public static class WorldTileTravelSystem
    {
        public static ValidationResult ValidateRoute(GameConfigSnapshot config, IReadOnlyList<string> routeTileIds)
        {
            var result = new ValidationResult();
            if (routeTileIds == null) return result;
            foreach (var tileId in routeTileIds)
            {
                WorldTileDefinition tile;
                if (!config.TryGetWorldTile(tileId, out tile))
                {
                    result.Errors.Add("Route contains an unknown world tile: " + tileId + ".");
                }
                else if (tile.Type == WorldTileType.Camp || tile.Type == WorldTileType.Impassable)
                {
                    result.Errors.Add("Route contains an impassable world tile: " + tileId + ".");
                }
            }
            return result;
        }

        public static Dictionary<string, int> CalculateRoundTripCost(GameConfigSnapshot config, IReadOnlyList<string> routeTileIds, int survivorCount)
        {
            var cost = new Dictionary<string, int>(StringComparer.Ordinal);
            if (config == null || routeTileIds == null || survivorCount <= 0) return cost;
            var food = 0;
            var water = 0;
            foreach (var tileId in routeTileIds)
            {
                WorldTileDefinition tile;
                if (!config.TryGetWorldTile(tileId, out tile)) continue;
                food += Math.Max(0, tile.FoodCostPerSurvivor);
                water += Math.Max(0, tile.WaterCostPerSurvivor);
            }
            Add(cost, config.Balance.ExpeditionFoodResourceId, food * survivorCount * 2);
            Add(cost, config.Balance.ExpeditionWaterResourceId, water * survivorCount * 2);
            return cost;
        }

        public static bool ResolveOutbound(GameState state, GameConfigSnapshot config, ExpeditionState expedition)
        {
            return ResolveLeg(state, config, expedition, expedition.RouteTileIds, "outbound");
        }

        public static bool ResolveReturn(GameState state, GameConfigSnapshot config, ExpeditionState expedition)
        {
            if (expedition.ReturnTravelApplied) return true;
            expedition.ReturnTravelApplied = true;
            var reversed = new List<string>(expedition.RouteTileIds);
            reversed.Reverse();
            return ResolveLeg(state, config, expedition, reversed, "return");
        }

        private static bool ResolveLeg(GameState state, GameConfigSnapshot config, ExpeditionState expedition, IReadOnlyList<string> route, string label)
        {
            if (route == null || route.Count == 0) return true;
            AddLog(expedition, label + " route begins through " + route.Count + " cells.");
            var rng = new SeededRandom(expedition.RandomState == 0 ? 1u : expedition.RandomState);
            foreach (var tileId in route)
            {
                WorldTileDefinition tile;
                if (!config.TryGetWorldTile(tileId, out tile)) continue;
                RollTransitReward(expedition, tile, ref rng);
                if (tile.EnemyTable.Count > 0 && rng.NextDouble() < GameMath.Clamp(tile.EncounterChance, 0, 1))
                {
                    var zone = new ZoneDefinition
                    {
                        Id = tile.Id,
                        Name = tile.Name,
                        RiskTier = StrengthToRisk(tile.Strength),
                        MinEnemyCount = 1,
                        MaxEnemyCount = Math.Max(1, Math.Min(4, 1 + tile.Strength / 4)),
                        DurabilityPressure = Math.Max(0, tile.Strength / 3)
                    };
                    foreach (var enemy in tile.EnemyTable) zone.EnemyTable.Add(new WeightedEntry { Id = enemy.Id, Weight = enemy.Weight });
                    expedition.RandomState = rng.State;
                    if (!CombatResolver.ResolveCombat(state, config, expedition, zone)) return false;
                    rng = new SeededRandom(expedition.RandomState);
                }
            }
            expedition.RandomState = rng.State;
            AddLog(expedition, label + " route completed.");
            return true;
        }

        private static void RollTransitReward(ExpeditionState expedition, WorldTileDefinition tile, ref SeededRandom rng)
        {
            foreach (var reward in tile.ResourceRewards)
            {
                if (reward.Weight <= 0 || rng.RangeInclusive(1, 100) > Math.Min(100, reward.Weight)) continue;
                var amount = rng.RangeInclusive(Math.Max(0, reward.Min), Math.Max(reward.Min, reward.Max));
                if (amount <= 0) continue;
                if (!expedition.AccumulatedLoot.ContainsKey(reward.ResourceId)) expedition.AccumulatedLoot[reward.ResourceId] = 0;
                expedition.AccumulatedLoot[reward.ResourceId] += amount;
            }
        }

        private static RiskTier StrengthToRisk(int strength)
        {
            if (strength >= 10) return RiskTier.DeadZone;
            if (strength >= 7) return RiskTier.Dangerous;
            if (strength >= 4) return RiskTier.Unstable;
            return RiskTier.Safe;
        }

        private static void Add(Dictionary<string, int> output, string resourceId, int amount)
        {
            if (!string.IsNullOrWhiteSpace(resourceId) && amount > 0) output[resourceId] = amount;
        }

        private static void AddLog(ExpeditionState expedition, string message)
        {
            expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = expedition.ElapsedSeconds, Message = message });
        }
    }
}
