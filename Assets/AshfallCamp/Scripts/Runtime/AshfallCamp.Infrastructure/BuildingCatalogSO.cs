using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Building Catalog")]
    public sealed class BuildingCatalogSO : ScriptableObject
    {
        public List<BuildingConfigData> Buildings = new List<BuildingConfigData>();
    }
}
