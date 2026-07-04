using System;
using System.Collections.Generic;
using AshfallCamp.Domain;

namespace AshfallCamp.Infrastructure
{
    [Serializable]
    public sealed class ResourceConfigData
    {
        public string Id;
        public string Name;
        public bool HasCap;
        public int StartAmount;
        public int StartCap;
    }

    [Serializable]
    public sealed class StartingSurvivorConfigData
    {
        public string Name = "Mara";
        public string BackgroundId = "scavenger";
        public List<string> TraitIds = new List<string>();
        public string WeaponItemId = "rusty_knife";
        public List<IntPairData> Skills = new List<IntPairData>();
    }

    [Serializable]
    public sealed class RecruitableSurvivorConfigData
    {
        public string Id;
        public string Name;
        public string BackgroundId;
        public List<string> TraitIds = new List<string>();
        public string WeaponItemId;
        public List<IntPairData> Skills = new List<IntPairData>();
    }

    [Serializable]
    public sealed class BackgroundConfigData
    {
        public string Id;
        public string Name;
        public List<IntPairData> SkillBonuses = new List<IntPairData>();
        public List<IntPairData> StatBonuses = new List<IntPairData>();
    }

    [Serializable]
    public sealed class TraitConfigData
    {
        public string Id;
        public string Name;
        public List<IntPairData> StatModifiers = new List<IntPairData>();
    }

    [Serializable]
    public sealed class ExpeditionPolicyConfigData
    {
        public string Id = "balanced";
        public string Name = "Balanced";
        public double RiskModifier = 1.0;
        public double LootModifier = 1.0;
        public double DurationModifier = 1.0;
        public double FoodModifier = 1.0;
        public double WaterModifier = 1.0;
        public double PowerModifier = 1.0;
        public int NoiseModifier;
    }

    [Serializable]
    public sealed class ZoneConfigData
    {
        public string Id;
        public string Name;
        public RiskTier RiskTier;
        public double BaseDurationSeconds;
        public double MinDurationSeconds;
        public double MaxDurationSeconds;
        public int FoodCostPerSurvivor;
        public int WaterCostPerSurvivor;
        public int RecommendedPower;
        public double BaseAmbushChance;
        public List<IntPairData> RequiredBuildingLevels = new List<IntPairData>();
        public List<WeightedEntryData> EnemyTable = new List<WeightedEntryData>();
        public List<LootTableEntryData> LootTable = new List<LootTableEntryData>();
        public List<WeightedEntryData> EventTable = new List<WeightedEntryData>();
        public List<UnlockConditionData> UnlockConditions = new List<UnlockConditionData>();
    }

    [Serializable]
    public sealed class EnemyConfigData
    {
        public string Id;
        public string Name;
        public int MaxHealth;
        public int Armor;
        public double Evasion;
        public int BaseDamage;
        public WeaponType AttackType = WeaponType.Melee;
        public double Accuracy = 0.75;
        public double AttackIntervalSeconds = 2;
        public int XpReward;
    }

    [Serializable]
    public sealed class ItemConfigData
    {
        public string Id;
        public string Name;
        public ItemSlot Slot;
        public WeaponType WeaponType;
        public int BaseDamage;
        public int Armor;
        public double AccuracyBonus;
        public double CritBonus;
        public int NoisePerAttack;
        public int MaxDurability;
        public double RepairCostMultiplier = 1.0;
        public int CarryCapacityBonus;
    }

    [Serializable]
    public sealed class BuildingConfigData
    {
        public string Id;
        public string Name;
        public int StartingLevel;
        public bool StartsUnlocked;
        public string AffectedResourceId;
        public string ProducedResourceId;
        public List<BuildingLevelConfigData> Levels = new List<BuildingLevelConfigData>();
    }

    [Serializable]
    public sealed class BuildingLevelConfigData
    {
        public int Level;
        public List<IntPairData> Cost = new List<IntPairData>();
        public int SurvivorCap;
        public int SquadSize;
        public int ResourceCap;
        public int ResourcePerMinute;
    }

    [Serializable]
    public sealed class BalanceConfigData
    {
        public int MaxOfflineSeconds = 43200;
        public double SimulationTickSeconds = 1;
        public double CombatTickSeconds = 2;
        public double ExpeditionStepSeconds = 10;
        public double AutosaveSeconds = 30;
        public double OfflineReportMinimumSeconds = 60;
        public double ActiveCommandCooldownSeconds = 30;
        public double BaseAccuracyMelee = 0.78;
        public double BaseAccuracyFirearms = 0.65;
        public double MinHitChance = 0.15;
        public double MaxHitChance = 0.95;
        public double BaseCritChance = 0.05;
        public double CritMultiplier = 1.75;
        public string RecruitmentRequiredBuildingId = "radio_tower";
        public int RecruitmentRequiredBuildingLevel;
        public string RecruitmentScrapResourceId = "scrap";
        public string RecruitmentFoodResourceId = "food";
        public string RecruitmentWaterResourceId = "water";
        public string ExpeditionFoodResourceId = "food";
        public string ExpeditionWaterResourceId = "water";
        public int RecruitmentBaseScrap = 20;
        public double RecruitmentScrapExponent = 1.25;
        public int RecruitmentBaseFood = 2;
        public int RecruitmentFoodDivisor = 2;
        public int RecruitmentBaseWater = 2;
        public int RecruitmentWaterDivisor = 3;
        public string WorkshopRequiredBuildingId = "workshop";
        public int WorkshopRequiredBuildingLevel = 1;
        public string WorkshopRepairResourceId = "weapon_parts";
        public int WorkshopRepairDurabilityBlock = 10;
        public string HealingRequiredBuildingId = "infirmary";
        public int HealingRequiredBuildingLevel = 1;
        public string HealingDefaultWoundId = "cuts";
        public double HealingDefaultWoundDurationSeconds = 300;
        public int HealingHealthOnWounded = 1;
    }

    [Serializable]
    public sealed class IntPairData
    {
        public string Id;
        public int Value;

        public IntPairData()
        {
        }

        public IntPairData(string id, int value)
        {
            Id = id;
            Value = value;
        }
    }

    [Serializable]
    public sealed class WeightedEntryData
    {
        public string Id;
        public int Weight;

        public WeightedEntryData()
        {
        }

        public WeightedEntryData(string id, int weight)
        {
            Id = id;
            Weight = weight;
        }
    }

    [Serializable]
    public sealed class LootTableEntryData
    {
        public string ResourceId;
        public int Min;
        public int Max;
        public int Weight;

        public LootTableEntryData()
        {
        }

        public LootTableEntryData(string resourceId, int min, int max, int weight)
        {
            ResourceId = resourceId;
            Min = min;
            Max = max;
            Weight = weight;
        }
    }

    [Serializable]
    public sealed class UnlockConditionData
    {
        public string Type;
        public string Id;
        public int Value;

        public UnlockConditionData()
        {
        }

        public UnlockConditionData(string type, string id, int value)
        {
            Type = type;
            Id = id;
            Value = value;
        }
    }
}
