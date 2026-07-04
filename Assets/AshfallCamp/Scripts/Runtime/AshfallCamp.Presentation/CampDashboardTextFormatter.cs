using System;
using System.Collections.Generic;
using AshfallCamp.Domain;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    public static class CampDashboardTextFormatter
    {
        public static List<CampAlertPresentation> BuildAlerts(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var result = new List<CampAlertPresentation>();
            if (state == null || config == null || catalog == null) return result;

            AddActiveExpeditionAlert(result, state, config, catalog);
            AddWoundedAlert(result, state, catalog);
            AddLowResourceAlert(result, state, config, catalog);
            AddUpgradeAlert(result, state, config, catalog);

            if (result.Count == 0 && (!string.IsNullOrWhiteSpace(catalog.NoAlertsTitle) || !string.IsNullOrWhiteSpace(catalog.NoAlertsBody)))
            {
                result.Add(new CampAlertPresentation(catalog.NoAlertsTitle, catalog.NoAlertsBody, catalog.Theme.Sage));
            }

            return result;
        }

        public static CampStatusPresentation BuildStatus(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null)
            {
                return new CampStatusPresentation();
            }

            var idle = UiStateQueries.CountIdleSurvivors(state);
            var unavailable = CountUnavailableSurvivors(state);
            var activeExpeditions = CountActiveExpeditions(state);
            var moralePercent = CalculateMoralePercent(state);
            var safetyPercent = CalculateSafetyPercent(state, unavailable, activeExpeditions);
            var suppliesPercent = CalculateSuppliesPercent(state, config);
            var strained = IsCampStrained(state, catalog, unavailable, suppliesPercent);

            return new CampStatusPresentation
            {
                Label = strained ? catalog.CampStatusStrainedLabel : catalog.CampStatusHealthyLabel,
                Body = string.IsNullOrWhiteSpace(catalog.CampStatusBodyFormat)
                    ? catalog.CampStatusBody
                    : Format(catalog.CampStatusBodyFormat, idle, unavailable, activeExpeditions, suppliesPercent, moralePercent),
                Badge = string.IsNullOrWhiteSpace(catalog.CampStatusBadgeFormat)
                    ? catalog.CampStatusBadgeLabel
                    : Format(catalog.CampStatusBadgeFormat, idle, unavailable, activeExpeditions, suppliesPercent),
                MoraleLabel = catalog.MoraleLabel,
                SafetyLabel = catalog.SafetyLabel,
                SuppliesLabel = catalog.SuppliesLabel,
                MoraleValue = string.IsNullOrWhiteSpace(catalog.MoraleValueFormat)
                    ? catalog.MoraleValueLabel
                    : Format(catalog.MoraleValueFormat, moralePercent),
                SafetyValue = string.IsNullOrWhiteSpace(catalog.SafetyValueFormat)
                    ? catalog.SafetyValueLabel
                    : Format(catalog.SafetyValueFormat, safetyPercent),
                SuppliesValue = string.IsNullOrWhiteSpace(catalog.SuppliesValueFormat)
                    ? catalog.SuppliesValueLabel
                    : Format(catalog.SuppliesValueFormat, suppliesPercent),
                MoralePercent = moralePercent,
                SafetyPercent = safetyPercent,
                SuppliesPercent = suppliesPercent
            };
        }

        public static List<CampExpeditionCardPresentation> BuildExpeditionCards(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var result = new List<CampExpeditionCardPresentation>();
            if (state == null || config == null || catalog == null) return result;

            foreach (var expedition in state.Expeditions)
            {
                if (expedition.Status != ExpeditionStatus.Active && expedition.Status != ExpeditionStatus.Returning) continue;
                ZoneDefinition zone;
                config.Zones.TryGetValue(expedition.ZoneId, out zone);
                ExpeditionPolicyDefinition policy;
                config.Policies.TryGetValue(expedition.PolicyId, out policy);

                var remainingSeconds = Math.Max(0, expedition.ExpectedDurationSeconds - expedition.ElapsedSeconds);
                var title = zone != null ? zone.Name : expedition.ZoneId;
                var subtitle = Format(
                    catalog.ExpeditionActiveSubtitleFormat,
                    expedition.SurvivorIds.Count,
                    policy != null ? policy.Name : expedition.PolicyId,
                    FormatMinutesCeil(remainingSeconds));
                var status = Format(catalog.ExpeditionActiveStatusFormat, ToWholePercent(expedition.Progress));
                result.Add(new CampExpeditionCardPresentation(title, subtitle, status, expedition.ZoneId, expedition.PolicyId, false));
            }

            if (result.Count > 0) return result;

            var hasIdleSurvivor = UiStateQueries.CountIdleSurvivors(state) > 0;
            foreach (var zoneState in state.Zones.Values)
            {
                if (!zoneState.IsUnlocked) continue;
                ZoneDefinition zone;
                if (!config.Zones.TryGetValue(zoneState.Id, out zone)) continue;
                var title = zone.Name;
                var subtitle = Format(
                    catalog.ExpeditionRouteSubtitleFormat,
                    zone.RiskTier,
                    zone.FoodCostPerSurvivor,
                    zone.WaterCostPerSurvivor);
                var status = Format(catalog.ExpeditionRouteStatusFormat, ToWholePercent(zoneState.Familiarity));
                result.Add(new CampExpeditionCardPresentation(title, subtitle, status, zone.Id, catalog.DefaultExpeditionPolicyId, hasIdleSurvivor));
            }

            if (result.Count == 0 && (!string.IsNullOrWhiteSpace(catalog.ExpeditionEmptyTitle) ||
                                      !string.IsNullOrWhiteSpace(catalog.ExpeditionEmptySubtitle) ||
                                      !string.IsNullOrWhiteSpace(catalog.ExpeditionEmptyStatus)))
            {
                result.Add(new CampExpeditionCardPresentation(catalog.ExpeditionEmptyTitle, catalog.ExpeditionEmptySubtitle, catalog.ExpeditionEmptyStatus, string.Empty, string.Empty, false));
            }

            return result;
        }

        public static string FormatRadioBody(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return string.Empty;
            if (string.IsNullOrWhiteSpace(catalog.RadioIntelBodyFormat)) return catalog.RadioIntelBody;

            var unlocked = 0;
            foreach (var zone in state.Zones.Values)
            {
                if (zone.IsUnlocked) unlocked++;
            }

            var cost = RecruitmentSystem.CalculateCost(state, config);
            return Format(
                catalog.RadioIntelBodyFormat,
                unlocked,
                config.Zones.Count,
                state.Statistics.ExpeditionsCompleted,
                state.Survivors.Count,
                state.SurvivorCap,
                GetCostAmount(cost, config.Balance.RecruitmentScrapResourceId),
                GetCostAmount(cost, config.Balance.RecruitmentFoodResourceId),
                GetCostAmount(cost, config.Balance.RecruitmentWaterResourceId),
                RecruitmentSystem.CountAvailableCandidates(state, config));
        }

        public static string Format(string template, params object[] args)
        {
            if (string.IsNullOrEmpty(template)) return string.Empty;
            try
            {
                return string.Format(template, args);
            }
            catch (FormatException)
            {
                return template;
            }
        }

        private static void AddActiveExpeditionAlert(List<CampAlertPresentation> output, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            ExpeditionState first = null;
            var count = 0;
            foreach (var expedition in state.Expeditions)
            {
                if (expedition.Status != ExpeditionStatus.Active && expedition.Status != ExpeditionStatus.Returning) continue;
                first = first ?? expedition;
                count++;
            }

            if (count == 0 || first == null) return;

            ZoneDefinition zone;
            config.Zones.TryGetValue(first.ZoneId, out zone);
            var zoneName = zone != null ? zone.Name : first.ZoneId;
            output.Add(new CampAlertPresentation(
                Format(catalog.ActiveExpeditionAlertTitleFormat, count),
                Format(catalog.ActiveExpeditionAlertBodyFormat, zoneName, ToWholePercent(first.Progress)),
                catalog.Theme.Teal));
        }

        private static void AddWoundedAlert(List<CampAlertPresentation> output, GameState state, CampUiCatalogSO catalog)
        {
            SurvivorState first = null;
            var count = 0;
            foreach (var survivor in state.Survivors)
            {
                if (survivor.State != SurvivorActivityState.Wounded && survivor.State != SurvivorActivityState.Missing) continue;
                first = first ?? survivor;
                count++;
            }

            if (count == 0 || first == null) return;

            output.Add(new CampAlertPresentation(
                Format(catalog.WoundedAlertTitleFormat, count),
                Format(catalog.WoundedAlertBodyFormat, first.Name),
                catalog.Theme.Rust));
        }

        private static void AddLowResourceAlert(List<CampAlertPresentation> output, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (catalog.LowResourceAlertPercentThreshold <= 0) return;

            ResourceDefinition selected = null;
            var selectedAmount = 0;
            var selectedCap = 0;
            var selectedPercent = double.MaxValue;
            foreach (var resource in config.Resources.Values)
            {
                if (!resource.HasCap) continue;
                int cap;
                if (!state.ResourceCaps.TryGetValue(resource.Id, out cap)) cap = resource.StartCap;
                if (cap <= 0) continue;

                int amount;
                state.Resources.TryGetValue(resource.Id, out amount);
                var percent = amount * 100.0 / cap;
                if (percent > catalog.LowResourceAlertPercentThreshold || percent >= selectedPercent) continue;

                selected = resource;
                selectedAmount = amount;
                selectedCap = cap;
                selectedPercent = percent;
            }

            if (selected == null) return;

            output.Add(new CampAlertPresentation(
                Format(catalog.LowResourceAlertTitleFormat, selected.Name),
                Format(catalog.LowResourceAlertBodyFormat, selectedAmount, selectedCap),
                catalog.Theme.Rust));
        }

        private static void AddUpgradeAlert(List<CampAlertPresentation> output, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            foreach (var definition in config.Buildings.Values)
            {
                BuildingState building;
                if (!state.Buildings.TryGetValue(definition.Id, out building)) continue;
                if (!building.IsUnlocked) continue;

                var nextLevel = BuildingSystem.GetLevel(definition, building.Level + 1);
                if (nextLevel == null) continue;
                if (!BuildingSystem.ValidateUpgrade(state, config, definition.Id).IsValid) continue;

                output.Add(new CampAlertPresentation(
                    Format(catalog.UpgradeAvailableAlertTitleFormat, definition.Name),
                    Format(catalog.UpgradeAvailableAlertBodyFormat, nextLevel.Level),
                    catalog.Theme.Amber));
                return;
            }
        }

        private static int ToWholePercent(double value)
        {
            return (int)Math.Round(GameMath.Clamp(value, 0, 100));
        }

        private static int GetCostAmount(Dictionary<string, int> cost, string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId)) return 0;
            int amount;
            return cost.TryGetValue(resourceId, out amount) ? amount : 0;
        }

        private static int FormatMinutesCeil(double seconds)
        {
            return Math.Max(0, (int)Math.Ceiling(seconds / 60.0));
        }

        private static bool IsCampStrained(GameState state, CampUiCatalogSO catalog, int unavailableSurvivors, int suppliesPercent)
        {
            if (unavailableSurvivors > 0) return true;
            if (catalog.LowResourceAlertPercentThreshold > 0 && suppliesPercent <= catalog.LowResourceAlertPercentThreshold) return true;
            if (string.IsNullOrWhiteSpace(catalog.StatusResourceId)) return false;

            int amount;
            return state.Resources.TryGetValue(catalog.StatusResourceId, out amount) &&
                   amount < catalog.StatusStrainedBelowAmount;
        }

        private static int CountActiveExpeditions(GameState state)
        {
            var count = 0;
            foreach (var expedition in state.Expeditions)
            {
                if (expedition.Status == ExpeditionStatus.Active || expedition.Status == ExpeditionStatus.Returning) count++;
            }

            return count;
        }

        private static int CountUnavailableSurvivors(GameState state)
        {
            var count = 0;
            foreach (var survivor in state.Survivors)
            {
                if (survivor.State == SurvivorActivityState.Wounded || survivor.State == SurvivorActivityState.Missing) count++;
            }

            return count;
        }

        private static int CalculateMoralePercent(GameState state)
        {
            if (state.Survivors.Count == 0) return 0;
            var total = 0;
            foreach (var survivor in state.Survivors)
            {
                total += GameMath.Clamp(survivor.Morale, 0, 100);
            }

            return ToWholePercent(total / (double)state.Survivors.Count);
        }

        private static int CalculateSafetyPercent(GameState state, int unavailableSurvivors, int activeExpeditions)
        {
            var value = 100 - unavailableSurvivors * 25 - activeExpeditions * 5;
            foreach (var expedition in state.Expeditions)
            {
                if (expedition.Status == ExpeditionStatus.Active || expedition.Status == ExpeditionStatus.Returning)
                {
                    value -= Math.Min(25, expedition.Noise);
                }
            }

            return GameMath.Clamp(value, 0, 100);
        }

        private static int CalculateSuppliesPercent(GameState state, GameConfigSnapshot config)
        {
            var foundCappedResource = false;
            var percent = 100.0;
            foreach (var resource in config.Resources.Values)
            {
                if (!resource.HasCap) continue;
                int cap;
                if (!state.ResourceCaps.TryGetValue(resource.Id, out cap)) cap = resource.StartCap;
                if (cap <= 0) continue;

                int amount;
                state.Resources.TryGetValue(resource.Id, out amount);
                percent = Math.Min(percent, amount * 100.0 / cap);
                foundCappedResource = true;
            }

            return foundCappedResource ? ToWholePercent(percent) : 100;
        }
    }

    public sealed class CampStatusPresentation
    {
        public string Label = string.Empty;
        public string Body = string.Empty;
        public string Badge = string.Empty;
        public string MoraleLabel = string.Empty;
        public string SafetyLabel = string.Empty;
        public string SuppliesLabel = string.Empty;
        public string MoraleValue = string.Empty;
        public string SafetyValue = string.Empty;
        public string SuppliesValue = string.Empty;
        public int MoralePercent;
        public int SafetyPercent;
        public int SuppliesPercent;
    }

    public sealed class CampAlertPresentation
    {
        public readonly string Title;
        public readonly string Body;
        public readonly Color ToneColor;

        public CampAlertPresentation(string title, string body, Color toneColor)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            ToneColor = toneColor;
        }
    }

    public sealed class CampExpeditionCardPresentation
    {
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string Status;
        public readonly string ZoneId;
        public readonly string PolicyId;
        public readonly bool CanLaunch;

        public CampExpeditionCardPresentation(string title, string subtitle, string status, string zoneId, string policyId, bool canLaunch)
        {
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            Status = status ?? string.Empty;
            ZoneId = zoneId ?? string.Empty;
            PolicyId = policyId ?? string.Empty;
            CanLaunch = canLaunch && !string.IsNullOrWhiteSpace(ZoneId);
        }
    }
}
