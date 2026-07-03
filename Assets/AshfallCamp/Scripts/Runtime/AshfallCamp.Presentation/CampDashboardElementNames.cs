namespace AshfallCamp.Presentation
{
    public static class CampDashboardElementNames
    {
        public const string PrefabName = "PF_CampDashboard";

        public const string Background = "Background";
        public const string TopBar = "TopBar";
        public const string LeftColumn = "LeftColumn";
        public const string BuildingGrid = "BuildingGrid";
        public const string RightColumn = "RightColumn";
        public const string BottomNav = "BottomNav";

        public const string BrandTitle = "BrandTitle";
        public const string BrandSubtitle = "BrandSubtitle";
        public const string BrandBadge = "BrandBadge";
        public const string BrandBadgeText = "BrandBadgeText";
        public const string TopResourcePlate = "TopResourcePlate";

        public const string CampStatus = "CampStatus";
        public const string CampStatusBadge = "CampStatusBadge";
        public const string CampStatusBadgeText = "CampStatusBadgeText";
        public const string CampStatusLabel = "CampStatusLabel";
        public const string CampStatusBody = "CampStatusBody";

        public const string CampSummary = "CampSummary";
        public const string CampSummaryNotePlate = "CampSummaryNotePlate";
        public const string CampSummaryNote = "CampSummaryNote";
        public const string RecentAlerts = "RecentAlerts";

        public const string BuildingsTitle = "BuildingsTitle";
        public const string EmptyBuildingSlot = "EmptyBuildingSlot";
        public const string EmptyBuildingPlus = "EmptyBuildingPlus";
        public const string EmptyBuildingTitle = "EmptyBuildingTitle";

        public const string CampOverview = "CampOverview";
        public const string CampMapPlate = "CampMapPlate";
        public const string ActiveExpeditions = "ActiveExpeditions";
        public const string RadioIntel = "RadioIntel";
        public const string RadioIntelBody = "RadioIntelBody";
        public const string RadioIntelButton = "RadioIntelButton";
        public const string RadioIntelButtonLabel = "RadioIntelButtonLabel";

        public const string BottomNavPlate = "BottomNavPlate";

        public const string SummaryPopulation = "population";
        public const string SummaryIdleSurvivors = "idle_survivors";
        public const string SummaryBuildings = "buildings";
        public const string SummaryProduction = "production";

        public static string PanelTitle(string panelName)
        {
            return panelName + "_Title";
        }

        public static string ResourceGroup(string id)
        {
            return "Resource_" + id;
        }

        public static string ResourceIcon(string id)
        {
            return "Icon_" + id;
        }

        public static string ResourceLabel(string id)
        {
            return "Label_" + id;
        }

        public static string ResourceValue(string id)
        {
            return "Value_" + id;
        }

        public static string MeterLabel(string id)
        {
            return "MeterLabel_" + id;
        }

        public static string MeterTrack(string id)
        {
            return "MeterTrack_" + id;
        }

        public static string MeterFill(string id)
        {
            return "MeterFill_" + id;
        }

        public static string MeterValue(string id)
        {
            return "MeterValue_" + id;
        }

        public static string SummaryLabel(string id)
        {
            return "SummaryLabel_" + id;
        }

        public static string SummaryValue(string id)
        {
            return "SummaryValue_" + id;
        }

        public static string AlertCard(string id)
        {
            return "Alert_" + id;
        }

        public static string AlertDot(string id)
        {
            return "AlertDot_" + id;
        }

        public static string AlertDotText(string id)
        {
            return "AlertDotText_" + id;
        }

        public static string AlertTitle(string id)
        {
            return "AlertTitle_" + id;
        }

        public static string AlertBody(string id)
        {
            return "AlertBody_" + id;
        }

        public static string Filter(string id)
        {
            return "Filter_" + id;
        }

        public static string FilterLabel(string id)
        {
            return "FilterLabel_" + id;
        }

        public static string BuildingCard(string id)
        {
            return "BuildingCard_" + id;
        }

        public static string BuildingImageSlot(string id)
        {
            return "BuildingImageSlot_" + id;
        }

        public static string BuildingImageLetter(string id)
        {
            return "BuildingImageLetter_" + id;
        }

        public static string BuildingIcon(string id)
        {
            return "BuildingIcon_" + id;
        }

        public static string BuildingName(string id)
        {
            return "BuildingName_" + id;
        }

        public static string BuildingLevel(string id)
        {
            return "BuildingLevel_" + id;
        }

        public static string BuildingDescription(string id)
        {
            return "BuildingDescription_" + id;
        }

        public static string BuildingEffect(string id)
        {
            return "BuildingEffect_" + id;
        }

        public static string BuildingCostRow(string id)
        {
            return "BuildingCostRow_" + id;
        }

        public static string BuildingCostLabel(string id)
        {
            return "BuildingCostLabel_" + id;
        }

        public static string BuildingCostMax(string id)
        {
            return "CostMax_" + id;
        }

        public static string BuildingCostIcon(string id, string resourceId)
        {
            return "CostIcon_" + id + "_" + resourceId;
        }

        public static string BuildingCostValue(string id, string resourceId)
        {
            return "CostValue_" + id + "_" + resourceId;
        }

        public static string BuildingCostIconPrefix(string id)
        {
            return "CostIcon_" + id + "_";
        }

        public static string BuildingCostValuePrefix(string id)
        {
            return "CostValue_" + id + "_";
        }

        public static string BuildingUpgradeButton(string id)
        {
            return "BuildingUpgradeButton_" + id;
        }

        public static string BuildingUpgradeLabel(string id)
        {
            return "BuildingUpgradeLabel_" + id;
        }

        public static string MapPin(string id)
        {
            return "MapPin_" + id;
        }

        public static string MapPinLabel(string id)
        {
            return "MapPinLabel_" + id;
        }

        public static string Expedition(string id)
        {
            return "Expedition_" + id;
        }

        public static string ExpeditionTitle(string id)
        {
            return "ExpeditionTitle_" + id;
        }

        public static string ExpeditionSubtitle(string id)
        {
            return "ExpeditionSubtitle_" + id;
        }

        public static string ExpeditionStatus(string id)
        {
            return "ExpeditionStatus_" + id;
        }

        public static string Nav(string id)
        {
            return "Nav_" + id;
        }

        public static string NavLabel(string id)
        {
            return "NavLabel_" + id;
        }
    }
}
