using System;
using System.Collections.Generic;
using DG.Tweening;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    [CreateAssetMenu(menuName = "Ashfall Camp/UI/Camp UI Catalog")]
    public sealed class CampUiCatalogSO : ScriptableObject
    {
        public CampUiTheme Theme = new CampUiTheme();
        public CampUiScreenTransition ScreenTransition = new CampUiScreenTransition();
        public CampUiToastSettings Toast = new CampUiToastSettings();
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
        public string NextGoalCompleteTitle = string.Empty;
        public string NextGoalCompleteBody = string.Empty;
        public string NextGoalProgressFormat = string.Empty;
        public string PopulationLabel = string.Empty;
        public string IdleSurvivorsLabel = string.Empty;
        public string BuildingsLabel = string.Empty;
        public string ProductionMetricLabel = string.Empty;
        public string ProductionMetricResourceId = string.Empty;
        public string RecentAlertsTitle = string.Empty;
        public string NoAlertsTitle = string.Empty;
        public string NoAlertsBody = string.Empty;
        public string SurvivorJoinedAlertTitleFormat = string.Empty;
        public string SurvivorJoinedAlertBodyFormat = string.Empty;
        public string DemoCompletedAlertTitleFormat = string.Empty;
        public string DemoCompletedAlertBodyFormat = string.Empty;
        public string ActiveExpeditionAlertTitleFormat = string.Empty;
        public string ActiveExpeditionAlertBodyFormat = string.Empty;
        public string LowResourceAlertTitleFormat = string.Empty;
        public string LowResourceAlertBodyFormat = string.Empty;
        public int LowResourceAlertPercentThreshold;
        public string WoundedAlertTitleFormat = string.Empty;
        public string WoundedAlertBodyFormat = string.Empty;
        public string IdleSurvivorsAlertTitleFormat = string.Empty;
        public string IdleSurvivorsAlertBodyFormat = string.Empty;
        public string EmergencyScavengeAlertTitle = string.Empty;
        public string EmergencyScavengeReadyBodyFormat = string.Empty;
        public string EmergencyScavengeActiveBodyFormat = string.Empty;
        public string EmergencyScavengeCooldownBodyFormat = string.Empty;
        public string EmergencyScavengeButton = string.Empty;
        public string EmergencyScavengeCompletedAlertTitle = string.Empty;
        public string EmergencyScavengeCompletedAlertBodyFormat = string.Empty;
        public string UpgradeAvailableAlertTitleFormat = string.Empty;
        public string UpgradeAvailableAlertBodyFormat = string.Empty;
        public Texture2D CampStatusPanelTexture;
        public Texture2D CampAlertCardTexture;
        public string CampOverviewTitle = string.Empty;
        public string ActiveExpeditionsTitle = string.Empty;
        public string RadioIntelTitle = string.Empty;
        public string RadioIntelBody = string.Empty;
        public string RadioIntelBodyFormat = string.Empty;
        public string RadioIntelButton = string.Empty;
        public Texture2D RadioIntelPanelTexture;
        public string RadioScreenTitle = string.Empty;
        public string RadioBroadcastTitle = string.Empty;
        public string RadioBroadcastCostFormat = string.Empty;
        public string RadioBroadcastReadyLabel = string.Empty;
        public string RadioBroadcastPendingLabel = string.Empty;
        public string RadioBroadcastBlockedFormat = string.Empty;
        public Texture2D RadioBroadcastPanelTexture;
        public string RadioCandidateListTitle = string.Empty;
        public string RadioCandidateAwaitingTitle = string.Empty;
        public string RadioCandidateAwaitingBody = string.Empty;
        public string RadioCandidateEmptyTitle = string.Empty;
        public string RadioCandidateEmptyBody = string.Empty;
        public string RadioCandidateCardMetaFormat = string.Empty;
        public string RadioCandidateSkillFormat = string.Empty;
        public string RadioCandidateTraitsFormat = string.Empty;
        public string RadioCandidateRecruitButton = string.Empty;
        public string RadioCandidateSkipButton = string.Empty;
        public Texture2D RadioEmptyPanelTexture;
        public Texture2D RadioCandidateCardTexture;
        public string ExpeditionEmptyTitle = string.Empty;
        public string ExpeditionEmptySubtitle = string.Empty;
        public string ExpeditionEmptyStatus = string.Empty;
        public string ExpeditionActiveSubtitleFormat = string.Empty;
        public string ExpeditionActiveStatusFormat = string.Empty;
        public string ExpeditionRouteSubtitleFormat = string.Empty;
        public string ExpeditionRouteStatusFormat = string.Empty;
        public string ExpeditionScreenTitle = string.Empty;
        public string ExpeditionSquadTitleFormat = string.Empty;
        public string ExpeditionSquadMemberMetaFormat = string.Empty;
        public string ExpeditionPolicyTitle = string.Empty;
        public string ExpeditionPolicyDetailsFormat = string.Empty;
        public string ExpeditionSurvivalChanceFormat = string.Empty;
        public string ExpeditionSelectedTitleFormat = string.Empty;
        public string ExpeditionSelectedDetailsFormat = string.Empty;
        public string ExpeditionSelectedLootFormat = string.Empty;
        public string ExpeditionLootRangeFormat = string.Empty;
        public string ExpeditionSelectedEnemiesFormat = string.Empty;
        public string ExpeditionSelectedWarningsFormat = string.Empty;
        public string ExpeditionLaunchButton = string.Empty;
        public string ExpeditionLaunchBlockedButton = string.Empty;
        public string ExpeditionReviewRiskButton = string.Empty;
        public string ExpeditionConfirmRiskButton = string.Empty;
        public string ExpeditionRiskConfirmationNoticeFormat = string.Empty;
        public string ExpeditionMonitorTitle = string.Empty;
        public string ExpeditionMonitorHeaderFormat = string.Empty;
        public string ExpeditionMonitorProgressFormat = string.Empty;
        public string ExpeditionMonitorLootFormat = string.Empty;
        public string ExpeditionMonitorNoiseFormat = string.Empty;
        public string ExpeditionMonitorLogFormat = string.Empty;
        public string ExpeditionMonitorThreatFormat = string.Empty;
        public string ExpeditionMonitorWoundsFormat = string.Empty;
        public string ExpeditionMonitorFoundItemsFormat = string.Empty;
        public string ExpeditionMonitorNoiseLowLabel = string.Empty;
        public string ExpeditionMonitorNoiseMediumLabel = string.Empty;
        public string ExpeditionMonitorNoiseHighLabel = string.Empty;
        public int ExpeditionMonitorNoiseMediumThreshold = 4;
        public int ExpeditionMonitorNoiseHighThreshold = 8;
        public Texture2D ExpeditionDetailPanelTexture;
        public Texture2D ExpeditionMonitorPanelTexture;
        public Texture2D ExpeditionSquadMemberCardTexture;
        public Texture2D ExpeditionPolicyCardTexture;
        public string ExpeditionLockedStatusFormat = string.Empty;
        public string ExpeditionUnlockRequirementFormat = string.Empty;
        public string ExpeditionNoWarningsLabel = string.Empty;
        public int ExpeditionMonitorLogLineCount = 3;
        public string DefaultExpeditionPolicyId = string.Empty;
        public string EmptyBuildingTitle = string.Empty;
        public string UpgradeCostLabel = string.Empty;
        public string BuildButtonLabel = string.Empty;
        public string UpgradeButtonLabel = string.Empty;
        public string MaxButtonLabel = string.Empty;
        public string LockedButtonLabel = string.Empty;
        public string NeedResourcesButtonLabel = string.Empty;
        public string MaxCostLabel = string.Empty;
        public string BuildingStatusBuildingFormat = string.Empty;
        public string BuildingStatusUpgradingFormat = string.Empty;
        public string BuildingStatusNotBuiltLabel = string.Empty;
        public string BuildingStatusMaxLabel = string.Empty;
        public string BuildingStatusRepairReadyLabel = string.Empty;
        public string BuildingStatusBroadcastReadyLabel = string.Empty;
        public string BuildingStatusBroadcastBlockedLabel = string.Empty;
        public string BuildingStatusNoWoundedLabel = string.Empty;
        public string BuildingStatusWoundedFormat = string.Empty;
        public string BuildingStatusProductionFormat = string.Empty;
        public string SurvivorEffectFormat = string.Empty;
        public string ResourceCapEffectFormat = string.Empty;
        public string ResourceCapOnlyEffectFormat = string.Empty;
        public string RouteUnlockEffectLabel = string.Empty;
        public Texture2D BuildingCardTexture;
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
        public string SurvivorDetailTreatmentFormat = string.Empty;
        public string SurvivorDetailHealthyLabel = string.Empty;
        public string SurvivorDetailWoundFormat = string.Empty;
        public string SurvivorDetailHealingLockedFormat = string.Empty;
        public string SurvivorDetailMedicineCostFormat = string.Empty;
        public string SurvivorDetailUseMedicineButton = string.Empty;
        public string SurvivorDetailStartRestButton = string.Empty;
        public string SurvivorDetailStopRestButton = string.Empty;
        public string SurvivorDetailRestingLabelFormat = string.Empty;
        public Texture2D SurvivorsEmptyPanelTexture;
        public Texture2D SurvivorDetailPanelTexture;
        public Texture2D SurvivorRosterCardTexture;
        public string DefaultSurvivorPortraitId = string.Empty;
        public string SurvivorNoWeaponLabel = string.Empty;
        public string SurvivorNoTraitsLabel = string.Empty;
        public string SurvivorInventoryTitle = "INVENTORY";
        public string SurvivorInventoryEquipButton = "EQUIP";
        public string SurvivorInventoryEmptyLabel = "No items in this category";
        public List<SurvivorInventoryFilterUiEntry> SurvivorInventoryFilters = new List<SurvivorInventoryFilterUiEntry>();
        public List<SurvivorInventoryItemUiEntry> SurvivorInventoryItems = new List<SurvivorInventoryItemUiEntry>();
        public string WorkshopScreenTitle = string.Empty;
        public string WorkshopStatusFormat = string.Empty;
        public string WorkshopEmptyTitle = string.Empty;
        public string WorkshopEmptyBody = string.Empty;
        public string WorkshopItemDurabilityFormat = string.Empty;
        public string WorkshopItemEquippedFormat = string.Empty;
        public string WorkshopItemUnequippedLabel = string.Empty;
        public string WorkshopRepairCostFormat = string.Empty;
        public string WorkshopRepairButton = string.Empty;
        public string WorkshopEquipButton = string.Empty;
        public string WorkshopBrokenLabel = string.Empty;
        public Texture2D WorkshopEmptyPanelTexture;
        public Texture2D WorkshopItemTileTexture;
        public string ReportsScreenId = string.Empty;
        public string ReportsScreenTitle = string.Empty;
        public string ReportsEmptyTitle = string.Empty;
        public string ReportsEmptyBody = string.Empty;
        public Texture2D ReportsEmptyPanelTexture;
        public Texture2D ReportsAfterActionPanelTexture;
        public Texture2D ReportsCampEventPanelTexture;
        public Texture2D ReportsOfflinePanelTexture;
        public string CampEventPanelTitle = string.Empty;
        public string SurvivorJoinedReportTitleFormat = string.Empty;
        public string SurvivorJoinedReportBodyFormat = string.Empty;
        public string DemoCompletedReportTitleFormat = string.Empty;
        public string DemoCompletedReportBodyFormat = string.Empty;
        public string EmergencyScavengeReportTitle = string.Empty;
        public string EmergencyScavengeReportBodyFormat = string.Empty;
        public string AfterActionPanelTitle = string.Empty;
        public string AfterActionSuccessLabel = string.Empty;
        public string AfterActionFailureLabel = string.Empty;
        public string AfterActionOutcomeFormat = string.Empty;
        public string AfterActionLootFormat = string.Empty;
        public string AfterActionXpFormat = string.Empty;
        public string AfterActionWoundsFormat = string.Empty;
        public string AfterActionEnemiesFormat = string.Empty;
        public string AfterActionEventsFormat = string.Empty;
        public string AfterActionSkillXpFormat = string.Empty;
        public string AfterActionDurabilityFormat = string.Empty;
        public string AfterActionDurabilityItemFormat = string.Empty;
        public string AfterActionBrokenItemsFormat = string.Empty;
        public string AfterActionProgressFormat = string.Empty;
        public string AfterActionUnlockedFormat = string.Empty;
        public string AfterActionDemoProgressFormat = string.Empty;
        public string AfterActionSendAgainButton = string.Empty;
        public string OfflineReportPanelTitle = string.Empty;
        public string OfflineReportSummaryFormat = string.Empty;
        public string OfflineReportResourcesFormat = string.Empty;
        public string OfflineReportResourcesSpentFormat = string.Empty;
        public string OfflineReportCompletedFormat = string.Empty;
        public string OfflineReportHealingFormat = string.Empty;
        public string OfflineReportWarningsFormat = string.Empty;
        public string SettingsScreenTitle = string.Empty;
        public string SettingsAutosaveTitle = string.Empty;
        public string SettingsAutosaveEnabledBodyFormat = string.Empty;
        public string SettingsAutosaveDisabledBody = string.Empty;
        public string SettingsAutosaveEnabledLabel = string.Empty;
        public string SettingsAutosaveDisabledLabel = string.Empty;
        public string SettingsAutosaveToggleLabel = string.Empty;
        public string SettingsManualSaveTitle = string.Empty;
        public string SettingsManualSaveBody = string.Empty;
        public string SettingsManualSaveButton = string.Empty;
        public Texture2D SettingsRowTexture;
        public Texture2D SettingsToggleTrackActiveTexture;
        public Texture2D SettingsToggleTrackInactiveTexture;
        public Texture2D SettingsToggleKnobTexture;
        public Texture2D SettingsSliderTrackTexture;
        public Texture2D SettingsSliderHandleTexture;
        public string ReportNoneLabel = string.Empty;
        public string ReportListSeparator = string.Empty;
        public string ReportCountFormat = string.Empty;
        public List<CampNextGoalEntry> NextGoals = new List<CampNextGoalEntry>();
        public List<ResourceUiEntry> ResourceBar = new List<ResourceUiEntry>();
        public List<FilterUiEntry> BuildingFilters = new List<FilterUiEntry>();
        public List<BuildingUiEntry> Buildings = new List<BuildingUiEntry>();
        public List<AlertUiEntry> Alerts = new List<AlertUiEntry>();
        public List<ExpeditionUiEntry> ExpeditionCards = new List<ExpeditionUiEntry>();
        public List<ExpeditionZoneArtworkUiEntry> ExpeditionZoneArtwork = new List<ExpeditionZoneArtworkUiEntry>();
        public List<ExpeditionRiskArtworkUiEntry> ExpeditionRiskArtwork = new List<ExpeditionRiskArtworkUiEntry>();
        public List<NavUiEntry> NavItems = new List<NavUiEntry>();
        public List<SurvivorSkillUiEntry> SurvivorSkillLabels = new List<SurvivorSkillUiEntry>();
        public List<SurvivorPortraitUiEntry> SurvivorPortraits = new List<SurvivorPortraitUiEntry>();
        public List<CampToastUiEntry> ToastMessages = new List<CampToastUiEntry>();
    }

    [Serializable]
    public sealed class CampUiScreenTransition
    {
        public bool Enabled = true;
        public float DurationSeconds = 0.16f;
        public Ease Ease = Ease.OutCubic;
        public bool UseUnscaledTime = true;
    }

    [Serializable]
    public sealed class CampUiToastSettings
    {
        public bool Enabled = true;
        [Range(0f, 10f)] public float VisibleSeconds = 2.2f;
        [Range(0f, 2f)] public float FadeSeconds = 0.14f;
        public Ease Ease = Ease.OutCubic;
        public bool UseUnscaledTime = true;
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
        [Range(0f, 1f)] public float NavInactivePanelAlpha = 0.38f;
        [Range(0f, 1f)] public float BuildingFilterInactivePanelAlpha = 0.55f;
        [Range(0f, 1f)] public float AlertPanelAlpha = 0.08f;
        [Range(0f, 1f)] public float ExpeditionDetailPanelAlpha = 0.86f;
        [Range(0f, 1f)] public float ExpeditionRouteSelectedPanelAlpha = 0.84f;
        [Range(0f, 1f)] public float ExpeditionRouteAvailablePanelAlpha = 0.72f;
        [Range(0f, 1f)] public float ExpeditionRouteBlockedPanelAlpha = 0.38f;
        [Range(0f, 1f)] public float ExpeditionSquadSelectedPanelAlpha = 0.86f;
        [Range(0f, 1f)] public float ExpeditionSquadAvailablePanelAlpha = 0.66f;
        [Range(0f, 1f)] public float ExpeditionSquadBlockedPanelAlpha = 0.32f;
        [Range(0f, 1f)] public float ExpeditionPolicySelectedPanelAlpha = 0.82f;
        [Range(0f, 1f)] public float ExpeditionPolicyInactivePanelAlpha = 0.62f;
        [Range(0f, 1f)] public float RadioCandidatePanelAlpha = 0.68f;
        [Range(0f, 1f)] public float SurvivorSelectedPanelAlpha = 0.86f;
        [Range(0f, 1f)] public float SurvivorInactivePanelAlpha = 0.64f;
        [Range(0f, 1f)] public float WorkshopItemPanelAlpha = 0.74f;

        public Color WithAlpha(Color color, float alpha)
        {
            return new Color(color.r, color.g, color.b, Mathf.Clamp01(alpha));
        }
    }

    [Serializable]
    public sealed class CampNextGoalEntry
    {
        public string Id = string.Empty;
        public string Title = string.Empty;
        public string Body = string.Empty;
        public bool CompleteWhenAnyCondition;
        public List<CampGoalConditionEntry> Conditions = new List<CampGoalConditionEntry>();
    }

    [Serializable]
    public sealed class CampGoalConditionEntry
    {
        public string Type = string.Empty;
        public string Id = string.Empty;
        public int Value;
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
        public string Id = string.Empty;
        public CampAlertSeverity Severity = CampAlertSeverity.Info;
        public int Priority;
        public string Category = string.Empty;
        public CampAlertAction Action = CampAlertAction.None;
        public string TargetScreenId = string.Empty;
        public Texture2D Icon;
        public Texture2D ActionIcon;
        public CampAlertButtonView ButtonView = CampAlertButtonView.Text;
        public string ButtonLabel = string.Empty;
        public string Title = string.Empty;
        public string Body = string.Empty;
        public Color ToneColor = Color.white;
    }

    public enum CampAlertSeverity
    {
        Info,
        Success,
        Warning,
        Critical
    }

    public enum CampAlertAction
    {
        None,
        OpenScreen,
        StartEmergencyScavenge
    }

    public enum CampAlertButtonView
    {
        Hidden,
        Text,
        Icon,
        IconAndText
    }

    [Serializable]
    public sealed class ExpeditionUiEntry
    {
        public string Title = string.Empty;
        public string Subtitle = string.Empty;
        public string Status = string.Empty;
    }

    [Serializable]
    public sealed class ExpeditionZoneArtworkUiEntry
    {
        public string ZoneId = string.Empty;
        public Texture2D Thumbnail;
    }

    [Serializable]
    public sealed class ExpeditionRiskArtworkUiEntry
    {
        public string RiskTier = string.Empty;
        public Texture2D Badge;
    }

    [Serializable]
    public sealed class NavUiEntry
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
        public Texture2D Icon;
        public bool IsActive;
    }

    [Serializable]
    public sealed class SurvivorSkillUiEntry
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
    }

    [Serializable]
    public sealed class SurvivorPortraitUiEntry
    {
        public string Id = string.Empty;
        public Texture2D Portrait;
    }

    [Serializable]
    public sealed class SurvivorInventoryFilterUiEntry
    {
        public string Id = string.Empty;
        public string Label = string.Empty;
        public Texture2D Icon;
    }

    [Serializable]
    public sealed class SurvivorInventoryItemUiEntry
    {
        public string ItemId = string.Empty;
        public string Description = string.Empty;
        public Texture2D Icon;
    }

    [Serializable]
    public sealed class CampToastUiEntry
    {
        public string Id = string.Empty;
        public string TitleFormat = string.Empty;
        public string BodyFormat = string.Empty;
        public Color ToneColor = Color.white;
    }
}
