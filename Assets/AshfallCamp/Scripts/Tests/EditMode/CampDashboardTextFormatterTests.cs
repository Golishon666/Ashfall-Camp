using System.Collections.Generic;
using System.Reflection;
using AshfallCamp.Domain;
using AshfallCamp.Presentation;
using TMPro;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

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
            Assert.AreEqual("wounded_survivors", alerts[0].Id);
            Assert.AreEqual(CampAlertSeverity.Critical, alerts[0].Severity);
            Assert.AreEqual(CampAlertAction.OpenScreen, alerts[0].Action);
            Assert.AreEqual("survivors", alerts[0].TargetScreenId);
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
        public void IdleSurvivorAlertUsesExpeditionAction()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            FillCappedResources(state, config);

            var alerts = CampDashboardTextFormatter.BuildAlerts(state, config, catalog);
            var idle = FindAlert(alerts, "idle_survivors");

            Assert.NotNull(idle);
            Assert.AreEqual(CampAlertAction.OpenScreen, idle.Action);
            Assert.AreEqual("expeditions", idle.TargetScreenId);
            Assert.AreEqual("MANAGE", idle.ActionLabel);
            Assert.AreEqual(CampAlertButtonView.Text, idle.ButtonView);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void AlertConfigOverridesPriorityAndActionMetadata()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            FillCappedResources(state, config);
            state.Resources["scrap"] = 100;
            catalog.Alerts.Add(new AlertUiEntry
            {
                Id = "upgrade_available",
                Severity = CampAlertSeverity.Warning,
                Priority = 1200,
                Category = "buildings",
                Action = CampAlertAction.OpenScreen,
                TargetScreenId = "buildings",
                ButtonLabel = "GO",
                ButtonView = CampAlertButtonView.Text,
                ToneColor = Color.yellow
            });

            var alerts = CampDashboardTextFormatter.BuildAlerts(state, config, catalog);

            Assert.That(alerts.Count, Is.GreaterThan(0));
            Assert.AreEqual("upgrade_available", alerts[0].Id);
            Assert.AreEqual(1200, alerts[0].Priority);
            Assert.AreEqual("buildings", alerts[0].Category);
            Assert.AreEqual(CampAlertAction.OpenScreen, alerts[0].Action);
            Assert.AreEqual("buildings", alerts[0].TargetScreenId);
            Assert.AreEqual("GO", alerts[0].ActionLabel);

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
            Assert.AreEqual("Manual save", enabled.ManualSaveTitle);
            Assert.AreEqual("Writes now", enabled.ManualSaveBody);
            Assert.AreEqual("Save now", enabled.ManualSaveButton);
            Assert.IsTrue(enabled.AutosaveEnabled);

            Assert.AreEqual("Off body", disabled.AutosaveBody);
            Assert.AreEqual("Off", disabled.AutosaveState);
            Assert.IsFalse(disabled.AutosaveEnabled);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SettingsPanelRaisesManualSaveRequest()
        {
            var root = new GameObject("SettingsPanel");
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();

            try
            {
                var view = root.AddComponent<SettingsPanelView>();
                var title = CreateText("Title", root.transform);
                var autosaveTitle = CreateText("AutosaveTitle", root.transform);
                var autosaveBody = CreateText("AutosaveBody", root.transform);
                var autosaveState = CreateText("AutosaveState", root.transform);
                var autosaveToggle = CreateToggle("AutosaveToggle", root.transform);
                var autosaveToggleLabel = CreateText("AutosaveToggleLabel", root.transform);
                var manualSaveTitle = CreateText("ManualSaveTitle", root.transform);
                var manualSaveBody = CreateText("ManualSaveBody", root.transform);
                var manualSaveButton = CreateButton("ManualSaveButton", root.transform);
                var manualSaveButtonLabel = CreateText("ManualSaveButtonLabel", manualSaveButton.transform);
                var saveRequested = false;

                view.ConfigureBindings(
                    title,
                    autosaveTitle,
                    autosaveBody,
                    autosaveState,
                    autosaveToggle,
                    autosaveToggleLabel,
                    manualSaveTitle,
                    manualSaveBody,
                    manualSaveButton,
                    manualSaveButtonLabel);
                view.SetManualSaveHandler(() => saveRequested = true);

                view.Render(state, config, catalog);
                manualSaveButton.onClick.Invoke();

                Assert.AreEqual("Manual save", manualSaveTitle.text);
                Assert.AreEqual("Writes now", manualSaveBody.text);
                Assert.AreEqual("Save now", manualSaveButtonLabel.text);
                Assert.IsTrue(manualSaveButton.interactable);
                Assert.IsTrue(saveRequested);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void ToastPanelFormatsCatalogMessageAndAppliesAccent()
        {
            var root = new GameObject("ToastPanel", typeof(RectTransform), typeof(CanvasGroup));
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();

            try
            {
                var group = root.GetComponent<CanvasGroup>();
                var accent = CreateImage("Accent", root.transform);
                var title = CreateText("ToastTitle", root.transform);
                var body = CreateText("ToastBody", root.transform);
                var tone = new Color(0.2f, 0.4f, 0.6f, 1f);

                catalog.Toast.Enabled = false;
                catalog.ToastMessages.Add(new CampToastUiEntry
                {
                    Id = CampToastIds.ManualSaved,
                    TitleFormat = "Saved {0}",
                    BodyFormat = "Body {0}",
                    ToneColor = tone
                });

                var view = root.AddComponent<ToastPanelView>();
                view.ConfigureBindings(group, accent, title, body);

                view.Show(new CampToastRequest(CampToastIds.ManualSaved, "Now"), catalog);

                Assert.IsTrue(root.activeSelf);
                Assert.AreEqual(1f, group.alpha);
                Assert.AreEqual("Saved Now", title.text);
                Assert.AreEqual("Body Now", body.text);
                Assert.AreEqual(tone, accent.color);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void ProductionCatalogRoutesSettingsThroughBottomNavigation()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>("Assets/AshfallCamp/UI/CampUiCatalog.asset");

            Assert.IsNotNull(catalog);

            var settingsInNav = false;
            foreach (var item in catalog.NavItems)
            {
                if (item != null && item.Id == "settings")
                {
                    settingsInNav = true;
                    break;
                }
            }

            var settingsInBuildingFilters = false;
            foreach (var item in catalog.BuildingFilters)
            {
                if (item != null && item.Id == "settings")
                {
                    settingsInBuildingFilters = true;
                    break;
                }
            }

            Assert.IsTrue(settingsInNav);
            Assert.IsFalse(settingsInBuildingFilters);
        }

        [Test]
        public void BottomNavInactivePanelAlphaComesFromCatalogTheme()
        {
            var root = new GameObject("BottomNav");
            var activePanel = new GameObject("ActivePanel", typeof(RectTransform), typeof(Image));
            var inactivePanel = new GameObject("InactivePanel", typeof(RectTransform), typeof(Image));
            var activeButton = activePanel.AddComponent<Button>();
            var inactiveButton = inactivePanel.AddComponent<Button>();
            var activeLabel = CreateText("ActiveLabel", activePanel.transform);
            var inactiveLabel = CreateText("InactiveLabel", inactivePanel.transform);
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();

            try
            {
                activePanel.transform.SetParent(root.transform, false);
                inactivePanel.transform.SetParent(root.transform, false);

                catalog.Theme.Teal = new Color(0.1f, 0.2f, 0.3f, 1f);
                catalog.Theme.Paper = new Color(0.9f, 0.8f, 0.7f, 1f);
                catalog.Theme.Ink = new Color(0.2f, 0.1f, 0.05f, 1f);
                catalog.Theme.PaperDark = new Color(0.4f, 0.35f, 0.3f, 1f);
                catalog.Theme.NavInactivePanelAlpha = 0.27f;
                catalog.NavItems.Add(new NavUiEntry { Id = "active", Label = "ACTIVE", IsActive = true });
                catalog.NavItems.Add(new NavUiEntry { Id = "inactive", Label = "INACTIVE" });

                var view = root.AddComponent<BottomNavView>();
                view.ConfigureBindings(new[]
                {
                    new BottomNavView.NavBinding("active", activePanel.GetComponent<Image>(), activeButton, activeLabel),
                    new BottomNavView.NavBinding("inactive", inactivePanel.GetComponent<Image>(), inactiveButton, inactiveLabel)
                });

                view.Render(catalog, "active");

                Assert.AreEqual("ACTIVE", activeLabel.text);
                Assert.AreEqual("INACTIVE", inactiveLabel.text);
                Assert.AreEqual(catalog.Theme.Teal, activePanel.GetComponent<Image>().color);
                Assert.AreEqual(catalog.Theme.PaperDark.r, inactivePanel.GetComponent<Image>().color.r);
                Assert.AreEqual(catalog.Theme.PaperDark.g, inactivePanel.GetComponent<Image>().color.g);
                Assert.AreEqual(catalog.Theme.PaperDark.b, inactivePanel.GetComponent<Image>().color.b);
                Assert.AreEqual(0.27f, inactivePanel.GetComponent<Image>().color.a);
                Assert.IsFalse(activeButton.interactable);
                Assert.IsTrue(inactiveButton.interactable);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void BuildingFilterInactivePanelAlphaComesFromCatalogTheme()
        {
            var root = new GameObject("BuildingGrid");
            var activePanel = new GameObject("ActiveFilter", typeof(RectTransform), typeof(Image));
            var inactivePanel = new GameObject("InactiveFilter", typeof(RectTransform), typeof(Image));
            var title = CreateText("Title", root.transform);
            var activeLabel = CreateText("ActiveFilterLabel", activePanel.transform);
            var inactiveLabel = CreateText("InactiveFilterLabel", inactivePanel.transform);
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);

            try
            {
                activePanel.transform.SetParent(root.transform, false);
                inactivePanel.transform.SetParent(root.transform, false);

                catalog.BuildingScreenTitle = "BUILDINGS";
                catalog.Theme.Teal = new Color(0.1f, 0.2f, 0.3f, 1f);
                catalog.Theme.Paper = new Color(0.9f, 0.8f, 0.7f, 1f);
                catalog.Theme.Ink = new Color(0.2f, 0.1f, 0.05f, 1f);
                catalog.Theme.PaperDark = new Color(0.4f, 0.35f, 0.3f, 1f);
                catalog.Theme.BuildingFilterInactivePanelAlpha = 0.42f;
                catalog.BuildingFilters.Add(new FilterUiEntry { Id = "all", Label = "ALL", IsActive = true });
                catalog.BuildingFilters.Add(new FilterUiEntry { Id = "support", Label = "SUPPORT" });

                var view = root.AddComponent<BuildingGridView>();
                view.ConfigureBindings(
                    title,
                    new[]
                    {
                        new BuildingGridView.FilterBinding("all", activePanel.GetComponent<Image>(), activeLabel),
                        new BuildingGridView.FilterBinding("support", inactivePanel.GetComponent<Image>(), inactiveLabel)
                    },
                    new BuildingCardView[0]);

                view.Render(state, config, catalog);

                Assert.AreEqual("BUILDINGS", title.text);
                Assert.AreEqual("ALL", activeLabel.text);
                Assert.AreEqual("SUPPORT", inactiveLabel.text);
                Assert.AreEqual(catalog.Theme.Teal, activePanel.GetComponent<Image>().color);
                Assert.AreEqual(catalog.Theme.PaperDark.r, inactivePanel.GetComponent<Image>().color.r);
                Assert.AreEqual(catalog.Theme.PaperDark.g, inactivePanel.GetComponent<Image>().color.g);
                Assert.AreEqual(catalog.Theme.PaperDark.b, inactivePanel.GetComponent<Image>().color.b);
                Assert.AreEqual(0.42f, inactivePanel.GetComponent<Image>().color.a);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void AlertPanelAlphaComesFromCatalogTheme()
        {
            var root = new GameObject("Alerts");
            var panel = CreateImage("Panel", root.transform);
            var dot = CreateImage("Dot", root.transform);
            var title = CreateText("Title", root.transform);
            var alertTitle = CreateText("AlertTitle", root.transform);
            var alertBody = CreateText("AlertBody", root.transform);
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);

            try
            {
                catalog.RecentAlertsTitle = "ALERTS";
                catalog.Theme.AlertPanelAlpha = 0.19f;
                catalog.Alerts.Add(new AlertUiEntry
                {
                    Id = "static",
                    Title = "Static",
                    Body = "Ready",
                    ToneColor = new Color(0.2f, 0.3f, 0.4f, 1f)
                });

                var view = root.AddComponent<CampAlertsPanelView>();
                view.ConfigureBindings(title, new[]
                {
                    new CampAlertsPanelView.AlertBinding(panel, dot, alertTitle, alertBody)
                });

                view.Render(state, config, catalog);

                Assert.AreEqual("ALERTS", title.text);
                Assert.AreEqual(0.19f, panel.color.a, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void ExpeditionCardAlphasComeFromCatalogTheme()
        {
            var root = new GameObject("ExpeditionBindings");
            var routeButton = CreateButton("Route", root.transform);
            var squadButton = CreateButton("Squad", root.transform);
            var policyButton = CreateButton("Policy", root.transform);
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();

            try
            {
                catalog.Theme.Teal = new Color(0.1f, 0.2f, 0.3f, 1f);
                catalog.Theme.Amber = new Color(0.7f, 0.5f, 0.2f, 1f);
                catalog.Theme.PaperDark = new Color(0.4f, 0.35f, 0.3f, 1f);
                catalog.Theme.ExpeditionRouteSelectedPanelAlpha = 0.21f;
                catalog.Theme.ExpeditionRouteAvailablePanelAlpha = 0.22f;
                catalog.Theme.ExpeditionRouteBlockedPanelAlpha = 0.23f;
                catalog.Theme.ExpeditionSquadSelectedPanelAlpha = 0.31f;
                catalog.Theme.ExpeditionSquadAvailablePanelAlpha = 0.32f;
                catalog.Theme.ExpeditionSquadBlockedPanelAlpha = 0.33f;
                catalog.Theme.ExpeditionPolicySelectedPanelAlpha = 0.41f;
                catalog.Theme.ExpeditionPolicyInactivePanelAlpha = 0.42f;

                var route = new ExpeditionsPanelView.RouteCardBinding(
                    routeButton.GetComponent<Image>(),
                    routeButton,
                    CreateText("RouteTitle", routeButton.transform),
                    CreateText("RouteSubtitle", routeButton.transform),
                    CreateText("RouteStatus", routeButton.transform));
                route.Render(new CampExpeditionRoutePresentation("zone", "Zone", "Open", "Ready", true, true, true), catalog);
                Assert.AreEqual(0.21f, routeButton.GetComponent<Image>().color.a, 0.0001f);
                route.Render(new CampExpeditionRoutePresentation("zone", "Zone", "Open", "Ready", true, true, false), catalog);
                Assert.AreEqual(0.22f, routeButton.GetComponent<Image>().color.a, 0.0001f);
                route.Render(new CampExpeditionRoutePresentation("zone", "Zone", "Locked", "No", false, false, false), catalog);
                Assert.AreEqual(0.23f, routeButton.GetComponent<Image>().color.a, 0.0001f);

                var mapNodeLabel = CreateText("MapNodeLabel", routeButton.transform);
                var mapNode = new ExpeditionsPanelView.RouteMapNodeBinding(
                    routeButton.GetComponent<Image>(),
                    routeButton,
                    mapNodeLabel);
                mapNode.Render(new CampExpeditionRoutePresentation("store", "Store", "Open", "Ready", true, true, true), catalog);
                Assert.AreEqual("Store", mapNodeLabel.text);
                Assert.AreEqual(0.21f, routeButton.GetComponent<Image>().color.a, 0.0001f);
                Assert.IsFalse(routeButton.interactable);
                mapNode.Render(new CampExpeditionRoutePresentation("clinic", "Clinic", "Locked", "No", false, false, false), catalog);
                Assert.AreEqual(0.23f, routeButton.GetComponent<Image>().color.a, 0.0001f);
                Assert.IsFalse(routeButton.interactable);

                var squad = new ExpeditionsPanelView.SquadMemberBinding(
                    squadButton.GetComponent<Image>(),
                    squadButton,
                    CreateText("SquadTitle", squadButton.transform),
                    CreateText("SquadMeta", squadButton.transform));
                squad.Render(new CampExpeditionSquadMemberPresentation("survivor", "Mara", "Ready", true, true), catalog);
                Assert.AreEqual(0.31f, squadButton.GetComponent<Image>().color.a, 0.0001f);
                squad.Render(new CampExpeditionSquadMemberPresentation("survivor", "Mara", "Ready", false, true), catalog);
                Assert.AreEqual(0.32f, squadButton.GetComponent<Image>().color.a, 0.0001f);
                squad.Render(new CampExpeditionSquadMemberPresentation("survivor", "Mara", "Busy", false, false), catalog);
                Assert.AreEqual(0.33f, squadButton.GetComponent<Image>().color.a, 0.0001f);

                var policy = new ExpeditionsPanelView.PolicyBinding(
                    policyButton.GetComponent<Image>(),
                    policyButton,
                    CreateText("PolicyTitle", policyButton.transform),
                    CreateText("PolicyDetails", policyButton.transform));
                policy.Render(new CampExpeditionPolicyPresentation("balanced", "Balanced", "Safe", true, true), catalog);
                Assert.AreEqual(0.41f, policyButton.GetComponent<Image>().color.a, 0.0001f);
                policy.Render(new CampExpeditionPolicyPresentation("balanced", "Balanced", "Safe", false, true), catalog);
                Assert.AreEqual(0.42f, policyButton.GetComponent<Image>().color.a, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void RepeatedCardAlphasComeFromCatalogTheme()
        {
            var root = new GameObject("RepeatedBindings");
            var survivorButton = CreateButton("Survivor", root.transform);
            var radioButton = CreateButton("Radio", root.transform);
            var workshopPanel = CreateImage("Workshop", root.transform);
            var workshopRepairButton = CreateButton("Repair", root.transform);
            var workshopEquipButton = CreateButton("Equip", root.transform);
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();

            try
            {
                catalog.Theme.Teal = new Color(0.1f, 0.2f, 0.3f, 1f);
                catalog.Theme.PaperDark = new Color(0.4f, 0.35f, 0.3f, 1f);
                catalog.Theme.SurvivorSelectedPanelAlpha = 0.51f;
                catalog.Theme.SurvivorInactivePanelAlpha = 0.52f;
                catalog.Theme.RadioCandidatePanelAlpha = 0.61f;
                catalog.Theme.WorkshopItemPanelAlpha = 0.71f;
                catalog.WorkshopItemDurabilityFormat = "{0}/{1}";
                catalog.WorkshopRepairButton = "Repair";
                catalog.WorkshopEquipButton = "Equip";

                var survivor = new SurvivorsPanelView.SurvivorCardBinding(
                    survivorButton.GetComponent<Image>(),
                    survivorButton,
                    CreateText("SurvivorAvatar", survivorButton.transform),
                    CreateText("SurvivorName", survivorButton.transform),
                    CreateText("SurvivorState", survivorButton.transform),
                    CreateText("SurvivorSkill", survivorButton.transform));
                survivor.Render(new CampSurvivorCardPresentation("survivor", "Mara", "M", "Idle", "Scav"), catalog, true);
                Assert.AreEqual(0.51f, survivorButton.GetComponent<Image>().color.a, 0.0001f);
                survivor.Render(new CampSurvivorCardPresentation("survivor", "Mara", "M", "Idle", "Scav"), catalog, false);
                Assert.AreEqual(0.52f, survivorButton.GetComponent<Image>().color.a, 0.0001f);

                var radio = new RadioPanelView.CandidateCardBinding(
                    radioButton.GetComponent<Image>(),
                    CreateText("Avatar", radioButton.transform),
                    CreateText("Name", radioButton.transform),
                    CreateText("Meta", radioButton.transform),
                    CreateText("Skill", radioButton.transform),
                    CreateText("Traits", radioButton.transform),
                    radioButton,
                    CreateText("Recruit", radioButton.transform));
                radio.Render(new CampRadioCandidatePresentation("candidate", "Bram", "E", "Scout", "Surv", "Calm", "Recruit", true), catalog);
                Assert.AreEqual(0.61f, radioButton.GetComponent<Image>().color.a, 0.0001f);

                var workshop = new WorkshopPanelView.WorkshopItemBinding(
                    workshopPanel,
                    CreateText("ItemName", root.transform),
                    CreateText("Durability", root.transform),
                    CreateText("Equipped", root.transform),
                    CreateText("Broken", root.transform),
                    CreateText("RepairCost", root.transform),
                    workshopRepairButton,
                    CreateText("RepairLabel", workshopRepairButton.transform),
                    workshopEquipButton,
                    CreateText("EquipLabel", workshopEquipButton.transform));
                workshop.Render(new CampWorkshopItemPresentation("item", "Knife", 2, 4, "Equipped", string.Empty, "Scrap x1", true, true), catalog, "survivor");
                Assert.AreEqual(0.71f, workshopPanel.color.a, 0.0001f);
            }
            finally
            {
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void WorkshopItemUsesCatalogTileArtworkAndKeepsFallback()
        {
            var root = new GameObject("WorkshopTileBinding");
            var panel = CreateImage("WorkshopItem", root.transform);
            var tileArtwork = CreateRawImage("WorkshopTileArtwork", panel.transform);
            var repairButton = CreateButton("Repair", panel.transform);
            var equipButton = CreateButton("Equip", panel.transform);
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();
            var texture = new Texture2D(1, 1);

            try
            {
                catalog.WorkshopItemTileTexture = texture;
                catalog.WorkshopItemDurabilityFormat = "{0}/{1}";
                catalog.WorkshopRepairButton = "Repair";
                catalog.WorkshopEquipButton = "Equip";

                var item = new WorkshopPanelView.WorkshopItemBinding(
                    panel,
                    tileArtwork,
                    CreateText("ItemName", panel.transform),
                    CreateText("Durability", panel.transform),
                    CreateText("Equipped", panel.transform),
                    CreateText("Broken", panel.transform),
                    CreateText("RepairCost", panel.transform),
                    repairButton,
                    CreateText("RepairLabel", repairButton.transform),
                    equipButton,
                    CreateText("EquipLabel", equipButton.transform));

                item.Render(new CampWorkshopItemPresentation("item", "Knife", 2, 4, "Equipped", string.Empty, "Scrap x1", true, true), catalog, "survivor");

                Assert.AreSame(texture, tileArtwork.texture);
                Assert.IsTrue(tileArtwork.gameObject.activeSelf);

                catalog.WorkshopItemTileTexture = null;
                item.Render(new CampWorkshopItemPresentation("item", "Knife", 2, 4, "Equipped", string.Empty, "Scrap x1", true, true), catalog, "survivor");

                Assert.IsFalse(tileArtwork.gameObject.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(root);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void SurvivorCardUsesCatalogPortraitAndKeepsAvatarFallback()
        {
            var root = new GameObject("SurvivorPortraitBinding");
            var survivorButton = CreateButton("Survivor", root.transform);
            var portrait = CreateRawImage("SurvivorPortrait", survivorButton.transform);
            var avatar = CreateText("SurvivorAvatar", survivorButton.transform);
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();
            var texture = new Texture2D(1, 1);

            try
            {
                var survivor = new SurvivorsPanelView.SurvivorCardBinding(
                    survivorButton.GetComponent<Image>(),
                    survivorButton,
                    portrait,
                    avatar,
                    CreateText("SurvivorName", survivorButton.transform),
                    CreateText("SurvivorState", survivorButton.transform),
                    CreateText("SurvivorSkill", survivorButton.transform));

                survivor.Render(new CampSurvivorCardPresentation("survivor", "Mara", "M", "Idle", "Scav", texture), catalog, false);

                Assert.IsTrue(portrait.gameObject.activeSelf);
                Assert.AreSame(texture, portrait.texture);
                Assert.IsFalse(avatar.gameObject.activeSelf);

                survivor.Render(new CampSurvivorCardPresentation("survivor", "Mara", "M", "Idle", "Scav"), catalog, false);

                Assert.IsFalse(portrait.gameObject.activeSelf);
                Assert.IsTrue(avatar.gameObject.activeSelf);
                Assert.AreEqual("M", avatar.text);
            }
            finally
            {
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void RadioCandidateUsesCatalogPortraitAndKeepsAvatarFallback()
        {
            var root = new GameObject("RadioCandidatePortraitBinding");
            var cardPanel = CreateImage("RadioCandidate", root.transform);
            var recruitButton = CreateButton("Recruit", root.transform);
            var portrait = CreateRawImage("RadioCandidatePortrait", cardPanel.transform);
            var avatar = CreateText("RadioCandidateAvatar", cardPanel.transform);
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();
            var texture = new Texture2D(1, 1);

            try
            {
                var candidate = new RadioPanelView.CandidateCardBinding(
                    cardPanel,
                    portrait,
                    avatar,
                    CreateText("RadioCandidateName", cardPanel.transform),
                    CreateText("RadioCandidateMeta", cardPanel.transform),
                    CreateText("RadioCandidateSkill", cardPanel.transform),
                    CreateText("RadioCandidateTraits", cardPanel.transform),
                    recruitButton,
                    CreateText("RecruitLabel", recruitButton.transform));

                candidate.Render(new CampRadioCandidatePresentation("survivor_02", "Bram", "E", "Ex-Cop", "FIRE 7", "Traits Brave", "Recruit", true, texture), catalog);

                Assert.IsTrue(portrait.gameObject.activeSelf);
                Assert.AreSame(texture, portrait.texture);
                Assert.IsFalse(avatar.gameObject.activeSelf);

                candidate.Render(new CampRadioCandidatePresentation("survivor_02", "Bram", "E", "Ex-Cop", "FIRE 7", "Traits Brave", "Recruit", true), catalog);

                Assert.IsFalse(portrait.gameObject.activeSelf);
                Assert.IsTrue(avatar.gameObject.activeSelf);
                Assert.AreEqual("E", avatar.text);
            }
            finally
            {
                Object.DestroyImmediate(texture);
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void ScreenBindingAppliesCanvasGroupVisibilityState()
        {
            var root = new GameObject("ScreenRoot");

            try
            {
                var group = root.AddComponent<CanvasGroup>();
                var binding = new CampDashboardView.ScreenBinding("screen", new[] { root }, new[] { group });
                var transition = new CampUiScreenTransition { Enabled = false };

                binding.SetActive(false, transition, true);

                Assert.IsFalse(root.activeSelf);
                Assert.AreEqual(0, group.alpha);
                Assert.IsFalse(group.interactable);
                Assert.IsFalse(group.blocksRaycasts);

                binding.SetActive(true, transition, true);

                Assert.IsTrue(root.activeSelf);
                Assert.AreEqual(1, group.alpha);
                Assert.IsTrue(group.interactable);
                Assert.IsTrue(group.blocksRaycasts);
            }
            finally
            {
                Object.DestroyImmediate(root);
            }
        }

        [Test]
        public void DashboardViewStartsOnDashboardEvenWhenCatalogMarksAnotherNavItemActive()
        {
            var root = new GameObject("Dashboard");
            var dashboardRoot = new GameObject("DashboardScreen");
            var survivorsRoot = new GameObject("SurvivorsScreen");
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();

            try
            {
                dashboardRoot.transform.SetParent(root.transform, false);
                survivorsRoot.transform.SetParent(root.transform, false);

                catalog.ScreenTransition.Enabled = false;
                catalog.NavItems.Add(new NavUiEntry { Id = "survivors", Label = "SURVIVORS", IsActive = true });
                catalog.NavItems.Add(new NavUiEntry { Id = "buildings", Label = "BUILDINGS", IsActive = false });

                var view = root.AddComponent<CampDashboardView>();
                SetPrivateField(view, "catalog", catalog);
                SetPrivateField(view, "screens", new List<CampDashboardView.ScreenBinding>
                {
                    new CampDashboardView.ScreenBinding("buildings", new[] { dashboardRoot }),
                    new CampDashboardView.ScreenBinding("survivors", new[] { survivorsRoot })
                });

                InvokePrivate(view, "EnsureActiveScreenId");
                InvokePrivate(view, "ApplyScreenVisibility");

                Assert.AreEqual("buildings", GetPrivateField<string>(view, "_activeScreenId"));
                Assert.IsTrue(dashboardRoot.activeSelf);
                Assert.IsFalse(survivorsRoot.activeSelf);
            }
            finally
            {
                Object.DestroyImmediate(catalog);
                Object.DestroyImmediate(root);
            }
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
            Assert.AreEqual("Threats Radiated Hound", screen.Selected.Enemies);
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
            launched.Expedition.EnemiesDefeated["creature_weak_radiated_hound"] = 2;
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
            Assert.AreEqual("Log Threat Radiated Hound x2, Wounds Mara, Finds Rusty Knife x1, First event, Second event", active.Monitor.Log);

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
            var recruited = RecruitWithBroadcast(state, config, "survivor_02");
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
            var portrait = new Texture2D(1, 1);
            catalog.SurvivorPortraits.Add(new SurvivorPortraitUiEntry { Id = "survivor_02", Portrait = portrait });
            state.Resources["scrap"] = 100;
            state.Resources["food"] = 20;
            state.Resources["water"] = 20;
            TestBuildingUpgrades.UpgradeAndComplete(state, config, "barracks");

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
            Assert.AreEqual("survivor_02", pending.Candidates[0].CandidateId);
            Assert.AreEqual("Bram", pending.Candidates[0].Name);
            Assert.AreEqual("B", pending.Candidates[0].Avatar);
            Assert.AreSame(portrait, pending.Candidates[0].Portrait);
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
            Object.DestroyImmediate(portrait);
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
            TestBuildingUpgrades.UpgradeAndComplete(state, config, "barracks");

            var recruited = RecruitWithBroadcast(state, config, "survivor_02", 500);

            Assert.IsTrue(recruited.Validation.IsValid);
            var alerts = CampDashboardTextFormatter.BuildAlerts(state, config, catalog);
            Assert.AreEqual("JOIN Bram", alerts[0].Title);
            Assert.AreEqual("Roster 2/2", alerts[0].Body);

            var reports = CampDashboardTextFormatter.BuildReports(state, config, catalog);
            Assert.IsTrue(reports.HasCampEvent);
            Assert.AreEqual("Notes", reports.CampEventPanelTitle);
            Assert.AreEqual("Bram joined", reports.CampEventTitle);
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
            state.Survivors.Add(new SurvivorState { Id = "survivor_2", Name = "Bram" });
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
            state.Inventory[0].Durability = 1;
            launched.Expedition.AccumulatedLoot["scrap"] = 8;
            launched.Expedition.EnemiesDefeated["creature_weak_radiated_hound"] = 1;
            launched.Expedition.WoundedSurvivorIds.Add("survivor_1");
            launched.Expedition.Log.Add(new ExpeditionLogEntry { AtSeconds = 12, Message = "Mara opened a cache." });
            ExpeditionSimulator.Complete(state, config, launched.Expedition);
            state.LastOfflineReport = new OfflineProgressReport { AppliedSeconds = 120 };
            state.LastOfflineReport.ResourcesGained["food"] = 3;
            state.LastOfflineReport.ResourcesSpent["water"] = 2;
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
            Assert.AreEqual("Enemies Radiated Hound x1", reports.AfterActionEnemies);
            Assert.That(reports.AfterActionEvents, Does.Contain("Skill XP Survival x3"));
            Assert.That(reports.AfterActionEvents, Does.Contain("Gear Rusty Knife -1"));
            Assert.That(reports.AfterActionEvents, Does.Contain("Broken Rusty Knife"));
            Assert.That(reports.AfterActionEvents, Does.Contain("Progress Abandoned Store x1"));
            Assert.That(reports.AfterActionEvents, Does.Contain("Mara opened a cache."));
            Assert.AreEqual("Again", reports.AfterActionSendAgainButton);
            Assert.IsFalse(reports.AfterActionCanSendAgain);
            Assert.IsNotNull(reports.AfterActionSendAgainRequest);
            Assert.AreEqual("abandoned_store", reports.AfterActionSendAgainRequest.ZoneId);
            Assert.AreEqual("balanced", reports.AfterActionSendAgainRequest.PolicyId);
            CollectionAssert.AreEqual(new[] { "survivor_1" }, reports.AfterActionSendAgainRequest.SurvivorIds);
            Assert.AreEqual("Offline 2", reports.OfflineSummary);
            Assert.AreEqual("Resources Food x3, Spent Water x2", reports.OfflineResources);
            Assert.AreEqual("Completed abandoned_store", reports.OfflineCompleted);
            Assert.AreEqual("Healing Mara", reports.OfflineHealing);
            Assert.AreEqual("Warnings Mara", reports.OfflineWarnings);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void AfterActionDetailsShowUnlockAndDemoProgress()
        {
            var config = TestConfigFactory.Create();
            config.Balance.DemoCompletionConditions.Clear();
            config.Balance.DemoCompletionConditions.Add(new UnlockCondition { Type = GameConditionTypes.ZoneCompletions, Id = "abandoned_store", Value = 2 });
            var state = GameStateFactory.CreateNew(config, 0);
            state.Zones["abandoned_store"].Completions = 1;
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

            ExpeditionSimulator.Complete(state, config, launched.Expedition);

            var reports = CampDashboardTextFormatter.BuildReports(state, config, catalog);

            Assert.That(reports.AfterActionEvents, Does.Contain("Progress Abandoned Store x2"));
            Assert.That(reports.AfterActionEvents, Does.Contain("Unlocked Dry Suburb"));
            Assert.That(reports.AfterActionEvents, Does.Contain("Demo Abandoned Store"));

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

        private static CampAlertPresentation FindAlert(IEnumerable<CampAlertPresentation> alerts, string id)
        {
            foreach (var alert in alerts)
            {
                if (alert != null && alert.Id == id)
                {
                    return alert;
                }
            }

            return null;
        }

        private static void FillCappedResources(GameState state, GameConfigSnapshot config)
        {
            foreach (var resource in config.Resources.Values)
            {
                if (!resource.HasCap) continue;
                int cap;
                if (!state.ResourceCaps.TryGetValue(resource.Id, out cap)) cap = resource.StartCap;
                state.Resources[resource.Id] = cap;
            }
        }

        private static TextMeshProUGUI CreateText(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            return go.AddComponent<TextMeshProUGUI>();
        }

        private static Toggle CreateToggle(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var toggle = go.AddComponent<Toggle>();
            toggle.targetGraphic = go.GetComponent<Image>();
            return toggle;
        }

        private static Button CreateButton(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            var button = go.AddComponent<Button>();
            button.targetGraphic = go.GetComponent<Image>();
            return button;
        }

        private static Image CreateImage(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(Image));
            go.transform.SetParent(parent, false);
            return go.GetComponent<Image>();
        }

        private static RawImage CreateRawImage(string name, Transform parent)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(RawImage));
            go.transform.SetParent(parent, false);
            return go.GetComponent<RawImage>();
        }

        private static void SetPrivateField(object target, string fieldName, object value)
        {
            target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .SetValue(target, value);
        }

        private static T GetPrivateField<T>(object target, string fieldName)
        {
            return (T)target.GetType()
                .GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic)
                .GetValue(target);
        }

        private static void InvokePrivate(object target, string methodName)
        {
            target.GetType()
                .GetMethod(methodName, BindingFlags.Instance | BindingFlags.NonPublic)
                .Invoke(target, null);
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
            catalog.IdleSurvivorsAlertTitleFormat = "IDLE {0}";
            catalog.IdleSurvivorsAlertBodyFormat = "{0} ready";
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
            catalog.AfterActionSkillXpFormat = "Skill XP {0}";
            catalog.AfterActionDurabilityFormat = "Gear {0}";
            catalog.AfterActionDurabilityItemFormat = "{0} -{1}";
            catalog.AfterActionBrokenItemsFormat = "Broken {0}";
            catalog.AfterActionProgressFormat = "Progress {0}";
            catalog.AfterActionUnlockedFormat = "Unlocked {0}";
            catalog.AfterActionDemoProgressFormat = "Demo {0}";
            catalog.AfterActionSendAgainButton = "Again";
            catalog.OfflineReportPanelTitle = "Offline";
            catalog.OfflineReportSummaryFormat = "Offline {0}";
            catalog.OfflineReportResourcesFormat = "Resources {0}";
            catalog.OfflineReportResourcesSpentFormat = "Spent {0}";
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
            catalog.SettingsManualSaveTitle = "Manual save";
            catalog.SettingsManualSaveBody = "Writes now";
            catalog.SettingsManualSaveButton = "Save now";
            catalog.ReportNoneLabel = "None";
            catalog.ReportListSeparator = ", ";
            catalog.ReportCountFormat = "{0} x{1}";
            catalog.SurvivorSkillLabels.Add(new SurvivorSkillUiEntry { Id = "survival", Label = "Survival" });
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
