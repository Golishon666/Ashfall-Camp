using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Armor Catalog")]
    public sealed class ArmorCatalogSO : ScriptableObject
    {
        public List<ArmorConfigData> Armor = new List<ArmorConfigData>();
    }
}
