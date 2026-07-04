using AshfallCamp.Domain;
using AshfallCamp.Presentation;
using NUnit.Framework;
using UnityEngine;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class CampDashboardSurvivorFormatterTests
    {
        [Test]
        public void SurvivorCardsAndDetailUseStateConfigAndCatalogText()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();

            var cards = CampDashboardTextFormatter.BuildSurvivorCards(state, catalog);
            var detail = CampDashboardTextFormatter.BuildSurvivorDetail(state.Survivors[0], state, config, catalog);

            Assert.AreEqual(1, cards.Count);
            Assert.AreEqual("survivor_1", cards[0].SurvivorId);
            Assert.AreEqual("Mara", cards[0].Name);
            Assert.AreEqual("M", cards[0].Avatar);
            Assert.AreEqual("Idle * 1", cards[0].State);
            Assert.AreEqual("SCAV 7", cards[0].Skill);
            Assert.AreEqual("Mara L1", detail.Title);
            Assert.That(detail.Background, Does.Contain("Scavenger"));
            Assert.That(detail.Traits, Does.Contain("Careful"));
            Assert.That(detail.Weapon, Does.Contain("Rusty Knife"));
            Assert.AreEqual("Treatment: Healthy", detail.Treatment);
            Assert.That(detail.Stats, Does.Contain("HP 30/30"));

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void SurvivorDetailShowsWoundTreatmentStatus()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            HealingSystem.ApplyWound(state, config, "survivor_1");

            var detail = CampDashboardTextFormatter.BuildSurvivorDetail(state.Survivors[0], state, config, catalog);

            Assert.That(detail.Treatment, Does.Contain("Treatment: cuts 5m locked 1"));

            state.Buildings["infirmary"].Level = 1;
            detail = CampDashboardTextFormatter.BuildSurvivorDetail(state.Survivors[0], state, config, catalog);

            Assert.AreEqual("Treatment: cuts 5m", detail.Treatment);

            Object.DestroyImmediate(catalog);
        }

        [Test]
        public void WorkshopItemsUseStateConfigAndCatalogText()
        {
            var config = TestConfigFactory.Create();
            var state = GameStateFactory.CreateNew(config, 0);
            var catalog = CreateCatalog();
            state.Buildings["workshop"].Level = 1;
            state.Inventory[0].Durability = 50;
            ResourceSystem.Add(state, "weapon_parts", 5);

            var status = CampDashboardTextFormatter.FormatWorkshopStatus(state, config, catalog, "survivor_1");
            var items = CampDashboardTextFormatter.BuildWorkshopItems(state, config, catalog, "survivor_1");

            Assert.AreEqual("Target Mara Items 1 Parts 5 Need 1", status);
            Assert.AreEqual(1, items.Count);
            Assert.AreEqual("Rusty Knife", items[0].Name);
            Assert.AreEqual(50, items[0].Durability);
            Assert.AreEqual(80, items[0].MaxDurability);
            Assert.AreEqual("Equipped by Mara", items[0].Equipped);
            Assert.AreEqual(string.Empty, items[0].BrokenLabel);
            Assert.AreEqual("Repair 3", items[0].RepairCost);
            Assert.IsTrue(items[0].CanRepair);
            Assert.IsFalse(items[0].CanEquip);

            Object.DestroyImmediate(catalog);
        }

        private static CampUiCatalogSO CreateCatalog()
        {
            var catalog = ScriptableObject.CreateInstance<CampUiCatalogSO>();
            catalog.SurvivorCardStateFormat = "{0} * {1}";
            catalog.SurvivorCardSkillFormat = "{0} {1}";
            catalog.SurvivorDetailTitle = "{0} L{1}";
            catalog.SurvivorDetailBackgroundFormat = "Background: {0}";
            catalog.SurvivorDetailTraitsFormat = "Traits: {0}";
            catalog.SurvivorDetailWeaponFormat = "Weapon: {0} ({1}/{2})";
            catalog.SurvivorDetailStatsFormat = "HP {0}/{1} M {2} F {3} XP {4}";
            catalog.SurvivorDetailTreatmentFormat = "Treatment: {0}";
            catalog.SurvivorDetailHealthyLabel = "Healthy";
            catalog.SurvivorDetailWoundFormat = "{0} {1}m";
            catalog.SurvivorDetailHealingLockedFormat = "{0} locked {1}";
            catalog.SurvivorNoWeaponLabel = "Unarmed";
            catalog.SurvivorNoTraitsLabel = "None";
            catalog.SurvivorSkillLabels.Add(new SurvivorSkillUiEntry { Id = "scavenging", Label = "SCAV" });
            catalog.WorkshopStatusFormat = "Target {0} Items {1} Parts {2} Need {3}";
            catalog.WorkshopItemEquippedFormat = "Equipped by {0}";
            catalog.WorkshopItemUnequippedLabel = "Stored";
            catalog.WorkshopRepairCostFormat = "Repair {0}";
            catalog.WorkshopBrokenLabel = "Broken";
            return catalog;
        }
    }
}
