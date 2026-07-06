using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Utility Catalog")]
    public sealed class UtilityCatalogSO : ScriptableObject
    {
        public List<UtilityConfigData> Utilities = new List<UtilityConfigData>();
    }
}
