using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Enemy Catalog")]
    public sealed class EnemyCatalogSO : ScriptableObject
    {
        public List<EnemyConfigData> Enemies = new List<EnemyConfigData>();
    }
}
