const VERSION = "ashfall-restore-icons-backplates-off-combat-structure-2026-07-05";

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
  orange: "#C76A3B",
  red: "#A94D38",
  gold: "#B98533"
};

const survivors = [
  { name: "Mara", role: "Scavenger", hp: 92, maxHp: 100, tag: "Idle", stats: [34, 10, 11] },
  { name: "Elias", role: "Ex-Cop", hp: 100, maxHp: 100, tag: "Guard", stats: [40, 12, 12] },
  { name: "Nika", role: "Mechanic", hp: 86, maxHp: 100, tag: "Workshop", stats: [46, 14, 13] },
  { name: "June", role: "Nurse", hp: 78, maxHp: 100, tag: "Infirmary", stats: [52, 16, 14] }
];

const enemies = [
  { name: "Feral Dog", type: "Beast", hp: 14, maxHp: 14, tag: "Fast", xp: 4, stats: [3, 0, 8] },
  { name: "Starving Survivor", type: "Human", hp: 18, maxHp: 18, tag: "Desperate", xp: 5, stats: [4, 0, 4] },
  { name: "Raider", type: "Human", hp: 30, maxHp: 30, tag: "Ranged", xp: 10, stats: [6, 1, 5] },
  { name: "Mutant Stray", type: "Mutant", hp: 26, maxHp: 26, tag: "Pack", xp: 8, stats: [5, 1, 6] }
];

const targetFrames = [
  "Ashfall Camp - Full HD / 01 Camp Dashboard",
  "Ashfall Camp - Full HD / 02 Survivors",
  "Ashfall Camp - Full HD / 03 Expeditions",
  "Ashfall Camp - Full HD / 04 Buildings",
  "Ashfall Camp - Full HD / 05 Workshop",
  "Ashfall Camp - Full HD / 06 Radio",
  "Ashfall Camp - Full HD / 07 Expedition Monitor"
];

const frames = targetFrames.map(name => {
  const node = page.findOne(candidate => candidate.type === "FRAME" && candidate.name === name);
  if (!node) throw new Error(`Missing frame: ${name}`);
  return node;
});

const skippedTextFixes = [];
const componentFixes = cleanupComponentSources();
const nonCombatFrames = frames.filter(frame => !frame.name.endsWith("07 Expedition Monitor"));
const removedExtraHeaderNodes = removeDuplicateRootHeaders(nonCombatFrames);
const cleanedBackplates = removeLargeBackplates(nonCombatFrames);
const textSpacingFixes = cleanupTextSpacing(nonCombatFrames);
const combatFrame = frames.find(frame => frame.name.endsWith("07 Expedition Monitor"));
rebuildCombatStructure(combatFrame);

return {
  version: VERSION,
  componentFixes,
  removedExtraHeaderNodes,
  cleanedBackplates,
  textSpacingFixes,
  skippedTextFixes,
  audit: audit(frames)
};

function removeDuplicateRootHeaders(nonCombatFrames) {
  const removed = [];
  const extraNames = new Set(["Frame Label", "Screen Title", "Screen Subtitle"]);
  for (const frameNode of nonCombatFrames) {
    for (const child of [...frameNode.children]) {
      if (!extraNames.has(child.name)) continue;
      removed.push(`${frameNode.name}: ${child.name}`);
      child.remove();
    }
  }
  return removed;
}

function removeLargeBackplates(nonCombatFrames) {
  const cleaned = [];
  for (const frameNode of nonCombatFrames) {
    const imageNodes = frameNode.findAll(node =>
      "fills" in node &&
      Array.isArray(node.fills) &&
      node.fills.some(fill => fill.type === "IMAGE" && fill.imageHash)
    );
    for (const node of imageNodes) {
      if (!isBackplate(node)) continue;
      node.fills = [solid("#D8C6A9", 0.58)];
      if ("strokes" in node) node.strokes = [solid("#A48E6B", 0.65)];
      cleaned.push({
        frame: frameNode.name,
        node: node.name,
        width: Math.round(node.width || 0),
        height: Math.round(node.height || 0)
      });
    }
  }
  return cleaned;
}

function cleanupComponentSources() {
  const fixes = [];
  fixComponentText("Ashfall/Common/Resource Pill", [
    ["Value", 42, 5, 72, 18, 14],
    ["Label", 42, 28, 76, 11, 8]
  ], fixes);
  fixComponentText("Ashfall/Survivor/Roster Card", [
    ["Name", 88, 8, 122, 20, 15],
    ["Role", 88, 32, 102, 13, 10],
    ["Status", 88, 55, 116, 13, 10],
    ["HP", 210, 10, 70, 17, 12]
  ], fixes);
  fixComponentText("Ashfall/Survivor/Detail Panel", [
    ["Name", 44, 326, 180, 36, 30],
    ["Role", 48, 366, 130, 16, 12],
    ["Level", 218, 362, 48, 16, 12]
  ], fixes);
  fixComponentText("Ashfall/Survivor/Skill Row", [
    ["Label", 80, 8, 170, 16, 13],
    ["Description", 80, 30, 230, 13, 10]
  ], fixes);
  fixComponentText("Ashfall/Buildings/Building Card", [
    ["Name", 154, 18, 220, 28, 20],
    ["Level", 154, 52, 120, 15, 11]
  ], fixes);
  return fixes;
}

function fixComponentText(componentName, edits, fixes) {
  const component = page.findOne(node => node.type === "COMPONENT" && node.name === componentName);
  if (!component) return;
  for (const [textName, x, y, w, h, fontSize] of edits) {
    const node = component.findOne(candidate => candidate.type === "TEXT" && candidate.name === textName);
    if (node && setTextBox(node, x, y, w, h, fontSize)) fixes.push(`${componentName}: ${textName}`);
  }
}

function isBackplate(node) {
  const name = String(node.name || "").toLowerCase();
  if (name.includes("portrait") || name.includes("icon") || name.includes("equipment")) return false;
  if (name.includes("panel asset") || name.includes("backplate") || name.includes("background")) return true;
  if (name.includes("minimap") || name.includes("route map") || name.includes("camp overview art")) return true;
  const w = Number(node.width || 0);
  const h = Number(node.height || 0);
  return w >= 280 && h >= 120;
}

function rebuildCombatStructure(frameNode) {
  clear(frameNode);
  frameNode.fills = [solid(palette.paper)];
  const root = frame(frameNode, "Ashfall Structure / Expedition Monitor", 0, 0, 1920, 1080, { fill: palette.paper });
  safe(root);
  addHeader(root, "EXPEDITION MONITOR", "4 player slots vs 4 enemy slots");
  buildMission(root);
  buildEncounter(root);
  buildStatus(root);
  buildZone(root);
  buildLog(root);
  buildPhase(root);
  buildBattlefield(root);
  buildBottom(root);
}

function addHeader(parent, title, subtitle) {
  addText(parent, "Brand", "ASHFALL CAMP", 66, 42, 300, 44, { font: "heading", size: 34, fill: palette.ink });
  addText(parent, "Tagline", "REBUILD * SURVIVE * THRIVE", 68, 86, 220, 18, { font: "bodyMedium", size: 11, fill: palette.teal });
  addText(parent, "Screen Title", title, 690, 34, 540, 42, { font: "heading", size: 34, fill: palette.ink, align: "CENTER" });
  addText(parent, "Screen Subtitle", subtitle, 740, 84, 440, 18, { font: "body", size: 11, fill: palette.muted, align: "CENTER" });
}

function buildMission(parent) {
  const panel = panelFrame(parent, "Mission Header", 28, 118, 560, 216);
  section(panel, "MISSION: SCAVENGE RUN", 24, 18);
  addText(panel, "Subtitle", "Dry Suburb - Ambush Site", 24, 48, 220, 18, { font: "body", size: 11, fill: palette.muted });
  section(panel, "SQUAD 4/4", 24, 92);
  addText(panel, "Squad Summary", "Mara  /  Elias  /  Nika  /  June", 24, 128, 300, 18, { font: "headingMedium", size: 13, fill: palette.ink });
  addText(panel, "Mission Rule", "Compact header only. Detailed combat cards are below.", 24, 160, 310, 18, { font: "body", size: 11, fill: palette.muted });
}

function buildEncounter(parent) {
  const panel = panelFrame(parent, "Encounter", 608, 118, 464, 216);
  section(panel, "ENCOUNTER", 22, 18);
  const slot = frame(panel, "Encounter Illustration Slot", 22, 52, 420, 88, { fill: "#D8C6A9", stroke: "#A48E6B", radius: 6 });
  addText(slot, "Label", "ENCOUNTER ILLUSTRATION SLOT", 0, 31, 420, 20, { font: "heading", size: 14, fill: palette.muted, align: "CENTER" });
  statBox(panel, "ENEMIES", "4", 22, 158, 100);
  statBox(panel, "ROUND", "∞", 134, 158, 100);
  statBox(panel, "VICTORY", "Defeat all", 246, 158, 168);
}

function buildStatus(parent) {
  const panel = panelFrame(parent, "Time Status", 1092, 118, 220, 216);
  section(panel, "TIME & STATUS", 20, 18);
  ["Time", "Threat", "Noise"].forEach((label, index) => {
    addText(panel, label, label, 22, 60 + index * 44, 76, 18, { font: "bodyMedium", size: 11, fill: palette.ink });
    progress(panel, `${label} Track`, `${label} Fill`, 100, 65 + index * 44, 78, 8, [0.55, 0.7, 0.32][index], [palette.teal, palette.red, palette.gold][index]);
  });
}

function buildZone(parent) {
  const panel = panelFrame(parent, "Zone Overview", 1330, 118, 342, 216);
  section(panel, "ZONE OVERVIEW", 20, 18);
  const map = frame(panel, "Minimap Structure Slot", 20, 54, 302, 86, { fill: "#D8C6A9", stroke: "#A48E6B", radius: 6 });
  addText(map, "Label", "MINIMAP", 0, 33, 302, 18, { font: "heading", size: 13, fill: palette.muted, align: "CENTER" });
  addText(panel, "Distance", "DISTANCE LEFT 5", 22, 166, 140, 18, { font: "heading", size: 13, fill: palette.ink });
  addText(panel, "Weather", "CLOUDY", 228, 166, 70, 18, { font: "heading", size: 13, fill: palette.ink, align: "RIGHT" });
}

function buildLog(parent) {
  const panel = panelFrame(parent, "Combat Log", 1690, 118, 202, 216);
  section(panel, "COMBAT LOG", 18, 18);
  ["Round 1 begins", "Mara gains Stealth", "Raider prepares", "Feral Dog closes in"].forEach((line, index) => {
    rect(panel, `Dot ${index}`, 20, 60 + index * 28, 7, 7, index === 2 ? palette.red : palette.teal, 7);
    addText(panel, `Line ${index}`, line, 34, 53 + index * 28, 136, 18, { font: "body", size: 10, fill: palette.ink });
  });
}

function buildPhase(parent) {
  const bar = panelFrame(parent, "Phase Bar", 28, 360, 1864, 70);
  addText(bar, "Label", "COMBAT\nPHASE", 18, 14, 150, 42, { font: "heading", size: 22, fill: palette.ink });
  rect(bar, "Player", 190, 22, 610, 18, palette.green, 4);
  addText(bar, "PT", "PLAYER TURN", 190, 22, 610, 18, { font: "heading", size: 11, fill: "#F7EBD6", align: "CENTER" });
  rect(bar, "Clash", 826, 22, 220, 18, palette.gold, 4);
  addText(bar, "CT", "CLASH", 826, 22, 220, 18, { font: "heading", size: 11, fill: palette.ink, align: "CENTER" });
  rect(bar, "Enemy", 1072, 22, 690, 18, palette.red, 4);
  addText(bar, "ET", "ENEMY TURN", 1072, 22, 690, 18, { font: "heading", size: 11, fill: "#F7EBD6", align: "CENTER" });
}

function buildBattlefield(parent) {
  const field = frame(parent, "Battlefield", 28, 446, 1864, 360, { fill: "#D5C1A0", stroke: "#8F7E65", radius: 8 });
  addText(field, "Left Lane", "PLAYER LANE", 26, 22, 120, 16, { font: "bodyMedium", size: 10, fill: palette.teal });
  addText(field, "Right Lane", "ENEMY LANE", 1730, 22, 100, 16, { font: "bodyMedium", size: 10, fill: palette.red, align: "RIGHT" });
  survivors.forEach((survivor, index) => battleCard(field, survivor, 26 + index * 190, 56, false));
  addText(field, "VS", "VS", 900, 158, 64, 48, { font: "heading", size: 34, fill: palette.lineDark, align: "CENTER" });
  enemies.forEach((enemy, index) => battleCard(field, enemy, 1082 + index * 190, 56, true));
}

function buildBottom(parent) {
  const bottom = panelFrame(parent, "Action Command Bar", 28, 824, 1864, 126);
  rect(bottom, "Energy Icon", 28, 28, 58, 58, palette.gold, 58);
  addText(bottom, "Energy", "4 / 5", 106, 38, 84, 34, { font: "heading", size: 26, fill: palette.ink });
  section(bottom, "ABILITIES", 240, 24);
  ["Aim", "Guard", "Treat", "Move", "Item"].forEach((label, index) => {
    const slot = frame(bottom, `Ability / ${label}`, 240 + index * 72, 58, 48, 48, { fill: "#F2DFC0", stroke: palette.line, radius: 6 });
    rect(slot, "Glyph", 14, 10, 20, 20, palette.gold, 20);
    addText(slot, "Label", label, 0, 34, 48, 10, { font: "body", size: 7, fill: palette.ink, align: "CENTER" });
  });
  addText(bottom, "Info", "Use abilities or move before clash. All battle cards fit inside fixed lane bounds.", 720, 38, 380, 36, { font: "body", size: 11, fill: palette.ink });
  button(bottom, "RETREAT", 1160, 34, 160, palette.orange);
  button(bottom, "USE ITEM", 1350, 34, 160, palette.green);
  button(bottom, "END TURN", 1540, 34, 160, palette.orange);
}

function battleCard(parent, actor, x, y, isEnemy) {
  const card = frame(parent, `${isEnemy ? "Enemy" : "Player"} Battle Card / ${actor.name}`, x, y, 176, 286, {
    fill: isEnemy ? "#F0D1C2" : "#E3EBD5",
    stroke: isEnemy ? palette.red : palette.green,
    radius: 8
  });
  rect(card, "Header", 10, 10, 156, 36, isEnemy ? palette.red : palette.teal, 5);
  addText(card, "Name", actor.name, 18, 15, 100, 18, { font: "heading", size: actor.name.length > 15 ? 12 : 14, fill: "#F8EBD5" });
  addText(card, "Level", isEnemy ? `XP ${actor.xp}` : "LVL 3", 122, 18, 36, 13, { font: "bodyMedium", size: 8, fill: "#F8EBD5", align: "RIGHT" });
  const initials = actor.name.split(" ").map(part => part[0]).join("").slice(0, 2);
  rect(card, "Portrait Structure", 22, 66, 132, 88, isEnemy ? "#D8B6A7" : "#C7D8B9", 5);
  addText(card, "Initials", initials, 22, 94, 132, 30, { font: "heading", size: 24, fill: isEnemy ? palette.red : palette.teal, align: "CENTER" });
  addText(card, "HP", `${actor.hp}/${actor.maxHp}`, 48, 166, 80, 20, { font: "heading", size: 17, fill: palette.ink, align: "CENTER" });
  progress(card, "HP Track", "HP Fill", 22, 196, 132, 8, actor.hp / actor.maxHp, isEnemy ? palette.red : palette.green);
  addText(card, "Tag", actor.tag.toUpperCase(), 22, 214, 120, 14, { font: "bodyMedium", size: 9, fill: isEnemy ? palette.red : palette.green });
  [0, 1, 2].forEach(index => {
    rect(card, `Slot ${index}`, 24 + index * 48, 238, 32, 32, "#22201C", 4);
    addText(card, `Stat ${index}`, String(actor.stats[index]), 24 + index * 48, 270, 32, 12, { font: "bodyMedium", size: 8, fill: palette.ink, align: "CENTER" });
  });
}

function audit(framesToAudit) {
  const result = {};
  for (const frameNode of framesToAudit) {
    const outOfBounds = [];
    walk(frameNode, child => {
      if (child === frameNode || !("x" in child) || !("y" in child) || !("width" in child) || !("height" in child)) return;
      const absolute = absoluteBox(child, frameNode);
      if (absolute.x < -0.5 || absolute.y < -0.5 || absolute.x + absolute.width > frameNode.width + 0.5 || absolute.y + absolute.height > frameNode.height + 0.5) {
        outOfBounds.push({ name: child.name, type: child.type, box: absolute });
      }
    });
    result[frameNode.name] = {
      outOfBounds,
      imageFills: frameNode.findAll(node => "fills" in node && Array.isArray(node.fills) && node.fills.some(fill => fill.type === "IMAGE")).map(node => ({
        name: node.name,
        width: Math.round(node.width || 0),
        height: Math.round(node.height || 0)
      }))
    };
  }

  const combat = framesToAudit.find(frame => frame.name.endsWith("07 Expedition Monitor"));
  const cards = combat.findAll(node => node.name.startsWith("Enemy Battle Card /")).map(node => ({
    name: node.name,
    x: Math.round(node.x),
    y: Math.round(node.y),
    right: Math.round(node.x + node.width),
    bottom: Math.round(node.y + node.height)
  }));

  return {
    frames: result,
    combatEnemyCards: cards,
    combatEnemyOverlap: cards.some((card, index) => cards.some((other, otherIndex) => otherIndex !== index && overlaps(card, other)))
  };
}

function overlaps(a, b) {
  return a.x < b.right && a.right > b.x && a.y < b.bottom && a.bottom > b.y;
}

function walk(node, visit) {
  visit(node);
  if ("children" in node) {
    for (const child of node.children) walk(child, visit);
  }
}

function absoluteBox(node, ancestor) {
  let x = node.x || 0;
  let y = node.y || 0;
  let parent = node.parent;
  while (parent && parent !== ancestor && "x" in parent && "y" in parent) {
    x += parent.x || 0;
    y += parent.y || 0;
    parent = parent.parent;
  }
  return {
    x: Math.round(x * 10) / 10,
    y: Math.round(y * 10) / 10,
    width: Math.round((node.width || 0) * 10) / 10,
    height: Math.round((node.height || 0) * 10) / 10
  };
}

function panelFrame(parent, name, x, y, w, h) {
  return frame(parent, name, x, y, w, h, { fill: palette.paper2, stroke: palette.line, radius: 8 });
}

function statBox(parent, label, value, x, y, w) {
  const box = frame(parent, `Stat / ${label}`, x, y, w, 32, { fill: "#F8E9D0", stroke: palette.line, radius: 4 });
  addText(box, "Label", label, 10, 5, w - 20, 10, { font: "bodyMedium", size: 8, fill: palette.muted, align: "CENTER" });
  addText(box, "Value", value, 10, 16, w - 20, 12, { font: "heading", size: 11, fill: palette.ink, align: "CENTER" });
}

function section(parent, textValue, x, y) {
  addText(parent, `Section ${textValue}`, textValue, x, y, Math.min(220, parent.width - x - 8), 22, { font: "heading", size: 15, fill: palette.teal });
}

function button(parent, label, x, y, w, color) {
  const node = frame(parent, `Action / ${label}`, x, y, w, 44, { fill: color, stroke: "#6A432D", radius: 6 });
  addText(node, "Label", label, 0, 12, w, 18, { font: "heading", size: 14, fill: "#FFF1D9", align: "CENTER" });
}

function cleanupTextSpacing(nonCombatFrames) {
  const fixes = [];
  for (const frameNode of nonCombatFrames) {
    const texts = frameNode.findAll(node => node.type === "TEXT");
    for (const text of texts) {
      const parent = text.parent;
      const parentName = String(parent && parent.name || "");
      const name = String(text.name || "");

      if (name.startsWith("Screen Title /")) {
        setTextBox(text, text.x, 24, text.width, 56, 46);
        fixes.push(`${frameNode.name}: screen title`);
      }
      if (name.startsWith("Screen Subtitle /")) {
        setTextBox(text, text.x, 92, text.width, 18, 13);
        fixes.push(`${frameNode.name}: screen subtitle`);
      }

      if (parentName.startsWith("Resource Pill /")) {
        if (name === "Value") setTextBox(text, text.x, 5, text.width, 18, 14);
        if (name === "Label") setTextBox(text, text.x, 28, text.width, 11, 8);
        fixes.push(`${frameNode.name}: ${parentName}`);
      }

      if (parentName.startsWith("Roster Card /")) {
        if (name === "Name") setTextBox(text, text.x, 8, text.width, 20, 15);
        if (name === "Role") setTextBox(text, text.x, 32, text.width, 13, 10);
        if (name === "Status") setTextBox(text, text.x, 55, text.width, 13, 10);
        fixes.push(`${frameNode.name}: ${parentName}`);
      }

      if (parentName.startsWith("Building Card /")) {
        if (name === "Name") setTextBox(text, text.x, 18, text.width, 28, 20);
        if (name === "Level") setTextBox(text, text.x, 52, text.width, 15, 11);
        fixes.push(`${frameNode.name}: ${parentName}`);
      }

      if (parentName.startsWith("Building Summary /")) {
        if (name === "Name") setTextBox(text, text.x, 16, text.width, 20, 15);
        if (name === "Level") setTextBox(text, text.x, 42, text.width, 13, 10);
        fixes.push(`${frameNode.name}: ${parentName}`);
      }

      if (parentName.startsWith("Recipe /")) {
        if (name === "Name") setTextBox(text, text.x, 18, text.width, 21, 15);
        if (name === "Type") setTextBox(text, text.x, 46, text.width, 12, 10);
        if (name === "Cost") setTextBox(text, text.x, 72, text.width, 12, 10);
        fixes.push(`${frameNode.name}: ${parentName}`);
      }

      if (parentName.startsWith("Enemy Catalog /")) {
        if (name === "Name") setTextBox(text, text.x, 10, text.width, 20, 14);
        if (name === "Id") setTextBox(text, text.x, 36, text.width, 12, 9);
        if (name === "Stats") setTextBox(text, text.x, 58, text.width, 14, 10);
        fixes.push(`${frameNode.name}: ${parentName}`);
      }

      if (parentName.startsWith("Broadcast /")) {
        if (name === "Name") setTextBox(text, text.x, 14, text.width, 21, 15);
        if (name === "Body") setTextBox(text, text.x, 42, text.width, 13, 10);
        fixes.push(`${frameNode.name}: ${parentName}`);
      }

      if (parentName === "Recruit") {
        if (name === "Name") setTextBox(text, text.x, 74, text.width, 32, 28);
        if (name === "Role") setTextBox(text, text.x, 116, text.width, 17, 12);
        fixes.push(`${frameNode.name}: Recruit`);
      }

      if (parentName === "Upgrade Details") {
        if (name === "Name") setTextBox(text, text.x, 74, text.width, 28, 20);
        if (name === "Level") setTextBox(text, text.x, 108, text.width, 15, 11);
        fixes.push(`${frameNode.name}: Upgrade Details`);
      }

      if (parentName === "Empty Slot") {
        if (name === "Plus") setTextBox(text, text.x, 34, text.width, 32, 30);
        if (name === "Label") setTextBox(text, text.x, 78, text.width, 16, 13);
        fixes.push(`${frameNode.name}: Empty Slot`);
      }

      if (parentName === "Camp Snapshot") {
        if (name === "Snapshot Title") setTextBox(text, text.x, 235, text.width, 24, 20);
        if (name === "Snapshot Body") setTextBox(text, text.x, 269, text.width, 30, 11);
        fixes.push(`${frameNode.name}: Camp Snapshot`);
      }
    }

    const portraitChips = frameNode.findAll(node => node.type === "FRAME" && String(node.name || "").startsWith("Portrait Chip /"));
    for (const chip of portraitChips) {
      if (!chip.parent || chip.parent.name !== "Squad") continue;
      try {
        chip.resize(chip.width, 112);
        const label = chip.findOne(node => node.type === "TEXT" && node.name === "Name");
        if (label) setTextBox(label, label.x, 88, label.width, 14, 10);
        fixes.push(`${frameNode.name}: ${chip.name}`);
      } catch (error) {
        skippedTextFixes.push({
          name: chip.name,
          parent: chip.parent ? chip.parent.name : null,
          reason: error && error.message ? error.message : String(error)
        });
      }
    }
  }
  return Array.from(new Set(fixes));
}

function setTextBox(node, x, y, w, h, fontSize) {
  try {
    if (fontSize) node.fontSize = fontSize;
    if ("textAutoResize" in node) node.textAutoResize = "NONE";
    node.x = x;
    node.y = y;
    node.resize(w, h);
    return true;
  } catch (error) {
    skippedTextFixes.push({
      name: node.name,
      parent: node.parent ? node.parent.name : null,
      reason: error && error.message ? error.message : String(error)
    });
    return false;
  }
}

function safe(node) {
  node.clipsContent = true;
}

function clear(parent) {
  for (const child of [...parent.children]) child.remove();
}

function frame(parent, name, x, y, w, h, options = {}) {
  const node = figma.createFrame();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.fills = options.fill ? [solid(options.fill, options.opacity ?? 1)] : [];
  node.strokes = options.stroke ? [solid(options.stroke)] : [];
  node.strokeWeight = options.stroke ? 1 : 0;
  node.cornerRadius = options.radius || 0;
  node.clipsContent = Boolean(options.clip);
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

function progress(parent, trackName, fillName, x, y, w, h, value, color) {
  rect(parent, trackName, x, y, w, h, "#CBB89A", h / 2, 0.7);
  rect(parent, fillName, x, y, Math.max(2, w * Math.max(0, Math.min(1, value))), h, color, h / 2);
}

function addText(parent, name, value, x, y, w, h, options = {}) {
  const node = figma.createText();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.fontName = fonts[options.font || "body"];
  node.fontSize = options.size || 12;
  node.fills = [solid(options.fill || palette.ink, options.opacity ?? 1)];
  node.textAlignHorizontal = options.align || "LEFT";
  node.textAlignVertical = "TOP";
  node.characters = String(value);
  parent.appendChild(node);
  return node;
}

function solid(hex, opacity = 1) {
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
