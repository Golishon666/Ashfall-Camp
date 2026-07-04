using System;
using AshfallCamp.Domain;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    public interface IUiRootView
    {
        Transform Root { get; }
        event Action<string> UpgradeRequested;
        event Action<ExpeditionLaunchViewRequest> ExpeditionLaunchRequested;
        event Action RecruitRequested;
        void Render(GameState state, GameConfigSnapshot config);
    }

    public sealed class ExpeditionLaunchViewRequest
    {
        public readonly string ZoneId;
        public readonly string PolicyId;

        public ExpeditionLaunchViewRequest(string zoneId, string policyId)
        {
            ZoneId = zoneId ?? string.Empty;
            PolicyId = policyId ?? string.Empty;
        }
    }
}
