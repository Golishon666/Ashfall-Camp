using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Survivor Catalog")]
    public sealed class SurvivorCatalogSO : ScriptableObject
    {
        public StartingSurvivorConfigData StartingSurvivor = new StartingSurvivorConfigData();
    }
}
