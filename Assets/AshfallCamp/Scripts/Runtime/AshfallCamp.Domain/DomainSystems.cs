using System;
using System.Collections.Generic;

namespace AshfallCamp.Domain
{
    internal static class ExpeditionLogMarkup
    {
        public const string Time = "#7F9397";
        public const string Survivor = "#E8C66B";
        public const string Enemy = "#E26D5A";
        public const string Weapon = "#79B8FF";
        public const string Damage = "#FF5C5C";
        public const string Heal = "#77D982";
        public const string Miss = "#A7ADB2";
        public const string Crit = "#FFD166";
        public const string Noise = "#C792EA";
        public const string Hp = "#9EE6FF";
        public const string Wound = "#FF9A76";
        public const string Loot = "#8BE08B";
        public const string Success = "#7DDE92";
        public const string Warning = "#F2A65A";
        public const string Info = "#C7D0D4";

        public static string Color(string value, string color)
        {
            if (string.IsNullOrEmpty(value)) return string.Empty;
            return "<color=" + color + ">" + Sanitize(value) + "</color>";
        }

        public static string Number(int value, string color)
        {
            return Color(value.ToString(), color);
        }

        public static string Percent(double value)
        {
            var percent = ((int)Math.Round(GameMath.Clamp(value, 0, 1) * 100)).ToString() + "%";
            return Color(percent, Info);
        }

        private static string Sanitize(string value)
        {
            return value.Replace("<", "[").Replace(">", "]");
        }
    }

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
        public static readonly string[] SkillIds =
        {
            GameIds.Skills.Scavenging,
            GameIds.Skills.Melee,
            GameIds.Skills.Firearms,
            GameIds.Skills.Survival,
            GameIds.Skills.Mechanics,
            GameIds.Skills.Medicine
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
                var startLevel = BuildingSystem.GetLevel(building, building.StartingLevel);
                state.Buildings[building.Id] = new BuildingState
                {
                    Id = building.Id,
                    Level = building.StartingLevel,
                    IsUnlocked = building.StartsUnlocked,
                    AssignedWorkers = startLevel != null ? startLevel.DefaultWorkers : 0,
                    ConditionPercent = startLevel != null ? startLevel.DefaultConditionPercent : 0
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

            var survivor = CreateSurvivorState(
                "survivor_1",
                config.StartingSurvivor.Name,
                config.StartingSurvivor.BackgroundId,
                config.StartingSurvivor.TraitIds,
                config.StartingSurvivor.Skills,
                config);
            survivor.Equipment.WeaponItemUid = weapon.Uid;
            weapon.EquippedBySurvivorId = survivor.Id;
            state.Survivors.Add(survivor);
            state.NextId = 2;
            return state;
        }

        public static SurvivorState CreateSurvivorState(
            string survivorId,
            string name,
            string backgroundId,
            List<string> traitIds,
            Dictionary<string, int> skills,
            GameConfigSnapshot config)
        {
            var survivor = new SurvivorState
            {
                Id = survivorId ?? string.Empty,
                Name = name ?? string.Empty,
                BackgroundId = backgroundId ?? string.Empty,
                TraitIds = traitIds == null ? new List<string>() : new List<string>(traitIds),
                Health = 30,
                MaxHealth = 30,
                Morale = 50
            };

            foreach (var skillId in SkillIds)
            {
                survivor.Skills[skillId] = 0;
                survivor.SkillXp[skillId] = 0;
            }

            if (skills != null)
            {
                foreach (var pair in skills)
                {
                    survivor.Skills[pair.Key] = pair.Value;
                }
            }

            ApplyBackground(survivor, config);
            ApplyTraits(survivor, config);
            return survivor;
        }

        public static InventoryItemState CreateItemState(string itemId, GameConfigSnapshot config, string uid)
        {
            ItemDefinition item;
            if (!config.TryGetItem(itemId, out item))
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
            if (!config.TryGetBackground(survivor.BackgroundId, out background)) return;
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
                if (config.TryGetTrait(traitId, out trait))
                {
                    ApplyStats(survivor, trait.StatModifiers);
                }
            }
        }

        private static void ApplyStats(SurvivorState survivor, Dictionary<string, int> stats)
        {
            foreach (var pair in stats)
            {
                if (pair.Key == GameIds.Stats.MaxHealth)
                {
                    survivor.MaxHealth += pair.Value;
                    survivor.Health += pair.Value;
                }
                else if (pair.Key == GameIds.Stats.Morale || pair.Key == GameIds.Stats.CombatMorale)
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
                SpendUpTo(state, pair.Key, pair.Value);
            }
            return true;
        }

        public static int SpendUpTo(GameState state, string resourceId, int amount)
        {
            if (state == null || string.IsNullOrWhiteSpace(resourceId) || amount <= 0) return 0;
            var current = GetAmount(state, resourceId);
            var spent = Math.Min(current, amount);
            if (spent <= 0) return 0;
            state.Resources[resourceId] = current - spent;
            AddSpentStatistic(state, resourceId, spent);
            return spent;
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
            if (state.Statistics == null)
            {
                state.Statistics = new GameStatistics();
            }

            if (state.Statistics.TotalResourcesGained == null)
            {
                state.Statistics.TotalResourcesGained = new Dictionary<string, int>(StringComparer.Ordinal);
            }

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

        private static void AddSpentStatistic(GameState state, string resourceId, int amount)
        {
            if (amount <= 0) return;
            if (state.Statistics == null)
            {
                state.Statistics = new GameStatistics();
            }

            if (state.Statistics.TotalResourcesSpent == null)
            {
                state.Statistics.TotalResourcesSpent = new Dictionary<string, int>(StringComparer.Ordinal);
            }

            if (!state.Statistics.TotalResourcesSpent.ContainsKey(resourceId))
            {
                state.Statistics.TotalResourcesSpent[resourceId] = 0;
            }

            state.Statistics.TotalResourcesSpent[resourceId] += amount;
        }
    }

    public static class CampUpkeepSystem
    {
        public static void Tick(GameState state, GameConfigSnapshot config, double deltaSeconds)
        {
            if (state == null || config == null || config.Balance == null) return;
            var interval = Math.Max(1, config.Balance.CampUpkeepIntervalSeconds);
            var dt = Math.Max(0, deltaSeconds);
            state.CampUpkeepAccumulatorSeconds = Math.Max(0, state.CampUpkeepAccumulatorSeconds) + dt;

            while (state.CampUpkeepAccumulatorSeconds >= interval)
            {
                state.CampUpkeepAccumulatorSeconds -= interval;
                ApplyInterval(state, config);
            }
        }

        private static void ApplyInterval(GameState state, GameConfigSnapshot config)
        {
            var survivors = CountSupportedSurvivors(state);
            if (survivors <= 0) return;

            var shortage = ApplyNeed(state, config.Balance.CampUpkeepFoodResourceId, config.Balance.CampUpkeepFoodPerSurvivor, survivors);
            shortage |= ApplyNeed(state, config.Balance.CampUpkeepWaterResourceId, config.Balance.CampUpkeepWaterPerSurvivor, survivors);
            if (shortage)
            {
                ApplyShortagePressure(state, config);
            }
        }

        private static bool ApplyNeed(GameState state, string resourceId, int perSurvivor, int survivorCount)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || perSurvivor <= 0 || survivorCount <= 0) return false;
            var required = perSurvivor * survivorCount;
            var spent = ResourceSystem.SpendUpTo(state, resourceId, required);
            return spent < required;
        }

        private static int CountSupportedSurvivors(GameState state)
        {
            var count = 0;
            foreach (var survivor in state.Survivors)
            {
                if (survivor != null && survivor.State != SurvivorActivityState.Missing && survivor.State != SurvivorActivityState.OnExpedition)
                {
                    count++;
                }
            }

            return count;
        }

        private static void ApplyShortagePressure(GameState state, GameConfigSnapshot config)
        {
            var moralePenalty = Math.Max(0, config.Balance.CampUpkeepShortageMoralePenalty);
            var fatigue = Math.Max(0, config.Balance.CampUpkeepShortageFatigue);
            if (moralePenalty <= 0 && fatigue <= 0) return;

            foreach (var survivor in state.Survivors)
            {
                if (survivor == null || survivor.State == SurvivorActivityState.Missing || survivor.State == SurvivorActivityState.OnExpedition) continue;
                survivor.Morale = GameMath.Clamp(survivor.Morale - moralePenalty, 0, 100);
                survivor.Fatigue = GameMath.Clamp(survivor.Fatigue + fatigue, 0, 100);
            }
        }
    }

    public static class RecoverySystem
    {
        public static ValidationResult ValidateEmergencyScavenge(GameState state, GameConfigSnapshot config, EmergencyScavengeRequest request)
        {
            var result = new ValidationResult();
            request = request ?? new EmergencyScavengeRequest();
            if (state == null || config == null || state.Recovery == null)
            {
                result.Errors.Add("Recovery state is missing.");
                return result;
            }

            if (state.Recovery.EmergencyScavengeActive)
            {
                result.Errors.Add("Emergency scavenge is already active.");
            }

            if (state.Recovery.EmergencyScavengeCooldownRemainingSeconds > 0)
            {
                result.Errors.Add("Emergency scavenge is cooling down.");
            }

            if (config.Balance.EmergencyScavengeRewards.Count == 0)
            {
                result.Errors.Add("Emergency scavenge has no rewards.");
            }

            return result;
        }

        public static EmergencyScavengeResult StartEmergencyScavenge(GameState state, GameConfigSnapshot config, EmergencyScavengeRequest request)
        {
            request = request ?? new EmergencyScavengeRequest();
            var result = new EmergencyScavengeResult
            {
                Validation = ValidateEmergencyScavenge(state, config, request),
                DurationSeconds = config != null ? Math.Max(1, config.Balance.EmergencyScavengeDurationSeconds) : 0
            };

            if (config != null)
            {
                result.Rewards = CalculateEmergencyScavengeRewards(config);
            }

            if (!result.Validation.IsValid)
            {
                return result;
            }

            state.Recovery.EmergencyScavengeActive = true;
            state.Recovery.EmergencyScavengeStartedAtUnixMs = Math.Max(0, request.NowUnixMs);
            state.Recovery.EmergencyScavengeRemainingSeconds = result.DurationSeconds;
            result.Started = true;
            return result;
        }

        public static void Tick(GameState state, GameConfigSnapshot config, double deltaSeconds)
        {
            if (state == null || config == null || state.Recovery == null) return;
            if (deltaSeconds <= 0) return;

            if (!state.Recovery.EmergencyScavengeActive)
            {
                state.Recovery.EmergencyScavengeCooldownRemainingSeconds = Math.Max(0, state.Recovery.EmergencyScavengeCooldownRemainingSeconds - deltaSeconds);
                return;
            }

            state.Recovery.EmergencyScavengeRemainingSeconds = Math.Max(0, state.Recovery.EmergencyScavengeRemainingSeconds - deltaSeconds);
            if (state.Recovery.EmergencyScavengeRemainingSeconds > 0) return;

            CompleteEmergencyScavenge(state, config);
        }

        public static Dictionary<string, int> CalculateEmergencyScavengeRewards(GameConfigSnapshot config)
        {
            var rewards = new Dictionary<string, int>(StringComparer.Ordinal);
            if (config == null) return rewards;
            foreach (var pair in config.Balance.EmergencyScavengeRewards)
            {
                if (!string.IsNullOrWhiteSpace(pair.Key) && pair.Value > 0)
                {
                    rewards[pair.Key] = pair.Value;
                }
            }

            return rewards;
        }

        private static void CompleteEmergencyScavenge(GameState state, GameConfigSnapshot config)
        {
            state.Recovery.EmergencyScavengeActive = false;
            state.Recovery.EmergencyScavengeRemainingSeconds = 0;

            foreach (var pair in config.Balance.EmergencyScavengeRewards)
            {
                ResourceSystem.Add(state, pair.Key, pair.Value);
            }

            var completedAt = state.Recovery.EmergencyScavengeStartedAtUnixMs + SecondsToMilliseconds(config.Balance.EmergencyScavengeDurationSeconds);
            state.Recovery.EmergencyScavengeCooldownRemainingSeconds = Math.Max(0, config.Balance.EmergencyScavengeCooldownSeconds);
            state.CampEvents.Add(new CampEventState
            {
                Id = "event_" + state.NextId++,
                EventId = GameEventIds.EmergencyScavengeCompleted,
                SubjectId = GameEventIds.EmergencyScavengeCompleted,
                SubjectName = GameEventIds.EmergencyScavengeCompleted,
                AtUnixMs = completedAt
            });
        }

        private static long SecondsToMilliseconds(double seconds)
        {
            return (long)Math.Round(Math.Max(0, seconds) * 1000.0);
        }
    }

    public static class RecruitmentSystem
    {
        public const int MaxCandidateCount = 4;
        private static readonly double[] CandidateSlotBaseChances = { 1.0, 0.5, 0.25, 0.1 };

        public static ValidationResult ValidateBroadcast(GameState state, GameConfigSnapshot config)
        {
            var result = new ValidationResult();
            if (state == null || config == null)
            {
                result.Errors.Add("Recruitment state is missing.");
                return result;
            }

            if (!HasRequiredBuilding(state, config))
            {
                result.Errors.Add("Radio tower is not ready.");
            }

            if (HasPendingCandidates(state))
            {
                result.Errors.Add("Resolve current survivor signals first.");
            }

            if (CountAvailableCandidates(state, config) == 0)
            {
                result.Errors.Add("No survivor signals available.");
            }

            if (!ResourceSystem.CanSpend(state, CalculateCost(state, config)))
            {
                result.Errors.Add("Not enough resources.");
            }

            return result;
        }

        public static ValidationResult ValidateRecruitSelection(GameState state, GameConfigSnapshot config, string candidateId)
        {
            var result = new ValidationResult();
            if (state == null || config == null)
            {
                result.Errors.Add("Recruitment state is missing.");
                return result;
            }

            if (state.Survivors.Count >= state.SurvivorCap)
            {
                result.Errors.Add("Survivor cap reached.");
            }

            if (!HasRequiredBuilding(state, config))
            {
                result.Errors.Add("Radio tower is not ready.");
            }

            if (string.IsNullOrWhiteSpace(candidateId))
            {
                result.Errors.Add("Select a survivor signal.");
                return result;
            }

            var recruitment = EnsureRecruitmentState(state);
            if (!recruitment.PendingCandidateIds.Contains(candidateId))
            {
                result.Errors.Add("Selected survivor signal is unavailable.");
                return result;
            }

            RecruitableSurvivorDefinition candidate;
            if (!config.TryGetRecruitableSurvivor(candidateId, out candidate) || IsAlreadyRecruited(state, candidate))
            {
                result.Errors.Add("Selected survivor signal is unavailable.");
            }

            return result;
        }

        public static BroadcastRecruitmentResult Broadcast(GameState state, GameConfigSnapshot config, BroadcastRecruitmentRequest request)
        {
            request = request ?? new BroadcastRecruitmentRequest();
            var validation = ValidateBroadcast(state, config);
            var result = new BroadcastRecruitmentResult
            {
                Validation = validation,
                Cost = CalculateCost(state, config)
            };

            if (!validation.IsValid)
            {
                return result;
            }

            var candidates = PickCandidates(state, config, request.Seed, config.Balance.RecruitmentCandidateCount, request.CandidateChanceMultiplier);
            if (candidates.Count == 0)
            {
                result.Validation.Errors.Add("No survivor signals available.");
                return result;
            }

            ResourceSystem.TrySpend(state, result.Cost);
            var recruitment = EnsureRecruitmentState(state);
            recruitment.PendingCandidateIds.Clear();
            recruitment.LastBroadcastAtUnixMs = request.NowUnixMs;
            foreach (var candidate in candidates)
            {
                recruitment.PendingCandidateIds.Add(candidate.Id);
                result.CandidateIds.Add(candidate.Id);
            }

            return result;
        }

        public static SkipRecruitmentCandidatesResult SkipCandidates(GameState state)
        {
            var result = new SkipRecruitmentCandidatesResult();
            if (state == null)
            {
                result.Validation.Errors.Add("Recruitment state is missing.");
                return result;
            }

            var recruitment = EnsureRecruitmentState(state);
            if (recruitment.PendingCandidateIds.Count == 0)
            {
                result.Validation.Errors.Add("No survivor signals available.");
                return result;
            }

            result.SkippedCandidateIds.AddRange(recruitment.PendingCandidateIds);
            recruitment.PendingCandidateIds.Clear();
            return result;
        }

        public static RecruitSurvivorResult Recruit(GameState state, GameConfigSnapshot config, RecruitSurvivorRequest request)
        {
            request = request ?? new RecruitSurvivorRequest();
            var validation = ValidateRecruitSelection(state, config, request.CandidateId);
            var result = new RecruitSurvivorResult
            {
                Validation = validation
            };

            RecruitableSurvivorDefinition candidate = null;
            if (state != null && config != null)
            {
                candidate = ResolveCandidate(state, config, request.CandidateId);
            }

            if (!validation.IsValid)
            {
                return result;
            }

            if (candidate == null)
            {
                return result;
            }

            var weapon = GameStateFactory.CreateItemState(candidate.WeaponItemId, config, "item_" + state.NextId++);
            var survivor = GameStateFactory.CreateSurvivorState(
                "survivor_" + state.NextId++,
                candidate.Name,
                candidate.BackgroundId,
                candidate.TraitIds,
                candidate.Skills,
                config);

            survivor.Equipment.WeaponItemUid = weapon.Uid;
            weapon.EquippedBySurvivorId = survivor.Id;
            state.Inventory.Add(weapon);
            state.Survivors.Add(survivor);
            state.Statistics.SurvivorsRecruited++;
            AddRecruitmentEvent(state, survivor, request);
            EnsureRecruitmentState(state).PendingCandidateIds.Clear();

            result.Survivor = survivor;
            result.Weapon = weapon;
            return result;
        }

        public static Dictionary<string, int> CalculateCost(GameState state, GameConfigSnapshot config)
        {
            var cost = new Dictionary<string, int>(StringComparer.Ordinal);
            if (state == null || config == null) return cost;

            var balance = config.Balance;
            if (balance.RecruitmentBroadcastCost != null && balance.RecruitmentBroadcastCost.Count > 0)
            {
                foreach (var pair in balance.RecruitmentBroadcastCost)
                {
                    AddCost(cost, pair.Key, pair.Value);
                }

                return cost;
            }

            var survivorCount = Math.Max(1, state.Survivors.Count);
            AddCost(cost, balance.RecruitmentScrapResourceId, (int)Math.Floor(balance.RecruitmentBaseScrap * Math.Pow(survivorCount, balance.RecruitmentScrapExponent)));
            AddCost(cost, balance.RecruitmentFoodResourceId, balance.RecruitmentBaseFood + survivorCount / Math.Max(1, balance.RecruitmentFoodDivisor));
            AddCost(cost, balance.RecruitmentWaterResourceId, balance.RecruitmentBaseWater + survivorCount / Math.Max(1, balance.RecruitmentWaterDivisor));
            return cost;
        }

        public static int CountAvailableCandidates(GameState state, GameConfigSnapshot config)
        {
            if (state == null || config == null) return 0;
            var count = 0;
            foreach (var candidate in config.RecruitableSurvivors.Values)
            {
                if (!IsAlreadyRecruited(state, candidate) && !IsPendingCandidate(state, candidate.Id)) count++;
            }

            return count;
        }

        public static List<string> GetPendingCandidateIds(GameState state)
        {
            var result = new List<string>();
            if (state == null) return result;

            result.AddRange(EnsureRecruitmentState(state).PendingCandidateIds);
            return result;
        }

        public static bool HasPendingCandidates(GameState state)
        {
            return state != null &&
                   state.Recruitment != null &&
                   state.Recruitment.PendingCandidateIds != null &&
                   state.Recruitment.PendingCandidateIds.Count > 0;
        }

        private static void AddCost(Dictionary<string, int> cost, string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || amount <= 0) return;
            cost[resourceId] = amount;
        }

        private static bool HasRequiredBuilding(GameState state, GameConfigSnapshot config)
        {
            var requiredId = config.Balance.RecruitmentRequiredBuildingId;
            if (string.IsNullOrWhiteSpace(requiredId)) return true;

            BuildingState building;
            return state.Buildings.TryGetValue(requiredId, out building) &&
                   building.IsUnlocked &&
                   building.Level >= config.Balance.RecruitmentRequiredBuildingLevel;
        }

        private static List<RecruitableSurvivorDefinition> PickCandidates(GameState state, GameConfigSnapshot config, uint seed, int count, double chanceMultiplier)
        {
            var candidates = new List<RecruitableSurvivorDefinition>();
            foreach (var candidate in config.RecruitableSurvivors.Values)
            {
                if (!IsAlreadyRecruited(state, candidate) && !IsPendingCandidate(state, candidate.Id))
                {
                    candidates.Add(candidate);
                }
            }

            candidates.Sort((left, right) => string.CompareOrdinal(left.Id, right.Id));
            var selected = new List<RecruitableSurvivorDefinition>();
            if (candidates.Count == 0) return selected;

            var rng = new SeededRandom(seed == 0 ? (uint)Math.Max(1, state.NextId) * 2654435761u : seed);
            var targetCount = Math.Min(Math.Min(Math.Max(1, count), MaxCandidateCount), candidates.Count);
            var fallbackCandidate = candidates[0];
            var normalizedMultiplier = GameMath.Clamp(chanceMultiplier, 0, double.MaxValue);
            for (var slot = 0; slot < targetCount && candidates.Count > 0; slot++)
            {
                var index = rng.RangeInclusive(0, candidates.Count - 1);
                var candidate = candidates[index];
                candidates.RemoveAt(index);
                var baseChance = slot < CandidateSlotBaseChances.Length ? CandidateSlotBaseChances[slot] : 0.0;
                var chance = GameMath.Clamp(baseChance * normalizedMultiplier, 0, 1);
                if (rng.NextDouble() <= chance)
                {
                    selected.Add(candidate);
                }
            }

            if (selected.Count == 0)
            {
                selected.Add(fallbackCandidate);
            }

            return selected;
        }

        private static RecruitableSurvivorDefinition ResolveCandidate(GameState state, GameConfigSnapshot config, string candidateId)
        {
            if (string.IsNullOrWhiteSpace(candidateId)) return null;
            if (!EnsureRecruitmentState(state).PendingCandidateIds.Contains(candidateId)) return null;

            RecruitableSurvivorDefinition candidate;
            if (!config.TryGetRecruitableSurvivor(candidateId, out candidate)) return null;
            return IsAlreadyRecruited(state, candidate) ? null : candidate;
        }

        private static void AddRecruitmentEvent(GameState state, SurvivorState survivor, RecruitSurvivorRequest request)
        {
            state.CampEvents.Add(new CampEventState
            {
                Id = "event_" + state.NextId++,
                EventId = GameEventIds.SurvivorJoined,
                SubjectId = survivor.Id,
                SubjectName = survivor.Name,
                DetailId = survivor.BackgroundId,
                AtUnixMs = request.NowUnixMs
            });

            while (state.CampEvents.Count > 50)
            {
                state.CampEvents.RemoveAt(0);
            }
        }

        private static bool IsAlreadyRecruited(GameState state, RecruitableSurvivorDefinition candidate)
        {
            if (state == null || candidate == null) return false;
            foreach (var survivor in state.Survivors)
            {
                if (string.Equals(survivor.Name, candidate.Name, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static bool IsPendingCandidate(GameState state, string candidateId)
        {
            return !string.IsNullOrWhiteSpace(candidateId) &&
                   state != null &&
                   state.Recruitment != null &&
                   state.Recruitment.PendingCandidateIds != null &&
                   state.Recruitment.PendingCandidateIds.Contains(candidateId);
        }

        private static RecruitmentState EnsureRecruitmentState(GameState state)
        {
            if (state.Recruitment == null)
            {
                state.Recruitment = new RecruitmentState();
            }

            if (state.Recruitment.PendingCandidateIds == null)
            {
                state.Recruitment.PendingCandidateIds = new List<string>();
            }

            return state.Recruitment;
        }
    }

    public static class WorkshopSystem
    {
        public static ValidationResult ValidateRepair(GameState state, GameConfigSnapshot config, RepairItemRequest request)
        {
            var result = new ValidationResult();
            if (state == null || config == null || request == null)
            {
                result.Errors.Add("Workshop state is missing.");
                return result;
            }

            if (!HasRequiredWorkshop(state, config))
            {
                result.Errors.Add("Workshop repair is locked.");
            }

            var item = FindItem(state, request.ItemUid);
            if (item == null)
            {
                result.Errors.Add("Item is missing.");
                return result;
            }

            ItemDefinition itemDefinition;
            if (!config.TryGetItem(item.ItemId, out itemDefinition))
            {
                result.Errors.Add("Item definition is missing.");
                return result;
            }

            if (item.Durability >= item.MaxDurability)
            {
                result.Errors.Add("Item is already fully repaired.");
            }

            if (!ResourceSystem.CanSpend(state, CalculateRepairCost(state, config, request.ItemUid)))
            {
                result.Errors.Add("Not enough repair resources.");
            }

            return result;
        }

        public static RepairItemResult Repair(GameState state, GameConfigSnapshot config, RepairItemRequest request)
        {
            var validation = ValidateRepair(state, config, request);
            var result = new RepairItemResult
            {
                Validation = validation,
                Item = request != null ? FindItem(state, request.ItemUid) : null,
                Cost = request != null ? CalculateRepairCost(state, config, request.ItemUid) : new Dictionary<string, int>(StringComparer.Ordinal)
            };

            if (!validation.IsValid || result.Item == null)
            {
                return result;
            }

            ResourceSystem.TrySpend(state, result.Cost);
            result.Item.Durability = Math.Max(1, result.Item.MaxDurability);
            return result;
        }

        public static Dictionary<string, int> CalculateRepairCost(GameState state, GameConfigSnapshot config, string itemUid)
        {
            var cost = new Dictionary<string, int>(StringComparer.Ordinal);
            if (state == null || config == null || string.IsNullOrWhiteSpace(itemUid)) return cost;

            var item = FindItem(state, itemUid);
            if (item == null) return cost;

            ItemDefinition definition;
            if (!config.TryGetItem(item.ItemId, out definition)) return cost;

            var missingDurability = Math.Max(0, item.MaxDurability - item.Durability);
            if (missingDurability <= 0) return cost;

            var block = Math.Max(1, config.Balance.WorkshopRepairDurabilityBlock);
            var amount = (int)Math.Ceiling(missingDurability / (double)block * Math.Max(0, definition.RepairCostMultiplier));
            if (!string.IsNullOrWhiteSpace(config.Balance.WorkshopRepairResourceId) && amount > 0)
            {
                cost[config.Balance.WorkshopRepairResourceId] = amount;
            }

            return cost;
        }

        public static ValidationResult ValidateEquip(GameState state, GameConfigSnapshot config, EquipItemRequest request)
        {
            var result = new ValidationResult();
            if (state == null || config == null || request == null)
            {
                result.Errors.Add("Workshop state is missing.");
                return result;
            }

            var survivor = FindSurvivor(state, request.SurvivorId);
            if (survivor == null)
            {
                result.Errors.Add("Survivor is missing.");
                return result;
            }

            if (survivor.State == SurvivorActivityState.OnExpedition || survivor.State == SurvivorActivityState.Missing)
            {
                result.Errors.Add("Survivor is not available.");
            }

            var item = FindItem(state, request.ItemUid);
            if (item == null)
            {
                result.Errors.Add("Item is missing.");
                return result;
            }

            ItemDefinition definition;
            if (!config.TryGetItem(item.ItemId, out definition))
            {
                result.Errors.Add("Item definition is missing.");
                return result;
            }

            if (item.Durability <= 0)
            {
                result.Errors.Add("Item is broken.");
            }

            if (string.Equals(GetEquippedUid(survivor, definition.Slot), item.Uid, StringComparison.Ordinal))
            {
                result.Errors.Add("Item is already equipped.");
            }

            return result;
        }

        public static EquipItemResult Equip(GameState state, GameConfigSnapshot config, EquipItemRequest request)
        {
            var validation = ValidateEquip(state, config, request);
            var result = new EquipItemResult
            {
                Validation = validation,
                Survivor = request != null ? FindSurvivor(state, request.SurvivorId) : null,
                Item = request != null ? FindItem(state, request.ItemUid) : null
            };

            if (!validation.IsValid || result.Survivor == null || result.Item == null)
            {
                return result;
            }

            var definition = config.RequireItem(result.Item.ItemId);
            result.PreviouslyEquippedItem = FindItem(state, GetEquippedUid(result.Survivor, definition.Slot));
            if (result.PreviouslyEquippedItem != null)
            {
                result.PreviouslyEquippedItem.EquippedBySurvivorId = string.Empty;
            }

            ClearPreviousOwner(state, result.Item);
            SetEquippedUid(result.Survivor, definition.Slot, result.Item.Uid);
            result.Item.EquippedBySurvivorId = result.Survivor.Id;
            return result;
        }

        public static InventoryItemState FindItem(GameState state, string itemUid)
        {
            if (state == null || string.IsNullOrWhiteSpace(itemUid)) return null;
            foreach (var item in state.Inventory)
            {
                if (string.Equals(item.Uid, itemUid, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        public static SurvivorState FindSurvivor(GameState state, string survivorId)
        {
            if (state == null || string.IsNullOrWhiteSpace(survivorId)) return null;
            foreach (var survivor in state.Survivors)
            {
                if (string.Equals(survivor.Id, survivorId, StringComparison.Ordinal))
                {
                    return survivor;
                }
            }

            return null;
        }

        private static bool HasRequiredWorkshop(GameState state, GameConfigSnapshot config)
        {
            var requiredId = config.Balance.WorkshopRequiredBuildingId;
            if (string.IsNullOrWhiteSpace(requiredId)) return true;

            BuildingState building;
            return state.Buildings.TryGetValue(requiredId, out building) &&
                   building.IsUnlocked &&
                   building.Level >= config.Balance.WorkshopRequiredBuildingLevel;
        }

        private static void ClearPreviousOwner(GameState state, InventoryItemState item)
        {
            if (item == null || string.IsNullOrWhiteSpace(item.EquippedBySurvivorId)) return;
            var owner = FindSurvivor(state, item.EquippedBySurvivorId);
            if (owner == null) return;

            if (string.Equals(owner.Equipment.WeaponItemUid, item.Uid, StringComparison.Ordinal)) owner.Equipment.WeaponItemUid = string.Empty;
            if (string.Equals(owner.Equipment.ArmorItemUid, item.Uid, StringComparison.Ordinal)) owner.Equipment.ArmorItemUid = string.Empty;
            if (string.Equals(owner.Equipment.UtilityItemUid, item.Uid, StringComparison.Ordinal)) owner.Equipment.UtilityItemUid = string.Empty;
            if (string.Equals(owner.Equipment.BackpackItemUid, item.Uid, StringComparison.Ordinal)) owner.Equipment.BackpackItemUid = string.Empty;
            item.EquippedBySurvivorId = string.Empty;
        }

        private static string GetEquippedUid(SurvivorState survivor, ItemSlot slot)
        {
            if (survivor == null || survivor.Equipment == null) return string.Empty;
            if (slot == ItemSlot.Armor) return survivor.Equipment.ArmorItemUid;
            if (slot == ItemSlot.Utility) return survivor.Equipment.UtilityItemUid;
            if (slot == ItemSlot.Backpack) return survivor.Equipment.BackpackItemUid;
            return survivor.Equipment.WeaponItemUid;
        }

        private static void SetEquippedUid(SurvivorState survivor, ItemSlot slot, string itemUid)
        {
            if (slot == ItemSlot.Armor)
            {
                survivor.Equipment.ArmorItemUid = itemUid ?? string.Empty;
            }
            else if (slot == ItemSlot.Utility)
            {
                survivor.Equipment.UtilityItemUid = itemUid ?? string.Empty;
            }
            else if (slot == ItemSlot.Backpack)
            {
                survivor.Equipment.BackpackItemUid = itemUid ?? string.Empty;
            }
            else
            {
                survivor.Equipment.WeaponItemUid = itemUid ?? string.Empty;
            }
        }
    }

    public static class BuildingSystem
    {
        private const int MillisecondsPerSecond = 1000;

        public static ValidationResult ValidateUpgrade(GameState state, GameConfigSnapshot config, string buildingId)
        {
            var result = new ValidationResult();
            if (string.IsNullOrWhiteSpace(buildingId))
            {
                result.Errors.Add("Building id is required.");
                return result;
            }

            BuildingDefinition definition;
            if (!config.TryGetBuilding(buildingId, out definition))
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

            if (IsUpgradeActive(building))
            {
                result.Errors.Add("Building upgrade is already in progress.");
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
            return Upgrade(state, config, buildingId, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
        }

        public static BuildingUpgradeResult Upgrade(GameState state, GameConfigSnapshot config, string buildingId, long nowUnixMs)
        {
            var validation = ValidateUpgrade(state, config, buildingId);
            var result = new BuildingUpgradeResult { Validation = validation };
            if (!validation.IsValid)
            {
                return result;
            }

            var definition = config.RequireBuilding(buildingId);
            var building = state.Buildings[buildingId];
            var nextLevel = GetLevel(definition, building.Level + 1);
            ResourceSystem.TrySpend(state, nextLevel.Cost);
            var durationSeconds = Math.Max(0.001, nextLevel.UpgradeDurationSeconds);
            building.UpgradeStartedAtUnixMs = Math.Max(0, nowUnixMs);
            building.UpgradeFinishedAtUnixMs = building.UpgradeStartedAtUnixMs + Math.Max(1, (long)Math.Ceiling(durationSeconds * MillisecondsPerSecond));
            result.Building = building;
            result.TargetLevel = nextLevel.Level;
            result.DurationSeconds = durationSeconds;
            result.Started = true;
            return result;
        }

        public static List<string> CompleteReadyUpgrades(GameState state, GameConfigSnapshot config, long nowUnixMs)
        {
            var completed = new List<string>();
            foreach (var building in state.Buildings.Values)
            {
                if (!IsUpgradeActive(building) || building.UpgradeFinishedAtUnixMs > nowUnixMs) continue;

                BuildingDefinition definition;
                if (!config.TryGetBuilding(building.Id, out definition))
                {
                    ClearUpgradeTimer(building);
                    continue;
                }

                var nextLevel = GetLevel(definition, building.Level + 1);
                if (nextLevel == null)
                {
                    ClearUpgradeTimer(building);
                    continue;
                }

                building.Level = nextLevel.Level;
                ApplyLevelDisplayDefaults(building, nextLevel, true);
                ClearUpgradeTimer(building);
                completed.Add(building.Id);
            }

            if (completed.Count > 0)
            {
                ApplyAllBuildingEffects(state, config);
                UnlockSystem.RefreshZoneUnlocks(state, config);
                ProgressionSystem.RefreshDemoCompletion(state, config, nowUnixMs);
            }

            return completed;
        }

        public static bool IsUpgradeActive(BuildingState building)
        {
            return building != null && building.UpgradeStartedAtUnixMs > 0 && building.UpgradeFinishedAtUnixMs > building.UpgradeStartedAtUnixMs;
        }

        public static double GetRemainingUpgradeSeconds(BuildingState building, long nowUnixMs)
        {
            if (!IsUpgradeActive(building)) return 0;
            return Math.Max(0, (building.UpgradeFinishedAtUnixMs - nowUnixMs) / 1000.0);
        }

        public static void ApplyConfiguredDisplayDefaults(BuildingState building, BuildingDefinition definition)
        {
            if (building == null || definition == null) return;
            ApplyLevelDisplayDefaults(building, GetLevel(definition, building.Level), false);
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
                if (!config.TryGetBuilding(building.Id, out definition)) continue;
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
                if (!config.TryGetBuilding(building.Id, out definition)) continue;
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

        private static void ClearUpgradeTimer(BuildingState building)
        {
            building.UpgradeStartedAtUnixMs = 0;
            building.UpgradeFinishedAtUnixMs = 0;
        }

        private static void ApplyLevelDisplayDefaults(BuildingState building, BuildingLevelDefinition level, bool overwrite)
        {
            if (level == null) return;
            if (overwrite || building.AssignedWorkers <= 0)
            {
                building.AssignedWorkers = Math.Max(0, Math.Min(level.DefaultWorkers, level.WorkerCapacity));
            }

            if (overwrite || building.ConditionPercent <= 0)
            {
                building.ConditionPercent = Math.Max(0, Math.Min(100, level.DefaultConditionPercent));
            }
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
                    if (condition.Type == GameConditionTypes.ZoneCompletions)
                    {
                        ZoneState requiredZone;
                        if (!state.Zones.TryGetValue(condition.Id, out requiredZone) || requiredZone.Completions < condition.Value) unlocked = false;
                    }
                    else if (condition.Type == GameConditionTypes.BuildingLevel)
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

    public static class ProgressionSystem
    {
        public static bool RefreshDemoCompletion(GameState state, GameConfigSnapshot config, long nowUnixMs = 0)
        {
            if (state == null || config == null || config.Balance == null) return false;
            if (state.Progress == null) state.Progress = new GameProgressState();
            if (state.Progress.DemoCompleted) return false;
            if (config.Balance.DemoCompletionConditions == null || config.Balance.DemoCompletionConditions.Count == 0) return false;

            UnlockCondition completedCondition;
            if (!AreCompletionConditionsMet(state, config.Balance, out completedCondition)) return false;

            state.Progress.DemoCompleted = true;
            state.Progress.DemoCompletionId = FormatCompletionId(completedCondition);
            state.Progress.DemoCompletedAtUnixMs = Math.Max(0, nowUnixMs);
            AddDemoCompletedEvent(state, state.Progress.DemoCompletionId, nowUnixMs);
            return true;
        }

        private static bool AreCompletionConditionsMet(GameState state, BalanceDefinition balance, out UnlockCondition completedCondition)
        {
            completedCondition = null;
            if (balance.DemoCompletionRequiresAnyCondition)
            {
                foreach (var condition in balance.DemoCompletionConditions)
                {
                    if (!IsConditionMet(state, condition)) continue;
                    completedCondition = condition;
                    return true;
                }

                return false;
            }

            foreach (var condition in balance.DemoCompletionConditions)
            {
                if (IsConditionMet(state, condition))
                {
                    completedCondition = condition;
                    continue;
                }

                completedCondition = condition;
                return false;
            }

            return completedCondition != null;
        }

        private static bool IsConditionMet(GameState state, UnlockCondition condition)
        {
            if (state == null || condition == null || string.IsNullOrWhiteSpace(condition.Type)) return false;
            if (string.Equals(condition.Type, GameConditionTypes.ZoneCompletions, StringComparison.Ordinal))
            {
                ZoneState zone;
                return state.Zones.TryGetValue(condition.Id, out zone) && zone.Completions >= Math.Max(1, condition.Value);
            }

            if (string.Equals(condition.Type, GameConditionTypes.ZoneUnlocked, StringComparison.Ordinal))
            {
                ZoneState zone;
                return state.Zones.TryGetValue(condition.Id, out zone) && zone.IsUnlocked;
            }

            if (string.Equals(condition.Type, GameConditionTypes.BuildingLevel, StringComparison.Ordinal))
            {
                BuildingState building;
                return state.Buildings.TryGetValue(condition.Id, out building) && building.Level >= Math.Max(1, condition.Value);
            }

            if (string.Equals(condition.Type, GameConditionTypes.SurvivorCount, StringComparison.Ordinal))
            {
                return state.Survivors.Count >= Math.Max(1, condition.Value);
            }

            if (string.Equals(condition.Type, GameConditionTypes.ResourceAmount, StringComparison.Ordinal))
            {
                int amount;
                return state.Resources.TryGetValue(condition.Id, out amount) && amount >= Math.Max(1, condition.Value);
            }

            if (string.Equals(condition.Type, GameConditionTypes.ExpeditionsCompleted, StringComparison.Ordinal))
            {
                return state.Statistics.ExpeditionsCompleted >= Math.Max(1, condition.Value);
            }

            return false;
        }

        private static string FormatCompletionId(UnlockCondition condition)
        {
            if (condition == null) return string.Empty;
            return condition.Type + ":" + condition.Id;
        }

        private static void AddDemoCompletedEvent(GameState state, string completionId, long nowUnixMs)
        {
            foreach (var campEvent in state.CampEvents)
            {
                if (campEvent != null && string.Equals(campEvent.EventId, GameEventIds.DemoCompleted, StringComparison.Ordinal)) return;
            }

            state.CampEvents.Add(new CampEventState
            {
                Id = "event_" + state.NextId++,
                EventId = GameEventIds.DemoCompleted,
                SubjectId = completionId ?? string.Empty,
                SubjectName = completionId ?? string.Empty,
                DetailId = completionId ?? string.Empty,
                AtUnixMs = Math.Max(0, nowUnixMs)
            });
        }

        public static void GrantExpeditionCompletion(SurvivorState survivor, BalanceDefinition balance)
        {
            if (survivor == null || balance == null) return;
            GrantSurvivorXp(survivor, Math.Max(0, balance.ExpeditionCompletionXp), balance);
            GrantSkillXp(survivor, balance.ExpeditionCompletionSkillId, Math.Max(0, balance.ExpeditionCompletionSkillXp), balance);
        }

        public static void GrantSurvivorXp(SurvivorState survivor, int amount, BalanceDefinition balance)
        {
            if (survivor == null || balance == null || amount <= 0) return;
            survivor.Xp += amount;
            var maxLevel = Math.Max(1, balance.SurvivorMaxLevel);
            while (survivor.Level < maxLevel)
            {
                var required = GetSurvivorXpRequired(survivor.Level, balance);
                if (required <= 0 || survivor.Xp < required) break;
                survivor.Xp -= required;
                survivor.Level++;
                var healthGain = Math.Max(0, balance.SurvivorHealthPerLevel);
                survivor.MaxHealth += healthGain;
                survivor.Health = GameMath.Clamp(survivor.Health + healthGain, 0, survivor.MaxHealth);
            }
        }

        public static void GrantSkillXp(SurvivorState survivor, string skillId, int amount, BalanceDefinition balance)
        {
            if (survivor == null || balance == null || amount <= 0 || string.IsNullOrWhiteSpace(skillId)) return;
            if (!survivor.Skills.ContainsKey(skillId)) survivor.Skills[skillId] = 0;
            if (!survivor.SkillXp.ContainsKey(skillId)) survivor.SkillXp[skillId] = 0;

            survivor.SkillXp[skillId] += amount;
            var maxLevel = Math.Max(0, balance.SkillMaxLevel);
            while (survivor.Skills[skillId] < maxLevel)
            {
                var required = GetSkillXpRequired(survivor.Skills[skillId], balance);
                if (required <= 0 || survivor.SkillXp[skillId] < required) break;
                survivor.SkillXp[skillId] -= required;
                survivor.Skills[skillId]++;
            }
        }

        public static int GetSurvivorXpRequired(int level, BalanceDefinition balance)
        {
            if (balance == null) return 0;
            return Math.Max(1, (int)Math.Floor(Math.Max(1, balance.SurvivorXpThresholdBase) * Math.Pow(Math.Max(1, level), Math.Max(0.01, balance.SurvivorXpThresholdExponent))));
        }

        public static int GetSkillXpRequired(int skillLevel, BalanceDefinition balance)
        {
            if (balance == null) return 0;
            return Math.Max(1, (int)Math.Floor(Math.Max(1, balance.SkillXpThresholdBase) * Math.Pow(Math.Max(1, skillLevel + 1), Math.Max(0.01, balance.SkillXpThresholdExponent))));
        }
    }

    public static class ExpeditionValidator
    {
        public static ValidationResult Validate(GameState state, GameConfigSnapshot config, LaunchExpeditionRequest request)
        {
            var result = new ValidationResult();
            ZoneDefinition zone;
            if (!config.TryGetZone(request.ZoneId, out zone))
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

            var routeValidation = WorldTileTravelSystem.ValidateRoute(config, request.RouteTileIds);
            result.Errors.AddRange(routeValidation.Errors);

            var cost = CalculateCost(config, zone, GetPolicy(config, request.PolicyId), request.SurvivorIds.Count);
            MergeCost(cost, WorldTileTravelSystem.CalculateRoundTripCost(config, request.RouteTileIds, request.SurvivorIds.Count));
            if (!ResourceSystem.CanSpend(state, cost))
            {
                result.Errors.Add("Not enough expedition resources.");
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
            return CalculateCost(new BalanceDefinition(), zone, policy, survivorCount);
        }

        public static Dictionary<string, int> CalculateCost(GameConfigSnapshot config, ZoneDefinition zone, ExpeditionPolicyDefinition policy, int survivorCount)
        {
            return CalculateCost(config != null ? config.Balance : new BalanceDefinition(), zone, policy, survivorCount);
        }

        private static Dictionary<string, int> CalculateCost(BalanceDefinition balance, ZoneDefinition zone, ExpeditionPolicyDefinition policy, int survivorCount)
        {
            var cost = new Dictionary<string, int>(StringComparer.Ordinal);
            var food = (int)Math.Ceiling(zone.FoodCostPerSurvivor * survivorCount * policy.FoodModifier);
            var water = (int)Math.Ceiling(zone.WaterCostPerSurvivor * survivorCount * policy.WaterModifier);
            AddCost(cost, balance.ExpeditionFoodResourceId, food);
            AddCost(cost, balance.ExpeditionWaterResourceId, water);
            return cost;
        }

        private static void AddCost(Dictionary<string, int> cost, string resourceId, int amount)
        {
            if (string.IsNullOrWhiteSpace(resourceId) || amount <= 0) return;
            cost[resourceId] = amount;
        }

        internal static void MergeCost(Dictionary<string, int> target, Dictionary<string, int> addition)
        {
            if (target == null || addition == null) return;
            foreach (var pair in addition)
            {
                int current;
                target.TryGetValue(pair.Key, out current);
                target[pair.Key] = current + Math.Max(0, pair.Value);
            }
        }

        private static int GetMaxSquadSize(GameState state)
        {
            return Math.Max(1, state.SquadSize);
        }

        private static ExpeditionPolicyDefinition GetPolicy(GameConfigSnapshot config, string policyId)
        {
            ExpeditionPolicyDefinition policy;
            return config.TryGetPolicy(policyId, out policy) ? policy : new ExpeditionPolicyDefinition();
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

            var zone = config.RequireZone(request.ZoneId);
            ExpeditionPolicyDefinition policy;
            if (!config.TryGetPolicy(request.PolicyId, out policy)) policy = new ExpeditionPolicyDefinition();
            var launchCost = ExpeditionValidator.CalculateCost(config, zone, policy, request.SurvivorIds.Count);
            ExpeditionValidator.MergeCost(launchCost, WorldTileTravelSystem.CalculateRoundTripCost(config, request.RouteTileIds, request.SurvivorIds.Count));
            ResourceSystem.TrySpend(state, launchCost);

            var expedition = new ExpeditionState
            {
                Id = "expedition_" + state.NextId++,
                ZoneId = request.ZoneId,
                SurvivorIds = new List<string>(request.SurvivorIds),
                PolicyId = request.PolicyId,
                StartedAtUnixMs = request.NowUnixMs,
                ExpectedDurationSeconds = CalculateDuration(state, config, zone, policy, request.SurvivorIds),
                RandomState = request.Seed == 0 ? (uint)state.NextId * 7919u : request.Seed,
                Status = ExpeditionStatus.Active,
                WorldTileId = request.WorldTileId,
                TargetCell = request.TargetCell,
                RouteTileIds = request.RouteTileIds != null ? new List<string>(request.RouteTileIds) : new List<string>()
            };

            foreach (var survivorId in request.SurvivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor == null) continue;
                survivor.State = SurvivorActivityState.OnExpedition;
                survivor.CurrentExpeditionId = expedition.Id;
            }

            state.Expeditions.Add(expedition);
            if (!WorldTileTravelSystem.ResolveOutbound(state, config, expedition))
            {
                expedition.Status = ExpeditionStatus.Failed;
                ExpeditionSimulator.Fail(state, config, expedition);
            }
            result.Expedition = expedition;
            return result;
        }

        private static double CalculateDuration(GameState state, GameConfigSnapshot config, ZoneDefinition zone, ExpeditionPolicyDefinition policy, List<string> survivorIds)
        {
            var averageSurvival = AverageSkill(state, survivorIds, GameIds.Skills.Survival);
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
            var modifier = config.TryGetPolicy(policyId, out policy) ? policy.PowerModifier : 1.0;
            return total * modifier;
        }

        public static double CalculateSurvivorPower(GameState state, GameConfigSnapshot config, SurvivorState survivor)
        {
            var weapon = GetEquippedItem(state, config, survivor.Equipment.WeaponItemUid);
            var armor = GetEquippedItem(state, config, survivor.Equipment.ArmorItemUid);
            var weaponDamage = weapon != null ? weapon.BaseDamage : 1;
            var armorValue = armor != null ? armor.Armor : 0;
            var melee = survivor.Skills.ContainsKey(GameIds.Skills.Melee) ? survivor.Skills[GameIds.Skills.Melee] : 0;
            var firearms = survivor.Skills.ContainsKey(GameIds.Skills.Firearms) ? survivor.Skills[GameIds.Skills.Firearms] : 0;
            var fatiguePenalty = survivor.Fatigue >= 75 ? 5 : survivor.Fatigue >= 50 ? 2 : 0;
            return survivor.MaxHealth * 0.15 + melee * 0.7 + firearms * 0.7 + weaponDamage * 2 + armorValue * 4 + survivor.Level * 2 - fatiguePenalty;
        }

        public static ItemDefinition GetEquippedItem(GameState state, GameConfigSnapshot config, string itemUid)
        {
            if (string.IsNullOrEmpty(itemUid)) return null;
            for (var i = 0; i < state.Inventory.Count; i++)
            {
                if (state.Inventory[i].Uid != itemUid) continue;
                if (state.Inventory[i].Durability <= 0) return null;
                ItemDefinition definition;
                return config.TryGetItem(state.Inventory[i].ItemId, out definition) ? definition : null;
            }

            return null;
        }
    }

    public static class CombatResolver
    {
        private const int MaxCombatantsPerSide = 4;
        private const int CombatTurnSafetyLimit = 10000;
        private const int DefaultSurvivorBaseAttack = 2;
        private const int DefaultSurvivorBaseSpeed = 10;
        private const int DefaultEnemyBaseSpeed = 8;

        private enum CombatSide
        {
            Survivor,
            Enemy
        }

        private sealed class SurvivorCombatLoadout
        {
            public int BaseAttack = DefaultSurvivorBaseAttack;
            public int BaseSpeed = DefaultSurvivorBaseSpeed;
            public string WeaponConfigId = string.Empty;
            public string ArmorConfigId = string.Empty;
            public string UtilityConfigId = string.Empty;
        }

        private sealed class CombatUnit
        {
            public CombatSide Side;
            public string Id = string.Empty;
            public string DefinitionId = string.Empty;
            public string Name = string.Empty;
            public int SlotIndex;
            public int Hp;
            public int MaxHp;
            public int BaseAttack;
            public int Level = 1;
            public int SkillLevel;
            public int Defense;
            public double Evasion;
            public double BaseSpeed = DefaultSurvivorBaseSpeed;
            public WeaponType FallbackAttackType = WeaponType.Melee;
            public double FallbackAccuracy = 0.75;
            public WeaponDefinition Weapon;
            public ArmorDefinition Armor;
            public UtilityDefinition Utility;
            public SurvivorState Survivor;
            public EnemyDefinition Enemy;
            public bool UtilityUsed;

            public bool IsAlive
            {
                get { return Hp > 0; }
            }
        }

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
            var zone = new ZoneDefinition
            {
                Id = expedition != null ? expedition.ZoneId : string.Empty,
                Name = "Combat",
                MinEnemyCount = 1,
                MaxEnemyCount = 1
            };
            zone.EnemyTable.Add(new WeightedEntry { Id = enemyId, Weight = 100 });
            return ResolveCombat(state, config, expedition, zone);
        }

        public static bool ResolveCombat(GameState state, GameConfigSnapshot config, ExpeditionState expedition, ZoneDefinition zone)
        {
            if (state == null || config == null || expedition == null || zone == null) return true;
            var rng = new SeededRandom(expedition.RandomState);
            var survivors = BuildSurvivorUnits(state, config, expedition);
            var enemies = BuildEnemyUnits(config, zone, ref rng);
            if (survivors.Count == 0 || enemies.Count == 0)
            {
                expedition.RandomState = rng.State;
                return true;
            }

            AddCombatLog(
                expedition,
                ExpeditionLogMarkup.Color("combat starts", ExpeditionLogMarkup.Warning) +
                " against " + ExpeditionLogMarkup.Color(FormatEnemyCount(enemies.Count), ExpeditionLogMarkup.Enemy) +
                ": " + FormatUnitRoster(enemies) + ".");

            var turns = 0;
            while (AnyAlive(survivors) && AnyAlive(enemies) && turns < CombatTurnSafetyLimit)
            {
                var queue = BuildTurnQueue(survivors, enemies);
                for (var i = 0; i < queue.Count; i++)
                {
                    var actor = queue[i];
                    if (!actor.IsAlive) continue;

                    var opponents = actor.Side == CombatSide.Survivor ? enemies : survivors;
                    if (!AnyAlive(opponents)) break;

                    if (TryUseMedkit(actor, expedition))
                    {
                        AdvanceCombatTurn(config, expedition);
                        turns++;
                        continue;
                    }

                    ResolveUnitAttack(actor, opponents, config, expedition, ref rng);
                    AdvanceCombatTurn(config, expedition);
                    turns++;

                    if (!AnyAlive(opponents)) break;
                    if (turns >= CombatTurnSafetyLimit) break;
                }
            }

            expedition.RandomState = rng.State;
            UpdateExpeditionProgress(expedition);

            if (!AnyAlive(survivors))
            {
                AddCombatLog(expedition, ExpeditionLogMarkup.Color("combat lost", ExpeditionLogMarkup.Wound) + "; the squad is overwhelmed.");
                state.Statistics.CombatsLost++;
                return false;
            }

            if (turns >= CombatTurnSafetyLimit && AnyAlive(enemies))
            {
                AddCombatLog(expedition, ExpeditionLogMarkup.Color("combat stalled", ExpeditionLogMarkup.Warning) + " in the ash.");
                state.Statistics.CombatsLost++;
                return false;
            }

            for (var i = 0; i < enemies.Count; i++)
            {
                var enemy = enemies[i];
                if (enemy.IsAlive) continue;
                if (!expedition.EnemiesDefeated.ContainsKey(enemy.DefinitionId)) expedition.EnemiesDefeated[enemy.DefinitionId] = 0;
                expedition.EnemiesDefeated[enemy.DefinitionId]++;
                foreach (var survivorId in expedition.SurvivorIds)
                {
                    var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                    ProgressionSystem.GrantSurvivorXp(survivor, enemy.Enemy != null ? enemy.Enemy.XpReward : 0, config.Balance);
                }
            }

            AddCombatLog(
                expedition,
                ExpeditionLogMarkup.Color("combat won", ExpeditionLogMarkup.Success) +
                "; defeated " + ExpeditionLogMarkup.Color(FormatEnemyCount(CountDefeatedEnemies(enemies)), ExpeditionLogMarkup.Enemy) +
                FormatWoundedSummary(expedition) + ".");
            state.Statistics.CombatsWon++;
            return true;
        }

        private static List<CombatUnit> BuildSurvivorUnits(GameState state, GameConfigSnapshot config, ExpeditionState expedition)
        {
            var units = new List<CombatUnit>();
            for (var i = 0; i < expedition.SurvivorIds.Count && units.Count < MaxCombatantsPerSide; i++)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, expedition.SurvivorIds[i]);
                if (survivor == null) continue;

                var loadout = ResolveSurvivorLoadout(survivor, config);
                var armor = ResolveArmor(config, loadout.ArmorConfigId);
                var weapon = ResolveWeapon(config, loadout.WeaponConfigId);
                var utility = ResolveUtility(config, loadout.UtilityConfigId);
                var bonusHealth = armor != null ? Math.Max(0, armor.BonusHealth) : 0;
                var maxHp = Math.Max(1, survivor.MaxHealth + bonusHealth);
                var hp = GameMath.Clamp(Math.Max(1, survivor.Health) + bonusHealth, 1, maxHp);

                var unit = new CombatUnit
                {
                    Side = CombatSide.Survivor,
                    Id = survivor.Id,
                    DefinitionId = survivor.Id,
                    Name = string.IsNullOrWhiteSpace(survivor.Name) ? survivor.Id : survivor.Name,
                    SlotIndex = units.Count,
                    Hp = hp,
                    MaxHp = maxHp,
                    BaseAttack = Math.Max(0, loadout.BaseAttack),
                    BaseSpeed = Math.Max(1, loadout.BaseSpeed),
                    Level = Math.Max(1, survivor.Level),
                    Weapon = weapon,
                    Armor = armor,
                    Utility = utility,
                    Survivor = survivor,
                    Defense = armor != null ? Math.Max(0, armor.Defense) : 0,
                    Evasion = armor != null ? GameMath.Clamp(armor.EvasionChance, 0, 1) : 0,
                    FallbackAttackType = WeaponType.Melee,
                    FallbackAccuracy = config.Balance.BaseAccuracyMelee
                };
                unit.SkillLevel = GetSkillLevel(survivor, GetSkillId(unit));
                units.Add(unit);
            }

            return units;
        }

        private static List<CombatUnit> BuildEnemyUnits(GameConfigSnapshot config, ZoneDefinition zone, ref SeededRandom rng)
        {
            var units = new List<CombatUnit>();
            if (zone.EnemyTable.Count == 0) return units;

            var min = GameMath.Clamp(zone.MinEnemyCount <= 0 ? 1 : zone.MinEnemyCount, 1, MaxCombatantsPerSide);
            var max = GameMath.Clamp(zone.MaxEnemyCount <= 0 ? min : zone.MaxEnemyCount, min, MaxCombatantsPerSide);
            var count = rng.RangeInclusive(min, max);
            for (var i = 0; i < count; i++)
            {
                var enemyId = PickWeightedEnemy(zone.EnemyTable, ref rng);
                EnemyDefinition enemy;
                if (!config.TryGetEnemy(enemyId, out enemy)) continue;

                var armor = ResolveArmor(config, enemy.ArmorConfigId);
                var weapon = ResolveWeapon(config, enemy.WeaponConfigId);
                var utility = ResolveUtility(config, enemy.UtilityConfigId);
                var bonusHealth = armor != null ? Math.Max(0, armor.BonusHealth) : 0;
                var maxHp = Math.Max(1, enemy.MaxHealth + bonusHealth);
                var unit = new CombatUnit
                {
                    Side = CombatSide.Enemy,
                    Id = enemy.Id + "_" + i,
                    DefinitionId = enemy.Id,
                    Name = string.IsNullOrWhiteSpace(enemy.Name) ? enemy.Id : enemy.Name,
                    SlotIndex = units.Count,
                    Hp = maxHp,
                    MaxHp = maxHp,
                    BaseAttack = Math.Max(0, enemy.BaseDamage),
                    BaseSpeed = Math.Max(1, enemy.BaseSpeed <= 0 ? DefaultEnemyBaseSpeed : enemy.BaseSpeed),
                    Level = 1,
                    Weapon = weapon,
                    Armor = armor,
                    Utility = utility,
                    Enemy = enemy,
                    Defense = Math.Max(0, enemy.Armor) + (armor != null ? Math.Max(0, armor.Defense) : 0),
                    Evasion = GameMath.Clamp(enemy.Evasion + (armor != null ? armor.EvasionChance : 0), 0, 1),
                    FallbackAttackType = enemy.AttackType,
                    FallbackAccuracy = enemy.Accuracy
                };
                units.Add(unit);
            }

            return units;
        }

        private static SurvivorCombatLoadout ResolveSurvivorLoadout(SurvivorState survivor, GameConfigSnapshot config)
        {
            var loadout = new SurvivorCombatLoadout();
            if (survivor == null || config == null) return loadout;

            if (string.Equals(survivor.Name, config.StartingSurvivor.Name, StringComparison.Ordinal))
            {
                loadout.BaseAttack = config.StartingSurvivor.BaseAttack;
                loadout.BaseSpeed = config.StartingSurvivor.BaseSpeed;
                loadout.WeaponConfigId = config.StartingSurvivor.WeaponConfigId;
                loadout.ArmorConfigId = config.StartingSurvivor.ArmorConfigId;
                loadout.UtilityConfigId = config.StartingSurvivor.UtilityConfigId;
                return loadout;
            }

            foreach (var candidate in config.RecruitableSurvivors.Values)
            {
                if (!string.Equals(survivor.Name, candidate.Name, StringComparison.Ordinal)) continue;
                loadout.BaseAttack = candidate.BaseAttack;
                loadout.BaseSpeed = candidate.BaseSpeed;
                loadout.WeaponConfigId = candidate.WeaponConfigId;
                loadout.ArmorConfigId = candidate.ArmorConfigId;
                loadout.UtilityConfigId = candidate.UtilityConfigId;
                return loadout;
            }

            return loadout;
        }

        private static WeaponDefinition ResolveWeapon(GameConfigSnapshot config, string weaponId)
        {
            if (config == null || string.IsNullOrWhiteSpace(weaponId)) return null;
            WeaponDefinition weapon;
            return config.TryGetWeapon(weaponId, out weapon) ? weapon : null;
        }

        private static ArmorDefinition ResolveArmor(GameConfigSnapshot config, string armorId)
        {
            if (config == null || string.IsNullOrWhiteSpace(armorId)) return null;
            ArmorDefinition armor;
            return config.TryGetArmor(armorId, out armor) ? armor : null;
        }

        private static UtilityDefinition ResolveUtility(GameConfigSnapshot config, string utilityId)
        {
            if (config == null || string.IsNullOrWhiteSpace(utilityId)) return null;
            UtilityDefinition utility;
            return config.TryGetUtility(utilityId, out utility) ? utility : null;
        }

        private static List<CombatUnit> BuildTurnQueue(List<CombatUnit> survivors, List<CombatUnit> enemies)
        {
            var queue = new List<CombatUnit>();
            AddLiving(queue, survivors);
            AddLiving(queue, enemies);
            queue.Sort(CompareTurnOrder);
            return queue;
        }

        private static void AddLiving(List<CombatUnit> queue, List<CombatUnit> units)
        {
            for (var i = 0; i < units.Count; i++)
            {
                if (units[i].IsAlive) queue.Add(units[i]);
            }
        }

        private static int CompareTurnOrder(CombatUnit left, CombatUnit right)
        {
            var speed = GetFinalSpeed(right).CompareTo(GetFinalSpeed(left));
            if (speed != 0) return speed;
            if (left.Side != right.Side) return left.Side == CombatSide.Survivor ? -1 : 1;
            return left.SlotIndex.CompareTo(right.SlotIndex);
        }

        private static double GetFinalSpeed(CombatUnit unit)
        {
            var modifier = unit != null && unit.Armor != null ? unit.Armor.SpeedModifier : 0;
            return Math.Max(1, (unit != null ? unit.BaseSpeed : 1) * (1 + modifier));
        }

        private static bool TryUseMedkit(CombatUnit actor, ExpeditionState expedition)
        {
            if (actor == null || actor.Side != CombatSide.Survivor || actor.UtilityUsed || actor.Utility == null) return false;
            if (actor.Utility.Type != UtilityEquipmentType.Medkit || actor.Utility.HealAmount <= 0) return false;
            if (actor.Hp > Math.Floor(actor.MaxHp * 0.2)) return false;

            var before = actor.Hp;
            actor.Hp = GameMath.Clamp(actor.Hp + actor.Utility.HealAmount, 1, actor.MaxHp);
            actor.UtilityUsed = true;
            AddCombatLog(
                expedition,
                FormatUnitName(actor) + " uses " + ExpeditionLogMarkup.Color(actor.Utility.Name, ExpeditionLogMarkup.Heal) +
                " at " + ExpeditionLogMarkup.Color(FormatHp(before, actor.MaxHp), ExpeditionLogMarkup.Hp) +
                " HP and recovers " + ExpeditionLogMarkup.Number(actor.Hp - before, ExpeditionLogMarkup.Heal) +
                " HP (" + ExpeditionLogMarkup.Color(FormatHp(actor), ExpeditionLogMarkup.Hp) + " HP).");
            return true;
        }

        private static void ResolveUnitAttack(CombatUnit actor, List<CombatUnit> opponents, GameConfigSnapshot config, ExpeditionState expedition, ref SeededRandom rng)
        {
            var attacks = Math.Max(1, actor.Weapon != null ? actor.Weapon.AttacksPerTurn : 1);
            for (var attack = 0; attack < attacks; attack++)
            {
                var targets = SelectTargets(actor, opponents);
                if (targets.Count == 0) return;

                for (var i = 0; i < targets.Count; i++)
                {
                    var target = targets[i];
                    if (!actor.IsAlive || !target.IsAlive) continue;

                    var noiseAdded = Math.Max(0, actor.Weapon != null ? actor.Weapon.NoisePerAttack : 0);
                    expedition.Noise += noiseAdded;
                    var hitChance = CalculateUnitHitChance(actor, target, config);
                    if (rng.NextDouble() <= hitChance)
                    {
                        var critChance = CalculateUnitCritChance(actor, config);
                        var isCrit = rng.NextDouble() <= critChance;
                        var damage = CalculateUnitDamage(actor, target, config, isCrit, ref rng);
                        target.Hp = Math.Max(0, target.Hp - damage);
                        AddCombatLog(
                            expedition,
                            FormatUnitName(actor) + " hits " + FormatUnitName(target) + " with " + FormatAttackName(actor) +
                            " for " + ExpeditionLogMarkup.Number(damage, ExpeditionLogMarkup.Damage) + " damage" +
                            FormatAttackDetails(hitChance, isCrit, noiseAdded, expedition, target) + ".");
                        LogUnitDefeatedIfNeeded(target, expedition);
                    }
                    else
                    {
                        AddCombatLog(
                            expedition,
                            FormatUnitName(actor) + " " + ExpeditionLogMarkup.Color("misses", ExpeditionLogMarkup.Miss) +
                            " " + FormatUnitName(target) + " with " + FormatAttackName(actor) +
                            FormatAttackDetails(hitChance, false, noiseAdded, expedition, target) + ".");
                    }
                }
            }
        }

        private static List<CombatUnit> SelectTargets(CombatUnit actor, List<CombatUnit> opponents)
        {
            var targets = new List<CombatUnit>();
            var rule = GetTargetingRule(actor);
            if (rule == WeaponTargetingRule.FrontlineOnly)
            {
                var frontline = FirstLivingBySlot(opponents);
                if (frontline != null) targets.Add(frontline);
                return targets;
            }

            var vulnerable = new List<CombatUnit>();
            AddLiving(vulnerable, opponents);
            vulnerable.Sort(CompareVulnerability);
            var targetCount = Math.Max(1, actor.Weapon != null ? actor.Weapon.TargetCount : 1);
            targetCount = GameMath.Clamp(targetCount, 1, MaxCombatantsPerSide);
            for (var i = 0; i < vulnerable.Count && targets.Count < targetCount; i++)
            {
                targets.Add(vulnerable[i]);
            }

            return targets;
        }

        private static WeaponTargetingRule GetTargetingRule(CombatUnit actor)
        {
            if (actor.Weapon != null) return actor.Weapon.TargetingRule;
            return actor.FallbackAttackType == WeaponType.Firearm ? WeaponTargetingRule.AnyEnemy : WeaponTargetingRule.FrontlineOnly;
        }

        private static CombatUnit FirstLivingBySlot(List<CombatUnit> units)
        {
            CombatUnit best = null;
            for (var i = 0; i < units.Count; i++)
            {
                var unit = units[i];
                if (!unit.IsAlive) continue;
                if (best == null || unit.SlotIndex < best.SlotIndex) best = unit;
            }

            return best;
        }

        private static int CompareVulnerability(CombatUnit left, CombatUnit right)
        {
            var ratio = GetHealthRatio(left).CompareTo(GetHealthRatio(right));
            if (ratio != 0) return ratio;
            var hp = left.Hp.CompareTo(right.Hp);
            if (hp != 0) return hp;
            return left.SlotIndex.CompareTo(right.SlotIndex);
        }

        private static double GetHealthRatio(CombatUnit unit)
        {
            return unit == null || unit.MaxHp <= 0 ? 1 : unit.Hp / (double)unit.MaxHp;
        }

        private static double CalculateUnitHitChance(CombatUnit actor, CombatUnit target, GameConfigSnapshot config)
        {
            var baseAccuracy = actor.Weapon != null ? actor.Weapon.HitChance : GetFallbackAccuracy(actor, config);
            return CalculateHitChance(baseAccuracy, actor.SkillLevel, 0, target.Evasion, config.Balance.MinHitChance, config.Balance.MaxHitChance);
        }

        private static double GetFallbackAccuracy(CombatUnit actor, GameConfigSnapshot config)
        {
            if (actor.Side == CombatSide.Enemy) return actor.FallbackAccuracy;
            return actor.FallbackAttackType == WeaponType.Firearm ? config.Balance.BaseAccuracyFirearms : config.Balance.BaseAccuracyMelee;
        }

        private static double CalculateUnitCritChance(CombatUnit actor, GameConfigSnapshot config)
        {
            var weaponCrit = actor.Weapon != null ? actor.Weapon.CriticalChance : 0;
            return GameMath.Clamp(config.Balance.BaseCritChance + actor.SkillLevel * 0.001 + weaponCrit, 0.02, 0.35);
        }

        private static int CalculateUnitDamage(CombatUnit actor, CombatUnit target, GameConfigSnapshot config, bool isCrit, ref SeededRandom rng)
        {
            var weaponAttack = actor.Weapon != null ? Math.Max(0, actor.Weapon.Attack) : 0;
            var attack = Math.Max(0, actor.BaseAttack) + weaponAttack;
            var skillMultiplier = 1 + actor.SkillLevel * 0.006;
            var levelMultiplier = 1 + Math.Max(1, actor.Level) * 0.015;
            var randomMultiplier = rng.Range(0.85, 1.15);
            var raw = (attack + 1) * skillMultiplier * levelMultiplier * randomMultiplier;
            if (isCrit) raw *= config.Balance.CritMultiplier;

            var penetration = actor.Weapon != null ? GameMath.Clamp(actor.Weapon.ArmorPenetration, 0, 1) : 0;
            var effectiveArmor = target.Defense * (1 - penetration);
            return Math.Max(1, (int)Math.Floor(raw - effectiveArmor));
        }

        private static string GetSkillId(CombatUnit actor)
        {
            if (actor.Weapon != null)
            {
                return actor.Weapon.Type == WeaponCombatType.Melee ? GameIds.Skills.Melee : GameIds.Skills.Firearms;
            }

            return actor.FallbackAttackType == WeaponType.Firearm ? GameIds.Skills.Firearms : GameIds.Skills.Melee;
        }

        private static int GetSkillLevel(SurvivorState survivor, string skillId)
        {
            if (survivor == null || string.IsNullOrWhiteSpace(skillId)) return 0;
            int value;
            return survivor.Skills.TryGetValue(skillId, out value) ? value : 0;
        }

        private static void LogUnitDefeatedIfNeeded(CombatUnit target, ExpeditionState expedition)
        {
            if (target == null || target.Hp > 0 || expedition == null) return;

            if (target.Side == CombatSide.Survivor)
            {
                if (!expedition.WoundedSurvivorIds.Contains(target.Id))
                {
                    expedition.WoundedSurvivorIds.Add(target.Id);
                    AddCombatLog(expedition, FormatUnitName(target) + " is " + ExpeditionLogMarkup.Color("downed", ExpeditionLogMarkup.Wound) + " and will need treatment if the squad makes it home.");
                }

                return;
            }

            AddCombatLog(expedition, FormatUnitName(target) + " is " + ExpeditionLogMarkup.Color("defeated", ExpeditionLogMarkup.Success) + ".");
        }

        private static void AdvanceCombatTurn(GameConfigSnapshot config, ExpeditionState expedition)
        {
            expedition.ElapsedSeconds += Math.Max(0, config.Balance.AttackTurnSeconds);
            UpdateExpeditionProgress(expedition);
        }

        private static void UpdateExpeditionProgress(ExpeditionState expedition)
        {
            if (expedition == null) return;
            expedition.Progress = GameMath.Clamp(expedition.ElapsedSeconds / Math.Max(1, expedition.ExpectedDurationSeconds) * 100, 0, 100);
        }

        private static bool AnyAlive(List<CombatUnit> units)
        {
            for (var i = 0; i < units.Count; i++)
            {
                if (units[i].IsAlive) return true;
            }

            return false;
        }

        private static string PickWeightedEnemy(List<WeightedEntry> entries, ref SeededRandom rng)
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

        private static void AddCombatLog(ExpeditionState expedition, string message)
        {
            if (expedition == null || string.IsNullOrWhiteSpace(message)) return;
            expedition.Log.Add(new ExpeditionLogEntry
            {
                AtSeconds = expedition.ElapsedSeconds,
                Message = ExpeditionLogMarkup.Color(FormatLogTime(expedition.ElapsedSeconds), ExpeditionLogMarkup.Time) + ": " + message
            });
        }

        private static string FormatUnitName(CombatUnit unit)
        {
            if (unit == null) return ExpeditionLogMarkup.Color("Unknown", ExpeditionLogMarkup.Info);
            var color = unit.Side == CombatSide.Survivor ? ExpeditionLogMarkup.Survivor : ExpeditionLogMarkup.Enemy;
            return ExpeditionLogMarkup.Color(string.IsNullOrWhiteSpace(unit.Name) ? unit.Id : unit.Name, color);
        }

        private static string FormatAttackName(CombatUnit actor)
        {
            if (actor == null) return ExpeditionLogMarkup.Color("an attack", ExpeditionLogMarkup.Weapon);
            if (actor.Weapon != null && !string.IsNullOrWhiteSpace(actor.Weapon.Name)) return ExpeditionLogMarkup.Color(actor.Weapon.Name, ExpeditionLogMarkup.Weapon);
            if (actor.Weapon != null && !string.IsNullOrWhiteSpace(actor.Weapon.Id)) return ExpeditionLogMarkup.Color(actor.Weapon.Id, ExpeditionLogMarkup.Weapon);
            if (actor.FallbackAttackType == WeaponType.Firearm) return ExpeditionLogMarkup.Color("ranged attack", ExpeditionLogMarkup.Weapon);
            if (actor.Side == CombatSide.Enemy) return ExpeditionLogMarkup.Color("melee attack", ExpeditionLogMarkup.Weapon);
            return ExpeditionLogMarkup.Color("bare hands", ExpeditionLogMarkup.Weapon);
        }

        private static string FormatAttackDetails(double hitChance, bool isCrit, int noiseAdded, ExpeditionState expedition, CombatUnit target)
        {
            var details = new List<string>();
            details.Add("hit " + ExpeditionLogMarkup.Percent(hitChance));
            if (isCrit) details.Add(ExpeditionLogMarkup.Color("critical", ExpeditionLogMarkup.Crit));
            if (target != null && target.Defense > 0) details.Add("armor " + ExpeditionLogMarkup.Number(target.Defense, ExpeditionLogMarkup.Warning));
            if (noiseAdded > 0)
            {
                details.Add(
                    "noise +" + ExpeditionLogMarkup.Number(noiseAdded, ExpeditionLogMarkup.Noise) +
                    ", total " + ExpeditionLogMarkup.Number(Math.Max(0, expedition != null ? expedition.Noise : 0), ExpeditionLogMarkup.Noise));
            }

            if (target != null) details.Add(FormatUnitName(target) + " " + ExpeditionLogMarkup.Color(FormatHp(target), ExpeditionLogMarkup.Hp) + " HP");
            return " (" + string.Join(", ", details.ToArray()) + ")";
        }

        private static string FormatHp(CombatUnit unit)
        {
            if (unit == null) return "0/0";
            return FormatHp(unit.Hp, unit.MaxHp);
        }

        private static string FormatHp(int hp, int maxHp)
        {
            return Math.Max(0, hp) + "/" + Math.Max(1, maxHp);
        }

        private static string FormatEnemyCount(int count)
        {
            return Math.Max(0, count) + (count == 1 ? " enemy" : " enemies");
        }

        private static int CountDefeatedEnemies(List<CombatUnit> enemies)
        {
            var count = 0;
            if (enemies == null) return count;
            for (var i = 0; i < enemies.Count; i++)
            {
                if (!enemies[i].IsAlive) count++;
            }

            return count;
        }

        private static string FormatWoundedSummary(ExpeditionState expedition)
        {
            var count = expedition != null && expedition.WoundedSurvivorIds != null ? expedition.WoundedSurvivorIds.Count : 0;
            if (count <= 0) return string.Empty;
            return ", " + ExpeditionLogMarkup.Number(count, ExpeditionLogMarkup.Wound) + (count == 1 ? " survivor downed" : " survivors downed");
        }

        private static string FormatUnitRoster(List<CombatUnit> units)
        {
            if (units == null || units.Count == 0) return "none";

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            var order = new List<string>();
            for (var i = 0; i < units.Count; i++)
            {
                var name = units[i] != null && !string.IsNullOrWhiteSpace(units[i].Name) ? units[i].Name : "Unknown";
                if (!counts.ContainsKey(name))
                {
                    counts[name] = 0;
                    order.Add(name);
                }

                counts[name]++;
            }

            var parts = new List<string>();
            for (var i = 0; i < order.Count; i++)
            {
                var name = order[i];
                var count = counts[name];
                var coloredName = ExpeditionLogMarkup.Color(name, ExpeditionLogMarkup.Enemy);
                parts.Add(count > 1 ? coloredName + " x" + ExpeditionLogMarkup.Number(count, ExpeditionLogMarkup.Enemy) : coloredName);
            }

            return string.Join(", ", parts.ToArray());
        }

        private static string FormatLogTime(double elapsedSeconds)
        {
            var totalSeconds = Math.Max(0, (int)Math.Floor(elapsedSeconds));
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return minutes.ToString("00") + ":" + seconds.ToString("00");
        }
    }

    public static class HealingSystem
    {
        public static void ApplyWound(GameState state, GameConfigSnapshot config, string survivorId)
        {
            if (state == null || config == null) return;
            var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
            if (survivor == null) return;

            survivor.State = SurvivorActivityState.Wounded;
            survivor.Health = GameMath.Clamp(Math.Min(survivor.Health, config.Balance.HealingHealthOnWounded), 1, Math.Max(1, survivor.MaxHealth));
            EnsureWoundEffect(survivor, config);
        }

        public static void Tick(GameState state, GameConfigSnapshot config, double deltaSeconds)
        {
            if (state == null || config == null || deltaSeconds <= 0) return;
            if (!IsHealingUnlocked(state, config)) return;

            var dt = Math.Max(0, deltaSeconds);
            foreach (var survivor in state.Survivors)
            {
                if (survivor.State != SurvivorActivityState.Wounded) continue;
                var wound = EnsureWoundEffect(survivor, config);
                if (wound == null) continue;

                wound.RemainingSeconds = Math.Max(0, wound.RemainingSeconds - dt);
                if (wound.RemainingSeconds <= 0)
                {
                    CompleteWoundTreatment(survivor, config);
                }
            }
        }

        public static ValidationResult ValidateUseMedicine(GameState state, GameConfigSnapshot config, UseMedicineRequest request)
        {
            var result = new ValidationResult();
            if (state == null || config == null || request == null)
            {
                result.Errors.Add("Healing state is missing.");
                return result;
            }

            if (!IsHealingUnlocked(state, config))
            {
                result.Errors.Add("Infirmary is not ready.");
            }

            var survivor = ExpeditionValidator.FindSurvivor(state, request.SurvivorId);
            if (survivor == null)
            {
                result.Errors.Add("Survivor is missing.");
                return result;
            }

            if (survivor.State != SurvivorActivityState.Wounded || FindStatusEffect(survivor, config.Balance.HealingDefaultWoundId) == null)
            {
                result.Errors.Add("Survivor has no treatable wound.");
            }

            if (!ResourceSystem.CanSpend(state, CalculateMedicineCost(config)))
            {
                result.Errors.Add("Not enough medicine.");
            }

            return result;
        }

        public static UseMedicineResult UseMedicine(GameState state, GameConfigSnapshot config, UseMedicineRequest request)
        {
            var result = new UseMedicineResult
            {
                Validation = ValidateUseMedicine(state, config, request),
                Cost = CalculateMedicineCost(config)
            };

            if (!result.Validation.IsValid)
            {
                return result;
            }

            var survivor = ExpeditionValidator.FindSurvivor(state, request.SurvivorId);
            var wound = FindStatusEffect(survivor, config.Balance.HealingDefaultWoundId);
            if (survivor == null || wound == null)
            {
                result.Validation.Errors.Add("Survivor has no treatable wound.");
                return result;
            }

            ResourceSystem.TrySpend(state, result.Cost);
            wound.RemainingSeconds = Math.Max(0, wound.RemainingSeconds - Math.Max(0, config.Balance.HealingMedicineSeconds));
            if (wound.RemainingSeconds <= 0)
            {
                CompleteWoundTreatment(survivor, config);
                result.Healed = true;
            }

            result.Survivor = survivor;
            return result;
        }

        public static Dictionary<string, int> CalculateMedicineCost(GameConfigSnapshot config)
        {
            var cost = new Dictionary<string, int>(StringComparer.Ordinal);
            if (config == null) return cost;
            if (!string.IsNullOrWhiteSpace(config.Balance.HealingMedicineResourceId) && config.Balance.HealingMedicineCost > 0)
            {
                cost[config.Balance.HealingMedicineResourceId] = config.Balance.HealingMedicineCost;
            }

            return cost;
        }

        public static bool IsHealingUnlocked(GameState state, GameConfigSnapshot config)
        {
            if (state == null || config == null) return false;
            var requiredId = config.Balance.HealingRequiredBuildingId;
            if (string.IsNullOrWhiteSpace(requiredId)) return true;

            BuildingState building;
            return state.Buildings.TryGetValue(requiredId, out building) &&
                   building.IsUnlocked &&
                   building.Level >= config.Balance.HealingRequiredBuildingLevel;
        }

        private static StatusEffectState EnsureWoundEffect(SurvivorState survivor, GameConfigSnapshot config)
        {
            if (survivor == null || config == null || string.IsNullOrWhiteSpace(config.Balance.HealingDefaultWoundId)) return null;
            var wound = FindStatusEffect(survivor, config.Balance.HealingDefaultWoundId);
            if (wound == null)
            {
                wound = new StatusEffectState
                {
                    Id = config.Balance.HealingDefaultWoundId,
                    RemainingSeconds = Math.Max(1, config.Balance.HealingDefaultWoundDurationSeconds)
                };
                survivor.StatusEffects.Add(wound);
            }
            else if (wound.RemainingSeconds <= 0)
            {
                wound.RemainingSeconds = Math.Max(1, config.Balance.HealingDefaultWoundDurationSeconds);
            }

            return wound;
        }

        private static StatusEffectState FindStatusEffect(SurvivorState survivor, string effectId)
        {
            if (survivor == null || string.IsNullOrWhiteSpace(effectId)) return null;
            foreach (var effect in survivor.StatusEffects)
            {
                if (effect != null && string.Equals(effect.Id, effectId, StringComparison.Ordinal))
                {
                    return effect;
                }
            }

            return null;
        }

        private static void CompleteWoundTreatment(SurvivorState survivor, GameConfigSnapshot config)
        {
            RemoveWoundEffect(survivor, config.Balance.HealingDefaultWoundId);
            survivor.Health = Math.Max(1, survivor.MaxHealth);
            survivor.State = SurvivorActivityState.Idle;
        }

        private static void RemoveWoundEffect(SurvivorState survivor, string effectId)
        {
            if (survivor == null || string.IsNullOrWhiteSpace(effectId)) return;
            for (var i = survivor.StatusEffects.Count - 1; i >= 0; i--)
            {
                var effect = survivor.StatusEffects[i];
                if (effect != null && string.Equals(effect.Id, effectId, StringComparison.Ordinal))
                {
                    survivor.StatusEffects.RemoveAt(i);
                }
            }
        }
    }

    public static class RestSystem
    {
        public static ValidationResult ValidateStartRest(GameState state, GameConfigSnapshot config, StartRestRequest request)
        {
            var result = new ValidationResult();
            request = request ?? new StartRestRequest();
            if (state == null || config == null)
            {
                result.Errors.Add("Rest state is missing.");
                return result;
            }

            if (config.Balance.RestFatigueRecoveryPerMinute <= 0)
            {
                result.Errors.Add("Rest recovery is not configured.");
            }

            var survivor = FindSurvivor(state, request.SurvivorId);
            if (survivor == null)
            {
                result.Errors.Add("Unknown survivor.");
                return result;
            }

            if (survivor.State == SurvivorActivityState.Resting)
            {
                result.Errors.Add("Survivor is already resting.");
            }
            else if (survivor.State != SurvivorActivityState.Idle)
            {
                result.Errors.Add("Survivor cannot rest right now.");
            }

            if (survivor.Fatigue <= 0)
            {
                result.Errors.Add("Survivor is not tired.");
            }

            return result;
        }

        public static RestSurvivorResult StartRest(GameState state, GameConfigSnapshot config, StartRestRequest request)
        {
            var result = new RestSurvivorResult
            {
                Validation = ValidateStartRest(state, config, request)
            };
            if (!result.Validation.IsValid)
            {
                return result;
            }

            var survivor = FindSurvivor(state, request.SurvivorId);
            survivor.State = SurvivorActivityState.Resting;
            EnsureRemainders(state)[survivor.Id] = 0;
            result.Survivor = survivor;
            result.Started = true;
            return result;
        }

        public static ValidationResult ValidateStopRest(GameState state, StopRestRequest request)
        {
            var result = new ValidationResult();
            request = request ?? new StopRestRequest();
            if (state == null)
            {
                result.Errors.Add("Rest state is missing.");
                return result;
            }

            var survivor = FindSurvivor(state, request.SurvivorId);
            if (survivor == null)
            {
                result.Errors.Add("Unknown survivor.");
                return result;
            }

            if (survivor.State != SurvivorActivityState.Resting)
            {
                result.Errors.Add("Survivor is not resting.");
            }

            return result;
        }

        public static RestSurvivorResult StopRest(GameState state, StopRestRequest request)
        {
            var result = new RestSurvivorResult
            {
                Validation = ValidateStopRest(state, request)
            };
            if (!result.Validation.IsValid)
            {
                return result;
            }

            var survivor = FindSurvivor(state, request.SurvivorId);
            CompleteRest(state, survivor);
            result.Survivor = survivor;
            result.Stopped = true;
            return result;
        }

        public static List<string> Tick(GameState state, GameConfigSnapshot config, double deltaSeconds)
        {
            var completed = new List<string>();
            if (state == null || config == null || config.Balance.RestFatigueRecoveryPerMinute <= 0) return completed;
            var dt = Math.Max(0, deltaSeconds);
            if (dt <= 0) return completed;

            var remainders = EnsureRemainders(state);
            foreach (var survivor in state.Survivors)
            {
                if (survivor == null || survivor.State != SurvivorActivityState.Resting) continue;
                double remainder;
                remainders.TryGetValue(survivor.Id, out remainder);
                var recovery = remainder + Math.Max(0, config.Balance.RestFatigueRecoveryPerMinute) * dt / 60.0;
                var whole = (int)Math.Floor(recovery);
                remainders[survivor.Id] = recovery - whole;
                if (whole > 0)
                {
                    survivor.Fatigue = GameMath.Clamp(survivor.Fatigue - whole, 0, 100);
                }

                if (survivor.Fatigue <= 0)
                {
                    CompleteRest(state, survivor);
                    completed.Add(survivor.Id);
                }
            }

            return completed;
        }

        private static void CompleteRest(GameState state, SurvivorState survivor)
        {
            if (survivor == null) return;
            survivor.State = SurvivorActivityState.Idle;
            if (state != null && state.RestFatigueRecoveryRemainders != null)
            {
                state.RestFatigueRecoveryRemainders.Remove(survivor.Id);
            }
        }

        private static Dictionary<string, double> EnsureRemainders(GameState state)
        {
            if (state.RestFatigueRecoveryRemainders == null)
            {
                state.RestFatigueRecoveryRemainders = new Dictionary<string, double>(StringComparer.Ordinal);
            }

            return state.RestFatigueRecoveryRemainders;
        }

        private static SurvivorState FindSurvivor(GameState state, string survivorId)
        {
            if (state == null || string.IsNullOrWhiteSpace(survivorId)) return null;
            foreach (var survivor in state.Survivors)
            {
                if (survivor != null && string.Equals(survivor.Id, survivorId, StringComparison.Ordinal))
                {
                    return survivor;
                }
            }

            return null;
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
            if (!config.TryGetZone(expedition.ZoneId, out zone))
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
                expedition.RandomState = rng.State;
                if (!CombatResolver.ResolveCombat(state, config, expedition, zone))
                {
                    expedition.Status = ExpeditionStatus.Failed;
                    Fail(state, config, expedition);
                }
            }
            else if (roll < 70 && zone.LootTable.Count > 0)
            {
                RollLoot(state, config, expedition, zone, ref rng);
                expedition.RandomState = rng.State;
            }
            else if (roll < 85)
            {
                AddExpeditionLog(expedition, "the squad handles a " + ExpeditionLogMarkup.Color("small obstacle", ExpeditionLogMarkup.Warning) + ".");
                foreach (var survivorId in expedition.SurvivorIds)
                {
                    var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                    ProgressionSystem.GrantSurvivorXp(survivor, 1, config.Balance);
                }
            }
            else
            {
                AddExpeditionLog(expedition, ExpeditionLogMarkup.Color("quiet travel", ExpeditionLogMarkup.Info) + " through the ash.");
            }
        }

        private static void RollLoot(GameState state, GameConfigSnapshot config, ExpeditionState expedition, ZoneDefinition zone, ref SeededRandom rng)
        {
            var entry = PickWeightedLoot(zone.LootTable, ref rng);
            if (entry == null) return;
            var averageScavenging = AverageSkill(state, expedition.SurvivorIds, GameIds.Skills.Scavenging);
            ZoneState zoneState;
            var familiarity = state.Zones.TryGetValue(zone.Id, out zoneState) ? zoneState.Familiarity : 0;
            ExpeditionPolicyDefinition policy;
            var policyBonus = config.TryGetPolicy(expedition.PolicyId, out policy) ? policy.LootModifier : 1.0;
            var amount = (int)Math.Floor(rng.RangeInclusive(entry.Min, entry.Max) * (1 + averageScavenging * 0.015) * policyBonus * (1 + familiarity * 0.003));
            amount = Math.Max(1, amount);
            if (!expedition.AccumulatedLoot.ContainsKey(entry.ResourceId)) expedition.AccumulatedLoot[entry.ResourceId] = 0;
            expedition.AccumulatedLoot[entry.ResourceId] += amount;
            ResourceDefinition resource;
            var resourceName = config.TryGetResource(entry.ResourceId, out resource) ? resource.Name : entry.ResourceId;
            AddExpeditionLog(
                expedition,
                "found " + ExpeditionLogMarkup.Number(amount, ExpeditionLogMarkup.Loot) +
                " " + ExpeditionLogMarkup.Color(resourceName, ExpeditionLogMarkup.Loot) + ".");
            RollEquipment(config, expedition, zone, ref rng);
        }

        private static void RollEquipment(GameConfigSnapshot config, ExpeditionState expedition, ZoneDefinition zone, ref SeededRandom rng)
        {
            if (zone.EquipmentTable == null || zone.EquipmentTable.Count == 0 || rng.NextDouble() > GameMath.Clamp(zone.EquipmentDropChance, 0, 1)) return;
            var itemId = PickWeighted(zone.EquipmentTable, ref rng);
            ItemDefinition definition;
            if (!config.TryGetItem(itemId, out definition)) return;
            var item = GameStateFactory.CreateItemState(itemId, config, "item_" + Math.Abs((long)rng.State));
            if (item == null) return;
            expedition.FoundItems.Add(item);
            AddExpeditionLog(expedition, "found equipment: " + ExpeditionLogMarkup.Color(definition.Name, ExpeditionLogMarkup.Loot) + ".");
        }

        public static void Complete(GameState state, GameConfigSnapshot config, ExpeditionState expedition)
        {
            if (!WorldTileTravelSystem.ResolveReturn(state, config, expedition))
            {
                expedition.Status = ExpeditionStatus.Failed;
                Fail(state, config, expedition);
                return;
            }
            expedition.Status = ExpeditionStatus.Completed;
            expedition.Progress = 100;
            foreach (var pair in expedition.AccumulatedLoot)
            {
                ResourceSystem.Add(state, pair.Key, pair.Value);
            }
            foreach (var item in expedition.FoundItems)
            {
                if (item != null && !state.Inventory.Exists(existing => existing.Uid == item.Uid)) state.Inventory.Add(item);
            }

            ApplyEquipmentDurabilityLoss(state, config, expedition);

            foreach (var survivorId in expedition.SurvivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor == null) continue;
                survivor.CurrentExpeditionId = string.Empty;
                if (expedition.WoundedSurvivorIds.Contains(survivorId))
                {
                    HealingSystem.ApplyWound(state, config, survivorId);
                }
                else
                {
                    survivor.State = SurvivorActivityState.Idle;
                }

                survivor.Fatigue = GameMath.Clamp(survivor.Fatigue + 8, 0, 100);
                ProgressionSystem.GrantExpeditionCompletion(survivor, config.Balance);
            }

            ZoneState zoneState;
            if (state.Zones.TryGetValue(expedition.ZoneId, out zoneState))
            {
                zoneState.Completions++;
                zoneState.Familiarity = GameMath.Clamp(zoneState.Familiarity + 5 + AverageSkill(state, expedition.SurvivorIds, GameIds.Skills.Survival) * 0.05, 0, 100);
                if (zoneState.BestClearTimeSeconds <= 0 || expedition.ElapsedSeconds < zoneState.BestClearTimeSeconds)
                {
                    zoneState.BestClearTimeSeconds = expedition.ElapsedSeconds;
                }
            }

            UnlockSystem.RefreshZoneUnlocks(state, config);
            state.Statistics.ExpeditionsCompleted++;
            ProgressionSystem.RefreshDemoCompletion(state, config);
        }

        public static int CalculateDurabilityLoss(GameConfigSnapshot config, ZoneDefinition zone, ExpeditionPolicyDefinition policy, SurvivorState survivor)
        {
            if (config == null || zone == null || survivor == null) return 0;
            var policyModifier = policy != null ? policy.DurabilityModifier : 0;
            var traitModifier = CalculateTraitDurabilityModifier(config, survivor);
            return Math.Max(0, zone.DurabilityPressure + policyModifier + traitModifier);
        }

        private static void ApplyEquipmentDurabilityLoss(GameState state, GameConfigSnapshot config, ExpeditionState expedition)
        {
            if (state == null || config == null || expedition == null) return;
            ZoneDefinition zone;
            if (!config.TryGetZone(expedition.ZoneId, out zone)) return;
            ExpeditionPolicyDefinition policy;
            config.TryGetPolicy(expedition.PolicyId, out policy);

            var damagedItems = new HashSet<string>(StringComparer.Ordinal);
            foreach (var survivorId in expedition.SurvivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor == null || survivor.Equipment == null) continue;
                var loss = CalculateDurabilityLoss(config, zone, policy, survivor);
                if (loss <= 0) continue;

                DamageEquippedItem(state, expedition, survivor.Equipment.WeaponItemUid, loss, damagedItems);
                DamageEquippedItem(state, expedition, survivor.Equipment.ArmorItemUid, loss, damagedItems);
                DamageEquippedItem(state, expedition, survivor.Equipment.UtilityItemUid, loss, damagedItems);
                DamageEquippedItem(state, expedition, survivor.Equipment.BackpackItemUid, loss, damagedItems);
            }
        }

        private static int CalculateTraitDurabilityModifier(GameConfigSnapshot config, SurvivorState survivor)
        {
            if (config == null || survivor == null || config.Balance == null || string.IsNullOrWhiteSpace(config.Balance.DurabilityTraitModifierId)) return 0;
            var total = 0;
            foreach (var traitId in survivor.TraitIds)
            {
                TraitDefinition trait;
                if (!config.TryGetTrait(traitId, out trait)) continue;
                int modifier;
                if (trait.StatModifiers.TryGetValue(config.Balance.DurabilityTraitModifierId, out modifier))
                {
                    total += modifier;
                }
            }

            return total;
        }

        private static void DamageEquippedItem(GameState state, ExpeditionState expedition, string itemUid, int loss, HashSet<string> damagedItems)
        {
            if (string.IsNullOrWhiteSpace(itemUid) || loss <= 0 || damagedItems == null || !damagedItems.Add(itemUid)) return;
            var item = WorkshopSystem.FindItem(state, itemUid);
            if (item == null || item.Durability <= 0) return;
            var actualLoss = Math.Min(item.Durability, loss);
            item.Durability = Math.Max(0, item.Durability - loss);
            if (actualLoss > 0 && expedition != null)
            {
                if (expedition.EquipmentDurabilityLost == null)
                {
                    expedition.EquipmentDurabilityLost = new Dictionary<string, int>(StringComparer.Ordinal);
                }

                int existingLoss;
                expedition.EquipmentDurabilityLost.TryGetValue(item.Uid, out existingLoss);
                expedition.EquipmentDurabilityLost[item.Uid] = existingLoss + actualLoss;
            }

            if (item.Durability <= 0 && expedition != null)
            {
                if (expedition.BrokenItemUids == null)
                {
                    expedition.BrokenItemUids = new List<string>();
                }

                if (!expedition.BrokenItemUids.Contains(item.Uid))
                {
                    expedition.BrokenItemUids.Add(item.Uid);
                }
            }
        }

        public static void Fail(GameState state, GameConfigSnapshot config, ExpeditionState expedition)
        {
            foreach (var survivorId in expedition.SurvivorIds)
            {
                var survivor = ExpeditionValidator.FindSurvivor(state, survivorId);
                if (survivor == null) continue;
                survivor.CurrentExpeditionId = string.Empty;
                if (expedition.WoundedSurvivorIds.Contains(survivorId))
                {
                    HealingSystem.ApplyWound(state, config, survivorId);
                }
                else
                {
                    survivor.State = SurvivorActivityState.Missing;
                }
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

        private static void AddExpeditionLog(ExpeditionState expedition, string message)
        {
            if (expedition == null || string.IsNullOrWhiteSpace(message)) return;
            expedition.Log.Add(new ExpeditionLogEntry
            {
                AtSeconds = expedition.ElapsedSeconds,
                Message = ExpeditionLogMarkup.Color(FormatLogTime(expedition.ElapsedSeconds), ExpeditionLogMarkup.Time) + ": " + message
            });
        }

        private static string FormatLogTime(double elapsedSeconds)
        {
            var totalSeconds = Math.Max(0, (int)Math.Floor(elapsedSeconds));
            var minutes = totalSeconds / 60;
            var seconds = totalSeconds % 60;
            return minutes.ToString("00") + ":" + seconds.ToString("00");
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
