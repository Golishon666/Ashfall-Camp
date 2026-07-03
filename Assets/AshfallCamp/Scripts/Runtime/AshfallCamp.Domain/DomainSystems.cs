using System;
using System.Collections.Generic;

namespace AshfallCamp.Domain
{
    public static class GameMath
    {
        public static double Clamp(double value, double min, double max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }

        public static int Clamp(int value, int min, int max)
        {
            if (value < min) return min;
            if (value > max) return max;
            return value;
        }
    }

    public struct SeededRandom
    {
        private uint _state;

        public SeededRandom(uint seed)
        {
            _state = seed == 0 ? 2463534242u : seed;
        }

        public uint State { get { return _state; } }

        public int RangeInclusive(int min, int max)
        {
            if (max <= min) return min;
            return min + (int)(NextUInt() % (uint)(max - min + 1));
        }

        public double Range(double min, double max)
        {
            return min + (NextDouble() * (max - min));
        }

        public double NextDouble()
        {
            return NextUInt() / (double)uint.MaxValue;
        }

        private uint NextUInt()
        {
            var x = _state;
            x ^= x << 13;
            x ^= x >> 17;
            x ^= x << 5;
            _state = x == 0 ? 2463534242u : x;
            return _state;
        }
    }

    public static class GameStateFactory
    {
        private static readonly string[] SkillIds =
        {
            "scavenging", "melee", "firearms", "survival", "mechanics", "medicine"
        };

        public static GameState CreateNew(GameConfigSnapshot config, long nowUnixMs)
        {
            var state = new GameState
            {
                CreatedAtUnixMs = nowUnixMs,
                LastSaveAtUnixMs = nowUnixMs,
                Version = GameConstants.CurrentSaveVersion
            };

            foreach (var resource in config.Resources.Values)
            {
                state.Resources[resource.Id] = Math.Max(0, resource.StartAmount);
                if (resource.HasCap)
                {
                    state.ResourceCaps[resource.Id] = Math.Max(0, resource.StartCap);
                }
            }

            foreach (var building in config.Buildings.Values)
            {
                state.Buildings[building.Id] = new BuildingState
                {
                    Id = building.Id,
                    Level = building.StartingLevel,
                    IsUnlocked = building.StartsUnlocked
                };
            }

            foreach (var zone in config.Zones.Values)
            {
                state.Zones[zone.Id] = new ZoneState
                {
                    Id = zone.Id,
                    IsUnlocked = zone.UnlockConditions.Count == 0
                };
            }

            BuildingSystem.ApplyAllBuildingEffects(state, config);
            UnlockSystem.RefreshZoneUnlocks(state, config);

            var weapon = CreateItemState(config.StartingSurvivor.WeaponItemId, config, "item_1");
            state.Inventory.Add(weapon);

            var survivor = new SurvivorState
            {
                Id = "survivor_1",
                Name = config.StartingSurvivor.Name,
                BackgroundId = config.StartingSurvivor.BackgroundId,
                TraitIds = new List<string>(config.StartingSurvivor.TraitIds),
                Health = 30,
                MaxHealth = 30,
                Morale = 50
            };

            foreach (var skillId in SkillIds)
            {
                survivor.Skills[skillId] = 0;
                survivor.SkillXp[skillId] = 0;
            }

            foreach (var pair in config.StartingSurvivor.Skills)
            {
                survivor.Skills[pair.Key] = pair.Value;
            }

            ApplyBackground(survivor, config);
            ApplyTraits(survivor, config);
            survivor.Equipment.WeaponItemUid = weapon.Uid;
            weapon.EquippedBySurvivorId = survivor.Id;
            state.Survivors.Add(survivor);
            state.NextId = 2;
            return state;
        }

        private static InventoryItemState CreateItemState(string itemId, GameConfigSnapshot config, string uid)
        {
            ItemDefinition item;
            if (!config.Items.TryGetValue(itemId, out item))
            {
                item = new ItemDefinition { Id = itemId, MaxDurability = 1 };
            }

            return new InventoryItemState
            {
                Uid = uid,
                ItemId = itemId,
                MaxDurability = Math.Max(1, item.MaxDurability),
                Durability = Math.Max(1, item.MaxDurability)
            };
        }

        private static void ApplyBackground(SurvivorState survivor, GameConfigSnapshot config)
        {
            BackgroundDefinition background;
            if (!config.Backgrounds.TryGetValue(survivor.BackgroundId, out background)) return;
            foreach (var pair in background.SkillBonuses)
            {
                var current = survivor.Skills.ContainsKey(pair.Key) ? survivor.Skills[pair.Key] : 0;
                survivor.Skills[pair.Key] = current + pair.Value;
            }

            ApplyStats(survivor, background.StatBonuses);
        }

        private static void ApplyTraits(SurvivorState survivor, GameConfigSnapshot config)
        {
            foreach (var traitId in survivor.TraitIds)
            {
                TraitDefinition trait;
                if (config.Traits.TryGetValue(traitId, out trait))
                {
                    ApplyStats(survivor, trait.StatModifiers);
                }
            }
        }

        private static void ApplyStats(SurvivorState survivor, Dictionary<string, int> stats)
        {
            foreach (var pair in stats)
            {
                if (pair.Key == "max_health")
                {
                    survivor.MaxHealth += pair.Value;
                    survivor.Health += pair.Value;
                }
                else if (pair.Key == "morale" || pair.Key == "combat_morale")
                {
                    survivor.Morale += pair.Value;
                }
            }
        }
    }

    public static class ResourceSystem
    {
        public static bool CanSpend(GameState state, Dictionary<string, int> cost)
        {
            foreach (var pair in cost)
            {
                if (GetAmount(state, pair.Key) < pair.Value) return false;
            }
            return true;
        }

        public static bool TrySpend(GameState state, Dictionary<string, int> cost)
        {
            if (!CanSpend(state, cost)) return false;
            foreach (var pair in cost)
            {
                state.Resources[pair.Key] = GetAmount(state, pair.Key) - pair.Value;
            }
            return true;
        }

        public static void Add(GameState state, string resourceId, int amount)
        {
            if (amount <= 0) return;
            var current = GetAmount(state, resourceId);
            int cap;
            var next = current + amount;
            if (state.ResourceCaps.TryGetValue(resourceId, out cap))
            {
                next = Math.Min(next, cap);
            }
            state.Resources[resourceId] = next;
            if (!state.Statistics.TotalResourcesGained.ContainsKey(resourceId))
            {
                state.Statistics.TotalResourcesGained[resourceId] = 0;
            }
            state.Statistics.TotalResourcesGained[resourceId] += Math.Max(0, next - current);
        }

        public static int GetAmount(GameState state, string resourceId)
        {
            int amount;
            return state.Resources.TryGetValue(resourceId, out amount) ? amount : 0;
        }
    }

    public static class BuildingSystem
    {
        public static ValidationResult ValidateUpgrade(GameState state, GameConfigSnapshot config, string buildingId)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(buildingId))
            {
                result.Errors.Add("Building id is required.");
                return result;
            }

            BuildingDefinition definition;
            if (!config.Buildings.TryGetValue(buildingId, out definition))
            {
                result.Errors.Add("Unknown building.");
                return result;
            }

            BuildingState building;
            if (!state.Buildings.TryGetValue(buildingId, out building))
            {
                result.Errors.Add("Building state is missing.");
                return result;
            }

            if (!building.IsUnlocked)
            {
                result.Errors.Add("Building is locked.");
            }

            var nextLevel = GetLevel(definition, building.Level + 1);
            if (nextLevel == null)
            {
                result.Errors.Add("Building is already at max level.");
                return result;
            }

            if (!ResourceSystem.CanSpend(state, nextLevel.Cost))
            {
                result.Errors.Add("Not enough resources.");
            }

            return result;
        }

        public static BuildingUpgradeResult Upgrade(GameState state, GameConfigSnapshot config, string buildingId)
        {
            var validation = ValidateUpgrade(state, config, buildingId);
            var result = new BuildingUpgradeResult { Validation = validation };
            if (!validation.IsValid)
            {
                return result;
            }

            var definition = config.Buildings[buildingId];
            var building = state.Buildings[buildingId];
            var nextLevel = GetLevel(definition, building.Level + 1);
            ResourceSystem.TrySpend(state, nextLevel.Cost);
            building.Level = nextLevel.Level;
            ApplyAllBuildingEffects(state, config);
            UnlockSystem.RefreshZoneUnlocks(state, config);
            result.Building = building;
            return result;
        }

        public static void ApplyAllBuildingEffects(GameState state, GameConfigSnapshot config)
        {
            if (state.ResourceCaps == null)
            {
                state.ResourceCaps = new Dictionary<string, int>(StringComparer.Ordinal);
            }

            state.SurvivorCap = 1;
            state.SquadSize = 1;

            foreach (var resource in config.Resources.Values)
            {
                if (resource.HasCap)
                {
                    state.ResourceCaps[resource.Id] = Math.Max(0, resource.StartCap);
                }
                else
                {
                    state.ResourceCaps.Remove(resource.Id);
                }
            }

            foreach (var building in state.Buildings.Values)
            {
                if (!building.IsUnlocked) continue;
                BuildingDefinition definition;
                if (!config.Buildings.TryGetValue(building.Id, out definition)) continue;
                var level = GetLevel(definition, building.Level);
                if (level == null) continue;

                if (level.SurvivorCap > 0)
                {
                    state.SurvivorCap = Math.Max(state.SurvivorCap, level.SurvivorCap);
                }

                if (level.SquadSize > 0)
                {
                    state.SquadSize = Math.Max(state.SquadSize, level.SquadSize);
                }

                if (!string.IsNullOrWhiteSpace(definition.AffectedResourceId) && level.ResourceCap > 0)
                {
                    int currentCap;
                    state.ResourceCaps.TryGetValue(definition.AffectedResourceId, out currentCap);
                    state.ResourceCaps[definition.AffectedResourceId] = Math.Max(currentCap, level.ResourceCap);
                }
            }

            ClampResourcesToCaps(state);
        }

        public static void TickProduction(GameState state, GameConfigSnapshot config, double deltaSeconds)
        {
            if (deltaSeconds <= 0) return;
            if (state.ResourceProductionRemainders == null)
            {
                state.ResourceProductionRemainders = new Dictionary<string, double>(StringComparer.Ordinal);
            }

            foreach (var building in state.Buildings.Values)
            {
                if (!building.IsUnlocked) continue;
                BuildingDefinition definition;
                if (!config.Buildings.TryGetValue(building.Id, out definition)) continue;
                if (string.IsNullOrWhiteSpace(definition.ProducedResourceId)) continue;

                var level = GetLevel(definition, building.Level);
                if (level == null || level.ResourcePerMinute <= 0) continue;

                var key = building.Id + ":" + definition.ProducedResourceId;
                double remainder;
                state.ResourceProductionRemainders.TryGetValue(key, out remainder);
                var produced = remainder + level.ResourcePerMinute * deltaSeconds / 60.0;
                var whole = (int)Math.Floor(produced);
                state.ResourceProductionRemainders[key] = produced - whole;
                if (whole > 0)
                {
                    ResourceSystem.Add(state, definition.ProducedResourceId, whole);
                }
            }
        }

        public static BuildingLevelDefinition GetLevel(BuildingDefinition definition, int level)
        {
            for (var i = 0; i < definition.Levels.Count; i++)
            {
                if (definition.Levels[i].Level == level) return definition.Levels[i];
            }

            return null;
        }

        private static void ClampResourcesToCaps(GameState state)
        {
            foreach (var cap in state.ResourceCaps)
            {
                int amount;
                if (state.Resources.TryGetValue(cap.Key, out amount) && amount > cap.Value)
                {
                    state.Resources[cap.Key] = cap.Value;
                }
            }
        }
    }

    public static class UnlockSystem
    {
        public static void RefreshZoneUnlocks(GameState state, GameConfigSnapshot config)
        {
            foreach (var zone in config.Zones.Values)
            {
                ZoneState zoneState;
                if (!state.Zones.TryGetValue(zone.Id, out zoneState))
                {
                    continue;
                }

                if (zone.UnlockConditions.Count == 0)
                {
                    zoneState.IsUnlocked = true;
                    continue;
                }

                var unlocked = true;
                foreach (var condition in zone.UnlockConditions)
                {
                    if (condition.Type == "zone_completions")
                    {
                        ZoneState requiredZone;
                        if (!state.Zones.TryGetValue(condition.Id, out requiredZone) || requiredZone.Completions < condition.Value) unlocked = false;
                    }
                    else if (condition.Type == "building_level")
                    {
                        BuildingState building;
                        if (!state.Buildings.TryGetValue(condition.Id, out building) || building.Level < condition.Value) unlocked = false;
                    }
                    else
                    {
                        unlocked = false;
                    }
                }

                if (unlocked)
                {
                    zoneState.IsUnlocked = true;
                }
            }
        }
    }

    public static class ExpeditionValidator
    {
        public static ValidationResult Validate(GameState state, GameConfigSnapshot config, LaunchExpeditionRequest request)
        {
            var result = new ValidationResult();
            ZoneDefinition zone;
            if (!config.Zones.TryGetValue(request.ZoneId, out zone))
            {
                result.Errors.Add("Unknown zone.");
                return result;
            }

            ZoneState zoneState;
            if (!state.Zones.TryGetValue(request.ZoneId, out zoneState) || !zoneState.IsUnlocked)
            {
                result.Errors.Add("Zone is locked.");
            }

            if (request.SurvivorIds.Count == 0)
            {
                result.Errors.Add("Select at least one survivor.");
            }

            var maxSquad = GetMaxSquadSize(state);
            if (request.SurvivorIds.Count > maxSquad)
            {
                result.Errors.Add("Squad is larger than current camp limit.");
            }

            foreach (var survivorId in request.SurvivorIds)
            {
                var survivor = FindSurvivor(state, survivorId);
                if (survivor == null)
                {
                    result.Errors.Add("Unknown survivor.");
                    continue;
                }

                if (survivor.State != SurvivorActivityState.Idle)
                {
                    result.Errors.Add("All survivors must be idle.");
                }
            }

            foreach (var requirement in zone.RequiredBuildingLevels)
            {
                BuildingState building;
                if (!state.Buildings.TryGetValue(requirement.Key, out building) || building.Level < requirement.Value)
                {
                    result.Errors.Add("Required building level is missing.");
                }
            }

            var cost = CalculateCost(zone, GetPolicy(config, request.PolicyId), request.SurvivorIds.Count);
            if (!ResourceSystem.CanSpend(state, cost))
            {
                result.Errors.Add("Not enough food or water.");
            }

            var power = SquadPowerSystem.CalculateSquadPower(state, config, request.SurvivorIds, request.PolicyId);
            if (zone.RecommendedPower > 0)
            {
                var ratio = power / zone.RecommendedPower;
                if (ratio < 0.75)
                {
                    result.Warnings.Add("Extreme risk.");
                }
                else if (ratio < 1.0)
                {
                    result.Warnings.Add("High risk.");
                }
            }

            return result;
        }

        public static Dictionary<string, int> CalculateCost(ZoneDefinition zone, ExpeditionPolicyDefinition policy, int survivorCount)
        {
            var cost = new Dictionary<string, int>(StringComparer.Ordinal);
            var food = (int)Math.Ceiling(zone.FoodCostPerSurvivor * survivorCount * policy.FoodModifier);
            var water = (int)Math.Ceiling(zone.WaterCostPerSurvivor * survivorCount * policy.WaterModifier);
            if (food > 0) cost["food"] = food;
            if (water > 0) cost["water"] = water;
            return cost;
        }

        private static int GetMaxSquadSize(GameState state)
        {
            return Math.Max(1, state.SquadSize);
        }

        private static ExpeditionPolicyDefinition GetPolicy(GameConfigSnapshot config, string policyId)
        {
            ExpeditionPolicyDefinition policy;
            return config.Policies.TryGetValue(policyId, out policy) ? policy : new ExpeditionPolicyDefinition();
        }

        internal static SurvivorState FindSurvivor(GameState state, string survivorId)
        {
            for (var i = 0; i < state.Survivors.Count; i++)
            {
                if (state.Survivors[i].Id == survivorId) return state.Survivors[i];
            }
            return null;
        }
    }

    public static class ExpeditionLauncher
    {
        public static LaunchExpeditionResult Launch(GameState state, GameConfigSnapshot config, LaunchExpeditionRequest request)
        {
            var validation = ExpeditionValidator.Validate(state, config, request);
            var result = new LaunchExpeditionResult { Validation = validation };
            if (!validation.IsValid || (validation.Warnings.Count > 0 && !request.ConfirmWarnings))
            {
                return result;
            }

            var zone = config.Zones[request.ZoneId];
            var policy = config.Policies.ContainsKey(request.PolicyId) ? config.Policies[request.PolicyId] : new ExpeditionPolicyDefinition();
            ResourceSystem.TrySpend(state, ExpeditionValidator.CalculateCost(zone, policy, request.SurvivorIds.Count));

            var expedition = new ExpeditionState
            {
                Id = "expedition_" + state.NextId++,
                ZoneId = request.ZoneId,
                SurvivorIds = new List<string>(request.SurvivorIds),
                PolicyId = request.PolicyId,
                StartedAtUnixMs = request.NowUnixMs,
                ExpectedDurationSeconds = CalculateDuration(state, config, zone, policy, request.SurvivorIds),
                RandomState = request.Seed == 0 ? (uint)state.NextId * 7919u : request.Seed,
                Status = ExpeditionStatus.Active
            };

            foreach (var survivorId in request.SurvivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor == null) continue;
                survivor.State = SurvivorActivityState.OnExpedition;
                survivor.CurrentExpeditionId = expedition.Id;
            }

            state.Expeditions.Add(expedition);
            result.Expedition = expedition;
            return result;
        }

        private static double CalculateDuration(GameState state, GameConfigSnapshot config, ZoneDefinition zone, ExpeditionPolicyDefinition policy, List<string> survivorIds)
        {
            var averageSurvival = AverageSkill(state, survivorIds, "survival");
            ZoneState zoneState;
            var familiarity = state.Zones.TryGetValue(zone.Id, out zoneState) ? zoneState.Familiarity : 0;
            var survivalBonus = averageSurvival * 0.003;
            var familiarityBonus = familiarity * 0.002;
            var duration = zone.BaseDurationSeconds * policy.DurationModifier * (1 - survivalBonus - familiarityBonus);
            return GameMath.Clamp(duration, zone.MinDurationSeconds, zone.MaxDurationSeconds);
        }

        private static double AverageSkill(GameState state, List<string> survivorIds, string skillId)
        {
            if (survivorIds.Count == 0) return 0;
            var total = 0;
            foreach (var survivorId in survivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor == null) continue;
                total += survivor.Skills.ContainsKey(skillId) ? survivor.Skills[skillId] : 0;
            }
            return total / (double)survivorIds.Count;
        }
    }

    public static class SquadPowerSystem
    {
        public static double CalculateSquadPower(GameState state, GameConfigSnapshot config, List<string> survivorIds, string policyId)
        {
            var total = 0.0;
            foreach (var survivorId in survivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor != null) total += CalculateSurvivorPower(state, config, survivor);
            }

            ExpeditionPolicyDefinition policy;
            var modifier = config.Policies.TryGetValue(policyId, out policy) ? policy.PowerModifier : 1.0;
            return total * modifier;
        }

        public static double CalculateSurvivorPower(GameState state, GameConfigSnapshot config, SurvivorState survivor)
        {
            var weapon = GetEquippedItem(state, config, survivor.Equipment.WeaponItemUid);
            var armor = GetEquippedItem(state, config, survivor.Equipment.ArmorItemUid);
            var weaponDamage = weapon != null ? weapon.BaseDamage : 1;
            var armorValue = armor != null ? armor.Armor : 0;
            var melee = survivor.Skills.ContainsKey("melee") ? survivor.Skills["melee"] : 0;
            var firearms = survivor.Skills.ContainsKey("firearms") ? survivor.Skills["firearms"] : 0;
            var fatiguePenalty = survivor.Fatigue >= 75 ? 5 : survivor.Fatigue >= 50 ? 2 : 0;
            return survivor.MaxHealth * 0.15 + melee * 0.7 + firearms * 0.7 + weaponDamage * 2 + armorValue * 4 + survivor.Level * 2 - fatiguePenalty;
        }

        public static ItemDefinition GetEquippedItem(GameState state, GameConfigSnapshot config, string itemUid)
        {
            if (string.IsNullOrEmpty(itemUid)) return null;
            for (var i = 0; i < state.Inventory.Count; i++)
            {
                if (state.Inventory[i].Uid != itemUid) continue;
                ItemDefinition definition;
                return config.Items.TryGetValue(state.Inventory[i].ItemId, out definition) ? definition : null;
            }

            return null;
        }
    }

    public static class CombatResolver
    {
        public static double CalculateHitChance(double baseAccuracy, int skillLevel, double weaponAccuracyBonus, double targetEvasion, double min, double max)
        {
            return GameMath.Clamp(baseAccuracy + skillLevel * 0.003 + weaponAccuracyBonus - targetEvasion, min, max);
        }

        public static int CalculateDamage(int weaponBaseDamage, int skillLevel, int level, double critMultiplier, bool isCrit, int armor, ref SeededRandom rng)
        {
            var skillMultiplier = 1 + skillLevel * 0.006;
            var levelMultiplier = 1 + level * 0.015;
            var randomMultiplier = rng.Range(0.85, 1.15);
            var raw = (weaponBaseDamage + 1) * skillMultiplier * levelMultiplier * randomMultiplier;
            if (isCrit) raw *= critMultiplier;
            return Math.Max(1, (int)Math.Floor(raw - armor));
        }

        public static bool ResolveCombat(GameState state, GameConfigSnapshot config, ExpeditionState expedition, string enemyId)
        {
            EnemyDefinition enemy;
            if (!config.Enemies.TryGetValue(enemyId, out enemy)) return true;
            var rng = new SeededRandom(expedition.RandomState);
            var enemyHp = enemy.MaxHealth;
            var survivorHp = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var survivorId in expedition.SurvivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor != null) survivorHp[survivorId] = Math.Max(1, survivor.Health);
            }

            for (var round = 0; round < 20 && enemyHp > 0 && AnyAlive(survivorHp); round++)
            {
                foreach (var survivorId in expedition.SurvivorIds)
                {
                    if (!survivorHp.ContainsKey(survivorId) || survivorHp[survivorId] <= 0 || enemyHp <= 0) continue;
                    var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                    if (survivor == null) continue;
                    var weapon = SquadPowerSystem.GetEquippedItem(state, config, survivor.Equipment.WeaponItemUid) ?? new ItemDefinition { WeaponType = WeaponType.Melee, BaseDamage = 1 };
                    var skillId = weapon.WeaponType == WeaponType.Firearm ? "firearms" : "melee";
                    var skill = survivor.Skills.ContainsKey(skillId) ? survivor.Skills[skillId] : 0;
                    var baseAccuracy = weapon.WeaponType == WeaponType.Firearm ? config.Balance.BaseAccuracyFirearms : config.Balance.BaseAccuracyMelee;
                    var hitChance = CalculateHitChance(baseAccuracy, skill, weapon.AccuracyBonus, enemy.Evasion, config.Balance.MinHitChance, config.Balance.MaxHitChance);
                    expedition.Noise += weapon.NoisePerAttack;
                    if (rng.NextDouble() <= hitChance)
                    {
                        var critChance = GameMath.Clamp(config.Balance.BaseCritChance + skill * 0.001 + weapon.CritBonus, 0.02, 0.35);
                        var isCrit = rng.NextDouble() <= critChance;
                        var damage = CalculateDamage(Math.Max(1, weapon.BaseDamage), skill, survivor.Level, config.Balance.CritMultiplier, isCrit, enemy.Armor, ref rng);
                        enemyHp -= damage;
                        expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = expedition.ElapsedSeconds, Message = survivor.Name + " hits " + enemy.Name + " for " + damage + " damage." });
                    }
                    else
                    {
                        expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = expedition.ElapsedSeconds, Message = survivor.Name + " misses " + enemy.Name + "." });
                    }
                }

                if (enemyHp <= 0) break;
                var targetId = FirstAlive(survivorHp);
                if (targetId.Length == 0) break;
                var target = ExpeditionValidator.FindSurvivor(state, targetId);
                var enemyHit = CalculateHitChance(enemy.Accuracy, 0, 0, 0, config.Balance.MinHitChance, config.Balance.MaxHitChance);
                if (rng.NextDouble() <= enemyHit)
                {
                    survivorHp[targetId] -= Math.Max(1, enemy.BaseDamage);
                    expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = expedition.ElapsedSeconds, Message = enemy.Name + " hits " + (target != null ? target.Name : targetId) + "." });
                    if (survivorHp[targetId] <= 0 && !expedition.WoundedSurvivorIds.Contains(targetId))
                    {
                        expedition.WoundedSurvivorIds.Add(targetId);
                    }
                }
            }

            expedition.RandomState = rng.State;
            if (enemyHp <= 0)
            {
                if (!expedition.EnemiesDefeated.ContainsKey(enemy.Id)) expedition.EnemiesDefeated[enemy.Id] = 0;
                expedition.EnemiesDefeated[enemy.Id]++;
                foreach (var survivorId in expedition.SurvivorIds)
                {
                    var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                    if (survivor != null) survivor.Xp += enemy.XpReward;
                }
                state.Statistics.CombatsWon++;
                return true;
            }

            state.Statistics.CombatsLost++;
            return false;
        }

        private static bool AnyAlive(Dictionary<string, int> hp)
        {
            foreach (var pair in hp)
            {
                if (pair.Value > 0) return true;
            }
            return false;
        }

        private static string FirstAlive(Dictionary<string, int> hp)
        {
            foreach (var pair in hp)
            {
                if (pair.Value > 0) return pair.Key;
            }
            return string.Empty;
        }
    }

    public static class ExpeditionSimulator
    {
        public static void TickAll(GameState state, GameConfigSnapshot config, double deltaSeconds)
        {
            for (var i = 0; i < state.Expeditions.Count; i++)
            {
                if (state.Expeditions[i].Status == ExpeditionStatus.Active)
                {
                    Tick(state, config, state.Expeditions[i], deltaSeconds);
                }
            }
        }

        public static void Tick(GameState state, GameConfigSnapshot config, ExpeditionState expedition, double deltaSeconds)
        {
            if (expedition.Status != ExpeditionStatus.Active) return;
            ZoneDefinition zone;
            if (!config.Zones.TryGetValue(expedition.ZoneId, out zone))
            {
                expedition.Status = ExpeditionStatus.Failed;
                return;
            }

            expedition.ElapsedSeconds += Math.Max(0, deltaSeconds);
            expedition.Progress = GameMath.Clamp(expedition.ElapsedSeconds / Math.Max(1, expedition.ExpectedDurationSeconds) * 100, 0, 100);
            expedition.StepAccumulatorSeconds += Math.Max(0, deltaSeconds);

            while (expedition.StepAccumulatorSeconds >= config.Balance.ExpeditionStepSeconds && expedition.Status == ExpeditionStatus.Active)
            {
                expedition.StepAccumulatorSeconds -= config.Balance.ExpeditionStepSeconds;
                ResolveStep(state, config, expedition, zone);
            }

            if (expedition.Progress >= 100 && expedition.Status == ExpeditionStatus.Active)
            {
                Complete(state, config, expedition);
            }
        }

        private static void ResolveStep(GameState state, GameConfigSnapshot config, ExpeditionState expedition, ZoneDefinition zone)
        {
            var rng = new SeededRandom(expedition.RandomState);
            var roll = rng.RangeInclusive(0, 99);
            expedition.RandomState = rng.State;
            if (roll < 35 && zone.EnemyTable.Count > 0)
            {
                var enemyId = PickWeighted(zone.EnemyTable, ref rng);
                expedition.RandomState = rng.State;
                if (!CombatResolver.ResolveCombat(state, config, expedition, enemyId))
                {
                    expedition.Status = ExpeditionStatus.Failed;
                    Fail(state, expedition);
                }
            }
            else if (roll < 70 && zone.LootTable.Count > 0)
            {
                RollLoot(state, config, expedition, zone, ref rng);
                expedition.RandomState = rng.State;
            }
            else if (roll < 85)
            {
                expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = expedition.ElapsedSeconds, Message = "The squad handles a small obstacle." });
                foreach (var survivorId in expedition.SurvivorIds)
                {
                    var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                    if (survivor != null) survivor.Xp += 1;
                }
            }
            else
            {
                expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = expedition.ElapsedSeconds, Message = "Quiet travel through the ash." });
            }
        }

        private static void RollLoot(GameState state, GameConfigSnapshot config, ExpeditionState expedition, ZoneDefinition zone, ref SeededRandom rng)
        {
            var entry = PickWeightedLoot(zone.LootTable, ref rng);
            if (entry == null) return;
            var averageScavenging = AverageSkill(state, expedition.SurvivorIds, "scavenging");
            ZoneState zoneState;
            var familiarity = state.Zones.TryGetValue(zone.Id, out zoneState) ? zoneState.Familiarity : 0;
            ExpeditionPolicyDefinition policy;
            var policyBonus = config.Policies.TryGetValue(expedition.PolicyId, out policy) ? policy.LootModifier : 1.0;
            var amount = (int)Math.Floor(rng.RangeInclusive(entry.Min, entry.Max) * (1 + averageScavenging * 0.015) * policyBonus * (1 + familiarity * 0.003));
            amount = Math.Max(1, amount);
            if (!expedition.AccumulatedLoot.ContainsKey(entry.ResourceId)) expedition.AccumulatedLoot[entry.ResourceId] = 0;
            expedition.AccumulatedLoot[entry.ResourceId] += amount;
            expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = expedition.ElapsedSeconds, Message = "Found " + amount + " " + entry.ResourceId + "." });
        }

        public static void Complete(GameState state, GameConfigSnapshot config, ExpeditionState expedition)
        {
            expedition.Status = ExpeditionStatus.Completed;
            expedition.Progress = 100;
            foreach (var pair in expedition.AccumulatedLoot)
            {
                ResourceSystem.Add(state, pair.Key, pair.Value);
            }

            foreach (var survivorId in expedition.SurvivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor == null) continue;
                survivor.CurrentExpeditionId = string.Empty;
                survivor.State = expedition.WoundedSurvivorIds.Contains(survivorId) ? SurvivorActivityState.Wounded : SurvivorActivityState.Idle;
                survivor.Fatigue = GameMath.Clamp(survivor.Fatigue + 8, 0, 100);
                survivor.Xp += 5;
            }

            ZoneState zoneState;
            if (state.Zones.TryGetValue(expedition.ZoneId, out zoneState))
            {
                zoneState.Completions++;
                zoneState.Familiarity = GameMath.Clamp(zoneState.Familiarity + 5 + AverageSkill(state, expedition.SurvivorIds, "survival") * 0.05, 0, 100);
                if (zoneState.BestClearTimeSeconds <= 0 || expedition.ElapsedSeconds < zoneState.BestClearTimeSeconds)
                {
                    zoneState.BestClearTimeSeconds = expedition.ElapsedSeconds;
                }
            }

            UnlockSystem.RefreshZoneUnlocks(state, config);
            state.Statistics.ExpeditionsCompleted++;
        }

        private static void Fail(GameState state, ExpeditionState expedition)
        {
            foreach (var survivorId in expedition.SurvivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor == null) continue;
                survivor.CurrentExpeditionId = string.Empty;
                survivor.State = expedition.WoundedSurvivorIds.Contains(survivorId) ? SurvivorActivityState.Wounded : SurvivorActivityState.Missing;
            }
            state.Statistics.ExpeditionsFailed++;
        }

        private static string PickWeighted(List<WeightedEntry> entries, ref SeededRandom rng)
        {
            var total = 0;
            foreach (var entry in entries) total += Math.Max(0, entry.Weight);
            if (total <= 0) return entries[0].Id;
            var roll = rng.RangeInclusive(1, total);
            var cursor = 0;
            foreach (var entry in entries)
            {
                cursor += Math.Max(0, entry.Weight);
                if (roll <= cursor) return entry.Id;
            }
            return entries[entries.Count - 1].Id;
        }

        private static LootTableEntry PickWeightedLoot(List<LootTableEntry> entries, ref SeededRandom rng)
        {
            var total = 0;
            foreach (var entry in entries) total += Math.Max(0, entry.Weight);
            if (entries.Count == 0) return null;
            if (total <= 0) return entries[0];
            var roll = rng.RangeInclusive(1, total);
            var cursor = 0;
            foreach (var entry in entries)
            {
                cursor += Math.Max(0, entry.Weight);
                if (roll <= cursor) return entry;
            }
            return entries[entries.Count - 1];
        }

        private static double AverageSkill(GameState state, List<string> survivorIds, string skillId)
        {
            if (survivorIds.Count == 0) return 0;
            var total = 0;
            foreach (var survivorId in survivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor != null && survivor.Skills.ContainsKey(skillId)) total += survivor.Skills[skillId];
            }
            return total / (double)survivorIds.Count;
        }
    }
}
