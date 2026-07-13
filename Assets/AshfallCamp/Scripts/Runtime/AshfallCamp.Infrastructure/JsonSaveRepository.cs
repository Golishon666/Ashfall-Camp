using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Domain;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AshfallCamp.Infrastructure
{
    public sealed class JsonSaveRepository : ISaveRepository
    {
        private readonly string _folderPath;
        private readonly string _savePath;
        private readonly string _backupPath;

        public JsonSaveRepository()
            : this(Path.Combine(UnityEngine.Application.persistentDataPath, "AshfallCamp"))
        {
        }

        public JsonSaveRepository(string folderPath)
        {
            _folderPath = folderPath;
            _savePath = Path.Combine(_folderPath, "save.json");
            _backupPath = Path.Combine(_folderPath, "save.backup.json");
        }

        public UniTask<SaveRepositoryLoadResult> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var primary = TryLoad(_savePath);
            if (primary != null)
            {
                return UniTask.FromResult(new SaveRepositoryLoadResult { State = primary });
            }

            var backup = TryLoad(_backupPath);
            return UniTask.FromResult(backup == null
                ? null
                : new SaveRepositoryLoadResult { State = backup, UsedBackup = true });
        }

        public UniTask SaveAsync(GameState state, CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            Directory.CreateDirectory(_folderPath);
            if (File.Exists(_savePath))
            {
                File.Copy(_savePath, _backupPath, true);
            }

            var dto = SaveData.FromState(state);
            var json = JsonUtility.ToJson(dto, true);
            File.WriteAllText(_savePath, json);
            File.Copy(_savePath, _backupPath, true);
            return UniTask.CompletedTask;
        }

        private static GameState TryLoad(string path)
        {
            if (!File.Exists(path)) return null;
            try
            {
                var json = File.ReadAllText(path);
                var dto = JsonUtility.FromJson<SaveData>(json);
                return dto == null ? null : dto.ToState();
            }
            catch (Exception)
            {
                return null;
            }
        }

        [Serializable]
        private sealed class SaveData
        {
            public string Version;
            public long CreatedAtUnixMs;
            public long LastSaveAtUnixMs;
            public double TotalPlayTimeSeconds;
            public double CampUpkeepAccumulatorSeconds;
            public int NextId;
            public int SurvivorCap;
            public int SquadSize;
            public List<IntPairData> Resources = new List<IntPairData>();
            public List<IntPairData> ResourceCaps = new List<IntPairData>();
            public List<DoublePairData> ResourceProductionRemainders = new List<DoublePairData>();
            public List<DoublePairData> RestFatigueRecoveryRemainders = new List<DoublePairData>();
            public List<SurvivorSaveData> Survivors = new List<SurvivorSaveData>();
            public List<InventoryItemSaveData> Inventory = new List<InventoryItemSaveData>();
            public List<BuildingSaveData> Buildings = new List<BuildingSaveData>();
            public List<ZoneSaveData> Zones = new List<ZoneSaveData>();
            public List<UpgradeSaveData> Upgrades = new List<UpgradeSaveData>();
            public List<ExpeditionSaveData> Expeditions = new List<ExpeditionSaveData>();
            public List<CampEventSaveData> CampEvents = new List<CampEventSaveData>();
            public RecruitmentSaveData Recruitment = new RecruitmentSaveData();
            public RecoverySaveData Recovery = new RecoverySaveData();
            public OfflineReportSaveData LastOfflineReport;
            public GameProgressSaveData Progress = new GameProgressSaveData();
            public GameSettingsSaveData Settings = new GameSettingsSaveData();
            public GameStatisticsSaveData Statistics = new GameStatisticsSaveData();
            public bool MapFogInitialized;
            public List<MapCellSaveData> RevealedMapCells = new List<MapCellSaveData>();

            public static SaveData FromState(GameState state)
            {
                var dto = new SaveData
                {
                    Version = state.Version,
                    CreatedAtUnixMs = state.CreatedAtUnixMs,
                    LastSaveAtUnixMs = state.LastSaveAtUnixMs,
                    TotalPlayTimeSeconds = state.TotalPlayTimeSeconds,
                    CampUpkeepAccumulatorSeconds = state.CampUpkeepAccumulatorSeconds,
                    NextId = state.NextId,
                    SurvivorCap = state.SurvivorCap,
                    SquadSize = state.SquadSize,
                    Recruitment = RecruitmentSaveData.FromState(state.Recruitment),
                    Recovery = RecoverySaveData.FromState(state.Recovery),
                    LastOfflineReport = OfflineReportSaveData.FromState(state.LastOfflineReport),
                    Progress = GameProgressSaveData.FromState(state.Progress),
                    Settings = new GameSettingsSaveData { AutosaveEnabled = state.Settings.AutosaveEnabled },
                    Statistics = GameStatisticsSaveData.FromState(state.Statistics),
                    MapFogInitialized = state.MapFogInitialized
                };
                dto.Resources = FromDictionary(state.Resources);
                dto.ResourceCaps = FromDictionary(state.ResourceCaps);
                dto.ResourceProductionRemainders = FromDictionary(state.ResourceProductionRemainders);
                dto.RestFatigueRecoveryRemainders = FromDictionary(state.RestFatigueRecoveryRemainders);
                foreach (var item in state.Survivors) dto.Survivors.Add(SurvivorSaveData.FromState(item));
                foreach (var item in state.Inventory) dto.Inventory.Add(InventoryItemSaveData.FromState(item));
                foreach (var item in state.Buildings.Values) dto.Buildings.Add(BuildingSaveData.FromState(item));
                foreach (var item in state.Zones.Values) dto.Zones.Add(ZoneSaveData.FromState(item));
                foreach (var item in state.Upgrades.Values) dto.Upgrades.Add(UpgradeSaveData.FromState(item));
                foreach (var item in state.Expeditions) dto.Expeditions.Add(ExpeditionSaveData.FromState(item));
                foreach (var item in state.CampEvents) dto.CampEvents.Add(CampEventSaveData.FromState(item));
                if (state.RevealedMapCells != null)
                {
                    foreach (var cell in state.RevealedMapCells) dto.RevealedMapCells.Add(MapCellSaveData.FromState(cell));
                }
                return dto;
            }

            public GameState ToState()
            {
                var state = new GameState
                {
                    Version = string.IsNullOrEmpty(Version) ? GameConstants.CurrentSaveVersion : Version,
                    CreatedAtUnixMs = CreatedAtUnixMs,
                    LastSaveAtUnixMs = LastSaveAtUnixMs,
                    TotalPlayTimeSeconds = TotalPlayTimeSeconds,
                    CampUpkeepAccumulatorSeconds = CampUpkeepAccumulatorSeconds,
                    NextId = NextId <= 0 ? 1 : NextId,
                    SurvivorCap = SurvivorCap <= 0 ? 1 : SurvivorCap,
                    SquadSize = SquadSize <= 0 ? 1 : SquadSize,
                    Resources = ToDictionary(Resources),
                    ResourceCaps = ToDictionary(ResourceCaps),
                    ResourceProductionRemainders = ToDoubleDictionary(ResourceProductionRemainders),
                    RestFatigueRecoveryRemainders = ToDoubleDictionary(RestFatigueRecoveryRemainders),
                    Recruitment = Recruitment != null ? Recruitment.ToState() : new RecruitmentState(),
                    Recovery = Recovery != null ? Recovery.ToState() : new RecoveryActionState(),
                    LastOfflineReport = LastOfflineReport != null ? LastOfflineReport.ToState() : null,
                    Progress = Progress != null ? Progress.ToState() : new GameProgressState(),
                    Settings = Settings != null ? Settings.ToState() : new GameSettings(),
                    Statistics = Statistics != null ? Statistics.ToState() : new GameStatistics(),
                    MapFogInitialized = MapFogInitialized,
                    RevealedMapCells = new List<MapCellCoordinate>()
                };
                foreach (var item in Survivors) state.Survivors.Add(item.ToState());
                foreach (var item in Inventory) state.Inventory.Add(item.ToState());
                foreach (var item in Buildings) state.Buildings[item.Id] = item.ToState();
                foreach (var item in Zones) state.Zones[item.Id] = item.ToState();
                foreach (var item in Upgrades) state.Upgrades[item.Id] = item.ToState();
                foreach (var item in Expeditions) state.Expeditions.Add(item.ToState());
                if (CampEvents != null)
                {
                    foreach (var item in CampEvents) state.CampEvents.Add(item.ToState());
                }
                if (RevealedMapCells != null)
                {
                    foreach (var cell in RevealedMapCells) state.RevealedMapCells.Add(cell.ToState());
                }

                return state;
            }
        }

        [Serializable]
        private sealed class MapCellSaveData
        {
            public int X;
            public int Y;

            public static MapCellSaveData FromState(MapCellCoordinate cell)
            {
                return new MapCellSaveData { X = cell.X, Y = cell.Y };
            }

            public MapCellCoordinate ToState()
            {
                return new MapCellCoordinate(X, Y);
            }
        }

        [Serializable]
        private sealed class SurvivorSaveData
        {
            public string Id;
            public string Name;
            public int Level;
            public int Xp;
            public string BackgroundId;
            public List<string> TraitIds = new List<string>();
            public SurvivorActivityState State;
            public int Health;
            public int MaxHealth;
            public int Fatigue;
            public int Morale;
            public List<IntPairData> Skills = new List<IntPairData>();
            public List<IntPairData> SkillXp = new List<IntPairData>();
            public SurvivorEquipmentSaveData Equipment = new SurvivorEquipmentSaveData();
            public List<StatusEffectSaveData> StatusEffects = new List<StatusEffectSaveData>();
            public string CurrentExpeditionId;

            public static SurvivorSaveData FromState(SurvivorState state)
            {
                var statusEffects = new List<StatusEffectSaveData>();
                foreach (var effect in state.StatusEffects)
                {
                    statusEffects.Add(StatusEffectSaveData.FromState(effect));
                }

                return new SurvivorSaveData
                {
                    Id = state.Id,
                    Name = state.Name,
                    Level = state.Level,
                    Xp = state.Xp,
                    BackgroundId = state.BackgroundId,
                    TraitIds = new List<string>(state.TraitIds),
                    State = state.State,
                    Health = state.Health,
                    MaxHealth = state.MaxHealth,
                    Fatigue = state.Fatigue,
                    Morale = state.Morale,
                    Skills = FromDictionary(state.Skills),
                    SkillXp = FromDictionary(state.SkillXp),
                    Equipment = SurvivorEquipmentSaveData.FromState(state.Equipment),
                    StatusEffects = statusEffects,
                    CurrentExpeditionId = state.CurrentExpeditionId
                };
            }

            public SurvivorState ToState()
            {
                var survivor = new SurvivorState
                {
                    Id = Id,
                    Name = Name,
                    Level = Level,
                    Xp = Xp,
                    BackgroundId = BackgroundId,
                    TraitIds = TraitIds ?? new List<string>(),
                    State = State,
                    Health = Health,
                    MaxHealth = MaxHealth,
                    Fatigue = Fatigue,
                    Morale = Morale,
                    Skills = ToDictionary(Skills),
                    SkillXp = ToDictionary(SkillXp),
                    Equipment = Equipment != null ? Equipment.ToState() : new SurvivorEquipmentState(),
                    CurrentExpeditionId = CurrentExpeditionId
                };

                if (StatusEffects != null)
                {
                    foreach (var effect in StatusEffects)
                    {
                        survivor.StatusEffects.Add(effect.ToState());
                    }
                }

                return survivor;
            }
        }

        [Serializable]
        private sealed class StatusEffectSaveData
        {
            public string Id;
            public double RemainingSeconds;

            public static StatusEffectSaveData FromState(StatusEffectState state)
            {
                return new StatusEffectSaveData { Id = state.Id, RemainingSeconds = state.RemainingSeconds };
            }

            public StatusEffectState ToState()
            {
                return new StatusEffectState { Id = Id, RemainingSeconds = RemainingSeconds };
            }
        }

        [Serializable]
        private sealed class SurvivorEquipmentSaveData
        {
            public string WeaponItemUid;
            public string ArmorItemUid;
            public string UtilityItemUid;

            public static SurvivorEquipmentSaveData FromState(SurvivorEquipmentState state)
            {
                return new SurvivorEquipmentSaveData
                {
                    WeaponItemUid = state.WeaponItemUid,
                    ArmorItemUid = state.ArmorItemUid,
                    UtilityItemUid = state.UtilityItemUid
                };
            }

            public SurvivorEquipmentState ToState()
            {
                return new SurvivorEquipmentState
                {
                    WeaponItemUid = WeaponItemUid,
                    ArmorItemUid = ArmorItemUid,
                    UtilityItemUid = UtilityItemUid
                };
            }
        }

        [Serializable]
        private sealed class InventoryItemSaveData
        {
            public string Uid;
            public string ItemId;
            public int Level;
            public int Durability;
            public int MaxDurability;
            public string EquippedBySurvivorId;

            public static InventoryItemSaveData FromState(InventoryItemState state)
            {
                return new InventoryItemSaveData
                {
                    Uid = state.Uid,
                    ItemId = state.ItemId,
                    Level = state.Level,
                    Durability = state.Durability,
                    MaxDurability = state.MaxDurability,
                    EquippedBySurvivorId = state.EquippedBySurvivorId
                };
            }

            public InventoryItemState ToState()
            {
                return new InventoryItemState
                {
                    Uid = Uid,
                    ItemId = ItemId,
                    Level = Level,
                    Durability = Durability,
                    MaxDurability = MaxDurability,
                    EquippedBySurvivorId = EquippedBySurvivorId
                };
            }
        }

        [Serializable]
        private sealed class BuildingSaveData
        {
            public string Id;
            public int Level;
            public bool IsUnlocked;
            public long UpgradeStartedAtUnixMs;
            public long UpgradeFinishedAtUnixMs;
            public int AssignedWorkers;
            public int ConditionPercent;

            public static BuildingSaveData FromState(BuildingState state)
            {
                return new BuildingSaveData { Id = state.Id, Level = state.Level, IsUnlocked = state.IsUnlocked, UpgradeStartedAtUnixMs = state.UpgradeStartedAtUnixMs, UpgradeFinishedAtUnixMs = state.UpgradeFinishedAtUnixMs, AssignedWorkers = state.AssignedWorkers, ConditionPercent = state.ConditionPercent };
            }

            public BuildingState ToState()
            {
                return new BuildingState { Id = Id, Level = Level, IsUnlocked = IsUnlocked, UpgradeStartedAtUnixMs = UpgradeStartedAtUnixMs, UpgradeFinishedAtUnixMs = UpgradeFinishedAtUnixMs, AssignedWorkers = AssignedWorkers, ConditionPercent = ConditionPercent };
            }
        }

        [Serializable]
        private sealed class ZoneSaveData
        {
            public string Id;
            public bool IsUnlocked;
            public int Completions;
            public double Familiarity;
            public double BestClearTimeSeconds;

            public static ZoneSaveData FromState(ZoneState state)
            {
                return new ZoneSaveData { Id = state.Id, IsUnlocked = state.IsUnlocked, Completions = state.Completions, Familiarity = state.Familiarity, BestClearTimeSeconds = state.BestClearTimeSeconds };
            }

            public ZoneState ToState()
            {
                return new ZoneState { Id = Id, IsUnlocked = IsUnlocked, Completions = Completions, Familiarity = Familiarity, BestClearTimeSeconds = BestClearTimeSeconds };
            }
        }

        [Serializable]
        private sealed class UpgradeSaveData
        {
            public string Id;
            public bool IsUnlocked;
            public bool IsPurchased;

            public static UpgradeSaveData FromState(UpgradeState state)
            {
                return new UpgradeSaveData { Id = state.Id, IsUnlocked = state.IsUnlocked, IsPurchased = state.IsPurchased };
            }

            public UpgradeState ToState()
            {
                return new UpgradeState { Id = Id, IsUnlocked = IsUnlocked, IsPurchased = IsPurchased };
            }
        }

        [Serializable]
        private sealed class ExpeditionSaveData
        {
            public string Id;
            public string ZoneId;
            public List<string> SurvivorIds = new List<string>();
            public string PolicyId;
            public long StartedAtUnixMs;
            public double ExpectedDurationSeconds;
            public double ElapsedSeconds;
            public double Progress;
            public double StepAccumulatorSeconds;
            public ExpeditionStatus Status;
            public uint RandomState;
            public int Noise;
            public List<ExpeditionLogSaveData> Log = new List<ExpeditionLogSaveData>();
            public List<IntPairData> AccumulatedLoot = new List<IntPairData>();
            public List<InventoryItemSaveData> FoundItems = new List<InventoryItemSaveData>();
            public List<IntPairData> EnemiesDefeated = new List<IntPairData>();
            public List<string> WoundedSurvivorIds = new List<string>();
            public List<IntPairData> EquipmentDurabilityLost = new List<IntPairData>();
            public List<string> BrokenItemUids = new List<string>();
            public string WorldTileId;
            public MapCellSaveData TargetCell;
            public List<string> RouteTileIds = new List<string>();
            public bool ReturnTravelApplied;

            public static ExpeditionSaveData FromState(ExpeditionState state)
            {
                var log = new List<ExpeditionLogSaveData>();
                foreach (var entry in state.Log)
                {
                    log.Add(ExpeditionLogSaveData.FromState(entry));
                }

                var foundItems = new List<InventoryItemSaveData>();
                foreach (var item in state.FoundItems)
                {
                    foundItems.Add(InventoryItemSaveData.FromState(item));
                }

                return new ExpeditionSaveData
                {
                    Id = state.Id,
                    ZoneId = state.ZoneId,
                    SurvivorIds = new List<string>(state.SurvivorIds),
                    PolicyId = state.PolicyId,
                    StartedAtUnixMs = state.StartedAtUnixMs,
                    ExpectedDurationSeconds = state.ExpectedDurationSeconds,
                    ElapsedSeconds = state.ElapsedSeconds,
                    Progress = state.Progress,
                    StepAccumulatorSeconds = state.StepAccumulatorSeconds,
                    Status = state.Status,
                    RandomState = state.RandomState,
                    Noise = state.Noise,
                    Log = log,
                    AccumulatedLoot = FromDictionary(state.AccumulatedLoot),
                    FoundItems = foundItems,
                    EnemiesDefeated = FromDictionary(state.EnemiesDefeated),
                    WoundedSurvivorIds = new List<string>(state.WoundedSurvivorIds),
                    EquipmentDurabilityLost = FromDictionary(state.EquipmentDurabilityLost),
                    BrokenItemUids = new List<string>(state.BrokenItemUids)
                    ,WorldTileId = state.WorldTileId
                    ,TargetCell = new MapCellSaveData { X = state.TargetCell.X, Y = state.TargetCell.Y }
                    ,RouteTileIds = new List<string>(state.RouteTileIds)
                    ,ReturnTravelApplied = state.ReturnTravelApplied
                };
            }

            public ExpeditionState ToState()
            {
                var expedition = new ExpeditionState
                {
                    Id = Id,
                    ZoneId = ZoneId,
                    SurvivorIds = SurvivorIds ?? new List<string>(),
                    PolicyId = PolicyId,
                    StartedAtUnixMs = StartedAtUnixMs,
                    ExpectedDurationSeconds = ExpectedDurationSeconds,
                    ElapsedSeconds = ElapsedSeconds,
                    Progress = Progress,
                    StepAccumulatorSeconds = StepAccumulatorSeconds,
                    Status = Status,
                    RandomState = RandomState,
                    Noise = Noise,
                    AccumulatedLoot = ToDictionary(AccumulatedLoot),
                    EnemiesDefeated = ToDictionary(EnemiesDefeated),
                    WoundedSurvivorIds = WoundedSurvivorIds ?? new List<string>(),
                    EquipmentDurabilityLost = ToDictionary(EquipmentDurabilityLost),
                    BrokenItemUids = BrokenItemUids ?? new List<string>()
                    ,WorldTileId = WorldTileId ?? string.Empty
                    ,TargetCell = TargetCell != null ? new MapCellCoordinate(TargetCell.X, TargetCell.Y) : default(MapCellCoordinate)
                    ,RouteTileIds = RouteTileIds ?? new List<string>()
                    ,ReturnTravelApplied = ReturnTravelApplied
                };

                if (Log != null)
                {
                    foreach (var entry in Log)
                    {
                        expedition.Log.Add(entry.ToState());
                    }
                }

                if (FoundItems != null)
                {
                    foreach (var item in FoundItems)
                    {
                        expedition.FoundItems.Add(item.ToState());
                    }
                }

                return expedition;
            }
        }

        [Serializable]
        private sealed class ExpeditionLogSaveData
        {
            public double AtSeconds;
            public string Message;

            public static ExpeditionLogSaveData FromState(ExpeditionLogEntry state)
            {
                return new ExpeditionLogSaveData { AtSeconds = state.AtSeconds, Message = state.Message };
            }

            public ExpeditionLogEntry ToState()
            {
                return new ExpeditionLogEntry { AtSeconds = AtSeconds, Message = Message };
            }
        }

        [Serializable]
        private sealed class GameSettingsSaveData
        {
            public bool AutosaveEnabled = true;

            public GameSettings ToState()
            {
                return new GameSettings { AutosaveEnabled = AutosaveEnabled };
            }
        }

        [Serializable]
        private sealed class CampEventSaveData
        {
            public string Id;
            public string EventId;
            public string SubjectId;
            public string SubjectName;
            public string DetailId;
            public long AtUnixMs;

            public static CampEventSaveData FromState(CampEventState state)
            {
                return new CampEventSaveData
                {
                    Id = state.Id,
                    EventId = state.EventId,
                    SubjectId = state.SubjectId,
                    SubjectName = state.SubjectName,
                    DetailId = state.DetailId,
                    AtUnixMs = state.AtUnixMs
                };
            }

            public CampEventState ToState()
            {
                return new CampEventState
                {
                    Id = Id ?? string.Empty,
                    EventId = EventId ?? string.Empty,
                    SubjectId = SubjectId ?? string.Empty,
                    SubjectName = SubjectName ?? string.Empty,
                    DetailId = DetailId ?? string.Empty,
                    AtUnixMs = AtUnixMs
                };
            }
        }

        [Serializable]
        private sealed class RecruitmentSaveData
        {
            public List<string> PendingCandidateIds = new List<string>();
            public long LastBroadcastAtUnixMs;

            public static RecruitmentSaveData FromState(RecruitmentState state)
            {
                var dto = new RecruitmentSaveData();
                if (state == null) return dto;

                if (state.PendingCandidateIds != null)
                {
                    dto.PendingCandidateIds = new List<string>(state.PendingCandidateIds);
                }

                dto.LastBroadcastAtUnixMs = state.LastBroadcastAtUnixMs;
                return dto;
            }

            public RecruitmentState ToState()
            {
                return new RecruitmentState
                {
                    PendingCandidateIds = PendingCandidateIds != null ? new List<string>(PendingCandidateIds) : new List<string>(),
                    LastBroadcastAtUnixMs = LastBroadcastAtUnixMs
                };
            }
        }

        [Serializable]
        private sealed class RecoverySaveData
        {
            public bool EmergencyScavengeActive;
            public double EmergencyScavengeRemainingSeconds;
            public long EmergencyScavengeStartedAtUnixMs;
            public double EmergencyScavengeCooldownRemainingSeconds;

            public static RecoverySaveData FromState(RecoveryActionState state)
            {
                if (state == null) return new RecoverySaveData();
                return new RecoverySaveData
                {
                    EmergencyScavengeActive = state.EmergencyScavengeActive,
                    EmergencyScavengeRemainingSeconds = state.EmergencyScavengeRemainingSeconds,
                    EmergencyScavengeStartedAtUnixMs = state.EmergencyScavengeStartedAtUnixMs,
                    EmergencyScavengeCooldownRemainingSeconds = state.EmergencyScavengeCooldownRemainingSeconds
                };
            }

            public RecoveryActionState ToState()
            {
                return new RecoveryActionState
                {
                    EmergencyScavengeActive = EmergencyScavengeActive,
                    EmergencyScavengeRemainingSeconds = EmergencyScavengeRemainingSeconds,
                    EmergencyScavengeStartedAtUnixMs = EmergencyScavengeStartedAtUnixMs,
                    EmergencyScavengeCooldownRemainingSeconds = EmergencyScavengeCooldownRemainingSeconds
                };
            }
        }

        [Serializable]
        private sealed class OfflineReportSaveData
        {
            public double AppliedSeconds;
            public bool WasPresented;
            public List<IntPairData> ResourcesGained = new List<IntPairData>();
            public List<IntPairData> ResourcesSpent = new List<IntPairData>();
            public List<string> CompletedExpeditionIds = new List<string>();
            public List<string> CompletedBuildingIds = new List<string>();
            public List<string> WoundedSurvivorIds = new List<string>();
            public List<string> HealedSurvivorIds = new List<string>();

            public static OfflineReportSaveData FromState(OfflineProgressReport state)
            {
                if (state == null) return null;
                return new OfflineReportSaveData
                {
                    AppliedSeconds = state.AppliedSeconds,
                    WasPresented = state.WasPresented,
                    ResourcesGained = state.ResourcesGained != null ? FromDictionary(state.ResourcesGained) : new List<IntPairData>(),
                    ResourcesSpent = state.ResourcesSpent != null ? FromDictionary(state.ResourcesSpent) : new List<IntPairData>(),
                    CompletedExpeditionIds = state.CompletedExpeditionIds != null ? new List<string>(state.CompletedExpeditionIds) : new List<string>(),
                    CompletedBuildingIds = state.CompletedBuildingIds != null ? new List<string>(state.CompletedBuildingIds) : new List<string>(),
                    WoundedSurvivorIds = state.WoundedSurvivorIds != null ? new List<string>(state.WoundedSurvivorIds) : new List<string>(),
                    HealedSurvivorIds = state.HealedSurvivorIds != null ? new List<string>(state.HealedSurvivorIds) : new List<string>()
                };
            }

            public OfflineProgressReport ToState()
            {
                return new OfflineProgressReport
                {
                    AppliedSeconds = AppliedSeconds,
                    WasPresented = WasPresented,
                    ResourcesGained = ToDictionary(ResourcesGained),
                    ResourcesSpent = ToDictionary(ResourcesSpent),
                    CompletedExpeditionIds = CompletedExpeditionIds != null ? new List<string>(CompletedExpeditionIds) : new List<string>(),
                    CompletedBuildingIds = CompletedBuildingIds != null ? new List<string>(CompletedBuildingIds) : new List<string>(),
                    WoundedSurvivorIds = WoundedSurvivorIds != null ? new List<string>(WoundedSurvivorIds) : new List<string>(),
                    HealedSurvivorIds = HealedSurvivorIds != null ? new List<string>(HealedSurvivorIds) : new List<string>()
                };
            }
        }

        [Serializable]
        private sealed class GameProgressSaveData
        {
            public bool DemoCompleted;
            public string DemoCompletionId = string.Empty;
            public long DemoCompletedAtUnixMs;

            public static GameProgressSaveData FromState(GameProgressState state)
            {
                if (state == null) return new GameProgressSaveData();
                return new GameProgressSaveData
                {
                    DemoCompleted = state.DemoCompleted,
                    DemoCompletionId = state.DemoCompletionId,
                    DemoCompletedAtUnixMs = state.DemoCompletedAtUnixMs
                };
            }

            public GameProgressState ToState()
            {
                return new GameProgressState
                {
                    DemoCompleted = DemoCompleted,
                    DemoCompletionId = DemoCompletionId ?? string.Empty,
                    DemoCompletedAtUnixMs = DemoCompletedAtUnixMs
                };
            }
        }

        [Serializable]
        private sealed class GameStatisticsSaveData
        {
            public int ExpeditionsCompleted;
            public int ExpeditionsFailed;
            public int SurvivorsRecruited;
            public int CombatsWon;
            public int CombatsLost;
            public List<IntPairData> TotalResourcesGained = new List<IntPairData>();
            public List<IntPairData> TotalResourcesSpent = new List<IntPairData>();

            public static GameStatisticsSaveData FromState(GameStatistics state)
            {
                if (state == null) return new GameStatisticsSaveData();
                return new GameStatisticsSaveData
                {
                    ExpeditionsCompleted = state.ExpeditionsCompleted,
                    ExpeditionsFailed = state.ExpeditionsFailed,
                    SurvivorsRecruited = state.SurvivorsRecruited,
                    CombatsWon = state.CombatsWon,
                    CombatsLost = state.CombatsLost,
                    TotalResourcesGained = FromDictionary(state.TotalResourcesGained),
                    TotalResourcesSpent = FromDictionary(state.TotalResourcesSpent)
                };
            }

            public GameStatistics ToState()
            {
                return new GameStatistics
                {
                    ExpeditionsCompleted = ExpeditionsCompleted,
                    ExpeditionsFailed = ExpeditionsFailed,
                    SurvivorsRecruited = SurvivorsRecruited,
                    CombatsWon = CombatsWon,
                    CombatsLost = CombatsLost,
                    TotalResourcesGained = ToDictionary(TotalResourcesGained),
                    TotalResourcesSpent = ToDictionary(TotalResourcesSpent)
                };
            }
        }

        private static List<IntPairData> FromDictionary(Dictionary<string, int> values)
        {
            var result = new List<IntPairData>();
            if (values == null) return result;
            foreach (var pair in values)
            {
                result.Add(new IntPairData(pair.Key, pair.Value));
            }
            return result;
        }

        private static Dictionary<string, int> ToDictionary(List<IntPairData> values)
        {
            var result = new Dictionary<string, int>(StringComparer.Ordinal);
            if (values == null) return result;
            foreach (var pair in values)
            {
                if (!string.IsNullOrEmpty(pair.Id)) result[pair.Id] = pair.Value;
            }
            return result;
        }

        [Serializable]
        private sealed class DoublePairData
        {
            public string Id;
            public double Value;

            public DoublePairData()
            {
            }

            public DoublePairData(string id, double value)
            {
                Id = id;
                Value = value;
            }
        }

        private static List<DoublePairData> FromDictionary(Dictionary<string, double> values)
        {
            var result = new List<DoublePairData>();
            if (values == null) return result;
            foreach (var pair in values)
            {
                result.Add(new DoublePairData(pair.Key, pair.Value));
            }
            return result;
        }

        private static Dictionary<string, double> ToDoubleDictionary(List<DoublePairData> values)
        {
            var result = new Dictionary<string, double>(StringComparer.Ordinal);
            if (values == null) return result;
            foreach (var pair in values)
            {
                if (!string.IsNullOrEmpty(pair.Id)) result[pair.Id] = pair.Value;
            }
            return result;
        }
    }
}
