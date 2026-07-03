using System;
using System.Collections.Generic;
using System.IO;
using AshfallCamp.Composition;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using AshfallCamp.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;

namespace AshfallCamp.Editor
{
    public static class BootCoreAssetGenerator
    {
        private const string Root = "Assets/AshfallCamp";
        private const string ConfigRoot = Root + "/Configs";
        private const string SceneRoot = Root + "/Scenes";
        private const string UiRoot = Root + "/UI";
        private const string PrefabRoot = Root + "/Prefabs";
        private const string UiPrefabRoot = PrefabRoot + "/UI";
        private const string BootScenePath = SceneRoot + "/SC_Boot.unity";
        private const string SampleScenePath = "Assets/Scenes/SampleScene.unity";
        private const string CampDashboardPrefabPath = UiPrefabRoot + "/PF_CampDashboard.prefab";
        private const string CampUiCatalogPath = UiRoot + "/CampUiCatalog.asset";

        [MenuItem("Tools/Ashfall Camp/Generate Boot Core Assets")]
        public static void Generate()
        {
            EnsureFolder("Assets", "AshfallCamp");
            EnsureFolder(Root, "Configs");
            EnsureFolder(Root, "Scenes");
            EnsureFolder(Root, "UI");
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(UiRoot, "UnityThemes");
            EnsureFolder(PrefabRoot, "UI");

            var resources = CreateOrLoad<ResourceCatalogSO>(ConfigRoot + "/ResourceCatalog.asset");
            resources.Resources.Clear();
            resources.Resources.Add(new ResourceConfigData { Id = "scrap", Name = "Scrap", HasCap = false, StartAmount = 15 });
            resources.Resources.Add(new ResourceConfigData { Id = "food", Name = "Food", HasCap = true, StartAmount = 8, StartCap = 50 });
            resources.Resources.Add(new ResourceConfigData { Id = "water", Name = "Water", HasCap = true, StartAmount = 6, StartCap = 40 });
            resources.Resources.Add(new ResourceConfigData { Id = "weapon_parts", Name = "Weapon Parts", HasCap = false, StartAmount = 0 });
            resources.Resources.Add(new ResourceConfigData { Id = "medicine", Name = "Medicine", HasCap = true, StartAmount = 1, StartCap = 20 });

            var survivors = CreateOrLoad<SurvivorCatalogSO>(ConfigRoot + "/SurvivorCatalog.asset");
            survivors.StartingSurvivor = new StartingSurvivorConfigData
            {
                Name = "Mara",
                BackgroundId = "scavenger",
                TraitIds = new List<string> { "careful" },
                WeaponItemId = "rusty_knife",
                Skills = new List<IntPairData>
                {
                    new IntPairData("scavenging", 4),
                    new IntPairData("melee", 1),
                    new IntPairData("firearms", 0),
                    new IntPairData("survival", 2),
                    new IntPairData("mechanics", 0),
                    new IntPairData("medicine", 0)
                }
            };

            var backgrounds = CreateOrLoad<BackgroundCatalogSO>(ConfigRoot + "/BackgroundCatalog.asset");
            backgrounds.Backgrounds.Clear();
            backgrounds.Backgrounds.Add(Background("scavenger", "Scavenger", new[] { Pair("scavenging", 3), Pair("survival", 1) }, new[] { Pair("carry_capacity", 5) }));
            backgrounds.Backgrounds.Add(Background("ex_cop", "Ex-Cop", new[] { Pair("firearms", 3), Pair("medicine", 1) }, new[] { Pair("combat_morale", 5) }));
            backgrounds.Backgrounds.Add(Background("mechanic", "Mechanic", new[] { Pair("mechanics", 3) }, Array.Empty<IntPairData>()));
            backgrounds.Backgrounds.Add(Background("nurse", "Nurse", new[] { Pair("medicine", 3) }, Array.Empty<IntPairData>()));
            backgrounds.Backgrounds.Add(Background("brawler", "Brawler", new[] { Pair("melee", 3) }, new[] { Pair("max_health", 10) }));
            backgrounds.Backgrounds.Add(Background("hunter", "Hunter", new[] { Pair("firearms", 2), Pair("survival", 2) }, Array.Empty<IntPairData>()));

            var traits = CreateOrLoad<TraitCatalogSO>(ConfigRoot + "/TraitCatalog.asset");
            traits.Traits.Clear();
            traits.Traits.Add(Trait("brave", "Brave", Pair("morale", 10)));
            traits.Traits.Add(Trait("coward", "Coward", Pair("morale", -10)));
            traits.Traits.Add(Trait("careful", "Careful"));
            traits.Traits.Add(Trait("greedy", "Greedy"));
            traits.Traits.Add(Trait("lucky", "Lucky"));
            traits.Traits.Add(Trait("trigger_happy", "Trigger Happy"));
            traits.Traits.Add(Trait("tough", "Tough", Pair("max_health", 15)));
            traits.Traits.Add(Trait("old_injury", "Old Injury"));
            traits.Traits.Add(Trait("quiet", "Quiet"));
            traits.Traits.Add(Trait("clumsy", "Clumsy"));

            var policies = CreateOrLoad<ExpeditionPolicyCatalogSO>(ConfigRoot + "/ExpeditionPolicyCatalog.asset");
            policies.Policies.Clear();
            policies.Policies.Add(Policy("cautious", "Cautious", 0.8, 0.85, 1.15, 1, 1, 0.95, -1));
            policies.Policies.Add(Policy("balanced", "Balanced", 1, 1, 1, 1, 1, 1, 0));
            policies.Policies.Add(Policy("aggressive", "Aggressive", 1.2, 1.1, 0.9, 1, 1, 1.1, 1));
            policies.Policies.Add(Policy("loot_focused", "Loot Focused", 1.15, 1.25, 1.15, 1, 1, 0.95, 1));
            policies.Policies.Add(Policy("ammo_saving", "Ammo Saving", 0.9, 0.95, 1.1, 1, 1, 0.9, -2));

            var enemies = CreateOrLoad<EnemyCatalogSO>(ConfigRoot + "/EnemyCatalog.asset");
            enemies.Enemies.Clear();
            enemies.Enemies.Add(Enemy("feral_dog", "Feral Dog", 14, 0, 0.08, 3, WeaponType.Melee, 0.75, 4));
            enemies.Enemies.Add(Enemy("starving_survivor", "Starving Survivor", 18, 0, 0.04, 4, WeaponType.Melee, 0.65, 5));
            enemies.Enemies.Add(Enemy("mutant_stray", "Mutant Stray", 26, 1, 0.03, 5, WeaponType.Mutant, 0.68, 8));
            enemies.Enemies.Add(Enemy("raider", "Raider", 30, 1, 0.05, 6, WeaponType.Firearm, 0.62, 10));
            enemies.Enemies.Add(Enemy("armored_raider", "Armored Raider", 45, 4, 0.02, 7, WeaponType.Firearm, 0.6, 14));
            enemies.Enemies.Add(Enemy("mutant_brute", "Mutant Brute", 80, 2, 0.01, 11, WeaponType.Mutant, 0.7, 24));

            var items = CreateOrLoad<ItemCatalogSO>(ConfigRoot + "/ItemCatalog.asset");
            items.Items.Clear();
            items.Items.Add(Item("rusty_knife", "Rusty Knife", ItemSlot.Weapon, WeaponType.Melee, 4, 0, 0.02, 0.01, 0, 80));
            items.Items.Add(Item("metal_pipe", "Metal Pipe", ItemSlot.Weapon, WeaponType.Melee, 6, 0, 0, 0.01, 0, 90));
            items.Items.Add(Item("machete", "Machete", ItemSlot.Weapon, WeaponType.Melee, 9, 0, -0.01, 0.02, 0, 80));
            items.Items.Add(Item("rusty_revolver", "Rusty Revolver", ItemSlot.Weapon, WeaponType.Firearm, 10, 0, -0.03, 0.04, 3, 60));
            items.Items.Add(Item("sawn_off_shotgun", "Sawn-off Shotgun", ItemSlot.Weapon, WeaponType.Firearm, 16, 0, -0.08, 0.03, 5, 50));
            items.Items.Add(Item("hunting_rifle", "Hunting Rifle", ItemSlot.Weapon, WeaponType.Firearm, 14, 0, 0.04, 0.05, 3, 70));
            items.Items.Add(Item("leather_jacket", "Leather Jacket", ItemSlot.Armor, WeaponType.None, 0, 1, 0, 0, 0, 100));
            items.Items.Add(Item("scrap_armor", "Scrap Armor", ItemSlot.Armor, WeaponType.None, 0, 3, 0, 0, 0, 120));
            items.Items.Add(Item("medkit", "Medkit", ItemSlot.Utility, WeaponType.None, 0, 0, 0, 0, 0, 1));
            items.Items.Add(Item("toolkit", "Toolkit", ItemSlot.Utility, WeaponType.None, 0, 0, 0, 0, 0, 80));
            items.Items.Add(Item("ammo_pack", "Ammo Pack", ItemSlot.Utility, WeaponType.None, 0, 0, 0, 0, 0, 1));
            items.Items.Add(Item("backpack", "Backpack", ItemSlot.Utility, WeaponType.None, 0, 0, 0, 0, 0, 100, 10));

            var zones = CreateOrLoad<ZoneCatalogSO>(ConfigRoot + "/ZoneCatalog.asset");
            zones.Zones.Clear();
            zones.Zones.Add(Zone("abandoned_store", "Abandoned Store", RiskTier.Safe, 45, 30, 90, 1, 0, 5,
                new[] { Weighted("feral_dog", 60), Weighted("starving_survivor", 40) },
                new[] { Loot("scrap", 4, 10, 100), Loot("food", 1, 4, 70) },
                Array.Empty<UnlockConditionData>()));
            zones.Zones.Add(Zone("dry_suburb", "Dry Suburb", RiskTier.Safe, 90, 60, 150, 1, 1, 8,
                new[] { Weighted("feral_dog", 50), Weighted("mutant_stray", 50) },
                new[] { Loot("scrap", 5, 12, 100), Loot("water", 1, 5, 80) },
                new[] { Unlock("zone_completions", "abandoned_store", 2) }));
            zones.Zones.Add(Zone("ruined_clinic", "Ruined Clinic", RiskTier.Unstable, 180, 120, 260, 2, 1, 14,
                new[] { Weighted("starving_survivor", 50), Weighted("raider", 50) },
                new[] { Loot("medicine", 1, 3, 80), Loot("scrap", 5, 14, 100) },
                new[] { Unlock("building_level", "radio_tower", 1) }));
            zones.Zones.Add(Zone("police_outpost", "Police Outpost", RiskTier.Unstable, 300, 220, 420, 3, 2, 22,
                new[] { Weighted("raider", 70), Weighted("armored_raider", 30) },
                new[] { Loot("weapon_parts", 2, 8, 100), Loot("scrap", 8, 18, 70) },
                new[] { Unlock("building_level", "workshop", 1) }));
            zones.Zones.Add(Zone("mutant_tunnel", "Mutant Tunnel", RiskTier.Dangerous, 600, 420, 780, 4, 3, 36,
                new[] { Weighted("mutant_stray", 70), Weighted("mutant_brute", 30) },
                new[] { Loot("medicine", 2, 6, 80), Loot("scrap", 12, 30, 100) },
                new[] { Unlock("building_level", "radio_tower", 2) }));

            var buildings = CreateOrLoad<BuildingCatalogSO>(ConfigRoot + "/BuildingCatalog.asset");
            buildings.Buildings.Clear();
            buildings.Buildings.Add(Building("barracks", "Barracks", 0, true, string.Empty, string.Empty,
                BuildingLevel(0, Array.Empty<IntPairData>(), 1, 1, 0, 0),
                BuildingLevel(1, new[] { Pair("scrap", 25), Pair("food", 4) }, 2, 1, 0, 0),
                BuildingLevel(2, new[] { Pair("scrap", 60), Pair("food", 8) }, 3, 2, 0, 0),
                BuildingLevel(3, new[] { Pair("scrap", 140), Pair("food", 16) }, 5, 2, 0, 0),
                BuildingLevel(4, new[] { Pair("scrap", 300), Pair("food", 30) }, 7, 3, 0, 0)));
            buildings.Buildings.Add(Building("workshop", "Workshop", 0, true, string.Empty, string.Empty,
                BuildingLevel(0, Array.Empty<IntPairData>(), 0, 0, 0, 0),
                BuildingLevel(1, new[] { Pair("scrap", 35) }, 0, 0, 0, 0),
                BuildingLevel(2, new[] { Pair("scrap", 90), Pair("weapon_parts", 4) }, 0, 0, 0, 0),
                BuildingLevel(3, new[] { Pair("scrap", 200), Pair("weapon_parts", 12) }, 0, 0, 0, 0)));
            buildings.Buildings.Add(Building("water_collector", "Water Collector", 0, true, "water", "water",
                BuildingLevel(0, Array.Empty<IntPairData>(), 0, 0, 40, 0),
                BuildingLevel(1, new[] { Pair("scrap", 30) }, 0, 0, 60, 1),
                BuildingLevel(2, new[] { Pair("scrap", 75), Pair("weapon_parts", 5) }, 0, 0, 100, 2),
                BuildingLevel(3, new[] { Pair("scrap", 160), Pair("weapon_parts", 15) }, 0, 0, 160, 3)));
            buildings.Buildings.Add(Building("infirmary", "Infirmary", 0, true, "medicine", string.Empty,
                BuildingLevel(0, Array.Empty<IntPairData>(), 0, 0, 20, 0),
                BuildingLevel(1, new[] { Pair("scrap", 50), Pair("medicine", 2) }, 0, 0, 30, 0),
                BuildingLevel(2, new[] { Pair("scrap", 120), Pair("medicine", 6) }, 0, 0, 45, 0),
                BuildingLevel(3, new[] { Pair("scrap", 260), Pair("medicine", 12) }, 0, 0, 60, 0)));
            buildings.Buildings.Add(Building("radio_tower", "Radio Tower", 0, true, string.Empty, string.Empty,
                BuildingLevel(0, Array.Empty<IntPairData>(), 0, 0, 0, 0),
                BuildingLevel(1, new[] { Pair("scrap", 45), Pair("weapon_parts", 2) }, 0, 0, 0, 0),
                BuildingLevel(2, new[] { Pair("scrap", 120), Pair("weapon_parts", 10) }, 0, 0, 0, 0),
                BuildingLevel(3, new[] { Pair("scrap", 260), Pair("weapon_parts", 25) }, 0, 0, 0, 0)));

            var balance = CreateOrLoad<BalanceConfigSO>(ConfigRoot + "/BalanceConfig.asset");
            balance.Balance = new BalanceConfigData();

            var uiCatalog = CreateOrLoad<CampUiCatalogSO>(CampUiCatalogPath);
            FillCampUiCatalog(uiCatalog);

            var database = CreateOrLoad<GameConfigDatabaseSO>(ConfigRoot + "/GameConfigDatabase.asset");
            database.Resources = resources;
            database.Survivors = survivors;
            database.Backgrounds = backgrounds;
            database.Traits = traits;
            database.Policies = policies;
            database.Zones = zones;
            database.Enemies = enemies;
            database.Items = items;
            database.Buildings = buildings;
            database.Balance = balance;

            MarkDirty(resources, survivors, backgrounds, traits, policies, enemies, items, zones, buildings, balance, uiCatalog, database);
            AssetDatabase.SaveAssets();
            CampGameObjectUiPrefabGenerator.Generate();
            GenerateBootScene(database, uiCatalog);
            AssetDatabase.Refresh();
            Debug.Log("Ashfall Camp boot core assets generated.");
        }

        private static void GenerateBootScene(GameConfigDatabaseSO database, CampUiCatalogSO uiCatalog)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);
            scene.name = "SC_Boot";

            var cameraObject = UnityEditor.ObjectFactory.CreateGameObject("Main Camera", typeof(Camera), typeof(AudioListener));
            cameraObject.tag = "MainCamera";
            cameraObject.transform.position = new Vector3(0, 0, -10);
            cameraObject.GetComponent<Camera>().orthographic = true;

            var lightType = Type.GetType("UnityEngine.Rendering.Universal.Light2D, Unity.RenderPipelines.Universal.Runtime");
            var lightObject = lightType != null
                ? UnityEditor.ObjectFactory.CreateGameObject("Global Light 2D", lightType)
                : UnityEditor.ObjectFactory.CreateGameObject("Global Light 2D", typeof(Light));
            lightObject.transform.position = Vector3.zero;
            var fallbackLight = lightObject.GetComponent<Light>();
            if (fallbackLight != null)
            {
                fallbackLight.type = LightType.Directional;
                fallbackLight.intensity = 1f;
            }

            var inputModuleType = Type.GetType("UnityEngine.InputSystem.UI.InputSystemUIInputModule, Unity.InputSystem");
            UnityEditor.ObjectFactory.CreateGameObject(
                "EventSystem",
                inputModuleType != null
                    ? new[] { typeof(EventSystem), inputModuleType }
                    : new[] { typeof(EventSystem), typeof(StandaloneInputModule) });

            var scopeObject = UnityEditor.ObjectFactory.CreateGameObject("ProjectLifetimeScope", typeof(ProjectLifetimeScope));
            var dashboardPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(CampDashboardPrefabPath);
            if (dashboardPrefab == null)
            {
                throw new InvalidOperationException("Camp dashboard prefab is missing: " + CampDashboardPrefabPath);
            }

            var dashboardObject = (GameObject)UnityEditor.PrefabUtility.InstantiatePrefab(dashboardPrefab, scene);
            dashboardObject.name = "PF_CampDashboard";
            var scope = scopeObject.GetComponent<ProjectLifetimeScope>();
            var dashboardView = dashboardObject.GetComponent<CampDashboardView>();
            if (dashboardView == null)
            {
                dashboardView = dashboardObject.AddComponent<CampDashboardView>();
            }

            dashboardView.SetCatalog(uiCatalog);
            scope.SetDashboardReferences(database, dashboardView);
            EditorUtility.SetDirty(dashboardView);
            EditorUtility.SetDirty(scope);

            EditorSceneManager.SaveScene(scene, BootScenePath);
            AssetDatabase.SaveAssets();
            var scenes = new List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(BootScenePath, true) };
            if (AssetDatabase.LoadAssetAtPath<SceneAsset>(SampleScenePath) != null)
            {
                scenes.Add(new EditorBuildSettingsScene(SampleScenePath, true));
            }
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        private static T CreateOrLoad<T>(string path) where T : ScriptableObject
        {
            var asset = AssetDatabase.LoadAssetAtPath<T>(path);
            if (asset != null && !HasMissingScript(path)) return asset;
            if (AssetDatabase.LoadMainAssetAtPath(path) != null)
            {
                AssetDatabase.DeleteAsset(path);
            }
            asset = ScriptableObject.CreateInstance<T>();
            AssetDatabase.CreateAsset(asset, path);
            return asset;
        }

        private static bool HasMissingScript(string assetPath)
        {
            var fullPath = Path.Combine(Directory.GetCurrentDirectory(), assetPath);
            return File.Exists(fullPath) && File.ReadAllText(fullPath).Contains("m_Script: {fileID: 0}");
        }

        private static void FillCampUiCatalog(CampUiCatalogSO catalog)
        {
            catalog.BrandBannerText = "A";
            catalog.BrandTitle = "ASHFALL CAMP";
            catalog.BrandSubtitle = "REBUILD  *  SURVIVE  *  THRIVE";
            catalog.TopResourcePlate = LoadTexture(Root + "/Art/UI/Production/Shared/ui_topbar_resource_plate.png");
            catalog.BottomNavPlate = LoadTexture(Root + "/Art/UI/Production/Shared/ui_navbar_bottom_tabs.png");
            catalog.BuildingScreenTitle = "BUILDINGS";
            catalog.CampStatusTitle = "CAMP STATUS";
            catalog.CampStatusBody = "Our camp is stable and ready to grow.";
            catalog.CampStatusHealthyLabel = "THRIVING";
            catalog.CampStatusStrainedLabel = "STRAINED";
            catalog.CampStatusBadgeLabel = "OK";
            catalog.StatusResourceId = "food";
            catalog.StatusStrainedBelowAmount = 2;
            catalog.MoraleLabel = "Morale";
            catalog.SafetyLabel = "Safety";
            catalog.SuppliesLabel = "Supplies";
            catalog.MoraleValueLabel = "Good";
            catalog.SafetyValueLabel = "Secure";
            catalog.SuppliesValueLabel = "Stable";
            catalog.CampSummaryTitle = "CAMP SUMMARY";
            catalog.CampSummaryNote = "Expand the camp to unlock routes and systems.";
            catalog.PopulationLabel = "Population";
            catalog.IdleSurvivorsLabel = "Idle Survivors";
            catalog.BuildingsLabel = "Buildings";
            catalog.ProductionMetricLabel = "Water / Hour";
            catalog.ProductionMetricResourceId = "water";
            catalog.RecentAlertsTitle = "RECENT ALERTS";
            catalog.CampOverviewTitle = "CAMP OVERVIEW";
            catalog.ActiveExpeditionsTitle = "ACTIVE EXPEDITIONS";
            catalog.RadioIntelTitle = "RADIO INTEL";
            catalog.RadioIntelBody = "No transmission yet. Upgrade the radio tower to widen the search.";
            catalog.RadioIntelButton = "LISTEN";
            catalog.EmptyBuildingTitle = "BUILD NEW STRUCTURE";
            catalog.UpgradeCostLabel = "UPGRADE COST";
            catalog.UpgradeButtonLabel = "UPGRADE";
            catalog.MaxButtonLabel = "MAX";
            catalog.LockedButtonLabel = "LOCKED";
            catalog.NeedResourcesButtonLabel = "NEED";
            catalog.MaxCostLabel = "MAX";
            catalog.SurvivorEffectFormat = "Survivor cap {0}, squad size {1}.";
            catalog.ResourceCapEffectFormat = "{0} cap {1}, +{2}/h.";
            catalog.ResourceCapOnlyEffectFormat = "{0} cap {1}.";
            catalog.RouteUnlockEffectLabel = "Unlocks routes and future camp systems.";
            catalog.IdleSuffixLabel = "idle";
            catalog.PerHourSuffixLabel = "/h";
            catalog.LevelLabelFormat = "Level {0}";

            var scrap = LoadTexture(Root + "/Art/UI/Production/Icons/Resources/ui_icon_resource_scrap.png");
            var food = LoadTexture(Root + "/Art/UI/Production/Icons/Resources/ui_icon_resource_food.png");
            var water = LoadTexture(Root + "/Art/UI/Production/Icons/Resources/ui_icon_resource_water.png");
            var medicine = LoadTexture(Root + "/Art/UI/Production/Icons/Resources/ui_icon_resource_medicine.png");
            var parts = LoadTexture(Root + "/Art/UI/Production/Icons/Resources/ui_icon_resource_parts.png");

            catalog.ResourceBar.Clear();
            catalog.ResourceBar.Add(ResourceEntry("scrap", "SCRAP", scrap, false));
            catalog.ResourceBar.Add(ResourceEntry("food", "FOOD", food, false));
            catalog.ResourceBar.Add(ResourceEntry("water", "WATER", water, false));
            catalog.ResourceBar.Add(ResourceEntry("medicine", "MEDICINE", medicine, false));
            catalog.ResourceBar.Add(ResourceEntry("weapon_parts", "PARTS", parts, false));
            catalog.ResourceBar.Add(ResourceEntry("survivors", "CAPACITY", null, true));

            catalog.BuildingFilters.Clear();
            catalog.BuildingFilters.Add(Filter("all", "ALL", true));
            catalog.BuildingFilters.Add(Filter("production", "PRODUCTION", false));
            catalog.BuildingFilters.Add(Filter("support", "SUPPORT", false));
            catalog.BuildingFilters.Add(Filter("utility", "UTILITY", false));
            catalog.BuildingFilters.Add(Filter("radio", "RADIO", false));

            catalog.Buildings.Clear();
            catalog.Buildings.Add(BuildingUi("barracks", "Population capacity and squad size.", null, null, 10, 18));
            catalog.Buildings.Add(BuildingUi("workshop", "Crafting, repairs, and route unlocks.", null, null, 42, 44));
            catalog.Buildings.Add(BuildingUi("water_collector", "Water storage and passive production.", water, null, 8, 68));
            catalog.Buildings.Add(BuildingUi("infirmary", "Medicine storage and recovery room.", medicine, null, 56, 64));
            catalog.Buildings.Add(BuildingUi("radio_tower", "Signals, intel range, and mission unlocks.", null, null, 60, 20));

            catalog.Alerts.Clear();
            catalog.Alerts.Add(Alert("UPGRADE AVAILABLE", "Spend scrap to improve camp systems.", catalog.Theme.Amber));
            catalog.Alerts.Add(Alert("ACTIVE EXPEDITIONS", "Launch squads from the expedition tab.", catalog.Theme.Teal));
            catalog.Alerts.Add(Alert("LOW SUPPLIES", "Food and water caps are tracked here.", catalog.Theme.Rust));

            catalog.ExpeditionCards.Clear();
            catalog.ExpeditionCards.Add(ExpeditionUi("ABANDONED STORE", "Scavenge", "Ready"));
            catalog.ExpeditionCards.Add(ExpeditionUi("DRY SUBURB", "Locked", "Need clears"));

            catalog.NavItems.Clear();
            catalog.NavItems.Add(Nav("expeditions", "EXPEDITIONS", false));
            catalog.NavItems.Add(Nav("survivors", "SURVIVORS", false));
            catalog.NavItems.Add(Nav("buildings", "BUILDINGS", true));
            catalog.NavItems.Add(Nav("workshop", "WORKSHOP", false));
            catalog.NavItems.Add(Nav("radio", "RADIO", false));
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }

        private static void MarkDirty(params UnityEngine.Object[] assets)
        {
            foreach (var asset in assets)
            {
                EditorUtility.SetDirty(asset);
            }
        }

        private static IntPairData Pair(string id, int value)
        {
            return new IntPairData(id, value);
        }

        private static WeightedEntryData Weighted(string id, int weight)
        {
            return new WeightedEntryData(id, weight);
        }

        private static LootTableEntryData Loot(string resourceId, int min, int max, int weight)
        {
            return new LootTableEntryData(resourceId, min, max, weight);
        }

        private static UnlockConditionData Unlock(string type, string id, int value)
        {
            return new UnlockConditionData(type, id, value);
        }

        private static BackgroundConfigData Background(string id, string name, IEnumerable<IntPairData> skills, IEnumerable<IntPairData> stats)
        {
            return new BackgroundConfigData { Id = id, Name = name, SkillBonuses = new List<IntPairData>(skills), StatBonuses = new List<IntPairData>(stats) };
        }

        private static TraitConfigData Trait(string id, string name, params IntPairData[] stats)
        {
            return new TraitConfigData { Id = id, Name = name, StatModifiers = new List<IntPairData>(stats) };
        }

        private static ExpeditionPolicyConfigData Policy(string id, string name, double risk, double loot, double duration, double food, double water, double power, int noise)
        {
            return new ExpeditionPolicyConfigData { Id = id, Name = name, RiskModifier = risk, LootModifier = loot, DurationModifier = duration, FoodModifier = food, WaterModifier = water, PowerModifier = power, NoiseModifier = noise };
        }

        private static EnemyConfigData Enemy(string id, string name, int hp, int armor, double evasion, int damage, WeaponType attackType, double accuracy, int xp)
        {
            return new EnemyConfigData { Id = id, Name = name, MaxHealth = hp, Armor = armor, Evasion = evasion, BaseDamage = damage, AttackType = attackType, Accuracy = accuracy, XpReward = xp };
        }

        private static ItemConfigData Item(string id, string name, ItemSlot slot, WeaponType weaponType, int damage, int armor, double accuracy, double crit, int noise, int durability, int carry = 0)
        {
            return new ItemConfigData { Id = id, Name = name, Slot = slot, WeaponType = weaponType, BaseDamage = damage, Armor = armor, AccuracyBonus = accuracy, CritBonus = crit, NoisePerAttack = noise, MaxDurability = durability, CarryCapacityBonus = carry };
        }

        private static ZoneConfigData Zone(string id, string name, RiskTier risk, int duration, int minDuration, int maxDuration, int food, int water, int power, IEnumerable<WeightedEntryData> enemies, IEnumerable<LootTableEntryData> loot, IEnumerable<UnlockConditionData> unlocks)
        {
            return new ZoneConfigData
            {
                Id = id,
                Name = name,
                RiskTier = risk,
                BaseDurationSeconds = duration,
                MinDurationSeconds = minDuration,
                MaxDurationSeconds = maxDuration,
                FoodCostPerSurvivor = food,
                WaterCostPerSurvivor = water,
                RecommendedPower = power,
                BaseAmbushChance = risk == RiskTier.Safe ? 0.05 : risk == RiskTier.Unstable ? 0.12 : 0.2,
                EnemyTable = new List<WeightedEntryData>(enemies),
                LootTable = new List<LootTableEntryData>(loot),
                UnlockConditions = new List<UnlockConditionData>(unlocks)
            };
        }

        private static BuildingConfigData Building(string id, string name, int level, bool unlocked, string affectedResourceId, string producedResourceId, params BuildingLevelConfigData[] levels)
        {
            return new BuildingConfigData
            {
                Id = id,
                Name = name,
                StartingLevel = level,
                StartsUnlocked = unlocked,
                AffectedResourceId = affectedResourceId,
                ProducedResourceId = producedResourceId,
                Levels = new List<BuildingLevelConfigData>(levels)
            };
        }

        private static BuildingLevelConfigData BuildingLevel(int level, IEnumerable<IntPairData> cost, int survivorCap, int squadSize, int resourceCap, int resourcePerMinute)
        {
            return new BuildingLevelConfigData
            {
                Level = level,
                Cost = new List<IntPairData>(cost),
                SurvivorCap = survivorCap,
                SquadSize = squadSize,
                ResourceCap = resourceCap,
                ResourcePerMinute = resourcePerMinute
            };
        }

        private static Texture2D LoadTexture(string path)
        {
            return AssetDatabase.LoadAssetAtPath<Texture2D>(path);
        }

        private static ResourceUiEntry ResourceEntry(string id, string label, Texture2D icon, bool survivorCapacity)
        {
            return new ResourceUiEntry { Id = id, Label = label, Icon = icon, UsesSurvivorCapacity = survivorCapacity };
        }

        private static FilterUiEntry Filter(string id, string label, bool active)
        {
            return new FilterUiEntry { Id = id, Label = label, IsActive = active };
        }

        private static BuildingUiEntry BuildingUi(string buildingId, string description, Texture2D icon, Texture2D image, float left, float top)
        {
            return new BuildingUiEntry { BuildingId = buildingId, Description = description, Icon = icon, Image = image, OverviewLeftPercent = left, OverviewTopPercent = top };
        }

        private static AlertUiEntry Alert(string title, string body, Color color)
        {
            return new AlertUiEntry { Title = title, Body = body, ToneColor = color };
        }

        private static ExpeditionUiEntry ExpeditionUi(string title, string subtitle, string status)
        {
            return new ExpeditionUiEntry { Title = title, Subtitle = subtitle, Status = status };
        }

        private static NavUiEntry Nav(string id, string label, bool active)
        {
            return new NavUiEntry { Id = id, Label = label, IsActive = active };
        }
    }
}
