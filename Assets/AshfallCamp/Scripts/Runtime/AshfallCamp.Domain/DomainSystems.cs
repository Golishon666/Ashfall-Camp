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
        public static readonly string[] SkillIds =
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
        public static ValidationResult ValidateBroadcast(GameState state, GameConfigSnapshot config)
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
            if (!config.RecruitableSurvivors.TryGetValue(candidateId, out candidate) || IsAlreadyRecruited(state, candidate))
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

            var candidates = PickCandidates(state, config, request.Seed, config.Balance.RecruitmentCandidateCount);
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

        private static List<RecruitableSurvivorDefinition> PickCandidates(GameState state, GameConfigSnapshot config, uint seed, int count)
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
            var targetCount = Math.Min(Math.Max(1, count), candidates.Count);
            while (selected.Count < targetCount)
            {
                var index = rng.RangeInclusive(0, candidates.Count - 1);
                selected.Add(candidates[index]);
                candidates.RemoveAt(index);
            }

            return selected;
        }

        private static RecruitableSurvivorDefinition ResolveCandidate(GameState state, GameConfigSnapshot config, string candidateId)
        {
            if (string.IsNullOrWhiteSpace(candidateId)) return null;
            if (!EnsureRecruitmentState(state).PendingCandidateIds.Contains(candidateId)) return null;

            RecruitableSurvivorDefinition candidate;
            if (!config.RecruitableSurvivors.TryGetValue(candidateId, out candidate)) return null;
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

            if (!config.Items.ContainsKey(item.ItemId))
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
            if (!config.Items.TryGetValue(item.ItemId, out definition)) return cost;

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
            if (!config.Items.TryGetValue(item.ItemId, out definition))
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

            var definition = config.Items[result.Item.ItemId];
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
            item.EquippedBySurvivorId = string.Empty;
        }

        private static string GetEquippedUid(SurvivorState survivor, ItemSlot slot)
        {
            if (survivor == null || survivor.Equipment == null) return string.Empty;
            if (slot == ItemSlot.Armor) return survivor.Equipment.ArmorItemUid;
            if (slot == ItemSlot.Utility) return survivor.Equipment.UtilityItemUid;
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
            else
            {
                survivor.Equipment.WeaponItemUid = itemUid ?? string.Empty;
            }
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
            ProgressionSystem.RefreshDemoCompletion(state, config);
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

            var cost = CalculateCost(config, zone, GetPolicy(config, request.PolicyId), request.SurvivorIds.Count);
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
            ResourceSystem.TrySpend(state, ExpeditionValidator.CalculateCost(config, zone, policy, request.SurvivorIds.Count));

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
                if (expedition.WoundedSurvivorIds.Contains(survivorId))
                {
                    HealingSystem.ApplyWound(state, config, survivorId);
                }
                else
                {
                    survivor.State = SurvivorActivityState.Idle;
                }

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
            ProgressionSystem.RefreshDemoCompletion(state, config);
        }

        private static void Fail(GameState state, GameConfigSnapshot config, ExpeditionState expedition)
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
