using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Zone Catalog")]
    public sealed class ZoneCatalogSO : ScriptableObject
    {
        public List<ZoneConfigData> Zones = new List<ZoneConfigData>();
    }
}
