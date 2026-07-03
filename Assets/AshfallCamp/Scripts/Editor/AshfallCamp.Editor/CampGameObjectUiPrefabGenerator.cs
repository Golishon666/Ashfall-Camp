using System;
using System.Linq;
using System.Threading;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using AshfallCamp.Presentation;
using TMPro;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;

namespace AshfallCamp.Editor
{
    public static class CampGameObjectUiPrefabGenerator
    {
        private const string Root = "Assets/AshfallCamp";
        private const string PrefabRoot = Root + "/Prefabs";
        private const string UiPrefabRoot = PrefabRoot + "/UI";
        private const string PrefabPath = UiPrefabRoot + "/PF_CampDashboard.prefab";
        private const string CatalogPath = Root + "/UI/CampUiCatalog.asset";
        private const string ConfigPath = Root + "/Configs/GameConfigDatabase.asset";

        [MenuItem("Tools/Ashfall Camp/Generate GameObject Camp UI Prefab")]
        public static void Generate()
        {
            EnsureFolder(Root, "Prefabs");
            EnsureFolder(PrefabRoot, "UI");

            var catalog = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>(CatalogPath);
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(ConfigPath);
            if (catalog == null) throw new InvalidOperationException("Camp UI catalog is missing: " + CatalogPath);
            if (database == null) throw new InvalidOperationException("Game config database is missing: " + ConfigPath);

            var config = LoadConfigSnapshot(database);
            var state = GameStateFactory.CreateNew(config, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());

            var root = CreateCanvasRoot();
            root.AddComponent<CampDashboardView>().SetCatalog(catalog);
            BuildDashboard(root.transform, catalog, config, state);

            PrefabUtility.SaveAsPrefabAsset(root, PrefabPath);
            UnityEngine.Object.DestroyImmediate(root);
            AssetDatabase.SaveAssets();
            AssetDatabase.Refresh();
            Debug.Log("Ashfall Camp GameObject UI prefab generated: " + PrefabPath);
        }

        private static GameObject CreateCanvasRoot()
        {
            var root = new GameObject("PF_CampDashboard", typeof(RectTransform), typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
            var rect = root.GetComponent<RectTransform>();
            rect.sizeDelta = new Vector2(1920, 1080);

            var canvas = root.GetComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 10;

            var scaler = root.GetComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.screenMatchMode = CanvasScaler.ScreenMatchMode.MatchWidthOrHeight;
            scaler.matchWidthOrHeight = 0.5f;

            return root;
        }

        private static GameConfigSnapshot LoadConfigSnapshot(GameConfigDatabaseSO database)
        {
            var provider = new ScriptableObjectGameConfigProvider(database);
            var task = typeof(ScriptableObjectGameConfigProvider)
                .GetMethod("LoadAsync")
                .Invoke(provider, new object[] { CancellationToken.None });
            var awaiter = task.GetType().GetMethod("GetAwaiter").Invoke(task, null);
            return (GameConfigSnapshot)awaiter.GetType().GetMethod("GetResult").Invoke(awaiter, null);
        }

        private static void BuildDashboard(Transform root, CampUiCatalogSO catalog, GameConfigSnapshot config, GameState state)
        {
            var theme = catalog.Theme;
            AddPanel(root, "Background", 0, 0, 1920, 1080, theme.Paper);
            BuildTopBar(root, catalog, config, state);
            BuildLeftColumn(root, catalog, config, state);
            BuildBuildingGrid(root, catalog, config, state);
            BuildRightColumn(root, catalog, config);
            BuildBottomNav(root, catalog);
            OrganizeHierarchy(root, catalog);
        }

        private static void BuildTopBar(Transform root, CampUiCatalogSO catalog, GameConfigSnapshot config, GameState state)
        {
            var theme = catalog.Theme;
            AddText(root, "BrandTitle", catalog.BrandTitle, 88, 20, 310, 34, 30, theme.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
            AddText(root, "BrandSubtitle", catalog.BrandSubtitle, 88, 55, 360, 18, 11, theme.Teal, TextAlignmentOptions.Left, FontStyles.Bold);
            AddPanel(root, "BrandBadge", 28, 18, 52, 52, theme.Teal);
            AddText(root, "BrandBadgeText", catalog.BrandBannerText, 28, 23, 52, 44, 27, theme.Paper, TextAlignmentOptions.Center, FontStyles.Bold);

            AddRaw(root, "TopResourcePlate", catalog.TopResourcePlate, 560, 18, 1320, 46, new Color(1, 1, 1, 0.86f));
            var x = 610f;
            foreach (var entry in catalog.ResourceBar)
            {
                AddResourceCounter(root, catalog, config, state, entry, x, 25);
                x += 198f;
            }
        }

        private static void AddResourceCounter(Transform root, CampUiCatalogSO catalog, GameConfigSnapshot config, GameState state, ResourceUiEntry entry, float x, float y)
        {
            var theme = catalog.Theme;
            AddRaw(root, "Icon_" + entry.Id, entry.Icon, x, y + 6, 25, 25, Color.white);
            AddText(root, "Label_" + entry.Id, entry.Label, x + 34, y, 126, 14, 10, theme.MutedInk, TextAlignmentOptions.Left, FontStyles.Bold);

            var value = entry.UsesSurvivorCapacity
                ? state.Survivors.Count + " / " + state.SurvivorCap
                : ResourceValue(state, config, entry.Id);
            AddText(root, "Value_" + entry.Id, value, x + 34, y + 16, 126, 24, 18, theme.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
        }

        private static string ResourceValue(GameState state, GameConfigSnapshot config, string resourceId)
        {
            var amount = state.Resources.ContainsKey(resourceId) ? state.Resources[resourceId] : 0;
            ResourceDefinition definition;
            if (config.Resources.TryGetValue(resourceId, out definition) && definition.HasCap)
            {
                var cap = state.ResourceCaps.ContainsKey(resourceId) ? state.ResourceCaps[resourceId] : definition.StartCap;
                return amount + " / " + cap;
            }

            return amount.ToString();
        }

        private static void BuildLeftColumn(Transform root, CampUiCatalogSO catalog, GameConfigSnapshot config, GameState state)
        {
            var theme = catalog.Theme;
            AddTitlePanel(root, "CampStatus", catalog.CampStatusTitle, 28, 102, 300, 230, theme);
            AddPanel(root, "CampStatusBadge", 48, 150, 56, 56, theme.Sage);
            AddText(root, "CampStatusBadgeText", catalog.CampStatusBadgeLabel, 48, 160, 56, 36, 15, theme.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            AddText(root, "CampStatusLabel", catalog.CampStatusHealthyLabel, 116, 148, 180, 30, 23, theme.Sage, TextAlignmentOptions.Left, FontStyles.Bold);
            AddText(root, "CampStatusBody", catalog.CampStatusBody, 48, 205, 240, 36, 13, theme.MutedInk, TextAlignmentOptions.Left, FontStyles.Normal);
            AddMeter(root, catalog.MoraleLabel, catalog.MoraleValueLabel, 48, 258, 0.72f, theme);
            AddMeter(root, catalog.SafetyLabel, catalog.SafetyValueLabel, 48, 284, 0.66f, theme);
            AddMeter(root, catalog.SuppliesLabel, catalog.SuppliesValueLabel, 48, 310, 0.58f, theme);

            AddTitlePanel(root, "CampSummary", catalog.CampSummaryTitle, 28, 350, 300, 248, theme);
            AddSummaryRow(root, catalog.PopulationLabel, state.Survivors.Count + " / " + state.SurvivorCap, 50, 398, theme);
            AddSummaryRow(root, catalog.IdleSurvivorsLabel, state.Survivors.Count.ToString(), 50, 430, theme);
            AddSummaryRow(root, catalog.BuildingsLabel, state.Buildings.Count(b => b.Value.IsUnlocked) + " / " + state.Buildings.Count, 50, 462, theme);
            AddSummaryRow(root, catalog.ProductionMetricLabel, "+" + CalculateProduction(config, state, catalog.ProductionMetricResourceId) + catalog.PerHourSuffixLabel, 50, 494, theme);
            AddPanel(root, "CampSummaryNotePlate", 50, 530, 256, 46, new Color(theme.PaperDark.r, theme.PaperDark.g, theme.PaperDark.b, 0.55f));
            AddText(root, "CampSummaryNote", catalog.CampSummaryNote, 62, 536, 232, 34, 12, theme.MutedInk, TextAlignmentOptions.Center, FontStyles.Normal);

            AddTitlePanel(root, "RecentAlerts", catalog.RecentAlertsTitle, 28, 616, 300, 372, theme);
            var y = 668f;
            foreach (var alert in catalog.Alerts)
            {
                AddAlert(root, alert, 50, y, theme);
                y += 92f;
            }
        }

        private static int CalculateProduction(GameConfigSnapshot config, GameState state, string resourceId)
        {
            var perMinute = 0;
            foreach (var building in state.Buildings.Values)
            {
                BuildingDefinition definition;
                if (!config.Buildings.TryGetValue(building.Id, out definition)) continue;
                if (definition.ProducedResourceId != resourceId) continue;
                var level = BuildingSystem.GetLevel(definition, building.Level);
                if (level != null) perMinute += level.ResourcePerMinute;
            }

            return perMinute * 60;
        }

        private static void AddMeter(Transform root, string label, string value, float x, float y, float fill, CampUiTheme theme)
        {
            AddText(root, "MeterLabel_" + label, label, x, y, 92, 20, 12, theme.MutedInk, TextAlignmentOptions.Left, FontStyles.Normal);
            AddPanel(root, "MeterTrack_" + label, x + 94, y + 8, 72, 6, new Color(theme.Line.r, theme.Line.g, theme.Line.b, 0.55f));
            AddPanel(root, "MeterFill_" + label, x + 94, y + 8, 72 * fill, 6, theme.Sage);
            AddText(root, "MeterValue_" + label, value, x + 176, y, 80, 20, 12, theme.MutedInk, TextAlignmentOptions.Left, FontStyles.Normal);
        }

        private static void AddSummaryRow(Transform root, string label, string value, float x, float y, CampUiTheme theme)
        {
            AddText(root, "SummaryLabel_" + label, label, x, y, 150, 24, 14, theme.MutedInk, TextAlignmentOptions.Left, FontStyles.Normal);
            AddText(root, "SummaryValue_" + label, value, x + 166, y, 88, 24, 18, theme.Ink, TextAlignmentOptions.Right, FontStyles.Bold);
        }

        private static void AddAlert(Transform root, AlertUiEntry alert, float x, float y, CampUiTheme theme)
        {
            AddPanel(root, "Alert_" + alert.Title, x, y, 256, 70, new Color(alert.ToneColor.r, alert.ToneColor.g, alert.ToneColor.b, 0.08f));
            AddPanel(root, "AlertDot_" + alert.Title, x + 12, y + 18, 34, 34, alert.ToneColor);
            AddText(root, "AlertDotText_" + alert.Title, "!", x + 12, y + 22, 34, 24, 17, theme.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            AddText(root, "AlertTitle_" + alert.Title, alert.Title, x + 56, y + 12, 180, 20, 12, alert.ToneColor, TextAlignmentOptions.Left, FontStyles.Bold);
            AddText(root, "AlertBody_" + alert.Title, alert.Body, x + 56, y + 34, 184, 30, 11, theme.MutedInk, TextAlignmentOptions.Left, FontStyles.Normal);
        }

        private static void BuildBuildingGrid(Transform root, CampUiCatalogSO catalog, GameConfigSnapshot config, GameState state)
        {
            var theme = catalog.Theme;
            AddText(root, "BuildingsTitle", catalog.BuildingScreenTitle, 356, 103, 300, 52, 42, theme.Teal, TextAlignmentOptions.Left, FontStyles.Bold);

            var tabX = 650f;
            foreach (var filter in catalog.BuildingFilters)
            {
                AddPanel(root, "Filter_" + filter.Id, tabX, 112, 138, 38, filter.IsActive ? theme.Teal : new Color(theme.PaperDark.r, theme.PaperDark.g, theme.PaperDark.b, 0.55f));
                AddText(root, "FilterLabel_" + filter.Id, filter.Label, tabX, 120, 138, 20, 12, filter.IsActive ? theme.Paper : theme.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
                tabX += 150f;
            }

            for (var i = 0; i < catalog.Buildings.Count; i++)
            {
                var col = i % 3;
                var row = i / 3;
                AddBuildingCard(root, catalog, config, state, catalog.Buildings[i], 356 + col * 344, 176 + row * 292, theme);
            }

            AddPanel(root, "EmptyBuildingSlot", 356 + 2 * 344, 176 + 292, 320, 270, new Color(theme.PaperDark.r, theme.PaperDark.g, theme.PaperDark.b, 0.22f));
            AddText(root, "EmptyBuildingPlus", "+", 478 + 2 * 344, 244 + 292, 76, 76, 44, theme.MutedInk, TextAlignmentOptions.Center, FontStyles.Normal);
            AddText(root, "EmptyBuildingTitle", catalog.EmptyBuildingTitle, 386 + 2 * 344, 328 + 292, 260, 26, 13, theme.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private static void AddBuildingCard(Transform root, CampUiCatalogSO catalog, GameConfigSnapshot config, GameState state, BuildingUiEntry entry, float x, float y, CampUiTheme theme)
        {
            BuildingDefinition definition;
            BuildingState building;
            if (!config.Buildings.TryGetValue(entry.BuildingId, out definition) || !state.Buildings.TryGetValue(entry.BuildingId, out building)) return;

            AddPanel(root, "BuildingCard_" + entry.BuildingId, x, y, 320, 270, new Color(theme.PaperDark.r, theme.PaperDark.g, theme.PaperDark.b, 0.42f));
            AddPanel(root, "BuildingImageSlot_" + entry.BuildingId, x + 18, y + 20, 136, 132, new Color(0.52f, 0.62f, 0.58f, 0.22f));
            AddText(root, "BuildingImageLetter_" + entry.BuildingId, definition.Name.Substring(0, 1).ToUpperInvariant(), x + 18, y + 52, 136, 70, 48, new Color(0.18f, 0.38f, 0.4f, 0.32f), TextAlignmentOptions.Center, FontStyles.Bold);
            AddRaw(root, "BuildingIcon_" + entry.BuildingId, entry.Icon, x + 170, y + 22, 28, 28, theme.Teal);
            AddText(root, "BuildingName_" + entry.BuildingId, definition.Name, x + 206, y + 18, 100, 26, 18, theme.Teal, TextAlignmentOptions.Left, FontStyles.Bold);
            AddText(root, "BuildingLevel_" + entry.BuildingId, string.Format(catalog.LevelLabelFormat, building.Level), x + 206, y + 44, 90, 20, 12, theme.Ink, TextAlignmentOptions.Left, FontStyles.Normal);
            AddText(root, "BuildingDescription_" + entry.BuildingId, entry.Description, x + 170, y + 74, 126, 58, 11, theme.Ink, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            AddText(root, "BuildingEffect_" + entry.BuildingId, BuildEffectText(catalog, definition, building), x + 170, y + 136, 126, 42, 10, theme.MutedInk, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            AddPanel(root, "BuildingCostRow_" + entry.BuildingId, x + 14, y + 204, 292, 46, new Color(theme.Line.r, theme.Line.g, theme.Line.b, 0.10f));
            AddText(root, "BuildingCostLabel_" + entry.BuildingId, catalog.UpgradeCostLabel, x + 22, y + 218, 88, 20, 9, theme.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
            AddCostChips(root, catalog, definition, building, x + 112, y + 214);
            var buttonImage = AddPanel(root, "BuildingUpgradeButton_" + entry.BuildingId, x + 218, y + 214, 78, 24, new Color(theme.Rust.r, theme.Rust.g, theme.Rust.b, 0.28f));
            buttonImage.raycastTarget = true;
            var button = buttonImage.gameObject.AddComponent<Button>();
            button.targetGraphic = buttonImage;
            button.interactable = BuildingSystem.ValidateUpgrade(state, config, definition.Id).IsValid;
            AddText(root, "BuildingUpgradeLabel_" + entry.BuildingId, GetUpgradeLabel(catalog, state, config, definition, building), x + 218, y + 219, 78, 14, 9, theme.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private static string BuildEffectText(CampUiCatalogSO catalog, BuildingDefinition definition, BuildingState building)
        {
            var level = BuildingSystem.GetLevel(definition, building.Level);
            if (level == null) return string.Empty;
            if (level.SurvivorCap > 0) return string.Format(catalog.SurvivorEffectFormat, level.SurvivorCap, Math.Max(1, level.SquadSize));
            if (!string.IsNullOrWhiteSpace(definition.AffectedResourceId) && level.ResourceCap > 0)
            {
                return level.ResourcePerMinute > 0
                    ? string.Format(catalog.ResourceCapEffectFormat, definition.AffectedResourceId, level.ResourceCap, level.ResourcePerMinute * 60)
                    : string.Format(catalog.ResourceCapOnlyEffectFormat, definition.AffectedResourceId, level.ResourceCap);
            }

            return catalog.RouteUnlockEffectLabel;
        }

        private static void AddCostChips(Transform root, CampUiCatalogSO catalog, BuildingDefinition definition, BuildingState building, float x, float y)
        {
            var next = BuildingSystem.GetLevel(definition, building.Level + 1);
            var resourceIds = definition.Levels
                .SelectMany(level => level.Cost.Keys)
                .Distinct(StringComparer.Ordinal)
                .Take(2)
                .ToList();

            var chipX = x;
            foreach (var resourceId in resourceIds)
            {
                var amount = 0;
                var isVisible = next != null && next.Cost.TryGetValue(resourceId, out amount);
                var icon = catalog.ResourceBar.FirstOrDefault(resource => resource.Id == resourceId)?.Icon;
                var iconImage = AddRaw(root, "CostIcon_" + definition.Id + "_" + resourceId, icon, chipX, y, 20, 20, new Color(0.16f, 0.14f, 0.11f, 0.35f));
                var valueText = AddText(root, "CostValue_" + definition.Id + "_" + resourceId, isVisible ? amount.ToString() : string.Empty, chipX + 24, y, 34, 20, 12, catalog.Theme.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
                iconImage.gameObject.SetActive(isVisible);
                valueText.gameObject.SetActive(isVisible);
                chipX += 58;
            }

            var maxText = AddText(root, "CostMax_" + definition.Id, catalog.MaxCostLabel, x, y, 80, 20, 13, catalog.Theme.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
            maxText.gameObject.SetActive(next == null);
        }

        private static string GetUpgradeLabel(CampUiCatalogSO catalog, GameState state, GameConfigSnapshot config, BuildingDefinition definition, BuildingState building)
        {
            if (BuildingSystem.GetLevel(definition, building.Level + 1) == null) return catalog.MaxButtonLabel;
            if (!building.IsUnlocked) return catalog.LockedButtonLabel;
            return BuildingSystem.ValidateUpgrade(state, config, definition.Id).IsValid ? catalog.UpgradeButtonLabel : catalog.NeedResourcesButtonLabel;
        }

        private static void BuildRightColumn(Transform root, CampUiCatalogSO catalog, GameConfigSnapshot config)
        {
            var theme = catalog.Theme;
            AddTitlePanel(root, "CampOverview", catalog.CampOverviewTitle, 1460, 102, 430, 315, theme);
            AddPanel(root, "CampMapPlate", 1490, 158, 370, 215, new Color(0.52f, 0.62f, 0.58f, 0.18f));
            foreach (var entry in catalog.Buildings)
            {
                BuildingDefinition definition;
                var label = config.Buildings.TryGetValue(entry.BuildingId, out definition) ? definition.Name : entry.BuildingId;
                var px = 1490 + (entry.OverviewLeftPercent * 3.45f);
                var py = 158 + (entry.OverviewTopPercent * 1.85f);
                AddPanel(root, "MapPin_" + entry.BuildingId, px, py, 30, 30, theme.Teal);
                AddText(root, "MapPinLabel_" + entry.BuildingId, label.Substring(0, 1).ToUpperInvariant(), px, py + 6, 30, 16, 11, theme.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
            }

            AddTitlePanel(root, "ActiveExpeditions", catalog.ActiveExpeditionsTitle, 1460, 438, 430, 230, theme);
            var y = 490f;
            foreach (var expedition in catalog.ExpeditionCards)
            {
                AddPanel(root, "Expedition_" + expedition.Title, 1490, y, 370, 54, new Color(theme.PaperDark.r, theme.PaperDark.g, theme.PaperDark.b, 0.38f));
                AddText(root, "ExpeditionTitle_" + expedition.Title, expedition.Title, 1510, y + 8, 180, 18, 12, theme.Ink, TextAlignmentOptions.Left, FontStyles.Bold);
                AddText(root, "ExpeditionSubtitle_" + expedition.Title, expedition.Subtitle, 1510, y + 28, 140, 16, 10, theme.MutedInk, TextAlignmentOptions.Left, FontStyles.Normal);
                AddText(root, "ExpeditionStatus_" + expedition.Title, expedition.Status, 1730, y + 18, 100, 18, 10, theme.Teal, TextAlignmentOptions.Right, FontStyles.Bold);
                y += 66;
            }

            AddTitlePanel(root, "RadioIntel", catalog.RadioIntelTitle, 1460, 688, 430, 232, theme);
            AddText(root, "RadioIntelBody", catalog.RadioIntelBody, 1490, 746, 360, 58, 13, theme.MutedInk, TextAlignmentOptions.TopLeft, FontStyles.Normal);
            AddPanel(root, "RadioIntelButton", 1610, 840, 136, 32, theme.Rust);
            AddText(root, "RadioIntelButtonLabel", catalog.RadioIntelButton, 1610, 848, 136, 16, 11, theme.Paper, TextAlignmentOptions.Center, FontStyles.Bold);
        }

        private static void BuildBottomNav(Transform root, CampUiCatalogSO catalog)
        {
            var theme = catalog.Theme;
            AddRaw(root, "BottomNavPlate", catalog.BottomNavPlate, 30, 1000, 1860, 55, new Color(1, 1, 1, 0.86f));
            var itemWidth = 250f;
            var startX = 332f;
            for (var i = 0; i < catalog.NavItems.Count; i++)
            {
                var item = catalog.NavItems[i];
                var x = startX + (i * itemWidth);
                AddPanel(root, "Nav_" + item.Id, x, 1009, 230, 36, item.IsActive ? theme.Teal : new Color(theme.PaperDark.r, theme.PaperDark.g, theme.PaperDark.b, 0.38f));
                AddText(root, "NavLabel_" + item.Id, item.Label, x, 1017, 230, 18, 14, item.IsActive ? theme.Paper : theme.Ink, TextAlignmentOptions.Center, FontStyles.Bold);
            }
        }

        private static void OrganizeHierarchy(Transform root, CampUiCatalogSO catalog)
        {
            var topBar = CreateGroup(root, "TopBar");
            var leftColumn = CreateGroup(root, "LeftColumn");
            var buildingGrid = CreateGroup(root, "BuildingGrid");
            var rightColumn = CreateGroup(root, "RightColumn");
            var bottomNav = CreateGroup(root, "BottomNav");

            MoveDirect(root, topBar, name =>
                name.StartsWith("Brand", StringComparison.Ordinal) ||
                name == "TopResourcePlate" ||
                name.StartsWith("Icon_", StringComparison.Ordinal) ||
                name.StartsWith("Label_", StringComparison.Ordinal) ||
                name.StartsWith("Value_", StringComparison.Ordinal));

            MoveDirect(root, leftColumn, name =>
                name.StartsWith("CampStatus", StringComparison.Ordinal) ||
                name.StartsWith("Meter", StringComparison.Ordinal) ||
                name.StartsWith("CampSummary", StringComparison.Ordinal) ||
                name.StartsWith("Summary", StringComparison.Ordinal) ||
                name.StartsWith("RecentAlerts", StringComparison.Ordinal) ||
                name.StartsWith("Alert", StringComparison.Ordinal));

            MoveDirect(root, buildingGrid, name =>
                name == "BuildingsTitle" ||
                name.StartsWith("Filter_", StringComparison.Ordinal) ||
                name.StartsWith("FilterLabel_", StringComparison.Ordinal) ||
                name.StartsWith("Building", StringComparison.Ordinal) ||
                name.StartsWith("CostIcon_", StringComparison.Ordinal) ||
                name.StartsWith("CostValue_", StringComparison.Ordinal) ||
                name.StartsWith("CostMax_", StringComparison.Ordinal) ||
                name.StartsWith("EmptyBuilding", StringComparison.Ordinal));

            MoveDirect(root, rightColumn, name =>
                name.StartsWith("CampOverview", StringComparison.Ordinal) ||
                name.StartsWith("CampMap", StringComparison.Ordinal) ||
                name.StartsWith("MapPin", StringComparison.Ordinal) ||
                name.StartsWith("ActiveExpeditions", StringComparison.Ordinal) ||
                name.StartsWith("Expedition", StringComparison.Ordinal) ||
                name.StartsWith("RadioIntel", StringComparison.Ordinal));

            MoveDirect(root, bottomNav, name =>
                name.StartsWith("BottomNav", StringComparison.Ordinal) ||
                name.StartsWith("Nav_", StringComparison.Ordinal) ||
                name.StartsWith("NavLabel_", StringComparison.Ordinal));

            MoveDirect(topBar, FindDirect(topBar, "BrandBadge"), name => name == "BrandBadgeText", true);
            MoveResourcesIntoPlate(topBar, catalog);

            MoveDirect(leftColumn, FindDirect(leftColumn, "CampStatus"), name => name.StartsWith("CampStatus", StringComparison.Ordinal) || name.StartsWith("Meter", StringComparison.Ordinal), true);
            MoveDirect(leftColumn, FindDirect(leftColumn, "CampSummary"), name => name.StartsWith("CampSummary_", StringComparison.Ordinal) || name.StartsWith("Summary", StringComparison.Ordinal) || name.StartsWith("CampSummaryNote", StringComparison.Ordinal), true);
            MoveDirect(leftColumn, FindDirect(leftColumn, "RecentAlerts"), name => name.StartsWith("RecentAlerts_", StringComparison.Ordinal) || name.StartsWith("Alert", StringComparison.Ordinal), true);

            foreach (var alert in catalog.Alerts)
            {
                var card = FindDirect(FindDirect(leftColumn, "RecentAlerts"), "Alert_" + alert.Title);
                MoveDirect(FindDirect(leftColumn, "RecentAlerts"), card, name => name.EndsWith("_" + alert.Title, StringComparison.Ordinal) && name != "Alert_" + alert.Title, true);
            }

            foreach (var entry in catalog.Buildings)
            {
                var card = FindDirect(buildingGrid, "BuildingCard_" + entry.BuildingId);
                MoveDirect(buildingGrid, card, name => IsBuildingCardChild(name, entry.BuildingId), true);
                MoveDirect(card, FindDirect(card, "BuildingImageSlot_" + entry.BuildingId), name => name == "BuildingImageLetter_" + entry.BuildingId, true);
                MoveDirect(card, FindDirect(card, "BuildingCostRow_" + entry.BuildingId), name =>
                    name == "BuildingCostLabel_" + entry.BuildingId ||
                    name == "CostMax_" + entry.BuildingId ||
                    name.StartsWith("CostIcon_" + entry.BuildingId + "_", StringComparison.Ordinal) ||
                    name.StartsWith("CostValue_" + entry.BuildingId + "_", StringComparison.Ordinal), true);
                MoveDirect(card, FindDirect(card, "BuildingUpgradeButton_" + entry.BuildingId), name => name == "BuildingUpgradeLabel_" + entry.BuildingId, true);
            }

            foreach (var filter in catalog.BuildingFilters)
            {
                MoveDirect(buildingGrid, FindDirect(buildingGrid, "Filter_" + filter.Id), name => name == "FilterLabel_" + filter.Id, true);
            }

            MoveDirect(buildingGrid, FindDirect(buildingGrid, "EmptyBuildingSlot"), name => name == "EmptyBuildingPlus" || name == "EmptyBuildingTitle", true);

            MoveDirect(rightColumn, FindDirect(rightColumn, "CampOverview"), name => name.StartsWith("CampOverview_", StringComparison.Ordinal) || name.StartsWith("CampMap", StringComparison.Ordinal) || name.StartsWith("MapPin", StringComparison.Ordinal), true);
            MoveDirect(rightColumn, FindDirect(rightColumn, "ActiveExpeditions"), name => name.StartsWith("ActiveExpeditions_", StringComparison.Ordinal) || name.StartsWith("Expedition", StringComparison.Ordinal), true);
            MoveDirect(rightColumn, FindDirect(rightColumn, "RadioIntel"), name => name.StartsWith("RadioIntel", StringComparison.Ordinal) && name != "RadioIntel", true);

            MoveMapPinsIntoPlate(rightColumn, catalog);

            foreach (var expedition in catalog.ExpeditionCards)
            {
                var activeExpeditions = FindDirect(rightColumn, "ActiveExpeditions");
                var row = FindDirect(activeExpeditions, "Expedition_" + expedition.Title);
                MoveDirect(activeExpeditions, row, name => name.EndsWith("_" + expedition.Title, StringComparison.Ordinal) && name != "Expedition_" + expedition.Title, true);
            }

            MoveDirect(FindDirect(rightColumn, "RadioIntel"), FindDirect(FindDirect(rightColumn, "RadioIntel"), "RadioIntelButton"), name => name == "RadioIntelButtonLabel", true);
            MoveDirect(bottomNav, FindDirect(bottomNav, "BottomNavPlate"), name => name.StartsWith("Nav_", StringComparison.Ordinal) || name.StartsWith("NavLabel_", StringComparison.Ordinal), true);
            MoveNavLabelsIntoButtons(bottomNav, catalog);

            FindDirect(root, "Background").SetSiblingIndex(0);
            topBar.SetSiblingIndex(1);
            leftColumn.SetSiblingIndex(2);
            buildingGrid.SetSiblingIndex(3);
            rightColumn.SetSiblingIndex(4);
            bottomNav.SetSiblingIndex(5);
        }

        private static void MoveResourcesIntoPlate(Transform topBar, CampUiCatalogSO catalog)
        {
            var plate = FindDirect(topBar, "TopResourcePlate");
            if (plate == null) return;

            for (var i = 0; i < catalog.ResourceBar.Count; i++)
            {
                var entry = catalog.ResourceBar[i];
                var group = CreateRect("Resource_" + entry.Id, plate, 50 + i * 198, 7, 178, 36).transform;
                MoveDirect(topBar, group, name =>
                    name == "Icon_" + entry.Id ||
                    name == "Label_" + entry.Id ||
                    name == "Value_" + entry.Id, true);
            }
        }

        private static void MoveMapPinsIntoPlate(Transform rightColumn, CampUiCatalogSO catalog)
        {
            var overview = FindDirect(rightColumn, "CampOverview");
            var plate = FindDirect(overview, "CampMapPlate");
            if (overview == null || plate == null) return;

            foreach (var entry in catalog.Buildings)
            {
                var pin = FindDirect(overview, "MapPin_" + entry.BuildingId);
                MoveDirect(overview, pin, name => name == "MapPinLabel_" + entry.BuildingId, true);
                if (pin != null)
                {
                    pin.SetParent(plate, true);
                }
            }
        }

        private static void MoveNavLabelsIntoButtons(Transform bottomNav, CampUiCatalogSO catalog)
        {
            var plate = FindDirect(bottomNav, "BottomNavPlate");
            if (plate == null) return;

            foreach (var item in catalog.NavItems)
            {
                MoveDirect(plate, FindDirect(plate, "Nav_" + item.Id), name => name == "NavLabel_" + item.Id, true);
            }
        }

        private static bool IsBuildingCardChild(string name, string buildingId)
        {
            if (name == "BuildingCard_" + buildingId) return false;
            return name.EndsWith("_" + buildingId, StringComparison.Ordinal) ||
                   name.Contains("_" + buildingId + "_");
        }

        private static Transform CreateGroup(Transform root, string name)
        {
            return CreateRect(name, root, 0, 0, 1920, 1080).transform;
        }

        private static Transform FindDirect(Transform parent, string name)
        {
            if (parent == null) return null;
            for (var i = 0; i < parent.childCount; i++)
            {
                var child = parent.GetChild(i);
                if (child.name == name) return child;
            }

            return null;
        }

        private static void MoveDirect(Transform source, Transform target, Func<string, bool> predicate, bool preserveWorld = false)
        {
            if (source == null || target == null) return;
            var matches = source.Cast<Transform>().Where(child => child != target && predicate(child.name)).ToArray();
            foreach (var match in matches)
            {
                match.SetParent(target, preserveWorld);
            }
        }

        private static void AddTitlePanel(Transform root, string name, string title, float x, float y, float width, float height, CampUiTheme theme)
        {
            AddPanel(root, name, x, y, width, height, new Color(theme.PaperDark.r, theme.PaperDark.g, theme.PaperDark.b, 0.36f));
            AddText(root, name + "_Title", title, x + 18, y + 16, width - 36, 24, 15, theme.Teal, TextAlignmentOptions.Left, FontStyles.Bold);
        }

        private static Image AddPanel(Transform parent, string name, float x, float y, float width, float height, Color color)
        {
            var go = CreateRect(name, parent, x, y, width, height);
            var image = go.AddComponent<Image>();
            image.color = color;
            image.raycastTarget = false;
            return image;
        }

        private static RawImage AddRaw(Transform parent, string name, Texture texture, float x, float y, float width, float height, Color color)
        {
            var go = CreateRect(name, parent, x, y, width, height);
            var image = go.AddComponent<RawImage>();
            image.texture = texture;
            image.color = texture == null ? color : Color.white;
            image.raycastTarget = false;
            return image;
        }

        private static TextMeshProUGUI AddText(Transform parent, string name, string text, float x, float y, float width, float height, float size, Color color, TextAlignmentOptions alignment, FontStyles style)
        {
            var go = CreateRect(name, parent, x, y, width, height);
            var label = go.AddComponent<TextMeshProUGUI>();
            label.text = text ?? string.Empty;
            label.fontSize = size;
            label.color = color;
            label.alignment = alignment;
            label.fontStyle = style;
            label.textWrappingMode = TextWrappingModes.Normal;
            label.overflowMode = TextOverflowModes.Truncate;
            label.raycastTarget = false;
            return label;
        }

        private static GameObject CreateRect(string name, Transform parent, float x, float y, float width, float height)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var rect = go.GetComponent<RectTransform>();
            rect.anchorMin = new Vector2(0, 1);
            rect.anchorMax = new Vector2(0, 1);
            rect.pivot = new Vector2(0, 1);
            rect.anchoredPosition = new Vector2(x, -y);
            rect.sizeDelta = new Vector2(width, height);
            return go;
        }

        private static void EnsureFolder(string parent, string child)
        {
            var path = parent + "/" + child;
            if (!AssetDatabase.IsValidFolder(path))
            {
                AssetDatabase.CreateFolder(parent, child);
            }
        }
    }
}
