using System.Collections.Generic;
using AshfallCamp.Domain;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/World Tile")]
    public sealed class WorldTileConfigSO : ScriptableObject
    {
        public string Id;
        public string DisplayName;
        public string Category;
        public WorldTileType Type;
        [Range(1, 12)] public int Strength = 1;
        public string ZoneId;
        public int FoodCostPerSurvivor;
        public int WaterCostPerSurvivor;
        [Range(0, 1)] public double EncounterChance;
        [Range(0, 1)] public double RareEquipmentChance;
        public List<LootTableEntryData> ResourceRewards = new List<LootTableEntryData>();
        public List<WeightedEntryData> EnemyTable = new List<WeightedEntryData>();
        public List<WeightedEntryData> EquipmentTable = new List<WeightedEntryData>();
        public TileBase ContentTile;
        public TileBase MarkerTile;
        public Sprite Thumbnail;
    }
}
