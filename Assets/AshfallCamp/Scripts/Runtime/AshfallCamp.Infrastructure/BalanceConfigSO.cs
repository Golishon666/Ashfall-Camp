using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Balance")]
    public sealed class BalanceConfigSO : ScriptableObject
    {
        public BalanceConfigData Balance = new BalanceConfigData();
    }
}
