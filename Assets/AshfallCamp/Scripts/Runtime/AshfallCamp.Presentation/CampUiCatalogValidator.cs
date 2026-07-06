using System;
using System.Collections.Generic;
using System.Reflection;
using UnityEngine;

namespace AshfallCamp.Presentation
{
    public static class CampUiCatalogValidator
    {
        public static CampUiCatalogValidationResult Validate(CampUiCatalogSO catalog)
        {
            var result = new CampUiCatalogValidationResult();
            if (catalog == null)
            {
                result.Errors.Add("UI catalog is missing.");
                return result;
            }

            if (catalog.Theme == null)
            {
                result.Errors.Add("UI theme is missing.");
            }
            else
            {
                ValidateRangedThemeValues(catalog.Theme, result.Errors);
            }

            if (catalog.ScreenTransition == null)
            {
                result.Errors.Add("UI screen transition config is missing.");
            }
            else if (catalog.ScreenTransition.DurationSeconds < 0)
            {
                result.Errors.Add("UI screen transition duration cannot be negative.");
            }

            if (catalog.Toast == null)
            {
                result.Errors.Add("UI toast config is missing.");
            }
            else
            {
                ValidateRangedToastValues(catalog.Toast, result.Errors);
            }

            ValidateUniqueIds(catalog.ResourceBar, "resource bar", entry => entry != null ? entry.Id : string.Empty, result.Errors, true);
            ValidateUniqueIds(catalog.BuildingFilters, "building filters", entry => entry != null ? entry.Id : string.Empty, result.Errors, true);
            ValidateUniqueIds(catalog.Buildings, "building UI entries", entry => entry != null ? entry.BuildingId : string.Empty, result.Errors, true);
            ValidateUniqueIds(catalog.NavItems, "navigation items", entry => entry != null ? entry.Id : string.Empty, result.Errors, true);
            ValidateUniqueIds(catalog.NextGoals, "next goals", entry => entry != null ? entry.Id : string.Empty, result.Errors, false);
            ValidateUniqueIds(catalog.ExpeditionZoneArtwork, "expedition zone artwork", entry => entry != null ? entry.ZoneId : string.Empty, result.Errors, false);
            ValidateUniqueIds(catalog.ExpeditionRiskArtwork, "expedition risk artwork", entry => entry != null ? entry.RiskTier : string.Empty, result.Errors, false);
            ValidateUniqueIds(catalog.SurvivorSkillLabels, "survivor skill labels", entry => entry != null ? entry.Id : string.Empty, result.Errors, false);
            ValidateUniqueIds(catalog.SurvivorPortraits, "survivor portraits", entry => entry != null ? entry.Id : string.Empty, result.Errors, false);
            ValidateUniqueIds(catalog.ToastMessages, "toast messages", entry => entry != null ? entry.Id : string.Empty, result.Errors, true);
            ValidateExpeditionZoneArtworkAssets(catalog.ExpeditionZoneArtwork, result.Errors);
            ValidateExpeditionRiskArtworkAssets(catalog.ExpeditionRiskArtwork, result.Errors);
            ValidateRequiredTexture(catalog.ExpeditionDetailPanelTexture, "expedition detail panel texture", result.Errors);
            ValidateRequiredTexture(catalog.ExpeditionMonitorPanelTexture, "expedition monitor panel texture", result.Errors);
            ValidateRequiredTexture(catalog.ExpeditionSquadMemberCardTexture, "expedition squad member card texture", result.Errors);
            ValidateRequiredTexture(catalog.ExpeditionPolicyCardTexture, "expedition policy card texture", result.Errors);
            ValidateRequiredTexture(catalog.CampStatusPanelTexture, "camp status panel texture", result.Errors);
            ValidateRequiredTexture(catalog.CampAlertCardTexture, "camp alert card texture", result.Errors);
            ValidateRequiredTexture(catalog.BuildingCardTexture, "building card texture", result.Errors);
            ValidateRequiredTexture(catalog.SurvivorsEmptyPanelTexture, "survivors empty panel texture", result.Errors);
            ValidateRequiredTexture(catalog.RadioIntelPanelTexture, "radio intel panel texture", result.Errors);
            ValidateRequiredTexture(catalog.RadioBroadcastPanelTexture, "radio broadcast panel texture", result.Errors);
            ValidateRequiredTexture(catalog.RadioEmptyPanelTexture, "radio empty panel texture", result.Errors);
            ValidateRequiredTexture(catalog.RadioCandidateCardTexture, "radio candidate card texture", result.Errors);
            ValidateRequiredTexture(catalog.SettingsRowTexture, "settings row texture", result.Errors);
            ValidateRequiredTexture(catalog.SettingsToggleTrackActiveTexture, "settings toggle active track texture", result.Errors);
            ValidateRequiredTexture(catalog.SettingsToggleTrackInactiveTexture, "settings toggle inactive track texture", result.Errors);
            ValidateRequiredTexture(catalog.SettingsToggleKnobTexture, "settings toggle knob texture", result.Errors);
            ValidateRequiredTexture(catalog.SettingsSliderTrackTexture, "settings slider track texture", result.Errors);
            ValidateRequiredTexture(catalog.SettingsSliderHandleTexture, "settings slider handle texture", result.Errors);
            ValidateResourceIcons(catalog.ResourceBar, result.Errors);
            ValidateBuildingIcons(catalog.Buildings, result.Errors);
            ValidatePortraitAssets(catalog.SurvivorPortraits, result.Errors);
            ValidateRequiredToasts(catalog.ToastMessages, result.Errors);
            ValidateReportsScreenId(catalog, result.Errors);
            ValidateSingleActive(catalog.NavItems, "navigation items", result.Errors);
            ValidateSingleActive(catalog.BuildingFilters, "building filters", result.Errors);
            return result;
        }

        public static void ValidateOrThrow(CampUiCatalogSO catalog)
        {
            var result = Validate(catalog);
            if (!result.IsValid)
            {
                throw new InvalidOperationException("Invalid UI catalog: " + string.Join("; ", result.Errors));
            }
        }

        private static void ValidateRangedThemeValues(CampUiTheme theme, List<string> errors)
        {
            var fields = typeof(CampUiTheme).GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.FieldType != typeof(float)) continue;

                var range = field.GetCustomAttribute<RangeAttribute>();
                if (range == null) continue;

                var value = (float)field.GetValue(theme);
                if (value < range.min || value > range.max)
                {
                    errors.Add("UI theme value is out of range: " + field.Name);
                }
            }
        }

        private static void ValidateRangedToastValues(CampUiToastSettings toast, List<string> errors)
        {
            var fields = typeof(CampUiToastSettings).GetFields(BindingFlags.Instance | BindingFlags.Public);
            for (var i = 0; i < fields.Length; i++)
            {
                var field = fields[i];
                if (field.FieldType != typeof(float)) continue;

                var range = field.GetCustomAttribute<RangeAttribute>();
                if (range == null) continue;

                var value = (float)field.GetValue(toast);
                if (value < range.min || value > range.max)
                {
                    errors.Add("UI toast value is out of range: " + field.Name);
                }
            }
        }

        private static void ValidateUniqueIds<T>(IEnumerable<T> entries, string label, Func<T, string> getId, List<string> errors, bool requireEntries)
        {
            if (entries == null)
            {
                errors.Add("UI catalog list is missing: " + label);
                return;
            }

            var count = 0;
            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                count++;
                if (entry == null)
                {
                    errors.Add("UI catalog contains a null entry: " + label);
                    continue;
                }

                var id = getId(entry);
                if (string.IsNullOrWhiteSpace(id))
                {
                    errors.Add("UI catalog contains an empty id: " + label);
                    continue;
                }

                if (!ids.Add(id))
                {
                    errors.Add("UI catalog contains a duplicate id in " + label + ": " + id);
                }
            }

            if (requireEntries && count == 0)
            {
                errors.Add("UI catalog list is empty: " + label);
            }
        }

        private static void ValidateSingleActive(IEnumerable<NavUiEntry> entries, string label, List<string> errors)
        {
            if (entries == null) return;

            var activeCount = 0;
            foreach (var entry in entries)
            {
                if (entry != null && entry.IsActive)
                {
                    activeCount++;
                }
            }

            if (activeCount > 1)
            {
                errors.Add("UI catalog has more than one active entry: " + label);
            }
        }

        private static void ValidateSingleActive(IEnumerable<FilterUiEntry> entries, string label, List<string> errors)
        {
            if (entries == null) return;

            var activeCount = 0;
            foreach (var entry in entries)
            {
                if (entry != null && entry.IsActive)
                {
                    activeCount++;
                }
            }

            if (activeCount > 1)
            {
                errors.Add("UI catalog has more than one active entry: " + label);
            }
        }

        private static void ValidateRequiredToasts(IEnumerable<CampToastUiEntry> entries, List<string> errors)
        {
            if (entries == null) return;

            var ids = new HashSet<string>(StringComparer.Ordinal);
            foreach (var entry in entries)
            {
                if (entry != null && !string.IsNullOrWhiteSpace(entry.Id))
                {
                    ids.Add(entry.Id);
                }
            }

            var required = CampToastIds.Required;
            for (var i = 0; i < required.Length; i++)
            {
                if (!ids.Contains(required[i]))
                {
                    errors.Add("UI catalog is missing required toast message: " + required[i]);
                }
            }
        }

        private static void ValidateReportsScreenId(CampUiCatalogSO catalog, List<string> errors)
        {
            if (string.IsNullOrWhiteSpace(catalog.ReportsScreenId))
            {
                errors.Add("UI catalog reports screen id is missing.");
                return;
            }

            if (!ContainsNavItemId(catalog.NavItems, catalog.ReportsScreenId))
            {
                errors.Add("UI catalog reports screen id does not match a navigation item: " + catalog.ReportsScreenId);
            }
        }

        private static bool ContainsNavItemId(IEnumerable<NavUiEntry> entries, string id)
        {
            if (entries == null || string.IsNullOrWhiteSpace(id)) return false;
            foreach (var entry in entries)
            {
                if (entry != null && string.Equals(entry.Id, id, StringComparison.Ordinal))
                {
                    return true;
                }
            }

            return false;
        }

        private static void ValidateRequiredTexture(Texture2D texture, string label, List<string> errors)
        {
            if (texture == null)
            {
                errors.Add("UI catalog is missing required texture: " + label);
            }
        }

        private static void ValidateResourceIcons(IEnumerable<ResourceUiEntry> entries, List<string> errors)
        {
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) continue;
                if (entry.Icon == null)
                {
                    errors.Add("UI catalog resource entry is missing icon: " + entry.Id);
                }
            }
        }

        private static void ValidateBuildingIcons(IEnumerable<BuildingUiEntry> entries, List<string> errors)
        {
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.BuildingId)) continue;
                if (entry.Icon == null)
                {
                    errors.Add("UI catalog building entry is missing icon: " + entry.BuildingId);
                }
            }
        }

        private static void ValidateExpeditionZoneArtworkAssets(IEnumerable<ExpeditionZoneArtworkUiEntry> entries, List<string> errors)
        {
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.ZoneId)) continue;
                if (entry.Thumbnail == null)
                {
                    errors.Add("UI catalog expedition zone artwork is missing texture: " + entry.ZoneId);
                }
            }
        }

        private static void ValidateExpeditionRiskArtworkAssets(IEnumerable<ExpeditionRiskArtworkUiEntry> entries, List<string> errors)
        {
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.RiskTier)) continue;
                if (entry.Badge == null)
                {
                    errors.Add("UI catalog expedition risk artwork is missing texture: " + entry.RiskTier);
                }
            }
        }

        private static void ValidatePortraitAssets(IEnumerable<SurvivorPortraitUiEntry> entries, List<string> errors)
        {
            if (entries == null) return;

            foreach (var entry in entries)
            {
                if (entry == null || string.IsNullOrWhiteSpace(entry.Id)) continue;
                if (entry.Portrait == null)
                {
                    errors.Add("UI catalog survivor portrait is missing texture: " + entry.Id);
                }
            }
        }
    }

    public sealed class CampUiCatalogValidationResult
    {
        public readonly List<string> Errors = new List<string>();

        public bool IsValid
        {
            get { return Errors.Count == 0; }
        }
    }
}
