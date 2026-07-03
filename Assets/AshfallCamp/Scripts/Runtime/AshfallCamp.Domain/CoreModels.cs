using System;
using System.Collections.Generic;

namespace AshfallCamp.Domain
{
    public static class GameConstants
    {
        public const string CurrentSaveVersion = "0.1.0";
    }

    public enum SurvivorActivityState
    {
        Idle,
        OnExpedition,
        Resting,
        Wounded,
        Missing
    }

    public enum ExpeditionStatus
    {
        Active,
        Returning,
        Completed,
        Failed
    }

    public enum RiskTier
    {
        Safe,
        Unstable,
        Dangerous,
        DeadZone
    }

    public enum ItemSlot
    {
        Weapon,
        Armor,
        Utility
    }

    public enum WeaponType
    {
        None,
        Melee,
        Firearm,
        Mutant
    }

    public sealed class GameState
    {
        public string Version = GameConstants.CurrentSaveVersion;
        public long CreatedAtUnixMs;
        public long LastSaveAtUnixMs;
        public double TotalPlayTimeSeconds;
        public int NextId = 1;
        public int SurvivorCap = 1;
        public int SquadSize = 1;
        public Dictionary<string, int> Resources = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> ResourceCaps = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, double> ResourceProductionRemainders = new Dictionary<string, double>(StringComparer.Ordinal);
        public List<SurvivorState> Survivors = new List<SurvivorState>();
        public List<InventoryItemState> Inventory = new List<InventoryItemState>();
        public Dictionary<string, BuildingState> Buildings = new Dictionary<string, BuildingState>(StringComparer.Ordinal);
        public Dictionary<string, ZoneState> Zones = new Dictionary<string, ZoneState>(StringComparer.Ordinal);
        public Dictionary<string, UpgradeState> Upgrades = new Dictionary<string, UpgradeState>(StringComparer.Ordinal);
        public List<ExpeditionState> Expeditions = new List<ExpeditionState>();
        public GameSettings Settings = new GameSettings();
        public GameStatistics Statistics = new GameStatistics();
    }

    public sealed class GameSettings
    {
        public bool AutosaveEnabled = true;
    }

    public sealed class GameStatistics
    {
        public int ExpeditionsCompleted;
        public int ExpeditionsFailed;
        public int CombatsWon;
        public int CombatsLost;
        public Dictionary<string, int> TotalResourcesGained = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class SurvivorState
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public int Level = 1;
        public int Xp;
        public string BackgroundId = string.Empty;
        public List<string> TraitIds = new List<string>();
        public SurvivorActivityState State = SurvivorActivityState.Idle;
        public int Health = 30;
        public int MaxHealth = 30;
        public int Fatigue;
        public int Morale = 50;
        public Dictionary<string, int> Skills = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> SkillXp = new Dictionary<string, int>(StringComparer.Ordinal);
        public SurvivorEquipmentState Equipment = new SurvivorEquipmentState();
        public List<StatusEffectState> StatusEffects = new List<StatusEffectState>();
        public string CurrentExpeditionId = string.Empty;
    }

    public sealed class SurvivorEquipmentState
    {
        public string WeaponItemUid = string.Empty;
        public string ArmorItemUid = string.Empty;
        public string UtilityItemUid = string.Empty;
    }

    public sealed class StatusEffectState
    {
        public string Id = string.Empty;
        public double RemainingSeconds;
    }

    public sealed class InventoryItemState
    {
        public string Uid = string.Empty;
        public string ItemId = string.Empty;
        public int Level = 1;
        public int Durability;
        public int MaxDurability;
        public string EquippedBySurvivorId = string.Empty;
    }

    public sealed class BuildingState
    {
        public string Id = string.Empty;
        public int Level;
        public bool IsUnlocked;
        public long UpgradeStartedAtUnixMs;
        public long UpgradeFinishedAtUnixMs;
    }

    public sealed class ZoneState
    {
        public string Id = string.Empty;
        public bool IsUnlocked;
        public int Completions;
        public double Familiarity;
        public double BestClearTimeSeconds;
    }

    public sealed class UpgradeState
    {
        public string Id = string.Empty;
        public bool IsUnlocked;
        public bool IsPurchased;
    }

    public sealed class ExpeditionState
    {
        public string Id = string.Empty;
        public string ZoneId = string.Empty;
        public List<string> SurvivorIds = new List<string>();
        public string PolicyId = "balanced";
        public long StartedAtUnixMs;
        public double ExpectedDurationSeconds;
        public double ElapsedSeconds;
        public double Progress;
        public double StepAccumulatorSeconds;
        public ExpeditionStatus Status = ExpeditionStatus.Active;
        public uint RandomState;
        public int Noise;
        public List<ExpeditionLogEntry> Log = new List<ExpeditionLogEntry>();
        public Dictionary<string, int> AccumulatedLoot = new Dictionary<string, int>(StringComparer.Ordinal);
        public List<InventoryItemState> FoundItems = new List<InventoryItemState>();
        public Dictionary<string, int> EnemiesDefeated = new Dictionary<string, int>(StringComparer.Ordinal);
        public List<string> WoundedSurvivorIds = new List<string>();
    }

    public sealed class ExpeditionLogEntry
    {
        public double AtSeconds;
        public string Message = string.Empty;
    }

    public sealed class ValidationResult
    {
        public bool IsValid { get { return Errors.Count == 0; } }
        public List<string> Errors = new List<string>();
        public List<string> Warnings = new List<string>();
    }

    public sealed class LaunchExpeditionRequest
    {
        public string ZoneId = string.Empty;
        public List<string> SurvivorIds = new List<string>();
        public string PolicyId = "balanced";
        public uint Seed;
        public long NowUnixMs;
        public bool ConfirmWarnings;
    }

    public sealed class LaunchExpeditionResult
    {
        public ValidationResult Validation = new ValidationResult();
        public ExpeditionState Expedition;
    }

    public sealed class BuildingUpgradeResult
    {
        public ValidationResult Validation = new ValidationResult();
        public BuildingState Building;
    }

    public sealed class OfflineProgressReport
    {
        public double AppliedSeconds;
        public Dictionary<string, int> ResourcesGained = new Dictionary<string, int>(StringComparer.Ordinal);
        public List<string> CompletedExpeditionIds = new List<string>();
        public List<string> WoundedSurvivorIds = new List<string>();
    }

    public sealed class GameConfigSnapshot
    {
        public Dictionary<string, ResourceDefinition> Resources = new Dictionary<string, ResourceDefinition>(StringComparer.Ordinal);
        public Dictionary<string, BackgroundDefinition> Backgrounds = new Dictionary<string, BackgroundDefinition>(StringComparer.Ordinal);
        public Dictionary<string, TraitDefinition> Traits = new Dictionary<string, TraitDefinition>(StringComparer.Ordinal);
        public Dictionary<string, ExpeditionPolicyDefinition> Policies = new Dictionary<string, ExpeditionPolicyDefinition>(StringComparer.Ordinal);
        public Dictionary<string, ZoneDefinition> Zones = new Dictionary<string, ZoneDefinition>(StringComparer.Ordinal);
        public Dictionary<string, EnemyDefinition> Enemies = new Dictionary<string, EnemyDefinition>(StringComparer.Ordinal);
        public Dictionary<string, ItemDefinition> Items = new Dictionary<string, ItemDefinition>(StringComparer.Ordinal);
        public Dictionary<string, BuildingDefinition> Buildings = new Dictionary<string, BuildingDefinition>(StringComparer.Ordinal);
        public BalanceDefinition Balance = new BalanceDefinition();
        public StartingSurvivorDefinition StartingSurvivor = new StartingSurvivorDefinition();
    }

    public sealed class ResourceDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public bool HasCap;
        public int StartAmount;
        public int StartCap;
    }

    public sealed class BackgroundDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public Dictionary<string, int> SkillBonuses = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> StatBonuses = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class TraitDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public Dictionary<string, int> StatModifiers = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class ExpeditionPolicyDefinition
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

    public sealed class ZoneDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public RiskTier RiskTier;
        public double BaseDurationSeconds;
        public double MinDurationSeconds;
        public double MaxDurationSeconds;
        public int FoodCostPerSurvivor;
        public int WaterCostPerSurvivor;
        public int RecommendedPower;
        public double BaseAmbushChance;
        public Dictionary<string, int> RequiredBuildingLevels = new Dictionary<string, int>(StringComparer.Ordinal);
        public List<WeightedEntry> EnemyTable = new List<WeightedEntry>();
        public List<LootTableEntry> LootTable = new List<LootTableEntry>();
        public List<WeightedEntry> EventTable = new List<WeightedEntry>();
        public List<UnlockCondition> UnlockConditions = new List<UnlockCondition>();
    }

    public sealed class WeightedEntry
    {
        public string Id = string.Empty;
        public int Weight;
    }

    public sealed class LootTableEntry
    {
        public string ResourceId = string.Empty;
        public int Min;
        public int Max;
        public int Weight;
    }

    public sealed class UnlockCondition
    {
        public string Type = string.Empty;
        public string Id = string.Empty;
        public int Value;
    }

    public sealed class EnemyDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public int MaxHealth;
        public int Armor;
        public double Evasion;
        public int BaseDamage;
        public WeaponType AttackType = WeaponType.Melee;
        public double Accuracy = 0.75;
        public double AttackIntervalSeconds = 2;
        public int XpReward;
    }

    public sealed class ItemDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
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

    public sealed class BuildingDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public int StartingLevel;
        public bool StartsUnlocked;
        public string AffectedResourceId = string.Empty;
        public string ProducedResourceId = string.Empty;
        public List<BuildingLevelDefinition> Levels = new List<BuildingLevelDefinition>();
    }

    public sealed class BuildingLevelDefinition
    {
        public int Level;
        public Dictionary<string, int> Cost = new Dictionary<string, int>(StringComparer.Ordinal);
        public int SurvivorCap;
        public int SquadSize;
        public int ResourceCap;
        public int ResourcePerMinute;
    }

    public sealed class BalanceDefinition
    {
        public int MaxOfflineSeconds = 43200;
        public double SimulationTickSeconds = 1;
        public double CombatTickSeconds = 2;
        public double ExpeditionStepSeconds = 10;
        public double AutosaveSeconds = 30;
        public double ActiveCommandCooldownSeconds = 30;
        public double BaseAccuracyMelee = 0.78;
        public double BaseAccuracyFirearms = 0.65;
        public double MinHitChance = 0.15;
        public double MaxHitChance = 0.95;
        public double BaseCritChance = 0.05;
        public double CritMultiplier = 1.75;
    }

    public sealed class StartingSurvivorDefinition
    {
        public string Name = "Mara";
        public string BackgroundId = "scavenger";
        public List<string> TraitIds = new List<string>();
        public string WeaponItemId = "rusty_knife";
        public Dictionary<string, int> Skills = new Dictionary<string, int>(StringComparer.Ordinal);
    }
}
