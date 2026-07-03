using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Resource Catalog")]
    public sealed class ResourceCatalogSO : ScriptableObject
    {
        public List<ResourceConfigData> Resources = new List<ResourceConfigData>();
    }
}
