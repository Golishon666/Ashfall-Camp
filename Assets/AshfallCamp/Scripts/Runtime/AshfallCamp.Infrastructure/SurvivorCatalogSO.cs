using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Survivor Catalog")]
    public sealed class SurvivorCatalogSO : ScriptableObject
    {
        public StartingSurvivorConfigData StartingSurvivor = new StartingSurvivorConfigData();
        public List<RecruitableSurvivorConfigData> RecruitableSurvivors = new List<RecruitableSurvivorConfigData>();
    }
}
