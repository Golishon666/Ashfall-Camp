using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/World Tile Catalog")]
    public sealed class WorldTileCatalogSO : ScriptableObject
    {
        public List<WorldTileConfigSO> Tiles = new List<WorldTileConfigSO>();

        public bool TryGetByTile(TileBase tile, out WorldTileConfigSO config)
        {
            config = null;
            if (tile == null) return false;
            foreach (var entry in Tiles)
            {
                if (entry != null && entry.ContentTile == tile)
                {
                    config = entry;
                    return true;
                }
            }
            return false;
        }
    }
}
