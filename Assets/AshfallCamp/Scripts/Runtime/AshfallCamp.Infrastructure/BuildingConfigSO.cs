using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Building Config")]
    public sealed class BuildingConfigSO : ScriptableObject
    {
        public BuildingConfigData Building = new BuildingConfigData();
    }
}
