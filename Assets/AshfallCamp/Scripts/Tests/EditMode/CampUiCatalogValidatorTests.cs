using System;
using AshfallCamp.Presentation;
using NUnit.Framework;
using UnityEditor;
using UnityEngine;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class CampUiCatalogValidatorTests
    {
        [Test]
        public void ProductionCatalogPassesValidation()
        {
            var catalog = AssetDatabase.LoadAssetAtPath<CampUiCatalogSO>("Assets/AshfallCamp/UI/CampUiCatalog.asset");

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsTrue(result.IsValid, string.Join("; ", result.Errors));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.AfterActionSkillXpFormat));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.AfterActionDurabilityFormat));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.AfterActionDurabilityItemFormat));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.AfterActionBrokenItemsFormat));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.AfterActionProgressFormat));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.AfterActionUnlockedFormat));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.AfterActionDemoProgressFormat));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.OfflineReportResourcesSpentFormat));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.SurvivorDetailStartRestButton));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.SurvivorDetailStopRestButton));
            Assert.IsFalse(string.IsNullOrWhiteSpace(catalog.SurvivorDetailRestingLabelFormat));
        }

        [Test]
        public void DuplicateNavigationIdsFailValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.NavItems.Add(new NavUiEntry { Id = "buildings" });

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("duplicate id in navigation items: buildings", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void MultipleActiveNavigationItemsFailValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.NavItems.Add(new NavUiEntry { Id = "settings", Label = "Settings", IsActive = true });

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("more than one active entry: navigation items", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void MissingReportsScreenIdFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.ReportsScreenId = string.Empty;

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("reports screen id is missing", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ReportsScreenIdWithoutNavigationItemFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.ReportsScreenId = "missing_reports";

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("reports screen id does not match a navigation item: missing_reports", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void OutOfRangeThemeAlphaFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.Theme.NavInactivePanelAlpha = 1.2f;

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("NavInactivePanelAlpha", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void NegativeScreenTransitionDurationFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.ScreenTransition.DurationSeconds = -0.1f;

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("transition duration cannot be negative", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void MissingRequiredToastFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.ToastMessages.RemoveAt(0);

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("missing required toast message", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SurvivorPortraitWithoutTextureFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.SurvivorPortraits.Add(new SurvivorPortraitUiEntry { Id = "survivor_1" });

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("survivor portrait is missing texture: survivor_1", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ExpeditionZoneArtworkWithoutTextureFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.ExpeditionZoneArtwork.Add(new ExpeditionZoneArtworkUiEntry { ZoneId = "abandoned_store" });

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("expedition zone artwork is missing texture: abandoned_store", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ExpeditionRiskArtworkWithoutTextureFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.ExpeditionRiskArtwork.Add(new ExpeditionRiskArtworkUiEntry { RiskTier = "Low" });

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("expedition risk artwork is missing texture: Low", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void MissingExpeditionPolicyCardTextureFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.ExpeditionPolicyCardTexture = null;

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("missing required texture: expedition policy card texture", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void MissingCampStatusPanelTextureFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.CampStatusPanelTexture = null;

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("missing required texture: camp status panel texture", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void MissingRadioCandidateCardTextureFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.RadioCandidateCardTexture = null;

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("missing required texture: radio candidate card texture", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void MissingSettingsToggleKnobTextureFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.SettingsToggleKnobTexture = null;

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("missing required texture: settings toggle knob texture", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void MissingResourceIconFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.ResourceBar[0].Icon = null;

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("resource entry is missing icon: scrap", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void MissingBuildingIconFailsValidation()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.Buildings[0].Icon = null;

            var result = CampUiCatalogValidator.Validate(catalog);

            Assert.IsFalse(result.IsValid);
            StringAssert.Contains("building entry is missing icon: barracks", string.Join("; ", result.Errors));

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        [Test]
        public void ValidateOrThrowIncludesValidationErrors()
        {
            var catalog = CreateMinimalValidCatalog();
            catalog.BuildingFilters.Add(new FilterUiEntry { Id = "all" });

            var ex = Assert.Throws<InvalidOperationException>(() => CampUiCatalogValidator.ValidateOrThrow(catalog));

            StringAssert.Contains("duplicate id in building filters: all", ex.Message);

            UnityEngine.Object.DestroyImmediate(catalog);
        }

        private static CampUiCatalogSO CreateMinimalValidCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();
            catalog.ResourceBar.Add(new ResourceUiEntry { Id = "scrap", Label = "Scrap", Icon = Texture2D.whiteTexture });
            catalog.BuildingFilters.Add(new FilterUiEntry { Id = "all", Label = "All", IsActive = true });
            catalog.Buildings.Add(new BuildingUiEntry { BuildingId = "barracks", Description = "Beds", Icon = Texture2D.whiteTexture });
            catalog.NavItems.Add(new NavUiEntry { Id = "buildings", Label = "Buildings", IsActive = true });
            catalog.NavItems.Add(new NavUiEntry { Id = "reports", Label = "Reports" });
            catalog.ReportsScreenId = "reports";
            catalog.ExpeditionDetailPanelTexture = Texture2D.whiteTexture;
            catalog.ExpeditionMonitorPanelTexture = Texture2D.whiteTexture;
            catalog.ExpeditionSquadMemberCardTexture = Texture2D.whiteTexture;
            catalog.ExpeditionPolicyCardTexture = Texture2D.whiteTexture;
            catalog.CampStatusPanelTexture = Texture2D.whiteTexture;
            catalog.CampAlertCardTexture = Texture2D.whiteTexture;
            catalog.BuildingCardTexture = Texture2D.whiteTexture;
            catalog.SurvivorsEmptyPanelTexture = Texture2D.whiteTexture;
            catalog.RadioIntelPanelTexture = Texture2D.whiteTexture;
            catalog.RadioBroadcastPanelTexture = Texture2D.whiteTexture;
            catalog.RadioEmptyPanelTexture = Texture2D.whiteTexture;
            catalog.RadioCandidateCardTexture = Texture2D.whiteTexture;
            catalog.SettingsRowTexture = Texture2D.whiteTexture;
            catalog.SettingsToggleTrackActiveTexture = Texture2D.whiteTexture;
            catalog.SettingsToggleTrackInactiveTexture = Texture2D.whiteTexture;
            catalog.SettingsToggleKnobTexture = Texture2D.whiteTexture;
            catalog.SettingsSliderTrackTexture = Texture2D.whiteTexture;
            catalog.SettingsSliderHandleTexture = Texture2D.whiteTexture;
            foreach (var toastId in CampToastIds.Required)
            {
                catalog.ToastMessages.Add(new CampToastUiEntry
                {
                    Id = toastId,
                    TitleFormat = toastId,
                    BodyFormat = toastId
                });
            }

            return catalog;
        }
    }
}
