const VERSION = "ashfall-structure-only-ui-2026-07-05";

const fonts = {
  title: { family: "Inter", style: "Bold" },
  heading: { family: "Inter", style: "Semi Bold" },
  body: { family: "Inter", style: "Regular" },
  bodyMedium: { family: "Inter", style: "Medium" }
};

for (const font of Object.values(fonts)) {
  await figma.loadFontAsync(font);
}

const page = figma.currentPage;
const palette = {
  bg: "#EFE6D7",
  surface: "#FAF3E7",
  surface2: "#F4E9D8",
  line: "#B8A88E",
  line2: "#8F7E65",
  ink: "#252725",
  muted: "#6C665C",
  teal: "#25606A",
  tealDark: "#173E46",
  green: "#5E7B4C",
  blue: "#486E7A",
  amber: "#B78632",
  orange: "#C86B3D",
  red: "#A44B3D",
  dark: "#25211D",
  white: "#FFF9EC"
};

const survivors = [
  ["Mara", "Scavenger", "Idle", "92/100", "M", 0.92],
  ["Elias", "Ex-Cop", "Guard", "100/100", "E", 1],
  ["Nika", "Mechanic", "Workshop", "86/100", "N", 0.86],
  ["June", "Nurse", "Infirmary", "78/100", "J", 0.78],
  ["Tomas", "Brawler", "Ready", "112/112", "T", 1],
  ["Rhea", "Hunter", "Scouting", "94/100", "R", 0.94],
  ["Kade", "Scavenger", "Idle", "88/100", "K", 0.88]
];

const enemies = [
  ["Feral Dog", "Beast", "14/14", "FAST", "FD", 3, 0, 8],
  ["Starving Survivor", "Human", "18/18", "DESPERATE", "SS", 4, 0, 4],
  ["Raider", "Human", "30/30", "RANGED", "RA", 6, 1, 5],
  ["Mutant Stray", "Mutant", "26/26", "PACK", "MS", 5, 1, 6],
  ["Armored Raider", "Human", "45/45", "ARMORED", "AR", 7, 4, 2],
  ["Mutant Brute", "Mutant", "80/80", "BRUTE", "MB", 11, 2, 1]
];

const buildings = [
  ["Barracks", "Level 2", "5/6", "82%", "Population capacity", "Recovery speed"],
  ["Workshop", "Level 2", "4/5", "78%", "Crafting and repairs", "Tool durability"],
  ["Water Collector", "Level 3", "4/4", "88%", "Daily water", "Purification"],
  ["Infirmary", "Level 2", "3/4", "75%", "Faster healing", "Less illness"],
  ["Radio Tower", "Level 2", "2/3", "70%", "Radio missions", "Intel range"]
];

const resources = [
  ["Scrap", "1,250"],
  ["Food", "320"],
  ["Water", "480"],
  ["Medicine", "210"],
  ["Weapon Parts", "75"]
];

const componentNames = [
  "Ashfall Structure/Survivor Row",
  "Ashfall Structure/Battle Card",
  "Ashfall Structure/Enemy Card",
  "Ashfall Structure/Building Card",
  "Ashfall Structure/Resource Pill",
  "Ashfall Structure/Button",
  "Ashfall Structure/Panel",
  "Ashfall Structure/Bottom Nav"
];

const createdComponents = {};
const mutatedNodeIds = [];
const createdNodeIds = [];

removePriorStructureComponents();
const componentFrame = ensureComponentFrame();
buildComponents(componentFrame);

const frames = [
  ["Ashfall Camp - Full HD / 01 Camp Dashboard", buildCamp],
  ["Ashfall Camp - Full HD / 02 Survivors", buildSurvivors],
  ["Ashfall Camp - Full HD / 03 Expeditions", buildExpeditions],
  ["Ashfall Camp - Full HD / 04 Buildings", buildBuildings],
  ["Ashfall Camp - Full HD / 05 Workshop", buildWorkshop],
  ["Ashfall Camp - Full HD / 06 Radio", buildRadio],
  ["Ashfall Camp - Full HD / 07 Expedition Monitor", buildCombat]
];

const touchedFrames = [];
for (const [name, builder] of frames) {
  const frame = ensureFrame(name);
  wipe(frame);
  drawBase(frame, name);
  builder(frame);
  touchedFrames.push(frame);
  mutatedNodeIds.push(frame.id);
}

const audit = auditFrames(touchedFrames);
figma.currentPage.selection = touchedFrames;
figma.viewport.scrollAndZoomIntoView(touchedFrames);

return {
  version: VERSION,
  createdNodeIds,
  mutatedNodeIds,
  components: componentNames,
  frames: touchedFrames.map(f => ({ id: f.id, name: f.name, x: f.x, y: f.y, width: f.width, height: f.height })),
  audit
};

function removePriorStructureComponents() {
  const names = new Set(componentNames);
  for (const node of [...page.findAll(n => (n.type === "COMPONENT" || n.type === "COMPONENT_SET") && names.has(n.name))]) {
    node.remove();
  }
}

function ensureComponentFrame() {
  let frame = page.findOne(n => n.type === "FRAME" && n.name === "Ashfall Camp - Structure Components");
  if (!frame) {
    frame = figma.createFrame();
    frame.name = "Ashfall Camp - Structure Components";
    frame.x = -3460;
    frame.y = -4;
    frame.resize(1700, 1220);
    page.appendChild(frame);
    createdNodeIds.push(frame.id);
  }
  wipe(frame);
  frame.fills = [solid(palette.bg)];
  frame.strokes = [solid(palette.line)];
  frame.strokeWeight = 1;
  addText(frame, "Title", "ASHFALL STRUCTURE COMPONENTS", 40, 34, 760, 38, { font: "heading", size: 28, fill: palette.ink });
  addText(frame, "Note", "No image fills. These components are layout placeholders for readable structure and spacing checks.", 40, 76, 980, 22, { font: "body", size: 14, fill: palette.muted });
  return frame;
}

function buildComponents(parent) {
  createdComponents["Ashfall Structure/Panel"] = makePanelComponent(parent, 40, 132);
  createdComponents["Ashfall Structure/Survivor Row"] = makeSurvivorRowComponent(parent, 40, 370);
  createdComponents["Ashfall Structure/Resource Pill"] = makeResourcePillComponent(parent, 40, 490);
  createdComponents["Ashfall Structure/Button"] = makeButtonComponent(parent, 40, 570);
  createdComponents["Ashfall Structure/Building Card"] = makeBuildingCardComponent(parent, 420, 132);
  createdComponents["Ashfall Structure/Battle Card"] = makeBattleCardComponent(parent, 980, 132, false);
  createdComponents["Ashfall Structure/Enemy Card"] = makeBattleCardComponent(parent, 1180, 132, true);
  createdComponents["Ashfall Structure/Bottom Nav"] = makeBottomNavComponent(parent, 420, 410);
}

function makePanelComponent(parent, x, y) {
  const c = component(parent, "Ashfall Structure/Panel", x, y, 320, 190);
  c.fills = [solid(palette.surface)];
  c.strokes = [solid(palette.line)];
  c.strokeWeight = 1;
  c.cornerRadius = 8;
  addText(c, "Heading", "PANEL TITLE", 18, 16, 260, 24, { font: "heading", size: 16, fill: palette.teal });
  line(c, 18, 50, 284);
  addText(c, "Body", "Structured content area", 18, 70, 260, 20, { font: "body", size: 13, fill: palette.muted });
  return c;
}

function makeSurvivorRowComponent(parent, x, y) {
  const c = component(parent, "Ashfall Structure/Survivor Row", x, y, 292, 78);
  c.fills = [solid(palette.surface)];
  c.strokes = [solid(palette.line)];
  c.strokeWeight = 1;
  c.cornerRadius = 7;
  rect(c, "Avatar", 12, 12, 54, 54, palette.teal, 6);
  addText(c, "Initials", "MA", 12, 28, 54, 18, { font: "heading", size: 16, fill: palette.white, align: "CENTER" });
  addText(c, "Name", "Mara", 80, 12, 110, 20, { font: "heading", size: 16, fill: palette.ink });
  addText(c, "Role", "Scavenger", 80, 34, 120, 16, { font: "bodyMedium", size: 11, fill: palette.teal });
  addText(c, "State", "Idle", 80, 54, 96, 14, { font: "body", size: 10, fill: palette.muted });
  addText(c, "HP Text", "92/100", 206, 12, 64, 16, { font: "bodyMedium", size: 11, fill: palette.ink, align: "RIGHT" });
  bar(c, "HP", 200, 40, 72, 8, 0.78, palette.green);
  return c;
}

function makeResourcePillComponent(parent, x, y) {
  const c = component(parent, "Ashfall Structure/Resource Pill", x, y, 142, 44);
  c.fills = [solid(palette.dark)];
  c.cornerRadius = 7;
  rect(c, "Icon", 12, 12, 20, 20, palette.amber, 4);
  addText(c, "Value", "1,250", 42, 7, 72, 20, { font: "heading", size: 16, fill: palette.white });
  addText(c, "Label", "Scrap", 42, 27, 78, 12, { font: "body", size: 9, fill: "#D5C8B5" });
  return c;
}

function makeButtonComponent(parent, x, y) {
  const c = component(parent, "Ashfall Structure/Button", x, y, 178, 44);
  c.fills = [solid(palette.orange)];
  c.strokes = [solid("#873A20")];
  c.strokeWeight = 1;
  c.cornerRadius = 6;
  addText(c, "Label", "ACTION", 18, 12, 142, 18, { font: "heading", size: 14, fill: palette.white, align: "CENTER" });
  return c;
}

function makeBuildingCardComponent(parent, x, y) {
  const c = component(parent, "Ashfall Structure/Building Card", x, y, 460, 174);
  c.fills = [solid(palette.surface)];
  c.strokes = [solid(palette.line)];
  c.strokeWeight = 1;
  c.cornerRadius = 8;
  rect(c, "Icon", 18, 20, 62, 62, palette.teal, 8);
  addText(c, "Glyph", "B", 18, 40, 62, 24, { font: "heading", size: 22, fill: palette.white, align: "CENTER" });
  addText(c, "Name", "Barracks", 98, 18, 210, 24, { font: "heading", size: 20, fill: palette.ink });
  addText(c, "Level", "Level 2", 98, 44, 90, 16, { font: "bodyMedium", size: 11, fill: palette.teal });
  addText(c, "Workers", "5/6", 380, 22, 44, 16, { font: "heading", size: 13, fill: palette.ink, align: "RIGHT" });
  addText(c, "Effect1", "Population capacity", 98, 76, 220, 16, { font: "body", size: 11, fill: palette.ink });
  addText(c, "Effect2", "Recovery speed", 98, 98, 220, 16, { font: "body", size: 11, fill: palette.muted });
  addText(c, "ConditionText", "82%", 18, 132, 42, 16, { font: "bodyMedium", size: 11, fill: palette.ink });
  bar(c, "Condition", 66, 137, 144, 8, 0.82, palette.green);
  rect(c, "CTA", 322, 122, 112, 34, palette.orange, 5);
  addText(c, "CTA Text", "UPGRADE", 334, 131, 88, 14, { font: "heading", size: 12, fill: palette.white, align: "CENTER" });
  return c;
}

function makeBattleCardComponent(parent, x, y, enemy) {
  const c = component(parent, enemy ? "Ashfall Structure/Enemy Card" : "Ashfall Structure/Battle Card", x, y, 176, 286);
  c.fills = [solid(enemy ? "#F1DED3" : "#E8EFD8")];
  c.strokes = [solid(enemy ? palette.red : palette.green)];
  c.strokeWeight = 2;
  c.cornerRadius = 8;
  rect(c, "Header", 10, 10, 156, 42, enemy ? palette.red : palette.tealDark, 5);
  addText(c, "Name", enemy ? "Raider" : "Mara", 18, 16, 98, 18, { font: "heading", size: 14, fill: palette.white });
  addText(c, "Meta", enemy ? "XP 10" : "LVL 4", 120, 18, 36, 14, { font: "bodyMedium", size: 9, fill: palette.white, align: "RIGHT" });
  rect(c, "Portrait Block", 18, 66, 140, 94, enemy ? "#D8B9A9" : "#C9D7BA", 6);
  addText(c, "Initials", enemy ? "RA" : "MA", 18, 98, 140, 28, { font: "heading", size: 26, fill: enemy ? palette.red : palette.teal, align: "CENTER" });
  addText(c, "HP Text", enemy ? "30/30" : "92/100", 42, 170, 92, 20, { font: "heading", size: 18, fill: palette.ink, align: "CENTER" });
  bar(c, "HP", 22, 196, 132, 10, enemy ? 0.82 : 0.76, enemy ? palette.red : palette.green);
  addText(c, "Tag", enemy ? "RANGED" : "CAREFUL", 22, 216, 86, 14, { font: "bodyMedium", size: 10, fill: enemy ? palette.red : palette.green });
  for (let i = 0; i < 3; i += 1) {
    rect(c, `Slot ${i + 1}`, 22 + i * 46, 238, 36, 32, palette.dark, 4);
  }
  addText(c, "Damage", "6", 26, 270, 28, 14, { font: "bodyMedium", size: 10, fill: palette.ink, align: "CENTER" });
  addText(c, "Armor", "1", 74, 270, 28, 14, { font: "bodyMedium", size: 10, fill: palette.ink, align: "CENTER" });
  addText(c, "Speed", "5", 122, 270, 28, 14, { font: "bodyMedium", size: 10, fill: palette.ink, align: "CENTER" });
  return c;
}

function makeBottomNavComponent(parent, x, y) {
  const c = component(parent, "Ashfall Structure/Bottom Nav", x, y, 1664, 82);
  c.fills = [solid(palette.surface2)];
  c.strokes = [solid(palette.line)];
  c.strokeWeight = 1;
  c.cornerRadius = 8;
  const tabs = ["Camp", "Survivors", "Expeditions", "Buildings", "Workshop", "Radio"];
  tabs.forEach((tab, i) => {
    const tabX = 16 + i * 272;
    rect(c, `Tab ${tab}`, tabX, 12, 250, 58, i === 0 ? palette.teal : "#FDF5E8", 6);
    rect(c, `Tab Icon ${tab}`, tabX + 20, 24, 24, 24, i === 0 ? palette.white : "#D7C8AF", 4);
    addText(c, `Tab Label ${tab}`, tab.toUpperCase(), tabX + 58, 27, 160, 18, { font: "heading", size: 14, fill: i === 0 ? palette.white : palette.ink });
  });
  return c;
}

function buildCamp(root) {
  header(root, "CAMP DASHBOARD", "Structure-only overview");
  resourceBar(root);
  const left = panel(root, "Camp Status", 32, 134, 320, 800);
  statList(left, [["Population", "42 / 60"], ["Workers", "28 / 32"], ["Morale", "Good"], ["Safety", "Good"], ["Food / Day", "324"], ["Water / Day", "280"], ["Power", "210"]], 22, 72, 42);
  section(left, "ACTIVE SURVIVORS", 22, 410);
  survivors.slice(0, 4).forEach((s, i) => {
    const row = inst("Ashfall Structure/Survivor Row", left, 16, 452 + i * 82);
    setSurvivor(row, s);
  });

  const center = panel(root, "Camp Overview", 382, 134, 1024, 800);
  section(center, "CAMP OVERVIEW", 22, 20);
  const map = frame(center, "Map Structure Placeholder", 22, 64, 658, 354, { fill: "#E4D5BE", stroke: palette.line, radius: 8 });
  addText(map, "Map Label", "CAMP MAP / BUILDING LAYOUT", 0, 150, 658, 28, { font: "heading", size: 24, fill: palette.muted, align: "CENTER" });
  [["Barracks", 90, 86], ["Workshop", 350, 190], ["Water", 150, 248], ["Radio", 470, 96], ["Infirmary", 510, 260]].forEach(([name, x, y]) => {
    const marker = frame(map, `Marker ${name}`, x, y, 148, 56, { fill: palette.surface, stroke: palette.line2, radius: 6 });
    addText(marker, "Name", name, 12, 10, 104, 18, { font: "heading", size: 13, fill: palette.ink });
    addText(marker, "Meta", "Level 2", 12, 30, 80, 14, { font: "body", size: 10, fill: palette.muted });
  });
  const queue = panel(center, "Priority Queue", 704, 64, 286, 354);
  section(queue, "TODAY", 18, 16);
  ["Assign workers", "Craft bandages", "Scout Dry Suburb", "Repair Workshop"].forEach((item, i) => checklist(queue, item, 22, 62 + i * 54, i < 2));
  section(center, "SUMMARY CARDS", 22, 460);
  [["Wounded Survivors", "2 need treatment"], ["Idle Survivors", "8 ready for work"], ["Upgrade Available", "Workshop can upgrade"]].forEach((item, i) => {
    const card = frame(center, `Summary ${item[0]}`, 22 + i * 318, 508, 296, 112, { fill: palette.surface, stroke: palette.line, radius: 8 });
    addText(card, "Title", item[0].toUpperCase(), 20, 18, 190, 24, { font: "heading", size: 15, fill: palette.teal });
    addText(card, "Text", item[1], 20, 54, 220, 34, { font: "body", size: 12, fill: palette.ink });
  });
  const actions = [["GO TO INFIRMARY", 22], ["MANAGE CREW", 342], ["VIEW UPGRADE", 662]];
  actions.forEach(([label, x]) => setButton(inst("Ashfall Structure/Button", center, x, 678), label));

  const right = panel(root, "Active Expeditions", 1436, 134, 452, 800);
  section(right, "ACTIVE EXPEDITIONS", 22, 20);
  expeditionCard(right, "Abandoned Store", "Scavenge / spare parts", "65%", 22, 68);
  expeditionCard(right, "Dry Suburb", "Food and water cache", "48%", 22, 214);
  const empty = frame(right, "Empty Slot", 22, 378, 408, 124, { fill: palette.surface2, stroke: palette.line, radius: 8, dash: [8, 6] });
  addText(empty, "Plus", "+", 0, 36, 408, 26, { font: "heading", size: 28, fill: palette.muted, align: "CENTER" });
  addText(empty, "Label", "SLOT AVAILABLE", 0, 68, 408, 16, { font: "heading", size: 13, fill: palette.muted, align: "CENTER" });
  section(right, "RADIO INTEL", 22, 564);
  addText(right, "Intel", "New transmission received. Nearby settlement may trade for medicine and tools.", 22, 606, 350, 52, { font: "body", size: 13, fill: palette.ink });
  setButton(inst("Ashfall Structure/Button", right, 22, 696), "LISTEN");
  bottomNav(root, "Camp");
}

function buildSurvivors(root) {
  header(root, "SURVIVORS", "Roster, detail, status and assignment panels");
  resourceBar(root);
  const roster = panel(root, "Roster", 32, 134, 360, 800);
  section(roster, "ROSTER", 22, 20);
  segmented(roster, ["ALL", "READY", "WOUNDED"], 22, 62, 316, 38, 0);
  survivors.forEach((s, i) => {
    const row = inst("Ashfall Structure/Survivor Row", roster, 22, 122 + i * 88);
    row.resize(316, 78);
    setSurvivor(row, s);
  });

  const detail = panel(root, "Survivor Detail", 422, 134, 812, 800);
  section(detail, "MARA / SCAVENGER", 26, 20);
  const portrait = frame(detail, "Portrait Placeholder", 26, 64, 244, 300, { fill: "#D8CAB3", stroke: palette.line, radius: 8 });
  addText(portrait, "Initials", "M", 0, 120, 244, 64, { font: "heading", size: 58, fill: palette.teal, align: "CENTER" });
  statList(detail, [["Status", "Idle in Camp"], ["Health", "92 / 100"], ["Fatigue", "18 / 100"], ["Morale", "76 / 100"], ["Trait", "Careful"]], 300, 78, 46);
  section(detail, "SKILLS", 300, 332);
  [["Scavenging", 4], ["Melee", 1], ["Firearms", 0], ["Survival", 2], ["Mechanics", 0], ["Medicine", 0]].forEach(([name, v], i) => skillRow(detail, name, v, 300, 374 + i * 54));
  section(detail, "EQUIPMENT", 26, 414);
  ["Rusty Knife", "Scout Pack", "Bandages (3)"].forEach((item, i) => equipRow(detail, item, 26, 456 + i * 66));
  section(detail, "NOTES", 26, 672);
  addText(detail, "Notes", "Starting scavenger. Strongest in short supply runs and early resource recovery.", 26, 710, 470, 42, { font: "body", size: 13, fill: palette.ink });

  const actions = panel(root, "Actions", 1264, 134, 624, 800);
  section(actions, "ASSIGNMENT", 22, 20);
  ["Rest", "Treat", "Assign to Expedition", "Dismiss"].forEach((item, i) => {
    const b = inst("Ashfall Structure/Button", actions, 22 + (i % 2) * 204, 70 + Math.floor(i / 2) * 62);
    setButton(b, item.toUpperCase());
    if (item === "Dismiss") b.fills = [solid(palette.red)];
  });
  section(actions, "TEAM COVERAGE", 22, 230);
  [["Scavenging", 7], ["Firearms", 7], ["Mechanics", 5], ["Medicine", 4], ["Survival", 7], ["Melee", 8]].forEach(([name, value], i) => skillRow(actions, name, value / 2, 22 + (i % 2) * 290, 276 + Math.floor(i / 2) * 68, 250));
  section(actions, "CATALOG LIST", 22, 536);
  addText(actions, "Catalog", "Mara, Elias, Nika, June, Tomas, Rhea and Kade are all present. No visual asset dependency in this wire structure.", 22, 578, 520, 54, { font: "body", size: 13, fill: palette.ink });
  bottomNav(root, "Survivors");
}

function buildExpeditions(root) {
  header(root, "EXPEDITIONS", "Mission setup and zone selection");
  resourceBar(root);
  const zones = panel(root, "Zone Catalog", 32, 134, 366, 800);
  section(zones, "ZONE CATALOG", 22, 20);
  [["Abandoned Store", "Low", "3", "Food, Medicine"], ["Dry Suburb", "Medium", "5", "Scrap, Water"], ["Ruined Clinic", "Medium", "4", "Medicine"], ["Police Outpost", "High", "6", "Weapon Parts"], ["Mutant Tunnel", "High", "7", "Scrap, Parts"]].forEach((z, i) => zoneRow(zones, z, 22, 66 + i * 120, i === 1));

  const plan = panel(root, "Route Planner", 428, 134, 1008, 800);
  section(plan, "SCAVENGE RUN / DRY SUBURB", 24, 20);
  const map = frame(plan, "Route Map", 24, 70, 596, 342, { fill: "#E2D3BC", stroke: palette.line, radius: 8 });
  addText(map, "Map", "ROUTE MAP PLACEHOLDER", 0, 142, 596, 28, { font: "heading", size: 24, fill: palette.muted, align: "CENTER" });
  for (let i = 0; i < 6; i += 1) {
    rect(map, `Route Node ${i}`, 62 + i * 90, 172 + (i % 2) * 28, 20, 20, i === 0 ? palette.green : (i === 5 ? palette.red : palette.surface), 20);
  }
  const squad = panel(plan, "Squad", 650, 70, 330, 342);
  section(squad, "SQUAD 4/4", 18, 16);
  survivors.slice(0, 4).forEach((s, i) => smallChip(squad, s, 22 + (i % 2) * 146, 58 + Math.floor(i / 2) * 112, 122));
  section(plan, "ENEMY CATALOG", 24, 452);
  enemies.forEach((e, i) => enemyChip(plan, e, 24 + (i % 3) * 316, 496 + Math.floor(i / 3) * 108));

  const launch = panel(root, "Launch Summary", 1466, 134, 422, 800);
  section(launch, "LAUNCH SUMMARY", 22, 20);
  statList(launch, [["Mission", "Scavenge Run"], ["Zone", "Dry Suburb"], ["Threat", "Medium"], ["Distance", "5"], ["Weather", "Cloudy"]], 22, 70, 46);
  section(launch, "LOOT PREVIEW", 22, 344);
  resources.forEach((r, i) => {
    const p = inst("Ashfall Structure/Resource Pill", launch, 22 + (i % 2) * 170, 388 + Math.floor(i / 2) * 56);
    setResource(p, r);
  });
  setButton(inst("Ashfall Structure/Button", launch, 22, 596), "START");
  setButton(inst("Ashfall Structure/Button", launch, 222, 596), "EDIT SQUAD");
  bottomNav(root, "Expeditions");
}

function buildBuildings(root) {
  header(root, "BUILDINGS", "Catalog cards without thumbnails");
  resourceBar(root);
  const summary = panel(root, "Summary", 32, 134, 330, 800);
  section(summary, "CAMP SUMMARY", 22, 20);
  statList(summary, [["Population", "42 / 60"], ["Workers", "28 / 32"], ["Morale", "Good"], ["Safety", "Good"]], 22, 72, 42);
  section(summary, "CAPACITY", 22, 296);
  statList(summary, [["Total Buildings", "12 / 20"], ["Assigned", "28 / 32"], ["Idle", "4"]], 22, 342, 42);
  const note = frame(summary, "Asset Gap Note", 22, 506, 286, 146, { fill: palette.surface2, stroke: palette.line, radius: 8 });
  addText(note, "Title", "STRUCTURE FIRST", 18, 18, 180, 20, { font: "heading", size: 15, fill: palette.teal });
  addText(note, "Text", "No building art used. Thumbnail area is represented as icon placeholder and metadata layout.", 18, 52, 230, 54, { font: "body", size: 12, fill: palette.ink });

  const grid = panel(root, "Building Grid", 392, 134, 1018, 800);
  section(grid, "BUILDING CATALOG", 22, 20);
  segmented(grid, ["ALL", "PRODUCTION", "SUPPORT", "UTILITY", "DEFENSE"], 22, 62, 720, 38, 0);
  buildings.forEach((b, i) => {
    const card = inst("Ashfall Structure/Building Card", grid, 22 + (i % 2) * 496, 128 + Math.floor(i / 2) * 196);
    setBuilding(card, b);
  });
  const add = frame(grid, "Build Slot", 518, 520, 460, 174, { fill: palette.surface2, stroke: palette.line, radius: 8, dash: [8, 6] });
  addText(add, "Plus", "+", 0, 48, 460, 36, { font: "heading", size: 34, fill: palette.muted, align: "CENTER" });
  addText(add, "Label", "BUILD NEW STRUCTURE", 0, 92, 460, 18, { font: "heading", size: 14, fill: palette.muted, align: "CENTER" });

  const upgrade = panel(root, "Upgrade", 1440, 134, 448, 800);
  section(upgrade, "SELECTED: WORKSHOP", 22, 20);
  statList(upgrade, [["Level", "2 -> 3"], ["Workers", "4 / 5"], ["Condition", "78%"], ["Cost", "260 Scrap, 120 Water, 80 Parts"]], 22, 76, 56);
  addText(upgrade, "Effect", "Enables crafting and repairs. Improves tool durability.", 22, 336, 340, 48, { font: "body", size: 13, fill: palette.ink });
  setButton(inst("Ashfall Structure/Button", upgrade, 22, 430), "UPGRADE");
  bottomNav(root, "Buildings");
}

function buildWorkshop(root) {
  header(root, "WORKSHOP", "Craft queue and recipes");
  resourceBar(root);
  const queue = panel(root, "Queue", 32, 134, 344, 800);
  section(queue, "CRAFT QUEUE", 22, 20);
  [["Bandages", "2 turns", 0.35], ["Rifle Repair", "1 turn", 0.72], ["Ammo Box", "3 turns", 0.2]].forEach((q, i) => queueRow(queue, q, 22, 70 + i * 110));

  const recipes = panel(root, "Recipes", 406, 134, 1030, 800);
  section(recipes, "RECIPE BROWSER", 22, 20);
  [["Rusty Revolver", "Weapon", "25 Scrap, 15 Parts"], ["Hunting Rifle", "Weapon", "40 Scrap, 35 Parts"], ["Machete", "Melee", "18 Scrap, 8 Parts"], ["Armor Vest", "Armor", "30 Scrap, 22 Parts"], ["Scout Pack", "Gear", "12 Scrap, 6 Parts"], ["Field Medkit", "Supplies", "8 Medicine, 5 Scrap"]].forEach((r, i) => recipeCard(recipes, r, 22 + (i % 2) * 500, 70 + Math.floor(i / 2) * 150));
  section(recipes, "RESOURCE CHECK", 22, 556);
  resources.forEach((r, i) => setResource(inst("Ashfall Structure/Resource Pill", recipes, 22 + i * 156, 604), r));

  const selected = panel(root, "Selected Recipe", 1466, 134, 422, 800);
  section(selected, "SELECTED: HUNTING RIFLE", 22, 20);
  statList(selected, [["Type", "Weapon / Firearms"], ["Damage", "+6"], ["Accuracy", "+12%"], ["Cost", "40 Scrap, 35 Parts"]], 22, 78, 58);
  addText(selected, "Desc", "Reliable long-range weapon for Rhea or Elias. Wireframe keeps slot, stat, cost and CTA zones stable.", 22, 338, 340, 68, { font: "body", size: 13, fill: palette.ink });
  setButton(inst("Ashfall Structure/Button", selected, 22, 482), "CRAFT");
  bottomNav(root, "Workshop");
}

function buildRadio(root) {
  header(root, "RADIO", "Recruitment and signal screen");
  resourceBar(root);
  const signal = panel(root, "Signal", 32, 134, 350, 800);
  section(signal, "SIGNAL", 22, 20);
  statList(signal, [["Radio Tower", "Level 2"], ["Intel Range", "Medium"], ["Daily Contacts", "3"]], 22, 76, 54);
  bar(signal, "Signal", 22, 270, 286, 14, 0.64, palette.teal);
  addText(signal, "Hint", "Better tower condition expands candidate quality and zone intel.", 22, 320, 260, 44, { font: "body", size: 13, fill: palette.ink });

  const recruits = panel(root, "Recruits", 412, 134, 1024, 800);
  section(recruits, "AVAILABLE RECRUITS", 22, 20);
  survivors.slice(1).forEach((s, i) => {
    const row = inst("Ashfall Structure/Survivor Row", recruits, 22 + (i % 3) * 320, 70 + Math.floor(i / 3) * 102);
    setSurvivor(row, s);
  });
  section(recruits, "BROADCAST OPTIONS", 22, 330);
  [["General Call", "Balanced background chance"], ["Medic Request", "Higher medicine chance"], ["Guard Request", "Higher firearms/melee chance"]].forEach((b, i) => broadcastRow(recruits, b, 22, 382 + i * 94));

  const selected = panel(root, "Selected Recruit", 1466, 134, 422, 800);
  section(selected, "SELECTED: RHEA", 22, 20);
  const av = frame(selected, "Recruit Placeholder", 22, 70, 156, 196, { fill: "#D8CAB3", stroke: palette.line, radius: 8 });
  addText(av, "Initial", "R", 0, 74, 156, 52, { font: "heading", size: 48, fill: palette.teal, align: "CENTER" });
  statList(selected, [["Role", "Hunter / Quiet"], ["Firearms", "3"], ["Survival", "3"], ["Weapon", "Hunting Rifle"]], 202, 80, 52);
  setButton(inst("Ashfall Structure/Button", selected, 22, 354), "RECRUIT");
  bottomNav(root, "Radio");
}

function buildCombat(root) {
  header(root, "EXPEDITION MONITOR", "4 player slots vs 4 enemy slots");
  const topY = 112;
  const mission = panel(root, "Mission Header", 24, topY, 520, 210);
  section(mission, "MISSION: SCAVENGE RUN", 20, 18);
  addText(mission, "Zone", "Dry Suburb - Ambush Site", 20, 48, 220, 18, { font: "bodyMedium", size: 12, fill: palette.muted });
  section(mission, "SQUAD 4/4", 20, 84);
  survivors.slice(0, 4).forEach((s, i) => smallChip(mission, s, 20 + i * 120, 122, 96));

  const encounter = panel(root, "Encounter", 564, topY, 456, 210);
  section(encounter, "ENCOUNTER", 20, 18);
  const art = frame(encounter, "Scene Placeholder", 20, 54, 416, 96, { fill: "#E2D3BC", stroke: palette.line, radius: 6 });
  addText(art, "Label", "ENCOUNTER ILLUSTRATION SLOT", 0, 36, 416, 20, { font: "heading", size: 16, fill: palette.muted, align: "CENTER" });
  statStrip(encounter, [["Enemies", "4"], ["Round", "∞"], ["Victory", "Defeat all"]], 20, 166, 416);

  const status = panel(root, "Status", 1040, topY, 222, 210);
  section(status, "TIME & STATUS", 18, 18);
  statList(status, [["Time", "10:30"], ["Threat", "Medium"], ["Noise", "Low"]], 18, 62, 42);

  const map = panel(root, "Zone Overview", 1282, topY, 330, 210);
  section(map, "ZONE OVERVIEW", 18, 18);
  const mini = frame(map, "Minimap Placeholder", 18, 54, 294, 94, { fill: "#E2D3BC", stroke: palette.line, radius: 6 });
  addText(mini, "Mini", "MINIMAP", 0, 34, 294, 20, { font: "heading", size: 15, fill: palette.muted, align: "CENTER" });
  addText(map, "Distance", "DISTANCE LEFT 5", 18, 168, 128, 16, { font: "heading", size: 12, fill: palette.ink });
  addText(map, "Weather", "CLOUDY", 236, 168, 70, 16, { font: "heading", size: 12, fill: palette.ink, align: "RIGHT" });

  const log = panel(root, "Combat Log", 1632, topY, 264, 210);
  section(log, "COMBAT LOG", 18, 18);
  ["Round 1 begins", "Mara gains Stealth", "Raider prepares", "Feral Dog closes in"].forEach((l, i) => logLine(log, l, 18, 58 + i * 32));

  const phase = frame(root, "Phase Bar", 24, 344, 1872, 62, { fill: palette.surface, stroke: palette.line, radius: 8 });
  addText(phase, "Label", "COMBAT PHASE", 18, 18, 170, 24, { font: "heading", size: 22, fill: palette.ink });
  rect(phase, "Player", 240, 20, 620, 20, palette.green, 4);
  rect(phase, "Clash", 890, 20, 240, 20, palette.amber, 4);
  rect(phase, "Enemy", 1160, 20, 560, 20, palette.red, 4);
  addText(phase, "PT", "PLAYER TURN", 240, 22, 620, 14, { font: "heading", size: 11, fill: palette.white, align: "CENTER" });
  addText(phase, "CT", "CLASH", 890, 22, 240, 14, { font: "heading", size: 11, fill: palette.ink, align: "CENTER" });
  addText(phase, "ET", "ENEMY TURN", 1160, 22, 560, 14, { font: "heading", size: 11, fill: palette.white, align: "CENTER" });

  const field = frame(root, "Battlefield", 24, 424, 1872, 362, { fill: "#D5C6AD", stroke: palette.line2, radius: 8 });
  addText(field, "Left Lane", "PLAYER LANE", 24, 20, 160, 16, { font: "heading", size: 12, fill: palette.muted });
  addText(field, "Right Lane", "ENEMY LANE", 1688, 20, 160, 16, { font: "heading", size: 12, fill: palette.muted, align: "RIGHT" });
  survivors.slice(0, 4).forEach((s, i) => {
    const card = inst("Ashfall Structure/Battle Card", field, 24 + i * 188, 48);
    card.name = `Player Battle Card / ${s[0]}`;
    setBattle(card, s, false, i);
  });
  enemies.slice(0, 4).forEach((e, i) => {
    const card = inst("Ashfall Structure/Enemy Card", field, 1084 + i * 188, 48);
    card.name = `Enemy Battle Card / ${e[0]}`;
    setBattle(card, e, true, i);
  });
  addText(field, "VS", "VS", 900, 150, 72, 44, { font: "heading", size: 34, fill: palette.muted, align: "CENTER" });

  const bottom = frame(root, "Action Bar", 24, 808, 1872, 124, { fill: palette.surface, stroke: palette.line, radius: 8 });
  rect(bottom, "Energy Icon", 24, 30, 58, 58, palette.amber, 30);
  addText(bottom, "Energy", "4 / 5", 100, 44, 80, 30, { font: "heading", size: 26, fill: palette.ink });
  section(bottom, "ABILITIES", 250, 18);
  ["Aim", "Guard", "Treat", "Move", "Item"].forEach((a, i) => ability(bottom, a, 250 + i * 82, 54));
  addText(bottom, "Info", "Use abilities or move before clash. All battle cards fit inside fixed lane bounds.", 720, 42, 394, 34, { font: "body", size: 12, fill: palette.ink });
  setButton(inst("Ashfall Structure/Button", bottom, 1160, 40), "RETREAT");
  const use = inst("Ashfall Structure/Button", bottom, 1362, 40);
  setButton(use, "USE ITEM");
  use.fills = [solid(palette.green)];
  setButton(inst("Ashfall Structure/Button", bottom, 1564, 40), "END TURN");
}

function ensureFrame(name) {
  let frame = page.findOne(n => n.type === "FRAME" && n.name === name);
  if (!frame) {
    const existing = page.children.filter(n => n.type === "FRAME" && n.name.startsWith("Ashfall Camp - Full HD /"));
    const maxX = existing.length ? Math.max(...existing.map(n => n.x + n.width)) : 0;
    frame = figma.createFrame();
    frame.name = name;
    frame.x = maxX + 120;
    frame.y = 0;
    frame.resize(1920, 1080);
    page.appendChild(frame);
    createdNodeIds.push(frame.id);
  }
  frame.resize(1920, 1080);
  return frame;
}

function wipe(node) {
  if (!("children" in node)) return;
  for (const child of [...node.children]) child.remove();
}

function drawBase(root, name) {
  root.fills = [solid(palette.bg)];
  root.strokes = [solid("#6B604E")];
  root.strokeWeight = 1;
  root.clipsContent = true;
  rect(root, "Canvas Safe Area", 16, 16, 1888, 1048, "transparent", 0, 0);
  addText(root, "Frame Label", name, 24, 14, 620, 18, { font: "body", size: 11, fill: palette.muted });
}

function header(root, title, subtitle) {
  addText(root, "Brand", "ASHFALL CAMP", 72, 40, 320, 38, { font: "title", size: 34, fill: palette.ink });
  addText(root, "Tagline", "REBUILD  *  SURVIVE  *  THRIVE", 72, 82, 320, 16, { font: "heading", size: 12, fill: palette.teal });
  addText(root, "Screen Title", title, 612, 36, 520, 42, { font: "title", size: 36, fill: palette.ink, align: "CENTER" });
  addText(root, "Screen Subtitle", subtitle, 612, 82, 520, 18, { font: "bodyMedium", size: 12, fill: palette.muted, align: "CENTER" });
}

function resourceBar(root) {
  const barFrame = frame(root, "Top Resource Bar", 1128, 32, 750, 62, { fill: palette.surface, stroke: palette.line, radius: 8 });
  resources.forEach((r, i) => {
    const pill = inst("Ashfall Structure/Resource Pill", barFrame, 12 + i * 146, 9);
    setResource(pill, r);
    pill.resize(132, 44);
  });
}

function bottomNav(root, active) {
  const nav = inst("Ashfall Structure/Bottom Nav", root, 128, 970);
  nav.name = `Bottom Nav / ${active}`;
  const tabs = ["Camp", "Survivors", "Expeditions", "Buildings", "Workshop", "Radio"];
  tabs.forEach(tab => {
    const bg = nav.findOne(n => n.name === `Tab ${tab}`);
    const icon = nav.findOne(n => n.name === `Tab Icon ${tab}`);
    const label = nav.findOne(n => n.name === `Tab Label ${tab}`);
    const isActive = tab === active;
    if (bg && "fills" in bg) bg.fills = [solid(isActive ? palette.teal : "#FDF5E8")];
    if (icon && "fills" in icon) icon.fills = [solid(isActive ? palette.white : "#D7C8AF")];
    if (label && "fills" in label) label.fills = [solid(isActive ? palette.white : palette.ink)];
  });
}

function panel(parent, name, x, y, w, h) {
  return frame(parent, name, x, y, w, h, { fill: palette.surface, stroke: palette.line, radius: 8 });
}

function statList(parent, list, x, y, gap) {
  list.forEach(([label, value], i) => {
    addText(parent, `${label} Label`, label, x, y + i * gap, 150, 18, { font: "body", size: 13, fill: palette.ink });
    addText(parent, `${label} Value`, value, x + 176, y + i * gap, Math.max(70, parent.width - x - 198), 18, { font: "heading", size: 13, fill: palette.ink, align: "RIGHT" });
  });
}

function section(parent, label, x, y) {
  addText(parent, `Section ${label}`, label, x, y, Math.max(80, parent.width - x * 2), 22, { font: "heading", size: 16, fill: palette.teal });
}

function skillRow(parent, name, value, x, y, width = 420) {
  const row = frame(parent, `Skill ${name}`, x, y, width, 42, { fill: palette.surface2, stroke: palette.line, radius: 6 });
  addText(row, "Name", name, 12, 11, 118, 16, { font: "bodyMedium", size: 11, fill: palette.ink });
  bar(row, "Value", 142, 17, width - 194, 8, Math.min(1, value / 5), palette.green);
  addText(row, "Number", String(value), width - 36, 9, 22, 16, { font: "heading", size: 12, fill: palette.ink, align: "RIGHT" });
}

function equipRow(parent, label, x, y) {
  const row = frame(parent, `Equipment ${label}`, x, y, 244, 48, { fill: palette.surface2, stroke: palette.line, radius: 6 });
  rect(row, "Icon", 12, 12, 24, 24, palette.dark, 4);
  addText(row, "Label", label, 50, 14, 150, 18, { font: "heading", size: 13, fill: palette.ink });
}

function expeditionCard(parent, title, body, progressText, x, y) {
  const card = frame(parent, `Expedition ${title}`, x, y, 408, 126, { fill: palette.surface2, stroke: palette.line, radius: 8 });
  addText(card, "Title", title.toUpperCase(), 20, 18, 220, 20, { font: "heading", size: 16, fill: palette.ink });
  addText(card, "Body", body, 20, 46, 260, 32, { font: "body", size: 12, fill: palette.ink });
  bar(card, "Progress", 20, 94, 300, 10, parseInt(progressText, 10) / 100, palette.blue);
  addText(card, "Progress Text", progressText, 334, 88, 42, 18, { font: "heading", size: 13, fill: palette.ink, align: "RIGHT" });
}

function zoneRow(parent, z, x, y, selected) {
  const row = frame(parent, `Zone ${z[0]}`, x, y, 322, 94, { fill: selected ? "#E8F0E1" : palette.surface2, stroke: selected ? palette.teal : palette.line, radius: 7 });
  addText(row, "Name", z[0], 16, 14, 188, 20, { font: "heading", size: 16, fill: palette.ink });
  addText(row, "Threat", `Threat: ${z[1]}`, 16, 40, 100, 14, { font: "bodyMedium", size: 10, fill: z[1] === "High" ? palette.red : palette.teal });
  addText(row, "Loot", z[3], 16, 62, 160, 14, { font: "body", size: 10, fill: palette.muted });
  addText(row, "Distance", z[2], 270, 30, 28, 26, { font: "heading", size: 22, fill: palette.ink, align: "CENTER" });
}

function enemyChip(parent, e, x, y) {
  const chip = frame(parent, `Enemy ${e[0]}`, x, y, 292, 76, { fill: palette.surface2, stroke: palette.line, radius: 7 });
  rect(chip, "Avatar", 12, 12, 46, 46, palette.red, 6);
  addText(chip, "Initials", e[4], 12, 27, 46, 16, { font: "heading", size: 12, fill: palette.white, align: "CENTER" });
  addText(chip, "Name", e[0], 72, 12, 154, 18, { font: "heading", size: 14, fill: palette.ink });
  addText(chip, "Meta", `HP ${e[2]}  DMG ${e[5]}`, 72, 38, 150, 14, { font: "bodyMedium", size: 10, fill: palette.muted });
}

function smallChip(parent, s, x, y, width) {
  const chip = frame(parent, `Chip ${s[0]}`, x, y, width, 82, { fill: palette.surface2, stroke: palette.line, radius: 7 });
  rect(chip, "Avatar", 12, 10, 44, 44, palette.teal, 6);
  addText(chip, "Initial", s[4], 12, 24, 44, 16, { font: "heading", size: 13, fill: palette.white, align: "CENTER" });
  addText(chip, "Name", s[0], 64, 12, width - 76, 16, { font: "heading", size: 12, fill: palette.ink });
  addText(chip, "Role", s[1], 64, 34, width - 76, 14, { font: "body", size: 10, fill: palette.muted });
  bar(chip, "HP", 64, 58, width - 84, 7, s[5], palette.green);
}

function queueRow(parent, q, x, y) {
  const row = frame(parent, `Queue ${q[0]}`, x, y, 300, 84, { fill: palette.surface2, stroke: palette.line, radius: 7 });
  rect(row, "Icon", 14, 16, 44, 44, palette.dark, 6);
  addText(row, "Name", q[0], 72, 16, 140, 18, { font: "heading", size: 15, fill: palette.ink });
  addText(row, "Time", q[1], 72, 42, 80, 14, { font: "bodyMedium", size: 10, fill: palette.teal });
  bar(row, "Progress", 174, 48, 90, 8, q[2], palette.green);
}

function recipeCard(parent, r, x, y) {
  const card = frame(parent, `Recipe ${r[0]}`, x, y, 470, 118, { fill: palette.surface2, stroke: palette.line, radius: 8 });
  rect(card, "Icon", 18, 24, 52, 52, palette.dark, 6);
  addText(card, "Name", r[0], 88, 20, 176, 20, { font: "heading", size: 16, fill: palette.ink });
  addText(card, "Type", r[1], 88, 46, 100, 14, { font: "bodyMedium", size: 10, fill: palette.teal });
  addText(card, "Cost", r[2], 88, 70, 190, 14, { font: "body", size: 10, fill: palette.muted });
  setButton(inst("Ashfall Structure/Button", card, 300, 38), "CRAFT");
}

function broadcastRow(parent, b, x, y) {
  const row = frame(parent, `Broadcast ${b[0]}`, x, y, 940, 70, { fill: palette.surface2, stroke: palette.line, radius: 7 });
  addText(row, "Name", b[0], 20, 16, 160, 18, { font: "heading", size: 15, fill: palette.ink });
  addText(row, "Body", b[1], 20, 40, 300, 14, { font: "body", size: 11, fill: palette.muted });
  setButton(inst("Ashfall Structure/Button", row, 738, 14), "BROADCAST");
}

function statStrip(parent, values, x, y, width) {
  const boxW = width / values.length - 10;
  values.forEach(([label, value], i) => {
    const box = frame(parent, `Stat ${label}`, x + i * (boxW + 15), y, boxW, 28, { fill: palette.surface2, stroke: palette.line, radius: 5 });
    addText(box, "Label", label.toUpperCase(), 8, 8, 62, 12, { font: "bodyMedium", size: 8, fill: palette.muted });
    addText(box, "Value", value, 74, 6, boxW - 84, 14, { font: "heading", size: 11, fill: palette.ink, align: "RIGHT" });
  });
}

function logLine(parent, textValue, x, y) {
  rect(parent, "Dot", x, y + 4, 8, 8, palette.teal, 8);
  addText(parent, "Text", textValue, x + 18, y, 190, 16, { font: "body", size: 11, fill: palette.ink });
}

function ability(parent, label, x, y) {
  const item = frame(parent, `Ability ${label}`, x, y, 54, 54, { fill: palette.surface2, stroke: palette.line, radius: 8 });
  rect(item, "Icon", 13, 8, 28, 28, palette.amber, 14);
  addText(item, "Label", label, 0, 40, 54, 10, { font: "body", size: 8, fill: palette.ink, align: "CENTER" });
}

function segmented(parent, labels, x, y, w, h, active) {
  const seg = frame(parent, "Segmented", x, y, w, h, { fill: palette.surface2, stroke: palette.line, radius: 6 });
  const tabW = w / labels.length;
  labels.forEach((label, i) => {
    rect(seg, `Seg ${label}`, i * tabW, 0, tabW, h, i === active ? palette.teal : "transparent", 5, i === active ? 1 : 0);
    addText(seg, `Label ${label}`, label, i * tabW, 11, tabW, 14, { font: "heading", size: 11, fill: i === active ? palette.white : palette.ink, align: "CENTER" });
  });
}

function checklist(parent, textValue, x, y, checked) {
  rect(parent, "Check", x, y, 18, 18, checked ? palette.green : palette.surface, 4);
  addText(parent, "Label", textValue, x + 30, y, 210, 18, { font: "body", size: 12, fill: palette.ink });
}

function setSurvivor(row, s) {
  setText(row, "Initials", s[4]);
  setText(row, "Name", s[0]);
  setText(row, "Role", s[1]);
  setText(row, "State", s[2]);
  setText(row, "HP Text", s[3]);
  setBar(row, "HP Fill", "HP Track", s[5]);
}

function setResource(pill, r) {
  pill.name = `Resource Pill / ${r[0]}`;
  setText(pill, "Label", r[0]);
  setText(pill, "Value", r[1]);
}

function setBuilding(card, b) {
  card.name = `Building Card / ${b[0]}`;
  setText(card, "Glyph", b[0].slice(0, 1));
  setText(card, "Name", b[0]);
  setText(card, "Level", b[1]);
  setText(card, "Workers", b[2]);
  setText(card, "ConditionText", b[3]);
  setText(card, "Effect1", b[4]);
  setText(card, "Effect2", b[5]);
  setBar(card, "Condition Fill", "Condition Track", parseInt(b[3], 10) / 100);
}

function setBattle(card, actor, enemy, index) {
  if (enemy) {
    setText(card, "Name", actor[0]);
    setText(card, "Meta", `XP ${index === 0 ? 4 : index === 1 ? 5 : index === 2 ? 10 : 8}`);
    setText(card, "Initials", actor[4]);
    setText(card, "HP Text", actor[2]);
    setText(card, "Tag", actor[3]);
    setText(card, "Damage", String(actor[5]));
    setText(card, "Armor", String(actor[6]));
    setText(card, "Speed", String(actor[7]));
  } else {
    setText(card, "Name", actor[0]);
    setText(card, "Meta", `LVL ${4 - Math.min(index, 1)}`);
    setText(card, "Initials", actor[4]);
    setText(card, "HP Text", actor[3]);
    setText(card, "Tag", actor[2].toUpperCase());
    setText(card, "Damage", String(34 + index * 6));
    setText(card, "Armor", String(10 + index * 2));
    setText(card, "Speed", String(11 + index));
  }
}

function setButton(button, label) {
  button.name = `Button / ${label}`;
  setText(button, "Label", label);
}

function component(parent, name, x, y, w, h) {
  const c = figma.createComponent();
  c.name = name;
  c.x = x;
  c.y = y;
  c.resize(w, h);
  parent.appendChild(c);
  createdNodeIds.push(c.id);
  return c;
}

function inst(name, parent, x, y) {
  const comp = createdComponents[name];
  if (!comp) throw new Error(`Missing component ${name}`);
  const i = comp.createInstance();
  i.x = x;
  i.y = y;
  parent.appendChild(i);
  createdNodeIds.push(i.id);
  return i;
}

function frame(parent, name, x, y, w, h, options = {}) {
  const f = figma.createFrame();
  f.name = name;
  f.x = x;
  f.y = y;
  f.resize(w, h);
  f.fills = options.fill === "transparent" ? [] : [solid(options.fill || palette.surface)];
  f.strokes = options.stroke ? [solid(options.stroke)] : [];
  f.strokeWeight = options.stroke ? 1 : 0;
  f.cornerRadius = options.radius || 0;
  if (options.dash) f.dashPattern = options.dash;
  f.clipsContent = true;
  parent.appendChild(f);
  createdNodeIds.push(f.id);
  return f;
}

function rect(parent, name, x, y, w, h, fill, radius = 0, opacity = 1) {
  const r = figma.createRectangle();
  r.name = name;
  r.x = x;
  r.y = y;
  r.resize(w, h);
  r.fills = fill === "transparent" ? [] : [solid(fill, opacity)];
  r.cornerRadius = radius;
  parent.appendChild(r);
  createdNodeIds.push(r.id);
  return r;
}

function line(parent, x, y, w) {
  rect(parent, "Divider", x, y, w, 1, palette.line, 0, 0.9);
}

function bar(parent, name, x, y, w, h, value, color) {
  rect(parent, `${name} Track`, x, y, w, h, "#D8CAB3", h / 2);
  rect(parent, `${name} Fill`, x, y, Math.max(2, w * value), h, color, h / 2);
}

function setBar(parent, fillName, trackName, value) {
  const fill = parent.findOne(n => n.name === fillName);
  const track = parent.findOne(n => n.name === trackName);
  if (fill && track && "resize" in fill && "width" in track) {
    fill.resize(Math.max(2, track.width * Math.max(0, Math.min(1, value))), fill.height);
  }
}

function addText(parent, name, text, x, y, w, h, opts = {}) {
  const t = figma.createText();
  t.name = name;
  t.x = x;
  t.y = y;
  t.resize(w, h);
  t.fontName = fonts[opts.font || "body"];
  t.fontSize = opts.size || 12;
  t.fills = [solid(opts.fill || palette.ink, opts.opacity ?? 1)];
  t.textAlignHorizontal = opts.align || "LEFT";
  t.textAlignVertical = opts.valign || "TOP";
  t.textAutoResize = "HEIGHT";
  t.characters = String(text);
  parent.appendChild(t);
  createdNodeIds.push(t.id);
  return t;
}

function setText(parent, name, value) {
  const text = parent.findOne(n => n.type === "TEXT" && n.name === name);
  if (text) text.characters = String(value);
}

function solid(hex, opacity = 1) {
  if (hex === "transparent") return { type: "SOLID", color: { r: 0, g: 0, b: 0 }, opacity: 0 };
  const h = hex.replace("#", "");
  return {
    type: "SOLID",
    color: {
      r: parseInt(h.slice(0, 2), 16) / 255,
      g: parseInt(h.slice(2, 4), 16) / 255,
      b: parseInt(h.slice(4, 6), 16) / 255
    },
    opacity
  };
}

function auditFrames(frames) {
  const result = [];
  for (const frame of frames) {
    const nodes = frame.findAll(n => "x" in n && "y" in n && "width" in n && "height" in n);
    const outOfBounds = [];
    for (const node of nodes) {
      const x = node.absoluteTransform[0][2] - frame.absoluteTransform[0][2];
      const y = node.absoluteTransform[1][2] - frame.absoluteTransform[1][2];
      if (x < -1 || y < -1 || x + node.width > frame.width + 1 || y + node.height > frame.height + 1) {
        outOfBounds.push({ name: node.name, type: node.type, x: Math.round(x), y: Math.round(y), w: Math.round(node.width), h: Math.round(node.height) });
      }
    }
    const imageFills = frame.findAll(n => "fills" in n && Array.isArray(n.fills) && n.fills.some(f => f.type === "IMAGE")).map(n => n.name);
    result.push({ frame: frame.name, outOfBounds, imageFills, childCount: frame.children.length });
  }
  const combat = frames.find(f => f.name.endsWith("07 Expedition Monitor"));
  const enemyCards = combat.findAll(n => n.name.startsWith("Enemy Battle Card /")).map(n => {
    const x = n.absoluteTransform[0][2] - combat.absoluteTransform[0][2];
    const y = n.absoluteTransform[1][2] - combat.absoluteTransform[1][2];
    return { name: n.name, x: Math.round(x), y: Math.round(y), right: Math.round(x + n.width), bottom: Math.round(y + n.height), width: Math.round(n.width), height: Math.round(n.height) };
  });
  const overlaps = [];
  for (let i = 0; i < enemyCards.length; i += 1) {
    for (let j = i + 1; j < enemyCards.length; j += 1) {
      const a = enemyCards[i];
      const b = enemyCards[j];
      if (!(a.right <= b.x || b.right <= a.x || a.bottom <= b.y || b.bottom <= a.y)) overlaps.push([a.name, b.name]);
    }
  }
  return { frames: result, enemyCards, overlaps };
}
