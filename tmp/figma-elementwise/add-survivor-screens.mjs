import { spawn, spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const outDir = join(process.cwd(), "tmp", "figma-elementwise");
const shotsDir = join(outDir, "screenshots");
mkdirSync(shotsDir, { recursive: true });

const P = (...parts) => join(process.cwd(), ...parts);
const assetPaths = {
  bgSurvivors: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Backgrounds", "ui_bg_survivors_roster_01.png"),
  bgSurvivorDetail: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Backgrounds", "ui_bg_survivor_detail_01.png"),
  filterBar: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Survivors", "ui_bar_survivor_filter.png"),
  portraitFrameLarge: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Survivors", "ui_frame_survivor_portrait_large.png"),
  detailPortrait: P("Assets", "AshfallCamp", "Art", "UI", "Production", "SurvivorDetail", "ui_illustration_survivor_portrait.png"),
  navCamp: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_camp.png"),
  navSurvivors: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_survivors.png"),
  navBuildings: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_buildings.png"),
  navWorkshop: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_workshop.png"),
  navExpedition: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_expedition.png"),
  navRadio: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_radio.png"),
  statusHealthy: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_healthy.png"),
  statusMorale: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_morale.png"),
  statusSafe: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_safe.png"),
  statusWounded: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_wounded.png"),
  statusIdle: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_idle.png"),
  statusFatigue: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_fatigue.png"),
  statusAssigned: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_assigned.png"),
  statusScouting: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_scouting.png"),
  eqRifle: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_rifle.png"),
  eqPistol: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_pistol.png"),
  eqShotgun: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_shotgun.png"),
  eqVest: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_armor_vest.png"),
  eqMedkit: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_medkit.png"),
  eqBackpack: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_backpack.png"),
  eqHatchet: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_hatchet.png"),
  eqMachete: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_machete.png"),
  eqTape: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_duct_tape.png"),
  survivor01: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_01.png"),
  survivor02: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_02.png"),
  survivor03: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_03.png"),
  survivor04: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_04.png"),
  survivor05: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_05.png"),
  survivor06: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_06.png"),
  survivor07: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_07.png"),
  survivor08: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_08.png"),
};

for (const [key, filePath] of Object.entries(assetPaths)) {
  if (!existsSync(filePath)) throw new Error(`Missing image asset ${key}: ${filePath}`);
}

const mcpCmd = `${process.env.APPDATA}\\npm\\figma-console-mcp.cmd`;
const child = spawn("cmd.exe", ["/d", "/s", "/c", mcpCmd], {
  env: { ...process.env, FIGMA_WS_PORT: "9225", NO_COLOR: "1" },
  stdio: ["pipe", "pipe", "pipe"],
  windowsHide: true,
});

let nextId = 1;
let buffer = "";
const pending = new Map();

function send(method, params = {}, timeoutMs = 45000) {
  const id = nextId++;
  child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", id, method, params })}\n`);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      if (!pending.has(id)) return;
      pending.delete(id);
      reject(new Error(`Timeout waiting for ${method}`));
    }, timeoutMs);
    pending.set(id, { resolve, reject, timer });
  });
}

function notify(method, params = {}) {
  child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", method, params })}\n`);
}

function callTool(name, args = {}, timeoutMs = 60000) {
  return send("tools/call", { name, arguments: args }, timeoutMs);
}

function parseJsonContent(result) {
  const text = result?.content?.find((item) => item.type === "text")?.text;
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return { rawText: text };
  }
}

function saveScreenshot(name, result) {
  const image = result?.content?.find((item) => item.type === "image");
  const meta = parseJsonContent(result) || {};
  if (!image?.data) return null;
  const safe = name.replace(/[^a-z0-9]+/gi, "-").replace(/^-|-$/g, "").toLowerCase();
  const filePath = join(shotsDir, `${safe}.png`);
  writeFileSync(filePath, Buffer.from(image.data, "base64"));
  return { filePath, meta };
}

child.stdout.on("data", (chunk) => {
  buffer += chunk.toString("utf8");
  let idx;
  while ((idx = buffer.indexOf("\n")) >= 0) {
    const line = buffer.slice(0, idx).trim();
    buffer = buffer.slice(idx + 1);
    if (!line) continue;
    let msg;
    try {
      msg = JSON.parse(line);
    } catch {
      process.stderr.write(`[server] ${line}\n`);
      continue;
    }
    if (msg.id && pending.has(msg.id)) {
      const call = pending.get(msg.id);
      pending.delete(msg.id);
      clearTimeout(call.timer);
      msg.error ? call.reject(new Error(JSON.stringify(msg.error))) : call.resolve(msg.result);
    }
  }
});

child.stderr.on("data", (chunk) => process.stderr.write(chunk));

function killTree() {
  try { child.stdin.end(); } catch {}
  if (child.pid) spawnSync("taskkill.exe", ["/pid", String(child.pid), "/t", "/f"], { stdio: "ignore" });
}

async function waitForBridge() {
  for (let i = 0; i < 40; i++) {
    const status = parseJsonContent(await callTool("figma_get_status", { probe: true }, 20000)) || {};
    if (status?.setup?.valid === true && status?.setup?.probeResult?.success === true) return status;
    await new Promise((resolve) => setTimeout(resolve, 1500));
  }
  throw new Error("Desktop Bridge did not connect");
}

const figmaCode = String.raw`
await figma.loadAllPagesAsync();
await Promise.all([
  figma.loadFontAsync({ family: "Inter", style: "Regular" }),
  figma.loadFontAsync({ family: "Inter", style: "Medium" }),
  figma.loadFontAsync({ family: "Inter", style: "Semi Bold" }),
  figma.loadFontAsync({ family: "Inter", style: "Bold" }),
]);

const C = {
  ink: "#24211b",
  muted: "#6b5e4d",
  parchment: "#efe2c8",
  parchment2: "#f6ead2",
  line: "#c8ad83",
  line2: "#a98b61",
  teal: "#245f67",
  tealDark: "#15474f",
  green: "#557840",
  greenDark: "#394c2f",
  rust: "#b95c2f",
  gold: "#b7832c",
  red: "#a54834",
  blue: "#5b8791",
  charcoal: "#3a3329",
  white: "#fff8e8",
};

const imageTargets = [];
const frameIds = [];

function hexToRgb(hex) {
  const v = hex.replace("#", "");
  return { r: parseInt(v.slice(0, 2), 16) / 255, g: parseInt(v.slice(2, 4), 16) / 255, b: parseInt(v.slice(4, 6), 16) / 255 };
}

function paint(hex, opacity = 1) {
  return { type: "SOLID", color: hexToRgb(hex), opacity };
}

function frame(parent, name, x, y, w, h, fill = null, strokeHex = null, radius = 0, strokeWeight = 1) {
  const n = figma.createFrame();
  n.name = name;
  n.x = x;
  n.y = y;
  n.resize(w, h);
  n.clipsContent = false;
  n.fills = fill ? [paint(fill)] : [];
  if (strokeHex) {
    n.strokes = [paint(strokeHex)];
    n.strokeWeight = strokeWeight;
  } else {
    n.strokes = [];
  }
  if (radius) n.cornerRadius = radius;
  parent.appendChild(n);
  return n;
}

function rect(parent, name, x, y, w, h, fill = C.parchment2, strokeHex = null, radius = 0, opacity = 1) {
  const n = figma.createRectangle();
  n.name = name;
  n.x = x;
  n.y = y;
  n.resize(w, h);
  n.fills = fill ? [paint(fill, opacity)] : [];
  if (strokeHex) {
    n.strokes = [paint(strokeHex)];
    n.strokeWeight = 1;
  } else {
    n.strokes = [];
  }
  if (radius) n.cornerRadius = radius;
  parent.appendChild(n);
  return n;
}

function line(parent, name, x1, y1, x2, y2, color = C.line, weight = 1) {
  const n = figma.createLine();
  n.name = name;
  n.x = x1;
  n.y = y1;
  n.resize(x2 - x1, y2 - y1);
  n.strokes = [paint(color)];
  n.strokeWeight = weight;
  parent.appendChild(n);
  return n;
}

function txt(parent, name, value, x, y, w, h, size = 16, style = "Regular", color = C.ink, align = "LEFT") {
  const n = figma.createText();
  n.name = name;
  n.fontName = { family: "Inter", style };
  n.fontSize = size;
  n.fills = [paint(color)];
  n.textAlignHorizontal = align;
  n.textAlignVertical = "CENTER";
  n.x = x;
  n.y = y;
  n.resize(w, h);
  n.characters = value;
  parent.appendChild(n);
  return n;
}

function img(parent, key, name, x, y, w, h, scaleMode = "FILL", radius = 0, strokeHex = null, opacity = 1) {
  const n = figma.createRectangle();
  n.name = name;
  n.x = x;
  n.y = y;
  n.resize(w, h);
  n.fills = [];
  n.opacity = opacity;
  if (radius) n.cornerRadius = radius;
  if (strokeHex) {
    n.strokes = [paint(strokeHex)];
    n.strokeWeight = 1;
  } else {
    n.strokes = [];
  }
  parent.appendChild(n);
  imageTargets.push({ key, nodeId: n.id, scaleMode });
  return n;
}

function slider(parent, name, x, y, w, value, color = C.green, h = 8) {
  const g = frame(parent, "Slider - " + name, x, y, w, 22, null);
  rect(g, "Track", 0, 7, w, h, "#d8c5a6", "#b99e75", h / 2);
  rect(g, "Fill", 0, 7, Math.max(8, w * value), h, color, null, h / 2);
  const knob = figma.createEllipse();
  knob.name = "Handle";
  knob.x = Math.max(0, w * value - 6);
  knob.y = 3;
  knob.resize(14, 14);
  knob.fills = [paint(C.parchment2)];
  knob.strokes = [paint(color)];
  knob.strokeWeight = 2;
  g.appendChild(knob);
  return g;
}

function sectionTitle(parent, label, x, y, w) {
  txt(parent, "Title - " + label, label, x, y, w, 30, 20, "Bold", C.teal);
  line(parent, "Rule - " + label, x, y + 34, x + w, y + 34, C.line, 1);
}

function button(parent, label, x, y, w, h, fill = C.green, icon = "") {
  const b = frame(parent, "Button - " + label, x, y, w, h, fill, C.line2, 5, 1);
  txt(b, "Label", icon ? icon + "  " + label : label, 12, 0, w - 24, h, Math.min(18, Math.max(12, h * 0.34)), "Bold", fill === C.green || fill === C.teal || fill === C.red || fill === C.gold || fill === C.blue ? C.white : C.ink, "CENTER");
  return b;
}

function topNav(parent, active) {
  const items = [
    ["CAMP", "navCamp"],
    ["SURVIVORS", "navSurvivors"],
    ["MISSIONS", "navExpedition"],
    ["WORKSHOP", "navWorkshop"],
    ["MAP", "navRadio"],
  ];
  items.forEach((item, i) => {
    const x = 690 + i * 145;
    const selected = item[0] === active;
    const tab = frame(parent, "Nav Tab - " + item[0], x, 34, 110, 76, null);
    img(tab, item[1], "Icon", 39, 0, 32, 32, "FIT", 0, null, selected ? 1 : 0.65);
    txt(tab, "Label", item[0], 0, 40, 110, 24, 13, "Bold", selected ? C.teal : C.charcoal, "CENTER");
    if (selected) rect(tab, "Active Underline", 8, 70, 94, 4, C.teal, null, 2);
  });
}

function brand(parent, compact = false) {
  const g = frame(parent, "Brand", 44, 24, compact ? 420 : 520, 92, null);
  rect(g, "Banner", 0, 0, 84, 92, C.teal, null, 0, 0.88);
  img(g, "navCamp", "Camp Mark", 19, 18, 46, 46, "FIT");
  txt(g, "Name", "ASHFALL CAMP", 110, 0, compact ? 290 : 390, 58, compact ? 35 : 40, "Bold", C.ink);
  txt(g, "Motto", "REBUILD  -  SURVIVE  -  THRIVE", 114, 58, 330, 26, 13, "Bold", C.teal);
  line(g, "Motto Rule Left", 112, 57, 162, 57, C.teal, 1);
  line(g, "Motto Rule Right", compact ? 340 : 438, 57, compact ? 404 : 502, 57, C.teal, 1);
  return g;
}

function pageFrame(name, x, y, bgKey) {
  const f = frame(figma.currentPage, "Ashfall Camp - Full HD Elements / " + name, x, y, 1920, 1080, C.parchment);
  frameIds.push({ title: name, frameId: f.id });
  img(f, bgKey, "Background Art", 0, 0, 1920, 1080, "FILL", 0, null, 0.2);
  rect(f, "Parchment Wash", 0, 0, 1920, 1080, C.parchment, null, 0, 0.78);
  return f;
}

const survivors = [
  { name: "JADE", role: "Scout", level: 12, power: "1,250", hp: 1, hpText: "124 / 124", fatigue: 0.18, fatigueText: "18 / 100", trait: "Keen Eyes", traitText: "+15% scavenge speed", weapon: "Scoped Rifle", portrait: "survivor01", status: "Idle in Camp", color: C.green, icon: "statusScouting", weaponIcon: "eqRifle" },
  { name: "BRUTUS", role: "Brawler", level: 12, power: "1,180", hp: 1, hpText: "168 / 168", fatigue: 0.26, fatigueText: "26 / 100", trait: "Juggernaut", traitText: "+10% melee damage", weapon: "Rebar Club", portrait: "survivor02", status: "On Expedition", color: C.red, icon: "statusHealthy", weaponIcon: "eqMachete" },
  { name: "MIRA", role: "Medic", level: 11, power: "1,100", hp: 1, hpText: "118 / 118", fatigue: 0.2, fatigueText: "20 / 100", trait: "Compassionate", traitText: "+20% healing effect", weapon: "Med Pistol", portrait: "survivor03", status: "Idle in Camp", color: C.green, icon: "eqMedkit", weaponIcon: "eqPistol" },
  { name: "SULLY", role: "Engineer", level: 11, power: "1,050", hp: 1, hpText: "132 / 132", fatigue: 0.22, fatigueText: "22 / 100", trait: "Fixer", traitText: "-15% building time", weapon: "Wrench", portrait: "survivor04", status: "On Expedition", color: C.blue, icon: "eqTape", weaponIcon: "eqHatchet" },
  { name: "RAVEN", role: "Sniper", level: 10, power: "980", hp: 1, hpText: "102 / 102", fatigue: 0.16, fatigueText: "16 / 100", trait: "Silent Hunter", traitText: "+10% critical chance", weapon: "Scoped Rifle", portrait: "survivor05", status: "Idle in Camp", color: C.green, icon: "statusScouting", weaponIcon: "eqRifle" },
  { name: "HANK", role: "Scavenger", level: 9, power: "880", hp: 1, hpText: "110 / 110", fatigue: 0.24, fatigueText: "24 / 100", trait: "Pack Rat", traitText: "+20% loot capacity", weapon: "Crowbar", portrait: "survivor06", status: "On Expedition", color: C.green, icon: "eqBackpack", weaponIcon: "eqHatchet" },
  { name: "DOC", role: "Medic", level: 9, power: "860", hp: 1, hpText: "106 / 106", fatigue: 0.28, fatigueText: "28 / 100", trait: "Veteran Doc", traitText: "+1 medkit charge", weapon: "Med Pistol", portrait: "survivor07", status: "Wounded", color: C.gold, icon: "eqMedkit", weaponIcon: "eqPistol" },
  { name: "LUNA", role: "Leader", level: 9, power: "820", hp: 1, hpText: "108 / 108", fatigue: 0.18, fatigueText: "18 / 100", trait: "Inspiring", traitText: "+10% morale gain", weapon: "Handgun", portrait: "survivor08", status: "Wounded", color: C.teal, icon: "statusMorale", weaponIcon: "eqPistol" },
];

function rosterCard(parent, data, x, y, selected = false) {
  const c = frame(parent, "Survivor Card - " + data.name, x, y, 282, 386, C.parchment2, selected ? C.gold : C.line, 6, selected ? 2 : 1);
  rect(c, "Card Top Rule", 0, 0, 282, 8, selected ? C.gold : C.teal, null, 6, 0.9);
  rect(c, "Portrait Well", 16, 44, 112, 164, "#e2d1b2", C.line, 5, 0.75);
  rect(c, "Stats Well", 136, 132, 120, 64, "#eadabd", C.line, 4, 0.55);
  if (selected) {
    rect(c, "Selected Accent", 0, 0, 282, 386, null, C.gold, 6, 1).strokeWeight = 2;
  }
  img(c, data.portrait, "Portrait", 22, 52, 100, 148, "FILL", 6, C.line);
  txt(c, "Name", data.name, 138, 48, 116, 32, 25, "Bold", C.ink);
  img(c, data.icon, "Role Icon", 140, 88, 20, 20, "FIT");
  txt(c, "Role", data.role, 166, 86, 88, 24, 13, "Semi Bold", C.ink);
  txt(c, "Level Label", "LEVEL " + data.level, 140, 116, 96, 18, 11, "Bold", C.muted);
  txt(c, "Power Label", "POWER", 140, 146, 80, 16, 10, "Bold", C.muted);
  txt(c, "Power", data.power, 140, 164, 90, 26, 20, "Bold", C.ink);
  line(c, "Divider 1", 18, 214, 264, 214, C.line, 1);
  txt(c, "HP Label", "HP", 18, 220, 40, 18, 10, "Bold", C.ink);
  slider(c, "HP", 80, 218, 136, data.hp, C.green, 7);
  txt(c, "HP Value", data.hpText, 214, 218, 54, 18, 10, "Bold", C.ink, "RIGHT");
  txt(c, "Fatigue Label", "FATIGUE", 18, 250, 60, 18, 10, "Bold", C.ink);
  slider(c, "Fatigue", 80, 248, 136, data.fatigue, data.fatigue > 0.25 ? C.gold : C.rust, 7);
  txt(c, "Fatigue Value", data.fatigueText, 214, 248, 54, 18, 10, "Bold", C.ink, "RIGHT");
  line(c, "Divider 2", 18, 280, 264, 280, C.line, 1);
  txt(c, "Trait Label", "TRAIT", 18, 286, 60, 18, 10, "Bold", C.muted);
  txt(c, "Trait", data.trait, 78, 284, 170, 18, 12, "Bold", C.ink);
  txt(c, "Trait Bonus", data.traitText, 78, 302, 178, 18, 11, "Medium", C.muted);
  txt(c, "Weapon Label", "WEAPON", 18, 330, 64, 18, 10, "Bold", C.muted);
  txt(c, "Weapon", data.weapon, 84, 330, 124, 18, 11, "Semi Bold", C.ink);
  img(c, data.weaponIcon, "Weapon Icon", 220, 322, 34, 28, "FIT");
  return c;
}

function filterItem(parent, label, iconKey, y, active = false) {
  const item = frame(parent, "Filter Item - " + label, 0, y, 216, 34, active ? C.green : null, active ? C.greenDark : null, 4);
  if (active) item.opacity = 0.92;
  img(item, iconKey, "Icon", 12, 7, 20, 20, "FIT", 0, null, active ? 1 : 0.62);
  txt(item, "Label", label, 42, 0, 150, 34, 14, active ? "Bold" : "Medium", active ? C.white : C.ink);
}

function buildSurvivors(x, y) {
  const f = pageFrame("06 Survivors Elementwise", x, y, "bgSurvivors");
  brand(f);
  topNav(f, "SURVIVORS");
  const quote = frame(f, "Quote Strip", 1485, 32, 340, 82, C.parchment2, C.line, 4);
  txt(quote, "Copy", "\"We don't just survive out here.\nWe build tomorrow.\"", 30, 10, 260, 46, 15, "Semi Bold", C.teal);
  txt(f, "Screen Title", "8 SURVIVORS", 310, 140, 300, 30, 20, "Bold", C.teal);
  txt(f, "Sort Label", "SORT BY", 1168, 140, 66, 26, 11, "Bold", C.ink, "RIGHT");
  const sort = frame(f, "Dropdown - Sort", 1246, 134, 205, 36, C.parchment2, C.line, 4);
  txt(sort, "Value", "Power (High to Low)", 14, 0, 160, 36, 12, "Medium", C.ink);
  txt(sort, "Arrow", "v", 178, 0, 18, 36, 13, "Bold", C.ink, "CENTER");

  const filters = frame(f, "Left Sidebar - Filters", 38, 140, 230, 850, null);
  sectionTitle(filters, "FILTERS", 26, 0, 160);
  filterItem(filters, "All Survivors", "navSurvivors", 48, true);
  filterItem(filters, "Idle", "statusIdle", 94);
  filterItem(filters, "On Expedition", "statusAssigned", 140);
  filterItem(filters, "Wounded", "statusWounded", 186);
  line(filters, "Split", 0, 246, 216, 246, C.line, 1);
  filterItem(filters, "Best Scavengers", "eqBackpack", 270);
  filterItem(filters, "Best Fighters", "eqMachete", 316);
  filterItem(filters, "Best Crafters", "eqTape", 362);
  filterItem(filters, "Best Medics", "eqMedkit", 408);
  filterItem(filters, "Best Leaders", "statusMorale", 454);
  [["ROLE", "All Roles"], ["STATUS", "All Status"], ["SORT BY", "Power (High to Low)"]].forEach((row, i) => {
    const yy = 560 + i * 86;
    txt(filters, "Label - " + row[0], row[0], 0, yy, 190, 18, 10, "Bold", C.ink);
    const dd = frame(filters, "Dropdown - " + row[0], 0, yy + 25, 210, 34, C.parchment2, C.line, 4);
    txt(dd, "Value", row[1], 12, 0, 150, 34, 12, "Medium", C.ink);
    txt(dd, "Arrow", "v", 176, 0, 20, 34, 12, "Bold", C.ink, "CENTER");
  });
  button(filters, "CLEAR FILTERS", 0, 770, 210, 45, C.parchment2, "C");

  const grid = frame(f, "Survivor Grid", 300, 185, 1166, 810, null);
  survivors.forEach((s, i) => rosterCard(grid, s, (i % 4) * 292, Math.floor(i / 4) * 405, i === 0));

  const side = frame(f, "Right Sidebar", 1492, 140, 386, 850, null);
  const summary = frame(side, "Panel - Roster Summary", 0, 0, 386, 380, C.parchment2, C.line, 6);
  sectionTitle(summary, "ROSTER SUMMARY", 72, 18, 250);
  [
    ["navSurvivors", "TOTAL SURVIVORS", "8"],
    ["statusIdle", "IDLE", "3"],
    ["statusAssigned", "ON EXPEDITION", "3"],
    ["statusWounded", "WOUNDED", "2"],
  ].forEach((row, i) => {
    const yy = 64 + i * 38;
    img(summary, row[0], "Icon - " + row[1], 26, yy + 7, 20, 20, "FIT");
    txt(summary, "Label - " + row[1], row[1], 64, yy, 230, 34, 12, "Bold", C.ink);
    txt(summary, "Value - " + row[1], row[2], 330, yy, 30, 34, 16, "Bold", C.ink, "RIGHT");
  });
  sectionTitle(summary, "TEAM COMPOSITION", 72, 206, 250);
  [
    ["SCOUT", 1, C.green],
    ["FIGHTER", 2, C.red],
    ["MEDIC", 2, C.green],
    ["ENGINEER", 1, C.blue],
    ["LEADER", 1, C.gold],
    ["SCAVENGER", 1, C.teal],
  ].forEach((row, i) => {
    const yy = 246 + i * 22;
    txt(summary, "Team Label - " + row[0], row[0], 28, yy, 92, 18, 10, "Bold", C.ink);
    slider(summary, row[0], 132, yy - 1, 158, row[1] / 3, row[2], 6);
    txt(summary, "Team Count - " + row[0], String(row[1]), 330, yy - 1, 30, 18, 11, "Bold", C.ink, "RIGHT");
  });

  const selected = frame(side, "Panel - Selected Survivor", 0, 400, 386, 380, C.parchment2, C.line, 6);
  sectionTitle(selected, "SELECTED SURVIVOR", 20, 18, 260);
  img(selected, "survivor01", "Selected Portrait", 24, 74, 116, 150, "FILL", 6, C.line);
  txt(selected, "Name", "JADE", 160, 86, 140, 34, 28, "Bold", C.ink);
  img(selected, "statusScouting", "Role Icon", 162, 128, 22, 22, "FIT");
  txt(selected, "Role", "Scout", 190, 126, 90, 26, 14, "Semi Bold", C.ink);
  txt(selected, "Level", "LEVEL 12", 162, 164, 100, 22, 11, "Bold", C.muted);
  txt(selected, "Power Label", "POWER", 162, 196, 80, 18, 10, "Bold", C.muted);
  txt(selected, "Power", "1,250", 162, 216, 90, 26, 20, "Bold", C.ink);
  txt(selected, "Summary", "Quick on her feet and eyes sharp as ever.\nJade finds what others miss.", 24, 250, 330, 46, 13, "Medium", C.ink);
  button(selected, "VIEW DETAILS", 24, 316, 338, 50, C.green, "->");
  const recruit = frame(side, "Button - Recruit Survivor", 0, 800, 386, 58, C.parchment2, C.line, 6);
  img(recruit, "navSurvivors", "Icon", 28, 15, 26, 26, "FIT", 0, null, 0.55);
  txt(recruit, "Label", "RECRUIT SURVIVOR", 78, 0, 240, 58, 16, "Bold", C.muted);
  return f;
}

function skillLine(parent, iconKey, name, desc, value, level, y, color = C.green) {
  const row = frame(parent, "Skill Row - " + name, 0, y, 482, 76, C.parchment2, C.line, 5);
  rect(row, "Icon Plate", 14, 14, 50, 48, "#e4d3b5", C.line, 4, 0.9);
  img(row, iconKey, "Icon", 18, 16, 44, 44, "FIT");
  txt(row, "Name", name, 82, 12, 180, 20, 14, "Bold", C.ink);
  txt(row, "Desc", desc, 82, 34, 220, 18, 11, "Regular", C.ink);
  slider(row, name, 82, 52, 220, value, color, 7);
  txt(row, "Percent", Math.round(value * 100) + "%", 388, 12, 60, 26, 22, "Bold", C.ink, "RIGHT");
  txt(row, "Level", level, 386, 40, 64, 22, 11, "Regular", C.ink, "RIGHT");
}

function equipmentLine(parent, iconKey, label, title, detail, value, y) {
  const row = frame(parent, "Equipment Row - " + label, 0, y, 440, 76, C.parchment2, C.line, 5);
  rect(row, "Icon Plate", 16, 14, 54, 48, "#e4d3b5", C.line, 4, 0.9);
  img(row, iconKey, "Icon", 18, 16, 48, 44, "FIT");
  txt(row, "Label", label, 84, 11, 180, 18, 11, "Bold", C.teal);
  txt(row, "Title", title, 84, 30, 220, 20, 14, "Semi Bold", C.ink);
  txt(row, "Detail", detail, 84, 52, 110, 16, 10, "Regular", C.ink);
  slider(row, "Durability", 196, 50, 126, value, C.green, 6);
}

function profileStat(parent, iconKey, label, valueText, value, y, color = C.green) {
  img(parent, iconKey, "Icon - " + label, 28, y + 4, 28, 28, "FIT");
  txt(parent, "Label - " + label, label, 68, y, 120, 36, 13, "Bold", C.ink);
  slider(parent, label, 210, y + 6, 220, value, color, 8);
  txt(parent, "Value - " + label, valueText, 438, y, 86, 36, 12, "Medium", C.ink, "RIGHT");
}

function buildSurvivorDetail(x, y) {
  const f = pageFrame("07 Survivor Detail Elementwise", x, y, "bgSurvivorDetail");
  brand(f, true);
  txt(f, "Page Title", "SURVIVOR DETAIL", 650, 28, 600, 84, 58, "Bold", C.ink, "CENTER");
  const quote = frame(f, "Quote Strip", 1460, 34, 340, 74, C.parchment2, C.line, 4);
  txt(quote, "Copy", "\"Out here, eyes up\nand ears open.\"", 36, 8, 230, 40, 15, "Semi Bold", C.teal);
  button(f, "X", 1834, 30, 48, 48, C.charcoal, "");

  const profile = frame(f, "Profile Panel", 46, 150, 546, 870, C.parchment2, C.line, 6);
  img(profile, "bgSurvivorDetail", "Profile Scene", 20, 22, 506, 340, "FILL", 5, C.line);
  img(profile, "survivor01", "Jade Portrait", 48, 40, 315, 325, "FILL", 5);
  rect(profile, "Favorite Flag", 24, 24, 44, 66, C.teal, null, 2, 0.92);
  txt(profile, "Favorite Mark", "*", 24, 31, 44, 34, 28, "Bold", C.white, "CENTER");
  txt(profile, "Name", "JADE", 28, 392, 250, 54, 42, "Bold", C.ink);
  img(profile, "statusScouting", "Role Icon", 30, 452, 22, 22, "FIT");
  txt(profile, "Role", "SCOUT", 62, 448, 140, 30, 16, "Bold", C.teal);
  line(profile, "Profile Split", 410, 366, 410, 492, C.line, 1);
  txt(profile, "Level Label", "LEVEL", 438, 390, 70, 18, 12, "Bold", C.teal, "CENTER");
  txt(profile, "Level", "7", 438, 412, 70, 46, 42, "Bold", C.teal, "CENTER");
  txt(profile, "XP", "1,450 XP", 432, 456, 82, 24, 13, "Medium", C.teal, "CENTER");
  line(profile, "Divider", 24, 508, 522, 508, C.line, 1);
  profileStat(profile, "statusIdle", "CURRENT STATE", "Idle in Camp", 1, 528, C.green);
  profileStat(profile, "statusHealthy", "HEALTH", "92 / 100", 0.92, 578, C.green);
  profileStat(profile, "statusFatigue", "FATIGUE", "28 / 100", 0.28, 628, C.teal);
  profileStat(profile, "statusMorale", "MORALE", "78/100   Good", 0.78, 678, C.green);
  img(profile, "statusSafe", "Trait Icon", 30, 752, 30, 30, "FIT");
  txt(profile, "Trait Label", "TRAIT", 72, 742, 120, 22, 14, "Bold", C.ink);
  txt(profile, "Trait Name", "Keen Eye", 72, 768, 170, 24, 18, "Semi Bold", C.teal);
  txt(profile, "Trait Desc", "+15% chance to find extra items while scavenging.", 72, 798, 400, 34, 13, "Regular", C.ink);

  const skills = frame(f, "Panel - Skills", 622, 150, 520, 600, C.parchment2, C.line, 6);
  sectionTitle(skills, "SKILLS", 50, 18, 420);
  [
    ["statusScouting", "SCAVENGING", "Find more and better resources.", 0.82, "Expert", C.green],
    ["eqMachete", "MELEE", "Effectiveness with melee weapons.", 0.64, "Skilled", C.gold],
    ["eqRifle", "FIREARMS", "Accuracy and handling with guns.", 0.57, "Skilled", C.teal],
    ["statusSafe", "SURVIVAL", "Wilderness survival and tracking.", 0.78, "Skilled", C.green],
    ["eqTape", "MECHANICS", "Repair and maintain equipment.", 0.45, "Adept", C.green],
    ["eqMedkit", "MEDICINE", "Treat injuries and illness.", 0.36, "Adept", C.blue],
  ].forEach((row, i) => skillLine(skills, row[0], row[1], row[2], row[3], row[4], 72 + i * 82, row[5]));

  const equipment = frame(f, "Panel - Equipment", 1162, 150, 474, 600, C.parchment2, C.line, 6);
  sectionTitle(equipment, "EQUIPMENT", 48, 18, 380);
  [
    ["eqRifle", "WEAPON", "Hunting Rifle", "Durability", 0.62],
    ["eqVest", "ARMOR", "Light Jacket", "Durability", 0.6],
    ["eqShotgun", "ACCESSORY", "Scout Goggles", "Durability", 0.82],
    ["eqMedkit", "SUPPLIES", "Bandages (3)", "Uses 3/3", 0.72],
    ["eqBackpack", "BACKPACK", "Scout Pack", "Slots 18 / 20", 0.76],
  ].forEach((row, i) => equipmentLine(equipment, row[0], row[1], row[2], row[3], row[4], 72 + i * 96));

  const right = frame(f, "Right Column", 1650, 150, 230, 600, null);
  const wounds = frame(right, "Panel - Wounds Status", 0, 0, 230, 250, C.parchment2, C.line, 6);
  txt(wounds, "Title - Wounds Status", "WOUNDS & STATUS", 18, 18, 194, 24, 15, "Bold", C.teal);
  line(wounds, "Rule - Wounds Status", 18, 52, 208, 52, C.line, 1);
  img(wounds, "statusHealthy", "Healthy Icon", 24, 76, 34, 34, "FIT");
  txt(wounds, "State", "Healthy", 72, 74, 130, 28, 17, "Bold", C.ink);
  txt(wounds, "Desc", "No active wounds.", 72, 104, 130, 24, 12, "Regular", C.ink);
  txt(wounds, "Note", "Well rested and in good condition.", 24, 170, 170, 38, 12, "Regular", C.ink);
  const actions = frame(right, "Panel - Actions", 0, 274, 230, 324, C.parchment2, C.line, 6);
  sectionTitle(actions, "ACTIONS", 18, 14, 190);
  button(actions, "REST", 20, 70, 190, 50, C.green, "BED");
  button(actions, "TREAT", 20, 134, 190, 50, C.blue, "+");
  button(actions, "ASSIGN", 20, 198, 190, 50, C.gold, ">>");
  button(actions, "DISMISS", 20, 262, 190, 50, C.red, "X");

  const history = frame(f, "Panel - Expedition History", 622, 770, 520, 250, C.parchment2, C.line, 6);
  sectionTitle(history, "EXPEDITION HISTORY", 48, 18, 410);
  [
    ["Riverside Gas Station", "Scavenging Run", "May 18, Day 41", "SUCCESS", "14", "3"],
    ["Old Farmstead", "Supply Run", "May 12, Day 35", "SUCCESS", "9", "2"],
    ["Collapsed Highway", "Scout Mission", "May 6, Day 29", "PARTIAL", "6", "1"],
    ["Abandoned Clinic", "Recovery", "Apr 28, Day 22", "SUCCESS", "-", "-"],
  ].forEach((r, i) => {
    const yy = 68 + i * 38;
    img(history, i === 2 ? "statusWounded" : "statusSafe", "Badge " + i, 24, yy + 3, 22, 22, "FIT");
    txt(history, "Location " + i, r[0], 58, yy, 130, 28, 11, "Semi Bold", C.ink);
    txt(history, "Type " + i, r[1], 200, yy, 88, 28, 10, "Regular", C.ink);
    txt(history, "Date " + i, r[2], 300, yy, 82, 28, 10, "Regular", C.ink);
    rect(history, "Status Pill " + i, 390, yy + 6, 54, 18, r[3] === "PARTIAL" ? "#d0ae55" : "#84a05b", null, 3);
    txt(history, "Status " + i, r[3], 390, yy + 6, 54, 18, 8, "Bold", C.ink, "CENTER");
    txt(history, "Loot " + i, r[4] + "   " + r[5], 456, yy, 42, 28, 10, "Semi Bold", C.ink, "RIGHT");
  });
  txt(history, "Link", "View Full Expedition Log", 24, 220, 190, 18, 12, "Semi Bold", C.teal);

  const bio = frame(f, "Panel - Biography Notes", 1162, 770, 474, 250, C.parchment2, C.line, 6);
  sectionTitle(bio, "BIOGRAPHY / NOTES", 48, 18, 370);
  txt(bio, "Paragraph 1", "Jade grew up on the road with her father, learning to read terrain and people.", 28, 68, 410, 42, 12, "Regular", C.ink);
  txt(bio, "Paragraph 2", "She has a knack for finding what others miss. Quick on her feet and calm under pressure, she's become one of our most reliable scouts.", 28, 120, 410, 58, 12, "Regular", C.ink);
  txt(bio, "Paragraph 3", "She keeps a small notebook of maps and sightings - for when we build something worth protecting, she says.", 28, 184, 410, 38, 12, "Regular", C.ink);

  const note = frame(f, "Pinned Memory Photo", 1650, 770, 230, 250, C.parchment2, C.line, 6);
  img(note, "bgSurvivors", "Camp Photo", 16, 18, 198, 140, "FILL", 3, C.line);
  txt(note, "Caption", "Good eyes save lives.\nKeep moving.", 22, 172, 178, 48, 17, "Medium", C.teal, "CENTER");
  return f;
}

const pageName = "Ashfall Camp - Elementwise Concept";
let page = figma.root.children.find(p => p.name === pageName);
if (!page) {
  page = figma.createPage();
  page.name = pageName;
}
await figma.setCurrentPageAsync(page);

const targetNames = new Set([
  "Ashfall Camp - Full HD Elements / 06 Survivors Elementwise",
  "Ashfall Camp - Full HD Elements / 07 Survivor Detail Elementwise",
]);
for (const child of [...page.children]) {
  if (targetNames.has(child.name)) child.remove();
}

buildSurvivors(4080, 1500);
buildSurvivorDetail(0, 2760);

const created = [];
for (const item of frameIds) {
  const node = await figma.getNodeByIdAsync(item.frameId);
  if (node) created.push(node);
}
figma.viewport.scrollAndZoomIntoView(created);
return { pageName, imageTargets, frameIds };
`;

async function main() {
  await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-survivor-screens-ashfall", version: "1.0.0" },
  });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  await waitForBridge();

  const createResult = parseJsonContent(await callTool("figma_execute", { code: figmaCode, timeout: 60000 }, 180000));
  writeFileSync(join(outDir, "add-survivor-screens-result.json"), JSON.stringify(createResult, null, 2));
  if (!createResult?.success) throw new Error(`figma_execute failed: ${JSON.stringify(createResult)}`);

  const groups = new Map();
  for (const target of createResult.result.imageTargets) {
    const key = `${target.key}|${target.scaleMode}`;
    if (!groups.has(key)) groups.set(key, { key: target.key, scaleMode: target.scaleMode, nodeIds: [] });
    groups.get(key).nodeIds.push(target.nodeId);
  }

  const fillResults = [];
  for (const group of groups.values()) {
    const imageData = readFileSync(assetPaths[group.key]).toString("base64");
    const result = parseJsonContent(await callTool("figma_set_image_fill", {
      nodeIds: group.nodeIds,
      imageData,
      scaleMode: group.scaleMode,
    }, 120000));
    fillResults.push({ key: group.key, scaleMode: group.scaleMode, count: group.nodeIds.length, success: result?.success === true });
    if (!result?.success) throw new Error(`image fill failed for ${group.key}: ${JSON.stringify(result)}`);
  }
  writeFileSync(join(outDir, "add-survivor-screens-fill-results.json"), JSON.stringify(fillResults, null, 2));

  const screenshots = [];
  for (const item of createResult.result.frameIds) {
    const shot = await callTool("figma_take_screenshot", {
      nodeId: item.frameId,
      scale: 0.42,
      format: "png",
      includeChildren: true,
    }, 120000);
    screenshots.push({ title: item.title, frameId: item.frameId, ...saveScreenshot(item.title, shot) });
  }
  writeFileSync(join(outDir, "add-survivor-screens-screenshots.json"), JSON.stringify(screenshots, null, 2));

  console.log(JSON.stringify({
    pageName: createResult.result.pageName,
    frames: createResult.result.frameIds,
    imageTargetCount: createResult.result.imageTargets.length,
    screenshots,
  }, null, 2));
}

main().catch((err) => {
  console.error(err);
  process.exitCode = 1;
}).finally(() => {
  killTree();
});
