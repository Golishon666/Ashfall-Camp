using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Configs/Weapon Catalog")]
    public sealed class WeaponCatalogSO : ScriptableObject
    {
        public List<WeaponConfigData> Weapons = new List<WeaponConfigData>();
    }
}
