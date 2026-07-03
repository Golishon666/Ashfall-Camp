using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Expedition Policy Catalog")]
    public sealed class ExpeditionPolicyCatalogSO : ScriptableObject
    {
        public List<ExpeditionPolicyConfigData> Policies = new List<ExpeditionPolicyConfigData>();
    }
}
