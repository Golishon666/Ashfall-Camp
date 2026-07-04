using System;
using System.Collections.Generic;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    [CreateAssetMenu(menuName = "Ashfall Camp/UI/Camp UI Catalog")]
    public sealed class CampUiCatalogSO : ScriptableObject
    {
        public CampUiTheme Theme = new CampUiTheme();
        public string BrandBannerText = string.Empty;
        public string BrandTitle = string.Empty;
        public string BrandSubtitle = string.Empty;
        public Texture2D TopResourcePlate;
        public Texture2D BottomNavPlate;
        public string BuildingScreenTitle = string.Empty;
        public string CampStatusTitle = string.Empty;
        public string CampStatusBody = string.Empty;
        public string CampStatusBodyFormat = string.Empty;
        public string CampStatusHealthyLabel = string.Empty;
        public string CampStatusStrainedLabel = string.Empty;
        public string CampStatusBadgeLabel = string.Empty;
        public string CampStatusBadgeFormat = string.Empty;
        public string StatusResourceId = string.Empty;
        public int StatusStrainedBelowAmount;
        public int StatusWarningPercentThreshold;
        public string MoraleLabel = string.Empty;
        public string SafetyLabel = string.Empty;
        public string SuppliesLabel = string.Empty;
        public string MoraleValueLabel = string.Empty;
        public string SafetyValueLabel = string.Empty;
        public string SuppliesValueLabel = string.Empty;
        public string MoraleValueFormat = string.Empty;
        public string SafetyValueFormat = string.Empty;
        public string SuppliesValueFormat = string.Empty;
        public string CampSummaryTitle = string.Empty;
        public string CampSummaryNote = string.Empty;
        public string PopulationLabel = string.Empty;
        public string IdleSurvivorsLabel = string.Empty;
        public string BuildingsLabel = string.Empty;
        public string ProductionMetricLabel = string.Empty;
        public string ProductionMetricResourceId = string.Empty;
        public string RecentAlertsTitle = string.Empty;
        public string NoAlertsTitle = string.Empty;
        public string NoAlertsBody = string.Empty;
        public string ActiveExpeditionAlertTitleFormat = string.Empty;
        public string ActiveExpeditionAlertBodyFormat = string.Empty;
        public string LowResourceAlertTitleFormat = string.Empty;
        public string LowResourceAlertBodyFormat = string.Empty;
        public int LowResourceAlertPercentThreshold;
        public string WoundedAlertTitleFormat = string.Empty;
        public string WoundedAlertBodyFormat = string.Empty;
        public string UpgradeAvailableAlertTitleFormat = string.Empty;
        public string UpgradeAvailableAlertBodyFormat = string.Empty;
        public string CampOverviewTitle = string.Empty;
        public string ActiveExpeditionsTitle = string.Empty;
        public string RadioIntelTitle = string.Empty;
        public string RadioIntelBody = string.Empty;
        public string RadioIntelBodyFormat = string.Empty;
        public string RadioIntelButton = string.Empty;
        public string ExpeditionEmptyTitle = string.Empty;
        public string ExpeditionEmptySubtitle = string.Empty;
        public string ExpeditionEmptyStatus = string.Empty;
        public string ExpeditionActiveSubtitleFormat = string.Empty;
        public string ExpeditionActiveStatusFormat = string.Empty;
        public string ExpeditionRouteSubtitleFormat = string.Empty;
        public string ExpeditionRouteStatusFormat = string.Empty;
        public string DefaultExpeditionPolicyId = string.Empty;
        public string EmptyBuildingTitle = string.Empty;
        public string UpgradeCostLabel = string.Empty;
        public string UpgradeButtonLabel = string.Empty;
        public string MaxButtonLabel = string.Empty;
        public string LockedButtonLabel = string.Empty;
        public string NeedResourcesButtonLabel = string.Empty;
        public string MaxCostLabel = string.Empty;
        public string SurvivorEffectFormat = string.Empty;
        public string ResourceCapEffectFormat = string.Empty;
        public string ResourceCapOnlyEffectFormat = string.Empty;
        public string RouteUnlockEffectLabel = string.Empty;
        public string IdleSuffixLabel = string.Empty;
        public string PerHourSuffixLabel = string.Empty;
        public string LevelLabelFormat = string.Empty;
        public string SurvivorsScreenTitle = string.Empty;
        public string SurvivorsCountFormat = string.Empty;
        public string SurvivorEmptyTitle = string.Empty;
        public string SurvivorEmptyBody = string.Empty;
        public string SurvivorCardStateFormat = string.Empty;
        public string SurvivorCardSkillFormat = string.Empty;
        public string SurvivorDetailTitle = string.Empty;
        public string SurvivorDetailBackgroundFormat = string.Empty;
        public string SurvivorDetailTraitsFormat = string.Empty;
        public string SurvivorDetailWeaponFormat = string.Empty;
        public string SurvivorDetailStatsFormat = string.Empty;
        public string SurvivorNoWeaponLabel = string.Empty;
        public string SurvivorNoTraitsLabel = string.Empty;
        public List<ResourceUiEntry> ResourceBar = new List<ResourceUiEntry>();
        public List<FilterUiEntry> BuildingFilters = new List<FilterUiEntry>();
        public List<BuildingUiEntry> Buildings = new List<BuildingUiEntry>();
        public List<AlertUiEntry> Alerts = new List<AlertUiEntry>();
        public List<ExpeditionUiEntry> ExpeditionCards = new List<ExpeditionUiEntry>();
        public List<NavUiEntry> NavItems = new List<NavUiEntry>();
        public List<SurvivorSkillUiEntry> SurvivorSkillLabels = new List<SurvivorSkillUiEntry>();
    }

    [Serializable]
    public sealed class CampUiTheme
    {
        public Color Paper = new Color32(0xF3, 0xE8, 0xD3, 0xFF);
        public Color PaperDark = new Color32(0xE6, 0xD7, 0xB6, 0xFF);
        public Color Ink = new Color32(0x2B, 0x2E, 0x33, 0xFF);
        public Color MutedInk = new Color32(0x6A, 0x5D, 0x4A, 0xFF);
        public Color Teal = new Color32(0x2F, 0x6A, 0x72, 0xFF);
        public Color Sage = new Color32(0x7B, 0x8A, 0x6B, 0xFF);
        public Color Rust = new Color32(0xC9, 0x63, 0x3A, 0xFF);
        public Color Amber = new Color32(0xE1, 0xB4, 0x6A, 0xFF);
        public Color Line = new Color(0.43f, 0.33f, 0.22f, 0.28f);
    }

    [Serializable]
    public sealed class ResourceUiEntry
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
        public Texture2D Icon;
        public bool UsesSurvivorCapacity;
    }

    [Serializable]
    public sealed class FilterUiEntry
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
        public bool IsActive;
    }

    [Serializable]
    public sealed class BuildingUiEntry
    {
        public string BuildingId = string.Empty;
        public string Description = string.Empty;
        public Texture2D Icon;
        public Texture2D Image;
        public float OverviewLeftPercent;
        public float OverviewTopPercent;
    }

    [Serializable]
    public sealed class AlertUiEntry
    {
        public string Title = string.Empty;
        public string Body = string.Empty;
        public Color ToneColor = Color.white;
    }

    [Serializable]
    public sealed class ExpeditionUiEntry
    {
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public string Status = string.Empty;
    }

    [Serializable]
    public sealed class NavUiEntry
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
        public bool IsActive;
    }

    [Serializable]
    public sealed class SurvivorSkillUiEntry
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
    }
}
