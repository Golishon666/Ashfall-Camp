using System;
using System.Collections.Generic;
using System.Globalization;
using AshfallCamp.Domain;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    public static class CampDashboardTextFormatter
    {
        private const string AlertIdSurvivorJoined = "survivor_joined";
        private const string AlertIdDemoCompleted = "demo_completed";
        private const string AlertIdEmergencyScavengeCompleted = "emergency_scavenge_completed";
        private const string AlertIdActiveExpedition = "active_expedition";
        private const string AlertIdWoundedSurvivors = "wounded_survivors";
        private const string AlertIdIdleSurvivors = "idle_survivors";
        private const string AlertIdEmergencyScavenge = "emergency_scavenge";
        private const string AlertIdLowResource = "low_resource";
        private const string AlertIdUpgradeAvailable = "upgrade_available";
        private const string AlertIdNoAlerts = "no_alerts";

        private const string AlertCategoryEvents = "events";
        private const string AlertCategoryExpeditions = "expeditions";
        private const string AlertCategorySurvivors = "survivors";
        private const string AlertCategorySupplies = "supplies";
        private const string AlertCategoryBuildings = "buildings";
        private const string AlertCategorySystem = "system";

        private const string ScreenIdSurvivors = "survivors";
        private const string ScreenIdBuildings = "buildings";
        private const string ScreenIdExpeditions = "expeditions";
        private const string ScreenIdReports = "reports";

        public static List<CampAlertPresentation> BuildAlerts(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            var result = new List<CampAlertPresentation>();
            if (state == null || config == null || catalog == null) return result;

            AddCampEventAlert(result, state, config, catalog);
            AddActiveExpeditionAlert(result, state, config, catalog);
            AddWoundedAlert(result, state, catalog);
            AddIdleSurvivorsAlert(result, state, catalog);
            AddEmergencyScavengeAlert(result, state, config, catalog);
            AddLowResourceAlert(result, state, config, catalog);
            AddUpgradeAlert(result, state, config, catalog);
            SortAndDeduplicateAlerts(result);

            if (result.Count == 0 && (!string.IsNullOrWhiteSpace(catalog.NoAlertsTitle) || !string.IsNullOrWhiteSpace(catalog.NoAlertsBody)))
            {
                result.Add(CreateAlert(
                    catalog,
                    AlertIdNoAlerts,
                    catalog.NoAlertsTitle,
                    catalog.NoAlertsBody,
                    catalog.Theme.Sage,
                    CampAlertSeverity.Info,
                    0,
                    AlertCategorySystem,
                    CampAlertAction.None,
                    string.Empty,
                    string.Empty));
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
                config.TryGetZone(expedition.ZoneId, out zone);
                ExpeditionPolicyDefinition policy;
                config.TryGetPolicy(expedition.PolicyId, out policy);

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
                if (!config.TryGetZone(zoneState.Id, out zone)) continue;
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
                var riskTier = zone.RiskTier.ToString();
                var validation = unlocked ? ValidateZoneLaunch(state, config, zone.Id, policyId, survivorIds) : null;
                var status = unlocked
                    ? Format(catalog.ExpeditionRouteStatusFormat, ToWholePercent(familiarity))
                    : Format(catalog.ExpeditionLockedStatusFormat, FormatUnlockRequirements(zone, config, catalog));

                result.Routes.Add(new CampExpeditionRoutePresentation(
                    zone.Id,
                    zone.Name,
                    Format(catalog.ExpeditionRouteSubtitleFormat, riskTier, zone.FoodCostPerSurvivor, zone.WaterCostPerSurvivor),
                    status,
                    unlocked,
                    validation != null && validation.IsValid,
                    string.Equals(zone.Id, selectedId, StringComparison.Ordinal),
                    riskTier,
                    FindExpeditionZoneThumbnail(catalog, zone.Id),
                    FindExpeditionRiskBadge(catalog, riskTier)));
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
                if (presentation.Candidates.Count >= RecruitmentSystem.MaxCandidateCount) break;
                RecruitableSurvivorDefinition candidate;
                if (!config.TryGetRecruitableSurvivor(candidateId, out candidate)) continue;
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
                    FormatTopSkill(survivor, catalog),
                    "LEVEL " + Math.Max(1, survivor.Level).ToString(CultureInfo.InvariantCulture),
                    CalculateSurvivorPower(survivor).ToString("N0", CultureInfo.InvariantCulture),
                    CalculateHealthValue(survivor),
                    CalculateFatigueValue(survivor),
                    FormatSurvivorHealth(survivor),
                    FormatSurvivorFatigue(survivor),
                    ResolveSurvivorPortrait(survivor, catalog)));
            }

            return result;
        }

        public static CampSurvivorDetailPresentation BuildSurvivorDetail(SurvivorState survivor, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (survivor == null || state == null || config == null || catalog == null)
            {
                return new CampSurvivorDetailPresentation();
            }

            var presentation = new CampSurvivorDetailPresentation
            {
                Title = Format(catalog.SurvivorDetailTitle, survivor.Name, survivor.Level),
                Background = Format(catalog.SurvivorDetailBackgroundFormat, GetBackgroundName(survivor, config)),
                Traits = Format(catalog.SurvivorDetailTraitsFormat, FormatTraits(survivor, config, catalog)),
                Weapon = FormatWeapon(survivor, state, config, catalog),
                Treatment = FormatTreatment(survivor, state, config, catalog),
                Stats = Format(catalog.SurvivorDetailStatsFormat, survivor.Health, survivor.MaxHealth, survivor.Morale, survivor.Fatigue, survivor.Xp),
                LevelText = "LEVEL " + Math.Max(1, survivor.Level).ToString(CultureInfo.InvariantCulture),
                XpText = Math.Max(0, survivor.Xp).ToString("N0", CultureInfo.InvariantCulture) + " XP",
                StatusText = survivor.State.ToString(),
                HealthValue = CalculateHealthValue(survivor),
                FatigueValue = CalculateFatigueValue(survivor),
                MoraleValue = CalculateMoraleValue(survivor),
                HealthText = FormatSurvivorHealth(survivor),
                FatigueText = FormatSurvivorFatigue(survivor),
                MoraleText = FormatSurvivorMorale(survivor),
                MedicineCost = Format(catalog.SurvivorDetailMedicineCostFormat, FormatResourceAmounts(HealingSystem.CalculateMedicineCost(config), config, catalog)),
                MedicineButton = catalog.SurvivorDetailUseMedicineButton,
                ActionCost = string.Empty,
                ActionButton = string.Empty,
                Portrait = ResolveSurvivorPortrait(survivor, catalog)
            };

            ApplySurvivorDetailAction(presentation, survivor, state, config, catalog);
            return presentation;
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
                var itemName = config.TryGetItem(item.ItemId, out definition) ? definition.Name : item.ItemId;
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
                config.TryGetZone(expedition.ZoneId, out zone);
                var zoneName = zone != null ? zone.Name : expedition.ZoneId;
                var outcome = expedition.Status == ExpeditionStatus.Completed ? catalog.AfterActionSuccessLabel : catalog.AfterActionFailureLabel;
                presentation.HasAfterAction = true;
                presentation.AfterActionTitle = catalog.AfterActionPanelTitle;
                presentation.AfterActionOutcome = Format(catalog.AfterActionOutcomeFormat, zoneName, outcome, FormatMinutesCeil(expedition.ElapsedSeconds));
                presentation.AfterActionLoot = Format(catalog.AfterActionLootFormat, FormatResourceAmounts(expedition.AccumulatedLoot, config, catalog));
                presentation.AfterActionXp = Format(catalog.AfterActionXpFormat, CalculateExpeditionXp(expedition, config));
                presentation.AfterActionWounds = Format(catalog.AfterActionWoundsFormat, FormatSurvivorNames(state, expedition.WoundedSurvivorIds, catalog));
                presentation.AfterActionEnemies = Format(catalog.AfterActionEnemiesFormat, FormatEnemyCounts(expedition.EnemiesDefeated, config, catalog));
                presentation.AfterActionEvents = Format(catalog.AfterActionEventsFormat, FormatAfterActionDetails(state, config, catalog, expedition, zone));
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
                presentation.OfflineResources = Format(catalog.OfflineReportResourcesFormat, FormatOfflineResourceChanges(offline, config, catalog));
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
            presentation.ManualSaveTitle = catalog.SettingsManualSaveTitle;
            presentation.ManualSaveBody = catalog.SettingsManualSaveBody;
            presentation.ManualSaveButton = catalog.SettingsManualSaveButton;
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
                output.Add(CreateAlert(
                    catalog,
                    AlertIdDemoCompleted,
                    Format(catalog.DemoCompletedAlertTitleFormat, completionName),
                    Format(catalog.DemoCompletedAlertBodyFormat, completionName),
                    catalog.Theme.Amber,
                    CampAlertSeverity.Success,
                    1000,
                    AlertCategoryEvents,
                    CampAlertAction.OpenScreen,
                    ScreenIdReports,
                    "VIEW"));
                return;
            }

            if (string.Equals(campEvent.EventId, GameEventIds.EmergencyScavengeCompleted, StringComparison.Ordinal))
            {
                output.Add(CreateAlert(
                    catalog,
                    AlertIdEmergencyScavengeCompleted,
                    catalog.EmergencyScavengeCompletedAlertTitle,
                    Format(catalog.EmergencyScavengeCompletedAlertBodyFormat, FormatResourceAmounts(RecoverySystem.CalculateEmergencyScavengeRewards(config), config, catalog)),
                    catalog.Theme.Sage,
                    CampAlertSeverity.Success,
                    1000,
                    AlertCategoryEvents,
                    CampAlertAction.OpenScreen,
                    ScreenIdReports,
                    "VIEW"));
                return;
            }

            output.Add(CreateAlert(
                catalog,
                AlertIdSurvivorJoined,
                Format(catalog.SurvivorJoinedAlertTitleFormat, GetCampEventSubjectName(state, campEvent)),
                Format(catalog.SurvivorJoinedAlertBodyFormat, GetCampEventSubjectName(state, campEvent), state.Survivors.Count, Math.Max(1, state.SurvivorCap)),
                catalog.Theme.Sage,
                CampAlertSeverity.Success,
                1000,
                AlertCategoryEvents,
                CampAlertAction.OpenScreen,
                ScreenIdReports,
                "VIEW"));
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
            config.TryGetZone(first.ZoneId, out zone);
            var zoneName = zone != null ? zone.Name : first.ZoneId;
            output.Add(CreateAlert(
                catalog,
                AlertIdActiveExpedition,
                Format(catalog.ActiveExpeditionAlertTitleFormat, count),
                Format(catalog.ActiveExpeditionAlertBodyFormat, zoneName, ToWholePercent(first.Progress)),
                catalog.Theme.Teal,
                CampAlertSeverity.Info,
                650,
                AlertCategoryExpeditions,
                CampAlertAction.OpenScreen,
                ScreenIdExpeditions,
                "VIEW"));
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

            output.Add(CreateAlert(
                catalog,
                AlertIdWoundedSurvivors,
                Format(catalog.WoundedAlertTitleFormat, count),
                Format(catalog.WoundedAlertBodyFormat, first.Name),
                catalog.Theme.Rust,
                CampAlertSeverity.Critical,
                900,
                AlertCategorySurvivors,
                CampAlertAction.OpenScreen,
                ScreenIdSurvivors,
                "VIEW"));
        }

        private static void AddIdleSurvivorsAlert(List<CampAlertPresentation> output, GameState state, CampUiCatalogSO catalog)
        {
            var idle = UiStateQueries.CountIdleSurvivors(state);
            if (idle <= 0) return;

            var title = string.IsNullOrWhiteSpace(catalog.IdleSurvivorsAlertTitleFormat)
                ? Format("{0} IDLE SURVIVORS", idle)
                : Format(catalog.IdleSurvivorsAlertTitleFormat, idle);
            var body = string.IsNullOrWhiteSpace(catalog.IdleSurvivorsAlertBodyFormat)
                ? Format("{0} survivors are ready for expeditions or camp work.", idle)
                : Format(catalog.IdleSurvivorsAlertBodyFormat, idle);
            output.Add(CreateAlert(
                catalog,
                AlertIdIdleSurvivors,
                title,
                body,
                catalog.Theme.Amber,
                CampAlertSeverity.Warning,
                500,
                AlertCategorySurvivors,
                CampAlertAction.OpenScreen,
                ScreenIdExpeditions,
                "MANAGE"));
        }

        private static void AddEmergencyScavengeAlert(List<CampAlertPresentation> output, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (state.Recovery == null) return;

            if (state.Recovery.EmergencyScavengeActive)
            {
                output.Add(CreateAlert(
                    catalog,
                    AlertIdEmergencyScavenge,
                    catalog.EmergencyScavengeAlertTitle,
                    Format(catalog.EmergencyScavengeActiveBodyFormat, FormatSecondsCeil(state.Recovery.EmergencyScavengeRemainingSeconds)),
                    catalog.Theme.Teal,
                    CampAlertSeverity.Info,
                    850,
                    AlertCategorySupplies,
                    CampAlertAction.None,
                    string.Empty,
                    string.Empty));
                return;
            }

            if (!HasLowEmergencyResource(state, config, catalog)) return;

            if (state.Recovery.EmergencyScavengeCooldownRemainingSeconds > 0)
            {
                output.Add(CreateAlert(
                    catalog,
                    AlertIdEmergencyScavenge,
                    catalog.EmergencyScavengeAlertTitle,
                    Format(catalog.EmergencyScavengeCooldownBodyFormat, FormatSecondsCeil(state.Recovery.EmergencyScavengeCooldownRemainingSeconds)),
                    catalog.Theme.Amber,
                    CampAlertSeverity.Warning,
                    850,
                    AlertCategorySupplies,
                    CampAlertAction.None,
                    string.Empty,
                    string.Empty));
                return;
            }

            var rewards = RecoverySystem.CalculateEmergencyScavengeRewards(config);
            var canStart = RecoverySystem.ValidateEmergencyScavenge(state, config, new EmergencyScavengeRequest()).IsValid;
            output.Add(CreateAlert(
                catalog,
                AlertIdEmergencyScavenge,
                catalog.EmergencyScavengeAlertTitle,
                Format(catalog.EmergencyScavengeReadyBodyFormat, FormatSecondsCeil(config.Balance.EmergencyScavengeDurationSeconds), FormatResourceAmounts(rewards, config, catalog)),
                catalog.Theme.Rust,
                CampAlertSeverity.Critical,
                850,
                AlertCategorySupplies,
                canStart ? CampAlertAction.StartEmergencyScavenge : CampAlertAction.None,
                string.Empty,
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

            output.Add(CreateAlert(
                catalog,
                AlertIdLowResource,
                Format(catalog.LowResourceAlertTitleFormat, selected.Name),
                Format(catalog.LowResourceAlertBodyFormat, selectedAmount, selectedCap),
                catalog.Theme.Rust,
                CampAlertSeverity.Critical,
                800,
                AlertCategorySupplies,
                CampAlertAction.OpenScreen,
                ScreenIdExpeditions,
                "VIEW"));
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

                output.Add(CreateAlert(
                    catalog,
                    AlertIdUpgradeAvailable,
                    Format(catalog.UpgradeAvailableAlertTitleFormat, definition.Name),
                    Format(catalog.UpgradeAvailableAlertBodyFormat, nextLevel.Level),
                    catalog.Theme.Amber,
                    CampAlertSeverity.Warning,
                    450,
                    AlertCategoryBuildings,
                    CampAlertAction.OpenScreen,
                    ScreenIdBuildings,
                    "VIEW"));
                return;
            }
        }

        private static CampAlertPresentation CreateAlert(
            CampUiCatalogSO catalog,
            string id,
            string title,
            string body,
            Color fallbackTone,
            CampAlertSeverity defaultSeverity,
            int defaultPriority,
            string defaultCategory,
            CampAlertAction defaultAction,
            string defaultTargetScreenId,
            string defaultActionLabel,
            bool canInvokeAction = true)
        {
            var config = FindAlertConfig(catalog, id);
            var severity = config != null ? config.Severity : defaultSeverity;
            var priority = config != null && config.Priority != 0 ? config.Priority : defaultPriority;
            var category = config != null && !string.IsNullOrWhiteSpace(config.Category) ? config.Category : defaultCategory;
            var action = defaultAction != CampAlertAction.None && config != null && config.Action != CampAlertAction.None
                ? config.Action
                : defaultAction;
            var targetScreenId = config != null && !string.IsNullOrWhiteSpace(config.TargetScreenId)
                ? config.TargetScreenId
                : defaultTargetScreenId;
            var actionLabel = config != null && !string.IsNullOrWhiteSpace(config.ButtonLabel)
                ? config.ButtonLabel
                : defaultActionLabel;
            var buttonView = config != null ? config.ButtonView : CampAlertButtonView.Text;
            if (action == CampAlertAction.None)
            {
                buttonView = CampAlertButtonView.Hidden;
                actionLabel = string.Empty;
                canInvokeAction = false;
            }

            var tone = ResolveAlertTone(catalog, config, fallbackTone, severity);
            return new CampAlertPresentation(
                id,
                title,
                body,
                tone,
                severity,
                priority,
                category,
                action,
                targetScreenId,
                actionLabel,
                buttonView,
                config != null ? config.Icon : null,
                config != null ? config.ActionIcon : null,
                canInvokeAction);
        }

        private static AlertUiEntry FindAlertConfig(CampUiCatalogSO catalog, string id)
        {
            if (catalog == null || catalog.Alerts == null || string.IsNullOrWhiteSpace(id)) return null;
            foreach (var entry in catalog.Alerts)
            {
                if (entry != null && string.Equals(entry.Id, id, StringComparison.Ordinal))
                {
                    return entry;
                }
            }

            return null;
        }

        private static Color ResolveAlertTone(CampUiCatalogSO catalog, AlertUiEntry config, Color fallbackTone, CampAlertSeverity severity)
        {
            if (config != null && config.ToneColor != Color.white)
            {
                return config.ToneColor;
            }

            if (catalog == null || catalog.Theme == null) return fallbackTone;
            switch (severity)
            {
                case CampAlertSeverity.Critical:
                    return catalog.Theme.Rust;
                case CampAlertSeverity.Warning:
                    return catalog.Theme.Amber;
                case CampAlertSeverity.Success:
                    return catalog.Theme.Sage;
                default:
                    return fallbackTone;
            }
        }

        private static void SortAndDeduplicateAlerts(List<CampAlertPresentation> alerts)
        {
            if (alerts == null || alerts.Count <= 1) return;

            for (var i = alerts.Count - 1; i >= 0; i--)
            {
                var current = alerts[i];
                if (current == null || string.IsNullOrWhiteSpace(current.Id)) continue;

                for (var j = 0; j < i; j++)
                {
                    var previous = alerts[j];
                    if (previous == null || !string.Equals(previous.Id, current.Id, StringComparison.Ordinal)) continue;

                    if (CompareAlertPriority(current, previous) > 0)
                    {
                        alerts[j] = current;
                    }

                    alerts.RemoveAt(i);
                    break;
                }
            }

            alerts.Sort((left, right) => -CompareAlertPriority(left, right));
        }

        private static int CompareAlertPriority(CampAlertPresentation left, CampAlertPresentation right)
        {
            if (ReferenceEquals(left, right)) return 0;
            if (left == null) return -1;
            if (right == null) return 1;
            var priority = left.Priority.CompareTo(right.Priority);
            if (priority != 0) return priority;
            var severity = left.Severity.CompareTo(right.Severity);
            if (severity != 0) return severity;
            return string.Compare(left.Id, right.Id, StringComparison.Ordinal);
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

        private static int CalculateSurvivorPower(SurvivorState survivor)
        {
            if (survivor == null) return 0;

            var skills = 0;
            foreach (var skill in survivor.Skills)
            {
                skills += Math.Max(0, skill.Value);
            }

            return Math.Max(0, survivor.Level) * 100 +
                   Math.Max(0, survivor.Health) +
                   Math.Max(0, survivor.Morale) +
                   skills * 30;
        }

        private static float CalculateHealthValue(SurvivorState survivor)
        {
            if (survivor == null || survivor.MaxHealth <= 0) return 0f;
            return Clamp01(survivor.Health / (float)survivor.MaxHealth);
        }

        private static float CalculateFatigueValue(SurvivorState survivor)
        {
            return survivor == null ? 0f : Clamp01(survivor.Fatigue / 100f);
        }

        private static float CalculateMoraleValue(SurvivorState survivor)
        {
            return survivor == null ? 0f : Clamp01(survivor.Morale / 100f);
        }

        private static string FormatSurvivorHealth(SurvivorState survivor)
        {
            if (survivor == null) return "0 / 0";
            return Math.Max(0, survivor.Health).ToString(CultureInfo.InvariantCulture) + " / " +
                   Math.Max(0, survivor.MaxHealth).ToString(CultureInfo.InvariantCulture);
        }

        private static string FormatSurvivorFatigue(SurvivorState survivor)
        {
            return survivor == null
                ? "0 / 100"
                : Math.Max(0, survivor.Fatigue).ToString(CultureInfo.InvariantCulture) + " / 100";
        }

        private static string FormatSurvivorMorale(SurvivorState survivor)
        {
            return survivor == null
                ? "0 / 100"
                : Math.Max(0, survivor.Morale).ToString(CultureInfo.InvariantCulture) + " / 100";
        }

        private static float Clamp01(float value)
        {
            if (value <= 0f) return 0f;
            return value >= 1f ? 1f : value;
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
                names.Add(config.TryGetTrait(traitId, out trait) ? trait.Name : traitId);
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
            var itemName = config.TryGetItem(item.ItemId, out definition) ? definition.Name : item.ItemId;
            return Format(catalog.SurvivorDetailWeaponFormat, itemName, item.Durability, item.MaxDurability);
        }

        private static string FormatTreatment(SurvivorState survivor, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (survivor.State == SurvivorActivityState.Resting)
            {
                return Format(
                    catalog.SurvivorDetailTreatmentFormat,
                    Format(catalog.SurvivorDetailRestingLabelFormat, Math.Max(0, config.Balance.RestFatigueRecoveryPerMinute)));
            }

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

        private static void ApplySurvivorDetailAction(CampSurvivorDetailPresentation presentation, SurvivorState survivor, GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (presentation == null || survivor == null || state == null || config == null || catalog == null) return;

            if (survivor.State == SurvivorActivityState.Wounded)
            {
                presentation.ActionKind = SurvivorDetailActionKind.UseMedicine;
                presentation.ActionCost = presentation.MedicineCost;
                presentation.ActionButton = catalog.SurvivorDetailUseMedicineButton;
                presentation.ShowAction = true;
                presentation.CanUseAction = HealingSystem.ValidateUseMedicine(state, config, new UseMedicineRequest { SurvivorId = survivor.Id }).IsValid;
            }
            else if (survivor.State == SurvivorActivityState.Resting)
            {
                presentation.ActionKind = SurvivorDetailActionKind.StopRest;
                presentation.ActionButton = catalog.SurvivorDetailStopRestButton;
                presentation.ShowAction = true;
                presentation.CanUseAction = RestSystem.ValidateStopRest(state, new StopRestRequest { SurvivorId = survivor.Id }).IsValid;
            }
            else if (survivor.Fatigue > 0)
            {
                presentation.ActionKind = SurvivorDetailActionKind.StartRest;
                presentation.ActionButton = catalog.SurvivorDetailStartRestButton;
                presentation.ShowAction = true;
                presentation.CanUseAction = RestSystem.ValidateStartRest(state, config, new StartRestRequest { SurvivorId = survivor.Id }).IsValid;
            }

            presentation.MedicineButton = presentation.ActionButton;
            presentation.ShowMedicineAction = presentation.ActionKind == SurvivorDetailActionKind.UseMedicine;
            presentation.CanUseMedicine = presentation.ActionKind == SurvivorDetailActionKind.UseMedicine && presentation.CanUseAction;
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
                canRecruit,
                ResolveRecruitablePortrait(candidate, catalog));
        }

        private static string GetBackgroundName(string backgroundId, GameConfigSnapshot config)
        {
            BackgroundDefinition background;
            return config.TryGetBackground(backgroundId, out background) ? background.Name : backgroundId;
        }

        private static string GetItemName(string itemId, GameConfigSnapshot config)
        {
            ItemDefinition item;
            return config.TryGetItem(itemId, out item) ? item.Name : itemId;
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
                if (config.TryGetZone(id, out zone)) return FormatConfiguredName(id, zone.Name);
            }

            if (config != null && type == GameConditionTypes.BuildingLevel)
            {
                BuildingDefinition building;
                if (config.TryGetBuilding(id, out building)) return FormatConfiguredName(id, building.Name);
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
            if (!config.TryGetZone(zoneId, out zone)) return;

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
            config.TryGetZone(expedition.ZoneId, out zone);
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
            ZoneDefinition selectedZone;
            if (!string.IsNullOrWhiteSpace(selectedZoneId) && config.TryGetZone(selectedZoneId, out selectedZone))
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
            ZoneDefinition zone;
            if (!config.TryGetZone(expedition.ZoneId, out zone)) return null;
            if (expedition.SurvivorIds.Count == 0) return null;

            var policyId = ResolvePolicyId(config, expedition.PolicyId);
            if (string.IsNullOrWhiteSpace(policyId)) return null;

            return new ExpeditionLaunchViewRequest(expedition.ZoneId, policyId, expedition.SurvivorIds, true);
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
            if (!string.IsNullOrWhiteSpace(policyId) && config.TryGetPolicy(policyId, out policy))
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
                var name = config.TryGetResource(loot.ResourceId, out definition) ? definition.Name : loot.ResourceId;
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
                names.Add(config.TryGetEnemy(entry.Id, out definition) ? definition.Name : entry.Id);
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
                    var name = config.TryGetZone(condition.Id, out requiredZone) ? requiredZone.Name : condition.Id;
                    entries.Add(Format(catalog.ReportCountFormat, name, condition.Value));
                }
                else if (condition.Type == GameConditionTypes.BuildingLevel)
                {
                    BuildingDefinition building;
                    var name = config.TryGetBuilding(condition.Id, out building) ? building.Name : condition.Id;
                    entries.Add(Format(catalog.ExpeditionUnlockRequirementFormat, Format(catalog.LevelLabelFormat, condition.Value), name));
                }
            }

            foreach (var requirement in zone.RequiredBuildingLevels)
            {
                BuildingDefinition building;
                var name = config.TryGetBuilding(requirement.Key, out building) ? building.Name : requirement.Key;
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
                var name = config.TryGetResource(pair.Key, out definition) ? definition.Name : pair.Key;
                entries.Add(Format(catalog.ReportCountFormat, name, pair.Value));
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatOfflineResourceChanges(OfflineProgressReport offline, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (offline == null) return catalog.ReportNoneLabel;
            var entries = new List<string>();
            var gained = FormatResourceAmounts(offline.ResourcesGained, config, catalog);
            if (!string.Equals(gained, catalog.ReportNoneLabel, StringComparison.Ordinal))
            {
                entries.Add(gained);
            }

            var spent = FormatResourceAmounts(offline.ResourcesSpent, config, catalog);
            if (!string.Equals(spent, catalog.ReportNoneLabel, StringComparison.Ordinal) && !string.IsNullOrWhiteSpace(catalog.OfflineReportResourcesSpentFormat))
            {
                entries.Add(Format(catalog.OfflineReportResourcesSpentFormat, spent));
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
                var name = config.TryGetEnemy(pair.Key, out definition) ? definition.Name : pair.Key;
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
                names.Add(config.TryGetZone(expedition.ZoneId, out zone) ? zone.Name : expedition.ZoneId);
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

        private static string FormatAfterActionDetails(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog, ExpeditionState expedition, ZoneDefinition zone)
        {
            var entries = new List<string>();
            AddFormattedReportDetail(entries, catalog.AfterActionSkillXpFormat, FormatExpeditionSkillXp(expedition, config, catalog), catalog);
            AddFormattedReportDetail(entries, catalog.AfterActionDurabilityFormat, FormatExpeditionDurabilityLoss(state, expedition, config, catalog), catalog);
            AddFormattedReportDetail(entries, catalog.AfterActionBrokenItemsFormat, FormatBrokenItems(state, expedition, config, catalog), catalog);
            AddFormattedReportDetail(entries, catalog.AfterActionProgressFormat, FormatAfterActionProgress(state, config, catalog, expedition, zone), catalog);

            var recentEvents = FormatRecentEvents(expedition, catalog);
            if (!string.Equals(recentEvents, catalog.ReportNoneLabel, StringComparison.Ordinal))
            {
                entries.Add(recentEvents);
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static void AddFormattedReportDetail(List<string> entries, string format, string value, CampUiCatalogSO catalog)
        {
            if (entries == null || string.IsNullOrWhiteSpace(format) || string.Equals(value, catalog.ReportNoneLabel, StringComparison.Ordinal)) return;
            entries.Add(Format(format, value));
        }

        private static string FormatExpeditionSkillXp(ExpeditionState expedition, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (expedition == null || expedition.Status != ExpeditionStatus.Completed || config == null || config.Balance == null) return catalog.ReportNoneLabel;
            var amount = Math.Max(0, config.Balance.ExpeditionCompletionSkillXp) * Math.Max(0, expedition.SurvivorIds.Count);
            if (amount <= 0 || string.IsNullOrWhiteSpace(config.Balance.ExpeditionCompletionSkillId)) return catalog.ReportNoneLabel;
            return Format(catalog.ReportCountFormat, GetSkillLabel(config.Balance.ExpeditionCompletionSkillId, catalog), amount);
        }

        private static string FormatExpeditionDurabilityLoss(GameState state, ExpeditionState expedition, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (expedition == null || expedition.EquipmentDurabilityLost == null || expedition.EquipmentDurabilityLost.Count == 0) return catalog.ReportNoneLabel;

            var entries = new List<string>();
            foreach (var pair in expedition.EquipmentDurabilityLost)
            {
                if (pair.Value <= 0) continue;
                entries.Add(Format(catalog.AfterActionDurabilityItemFormat, GetItemNameForUid(state, config, pair.Key), pair.Value));
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatBrokenItems(GameState state, ExpeditionState expedition, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            if (expedition == null || expedition.BrokenItemUids == null || expedition.BrokenItemUids.Count == 0) return catalog.ReportNoneLabel;

            var entries = new List<string>();
            foreach (var itemUid in expedition.BrokenItemUids)
            {
                var name = GetItemNameForUid(state, config, itemUid);
                if (!string.IsNullOrWhiteSpace(name)) entries.Add(name);
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static string FormatAfterActionProgress(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog, ExpeditionState expedition, ZoneDefinition zone)
        {
            if (state == null || config == null || expedition == null || expedition.Status != ExpeditionStatus.Completed) return catalog.ReportNoneLabel;

            var entries = new List<string>();
            ZoneState zoneState;
            if (zone != null && state.Zones.TryGetValue(expedition.ZoneId, out zoneState) && zoneState.Completions > 0)
            {
                entries.Add(Format(catalog.ReportCountFormat, FormatConfiguredName(zone.Id, zone.Name), zoneState.Completions));
            }

            foreach (var candidate in config.Zones.Values)
            {
                ZoneState candidateState;
                if (!state.Zones.TryGetValue(candidate.Id, out candidateState) || !candidateState.IsUnlocked) continue;
                if (WasUnlockedByCompletedExpedition(state, candidate, expedition))
                {
                    entries.Add(Format(catalog.AfterActionUnlockedFormat, FormatConfiguredName(candidate.Id, candidate.Name)));
                }
            }

            if (state.Progress != null && state.Progress.DemoCompleted)
            {
                var expectedCompletionId = GameConditionTypes.ZoneCompletions + ":" + expedition.ZoneId;
                if (string.Equals(state.Progress.DemoCompletionId, expectedCompletionId, StringComparison.Ordinal))
                {
                    var completionName = GetDemoCompletionName(config, new CampEventState { SubjectId = state.Progress.DemoCompletionId, DetailId = state.Progress.DemoCompletionId });
                    entries.Add(Format(catalog.AfterActionDemoProgressFormat, completionName));
                }
            }

            return entries.Count > 0 ? string.Join(catalog.ReportListSeparator, entries.ToArray()) : catalog.ReportNoneLabel;
        }

        private static bool WasUnlockedByCompletedExpedition(GameState state, ZoneDefinition zone, ExpeditionState expedition)
        {
            if (state == null || zone == null || expedition == null) return false;
            foreach (var condition in zone.UnlockConditions)
            {
                if (!string.Equals(condition.Type, GameConditionTypes.ZoneCompletions, StringComparison.Ordinal)) continue;
                if (!string.Equals(condition.Id, expedition.ZoneId, StringComparison.Ordinal)) continue;

                ZoneState requiredZone;
                if (state.Zones.TryGetValue(condition.Id, out requiredZone) && requiredZone.Completions == Math.Max(1, condition.Value))
                {
                    return true;
                }
            }

            return false;
        }

        private static string GetItemNameForUid(GameState state, GameConfigSnapshot config, string itemUid)
        {
            var item = WorkshopSystem.FindItem(state, itemUid);
            if (item == null) return itemUid ?? string.Empty;
            return GetItemName(item.ItemId, config);
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
                var name = config.TryGetItem(pair.Key, out definition) ? definition.Name : pair.Key;
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

            var completionXp = config != null && config.Balance != null ? Math.Max(0, config.Balance.ExpeditionCompletionXp) : 0;
            var xp = expedition.Status == ExpeditionStatus.Completed ? expedition.SurvivorIds.Count * completionXp : 0;
            foreach (var pair in expedition.EnemiesDefeated)
            {
                EnemyDefinition enemy;
                if (config.TryGetEnemy(pair.Key, out enemy))
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

        private static Texture2D ResolveSurvivorPortrait(SurvivorState survivor, CampUiCatalogSO catalog)
        {
            if (survivor == null || catalog == null) return null;

            var portrait = FindSurvivorPortrait(catalog, survivor.Id);
            if (portrait != null) return portrait;

            portrait = FindSurvivorPortrait(catalog, survivor.BackgroundId);
            if (portrait != null) return portrait;

            return FindSurvivorPortrait(catalog, catalog.DefaultSurvivorPortraitId);
        }

        private static Texture2D ResolveRecruitablePortrait(RecruitableSurvivorDefinition candidate, CampUiCatalogSO catalog)
        {
            if (candidate == null || catalog == null) return null;

            var portrait = FindSurvivorPortrait(catalog, candidate.Id);
            if (portrait != null) return portrait;

            portrait = FindSurvivorPortrait(catalog, candidate.BackgroundId);
            if (portrait != null) return portrait;

            return FindSurvivorPortrait(catalog, catalog.DefaultSurvivorPortraitId);
        }

        private static Texture2D FindSurvivorPortrait(CampUiCatalogSO catalog, string id)
        {
            if (catalog == null || catalog.SurvivorPortraits == null || string.IsNullOrWhiteSpace(id)) return null;
            foreach (var entry in catalog.SurvivorPortraits)
            {
                if (entry != null && string.Equals(entry.Id, id, StringComparison.Ordinal))
                {
                    return entry.Portrait;
                }
            }

            return null;
        }

        private static Texture2D FindExpeditionZoneThumbnail(CampUiCatalogSO catalog, string zoneId)
        {
            if (catalog == null || catalog.ExpeditionZoneArtwork == null || string.IsNullOrWhiteSpace(zoneId)) return null;
            foreach (var entry in catalog.ExpeditionZoneArtwork)
            {
                if (entry != null && string.Equals(entry.ZoneId, zoneId, StringComparison.Ordinal))
                {
                    return entry.Thumbnail;
                }
            }

            return null;
        }

        private static Texture2D FindExpeditionRiskBadge(CampUiCatalogSO catalog, string riskTier)
        {
            if (catalog == null || catalog.ExpeditionRiskArtwork == null || string.IsNullOrWhiteSpace(riskTier)) return null;
            foreach (var entry in catalog.ExpeditionRiskArtwork)
            {
                if (entry != null && string.Equals(entry.RiskTier, riskTier, StringComparison.Ordinal))
                {
                    return entry.Badge;
                }
            }

            return null;
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
                if (!config.TryGetResource(reward.Key, out resource) || !resource.HasCap) continue;

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
        public readonly string LevelText;
        public readonly string PowerText;
        public readonly float HealthValue;
        public readonly float FatigueValue;
        public readonly string HealthText;
        public readonly string FatigueText;
        public readonly Texture2D Portrait;

        public CampSurvivorCardPresentation(string survivorId, string name, string avatar, string state, string skill)
            : this(survivorId, name, avatar, state, skill, string.Empty, string.Empty, 0f, 0f, string.Empty, string.Empty, null)
        {
        }

        public CampSurvivorCardPresentation(string survivorId, string name, string avatar, string state, string skill, Texture2D portrait)
            : this(survivorId, name, avatar, state, skill, string.Empty, string.Empty, 0f, 0f, string.Empty, string.Empty, portrait)
        {
        }

        public CampSurvivorCardPresentation(
            string survivorId,
            string name,
            string avatar,
            string state,
            string skill,
            string levelText,
            string powerText,
            float healthValue,
            float fatigueValue,
            string healthText,
            string fatigueText,
            Texture2D portrait)
        {
            SurvivorId = survivorId ?? string.Empty;
            Name = name ?? string.Empty;
            Avatar = avatar ?? string.Empty;
            State = state ?? string.Empty;
            Skill = skill ?? string.Empty;
            LevelText = levelText ?? string.Empty;
            PowerText = powerText ?? string.Empty;
            HealthValue = healthValue;
            FatigueValue = fatigueValue;
            HealthText = healthText ?? string.Empty;
            FatigueText = fatigueText ?? string.Empty;
            Portrait = portrait;
        }
    }

    public enum SurvivorDetailActionKind
    {
        None,
        UseMedicine,
        StartRest,
        StopRest
    }

    public sealed class CampSurvivorDetailPresentation
    {
        public string Title = string.Empty;
        public string Background = string.Empty;
        public string Traits = string.Empty;
        public string Weapon = string.Empty;
        public string Treatment = string.Empty;
        public string Stats = string.Empty;
        public string LevelText = string.Empty;
        public string XpText = string.Empty;
        public string StatusText = string.Empty;
        public float HealthValue;
        public float FatigueValue;
        public float MoraleValue;
        public string HealthText = string.Empty;
        public string FatigueText = string.Empty;
        public string MoraleText = string.Empty;
        public string MedicineCost = string.Empty;
        public string MedicineButton = string.Empty;
        public string ActionCost = string.Empty;
        public string ActionButton = string.Empty;
        public Texture2D Portrait;
        public SurvivorDetailActionKind ActionKind;
        public bool ShowAction;
        public bool CanUseAction;
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
        public string ManualSaveTitle = string.Empty;
        public string ManualSaveBody = string.Empty;
        public string ManualSaveButton = string.Empty;
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
        public readonly Texture2D Portrait;

        public CampRadioCandidatePresentation(string candidateId, string name, string avatar, string meta, string skill, string traits, string recruitButton, bool canRecruit)
            : this(candidateId, name, avatar, meta, skill, traits, recruitButton, canRecruit, null)
        {
        }

        public CampRadioCandidatePresentation(string candidateId, string name, string avatar, string meta, string skill, string traits, string recruitButton, bool canRecruit, Texture2D portrait)
        {
            CandidateId = candidateId ?? string.Empty;
            Name = name ?? string.Empty;
            Avatar = avatar ?? string.Empty;
            Meta = meta ?? string.Empty;
            Skill = skill ?? string.Empty;
            Traits = traits ?? string.Empty;
            RecruitButton = recruitButton ?? string.Empty;
            CanRecruit = canRecruit;
            Portrait = portrait;
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
        public readonly string RiskTier;
        public readonly Texture2D Thumbnail;
        public readonly Texture2D RiskBadge;

        public CampExpeditionRoutePresentation(string zoneId, string title, string subtitle, string status, bool canSelect, bool canLaunch, bool isSelected)
            : this(zoneId, title, subtitle, status, canSelect, canLaunch, isSelected, string.Empty, null, null)
        {
        }

        public CampExpeditionRoutePresentation(
            string zoneId,
            string title,
            string subtitle,
            string status,
            bool canSelect,
            bool canLaunch,
            bool isSelected,
            string riskTier,
            Texture2D thumbnail,
            Texture2D riskBadge)
        {
            ZoneId = zoneId ?? string.Empty;
            Title = title ?? string.Empty;
            Subtitle = subtitle ?? string.Empty;
            Status = status ?? string.Empty;
            CanSelect = canSelect;
            CanLaunch = canLaunch;
            IsSelected = isSelected;
            RiskTier = riskTier ?? string.Empty;
            Thumbnail = thumbnail;
            RiskBadge = riskBadge;
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
        public readonly string Id;
        public readonly string Title;
        public readonly string Body;
        public readonly Color ToneColor;
        public readonly CampAlertSeverity Severity;
        public readonly int Priority;
        public readonly string Category;
        public readonly CampAlertAction Action;
        public readonly string TargetScreenId;
        public readonly string ActionLabel;
        public readonly CampAlertButtonView ButtonView;
        public readonly Texture2D Icon;
        public readonly Texture2D ActionIcon;
        public readonly bool CanInvokeAction;

        public bool HasAction
        {
            get
            {
                return CanInvokeAction &&
                       Action != CampAlertAction.None &&
                       ButtonView != CampAlertButtonView.Hidden &&
                       !string.IsNullOrWhiteSpace(ActionLabel);
            }
        }

        public bool CanStartEmergencyScavenge
        {
            get { return HasAction && Action == CampAlertAction.StartEmergencyScavenge; }
        }

        public CampAlertPresentation(string title, string body, Color toneColor)
            : this(string.Empty, title, body, toneColor, CampAlertSeverity.Info, 0, string.Empty, CampAlertAction.None, string.Empty, string.Empty, CampAlertButtonView.Hidden, null, null, false)
        {
        }

        public CampAlertPresentation(string title, string body, Color toneColor, string actionLabel, bool canStartEmergencyScavenge)
            : this(
                string.Empty,
                title,
                body,
                toneColor,
                canStartEmergencyScavenge ? CampAlertSeverity.Critical : CampAlertSeverity.Info,
                0,
                string.Empty,
                canStartEmergencyScavenge ? CampAlertAction.StartEmergencyScavenge : CampAlertAction.None,
                string.Empty,
                actionLabel,
                string.IsNullOrWhiteSpace(actionLabel) ? CampAlertButtonView.Hidden : CampAlertButtonView.Text,
                null,
                null,
                canStartEmergencyScavenge)
        {
        }

        public CampAlertPresentation(
            string id,
            string title,
            string body,
            Color toneColor,
            CampAlertSeverity severity,
            int priority,
            string category,
            CampAlertAction action,
            string targetScreenId,
            string actionLabel,
            CampAlertButtonView buttonView,
            Texture2D icon,
            Texture2D actionIcon,
            bool canInvokeAction)
        {
            Id = id ?? string.Empty;
            Title = title ?? string.Empty;
            Body = body ?? string.Empty;
            ToneColor = toneColor;
            Severity = severity;
            Priority = priority;
            Category = category ?? string.Empty;
            Action = action;
            TargetScreenId = targetScreenId ?? string.Empty;
            ActionLabel = actionLabel ?? string.Empty;
            ButtonView = buttonView;
            Icon = icon;
            ActionIcon = actionIcon;
            CanInvokeAction = canInvokeAction;
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
