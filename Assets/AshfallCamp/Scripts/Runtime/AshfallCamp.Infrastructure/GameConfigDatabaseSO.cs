using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    [CreateAssetMenu(menuName = "Ashfall Camp/Game Config Database")]
    public sealed class GameConfigDatabaseSO : ScriptableObject
    {
        public ResourceCatalogSO Resources;
        public SurvivorCatalogSO Survivors;
        public BackgroundCatalogSO Backgrounds;
        public TraitCatalogSO Traits;
        public ExpeditionPolicyCatalogSO Policies;
        public ZoneCatalogSO Zones;
        public EnemyCatalogSO Enemies;
        public ItemCatalogSO Items;
        public WeaponCatalogSO Weapons;
        public ArmorCatalogSO Armor;
        public UtilityCatalogSO Utilities;
        public BuildingCatalogSO Buildings;
        public BalanceConfigSO Balance;
    }
}
