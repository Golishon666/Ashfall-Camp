const VERSION = "ashfall-asset-driven-ui-cleanup-2026-07-05";

const fonts = {
  title: { family: "Anton", style: "Regular" },
  heading: { family: "Oswald", style: "SemiBold" },
  headingMedium: { family: "Oswald", style: "Medium" },
  body: { family: "Inter", style: "Regular" },
  bodyMedium: { family: "Inter", style: "Medium" }
};

for (const font of Object.values(fonts)) {
  await figma.loadFontAsync(font);
}

const page = figma.currentPage;
const palette = {
  ink: "#282B2B",
  muted: "#6E6454",
  teal: "#204E58",
  teal2: "#2E6570",
  paper: "#F4E6CE",
  paper2: "#FFF1D9",
  parchment: "#EADBC1",
  line: "#B9A586",
  lineDark: "#7B6950",
  green: "#587341",
  sage: "#7B8C65",
  orange: "#C76A3B",
  red: "#A94D38",
  gold: "#B98533",
  metal: "#2E2C29",
  dark: "#151615",
  blue: "#315F72"
};

const componentNames = [
  "Ashfall/Survivor/Roster Card",
  "Ashfall/Survivor/Detail Panel",
  "Ashfall/Survivor/Skill Row",
  "Ashfall/Combat/Player Battle Card",
  "Ashfall/Combat/Enemy Battle Card",
  "Ashfall/Combat/Equipment Slot",
  "Ashfall/Combat/Stat Row",
  "Ashfall/Buildings/Building Card",
  "Ashfall/Common/Resource Pill",
  "Ashfall/Common/Action Button"
];

const oldContentNames = new Set([
  "Left status column",
  "Camp overview panel",
  "Right expedition column",
  "Survivors roster panel",
  "Selected survivor panel",
  "Survivor assignment panel",
  "Expedition zones panel",
  "Route planning panel",
  "Expedition launch panel",
  "Building categories panel",
  "Building grid panel",
  "Building upgrade panel",
  "Workshop queue panel",
  "Recipe browser panel",
  "Selected recipe panel",
  "Radio tuner panel",
  "Broadcast panel",
  "Recruitment panel",
  "Motivational note"
]);

const survivors = [
  {
    id: "mara",
    name: "Mara",
    role: "Scavenger",
    background: "scavenger",
    trait: "Careful",
    status: "Idle in Camp",
    level: 4,
    hp: 92,
    maxHp: 100,
    fatigue: 18,
    morale: 76,
    weapon: "Rusty Knife",
    weaponAsset: "ui_icon_equipment_machete",
    portrait: "ui_character_battle_survivor_01",
    skills: { Scavenging: 4, Melee: 1, Firearms: 0, Survival: 2, Mechanics: 0, Medicine: 0 }
  },
  {
    id: "elias",
    name: "Elias",
    role: "Ex-Cop",
    background: "ex_cop",
    trait: "Brave",
    status: "Guard Duty",
    level: 4,
    hp: 100,
    maxHp: 100,
    fatigue: 24,
    morale: 70,
    weapon: "Rusty Revolver",
    weaponAsset: "ui_icon_equipment_pistol",
    portrait: "ui_character_battle_survivor_03",
    skills: { Scavenging: 0, Melee: 1, Firearms: 4, Survival: 1, Mechanics: 0, Medicine: 0 }
  },
  {
    id: "nika",
    name: "Nika",
    role: "Mechanic",
    background: "mechanic",
    trait: "Careful",
    status: "Workshop",
    level: 3,
    hp: 86,
    maxHp: 100,
    fatigue: 31,
    morale: 68,
    weapon: "Metal Pipe",
    weaponAsset: "ui_icon_equipment_hatchet",
    portrait: "ui_character_battle_survivor_04",
    skills: { Scavenging: 1, Melee: 0, Firearms: 0, Survival: 1, Mechanics: 4, Medicine: 0 }
  },
  {
    id: "june",
    name: "June",
    role: "Nurse",
    background: "nurse",
    trait: "Lucky",
    status: "Infirmary",
    level: 3,
    hp: 78,
    maxHp: 100,
    fatigue: 28,
    morale: 82,
    weapon: "Rusty Knife",
    weaponAsset: "ui_icon_equipment_medkit",
    portrait: "ui_character_battle_survivor_05",
    skills: { Scavenging: 0, Melee: 1, Firearms: 0, Survival: 0, Mechanics: 0, Medicine: 4 }
  },
  {
    id: "tomas",
    name: "Tomas",
    role: "Brawler",
    background: "brawler",
    trait: "Tough",
    status: "Ready",
    level: 3,
    hp: 112,
    maxHp: 112,
    fatigue: 21,
    morale: 73,
    weapon: "Metal Pipe",
    weaponAsset: "ui_icon_equipment_hatchet",
    portrait: "ui_character_battle_survivor_06",
    skills: { Scavenging: 0, Melee: 4, Firearms: 0, Survival: 1, Mechanics: 0, Medicine: 0 }
  },
  {
    id: "rhea",
    name: "Rhea",
    role: "Hunter",
    background: "hunter",
    trait: "Quiet",
    status: "Scouting",
    level: 5,
    hp: 94,
    maxHp: 100,
    fatigue: 26,
    morale: 79,
    weapon: "Hunting Rifle",
    weaponAsset: "ui_icon_equipment_rifle",
    portrait: "ui_character_battle_survivor_07",
    skills: { Scavenging: 1, Melee: 1, Firearms: 3, Survival: 3, Mechanics: 0, Medicine: 0 }
  },
  {
    id: "kade",
    name: "Kade",
    role: "Scavenger",
    background: "scavenger",
    trait: "Greedy",
    status: "Idle in Camp",
    level: 3,
    hp: 88,
    maxHp: 100,
    fatigue: 35,
    morale: 62,
    weapon: "Machete",
    weaponAsset: "ui_icon_equipment_machete",
    portrait: "ui_character_battle_survivor_08",
    skills: { Scavenging: 3, Melee: 2, Firearms: 0, Survival: 1, Mechanics: 1, Medicine: 0 }
  }
];

const enemies = [
  { id: "feral_dog", name: "Feral Dog", type: "beast", hp: 14, armor: 0, evasion: 8, damage: 3, accuracy: 75, xp: 4, portrait: "ui_character_enemy_weak_rust_claw_01", tag: "FAST" },
  { id: "starving_survivor", name: "Starving Survivor", type: "human", hp: 18, armor: 0, evasion: 4, damage: 4, accuracy: 65, xp: 5, portrait: "ui_character_enemy_weak_scavenger_01", tag: "DESPERATE" },
  { id: "mutant_stray", name: "Mutant Stray", type: "mutant", hp: 26, armor: 1, evasion: 6, damage: 5, accuracy: 68, xp: 8, portrait: "ui_character_enemy_weak_ash_gnawer_01", tag: "PACK" },
  { id: "raider", name: "Raider", type: "human", hp: 30, armor: 1, evasion: 5, damage: 6, accuracy: 62, xp: 10, portrait: "ui_character_enemy_raider_01", tag: "RANGED" },
  { id: "armored_raider", name: "Armored Raider", type: "human", hp: 45, armor: 4, evasion: 2, damage: 7, accuracy: 60, xp: 14, portrait: "ui_character_enemy_elite_tire_shield_mauler_01", tag: "ARMORED" },
  { id: "mutant_brute", name: "Mutant Brute", type: "mutant", hp: 80, armor: 2, evasion: 1, damage: 11, accuracy: 70, xp: 24, portrait: "ui_character_enemy_mid_blister_rusher_01", tag: "BRUTE" }
];

const buildings = [
  { id: "barracks", name: "Barracks", level: 2, workers: "5 / 6", condition: 82, icon: "ui_icon_status_assigned", effects: ["Increases population capacity.", "Improves survivor recovery speed."], cost: { Scrap: 240, Water: 160, Parts: 60 } },
  { id: "workshop", name: "Workshop", level: 2, workers: "4 / 5", condition: 78, icon: "ui_icon_equipment_duct_tape", effects: ["Enables item crafting and repairs.", "Improves tool durability."], cost: { Scrap: 260, Water: 120, Parts: 80 } },
  { id: "water_collector", name: "Water Collector", level: 3, workers: "4 / 4", condition: 88, icon: "ui_icon_resource_water", effects: ["Increases daily water production.", "Improves water purification."], cost: { Scrap: 280, Water: 180, Parts: 60 } },
  { id: "infirmary", name: "Infirmary", level: 2, workers: "3 / 4", condition: 75, icon: "ui_icon_equipment_medkit", effects: ["Heals injuries faster.", "Reduces illness chance."], cost: { Scrap: 220, Medicine: 120, Parts: 60 } },
  { id: "radio_tower", name: "Radio Tower", level: 2, workers: "2 / 3", condition: 70, icon: "ui_icon_equipment_radio", effects: ["Unlocks radio missions and trades.", "Improves intel range."], cost: { Scrap: 230, Water: 200, Parts: 100 } }
];

const resources = [
  { name: "Scrap", value: 1250, icon: "ui_icon_resource_scrap" },
  { name: "Food", value: 320, icon: "ui_icon_resource_food" },
  { name: "Water", value: 480, icon: "ui_icon_resource_water" },
  { name: "Medicine", value: 210, icon: "ui_icon_resource_medicine" },
  { name: "Weapon Parts", value: 75, icon: "ui_icon_resource_parts" }
];

const zones = [
  { name: "Abandoned Store", threat: "Low", distance: 3, loot: "Food, Medicine", enemies: ["Feral Dog", "Starving Survivor"] },
  { name: "Dry Suburb", threat: "Medium", distance: 5, loot: "Scrap, Water", enemies: ["Starving Survivor", "Raider"] },
  { name: "Ruined Clinic", threat: "Medium", distance: 4, loot: "Medicine", enemies: ["Feral Dog", "Mutant Stray"] },
  { name: "Police Outpost", threat: "High", distance: 6, loot: "Weapon Parts", enemies: ["Raider", "Armored Raider"] },
  { name: "Mutant Tunnel", threat: "High", distance: 7, loot: "Scrap, Parts", enemies: ["Mutant Stray", "Mutant Brute"] }
];

const missingAssets = new Set();
const createdComponents = {};

const uiFrame = ensureComponentsFrame();
removeGeneratedComponents();
createComponents(uiFrame);

const createdFrames = [];
createdFrames.push(rebuildCampDashboard());
createdFrames.push(rebuildSurvivors());
createdFrames.push(rebuildExpeditions());
createdFrames.push(rebuildBuildings());
createdFrames.push(rebuildWorkshop());
createdFrames.push(rebuildRadio());
createdFrames.push(rebuildCombatMonitor());

const audit = auditFile(createdFrames);
figma.currentPage.selection = createdFrames;
figma.viewport.scrollAndZoomIntoView(createdFrames);

return {
  version: VERSION,
  frames: createdFrames.map(frame => ({ id: frame.id, name: frame.name, x: frame.x, y: frame.y, width: frame.width, height: frame.height })),
  components: componentNames,
  missingAssets: Array.from(missingAssets).sort(),
  audit
};

function ensureComponentsFrame() {
  let frame = page.findOne(node => node.type === "FRAME" && node.name === "Ashfall Camp - UI Components");
  if (!frame) {
    frame = figma.createFrame();
    frame.name = "Ashfall Camp - UI Components";
    frame.x = -1240;
    frame.y = -4;
    page.appendChild(frame);
  }
  frame.resize(Math.max(frame.width, 1920), Math.max(frame.height, 1500));
  frame.fills = [solid("#F4E6CE")];
  return frame;
}

function removeGeneratedComponents() {
  const targets = new Set(componentNames);
  for (const node of [...page.findAll(candidate => (candidate.type === "COMPONENT" || candidate.type === "COMPONENT_SET") && targets.has(candidate.name))]) {
    node.remove();
  }
  for (const child of [...uiFrame.children]) {
    if (child.name === "Asset gap notes" || child.name === "Asset-driven component audit") child.remove();
  }
}

function createComponents(parent) {
  let x = 42;
  let y = 118;
  createdComponents["Ashfall/Survivor/Roster Card"] = createRosterCardComponent(parent, x, y);
  createdComponents["Ashfall/Survivor/Skill Row"] = createSkillRowComponent(parent, x, y + 118);
  createdComponents["Ashfall/Common/Resource Pill"] = createResourcePillComponent(parent, x, y + 204);
  createdComponents["Ashfall/Common/Action Button"] = createActionButtonComponent(parent, x, y + 276);

  x = 420;
  createdComponents["Ashfall/Survivor/Detail Panel"] = createDetailPanelComponent(parent, x, y);
  createdComponents["Ashfall/Buildings/Building Card"] = createBuildingCardComponent(parent, x, y + 820);

  x = 1020;
  createdComponents["Ashfall/Combat/Player Battle Card"] = createBattleCardComponent(parent, x, y, "player");
  createdComponents["Ashfall/Combat/Enemy Battle Card"] = createBattleCardComponent(parent, x + 216, y, "enemy");
  createdComponents["Ashfall/Combat/Equipment Slot"] = createEquipmentSlotComponent(parent, x, y + 330);
  createdComponents["Ashfall/Combat/Stat Row"] = createStatRowComponent(parent, x, y + 396);

  const note = frame(parent, "Asset gap notes", 42, 44, 1220, 50, { fill: "#FFF3DE", stroke: "#C2A987", radius: 8 });
  addText(note, "Title", "FIGMA NOTES", 18, 10, 118, 24, { font: "heading", size: 18, fill: palette.teal });
  addText(
    note,
    "Body",
    "Missing production asset gap: no individual building thumbnails for Barracks, Workshop, Water Collector, Infirmary, Radio Tower. Building cards use icon/card layout until modular thumbnails are produced without baked text.",
    148,
    11,
    1038,
    28,
    { font: "bodyMedium", size: 12, fill: palette.ink }
  );
}

function createRosterCardComponent(parent, x, y) {
  const c = component(parent, "Ashfall/Survivor/Roster Card", x, y, 296, 86);
  c.fills = [solid("#F8EBD5")];
  c.strokes = [solid("#C3AD8E")];
  c.strokeWeight = 1;
  c.cornerRadius = 8;
  assetRect(c, "Portrait", 10, 10, 66, 66, "ui_character_battle_survivor_01", 6);
  rect(c, "Selected Accent", 0, 0, 7, 86, "#2F6572", 0);
  addText(c, "Name", "Mara", 88, 10, 122, 22, { font: "heading", size: 18, fill: palette.ink });
  addText(c, "Role", "Scavenger", 88, 33, 102, 18, { font: "bodyMedium", size: 11, fill: palette.teal });
  addText(c, "Status", "Idle in Camp", 88, 57, 116, 18, { font: "body", size: 11, fill: palette.muted });
  addText(c, "HP", "92 / 100", 210, 12, 70, 18, { font: "headingMedium", size: 13, fill: palette.ink, align: "RIGHT" });
  progress(c, "HP Track", "HP Fill", 210, 38, 70, 8, 0.82, palette.green);
  return c;
}

function createDetailPanelComponent(parent, x, y) {
  const c = component(parent, "Ashfall/Survivor/Detail Panel", x, y, 646, 760);
  c.fills = [solid("#F6E8D1")];
  c.strokes = [solid("#B9A586")];
  c.strokeWeight = 1;
  c.cornerRadius = 10;
  addText(c, "Panel Title", "SURVIVOR DETAIL", 24, 22, 280, 34, { font: "heading", size: 26, fill: palette.teal });
  assetRect(c, "Portrait", 24, 72, 256, 310, "ui_character_battle_survivor_01", 8);
  rect(c, "Portrait Wash", 24, 320, 256, 62, "#0D0E0D", 8, 0.28);
  addText(c, "Name", "Mara", 44, 328, 180, 34, { font: "title", size: 38, fill: "#F7EBD6" });
  addText(c, "Role", "SCAVENGER", 48, 363, 130, 18, { font: "heading", size: 14, fill: "#D7E3D4" });
  addText(c, "Level", "LVL 4", 220, 356, 46, 18, { font: "heading", size: 14, fill: "#F7EBD6", align: "RIGHT" });
  return c;
}

function createSkillRowComponent(parent, x, y) {
  const c = component(parent, "Ashfall/Survivor/Skill Row", x, y, 490, 56);
  c.fills = [solid("#FFF2DC", 0.72)];
  c.strokes = [solid("#C5B08F", 0.72)];
  c.strokeWeight = 1;
  c.cornerRadius = 6;
  rect(c, "Icon Box", 10, 10, 36, 36, "#2F6572", 5);
  addText(c, "Icon", "S", 10, 16, 36, 22, { font: "heading", size: 17, fill: "#F8EBD5", align: "CENTER" });
  addText(c, "Label", "Scavenging", 58, 9, 170, 20, { font: "heading", size: 15, fill: palette.ink });
  addText(c, "Description", "Find supplies in ruins.", 58, 29, 206, 18, { font: "body", size: 11, fill: palette.muted });
  progress(c, "Skill Track", "Skill Fill", 280, 24, 150, 8, 0.66, palette.green);
  addText(c, "Value", "4", 444, 17, 28, 20, { font: "heading", size: 17, fill: palette.ink, align: "RIGHT" });
  return c;
}

function createResourcePillComponent(parent, x, y) {
  const c = component(parent, "Ashfall/Common/Resource Pill", x, y, 148, 42);
  c.fills = [solid("#2E2C29")];
  c.strokes = [solid("#867355")];
  c.strokeWeight = 1;
  c.cornerRadius = 8;
  assetRect(c, "Icon", 10, 9, 24, 24, "ui_icon_resource_scrap", 2);
  addText(c, "Value", "1,250", 42, 8, 78, 20, { font: "heading", size: 16, fill: "#F7EAD3" });
  addText(c, "Label", "Scrap", 42, 25, 76, 14, { font: "body", size: 9, fill: "#C8BCA8" });
  return c;
}

function createActionButtonComponent(parent, x, y) {
  const c = component(parent, "Ashfall/Common/Action Button", x, y, 186, 46);
  c.fills = [solid(palette.orange)];
  c.strokes = [solid("#7E3A22")];
  c.strokeWeight = 1.5;
  c.cornerRadius = 7;
  addText(c, "Icon", ">", 18, 12, 18, 20, { font: "heading", size: 18, fill: "#FFF1D9", align: "CENTER" });
  addText(c, "Label", "END TURN", 48, 11, 112, 24, { font: "heading", size: 17, fill: "#FFF1D9", align: "CENTER" });
  return c;
}

function createEquipmentSlotComponent(parent, x, y) {
  const c = component(parent, "Ashfall/Combat/Equipment Slot", x, y, 42, 42);
  c.fills = imageFills("ui_card_player_equipment_slot", "FILL");
  c.strokes = [solid("#4C402F")];
  c.strokeWeight = 1;
  c.cornerRadius = 4;
  assetRect(c, "Icon", 8, 8, 26, 26, "ui_icon_equipment_rifle", 2, "FIT");
  return c;
}

function createStatRowComponent(parent, x, y) {
  const c = component(parent, "Ashfall/Combat/Stat Row", x, y, 132, 24);
  c.fills = [solid("#241F19", 0.72)];
  c.cornerRadius = 4;
  assetRect(c, "Icon", 6, 4, 16, 16, "ui_card_icon_stat_damage", 2, "FIT");
  addText(c, "Label", "DMG", 28, 6, 42, 14, { font: "bodyMedium", size: 9, fill: "#D8C6A9" });
  addText(c, "Value", "64", 84, 2, 36, 18, { font: "heading", size: 15, fill: "#F9E8C8", align: "RIGHT" });
  return c;
}

function createBattleCardComponent(parent, x, y, kind) {
  const isEnemy = kind === "enemy";
  const c = component(parent, isEnemy ? "Ashfall/Combat/Enemy Battle Card" : "Ashfall/Combat/Player Battle Card", x, y, 176, 286);
  c.fills = imageFills(isEnemy ? "ui_card_enemy_outer_frame" : "ui_card_player_outer_frame", "FILL");
  c.strokes = [solid(isEnemy ? "#6B2E20" : "#2B4C31")];
  c.strokeWeight = 1;
  c.cornerRadius = 7;
  assetRect(c, "Portrait", 14, 48, 148, 126, isEnemy ? "ui_character_enemy_raider_01" : "ui_character_battle_survivor_01", 5);
  assetRect(c, "Portrait Frame", 10, 44, 156, 136, isEnemy ? "ui_card_enemy_portrait_frame" : "ui_card_player_portrait_frame", 5);
  rect(c, "Nameplate Tint", 11, 13, 154, 34, isEnemy ? "#4A1F16" : "#1F3B2C", 5, 0.88);
  addText(c, "Name", isEnemy ? "Raider" : "Mara", 20, 17, 102, 18, { font: "heading", size: 15, fill: "#F8EBD5" });
  addText(c, "Level", isEnemy ? "LVL 2" : "LVL 4", 126, 18, 32, 16, { font: "bodyMedium", size: 9, fill: "#F8EBD5", align: "RIGHT" });
  addText(c, "Role", isEnemy ? "HUMAN" : "SCAVENGER", 20, 33, 116, 12, { font: "bodyMedium", size: 8, fill: isEnemy ? "#E8B19D" : "#BFD6B4" });
  rect(c, "HP Track", 20, 184, 136, 10, "#2A2119", 5);
  rect(c, "HP Fill", 20, 184, 108, 10, isEnemy ? "#C85636" : "#6F8A4B", 5);
  addText(c, "HP", isEnemy ? "30/30" : "92/100", 48, 163, 80, 20, { font: "heading", size: 19, fill: "#F8EBD5", align: "CENTER" });
  rect(c, "Tag Dot", 21, 204, 9, 9, isEnemy ? palette.red : palette.sage, 9);
  addText(c, "Tag", isEnemy ? "RANGED" : "CAREFUL", 36, 201, 92, 16, { font: "heading", size: 11, fill: isEnemy ? "#EB8564" : "#C9DFB6" });
  for (let i = 0; i < 3; i += 1) {
    const slot = c.createInstance ? null : null;
    const slotFrame = frame(c, `Equipment Slot ${i + 1}`, 20 + i * 46, 222, 38, 38, { fill: "#221D17", stroke: "#67533A", radius: 4 });
    assetRect(slotFrame, "Icon", 7, 7, 24, 24, i === 0 ? (isEnemy ? "ui_icon_equipment_machete" : "ui_icon_equipment_rifle") : (i === 1 ? "ui_icon_equipment_armor_vest" : "ui_icon_equipment_backpack"), 2, "FIT");
  }
  addText(c, "Damage", "64", 22, 265, 34, 16, { font: "heading", size: 14, fill: "#F8EBD5", align: "CENTER" });
  addText(c, "Armor", "18", 72, 265, 34, 16, { font: "heading", size: 14, fill: "#F8EBD5", align: "CENTER" });
  addText(c, "Speed", "13", 122, 265, 34, 16, { font: "heading", size: 14, fill: "#F8EBD5", align: "CENTER" });
  return c;
}

function createBuildingCardComponent(parent, x, y) {
  const c = component(parent, "Ashfall/Buildings/Building Card", x, y, 500, 238);
  c.fills = [solid("#F6E8D0")];
  c.strokes = [solid("#B9A586")];
  c.strokeWeight = 1;
  c.cornerRadius = 8;
  rect(c, "Icon Tile", 18, 18, 116, 116, "#E3D2B6", 8);
  assetRect(c, "Icon", 46, 46, 60, 60, "ui_icon_status_assigned", 4, "FIT");
  addText(c, "Name", "Barracks", 154, 18, 220, 28, { font: "heading", size: 24, fill: palette.teal });
  addText(c, "Level", "Level 2", 154, 48, 120, 18, { font: "bodyMedium", size: 12, fill: palette.ink });
  addText(c, "Workers", "5 / 6", 404, 25, 56, 18, { font: "heading", size: 14, fill: palette.ink, align: "RIGHT" });
  addText(c, "Effect 1", "Increases population capacity.", 154, 82, 286, 18, { font: "body", size: 12, fill: palette.ink });
  addText(c, "Effect 2", "Improves recovery speed.", 154, 106, 286, 18, { font: "body", size: 12, fill: palette.ink });
  addText(c, "Condition Label", "Condition", 18, 148, 76, 16, { font: "bodyMedium", size: 10, fill: palette.muted });
  progress(c, "Condition Track", "Condition Fill", 92, 151, 180, 8, 0.82, palette.green);
  addText(c, "Condition Value", "82%", 282, 144, 44, 18, { font: "heading", size: 14, fill: palette.ink, align: "RIGHT" });
  addText(c, "Cost", "240 Scrap   160 Water   60 Parts", 18, 188, 260, 18, { font: "bodyMedium", size: 11, fill: palette.ink });
  const button = rect(c, "Upgrade Button", 352, 178, 122, 38, palette.orange, 6);
  button.strokes = [solid("#7E3A22")];
  button.strokeWeight = 1;
  addText(c, "Button Label", "UPGRADE", 368, 188, 90, 18, { font: "heading", size: 14, fill: "#FFF1D9", align: "CENTER" });
  return c;
}

function rebuildCampDashboard() {
  const frameNode = fullHdFrame("Ashfall Camp - Full HD / 01 Camp Dashboard");
  clearFrameContent(frameNode);
  const root = contentRoot(frameNode, "Ashfall Asset-Driven / Camp Dashboard");
  addScreenTitle(root, "CAMP DASHBOARD", "Catalog-driven overview: real resources, survivors, buildings");

  const left = panel(root, "Camp Status", 36, 134, 336, 776);
  sectionTitle(left, "CAMP STATUS", 22, 20);
  addMetric(left, "Population", "42 / 60", 26, 72);
  addMetric(left, "Workers", "28 / 32", 26, 118);
  addMetric(left, "Morale", "Good", 26, 164, "ui_icon_status_morale");
  addMetric(left, "Safety", "Good", 26, 210, "ui_icon_status_safe");
  addMetric(left, "Food / Day", "324", 26, 276, "ui_icon_resource_food");
  addMetric(left, "Water / Day", "280", 26, 322, "ui_icon_resource_water");
  addMetric(left, "Power", "210", 26, 368, "ui_icon_energy_lightning");
  line(left, 22, 430, 292);
  sectionTitle(left, "ACTIVE SURVIVORS", 22, 456);
  survivors.slice(0, 4).forEach((survivor, index) => {
    const card = instance("Ashfall/Survivor/Roster Card", left, 20, 500 + index * 62);
    card.resize(296, 58);
    setRosterCard(card, survivor, index === 0, true);
  });

  const center = panel(root, "Operations", 394, 134, 1014, 776);
  sectionTitle(center, "OPERATIONS BOARD", 22, 20);
  resources.forEach((resource, index) => {
    const pill = instance("Ashfall/Common/Resource Pill", center, 22 + index * 158, 64);
    setResourcePill(pill, resource);
  });
  const map = frame(center, "Camp Snapshot", 22, 132, 620, 300, { fill: "#D8C6A9", stroke: "#A48E6B", radius: 8 });
  assetRect(map, "Camp Overview Art", 0, 0, 620, 300, "ui_bg_camp_overview_01", 8);
  rect(map, "Camp Overlay", 0, 220, 620, 80, "#1E1D19", 8, 0.42);
  addText(map, "Snapshot Title", "Ashfall Camp", 26, 235, 180, 26, { font: "heading", size: 22, fill: "#FFF1D9" });
  addText(map, "Snapshot Body", "Rebuild priorities: water, medical capacity, radio range.", 26, 263, 360, 18, { font: "body", size: 12, fill: "#F4E6CE" });
  const priorities = frame(center, "Priority Queue", 670, 132, 300, 300, { fill: "#FFF0D9", stroke: "#B9A586", radius: 8 });
  sectionTitle(priorities, "TODAY", 20, 18);
  ["Assign 2 workers to Water Collector", "Craft bandages in Workshop", "Scout Dry Suburb for Food", "Repair Barracks condition"].forEach((item, index) => {
    addChecklist(priorities, item, 24, 62 + index * 52, index < 2);
  });
  sectionTitle(center, "BUILDINGS", 22, 468);
  buildings.slice(0, 3).forEach((building, index) => {
    const card = compactBuildingRow(center, 22 + index * 318, 514, building);
    center.appendChild(card);
  });

  const right = panel(root, "Expedition Readiness", 1430, 134, 454, 776);
  sectionTitle(right, "EXPEDITION READINESS", 22, 20);
  addText(right, "Mission", "Recommended Mission", 24, 64, 170, 18, { font: "bodyMedium", size: 12, fill: palette.muted });
  addText(right, "Mission Name", "Scavenge Run - Dry Suburb", 24, 88, 290, 26, { font: "heading", size: 22, fill: palette.ink });
  addMetric(right, "Threat", "Medium", 24, 140, "ui_icon_status_danger");
  addMetric(right, "Distance", "5", 24, 186);
  addMetric(right, "Expected Loot", "Scrap, Water", 24, 232, "ui_icon_resource_scrap");
  line(right, 22, 300, 408);
  sectionTitle(right, "SQUAD", 22, 326);
  survivors.slice(0, 4).forEach((survivor, index) => {
    addPortraitChip(right, survivor, 24 + index * 100, 374, 82);
  });
  const launch = instance("Ashfall/Common/Action Button", right, 24, 504);
  setButton(launch, "EXPEDITION", ">");
  const radio = instance("Ashfall/Common/Action Button", right, 224, 504);
  setButton(radio, "RADIO", ">");
  return frameNode;
}

function rebuildSurvivors() {
  const frameNode = fullHdFrame("Ashfall Camp - Full HD / 02 Survivors");
  clearFrameContent(frameNode);
  const root = contentRoot(frameNode, "Ashfall Asset-Driven / Survivors");
  addScreenTitle(root, "SURVIVORS", "Roster and detail panels use SurvivorCatalog names and portraits");

  const rosterPanel = panel(root, "Roster", 36, 134, 336, 804);
  sectionTitle(rosterPanel, "ROSTER", 22, 20);
  addSegmented(rosterPanel, ["ALL", "READY", "WOUNDED"], 22, 58, 292, 38, 0);
  survivors.forEach((survivor, index) => {
    const card = instance("Ashfall/Survivor/Roster Card", rosterPanel, 20, 116 + index * 94);
    setRosterCard(card, survivor, index === 0);
  });

  const detailWrap = frame(root, "Survivor Detail Panel Wrapper", 394, 134, 646, 760, {});
  detailWrap.clipsContent = false;
  const detail = instance("Ashfall/Survivor/Detail Panel", detailWrap, 0, 0);
  setText(detail, "Name", "MARA");
  setText(detail, "Role", "SCAVENGER / CAREFUL");
  setText(detail, "Level", "LVL 4");
  setImage(detail, "Portrait", survivors[0].portrait);
  addDetailStats(detailWrap, survivors[0]);
  Object.entries(survivors[0].skills).forEach(([skillName, value], index) => {
    const row = instance("Ashfall/Survivor/Skill Row", detailWrap, 306, 96 + index * 68);
    setSkillRow(row, skillName, value, skillDescription(skillName));
  });
  addText(detailWrap, "Bio Title", "BIOGRAPHY / NOTES", 24, 424, 220, 24, { font: "heading", size: 19, fill: palette.teal });
  addText(
    detailWrap,
    "Biography",
    "Mara is the starting scavenger: careful, fast to assign, and strongest when the camp needs food, scrap, or medicine from short ruins runs.",
    24,
    456,
    256,
    74,
    { font: "body", size: 12, fill: palette.ink }
  );
  addText(detailWrap, "Equipment Title", "EQUIPMENT", 306, 528, 140, 24, { font: "heading", size: 19, fill: palette.teal });
  addEquipmentLine(detailWrap, survivors[0].weaponAsset, survivors[0].weapon, "Weapon", 306, 566);
  addEquipmentLine(detailWrap, "ui_icon_equipment_backpack", "Scout Pack", "Backpack", 306, 626);
  addEquipmentLine(detailWrap, "ui_icon_equipment_medkit", "Bandages (3)", "Supplies", 306, 686);

  const actions = panel(root, "Actions", 1062, 134, 346, 804);
  sectionTitle(actions, "ASSIGNMENT", 22, 20);
  addMetric(actions, "Current State", "Idle in Camp", 24, 70, "ui_icon_status_idle");
  addMetric(actions, "Health", "92 / 100", 24, 116, "ui_icon_status_healthy");
  addMetric(actions, "Fatigue", "18 / 100", 24, 162, "ui_icon_status_fatigue");
  addMetric(actions, "Morale", "76 / 100", 24, 208, "ui_icon_status_morale");
  line(actions, 22, 270, 300);
  ["REST", "TREAT", "ASSIGN", "DISMISS"].forEach((label, index) => {
    const button = instance("Ashfall/Common/Action Button", actions, 24, 310 + index * 64);
    setButton(button, label, index === 3 ? "X" : ">");
    tintButton(button, index === 3 ? palette.red : (index === 1 ? palette.blue : palette.green));
  });
  sectionTitle(actions, "CATALOG SOURCE", 22, 600);
  addText(actions, "Source Body", "SurvivorCatalog + CampUiCatalog portrait map: Mara, Elias, Nika, June, Tomas, Rhea, Kade.", 24, 638, 292, 62, { font: "body", size: 12, fill: palette.ink });
  return frameNode;
}

function rebuildExpeditions() {
  const frameNode = fullHdFrame("Ashfall Camp - Full HD / 03 Expeditions");
  clearFrameContent(frameNode);
  const root = contentRoot(frameNode, "Ashfall Asset-Driven / Expeditions");
  addScreenTitle(root, "EXPEDITIONS", "ZoneCatalog and EnemyCatalog driven mission setup");

  const left = panel(root, "Zone List", 36, 134, 336, 804);
  sectionTitle(left, "ZONE CATALOG", 22, 20);
  zones.forEach((zone, index) => {
    const row = frame(left, `Zone / ${zone.name}`, 22, 64 + index * 104, 292, 84, { fill: index === 1 ? "#F2E1C6" : "#FFF1D9", stroke: "#C3AD8E", radius: 6 });
    addText(row, "Name", zone.name, 16, 12, 190, 20, { font: "heading", size: 16, fill: palette.ink });
    addText(row, "Threat", `Threat: ${zone.threat}`, 16, 36, 110, 14, { font: "bodyMedium", size: 10, fill: zone.threat === "High" ? palette.red : palette.teal });
    addText(row, "Loot", zone.loot, 16, 56, 160, 14, { font: "body", size: 10, fill: palette.muted });
    addText(row, "Distance", `${zone.distance}`, 244, 24, 28, 24, { font: "heading", size: 22, fill: palette.ink, align: "CENTER" });
  });

  const center = panel(root, "Mission Builder", 394, 134, 1014, 804);
  sectionTitle(center, "MISSION: SCAVENGE RUN", 22, 20);
  const map = frame(center, "Route Map", 22, 66, 610, 344, { fill: "#D6C09C", stroke: "#A48E6B", radius: 8 });
  assetRect(map, "Minimap", 0, 0, 610, 344, "ui_map_zone_minimap_local", 8);
  rect(map, "Route Overlay", 48, 160, 510, 4, palette.paper2, 2, 0.78);
  for (let i = 0; i < 6; i += 1) {
    rect(map, `Node ${i}`, 62 + i * 94, 148 + (i % 2) * 26, 24, 24, i === 0 ? palette.green : (i === 5 ? palette.red : palette.paper2), 24);
  }
  const squad = frame(center, "Squad", 660, 66, 322, 344, { fill: "#FFF0D9", stroke: "#B9A586", radius: 8 });
  sectionTitle(squad, "SQUAD 4 / 4", 20, 18);
  survivors.slice(0, 4).forEach((survivor, index) => addPortraitChip(squad, survivor, 24 + (index % 2) * 144, 64 + Math.floor(index / 2) * 120, 102));
  sectionTitle(center, "EXPECTED ENCOUNTERS", 22, 446);
  enemies.forEach((enemy, index) => {
    const chip = frame(center, `Enemy Catalog / ${enemy.name}`, 22 + (index % 3) * 318, 490 + Math.floor(index / 3) * 118, 292, 92, { fill: "#FFF1D9", stroke: "#C3AD8E", radius: 7 });
    assetRect(chip, "Portrait", 12, 12, 66, 66, enemy.portrait, 5);
    addText(chip, "Name", enemy.name, 90, 12, 148, 20, { font: "heading", size: 16, fill: palette.ink });
    addText(chip, "Id", enemy.id, 90, 34, 150, 14, { font: "body", size: 10, fill: palette.muted });
    addText(chip, "Stats", `HP ${enemy.hp}  ARM ${enemy.armor}  DMG ${enemy.damage}`, 90, 56, 180, 16, { font: "bodyMedium", size: 11, fill: palette.teal });
  });

  const right = panel(root, "Launch", 1430, 134, 454, 804);
  sectionTitle(right, "LAUNCH SUMMARY", 22, 20);
  addMetric(right, "Mission", "Scavenge Run", 24, 70);
  addMetric(right, "Selected Zone", "Dry Suburb", 24, 116);
  addMetric(right, "Threat", "Medium", 24, 162, "ui_icon_status_danger");
  addMetric(right, "Distance Left", "5", 24, 208);
  addMetric(right, "Weather", "Cloudy", 24, 254);
  line(right, 22, 318, 408);
  sectionTitle(right, "LOOT PREVIEW", 22, 344);
  ["ui_icon_resource_scrap", "ui_icon_resource_water", "ui_icon_resource_food", "ui_icon_resource_medicine", "ui_icon_resource_parts"].forEach((asset, index) => {
    frameIcon(right, asset, 24 + index * 72, 386, 54);
  });
  const button = instance("Ashfall/Common/Action Button", right, 24, 500);
  setButton(button, "START", ">");
  const retreat = instance("Ashfall/Common/Action Button", right, 224, 500);
  setButton(retreat, "EDIT SQUAD", "<");
  tintButton(retreat, palette.teal);
  addText(right, "Data", "Only EnemyCatalog names are used in previews. Concept-only enemies from old mockups are intentionally omitted.", 24, 620, 360, 58, { font: "bodyMedium", size: 12, fill: palette.ink });
  return frameNode;
}

function rebuildBuildings() {
  const frameNode = fullHdFrame("Ashfall Camp - Full HD / 04 Buildings");
  clearFrameContent(frameNode);
  const root = contentRoot(frameNode, "Ashfall Asset-Driven / Buildings");
  addScreenTitle(root, "BUILDINGS", "Production cards use BuildingCatalog data; thumbnails are noted as missing");

  const left = panel(root, "Building Summary", 36, 134, 336, 804);
  sectionTitle(left, "CAMP SUMMARY", 22, 20);
  addMetric(left, "Population", "42 / 60", 24, 72, "ui_icon_status_assigned");
  addMetric(left, "Workers", "28 / 32", 24, 118, "ui_icon_status_assigned");
  addMetric(left, "Morale", "Good", 24, 164, "ui_icon_status_morale");
  addMetric(left, "Safety", "Good", 24, 210, "ui_icon_status_safe");
  line(left, 22, 270, 292);
  sectionTitle(left, "BUILDING CAPACITY", 22, 302);
  addMetric(left, "Total Buildings", "12 / 20", 24, 354);
  addMetric(left, "Assigned Workers", "28 / 32", 24, 400);
  addMetric(left, "Idle Workers", "4", 24, 446);
  const note = frame(left, "Building Art Note", 24, 532, 286, 138, { fill: "#FFF2DC", stroke: "#C3AD8E", radius: 8 });
  addText(note, "Title", "ASSET GAP", 18, 18, 100, 18, { font: "heading", size: 15, fill: palette.teal });
  addText(note, "Body", "No separate production building thumbnails yet. Cards intentionally use icon/layout placeholders tied to real catalog IDs.", 18, 48, 244, 56, { font: "body", size: 12, fill: palette.ink });

  const grid = panel(root, "Building Grid", 394, 134, 1014, 804);
  sectionTitle(grid, "BUILDING CATALOG", 22, 20);
  addSegmented(grid, ["ALL", "PRODUCTION", "SUPPORT", "DEFENSE"], 22, 58, 632, 38, 0);
  buildings.forEach((building, index) => {
    const x = 22 + (index % 2) * 512;
    const y = 120 + Math.floor(index / 2) * 248;
    const card = instance("Ashfall/Buildings/Building Card", grid, x, y);
    setBuildingCard(card, building);
  });
  const addSlot = frame(grid, "Build New Structure Slot", 534, 616, 456, 150, { fill: "#F5E8D2", stroke: "#B9A586", radius: 8, dash: [8, 6] });
  addText(addSlot, "Plus", "+", 198, 28, 60, 46, { font: "heading", size: 42, fill: palette.lineDark, align: "CENTER" });
  addText(addSlot, "Label", "BUILD NEW STRUCTURE", 124, 90, 208, 20, { font: "heading", size: 17, fill: palette.ink, align: "CENTER" });

  const right = panel(root, "Upgrade Details", 1430, 134, 454, 804);
  sectionTitle(right, "SELECTED: WORKSHOP", 22, 20);
  assetRect(right, "Workshop Icon", 24, 70, 110, 110, "ui_icon_equipment_duct_tape", 10, "FIT");
  addText(right, "Name", "Workshop", 154, 76, 188, 28, { font: "heading", size: 24, fill: palette.ink });
  addText(right, "Level", "Level 2 -> 3", 154, 108, 120, 18, { font: "bodyMedium", size: 12, fill: palette.teal });
  addMetric(right, "Workers", "4 / 5", 24, 224, "ui_icon_status_assigned");
  addMetric(right, "Condition", "78%", 24, 270, "ui_icon_status_safe");
  addMetric(right, "Upgrade Cost", "260 Scrap, 120 Water, 80 Parts", 24, 336, "ui_icon_resource_scrap");
  const upgrade = instance("Ashfall/Common/Action Button", right, 24, 430);
  setButton(upgrade, "UPGRADE", ">");
  addText(right, "Effect", "Enables item crafting and repairs. Improves tool durability.", 24, 526, 340, 46, { font: "body", size: 12, fill: palette.ink });
  return frameNode;
}

function rebuildWorkshop() {
  const frameNode = fullHdFrame("Ashfall Camp - Full HD / 05 Workshop");
  clearFrameContent(frameNode);
  const root = contentRoot(frameNode, "Ashfall Asset-Driven / Workshop");
  addScreenTitle(root, "WORKSHOP", "Equipment and recipe surface uses production equipment/resource icons");

  const left = panel(root, "Queue", 36, 134, 336, 804);
  sectionTitle(left, "CRAFT QUEUE", 22, 20);
  [
    ["Bandages", "ui_icon_equipment_medkit", "2 turns"],
    ["Rifle Repair", "ui_icon_equipment_rifle", "1 turn"],
    ["Ammo Box", "ui_icon_equipment_ammo_box", "3 turns"]
  ].forEach((item, index) => {
    const row = frame(left, `Queue / ${item[0]}`, 22, 70 + index * 112, 292, 88, { fill: "#FFF1D9", stroke: "#C3AD8E", radius: 7 });
    assetRect(row, "Icon", 14, 14, 54, 54, item[1], 5, "FIT");
    addText(row, "Name", item[0], 82, 16, 140, 20, { font: "heading", size: 16, fill: palette.ink });
    addText(row, "Time", item[2], 82, 42, 80, 16, { font: "bodyMedium", size: 11, fill: palette.teal });
    progress(row, "Track", "Fill", 188, 48, 80, 8, index === 1 ? 0.72 : 0.36, palette.green);
  });

  const center = panel(root, "Recipes", 394, 134, 1014, 804);
  sectionTitle(center, "RECIPE BROWSER", 22, 20);
  const recipes = [
    ["Rusty Revolver", "Weapon", "ui_icon_equipment_pistol", "25 Scrap, 15 Parts"],
    ["Hunting Rifle", "Weapon", "ui_icon_equipment_rifle", "40 Scrap, 35 Parts"],
    ["Machete", "Melee", "ui_icon_equipment_machete", "18 Scrap, 8 Parts"],
    ["Armor Vest", "Armor", "ui_icon_equipment_armor_vest", "30 Scrap, 22 Parts"],
    ["Scout Pack", "Gear", "ui_icon_equipment_backpack", "12 Scrap, 6 Parts"],
    ["Field Medkit", "Supplies", "ui_icon_equipment_medkit", "8 Medicine, 5 Scrap"]
  ];
  recipes.forEach((recipe, index) => {
    const row = frame(center, `Recipe / ${recipe[0]}`, 22 + (index % 2) * 492, 70 + Math.floor(index / 2) * 148, 470, 118, { fill: "#FFF1D9", stroke: "#C3AD8E", radius: 8 });
    assetRect(row, "Icon", 18, 20, 64, 64, recipe[2], 5, "FIT");
    addText(row, "Name", recipe[0], 98, 20, 180, 22, { font: "heading", size: 18, fill: palette.ink });
    addText(row, "Type", recipe[1], 98, 46, 120, 16, { font: "bodyMedium", size: 11, fill: palette.teal });
    addText(row, "Cost", recipe[3], 98, 72, 190, 16, { font: "body", size: 11, fill: palette.muted });
    const craft = instance("Ashfall/Common/Action Button", row, 296, 36);
    setButton(craft, "CRAFT", ">");
    craft.resize(150, 40);
  });
  sectionTitle(center, "RESOURCE CHECK", 22, 556);
  resources.forEach((resource, index) => {
    const pill = instance("Ashfall/Common/Resource Pill", center, 22 + index * 158, 604);
    setResourcePill(pill, resource);
  });

  const right = panel(root, "Selected Recipe", 1430, 134, 454, 804);
  sectionTitle(right, "SELECTED: HUNTING RIFLE", 22, 20);
  assetRect(right, "Weapon Icon", 24, 74, 154, 154, "ui_icon_equipment_rifle", 8, "FIT");
  addText(right, "Role", "Weapon / Firearms", 204, 84, 140, 18, { font: "bodyMedium", size: 12, fill: palette.teal });
  addText(right, "Desc", "A reliable long-range weapon for Rhea or Elias. Uses Weapon Parts and Scrap from scavenging missions.", 204, 112, 188, 66, { font: "body", size: 12, fill: palette.ink });
  addMetric(right, "Damage", "+6", 24, 274, "ui_icon_stat_damage");
  addMetric(right, "Accuracy", "+12%", 24, 320, "ui_icon_status_scouting");
  addMetric(right, "Cost", "40 Scrap, 35 Parts", 24, 386, "ui_icon_resource_parts");
  const craft = instance("Ashfall/Common/Action Button", right, 24, 478);
  setButton(craft, "CRAFT", ">");
  return frameNode;
}

function rebuildRadio() {
  const frameNode = fullHdFrame("Ashfall Camp - Full HD / 06 Radio");
  clearFrameContent(frameNode);
  const root = contentRoot(frameNode, "Ashfall Asset-Driven / Radio");
  addScreenTitle(root, "RADIO", "Recruitment cards use real recruitable SurvivorCatalog entries");

  const left = panel(root, "Signal", 36, 134, 336, 804);
  sectionTitle(left, "SIGNAL", 22, 20);
  addMetric(left, "Radio Tower", "Level 2", 24, 72, "ui_icon_equipment_radio");
  addMetric(left, "Intel Range", "Medium", 24, 118, "ui_icon_resource_radio_intel");
  addMetric(left, "Daily Contacts", "3", 24, 164, "ui_icon_status_scouting");
  progress(left, "Signal Track", "Signal Fill", 24, 244, 286, 16, 0.64, palette.teal);
  addText(left, "Hint", "Better tower condition expands candidate quality and zone intel.", 24, 292, 260, 48, { font: "body", size: 12, fill: palette.ink });

  const center = panel(root, "Broadcast", 394, 134, 1014, 804);
  sectionTitle(center, "AVAILABLE RECRUITS", 22, 20);
  survivors.slice(1).forEach((survivor, index) => {
    const card = instance("Ashfall/Survivor/Roster Card", center, 22 + (index % 3) * 318, 72 + Math.floor(index / 3) * 118);
    setRosterCard(card, survivor, false);
  });
  sectionTitle(center, "BROADCAST OPTIONS", 22, 354);
  [
    ["General Call", "Balanced chance across all backgrounds.", "ui_icon_equipment_radio"],
    ["Medic Request", "Higher chance for Medicine skill.", "ui_icon_equipment_medkit"],
    ["Guard Request", "Higher chance for Firearms or Melee.", "ui_icon_equipment_rifle"]
  ].forEach((row, index) => {
    const box = frame(center, `Broadcast / ${row[0]}`, 22, 402 + index * 98, 926, 74, { fill: "#FFF1D9", stroke: "#C3AD8E", radius: 7 });
    assetRect(box, "Icon", 16, 14, 46, 46, row[2], 5, "FIT");
    addText(box, "Name", row[0], 78, 16, 160, 20, { font: "heading", size: 17, fill: palette.ink });
    addText(box, "Body", row[1], 78, 40, 370, 16, { font: "body", size: 11, fill: palette.muted });
    const action = instance("Ashfall/Common/Action Button", box, 720, 14);
    setButton(action, "BROADCAST", ">");
  });

  const right = panel(root, "Recruit", 1430, 134, 454, 804);
  sectionTitle(right, "SELECTED: RHEA", 22, 20);
  assetRect(right, "Rhea", 24, 72, 158, 198, "ui_character_battle_survivor_07", 8);
  addText(right, "Name", "Rhea", 206, 82, 110, 30, { font: "title", size: 34, fill: palette.ink });
  addText(right, "Role", "Hunter / Quiet", 208, 118, 150, 18, { font: "heading", size: 14, fill: palette.teal });
  addMetric(right, "Firearms", "3", 24, 312);
  addMetric(right, "Survival", "3", 24, 358);
  addMetric(right, "Weapon", "Hunting Rifle", 24, 404, "ui_icon_equipment_rifle");
  const recruit = instance("Ashfall/Common/Action Button", right, 24, 500);
  setButton(recruit, "RECRUIT", ">");
  return frameNode;
}

function rebuildCombatMonitor() {
  const source = page.findOne(node => node.type === "FRAME" && node.name === "Ashfall Camp - Full HD / 03 Expeditions")
    || page.findOne(node => node.type === "FRAME" && node.name === "Ashfall Camp - Full HD / 01 Camp Dashboard");
  let frameNode = page.findOne(node => node.type === "FRAME" && node.name === "Ashfall Camp - Full HD / 07 Expedition Monitor");
  if (!frameNode) {
    frameNode = source.clone();
    frameNode.name = "Ashfall Camp - Full HD / 07 Expedition Monitor";
    frameNode.x = 2102;
    frameNode.y = 2396;
    page.appendChild(frameNode);
  }
  clearFrameContent(frameNode);
  const root = contentRoot(frameNode, "Ashfall Asset-Driven / Expedition Monitor");
  addScreenTitle(root, "EXPEDITION MONITOR", "Mission: Scavenge Run / Normal / Day 7");

  const header = frame(root, "Mission Header", 28, 118, 560, 216, { fill: "#FFF1D9", stroke: "#B9A586", radius: 8 });
  assetRect(header, "Panel Asset", 0, 0, 560, 216, "ui_panel_mission_header_empty", 8);
  addText(header, "Title", "SCAVENGE RUN", 24, 22, 220, 30, { font: "heading", size: 26, fill: palette.ink });
  addText(header, "Subtitle", "Dry Suburb - Ambush Site", 24, 56, 260, 18, { font: "bodyMedium", size: 12, fill: palette.teal });
  sectionTitle(header, "SQUAD 4 / 4", 24, 94);
  survivors.slice(0, 4).forEach((survivor, index) => addPortraitChip(header, survivor, 26 + index * 128, 128, 104));

  const encounter = frame(root, "Encounter Panel", 608, 118, 464, 216, { fill: "#FFF1D9", stroke: "#B9A586", radius: 8 });
  addText(encounter, "Encounter", "ENCOUNTER", 22, 18, 116, 20, { font: "heading", size: 17, fill: palette.teal });
  addText(encounter, "Name", "AMBUSH SITE", 138, 18, 126, 20, { font: "heading", size: 17, fill: palette.ink });
  assetRect(encounter, "Encounter Art", 22, 50, 420, 112, "ui_backplate_clean_expedition_setup_01", 7);
  addCombatStat(encounter, "Enemies", "4", 22, 174);
  addCombatStat(encounter, "Round Limit", "∞", 150, 174);
  addCombatStat(encounter, "Victory", "Defeat all enemies", 278, 174, 144);

  const status = frame(root, "Time Status", 1092, 118, 220, 216, { fill: "#FFF1D9", stroke: "#B9A586", radius: 8 });
  assetRect(status, "Panel Asset", 0, 0, 220, 216, "ui_panel_time_status_empty", 8);
  sectionTitle(status, "TIME & STATUS", 20, 18);
  addMetric(status, "Time", "10:30", 20, 62);
  progress(status, "Light Track", "Light Fill", 96, 76, 84, 8, 0.54, palette.teal);
  addMetric(status, "Threat", "Medium", 20, 112, "ui_icon_status_danger");
  addMetric(status, "Noise", "Low", 20, 158, "ui_icon_status_scouting");

  const zone = frame(root, "Zone Minimap", 1330, 118, 342, 216, { fill: "#FFF1D9", stroke: "#B9A586", radius: 8 });
  assetRect(zone, "Panel Asset", 0, 0, 342, 216, "ui_panel_zone_minimap_empty", 8);
  sectionTitle(zone, "ZONE OVERVIEW", 18, 16);
  assetRect(zone, "Minimap", 18, 48, 306, 110, "ui_map_zone_minimap_local", 6);
  addText(zone, "Distance", "DISTANCE LEFT  5", 22, 174, 130, 18, { font: "heading", size: 13, fill: palette.ink });
  addText(zone, "Weather", "CLOUDY", 210, 174, 84, 18, { font: "heading", size: 13, fill: palette.ink, align: "RIGHT" });

  const log = frame(root, "Combat Log", 1690, 118, 202, 216, { fill: "#FFF1D9", stroke: "#B9A586", radius: 8 });
  assetRect(log, "Panel Asset", 0, 0, 202, 216, "ui_panel_combat_log_empty", 8);
  sectionTitle(log, "COMBAT LOG", 16, 16);
  ["Round 1 begins", "Mara gains +1 Stealth", "Raider prepares", "Feral Dog closes in"].forEach((lineText, index) => {
    rect(log, `Dot ${index}`, 18, 54 + index * 28, 8, 8, index === 2 ? palette.red : palette.teal, 8);
    addText(log, `Line ${index}`, lineText, 32, 48 + index * 28, 140, 18, { font: "body", size: 10, fill: palette.ink });
  });

  const phase = frame(root, "Combat Phase Bar", 28, 360, 1864, 70, { fill: "#D5C1A0", stroke: "#8D795A", radius: 8 });
  assetRect(phase, "Phase Asset", 0, 0, 1864, 70, "ui_bar_combat_phase_empty", 8);
  addText(phase, "Label", "COMBAT PHASE", 22, 20, 160, 30, { font: "heading", size: 25, fill: "#FFF1D9" });
  rect(phase, "Player Turn", 270, 24, 660, 22, palette.green, 4);
  addText(phase, "Player Turn Text", "PLAYER TURN", 520, 26, 160, 18, { font: "heading", size: 14, fill: "#FFF1D9", align: "CENTER" });
  rect(phase, "Clash", 954, 24, 190, 22, palette.gold, 4);
  addText(phase, "Clash Text", "CLASH", 1014, 26, 70, 18, { font: "heading", size: 14, fill: palette.ink, align: "CENTER" });
  rect(phase, "Enemy Turn", 1168, 24, 560, 22, palette.red, 4);
  addText(phase, "Enemy Turn Text", "ENEMY TURN", 1368, 26, 120, 18, { font: "heading", size: 14, fill: "#FFF1D9", align: "CENTER" });

  const battlefield = frame(root, "Battlefield", 28, 446, 1864, 360, { fill: "#C8B393", stroke: "#8D795A", radius: 8 });
  assetRect(battlefield, "Player Lane", 12, 20, 768, 312, "ui_lane_battlefield_player_empty", 6);
  assetRect(battlefield, "Enemy Lane", 1084, 20, 768, 312, "ui_lane_battlefield_enemy_empty", 6);
  addText(battlefield, "VS", "VS", 912, 124, 40, 24, { font: "heading", size: 18, fill: palette.lineDark, align: "CENTER" });
  assetRect(battlefield, "Clash Emblem", 854, 126, 154, 154, "ui_emblem_battlefield_clash", 0, "FIT", 0.42);

  const combatSurvivors = [survivors[0], survivors[1], survivors[2], survivors[3]];
  combatSurvivors.forEach((survivor, index) => {
    const card = instance("Ashfall/Combat/Player Battle Card", battlefield, 24 + index * 190, 38);
    setBattleCard(card, survivor, false, index + 1);
    card.name = `Player Battle Card / ${survivor.name}`;
  });
  const combatEnemies = [enemies[0], enemies[1], enemies[3], enemies[2]];
  combatEnemies.forEach((enemy, index) => {
    const card = instance("Ashfall/Combat/Enemy Battle Card", battlefield, 1108 + index * 190, 38);
    setBattleCard(card, enemy, true, index + 1);
    card.name = `Enemy Battle Card / ${enemy.name}`;
  });

  const bottom = frame(root, "Action Command Bar", 28, 824, 1864, 126, { fill: "#FFF1D9", stroke: "#B9A586", radius: 8 });
  assetRect(bottom, "Action Asset", 0, 0, 1864, 126, "ui_bar_actions_command_empty", 8);
  assetRect(bottom, "Energy Icon", 26, 25, 72, 72, "ui_icon_energy_lightning", 36, "FIT");
  addText(bottom, "Energy", "4 / 5", 112, 40, 82, 30, { font: "heading", size: 26, fill: palette.ink });
  sectionTitle(bottom, "ABILITIES", 260, 22);
  ["Aim", "Guard", "Treat", "Move", "Item"].forEach((ability, index) => {
    frameIcon(bottom, ["ui_card_icon_stat_damage", "ui_card_icon_stat_armor", "ui_icon_equipment_medkit", "ui_card_icon_stat_speed", "ui_icon_equipment_backpack"][index], 260 + index * 82, 56, 52);
    addText(bottom, `Ability ${index}`, ability, 248 + index * 82, 108, 76, 12, { font: "bodyMedium", size: 9, fill: palette.ink, align: "CENTER" });
  });
  addText(bottom, "Phase Info", "Use abilities or move to prepare for the clash. Four enemy cards are fully visible on the right lane.", 720, 34, 372, 42, { font: "body", size: 12, fill: palette.ink });
  const retreat = instance("Ashfall/Common/Action Button", bottom, 1140, 40);
  setButton(retreat, "RETREAT", "<");
  tintButton(retreat, palette.teal);
  const useItem = instance("Ashfall/Common/Action Button", bottom, 1348, 40);
  setButton(useItem, "USE ITEM", "+");
  tintButton(useItem, palette.green);
  const endTurn = instance("Ashfall/Common/Action Button", bottom, 1556, 40);
  setButton(endTurn, "END TURN", ">");

  return frameNode;
}

function fullHdFrame(name) {
  const frameNode = page.findOne(node => node.type === "FRAME" && node.name === name);
  if (!frameNode) throw new Error(`Missing Full HD frame: ${name}`);
  return frameNode;
}

function clearFrameContent(frameNode) {
  for (const child of [...frameNode.children]) {
    if (oldContentNames.has(child.name) || child.name.startsWith("Ashfall Asset-Driven /")) child.remove();
  }
}

function contentRoot(parent, name) {
  const root = figma.createFrame();
  root.name = name;
  root.x = 0;
  root.y = 0;
  root.resize(1920, 1080);
  root.fills = [];
  root.clipsContent = false;
  parent.appendChild(root);
  return root;
}

function addScreenTitle(parent, title, subtitle) {
  addText(parent, `Screen Title / ${title}`, title, 506, 28, 520, 58, { font: "title", size: 58, fill: palette.ink, align: "CENTER" });
  addText(parent, `Screen Subtitle / ${title}`, subtitle, 526, 88, 480, 20, { font: "heading", size: 15, fill: palette.teal, align: "CENTER" });
}

function addDetailStats(parent, survivor) {
  const stats = frame(parent, "Stats Block", 24, 396, 256, 190, { fill: "#000000", radius: 0, opacity: 0 });
  stats.clipsContent = false;
  addMetric(stats, "Health", `${survivor.hp} / ${survivor.maxHp}`, 0, 0, "ui_icon_status_healthy");
  addMetric(stats, "Fatigue", `${survivor.fatigue} / 100`, 0, 46, "ui_icon_status_fatigue");
  addMetric(stats, "Morale", `${survivor.morale} / 100`, 0, 92, "ui_icon_status_morale");
  addMetric(stats, "Trait", survivor.trait, 0, 138, "ui_icon_status_level_up");
}

function setRosterCard(card, survivor, selected, compact = false) {
  card.name = `Roster Card / ${survivor.name}`;
  setText(card, "Name", survivor.name);
  setText(card, "Role", survivor.role);
  setText(card, "Status", survivor.status);
  setText(card, "HP", `${survivor.hp} / ${survivor.maxHp}`);
  setImage(card, "Portrait", survivor.portrait);
  setProgress(card, "HP Fill", "HP Track", survivor.hp / survivor.maxHp);
  const accent = card.findOne(node => node.name === "Selected Accent");
  if (accent && "fills" in accent) accent.fills = [solid(selected ? palette.teal2 : "#D2BE9F", selected ? 1 : 0.36)];
  if (compact) {
    const portrait = card.findOne(node => node.name === "Portrait");
    if (portrait && "resize" in portrait) portrait.resize(44, 44);
  }
}

function setSkillRow(row, skillName, value, description) {
  row.name = `Skill Row / ${skillName}`;
  setText(row, "Icon", skillName.slice(0, 1).toUpperCase());
  setText(row, "Label", skillName);
  setText(row, "Description", description);
  setText(row, "Value", String(value));
  setProgress(row, "Skill Fill", "Skill Track", Math.min(1, value / 5));
}

function setResourcePill(pill, resource) {
  pill.name = `Resource Pill / ${resource.name}`;
  setImage(pill, "Icon", resource.icon);
  setText(pill, "Value", String(resource.value).replace(/\B(?=(\d{3})+(?!\d))/g, ","));
  setText(pill, "Label", resource.name);
}

function setButton(button, label, icon) {
  button.name = `Action Button / ${label}`;
  setText(button, "Label", label);
  setText(button, "Icon", icon);
}

function tintButton(button, color) {
  if ("fills" in button) button.fills = [solid(color)];
}

function setBuildingCard(card, building) {
  card.name = `Building Card / ${building.name}`;
  setImage(card, "Icon", building.icon);
  setText(card, "Name", building.name);
  setText(card, "Level", `Level ${building.level}`);
  setText(card, "Workers", building.workers);
  setText(card, "Effect 1", building.effects[0]);
  setText(card, "Effect 2", building.effects[1]);
  setText(card, "Condition Value", `${building.condition}%`);
  setText(card, "Cost", Object.entries(building.cost).map(([key, value]) => `${value} ${key}`).join("   "));
  setProgress(card, "Condition Fill", "Condition Track", building.condition / 100);
}

function setBattleCard(card, actor, isEnemy, index) {
  setText(card, "Name", actor.name);
  setText(card, "Level", isEnemy ? `XP ${actor.xp}` : `LVL ${actor.level}`);
  setText(card, "Role", isEnemy ? actor.type.toUpperCase() : actor.role.toUpperCase());
  setText(card, "HP", isEnemy ? `${actor.hp}/${actor.hp}` : `${actor.hp}/${actor.maxHp}`);
  setText(card, "Tag", isEnemy ? actor.tag : actor.trait.toUpperCase());
  setText(card, "Damage", String(isEnemy ? actor.damage : 28 + index * 6));
  setText(card, "Armor", String(isEnemy ? actor.armor : 8 + index * 2));
  setText(card, "Speed", String(isEnemy ? actor.evasion : 10 + index));
  setImage(card, "Portrait", actor.portrait);
  setProgress(card, "HP Fill", "HP Track", 1);
  const fill = card.findOne(node => node.name === "HP Fill");
  if (fill && "fills" in fill) fill.fills = [solid(isEnemy ? palette.red : palette.green)];
}

function skillDescription(skillName) {
  const map = {
    Scavenging: "Find more resources.",
    Melee: "Close combat control.",
    Firearms: "Accuracy with guns.",
    Survival: "Travel and scouting.",
    Mechanics: "Repair and crafting.",
    Medicine: "Heal wounds and illness."
  };
  return map[skillName] || "Catalog skill.";
}

function addMetric(parent, label, value, x, y, icon) {
  if (icon) assetRect(parent, `${label} Icon`, x, y + 2, 22, 22, icon, 3, "FIT");
  addText(parent, `${label} Label`, label, icon ? x + 34 : x, y + 2, 154, 18, { font: "bodyMedium", size: 12, fill: palette.ink });
  addText(parent, `${label} Value`, value, x + 188, y, Math.max(84, parent.width - x - 214), 20, { font: "heading", size: 15, fill: palette.ink, align: "RIGHT" });
}

function addChecklist(parent, label, x, y, active) {
  rect(parent, `${label} Check`, x, y + 2, 18, 18, active ? palette.green : "#D1BE9C", 18);
  addText(parent, `${label} Text`, label, x + 28, y, parent.width - x - 40, 20, { font: "body", size: 12, fill: palette.ink });
}

function addPortraitChip(parent, survivor, x, y, size) {
  const chip = frame(parent, `Portrait Chip / ${survivor.name}`, x, y, size, size + 34, { fill: "#FFF1D9", stroke: "#B9A586", radius: 8 });
  assetRect(chip, "Portrait", 8, 8, size - 16, size - 34, survivor.portrait, 6);
  addText(chip, "Name", survivor.name, 8, size - 18, size - 16, 16, { font: "heading", size: 12, fill: palette.ink, align: "CENTER" });
  return chip;
}

function compactBuildingRow(parent, x, y, building) {
  const row = frame(parent, `Building Summary / ${building.name}`, x, y, 298, 118, { fill: "#FFF1D9", stroke: "#C3AD8E", radius: 7 });
  assetRect(row, "Icon", 18, 18, 52, 52, building.icon, 5, "FIT");
  addText(row, "Name", building.name, 84, 18, 150, 20, { font: "heading", size: 17, fill: palette.ink });
  addText(row, "Level", `Level ${building.level}`, 84, 42, 80, 16, { font: "bodyMedium", size: 11, fill: palette.teal });
  progress(row, "Track", "Fill", 84, 76, 160, 8, building.condition / 100, palette.green);
  return row;
}

function addEquipmentLine(parent, asset, value, label, x, y) {
  const row = frame(parent, `Equipment / ${label}`, x, y, 294, 48, { fill: "#FFF1D9", stroke: "#C3AD8E", radius: 6 });
  assetRect(row, "Icon", 10, 8, 32, 32, asset, 3, "FIT");
  addText(row, "Label", label, 54, 7, 80, 14, { font: "bodyMedium", size: 10, fill: palette.teal });
  addText(row, "Value", value, 54, 23, 160, 18, { font: "heading", size: 14, fill: palette.ink });
  return row;
}

function addCombatStat(parent, label, value, x, y, w = 112) {
  const box = frame(parent, `Combat Stat / ${label}`, x, y, w, 30, { fill: "#F8E9D0", stroke: "#C3AD8E", radius: 5 });
  addText(box, "Label", label.toUpperCase(), 10, 8, 56, 14, { font: "bodyMedium", size: 9, fill: palette.ink });
  addText(box, "Value", value, 68, 5, w - 78, 18, { font: "heading", size: 14, fill: palette.ink, align: "RIGHT" });
}

function frameIcon(parent, asset, x, y, size) {
  const box = frame(parent, `Icon / ${asset}`, x, y, size, size, { fill: "#E7D5B8", stroke: "#B9A586", radius: 8 });
  assetRect(box, "Icon", 8, 8, size - 16, size - 16, asset, 4, "FIT");
  return box;
}

function addSegmented(parent, labels, x, y, w, h, selectedIndex) {
  const group = frame(parent, "Segmented Control", x, y, w, h, { fill: "#F8EBD5", stroke: "#C3AD8E", radius: 6 });
  const partW = w / labels.length;
  labels.forEach((label, index) => {
    const selected = index === selectedIndex;
    rect(group, `Segment / ${label}`, index * partW, 0, partW, h, selected ? palette.teal2 : "#FFF1D9", 5, selected ? 1 : 0.42);
    addText(group, `Label / ${label}`, label, index * partW, 10, partW, 18, { font: "heading", size: 12, fill: selected ? "#FFF1D9" : palette.ink, align: "CENTER" });
  });
}

function panel(parent, name, x, y, w, h) {
  return frame(parent, name, x, y, w, h, { fill: "#F6E8D0", stroke: "#B9A586", radius: 10 });
}

function sectionTitle(parent, textValue, x, y) {
  addText(parent, `Section / ${textValue}`, textValue, x, y, parent.width - x * 2, 24, { font: "heading", size: 18, fill: palette.teal });
}

function component(parent, name, x, y, w, h) {
  const c = figma.createComponent();
  c.name = name;
  c.x = x;
  c.y = y;
  c.resize(w, h);
  parent.appendChild(c);
  return c;
}

function instance(name, parent, x, y) {
  const c = createdComponents[name] || page.findOne(node => node.type === "COMPONENT" && node.name === name);
  if (!c) throw new Error(`Missing component: ${name}`);
  const node = c.createInstance();
  node.x = x;
  node.y = y;
  parent.appendChild(node);
  return node;
}

function frame(parent, name, x, y, w, h, options = {}) {
  const node = figma.createFrame();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.fills = options.fill ? [solid(options.fill, options.opacity ?? 1)] : [];
  if (options.stroke) node.strokes = [solid(options.stroke)];
  if (options.dash) node.dashPattern = options.dash;
  node.strokeWeight = options.stroke ? 1 : 0;
  node.cornerRadius = options.radius ?? 0;
  node.clipsContent = true;
  parent.appendChild(node);
  return node;
}

function rect(parent, name, x, y, w, h, fill, radius = 0, opacity = 1) {
  const node = figma.createRectangle();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.fills = [solid(fill, opacity)];
  node.cornerRadius = radius;
  parent.appendChild(node);
  return node;
}

function assetRect(parent, name, x, y, w, h, asset, radius = 0, scaleMode = "FILL", opacity = 1) {
  const node = figma.createRectangle();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.fills = imageFills(asset, scaleMode, opacity);
  node.cornerRadius = radius;
  parent.appendChild(node);
  return node;
}

function progress(parent, trackName, fillName, x, y, w, h, value, color) {
  rect(parent, trackName, x, y, w, h, "#D2C1A3", h / 2);
  rect(parent, fillName, x, y, Math.max(2, w * value), h, color, h / 2);
}

function setProgress(parent, fillName, trackName, value) {
  const fill = parent.findOne(node => node.name === fillName);
  const track = parent.findOne(node => node.name === trackName);
  if (!fill || !track || !("resize" in fill) || !("width" in track)) return;
  fill.resize(Math.max(2, track.width * Math.max(0, Math.min(1, value))), fill.height);
}

function line(parent, x, y, w) {
  rect(parent, "Divider", x, y, w, 1, palette.line, 0, 0.72);
}

function addText(parent, name, characters, x, y, w, h, options = {}) {
  const node = figma.createText();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.fontName = fonts[options.font || "body"];
  node.fontSize = options.size || 12;
  node.fills = [solid(options.fill || palette.ink, options.opacity ?? 1)];
  node.textAlignHorizontal = options.align || "LEFT";
  node.textAlignVertical = options.valign || "TOP";
  node.textAutoResize = "HEIGHT";
  node.characters = String(characters);
  parent.appendChild(node);
  return node;
}

function setText(parent, name, value) {
  const node = parent.findOne(candidate => candidate.type === "TEXT" && candidate.name === name);
  if (!node) return false;
  node.characters = String(value);
  return true;
}

function setImage(parent, name, asset) {
  const node = parent.findOne(candidate => candidate.name === name && "fills" in candidate);
  if (!node) return false;
  node.fills = imageFills(asset, "FILL");
  return true;
}

function imageFills(asset, scaleMode = "FILL", opacity = 1) {
  const hash = imageHash(asset);
  if (!hash) {
    missingAssets.add(asset);
    return [solid("#D9C7A7", opacity)];
  }
  return [{ type: "IMAGE", imageHash: hash, scaleMode, opacity }];
}

function imageHash(name) {
  const node = page.findOne(candidate => candidate.name === name && "fills" in candidate && Array.isArray(candidate.fills) && candidate.fills.some(fill => fill.type === "IMAGE"));
  if (!node || !Array.isArray(node.fills)) return null;
  const image = node.fills.find(fill => fill.type === "IMAGE" && fill.imageHash);
  return image ? image.imageHash : null;
}

function solid(hex, opacity = 1) {
  return { type: "SOLID", color: helpers.rgb(hex), opacity };
}

function auditFile(frames) {
  const textNodes = page.findAll(node => node.type === "TEXT");
  const forbidden = ["Feral Ghoul", "Mutant Hound", "Mara Voss", "Tom Ren", "Eli Gray", "Juno Park"];
  const forbiddenHits = [];
  for (const text of textNodes) {
    for (const word of forbidden) {
      if (text.characters && text.characters.includes(word)) forbiddenHits.push({ word, node: text.name });
    }
  }
  const combat = frames.find(frame => frame.name.endsWith("07 Expedition Monitor"));
  const enemyCards = combat ? combat.findAll(node => node.name.startsWith("Enemy Battle Card /")).map(node => ({
    name: node.name,
    x: Math.round(node.absoluteTransform[0][2] - combat.absoluteTransform[0][2]),
    y: Math.round(node.absoluteTransform[1][2] - combat.absoluteTransform[1][2]),
    width: Math.round(node.width),
    height: Math.round(node.height)
  })) : [];
  const assetlessImageNodes = page.findAll(node => "fills" in node && Array.isArray(node.fills) && node.fills.some(fill => fill.type === "IMAGE" && !fill.imageHash)).map(node => node.name);
  const componentInstances = frames.flatMap(frameNode => frameNode.findAll(node => node.type === "INSTANCE").map(node => node.name));
  return {
    fullHdFrameCount: page.children.filter(node => node.type === "FRAME" && node.name.startsWith("Ashfall Camp - Full HD /")).length,
    enemyCardCount: enemyCards.length,
    enemyCards,
    forbiddenHits,
    assetlessImageNodes,
    instanceCount: componentInstances.length,
    navBaseComponents: page.findAll(node => node.name === "Ashfall/Navigation/Bottom Nav Base").length,
    navCurrentTabComponents: page.findAll(node => node.name === "Ashfall/Navigation/Current Tab").length
  };
}
