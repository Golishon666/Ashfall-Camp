namespace AshfallCamp.Domain
{
    public static class GameIds
    {
        public static class Resources
        {
            public const string Scrap = "scrap";
            public const string Food = "food";
            public const string Water = "water";
            public const string WeaponParts = "weapon_parts";
            public const string Medicine = "medicine";

            public static readonly string[] All =
            {
                Scrap,
                Food,
                Water,
                WeaponParts,
                Medicine,
            };
        }

        public static class Skills
        {
            public const string Scavenging = "scavenging";
            public const string Melee = "melee";
            public const string Firearms = "firearms";
            public const string Survival = "survival";
            public const string Mechanics = "mechanics";
            public const string Medicine = "medicine";

            public static readonly string[] All =
            {
                Scavenging,
                Melee,
                Firearms,
                Survival,
                Mechanics,
                Medicine,
            };
        }

        public static class Stats
        {
            public const string MaxHealth = "max_health";
            public const string Morale = "morale";
            public const string CombatMorale = "combat_morale";
            public const string DurabilityLoss = "durability_loss";

            public static readonly string[] All =
            {
                MaxHealth,
                Morale,
                CombatMorale,
                DurabilityLoss,
            };
        }

        public static class Buildings
        {
            public const string Barracks = "barracks";
            public const string Workshop = "workshop";
            public const string WaterCollector = "water_collector";
            public const string MushroomBeds = "mushroom_beds";
            public const string Infirmary = "infirmary";
            public const string RadioTower = "radio_tower";

            public static readonly string[] All =
            {
                Barracks,
                Workshop,
                WaterCollector,
                MushroomBeds,
                Infirmary,
                RadioTower,
            };
        }

        public static class Zones
        {
            public const string AbandonedStore = "abandoned_store";
            public const string DrySuburb = "dry_suburb";
            public const string RuinedClinic = "ruined_clinic";
            public const string PoliceOutpost = "police_outpost";
            public const string MutantTunnel = "mutant_tunnel";

            public static readonly string[] All =
            {
                AbandonedStore,
                DrySuburb,
                RuinedClinic,
                PoliceOutpost,
                MutantTunnel,
            };
        }

        public static class Policies
        {
            public const string Cautious = "cautious";
            public const string Balanced = "balanced";
            public const string Aggressive = "aggressive";
            public const string LootFocused = "loot_focused";
            public const string AmmoSaving = "ammo_saving";

            public static readonly string[] All =
            {
                Cautious,
                Balanced,
                Aggressive,
                LootFocused,
                AmmoSaving,
            };
        }

        public static class Backgrounds
        {
            public const string Scavenger = "scavenger";
            public const string ExCop = "ex_cop";
            public const string Mechanic = "mechanic";
            public const string Nurse = "nurse";
            public const string Brawler = "brawler";
            public const string Hunter = "hunter";

            public static readonly string[] All =
            {
                Scavenger,
                ExCop,
                Mechanic,
                Nurse,
                Brawler,
                Hunter,
            };
        }

        public static class Traits
        {
            public const string Brave = "brave";
            public const string Coward = "coward";
            public const string Careful = "careful";
            public const string Greedy = "greedy";
            public const string Lucky = "lucky";
            public const string TriggerHappy = "trigger_happy";
            public const string Tough = "tough";
            public const string OldInjury = "old_injury";
            public const string Quiet = "quiet";
            public const string Clumsy = "clumsy";

            public static readonly string[] All =
            {
                Brave,
                Coward,
                Careful,
                Greedy,
                Lucky,
                TriggerHappy,
                Tough,
                OldInjury,
                Quiet,
                Clumsy,
            };
        }

        public static class RecruitableSurvivors
        {
            public const string Elias = "elias";
            public const string Nika = "nika";
            public const string June = "june";
            public const string Tomas = "tomas";
            public const string Rhea = "rhea";
            public const string Kade = "kade";

            public static readonly string[] All =
            {
                Elias,
                Nika,
                June,
                Tomas,
                Rhea,
                Kade,
            };
        }

        public static class Enemies
        {
            public const string FeralDog = "feral_dog";
            public const string StarvingSurvivor = "starving_survivor";
            public const string MutantStray = "mutant_stray";
            public const string Raider = "raider";
            public const string ArmoredRaider = "armored_raider";
            public const string MutantBrute = "mutant_brute";

            public static readonly string[] All =
            {
                FeralDog,
                StarvingSurvivor,
                MutantStray,
                Raider,
                ArmoredRaider,
                MutantBrute,
            };
        }

        public static class Weapons
        {
            public const string Melee01SurvivalKnife = "weapon_melee_01_survival_knife";
            public const string Melee02CombatKnife = "weapon_melee_02_combat_knife";
            public const string Melee03CurvedMachete = "weapon_melee_03_curved_machete";
            public const string Melee04SmallHatchet = "weapon_melee_04_small_hatchet";
            public const string Melee05FireAxe = "weapon_melee_05_fire_axe";
            public const string Melee06Crowbar = "weapon_melee_06_crowbar";
            public const string Melee07PipeWrench = "weapon_melee_07_pipe_wrench";
            public const string Melee08SpikedBat = "weapon_melee_08_spiked_bat";
            public const string Melee09NailedPlank = "weapon_melee_09_nailed_plank";
            public const string Melee10RebarSpear = "weapon_melee_10_rebar_spear";
            public const string Melee11SharpenedShovel = "weapon_melee_11_sharpened_shovel";
            public const string Melee12Sledgehammer = "weapon_melee_12_sledgehammer";
            public const string Melee13CarpenterHammer = "weapon_melee_13_carpenter_hammer";
            public const string Melee14ButcherCleaver = "weapon_melee_14_butcher_cleaver";
            public const string Melee15BoneClub = "weapon_melee_15_bone_club";
            public const string Melee16ScrapSword = "weapon_melee_16_scrap_sword";
            public const string Melee17TireIron = "weapon_melee_17_tire_iron";
            public const string Melee18ChainWhip = "weapon_melee_18_chain_whip";
            public const string Melee19RustedSickle = "weapon_melee_19_rusted_sickle";
            public const string Melee20Pickaxe = "weapon_melee_20_pickaxe";
            public const string Melee21Kukri = "weapon_melee_21_kukri";
            public const string Melee22MetalPipe = "weapon_melee_22_metal_pipe";
            public const string Melee23SpikedKnuckles = "weapon_melee_23_spiked_knuckles";
            public const string Melee24BottleShiv = "weapon_melee_24_bottle_shiv";
            public const string Melee25HarpoonSpear = "weapon_melee_25_harpoon_spear";
            public const string Melee26BayonetStaff = "weapon_melee_26_bayonet_staff";
            public const string Melee27ImprovisedChainsaw = "weapon_melee_27_improvised_chainsaw";
            public const string Melee28SawbladeAxe = "weapon_melee_28_sawblade_axe";
            public const string Melee29ClawGauntlet = "weapon_melee_29_claw_gauntlet";
            public const string Melee30ConcreteMaul = "weapon_melee_30_concrete_maul";
            public const string MeleeAdvanced01TacticalCarbonKnife = "weapon_melee_advanced_01_tactical_carbon_knife";
            public const string MeleeAdvanced02ReinforcedShockMachete = "weapon_melee_advanced_02_reinforced_shock_machete";
            public const string MeleeAdvanced03HydraulicFireAxe = "weapon_melee_advanced_03_hydraulic_fire_axe";
            public const string MeleeAdvanced04BalancedWarPick = "weapon_melee_advanced_04_balanced_war_pick";
            public const string MeleeAdvanced05BreachingHammer = "weapon_melee_advanced_05_breaching_hammer";
            public const string MeleeAdvanced06PoweredSawCleaver = "weapon_melee_advanced_06_powered_saw_cleaver";
            public const string MeleeAdvanced07EliteClawGauntlet = "weapon_melee_advanced_07_elite_claw_gauntlet";
            public const string MeleeAdvanced08TelescopicSpear = "weapon_melee_advanced_08_telescopic_spear";
            public const string MeleeAdvanced09MonofilamentChainWhip = "weapon_melee_advanced_09_monofilament_chain_whip";
            public const string MeleeAdvanced10CrusherMaul = "weapon_melee_advanced_10_crusher_maul";
            public const string Firearm01ServicePistol = "weapon_firearm_01_service_pistol";
            public const string Firearm02PipePistol = "weapon_firearm_02_pipe_pistol";
            public const string Firearm03HeavyRevolver = "weapon_firearm_03_heavy_revolver";
            public const string Firearm04MagnumRevolver = "weapon_firearm_04_magnum_revolver";
            public const string Firearm05FlarePistol = "weapon_firearm_05_flare_pistol";
            public const string Firearm06SawedOffShotgun = "weapon_firearm_06_sawed_off_shotgun";
            public const string Firearm07PumpShotgun = "weapon_firearm_07_pump_shotgun";
            public const string Firearm08LeverShotgun = "weapon_firearm_08_lever_shotgun";
            public const string Firearm09SemiAutoShotgun = "weapon_firearm_09_semi_auto_shotgun";
            public const string Firearm10WastelandAssaultRifle = "weapon_firearm_10_wasteland_assault_rifle";
            public const string Firearm12SuppressedSmg = "weapon_firearm_12_suppressed_smg";
            public const string Firearm13TapedCarbine = "weapon_firearm_13_taped_carbine";
            public const string Firearm14HuntingRifle = "weapon_firearm_14_hunting_rifle";
            public const string Firearm15ScopedBoltRifle = "weapon_firearm_15_scoped_bolt_rifle";
            public const string Firearm16LeverRifle = "weapon_firearm_16_lever_rifle";
            public const string Firearm18BattleRifle = "weapon_firearm_18_battle_rifle";
            public const string Firearm20BeltFedMg = "weapon_firearm_20_belt_fed_mg";
            public const string Firearm21PipeBlunderbuss = "weapon_firearm_21_pipe_blunderbuss";
            public const string Firearm22SingleGrenadeLauncher = "weapon_firearm_22_single_grenade_launcher";
            public const string Firearm23PistolCarbine = "weapon_firearm_23_pistol_carbine";
            public const string Firearm24NailGun = "weapon_firearm_24_nail_gun";
            public const string Firearm26RedWrapAutoRifle = "weapon_firearm_26_red_wrap_auto_rifle";
            public const string Firearm27BullpupRifle = "weapon_firearm_27_bullpup_rifle";
            public const string Firearm28SurvivalCarbine = "weapon_firearm_28_survival_carbine";
            public const string Firearm29AntiMutantRifle = "weapon_firearm_29_anti_mutant_rifle";
            public const string FirearmAdvanced01TacticalSuppressedPistol = "weapon_firearm_advanced_01_tactical_suppressed_pistol";
            public const string FirearmAdvanced02ArmorPiercingRevolver = "weapon_firearm_advanced_02_armor_piercing_revolver";
            public const string FirearmAdvanced03MachinePistol = "weapon_firearm_advanced_03_machine_pistol";
            public const string FirearmAdvanced04CompactPdw = "weapon_firearm_advanced_04_compact_pdw";
            public const string FirearmAdvanced05TacticalSmg = "weapon_firearm_advanced_05_tactical_smg";
            public const string FirearmAdvanced06SuppressedCarbine = "weapon_firearm_advanced_06_suppressed_carbine";
            public const string FirearmAdvanced07ModernAssaultRifle = "weapon_firearm_advanced_07_modern_assault_rifle";
            public const string FirearmAdvanced08HighGradeAk = "weapon_firearm_advanced_08_high_grade_ak";
            public const string FirearmAdvanced09CompactTacticalShotgun = "weapon_firearm_advanced_09_compact_tactical_shotgun";
            public const string FirearmAdvanced10DrumCombatShotgun = "weapon_firearm_advanced_10_drum_combat_shotgun";
            public const string FirearmAdvanced11DesignatedMarksmanRifle = "weapon_firearm_advanced_11_designated_marksman_rifle";
            public const string FirearmAdvanced12PrecisionSniperRifle = "weapon_firearm_advanced_12_precision_sniper_rifle";
            public const string FirearmAdvanced13AntiMaterielRifle = "weapon_firearm_advanced_13_anti_materiel_rifle";
            public const string FirearmAdvanced14BullpupAssaultRifle = "weapon_firearm_advanced_14_bullpup_assault_rifle";
            public const string FirearmAdvanced15HighTierBattleRifle = "weapon_firearm_advanced_15_high_tier_battle_rifle";
            public const string FirearmAdvanced16TacticalLmg = "weapon_firearm_advanced_16_tactical_lmg";
            public const string FirearmAdvanced17CompactSupportGun = "weapon_firearm_advanced_17_compact_support_gun";
            public const string FirearmAdvanced18HeavyAutoShotgun = "weapon_firearm_advanced_18_heavy_auto_shotgun";
            public const string FirearmAdvanced19BreachingShotgun = "weapon_firearm_advanced_19_breaching_shotgun";
            public const string FirearmAdvanced20AdvancedGrenadeLauncher = "weapon_firearm_advanced_20_advanced_grenade_launcher";
            public const string FirearmAdvanced22CoilCarbine = "weapon_firearm_advanced_22_coil_carbine";
            public const string FirearmAdvanced23ElectromagneticMarksmanRifle = "weapon_firearm_advanced_23_electromagnetic_marksman_rifle";
            public const string FirearmAdvanced24SuppressedScoutRifle = "weapon_firearm_advanced_24_suppressed_scout_rifle";
            public const string FirearmAdvanced25HighTierSurvivalRifle = "weapon_firearm_advanced_25_high_tier_survival_rifle";
            public const string FirearmAdvanced26TacticalLauncher = "weapon_firearm_advanced_26_tactical_launcher";
            public const string FirearmAdvanced27AntiMutantCannon = "weapon_firearm_advanced_27_anti_mutant_cannon";
            public const string FirearmAdvanced28RotaryMicrogun = "weapon_firearm_advanced_28_rotary_microgun";
            public const string FirearmAdvanced29EnergyProjector = "weapon_firearm_advanced_29_energy_projector";
            public const string FirearmAdvanced30EliteWastelandSniper = "weapon_firearm_advanced_30_elite_wasteland_sniper";

            public static readonly string[] All =
            {
                Melee01SurvivalKnife,
                Melee02CombatKnife,
                Melee03CurvedMachete,
                Melee04SmallHatchet,
                Melee05FireAxe,
                Melee06Crowbar,
                Melee07PipeWrench,
                Melee08SpikedBat,
                Melee09NailedPlank,
                Melee10RebarSpear,
                Melee11SharpenedShovel,
                Melee12Sledgehammer,
                Melee13CarpenterHammer,
                Melee14ButcherCleaver,
                Melee15BoneClub,
                Melee16ScrapSword,
                Melee17TireIron,
                Melee18ChainWhip,
                Melee19RustedSickle,
                Melee20Pickaxe,
                Melee21Kukri,
                Melee22MetalPipe,
                Melee23SpikedKnuckles,
                Melee24BottleShiv,
                Melee25HarpoonSpear,
                Melee26BayonetStaff,
                Melee27ImprovisedChainsaw,
                Melee28SawbladeAxe,
                Melee29ClawGauntlet,
                Melee30ConcreteMaul,
                MeleeAdvanced01TacticalCarbonKnife,
                MeleeAdvanced02ReinforcedShockMachete,
                MeleeAdvanced03HydraulicFireAxe,
                MeleeAdvanced04BalancedWarPick,
                MeleeAdvanced05BreachingHammer,
                MeleeAdvanced06PoweredSawCleaver,
                MeleeAdvanced07EliteClawGauntlet,
                MeleeAdvanced08TelescopicSpear,
                MeleeAdvanced09MonofilamentChainWhip,
                MeleeAdvanced10CrusherMaul,
                Firearm01ServicePistol,
                Firearm02PipePistol,
                Firearm03HeavyRevolver,
                Firearm04MagnumRevolver,
                Firearm05FlarePistol,
                Firearm06SawedOffShotgun,
                Firearm07PumpShotgun,
                Firearm08LeverShotgun,
                Firearm09SemiAutoShotgun,
                Firearm10WastelandAssaultRifle,
                Firearm12SuppressedSmg,
                Firearm13TapedCarbine,
                Firearm14HuntingRifle,
                Firearm15ScopedBoltRifle,
                Firearm16LeverRifle,
                Firearm18BattleRifle,
                Firearm20BeltFedMg,
                Firearm21PipeBlunderbuss,
                Firearm22SingleGrenadeLauncher,
                Firearm23PistolCarbine,
                Firearm24NailGun,
                Firearm26RedWrapAutoRifle,
                Firearm27BullpupRifle,
                Firearm28SurvivalCarbine,
                Firearm29AntiMutantRifle,
                FirearmAdvanced01TacticalSuppressedPistol,
                FirearmAdvanced02ArmorPiercingRevolver,
                FirearmAdvanced03MachinePistol,
                FirearmAdvanced04CompactPdw,
                FirearmAdvanced05TacticalSmg,
                FirearmAdvanced06SuppressedCarbine,
                FirearmAdvanced07ModernAssaultRifle,
                FirearmAdvanced08HighGradeAk,
                FirearmAdvanced09CompactTacticalShotgun,
                FirearmAdvanced10DrumCombatShotgun,
                FirearmAdvanced11DesignatedMarksmanRifle,
                FirearmAdvanced12PrecisionSniperRifle,
                FirearmAdvanced13AntiMaterielRifle,
                FirearmAdvanced14BullpupAssaultRifle,
                FirearmAdvanced15HighTierBattleRifle,
                FirearmAdvanced16TacticalLmg,
                FirearmAdvanced17CompactSupportGun,
                FirearmAdvanced18HeavyAutoShotgun,
                FirearmAdvanced19BreachingShotgun,
                FirearmAdvanced20AdvancedGrenadeLauncher,
                FirearmAdvanced22CoilCarbine,
                FirearmAdvanced23ElectromagneticMarksmanRifle,
                FirearmAdvanced24SuppressedScoutRifle,
                FirearmAdvanced25HighTierSurvivalRifle,
                FirearmAdvanced26TacticalLauncher,
                FirearmAdvanced27AntiMutantCannon,
                FirearmAdvanced28RotaryMicrogun,
                FirearmAdvanced29EnergyProjector,
                FirearmAdvanced30EliteWastelandSniper,
            };
        }

        public static class Armor
        {
            public const string PatchedClothJacket = "armor_01_patched_cloth_jacket";
            public const string LeatherJacket = "armor_02_leather_jacket";
            public const string PaddedScavengerVest = "armor_03_padded_scavenger_vest";
            public const string ReinforcedHoodie = "armor_04_reinforced_hoodie";
            public const string BikerVest = "armor_05_biker_vest";
            public const string ScrapShoulderArmor = "armor_06_scrap_shoulder_armor";
            public const string TireRubberChestGuard = "armor_07_tire_rubber_chest_guard";
            public const string RaiderLeatherArmor = "armor_08_raider_leather_armor";
            public const string MetalPlateVest = "armor_09_metal_plate_vest";
            public const string RiotVest = "armor_10_riot_vest";
            public const string PoliceTacticalVest = "armor_11_police_tactical_vest";
            public const string FireproofCoat = "armor_12_fireproof_coat";
            public const string HunterArmor = "armor_13_hunter_armor";
            public const string SpikedRaiderArmor = "armor_14_spiked_raider_armor";
            public const string ReinforcedKevlarVest = "armor_15_reinforced_kevlar_vest";
            public const string ScavengerHazmatSuit = "armor_16_scavenger_hazmat_suit";
            public const string ArmoredTrenchCoat = "armor_17_armored_trench_coat";
            public const string JunkyardPlateArmor = "armor_18_junkyard_plate_armor";
            public const string MutantHideArmor = "armor_19_mutant_hide_armor";
            public const string ExoBracedVest = "armor_20_exo_braced_vest";
            public const string CombatArmor = "armor_21_combat_armor";
            public const string HeavyRiotArmor = "armor_22_heavy_riot_armor";
            public const string BlastSuit = "armor_23_blast_suit";
            public const string AdvancedTacticalArmor = "armor_24_advanced_tactical_armor";
            public const string PoweredChestRig = "armor_25_powered_chest_rig";
            public const string CeramicPlateArmor = "armor_26_ceramic_plate_armor";
            public const string EliteWastelandArmor = "armor_27_elite_wasteland_armor";
            public const string HeavyMutantHunterArmor = "armor_28_heavy_mutant_hunter_armor";
            public const string ImprovisedExosuitArmor = "armor_29_improvised_exosuit_armor";
            public const string LegendaryPlatedSurvivalArmor = "armor_30_legendary_plated_survival_armor";
            public const string Midgame01ReinforcedScavengerJacket = "armor_midgame_01_reinforced_scavenger_jacket";
            public const string Midgame02RoadGuardVest = "armor_midgame_02_road_guard_vest";
            public const string Midgame03PatchedKevlarVest = "armor_midgame_03_patched_kevlar_vest";
            public const string Midgame04RaiderPlateVest = "armor_midgame_04_raider_plate_vest";
            public const string Midgame05GuardPatrolArmor = "armor_midgame_05_guard_patrol_armor";
            public const string Midgame06ScrapMetalBreastplate = "armor_midgame_06_scrap_metal_breastplate";
            public const string Midgame07ReinforcedBikerArmor = "armor_midgame_07_reinforced_biker_armor";
            public const string Midgame08WastelandRangerCoat = "armor_midgame_08_wasteland_ranger_coat";
            public const string Midgame09ArmoredMechanicVest = "armor_midgame_09_armored_mechanic_vest";
            public const string Midgame10PlatedLeatherCoat = "armor_midgame_10_plated_leather_coat";
            public const string Midgame11TireShoulderGuard = "armor_midgame_11_tire_shoulder_guard";
            public const string Midgame12PoliceSurplusArmor = "armor_midgame_12_police_surplus_armor";
            public const string Midgame13RiotChestRig = "armor_midgame_13_riot_chest_rig";
            public const string Midgame14FireproofResponderCoat = "armor_midgame_14_fireproof_responder_coat";
            public const string Midgame15HunterScaleVest = "armor_midgame_15_hunter_scale_vest";
            public const string Midgame16CaravanGuardArmor = "armor_midgame_16_caravan_guard_armor";
            public const string Midgame17QuarryWorkerArmor = "armor_midgame_17_quarry_worker_armor";
            public const string Midgame18NomadLamellarVest = "armor_midgame_18_nomad_lamellar_vest";
            public const string Midgame19ScavengedHazmatArmor = "armor_midgame_19_scavenged_hazmat_armor";
            public const string Midgame20WeldedPlateHarness = "armor_midgame_20_welded_plate_harness";
            public const string Midgame21ArmoredPoncho = "armor_midgame_21_armored_poncho";
            public const string Midgame22HighwaymanCuirass = "armor_midgame_22_highwayman_cuirass";
            public const string Midgame23ReinforcedTrenchVest = "armor_midgame_23_reinforced_trench_vest";
            public const string Midgame24MilitiaFlakVest = "armor_midgame_24_militia_flak_vest";
            public const string Midgame25MutantHideReinforcedVest = "armor_midgame_25_mutant_hide_reinforced_vest";
            public const string Midgame26ArmoredSalvageCoat = "armor_midgame_26_armored_salvage_coat";
            public const string Midgame27MediumCombatVest = "armor_midgame_27_medium_combat_vest";
            public const string Midgame28PatchedCeramicVest = "armor_midgame_28_patched_ceramic_vest";
            public const string Midgame29ReinforcedTacticalChestRig = "armor_midgame_29_reinforced_tactical_chest_rig";
            public const string Midgame30VeteranScavengerArmor = "armor_midgame_30_veteran_scavenger_armor";

            public static readonly string[] All =
            {
                PatchedClothJacket,
                LeatherJacket,
                PaddedScavengerVest,
                ReinforcedHoodie,
                BikerVest,
                ScrapShoulderArmor,
                TireRubberChestGuard,
                RaiderLeatherArmor,
                MetalPlateVest,
                RiotVest,
                PoliceTacticalVest,
                FireproofCoat,
                HunterArmor,
                SpikedRaiderArmor,
                ReinforcedKevlarVest,
                ScavengerHazmatSuit,
                ArmoredTrenchCoat,
                JunkyardPlateArmor,
                MutantHideArmor,
                ExoBracedVest,
                CombatArmor,
                HeavyRiotArmor,
                BlastSuit,
                AdvancedTacticalArmor,
                PoweredChestRig,
                CeramicPlateArmor,
                EliteWastelandArmor,
                HeavyMutantHunterArmor,
                ImprovisedExosuitArmor,
                LegendaryPlatedSurvivalArmor,
                Midgame01ReinforcedScavengerJacket,
                Midgame02RoadGuardVest,
                Midgame03PatchedKevlarVest,
                Midgame04RaiderPlateVest,
                Midgame05GuardPatrolArmor,
                Midgame06ScrapMetalBreastplate,
                Midgame07ReinforcedBikerArmor,
                Midgame08WastelandRangerCoat,
                Midgame09ArmoredMechanicVest,
                Midgame10PlatedLeatherCoat,
                Midgame11TireShoulderGuard,
                Midgame12PoliceSurplusArmor,
                Midgame13RiotChestRig,
                Midgame14FireproofResponderCoat,
                Midgame15HunterScaleVest,
                Midgame16CaravanGuardArmor,
                Midgame17QuarryWorkerArmor,
                Midgame18NomadLamellarVest,
                Midgame19ScavengedHazmatArmor,
                Midgame20WeldedPlateHarness,
                Midgame21ArmoredPoncho,
                Midgame22HighwaymanCuirass,
                Midgame23ReinforcedTrenchVest,
                Midgame24MilitiaFlakVest,
                Midgame25MutantHideReinforcedVest,
                Midgame26ArmoredSalvageCoat,
                Midgame27MediumCombatVest,
                Midgame28PatchedCeramicVest,
                Midgame29ReinforcedTacticalChestRig,
                Midgame30VeteranScavengerArmor,
            };
        }

        public static class Utilities
        {
            public const string MedkitTier01RagBundle = "utility_medkit_tier_01_rag_bundle";
            public const string MedkitTier02FieldPouch = "utility_medkit_tier_02_field_pouch";
            public const string MedkitTier03TraumaBag = "utility_medkit_tier_03_trauma_bag";
            public const string MedkitTier04HardcaseMedkit = "utility_medkit_tier_04_hardcase_medkit";
            public const string MedkitTier05AdvancedTraumaCase = "utility_medkit_tier_05_advanced_trauma_case";
            public const string ToolkitTier01RustyToolRoll = "utility_toolkit_tier_01_rusty_tool_roll";
            public const string ToolkitTier02MechanicToolRoll = "utility_toolkit_tier_02_mechanic_tool_roll";
            public const string ToolkitTier03ScrapToolbox = "utility_toolkit_tier_03_scrap_toolbox";
            public const string ToolkitTier04RepairCase = "utility_toolkit_tier_04_repair_case";
            public const string ToolkitTier05AdvancedRepairCase = "utility_toolkit_tier_05_advanced_repair_case";
            public const string AmmoPackTier01LooseAmmoPouch = "utility_ammo_pack_tier_01_loose_ammo_pouch";
            public const string AmmoPackTier02LeatherAmmoBag = "utility_ammo_pack_tier_02_leather_ammo_bag";
            public const string AmmoPackTier03CanvasAmmoPack = "utility_ammo_pack_tier_03_canvas_ammo_pack";
            public const string AmmoPackTier04TacticalAmmoCrate = "utility_ammo_pack_tier_04_tactical_ammo_crate";
            public const string AmmoPackTier05ReinforcedAmmoCrate = "utility_ammo_pack_tier_05_reinforced_ammo_crate";
            public const string BackpackTier01TornScavengerBag = "utility_backpack_tier_01_torn_scavenger_bag";
            public const string BackpackTier02FieldBackpack = "utility_backpack_tier_02_field_backpack";
            public const string BackpackTier03ExpeditionPack = "utility_backpack_tier_03_expedition_pack";
            public const string BackpackTier04ReinforcedPack = "utility_backpack_tier_04_reinforced_pack";
            public const string BackpackTier05EliteCargoPack = "utility_backpack_tier_05_elite_cargo_pack";

            public static readonly string[] All =
            {
                MedkitTier01RagBundle,
                MedkitTier02FieldPouch,
                MedkitTier03TraumaBag,
                MedkitTier04HardcaseMedkit,
                MedkitTier05AdvancedTraumaCase,
                ToolkitTier01RustyToolRoll,
                ToolkitTier02MechanicToolRoll,
                ToolkitTier03ScrapToolbox,
                ToolkitTier04RepairCase,
                ToolkitTier05AdvancedRepairCase,
                AmmoPackTier01LooseAmmoPouch,
                AmmoPackTier02LeatherAmmoBag,
                AmmoPackTier03CanvasAmmoPack,
                AmmoPackTier04TacticalAmmoCrate,
                AmmoPackTier05ReinforcedAmmoCrate,
                BackpackTier01TornScavengerBag,
                BackpackTier02FieldBackpack,
                BackpackTier03ExpeditionPack,
                BackpackTier04ReinforcedPack,
                BackpackTier05EliteCargoPack,
            };
        }

        public static class Items
        {
            public const string RustyKnife = "rusty_knife";
            public const string MetalPipe = "metal_pipe";
            public const string Machete = "machete";
            public const string RustyRevolver = "rusty_revolver";
            public const string SawnOffShotgun = "sawn_off_shotgun";
            public const string HuntingRifle = "hunting_rifle";
            public const string LeatherJacket = "leather_jacket";
            public const string ScrapArmor = "scrap_armor";
            public const string Medkit = "medkit";
            public const string Toolkit = "toolkit";
            public const string AmmoPack = "ammo_pack";
            public const string Backpack = "backpack";

            public static readonly string[] All =
            {
                RustyKnife,
                MetalPipe,
                Machete,
                RustyRevolver,
                SawnOffShotgun,
                HuntingRifle,
                LeatherJacket,
                ScrapArmor,
                Medkit,
                Toolkit,
                AmmoPack,
                Backpack,
            };
        }

        public static class StatusEffects
        {
            public const string Cuts = "cuts";

            public static readonly string[] All =
            {
                Cuts,
            };
        }

        public static class Sounds
        {
            public const string ArmorHeavy = "armor_heavy";
            public const string ArmorLight = "armor_light";
            public const string ArmorMedium = "armor_medium";
            public const string ExplosiveLauncher = "explosive_launcher";
            public const string FirearmAuto = "firearm_auto";
            public const string FirearmPrecision = "firearm_precision";
            public const string FirearmRifle = "firearm_rifle";
            public const string FirearmShotgun = "firearm_shotgun";
            public const string FirearmSidearm = "firearm_sidearm";
            public const string FirearmSmg = "firearm_smg";
            public const string MeleeBlade = "melee_blade";
            public const string MeleeBlunt = "melee_blunt";
            public const string MeleeChain = "melee_chain";
            public const string MeleeFist = "melee_fist";
            public const string MeleeHeavyBlade = "melee_heavy_blade";
            public const string MeleePierce = "melee_pierce";
            public const string UtilityAmmoPack = "utility_ammo_pack";
            public const string UtilityBackpack = "utility_backpack";
            public const string UtilityMedkit = "utility_medkit";
            public const string UtilityToolkit = "utility_toolkit";

            public static readonly string[] All =
            {
                ArmorHeavy,
                ArmorLight,
                ArmorMedium,
                ExplosiveLauncher,
                FirearmAuto,
                FirearmPrecision,
                FirearmRifle,
                FirearmShotgun,
                FirearmSidearm,
                FirearmSmg,
                MeleeBlade,
                MeleeBlunt,
                MeleeChain,
                MeleeFist,
                MeleeHeavyBlade,
                MeleePierce,
                UtilityAmmoPack,
                UtilityBackpack,
                UtilityMedkit,
                UtilityToolkit,
            };
        }

        public static class Events
        {
            public const string SurvivorJoined = GameEventIds.SurvivorJoined;
            public const string DemoCompleted = GameEventIds.DemoCompleted;
            public const string EmergencyScavengeCompleted = GameEventIds.EmergencyScavengeCompleted;
        }

        public static class Conditions
        {
            public const string ZoneCompletions = GameConditionTypes.ZoneCompletions;
            public const string ZoneUnlocked = GameConditionTypes.ZoneUnlocked;
            public const string BuildingLevel = GameConditionTypes.BuildingLevel;
            public const string SurvivorCount = GameConditionTypes.SurvivorCount;
            public const string ResourceAmount = GameConditionTypes.ResourceAmount;
            public const string ExpeditionsCompleted = GameConditionTypes.ExpeditionsCompleted;
            public const string ActiveExpeditions = GameConditionTypes.ActiveExpeditions;
        }
    }
}
