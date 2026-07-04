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
            Assert.AreEqual("SCAVENGE", alerts[1].Title);
            Assert.AreEqual("60:Scrap x3, Food x2, Water x2", alerts[1].Body);
            Assert.AreEqual("Go", alerts[1].ActionLabel);
            Assert.IsTrue(alerts[1].CanStartEmergencyScavenge);
            Assert.AreEqual("LOW Food", alerts[2].Title);
            Assert.AreEqual("1/50", alerts[2].Body);
            Assert.AreEqual("UP workshop", alerts[3].Title);
            Assert.AreEqual("Level 1", alerts[3].Body);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void EmergencyScavengeCompletionAppearsInAlertsAndReports()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            state.Resources["food"] = 0;
            state.Resources["water"] = 0;

            RecoverySystem.StartEmergencyScavenge(state, config, new EmergencyScavengeRequest { NowUnixMs = 1000 });
            RecoverySystem.Tick(state, config, 60);

            var alerts = CampDashboardTextFormatter.BuildAlerts(state, config, catalog);
            var reports = CampDashboardTextFormatter.BuildReports(state, config, catalog);

            Assert.AreEqual("RECOVERED", alerts[0].Title);
            Assert.AreEqual("Recovered Scrap x3, Food x2, Water x2", alerts[0].Body);
            Assert.AreEqual("Recovered report", reports.CampEventTitle);
            Assert.AreEqual("Report Scrap x3, Food x2, Water x2", reports.CampEventBody);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SettingsScreenShowsAutosaveStateAndInterval()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();

            var enabled = CampDashboardTextFormatter.BuildSettings(state, config, catalog);
            state.Settings.AutosaveEnabled = false;
            var disabled = CampDashboardTextFormatter.BuildSettings(state, config, catalog);

            Assert.AreEqual("Settings", enabled.Title);
            Assert.AreEqual("Autosave", enabled.AutosaveTitle);
            Assert.AreEqual("Every 30s", enabled.AutosaveBody);
            Assert.AreEqual("On", enabled.AutosaveState);
            Assert.AreEqual("Autosave toggle", enabled.AutosaveToggleLabel);
            Assert.IsTrue(enabled.AutosaveEnabled);

            Assert.AreEqual("Off body", disabled.AutosaveBody);
            Assert.AreEqual("Off", disabled.AutosaveState);
            Assert.IsFalse(disabled.AutosaveEnabled);

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
            launched.Expedition.EnemiesDefeated["feral_dog"] = 2;
            launched.Expedition.WoundedSurvivorIds.Add("survivor_1");
            launched.Expedition.FoundItems.Add(new InventoryItemState { Uid = "found_1", ItemId = "rusty_knife", Level = 1, Durability = 40, MaxDurability = 80 });
            launched.Expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = 4, Message = "First event" });
            launched.Expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = 8, Message = "Second event" });

            var active = CampDashboardTextFormatter.BuildExpeditionScreen(state, config, catalog, "abandoned_store");

            Assert.IsTrue(active.Monitor.HasActiveExpedition);
            Assert.AreEqual("ACTIVE", active.Monitor.Title);
            Assert.AreEqual("abandoned_store/1/2", active.Monitor.Header);
            Assert.AreEqual("Progress 42", active.Monitor.Progress);
            Assert.AreEqual("Loot Scrap x8", active.Monitor.Loot);
            Assert.AreEqual("Noise MED/3", active.Monitor.Noise);
            Assert.AreEqual("Log Threat Feral Dog x2, Wounds Mara, Finds Rusty Knife x1, First event, Second event", active.Monitor.Log);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ExpeditionScreenRequiresRiskReviewBeforeWarningLaunch()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            config.Zones["abandoned_store"].RecommendedPower = 999;

            var review = CampDashboardTextFormatter.BuildExpeditionScreen(state, config, catalog, "abandoned_store", "balanced", null, false);
            var confirm = CampDashboardTextFormatter.BuildExpeditionScreen(state, config, catalog, "abandoned_store", "balanced", null, true);

            Assert.IsTrue(review.Selected.CanLaunch);
            Assert.IsTrue(review.Selected.RequiresRiskConfirmation);
            Assert.IsFalse(review.Selected.IsRiskConfirmationPending);
            Assert.AreEqual("REVIEW", review.Selected.LaunchButton);
            Assert.AreEqual("Warnings Extreme risk.", review.Selected.Warnings);

            Assert.IsTrue(confirm.Selected.RequiresRiskConfirmation);
            Assert.IsTrue(confirm.Selected.IsRiskConfirmationPending);
            Assert.AreEqual("CONFIRM", confirm.Selected.LaunchButton);
            Assert.AreEqual("Warnings Confirm Extreme risk.", confirm.Selected.Warnings);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ExpeditionScreenUsesExplicitSquadAndPolicySelection()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            state.SurvivorCap = 2;
            state.SquadSize = 2;
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            var recruited = RecruitWithBroadcast(state, config, "elias");
            Assert.IsTrue(recruited.Validation.IsValid);

            catalog.ExpeditionSelectedDetailsFormat = "{3}/{4} {6}";
            var selected = new List<string> { state.Survivors[0].Id, state.Survivors[1].Id };
            var screen = CampDashboardTextFormatter.BuildExpeditionScreen(state, config, catalog, "abandoned_store", "aggressive", selected);

            Assert.AreEqual("aggressive", screen.SelectedPolicyId);
            Assert.AreEqual("aggressive", screen.Selected.PolicyId);
            Assert.AreEqual(2, screen.SelectedSurvivorIds.Count);
            Assert.AreEqual("Squad 2/2", screen.SquadTitle);
            Assert.IsTrue(screen.SquadMembers[0].IsSelected);
            Assert.IsTrue(screen.SquadMembers[1].IsSelected);
            Assert.AreEqual("Policy", screen.PolicyTitle);
            Assert.IsTrue(screen.Policies.Exists(policy => policy.PolicyId == "aggressive" && policy.IsSelected));
            Assert.AreEqual("abandoned_store * Aggressive", screen.Selected.Title);
            Assert.That(screen.Selected.Details, Does.Contain("Survival"));
            Assert.IsTrue(screen.Selected.CanLaunch);

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
            Assert.IsFalse(radio.CanSkipCandidates);
            Assert.AreEqual("Signals", radio.CandidateListTitle);
            Assert.AreEqual("Awaiting", radio.EmptyTitle);
            Assert.AreEqual("Press broadcast", radio.EmptyBody);
            Assert.AreEqual(0, radio.Candidates.Count);

            var broadcast = RecruitmentSystem.Broadcast(state, config, new BroadcastRecruitmentRequest { Seed = 1 });
            var pending = CampDashboardTextFormatter.BuildRadioScreen(state, config, catalog);

            Assert.IsTrue(broadcast.Validation.IsValid);
            Assert.AreEqual("Pending", pending.BroadcastStatus);
            Assert.AreEqual("Skip", pending.BroadcastButton);
            Assert.IsFalse(pending.CanBroadcast);
            Assert.IsTrue(pending.CanSkipCandidates);
            Assert.AreEqual(1, pending.Candidates.Count);
            Assert.AreEqual("elias", pending.Candidates[0].CandidateId);
            Assert.AreEqual("Elias", pending.Candidates[0].Name);
            Assert.AreEqual("E", pending.Candidates[0].Avatar);
            Assert.AreEqual("Scavenger/Rusty Knife", pending.Candidates[0].Meta);
            Assert.AreEqual("scavenging 1", pending.Candidates[0].Skill);
            Assert.AreEqual("Traits Careful", pending.Candidates[0].Traits);
            Assert.AreEqual("Recruit", pending.Candidates[0].RecruitButton);
            Assert.IsTrue(pending.Candidates[0].CanRecruit);

            RecruitmentSystem.Recruit(state, config, new RecruitSurvivorRequest { CandidateId = pending.Candidates[0].CandidateId });
            var afterRecruit = CampDashboardTextFormatter.BuildRadioScreen(state, config, catalog);

            Assert.AreEqual(0, afterRecruit.Candidates.Count);
            Assert.IsFalse(afterRecruit.CanBroadcast);
            Assert.IsFalse(afterRecruit.CanSkipCandidates);
            Assert.That(afterRecruit.BroadcastStatus, Does.Contain("Blocked"));

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void RecruitmentEventAppearsInAlertsAndReports()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            BuildingSystem.Upgrade(state, config, "barracks");

            var recruited = RecruitWithBroadcast(state, config, "elias", 500);

            Assert.IsTrue(recruited.Validation.IsValid);
            var alerts = CampDashboardTextFormatter.BuildAlerts(state, config, catalog);
            Assert.AreEqual("JOIN Elias", alerts[0].Title);
            Assert.AreEqual("Roster 2/2", alerts[0].Body);

            var reports = CampDashboardTextFormatter.BuildReports(state, config, catalog);
            Assert.IsTrue(reports.HasCampEvent);
            Assert.AreEqual("Notes", reports.CampEventPanelTitle);
            Assert.AreEqual("Elias joined", reports.CampEventTitle);
            Assert.AreEqual("Scavenger:2/2", reports.CampEventBody);
            Assert.IsTrue(reports.HasAnyReport);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void DemoCompletionEventAppearsInAlertsAndReports()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();

            state.CampEvents.Add(new CampEventState
            {
                Id = "event_1",
                EventId = GameEventIds.DemoCompleted,
                SubjectId = GameConditionTypes.ZoneCompletions + ":abandoned_store",
                SubjectName = GameConditionTypes.ZoneCompletions + ":abandoned_store",
                DetailId = GameConditionTypes.ZoneCompletions + ":abandoned_store",
                AtUnixMs = 250
            });

            var alerts = CampDashboardTextFormatter.BuildAlerts(state, config, catalog);
            Assert.AreEqual("DONE Abandoned Store", alerts[0].Title);
            Assert.AreEqual("Signal Abandoned Store", alerts[0].Body);

            var reports = CampDashboardTextFormatter.BuildReports(state, config, catalog);
            Assert.IsTrue(reports.HasCampEvent);
            Assert.AreEqual("Notes", reports.CampEventPanelTitle);
            Assert.AreEqual("Complete Abandoned Store", reports.CampEventTitle);
            Assert.AreEqual("Demo Abandoned Store", reports.CampEventBody);

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
        public void NextGoalUsesCatalogRulesAndStateProgress()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();

            var first = CampDashboardTextFormatter.BuildNextGoal(state, config, catalog);
            Assert.AreEqual("Store", first.Title);
            Assert.AreEqual("Run the first route.", first.Body);
            Assert.AreEqual("0/1", first.Progress);
            Assert.IsFalse(first.IsComplete);

            state.Zones["abandoned_store"].Completions = 1;
            var second = CampDashboardTextFormatter.BuildNextGoal(state, config, catalog);
            Assert.AreEqual("Barracks", second.Title);
            Assert.AreEqual("0/1", second.Progress);

            state.Buildings["barracks"].Level = 1;
            state.SurvivorCap = 2;
            state.Survivors.Add(new SurvivorState { Id = "survivor_2", Name = "Elias" });
            state.Buildings["workshop"].Level = 1;
            state.Buildings["radio_tower"].Level = 2;
            state.Progress.DemoCompleted = true;
            var complete = CampDashboardTextFormatter.BuildNextGoal(state, config, catalog);
            Assert.AreEqual("Done", complete.Title);
            Assert.IsTrue(complete.IsComplete);

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
            Assert.AreEqual("Again", reports.AfterActionSendAgainButton);
            Assert.IsFalse(reports.AfterActionCanSendAgain);
            Assert.IsNotNull(reports.AfterActionSendAgainRequest);
            Assert.AreEqual("abandoned_store", reports.AfterActionSendAgainRequest.ZoneId);
            Assert.AreEqual("balanced", reports.AfterActionSendAgainRequest.PolicyId);
            CollectionAssert.AreEqual(new[] { "survivor_1" }, reports.AfterActionSendAgainRequest.SurvivorIds);
            Assert.AreEqual("Offline 2", reports.OfflineSummary);
            Assert.AreEqual("Resources Food x3", reports.OfflineResources);
            Assert.AreEqual("Completed abandoned_store", reports.OfflineCompleted);
            Assert.AreEqual("Healing Mara", reports.OfflineHealing);
            Assert.AreEqual("Warnings Mara", reports.OfflineWarnings);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void AfterActionSendAgainIsEnabledForSameIdleSquad()
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
            ExpeditionSimulator.Complete(state, config, launched.Expedition);

            var reports = CampDashboardTextFormatter.BuildReports(state, config, catalog);

            Assert.IsTrue(reports.HasAfterAction);
            Assert.IsTrue(reports.AfterActionCanSendAgain);
            Assert.IsNotNull(reports.AfterActionSendAgainRequest);
            Assert.AreEqual("abandoned_store", reports.AfterActionSendAgainRequest.ZoneId);
            Assert.AreEqual("balanced", reports.AfterActionSendAgainRequest.PolicyId);
            CollectionAssert.AreEqual(new[] { "survivor_1" }, reports.AfterActionSendAgainRequest.SurvivorIds);

            Object.DestroyImmediate(catalog);
        }

        private static RecruitSurvivorResult RecruitWithBroadcast(GameState state, GameConfigSnapshot config, string candidateId, long nowUnixMs = 0)
        {
            var broadcast = RecruitmentSystem.Broadcast(state, config, new BroadcastRecruitmentRequest { Seed = 1, NowUnixMs = nowUnixMs });
            Assert.IsTrue(broadcast.Validation.IsValid);
            return RecruitmentSystem.Recruit(state, config, new RecruitSurvivorRequest { CandidateId = candidateId, NowUnixMs = nowUnixMs });
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
            catalog.EmergencyScavengeAlertTitle = "SCAVENGE";
            catalog.EmergencyScavengeReadyBodyFormat = "{0}:{1}";
            catalog.EmergencyScavengeActiveBodyFormat = "Active {0}";
            catalog.EmergencyScavengeCooldownBodyFormat = "Cooldown {0}";
            catalog.EmergencyScavengeButton = "Go";
            catalog.EmergencyScavengeCompletedAlertTitle = "RECOVERED";
            catalog.EmergencyScavengeCompletedAlertBodyFormat = "Recovered {0}";
            catalog.UpgradeAvailableAlertTitleFormat = "UP {0}";
            catalog.UpgradeAvailableAlertBodyFormat = "Level {0}";
            catalog.SurvivorJoinedAlertTitleFormat = "JOIN {0}";
            catalog.SurvivorJoinedAlertBodyFormat = "Roster {1}/{2}";
            catalog.DemoCompletedAlertTitleFormat = "DONE {0}";
            catalog.DemoCompletedAlertBodyFormat = "Signal {0}";
            catalog.ExpeditionActiveSubtitleFormat = "{0}:{1}:{2}";
            catalog.ExpeditionActiveStatusFormat = "{0}%";
            catalog.ExpeditionRouteSubtitleFormat = "{0}:{1}:{2}";
            catalog.ExpeditionRouteStatusFormat = "{0}%";
            catalog.ExpeditionScreenTitle = "EXPEDITIONS";
            catalog.ExpeditionSquadTitleFormat = "Squad {0}/{1}";
            catalog.ExpeditionSquadMemberMetaFormat = "{0}/{1}/{2}/{3}";
            catalog.ExpeditionPolicyTitle = "Policy";
            catalog.ExpeditionPolicyDetailsFormat = "{0}/{1}/{2}/{3}";
            catalog.ExpeditionSurvivalChanceFormat = "Survival {0}";
            catalog.ExpeditionSelectedTitleFormat = "{0} * {1}";
            catalog.ExpeditionSelectedDetailsFormat = "Duration {0} Cost {1}/{2} Power {3}/{4} Familiarity {5}";
            catalog.ExpeditionSelectedLootFormat = "Loot {0}";
            catalog.ExpeditionLootRangeFormat = "{0} {1}-{2}";
            catalog.ExpeditionSelectedEnemiesFormat = "Threats {0}";
            catalog.ExpeditionSelectedWarningsFormat = "Warnings {0}";
            catalog.ExpeditionLaunchButton = "LAUNCH";
            catalog.ExpeditionLaunchBlockedButton = "BLOCKED";
            catalog.ExpeditionReviewRiskButton = "REVIEW";
            catalog.ExpeditionConfirmRiskButton = "CONFIRM";
            catalog.ExpeditionRiskConfirmationNoticeFormat = "Confirm {0}";
            catalog.ExpeditionMonitorTitle = "ACTIVE";
            catalog.ExpeditionMonitorHeaderFormat = "{0}/{1}/{2}";
            catalog.ExpeditionMonitorProgressFormat = "Progress {0}";
            catalog.ExpeditionMonitorLootFormat = "Loot {0}";
            catalog.ExpeditionMonitorNoiseFormat = "Noise {0}/{1}";
            catalog.ExpeditionMonitorLogFormat = "Log {0}";
            catalog.ExpeditionMonitorThreatFormat = "Threat {0}";
            catalog.ExpeditionMonitorWoundsFormat = "Wounds {0}";
            catalog.ExpeditionMonitorFoundItemsFormat = "Finds {0}";
            catalog.ExpeditionMonitorNoiseLowLabel = "LOW";
            catalog.ExpeditionMonitorNoiseMediumLabel = "MED";
            catalog.ExpeditionMonitorNoiseHighLabel = "HIGH";
            catalog.ExpeditionMonitorNoiseMediumThreshold = 3;
            catalog.ExpeditionMonitorNoiseHighThreshold = 6;
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
            catalog.RadioBroadcastPendingLabel = "Pending";
            catalog.RadioBroadcastBlockedFormat = "Blocked {0}";
            catalog.RadioCandidateListTitle = "Signals";
            catalog.RadioCandidateAwaitingTitle = "Awaiting";
            catalog.RadioCandidateAwaitingBody = "Press broadcast";
            catalog.RadioCandidateEmptyTitle = "No signals";
            catalog.RadioCandidateEmptyBody = "Empty";
            catalog.RadioCandidateCardMetaFormat = "{0}/{1}";
            catalog.RadioCandidateSkillFormat = "{0} {1}";
            catalog.RadioCandidateTraitsFormat = "Traits {0}";
            catalog.RadioCandidateRecruitButton = "Recruit";
            catalog.RadioCandidateSkipButton = "Skip";
            catalog.ReportsScreenTitle = "Reports";
            catalog.ReportsEmptyTitle = "Empty";
            catalog.ReportsEmptyBody = "No reports";
            catalog.CampEventPanelTitle = "Notes";
            catalog.SurvivorJoinedReportTitleFormat = "{0} joined";
            catalog.SurvivorJoinedReportBodyFormat = "{1}:{2}/{3}";
            catalog.DemoCompletedReportTitleFormat = "Complete {0}";
            catalog.DemoCompletedReportBodyFormat = "Demo {0}";
            catalog.EmergencyScavengeReportTitle = "Recovered report";
            catalog.EmergencyScavengeReportBodyFormat = "Report {0}";
            catalog.AfterActionPanelTitle = "After";
            catalog.AfterActionSuccessLabel = "SUCCESS";
            catalog.AfterActionFailureLabel = "FAIL";
            catalog.AfterActionOutcomeFormat = "{0}/{1}/{2}";
            catalog.AfterActionLootFormat = "Loot {0}";
            catalog.AfterActionXpFormat = "XP {0}";
            catalog.AfterActionWoundsFormat = "Wounds {0}";
            catalog.AfterActionEnemiesFormat = "Enemies {0}";
            catalog.AfterActionEventsFormat = "Events {0}";
            catalog.AfterActionSendAgainButton = "Again";
            catalog.OfflineReportPanelTitle = "Offline";
            catalog.OfflineReportSummaryFormat = "Offline {0}";
            catalog.OfflineReportResourcesFormat = "Resources {0}";
            catalog.OfflineReportCompletedFormat = "Completed {0}";
            catalog.OfflineReportHealingFormat = "Healing {0}";
            catalog.OfflineReportWarningsFormat = "Warnings {0}";
            catalog.SettingsScreenTitle = "Settings";
            catalog.SettingsAutosaveTitle = "Autosave";
            catalog.SettingsAutosaveEnabledBodyFormat = "Every {0}s";
            catalog.SettingsAutosaveDisabledBody = "Off body";
            catalog.SettingsAutosaveEnabledLabel = "On";
            catalog.SettingsAutosaveDisabledLabel = "Off";
            catalog.SettingsAutosaveToggleLabel = "Autosave toggle";
            catalog.ReportNoneLabel = "None";
            catalog.ReportListSeparator = ", ";
            catalog.ReportCountFormat = "{0} x{1}";
            catalog.NextGoalCompleteTitle = "Done";
            catalog.NextGoalCompleteBody = "Complete";
            catalog.NextGoalProgressFormat = "{0}/{1}";
            catalog.NextGoals.Add(new CampNextGoalEntry
            {
                Id = "store",
                Title = "Store",
                Body = "Run the first route.",
                Conditions = new List<CampGoalConditionEntry>
                {
                    new CampGoalConditionEntry { Type = GameConditionTypes.ZoneCompletions, Id = "abandoned_store", Value = 1 }
                }
            });
            catalog.NextGoals.Add(new CampNextGoalEntry
            {
                Id = "barracks",
                Title = "Barracks",
                Body = "Upgrade beds.",
                Conditions = new List<CampGoalConditionEntry>
                {
                    new CampGoalConditionEntry { Type = GameConditionTypes.BuildingLevel, Id = "barracks", Value = 1 }
                }
            });
            catalog.NextGoals.Add(new CampNextGoalEntry
            {
                Id = "recruit",
                Title = "Recruit",
                Body = "Bring another survivor.",
                Conditions = new List<CampGoalConditionEntry>
                {
                    new CampGoalConditionEntry { Type = GameConditionTypes.SurvivorCount, Value = 2 }
                }
            });
            catalog.NextGoals.Add(new CampNextGoalEntry
            {
                Id = "workshop",
                Title = "Workshop",
                Body = "Build repair capacity.",
                Conditions = new List<CampGoalConditionEntry>
                {
                    new CampGoalConditionEntry { Type = GameConditionTypes.BuildingLevel, Id = "workshop", Value = 1 }
                }
            });
            catalog.NextGoals.Add(new CampNextGoalEntry
            {
                Id = "demo",
                Title = "Signal",
                Body = "Finish the demo challenge.",
                CompleteWhenAnyCondition = true,
                Conditions = new List<CampGoalConditionEntry>
                {
                    new CampGoalConditionEntry { Type = GameConditionTypes.ZoneCompletions, Id = "mutant_tunnel", Value = 1 },
                    new CampGoalConditionEntry { Type = GameConditionTypes.BuildingLevel, Id = "radio_tower", Value = 2 }
                }
            });
            return catalog;
        }
    }
}
