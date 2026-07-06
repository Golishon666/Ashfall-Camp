using System;
using System.Collections.Generic;

namespace AshfallCamp.Domain
{
    public static class GameConstants
    {
        public const string CurrentSaveVersion = "0.1.0";
    }

    public static class GameEventIds
    {
        public const string SurvivorJoined = "survivor_joined";
        public const string DemoCompleted = "demo_completed";
        public const string EmergencyScavengeCompleted = "emergency_scavenge_completed";
    }

    public static class GameConditionTypes
    {
        public const string ZoneCompletions = "zone_completions";
        public const string ZoneUnlocked = "zone_unlocked";
        public const string BuildingLevel = "building_level";
        public const string SurvivorCount = "survivor_count";
        public const string ResourceAmount = "resource_amount";
        public const string ExpeditionsCompleted = "expeditions_completed";
        public const string ActiveExpeditions = "active_expeditions";
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

    public enum WeaponCombatType
    {
        Melee,
        Ranged,
        Explosive
    }

    public enum WeaponRarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    public enum ArmorType
    {
        Light,
        Medium,
        Heavy
    }

    public enum UtilityEquipmentType
    {
        Medkit,
        Toolkit,
        AmmoPack,
        Backpack
    }

    public enum EnemyKind
    {
        Creature,
        Human,
        Mutant
    }

    public enum WeaponTargetingRule
    {
        FrontlineOnly,
        AnyEnemy,
        AreaAnyEnemies
    }

    public sealed class GameState
    {
        public string Version = GameConstants.CurrentSaveVersion;
        public long CreatedAtUnixMs;
        public long LastSaveAtUnixMs;
        public double TotalPlayTimeSeconds;
        public double CampUpkeepAccumulatorSeconds;
        public int NextId = 1;
        public int SurvivorCap = 1;
        public int SquadSize = 1;
        public Dictionary<string, int> Resources = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> ResourceCaps = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, double> ResourceProductionRemainders = new Dictionary<string, double>(StringComparer.Ordinal);
        public Dictionary<string, double> RestFatigueRecoveryRemainders = new Dictionary<string, double>(StringComparer.Ordinal);
        public List<SurvivorState> Survivors = new List<SurvivorState>();
        public List<InventoryItemState> Inventory = new List<InventoryItemState>();
        public Dictionary<string, BuildingState> Buildings = new Dictionary<string, BuildingState>(StringComparer.Ordinal);
        public Dictionary<string, ZoneState> Zones = new Dictionary<string, ZoneState>(StringComparer.Ordinal);
        public Dictionary<string, UpgradeState> Upgrades = new Dictionary<string, UpgradeState>(StringComparer.Ordinal);
        public List<ExpeditionState> Expeditions = new List<ExpeditionState>();
        public List<CampEventState> CampEvents = new List<CampEventState>();
        public RecruitmentState Recruitment = new RecruitmentState();
        public RecoveryActionState Recovery = new RecoveryActionState();
        public OfflineProgressReport LastOfflineReport;
        public GameProgressState Progress = new GameProgressState();
        public GameSettings Settings = new GameSettings();
        public GameStatistics Statistics = new GameStatistics();
    }

    public sealed class GameProgressState
    {
        public bool DemoCompleted;
        public string DemoCompletionId = string.Empty;
        public long DemoCompletedAtUnixMs;
    }

    public sealed class RecruitmentState
    {
        public List<string> PendingCandidateIds = new List<string>();
        public long LastBroadcastAtUnixMs;
    }

    public sealed class RecoveryActionState
    {
        public bool EmergencyScavengeActive;
        public double EmergencyScavengeRemainingSeconds;
        public long EmergencyScavengeStartedAtUnixMs;
        public double EmergencyScavengeCooldownRemainingSeconds;
    }

    public sealed class CampEventState
    {
        public string Id = string.Empty;
        public string EventId = string.Empty;
        public string SubjectId = string.Empty;
        public string SubjectName = string.Empty;
        public string DetailId = string.Empty;
        public long AtUnixMs;
    }

    public sealed class GameSettings
    {
        public bool AutosaveEnabled = true;
    }

    public sealed class GameStatistics
    {
        public int ExpeditionsCompleted;
        public int ExpeditionsFailed;
        public int SurvivorsRecruited;
        public int CombatsWon;
        public int CombatsLost;
        public Dictionary<string, int> TotalResourcesGained = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> TotalResourcesSpent = new Dictionary<string, int>(StringComparer.Ordinal);
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
        public Dictionary<string, int> EquipmentDurabilityLost = new Dictionary<string, int>(StringComparer.Ordinal);
        public List<string> BrokenItemUids = new List<string>();
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

    public sealed class RecruitSurvivorRequest
    {
        public string CandidateId = string.Empty;
        public long NowUnixMs;
    }

    public sealed class BroadcastRecruitmentRequest
    {
        public uint Seed;
        public long NowUnixMs;
    }

    public sealed class BroadcastRecruitmentResult
    {
        public ValidationResult Validation = new ValidationResult();
        public Dictionary<string, int> Cost = new Dictionary<string, int>(StringComparer.Ordinal);
        public List<string> CandidateIds = new List<string>();
    }

    public sealed class SkipRecruitmentCandidatesResult
    {
        public ValidationResult Validation = new ValidationResult();
        public List<string> SkippedCandidateIds = new List<string>();
    }

    public sealed class RecruitSurvivorResult
    {
        public ValidationResult Validation = new ValidationResult();
        public SurvivorState Survivor;
        public InventoryItemState Weapon;
        public Dictionary<string, int> Cost = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class RepairItemRequest
    {
        public string ItemUid = string.Empty;
    }

    public sealed class RepairItemResult
    {
        public ValidationResult Validation = new ValidationResult();
        public InventoryItemState Item;
        public Dictionary<string, int> Cost = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class EquipItemRequest
    {
        public string SurvivorId = string.Empty;
        public string ItemUid = string.Empty;
    }

    public sealed class EquipItemResult
    {
        public ValidationResult Validation = new ValidationResult();
        public SurvivorState Survivor;
        public InventoryItemState Item;
        public InventoryItemState PreviouslyEquippedItem;
    }

    public sealed class UseMedicineRequest
    {
        public string SurvivorId = string.Empty;
    }

    public sealed class UseMedicineResult
    {
        public ValidationResult Validation = new ValidationResult();
        public SurvivorState Survivor;
        public Dictionary<string, int> Cost = new Dictionary<string, int>(StringComparer.Ordinal);
        public bool Healed;
    }

    public sealed class StartRestRequest
    {
        public string SurvivorId = string.Empty;
    }

    public sealed class StopRestRequest
    {
        public string SurvivorId = string.Empty;
    }

    public sealed class RestSurvivorResult
    {
        public ValidationResult Validation = new ValidationResult();
        public SurvivorState Survivor;
        public bool Started;
        public bool Stopped;
    }

    public sealed class EmergencyScavengeRequest
    {
        public long NowUnixMs;
    }

    public sealed class EmergencyScavengeResult
    {
        public ValidationResult Validation = new ValidationResult();
        public bool Started;
        public double DurationSeconds;
        public Dictionary<string, int> Rewards = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class OfflineProgressReport
    {
        public double AppliedSeconds;
        public bool WasPresented;
        public Dictionary<string, int> ResourcesGained = new Dictionary<string, int>(StringComparer.Ordinal);
        public Dictionary<string, int> ResourcesSpent = new Dictionary<string, int>(StringComparer.Ordinal);
        public List<string> CompletedExpeditionIds = new List<string>();
        public List<string> WoundedSurvivorIds = new List<string>();
        public List<string> HealedSurvivorIds = new List<string>();
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
        public Dictionary<string, WeaponDefinition> Weapons = new Dictionary<string, WeaponDefinition>(StringComparer.Ordinal);
        public Dictionary<string, ArmorDefinition> Armor = new Dictionary<string, ArmorDefinition>(StringComparer.Ordinal);
        public Dictionary<string, UtilityDefinition> Utilities = new Dictionary<string, UtilityDefinition>(StringComparer.Ordinal);
        public Dictionary<string, BuildingDefinition> Buildings = new Dictionary<string, BuildingDefinition>(StringComparer.Ordinal);
        public Dictionary<string, RecruitableSurvivorDefinition> RecruitableSurvivors = new Dictionary<string, RecruitableSurvivorDefinition>(StringComparer.Ordinal);
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
        public int DurabilityModifier;
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
        public int DurabilityPressure;
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
        public EnemyKind Kind;
        public string PortraitId = string.Empty;
        public int MaxHealth;
        public int Armor;
        public double Evasion;
        public int BaseDamage;
        public WeaponType AttackType = WeaponType.Melee;
        public double Accuracy = 0.75;
        public double AttackIntervalSeconds = 2;
        public int XpReward;
        public string WeaponConfigId = string.Empty;
        public string ArmorConfigId = string.Empty;
        public string UtilityConfigId = string.Empty;
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

    public sealed class WeaponDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public WeaponCombatType Type;
        public WeaponRarity Rarity;
        public int Attack;
        public int AttacksPerTurn = 1;
        public int TargetCount = 1;
        public WeaponTargetingRule TargetingRule = WeaponTargetingRule.FrontlineOnly;
        public double HitChance = 0.75;
        public double ArmorPenetration;
        public double CriticalChance;
        public int AmmoCostPerAttack;
        public int NoisePerAttack;
        public float Durability = 1f;
        public int MaxDurability = 100;
        public double RepairCostMultiplier = 1.0;
        public int SortOrder;
        public string AttackSoundId = string.Empty;
    }

    public sealed class ArmorDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public ArmorType Type;
        public WeaponRarity Rarity;
        public int Defense;
        public double EvasionChance;
        public int BonusHealth;
        public int BonusStamina;
        public double SpeedModifier;
        public float Durability = 1f;
        public int MaxDurability = 100;
        public double RepairCostMultiplier = 1.0;
        public int SortOrder;
        public string EquipSoundId = string.Empty;
    }

    public sealed class UtilityDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public string Description = string.Empty;
        public UtilityEquipmentType Type;
        public WeaponRarity Rarity;
        public int Tier = 1;
        public int HealAmount;
        public int RepairBonus;
        public int AmmoCapacityBonus;
        public int CarryCapacityBonus;
        public int BonusStamina;
        public int MaxDurability = 100;
        public double RepairCostMultiplier = 1.0;
        public int SortOrder;
        public string UseSoundId = string.Empty;
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
        public int ExpeditionCompletionXp = 5;
        public string ExpeditionCompletionSkillId = "survival";
        public int ExpeditionCompletionSkillXp = 3;
        public double CampUpkeepIntervalSeconds = 300;
        public string CampUpkeepFoodResourceId = "food";
        public int CampUpkeepFoodPerSurvivor = 1;
        public string CampUpkeepWaterResourceId = "water";
        public int CampUpkeepWaterPerSurvivor = 1;
        public int CampUpkeepShortageMoralePenalty = 4;
        public int CampUpkeepShortageFatigue = 2;
        public double RestFatigueRecoveryPerMinute = 10;
        public int SurvivorXpThresholdBase = 50;
        public double SurvivorXpThresholdExponent = 1.55;
        public int SurvivorMaxLevel = 50;
        public int SurvivorHealthPerLevel = 2;
        public int SkillXpThresholdBase = 20;
        public double SkillXpThresholdExponent = 1.35;
        public int SkillMaxLevel = 50;
        public int RecruitmentBaseScrap = 20;
        public double RecruitmentScrapExponent = 1.25;
        public int RecruitmentBaseFood = 2;
        public int RecruitmentFoodDivisor = 2;
        public int RecruitmentBaseWater = 2;
        public int RecruitmentWaterDivisor = 3;
        public int RecruitmentCandidateCount = 2;
        public string WorkshopRequiredBuildingId = "workshop";
        public int WorkshopRequiredBuildingLevel = 1;
        public string WorkshopRepairResourceId = "weapon_parts";
        public int WorkshopRepairDurabilityBlock = 10;
        public string DurabilityTraitModifierId = "durability_loss";
        public string HealingRequiredBuildingId = "infirmary";
        public int HealingRequiredBuildingLevel = 1;
        public string HealingDefaultWoundId = "cuts";
        public double HealingDefaultWoundDurationSeconds = 300;
        public int HealingHealthOnWounded = 1;
        public string HealingMedicineResourceId = "medicine";
        public int HealingMedicineCost = 1;
        public double HealingMedicineSeconds = 300;
        public double EmergencyScavengeDurationSeconds = 60;
        public double EmergencyScavengeCooldownSeconds = 300;
        public Dictionary<string, int> EmergencyScavengeRewards = new Dictionary<string, int>(StringComparer.Ordinal);
        public bool DemoCompletionRequiresAnyCondition = true;
        public List<UnlockCondition> DemoCompletionConditions = new List<UnlockCondition>();
    }

    public sealed class StartingSurvivorDefinition
    {
        public string Name = "Mara";
        public string PortraitId = string.Empty;
        public string BackgroundId = "scavenger";
        public List<string> TraitIds = new List<string>();
        public string WeaponItemId = "rusty_knife";
        public string WeaponConfigId = string.Empty;
        public string ArmorConfigId = string.Empty;
        public string UtilityConfigId = string.Empty;
        public Dictionary<string, int> Skills = new Dictionary<string, int>(StringComparer.Ordinal);
    }

    public sealed class RecruitableSurvivorDefinition
    {
        public string Id = string.Empty;
        public string Name = string.Empty;
        public string PortraitId = string.Empty;
        public string BackgroundId = string.Empty;
        public List<string> TraitIds = new List<string>();
        public string WeaponItemId = string.Empty;
        public string WeaponConfigId = string.Empty;
        public string ArmorConfigId = string.Empty;
        public string UtilityConfigId = string.Empty;
        public Dictionary<string, int> Skills = new Dictionary<string, int>(StringComparer.Ordinal);
    }
}
