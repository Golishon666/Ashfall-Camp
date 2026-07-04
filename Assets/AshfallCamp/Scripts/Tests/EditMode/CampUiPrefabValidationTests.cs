using System.Collections.Generic;
using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Composition;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using AshfallCamp.Presentation;
using Cysharp.Threading.Tasks;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class CampUiPrefabValidationTests
    {
        private const string DashboardPrefabPath = "Assets/AshfallCamp/Prefabs/UI/PF_CampDashboard.prefab";
        private const string CampUiCatalogPath = "Assets/AshfallCamp/UI/CampUiCatalog.asset";
        private const string GameConfigDatabasePath = "Assets/AshfallCamp/Configs/GameConfigDatabase.asset";

        private static readonly string[] UiPrefabPaths =
        {
            "Assets/AshfallCamp/Prefabs/UI/BottomNav.prefab",
            "Assets/AshfallCamp/Prefabs/UI/BuildingCard.prefab",
            "Assets/AshfallCamp/Prefabs/UI/ExpeditionsScreen.prefab",
            DashboardPrefabPath,
            "Assets/AshfallCamp/Prefabs/UI/RadioScreen.prefab",
            "Assets/AshfallCamp/Prefabs/UI/ReportsScreen.prefab",
            "Assets/AshfallCamp/Prefabs/UI/SurvivorsScreen.prefab",
            "Assets/AshfallCamp/Prefabs/UI/TopBar.prefab",
            "Assets/AshfallCamp/Prefabs/UI/WorkshopScreen.prefab"
        };

        [Test]
        public void ProductionUiPrefabsHaveNoMissingPresentationReferences()
        {
            var failures = new List<string>();

            foreach (var prefabPath in UiPrefabPaths)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (root == null)
                {
                    failures.Add(prefabPath + " is missing.");
                    continue;
                }

                var behaviours = root.GetComponentsInChildren<MonoBehaviour>(true);
                foreach (var behaviour in behaviours)
                {
                    if (behaviour == null)
                    {
                        failures.Add(prefabPath + " contains a missing MonoBehaviour script.");
                        continue;
                    }

                    var type = behaviour.GetType();
                    if (type.Namespace != typeof(CampDashboardView).Namespace) continue;

                    CollectMissingReferences(prefabPath, behaviour, type.Name, failures);
                }
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void ProductionUiPrefabsDoNotExposeTechnicalPlaceholderText()
        {
            var failures = new List<string>();

            foreach (var prefabPath in UiPrefabPaths)
            {
                var root = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
                if (root == null)
                {
                    failures.Add(prefabPath + " is missing.");
                    continue;
                }

                var labels = root.GetComponentsInChildren<TextMeshProUGUI>(true);
                foreach (var label in labels)
                {
                    if (label == null) continue;
                    var text = label.text ?? string.Empty;
                    var lower = text.ToLowerInvariant();
                    if (lower.Contains("placeholder") ||
                        lower.Contains("todo") ||
                        lower.Contains("lorem") ||
                        lower.Contains("generator"))
                    {
                        failures.Add(prefabPath + " " + label.name + " exposes technical text: " + text);
                    }
                }
            }

            Assert.IsEmpty(failures, string.Join("\n", failures));
        }

        [Test]
        public void CampDashboardPrefabNavItemsUseCatalogIcons()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                var dashboard = instance.GetComponent<CampDashboardView>();

                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                dashboard.Render(state, config);

                var navIcons = ReadNavIcons(dashboard);
                foreach (var navItem in catalog.NavItems)
                {
                    if (navItem == null || string.IsNullOrWhiteSpace(navItem.Id)) continue;

                    Assert.NotNull(navItem.Icon, "Catalog nav icon is missing for nav id: " + navItem.Id);
                    Assert.That(navIcons.ContainsKey(navItem.Id), Is.True, "Missing bottom nav icon binding for nav id: " + navItem.Id);
                    Assert.NotNull(navIcons[navItem.Id], "Bottom nav icon is null for nav id: " + navItem.Id);
                    Assert.AreSame(navItem.Icon, navIcons[navItem.Id].texture, "Bottom nav icon texture mismatch for nav id: " + navItem.Id);
                    Assert.That(navIcons[navItem.Id].gameObject.activeSelf, Is.True, "Bottom nav icon is inactive for nav id: " + navItem.Id);
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabSurvivorCardsUseCatalogPortraits()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                var dashboard = instance.GetComponent<CampDashboardView>();
                var survivors = instance.GetComponentInChildren<SurvivorsPanelView>(true);

                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");
                Assert.NotNull(survivors, DashboardPrefabPath + " does not contain SurvivorsPanelView.");

                dashboard.SetCatalog(catalog);
                dashboard.Render(state, config);

                var expectedCard = CampDashboardTextFormatter.BuildSurvivorCards(state, catalog)[0];
                var expectedDetail = CampDashboardTextFormatter.BuildSurvivorDetail(state.Survivors[0], state, config, catalog);
                var bindings = ReadSurvivorPortraitBindings(survivors);

                Assert.NotNull(expectedCard.Portrait, "Catalog survivor portrait is missing for starter survivor.");
                Assert.NotNull(bindings.CardPortrait, "Starter survivor card portrait binding is missing.");
                Assert.NotNull(bindings.CardAvatar, "Starter survivor card avatar binding is missing.");
                Assert.AreSame(expectedCard.Portrait, bindings.CardPortrait.texture);
                Assert.That(bindings.CardPortrait.gameObject.activeSelf, Is.True);
                Assert.That(bindings.CardAvatar.gameObject.activeSelf, Is.False);

                Assert.NotNull(expectedDetail.Portrait, "Catalog detail portrait is missing for starter survivor.");
                Assert.NotNull(bindings.DetailPortrait, "Survivor detail portrait binding is missing.");
                Assert.AreSame(expectedDetail.Portrait, bindings.DetailPortrait.texture);
                Assert.That(bindings.DetailPortrait.gameObject.activeSelf, Is.True);

                var artworkBindings = ReadSurvivorArtworkBindings(survivors);
                AssertSurvivorArtwork(catalog.SurvivorRosterCardTexture, artworkBindings.FirstCard, "first survivor card");
                AssertSurvivorArtwork(catalog.SurvivorDetailPanelTexture, artworkBindings.Detail, "survivor detail");

                state.Survivors.Clear();
                dashboard.Render(state, config);
                AssertSurvivorArtwork(catalog.SurvivorsEmptyPanelTexture, artworkBindings.Empty, "survivor empty state");
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabSwitchesEveryCatalogNavScreen()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                var dashboard = instance.GetComponent<CampDashboardView>();

                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                dashboard.Render(state, config);

                var screenRoots = ReadScreenRoots(dashboard);
                var navButtons = ReadNavButtons(dashboard);

                foreach (var navItem in catalog.NavItems)
                {
                    if (navItem == null || string.IsNullOrWhiteSpace(navItem.Id)) continue;

                    Assert.That(screenRoots.ContainsKey(navItem.Id), Is.True, "Missing screen binding for nav id: " + navItem.Id);
                    Assert.That(navButtons.ContainsKey(navItem.Id), Is.True, "Missing bottom nav button binding for nav id: " + navItem.Id);

                    if (!IsOnlyActiveScreen(screenRoots, navItem.Id))
                    {
                        Assert.NotNull(navButtons[navItem.Id], "Bottom nav button is null for nav id: " + navItem.Id);
                        Assert.That(navButtons[navItem.Id].interactable, Is.True, "Inactive nav button is not interactable: " + navItem.Id);
                        navButtons[navItem.Id].onClick.Invoke();
                    }

                    AssertOnlyScreenActive(screenRoots, navItem.Id);
                }
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabLaunchButtonStartsExpeditionThroughPresenter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);
            CampHudPresenter presenter = null;
            GameStateStore store = null;

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                store = new GameStateStore();
                store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();

                var staticConfig = new StaticConfigProvider(config);
                var repository = new FakeSaveRepository(state);
                var saveLoad = new SaveLoadUseCase(repository, store, staticConfig);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                presenter = CreatePresenter(dashboard, store, staticConfig, repository, saveLoad);
                presenter.Start();

                var navButtons = ReadNavButtons(dashboard);
                var expeditionsNavId = FindNavIdByLabel(catalog, catalog.ExpeditionScreenTitle);
                Assert.That(navButtons.ContainsKey(expeditionsNavId), Is.True, "Missing expeditions nav binding.");
                Assert.NotNull(navButtons[expeditionsNavId], "Expeditions nav button is null.");
                Assert.That(navButtons[expeditionsNavId].interactable, Is.True, "Expeditions nav button is not interactable.");
                navButtons[expeditionsNavId].onClick.Invoke();

                var launchButton = ReadExpeditionLaunchButton(dashboard);
                Assert.NotNull(launchButton, "Expedition launch button is not assigned.");
                Assert.That(launchButton.interactable, Is.True, "Expedition launch button is not interactable for a new game state.");

                var foodBefore = ResourceSystem.GetAmount(state, config.Balance.ExpeditionFoodResourceId);
                var waterBefore = ResourceSystem.GetAmount(state, config.Balance.ExpeditionWaterResourceId);

                launchButton.onClick.Invoke();

                var current = store.State.CurrentValue;
                Assert.AreEqual(1, current.Expeditions.Count);
                Assert.AreEqual(ExpeditionStatus.Active, current.Expeditions[0].Status);
                Assert.AreEqual(SurvivorActivityState.OnExpedition, current.Survivors[0].State);
                Assert.AreEqual(current.Expeditions[0].Id, current.Survivors[0].CurrentExpeditionId);
                var launched = current.Expeditions[0];
                var zone = config.Zones[launched.ZoneId];
                var policy = config.Policies[launched.PolicyId];
                var cost = ExpeditionValidator.CalculateCost(config, zone, policy, launched.SurvivorIds.Count);
                Assert.AreEqual(foodBefore - CostFor(cost, config.Balance.ExpeditionFoodResourceId), ResourceSystem.GetAmount(current, config.Balance.ExpeditionFoodResourceId));
                Assert.AreEqual(waterBefore - CostFor(cost, config.Balance.ExpeditionWaterResourceId), ResourceSystem.GetAmount(current, config.Balance.ExpeditionWaterResourceId));
                Assert.NotNull(repository.SavedState, "Launching an expedition should critical-save the updated game state.");
                Assert.AreSame(current, repository.SavedState);
            }
            finally
            {
                presenter?.Dispose();
                store?.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabExpeditionSetupLaunchesSelectedRoutePolicyAndSquad()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);
            CampHudPresenter presenter = null;
            GameStateStore store = null;

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                EnsureTwoSurvivorSquad(state, config);
                var keptSurvivor = AddFirstRecruitableSurvivor(state, config);
                var removedSurvivorId = state.Survivors[0].Id;

                var initialScreen = CampDashboardTextFormatter.BuildExpeditionScreen(state, config, catalog, string.Empty);
                string selectedZoneId;
                var selectedZoneIndex = FindAlternateUnlockedZoneIndex(state, config, initialScreen.SelectedZoneId, out selectedZoneId);
                string selectedPolicyId;
                var selectedPolicyIndex = FindAlternatePolicyIndex(config, initialScreen.SelectedPolicyId, out selectedPolicyId);
                var selectedZone = config.Zones[selectedZoneId];
                var selectedPolicy = config.Policies[selectedPolicyId];
                var selectedCost = ExpeditionValidator.CalculateCost(config, selectedZone, selectedPolicy, 1);
                EnsureCanAfford(state, selectedCost);

                store = new GameStateStore();
                store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();

                var staticConfig = new StaticConfigProvider(config);
                var repository = new CountingSaveRepository(state);
                var saveLoad = new SaveLoadUseCase(repository, store, staticConfig);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                presenter = CreatePresenter(dashboard, store, staticConfig, repository, saveLoad);
                presenter.Start();

                var navButtons = ReadNavButtons(dashboard);
                var expeditionsNavId = FindNavIdByLabel(catalog, catalog.ExpeditionScreenTitle);
                Assert.That(navButtons.ContainsKey(expeditionsNavId), Is.True, "Missing expeditions nav binding.");
                Assert.NotNull(navButtons[expeditionsNavId], "Expeditions nav button is null.");
                Assert.That(navButtons[expeditionsNavId].interactable, Is.True, "Expeditions nav button is not interactable.");
                navButtons[expeditionsNavId].onClick.Invoke();

                var routeButton = ReadExpeditionRouteButton(dashboard, selectedZoneIndex);
                Assert.NotNull(routeButton, "Selected route button is not assigned.");
                Assert.That(routeButton.interactable, Is.True, "Selected route button is not interactable.");
                routeButton.onClick.Invoke();

                var policyButton = ReadExpeditionPolicyButton(dashboard, selectedPolicyIndex);
                Assert.NotNull(policyButton, "Selected policy button is not assigned.");
                Assert.That(policyButton.interactable, Is.True, "Selected policy button is not interactable.");
                policyButton.onClick.Invoke();

                var removedSurvivorButton = ReadExpeditionSquadMemberButton(dashboard, 0);
                Assert.NotNull(removedSurvivorButton, "Squad member button is not assigned.");
                Assert.That(removedSurvivorButton.interactable, Is.True, "Squad member button is not interactable.");
                removedSurvivorButton.onClick.Invoke();

                var resourcesBefore = CopyResources(store.State.CurrentValue.Resources);
                var launchButton = ReadExpeditionLaunchButton(dashboard);
                Assert.NotNull(launchButton, "Expedition launch button is not assigned.");
                Assert.That(launchButton.interactable, Is.True, "Expedition launch button is not interactable after selecting route, policy, and squad.");

                var expeditionCountBefore = store.State.CurrentValue.Expeditions.Count;
                launchButton.onClick.Invoke();
                if (store.State.CurrentValue.Expeditions.Count == expeditionCountBefore)
                {
                    Assert.That(launchButton.interactable, Is.True, "Risk confirmation did not keep launch button interactable.");
                    launchButton.onClick.Invoke();
                }

                var current = store.State.CurrentValue;
                Assert.AreEqual(expeditionCountBefore + 1, current.Expeditions.Count);
                var launched = current.Expeditions[current.Expeditions.Count - 1];
                Assert.AreEqual(selectedZoneId, launched.ZoneId);
                Assert.AreEqual(selectedPolicyId, launched.PolicyId);
                CollectionAssert.AreEqual(new[] { keptSurvivor.Id }, launched.SurvivorIds);
                AssertCostSpent(resourcesBefore, current.Resources, selectedCost);

                var removedSurvivor = FindSurvivor(current, removedSurvivorId);
                var sentSurvivor = FindSurvivor(current, keptSurvivor.Id);
                Assert.NotNull(removedSurvivor, "Deselected survivor is missing after launch.");
                Assert.NotNull(sentSurvivor, "Selected survivor is missing after launch.");
                Assert.AreEqual(SurvivorActivityState.Idle, removedSurvivor.State);
                Assert.AreEqual(SurvivorActivityState.OnExpedition, sentSurvivor.State);
                Assert.AreEqual(launched.Id, sentSurvivor.CurrentExpeditionId);
                Assert.AreEqual(1, repository.SaveCount, "Launching selected expedition should critical-save once.");
                Assert.AreSame(current, repository.SavedState);
            }
            finally
            {
                presenter?.Dispose();
                store?.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabRadioButtonsBroadcastAndRecruitThroughPresenter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);
            CampHudPresenter presenter = null;
            GameStateStore store = null;

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                EnsureCanRecruitOneMoreSurvivor(state, config);
                EnsureCanAffordRecruitmentBroadcast(state, config);
                store = new GameStateStore();
                store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();

                var staticConfig = new StaticConfigProvider(config);
                var repository = new FakeSaveRepository(state);
                var saveLoad = new SaveLoadUseCase(repository, store, staticConfig);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                presenter = CreatePresenter(dashboard, store, staticConfig, repository, saveLoad);
                presenter.Start();

                var navButtons = ReadNavButtons(dashboard);
                var radioNavId = FindNavIdByLabel(catalog, catalog.RadioScreenTitle);
                Assert.That(navButtons.ContainsKey(radioNavId), Is.True, "Missing radio nav binding.");
                Assert.NotNull(navButtons[radioNavId], "Radio nav button is null.");
                Assert.That(navButtons[radioNavId].interactable, Is.True, "Radio nav button is not interactable.");
                navButtons[radioNavId].onClick.Invoke();

                var broadcastButton = ReadRadioBroadcastButton(dashboard);
                Assert.NotNull(broadcastButton, "Radio broadcast button is not assigned.");
                Assert.That(broadcastButton.interactable, Is.True, "Radio broadcast button is not interactable for an affordable new game state.");

                var recruitmentCost = RecruitmentSystem.CalculateCost(state, config);
                var scrapBefore = ResourceSystem.GetAmount(state, config.Balance.RecruitmentScrapResourceId);
                var foodBefore = ResourceSystem.GetAmount(state, config.Balance.RecruitmentFoodResourceId);
                var waterBefore = ResourceSystem.GetAmount(state, config.Balance.RecruitmentWaterResourceId);

                broadcastButton.onClick.Invoke();

                var afterBroadcast = store.State.CurrentValue;
                Assert.Greater(afterBroadcast.Recruitment.PendingCandidateIds.Count, 0);
                Assert.AreEqual(scrapBefore - CostFor(recruitmentCost, config.Balance.RecruitmentScrapResourceId), ResourceSystem.GetAmount(afterBroadcast, config.Balance.RecruitmentScrapResourceId));
                Assert.AreEqual(foodBefore - CostFor(recruitmentCost, config.Balance.RecruitmentFoodResourceId), ResourceSystem.GetAmount(afterBroadcast, config.Balance.RecruitmentFoodResourceId));
                Assert.AreEqual(waterBefore - CostFor(recruitmentCost, config.Balance.RecruitmentWaterResourceId), ResourceSystem.GetAmount(afterBroadcast, config.Balance.RecruitmentWaterResourceId));
                Assert.AreSame(afterBroadcast, repository.SavedState);

                var recruitButton = ReadFirstInteractableRecruitButton(dashboard);
                Assert.NotNull(recruitButton, "No recruit button became interactable after broadcast.");

                var survivorCountBeforeRecruit = afterBroadcast.Survivors.Count;
                recruitButton.onClick.Invoke();

                var afterRecruit = store.State.CurrentValue;
                Assert.AreEqual(survivorCountBeforeRecruit + 1, afterRecruit.Survivors.Count);
                Assert.AreEqual(0, afterRecruit.Recruitment.PendingCandidateIds.Count);
                Assert.AreEqual(1, afterRecruit.Statistics.SurvivorsRecruited);
                Assert.AreSame(afterRecruit, repository.SavedState);
            }
            finally
            {
                presenter?.Dispose();
                store?.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabRadioCandidatesUseCatalogPortraits()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                EnsureCanRecruitOneMoreSurvivor(state, config);
                EnsureCanAffordRecruitmentBroadcast(state, config);

                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                dashboard.Render(state, config);

                var artwork = ReadRadioArtworkBindings(dashboard);
                AssertRadioArtwork(catalog.RadioIntelPanelTexture, artwork.Intel, "intel panel");
                AssertRadioArtwork(catalog.RadioBroadcastPanelTexture, artwork.Broadcast, "broadcast panel");
                AssertRadioArtwork(catalog.RadioEmptyPanelTexture, artwork.Empty, "empty panel");

                var broadcast = RecruitmentSystem.Broadcast(state, config, new BroadcastRecruitmentRequest { Seed = 1 });
                Assert.IsTrue(broadcast.Validation.IsValid, string.Join(", ", broadcast.Validation.Errors));
                dashboard.Render(state, config);

                var expected = CampDashboardTextFormatter.BuildRadioScreen(state, config, catalog).Candidates[0];
                var bindings = ReadFirstRadioCandidatePortraitBindings(dashboard);

                Assert.NotNull(expected.Portrait, "Catalog radio candidate portrait is missing for " + expected.CandidateId + ".");
                Assert.NotNull(bindings.Portrait, "Radio candidate portrait binding is missing.");
                Assert.NotNull(bindings.Avatar, "Radio candidate avatar binding is missing.");
                Assert.AreSame(expected.Portrait, bindings.Portrait.texture);
                Assert.That(bindings.Portrait.gameObject.activeSelf, Is.True);
                Assert.That(bindings.Avatar.gameObject.activeSelf, Is.False);
                AssertRadioArtwork(catalog.RadioCandidateCardTexture, artwork.FirstCandidateCard, "first candidate card");
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabReportsUseCatalogArtwork()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                var launched = ExpeditionLauncher.Launch(state, config, new LaunchExpeditionRequest
                {
                    ZoneId = "abandoned_store",
                    PolicyId = "balanced",
                    Seed = 123,
                    NowUnixMs = 1000,
                    ConfirmWarnings = true,
                    SurvivorIds = new List<string> { state.Survivors[0].Id }
                });
                Assert.IsTrue(launched.Validation.IsValid, string.Join(", ", launched.Validation.Errors));

                launched.Expedition.ElapsedSeconds = 125;
                launched.Expedition.AccumulatedLoot["scrap"] = 4;
                ExpeditionSimulator.Complete(state, config, launched.Expedition);
                state.CampEvents.Add(new CampEventState
                {
                    Id = "event_report_test",
                    EventId = GameEventIds.SurvivorJoined,
                    SubjectId = state.Survivors[0].Id,
                    SubjectName = state.Survivors[0].Name,
                    DetailId = state.Survivors[0].BackgroundId,
                    AtUnixMs = 2000
                });
                state.LastOfflineReport = new OfflineProgressReport
                {
                    AppliedSeconds = config.Balance.OfflineReportMinimumSeconds + 60
                };
                state.LastOfflineReport.ResourcesGained["food"] = 1;
                state.LastOfflineReport.CompletedExpeditionIds.Add(launched.Expedition.Id);

                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                dashboard.Render(state, config);

                var bindings = ReadReportsArtworkBindings(dashboard);
                AssertReportArtwork(catalog.ReportsEmptyPanelTexture, bindings.Empty, "empty");
                AssertReportArtwork(catalog.ReportsAfterActionPanelTexture, bindings.AfterAction, "after-action");
                AssertReportArtwork(catalog.ReportsCampEventPanelTexture, bindings.CampEvent, "camp event");
                AssertReportArtwork(catalog.ReportsOfflinePanelTexture, bindings.Offline, "offline");
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabExpeditionsUseCatalogArtwork()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                var zoneId = FindFirstUnlockedZoneId(state, config);
                var policyId = ResolvePolicyId(config, string.Empty);
                var zone = config.Zones[zoneId];
                var policy = config.Policies[policyId];
                PrepareZoneLaunchRequirements(state, config, zone);
                EnsureCanAfford(state, ExpeditionValidator.CalculateCost(config, zone, policy, 1));

                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                dashboard.Render(state, config);

                var bindings = ReadExpeditionArtworkBindings(dashboard);
                AssertExpeditionArtwork(catalog.ExpeditionDetailPanelTexture, bindings.Detail, "selected detail");

                var screen = CampDashboardTextFormatter.BuildExpeditionScreen(state, config, catalog, string.Empty);
                Assert.That(screen.Routes.Count, Is.GreaterThan(0), "Expedition screen has no route presentations.");
                var routeArtwork = ReadExpeditionRouteArtworkBindings(dashboard, 0);
                AssertExpeditionArtwork(screen.Routes[0].Thumbnail, routeArtwork.Thumbnail, "first route thumbnail");
                AssertExpeditionArtwork(screen.Routes[0].RiskBadge, routeArtwork.RiskBadge, "first route risk badge");

                Assert.That(screen.SquadMembers.Count, Is.GreaterThan(0), "Expedition screen has no squad member presentations.");
                Assert.That(screen.Policies.Count, Is.GreaterThan(0), "Expedition screen has no policy presentations.");
                var setupArtwork = ReadExpeditionSetupCardArtworkBindings(dashboard);
                AssertExpeditionArtwork(catalog.ExpeditionSquadMemberCardTexture, setupArtwork.FirstSquadMember, "first squad member card");
                AssertExpeditionArtwork(catalog.ExpeditionPolicyCardTexture, setupArtwork.FirstPolicy, "first policy card");

                var launched = ExpeditionLauncher.Launch(state, config, new LaunchExpeditionRequest
                {
                    ZoneId = zoneId,
                    PolicyId = policyId,
                    Seed = 123,
                    NowUnixMs = 1000,
                    ConfirmWarnings = true,
                    SurvivorIds = new List<string> { state.Survivors[0].Id }
                });
                Assert.IsTrue(launched.Validation.IsValid, string.Join(", ", launched.Validation.Errors));

                launched.Expedition.Progress = 0.42;
                dashboard.Render(state, config);

                AssertExpeditionArtwork(catalog.ExpeditionMonitorPanelTexture, bindings.Monitor, "monitor");
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabResourceBarUsesCatalogIcons()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                dashboard.Render(state, config);

                var entry = FindResourceUiEntry(catalog, "survivors");
                var icon = ReadResourceIcon(dashboard, "survivors");
                Assert.NotNull(entry.Icon, "Catalog resource icon is missing for survivors.");
                Assert.NotNull(icon, "Resource icon binding is missing for survivors.");
                Assert.AreSame(entry.Icon, icon.texture, "Resource icon texture mismatch for survivors.");
                Assert.That(icon.gameObject.activeSelf, Is.True, "Resource icon object is inactive for survivors.");
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabCampOverviewUsesCatalogArtwork()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                var buildingId = FindFirstUpgradeableUiBuildingId(state, config, catalog);

                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                dashboard.Render(state, config);

                AssertCampArtwork(catalog.CampStatusPanelTexture, ReadCampStatusArtwork(dashboard), "camp status panel");
                AssertCampArtwork(catalog.CampAlertCardTexture, ReadFirstAlertArtwork(dashboard), "first alert card");
                var buildingVisuals = ReadBuildingCardVisualBindings(dashboard, buildingId);
                var buildingEntry = FindBuildingUiEntry(catalog, buildingId);
                AssertCampArtwork(catalog.BuildingCardTexture, buildingVisuals.CardArtwork, "building card");
                Assert.NotNull(buildingEntry.Icon, "Catalog building icon is missing for " + buildingId + ".");
                Assert.NotNull(buildingVisuals.Icon, "Building icon binding is missing for " + buildingId + ".");
                Assert.AreSame(buildingEntry.Icon, buildingVisuals.Icon.texture, "Building icon texture mismatch for " + buildingId + ".");
                Assert.That(buildingVisuals.Icon.gameObject.activeSelf, Is.True, "Building icon object is inactive for " + buildingId + ".");
                Assert.NotNull(buildingVisuals.ImageLetter, "Building fallback letter binding is missing for " + buildingId + ".");
                Assert.That(buildingVisuals.ImageLetter.gameObject.activeSelf, Is.False, "Building fallback letter should be inactive when icon exists for " + buildingId + ".");
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabBuildingUpgradeButtonUpgradesThroughPresenter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);
            CampHudPresenter presenter = null;
            GameStateStore store = null;

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                var targetBuildingId = FindFirstUpgradeableUiBuildingId(state, config, catalog);
                var definition = config.Buildings[targetBuildingId];
                var before = state.Buildings[targetBuildingId];
                var nextLevel = BuildingSystem.GetLevel(definition, before.Level + 1);
                Assert.NotNull(nextLevel, "Target building has no next level: " + targetBuildingId);
                EnsureCanAfford(state, nextLevel.Cost);

                store = new GameStateStore();
                store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();

                var staticConfig = new StaticConfigProvider(config);
                var repository = new FakeSaveRepository(state);
                var saveLoad = new SaveLoadUseCase(repository, store, staticConfig);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                presenter = CreatePresenter(dashboard, store, staticConfig, repository, saveLoad);
                presenter.Start();

                var upgradeButton = ReadBuildingUpgradeButton(dashboard, targetBuildingId);
                Assert.NotNull(upgradeButton, "Upgrade button is not assigned for building: " + targetBuildingId);
                Assert.That(upgradeButton.interactable, Is.True, "Upgrade button is not interactable for prepared building: " + targetBuildingId);

                var resourcesBefore = CopyResources(state.Resources);
                var levelBefore = before.Level;

                upgradeButton.onClick.Invoke();

                var current = store.State.CurrentValue;
                var upgraded = current.Buildings[targetBuildingId];
                Assert.AreEqual(nextLevel.Level, upgraded.Level);
                Assert.Greater(upgraded.Level, levelBefore);
                AssertCostSpent(resourcesBefore, current.Resources, nextLevel.Cost);
                Assert.NotNull(repository.SavedState, "Upgrading a building should critical-save the updated game state.");
                Assert.AreSame(current, repository.SavedState);
            }
            finally
            {
                presenter?.Dispose();
                store?.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabWorkshopButtonsRepairAndEquipThroughPresenter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);
            CampHudPresenter presenter = null;
            GameStateStore store = null;

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                PrepareWorkshopAccess(state, config);

                var repairTarget = state.Inventory[0];
                repairTarget.Durability = System.Math.Max(0, repairTarget.MaxDurability - System.Math.Max(1, config.Balance.WorkshopRepairDurabilityBlock));
                var repairRequest = new RepairItemRequest { ItemUid = repairTarget.Uid };
                var repairCost = WorkshopSystem.CalculateRepairCost(state, config, repairTarget.Uid);
                EnsureCanAfford(state, repairCost);
                Assert.That(WorkshopSystem.ValidateRepair(state, config, repairRequest).IsValid, Is.True, "Prepared repair target is not repairable.");

                var equipTarget = CreateStoredEquipItem(state, config);
                var survivor = state.Survivors[0];
                var equipDefinition = config.Items[equipTarget.ItemId];
                var equipRequest = new EquipItemRequest { SurvivorId = survivor.Id, ItemUid = equipTarget.Uid };
                Assert.That(WorkshopSystem.ValidateEquip(state, config, equipRequest).IsValid, Is.True, "Prepared equip target is not equippable.");

                store = new GameStateStore();
                store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();

                var staticConfig = new StaticConfigProvider(config);
                var repository = new FakeSaveRepository(state);
                var saveLoad = new SaveLoadUseCase(repository, store, staticConfig);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                presenter = CreatePresenter(dashboard, store, staticConfig, repository, saveLoad);
                presenter.Start();

                var navButtons = ReadNavButtons(dashboard);
                var workshopNavId = FindNavIdByLabel(catalog, catalog.WorkshopScreenTitle);
                Assert.That(navButtons.ContainsKey(workshopNavId), Is.True, "Missing workshop nav binding.");
                Assert.NotNull(navButtons[workshopNavId], "Workshop nav button is null.");
                Assert.That(navButtons[workshopNavId].interactable, Is.True, "Workshop nav button is not interactable.");
                navButtons[workshopNavId].onClick.Invoke();

                var repairButton = ReadWorkshopRepairButton(dashboard, 0);
                Assert.NotNull(repairButton, "Workshop repair button is not assigned.");
                Assert.That(repairButton.interactable, Is.True, "Workshop repair button is not interactable for prepared item.");

                var resourcesBeforeRepair = CopyResources(state.Resources);
                repairButton.onClick.Invoke();

                var afterRepair = store.State.CurrentValue;
                var repairedItem = WorkshopSystem.FindItem(afterRepair, repairTarget.Uid);
                Assert.NotNull(repairedItem, "Repaired item is missing from inventory.");
                Assert.AreEqual(repairedItem.MaxDurability, repairedItem.Durability);
                AssertCostSpent(resourcesBeforeRepair, afterRepair.Resources, repairCost);
                Assert.AreSame(afterRepair, repository.SavedState, "Repairing an item should critical-save the updated game state.");

                var equipButton = ReadWorkshopEquipButton(dashboard, 1);
                Assert.NotNull(equipButton, "Workshop equip button is not assigned.");
                Assert.That(equipButton.interactable, Is.True, "Workshop equip button is not interactable for prepared item.");

                equipButton.onClick.Invoke();

                var afterEquip = store.State.CurrentValue;
                var equippedSurvivor = FindSurvivor(afterEquip, survivor.Id);
                var equippedItem = WorkshopSystem.FindItem(afterEquip, equipTarget.Uid);
                Assert.NotNull(equippedSurvivor, "Equipped survivor is missing.");
                Assert.NotNull(equippedItem, "Equipped item is missing from inventory.");
                Assert.AreEqual(equipTarget.Uid, GetEquippedUid(equippedSurvivor, equipDefinition.Slot));
                Assert.AreEqual(survivor.Id, equippedItem.EquippedBySurvivorId);
                Assert.AreSame(afterEquip, repository.SavedState, "Equipping an item should critical-save the updated game state.");
            }
            finally
            {
                presenter?.Dispose();
                store?.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabWorkshopUsesCatalogArtwork()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                Assert.That(state.Inventory.Count, Is.GreaterThan(0), "New game state should include a workshop item.");

                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                dashboard.Render(state, config);

                var bindings = ReadWorkshopArtworkBindings(dashboard);
                AssertWorkshopArtwork(catalog.WorkshopEmptyPanelTexture, bindings.Empty, "empty");
                AssertWorkshopArtwork(catalog.WorkshopItemTileTexture, bindings.FirstItemTile, "first item tile");
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabUseMedicineButtonHealsThroughPresenter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);
            CampHudPresenter presenter = null;
            GameStateStore store = null;

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                PrepareHealingAccess(state, config);
                var survivor = state.Survivors[0];
                HealingSystem.ApplyWound(state, config, survivor.Id);
                CapWoundRemainingToMedicineUse(survivor, config);
                var medicineCost = HealingSystem.CalculateMedicineCost(config);
                EnsureCanAfford(state, medicineCost);
                Assert.That(
                    HealingSystem.ValidateUseMedicine(state, config, new UseMedicineRequest { SurvivorId = survivor.Id }).IsValid,
                    Is.True,
                    "Prepared survivor cannot use medicine.");

                store = new GameStateStore();
                store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();

                var staticConfig = new StaticConfigProvider(config);
                var repository = new FakeSaveRepository(state);
                var saveLoad = new SaveLoadUseCase(repository, store, staticConfig);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                presenter = CreatePresenter(dashboard, store, staticConfig, repository, saveLoad);
                presenter.Start();

                var navButtons = ReadNavButtons(dashboard);
                var survivorsNavId = FindNavIdByLabel(catalog, catalog.SurvivorsScreenTitle);
                Assert.That(navButtons.ContainsKey(survivorsNavId), Is.True, "Missing survivors nav binding.");
                Assert.NotNull(navButtons[survivorsNavId], "Survivors nav button is null.");
                Assert.That(navButtons[survivorsNavId].interactable, Is.True, "Survivors nav button is not interactable.");
                navButtons[survivorsNavId].onClick.Invoke();

                var useMedicineButton = ReadUseMedicineButton(dashboard);
                Assert.NotNull(useMedicineButton, "Use medicine button is not assigned.");
                Assert.That(useMedicineButton.gameObject.activeSelf, Is.True, "Use medicine button is not visible for a wounded survivor.");
                Assert.That(useMedicineButton.interactable, Is.True, "Use medicine button is not interactable for prepared survivor.");

                var resourcesBefore = CopyResources(state.Resources);
                useMedicineButton.onClick.Invoke();

                var current = store.State.CurrentValue;
                var healed = FindSurvivor(current, survivor.Id);
                Assert.NotNull(healed, "Healed survivor is missing.");
                Assert.AreEqual(SurvivorActivityState.Idle, healed.State);
                Assert.AreEqual(healed.MaxHealth, healed.Health);
                AssertCostSpent(resourcesBefore, current.Resources, medicineCost);
                Assert.AreSame(current, repository.SavedState, "Using medicine should critical-save the updated game state.");
            }
            finally
            {
                presenter?.Dispose();
                store?.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabSettingsToggleAndManualSaveUsePresenter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);
            CampHudPresenter presenter = null;
            GameStateStore store = null;

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                Assert.That(state.Settings.AutosaveEnabled, Is.True, "New game should start with autosave enabled.");

                store = new GameStateStore();
                store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();

                var staticConfig = new StaticConfigProvider(config);
                var repository = new CountingSaveRepository(state);
                var saveLoad = new SaveLoadUseCase(repository, store, staticConfig);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                presenter = CreatePresenter(dashboard, store, staticConfig, repository, saveLoad);
                presenter.Start();

                var navButtons = ReadNavButtons(dashboard);
                var settingsNavId = FindNavIdByLabel(catalog, catalog.SettingsScreenTitle);
                Assert.That(navButtons.ContainsKey(settingsNavId), Is.True, "Missing settings nav binding.");
                Assert.NotNull(navButtons[settingsNavId], "Settings nav button is null.");
                Assert.That(navButtons[settingsNavId].interactable, Is.True, "Settings nav button is not interactable.");
                navButtons[settingsNavId].onClick.Invoke();

                var autosaveToggle = ReadAutosaveToggle(dashboard);
                Assert.NotNull(autosaveToggle, "Autosave toggle is not assigned.");
                Assert.That(autosaveToggle.isOn, Is.True, "Autosave toggle does not reflect enabled state.");
                Assert.That(autosaveToggle.interactable, Is.True, "Autosave toggle is not interactable.");

                autosaveToggle.isOn = false;

                var afterToggle = store.State.CurrentValue;
                Assert.That(afterToggle.Settings.AutosaveEnabled, Is.False);
                Assert.AreEqual(1, repository.SaveCount);
                Assert.AreSame(afterToggle, repository.SavedState);

                var manualSaveButton = ReadManualSaveButton(dashboard);
                Assert.NotNull(manualSaveButton, "Manual save button is not assigned.");
                Assert.That(manualSaveButton.interactable, Is.True, "Manual save button is not interactable.");

                manualSaveButton.onClick.Invoke();

                Assert.AreEqual(2, repository.SaveCount);
                Assert.AreSame(store.State.CurrentValue, repository.SavedState);
            }
            finally
            {
                presenter?.Dispose();
                store?.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabSettingsUseCatalogArtwork()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                dashboard.Render(state, config);

                var bindings = ReadSettingsArtworkBindings(dashboard);
                AssertSettingsArtwork(catalog.SettingsRowTexture, bindings.AutosaveRow, "autosave row");
                AssertSettingsArtwork(catalog.SettingsToggleTrackActiveTexture, bindings.ToggleTrack, "autosave active track");
                AssertSettingsArtwork(catalog.SettingsToggleKnobTexture, bindings.ToggleKnob, "autosave knob");
                AssertSettingsArtwork(catalog.SettingsRowTexture, bindings.ManualSaveRow, "manual save row");

                state.Settings.AutosaveEnabled = false;
                dashboard.Render(state, config);

                AssertSettingsArtwork(catalog.SettingsToggleTrackInactiveTexture, bindings.ToggleTrack, "autosave inactive track");
            }
            finally
            {
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabEmergencyScavengeAlertStartsRecoveryThroughPresenter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);
            CampHudPresenter presenter = null;
            GameStateStore store = null;

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                PrepareLowEmergencyResource(state, config);
                Assert.That(
                    RecoverySystem.ValidateEmergencyScavenge(state, config, new EmergencyScavengeRequest()).IsValid,
                    Is.True,
                    "Prepared state cannot start emergency scavenge.");

                store = new GameStateStore();
                store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();

                var staticConfig = new StaticConfigProvider(config);
                var repository = new FakeSaveRepository(state);
                var saveLoad = new SaveLoadUseCase(repository, store, staticConfig);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                presenter = CreatePresenter(dashboard, store, staticConfig, repository, saveLoad);
                presenter.Start();

                var actionButton = ReadFirstInteractableAlertActionButton(dashboard);
                Assert.NotNull(actionButton, "No interactable emergency scavenge alert action button was rendered.");
                Assert.That(actionButton.gameObject.activeSelf, Is.True, "Emergency scavenge alert action button is hidden.");
                Assert.That(actionButton.interactable, Is.True, "Emergency scavenge alert action button is not interactable.");

                actionButton.onClick.Invoke();

                var current = store.State.CurrentValue;
                Assert.That(current.Recovery.EmergencyScavengeActive, Is.True);
                Assert.AreEqual(config.Balance.EmergencyScavengeDurationSeconds, current.Recovery.EmergencyScavengeRemainingSeconds);
                Assert.GreaterOrEqual(current.Recovery.EmergencyScavengeStartedAtUnixMs, 0);
                Assert.NotNull(repository.SavedState, "Starting emergency scavenge should critical-save the updated game state.");
                Assert.AreSame(current, repository.SavedState);
            }
            finally
            {
                presenter?.Dispose();
                store?.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabReportsSendAgainLaunchesExpeditionThroughPresenter()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);
            CampHudPresenter presenter = null;
            GameStateStore store = null;

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                var survivorId = state.Survivors[0].Id;
                var zoneId = FindFirstUnlockedZoneId(state, config);
                var policyId = ResolvePolicyId(config, catalog.DefaultExpeditionPolicyId);
                var sendAgainCost = ExpeditionValidator.CalculateCost(config, config.Zones[zoneId], config.Policies[policyId], 1);
                EnsureCanAfford(state, sendAgainCost);

                var completed = ExpeditionLauncher.Launch(state, config, new LaunchExpeditionRequest
                {
                    ZoneId = zoneId,
                    PolicyId = policyId,
                    Seed = 123,
                    NowUnixMs = 1000,
                    ConfirmWarnings = true,
                    SurvivorIds = new List<string> { survivorId }
                });
                Assert.That(completed.Validation.IsValid, Is.True, "Prepared expedition launch failed: " + string.Join(", ", completed.Validation.Errors));

                completed.Expedition.ElapsedSeconds = completed.Expedition.ExpectedDurationSeconds;
                ExpeditionSimulator.Complete(state, config, completed.Expedition);
                Assert.AreEqual(SurvivorActivityState.Idle, state.Survivors[0].State);
                EnsureCanAfford(state, sendAgainCost);

                store = new GameStateStore();
                store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();

                var staticConfig = new StaticConfigProvider(config);
                var repository = new FakeSaveRepository(state);
                var saveLoad = new SaveLoadUseCase(repository, store, staticConfig);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                presenter = CreatePresenter(dashboard, store, staticConfig, repository, saveLoad);
                presenter.Start();

                var navButtons = ReadNavButtons(dashboard);
                var reportsNavId = FindNavIdByLabel(catalog, catalog.ReportsScreenTitle);
                Assert.That(navButtons.ContainsKey(reportsNavId), Is.True, "Missing reports nav binding.");
                Assert.NotNull(navButtons[reportsNavId], "Reports nav button is null.");
                Assert.That(navButtons[reportsNavId].interactable, Is.True, "Reports nav button is not interactable.");
                navButtons[reportsNavId].onClick.Invoke();

                var sendAgainButton = ReadAfterActionSendAgainButton(dashboard);
                Assert.NotNull(sendAgainButton, "After action send-again button is not assigned.");
                Assert.That(sendAgainButton.interactable, Is.True, "After action send-again button is not interactable for prepared report.");

                var resourcesBefore = CopyResources(state.Resources);
                sendAgainButton.onClick.Invoke();

                var current = store.State.CurrentValue;
                Assert.AreEqual(2, current.Expeditions.Count);
                var relaunched = current.Expeditions[1];
                Assert.AreEqual(ExpeditionStatus.Active, relaunched.Status);
                Assert.AreEqual(zoneId, relaunched.ZoneId);
                Assert.AreEqual(policyId, relaunched.PolicyId);
                CollectionAssert.AreEqual(new[] { survivorId }, relaunched.SurvivorIds);
                Assert.AreEqual(SurvivorActivityState.OnExpedition, current.Survivors[0].State);
                Assert.AreEqual(relaunched.Id, current.Survivors[0].CurrentExpeditionId);
                AssertCostSpent(resourcesBefore, current.Resources, sendAgainCost);
                Assert.NotNull(repository.SavedState, "Sending again from reports should critical-save the updated game state.");
                Assert.AreSame(current, repository.SavedState);
            }
            finally
            {
                presenter?.Dispose();
                store?.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        [Test]
        public void CampDashboardPrefabLaunchedExpeditionTicksIntoAfterActionReport()
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(DashboardPrefabPath);
            var catalogAsset = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CampUiCatalogPath);
            var configDatabase = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(GameConfigDatabasePath);

            Assert.NotNull(prefab, DashboardPrefabPath + " is missing.");
            Assert.NotNull(catalogAsset, CampUiCatalogPath + " is missing.");
            Assert.NotNull(configDatabase, GameConfigDatabasePath + " is missing.");

            var instance = Object.Instantiate(prefab);
            var catalog = Object.Instantiate(catalogAsset);
            CampHudPresenter presenter = null;
            GameStateStore store = null;

            try
            {
                catalog.ScreenTransition.Enabled = false;

                var configProvider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = configProvider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, 0);
                store = new GameStateStore();
                store.MutateAsync(_ => state, CancellationToken.None).GetAwaiter().GetResult();

                var staticConfig = new StaticConfigProvider(config);
                var repository = new CountingSaveRepository(state);
                var saveLoad = new SaveLoadUseCase(repository, store, staticConfig);
                var dashboard = instance.GetComponent<CampDashboardView>();
                Assert.NotNull(dashboard, DashboardPrefabPath + " does not contain CampDashboardView.");

                dashboard.SetCatalog(catalog);
                presenter = CreatePresenter(dashboard, store, staticConfig, repository, saveLoad);
                presenter.Start();

                var navButtons = ReadNavButtons(dashboard);
                var screenRoots = ReadScreenRoots(dashboard);
                var expeditionsNavId = FindNavIdByLabel(catalog, catalog.ExpeditionScreenTitle);
                Assert.That(navButtons.ContainsKey(expeditionsNavId), Is.True, "Missing expeditions nav binding.");
                navButtons[expeditionsNavId].onClick.Invoke();

                var launchButton = ReadExpeditionLaunchButton(dashboard);
                Assert.NotNull(launchButton, "Expedition launch button is not assigned.");
                Assert.That(launchButton.interactable, Is.True, "Expedition launch button is not interactable for a new game state.");

                launchButton.onClick.Invoke();

                var afterLaunch = store.State.CurrentValue;
                Assert.AreEqual(1, afterLaunch.Expeditions.Count);
                var launched = afterLaunch.Expeditions[0];
                Assert.AreEqual(ExpeditionStatus.Active, launched.Status);
                Assert.AreEqual(1, repository.SaveCount, "Launching the expedition should save once.");

                var gameClock = new GameClockLoop(new TickGameUseCase(store, staticConfig), staticConfig, saveLoad, store);
                var completionStep = launched.ExpectedDurationSeconds + config.Balance.ExpeditionStepSeconds * 25;
                gameClock.TickStepAsync(completionStep, CancellationToken.None).GetAwaiter().GetResult();

                var afterTick = store.State.CurrentValue;
                var finished = afterTick.Expeditions[0];
                Assert.That(finished.Status, Is.Not.EqualTo(ExpeditionStatus.Active));
                Assert.That(finished.Status, Is.Not.EqualTo(ExpeditionStatus.Returning));
                Assert.GreaterOrEqual(repository.SaveCount, 2, "Completing or failing an expedition should trigger a critical save.");

                Assert.That(screenRoots.ContainsKey(catalog.ReportsScreenId), Is.True, "Missing configured reports screen binding.");
                AssertOnlyScreenActive(screenRoots, catalog.ReportsScreenId);

                var afterActionPanel = ReadAfterActionPanel(dashboard);
                var afterActionOutcome = ReadAfterActionOutcomeLabel(dashboard);
                Assert.NotNull(afterActionPanel, "After-action panel is not assigned.");
                Assert.NotNull(afterActionOutcome, "After-action outcome label is not assigned.");
                Assert.That(afterActionPanel.gameObject.activeSelf, Is.True, "After-action report was not rendered after expedition finished.");
                Assert.That(afterActionOutcome.text, Is.Not.Empty, "After-action outcome text is empty.");
            }
            finally
            {
                presenter?.Dispose();
                store?.Dispose();
                Object.DestroyImmediate(instance);
                Object.DestroyImmediate(catalog);
            }
        }

        private static void CollectMissingReferences(string prefabPath, Object target, string typeName, List<string> failures)
        {
            var serialized = new SerializedObject(target);
            var property = serialized.GetIterator();
            var enterChildren = true;
            while (property.NextVisible(enterChildren))
            {
                enterChildren = false;
                if (property.name == "m_Script") continue;
                if (property.propertyType != SerializedPropertyType.ObjectReference) continue;
                if (property.objectReferenceValue != null) continue;
                if (IsAllowedNullReference(property.propertyPath)) continue;

                failures.Add(prefabPath + " " + typeName + "." + property.propertyPath + " is null.");
            }
        }

        private static bool IsAllowedNullReference(string propertyPath)
        {
            return propertyPath == "root" ||
                   propertyPath == "icon" ||
                   propertyPath.EndsWith(".icon");
        }

        private static Dictionary<string, List<GameObject>> ReadScreenRoots(CampDashboardView dashboard)
        {
            var result = new Dictionary<string, List<GameObject>>();
            var serialized = new SerializedObject(dashboard);
            var screens = serialized.FindProperty("screens");

            for (var i = 0; i < screens.arraySize; i++)
            {
                var screen = screens.GetArrayElementAtIndex(i);
                var id = screen.FindPropertyRelative("id").stringValue;
                if (string.IsNullOrWhiteSpace(id)) continue;

                var roots = new List<GameObject>();
                var rootProperties = screen.FindPropertyRelative("roots");
                for (var rootIndex = 0; rootIndex < rootProperties.arraySize; rootIndex++)
                {
                    var root = rootProperties.GetArrayElementAtIndex(rootIndex).objectReferenceValue as GameObject;
                    if (root != null)
                    {
                        roots.Add(root);
                    }
                }

                result[id] = roots;
            }

            return result;
        }

        private static Dictionary<string, Button> ReadNavButtons(CampDashboardView dashboard)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var bottomNav = dashboardSerialized.FindProperty("bottomNav").objectReferenceValue as BottomNavView;
            Assert.NotNull(bottomNav, "CampDashboardView.bottomNav is not assigned.");

            var result = new Dictionary<string, Button>();
            var navSerialized = new SerializedObject(bottomNav);
            var items = navSerialized.FindProperty("items");

            for (var i = 0; i < items.arraySize; i++)
            {
                var item = items.GetArrayElementAtIndex(i);
                var id = item.FindPropertyRelative("id").stringValue;
                if (string.IsNullOrWhiteSpace(id)) continue;

                result[id] = item.FindPropertyRelative("button").objectReferenceValue as Button;
            }

            return result;
        }

        private static Dictionary<string, RawImage> ReadNavIcons(CampDashboardView dashboard)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var bottomNav = dashboardSerialized.FindProperty("bottomNav").objectReferenceValue as BottomNavView;
            Assert.NotNull(bottomNav, "CampDashboardView.bottomNav is not assigned.");

            var result = new Dictionary<string, RawImage>();
            var navSerialized = new SerializedObject(bottomNav);
            var items = navSerialized.FindProperty("items");

            for (var i = 0; i < items.arraySize; i++)
            {
                var item = items.GetArrayElementAtIndex(i);
                var id = item.FindPropertyRelative("id").stringValue;
                if (string.IsNullOrWhiteSpace(id)) continue;

                result[id] = item.FindPropertyRelative("icon").objectReferenceValue as RawImage;
            }

            return result;
        }

        private static SurvivorPortraitBindings ReadSurvivorPortraitBindings(SurvivorsPanelView survivors)
        {
            var serialized = new SerializedObject(survivors);
            var cards = serialized.FindProperty("cards");
            Assert.That(cards.arraySize, Is.GreaterThan(0), "SurvivorsPanelView has no card bindings.");

            var firstCard = cards.GetArrayElementAtIndex(0);
            return new SurvivorPortraitBindings(
                firstCard.FindPropertyRelative("portrait").objectReferenceValue as RawImage,
                firstCard.FindPropertyRelative("avatarLabel").objectReferenceValue as TextMeshProUGUI,
                serialized.FindProperty("detailPortrait").objectReferenceValue as RawImage);
        }

        private static SurvivorArtworkBindings ReadSurvivorArtworkBindings(SurvivorsPanelView survivors)
        {
            var serialized = new SerializedObject(survivors);
            var cards = serialized.FindProperty("cards");
            Assert.That(cards.arraySize, Is.GreaterThan(0), "SurvivorsPanelView has no card bindings.");

            return new SurvivorArtworkBindings(
                serialized.FindProperty("emptyPanelArtwork").objectReferenceValue as RawImage,
                serialized.FindProperty("detailPanelArtwork").objectReferenceValue as RawImage,
                cards.GetArrayElementAtIndex(0).FindPropertyRelative("cardArtwork").objectReferenceValue as RawImage);
        }

        private static void AssertSurvivorArtwork(Texture2D expected, RawImage binding, string label)
        {
            Assert.NotNull(expected, "Catalog survivor artwork is missing for " + label + ".");
            Assert.NotNull(binding, "Survivor artwork binding is missing for " + label + ".");
            Assert.AreSame(expected, binding.texture, "Survivor artwork texture mismatch for " + label + ".");
            Assert.That(binding.gameObject.activeSelf, Is.True, "Survivor artwork object is inactive for " + label + ".");
        }

        private static Button ReadExpeditionLaunchButton(CampDashboardView dashboard)
        {
            var expeditionsPanel = ReadExpeditionsPanel(dashboard);
            var panelSerialized = new SerializedObject(expeditionsPanel);
            return panelSerialized.FindProperty("launchButton").objectReferenceValue as Button;
        }

        private static ExpeditionArtworkBindings ReadExpeditionArtworkBindings(CampDashboardView dashboard)
        {
            var expeditionsPanel = ReadExpeditionsPanel(dashboard);
            var panelSerialized = new SerializedObject(expeditionsPanel);
            return new ExpeditionArtworkBindings(
                panelSerialized.FindProperty("detailPanelArtwork").objectReferenceValue as RawImage,
                panelSerialized.FindProperty("monitorPanelArtwork").objectReferenceValue as RawImage);
        }

        private static ExpeditionRouteArtworkBindings ReadExpeditionRouteArtworkBindings(CampDashboardView dashboard, int routeIndex)
        {
            var expeditionsPanel = ReadExpeditionsPanel(dashboard);
            var panelSerialized = new SerializedObject(expeditionsPanel);
            var routeCards = panelSerialized.FindProperty("routeCards");
            Assert.That(routeCards.arraySize, Is.GreaterThan(routeIndex), "ExpeditionsPanelView.routeCards has too few bindings.");

            var routeCard = routeCards.GetArrayElementAtIndex(routeIndex);
            return new ExpeditionRouteArtworkBindings(
                routeCard.FindPropertyRelative("thumbnail").objectReferenceValue as RawImage,
                routeCard.FindPropertyRelative("riskBadge").objectReferenceValue as RawImage);
        }

        private static ExpeditionSetupCardArtworkBindings ReadExpeditionSetupCardArtworkBindings(CampDashboardView dashboard)
        {
            var expeditionsPanel = ReadExpeditionsPanel(dashboard);
            var panelSerialized = new SerializedObject(expeditionsPanel);
            var squadMembers = panelSerialized.FindProperty("squadMembers");
            var policies = panelSerialized.FindProperty("policies");
            Assert.That(squadMembers.arraySize, Is.GreaterThan(0), "ExpeditionsPanelView.squadMembers has no bindings.");
            Assert.That(policies.arraySize, Is.GreaterThan(0), "ExpeditionsPanelView.policies has no bindings.");

            return new ExpeditionSetupCardArtworkBindings(
                squadMembers.GetArrayElementAtIndex(0).FindPropertyRelative("cardArtwork").objectReferenceValue as RawImage,
                policies.GetArrayElementAtIndex(0).FindPropertyRelative("cardArtwork").objectReferenceValue as RawImage);
        }

        private static void AssertExpeditionArtwork(Texture2D expected, RawImage binding, string label)
        {
            Assert.NotNull(expected, "Catalog expedition artwork is missing for " + label + ".");
            Assert.NotNull(binding, "Expedition artwork binding is missing for " + label + ".");
            Assert.AreSame(expected, binding.texture, "Expedition artwork texture mismatch for " + label + ".");
            Assert.That(binding.gameObject.activeSelf, Is.True, "Expedition artwork object is inactive for " + label + ".");
        }

        private static RawImage ReadCampStatusArtwork(CampDashboardView dashboard)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var panel = dashboardSerialized.FindProperty("statusPanel").objectReferenceValue as CampStatusPanelView;
            Assert.NotNull(panel, "CampDashboardView.statusPanel is not assigned.");

            var panelSerialized = new SerializedObject(panel);
            return panelSerialized.FindProperty("panelArtwork").objectReferenceValue as RawImage;
        }

        private static RawImage ReadResourceIcon(CampDashboardView dashboard, string resourceId)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var resourceBar = dashboardSerialized.FindProperty("resourceBar").objectReferenceValue as ResourceBarView;
            Assert.NotNull(resourceBar, "CampDashboardView.resourceBar is not assigned.");

            var resourceBarSerialized = new SerializedObject(resourceBar);
            var resources = resourceBarSerialized.FindProperty("resources");
            for (var i = 0; i < resources.arraySize; i++)
            {
                var resource = resources.GetArrayElementAtIndex(i);
                if (resource.FindPropertyRelative("id").stringValue == resourceId)
                {
                    return resource.FindPropertyRelative("icon").objectReferenceValue as RawImage;
                }
            }

            Assert.Fail("Missing resource bar binding for resource id: " + resourceId);
            return null;
        }

        private static ResourceUiEntry FindResourceUiEntry(CampUiCatalogSO catalog, string resourceId)
        {
            foreach (var entry in catalog.ResourceBar)
            {
                if (entry != null && entry.Id == resourceId)
                {
                    return entry;
                }
            }

            Assert.Fail("Missing resource UI entry for resource id: " + resourceId);
            return null;
        }

        private static RawImage ReadFirstAlertArtwork(CampDashboardView dashboard)
        {
            var panel = ReadAlertsPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            var alerts = panelSerialized.FindProperty("alerts");
            Assert.That(alerts.arraySize, Is.GreaterThan(0), "CampAlertsPanelView has no alert bindings.");

            return alerts.GetArrayElementAtIndex(0).FindPropertyRelative("cardArtwork").objectReferenceValue as RawImage;
        }

        private static RawImage ReadBuildingCardArtwork(CampDashboardView dashboard, string buildingId)
        {
            var card = ReadBuildingCard(dashboard, buildingId);
            var cardSerialized = new SerializedObject(card);
            return cardSerialized.FindProperty("cardArtwork").objectReferenceValue as RawImage;
        }

        private static BuildingCardVisualBindings ReadBuildingCardVisualBindings(CampDashboardView dashboard, string buildingId)
        {
            var card = ReadBuildingCard(dashboard, buildingId);
            var cardSerialized = new SerializedObject(card);
            return new BuildingCardVisualBindings(
                cardSerialized.FindProperty("cardArtwork").objectReferenceValue as RawImage,
                cardSerialized.FindProperty("icon").objectReferenceValue as RawImage,
                cardSerialized.FindProperty("imageLetter").objectReferenceValue as TextMeshProUGUI);
        }

        private static BuildingUiEntry FindBuildingUiEntry(CampUiCatalogSO catalog, string buildingId)
        {
            foreach (var entry in catalog.Buildings)
            {
                if (entry != null && entry.BuildingId == buildingId)
                {
                    return entry;
                }
            }

            Assert.Fail("Missing building UI entry for building id: " + buildingId);
            return null;
        }

        private static void AssertCampArtwork(Texture2D expected, RawImage binding, string label)
        {
            Assert.NotNull(expected, "Catalog camp artwork is missing for " + label + ".");
            Assert.NotNull(binding, "Camp artwork binding is missing for " + label + ".");
            Assert.AreSame(expected, binding.texture, "Camp artwork texture mismatch for " + label + ".");
            Assert.That(binding.gameObject.activeSelf, Is.True, "Camp artwork object is inactive for " + label + ".");
        }

        private static Button ReadExpeditionRouteButton(CampDashboardView dashboard, int routeIndex)
        {
            return ReadExpeditionBindingButton(dashboard, "routeCards", routeIndex);
        }

        private static Button ReadExpeditionPolicyButton(CampDashboardView dashboard, int policyIndex)
        {
            return ReadExpeditionBindingButton(dashboard, "policies", policyIndex);
        }

        private static Button ReadExpeditionSquadMemberButton(CampDashboardView dashboard, int survivorIndex)
        {
            return ReadExpeditionBindingButton(dashboard, "squadMembers", survivorIndex);
        }

        private static Button ReadExpeditionBindingButton(CampDashboardView dashboard, string bindingsField, int bindingIndex)
        {
            var panelSerialized = new SerializedObject(ReadExpeditionsPanel(dashboard));
            var bindings = panelSerialized.FindProperty(bindingsField);
            Assert.NotNull(bindings, "ExpeditionsPanelView." + bindingsField + " is missing.");
            Assert.That(bindingIndex, Is.GreaterThanOrEqualTo(0), "Binding index must be non-negative.");
            Assert.That(bindings.arraySize, Is.GreaterThan(bindingIndex), "ExpeditionsPanelView." + bindingsField + " has too few bindings.");

            return bindings.GetArrayElementAtIndex(bindingIndex).FindPropertyRelative("button").objectReferenceValue as Button;
        }

        private static ExpeditionsPanelView ReadExpeditionsPanel(CampDashboardView dashboard)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var expeditionsPanel = dashboardSerialized.FindProperty("expeditionsPanel").objectReferenceValue as ExpeditionsPanelView;
            Assert.NotNull(expeditionsPanel, "CampDashboardView.expeditionsPanel is not assigned.");
            return expeditionsPanel;
        }

        private static Button ReadRadioBroadcastButton(CampDashboardView dashboard)
        {
            var radioPanel = ReadRadioPanel(dashboard);
            var panelSerialized = new SerializedObject(radioPanel);
            return panelSerialized.FindProperty("broadcastButton").objectReferenceValue as Button;
        }

        private static Button ReadFirstInteractableRecruitButton(CampDashboardView dashboard)
        {
            var radioPanel = ReadRadioPanel(dashboard);
            var panelSerialized = new SerializedObject(radioPanel);
            var cards = panelSerialized.FindProperty("candidateCards");
            for (var i = 0; i < cards.arraySize; i++)
            {
                var card = cards.GetArrayElementAtIndex(i);
                var button = card.FindPropertyRelative("recruitButton").objectReferenceValue as Button;
                if (button != null && button.interactable)
                {
                    return button;
                }
            }

            return null;
        }

        private static RadioCandidatePortraitBindings ReadFirstRadioCandidatePortraitBindings(CampDashboardView dashboard)
        {
            var radioPanel = ReadRadioPanel(dashboard);
            var panelSerialized = new SerializedObject(radioPanel);
            var cards = panelSerialized.FindProperty("candidateCards");
            Assert.That(cards.arraySize, Is.GreaterThan(0), "RadioPanelView has no candidate card bindings.");

            var card = cards.GetArrayElementAtIndex(0);
            return new RadioCandidatePortraitBindings(
                card.FindPropertyRelative("portrait").objectReferenceValue as RawImage,
                card.FindPropertyRelative("avatarLabel").objectReferenceValue as TextMeshProUGUI);
        }

        private static RadioArtworkBindings ReadRadioArtworkBindings(CampDashboardView dashboard)
        {
            var radioPanel = ReadRadioPanel(dashboard);
            var panelSerialized = new SerializedObject(radioPanel);
            var cards = panelSerialized.FindProperty("candidateCards");
            Assert.That(cards.arraySize, Is.GreaterThan(0), "RadioPanelView has no candidate card bindings.");

            return new RadioArtworkBindings(
                panelSerialized.FindProperty("intelPanelArtwork").objectReferenceValue as RawImage,
                panelSerialized.FindProperty("broadcastPanelArtwork").objectReferenceValue as RawImage,
                panelSerialized.FindProperty("emptyPanelArtwork").objectReferenceValue as RawImage,
                cards.GetArrayElementAtIndex(0).FindPropertyRelative("cardArtwork").objectReferenceValue as RawImage);
        }

        private static void AssertRadioArtwork(Texture2D expected, RawImage binding, string label)
        {
            Assert.NotNull(expected, "Catalog radio artwork is missing for " + label + ".");
            Assert.NotNull(binding, "Radio artwork binding is missing for " + label + ".");
            Assert.AreSame(expected, binding.texture, "Radio artwork texture mismatch for " + label + ".");
            Assert.That(binding.gameObject.activeSelf, Is.True, "Radio artwork object is inactive for " + label + ".");
        }

        private static Button ReadAfterActionSendAgainButton(CampDashboardView dashboard)
        {
            var panel = ReadReportsPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            return panelSerialized.FindProperty("afterActionSendAgainButton").objectReferenceValue as Button;
        }

        private static Image ReadAfterActionPanel(CampDashboardView dashboard)
        {
            var panel = ReadReportsPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            return panelSerialized.FindProperty("afterActionPanel").objectReferenceValue as Image;
        }

        private static TextMeshProUGUI ReadAfterActionOutcomeLabel(CampDashboardView dashboard)
        {
            var panel = ReadReportsPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            return panelSerialized.FindProperty("afterActionOutcome").objectReferenceValue as TextMeshProUGUI;
        }

        private static ReportsArtworkBindings ReadReportsArtworkBindings(CampDashboardView dashboard)
        {
            var panel = ReadReportsPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            return new ReportsArtworkBindings(
                panelSerialized.FindProperty("emptyPanelArtwork").objectReferenceValue as RawImage,
                panelSerialized.FindProperty("afterActionPanelArtwork").objectReferenceValue as RawImage,
                panelSerialized.FindProperty("campEventPanelArtwork").objectReferenceValue as RawImage,
                panelSerialized.FindProperty("offlinePanelArtwork").objectReferenceValue as RawImage);
        }

        private static ReportsPanelView ReadReportsPanel(CampDashboardView dashboard)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var reportsPanel = dashboardSerialized.FindProperty("reportsPanel").objectReferenceValue as ReportsPanelView;
            Assert.NotNull(reportsPanel, "CampDashboardView.reportsPanel is not assigned.");
            return reportsPanel;
        }

        private static void AssertReportArtwork(Texture2D expected, RawImage binding, string label)
        {
            Assert.NotNull(expected, "Catalog report artwork is missing for " + label + ".");
            Assert.NotNull(binding, "Report artwork binding is missing for " + label + ".");
            Assert.AreSame(expected, binding.texture, "Report artwork texture mismatch for " + label + ".");
            Assert.That(binding.gameObject.activeSelf, Is.True, "Report artwork object is inactive for " + label + ".");
        }

        private static Button ReadWorkshopRepairButton(CampDashboardView dashboard, int itemIndex)
        {
            return ReadWorkshopItemButton(dashboard, itemIndex, "repairButton");
        }

        private static Button ReadWorkshopEquipButton(CampDashboardView dashboard, int itemIndex)
        {
            return ReadWorkshopItemButton(dashboard, itemIndex, "equipButton");
        }

        private static Button ReadWorkshopItemButton(CampDashboardView dashboard, int itemIndex, string buttonField)
        {
            var panel = ReadWorkshopPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            var items = panelSerialized.FindProperty("items");
            Assert.That(items.arraySize, Is.GreaterThan(itemIndex), "Workshop panel does not have enough item bindings.");

            var item = items.GetArrayElementAtIndex(itemIndex);
            return item.FindPropertyRelative(buttonField).objectReferenceValue as Button;
        }

        private static WorkshopArtworkBindings ReadWorkshopArtworkBindings(CampDashboardView dashboard)
        {
            var panel = ReadWorkshopPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            var items = panelSerialized.FindProperty("items");
            Assert.That(items.arraySize, Is.GreaterThan(0), "Workshop panel does not have item bindings.");

            return new WorkshopArtworkBindings(
                panelSerialized.FindProperty("emptyPanelArtwork").objectReferenceValue as RawImage,
                items.GetArrayElementAtIndex(0).FindPropertyRelative("tileArtwork").objectReferenceValue as RawImage);
        }

        private static void AssertWorkshopArtwork(Texture2D expected, RawImage binding, string label)
        {
            Assert.NotNull(expected, "Catalog workshop artwork is missing for " + label + ".");
            Assert.NotNull(binding, "Workshop artwork binding is missing for " + label + ".");
            Assert.AreSame(expected, binding.texture, "Workshop artwork texture mismatch for " + label + ".");
            Assert.That(binding.gameObject.activeSelf, Is.True, "Workshop artwork object is inactive for " + label + ".");
        }

        private static Button ReadUseMedicineButton(CampDashboardView dashboard)
        {
            var panel = ReadSurvivorsPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            return panelSerialized.FindProperty("useMedicineButton").objectReferenceValue as Button;
        }

        private static Toggle ReadAutosaveToggle(CampDashboardView dashboard)
        {
            var panel = ReadSettingsPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            return panelSerialized.FindProperty("autosaveToggle").objectReferenceValue as Toggle;
        }

        private static Button ReadManualSaveButton(CampDashboardView dashboard)
        {
            var panel = ReadSettingsPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            return panelSerialized.FindProperty("manualSaveButton").objectReferenceValue as Button;
        }

        private static SettingsArtworkBindings ReadSettingsArtworkBindings(CampDashboardView dashboard)
        {
            var panel = ReadSettingsPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            return new SettingsArtworkBindings(
                panelSerialized.FindProperty("autosaveRowArtwork").objectReferenceValue as RawImage,
                panelSerialized.FindProperty("autosaveToggleTrackArtwork").objectReferenceValue as RawImage,
                panelSerialized.FindProperty("autosaveToggleKnobArtwork").objectReferenceValue as RawImage,
                panelSerialized.FindProperty("manualSaveRowArtwork").objectReferenceValue as RawImage);
        }

        private static void AssertSettingsArtwork(Texture2D expected, RawImage binding, string label)
        {
            Assert.NotNull(expected, "Catalog settings artwork is missing for " + label + ".");
            Assert.NotNull(binding, "Settings artwork binding is missing for " + label + ".");
            Assert.AreSame(expected, binding.texture, "Settings artwork texture mismatch for " + label + ".");
            Assert.That(binding.gameObject.activeSelf, Is.True, "Settings artwork object is inactive for " + label + ".");
        }

        private static Button ReadFirstInteractableAlertActionButton(CampDashboardView dashboard)
        {
            var panel = ReadAlertsPanel(dashboard);
            var panelSerialized = new SerializedObject(panel);
            var alerts = panelSerialized.FindProperty("alerts");

            for (var i = 0; i < alerts.arraySize; i++)
            {
                var alert = alerts.GetArrayElementAtIndex(i);
                var button = alert.FindPropertyRelative("actionButton").objectReferenceValue as Button;
                if (button != null && button.gameObject.activeSelf && button.interactable)
                {
                    return button;
                }
            }

            return null;
        }

        private static CampAlertsPanelView ReadAlertsPanel(CampDashboardView dashboard)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var alertsPanel = dashboardSerialized.FindProperty("alertsPanel").objectReferenceValue as CampAlertsPanelView;
            Assert.NotNull(alertsPanel, "CampDashboardView.alertsPanel is not assigned.");
            return alertsPanel;
        }

        private static SettingsPanelView ReadSettingsPanel(CampDashboardView dashboard)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var settingsPanel = dashboardSerialized.FindProperty("settingsPanel").objectReferenceValue as SettingsPanelView;
            Assert.NotNull(settingsPanel, "CampDashboardView.settingsPanel is not assigned.");
            return settingsPanel;
        }

        private static SurvivorsPanelView ReadSurvivorsPanel(CampDashboardView dashboard)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var survivorsPanel = dashboardSerialized.FindProperty("survivorsPanel").objectReferenceValue as SurvivorsPanelView;
            Assert.NotNull(survivorsPanel, "CampDashboardView.survivorsPanel is not assigned.");
            return survivorsPanel;
        }

        private static WorkshopPanelView ReadWorkshopPanel(CampDashboardView dashboard)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var workshopPanel = dashboardSerialized.FindProperty("workshopPanel").objectReferenceValue as WorkshopPanelView;
            Assert.NotNull(workshopPanel, "CampDashboardView.workshopPanel is not assigned.");
            return workshopPanel;
        }

        private static RadioPanelView ReadRadioPanel(CampDashboardView dashboard)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var radioPanel = dashboardSerialized.FindProperty("radioPanel").objectReferenceValue as RadioPanelView;
            Assert.NotNull(radioPanel, "CampDashboardView.radioPanel is not assigned.");
            return radioPanel;
        }

        private static Button ReadBuildingUpgradeButton(CampDashboardView dashboard, string buildingId)
        {
            var card = ReadBuildingCard(dashboard, buildingId);
            var cardSerialized = new SerializedObject(card);
            return cardSerialized.FindProperty("upgradeButton").objectReferenceValue as Button;
        }

        private static BuildingCardView ReadBuildingCard(CampDashboardView dashboard, string buildingId)
        {
            var dashboardSerialized = new SerializedObject(dashboard);
            var grid = dashboardSerialized.FindProperty("buildingGrid").objectReferenceValue as BuildingGridView;
            Assert.NotNull(grid, "CampDashboardView.buildingGrid is not assigned.");

            var gridSerialized = new SerializedObject(grid);
            var cards = gridSerialized.FindProperty("cards");
            for (var i = 0; i < cards.arraySize; i++)
            {
                var card = cards.GetArrayElementAtIndex(i).objectReferenceValue as BuildingCardView;
                if (card != null && card.BuildingId == buildingId)
                {
                    return card;
                }
            }

            Assert.Fail("Missing building card binding for building id: " + buildingId);
            return null;
        }

        private static CampHudPresenter CreatePresenter(
            CampDashboardView dashboard,
            GameStateStore store,
            IGameConfigProvider configProvider,
            ISaveRepository repository,
            ISaveLoadUseCase saveLoad)
        {
            return new CampHudPresenter(
                dashboard,
                store,
                store,
                configProvider,
                new UpgradeBuildingUseCase(store, configProvider),
                new LaunchExpeditionUseCase(store, configProvider),
                new BroadcastRecruitmentUseCase(store, configProvider),
                new RecruitSurvivorUseCase(store, configProvider),
                new SkipRecruitmentCandidatesUseCase(store),
                new RepairItemUseCase(store, configProvider),
                new EquipItemUseCase(store, configProvider),
                new UseMedicineUseCase(store, configProvider),
                new StartEmergencyScavengeUseCase(store, configProvider),
                new SetAutosaveUseCase(store, repository),
                saveLoad);
        }

        private static int CostFor(Dictionary<string, int> cost, string resourceId)
        {
            if (string.IsNullOrWhiteSpace(resourceId)) return 0;
            int amount;
            return cost.TryGetValue(resourceId, out amount) ? amount : 0;
        }

        private static string FindFirstUpgradeableUiBuildingId(GameState state, GameConfigSnapshot config, CampUiCatalogSO catalog)
        {
            foreach (var entry in catalog.Buildings)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.BuildingId)) continue;

                BuildingDefinition definition;
                BuildingState building;
                if (!config.Buildings.TryGetValue(entry.BuildingId, out definition) ||
                    !state.Buildings.TryGetValue(entry.BuildingId, out building) ||
                    !building.IsUnlocked)
                {
                    continue;
                }

                var nextLevel = BuildingSystem.GetLevel(definition, building.Level + 1);
                if (nextLevel != null)
                {
                    return entry.BuildingId;
                }
            }

            Assert.Fail("No upgradeable UI building exists in config.");
            return string.Empty;
        }

        private static string FindFirstUnlockedZoneId(GameState state, GameConfigSnapshot config)
        {
            foreach (var zone in config.Zones.Values)
            {
                ZoneState zoneState;
                if (state.Zones.TryGetValue(zone.Id, out zoneState) && zoneState.IsUnlocked)
                {
                    return zone.Id;
                }
            }

            Assert.Fail("No unlocked zone exists in config.");
            return string.Empty;
        }

        private static string ResolvePolicyId(GameConfigSnapshot config, string preferredPolicyId)
        {
            if (!string.IsNullOrWhiteSpace(preferredPolicyId) && config.Policies.ContainsKey(preferredPolicyId))
            {
                return preferredPolicyId;
            }

            foreach (var policyId in config.Policies.Keys)
            {
                return policyId;
            }

            Assert.Fail("No expedition policy exists in config.");
            return string.Empty;
        }

        private static int FindAlternateUnlockedZoneIndex(GameState state, GameConfigSnapshot config, string selectedZoneId, out string zoneId)
        {
            var index = 0;
            foreach (var zone in config.Zones.Values)
            {
                if (!string.Equals(zone.Id, selectedZoneId, System.StringComparison.Ordinal))
                {
                    ZoneState zoneState;
                    if (!state.Zones.TryGetValue(zone.Id, out zoneState))
                    {
                        zoneState = new ZoneState { Id = zone.Id };
                        state.Zones[zone.Id] = zoneState;
                    }

                    zoneState.IsUnlocked = true;
                    PrepareZoneLaunchRequirements(state, config, zone);
                    zoneId = zone.Id;
                    return index;
                }

                index++;
            }

            Assert.Fail("No alternate expedition zone exists in config.");
            zoneId = string.Empty;
            return -1;
        }

        private static int FindAlternatePolicyIndex(GameConfigSnapshot config, string selectedPolicyId, out string policyId)
        {
            var index = 0;
            foreach (var policy in config.Policies.Values)
            {
                if (!string.Equals(policy.Id, selectedPolicyId, System.StringComparison.Ordinal))
                {
                    policyId = policy.Id;
                    return index;
                }

                index++;
            }

            Assert.Fail("No alternate expedition policy exists in config.");
            policyId = string.Empty;
            return -1;
        }

        private static void EnsureTwoSurvivorSquad(GameState state, GameConfigSnapshot config)
        {
            if (state.SurvivorCap >= 2 && state.SquadSize >= 2) return;

            foreach (var definition in config.Buildings.Values)
            {
                BuildingState building;
                if (!state.Buildings.TryGetValue(definition.Id, out building)) continue;

                foreach (var level in definition.Levels)
                {
                    if (level.SurvivorCap < 2 || level.SquadSize < 2) continue;

                    building.IsUnlocked = true;
                    building.Level = level.Level;
                    BuildingSystem.ApplyAllBuildingEffects(state, config);
                    if (state.SurvivorCap >= 2 && state.SquadSize >= 2)
                    {
                        return;
                    }
                }
            }

            Assert.Fail("No configured building level can support two survivors in one squad.");
        }

        private static SurvivorState AddFirstRecruitableSurvivor(GameState state, GameConfigSnapshot config)
        {
            foreach (var candidate in config.RecruitableSurvivors.Values)
            {
                var weapon = GameStateFactory.CreateItemState(candidate.WeaponItemId, config, "item_" + state.NextId++);
                var survivor = GameStateFactory.CreateSurvivorState(
                    "survivor_" + state.NextId++,
                    candidate.Name,
                    candidate.BackgroundId,
                    candidate.TraitIds,
                    candidate.Skills,
                    config);

                survivor.Equipment.WeaponItemUid = weapon.Uid;
                weapon.EquippedBySurvivorId = survivor.Id;
                state.Inventory.Add(weapon);
                state.Survivors.Add(survivor);
                return survivor;
            }

            Assert.Fail("No recruitable survivor exists in config.");
            return null;
        }

        private static void PrepareZoneLaunchRequirements(GameState state, GameConfigSnapshot config, ZoneDefinition zone)
        {
            foreach (var requirement in zone.RequiredBuildingLevels)
            {
                BuildingState building;
                Assert.That(
                    state.Buildings.TryGetValue(requirement.Key, out building),
                    Is.True,
                    "Required zone building is missing from state: " + requirement.Key);

                building.IsUnlocked = true;
                building.Level = System.Math.Max(building.Level, requirement.Value);
            }

            BuildingSystem.ApplyAllBuildingEffects(state, config);
        }

        private static void EnsureCanAfford(GameState state, Dictionary<string, int> cost)
        {
            foreach (var entry in cost)
            {
                var current = ResourceSystem.GetAmount(state, entry.Key);
                if (current < entry.Value)
                {
                    ResourceSystem.Add(state, entry.Key, entry.Value - current);
                }
            }

            Assert.That(ResourceSystem.CanSpend(state, cost), Is.True, "Prepared state cannot afford configured cost.");
        }

        private static void PrepareWorkshopAccess(GameState state, GameConfigSnapshot config)
        {
            if (string.IsNullOrWhiteSpace(config.Balance.WorkshopRequiredBuildingId)) return;

            BuildingState building;
            Assert.That(
                state.Buildings.TryGetValue(config.Balance.WorkshopRequiredBuildingId, out building),
                Is.True,
                "Workshop required building is missing from state: " + config.Balance.WorkshopRequiredBuildingId);

            building.IsUnlocked = true;
            building.Level = System.Math.Max(building.Level, config.Balance.WorkshopRequiredBuildingLevel);
            BuildingSystem.ApplyAllBuildingEffects(state, config);
        }

        private static void PrepareHealingAccess(GameState state, GameConfigSnapshot config)
        {
            if (string.IsNullOrWhiteSpace(config.Balance.HealingRequiredBuildingId)) return;

            BuildingState building;
            Assert.That(
                state.Buildings.TryGetValue(config.Balance.HealingRequiredBuildingId, out building),
                Is.True,
                "Healing required building is missing from state: " + config.Balance.HealingRequiredBuildingId);

            building.IsUnlocked = true;
            building.Level = System.Math.Max(building.Level, config.Balance.HealingRequiredBuildingLevel);
            BuildingSystem.ApplyAllBuildingEffects(state, config);
        }

        private static void PrepareLowEmergencyResource(GameState state, GameConfigSnapshot config)
        {
            foreach (var reward in config.Balance.EmergencyScavengeRewards)
            {
                ResourceDefinition resource;
                if (!config.Resources.TryGetValue(reward.Key, out resource) || !resource.HasCap) continue;

                state.Resources[resource.Id] = 0;
                return;
            }

            Assert.Fail("Emergency scavenge has no capped reward resource to make low.");
        }

        private static InventoryItemState CreateStoredEquipItem(GameState state, GameConfigSnapshot config)
        {
            var item = GameStateFactory.CreateItemState(config.StartingSurvivor.WeaponItemId, config, "item_" + state.NextId++);
            Assert.That(config.Items.ContainsKey(item.ItemId), Is.True, "Configured equip item is missing: " + item.ItemId);
            item.EquippedBySurvivorId = string.Empty;
            state.Inventory.Add(item);
            return item;
        }

        private static string GetEquippedUid(SurvivorState survivor, ItemSlot slot)
        {
            if (slot == ItemSlot.Armor)
            {
                return survivor.Equipment.ArmorItemUid;
            }

            if (slot == ItemSlot.Utility)
            {
                return survivor.Equipment.UtilityItemUid;
            }

            return survivor.Equipment.WeaponItemUid;
        }

        private static SurvivorState FindSurvivor(GameState state, string survivorId)
        {
            foreach (var survivor in state.Survivors)
            {
                if (survivor.Id == survivorId)
                {
                    return survivor;
                }
            }

            return null;
        }

        private static void CapWoundRemainingToMedicineUse(SurvivorState survivor, GameConfigSnapshot config)
        {
            foreach (var effect in survivor.StatusEffects)
            {
                if (effect != null && effect.Id == config.Balance.HealingDefaultWoundId)
                {
                    effect.RemainingSeconds = System.Math.Max(1, config.Balance.HealingMedicineSeconds);
                    return;
                }
            }

            Assert.Fail("Prepared wounded survivor has no wound effect: " + config.Balance.HealingDefaultWoundId);
        }

        private static Dictionary<string, int> CopyResources(Dictionary<string, int> resources)
        {
            return new Dictionary<string, int>(resources);
        }

        private static void AssertCostSpent(Dictionary<string, int> before, Dictionary<string, int> after, Dictionary<string, int> cost)
        {
            foreach (var entry in cost)
            {
                int beforeAmount;
                int afterAmount;
                before.TryGetValue(entry.Key, out beforeAmount);
                after.TryGetValue(entry.Key, out afterAmount);
                Assert.AreEqual(beforeAmount - entry.Value, afterAmount, "Resource spend mismatch for " + entry.Key);
            }
        }

        private static void EnsureCanAffordRecruitmentBroadcast(GameState state, GameConfigSnapshot config)
        {
            var cost = RecruitmentSystem.CalculateCost(state, config);
            EnsureCanAfford(state, cost);
        }

        private static void EnsureCanRecruitOneMoreSurvivor(GameState state, GameConfigSnapshot config)
        {
            if (state.Survivors.Count < state.SurvivorCap) return;

            foreach (var definition in config.Buildings.Values)
            {
                BuildingState building;
                if (!state.Buildings.TryGetValue(definition.Id, out building)) continue;

                foreach (var level in definition.Levels)
                {
                    if (level.SurvivorCap <= state.Survivors.Count) continue;

                    building.IsUnlocked = true;
                    building.Level = level.Level;
                    BuildingSystem.ApplyAllBuildingEffects(state, config);
                    return;
                }
            }

            Assert.Fail("No configured building level can raise survivor cap above current survivor count.");
        }

        private static string FindNavIdByLabel(CampUiCatalogSO catalog, string label)
        {
            foreach (var item in catalog.NavItems)
            {
                if (item != null && item.Label == label && !string.IsNullOrWhiteSpace(item.Id))
                {
                    return item.Id;
                }
            }

            Assert.Fail("Missing nav item for label: " + label);
            return string.Empty;
        }

        private sealed class CountingSaveRepository : ISaveRepository
        {
            private readonly GameState _loadedState;

            public GameState SavedState { get; private set; }
            public int SaveCount { get; private set; }

            public CountingSaveRepository(GameState loadedState)
            {
                _loadedState = loadedState;
            }

            public UniTask<SaveRepositoryLoadResult> LoadAsync(CancellationToken ct)
            {
                return UniTask.FromResult(_loadedState == null
                    ? null
                    : new SaveRepositoryLoadResult { State = _loadedState });
            }

            public UniTask SaveAsync(GameState state, CancellationToken ct)
            {
                SaveCount++;
                SavedState = state;
                return UniTask.CompletedTask;
            }
        }

        private static bool IsOnlyActiveScreen(Dictionary<string, List<GameObject>> screenRoots, string activeId)
        {
            foreach (var entry in screenRoots)
            {
                var shouldBeActive = entry.Key == activeId;
                foreach (var root in entry.Value)
                {
                    if (root != null && root.activeSelf != shouldBeActive)
                    {
                        return false;
                    }
                }
            }

            return true;
        }

        private static void AssertOnlyScreenActive(Dictionary<string, List<GameObject>> screenRoots, string activeId)
        {
            foreach (var entry in screenRoots)
            {
                var shouldBeActive = entry.Key == activeId;
                foreach (var root in entry.Value)
                {
                    if (root == null) continue;
                    Assert.That(root.activeSelf, Is.EqualTo(shouldBeActive), entry.Key + " root " + root.name + " active state mismatch while selecting " + activeId);
                }
            }
        }

        private sealed class SurvivorPortraitBindings
        {
            public readonly RawImage CardPortrait;
            public readonly TextMeshProUGUI CardAvatar;
            public readonly RawImage DetailPortrait;

            public SurvivorPortraitBindings(RawImage cardPortrait, TextMeshProUGUI cardAvatar, RawImage detailPortrait)
            {
                CardPortrait = cardPortrait;
                CardAvatar = cardAvatar;
                DetailPortrait = detailPortrait;
            }
        }

        private sealed class SurvivorArtworkBindings
        {
            public readonly RawImage Empty;
            public readonly RawImage Detail;
            public readonly RawImage FirstCard;

            public SurvivorArtworkBindings(RawImage empty, RawImage detail, RawImage firstCard)
            {
                Empty = empty;
                Detail = detail;
                FirstCard = firstCard;
            }
        }

        private sealed class RadioCandidatePortraitBindings
        {
            public readonly RawImage Portrait;
            public readonly TextMeshProUGUI Avatar;

            public RadioCandidatePortraitBindings(RawImage portrait, TextMeshProUGUI avatar)
            {
                Portrait = portrait;
                Avatar = avatar;
            }
        }

        private sealed class RadioArtworkBindings
        {
            public readonly RawImage Intel;
            public readonly RawImage Broadcast;
            public readonly RawImage Empty;
            public readonly RawImage FirstCandidateCard;

            public RadioArtworkBindings(RawImage intel, RawImage broadcast, RawImage empty, RawImage firstCandidateCard)
            {
                Intel = intel;
                Broadcast = broadcast;
                Empty = empty;
                FirstCandidateCard = firstCandidateCard;
            }
        }

        private sealed class ReportsArtworkBindings
        {
            public readonly RawImage Empty;
            public readonly RawImage AfterAction;
            public readonly RawImage CampEvent;
            public readonly RawImage Offline;

            public ReportsArtworkBindings(RawImage empty, RawImage afterAction, RawImage campEvent, RawImage offline)
            {
                Empty = empty;
                AfterAction = afterAction;
                CampEvent = campEvent;
                Offline = offline;
            }
        }

        private sealed class ExpeditionArtworkBindings
        {
            public readonly RawImage Detail;
            public readonly RawImage Monitor;

            public ExpeditionArtworkBindings(RawImage detail, RawImage monitor)
            {
                Detail = detail;
                Monitor = monitor;
            }
        }

        private sealed class ExpeditionSetupCardArtworkBindings
        {
            public readonly RawImage FirstSquadMember;
            public readonly RawImage FirstPolicy;

            public ExpeditionSetupCardArtworkBindings(RawImage firstSquadMember, RawImage firstPolicy)
            {
                FirstSquadMember = firstSquadMember;
                FirstPolicy = firstPolicy;
            }
        }

        private sealed class ExpeditionRouteArtworkBindings
        {
            public readonly RawImage Thumbnail;
            public readonly RawImage RiskBadge;

            public ExpeditionRouteArtworkBindings(RawImage thumbnail, RawImage riskBadge)
            {
                Thumbnail = thumbnail;
                RiskBadge = riskBadge;
            }
        }

        private sealed class WorkshopArtworkBindings
        {
            public readonly RawImage Empty;
            public readonly RawImage FirstItemTile;

            public WorkshopArtworkBindings(RawImage empty, RawImage firstItemTile)
            {
                Empty = empty;
                FirstItemTile = firstItemTile;
            }
        }

        private sealed class SettingsArtworkBindings
        {
            public readonly RawImage AutosaveRow;
            public readonly RawImage ToggleTrack;
            public readonly RawImage ToggleKnob;
            public readonly RawImage ManualSaveRow;

            public SettingsArtworkBindings(RawImage autosaveRow, RawImage toggleTrack, RawImage toggleKnob, RawImage manualSaveRow)
            {
                AutosaveRow = autosaveRow;
                ToggleTrack = toggleTrack;
                ToggleKnob = toggleKnob;
                ManualSaveRow = manualSaveRow;
            }
        }

        private sealed class BuildingCardVisualBindings
        {
            public readonly RawImage CardArtwork;
            public readonly RawImage Icon;
            public readonly TextMeshProUGUI ImageLetter;

            public BuildingCardVisualBindings(RawImage cardArtwork, RawImage icon, TextMeshProUGUI imageLetter)
            {
                CardArtwork = cardArtwork;
                Icon = icon;
                ImageLetter = imageLetter;
            }
        }
    }
}
