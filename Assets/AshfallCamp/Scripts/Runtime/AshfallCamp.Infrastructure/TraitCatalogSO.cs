using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Trait Catalog")]
    public sealed class TraitCatalogSO : ScriptableObject
    {
        public List<TraitConfigData> Traits = new List<TraitConfigData>();
    }
}
