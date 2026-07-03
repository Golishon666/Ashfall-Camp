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

        public UniTask<GameState> LoadAsync(CancellationToken ct)
        {
            ct.ThrowIfCancellationRequested();
            var primary = TryLoad(_savePath);
            if (primary != null) return UniTask.FromResult(primary);
            var backup = TryLoad(_backupPath);
            return UniTask.FromResult(backup);
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
            public int NextId;
            public int SurvivorCap;
            public int SquadSize;
            public List<IntPairData> Resources = new List<IntPairData>();
            public List<IntPairData> ResourceCaps = new List<IntPairData>();
            public List<DoublePairData> ResourceProductionRemainders = new List<DoublePairData>();
            public List<SurvivorSaveData> Survivors = new List<SurvivorSaveData>();
            public List<InventoryItemSaveData> Inventory = new List<InventoryItemSaveData>();
            public List<BuildingSaveData> Buildings = new List<BuildingSaveData>();
            public List<ZoneSaveData> Zones = new List<ZoneSaveData>();
            public List<UpgradeSaveData> Upgrades = new List<UpgradeSaveData>();
            public List<ExpeditionSaveData> Expeditions = new List<ExpeditionSaveData>();
            public GameSettingsSaveData Settings = new GameSettingsSaveData();
            public GameStatisticsSaveData Statistics = new GameStatisticsSaveData();

            public static SaveData FromState(GameState state)
            {
                var dto = new SaveData
                {
                    Version = state.Version,
                    CreatedAtUnixMs = state.CreatedAtUnixMs,
                    LastSaveAtUnixMs = state.LastSaveAtUnixMs,
                    TotalPlayTimeSeconds = state.TotalPlayTimeSeconds,
                    NextId = state.NextId,
                    SurvivorCap = state.SurvivorCap,
                    SquadSize = state.SquadSize,
                    Settings = new GameSettingsSaveData { AutosaveEnabled = state.Settings.AutosaveEnabled },
                    Statistics = GameStatisticsSaveData.FromState(state.Statistics)
                };
                dto.Resources = FromDictionary(state.Resources);
                dto.ResourceCaps = FromDictionary(state.ResourceCaps);
                dto.ResourceProductionRemainders = FromDictionary(state.ResourceProductionRemainders);
                foreach (var item in state.Survivors) dto.Survivors.Add(SurvivorSaveData.FromState(item));
                foreach (var item in state.Inventory) dto.Inventory.Add(InventoryItemSaveData.FromState(item));
                foreach (var item in state.Buildings.Values) dto.Buildings.Add(BuildingSaveData.FromState(item));
                foreach (var item in state.Zones.Values) dto.Zones.Add(ZoneSaveData.FromState(item));
                foreach (var item in state.Upgrades.Values) dto.Upgrades.Add(UpgradeSaveData.FromState(item));
                foreach (var item in state.Expeditions) dto.Expeditions.Add(ExpeditionSaveData.FromState(item));
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
                    NextId = NextId <= 0 ? 1 : NextId,
                    SurvivorCap = SurvivorCap <= 0 ? 1 : SurvivorCap,
                    SquadSize = SquadSize <= 0 ? 1 : SquadSize,
                    Resources = ToDictionary(Resources),
                    ResourceCaps = ToDictionary(ResourceCaps),
                    ResourceProductionRemainders = ToDoubleDictionary(ResourceProductionRemainders),
                    Settings = Settings != null ? Settings.ToState() : new GameSettings(),
                    Statistics = Statistics != null ? Statistics.ToState() : new GameStatistics()
                };
                foreach (var item in Survivors) state.Survivors.Add(item.ToState());
                foreach (var item in Inventory) state.Inventory.Add(item.ToState());
                foreach (var item in Buildings) state.Buildings[item.Id] = item.ToState();
                foreach (var item in Zones) state.Zones[item.Id] = item.ToState();
                foreach (var item in Upgrades) state.Upgrades[item.Id] = item.ToState();
                foreach (var item in Expeditions) state.Expeditions.Add(item.ToState());
                return state;
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
            public string CurrentExpeditionId;

            public static SurvivorSaveData FromState(SurvivorState state)
            {
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
                    CurrentExpeditionId = state.CurrentExpeditionId
                };
            }

            public SurvivorState ToState()
            {
                return new SurvivorState
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

            public static BuildingSaveData FromState(BuildingState state)
            {
                return new BuildingSaveData { Id = state.Id, Level = state.Level, IsUnlocked = state.IsUnlocked, UpgradeStartedAtUnixMs = state.UpgradeStartedAtUnixMs, UpgradeFinishedAtUnixMs = state.UpgradeFinishedAtUnixMs };
            }

            public BuildingState ToState()
            {
                return new BuildingState { Id = Id, Level = Level, IsUnlocked = IsUnlocked, UpgradeStartedAtUnixMs = UpgradeStartedAtUnixMs, UpgradeFinishedAtUnixMs = UpgradeFinishedAtUnixMs };
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
            public List<IntPairData> AccumulatedLoot = new List<IntPairData>();
            public List<IntPairData> EnemiesDefeated = new List<IntPairData>();
            public List<string> WoundedSurvivorIds = new List<string>();

            public static ExpeditionSaveData FromState(ExpeditionState state)
            {
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
                    AccumulatedLoot = FromDictionary(state.AccumulatedLoot),
                    EnemiesDefeated = FromDictionary(state.EnemiesDefeated),
                    WoundedSurvivorIds = new List<string>(state.WoundedSurvivorIds)
                };
            }

            public ExpeditionState ToState()
            {
                return new ExpeditionState
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
                    WoundedSurvivorIds = WoundedSurvivorIds ?? new List<string>()
                };
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
        private sealed class GameStatisticsSaveData
        {
            public int ExpeditionsCompleted;
            public int ExpeditionsFailed;
            public int CombatsWon;
            public int CombatsLost;
            public List<IntPairData> TotalResourcesGained = new List<IntPairData>();

            public static GameStatisticsSaveData FromState(GameStatistics state)
            {
                return new GameStatisticsSaveData
                {
                    ExpeditionsCompleted = state.ExpeditionsCompleted,
                    ExpeditionsFailed = state.ExpeditionsFailed,
                    CombatsWon = state.CombatsWon,
                    CombatsLost = state.CombatsLost,
                    TotalResourcesGained = FromDictionary(state.TotalResourcesGained)
                };
            }

            public GameStatistics ToState()
            {
                return new GameStatistics
                {
                    ExpeditionsCompleted = ExpeditionsCompleted,
                    ExpeditionsFailed = ExpeditionsFailed,
                    CombatsWon = CombatsWon,
                    CombatsLost = CombatsLost,
                    TotalResourcesGained = ToDictionary(TotalResourcesGained)
                };
            }
        }

        private static List<IntPairData> FromDictionary(Dictionary<string, int> values)
        {
            var result = new List<IntPairData>();
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
