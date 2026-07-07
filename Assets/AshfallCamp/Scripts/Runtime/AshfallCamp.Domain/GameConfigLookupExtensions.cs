using System;
using System.Collections.Generic;

namespace AshfallCamp.Domain
{
    public static class GameConfigLookupExtensions
    {
        public static bool TryGetResource(this GameConfigSnapshot config, string id, out ResourceDefinition definition)
        {
            return TryGet(config != null ? config.Resources : null, id, out definition);
        }

        public static ResourceDefinition RequireResource(this GameConfigSnapshot config, string id)
        {
            return Require("Resource", config != null ? config.Resources : null, id);
        }

        public static bool TryGetBackground(this GameConfigSnapshot config, string id, out BackgroundDefinition definition)
        {
            return TryGet(config != null ? config.Backgrounds : null, id, out definition);
        }

        public static BackgroundDefinition RequireBackground(this GameConfigSnapshot config, string id)
        {
            return Require("Background", config != null ? config.Backgrounds : null, id);
        }

        public static bool TryGetTrait(this GameConfigSnapshot config, string id, out TraitDefinition definition)
        {
            return TryGet(config != null ? config.Traits : null, id, out definition);
        }

        public static TraitDefinition RequireTrait(this GameConfigSnapshot config, string id)
        {
            return Require("Trait", config != null ? config.Traits : null, id);
        }

        public static bool TryGetPolicy(this GameConfigSnapshot config, string id, out ExpeditionPolicyDefinition definition)
        {
            return TryGet(config != null ? config.Policies : null, id, out definition);
        }

        public static ExpeditionPolicyDefinition RequirePolicy(this GameConfigSnapshot config, string id)
        {
            return Require("Policy", config != null ? config.Policies : null, id);
        }

        public static bool TryGetZone(this GameConfigSnapshot config, string id, out ZoneDefinition definition)
        {
            return TryGet(config != null ? config.Zones : null, id, out definition);
        }

        public static ZoneDefinition RequireZone(this GameConfigSnapshot config, string id)
        {
            return Require("Zone", config != null ? config.Zones : null, id);
        }

        public static bool TryGetEnemy(this GameConfigSnapshot config, string id, out EnemyDefinition definition)
        {
            return TryGet(config != null ? config.Enemies : null, id, out definition);
        }

        public static EnemyDefinition RequireEnemy(this GameConfigSnapshot config, string id)
        {
            return Require("Enemy", config != null ? config.Enemies : null, id);
        }

        public static bool TryGetItem(this GameConfigSnapshot config, string id, out ItemDefinition definition)
        {
            return TryGet(config != null ? config.Items : null, id, out definition);
        }

        public static ItemDefinition RequireItem(this GameConfigSnapshot config, string id)
        {
            return Require("Item", config != null ? config.Items : null, id);
        }

        public static bool TryGetWeapon(this GameConfigSnapshot config, string id, out WeaponDefinition definition)
        {
            return TryGet(config != null ? config.Weapons : null, id, out definition);
        }

        public static WeaponDefinition RequireWeapon(this GameConfigSnapshot config, string id)
        {
            return Require("Weapon", config != null ? config.Weapons : null, id);
        }

        public static bool TryGetArmor(this GameConfigSnapshot config, string id, out ArmorDefinition definition)
        {
            return TryGet(config != null ? config.Armor : null, id, out definition);
        }

        public static ArmorDefinition RequireArmor(this GameConfigSnapshot config, string id)
        {
            return Require("Armor", config != null ? config.Armor : null, id);
        }

        public static bool TryGetUtility(this GameConfigSnapshot config, string id, out UtilityDefinition definition)
        {
            return TryGet(config != null ? config.Utilities : null, id, out definition);
        }

        public static UtilityDefinition RequireUtility(this GameConfigSnapshot config, string id)
        {
            return Require("Utility", config != null ? config.Utilities : null, id);
        }

        public static bool TryGetBuilding(this GameConfigSnapshot config, string id, out BuildingDefinition definition)
        {
            return TryGet(config != null ? config.Buildings : null, id, out definition);
        }

        public static BuildingDefinition RequireBuilding(this GameConfigSnapshot config, string id)
        {
            return Require("Building", config != null ? config.Buildings : null, id);
        }

        public static bool TryGetRecruitableSurvivor(this GameConfigSnapshot config, string id, out RecruitableSurvivorDefinition definition)
        {
            return TryGet(config != null ? config.RecruitableSurvivors : null, id, out definition);
        }

        public static RecruitableSurvivorDefinition RequireRecruitableSurvivor(this GameConfigSnapshot config, string id)
        {
            return Require("Recruitable survivor", config != null ? config.RecruitableSurvivors : null, id);
        }

        private static bool TryGet<T>(Dictionary<string, T> items, string id, out T definition)
        {
            definition = default(T);
            return items != null && !string.IsNullOrWhiteSpace(id) && items.TryGetValue(id, out definition);
        }

        private static T Require<T>(string label, Dictionary<string, T> items, string id)
        {
            T definition;
            if (TryGet(items, id, out definition))
            {
                return definition;
            }

            throw new InvalidOperationException(label + " config is missing: " + (id ?? "<null>"));
        }
    }
}
