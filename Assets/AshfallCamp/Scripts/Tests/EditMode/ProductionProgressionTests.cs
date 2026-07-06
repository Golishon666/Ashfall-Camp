using System;
using System.Collections.Generic;
using System.Threading;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using NUnit.Framework;
using UnityEditor;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class ProductionProgressionTests
    {
        private const string ConfigPath = "Assets/AshfallCamp/Configs/Core/GameConfigDatabase.asset";

        [Test]
        public void ProductionConfigCanReachMutantTunnelFromNewGame()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(ConfigPath);
            Assert.NotNull(database);

            var config = new ScriptableObjectGameConfigProvider(database)
                .LoadAsync(CancellationToken.None)
                .GetAwaiter()
                .GetResult();
            var state = GameStateFactory.CreateNew(config, 0);
            var driver = new DemoPathDriver(config, state);

            driver.RunToMutantTunnelCompletion();

            Assert.IsTrue(state.Zones["mutant_tunnel"].IsUnlocked, driver.Describe("Mutant Tunnel should be unlocked."));
            Assert.GreaterOrEqual(state.Zones["mutant_tunnel"].Completions, 1, driver.Describe("Mutant Tunnel should be completed."));
            Assert.IsTrue(state.Progress.DemoCompleted, driver.Describe("Demo completion should be recorded."));
            Assert.LessOrEqual(driver.ActionCount, DemoPathDriver.MaxActionBudget, driver.Describe("Production path took too many actions."));
            Assert.LessOrEqual(
                driver.LongestIdleWaitSeconds,
                Math.Max(config.Balance.HealingDefaultWoundDurationSeconds, config.Balance.EmergencyScavengeCooldownSeconds) + config.Balance.EmergencyScavengeDurationSeconds + 5,
                driver.Describe("Production path introduced a long idle wait."));
        }

        private sealed class DemoPathDriver
        {
            public const int MaxActionBudget = 260;

            private const string AbandonedStore = "abandoned_store";
            private const string DrySuburb = "dry_suburb";
            private const string PoliceOutpost = "police_outpost";
            private const string MutantTunnel = "mutant_tunnel";
            private const string Barracks = "barracks";
            private const string Workshop = "workshop";
            private const string MushroomBeds = "mushroom_beds";
            private const string WaterCollector = "water_collector";
            private const string RadioTower = "radio_tower";
            private const string Infirmary = "infirmary";
            private const string BalancedPolicy = "balanced";
            private const string Scrap = "scrap";
            private const string Food = "food";
            private const string Water = "water";
            private const string Medicine = "medicine";
            private const string WeaponParts = "weapon_parts";

            private readonly GameConfigSnapshot _config;
            private readonly GameState _state;
            private readonly List<string> _log = new List<string>();
            private uint _seed = 42000;

            public DemoPathDriver(GameConfigSnapshot config, GameState state)
            {
                _config = config;
                _state = state;
            }

            public int ActionCount { get; private set; }
            public double LongestIdleWaitSeconds { get; private set; }

            public void RunToMutantTunnelCompletion()
            {
                while (ActionCount < MaxActionBudget)
                {
                    RecoverSquad();
                    UnlockSystem.RefreshZoneUnlocks(_state, _config);

                    if (_state.Zones[MutantTunnel].IsUnlocked)
                    {
                        RunExpedition(MutantTunnel);
                        if (_state.Zones[MutantTunnel].Completions > 0)
                        {
                            return;
                        }
                        continue;
                    }

                    if (!_state.Zones[DrySuburb].IsUnlocked)
                    {
                        RunExpedition(AbandonedStore);
                        continue;
                    }

                    if (_state.Buildings[Barracks].Level < 1 && TryUpgrade(Barracks))
                    {
                        continue;
                    }

                    if (_state.Survivors.Count < 2 && _state.Survivors.Count < _state.SurvivorCap)
                    {
                        RecruitSurvivor();
                        continue;
                    }

                    if (_state.Buildings[Barracks].Level < 2 && TryUpgrade(Barracks))
                    {
                        continue;
                    }

                    if (_state.Buildings[Barracks].Level >= 2 && _state.Survivors.Count < Math.Min(3, _state.SurvivorCap))
                    {
                        RecruitSurvivor();
                        continue;
                    }

                    if (_state.Buildings[Workshop].Level < 1)
                    {
                        if (!TryUpgrade(Workshop))
                        {
                            RunExpedition(AbandonedStore);
                        }
                        continue;
                    }

                    if (_state.Buildings[MushroomBeds].Level < 1 && TryUpgrade(MushroomBeds))
                    {
                        continue;
                    }

                    if (_state.Buildings[WaterCollector].Level < 1 && TryUpgrade(WaterCollector))
                    {
                        continue;
                    }

                    if (_state.Buildings[RadioTower].Level < 1)
                    {
                        if (!TryUpgrade(RadioTower))
                        {
                            AcquireResource(WeaponParts);
                        }
                        continue;
                    }

                    if (_state.Buildings[Infirmary].Level < _config.Balance.HealingRequiredBuildingLevel)
                    {
                        if (!TryUpgrade(Infirmary))
                        {
                            AcquireResource(Medicine);
                        }
                        continue;
                    }

                    if (ResourceSystem.GetAmount(_state, WeaponParts) < 10)
                    {
                        AcquireResource(WeaponParts);
                        continue;
                    }

                    if (_state.Buildings[RadioTower].Level < 2)
                    {
                        if (!TryUpgrade(RadioTower))
                        {
                            AcquireResource(Scrap);
                        }
                        continue;
                    }
                }

                Assert.Fail(Describe("Production path did not reach Mutant Tunnel within action budget."));
            }

            public string Describe(string message)
            {
                var start = Math.Max(0, _log.Count - 16);
                var tail = new List<string>();
                for (var i = start; i < _log.Count; i++)
                {
                    tail.Add(_log[i]);
                }

                return message + " actions=" + ActionCount +
                       " resources=" + FormatResources() +
                       " buildings=" + FormatBuildings() +
                       " zones=" + FormatZones() +
                       " logTail=" + string.Join(" | ", tail.ToArray());
            }

            private bool TryUpgrade(string buildingId)
            {
                var building = _state.Buildings[buildingId];
                var next = BuildingSystem.GetLevel(_config.Buildings[buildingId], building.Level + 1);
                if (next == null)
                {
                    return true;
                }

                if (!CanAcquireCost(next.Cost))
                {
                    return false;
                }

                EnsureCost(next.Cost);
                var result = BuildingSystem.Upgrade(_state, _config, buildingId);
                if (!result.Validation.IsValid)
                {
                    Assert.Fail(Describe("Upgrade failed: " + buildingId + " " + string.Join(", ", result.Validation.Errors)));
                }

                ActionCount++;
                _log.Add("upgrade " + buildingId + "=>" + _state.Buildings[buildingId].Level + " " + FormatResources());
                return true;
            }

            private void RecruitSurvivor()
            {
                EnsureCost(RecruitmentSystem.CalculateCost(_state, _config));
                var broadcast = RecruitmentSystem.Broadcast(_state, _config, new BroadcastRecruitmentRequest
                {
                    Seed = _seed++,
                    NowUnixMs = CurrentUnixMs()
                });
                if (!broadcast.Validation.IsValid || broadcast.CandidateIds.Count == 0)
                {
                    Assert.Fail(Describe("Recruitment broadcast failed: " + string.Join(", ", broadcast.Validation.Errors)));
                }

                var recruited = RecruitmentSystem.Recruit(_state, _config, new RecruitSurvivorRequest
                {
                    CandidateId = SelectBestCandidate(broadcast.CandidateIds),
                    NowUnixMs = CurrentUnixMs()
                });
                if (!recruited.Validation.IsValid)
                {
                    Assert.Fail(Describe("Recruitment failed: " + string.Join(", ", recruited.Validation.Errors)));
                }

                ActionCount++;
                _log.Add("recruit " + recruited.Survivor.Name + " " + FormatResources());
            }

            private bool RunExpedition(string zoneId)
            {
                return RunExpedition(zoneId, Math.Max(1, _state.SquadSize));
            }

            private bool RunExpedition(string zoneId, int maxSurvivors)
            {
                UnlockSystem.RefreshZoneUnlocks(_state, _config);
                if (!_state.Zones[zoneId].IsUnlocked)
                {
                    Assert.Fail(Describe("Zone is locked: " + zoneId));
                }

                var zone = _config.Zones[zoneId];
                var policy = _config.Policies[BalancedPolicy];
                var survivorIds = PrepareExpeditionRosterAndCost(zone, policy, maxSurvivors, zoneId);
                var result = ExpeditionLauncher.Launch(_state, _config, new LaunchExpeditionRequest
                {
                    ZoneId = zoneId,
                    PolicyId = BalancedPolicy,
                    SurvivorIds = survivorIds,
                    Seed = _seed++,
                    NowUnixMs = CurrentUnixMs(),
                    ConfirmWarnings = true
                });
                if (!result.Validation.IsValid || result.Expedition == null)
                {
                    Assert.Fail(Describe("Expedition launch failed: " + zoneId + " " + string.Join(", ", result.Validation.Errors)));
                }

                Tick(result.Expedition.ExpectedDurationSeconds + 5);
                RecoverSquad();
                if (result.Expedition.Status != ExpeditionStatus.Completed)
                {
                    ActionCount++;
                    _log.Add("expedition " + zoneId + "=>" + result.Expedition.Status + " " + FormatResources());
                    if (zoneId == MutantTunnel)
                    {
                        return false;
                    }

                    return false;
                }

                ActionCount++;
                _log.Add("expedition " + zoneId + " " + FormatResources());
                return true;
            }

            private List<string> PrepareExpeditionRosterAndCost(
                ZoneDefinition zone,
                ExpeditionPolicyDefinition policy,
                int maxSurvivors,
                string zoneId)
            {
                for (var guard = 0; guard < 20; guard++)
                {
                    RecoverSquad();
                    var survivorIds = SelectIdleSurvivors(maxSurvivors);
                    if (survivorIds.Count == 0)
                    {
                        Assert.Fail(Describe("No idle survivors for expedition: " + zoneId));
                    }

                    var cost = ExpeditionValidator.CalculateCost(_config, zone, policy, survivorIds.Count);
                    if (ResourceSystem.CanSpend(_state, cost))
                    {
                        return survivorIds;
                    }

                    EnsureCost(cost);
                }

                Assert.Fail(Describe("Could not prepare expedition: " + zoneId));
                return new List<string>();
            }

            private List<string> SelectIdleSurvivors()
            {
                return SelectIdleSurvivors(Math.Max(1, _state.SquadSize));
            }

            private List<string> SelectIdleSurvivors(int maxSurvivors)
            {
                var survivors = new List<string>();
                var max = Math.Max(1, Math.Min(maxSurvivors, _state.SquadSize));
                foreach (var survivor in _state.Survivors)
                {
                    if (survivors.Count >= max)
                    {
                        break;
                    }

                    if (survivor.State == SurvivorActivityState.Idle)
                    {
                        InsertByPower(survivors, survivor, max);
                    }
                }

                return survivors;
            }

            private void InsertByPower(List<string> survivors, SurvivorState survivor, int max)
            {
                var power = SquadPowerSystem.CalculateSurvivorPower(_state, _config, survivor);
                var insertAt = survivors.Count;
                for (var i = 0; i < survivors.Count; i++)
                {
                    var existing = WorkshopSystem.FindSurvivor(_state, survivors[i]);
                    if (existing != null && power > SquadPowerSystem.CalculateSurvivorPower(_state, _config, existing))
                    {
                        insertAt = i;
                        break;
                    }
                }

                survivors.Insert(insertAt, survivor.Id);
                if (survivors.Count > max)
                {
                    survivors.RemoveAt(survivors.Count - 1);
                }
            }

            private string SelectBestCandidate(List<string> candidateIds)
            {
                var bestId = candidateIds[0];
                var bestPower = double.MinValue;
                foreach (var candidateId in candidateIds)
                {
                    RecruitableSurvivorDefinition candidate;
                    if (!_config.RecruitableSurvivors.TryGetValue(candidateId, out candidate))
                    {
                        continue;
                    }

                    var power = EstimateCandidatePower(candidate);
                    if (power > bestPower)
                    {
                        bestPower = power;
                        bestId = candidate.Id;
                    }
                }

                return bestId;
            }

            private double EstimateCandidatePower(RecruitableSurvivorDefinition candidate)
            {
                ItemDefinition weapon;
                var weaponDamage = _config.Items.TryGetValue(candidate.WeaponItemId, out weapon) ? weapon.BaseDamage : 1;
                return 30 * 0.15 +
                       SkillValue(candidate.Skills, "melee") * 0.7 +
                       SkillValue(candidate.Skills, "firearms") * 0.7 +
                       weaponDamage * 2 +
                       2;
            }

            private static int SkillValue(Dictionary<string, int> skills, string skillId)
            {
                int value;
                return skills != null && skills.TryGetValue(skillId, out value) ? value : 0;
            }

            private bool CanAcquireCost(Dictionary<string, int> cost)
            {
                foreach (var pair in cost)
                {
                    if (!CanAcquireResource(pair.Key))
                    {
                        return false;
                    }
                }

                return true;
            }

            private bool CanAcquireResource(string resourceId)
            {
                return resourceId == Scrap ||
                       resourceId == Food ||
                       resourceId == Water ||
                       resourceId == WeaponParts ||
                       resourceId == Medicine;
            }

            private void EnsureCost(Dictionary<string, int> cost)
            {
                for (var guard = 0; guard < 80; guard++)
                {
                    var missing = FindMissingCost(cost);
                    if (string.IsNullOrEmpty(missing))
                    {
                        return;
                    }

                    AcquireResource(missing);
                }

                var unresolved = FindMissingCost(cost);
                Assert.Fail(Describe("Could not acquire resource: " + unresolved + " need=" + RequiredAmount(cost, unresolved)));
            }

            private string FindMissingCost(Dictionary<string, int> cost)
            {
                if (IsMissing(cost, Water))
                {
                    return Water;
                }

                foreach (var pair in cost)
                {
                    if (ResourceSystem.GetAmount(_state, pair.Key) < pair.Value)
                    {
                        return pair.Key;
                    }
                }

                return string.Empty;
            }

            private bool IsMissing(Dictionary<string, int> cost, string resourceId)
            {
                int required;
                return cost.TryGetValue(resourceId, out required) && ResourceSystem.GetAmount(_state, resourceId) < required;
            }

            private static int RequiredAmount(Dictionary<string, int> cost, string resourceId)
            {
                int amount;
                return !string.IsNullOrEmpty(resourceId) && cost.TryGetValue(resourceId, out amount) ? amount : 0;
            }

            private void AcquireResource(string resourceId)
            {
                if (resourceId == Food)
                {
                    if (_state.Buildings[MushroomBeds].Level > 0)
                    {
                        Tick(60, true);
                        _log.Add("wait_food " + FormatResources());
                        return;
                    }

                    if (CanPayExpeditionCost(AbandonedStore, 1))
                    {
                        RunExpedition(AbandonedStore, 1);
                    }
                    else
                    {
                        RunEmergencyScavenge();
                    }
                    return;
                }

                if (resourceId == Water)
                {
                    if (_state.Buildings[WaterCollector].Level > 0)
                    {
                        Tick(60, true);
                        _log.Add("wait_water " + FormatResources());
                        return;
                    }

                    if (_state.Zones[DrySuburb].IsUnlocked && CanPayExpeditionCost(DrySuburb, 1))
                    {
                        RunExpedition(DrySuburb, 1);
                    }
                    else
                    {
                        RunEmergencyScavenge();
                    }
                    return;
                }

                if (resourceId == WeaponParts)
                {
                    if (_state.Zones[PoliceOutpost].IsUnlocked)
                    {
                        RunExpedition(PoliceOutpost);
                    }
                    else
                    {
                        RunExpedition(AbandonedStore);
                    }
                    return;
                }

                if (resourceId == Medicine && _state.Zones["ruined_clinic"].IsUnlocked)
                {
                    RunExpedition("ruined_clinic", 1);
                    return;
                }

                if (resourceId == Scrap && _state.Zones[PoliceOutpost].IsUnlocked)
                {
                    RunExpedition(PoliceOutpost);
                    return;
                }

                RunExpedition(AbandonedStore);
            }

            private bool CanPayExpeditionCost(string zoneId)
            {
                return CanPayExpeditionCost(zoneId, Math.Max(1, _state.SquadSize));
            }

            private bool CanPayExpeditionCost(string zoneId, int maxSurvivors)
            {
                var survivors = SelectIdleSurvivors(maxSurvivors);
                var survivorCount = Math.Max(1, survivors.Count);
                var zone = _config.Zones[zoneId];
                var policy = _config.Policies[BalancedPolicy];
                return ResourceSystem.CanSpend(_state, ExpeditionValidator.CalculateCost(_config, zone, policy, survivorCount));
            }

            private void RunEmergencyScavenge()
            {
                if (_state.Recovery.EmergencyScavengeCooldownRemainingSeconds > 0)
                {
                    Tick(_state.Recovery.EmergencyScavengeCooldownRemainingSeconds + 1, true);
                }

                var result = RecoverySystem.StartEmergencyScavenge(_state, _config, new EmergencyScavengeRequest
                {
                    NowUnixMs = CurrentUnixMs()
                });
                if (!result.Validation.IsValid)
                {
                    Assert.Fail(Describe("Emergency scavenge failed: " + string.Join(", ", result.Validation.Errors)));
                }

                Tick(_config.Balance.EmergencyScavengeDurationSeconds + 1, true);
                ActionCount++;
                _log.Add("emergency " + FormatResources());
            }

            private void RecoverSquad()
            {
                foreach (var survivor in _state.Survivors)
                {
                    if (survivor.State == SurvivorActivityState.Wounded)
                    {
                        RecoverWounded(survivor);
                    }
                }

                foreach (var survivor in _state.Survivors)
                {
                    if (survivor.State == SurvivorActivityState.Idle && survivor.Fatigue >= 50)
                    {
                        RestSystem.StartRest(_state, _config, new StartRestRequest { SurvivorId = survivor.Id });
                    }

                    if (survivor.State == SurvivorActivityState.Resting)
                    {
                        Tick(Math.Ceiling(survivor.Fatigue / Math.Max(1, _config.Balance.RestFatigueRecoveryPerMinute)) * 60 + 1, true);
                    }
                }
            }

            private void RecoverWounded(SurvivorState survivor)
            {
                if (_state.Buildings[Infirmary].Level < _config.Balance.HealingRequiredBuildingLevel)
                {
                    if (SelectIdleSurvivors().Count > 0)
                    {
                        return;
                    }

                    Assert.Fail(Describe("Only available survivor was wounded before infirmary was ready."));
                }

                if (ResourceSystem.GetAmount(_state, Medicine) >= Math.Max(0, _config.Balance.HealingMedicineCost))
                {
                    HealingSystem.UseMedicine(_state, _config, new UseMedicineRequest { SurvivorId = survivor.Id });
                }

                if (survivor.State == SurvivorActivityState.Wounded)
                {
                    Tick(_config.Balance.HealingDefaultWoundDurationSeconds + 1, true);
                }
            }

            private void Tick(double seconds)
            {
                Tick(seconds, false);
            }

            private void Tick(double seconds, bool countIdleWait)
            {
                if (countIdleWait)
                {
                    LongestIdleWaitSeconds = Math.Max(LongestIdleWaitSeconds, seconds);
                }

                var remaining = Math.Max(0, seconds);
                while (remaining > 0)
                {
                    var dt = Math.Min(10, remaining);
                    _state.TotalPlayTimeSeconds += dt;
                    CampUpkeepSystem.Tick(_state, _config, dt);
                    BuildingSystem.TickProduction(_state, _config, dt);
                    ExpeditionSimulator.TickAll(_state, _config, dt);
                    HealingSystem.Tick(_state, _config, dt);
                    RestSystem.Tick(_state, _config, dt);
                    RecoverySystem.Tick(_state, _config, dt);
                    remaining -= dt;
                }
            }

            private long CurrentUnixMs()
            {
                return (long)Math.Round(_state.TotalPlayTimeSeconds * 1000);
            }

            private string FormatResources()
            {
                var entries = new List<string>();
                foreach (var pair in _state.Resources)
                {
                    entries.Add(pair.Key + "=" + pair.Value);
                }

                return string.Join(",", entries.ToArray());
            }

            private string FormatBuildings()
            {
                return "barracks=" + _state.Buildings[Barracks].Level +
                       ",workshop=" + _state.Buildings[Workshop].Level +
                       ",food=" + _state.Buildings[MushroomBeds].Level +
                       ",water=" + _state.Buildings[WaterCollector].Level +
                       ",radio=" + _state.Buildings[RadioTower].Level +
                       ",infirmary=" + _state.Buildings[Infirmary].Level;
            }

            private string FormatZones()
            {
                return "store=" + _state.Zones[AbandonedStore].Completions +
                       ",suburb=" + _state.Zones[DrySuburb].Completions + "/" + _state.Zones[DrySuburb].IsUnlocked +
                       ",police=" + _state.Zones[PoliceOutpost].Completions + "/" + _state.Zones[PoliceOutpost].IsUnlocked +
                       ",mutant=" + _state.Zones[MutantTunnel].Completions + "/" + _state.Zones[MutantTunnel].IsUnlocked;
            }
        }
    }
}
