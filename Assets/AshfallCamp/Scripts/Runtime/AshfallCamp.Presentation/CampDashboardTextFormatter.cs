using System;
using System.Collections.Generic;
using System.Globalization;
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

            AddCampEventAlert(result, state, config, catalog);
            AddActiveExpeditionAlert(result, state, config, catalog);
            AddWoundedAlert(result, state, catalog);
            AddEmergencyScavengeAlert(result, state, config, catalog);
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

        public static CampNextGoalPresentation BuildNextGoal(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var result = new CampNextGoalPresentation();
            if (state == null || config == null || catalog == null) return result;
            if (state.Progress != null && state.Progress.DemoCompleted)
            {
                result.Title = catalog.NextGoalCompleteTitle;
                result.Body = catalog.NextGoalCompleteBody;
                result.Progress = string.Empty;
                result.IsComplete = true;
                return result;
            }

            foreach (var goal in catalog.NextGoals)
            {
                if (goal == null) continue;
                if (IsGoalComplete(state, goal)) continue;

                var condition = SelectProgressCondition(state, goal);
                var current = condition != null ? GetConditionProgress(state, condition) : 0;
                var target = condition != null ? Math.Max(0, condition.Value) : 0;
                result.Title = goal.Title;
                result.Body = goal.Body;
                result.Progress = condition != null && !string.IsNullOrWhiteSpace(catalog.NextGoalProgressFormat)
                    ? Format(catalog.NextGoalProgressFormat, Math.Min(current, target), target)
                    : string.Empty;
                return result;
            }

            result.Title = catalog.NextGoalCompleteTitle;
            result.Body = catalog.NextGoalCompleteBody;
            result.Progress = string.Empty;
            result.IsComplete = true;
            return result;
        }

        public static CampExpeditionScreenPresentation BuildExpeditionScreen(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog, string selectedZoneId)
        {
            return BuildExpeditionScreen(state, config, catalog, selectedZoneId, string.Empty, null, false);
        }

        public static CampExpeditionScreenPresentation BuildExpeditionScreen(
            GameState state,
            GameConfigSnapshot config,
            CampUiCatalogSO catalog,
            string selectedZoneId,
            string selectedPolicyId,
            IReadOnlyList<string> selectedSurvivorIds)
        {
            return BuildExpeditionScreen(state, config, catalog, selectedZoneId, selectedPolicyId, selectedSurvivorIds, false);
        }

        public static CampExpeditionScreenPresentation BuildExpeditionScreen(
            GameState state,
            GameConfigSnapshot config,
            CampUiCatalogSO catalog,
            string selectedZoneId,
            string selectedPolicyId,
            IReadOnlyList<string> selectedSurvivorIds,
            bool riskConfirmationPending)
        {
            var result = new CampExpeditionScreenPresentation { Title = catalog != null ? catalog.ExpeditionScreenTitle : string.Empty };
            if (state == null || config == null || catalog == null) return result;

            var selectedId = ResolveSelectedZoneId(state, config, selectedZoneId);
            var policyId = ResolvePolicyId(config, string.IsNullOrWhiteSpace(selectedPolicyId) ? catalog.DefaultExpeditionPolicyId : selectedPolicyId);
            var survivorIds = ResolveSelectedSurvivorIds(state, selectedSurvivorIds);
            if (selectedSurvivorIds == null && survivorIds.Count == 0)
            {
                survivorIds = SelectIdleSurvivorIds(state);
            }

            result.SelectedZoneId = selectedId;
            BuildExpeditionSetup(result, state, config, catalog, policyId, survivorIds);
            foreach (var zone in config.Zones.Values)
            {
                ZoneState zoneState;
                state.Zones.TryGetValue(zone.Id, out zoneState);
                var unlocked = zoneState != null && zoneState.IsUnlocked;
                var familiarity = zoneState != null ? zoneState.Familiarity : 0;
                var validation = unlocked ? ValidateZoneLaunch(state, config, zone.Id, policyId, survivorIds) : null;
                var status = unlocked
                    ? Format(catalog.ExpeditionRouteStatusFormat, ToWholePercent(familiarity))
                    : Format(catalog.ExpeditionLockedStatusFormat, FormatUnlockRequirements(zone, config, catalog));

                result.Routes.Add(new CampExpeditionRoutePresentation(
                    zone.Id,
                    zone.Name,
                    Format(catalog.ExpeditionRouteSubtitleFormat, zone.RiskTier, zone.FoodCostPerSurvivor, zone.WaterCostPerSurvivor),
                    status,
                    unlocked,
                    validation != null && validation.IsValid,
                    string.Equals(zone.Id, selectedId, StringComparison.Ordinal)));
            }

            BuildSelectedExpeditionDetail(result, state, config, catalog, selectedId, policyId, survivorIds, riskConfirmationPending);
            BuildExpeditionMonitor(result, state, config, catalog);
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
            var pendingCandidateIds = RecruitmentSystem.GetPendingCandidateIds(state);
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
                RecruitmentSystem.CountAvailableCandidates(state, config),
                pendingCandidateIds.Count);
        }

        public static CampRadioPresentation BuildRadioScreen(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var presentation = new CampRadioPresentation();
            if (state == null || config == null || catalog == null) return presentation;

            var cost = RecruitmentSystem.CalculateCost(state, config);
            var validation = RecruitmentSystem.ValidateBroadcast(state, config);
            var pendingCandidateIds = RecruitmentSystem.GetPendingCandidateIds(state);
            var hasPendingCandidates = pendingCandidateIds.Count > 0;
            presentation.Title = catalog.RadioScreenTitle;
            presentation.IntelTitle = catalog.RadioIntelTitle;
            presentation.IntelBody = FormatRadioBody(state, config, catalog);
            presentation.BroadcastTitle = catalog.RadioBroadcastTitle;
            presentation.BroadcastCost = Format(catalog.RadioBroadcastCostFormat, FormatResourceAmounts(cost, config, catalog));
            presentation.BroadcastStatus = hasPendingCandidates
                ? catalog.RadioBroadcastPendingLabel
                : validation.IsValid
                ? catalog.RadioBroadcastReadyLabel
                : Format(catalog.RadioBroadcastBlockedFormat, FormatValidationMessages(validation, catalog));
            presentation.BroadcastButton = hasPendingCandidates ? catalog.RadioCandidateSkipButton : catalog.RadioIntelButton;
            presentation.CanBroadcast = !hasPendingCandidates && validation.IsValid;
            presentation.CanSkipCandidates = hasPendingCandidates;
            presentation.CandidateListTitle = catalog.RadioCandidateListTitle;
            presentation.EmptyTitle = hasPendingCandidates || RecruitmentSystem.CountAvailableCandidates(state, config) == 0
                ? catalog.RadioCandidateEmptyTitle
                : catalog.RadioCandidateAwaitingTitle;
            presentation.EmptyBody = hasPendingCandidates || RecruitmentSystem.CountAvailableCandidates(state, config) == 0
                ? catalog.RadioCandidateEmptyBody
                : catalog.RadioCandidateAwaitingBody;

            foreach (var candidateId in pendingCandidateIds)
            {
                RecruitableSurvivorDefinition candidate;
                if (!config.RecruitableSurvivors.TryGetValue(candidateId, out candidate)) continue;
                if (IsCandidateRecruited(state, candidate)) continue;

                var candidateValidation = RecruitmentSystem.ValidateRecruitSelection(state, config, candidateId);
                presentation.Candidates.Add(BuildCandidatePresentation(candidate, config, catalog, candidateValidation.IsValid));
            }

            return presentation;
        }

        public static List<CampSurvivorCardPresentation> BuildSurvivorCards(GameState state, CampUiCatalogSO catalog)
        {
            var result = new List<CampSurvivorCardPresentation>();
            if (state == null || catalog == null) return result;

            foreach (var survivor in state.Survivors)
            {
                result.Add(new CampSurvivorCardPresentation(
                    survivor.Id,
                    survivor.Name,
                    string.IsNullOrEmpty(survivor.Name) ? string.Empty : survivor.Name.Substring(0, 1).ToUpperInvariant(),
                    Format(catalog.SurvivorCardStateFormat, survivor.State, survivor.Level),
                    FormatTopSkill(survivor, catalog)));
            }

            return result;
        }

        public static CampSurvivorDetailPresentation BuildSurvivorDetail(SurvivorState survivor, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (survivor == null || state == null || config == null || catalog == null)
            {
                return new CampSurvivorDetailPresentation();
            }

            return new CampSurvivorDetailPresentation
            {
                Title = Format(catalog.SurvivorDetailTitle, survivor.Name, survivor.Level),
                Background = Format(catalog.SurvivorDetailBackgroundFormat, GetBackgroundName(survivor, config)),
                Traits = Format(catalog.SurvivorDetailTraitsFormat, FormatTraits(survivor, config, catalog)),
                Weapon = FormatWeapon(survivor, state, config, catalog),
                Treatment = FormatTreatment(survivor, state, config, catalog),
                Stats = Format(catalog.SurvivorDetailStatsFormat, survivor.Health, survivor.MaxHealth, survivor.Morale, survivor.Fatigue, survivor.Xp),
                MedicineCost = Format(catalog.SurvivorDetailMedicineCostFormat, FormatResourceAmounts(HealingSystem.CalculateMedicineCost(config), config, catalog)),
                MedicineButton = catalog.SurvivorDetailUseMedicineButton,
                ShowMedicineAction = survivor.State == SurvivorActivityState.Wounded,
                CanUseMedicine = HealingSystem.ValidateUseMedicine(state, config, new UseMedicineRequest { SurvivorId = survivor.Id }).IsValid
            };
        }

        public static string FormatWorkshopStatus(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog, string targetSurvivorId)
        {
            if (state == null || config == null || catalog == null) return string.Empty;

            return Format(
                catalog.WorkshopStatusFormat,
                GetSurvivorName(state, targetSurvivorId),
                state.Inventory.Count,
                ResourceSystem.GetAmount(state, config.Balance.WorkshopRepairResourceId),
                Math.Max(0, config.Balance.WorkshopRequiredBuildingLevel));
        }

        public static List<CampWorkshopItemPresentation> BuildWorkshopItems(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog, string targetSurvivorId)
        {
            var result = new List<CampWorkshopItemPresentation>();
            if (state == null || config == null || catalog == null) return result;

            foreach (var item in state.Inventory)
            {
                ItemDefinition definition;
                var itemName = config.Items.TryGetValue(item.ItemId, out definition) ? definition.Name : item.ItemId;
                var cost = WorkshopSystem.CalculateRepairCost(state, config, item.Uid);
                var repairCost = GetCostAmount(cost, config.Balance.WorkshopRepairResourceId);
                var equippedLabel = string.IsNullOrWhiteSpace(item.EquippedBySurvivorId)
                    ? catalog.WorkshopItemUnequippedLabel
                    : Format(catalog.WorkshopItemEquippedFormat, GetSurvivorName(state, item.EquippedBySurvivorId));
                var targetRequest = new EquipItemRequest { SurvivorId = targetSurvivorId, ItemUid = item.Uid };

                result.Add(new CampWorkshopItemPresentation(
                    item.Uid,
                    itemName,
                    item.Durability,
                    item.MaxDurability,
                    equippedLabel,
                    item.Durability <= 0 ? catalog.WorkshopBrokenLabel : string.Empty,
                    Format(catalog.WorkshopRepairCostFormat, repairCost),
                    WorkshopSystem.ValidateRepair(state, config, new RepairItemRequest { ItemUid = item.Uid }).IsValid,
                    WorkshopSystem.ValidateEquip(state, config, targetRequest).IsValid));
            }

            return result;
        }

        public static CampReportsPresentation BuildReports(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var presentation = new CampReportsPresentation();
            if (state == null || config == null || catalog == null) return presentation;

            presentation.Title = catalog.ReportsScreenTitle;
            presentation.EmptyTitle = catalog.ReportsEmptyTitle;
            presentation.EmptyBody = catalog.ReportsEmptyBody;

            var campEvent = FindLatestCampEvent(state);
            if (campEvent != null)
            {
                presentation.HasCampEvent = true;
                presentation.CampEventPanelTitle = catalog.CampEventPanelTitle;
                SetCampEventReport(presentation, state, config, catalog, campEvent);
            }

            var expedition = FindLatestReportExpedition(state);
            if (expedition != null)
            {
                ZoneDefinition zone;
                config.Zones.TryGetValue(expedition.ZoneId, out zone);
                var zoneName = zone != null ? zone.Name : expedition.ZoneId;
                var outcome = expedition.Status == ExpeditionStatus.Completed ? catalog.AfterActionSuccessLabel : catalog.AfterActionFailureLabel;
                presentation.HasAfterAction = true;
                presentation.AfterActionTitle = catalog.AfterActionPanelTitle;
                presentation.AfterActionOutcome = Format(catalog.AfterActionOutcomeFormat, zoneName, outcome, FormatMinutesCeil(expedition.ElapsedSeconds));
                presentation.AfterActionLoot = Format(catalog.AfterActionLootFormat, FormatResourceAmounts(expedition.AccumulatedLoot, config, catalog));
                presentation.AfterActionXp = Format(catalog.AfterActionXpFormat, CalculateExpeditionXp(expedition, config));
                presentation.AfterActionWounds = Format(catalog.AfterActionWoundsFormat, FormatSurvivorNames(state, expedition.WoundedSurvivorIds, catalog));
                presentation.AfterActionEnemies = Format(catalog.AfterActionEnemiesFormat, FormatEnemyCounts(expedition.EnemiesDefeated, config, catalog));
                presentation.AfterActionEvents = Format(catalog.AfterActionEventsFormat, FormatRecentEvents(expedition, catalog));
                presentation.AfterActionSendAgainButton = catalog.AfterActionSendAgainButton;
                presentation.AfterActionSendAgainRequest = BuildSendAgainRequest(config, expedition);
                presentation.AfterActionCanSendAgain = presentation.AfterActionSendAgainRequest != null &&
                    ValidateZoneLaunch(
                        state,
                        config,
                        presentation.AfterActionSendAgainRequest.ZoneId,
                        presentation.AfterActionSendAgainRequest.PolicyId,
                        presentation.AfterActionSendAgainRequest.SurvivorIds).IsValid;
            }

            var offline = state.LastOfflineReport;
            if (offline != null && offline.AppliedSeconds >= Math.Max(0, config.Balance.OfflineReportMinimumSeconds))
            {
                presentation.HasOfflineReport = true;
                presentation.OfflineReportTitle = catalog.OfflineReportPanelTitle;
                presentation.OfflineSummary = Format(catalog.OfflineReportSummaryFormat, FormatMinutesCeil(offline.AppliedSeconds));
                presentation.OfflineResources = Format(catalog.OfflineReportResourcesFormat, FormatResourceAmounts(offline.ResourcesGained, config, catalog));
                presentation.OfflineCompleted = Format(catalog.OfflineReportCompletedFormat, FormatCompletedExpeditions(state, config, offline.CompletedExpeditionIds, catalog));
                presentation.OfflineHealing = Format(catalog.OfflineReportHealingFormat, FormatSurvivorNames(state, offline.HealedSurvivorIds, catalog));
                presentation.OfflineWarnings = Format(catalog.OfflineReportWarningsFormat, FormatSurvivorNames(state, offline.WoundedSurvivorIds, catalog));
            }

            return presentation;
        }

        public static CampSettingsPresentation BuildSettings(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var presentation = new CampSettingsPresentation();
            if (state == null || config == null || catalog == null) return presentation;

            var autosaveEnabled = state.Settings == null || state.Settings.AutosaveEnabled;
            presentation.Title = catalog.SettingsScreenTitle;
            presentation.AutosaveTitle = catalog.SettingsAutosaveTitle;
            presentation.AutosaveBody = autosaveEnabled
                ? Format(catalog.SettingsAutosaveEnabledBodyFormat, FormatSecondsCeil(config.Balance.AutosaveSeconds))
                : catalog.SettingsAutosaveDisabledBody;
            presentation.AutosaveState = autosaveEnabled ? catalog.SettingsAutosaveEnabledLabel : catalog.SettingsAutosaveDisabledLabel;
            presentation.AutosaveToggleLabel = catalog.SettingsAutosaveToggleLabel;
            presentation.AutosaveEnabled = autosaveEnabled;
            return presentation;
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

        private static void AddCampEventAlert(List<CampAlertPresentation> output, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var campEvent = FindLatestCampEvent(state);
            if (campEvent == null) return;

            if (string.Equals(campEvent.EventId, GameEventIds.DemoCompleted, StringComparison.Ordinal))
            {
                var completionName = GetDemoCompletionName(config, campEvent);
                output.Add(new CampAlertPresentation(
                    Format(catalog.DemoCompletedAlertTitleFormat, completionName),
                    Format(catalog.DemoCompletedAlertBodyFormat, completionName),
                    catalog.Theme.Amber));
                return;
            }

            if (string.Equals(campEvent.EventId, GameEventIds.EmergencyScavengeCompleted, StringComparison.Ordinal))
            {
                output.Add(new CampAlertPresentation(
                    catalog.EmergencyScavengeCompletedAlertTitle,
                    Format(catalog.EmergencyScavengeCompletedAlertBodyFormat, FormatResourceAmounts(RecoverySystem.CalculateEmergencyScavengeRewards(config), config, catalog)),
                    catalog.Theme.Sage));
                return;
            }

            output.Add(new CampAlertPresentation(
                Format(catalog.SurvivorJoinedAlertTitleFormat, GetCampEventSubjectName(state, campEvent)),
                Format(catalog.SurvivorJoinedAlertBodyFormat, GetCampEventSubjectName(state, campEvent), state.Survivors.Count, Math.Max(1, state.SurvivorCap)),
                catalog.Theme.Sage));
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

        private static void AddEmergencyScavengeAlert(List<CampAlertPresentation> output, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state.Recovery == null) return;

            if (state.Recovery.EmergencyScavengeActive)
            {
                output.Add(new CampAlertPresentation(
                    catalog.EmergencyScavengeAlertTitle,
                    Format(catalog.EmergencyScavengeActiveBodyFormat, FormatSecondsCeil(state.Recovery.EmergencyScavengeRemainingSeconds)),
                    catalog.Theme.Teal));
                return;
            }

            if (!HasLowEmergencyResource(state, config, catalog)) return;

            if (state.Recovery.EmergencyScavengeCooldownRemainingSeconds > 0)
            {
                output.Add(new CampAlertPresentation(
                    catalog.EmergencyScavengeAlertTitle,
                    Format(catalog.EmergencyScavengeCooldownBodyFormat, FormatSecondsCeil(state.Recovery.EmergencyScavengeCooldownRemainingSeconds)),
                    catalog.Theme.Amber));
                return;
            }

            var rewards = RecoverySystem.CalculateEmergencyScavengeRewards(config);
            var canStart = RecoverySystem.ValidateEmergencyScavenge(state, config, new EmergencyScavengeRequest()).IsValid;
            output.Add(new CampAlertPresentation(
                catalog.EmergencyScavengeAlertTitle,
                Format(catalog.EmergencyScavengeReadyBodyFormat, FormatSecondsCeil(config.Balance.EmergencyScavengeDurationSeconds), FormatResourceAmounts(rewards, config, catalog)),
                catalog.Theme.Rust,
                catalog.EmergencyScavengeButton,
                canStart));
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

        private static bool IsGoalComplete(GameState state, CampNextGoalEntry goal)
        {
            if (goal.Conditions == null || goal.Conditions.Count == 0) return false;
            if (goal.CompleteWhenAnyCondition)
            {
                foreach (var condition in goal.Conditions)
                {
                    if (IsConditionComplete(state, condition)) return true;
                }

                return false;
            }

            foreach (var condition in goal.Conditions)
            {
                if (!IsConditionComplete(state, condition)) return false;
            }

            return true;
        }

        private static CampGoalConditionEntry SelectProgressCondition(GameState state, CampNextGoalEntry goal)
        {
            if (goal == null || goal.Conditions == null || goal.Conditions.Count == 0) return null;
            CampGoalConditionEntry best = null;
            var bestProgress = -1;
            foreach (var condition in goal.Conditions)
            {
                if (condition == null) continue;
                if (!goal.CompleteWhenAnyCondition && !IsConditionComplete(state, condition)) return condition;

                var target = Math.Max(1, condition.Value);
                var progress = GameMath.Clamp((int)Math.Round(GetConditionProgress(state, condition) * 100.0 / target), 0, 100);
                if (progress > bestProgress)
                {
                    bestProgress = progress;
                    best = condition;
                }
            }

            return best;
        }

        private static bool IsConditionComplete(GameState state, CampGoalConditionEntry condition)
        {
            if (condition == null) return false;
            return GetConditionProgress(state, condition) >= Math.Max(1, condition.Value);
        }

        private static int GetConditionProgress(GameState state, CampGoalConditionEntry condition)
        {
            if (state == null || condition == null || string.IsNullOrWhiteSpace(condition.Type)) return 0;
            if (string.Equals(condition.Type, GameConditionTypes.ZoneCompletions, StringComparison.Ordinal))
            {
                ZoneState zone;
                return state.Zones.TryGetValue(condition.Id, out zone) ? Math.Max(0, zone.Completions) : 0;
            }

            if (string.Equals(condition.Type, GameConditionTypes.ZoneUnlocked, StringComparison.Ordinal))
            {
                ZoneState zone;
                return state.Zones.TryGetValue(condition.Id, out zone) && zone.IsUnlocked ? 1 : 0;
            }

            if (string.Equals(condition.Type, GameConditionTypes.BuildingLevel, StringComparison.Ordinal))
            {
                BuildingState building;
                return state.Buildings.TryGetValue(condition.Id, out building) ? Math.Max(0, building.Level) : 0;
            }

            if (string.Equals(condition.Type, GameConditionTypes.SurvivorCount, StringComparison.Ordinal))
            {
                return state.Survivors.Count;
            }

            if (string.Equals(condition.Type, GameConditionTypes.ResourceAmount, StringComparison.Ordinal))
            {
                int amount;
                return state.Resources.TryGetValue(condition.Id, out amount) ? Math.Max(0, amount) : 0;
            }

            if (string.Equals(condition.Type, GameConditionTypes.ExpeditionsCompleted, StringComparison.Ordinal))
            {
                return Math.Max(0, state.Statistics.ExpeditionsCompleted);
            }

            if (string.Equals(condition.Type, GameConditionTypes.ActiveExpeditions, StringComparison.Ordinal))
            {
                return CountActiveExpeditions(state);
            }

            return 0;
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

        private static int FormatSecondsCeil(double seconds)
        {
            return Math.Max(0, (int)Math.Ceiling(seconds));
        }

        private static string ResolveLaunchButton(CampUiCatalogSO catalog, bool canLaunch, bool requiresRiskConfirmation, bool riskConfirmationPending)
        {
            if (!canLaunch) return catalog.ExpeditionLaunchBlockedButton;
            if (!requiresRiskConfirmation) return catalog.ExpeditionLaunchButton;
            return riskConfirmationPending ? catalog.ExpeditionConfirmRiskButton : catalog.ExpeditionReviewRiskButton;
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

        private static string GetBackgroundName(SurvivorState survivor, GameConfigSnapshot config)
        {
            return GetBackgroundName(survivor.BackgroundId, config);
        }

        private static string GetSurvivorName(GameState state, string survivorId)
        {
            if (state == null || string.IsNullOrWhiteSpace(survivorId)) return string.Empty;
            foreach (var survivor in state.Survivors)
            {
                if (string.Equals(survivor.Id, survivorId, StringComparison.Ordinal))
                {
                    return survivor.Name;
                }
            }

            return survivorId;
        }

        private static string FormatTraits(SurvivorState survivor, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            return FormatTraits(survivor.TraitIds, config, catalog);
        }

        private static string FormatTraits(List<string> traitIds, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (traitIds == null || traitIds.Count == 0) return catalog.SurvivorNoTraitsLabel;

            var names = new List<string>();
            foreach (var traitId in traitIds)
            {
                TraitDefinition trait;
                names.Add(config.Traits.TryGetValue(traitId, out trait) ? trait.Name : traitId);
            }

            return string.Join(catalog.ReportListSeparator, names.ToArray());
        }

        private static string FormatWeapon(SurvivorState survivor, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var item = FindEquippedWeapon(survivor, state);
            if (item == null)
            {
                return Format(catalog.SurvivorDetailWeaponFormat, catalog.SurvivorNoWeaponLabel, 0, 0);
            }

            ItemDefinition definition;
            var itemName = config.Items.TryGetValue(item.ItemId, out definition) ? definition.Name : item.ItemId;
            return Format(catalog.SurvivorDetailWeaponFormat, itemName, item.Durability, item.MaxDurability);
        }

        private static string FormatTreatment(SurvivorState survivor, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (survivor.State != SurvivorActivityState.Wounded)
            {
                return Format(catalog.SurvivorDetailTreatmentFormat, catalog.SurvivorDetailHealthyLabel);
            }

            var wound = FindStatusEffect(survivor, config.Balance.HealingDefaultWoundId);
            var remainingMinutes = wound != null ? FormatMinutesCeil(wound.RemainingSeconds) : FormatMinutesCeil(config.Balance.HealingDefaultWoundDurationSeconds);
            var woundText = Format(catalog.SurvivorDetailWoundFormat, config.Balance.HealingDefaultWoundId, remainingMinutes);
            if (HealingSystem.IsHealingUnlocked(state, config))
            {
                return Format(catalog.SurvivorDetailTreatmentFormat, woundText);
            }

            return Format(
                catalog.SurvivorDetailTreatmentFormat,
                Format(catalog.SurvivorDetailHealingLockedFormat, woundText, Math.Max(0, config.Balance.HealingRequiredBuildingLevel)));
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

        private static InventoryItemState FindEquippedWeapon(SurvivorState survivor, GameState state)
        {
            if (survivor == null || state == null || string.IsNullOrWhiteSpace(survivor.Equipment.WeaponItemUid)) return null;
            foreach (var item in state.Inventory)
            {
                if (string.Equals(item.Uid, survivor.Equipment.WeaponItemUid, StringComparison.Ordinal))
                {
                    return item;
                }
            }

            return null;
        }

        private static CampRadioCandidatePresentation BuildCandidatePresentation(RecruitableSurvivorDefinition candidate, GameConfigSnapshot config, CampUiCatalogSO catalog, bool canRecruit)
        {
            var backgroundName = GetBackgroundName(candidate.BackgroundId, config);
            var weaponName = GetItemName(candidate.WeaponItemId, config);
            return new CampRadioCandidatePresentation(
                candidate.Id,
                candidate.Name,
                string.IsNullOrEmpty(candidate.Name) ? string.Empty : candidate.Name.Substring(0, 1).ToUpperInvariant(),
                Format(catalog.RadioCandidateCardMetaFormat, backgroundName, weaponName),
                Format(catalog.RadioCandidateSkillFormat, GetSkillLabel(GetBestSkillId(candidate.Skills), catalog), GetBestSkillValue(candidate.Skills)),
                Format(catalog.RadioCandidateTraitsFormat, FormatTraits(candidate.TraitIds, config, catalog)),
                catalog.RadioCandidateRecruitButton,
                canRecruit);
        }

        private static string GetBackgroundName(string backgroundId, GameConfigSnapshot config)
        {
            BackgroundDefinition background;
            return config.Backgrounds.TryGetValue(backgroundId, out background) ? background.Name : backgroundId;
        }

        private static string GetItemName(string itemId, GameConfigSnapshot config)
        {
            ItemDefinition item;
            return config.Items.TryGetValue(itemId, out item) ? item.Name : itemId;
        }

        private static string GetBestSkillId(Dictionary<string, int> skills)
        {
            if (skills == null || skills.Count == 0) return string.Empty;
            var bestId = string.Empty;
            var bestValue = int.MinValue;
            foreach (var pair in skills)
            {
                if (pair.Value <= bestValue) continue;
                bestId = pair.Key;
                bestValue = pair.Value;
            }

            return bestId;
        }

        private static int GetBestSkillValue(Dictionary<string, int> skills)
        {
            if (skills == null || skills.Count == 0) return 0;
            var bestValue = int.MinValue;
            foreach (var pair in skills)
            {
                if (pair.Value > bestValue) bestValue = pair.Value;
            }

            return Math.Max(0, bestValue);
        }

        private static bool IsCandidateRecruited(GameState state, RecruitableSurvivorDefinition candidate)
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

        private static string FormatValidationMessages(ValidationResult validation, CampUiCatalogSO catalog)
        {
            if (validation == null || validation.Errors.Count == 0) return catalog.ReportNoneLabel;
            return string.Join(catalog.ReportListSeparator, validation.Errors.ToArray());
        }

        private static string FormatTopSkill(SurvivorState survivor, CampUiCatalogSO catalog)
        {
            if (survivor == null || survivor.Skills.Count == 0) return string.Empty;

            var bestId = string.Empty;
            var bestValue = int.MinValue;
            foreach (var pair in survivor.Skills)
            {
                if (pair.Value < bestValue) continue;
                if (pair.Value == bestValue && string.CompareOrdinal(pair.Key, bestId) >= 0) continue;

                bestId = pair.Key;
                bestValue = pair.Value;
            }

            return Format(catalog.SurvivorCardSkillFormat, GetSkillLabel(bestId, catalog), Math.Max(0, bestValue));
        }

        private static ExpeditionState FindLatestReportExpedition(GameState state)
        {
            ExpeditionState latest = null;
            foreach (var expedition in state.Expeditions)
            {
                if (expedition.Status != ExpeditionStatus.Completed && expedition.Status != ExpeditionStatus.Failed) continue;
                if (latest == null || expedition.StartedAtUnixMs >= latest.StartedAtUnixMs)
                {
                    latest = expedition;
                }
            }

            return latest;
        }

        private static CampEventState FindLatestCampEvent(GameState state)
        {
            if (state == null) return null;

            CampEventState latest = null;
            foreach (var campEvent in state.CampEvents)
            {
                if (campEvent == null) continue;
                if (!IsSupportedCampEvent(campEvent)) continue;
                if (latest == null || campEvent.AtUnixMs >= latest.AtUnixMs)
                {
                    latest = campEvent;
                }
            }

            return latest;
        }

        private static bool IsSupportedCampEvent(CampEventState campEvent)
        {
            if (campEvent == null) return false;
            return string.Equals(campEvent.EventId, GameEventIds.SurvivorJoined, StringComparison.Ordinal) ||
                   string.Equals(campEvent.EventId, GameEventIds.DemoCompleted, StringComparison.Ordinal) ||
                   string.Equals(campEvent.EventId, GameEventIds.EmergencyScavengeCompleted, StringComparison.Ordinal);
        }

        private static void SetCampEventReport(CampReportsPresentation presentation, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog, CampEventState campEvent)
        {
            if (string.Equals(campEvent.EventId, GameEventIds.DemoCompleted, StringComparison.Ordinal))
            {
                var completionName = GetDemoCompletionName(config, campEvent);
                presentation.CampEventTitle = Format(catalog.DemoCompletedReportTitleFormat, completionName);
                presentation.CampEventBody = Format(catalog.DemoCompletedReportBodyFormat, completionName);
                return;
            }

            if (string.Equals(campEvent.EventId, GameEventIds.EmergencyScavengeCompleted, StringComparison.Ordinal))
            {
                presentation.CampEventTitle = catalog.EmergencyScavengeReportTitle;
                presentation.CampEventBody = Format(
                    catalog.EmergencyScavengeReportBodyFormat,
                    FormatResourceAmounts(RecoverySystem.CalculateEmergencyScavengeRewards(config), config, catalog));
                return;
            }

            presentation.CampEventTitle = Format(catalog.SurvivorJoinedReportTitleFormat, GetCampEventSubjectName(state, campEvent));
            presentation.CampEventBody = Format(
                catalog.SurvivorJoinedReportBodyFormat,
                GetCampEventSubjectName(state, campEvent),
                GetBackgroundName(campEvent.DetailId, config),
                state.Survivors.Count,
                Math.Max(1, state.SurvivorCap));
        }

        private static string GetDemoCompletionName(GameConfigSnapshot config, CampEventState campEvent)
        {
            if (campEvent == null) return string.Empty;
            var completionId = !string.IsNullOrWhiteSpace(campEvent.DetailId) ? campEvent.DetailId : campEvent.SubjectId;
            var separator = completionId.IndexOf(':');
            if (separator < 0 || separator >= completionId.Length - 1) return completionId;

            var type = completionId.Substring(0, separator);
            var id = completionId.Substring(separator + 1);
            if (config != null && (type == GameConditionTypes.ZoneCompletions || type == GameConditionTypes.ZoneUnlocked))
            {
                ZoneDefinition zone;
                if (config.Zones.TryGetValue(id, out zone)) return FormatConfiguredName(id, zone.Name);
            }

            if (config != null && type == GameConditionTypes.BuildingLevel)
            {
                BuildingDefinition building;
                if (config.Buildings.TryGetValue(id, out building)) return FormatConfiguredName(id, building.Name);
            }

            return FormatConfiguredName(completionId, string.Empty);
        }

        private static string FormatConfiguredName(string id, string configuredName)
        {
            if (!string.IsNullOrWhiteSpace(configuredName) && !string.Equals(configuredName, id, StringComparison.Ordinal))
            {
                return configuredName;
            }

            if (string.IsNullOrWhiteSpace(id)) return string.Empty;
            return CultureInfo.InvariantCulture.TextInfo.ToTitleCase(id.Replace('_', ' ').Replace('-', ' '));
        }

        private static void BuildExpeditionSetup(
            CampExpeditionScreenPresentation output,
            GameState state,
            GameConfigSnapshot config,
            CampUiCatalogSO catalog,
            string policyId,
            List<string> survivorIds)
        {
            if (output == null) return;

            var maxSquad = Math.Max(1, state.SquadSize);
            output.SelectedPolicyId = policyId ?? string.Empty;
            output.SelectedSurvivorIds.Clear();
            output.SelectedSurvivorIds.AddRange(survivorIds);
            output.SquadTitle = Format(catalog.ExpeditionSquadTitleFormat, survivorIds.Count, maxSquad);
            output.PolicyTitle = catalog.ExpeditionPolicyTitle;

            var selectedSurvivors = new HashSet<string>(survivorIds, StringComparer.Ordinal);
            foreach (var survivor in state.Survivors)
            {
                var isSelected = selectedSurvivors.Contains(survivor.Id);
                var isIdle = survivor.State == SurvivorActivityState.Idle;
                var power = (int)Math.Round(SquadPowerSystem.CalculateSquadPower(state, config, new List<string> { survivor.Id }, policyId));
                output.SquadMembers.Add(new CampExpeditionSquadMemberPresentation(
                    survivor.Id,
                    survivor.Name,
                    Format(catalog.ExpeditionSquadMemberMetaFormat, survivor.State, survivor.Level, FormatTopSkill(survivor, catalog), power),
                    isSelected,
                    isIdle));
            }

            foreach (var policy in config.Policies.Values)
            {
                output.Policies.Add(new CampExpeditionPolicyPresentation(
                    policy.Id,
                    policy.Name,
                    Format(
                        catalog.ExpeditionPolicyDetailsFormat,
                        FormatModifier(policy.RiskModifier),
                        FormatModifier(policy.LootModifier),
                        FormatModifier(policy.DurationModifier),
                        policy.NoiseModifier),
                    string.Equals(policy.Id, policyId, StringComparison.Ordinal),
                    true));
            }
        }

        private static void BuildSelectedExpeditionDetail(
            CampExpeditionScreenPresentation output,
            GameState state,
            GameConfigSnapshot config,
            CampUiCatalogSO catalog,
            string zoneId,
            string policyId,
            List<string> survivorIds,
            bool riskConfirmationPending)
        {
            if (output == null || string.IsNullOrWhiteSpace(zoneId)) return;
            ZoneDefinition zone;
            if (!config.Zones.TryGetValue(zoneId, out zone)) return;

            ZoneState zoneState;
            state.Zones.TryGetValue(zone.Id, out zoneState);
            var policy = GetExpeditionPolicy(config, policyId);
            var cost = ExpeditionValidator.CalculateCost(config, zone, policy, Math.Max(1, survivorIds.Count));
            var power = survivorIds.Count > 0
                ? (int)Math.Round(SquadPowerSystem.CalculateSquadPower(state, config, survivorIds, policy.Id))
                : 0;
            var familiarity = zoneState != null ? ToWholePercent(zoneState.Familiarity) : 0;
            var validation = ValidateZoneLaunch(state, config, zone.Id, policy.Id, survivorIds);
            var warnings = validation != null && validation.Warnings.Count > 0
                ? string.Join(catalog.ReportListSeparator, validation.Warnings.ToArray())
                : catalog.ExpeditionNoWarningsLabel;
            var requiresRiskConfirmation = validation != null && validation.Warnings.Count > 0;
            var displayWarnings = requiresRiskConfirmation && riskConfirmationPending
                ? Format(catalog.ExpeditionRiskConfirmationNoticeFormat, warnings)
                : warnings;
            var survivalEstimate = Format(catalog.ExpeditionSurvivalChanceFormat, CalculateSurvivalEstimatePercent(power, zone.RecommendedPower));
            var canLaunch = validation != null && validation.IsValid;

            output.Selected = new CampExpeditionDetailPresentation
            {
                ZoneId = zone.Id,
                PolicyId = policy.Id,
                Title = Format(catalog.ExpeditionSelectedTitleFormat, zone.Name, policy.Name),
                Details = Format(
                    catalog.ExpeditionSelectedDetailsFormat,
                    FormatMinutesCeil(zone.BaseDurationSeconds * policy.DurationModifier),
                     GetCostAmount(cost, config.Balance.ExpeditionFoodResourceId),
                     GetCostAmount(cost, config.Balance.ExpeditionWaterResourceId),
                     power,
                     Math.Max(0, zone.RecommendedPower),
                     familiarity,
                     survivalEstimate),
                Loot = Format(catalog.ExpeditionSelectedLootFormat, FormatLootRanges(zone, config, catalog)),
                Enemies = Format(catalog.ExpeditionSelectedEnemiesFormat, FormatEnemyNames(zone, config, catalog)),
                Warnings = Format(catalog.ExpeditionSelectedWarningsFormat, displayWarnings),
                LaunchButton = ResolveLaunchButton(catalog, canLaunch, requiresRiskConfirmation, riskConfirmationPending),
                CanLaunch = canLaunch,
                RequiresRiskConfirmation = requiresRiskConfirmation,
                IsRiskConfirmationPending = riskConfirmationPending && requiresRiskConfirmation
            };
        }

        private static void BuildExpeditionMonitor(CampExpeditionScreenPresentation output, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (output == null) return;
            var expedition = FindActiveExpedition(state);
            if (expedition == null) return;

            ZoneDefinition zone;
            config.Zones.TryGetValue(expedition.ZoneId, out zone);
            var remainingSeconds = Math.Max(0, expedition.ExpectedDurationSeconds - expedition.ElapsedSeconds);
            output.Monitor = new CampExpeditionMonitorPresentation
            {
                HasActiveExpedition = true,
                Title = catalog.ExpeditionMonitorTitle,
                Header = Format(catalog.ExpeditionMonitorHeaderFormat, zone != null ? zone.Name : expedition.ZoneId, expedition.SurvivorIds.Count, FormatMinutesCeil(remainingSeconds)),
                Progress = Format(catalog.ExpeditionMonitorProgressFormat, ToWholePercent(expedition.Progress)),
                Loot = Format(catalog.ExpeditionMonitorLootFormat, FormatResourceAmounts(expedition.AccumulatedLoot, config, catalog)),
                Noise = Format(catalog.ExpeditionMonitorNoiseFormat, FormatNoiseTier(expedition.Noise, catalog), Math.Max(0, expedition.Noise)),
                Log = Format(catalog.ExpeditionMonitorLogFormat, FormatMonitorEvents(state, config, expedition, catalog))
            };
        }

        private static ExpeditionState FindActiveExpedition(GameState state)
        {
            if (state == null) return null;
            foreach (var expedition in state.Expeditions)
            {
                if (expedition.Status == ExpeditionStatus.Active || expedition.Status == ExpeditionStatus.Returning)
                {
                    return expedition;
                }
            }

            return null;
        }

        private static string ResolveSelectedZoneId(GameState state, GameConfigSnapshot config, string selectedZoneId)
        {
            if (!string.IsNullOrWhiteSpace(selectedZoneId) && config.Zones.ContainsKey(selectedZoneId))
            {
                return selectedZoneId;
            }

            foreach (var zone in config.Zones.Values)
            {
                ZoneState zoneState;
                if (state.Zones.TryGetValue(zone.Id, out zoneState) && zoneState.IsUnlocked)
                {
                    return zone.Id;
                }
            }

            foreach (var zone in config.Zones.Values)
            {
                return zone.Id;
            }

            return string.Empty;
        }

        private static ValidationResult ValidateZoneLaunch(GameState state, GameConfigSnapshot config, string zoneId, string policyId, IReadOnlyList<string> survivorIds)
        {
            var request = new LaunchExpeditionRequest
            {
                ZoneId = zoneId,
                PolicyId = ResolvePolicyId(config, policyId),
                SurvivorIds = survivorIds != null ? new List<string>(survivorIds) : SelectIdleSurvivorIds(state),
                ConfirmWarnings = true
            };

            return ExpeditionValidator.Validate(state, config, request);
        }

        private static ExpeditionLaunchViewRequest BuildSendAgainRequest(GameConfigSnapshot config, ExpeditionState expedition)
        {
            if (config == null || expedition == null) return null;
            if (string.IsNullOrWhiteSpace(expedition.ZoneId)) return null;
            if (!config.Zones.ContainsKey(expedition.ZoneId)) return null;
            if (expedition.SurvivorIds.Count == 0) return null;

            var policyId = ResolvePolicyId(config, expedition.PolicyId);
            if (string.IsNullOrWhiteSpace(policyId)) return null;

            return new ExpeditionLaunchViewRequest(expedition.ZoneId, policyId, expedition.SurvivorIds);
        }

        private static List<string> ResolveSelectedSurvivorIds(GameState state, IReadOnlyList<string> requestedSurvivorIds)
        {
            var result = new List<string>();
            if (state == null || requestedSurvivorIds == null) return result;

            var maxCount = Math.Max(1, state.SquadSize);
            for (var i = 0; i < requestedSurvivorIds.Count; i++)
            {
                if (result.Count >= maxCount) break;
                var survivorId = requestedSurvivorIds[i];
                if (string.IsNullOrWhiteSpace(survivorId) || result.Contains(survivorId)) continue;
                var survivor = FindSurvivor(state, survivorId);
                if (survivor != null && survivor.State == SurvivorActivityState.Idle)
                {
                    result.Add(survivorId);
                }
            }

            return result;
        }

        private static List<string> SelectIdleSurvivorIds(GameState state)
        {
            var result = new List<string>();
            if (state == null) return result;
            var maxCount = Math.Max(1, state.SquadSize);
            foreach (var survivor in state.Survivors)
            {
                if (result.Count >= maxCount) break;
                if (survivor.State == SurvivorActivityState.Idle)
                {
                    result.Add(survivor.Id);
                }
            }

            return result;
        }

        private static ExpeditionPolicyDefinition GetExpeditionPolicy(GameConfigSnapshot config, string policyId)
        {
            ExpeditionPolicyDefinition policy;
            if (!string.IsNullOrWhiteSpace(policyId) && config.Policies.TryGetValue(policyId, out policy))
            {
                return policy;
            }

            foreach (var entry in config.Policies.Values)
            {
                return entry;
            }

            return new ExpeditionPolicyDefinition();
        }

        private static string ResolvePolicyId(GameConfigSnapshot config, string policyId)
        {
            return GetExpeditionPolicy(config, policyId).Id;
        }

        private static SurvivorState FindSurvivor(GameState state, string survivorId)
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

        private static string GetCampEventSubjectName(GameState state, CampEventState campEvent)
        {
            if (campEvent == null) return string.Empty;
            var survivor = FindSurvivor(state, campEvent.SubjectId);
            if (survivor != null && !string.IsNullOrWhiteSpace(survivor.Name))
            {
                return survivor.Name;
            }

            return campEvent.SubjectName;
        }

        private static int CalculateSurvivalEstimatePercent(int squadPower, int recommendedPower)
        {
            if (recommendedPower <= 0) return 100;
            if (squadPower <= 0) return 0;
            return Math.Max(5, Math.Min(100, (int)Math.Round(squadPower * 100.0 / recommendedPower)));
        }

        private static string FormatModifier(double value)
        {
            return value.ToString("0.##", CultureInfo.InvariantCulture);
        }

        private static string FormatLootRanges(ZoneDefinition zone, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (zone == null || zone.LootTable.Count == 0) return catalog.ReportNoneLabel;

            var entries = new List<string>();
            foreach (var loot in zone.LootTable)
            {
                ResourceDefinition definition;
                var name = config.Resources.TryGetValue(loot.ResourceId, out definition) ? definition.Name : loot.ResourceId;
                entries.Add(Format(catalog.ExpeditionLootRangeFormat, name, loot.Min, loot.Max));
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatEnemyNames(ZoneDefinition zone, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (zone == null || zone.EnemyTable.Count == 0) return catalog.ReportNoneLabel;

            var names = new List<string>();
            foreach (var entry in zone.EnemyTable)
            {
                EnemyDefinition definition;
                names.Add(config.Enemies.TryGetValue(entry.Id, out definition) ? definition.Name : entry.Id);
            }

            return names.Count > 0 ? string.Join(catalog.ReportListSeparator, names.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatUnlockRequirements(ZoneDefinition zone, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (zone == null) return catalog.ReportNoneLabel;
            var entries = new List<string>();
            foreach (var condition in zone.UnlockConditions)
            {
                if (condition.Type == GameConditionTypes.ZoneCompletions)
                {
                    ZoneDefinition requiredZone;
                    var name = config.Zones.TryGetValue(condition.Id, out requiredZone) ? requiredZone.Name : condition.Id;
                    entries.Add(Format(catalog.ReportCountFormat, name, condition.Value));
                }
                else if (condition.Type == GameConditionTypes.BuildingLevel)
                {
                    BuildingDefinition building;
                    var name = config.Buildings.TryGetValue(condition.Id, out building) ? building.Name : condition.Id;
                    entries.Add(Format(catalog.ExpeditionUnlockRequirementFormat, Format(catalog.LevelLabelFormat, condition.Value), name));
                }
            }

            foreach (var requirement in zone.RequiredBuildingLevels)
            {
                BuildingDefinition building;
                var name = config.Buildings.TryGetValue(requirement.Key, out building) ? building.Name : requirement.Key;
                entries.Add(Format(catalog.ExpeditionUnlockRequirementFormat, Format(catalog.LevelLabelFormat, requirement.Value), name));
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatResourceAmounts(Dictionary<string, int> amounts, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (amounts == null || amounts.Count == 0) return catalog.ReportNoneLabel;

            var entries = new List<string>();
            foreach (var pair in amounts)
            {
                if (pair.Value <= 0) continue;
                ResourceDefinition definition;
                var name = config.Resources.TryGetValue(pair.Key, out definition) ? definition.Name : pair.Key;
                entries.Add(Format(catalog.ReportCountFormat, name, pair.Value));
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatEnemyCounts(Dictionary<string, int> enemies, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (enemies == null || enemies.Count == 0) return catalog.ReportNoneLabel;

            var entries = new List<string>();
            foreach (var pair in enemies)
            {
                if (pair.Value <= 0) continue;
                EnemyDefinition definition;
                var name = config.Enemies.TryGetValue(pair.Key, out definition) ? definition.Name : pair.Key;
                entries.Add(Format(catalog.ReportCountFormat, name, pair.Value));
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatSurvivorNames(GameState state, List<string> survivorIds, CampUiCatalogSO catalog)
        {
            if (survivorIds == null || survivorIds.Count == 0) return catalog.ReportNoneLabel;

            var names = new List<string>();
            foreach (var survivorId in survivorIds)
            {
                var name = GetSurvivorName(state, survivorId);
                if (!string.IsNullOrWhiteSpace(name)) names.Add(name);
            }

            return names.Count > 0 ? string.Join(catalog.ReportListSeparator, names.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatCompletedExpeditions(GameState state, GameConfigSnapshot config, List<string> expeditionIds, CampUiCatalogSO catalog)
        {
            if (expeditionIds == null || expeditionIds.Count == 0) return catalog.ReportNoneLabel;

            var names = new List<string>();
            foreach (var expeditionId in expeditionIds)
            {
                var expedition = FindExpedition(state, expeditionId);
                if (expedition == null)
                {
                    names.Add(expeditionId);
                    continue;
                }

                ZoneDefinition zone;
                names.Add(config.Zones.TryGetValue(expedition.ZoneId, out zone) ? zone.Name : expedition.ZoneId);
            }

            return names.Count > 0 ? string.Join(catalog.ReportListSeparator, names.ToArray()) : catalog.ReportNoneLabel;
        }

        private static ExpeditionState FindExpedition(GameState state, string expeditionId)
        {
            if (state == null || string.IsNullOrWhiteSpace(expeditionId)) return null;
            foreach (var expedition in state.Expeditions)
            {
                if (string.Equals(expedition.Id, expeditionId, StringComparison.Ordinal))
                {
                    return expedition;
                }
            }

            return null;
        }

        private static string FormatRecentEvents(ExpeditionState expedition, CampUiCatalogSO catalog)
        {
            return FormatRecentEvents(expedition, catalog, Math.Max(0, catalog.ExpeditionMonitorLogLineCount));
        }

        private static string FormatRecentEvents(ExpeditionState expedition, CampUiCatalogSO catalog, int maxCount)
        {
            if (expedition == null || expedition.Log.Count == 0) return catalog.ReportNoneLabel;

            var start = Math.Max(0, expedition.Log.Count - Math.Max(1, maxCount));
            var entries = new List<string>();
            for (var i = start; i < expedition.Log.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(expedition.Log[i].Message))
                {
                    entries.Add(expedition.Log[i].Message);
                }
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatMonitorEvents(GameState state, GameConfigSnapshot config, ExpeditionState expedition, CampUiCatalogSO catalog)
        {
            if (expedition == null) return catalog.ReportNoneLabel;
            var entries = new List<string>();
            var defeated = FormatEnemyCounts(expedition.EnemiesDefeated, config, catalog);
            if (!string.Equals(defeated, catalog.ReportNoneLabel, StringComparison.Ordinal))
            {
                entries.Add(Format(catalog.ExpeditionMonitorThreatFormat, defeated));
            }

            var wounded = FormatSurvivorNames(state, expedition.WoundedSurvivorIds, catalog);
            if (!string.Equals(wounded, catalog.ReportNoneLabel, StringComparison.Ordinal))
            {
                entries.Add(Format(catalog.ExpeditionMonitorWoundsFormat, wounded));
            }

            var foundItems = FormatFoundItems(expedition.FoundItems, config, catalog);
            if (!string.Equals(foundItems, catalog.ReportNoneLabel, StringComparison.Ordinal))
            {
                entries.Add(Format(catalog.ExpeditionMonitorFoundItemsFormat, foundItems));
            }

            AppendRecentEvents(entries, expedition, catalog, Math.Max(0, catalog.ExpeditionMonitorLogLineCount));
            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static void AppendRecentEvents(List<string> entries, ExpeditionState expedition, CampUiCatalogSO catalog, int maxCount)
        {
            if (entries == null || expedition == null || expedition.Log.Count == 0) return;

            var start = Math.Max(0, expedition.Log.Count - Math.Max(1, maxCount));
            for (var i = start; i < expedition.Log.Count; i++)
            {
                if (!string.IsNullOrWhiteSpace(expedition.Log[i].Message))
                {
                    entries.Add(expedition.Log[i].Message);
                }
            }
        }

        private static string FormatFoundItems(List<InventoryItemState> foundItems, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (foundItems == null || foundItems.Count == 0) return catalog.ReportNoneLabel;

            var counts = new Dictionary<string, int>(StringComparer.Ordinal);
            foreach (var item in foundItems)
            {
                if (item == null || string.IsNullOrWhiteSpace(item.ItemId)) continue;
                int count;
                counts.TryGetValue(item.ItemId, out count);
                counts[item.ItemId] = count + 1;
            }

            if (counts.Count == 0) return catalog.ReportNoneLabel;

            var entries = new List<string>();
            foreach (var pair in counts)
            {
                ItemDefinition definition;
                var name = config.Items.TryGetValue(pair.Key, out definition) ? definition.Name : pair.Key;
                entries.Add(Format(catalog.ReportCountFormat, name, pair.Value));
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatNoiseTier(int noise, CampUiCatalogSO catalog)
        {
            var amount = Math.Max(0, noise);
            if (amount >= Math.Max(0, catalog.ExpeditionMonitorNoiseHighThreshold)) return catalog.ExpeditionMonitorNoiseHighLabel;
            if (amount >= Math.Max(0, catalog.ExpeditionMonitorNoiseMediumThreshold)) return catalog.ExpeditionMonitorNoiseMediumLabel;
            return catalog.ExpeditionMonitorNoiseLowLabel;
        }

        private static int CalculateExpeditionXp(ExpeditionState expedition, GameConfigSnapshot config)
        {
            if (expedition == null) return 0;

            var xp = expedition.Status == ExpeditionStatus.Completed ? expedition.SurvivorIds.Count * 5 : 0;
            foreach (var pair in expedition.EnemiesDefeated)
            {
                EnemyDefinition enemy;
                if (config.Enemies.TryGetValue(pair.Key, out enemy))
                {
                    xp += Math.Max(0, enemy.XpReward) * Math.Max(0, pair.Value);
                }
            }

            return xp;
        }

        private static string GetSkillLabel(string skillId, CampUiCatalogSO catalog)
        {
            foreach (var entry in catalog.SurvivorSkillLabels)
            {
                if (entry != null && string.Equals(entry.Id, skillId, StringComparison.Ordinal))
                {
                    return entry.Label;
                }
            }

            return skillId;
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

        private static bool HasLowEmergencyResource(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state == null || config == null || catalog == null) return false;
            foreach (var reward in config.Balance.EmergencyScavengeRewards)
            {
                ResourceDefinition resource;
                if (!config.Resources.TryGetValue(reward.Key, out resource) || !resource.HasCap) continue;

                int amount;
                state.Resources.TryGetValue(resource.Id, out amount);
                if (amount <= 0) return true;

                int cap;
                if (!state.ResourceCaps.TryGetValue(resource.Id, out cap)) cap = resource.StartCap;
                if (cap <= 0) continue;
                if (catalog.LowResourceAlertPercentThreshold > 0 && amount * 100.0 / cap <= catalog.LowResourceAlertPercentThreshold)
                {
                    return true;
                }
            }

            return false;
        }
    }

    public sealed class CampSurvivorCardPresentation
    {
        public readonly string SurvivorId;
        public readonly string Name;
        public readonly string Avatar;
        public readonly string State;
        public readonly string Skill;

        public CampSurvivorCardPresentation(string survivorId, string name, string avatar, string state, string skill)
        {
            SurvivorId = survivorId ?? string.Empty;
            Name = name ?? string.Empty;
            Avatar = avatar ?? string.Empty;
            State = state ?? string.Empty;
            Skill = skill ?? string.Empty;
        }
    }

    public sealed class CampSurvivorDetailPresentation
    {
        public string Title = string.Empty;
        public string Background = string.Empty;
        public string Traits = string.Empty;
        public string Weapon = string.Empty;
        public string Treatment = string.Empty;
        public string Stats = string.Empty;
        public string MedicineCost = string.Empty;
        public string MedicineButton = string.Empty;
        public bool ShowMedicineAction;
        public bool CanUseMedicine;
    }

    public sealed class CampWorkshopItemPresentation
    {
        public readonly string ItemUid;
        public readonly string Name;
        public readonly int Durability;
        public readonly int MaxDurability;
        public readonly string Equipped;
        public readonly string BrokenLabel;
        public readonly string RepairCost;
        public readonly bool CanRepair;
        public readonly bool CanEquip;

        public CampWorkshopItemPresentation(string itemUid, string name, int durability, int maxDurability, string equipped, string brokenLabel, string repairCost, bool canRepair, bool canEquip)
        {
            ItemUid = itemUid ?? string.Empty;
            Name = name ?? string.Empty;
            Durability = durability;
            MaxDurability = maxDurability;
            Equipped = equipped ?? string.Empty;
            BrokenLabel = brokenLabel ?? string.Empty;
            RepairCost = repairCost ?? string.Empty;
            CanRepair = canRepair;
            CanEquip = canEquip;
        }
    }

    public sealed class CampReportsPresentation
    {
        public string Title = string.Empty;
        public string EmptyTitle = string.Empty;
        public string EmptyBody = string.Empty;
        public bool HasAfterAction;
        public string AfterActionTitle = string.Empty;
        public string AfterActionOutcome = string.Empty;
        public string AfterActionLoot = string.Empty;
        public string AfterActionXp = string.Empty;
        public string AfterActionWounds = string.Empty;
        public string AfterActionEnemies = string.Empty;
        public string AfterActionEvents = string.Empty;
        public string AfterActionSendAgainButton = string.Empty;
        public bool AfterActionCanSendAgain;
        public ExpeditionLaunchViewRequest AfterActionSendAgainRequest;
        public bool HasCampEvent;
        public string CampEventPanelTitle = string.Empty;
        public string CampEventTitle = string.Empty;
        public string CampEventBody = string.Empty;
        public bool HasOfflineReport;
        public string OfflineReportTitle = string.Empty;
        public string OfflineSummary = string.Empty;
        public string OfflineResources = string.Empty;
        public string OfflineCompleted = string.Empty;
        public string OfflineHealing = string.Empty;
        public string OfflineWarnings = string.Empty;

        public bool HasAnyReport
        {
            get { return HasAfterAction || HasCampEvent || HasOfflineReport; }
        }
    }

    public sealed class CampSettingsPresentation
    {
        public string Title = string.Empty;
        public string AutosaveTitle = string.Empty;
        public string AutosaveBody = string.Empty;
        public string AutosaveState = string.Empty;
        public string AutosaveToggleLabel = string.Empty;
        public bool AutosaveEnabled;
    }

    public sealed class CampRadioPresentation
    {
        public string Title = string.Empty;
        public string IntelTitle = string.Empty;
        public string IntelBody = string.Empty;
        public string BroadcastTitle = string.Empty;
        public string BroadcastCost = string.Empty;
        public string BroadcastStatus = string.Empty;
        public string BroadcastButton = string.Empty;
        public bool CanBroadcast;
        public bool CanSkipCandidates;
        public string CandidateListTitle = string.Empty;
        public string EmptyTitle = string.Empty;
        public string EmptyBody = string.Empty;
        public List<CampRadioCandidatePresentation> Candidates = new List<CampRadioCandidatePresentation>();
    }

    public sealed class CampNextGoalPresentation
    {
        public string Title = string.Empty;
        public string Body = string.Empty;
        public string Progress = string.Empty;
        public bool IsComplete;
    }

    public sealed class CampRadioCandidatePresentation
    {
        public readonly string CandidateId;
        public readonly string Name;
        public readonly string Avatar;
        public readonly string Meta;
        public readonly string Skill;
        public readonly string Traits;
        public readonly string RecruitButton;
        public readonly bool CanRecruit;

        public CampRadioCandidatePresentation(string candidateId, string name, string avatar, string meta, string skill, string traits, string recruitButton, bool canRecruit)
        {
            CandidateId = candidateId ?? string.Empty;
            Name = name ?? string.Empty;
            Avatar = avatar ?? string.Empty;
            Meta = meta ?? string.Empty;
            Skill = skill ?? string.Empty;
            Traits = traits ?? string.Empty;
            RecruitButton = recruitButton ?? string.Empty;
            CanRecruit = canRecruit;
        }
    }

    public sealed class CampExpeditionScreenPresentation
    {
        public string Title = string.Empty;
        public string SelectedZoneId = string.Empty;
        public string SelectedPolicyId = string.Empty;
        public List<string> SelectedSurvivorIds = new List<string>();
        public List<CampExpeditionRoutePresentation> Routes = new List<CampExpeditionRoutePresentation>();
        public string SquadTitle = string.Empty;
        public List<CampExpeditionSquadMemberPresentation> SquadMembers = new List<CampExpeditionSquadMemberPresentation>();
        public string PolicyTitle = string.Empty;
        public List<CampExpeditionPolicyPresentation> Policies = new List<CampExpeditionPolicyPresentation>();
        public CampExpeditionDetailPresentation Selected = new CampExpeditionDetailPresentation();
        public CampExpeditionMonitorPresentation Monitor = new CampExpeditionMonitorPresentation();
    }

    public sealed class CampExpeditionRoutePresentation
    {
        public readonly string ZoneId;
        public readonly string Title;
        public readonly string Subtitle;
        public readonly string Status;
        public readonly bool CanSelect;
        public readonly bool CanLaunch;
        public readonly bool IsSelected;

        public CampExpeditionRoutePresentation(string zoneId, string title, string subtitle, string status, bool canSelect, bool canLaunch, bool isSelected)
        {
            ZoneId = zoneId ?? string.Empty;
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            Status = status ?? string.Empty;
            CanSelect = canSelect;
            CanLaunch = canLaunch;
            IsSelected = isSelected;
        }
    }

    public sealed class CampExpeditionSquadMemberPresentation
    {
        public readonly string SurvivorId;
        public readonly string Name;
        public readonly string Meta;
        public readonly bool IsSelected;
        public readonly bool CanSelect;

        public CampExpeditionSquadMemberPresentation(string survivorId, string name, string meta, bool isSelected, bool canSelect)
        {
            SurvivorId = survivorId ?? string.Empty;
            Name = name ?? string.Empty;
            Meta = meta ?? string.Empty;
            IsSelected = isSelected;
            CanSelect = canSelect;
        }
    }

    public sealed class CampExpeditionPolicyPresentation
    {
        public readonly string PolicyId;
        public readonly string Title;
        public readonly string Details;
        public readonly bool IsSelected;
        public readonly bool CanSelect;

        public CampExpeditionPolicyPresentation(string policyId, string title, string details, bool isSelected, bool canSelect)
        {
            PolicyId = policyId ?? string.Empty;
            Title = title ?? string.Empty;
            Details = details ?? string.Empty;
            IsSelected = isSelected;
            CanSelect = canSelect;
        }
    }

    public sealed class CampExpeditionDetailPresentation
    {
        public string ZoneId = string.Empty;
        public string PolicyId = string.Empty;
        public string Title = string.Empty;
        public string Details = string.Empty;
        public string Loot = string.Empty;
        public string Enemies = string.Empty;
        public string Warnings = string.Empty;
        public string LaunchButton = string.Empty;
        public bool CanLaunch;
        public bool RequiresRiskConfirmation;
        public bool IsRiskConfirmationPending;
    }

    public sealed class CampExpeditionMonitorPresentation
    {
        public bool HasActiveExpedition;
        public string Title = string.Empty;
        public string Header = string.Empty;
        public string Progress = string.Empty;
        public string Loot = string.Empty;
        public string Noise = string.Empty;
        public string Log = string.Empty;
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
        public readonly string ActionLabel;
        public readonly bool CanStartEmergencyScavenge;

        public CampAlertPresentation(string title, string body, Color toneColor)
            : this(title, body, toneColor, string.Empty, false)
        {
        }

        public CampAlertPresentation(string title, string body, Color toneColor, string actionLabel, bool canStartEmergencyScavenge)
        {
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            ToneColor = toneColor;
            ActionLabel = actionLabel ?? string.Empty;
            CanStartEmergencyScavenge = canStartEmergencyScavenge && !string.IsNullOrWhiteSpace(ActionLabel);
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
