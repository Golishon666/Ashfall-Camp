const VERSION = "ashfall-full-hd-ui-2026-07-05";

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

const palette = {
  ink: "#2B2E33",
  muted: "#6B6254",
  teal: "#1F4E59",
  teal2: "#2F6572",
  paper: "#F5EAD6",
  paper2: "#FFF3DE",
  parchment: "#F3E8D3",
  line: "#B8A68A",
  lineDark: "#7D6A52",
  green: "#4F623A",
  sage: "#7B8A6B",
  orange: "#C96A3A",
  gold: "#B38132",
  red: "#B55A5A",
  metal: "#2E2C29",
  dark: "#101010"
};

const L = { x: 32, y: 134, w: 326, h: 820 };
const C = { x: 386, y: 134, w: 1040, h: 820 };
const R = { x: 1454, y: 134, w: 398, h: 820 };

const payloadImages = Array.isArray(payload && payload.images) ? payload.images : [];
if (payload && payload.mode === "import-only") {
  return await importImages(payloadImages);
}

const page = figma.currentPage;
const master = page.findOne(node => node.type === "FRAME" && node.name === "Ashfall Camp - Camp Dashboard");
if (!master) {
  throw new Error("Could not find master frame: Ashfall Camp - Camp Dashboard");
}

for (const node of [...page.children]) {
  if (node.name.startsWith("Ashfall Camp - Full HD /")) {
    node.remove();
  }
}

const maxX = Math.max(...page.children.filter(node => "x" in node && "width" in node).map(node => node.x + node.width));
const startX = Math.ceil(maxX + 180);
const startY = Math.floor(master.y);

const screens = [
  { key: "CAMP", name: "01 Camp Dashboard", builder: null, backdrop: "ui_backplate_clean_camp_dashboard_01" },
  { key: "SURVIVORS", name: "02 Survivors", builder: buildSurvivors, backdrop: "ui_bg_survivors_roster_01" },
  { key: "EXPEDITIONS", name: "03 Expeditions", builder: buildExpeditions, backdrop: "ui_backplate_clean_world_map_01" },
  { key: "BUILDINGS", name: "04 Buildings", builder: buildBuildings, backdrop: "ui_backplate_clean_buildings_01" },
  { key: "WORKSHOP", name: "05 Workshop", builder: buildWorkshop, backdrop: "ui_backplate_clean_workshop_01" },
  { key: "RADIO", name: "06 Radio", builder: buildRadio, backdrop: "ui_backplate_clean_radio_recruitment_01" }
];

const created = [];
for (let i = 0; i < screens.length; i += 1) {
  const spec = screens[i];
  const frame = master.clone();
  frame.name = `Ashfall Camp - Full HD / ${spec.name}`;
  frame.x = startX + (i % 3) * 2040;
  frame.y = startY + Math.floor(i / 3) * 1200;
  updateNav(frame, spec.key);

  const backdropHash = imageHash(spec.backdrop);
  if (backdropHash) {
    addMoodBackdrop(frame, backdropHash);
  }

  if (spec.builder) {
    clearDashboardContent(frame);
    spec.builder(frame);
  }

  created.push(frame);
}

figma.currentPage.selection = created;
figma.viewport.scrollAndZoomIntoView(created);

return {
  version: VERSION,
  created: created.map(node => ({
    id: node.id,
    name: node.name,
    x: node.x,
    y: node.y,
    width: node.width,
    height: node.height,
    children: node.children.length
  })),
  importedAssetCount: page.children.filter(node => node.name.startsWith("ui_")).length
};

async function importImages(images) {
  const targetPage = figma.currentPage;
  const imported = [];
  for (let i = 0; i < images.length; i += 1) {
    const item = images[i];
    if (!item || !item.name || !item.base64) continue;
    const bytes = base64ToBytes(item.base64);
    const hash = figma.createImage(bytes).hash;
    let node = targetPage.findOne(candidate => candidate.name === item.name);
    if (!node || !("fills" in node) || !("resize" in node)) {
      node = figma.createFrame();
      targetPage.appendChild(node);
    }
    node.name = item.name;
    node.x = item.x ?? (-26 + (i % 6) * 224);
    node.y = item.y ?? (1730 + Math.floor(i / 6) * 188);
    node.resize(item.width || 200, item.height || 150);
    node.fills = [{ type: "IMAGE", imageHash: hash, scaleMode: "FILL" }];
    if ("clipsContent" in node) node.clipsContent = true;
    imported.push({ name: item.name, hash, id: node.id });
  }
  return { version: VERSION, imported };
}

function base64ToBytes(base64) {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}

function clearDashboardContent(frame) {
  const names = new Set(["Left status column", "Camp overview panel", "Right expedition column"]);
  for (const child of [...frame.children]) {
    if (names.has(child.name)) child.remove();
  }
}

function updateNav(frame, activeKey) {
  const nav = frame.findOne(node => node.name === "Bottom navigation");
  if (!nav || !("children" in nav)) return;
  for (const item of nav.children) {
    if (!item.name.startsWith("Nav / ")) continue;
    const key = item.name.replace("Nav / ", "");
    const active = key === activeKey;
    item.fills = [paint(active ? palette.teal2 : "#F7ECD9")];
    item.strokes = [paint(active ? palette.teal : palette.line, active ? 0.72 : 0.48)];
    item.strokeWeight = active ? 2 : 1;
    const label = item.findOne(node => node.type === "TEXT" && node.name === "label");
    if (label) label.fills = [paint(active ? "#F6EAD0" : palette.ink)];
  }
}

function addMoodBackdrop(frame, hash) {
  const backdrop = figma.createRectangle();
  backdrop.name = "Screen mood backdrop";
  backdrop.x = 0;
  backdrop.y = 0;
  backdrop.resize(1920, 1080);
  backdrop.fills = [{ type: "IMAGE", imageHash: hash, scaleMode: "FILL", opacity: 0.16 }];
  frame.appendChild(backdrop);
  frame.insertChild(1, backdrop);
}

function buildSurvivors(root) {
  const roster = panel(root, "Survivors roster panel", L.x, L.y, L.w, L.h, "SURVIVOR ROSTER");
  segmented(roster, 22, 66, ["ALL", "IDLE", "WOUNDED"], 1);
  const people = [
    ["MARA VOSS", "Scout", "Idle", 0.88, imageHash("ui_character_battle_survivor_01")],
    ["TOM REN", "Builder", "Assigned", 0.72, imageHash("ui_character_battle_survivor_02")],
    ["ELI GRAY", "Medic", "Healing", 0.54, imageHash("ui_character_battle_survivor_01")],
    ["NIKA VALE", "Guard", "Ready", 0.81, imageHash("ui_character_battle_survivor_02")],
    ["JUNO PARK", "Cook", "Resting", 0.63, imageHash("ui_character_battle_survivor_01")]
  ];
  people.forEach((person, index) => survivorRow(roster, 22, 126 + index * 104, 282, person, index === 0));
  button(roster, "MANAGE ALL", 22, 736, 282, 44, "secondary");

  const details = panel(root, "Selected survivor panel", C.x, C.y, C.w, C.h, "SELECTED SURVIVOR");
  imageBox(details, "Mara portrait", 26, 70, 340, 430, imageHash("ui_character_battle_survivor_01"), { radius: 8 });
  labelStrip(details, "MARA VOSS", "LEVEL 8 SCOUT  /  12 DAYS IN CAMP", 396, 70, 612, 82);
  chip(details, "RESILIENT", 416, 170, 122, palette.green);
  chip(details, "NIGHT EYES", 550, 170, 126, palette.teal2);
  chip(details, "LIGHT SLEEPER", 688, 170, 150, palette.gold);
  section(details, "CORE STATS", 396, 228, 286, 250);
  stat(details, "Scouting", "92", 420, 280, 226, 0.92, palette.teal2);
  stat(details, "Combat", "68", 420, 330, 226, 0.68, palette.orange);
  stat(details, "Medicine", "41", 420, 380, 226, 0.41, palette.green);
  stat(details, "Morale", "85", 420, 430, 226, 0.85, palette.gold);
  section(details, "CURRENT STATE", 706, 228, 302, 250);
  metric(details, "Health", "86%", 730, 280);
  metric(details, "Fatigue", "Low", 730, 332);
  metric(details, "Assignment", "Idle", 730, 384);
  metric(details, "Bond", "Tom Ren", 730, 436);
  card(details, "WOUND STATUS", "No active wounds. Last treated 2 days ago.", 26, 530, 312, 168, palette.green);
  equipmentSlot(details, "RIFLE", "Clean / 16 ammo", 366, 530);
  equipmentSlot(details, "BACKPACK", "+4 loot capacity", 588, 530);
  equipmentSlot(details, "RADIO", "Signal relay", 810, 530);
  button(details, "ASSIGN TO EXPEDITION", 26, 726, 312, 52, "primary");
  button(details, "SEND TO INFIRMARY", 366, 726, 312, 52, "secondary");
  button(details, "REST 8 HOURS", 706, 726, 302, 52, "secondary");

  const assignments = panel(root, "Survivor assignment panel", R.x, R.y, R.w, R.h, "ASSIGNMENTS");
  callout(assignments, "BEST FIT", "Abandoned Store run needs one scout. Mara adds +18% safety.", 24, 70, 350, 114, palette.teal2);
  miniTask(assignments, "EXPEDITION SQUAD", "2 / 3 ready", 24, 212, 350, 82, 0.66, palette.orange);
  miniTask(assignments, "WATCH DUTY", "4 posted", 24, 310, 350, 82, 0.92, palette.green);
  miniTask(assignments, "INFIRMARY", "2 patients", 24, 408, 350, 82, 0.44, palette.red);
  section(assignments, "CAMP NOTES", 24, 530, 350, 144);
  copy(assignments, "Mara found a sealed cache near the highway. Follow-up route marked for dawn.", 44, 584, 310, 64, 15);
  button(assignments, "CONFIRM ASSIGNMENT", 24, 724, 350, 52, "primary");
}

function buildExpeditions(root) {
  const list = panel(root, "Expedition zones panel", L.x, L.y, L.w, L.h, "AVAILABLE ZONES");
  segmented(list, 22, 66, ["NEAR", "SAFE", "RISK"], 0);
  zoneCard(list, "ABANDONED STORE", "MEDIUM", "Fuel, cans, parts", 22, 126, imageHash("ui_thumb_zone_abandoned_store"), palette.orange);
  zoneCard(list, "DRY SUBURB", "LOW", "Food, cloth, tools", 22, 300, imageHash("ui_thumb_zone_dry_suburb"), palette.green);
  zoneCard(list, "RUINED CLINIC", "HIGH", "Medicine, batteries", 22, 474, imageHash("ui_thumb_zone_abandoned_store"), palette.red);
  button(list, "SCAN NEW ZONE", 22, 736, 282, 44, "secondary");

  const map = panel(root, "Route planning panel", C.x, C.y, C.w, C.h, "ROUTE PLANNING");
  imageBox(map, "World map wash", 26, 70, 988, 520, imageHash("ui_backplate_clean_world_map_01") || imageHash("ui_bg_expedition_setup_01"), { radius: 8, opacity: 0.9 });
  route(map, 178, 422, 360, 330, palette.teal2);
  route(map, 538, 330, 764, 246, palette.orange);
  route(map, 764, 246, 846, 390, palette.red);
  mapMarker(map, "CAMP", 166, 410, palette.teal2, true);
  mapMarker(map, "STORE", 526, 318, palette.orange, true);
  mapMarker(map, "SUBURB", 752, 234, palette.green, false);
  mapMarker(map, "CLINIC", 834, 378, palette.red, false);
  card(map, "MISSION BRIEF", "Recover fuel cans and sealed food. Expected resistance: raider scouts.", 26, 616, 310, 152, palette.orange);
  card(map, "ROUTE TIME", "6 hours outbound, 2 hours search, 6 hours return.", 366, 616, 310, 152, palette.teal2);
  card(map, "CAMP IMPACT", "Success unlocks generator upgrade and one new trade request.", 706, 616, 308, 152, palette.green);

  const launch = panel(root, "Expedition launch panel", R.x, R.y, R.w, R.h, "LAUNCH");
  labelStrip(launch, "ABANDONED STORE", "MEDIUM RISK  /  65% SUCCESS", 24, 70, 350, 84);
  section(launch, "CREW", 24, 184, 350, 190);
  crewChip(launch, "MARA", "Scout", 44, 238, 148, imageHash("ui_character_battle_survivor_01"));
  crewChip(launch, "TOM", "Builder", 206, 238, 148, imageHash("ui_character_battle_survivor_02"));
  emptySlot(launch, "ADD SURVIVOR", 44, 318, 310, 42);
  section(launch, "EXPECTED LOOT", 24, 398, 350, 170);
  rewardRow(launch, "Fuel", "18-30", 44, 452);
  rewardRow(launch, "Food", "24-42", 44, 494);
  rewardRow(launch, "Parts", "8-14", 44, 536);
  miniTask(launch, "THREAT", "Raider scouts", 24, 596, 350, 82, 0.58, palette.orange);
  button(launch, "LAUNCH EXPEDITION", 24, 724, 350, 52, "primary");
}

function buildBuildings(root) {
  const categories = panel(root, "Building categories panel", L.x, L.y, L.w, L.h, "BUILD MENU");
  segmented(categories, 22, 66, ["ALL", "LIFE", "DEF"], 0);
  ["Shelter", "Water", "Food", "Medicine", "Power", "Defense"].forEach((name, index) => {
    menuRow(categories, name, index === 4 ? "UPGRADE READY" : "Level " + ((index % 3) + 1), 22, 126 + index * 82, 282, index === 4);
  });
  miniTask(categories, "BUILD CAPACITY", "12 / 18 plots", 22, 658, 282, 60, 0.66, palette.teal2);
  button(categories, "OPEN BLUEPRINTS", 22, 736, 282, 44, "secondary");

  const grid = panel(root, "Building grid panel", C.x, C.y, C.w, C.h, "CAMP BUILDINGS");
  const buildings = [
    ["BARRACKS", "Level 2", "20 / 24 beds", palette.teal2],
    ["WATER COLLECTOR", "Level 2", "+48 /h", palette.green],
    ["WORKSHOP", "Level 2", "Upgrade ready", palette.orange],
    ["INFIRMARY", "Level 1", "2 patients", palette.red],
    ["WATCHTOWER", "Level 2", "Secure", palette.gold],
    ["FARM PLOTS", "Level 3", "+28 food", palette.green],
    ["GENERATOR", "Damaged", "Needs fuel", palette.orange],
    ["RADIO TOWER", "Level 2", "+1 intel", palette.teal2],
    ["STORAGE", "Level 3", "86% full", palette.gold]
  ];
  buildings.forEach((b, index) => {
    const col = index % 3;
    const row = Math.floor(index / 3);
    buildingCard(grid, b, 26 + col * 330, 72 + row * 228, 306, 196, index === 2);
  });

  const upgrade = panel(root, "Building upgrade panel", R.x, R.y, R.w, R.h, "WORKSHOP UPGRADE");
  labelStrip(upgrade, "WORKSHOP", "LEVEL 2  ->  LEVEL 3", 24, 70, 350, 84);
  section(upgrade, "BENEFITS", 24, 184, 350, 174);
  benefit(upgrade, "+1 crafting slot", 44, 238);
  benefit(upgrade, "Advanced gear recipes", 44, 280);
  benefit(upgrade, "-12% repair time", 44, 322);
  section(upgrade, "REQUIREMENTS", 24, 386, 350, 210);
  req(upgrade, "Scrap", "860 / 750", 44, 438, true);
  req(upgrade, "Parts", "75 / 90", 44, 486, false);
  req(upgrade, "Blueprints", "2 / 2", 44, 534, true);
  callout(upgrade, "BLOCKED", "Need 15 more parts before construction can begin.", 24, 624, 350, 86, palette.orange);
  button(upgrade, "TRACK PARTS", 24, 724, 350, 52, "primary");
}

function buildWorkshop(root) {
  const queue = panel(root, "Workshop queue panel", L.x, L.y, L.w, L.h, "WORKSHOP");
  segmented(queue, 22, 66, ["GEAR", "TOOLS", "MED"], 0);
  miniTask(queue, "PIPE RIFLE", "1h 45m left", 22, 126, 282, 82, 0.72, palette.teal2);
  miniTask(queue, "FILTER MASKS", "Queued", 22, 224, 282, 82, 0.18, palette.gold);
  miniTask(queue, "FIELD MEDKIT", "Queued", 22, 322, 282, 82, 0.1, palette.green);
  section(queue, "MATERIAL STOCK", 22, 446, 282, 210);
  req(queue, "Scrap", "1,250", 44, 500, true);
  req(queue, "Parts", "75", 44, 548, false);
  req(queue, "Electronics", "18", 44, 596, true);
  button(queue, "AUTO-QUEUE REPAIRS", 22, 736, 282, 44, "secondary");

  const recipes = panel(root, "Recipe browser panel", C.x, C.y, C.w, C.h, "CRAFTING RECIPES");
  imageBox(recipes, "Workshop wash", 26, 70, 988, 202, imageHash("ui_backplate_clean_workshop_01"), { radius: 8, opacity: 0.78 });
  const cards = [
    ["PIPE RIFLE", "Weapon", "DMG +18", palette.orange],
    ["FILTER MASK", "Utility", "Ash safe", palette.teal2],
    ["FIELD MEDKIT", "Medicine", "+1 use", palette.green],
    ["LIGHT ARMOR", "Defense", "Armor +7", palette.gold],
    ["DUCT TAPE KIT", "Repair", "-20% time", palette.green],
    ["SIGNAL BOOSTER", "Radio", "+1 intel", palette.teal2]
  ];
  cards.forEach((item, index) => {
    const col = index % 3;
    const row = Math.floor(index / 3);
    recipeCard(recipes, item, 26 + col * 330, 306 + row * 198, 306, 168, index === 0);
  });
  button(recipes, "CRAFT SELECTED", 26, 724, 306, 52, "primary");
  button(recipes, "COMPARE LOADOUT", 356, 724, 306, 52, "secondary");
  button(recipes, "PIN MATERIALS", 686, 724, 328, 52, "secondary");

  const item = panel(root, "Selected recipe panel", R.x, R.y, R.w, R.h, "SELECTED ITEM");
  labelStrip(item, "PIPE RIFLE", "WEAPON  /  COMMON BLUEPRINT", 24, 70, 350, 84);
  section(item, "STAT CHANGE", 24, 184, 350, 174);
  stat(item, "Damage", "+18", 44, 238, 290, 0.78, palette.orange);
  stat(item, "Noise", "High", 44, 288, 290, 0.54, palette.red);
  stat(item, "Durability", "64", 44, 338, 290, 0.64, palette.gold);
  section(item, "MATERIALS", 24, 386, 350, 210);
  req(item, "Scrap", "80 / 80", 44, 438, true);
  req(item, "Parts", "10 / 12", 44, 486, false);
  req(item, "Duct tape", "4 / 4", 44, 534, true);
  callout(item, "ASSIGNMENT", "Tom Ren crafts 18% faster from Builder trait.", 24, 624, 350, 86, palette.green);
  button(item, "START CRAFT", 24, 724, 350, 52, "primary");
}

function buildRadio(root) {
  const tuner = panel(root, "Radio tuner panel", L.x, L.y, L.w, L.h, "RADIO TOWER");
  segmented(tuner, 22, 66, ["INTEL", "TRADE", "CALL"], 0);
  frequency(tuner, "88.3", "Settlement relay", "Clear", 22, 126, true);
  frequency(tuner, "91.7", "Distress loop", "Weak", 22, 234, false);
  frequency(tuner, "104.2", "Raider chatter", "Noisy", 22, 342, false);
  section(tuner, "SIGNAL", 22, 486, 282, 166);
  waveform(tuner, 42, 548, 242, 54, palette.teal2);
  stat(tuner, "Strength", "86%", 42, 618, 242, 0.86, palette.green);
  button(tuner, "BOOST SIGNAL", 22, 736, 282, 44, "secondary");

  const broadcast = panel(root, "Broadcast panel", C.x, C.y, C.w, C.h, "BROADCAST");
  imageBox(broadcast, "Radio operator", 26, 70, 380, 520, imageHash("ui_illustration_radio_operator_clean") || imageHash("ui_backplate_clean_radio_recruitment_01"), { radius: 8, opacity: 0.92 });
  labelStrip(broadcast, "NEW TRANSMISSION", "88.3 MHZ  /  09:42", 436, 70, 578, 84);
  callout(broadcast, "TRADE REQUEST", "North Ridge settlement will trade 2 blueprints for medicine and clean water.", 436, 184, 578, 136, palette.teal2);
  section(broadcast, "MESSAGE LOG", 436, 352, 578, 238);
  copy(broadcast, "09:42  North Ridge confirms route is passable.\n09:35  Static burst near clinic sector.\n09:10  Unknown signal repeats: 'keep lights low'.", 462, 408, 526, 112, 16);
  card(broadcast, "DECISION", "Accepting this request opens a trade convoy and raises radio reputation.", 26, 616, 310, 152, palette.green);
  card(broadcast, "RISK", "Convoy route crosses an ash storm corridor in 12 hours.", 366, 616, 310, 152, palette.orange);
  card(broadcast, "REWARD", "Blueprints, reputation, possible recruit lead.", 706, 616, 308, 152, palette.teal2);

  const recruits = panel(root, "Recruitment panel", R.x, R.y, R.w, R.h, "RECRUITS");
  crewChip(recruits, "SERA", "Radio Operator", 24, 76, 350, imageHash("ui_character_battle_survivor_02"));
  crewChip(recruits, "DAX", "Mechanic", 24, 166, 350, imageHash("ui_character_battle_survivor_01"));
  callout(recruits, "COST", "Food 40  /  Water 30  /  Radio Intel 3", 24, 278, 350, 92, palette.gold);
  section(recruits, "INTEL BOARD", 24, 408, 350, 216);
  benefit(recruits, "Clinic sector has medicine cache", 44, 462);
  benefit(recruits, "Farmland route unsafe after dusk", 44, 506);
  benefit(recruits, "Raider patrol moved east", 44, 550);
  button(recruits, "ACCEPT TRADE", 24, 724, 168, 52, "primary");
  button(recruits, "RECRUIT", 206, 724, 168, 52, "secondary");
}

function panel(parent, name, x, y, width, height, title) {
  const node = figma.createFrame();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(width, height);
  node.fills = [paint(palette.paper, 0.9)];
  node.strokes = [paint(palette.line, 0.72)];
  node.strokeWeight = 2;
  node.cornerRadius = 8;
  node.clipsContent = true;
  node.effects = [shadow(0, 8, 18, 0.12)];
  parent.appendChild(node);
  text(node, "Panel title", title, 0, 20, width, 34, fonts.heading, 25, palette.teal, { align: "CENTER" });
  rect(node, "panel divider", 26, 58, width - 52, 1, palette.line, { opacity: 0.65 });
  return node;
}

function labelStrip(parent, title, subtitle, x, y, w, h) {
  const node = frameBox(parent, `Label strip / ${title}`, x, y, w, h, palette.paper2, { opacity: 0.95, stroke: palette.line, radius: 8 });
  text(node, "title", title, 22, 14, w - 44, 34, fonts.heading, 25, palette.ink);
  text(node, "subtitle", subtitle, 22, 48, w - 44, 24, fonts.bodyMedium, 13, palette.muted);
  return node;
}

function segmented(parent, x, y, labels, activeIndex) {
  const width = 282;
  const h = 42;
  const seg = frameBox(parent, "Segmented control", x, y, width, h, "#EFE2C9", { opacity: 0.94, stroke: palette.line, radius: 6 });
  const itemW = width / labels.length;
  labels.forEach((label, i) => {
    const active = i === activeIndex;
    rect(seg, `segment ${label}`, i * itemW + 3, 4, itemW - 6, h - 8, active ? palette.teal2 : "#F8EEDC", { radius: 4, opacity: active ? 1 : 0.62 });
    text(seg, label, label, i * itemW, 10, itemW, 22, fonts.heading, 14, active ? "#F6EAD0" : palette.ink, { align: "CENTER" });
  });
}

function survivorRow(parent, x, y, w, person, active) {
  const [name, role, status, health, hash] = person;
  const row = frameBox(parent, `Roster / ${name}`, x, y, w, 88, active ? palette.paper2 : "#F8EEDC", { opacity: active ? 0.98 : 0.78, stroke: active ? palette.teal2 : palette.line, radius: 8 });
  imageBox(row, "portrait", 10, 10, 58, 68, hash, { radius: 6 });
  text(row, "name", name, 82, 10, 128, 24, fonts.heading, 17, palette.ink);
  text(row, "role", `${role} / ${status}`, 82, 36, 132, 22, fonts.body, 12, palette.muted);
  progress(row, 82, 64, 160, 8, health, health > 0.75 ? palette.green : palette.orange);
  badge(row, Math.round(health * 100) + "%", 226, 14, 42, palette.teal2);
}

function zoneCard(parent, title, risk, subtitle, x, y, hash, color) {
  const cardNode = frameBox(parent, `Zone / ${title}`, x, y, 282, 150, palette.paper2, { opacity: 0.95, stroke: palette.line, radius: 8 });
  imageBox(cardNode, "thumb", 10, 12, 108, 82, hash, { radius: 6 });
  text(cardNode, "title", title, 132, 14, 134, 26, fonts.heading, 17, palette.ink);
  badge(cardNode, risk, 132, 46, 76, color);
  copy(cardNode, subtitle, 132, 78, 134, 38, 12);
  progress(cardNode, 12, 124, 258, 8, risk === "LOW" ? 0.32 : risk === "HIGH" ? 0.82 : 0.58, color);
}

function buildingCard(parent, item, x, y, w, h, active) {
  const [name, level, value, color] = item;
  const node = frameBox(parent, `Building / ${name}`, x, y, w, h, active ? palette.paper2 : "#F8EEDC", { opacity: active ? 1 : 0.88, stroke: active ? color : palette.line, radius: 8 });
  rect(node, "icon base", 20, 22, 56, 56, color, { radius: 8 });
  text(node, "icon", name.slice(0, 1), 20, 29, 56, 42, fonts.heading, 28, "#F6EAD0", { align: "CENTER" });
  text(node, "name", name, 94, 18, 176, 28, fonts.heading, 19, palette.ink);
  text(node, "level", level, 94, 50, 176, 22, fonts.bodyMedium, 13, palette.muted);
  copy(node, value, 22, 102, w - 44, 30, 14);
  progress(node, 22, 148, w - 44, 10, active ? 0.78 : 0.54 + (x % 3) * 0.1, color);
}

function recipeCard(parent, item, x, y, w, h, active) {
  const [name, type, value, color] = item;
  const node = frameBox(parent, `Recipe / ${name}`, x, y, w, h, active ? palette.paper2 : "#F8EEDC", { opacity: active ? 1 : 0.86, stroke: active ? color : palette.line, radius: 8 });
  rect(node, "item icon", 20, 22, 74, 74, color, { radius: 8 });
  text(node, "initial", name.slice(0, 1), 20, 30, 74, 52, fonts.heading, 34, "#F6EAD0", { align: "CENTER" });
  text(node, "name", name, 112, 22, 168, 30, fonts.heading, 20, palette.ink);
  text(node, "type", type, 112, 58, 168, 22, fonts.bodyMedium, 13, palette.muted);
  badge(node, value, 112, 102, 104, color);
  progress(node, 20, 142, w - 40, 8, active ? 0.78 : 0.46, color);
}

function card(parent, title, body, x, y, w, h, color) {
  const node = frameBox(parent, `Card / ${title}`, x, y, w, h, palette.paper2, { opacity: 0.94, stroke: palette.line, radius: 8 });
  rect(node, "accent", 0, 0, 8, h, color, { radius: 4 });
  text(node, "title", title, 24, 16, w - 42, 28, fonts.heading, 18, palette.ink);
  copy(node, body, 24, 52, w - 42, h - 68, 13);
}

function callout(parent, title, body, x, y, w, h, color) {
  const node = frameBox(parent, `Callout / ${title}`, x, y, w, h, "#FFF4DF", { opacity: 0.95, stroke: color, radius: 8 });
  text(node, "title", title, 22, 14, w - 44, 28, fonts.heading, 18, color);
  copy(node, body, 22, 48, w - 44, h - 58, 14);
}

function miniTask(parent, title, subtitle, x, y, w, h, value, color) {
  const node = frameBox(parent, `Task / ${title}`, x, y, w, h, palette.paper2, { opacity: 0.92, stroke: palette.line, radius: 8 });
  text(node, "title", title, 18, 12, w - 36, 25, fonts.heading, 17, palette.ink);
  text(node, "subtitle", subtitle, 18, 38, w - 36, 22, fonts.body, 12, palette.muted);
  progress(node, 18, h - 18, w - 36, 8, value, color);
}

function section(parent, title, x, y, w, h) {
  const node = frameBox(parent, `Section / ${title}`, x, y, w, h, "#F8EEDC", { opacity: 0.82, stroke: palette.line, radius: 8 });
  text(node, "title", title, 18, 14, w - 36, 26, fonts.heading, 17, palette.teal);
  rect(node, "divider", 18, 48, w - 36, 1, palette.line, { opacity: 0.58 });
  return node;
}

function equipmentSlot(parent, title, body, x, y) {
  const node = frameBox(parent, `Equipment / ${title}`, x, y, 194, 168, palette.paper2, { opacity: 0.94, stroke: palette.line, radius: 8 });
  rect(node, "slot", 20, 20, 62, 62, "#EFE2C9", { radius: 8, stroke: palette.line });
  text(node, "icon", title.slice(0, 1), 20, 27, 62, 44, fonts.heading, 30, palette.teal, { align: "CENTER" });
  text(node, "title", title, 20, 100, 154, 24, fonts.heading, 17, palette.ink);
  text(node, "body", body, 20, 128, 154, 22, fonts.body, 12, palette.muted);
}

function crewChip(parent, title, body, x, y, w, hash) {
  const node = frameBox(parent, `Crew / ${title}`, x, y, w, 68, palette.paper2, { opacity: 0.94, stroke: palette.line, radius: 8 });
  imageBox(node, "portrait", 10, 10, 48, 48, hash, { radius: 6 });
  text(node, "name", title, 70, 10, w - 88, 24, fonts.heading, 17, palette.ink);
  text(node, "body", body, 70, 36, w - 88, 20, fonts.body, 12, palette.muted);
}

function emptySlot(parent, label, x, y, w, h) {
  const node = frameBox(parent, `Empty / ${label}`, x, y, w, h, "#F8EEDC", { opacity: 0.5, stroke: palette.line, radius: 8, dash: true });
  text(node, "label", label, 0, 10, w, 22, fonts.heading, 14, palette.muted, { align: "CENTER" });
}

function menuRow(parent, title, subtitle, x, y, w, active) {
  const node = frameBox(parent, `Menu / ${title}`, x, y, w, 64, active ? palette.paper2 : "#F8EEDC", { opacity: active ? 1 : 0.78, stroke: active ? palette.orange : palette.line, radius: 8 });
  text(node, "title", title.toUpperCase(), 18, 10, w - 36, 24, fonts.heading, 17, active ? palette.orange : palette.ink);
  text(node, "subtitle", subtitle, 18, 36, w - 36, 20, fonts.body, 12, palette.muted);
}

function frequency(parent, hz, title, status, x, y, active) {
  const node = frameBox(parent, `Frequency / ${hz}`, x, y, 282, 86, active ? palette.paper2 : "#F8EEDC", { opacity: active ? 1 : 0.76, stroke: active ? palette.teal2 : palette.line, radius: 8 });
  text(node, "hz", hz, 18, 14, 74, 34, fonts.heading, 26, active ? palette.teal : palette.ink);
  text(node, "title", title, 104, 16, 152, 22, fonts.bodyMedium, 13, palette.ink);
  text(node, "status", status, 104, 42, 152, 22, fonts.body, 12, palette.muted);
  waveform(node, 18, 62, 238, 12, active ? palette.teal2 : palette.lineDark);
}

function stat(parent, label, value, x, y, w, amount, color) {
  text(parent, `${label} label`, label, x, y, 126, 22, fonts.bodyMedium, 13, palette.ink);
  text(parent, `${label} value`, value, x + w - 62, y, 62, 22, fonts.bodyMedium, 13, palette.muted, { align: "RIGHT" });
  progress(parent, x, y + 28, w, 9, amount, color);
}

function metric(parent, label, value, x, y) {
  text(parent, label, label, x, y, 124, 22, fonts.body, 13, palette.muted);
  text(parent, `${label} value`, value, x + 138, y - 3, 112, 28, fonts.heading, 18, palette.ink, { align: "RIGHT" });
}

function rewardRow(parent, label, value, x, y) {
  text(parent, label, label, x, y, 174, 24, fonts.bodyMedium, 13, palette.ink);
  text(parent, `${label} value`, value, x + 206, y, 90, 24, fonts.heading, 15, palette.teal, { align: "RIGHT" });
  rect(parent, `${label} line`, x, y + 34, 296, 1, palette.line, { opacity: 0.45 });
}

function req(parent, label, value, x, y, ok) {
  rect(parent, `req dot ${label}`, x, y + 7, 12, 12, ok ? palette.green : palette.orange, { radius: 6 });
  text(parent, `req ${label}`, label, x + 24, y, 160, 24, fonts.bodyMedium, 13, palette.ink);
  text(parent, `req value ${label}`, value, x + 192, y, 96, 24, fonts.heading, 15, ok ? palette.green : palette.orange, { align: "RIGHT" });
}

function benefit(parent, label, x, y) {
  rect(parent, `benefit dot ${label}`, x, y + 7, 10, 10, palette.teal2, { radius: 5 });
  copy(parent, label, x + 24, y, 270, 24, 13);
}

function chip(parent, label, x, y, w, color) {
  const node = frameBox(parent, `Chip / ${label}`, x, y, w, 32, color, { opacity: 0.95, radius: 16 });
  text(node, "label", label, 0, 7, w, 18, fonts.heading, 13, "#F6EAD0", { align: "CENTER" });
}

function badge(parent, label, x, y, w, color) {
  const node = frameBox(parent, `Badge / ${label}`, x, y, w, 26, color, { opacity: 0.95, radius: 13 });
  text(node, "label", label, 0, 5, w, 16, fonts.heading, 12, "#F6EAD0", { align: "CENTER" });
}

function button(parent, label, x, y, w, h, style) {
  const primary = style === "primary";
  const fill = primary ? palette.orange : "#F8EEDC";
  const stroke = primary ? "#8D3F22" : palette.lineDark;
  const node = frameBox(parent, `Button / ${label}`, x, y, w, h, fill, { opacity: 1, stroke, radius: 6 });
  text(node, "label", label, 0, Math.max(8, (h - 24) / 2), w, 24, fonts.heading, 16, primary ? "#F6EAD0" : palette.ink, { align: "CENTER" });
}

function progress(parent, x, y, w, h, value, color) {
  rect(parent, "bar track", x, y, w, h, "#D8CDB8", { opacity: 0.92, radius: h / 2 });
  rect(parent, "bar fill", x, y, Math.max(4, w * Math.max(0, Math.min(1, value))), h, color, { radius: h / 2 });
}

function waveform(parent, x, y, w, h, color) {
  const count = 19;
  const gap = w / count;
  for (let i = 0; i < count; i += 1) {
    const height = 8 + ((i * 17) % 37);
    rect(parent, "wave", x + i * gap, y + (h - height) / 2, Math.max(3, gap - 5), height, color, { radius: 2, opacity: 0.85 });
  }
}

function route(parent, x1, y1, x2, y2, color) {
  const dx = x2 - x1;
  const dy = y2 - y1;
  const length = Math.sqrt(dx * dx + dy * dy);
  const node = rect(parent, "route", x1, y1, length, 4, color, { radius: 2, opacity: 0.75 });
  node.rotation = Math.atan2(dy, dx) * 180 / Math.PI;
}

function mapMarker(parent, label, x, y, color, active) {
  const dot = figma.createEllipse();
  dot.name = `Marker / ${label}`;
  dot.x = x;
  dot.y = y;
  dot.resize(active ? 34 : 28, active ? 34 : 28);
  dot.fills = [paint(color)];
  dot.strokes = [paint("#F6EAD0")];
  dot.strokeWeight = active ? 4 : 3;
  parent.appendChild(dot);
  text(parent, label, label, x - 34, y + 38, 100, 22, fonts.heading, 13, color, { align: "CENTER" });
}

function imageBox(parent, name, x, y, w, h, hash, options = {}) {
  const node = figma.createRectangle();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.cornerRadius = options.radius ?? 0;
  node.fills = hash
    ? [{ type: "IMAGE", imageHash: hash, scaleMode: "FILL", opacity: options.opacity ?? 1 }]
    : [paint("#D8CDB8", options.opacity ?? 1)];
  if (options.stroke) {
    node.strokes = [paint(options.stroke)];
    node.strokeWeight = options.strokeWeight || 1;
  }
  parent.appendChild(node);
  return node;
}

function frameBox(parent, name, x, y, w, h, fill, options = {}) {
  const node = figma.createFrame();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.fills = [paint(fill, options.opacity ?? 1)];
  node.cornerRadius = options.radius ?? 0;
  node.clipsContent = true;
  if (options.stroke) {
    node.strokes = [paint(options.stroke)];
    node.strokeWeight = options.strokeWeight || 1;
    if (options.dash) node.dashPattern = [8, 6];
  }
  parent.appendChild(node);
  return node;
}

function rect(parent, name, x, y, w, h, fill, options = {}) {
  const node = figma.createRectangle();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.cornerRadius = options.radius ?? 0;
  node.fills = [paint(fill, options.opacity ?? 1)];
  if (options.stroke) {
    node.strokes = [paint(options.stroke)];
    node.strokeWeight = options.strokeWeight || 1;
  }
  parent.appendChild(node);
  return node;
}

function text(parent, name, value, x, y, w, h, fontName, size, fill, options = {}) {
  const node = figma.createText();
  node.name = name;
  node.x = x;
  node.y = y;
  node.fontName = fontName;
  node.fontSize = size;
  node.lineHeight = { unit: "PIXELS", value: options.lineHeight || Math.round(size * 1.24) };
  node.letterSpacing = { unit: "PIXELS", value: 0 };
  node.textAlignHorizontal = options.align || "LEFT";
  node.textAlignVertical = options.valign || "TOP";
  node.textAutoResize = "NONE";
  node.resize(w, h);
  node.characters = value;
  node.fills = [paint(fill, options.opacity ?? 1)];
  parent.appendChild(node);
  return node;
}

function copy(parent, value, x, y, w, h, size = 13) {
  return text(parent, "copy", value, x, y, w, h, fonts.body, size, palette.ink, { lineHeight: Math.round(size * 1.42) });
}

function paint(hex, opacity = 1) {
  return { type: "SOLID", color: helpers.rgb(hex), opacity };
}

function shadow(x, y, radius, alpha) {
  return {
    type: "DROP_SHADOW",
    color: { r: 0, g: 0, b: 0, a: alpha },
    offset: { x, y },
    radius,
    spread: 0,
    visible: true,
    blendMode: "NORMAL"
  };
}

function imageHash(name) {
  const node = page.findOne(candidate => candidate.name === name);
  let found = null;
  function walk(current) {
    if (found) return;
    if ("fills" in current && Array.isArray(current.fills)) {
      const image = current.fills.find(fill => fill.type === "IMAGE" && fill.imageHash);
      if (image) {
        found = image.imageHash;
        return;
      }
    }
    if ("children" in current) {
      for (const child of current.children) walk(child);
    }
  }
  if (node) walk(node);
  return found;
}
