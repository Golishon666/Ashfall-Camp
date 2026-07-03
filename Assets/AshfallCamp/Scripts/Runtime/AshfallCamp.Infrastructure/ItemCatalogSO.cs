using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Item Catalog")]
    public sealed class ItemCatalogSO : ScriptableObject
    {
        public List<ItemConfigData> Items = new List<ItemConfigData>();
    }
}
