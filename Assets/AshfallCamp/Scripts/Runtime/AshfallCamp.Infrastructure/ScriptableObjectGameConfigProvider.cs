using System;
using System.Collections.Generic;
using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Domain;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    public sealed class ScriptableObjectGameConfigProvider : IGameConfigProvider
    {
        private readonly GameConfigDatabaseSO _database;
        private GameConfigSnapshot _current;

        public ScriptableObjectGameConfigProvider(GameConfigDatabaseSO database)
        {
            _database = database;
        }

        public GameConfigSnapshot Current
        {
            get { return _current; }
        }

        public UniTask<GameConfigSnapshot> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            if (_database == null)
            {
                throw new InvalidOperationException("GameConfigDatabaseSO is not assigned.");
            }

            var snapshot = new GameConfigSnapshot();
            FillResources(snapshot);
            FillSurvivor(snapshot);
            FillBackgrounds(snapshot);
            FillTraits(snapshot);
            FillPolicies(snapshot);
            FillZones(snapshot);
            FillEnemies(snapshot);
            FillItems(snapshot);
            FillWeapons(snapshot);
            FillBuildings(snapshot);
            FillBalance(snapshot);
            Validate(snapshot);
            _current = snapshot;
            return UniTask.FromResult(snapshot);
        }

        private void FillResources(GameConfigSnapshot snapshot)
        {
            if (_database.Resources == null) return;
            foreach (var item in _database.Resources.Resources)
            {
                AddDefinition(snapshot.Resources, item.Id, new ResourceDefinition
                {
                    Id = item.Id,
                    Name = item.Name,
                    HasCap = item.HasCap,
                    StartAmount = item.StartAmount,
                    StartCap = item.StartCap
                }, "resources");
            }
        }

        private void FillSurvivor(GameConfigSnapshot snapshot)
        {
            if (_database.Survivors == null) return;
            var source = _database.Survivors.StartingSurvivor;
            snapshot.StartingSurvivor.Name = source.Name;
            snapshot.StartingSurvivor.BackgroundId = source.BackgroundId;
            snapshot.StartingSurvivor.TraitIds = new List<string>(source.TraitIds);
            snapshot.StartingSurvivor.WeaponItemId = source.WeaponItemId;
            snapshot.StartingSurvivor.Skills = ToDictionary(source.Skills);

            foreach (var item in _database.Survivors.RecruitableSurvivors)
            {
                AddDefinition(snapshot.RecruitableSurvivors, item.Id, new RecruitableSurvivorDefinition
                {
                    Id = item.Id,
                    Name = item.Name,
                    BackgroundId = item.BackgroundId,
                    TraitIds = new List<string>(item.TraitIds),
                    WeaponItemId = item.WeaponItemId,
                    Skills = ToDictionary(item.Skills)
                }, "recruitable survivors");
            }
        }

        private void FillBackgrounds(GameConfigSnapshot snapshot)
        {
            if (_database.Backgrounds == null) return;
            foreach (var item in _database.Backgrounds.Backgrounds)
            {
                AddDefinition(snapshot.Backgrounds, item.Id, new BackgroundDefinition
                {
                    Id = item.Id,
                    Name = item.Name,
                    SkillBonuses = ToDictionary(item.SkillBonuses),
                    StatBonuses = ToDictionary(item.StatBonuses)
                }, "backgrounds");
            }
        }

        private void FillTraits(GameConfigSnapshot snapshot)
        {
            if (_database.Traits == null) return;
            foreach (var item in _database.Traits.Traits)
            {
                AddDefinition(snapshot.Traits, item.Id, new TraitDefinition
                {
                    Id = item.Id,
                    Name = item.Name,
                    StatModifiers = ToDictionary(item.StatModifiers)
                }, "traits");
            }
        }

        private void FillPolicies(GameConfigSnapshot snapshot)
        {
            if (_database.Policies == null) return;
            foreach (var item in _database.Policies.Policies)
            {
                AddDefinition(snapshot.Policies, item.Id, new ExpeditionPolicyDefinition
                {
                    Id = item.Id,
                    Name = item.Name,
                    RiskModifier = item.RiskModifier,
                    LootModifier = item.LootModifier,
                    DurationModifier = item.DurationModifier,
                    FoodModifier = item.FoodModifier,
                    WaterModifier = item.WaterModifier,
                    PowerModifier = item.PowerModifier,
                    NoiseModifier = item.NoiseModifier,
                    DurabilityModifier = item.DurabilityModifier
                }, "policies");
            }
        }

        private void FillZones(GameConfigSnapshot snapshot)
        {
            if (_database.Zones == null) return;
            foreach (var item in _database.Zones.Zones)
            {
                var zone = new ZoneDefinition
                {
                    Id = item.Id,
                    Name = item.Name,
                    RiskTier = item.RiskTier,
                    BaseDurationSeconds = item.BaseDurationSeconds,
                    MinDurationSeconds = item.MinDurationSeconds,
                    MaxDurationSeconds = item.MaxDurationSeconds,
                    FoodCostPerSurvivor = item.FoodCostPerSurvivor,
                    WaterCostPerSurvivor = item.WaterCostPerSurvivor,
                    RecommendedPower = item.RecommendedPower,
                    BaseAmbushChance = item.BaseAmbushChance,
                    DurabilityPressure = item.DurabilityPressure,
                    RequiredBuildingLevels = ToDictionary(item.RequiredBuildingLevels)
                };
                foreach (var entry in item.EnemyTable) zone.EnemyTable.Add(new WeightedEntry { Id = entry.Id, Weight = entry.Weight });
                foreach (var entry in item.EventTable) zone.EventTable.Add(new WeightedEntry { Id = entry.Id, Weight = entry.Weight });
                foreach (var entry in item.LootTable) zone.LootTable.Add(new LootTableEntry { ResourceId = entry.ResourceId, Min = entry.Min, Max = entry.Max, Weight = entry.Weight });
                foreach (var entry in item.UnlockConditions) zone.UnlockConditions.Add(new UnlockCondition { Type = entry.Type, Id = entry.Id, Value = entry.Value });
                AddDefinition(snapshot.Zones, item.Id, zone, "zones");
            }
        }

        private void FillEnemies(GameConfigSnapshot snapshot)
        {
            if (_database.Enemies == null) return;
            foreach (var item in _database.Enemies.Enemies)
            {
                AddDefinition(snapshot.Enemies, item.Id, new EnemyDefinition
                {
                    Id = item.Id,
                    Name = item.Name,
                    MaxHealth = item.MaxHealth,
                    Armor = item.Armor,
                    Evasion = item.Evasion,
                    BaseDamage = item.BaseDamage,
                    AttackType = item.AttackType,
                    Accuracy = item.Accuracy,
                    AttackIntervalSeconds = item.AttackIntervalSeconds,
                    XpReward = item.XpReward
                }, "enemies");
            }
        }

        private void FillItems(GameConfigSnapshot snapshot)
        {
            if (_database.Items == null) return;
            foreach (var item in _database.Items.Items)
            {
                AddDefinition(snapshot.Items, item.Id, new ItemDefinition
                {
                    Id = item.Id,
                    Name = item.Name,
                    Slot = item.Slot,
                    WeaponType = item.WeaponType,
                    BaseDamage = item.BaseDamage,
                    Armor = item.Armor,
                    AccuracyBonus = item.AccuracyBonus,
                    CritBonus = item.CritBonus,
                    NoisePerAttack = item.NoisePerAttack,
                    MaxDurability = item.MaxDurability,
                    RepairCostMultiplier = item.RepairCostMultiplier,
                    CarryCapacityBonus = item.CarryCapacityBonus
                }, "items");
            }
        }

        private void FillWeapons(GameConfigSnapshot snapshot)
        {
            if (_database.Weapons == null) return;
            foreach (var item in _database.Weapons.Weapons)
            {
                AddDefinition(snapshot.Weapons, item.Id, new WeaponDefinition
                {
                    Id = item.Id,
                    Name = item.Name,
                    Description = item.Description,
                    Type = item.Type,
                    Rarity = item.Rarity,
                    Attack = item.Attack,
                    AttacksPerTurn = item.AttacksPerTurn,
                    TargetCount = item.TargetCount,
                    TargetingRule = item.TargetingRule,
                    HitChance = item.HitChance,
                    ArmorPenetration = item.ArmorPenetration,
                    CriticalChance = item.CriticalChance,
                    AmmoCostPerAttack = item.AmmoCostPerAttack,
                    NoisePerAttack = item.NoisePerAttack,
                    MaxDurability = item.MaxDurability,
                    RepairCostMultiplier = item.RepairCostMultiplier,
                    SortOrder = item.SortOrder,
                    AttackSoundId = item.AttackSoundId
                }, "weapons");
            }
        }

        private void FillBuildings(GameConfigSnapshot snapshot)
        {
            if (_database.Buildings == null) return;
            foreach (var item in _database.Buildings.Buildings)
            {
                var building = new BuildingDefinition
                {
                    Id = item.Id,
                    Name = item.Name,
                    StartingLevel = item.StartingLevel,
                    StartsUnlocked = item.StartsUnlocked,
                    AffectedResourceId = item.AffectedResourceId ?? string.Empty,
                    ProducedResourceId = item.ProducedResourceId ?? string.Empty
                };
                foreach (var level in item.Levels)
                {
                    building.Levels.Add(new BuildingLevelDefinition
                    {
                        Level = level.Level,
                        Cost = ToDictionary(level.Cost),
                        SurvivorCap = level.SurvivorCap,
                        SquadSize = level.SquadSize,
                        ResourceCap = level.ResourceCap,
                        ResourcePerMinute = level.ResourcePerMinute
                    });
                }
                AddDefinition(snapshot.Buildings, item.Id, building, "buildings");
            }
        }

        private void FillBalance(GameConfigSnapshot snapshot)
        {
            if (_database.Balance == null) return;
            var source = _database.Balance.Balance;
            snapshot.Balance = new BalanceDefinition
            {
                MaxOfflineSeconds = source.MaxOfflineSeconds,
                SimulationTickSeconds = source.SimulationTickSeconds,
                CombatTickSeconds = source.CombatTickSeconds,
                ExpeditionStepSeconds = source.ExpeditionStepSeconds,
                AutosaveSeconds = source.AutosaveSeconds,
                OfflineReportMinimumSeconds = source.OfflineReportMinimumSeconds,
                ActiveCommandCooldownSeconds = source.ActiveCommandCooldownSeconds,
                BaseAccuracyMelee = source.BaseAccuracyMelee,
                BaseAccuracyFirearms = source.BaseAccuracyFirearms,
                MinHitChance = source.MinHitChance,
                MaxHitChance = source.MaxHitChance,
                BaseCritChance = source.BaseCritChance,
                CritMultiplier = source.CritMultiplier,
                RecruitmentRequiredBuildingId = source.RecruitmentRequiredBuildingId,
                RecruitmentRequiredBuildingLevel = source.RecruitmentRequiredBuildingLevel,
                RecruitmentScrapResourceId = source.RecruitmentScrapResourceId,
                RecruitmentFoodResourceId = source.RecruitmentFoodResourceId,
                RecruitmentWaterResourceId = source.RecruitmentWaterResourceId,
                ExpeditionFoodResourceId = source.ExpeditionFoodResourceId,
                ExpeditionWaterResourceId = source.ExpeditionWaterResourceId,
                ExpeditionCompletionXp = source.ExpeditionCompletionXp,
                ExpeditionCompletionSkillId = source.ExpeditionCompletionSkillId,
                ExpeditionCompletionSkillXp = source.ExpeditionCompletionSkillXp,
                CampUpkeepIntervalSeconds = source.CampUpkeepIntervalSeconds,
                CampUpkeepFoodResourceId = source.CampUpkeepFoodResourceId,
                CampUpkeepFoodPerSurvivor = source.CampUpkeepFoodPerSurvivor,
                CampUpkeepWaterResourceId = source.CampUpkeepWaterResourceId,
                CampUpkeepWaterPerSurvivor = source.CampUpkeepWaterPerSurvivor,
                CampUpkeepShortageMoralePenalty = source.CampUpkeepShortageMoralePenalty,
                CampUpkeepShortageFatigue = source.CampUpkeepShortageFatigue,
                RestFatigueRecoveryPerMinute = source.RestFatigueRecoveryPerMinute,
                SurvivorXpThresholdBase = source.SurvivorXpThresholdBase,
                SurvivorXpThresholdExponent = source.SurvivorXpThresholdExponent,
                SurvivorMaxLevel = source.SurvivorMaxLevel,
                SurvivorHealthPerLevel = source.SurvivorHealthPerLevel,
                SkillXpThresholdBase = source.SkillXpThresholdBase,
                SkillXpThresholdExponent = source.SkillXpThresholdExponent,
                SkillMaxLevel = source.SkillMaxLevel,
                RecruitmentBaseScrap = source.RecruitmentBaseScrap,
                RecruitmentScrapExponent = source.RecruitmentScrapExponent,
                RecruitmentBaseFood = source.RecruitmentBaseFood,
                RecruitmentFoodDivisor = source.RecruitmentFoodDivisor,
                RecruitmentBaseWater = source.RecruitmentBaseWater,
                RecruitmentWaterDivisor = source.RecruitmentWaterDivisor,
                RecruitmentCandidateCount = source.RecruitmentCandidateCount,
                WorkshopRequiredBuildingId = source.WorkshopRequiredBuildingId,
                WorkshopRequiredBuildingLevel = source.WorkshopRequiredBuildingLevel,
                WorkshopRepairResourceId = source.WorkshopRepairResourceId,
                WorkshopRepairDurabilityBlock = source.WorkshopRepairDurabilityBlock,
                DurabilityTraitModifierId = source.DurabilityTraitModifierId,
                HealingRequiredBuildingId = source.HealingRequiredBuildingId,
                HealingRequiredBuildingLevel = source.HealingRequiredBuildingLevel,
                HealingDefaultWoundId = source.HealingDefaultWoundId,
                HealingDefaultWoundDurationSeconds = source.HealingDefaultWoundDurationSeconds,
                HealingHealthOnWounded = source.HealingHealthOnWounded,
                HealingMedicineResourceId = source.HealingMedicineResourceId,
                HealingMedicineCost = source.HealingMedicineCost,
                HealingMedicineSeconds = source.HealingMedicineSeconds,
                EmergencyScavengeDurationSeconds = source.EmergencyScavengeDurationSeconds,
                EmergencyScavengeCooldownSeconds = source.EmergencyScavengeCooldownSeconds,
                EmergencyScavengeRewards = ToDictionary(source.EmergencyScavengeRewards),
                DemoCompletionRequiresAnyCondition = source.DemoCompletionRequiresAnyCondition
            };
            foreach (var condition in source.DemoCompletionConditions)
            {
                snapshot.Balance.DemoCompletionConditions.Add(new UnlockCondition { Type = condition.Type, Id = condition.Id, Value = condition.Value });
            }
        }

        private static Dictionary<string, int> ToDictionary(List<IntPairData> entries)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (!string.IsNullOrWhiteSpace(entry.Id)) result[entry.Id] = entry.Value;
            }
            return result;
        }

        private static void AddDefinition<T>(Dictionary<string, T> items, string id, T value, string label)
        {
            if (string.IsNullOrWhiteSpace(id)) throw new InvalidOperationException("Config catalog has empty id: " + label);
            if (items.ContainsKey(id)) throw new InvalidOperationException("Config catalog has duplicate id: " + label + "/" + id);
            items[id] = value;
        }

        private static void Validate(GameConfigSnapshot snapshot)
        {
            RequireAny(snapshot.Resources, "resources");
            RequireAny(snapshot.Backgrounds, "backgrounds");
            RequireAny(snapshot.Traits, "traits");
            RequireAny(snapshot.Policies, "policies");
            RequireAny(snapshot.Zones, "zones");
            RequireAny(snapshot.Enemies, "enemies");
            RequireAny(snapshot.Items, "items");
            RequireAny(snapshot.Weapons, "weapons");
            RequireAny(snapshot.Buildings, "buildings");
            if (!snapshot.Policies.ContainsKey("balanced")) throw new InvalidOperationException("Policy catalog must include balanced.");
            if (!snapshot.Backgrounds.ContainsKey(snapshot.StartingSurvivor.BackgroundId)) throw new InvalidOperationException("Starting survivor background is missing.");
            foreach (var traitId in snapshot.StartingSurvivor.TraitIds)
            {
                if (!snapshot.Traits.ContainsKey(traitId)) throw new InvalidOperationException("Starting survivor trait is missing: " + traitId);
            }
            if (!snapshot.Items.ContainsKey(snapshot.StartingSurvivor.WeaponItemId)) throw new InvalidOperationException("Starting weapon item is missing.");
            ValidateResources(snapshot);
            ValidatePolicies(snapshot);
            ValidateExpeditionBalance(snapshot);
            ValidateCampUpkeep(snapshot);
            ValidateRecruitment(snapshot);
            ValidateWorkshop(snapshot);
            ValidateHealing(snapshot);
            ValidateEmergencyScavenge(snapshot);
            ValidateDemoCompletion(snapshot);
            ValidateEnemies(snapshot);
            ValidateItems(snapshot);
            ValidateWeapons(snapshot);
            ValidateBuildings(snapshot);
            foreach (var zone in snapshot.Zones.Values)
            {
                if (zone.BaseDurationSeconds <= 0) throw new InvalidOperationException("Zone duration must be positive: " + zone.Id);
                if (zone.MinDurationSeconds <= 0 || zone.MaxDurationSeconds < zone.MinDurationSeconds) throw new InvalidOperationException("Zone duration bounds are invalid: " + zone.Id);
                if (zone.FoodCostPerSurvivor < 0 || zone.WaterCostPerSurvivor < 0) throw new InvalidOperationException("Zone costs cannot be negative: " + zone.Id);
                if (zone.DurabilityPressure < 0) throw new InvalidOperationException("Zone durability pressure cannot be negative: " + zone.Id);
                foreach (var requirement in zone.RequiredBuildingLevels)
                {
                    if (!snapshot.Buildings.ContainsKey(requirement.Key)) throw new InvalidOperationException("Zone requirement references unknown building: " + requirement.Key);
                    if (requirement.Value < 0) throw new InvalidOperationException("Zone requirement level cannot be negative: " + zone.Id);
                }
                foreach (var enemy in zone.EnemyTable)
                {
                    if (!snapshot.Enemies.ContainsKey(enemy.Id)) throw new InvalidOperationException("Zone enemy table references unknown enemy: " + enemy.Id);
                    if (enemy.Weight <= 0) throw new InvalidOperationException("Zone enemy weight must be positive: " + zone.Id);
                }
                foreach (var loot in zone.LootTable)
                {
                    if (!snapshot.Resources.ContainsKey(loot.ResourceId)) throw new InvalidOperationException("Zone loot references unknown resource: " + loot.ResourceId);
                    if (loot.Weight <= 0 || loot.Min < 0 || loot.Max < loot.Min) throw new InvalidOperationException("Zone loot entry is invalid: " + zone.Id);
                }
                foreach (var condition in zone.UnlockConditions)
                {
                    if (condition.Value < 0) throw new InvalidOperationException("Zone unlock value cannot be negative: " + zone.Id);
                    if (condition.Type == GameConditionTypes.ZoneCompletions)
                    {
                        if (!snapshot.Zones.ContainsKey(condition.Id)) throw new InvalidOperationException("Zone unlock references unknown zone: " + condition.Id);
                    }
                    else if (condition.Type == GameConditionTypes.BuildingLevel)
                    {
                        if (!snapshot.Buildings.ContainsKey(condition.Id)) throw new InvalidOperationException("Zone unlock references unknown building: " + condition.Id);
                    }
                    else
                    {
                        throw new InvalidOperationException("Zone unlock has unknown type: " + condition.Type);
                    }
                }
            }
        }

        private static void ValidateResources(GameConfigSnapshot snapshot)
        {
            foreach (var resource in snapshot.Resources.Values)
            {
                if (resource.StartAmount < 0) throw new InvalidOperationException("Resource start amount cannot be negative: " + resource.Id);
                if (resource.HasCap && resource.StartCap < 0) throw new InvalidOperationException("Resource cap cannot be negative: " + resource.Id);
                if (resource.HasCap && resource.StartAmount > resource.StartCap) throw new InvalidOperationException("Resource starts above cap: " + resource.Id);
            }
        }

        private static void ValidatePolicies(GameConfigSnapshot snapshot)
        {
            foreach (var policy in snapshot.Policies.Values)
            {
                if (policy.RiskModifier <= 0 || policy.LootModifier <= 0 || policy.DurationModifier <= 0 || policy.FoodModifier <= 0 || policy.WaterModifier <= 0 || policy.PowerModifier <= 0)
                {
                    throw new InvalidOperationException("Policy modifiers must be positive: " + policy.Id);
                }
            }

            if (snapshot.Balance.MaxOfflineSeconds <= 0) throw new InvalidOperationException("Max offline seconds must be positive.");
            if (snapshot.Balance.SimulationTickSeconds <= 0) throw new InvalidOperationException("Simulation tick seconds must be positive.");
            if (snapshot.Balance.ExpeditionStepSeconds <= 0) throw new InvalidOperationException("Expedition step seconds must be positive.");
            if (snapshot.Balance.OfflineReportMinimumSeconds < 0) throw new InvalidOperationException("Offline report minimum seconds cannot be negative.");
            if (snapshot.Balance.MinHitChance < 0 || snapshot.Balance.MaxHitChance > 1 || snapshot.Balance.MinHitChance > snapshot.Balance.MaxHitChance)
            {
                throw new InvalidOperationException("Hit chance bounds are invalid.");
            }
        }

        private static void ValidateRecruitment(GameConfigSnapshot snapshot)
        {
            if (snapshot.RecruitableSurvivors.Count == 0) throw new InvalidOperationException("Config catalog is empty: recruitable survivors");
            if (!string.IsNullOrWhiteSpace(snapshot.Balance.RecruitmentRequiredBuildingId) &&
                !snapshot.Buildings.ContainsKey(snapshot.Balance.RecruitmentRequiredBuildingId))
            {
                throw new InvalidOperationException("Recruitment references unknown building: " + snapshot.Balance.RecruitmentRequiredBuildingId);
            }

            ValidateRecruitmentResource(snapshot, snapshot.Balance.RecruitmentScrapResourceId);
            ValidateRecruitmentResource(snapshot, snapshot.Balance.RecruitmentFoodResourceId);
            ValidateRecruitmentResource(snapshot, snapshot.Balance.RecruitmentWaterResourceId);
            if (snapshot.Balance.RecruitmentRequiredBuildingLevel < 0) throw new InvalidOperationException("Recruitment building level cannot be negative.");
            if (snapshot.Balance.RecruitmentBaseScrap < 0 || snapshot.Balance.RecruitmentBaseFood < 0 || snapshot.Balance.RecruitmentBaseWater < 0)
            {
                throw new InvalidOperationException("Recruitment base costs cannot be negative.");
            }

            if (snapshot.Balance.RecruitmentScrapExponent <= 0) throw new InvalidOperationException("Recruitment scrap exponent must be positive.");
            if (snapshot.Balance.RecruitmentFoodDivisor <= 0 || snapshot.Balance.RecruitmentWaterDivisor <= 0) throw new InvalidOperationException("Recruitment cost divisors must be positive.");
            if (snapshot.Balance.RecruitmentCandidateCount <= 0) throw new InvalidOperationException("Recruitment candidate count must be positive.");

            var names = new HashSet<string>(StringComparer.Ordinal);
            foreach (var candidate in snapshot.RecruitableSurvivors.Values)
            {
                if (string.IsNullOrWhiteSpace(candidate.Name)) throw new InvalidOperationException("Recruitable survivor has empty name: " + candidate.Id);
                if (!names.Add(candidate.Name)) throw new InvalidOperationException("Recruitable survivor has duplicate name: " + candidate.Name);
                if (!snapshot.Backgrounds.ContainsKey(candidate.BackgroundId)) throw new InvalidOperationException("Recruitable survivor background is missing: " + candidate.Id);
                foreach (var traitId in candidate.TraitIds)
                {
                    if (!snapshot.Traits.ContainsKey(traitId)) throw new InvalidOperationException("Recruitable survivor trait is missing: " + traitId);
                }

                if (!snapshot.Items.ContainsKey(candidate.WeaponItemId)) throw new InvalidOperationException("Recruitable survivor weapon item is missing: " + candidate.Id);
            }
        }

        private static void ValidateRecruitmentResource(GameConfigSnapshot snapshot, string resourceId)
        {
            if (!string.IsNullOrWhiteSpace(resourceId) && !snapshot.Resources.ContainsKey(resourceId))
            {
                throw new InvalidOperationException("Recruitment cost references unknown resource: " + resourceId);
            }
        }

        private static void ValidateExpeditionBalance(GameConfigSnapshot snapshot)
        {
            ValidateExpeditionResource(snapshot, snapshot.Balance.ExpeditionFoodResourceId);
            ValidateExpeditionResource(snapshot, snapshot.Balance.ExpeditionWaterResourceId);
            if (snapshot.Balance.ExpeditionCompletionXp < 0) throw new InvalidOperationException("Expedition completion XP cannot be negative.");
            if (snapshot.Balance.ExpeditionCompletionSkillXp < 0) throw new InvalidOperationException("Expedition completion skill XP cannot be negative.");
            if (string.IsNullOrWhiteSpace(snapshot.Balance.ExpeditionCompletionSkillId)) throw new InvalidOperationException("Expedition completion skill id cannot be empty.");
            if (!ContainsConfiguredSkillId(snapshot, snapshot.Balance.ExpeditionCompletionSkillId))
            {
                throw new InvalidOperationException("Expedition completion references unknown skill: " + snapshot.Balance.ExpeditionCompletionSkillId);
            }

            if (snapshot.Balance.SurvivorXpThresholdBase <= 0 || snapshot.Balance.SkillXpThresholdBase <= 0)
            {
                throw new InvalidOperationException("Progression XP threshold bases must be positive.");
            }

            if (snapshot.Balance.SurvivorXpThresholdExponent <= 0 || snapshot.Balance.SkillXpThresholdExponent <= 0)
            {
                throw new InvalidOperationException("Progression XP threshold exponents must be positive.");
            }

            if (snapshot.Balance.SurvivorMaxLevel <= 0) throw new InvalidOperationException("Survivor max level must be positive.");
            if (snapshot.Balance.SkillMaxLevel < 0) throw new InvalidOperationException("Skill max level cannot be negative.");
            if (snapshot.Balance.SurvivorHealthPerLevel < 0) throw new InvalidOperationException("Survivor health per level cannot be negative.");
        }

        private static void ValidateExpeditionResource(GameConfigSnapshot snapshot, string resourceId)
        {
            if (!string.IsNullOrWhiteSpace(resourceId) && !snapshot.Resources.ContainsKey(resourceId))
            {
                throw new InvalidOperationException("Expedition cost references unknown resource: " + resourceId);
            }
        }

        private static void ValidateCampUpkeep(GameConfigSnapshot snapshot)
        {
            if (snapshot.Balance.CampUpkeepIntervalSeconds <= 0) throw new InvalidOperationException("Camp upkeep interval must be positive.");
            if (snapshot.Balance.CampUpkeepFoodPerSurvivor < 0 || snapshot.Balance.CampUpkeepWaterPerSurvivor < 0)
            {
                throw new InvalidOperationException("Camp upkeep resource costs cannot be negative.");
            }

            if (snapshot.Balance.CampUpkeepShortageMoralePenalty < 0 || snapshot.Balance.CampUpkeepShortageFatigue < 0)
            {
                throw new InvalidOperationException("Camp upkeep shortage penalties cannot be negative.");
            }

            if (snapshot.Balance.RestFatigueRecoveryPerMinute <= 0)
            {
                throw new InvalidOperationException("Rest fatigue recovery must be positive.");
            }

            ValidateCampUpkeepResource(snapshot, snapshot.Balance.CampUpkeepFoodResourceId, snapshot.Balance.CampUpkeepFoodPerSurvivor);
            ValidateCampUpkeepResource(snapshot, snapshot.Balance.CampUpkeepWaterResourceId, snapshot.Balance.CampUpkeepWaterPerSurvivor);
        }

        private static void ValidateCampUpkeepResource(GameConfigSnapshot snapshot, string resourceId, int amount)
        {
            if (amount <= 0 || string.IsNullOrWhiteSpace(resourceId)) return;
            if (!snapshot.Resources.ContainsKey(resourceId))
            {
                throw new InvalidOperationException("Camp upkeep references unknown resource: " + resourceId);
            }
        }

        private static bool ContainsConfiguredSkillId(GameConfigSnapshot snapshot, string skillId)
        {
            if (ContainsSkillId(snapshot.StartingSurvivor.Skills, skillId)) return true;
            foreach (var survivor in snapshot.RecruitableSurvivors.Values)
            {
                if (ContainsSkillId(survivor.Skills, skillId)) return true;
            }

            foreach (var background in snapshot.Backgrounds.Values)
            {
                if (ContainsSkillId(background.SkillBonuses, skillId)) return true;
            }

            return false;
        }

        private static bool ContainsSkillId(Dictionary<string, int> skills, string skillId)
        {
            return skills != null && !string.IsNullOrWhiteSpace(skillId) && skills.ContainsKey(skillId);
        }

        private static void ValidateDemoCompletion(GameConfigSnapshot snapshot)
        {
            foreach (var condition in snapshot.Balance.DemoCompletionConditions)
            {
                if (condition == null) throw new InvalidOperationException("Demo completion condition is missing.");
                if (condition.Value < 0) throw new InvalidOperationException("Demo completion condition value cannot be negative.");
                if (condition.Type == GameConditionTypes.ZoneCompletions || condition.Type == GameConditionTypes.ZoneUnlocked)
                {
                    if (!snapshot.Zones.ContainsKey(condition.Id)) throw new InvalidOperationException("Demo completion references unknown zone: " + condition.Id);
                }
                else if (condition.Type == GameConditionTypes.BuildingLevel)
                {
                    if (!snapshot.Buildings.ContainsKey(condition.Id)) throw new InvalidOperationException("Demo completion references unknown building: " + condition.Id);
                }
                else if (IsDemoCompletionCountOnlyCondition(condition.Type))
                {
                    continue;
                }
                else if (condition.Type == GameConditionTypes.ResourceAmount)
                {
                    if (!snapshot.Resources.ContainsKey(condition.Id)) throw new InvalidOperationException("Demo completion references unknown resource: " + condition.Id);
                }
                else
                {
                    throw new InvalidOperationException("Demo completion condition has unknown type: " + condition.Type);
                }
            }
        }

        private static bool IsDemoCompletionCountOnlyCondition(string type)
        {
            return type == GameConditionTypes.SurvivorCount || type == GameConditionTypes.ExpeditionsCompleted;
        }

        private static void ValidateWorkshop(GameConfigSnapshot snapshot)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.Balance.WorkshopRequiredBuildingId) &&
                !snapshot.Buildings.ContainsKey(snapshot.Balance.WorkshopRequiredBuildingId))
            {
                throw new InvalidOperationException("Workshop references unknown building: " + snapshot.Balance.WorkshopRequiredBuildingId);
            }

            if (!string.IsNullOrWhiteSpace(snapshot.Balance.WorkshopRepairResourceId) &&
                !snapshot.Resources.ContainsKey(snapshot.Balance.WorkshopRepairResourceId))
            {
                throw new InvalidOperationException("Workshop repair references unknown resource: " + snapshot.Balance.WorkshopRepairResourceId);
            }

            if (snapshot.Balance.WorkshopRequiredBuildingLevel < 0) throw new InvalidOperationException("Workshop building level cannot be negative.");
            if (snapshot.Balance.WorkshopRepairDurabilityBlock <= 0) throw new InvalidOperationException("Workshop repair durability block must be positive.");
        }

        private static void ValidateHealing(GameConfigSnapshot snapshot)
        {
            if (!string.IsNullOrWhiteSpace(snapshot.Balance.HealingRequiredBuildingId) &&
                !snapshot.Buildings.ContainsKey(snapshot.Balance.HealingRequiredBuildingId))
            {
                throw new InvalidOperationException("Healing references unknown building: " + snapshot.Balance.HealingRequiredBuildingId);
            }

            if (snapshot.Balance.HealingRequiredBuildingLevel < 0) throw new InvalidOperationException("Healing building level cannot be negative.");
            if (string.IsNullOrWhiteSpace(snapshot.Balance.HealingDefaultWoundId)) throw new InvalidOperationException("Healing wound id cannot be empty.");
            if (snapshot.Balance.HealingDefaultWoundDurationSeconds <= 0) throw new InvalidOperationException("Healing wound duration must be positive.");
            if (snapshot.Balance.HealingHealthOnWounded <= 0) throw new InvalidOperationException("Healing wounded health must be positive.");
            if (!string.IsNullOrWhiteSpace(snapshot.Balance.HealingMedicineResourceId) &&
                !snapshot.Resources.ContainsKey(snapshot.Balance.HealingMedicineResourceId))
            {
                throw new InvalidOperationException("Healing medicine references unknown resource: " + snapshot.Balance.HealingMedicineResourceId);
            }

            if (snapshot.Balance.HealingMedicineCost < 0) throw new InvalidOperationException("Healing medicine cost cannot be negative.");
            if (snapshot.Balance.HealingMedicineSeconds <= 0) throw new InvalidOperationException("Healing medicine seconds must be positive.");
        }

        private static void ValidateEmergencyScavenge(GameConfigSnapshot snapshot)
        {
            if (snapshot.Balance.EmergencyScavengeDurationSeconds <= 0) throw new InvalidOperationException("Emergency scavenge duration must be positive.");
            if (snapshot.Balance.EmergencyScavengeCooldownSeconds < 0) throw new InvalidOperationException("Emergency scavenge cooldown cannot be negative.");
            if (snapshot.Balance.EmergencyScavengeRewards.Count == 0) throw new InvalidOperationException("Emergency scavenge rewards cannot be empty.");
            foreach (var reward in snapshot.Balance.EmergencyScavengeRewards)
            {
                if (!snapshot.Resources.ContainsKey(reward.Key)) throw new InvalidOperationException("Emergency scavenge reward references unknown resource: " + reward.Key);
                if (reward.Value <= 0) throw new InvalidOperationException("Emergency scavenge reward must be positive: " + reward.Key);
            }
        }

        private static void ValidateEnemies(GameConfigSnapshot snapshot)
        {
            foreach (var enemy in snapshot.Enemies.Values)
            {
                if (enemy.MaxHealth <= 0) throw new InvalidOperationException("Enemy health must be positive: " + enemy.Id);
                if (enemy.BaseDamage < 0) throw new InvalidOperationException("Enemy damage cannot be negative: " + enemy.Id);
                if (enemy.Armor < 0) throw new InvalidOperationException("Enemy armor cannot be negative: " + enemy.Id);
                if (enemy.Evasion < 0 || enemy.Evasion > 1) throw new InvalidOperationException("Enemy evasion must be 0..1: " + enemy.Id);
                if (enemy.Accuracy < 0 || enemy.Accuracy > 1) throw new InvalidOperationException("Enemy accuracy must be 0..1: " + enemy.Id);
            }
        }

        private static void ValidateItems(GameConfigSnapshot snapshot)
        {
            foreach (var item in snapshot.Items.Values)
            {
                if (item.BaseDamage < 0) throw new InvalidOperationException("Item damage cannot be negative: " + item.Id);
                if (item.Armor < 0) throw new InvalidOperationException("Item armor cannot be negative: " + item.Id);
                if (item.MaxDurability <= 0) throw new InvalidOperationException("Item durability must be positive: " + item.Id);
                if (item.RepairCostMultiplier < 0) throw new InvalidOperationException("Item repair cost multiplier cannot be negative: " + item.Id);
            }
        }

        private static void ValidateWeapons(GameConfigSnapshot snapshot)
        {
            foreach (var weapon in snapshot.Weapons.Values)
            {
                if (string.IsNullOrWhiteSpace(weapon.Name)) throw new InvalidOperationException("Weapon name cannot be empty: " + weapon.Id);
                if (weapon.Attack <= 0) throw new InvalidOperationException("Weapon attack must be positive: " + weapon.Id);
                if (weapon.AttacksPerTurn <= 0) throw new InvalidOperationException("Weapon attacks per turn must be positive: " + weapon.Id);
                if (weapon.TargetCount < 1 || weapon.TargetCount > 4) throw new InvalidOperationException("Weapon target count must be 1..4: " + weapon.Id);
                if (weapon.HitChance < 0 || weapon.HitChance > 1) throw new InvalidOperationException("Weapon hit chance must be 0..1: " + weapon.Id);
                if (weapon.ArmorPenetration < 0 || weapon.ArmorPenetration > 1) throw new InvalidOperationException("Weapon armor penetration must be 0..1: " + weapon.Id);
                if (weapon.CriticalChance < 0 || weapon.CriticalChance > 1) throw new InvalidOperationException("Weapon critical chance must be 0..1: " + weapon.Id);
                if (weapon.AmmoCostPerAttack < 0) throw new InvalidOperationException("Weapon ammo cost cannot be negative: " + weapon.Id);
                if (weapon.NoisePerAttack < 0) throw new InvalidOperationException("Weapon noise cannot be negative: " + weapon.Id);
                if (weapon.MaxDurability <= 0) throw new InvalidOperationException("Weapon durability must be positive: " + weapon.Id);
                if (weapon.RepairCostMultiplier < 0) throw new InvalidOperationException("Weapon repair cost multiplier cannot be negative: " + weapon.Id);
                if (weapon.Type == WeaponCombatType.Melee && weapon.TargetingRule != WeaponTargetingRule.FrontlineOnly)
                {
                    throw new InvalidOperationException("Melee weapon must target frontline only: " + weapon.Id);
                }

                if (weapon.Type == WeaponCombatType.Ranged && weapon.TargetingRule != WeaponTargetingRule.AnyEnemy)
                {
                    throw new InvalidOperationException("Ranged weapon must be able to target any enemy: " + weapon.Id);
                }

                if (weapon.Type == WeaponCombatType.Explosive && weapon.TargetingRule != WeaponTargetingRule.AreaAnyEnemies)
                {
                    throw new InvalidOperationException("Explosive weapon must use area targeting: " + weapon.Id);
                }
            }
        }

        private static void ValidateBuildings(GameConfigSnapshot snapshot)
        {
            foreach (var building in snapshot.Buildings.Values)
            {
                if (!string.IsNullOrWhiteSpace(building.AffectedResourceId) && !snapshot.Resources.ContainsKey(building.AffectedResourceId))
                {
                    throw new InvalidOperationException("Building affects unknown resource: " + building.Id);
                }

                if (!string.IsNullOrWhiteSpace(building.ProducedResourceId) && !snapshot.Resources.ContainsKey(building.ProducedResourceId))
                {
                    throw new InvalidOperationException("Building produces unknown resource: " + building.Id);
                }

                if (building.Levels.Count == 0) throw new InvalidOperationException("Building has no levels: " + building.Id);
                var levels = new HashSet<int>();
                foreach (var level in building.Levels)
                {
                    if (level.Level < 0) throw new InvalidOperationException("Building level cannot be negative: " + building.Id);
                    if (!levels.Add(level.Level)) throw new InvalidOperationException("Building has duplicate level: " + building.Id + "/" + level.Level);
                    if (level.SurvivorCap < 0 || level.SquadSize < 0 || level.ResourceCap < 0 || level.ResourcePerMinute < 0) throw new InvalidOperationException("Building level values cannot be negative: " + building.Id);
                    foreach (var cost in level.Cost)
                    {
                        if (!snapshot.Resources.ContainsKey(cost.Key)) throw new InvalidOperationException("Building cost references unknown resource: " + cost.Key);
                        if (cost.Value < 0) throw new InvalidOperationException("Building cost cannot be negative: " + building.Id);
                    }
                }

                if (BuildingSystem.GetLevel(building, building.StartingLevel) == null)
                {
                    throw new InvalidOperationException("Building starting level is missing: " + building.Id);
                }
            }
        }

        private static void RequireAny<T>(Dictionary<string, T> items, string label)
        {
            if (items.Count == 0) throw new InvalidOperationException("Config catalog is empty: " + label);
            foreach (var key in items.Keys)
            {
                if (string.IsNullOrWhiteSpace(key)) throw new InvalidOperationException("Config catalog has empty id: " + label);
            }
        }
    }
}
