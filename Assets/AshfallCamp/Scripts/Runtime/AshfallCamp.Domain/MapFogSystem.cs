using System;
using System.Collections.Generic;

namespace AshfallCamp.Domain
{
    public static class MapFogSystem
    {
        public static ValidationResult Initialize(GameState state, BalanceDefinition balance, MapFogTopology topology)
        {
            var validation = ValidateTopology(topology);
            if (!validation.IsValid || state == null || balance == null)
            {
                if (state == null) validation.Errors.Add("Game state is missing.");
                if (balance == null) validation.Errors.Add("Map fog balance is missing.");
                return validation;
            }

            if (state.MapFogInitialized)
            {
                return validation;
            }

            if (state.RevealedMapCells == null)
            {
                state.RevealedMapCells = new List<MapCellCoordinate>();
            }
            else
            {
                state.RevealedMapCells.Clear();
            }

            var radius = Math.Max(0, balance.MapInitialRevealRadius);
            foreach (var cell in topology.RevealableCells)
            {
                if (ChebyshevDistance(cell, topology.Core) <= radius)
                {
                    AddRevealed(state, cell);
                }
            }

            state.MapFogInitialized = true;
            return validation;
        }

        public static RevealMapCellResult TryReveal(GameState state, BalanceDefinition balance, MapFogTopology topology, MapCellCoordinate cell)
        {
            var result = new RevealMapCellResult { Cell = cell };
            var topologyValidation = ValidateTopology(topology);
            if (!topologyValidation.IsValid)
            {
                result.Validation.Errors.AddRange(topologyValidation.Errors);
                return result;
            }

            if (state == null || balance == null)
            {
                result.Validation.Errors.Add(state == null ? "Game state is missing." : "Map fog balance is missing.");
                return result;
            }

            if (!state.MapFogInitialized)
            {
                result.Validation.Errors.Add("Map fog is not initialized.");
                return result;
            }

            if (!Contains(topology.RevealableCells, cell))
            {
                result.Validation.Errors.Add(Contains(topology.RadioactiveSeaCells, cell)
                    ? "Radioactive sea cannot be revealed for scrap."
                    : "Map cell is not revealable.");
                return result;
            }

            if (IsRevealed(state, cell))
            {
                result.Validation.Errors.Add("Map cell is already revealed.");
                return result;
            }

            if (GetVisibility(state, topology, cell) != MapFogVisibility.Frontier)
            {
                result.Validation.Errors.Add("Map cell is not on the current fog frontier.");
                return result;
            }

            result.Distance = ChebyshevDistance(cell, topology.Core);
            result.Cost = CalculateCost(balance, result.Distance);
            var cost = new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { balance.MapRevealResourceId, result.Cost }
            };
            if (!ResourceSystem.TrySpend(state, cost))
            {
                result.Validation.Errors.Add("Not enough resources.");
                return result;
            }

            AddRevealed(state, cell);
            return result;
        }

        public static MapFogVisibility GetVisibility(GameState state, MapFogTopology topology, MapCellCoordinate cell)
        {
            if (state == null || topology == null)
            {
                return MapFogVisibility.None;
            }

            if (Contains(topology.RadioactiveSeaCells, cell))
            {
                return HasRevealedNeighbour(state, cell) ? MapFogVisibility.Revealed : MapFogVisibility.Deep;
            }

            if (!Contains(topology.RevealableCells, cell))
            {
                return MapFogVisibility.None;
            }

            if (IsRevealed(state, cell))
            {
                return MapFogVisibility.Revealed;
            }

            return HasRevealedNeighbour(state, cell) ? MapFogVisibility.Frontier : MapFogVisibility.Deep;
        }

        public static int CalculateCost(BalanceDefinition balance, int distance)
        {
            if (balance == null || distance <= 0) return 0;
            var value = (long)Math.Max(0, balance.MapRevealBaseCost) * distance;
            return value > int.MaxValue ? int.MaxValue : (int)value;
        }

        public static int ChebyshevDistance(MapCellCoordinate a, MapCellCoordinate b)
        {
            return Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y));
        }

        public static bool IsRevealed(GameState state, MapCellCoordinate cell)
        {
            return state != null && Contains(state.RevealedMapCells, cell);
        }

        private static bool HasRevealedNeighbour(GameState state, MapCellCoordinate cell)
        {
            for (var y = -1; y <= 1; y++)
            {
                for (var x = -1; x <= 1; x++)
                {
                    if (x == 0 && y == 0) continue;
                    if (IsRevealed(state, new MapCellCoordinate(cell.X + x, cell.Y + y))) return true;
                }
            }

            return false;
        }

        private static void AddRevealed(GameState state, MapCellCoordinate cell)
        {
            if (!IsRevealed(state, cell)) state.RevealedMapCells.Add(cell);
        }

        private static ValidationResult ValidateTopology(MapFogTopology topology)
        {
            var validation = new ValidationResult();
            if (topology == null)
            {
                validation.Errors.Add("Map fog topology is missing.");
                return validation;
            }

            if (topology.RevealableCells == null || topology.RevealableCells.Count == 0)
            {
                validation.Errors.Add("Map fog topology has no revealable cells.");
            }
            else if (!Contains(topology.RevealableCells, topology.Core))
            {
                validation.Errors.Add("Camp Core is not a revealable map cell.");
            }

            return validation;
        }

        private static bool Contains(IReadOnlyList<MapCellCoordinate> cells, MapCellCoordinate cell)
        {
            if (cells == null) return false;
            for (var index = 0; index < cells.Count; index++)
            {
                if (cells[index].Equals(cell)) return true;
            }

            return false;
        }
    }
}
