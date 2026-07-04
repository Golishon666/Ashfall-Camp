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
        event Action BroadcastRecruitmentRequested;
        event Action<RecruitSurvivorViewRequest> RecruitRequested;
        event Action RecruitmentCandidatesSkipRequested;
        event Action<RepairItemRequest> RepairItemRequested;
        event Action<EquipItemRequest> EquipItemRequested;
        event Action<UseMedicineRequest> UseMedicineRequested;
        event Action EmergencyScavengeRequested;
        event Action<bool> AutosaveChanged;
        void Render(GameState state, GameConfigSnapshot config);
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
}
