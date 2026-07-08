using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    public interface IUiRootView
    {
        Transform Root { get; }
        event Action<string> UpgradeRequested;
        event Action<ExpeditionLaunchViewRequest> ExpeditionLaunchRequested;
        event Action<BroadcastRecruitmentRequest> BroadcastRecruitmentRequested;
        event Action<RecruitSurvivorViewRequest> RecruitRequested;
        event Action RecruitmentCandidatesSkipRequested;
        event Action<RepairItemRequest> RepairItemRequested;
        event Action<EquipItemRequest> EquipItemRequested;
        event Action<UseMedicineRequest> UseMedicineRequested;
        event Action<StartRestRequest> StartRestRequested;
        event Action<StopRestRequest> StopRestRequested;
        event Action EmergencyScavengeRequested;
        event Action<bool> AutosaveChanged;
        event Action ManualSaveRequested;
        void Render(GameState state, GameConfigSnapshot config);
        void OpenReports();
        void ShowToast(CampToastRequest request);
    }

    public sealed class ExpeditionLaunchViewRequest
    {
        public readonly string ZoneId;
        public readonly string PolicyId;
        public readonly List<string> SurvivorIds = new List<string>();
        public readonly bool ConfirmWarnings;

        public ExpeditionLaunchViewRequest(string zoneId, string policyId)
            : this(zoneId, policyId, null, false)
        {
        }

        public ExpeditionLaunchViewRequest(string zoneId, string policyId, IEnumerable<string> survivorIds)
            : this(zoneId, policyId, survivorIds, false)
        {
        }

        public ExpeditionLaunchViewRequest(string zoneId, string policyId, IEnumerable<string> survivorIds, bool confirmWarnings)
        {
            ZoneId = zoneId ?? string.Empty;
            PolicyId = policyId ?? string.Empty;
            ConfirmWarnings = confirmWarnings;
            if (survivorIds == null) return;

            foreach (var survivorId in survivorIds)
            {
                if (!string.IsNullOrWhiteSpace(survivorId))
                {
                    SurvivorIds.Add(survivorId);
                }
            }
        }
    }

    public sealed class RecruitSurvivorViewRequest
    {
        public readonly string CandidateId;

        public RecruitSurvivorViewRequest(string candidateId)
        {
            CandidateId = candidateId ?? string.Empty;
        }
    }

    public sealed class CampToastRequest
    {
        public readonly string Id;
        public readonly List<string> Args = new List<string>();

        public CampToastRequest(string id, params string[] args)
        {
            Id = id ?? string.Empty;
            if (args == null) return;

            foreach (var arg in args)
            {
                Args.Add(arg ?? string.Empty);
            }
        }
    }

    public static class CampToastIds
    {
        public const string ActionBlocked = "action_blocked";
        public const string NoIdleSurvivors = "no_idle_survivors";
        public const string BuildingUpgraded = "building_upgraded";
        public const string ExpeditionLaunched = "expedition_launched";
        public const string ExpeditionCompleted = "expedition_completed";
        public const string ExpeditionFailed = "expedition_failed";
        public const string OfflineReportReady = "offline_report_ready";
        public const string RecruitmentBroadcast = "recruitment_broadcast";
        public const string RecruitmentSkipped = "recruitment_skipped";
        public const string SurvivorRecruited = "survivor_recruited";
        public const string ItemRepaired = "item_repaired";
        public const string ItemEquipped = "item_equipped";
        public const string MedicineUsed = "medicine_used";
        public const string SurvivorRestStarted = "survivor_rest_started";
        public const string SurvivorRestStopped = "survivor_rest_stopped";
        public const string EmergencyScavengeStarted = "emergency_scavenge_started";
        public const string AutosaveEnabled = "autosave_enabled";
        public const string AutosaveDisabled = "autosave_disabled";
        public const string ManualSaved = "manual_saved";

        public static string[] Required
        {
            get
            {
                return new[]
                {
                    ActionBlocked,
                    NoIdleSurvivors,
                    BuildingUpgraded,
                    ExpeditionLaunched,
                    ExpeditionCompleted,
                    ExpeditionFailed,
                    OfflineReportReady,
                    RecruitmentBroadcast,
                    RecruitmentSkipped,
                    SurvivorRecruited,
                    ItemRepaired,
                    ItemEquipped,
                    MedicineUsed,
                    SurvivorRestStarted,
                    SurvivorRestStopped,
                    EmergencyScavengeStarted,
                    AutosaveEnabled,
                    AutosaveDisabled,
                    ManualSaved
                };
            }
        }
    }
}
