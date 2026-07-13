using System;
using System.Collections.Generic;
using System.Linq;

namespace AshfallCamp.Editor
{
    public static class CampMapProductionV2Manifest
    {
        public const string ArtRoot = "Assets/AshfallCamp/Art/UI/CampMap/ProductionV2";
        public const string SheetFolder = ArtRoot + "/Sheets";
        public const string TileTextureFolder = ArtRoot + "/Tiles";
        public const string PreviewFolder = ArtRoot + "/Previews";
        public const string MarkerRoot = ArtRoot + "/Markers";
        public const string MarkerSheetPath = MarkerRoot + "/location_markers_sheet_v6.png";
        public const string MarkerTextureFolder = MarkerRoot + "/Tiles";
        public const string TileAssetFolder = "Assets/AshfallCamp/Tiles/ProductionV2";
        public const string MarkerTileAssetFolder = TileAssetFolder + "/Markers";
        public const string BrushFolder = TileAssetFolder + "/Brushes";
        public const string BrushPath = BrushFolder + "/AshfallCampProductionV2Brush.asset";
        public const string PalettePath = TileAssetFolder + "/AshfallCampProductionV2SortedPalette.prefab";
        public const string MarkerPalettePath = MarkerTileAssetFolder + "/AshfallCampLocationMarkerSortedPalette.prefab";
        public const string CatalogPrefabPath = "Assets/AshfallCamp/Prefabs/UI/Main/PF_CampTileCatalogV2.prefab";
        public const string MarkerCatalogPrefabPath = "Assets/AshfallCamp/Prefabs/UI/Main/PF_CampLocationMarkerCatalogV2.prefab";
        public const float WorldTileCellSize = 5f;
        public const int RuntimeTextureSize = 512;
        public const int MarkerTextureSize = 256;

        private static readonly ProductionSheetSpec[] SheetsInternal =
        {
            Sheet("sheet_01_biomes_vivid_v5.png", "green_field", "forest_dense", "cracked_wasteland", "marsh_water", "snow_field", "toxic_swamp"),
            Sheet("sheet_02_camp_vivid_v5.png", "camp_core", "camp_buildable", "barracks_basic", "workshop_basic", "water_collector_basic", "mushroom_beds_basic"),
            Sheet("sheet_03_buildings_vivid_v5.png", "infirmary_basic", "radio_tower_basic", "barracks_upgraded", "workshop_upgraded", "water_collector_upgraded", "mushroom_beds_upgraded"),
            Sheet("sheet_04_buildings_vivid_v5.png", "infirmary_upgraded", "radio_tower_upgraded", "barracks_ruined", "workshop_ruined", "water_collector_ruined", "mushroom_beds_ruined"),
            Sheet("sheet_05_locations_vivid_v5.png", "infirmary_ruined", "radio_tower_ruined", "abandoned_store", "dry_suburb", "ruined_clinic", "police_outpost"),
            Sheet("sheet_06_forest_swamp_vivid_v5.png", "mutant_tunnel", "forest_sparse", "dead_forest", "irradiated_forest", "reed_marsh", "bog_islands"),
            Sheet("sheet_07_winter_desert_vivid_v5.png", "snowy_forest", "frozen_marsh", "ash_winter_wasteland", "desert_plain", "rocky_hills", "dried_field"),
            Sheet("sheet_08_city_vivid_v5.png", "ruined_city_residential", "ruined_city_commercial", "ruined_city_factory", "ruined_city_intersection", "ruined_city_overpass", "ruined_city_apartments"),
            Sheet("sheet_09_city_extras_vivid_v5.png", "ruined_city_rail_yard", "ruined_city_civic_center", "power_substation", "collapsed_bunker", "abandoned_checkpoint", "ash_cemetery"),
            Sheet("sheet_10_extras_vivid_v5.png", "wheat_field", "ruins_buffer", "gas_station", "scrapyard", "nuclear_crater", "waste_barrels_zone"),
            Sheet("sheet_11_future_locations_weak_v5.png", "ruined_homestead", "abandoned_greenhouse", "flooded_cellar", "scavenger_hideout", "collapsed_mine", "marauder_camp"),
            Sheet("sheet_12_future_locations_strong_v5.png", "mutant_nest", "military_depot", "underground_laboratory", "biohazard_plant", "reactor_complex", "warlord_fortress"),
            Sheet("sheet_13_grass_tiles_v5.png", "grass_meadow", "grass_tall_field", "grass_wildflower_clearing", "grass_mossy_boulders", "grass_overgrown_ruins", "grass_ivy_bunker"),
            Sheet("sheet_14_winter_tiles_v5.png", "winter_snowdrifts", "winter_cracked_ice", "winter_frosted_boulders", "winter_dead_grove", "winter_snow_ruins", "winter_supply_shelter"),
            Sheet("sheet_15_grass_locations_v5.png", "grass_abandoned_farm", "grass_ranger_station", "grass_overgrown_church", "grass_water_tower", "grass_moss_quarry", "grass_relay_station"),
            Sheet("sheet_16_winter_locations_v5.png", "winter_frozen_cabin", "winter_weather_station", "winter_ice_cave", "winter_snowbound_depot", "winter_research_outpost", "winter_crash_site"),
        };

        private static readonly ProductionMarkerSpec[] MarkersInternal =
        {
            M("abandoned_store"), M("dry_suburb"), M("ruined_clinic"), M("police_outpost"), M("mutant_tunnel"),
            M("ruined_homestead"), M("abandoned_greenhouse"), M("flooded_cellar"), M("scavenger_hideout"), M("collapsed_mine"), M("marauder_camp"),
            M("mutant_nest"), M("military_depot"), M("underground_laboratory"), M("biohazard_plant"), M("reactor_complex"), M("warlord_fortress"),
            M("grass_abandoned_farm"), M("grass_ranger_station"), M("grass_overgrown_church"), M("grass_water_tower"), M("grass_moss_quarry"), M("grass_relay_station"),
            M("winter_frozen_cabin"), M("winter_weather_station"), M("winter_ice_cave"), M("winter_snowbound_depot"), M("winter_research_outpost"), M("winter_crash_site"),
        };

        private static readonly string[] PaletteCategoryOrderInternal =
        {
            "camp", "building", "expedition", "future_location",
            "grass", "grass_location",
            "winter", "winter_extra", "winter_location",
            "forest", "swamp", "desert", "city", "buffer", "hazard", "map_border",
        };

        private static readonly ProductionTileSpec[] SpecsInternal =
        {
            S("camp_core", "camp", "central survivor camp with command tent, campfire, water tank, radio equipment and a rough defensive fence"),
            S("camp_buildable", "camp", "cleared empty camp construction plot marked by short stakes, rope and a few supply crates"),

            S("barracks_basic", "building", "basic survivor barracks made from a repaired military hut, sandbags and a small sleeping area"),
            S("barracks_upgraded", "building", "upgraded fortified survivor barracks with expanded sleeping quarters, reinforced walls and organized storage"),
            S("barracks_ruined", "building", "ruined survivor barracks with collapsed roof, broken bunks, torn canvas and scattered debris"),
            S("workshop_basic", "building", "basic scrap workshop with corrugated metal walls, workbench, tools, tires and parts crates"),
            S("workshop_upgraded", "building", "upgraded industrial scrap workshop with reinforced roof, welding station, organized machinery and parts racks"),
            S("workshop_ruined", "building", "ruined scrap workshop with collapsed sheet-metal roof, broken tools and wrecked machinery"),
            S("water_collector_basic", "building", "basic water collector with one patched tank, gutters, barrels and simple filtration pipes"),
            S("water_collector_upgraded", "building", "upgraded water collector with multiple tanks, elevated catchment, filters, pumps and protected pipes"),
            S("water_collector_ruined", "building", "ruined water collector with split tanks, bent gutters, leaking pipes and muddy ground"),
            S("mushroom_beds_basic", "building", "basic covered mushroom beds under patched canvas with wooden trays, compost sacks and lanterns"),
            S("mushroom_beds_upgraded", "building", "upgraded mushroom growing shelter with expanded tiered beds, water drums and controlled shade"),
            S("mushroom_beds_ruined", "building", "ruined mushroom beds with torn canvas, overturned trays and spoiled dark compost"),
            S("infirmary_basic", "building", "basic field infirmary with patched medical tent, cot, medicine boxes and a faded red cross panel"),
            S("infirmary_upgraded", "building", "upgraded reinforced field infirmary with clean treatment annex, organized supplies and protected entrance"),
            S("infirmary_ruined", "building", "ruined field infirmary with torn roof, broken cot, spilled medical crates and damaged walls"),
            S("radio_tower_basic", "building", "basic improvised radio tower with one lattice mast, antenna dishes, cables and a small operator shack"),
            S("radio_tower_upgraded", "building", "upgraded tall radio tower with reinforced lattice mast, several dishes, generator and fortified control shack"),
            S("radio_tower_ruined", "building", "ruined radio tower with bent collapsed mast, shattered dishes, snapped cables and wrecked control shack"),

            S("abandoned_store", "expedition", "abandoned roadside grocery store with collapsed awning, broken carts, boarded windows and scavenged shelves"),
            S("dry_suburb", "expedition", "dry ruined suburb with several damaged houses, dead trees, broken fences and an abandoned car"),
            S("ruined_clinic", "expedition", "ruined small clinic with collapsed upper floor, faded medical signs, supply crates and broken ambulance debris"),
            S("police_outpost", "expedition", "fortified ruined police outpost with concrete walls, watch platform, barricades and battered patrol vehicles"),
            S("mutant_tunnel", "expedition", "ominous concrete drainage tunnel entrance with bent grate, toxic runoff, warning debris and dark interior"),

            S("forest_sparse", "forest", "sparse conifer forest with young pines, grass, stones and fallen branches"),
            S("forest_dense", "forest", "dense mature conifer forest with layered pines, boulders, shrubs and fallen logs"),
            S("dead_forest", "forest", "dead post-nuclear forest of bare blackened trunks, ash soil, dry brush and scattered stones"),
            S("irradiated_forest", "forest", "irradiated dead forest with twisted trunks, sickly yellow-green moss, contaminated puddles and warning debris"),
            S("marsh_water", "swamp", "shallow marsh water with reeds, lily pads, muddy islands, stones and drowned branches"),
            S("reed_marsh", "swamp", "dense reed marsh with narrow dark water channels, cattails, mud and half-submerged debris"),
            S("bog_islands", "swamp", "peat bog with irregular grassy islands, still brown-green water, reeds and weathered logs"),
            S("toxic_swamp", "swamp", "toxic radioactive swamp with luminous green water, dead reeds, corroded barrels and oily scum"),
            S("snow_field", "winter", "windswept snow field with exposed dead grass, scattered rocks, small drifts and frozen debris"),
            S("snowy_forest", "winter", "snow-covered conifer forest with dark pines, frosted branches, rocks and fallen logs"),
            S("frozen_marsh", "winter", "frozen marsh with cracked blue-gray ice, snow-covered reed clusters, muddy banks and trapped branches"),
            S("ash_winter_wasteland", "winter", "nuclear winter wasteland with dirty snow, gray ash, dead shrubs, cracked soil and windblown debris"),
            S("desert_plain", "desert", "dry desert plain with cracked ochre soil, sparse thorn brush, stones and windblown dust"),
            S("rocky_hills", "desert", "rocky arid hills with layered boulders, eroded ground, dry grasses and narrow gullies"),
            S("cracked_wasteland", "desert", "severely cracked wasteland with dead shrubs, scattered scrap, bleached stones and dusty soil"),
            S("dried_field", "desert", "abandoned dried agricultural field with dead rows, broken irrigation pipe, dust and collapsed fencing"),

            S("ruined_city_residential", "city", "ruined city residential block with two damaged house shells, dead tree and large concrete slab on shared urban ground"),
            S("ruined_city_commercial", "city", "ruined city commercial block with one collapsed shop row, faded awnings, cart and crate on shared urban ground"),
            S("ruined_city_factory", "city", "ruined city factory block with one shattered workshop, smokestack, tank and large pipes on shared urban ground"),
            S("ruined_city_intersection", "city", "open ruined civic plaza with broken fountain, barrier and lamp on shared urban ground"),
            S("ruined_city_overpass", "city", "collapsed raised concrete span with exposed rebar and several large chunks on shared urban ground"),
            S("ruined_city_apartments", "city", "ruined apartment facade with faded colored walls, one car shell and large slabs on shared urban ground"),
            S("ruined_city_rail_yard", "city", "destroyed rail yard with parallel tracks, wrecked freight cars, loading debris and warehouse ruins"),
            S("ruined_city_civic_center", "city", "ruined civic center with luminous stone facade, broad stairs, broken statue and large slabs"),

            S("power_substation", "buffer", "compact damaged power substation with two transformers, teal metal enclosure and cable spool"),
            S("collapsed_bunker", "buffer", "half-buried collapsed concrete bunker entrance with broken hatch, barrel and a few large stones"),
            S("abandoned_checkpoint", "buffer", "abandoned fortified checkpoint with watch booth, sandbags, barrier and one supply crate"),
            S("ash_cemetery", "buffer", "small cemetery on gray ash soil with four dark gravestones, dead tree and broken monument"),
            S("green_field", "buffer", "recovering green field with patchy grass, wildflowers, stones and subtle post-war debris"),
            S("wheat_field", "buffer", "overgrown golden wheat field with broken wind pump, stones, dry weeds and damaged fencing"),
            S("ruins_buffer", "buffer", "low scattered concrete ruins with broken walls, bricks, twisted metal and a partly buried vehicle"),
            S("gas_station", "buffer", "abandoned roadside gas station with collapsed canopy, rusted pumps, cracked forecourt and wrecked car"),
            S("scrapyard", "buffer", "post-apocalyptic scrapyard with stacked wrecks, corrugated fences, tires, metal heaps and a small crane"),
            S("nuclear_crater", "hazard", "large nuclear blast crater with blackened fractured rim, faint sickly glow, ash and vitrified rubble"),
            S("waste_barrels_zone", "hazard", "radioactive waste dump with corroded barrels, leaking green sludge, warning placard debris and contaminated ground"),

            S("ruined_homestead", "future_location", "threat 1 ruined homestead with one damaged cottage, broken fence and supply crates"),
            S("abandoned_greenhouse", "future_location", "threat 2 abandoned greenhouse with cracked glass, overgrown planters and water barrel"),
            S("flooded_cellar", "future_location", "threat 3 flooded cellar with stone stair entrance, teal water, pump and barrels"),
            S("scavenger_hideout", "future_location", "threat 4 scavenger hideout with improvised shack, storage pile and tripwire posts"),
            S("collapsed_mine", "future_location", "threat 5 collapsed mine with timber entrance, cave-in, mine cart and large rocks"),
            S("marauder_camp", "future_location", "threat 6 marauder camp with fortified shelter, watch platform, sandbags and supplies"),
            S("mutant_nest", "future_location", "threat 7 mutant nest with toxic egg sacs, twisted trees and radioactive waste"),
            S("military_depot", "future_location", "threat 8 military depot with reinforced bunker, watch tower, sandbags and ammunition crates"),
            S("underground_laboratory", "future_location", "threat 9 underground laboratory with sealed cyan blast door, dish and hazard canisters"),
            S("biohazard_plant", "future_location", "threat 10 biohazard processing plant with tanks, containment pool and warning marker"),
            S("reactor_complex", "future_location", "threat 11 damaged reactor complex with cooling tower, energized conduits and toxic core"),
            S("warlord_fortress", "future_location", "threat 12 warlord fortress with scrap citadel, two watch towers, heavy gate and red banners"),

            S("grass_meadow", "grass", "lush vivid grass meadow with separated flower clumps and several large stones"),
            S("grass_tall_field", "grass", "broad bands of tall emerald grass with broken fence and sparse stones"),
            S("grass_wildflower_clearing", "grass", "green clearing with large colorful wildflower clusters, fallen log and stones"),
            S("grass_mossy_boulders", "grass", "large rounded boulders covered in vivid moss with sparse fern clusters"),
            S("grass_overgrown_ruins", "grass", "low collapsed stone wall group covered in ivy with slabs and grass clumps"),
            S("grass_ivy_bunker", "grass", "small half-buried concrete bunker covered by ivy and moss with rust barrel and ferns"),

            S("winter_snowdrifts", "winter_extra", "luminous winter field with large sculpted blue-shadow snowdrifts, grass and stones"),
            S("winter_cracked_ice", "winter_extra", "broad vivid cyan frozen lake with large crack lines, snow banks and frozen branch"),
            S("winter_frosted_boulders", "winter_extra", "large rounded boulders capped with snow among clean blue-shadow drifts"),
            S("winter_dead_grove", "winter_extra", "dark leafless grove with heavy frost, fallen log and sparse stones"),
            S("winter_snow_ruins", "winter_extra", "low collapsed stone wall group buried in snow with slabs and icy shrubs"),
            S("winter_supply_shelter", "winter_extra", "small olive emergency tent covered in snow with heater, crates and short fence"),

            S("grass_abandoned_farm", "grass_location", "abandoned farmhouse and barn in vivid grass with broken fence and supply crates"),
            S("grass_ranger_station", "grass_location", "green ranger cabin with lookout platform, log pile and radio antenna"),
            S("grass_overgrown_church", "grass_location", "small ruined stone chapel covered in ivy with broken bell frame and gravestones"),
            S("grass_water_tower", "grass_location", "rusted water tower station with pump shed, barrels and pipe in tall grass"),
            S("grass_moss_quarry", "grass_location", "shallow moss-covered quarry with cut blocks, crane tripod and tool crate"),
            S("grass_relay_station", "grass_location", "reinforced olive relay hut with lattice antenna, solar panel and cable spool"),

            S("winter_frozen_cabin", "winter_location", "damaged dark-wood cabin with heavy snow roof, heater barrel, wood pile and crates"),
            S("winter_weather_station", "winter_location", "pale-blue weather hut with instrument mast, satellite dish and sensor boxes"),
            S("winter_ice_cave", "winter_location", "large vivid cyan ice cave in snow-covered rock mound with sled and icy boulders"),
            S("winter_snowbound_depot", "winter_location", "reinforced olive storage bunker buried in snow with containers, fence and generator"),
            S("winter_research_outpost", "winter_location", "white-and-teal research module with antenna, cyan canisters and equipment crate"),
            S("winter_crash_site", "winter_location", "orange rescue helicopter wreck partly buried in snow with detached rotor and supply cases"),

            S("radioactive_sea_border", "map_border", "impassable luminous radioactive sea for the outer map boundary with broad toxic currents, oil slicks and sparse floating debris"),
        };

        public static IReadOnlyList<ProductionTileSpec> Specs => SpecsInternal;

        public static IReadOnlyList<ProductionTileSpec> PaletteSpecs => SpecsInternal
            .OrderBy(spec => GetPaletteCategoryOrder(spec.Category))
            .ToArray();

        public static IReadOnlyList<ProductionSheetSpec> Sheets => SheetsInternal;

        public static IReadOnlyList<ProductionMarkerSpec> Markers => MarkersInternal;

        public static IReadOnlyList<ProductionMarkerSpec> PaletteMarkers => MarkersInternal
            .OrderBy(marker => GetPaletteCategoryOrder(GetSpec(marker.LocationId).Category))
            .ToArray();

        public static ProductionTileSpec GetSpec(string id)
        {
            return SpecsInternal.FirstOrDefault(spec => string.Equals(spec.Id, id, StringComparison.Ordinal));
        }

        public static int GetPaletteCategoryOrder(string category)
        {
            var index = Array.IndexOf(PaletteCategoryOrderInternal, category);
            return index >= 0 ? index : PaletteCategoryOrderInternal.Length;
        }

        public static string GetTexturePath(string id)
        {
            return TileTextureFolder + "/tile_" + id + ".png";
        }

        public static string GetTileAssetPath(string id)
        {
            return TileAssetFolder + "/tile_" + id + ".asset";
        }

        public static string GetMarkerTexturePath(string id)
        {
            return MarkerTextureFolder + "/marker_" + id + ".png";
        }

        public static string GetMarkerTileAssetPath(string id)
        {
            return MarkerTileAssetFolder + "/tile_marker_" + id + ".asset";
        }

        private static ProductionTileSpec S(string id, string category, string subject)
        {
            return new ProductionTileSpec(id, category, subject);
        }

        private static ProductionSheetSpec Sheet(string fileName, params string[] tileIds)
        {
            return new ProductionSheetSpec(SheetFolder + "/" + fileName, tileIds);
        }

        private static ProductionMarkerSpec M(string locationId)
        {
            return new ProductionMarkerSpec(locationId);
        }
    }

    [Serializable]
    public sealed class ProductionMarkerSpec
    {
        public ProductionMarkerSpec(string locationId)
        {
            LocationId = locationId;
        }

        public string LocationId { get; }

        public string TexturePath => CampMapProductionV2Manifest.GetMarkerTexturePath(LocationId);

        public string TileAssetPath => CampMapProductionV2Manifest.GetMarkerTileAssetPath(LocationId);
    }

    [Serializable]
    public sealed class ProductionSheetSpec
    {
        public ProductionSheetSpec(string texturePath, string[] tileIds)
        {
            TexturePath = texturePath;
            TileIds = tileIds;
        }

        public string TexturePath { get; }

        public IReadOnlyList<string> TileIds { get; }
    }

    [Serializable]
    public sealed class ProductionTileSpec
    {
        public ProductionTileSpec(string id, string category, string subject)
        {
            Id = id;
            Category = category;
            Subject = subject;
        }

        public string Id { get; }

        public string Category { get; }

        public string Subject { get; }

        public string TexturePath => CampMapProductionV2Manifest.GetTexturePath(Id);

        public string TileAssetPath => CampMapProductionV2Manifest.GetTileAssetPath(Id);
    }
}
