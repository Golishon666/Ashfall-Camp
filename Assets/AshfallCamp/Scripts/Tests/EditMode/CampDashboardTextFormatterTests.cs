using System.Collections.Generic;
using AshfallCamp.Domain;
using AshfallCamp.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class CampDashboardTextFormatterTests
    {
        [Test]
        public void DashboardCopyComesFromStateAndCatalogTemplates()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();

            state.Resources["food"] = 1;
            state.Resources["scrap"] = 100;
            state.Survivors[0].State = SurvivorActivityState.Wounded;

            var alerts = CampDashboardTextFormatter.BuildAlerts(state, config, catalog);

            Assert.AreEqual("WOUNDED 1", alerts[0].Title);
            Assert.AreEqual("Mara needs treatment", alerts[0].Body);
            Assert.AreEqual("LOW Food", alerts[1].Title);
            Assert.AreEqual("1/50", alerts[1].Body);
            Assert.AreEqual("UP workshop", alerts[2].Title);
            Assert.AreEqual("Level 1", alerts[2].Body);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ExpeditionCardsPreferActiveRunsThenUnlockedRoutes()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();

            var routes = CampDashboardTextFormatter.BuildExpeditionCards(state, config, catalog);
            Assert.AreEqual("abandoned_store", routes[0].Title);
            Assert.AreEqual("Safe:1:0", routes[0].Subtitle);
            Assert.AreEqual("0%", routes[0].Status);
            Assert.AreEqual("abandoned_store", routes[0].ZoneId);
            Assert.AreEqual("balanced", routes[0].PolicyId);
            Assert.IsTrue(routes[0].CanLaunch);

            var launched = ExpeditionLauncher.Launch(state, config, new LaunchExpeditionRequest
            {
                ZoneId = "abandoned_store",
                PolicyId = "balanced",
                Seed = 123,
                NowUnixMs = 0,
                ConfirmWarnings = true,
                SurvivorIds = new List<string> { "survivor_1" }
            });
            launched.Expedition.Progress = 42;
            launched.Expedition.ElapsedSeconds = 12;
            launched.Expedition.ExpectedDurationSeconds = 132;

            var active = CampDashboardTextFormatter.BuildExpeditionCards(state, config, catalog);

            Assert.AreEqual("abandoned_store", active[0].Title);
            Assert.AreEqual("1:Balanced:2", active[0].Subtitle);
            Assert.AreEqual("42%", active[0].Status);
            Assert.IsFalse(active[0].CanLaunch);
            Assert.AreEqual("1/3/0", CampDashboardTextFormatter.FormatRadioBody(state, config, catalog));

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ExpeditionScreenShowsRoutesDetailAndActiveMonitor()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();

            var screen = CampDashboardTextFormatter.BuildExpeditionScreen(state, config, catalog, string.Empty);

            Assert.AreEqual("EXPEDITIONS", screen.Title);
            Assert.AreEqual("abandoned_store", screen.SelectedZoneId);
            Assert.AreEqual(3, screen.Routes.Count);
            Assert.AreEqual("abandoned_store", screen.Routes[0].ZoneId);
            Assert.IsTrue(screen.Routes[0].CanSelect);
            Assert.IsTrue(screen.Routes[0].CanLaunch);
            Assert.IsTrue(screen.Routes[0].IsSelected);
            Assert.AreEqual("dry_suburb", screen.Routes[1].ZoneId);
            Assert.IsFalse(screen.Routes[1].CanSelect);
            Assert.That(screen.Routes[1].Status, Does.Contain("Locked"));
            Assert.AreEqual("abandoned_store * Balanced", screen.Selected.Title);
            Assert.AreEqual("Duration 1 Cost 1/0 Power", screen.Selected.Details.Substring(0, 25));
            Assert.AreEqual("Loot Scrap 4-10, Food 1-4", screen.Selected.Loot);
            Assert.AreEqual("Threats Feral Dog", screen.Selected.Enemies);
            Assert.AreEqual("Warnings None", screen.Selected.Warnings);
            Assert.AreEqual("LAUNCH", screen.Selected.LaunchButton);
            Assert.IsTrue(screen.Selected.CanLaunch);
            Assert.IsFalse(screen.Monitor.HasActiveExpedition);

            var launched = ExpeditionLauncher.Launch(state, config, new LaunchExpeditionRequest
            {
                ZoneId = "abandoned_store",
                PolicyId = "balanced",
                Seed = 123,
                NowUnixMs = 1000,
                ConfirmWarnings = true,
                SurvivorIds = new List<string> { "survivor_1" }
            });
            launched.Expedition.Progress = 42;
            launched.Expedition.ElapsedSeconds = 12;
            launched.Expedition.ExpectedDurationSeconds = 132;
            launched.Expedition.Noise = 3;
            launched.Expedition.AccumulatedLoot["scrap"] = 8;
            launched.Expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = 4, Message = "First event" });
            launched.Expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = 8, Message = "Second event" });

            var active = CampDashboardTextFormatter.BuildExpeditionScreen(state, config, catalog, "abandoned_store");

            Assert.IsTrue(active.Monitor.HasActiveExpedition);
            Assert.AreEqual("ACTIVE", active.Monitor.Title);
            Assert.AreEqual("abandoned_store/1/2", active.Monitor.Header);
            Assert.AreEqual("Progress 42", active.Monitor.Progress);
            Assert.AreEqual("Loot Scrap x8", active.Monitor.Loot);
            Assert.AreEqual("Noise 3", active.Monitor.Noise);
            Assert.AreEqual("Log First event, Second event", active.Monitor.Log);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void RadioScreenShowsBroadcastCostStatusAndCandidates()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            BuildingSystem.Upgrade(state, config, "barracks");

            var radio = CampDashboardTextFormatter.BuildRadioScreen(state, config, catalog);

            Assert.AreEqual("RADIO", radio.Title);
            Assert.AreEqual("Intel", radio.IntelTitle);
            Assert.AreEqual("1/3/0", radio.IntelBody);
            Assert.AreEqual("Broadcast", radio.BroadcastTitle);
            Assert.AreEqual("Cost Scrap x20, Food x2, Water x2", radio.BroadcastCost);
            Assert.AreEqual("Ready", radio.BroadcastStatus);
            Assert.AreEqual("Broadcast", radio.BroadcastButton);
            Assert.IsTrue(radio.CanBroadcast);
            Assert.AreEqual("Signals", radio.CandidateListTitle);
            Assert.AreEqual(1, radio.Candidates.Count);
            Assert.AreEqual("elias", radio.Candidates[0].CandidateId);
            Assert.AreEqual("Elias", radio.Candidates[0].Name);
            Assert.AreEqual("E", radio.Candidates[0].Avatar);
            Assert.AreEqual("Scavenger/Rusty Knife", radio.Candidates[0].Meta);
            Assert.AreEqual("scavenging 1", radio.Candidates[0].Skill);
            Assert.AreEqual("Traits Careful", radio.Candidates[0].Traits);

            RecruitmentSystem.Recruit(state, config, new RecruitSurvivorRequest { Seed = 1 });
            var afterRecruit = CampDashboardTextFormatter.BuildRadioScreen(state, config, catalog);

            Assert.AreEqual(0, afterRecruit.Candidates.Count);
            Assert.IsFalse(afterRecruit.CanBroadcast);
            Assert.That(afterRecruit.BroadcastStatus, Does.Contain("Blocked"));

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void StatusMetersAreCalculatedFromGameState()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            catalog.CampStatusHealthyLabel = "OK";
            catalog.CampStatusStrainedLabel = "BAD";
            catalog.CampStatusBodyFormat = "{0}:{1}:{2}:{3}:{4}";
            catalog.CampStatusBadgeFormat = "{0}/{1}/{2}/{3}";
            catalog.MoraleLabel = "M";
            catalog.SafetyLabel = "S";
            catalog.SuppliesLabel = "P";
            catalog.MoraleValueFormat = "{0}%";
            catalog.SafetyValueFormat = "{0}%";
            catalog.SuppliesValueFormat = "{0}%";

            state.Survivors[0].Morale = 40;
            state.Survivors[0].State = SurvivorActivityState.Wounded;
            state.Resources["food"] = 25;
            state.Resources["water"] = 20;
            state.Resources["medicine"] = 5;

            var status = CampDashboardTextFormatter.BuildStatus(state, config, catalog);

            Assert.AreEqual("BAD", status.Label);
            Assert.AreEqual("0:1:0:25:40", status.Body);
            Assert.AreEqual("0/1/0/25", status.Badge);
            Assert.AreEqual("M", status.MoraleLabel);
            Assert.AreEqual("S", status.SafetyLabel);
            Assert.AreEqual("P", status.SuppliesLabel);
            Assert.AreEqual("40%", status.MoraleValue);
            Assert.AreEqual("75%", status.SafetyValue);
            Assert.AreEqual("25%", status.SuppliesValue);
            Assert.AreEqual(40, status.MoralePercent);
            Assert.AreEqual(75, status.SafetyPercent);
            Assert.AreEqual(25, status.SuppliesPercent);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ReportsSummarizeAfterActionAndOfflineProgress()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            var launched = ExpeditionLauncher.Launch(state, config, new LaunchExpeditionRequest
            {
                ZoneId = "abandoned_store",
                PolicyId = "balanced",
                Seed = 123,
                NowUnixMs = 1000,
                ConfirmWarnings = true,
                SurvivorIds = new List<string> { "survivor_1" }
            });
            launched.Expedition.ElapsedSeconds = 125;
            launched.Expedition.AccumulatedLoot["scrap"] = 8;
            launched.Expedition.EnemiesDefeated["feral_dog"] = 1;
            launched.Expedition.WoundedSurvivorIds.Add("survivor_1");
            launched.Expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = 12, Message = "Mara opened a cache." });
            ExpeditionSimulator.Complete(state, config, launched.Expedition);
            state.LastOfflineReport = new OfflineProgressReport { AppliedSeconds = 120 };
            state.LastOfflineReport.ResourcesGained["food"] = 3;
            state.LastOfflineReport.CompletedExpeditionIds.Add(launched.Expedition.Id);
            state.LastOfflineReport.WoundedSurvivorIds.Add("survivor_1");
            state.LastOfflineReport.HealedSurvivorIds.Add("survivor_1");

            var reports = CampDashboardTextFormatter.BuildReports(state, config, catalog);

            Assert.IsTrue(reports.HasAfterAction);
            Assert.IsTrue(reports.HasOfflineReport);
            Assert.AreEqual("abandoned_store/SUCCESS/3", reports.AfterActionOutcome);
            Assert.AreEqual("Loot Scrap x8", reports.AfterActionLoot);
            Assert.AreEqual("XP 9", reports.AfterActionXp);
            Assert.AreEqual("Wounds Mara", reports.AfterActionWounds);
            Assert.AreEqual("Enemies Feral Dog x1", reports.AfterActionEnemies);
            Assert.That(reports.AfterActionEvents, Does.Contain("Mara opened a cache."));
            Assert.AreEqual("Offline 2", reports.OfflineSummary);
            Assert.AreEqual("Resources Food x3", reports.OfflineResources);
            Assert.AreEqual("Completed abandoned_store", reports.OfflineCompleted);
            Assert.AreEqual("Healing Mara", reports.OfflineHealing);
            Assert.AreEqual("Warnings Mara", reports.OfflineWarnings);

            Object.DestroyImmediate(catalog);
        }

        private static CampUiCatalogSO CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();
            catalog.LowResourceAlertPercentThreshold = 25;
            catalog.StatusWarningPercentThreshold = 55;
            catalog.LowResourceAlertTitleFormat = "LOW {0}";
            catalog.LowResourceAlertBodyFormat = "{0}/{1}";
            catalog.WoundedAlertTitleFormat = "WOUNDED {0}";
            catalog.WoundedAlertBodyFormat = "{0} needs treatment";
            catalog.UpgradeAvailableAlertTitleFormat = "UP {0}";
            catalog.UpgradeAvailableAlertBodyFormat = "Level {0}";
            catalog.ExpeditionActiveSubtitleFormat = "{0}:{1}:{2}";
            catalog.ExpeditionActiveStatusFormat = "{0}%";
            catalog.ExpeditionRouteSubtitleFormat = "{0}:{1}:{2}";
            catalog.ExpeditionRouteStatusFormat = "{0}%";
            catalog.ExpeditionScreenTitle = "EXPEDITIONS";
            catalog.ExpeditionSelectedTitleFormat = "{0} * {1}";
            catalog.ExpeditionSelectedDetailsFormat = "Duration {0} Cost {1}/{2} Power {3}/{4} Familiarity {5}";
            catalog.ExpeditionSelectedLootFormat = "Loot {0}";
            catalog.ExpeditionLootRangeFormat = "{0} {1}-{2}";
            catalog.ExpeditionSelectedEnemiesFormat = "Threats {0}";
            catalog.ExpeditionSelectedWarningsFormat = "Warnings {0}";
            catalog.ExpeditionLaunchButton = "LAUNCH";
            catalog.ExpeditionLaunchBlockedButton = "BLOCKED";
            catalog.ExpeditionMonitorTitle = "ACTIVE";
            catalog.ExpeditionMonitorHeaderFormat = "{0}/{1}/{2}";
            catalog.ExpeditionMonitorProgressFormat = "Progress {0}";
            catalog.ExpeditionMonitorLootFormat = "Loot {0}";
            catalog.ExpeditionMonitorNoiseFormat = "Noise {0}";
            catalog.ExpeditionMonitorLogFormat = "Log {0}";
            catalog.ExpeditionLockedStatusFormat = "Locked: {0}";
            catalog.ExpeditionUnlockRequirementFormat = "{0} {1}";
            catalog.ExpeditionNoWarningsLabel = "None";
            catalog.ExpeditionMonitorLogLineCount = 3;
            catalog.DefaultExpeditionPolicyId = "balanced";
            catalog.RadioIntelBodyFormat = "{0}/{1}/{2}";
            catalog.RadioIntelTitle = "Intel";
            catalog.RadioIntelButton = "Broadcast";
            catalog.RadioScreenTitle = "RADIO";
            catalog.RadioBroadcastTitle = "Broadcast";
            catalog.RadioBroadcastCostFormat = "Cost {0}";
            catalog.RadioBroadcastReadyLabel = "Ready";
            catalog.RadioBroadcastBlockedFormat = "Blocked {0}";
            catalog.RadioCandidateListTitle = "Signals";
            catalog.RadioCandidateEmptyTitle = "No signals";
            catalog.RadioCandidateEmptyBody = "Empty";
            catalog.RadioCandidateCardMetaFormat = "{0}/{1}";
            catalog.RadioCandidateSkillFormat = "{0} {1}";
            catalog.RadioCandidateTraitsFormat = "Traits {0}";
            catalog.ReportsScreenTitle = "Reports";
            catalog.ReportsEmptyTitle = "Empty";
            catalog.ReportsEmptyBody = "No reports";
            catalog.AfterActionPanelTitle = "After";
            catalog.AfterActionSuccessLabel = "SUCCESS";
            catalog.AfterActionFailureLabel = "FAIL";
            catalog.AfterActionOutcomeFormat = "{0}/{1}/{2}";
            catalog.AfterActionLootFormat = "Loot {0}";
            catalog.AfterActionXpFormat = "XP {0}";
            catalog.AfterActionWoundsFormat = "Wounds {0}";
            catalog.AfterActionEnemiesFormat = "Enemies {0}";
            catalog.AfterActionEventsFormat = "Events {0}";
            catalog.OfflineReportPanelTitle = "Offline";
            catalog.OfflineReportSummaryFormat = "Offline {0}";
            catalog.OfflineReportResourcesFormat = "Resources {0}";
            catalog.OfflineReportCompletedFormat = "Completed {0}";
            catalog.OfflineReportHealingFormat = "Healing {0}";
            catalog.OfflineReportWarningsFormat = "Warnings {0}";
            catalog.ReportNoneLabel = "None";
            catalog.ReportListSeparator = ", ";
            catalog.ReportCountFormat = "{0} x{1}";
            return catalog;
        }
    }
}
