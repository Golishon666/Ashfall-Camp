using System;
using System.Collections.Generic;
using AshfallCamp.Domain;

namespace AshfallCamp.Composition
{
    public static class CampUiPreviewStateFactory
    {
        public static GameState CreateReferenceProfile(GameConfigSnapshot config, long nowUnixMs)
        {
            var state = GameStateFactory.CreateNew(config, nowUnixMs);
            state.Survivors.Clear();
            state.Inventory.Clear();
            state.SurvivorCap = 12;
            state.NextId = 32;
            state.TotalPlayTimeSeconds = 11 * 24 * 60 + 6 * 60 + 35;

            SetResource(state, GameIds.Resources.Scrap, 500);
            SetResource(state, GameIds.Resources.Food, 8);
            SetResource(state, GameIds.Resources.Water, 6);
            SetResource(state, GameIds.Resources.WeaponParts, 0);
            SetResource(state, GameIds.Resources.Medicine, 1);
            SetResource(state, GameIds.Resources.RadioIntel, 3);

            var asha = AddSurvivor(state, config, "survivor_1", "Asha", GameIds.Backgrounds.Mechanic,
                new[] { GameIds.Traits.Resourceful, GameIds.Traits.QuickLearner }, SurvivorActivityState.Idle, 3, 420, 32, 32, 24, 68,
                Skills(62, 48, 55, 60, 72, 41));
            asha.PreviewXpGoal = 900;
            AddSurvivor(state, config, "survivor_02", "Bram", GameIds.Backgrounds.Scavenger,
                new[] { GameIds.Traits.Tough }, SurvivorActivityState.OnExpedition, 2, 120, 36, 36, 62, 55,
                Skills(44, 58, 30, 52, 20, 18));
            AddSurvivor(state, config, "survivor_03", "Cora", GameIds.Backgrounds.ExCop,
                new[] { GameIds.Traits.Brave }, SurvivorActivityState.Resting, 2, 80, 28, 28, 48, 70,
                Skills(28, 44, 64, 40, 22, 24));
            AddSurvivor(state, config, "survivor_04", "Dima", GameIds.Backgrounds.Mechanic,
                new[] { GameIds.Traits.Careful }, SurvivorActivityState.Idle, 2, 60, 30, 30, 18, 62,
                Skills(36, 32, 20, 44, 66, 28));
            var elka = AddSurvivor(state, config, "survivor_05", "Elka", GameIds.Backgrounds.Nurse,
                new[] { GameIds.Traits.OldInjury }, SurvivorActivityState.Wounded, 2, 40, 18, 28, 72, 38,
                Skills(30, 28, 24, 38, 18, 70));
            elka.StatusEffects.Add(new StatusEffectState { Id = GameIds.StatusEffects.Cuts, RemainingSeconds = 300 });
            AddSurvivor(state, config, "survivor_06", "Faro", GameIds.Backgrounds.Hunter,
                new[] { GameIds.Traits.Quiet }, SurvivorActivityState.Idle, 2, 20, 26, 26, 35, 60,
                Skills(58, 46, 54, 62, 16, 20));

            var knife = AddItem(state, config, GameIds.Items.RustyKnife, "preview_item_knife");
            AddItem(state, config, GameIds.Items.MetalPipe, "preview_item_pipe");
            AddItem(state, config, GameIds.Items.RustyRevolver, "preview_item_revolver");
            var jacket = AddItem(state, config, GameIds.Items.LeatherJacket, "preview_item_jacket");
            AddItem(state, config, GameIds.Items.ScrapArmor, "preview_item_scrap_armor");
            var medkit = AddItem(state, config, GameIds.Items.Medkit, "preview_item_medkit");
            AddItem(state, config, GameIds.Items.Toolkit, "preview_item_toolkit");
            AddItem(state, config, GameIds.Items.AmmoPack, "preview_item_ammo");
            var backpack = AddItem(state, config, GameIds.Items.Backpack, "preview_item_backpack");

            Equip(asha, knife, ItemSlot.Weapon);
            Equip(asha, jacket, ItemSlot.Armor);
            Equip(asha, medkit, ItemSlot.Utility);
            Equip(asha, backpack, ItemSlot.Backpack);
            return state;
        }

        private static SurvivorState AddSurvivor(
            GameState state,
            GameConfigSnapshot config,
            string id,
            string name,
            string backgroundId,
            IEnumerable<string> traits,
            SurvivorActivityState activity,
            int level,
            int xp,
            int health,
            int maxHealth,
            int fatigue,
            int morale,
            Dictionary<string, int> skills)
        {
            var survivor = GameStateFactory.CreateSurvivorState(id, name, backgroundId, new List<string>(traits), skills, config);
            survivor.State = activity;
            survivor.Level = level;
            survivor.Xp = xp;
            survivor.Health = health;
            survivor.MaxHealth = maxHealth;
            survivor.Fatigue = fatigue;
            survivor.Morale = morale;
            survivor.Skills = new Dictionary<string, int>(skills, StringComparer.Ordinal);
            state.Survivors.Add(survivor);
            return survivor;
        }

        private static InventoryItemState AddItem(GameState state, GameConfigSnapshot config, string itemId, string uid)
        {
            ItemDefinition definition;
            if (!config.TryGetItem(itemId, out definition))
            {
                throw new InvalidOperationException("Preview item is missing from config: " + itemId);
            }

            var item = new InventoryItemState
            {
                Uid = uid,
                ItemId = itemId,
                Durability = Math.Max(1, definition.MaxDurability),
                MaxDurability = Math.Max(1, definition.MaxDurability)
            };
            state.Inventory.Add(item);
            return item;
        }

        private static void Equip(SurvivorState survivor, InventoryItemState item, ItemSlot slot)
        {
            item.EquippedBySurvivorId = survivor.Id;
            if (slot == ItemSlot.Weapon) survivor.Equipment.WeaponItemUid = item.Uid;
            else if (slot == ItemSlot.Armor) survivor.Equipment.ArmorItemUid = item.Uid;
            else if (slot == ItemSlot.Utility) survivor.Equipment.UtilityItemUid = item.Uid;
            else if (slot == ItemSlot.Backpack) survivor.Equipment.BackpackItemUid = item.Uid;
        }

        private static Dictionary<string, int> Skills(int scavenging, int melee, int firearms, int survival, int mechanics, int medicine)
        {
            return new Dictionary<string, int>(StringComparer.Ordinal)
            {
                { GameIds.Skills.Scavenging, scavenging },
                { GameIds.Skills.Melee, melee },
                { GameIds.Skills.Firearms, firearms },
                { GameIds.Skills.Survival, survival },
                { GameIds.Skills.Mechanics, mechanics },
                { GameIds.Skills.Medicine, medicine }
            };
        }

        private static void SetResource(GameState state, string id, int amount)
        {
            state.Resources[id] = Math.Max(0, amount);
        }
    }
}
