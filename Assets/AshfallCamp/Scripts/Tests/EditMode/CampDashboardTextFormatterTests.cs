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
            catalog.DefaultExpeditionPolicyId = "balanced";
            catalog.RadioIntelBodyFormat = "{0}/{1}/{2}";
            return catalog;
        }
    }
}
