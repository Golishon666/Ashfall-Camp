import { spawn, spawnSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const outDir = join(process.cwd(), "tmp", "figma-clean-rebuild");
const screenshotsDir = join(outDir, "screenshots");
mkdirSync(screenshotsDir, { recursive: true });

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
    pending.set(id, { resolve, reject, timer, method });
  });
}

function notify(method, params = {}) {
  child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", method, params })}\n`);
}

function callTool(name, args = {}, timeoutMs = 60000) {
  return send("tools/call", { name, arguments: args }, timeoutMs);
}

function parseJsonContent(toolResult) {
  const text = toolResult?.content?.find((item) => item.type === "text")?.text;
  if (!text) return null;
  try {
    return JSON.parse(text);
  } catch {
    return { rawText: text };
  }
}

function saveScreenshot(name, toolResult) {
  const image = toolResult?.content?.find((item) => item.type === "image");
  const meta = parseJsonContent(toolResult) || {};
  if (!image?.data) return null;
  const ext = image.mimeType === "image/jpeg" ? "jpg" : image.mimeType === "image/svg+xml" ? "svg" : "png";
  const safe = name.replace(/[^a-z0-9]+/gi, "-").replace(/^-|-$/g, "").toLowerCase();
  const filePath = join(screenshotsDir, `${safe}.${ext}`);
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
      if (msg.error) call.reject(new Error(JSON.stringify(msg.error)));
      else call.resolve(msg.result);
    } else {
      process.stderr.write(`[notify] ${JSON.stringify(msg)}\n`);
    }
  }
});

child.stderr.on("data", (chunk) => process.stderr.write(chunk));

child.on("exit", (code) => {
  for (const call of pending.values()) {
    clearTimeout(call.timer);
    call.reject(new Error(`Server exited while waiting for ${call.method}, code ${code}`));
  }
  pending.clear();
});

function killServerTree() {
  try {
    child.stdin.end();
  } catch {}
  if (child.pid) {
    spawnSync("taskkill.exe", ["/pid", String(child.pid), "/t", "/f"], { stdio: "ignore" });
  }
}

const buildCode = String.raw`
await figma.loadAllPagesAsync();

async function loadFont(style) {
  try {
    await figma.loadFontAsync({ family: "Inter", style: style });
    return { family: "Inter", style: style };
  } catch (error) {
    await figma.loadFontAsync({ family: "Inter", style: "Regular" });
    return { family: "Inter", style: "Regular" };
  }
}

const FONT_REG = await loadFont("Regular");
const FONT_MED = await loadFont("Medium");
const FONT_SEMI = await loadFont("Semi Bold");
const FONT_BOLD = await loadFont("Bold");

const C = {
  paper: "#efe3c9",
  paper2: "#f7efd9",
  panel: "#f3e6ca",
  panel2: "#ead7b3",
  ink: "#1e211a",
  muted: "#635b4c",
  line: "#9d8a6d",
  line2: "#c7b28d",
  teal: "#245d63",
  teal2: "#dce9e5",
  rust: "#a95435",
  rust2: "#f0d7c9",
  gold: "#b78632",
  green: "#54703a",
  red: "#8f3f31",
  black: "#2a271f",
  white: "#fff8e8"
};

function rgb(hex) {
  const h = hex.replace("#", "");
  return {
    r: parseInt(h.substring(0, 2), 16) / 255,
    g: parseInt(h.substring(2, 4), 16) / 255,
    b: parseInt(h.substring(4, 6), 16) / 255
  };
}

function paint(hex, opacity) {
  return { type: "SOLID", color: rgb(hex), opacity: opacity === undefined ? 1 : opacity };
}

function font(style) {
  if (style === "Bold") return FONT_BOLD;
  if (style === "Semi") return FONT_SEMI;
  if (style === "Medium") return FONT_MED;
  return FONT_REG;
}

function rect(parent, name, x, y, w, h, fill, stroke, radius, sw, opacity) {
  const n = figma.createRectangle();
  n.name = name || "Rect";
  parent.appendChild(n);
  n.x = x; n.y = y; n.resize(w, h);
  n.fills = fill ? [paint(fill, opacity)] : [];
  n.strokes = stroke ? [paint(stroke)] : [];
  n.strokeWeight = sw || (stroke ? 1 : 0);
  n.cornerRadius = radius || 0;
  return n;
}

function line(parent, name, x, y, w, h, color, opacity) {
  return rect(parent, name || "Line", x, y, w, h, color, null, 0, 0, opacity === undefined ? 1 : opacity);
}

function text(parent, name, value, x, y, w, h, size, style, color, align, valign) {
  const t = figma.createText();
  t.name = "TXT / " + name;
  parent.appendChild(t);
  t.x = x; t.y = y; t.resize(w, h);
  t.textAutoResize = "NONE";
  t.fontName = font(style || "Regular");
  t.fontSize = size || 14;
  t.lineHeight = { unit: "PIXELS", value: Math.ceil((size || 14) * 1.22) };
  t.fills = [paint(color || C.ink)];
  t.textAlignHorizontal = align || "LEFT";
  t.textAlignVertical = valign || "TOP";
  t.characters = String(value);
  return t;
}

function frame(parent, name, x, y, w, h, fill) {
  const f = figma.createFrame();
  f.name = name;
  parent.appendChild(f);
  f.x = x; f.y = y; f.resize(w, h);
  f.fills = [paint(fill || C.paper)];
  f.clipsContent = false;
  return f;
}

function component(name, x, y, w, h, fill) {
  const c = figma.createComponent();
  c.name = name;
  figma.currentPage.appendChild(c);
  c.x = x; c.y = y; c.resize(w, h);
  c.fills = fill ? [paint(fill)] : [];
  c.clipsContent = false;
  return c;
}

function inst(comp, parent, name, x, y, w, h) {
  const i = comp.createInstance();
  i.name = name;
  parent.appendChild(i);
  i.x = x; i.y = y;
  if (w && h) i.resize(w, h);
  return i;
}

function panel(parent, title, x, y, w, h) {
  rect(parent, "Panel / " + title, x, y, w, h, C.panel, C.line2, 6, 1);
  line(parent, "Panel top / " + title, x, y, w, 4, C.teal, 1);
  text(parent, "Panel title / " + title, title.toUpperCase(), x + 18, y + 16, w - 36, 24, 18, "Bold", C.teal);
}

function badge(parent, label, x, y, size, fill, color) {
  rect(parent, "Icon badge / " + label, x, y, size, size, fill || C.teal, null, Math.min(7, size / 4), 0);
  text(parent, "Icon badge label / " + label, label, x, y + Math.max(0, (size - 18) / 2), size, 20, Math.min(16, size * 0.45), "Bold", color || C.white, "CENTER");
}

function progress(parent, name, x, y, w, h, value, color) {
  rect(parent, "Progress bg / " + name, x, y, w, h, "#d9c8a8", null, h / 2, 0);
  rect(parent, "Progress fill / " + name, x, y, Math.max(4, w * Math.max(0, Math.min(1, value))), h, color || C.green, null, h / 2, 0);
}

function pill(parent, comps, label, value, x, y, w, icon, color) {
  inst(comps.resourcePill, parent, "Instance / Resource Pill / " + label, x, y, w, 36);
  rect(parent, "Resource icon dot / " + label, x + 10, y + 9, 18, 18, color || C.teal, null, 9, 0);
  text(parent, "Resource icon / " + label, icon, x + 10, y + 9, 18, 16, 10, "Bold", C.white, "CENTER");
  text(parent, "Resource label / " + label, label, x + 34, y + 8, w - 94, 16, 11, "Bold", C.muted);
  text(parent, "Resource value / " + label, value, x + w - 54, y + 8, 44, 16, 12, "Bold", C.ink, "RIGHT");
}

function addButton(parent, label, x, y, w, color, icon) {
  rect(parent, "Button / " + label, x, y, w, 52, color, "#6f4e35", 5, 1);
  text(parent, "Button icon / " + label, icon || ">", x + 16, y + 15, 24, 20, 16, "Bold", C.white, "CENTER");
  text(parent, "Button text / " + label, label, x + 48, y + 15, w - 62, 20, 16, "Bold", C.white);
}

function addFrameBase(parent, comps, title, subtitle, active) {
  rect(parent, "Outer stroke", 20, 20, 1880, 1040, C.paper, "#5b5144", 10, 2);
  rect(parent, "Top paper band", 20, 20, 1880, 90, C.paper2, "#d4bf98", 10, 1);
  badge(parent, "AC", 40, 34, 54, C.teal, C.white);
  text(parent, "Game title", "ASHFALL CAMP", 112, 32, 280, 30, 26, "Bold", C.black);
  text(parent, "Motto", "REBUILD  SURVIVE  THRIVE", 114, 66, 280, 18, 12, "Bold", C.teal);
  text(parent, "Screen title / " + title, title.toUpperCase(), 446, 28, 520, 42, 32, "Bold", C.black, "CENTER");
  text(parent, "Screen subtitle / " + title, subtitle, 446, 72, 520, 18, 12, "Medium", C.teal, "CENTER");
  const resources = [
    ["Scrap", "125", "S", C.black],
    ["Food", "8/50", "F", C.green],
    ["Water", "6/40", "W", C.teal],
    ["Meds", "1/20", "M", C.red],
    ["Parts", "0", "P", C.gold]
  ];
  let rx = 1112;
  for (let r = 0; r < resources.length; r++) {
    pill(parent, comps, resources[r][0], resources[r][1], rx, 40, 138, resources[r][2], resources[r][3]);
    rx += 146;
  }
  const tabs = ["Camp", "Survivors", "Buildings", "Expeditions", "Combat", "Workshop", "Radio", "Report"];
  const navX = 294, navY = 1002, tabW = 166;
  inst(comps.navBase, parent, "Instance / Bottom Nav Base", navX, navY, 1332, 58);
  for (let i = 0; i < tabs.length; i++) {
    const tx = navX + 8 + i * tabW;
    if (tabs[i] === active) inst(comps.currentTab, parent, "Instance / Current Tab / " + active, tx - 2, navY + 7, tabW - 8, 44);
    text(parent, "Nav / " + tabs[i], tabs[i].toUpperCase(), tx, navY + 22, tabW - 12, 16, 12, "Bold", tabs[i] === active ? C.white : "#d7c39d", "CENTER");
  }
}

function portrait(parent, name, x, y, w, h, color) {
  rect(parent, "Portrait panel / " + name, x, y, w, h, "#dbc8a8", C.line, 5, 1);
  rect(parent, "Portrait sky / " + name, x + 8, y + 8, w - 16, h - 16, "#cad5c7", null, 4, 0);
  rect(parent, "Portrait coat / " + name, x + w * 0.28, y + h * 0.54, w * 0.44, h * 0.32, color || C.green, null, 8, 0);
  rect(parent, "Portrait head / " + name, x + w * 0.37, y + h * 0.25, w * 0.26, w * 0.26, "#bd8b61", null, w * 0.13, 0);
  rect(parent, "Portrait scarf / " + name, x + w * 0.25, y + h * 0.49, w * 0.50, h * 0.08, C.teal, null, 4, 0);
  text(parent, "Portrait initials / " + name, name.substring(0, 2).toUpperCase(), x + 12, y + h - 40, w - 24, 28, 22, "Bold", C.white, "CENTER");
}

function statRow(parent, label, value, x, y, w, color) {
  text(parent, "Stat label / " + label, label, x, y, 120, 18, 13, "Bold", C.muted);
  progress(parent, "Stat / " + label, x + 128, y + 4, w - 180, 10, value, color || C.green);
  text(parent, "Stat value / " + label, Math.round(value * 100) + "%", x + w - 48, y, 48, 18, 13, "Bold", C.ink, "RIGHT");
}

function buildingCard(parent, comps, b, x, y) {
  inst(comps.buildingCard, parent, "Instance / Building Card / " + b.name, x, y, 470, 316);
  badge(parent, b.icon, x + 18, y + 18, 42, b.color, C.white);
  text(parent, "Building name / " + b.name, b.name.toUpperCase(), x + 76, y + 18, 250, 24, 20, "Bold", C.teal);
  text(parent, "Building level / " + b.name, "Level " + b.level + "  /  " + b.status, x + 76, y + 47, 250, 18, 12, "Medium", C.black);
  rect(parent, "Building thumbnail placeholder / " + b.name, x + 20, y + 78, 176, 116, b.tint, C.line2, 4, 1);
  badge(parent, b.icon, x + 82, y + 108, 54, b.color, C.white);
  text(parent, "Building effects / " + b.name, "EFFECTS", x + 220, y + 82, 170, 16, 12, "Bold", C.teal);
  text(parent, "Building effect 1 / " + b.name, b.effect1, x + 220, y + 106, 218, 38, 13, "Regular", C.ink);
  text(parent, "Building effect 2 / " + b.name, b.effect2, x + 220, y + 150, 218, 38, 13, "Regular", C.ink);
  text(parent, "Building condition label / " + b.name, "Condition", x + 20, y + 216, 90, 16, 12, "Bold", C.muted);
  progress(parent, "Building condition / " + b.name, x + 112, y + 220, 150, 8, b.condition, C.green);
  text(parent, "Building condition value / " + b.name, Math.round(b.condition * 100) + "%", x + 272, y + 212, 48, 20, 12, "Bold", C.ink);
  text(parent, "Building cost / " + b.name, "UPGRADE  " + b.cost, x + 20, y + 258, 225, 18, 12, "Bold", C.black);
  addButton(parent, "UPGRADE", x + 286, y + 244, 154, C.rust, ">");
}

function battleCard(parent, comps, side, data, x, y) {
  const isEnemy = side === "enemy";
  inst(isEnemy ? comps.enemyBattleCard : comps.playerBattleCard, parent, "Instance / " + side + " battle card / " + data.name, x, y, 178, 330);
  const accent = isEnemy ? C.red : C.teal;
  text(parent, "Battle name / " + data.name, data.name, x + 14, y + 14, 114, 20, 14, "Bold", C.white);
  text(parent, "Battle role / " + data.name, data.role, x + 14, y + 34, 104, 15, 9, "Bold", isEnemy ? C.rust2 : C.teal2);
  text(parent, "Battle level / " + data.name, data.level, x + 134, y + 16, 30, 16, 10, "Bold", C.white, "RIGHT");
  portrait(parent, data.name, x + 18, y + 60, 142, 132, data.color || accent);
  text(parent, "Battle HP / " + data.name, data.hp, x + 22, y + 196, 134, 24, 21, "Bold", C.white, "CENTER");
  progress(parent, "Battle HP bar / " + data.name, x + 22, y + 226, 134, 9, data.hpv, isEnemy ? C.rust : C.green);
  text(parent, "Battle trait / " + data.name, data.trait, x + 24, y + 246, 128, 16, 11, "Bold", isEnemy ? C.rust : C.green);
  for (let i = 0; i < 3; i++) {
    rect(parent, "Battle slot / " + data.name + " / " + i, x + 22 + i * 45, y + 270, 34, 34, C.black, C.line, 3, 1);
    text(parent, "Battle slot icon / " + data.name + " / " + i, data.icons[i], x + 22 + i * 45, y + 279, 34, 15, 11, "Bold", C.gold, "CENTER");
  }
  text(parent, "Battle stat a / " + data.name, data.stats[0], x + 18, y + 310, 42, 16, 12, "Bold", C.white, "CENTER");
  text(parent, "Battle stat b / " + data.name, data.stats[1], x + 68, y + 310, 42, 16, 12, "Bold", C.white, "CENTER");
  text(parent, "Battle stat c / " + data.name, data.stats[2], x + 118, y + 310, 42, 16, 12, "Bold", C.white, "CENTER");
}

const cleanPrefix = "Ashfall Camp - Clean Full HD";
const cleanupPages = figma.root.children.filter(function(p) { return p.name.indexOf(cleanPrefix) === 0; });
const fallbackPage = figma.root.children.find(function(p) { return p.name.indexOf(cleanPrefix) !== 0; });
if (fallbackPage) await figma.setCurrentPageAsync(fallbackPage);
for (let i = 0; i < cleanupPages.length; i++) {
  if (figma.root.children.length > 1) cleanupPages[i].remove();
}
const existingNames = figma.root.children.map(function(page) { return page.name; });
let pageName = cleanPrefix;
if (existingNames.indexOf(pageName) >= 0) {
  pageName = pageName + " " + new Date().toISOString().slice(0, 10) + " " + new Date().toTimeString().slice(0, 5).replace(":", "-");
}
const page = figma.createPage();
page.name = pageName;
await figma.setCurrentPageAsync(page);

const comps = {};
const compX = -2200;
text(page, "Component heading", "ASHFALL CAMP COMPONENTS", compX, -70, 680, 30, 24, "Bold", C.black);
text(page, "Component note", "Structure-first rebuild. No image fills or production backplates; only Figma primitives and real catalog names.", compX, -34, 780, 22, 13, "Medium", C.muted);

comps.resourcePill = component("Ashfall/Common/Resource Pill Shell", compX, 20, 138, 36, null);
rect(comps.resourcePill, "Pill bg", 0, 0, 138, 36, "#efe2c2", "#7e705d", 6, 1);
rect(comps.resourcePill, "Pill shade", 4, 4, 130, 28, "#fff5dc", null, 4, 0, 0.62);

comps.navBase = component("Ashfall/Common/Bottom Nav Base", compX, 78, 1332, 58, null);
rect(comps.navBase, "Nav bg", 0, 0, 1332, 58, C.black, "#6b5c48", 6, 1);
line(comps.navBase, "Nav top rule", 8, 6, 1316, 2, C.gold, 0.65);

comps.currentTab = component("Ashfall/Common/Current Tab", compX, 160, 158, 44, null);
rect(comps.currentTab, "Current bg", 0, 0, 158, 44, C.teal, "#bfa56f", 5, 1);
line(comps.currentTab, "Current shine", 8, 6, 142, 2, C.white, 0.35);

comps.playerBattleCard = component("Ashfall/Combat/Player Battle Card Shell", compX, 230, 178, 330, null);
rect(comps.playerBattleCard, "Card bg", 0, 0, 178, 330, "#263d35", "#1c231c", 5, 2);
rect(comps.playerBattleCard, "Card paper", 8, 54, 162, 206, "#d8c39e", "#867455", 3, 1);
rect(comps.playerBattleCard, "Header", 8, 8, 162, 42, C.teal, "#1b3c40", 3, 1);
rect(comps.playerBattleCard, "Footer", 8, 304, 162, 20, C.black, null, 2, 0);

comps.enemyBattleCard = component("Ashfall/Combat/Enemy Battle Card Shell", compX + 210, 230, 178, 330, null);
rect(comps.enemyBattleCard, "Card bg", 0, 0, 178, 330, "#4b2a22", "#211817", 5, 2);
rect(comps.enemyBattleCard, "Card paper", 8, 54, 162, 206, "#d8c39e", "#867455", 3, 1);
rect(comps.enemyBattleCard, "Header", 8, 8, 162, 42, C.red, "#4c201b", 3, 1);
rect(comps.enemyBattleCard, "Footer", 8, 304, 162, 20, C.black, null, 2, 0);

comps.buildingCard = component("Ashfall/Buildings/Building Card Shell", compX, 600, 470, 316, null);
rect(comps.buildingCard, "Building card bg", 0, 0, 470, 316, C.panel, C.line2, 6, 1);
line(comps.buildingCard, "Building top", 0, 0, 470, 4, C.teal, 1);
rect(comps.buildingCard, "Upgrade footer", 0, 238, 470, 78, "#ead8b7", null, 0, 0, 0.75);

const survivors = [
  { name: "Mara", role: "Scavenger", trait: "Careful", hp: "92/100", hpv: 0.92, color: "#6f7a49", weapon: "Rusty Knife", skills: [["Scavenging", .8], ["Survival", .4], ["Melee", .2]] },
  { name: "Elias", role: "Ex-cop", trait: "Brave", hp: "100/100", hpv: 1, color: "#315e70", weapon: "Rusty Revolver", skills: [["Firearms", .8], ["Medicine", .2], ["Survival", .2]] },
  { name: "Nika", role: "Mechanic", trait: "Careful", hp: "86/100", hpv: .86, color: "#69513c", weapon: "Metal Pipe", skills: [["Mechanics", .8], ["Scavenging", .2], ["Survival", .2]] },
  { name: "June", role: "Nurse", trait: "Lucky", hp: "78/100", hpv: .78, color: "#b8673f", weapon: "Rusty Knife", skills: [["Medicine", .8], ["Survival", .2], ["Melee", .2]] },
  { name: "Tomas", role: "Brawler", trait: "Tough", hp: "98/100", hpv: .98, color: "#7d4c33", weapon: "Metal Pipe", skills: [["Melee", .8], ["Survival", .2], ["Scavenging", .0]] },
  { name: "Rhea", role: "Hunter", trait: "Quiet", hp: "88/100", hpv: .88, color: "#546a42", weapon: "Hunting Rifle", skills: [["Firearms", .6], ["Survival", .6], ["Scavenging", .2]] },
  { name: "Kade", role: "Scavenger", trait: "Greedy", hp: "84/100", hpv: .84, color: "#745f3a", weapon: "Machete", skills: [["Scavenging", .6], ["Melee", .4], ["Mechanics", .2]] }
];

const enemies = [
  { name: "Feral Dog", role: "Beast", level: "XP 4", hp: "14/14", hpv: 1, trait: "FAST", stats: ["3", "0", "8"], icons: ["B", "A", "P"], color: "#7a613f" },
  { name: "Starving Survivor", role: "Human", level: "XP 5", hp: "18/18", hpv: 1, trait: "DESPERATE", stats: ["4", "0", "4"], icons: ["K", "C", "S"], color: "#82654a" },
  { name: "Raider", role: "Human", level: "XP 10", hp: "30/30", hpv: 1, trait: "RANGED", stats: ["6", "1", "5"], icons: ["R", "A", "P"], color: "#6d3f33" },
  { name: "Mutant Stray", role: "Beast", level: "XP 8", hp: "26/26", hpv: 1, trait: "PACK", stats: ["5", "1", "6"], icons: ["B", "H", "P"], color: "#6b5148" }
];

const buildings = [
  { name: "Barracks", icon: "B", level: 2, status: "Squad + survivor cap", effect1: "Increases survivor capacity.", effect2: "Unlocks larger expedition squads.", cost: "Scrap 60 / Food 8", condition: .82, color: C.teal, tint: "#d1c2a0" },
  { name: "Workshop", icon: "W", level: 2, status: "Craft and repair", effect1: "Enables item crafting and repairs.", effect2: "Improves weapon durability flow.", cost: "Scrap 90 / Parts 4", condition: .78, color: C.gold, tint: "#cab893" },
  { name: "Water Collector", icon: "H2", level: 3, status: "Water production", effect1: "Produces water each minute.", effect2: "Raises water storage cap.", cost: "Scrap 160 / Parts 15", condition: .88, color: C.teal, tint: "#c4d6d0" },
  { name: "Mushroom Beds", icon: "MB", level: 2, status: "Food production", effect1: "Produces food each minute.", effect2: "Raises food storage cap.", cost: "Scrap 75 / Parts 5", condition: .74, color: C.green, tint: "#cfd5aa" },
  { name: "Infirmary", icon: "I", level: 1, status: "Recovery support", effect1: "Expands medicine storage.", effect2: "Supports wounds and illness loops.", cost: "Scrap 50 / Med 2", condition: .70, color: C.red, tint: "#d9c5b7" },
  { name: "Radio Tower", icon: "R", level: 1, status: "Recruitment and intel", effect1: "Unlocks survivor recruitment.", effect2: "Improves mission discovery.", cost: "Scrap 45 / Parts 2", condition: .66, color: C.black, tint: "#c7c4ad" }
];

const frames = [];
function addScreen(name, subtitle, active, x, y) {
  const f = frame(page, "Ashfall Camp - Full HD / " + name, x, y, 1920, 1080, C.paper);
  addFrameBase(f, comps, name, subtitle, active);
  frames.push(f);
  return f;
}

// 01 Camp Dashboard
let f = addScreen("Camp Dashboard", "Day 7 / Morning cycle", "Camp", 0, 0);
panel(f, "Camp Summary", 40, 136, 340, 838);
const summaryRows = [["Population", "7 / 12"], ["Food", "8 / 50"], ["Water", "6 / 40"], ["Medicine", "1 / 20"], ["Morale", "Stable"], ["Threat", "Low"], ["Idle Survivors", "2"]];
for (let i = 0; i < summaryRows.length; i++) {
  const yy = 192 + i * 54;
  badge(f, summaryRows[i][0].substring(0, 1), 62, yy, 30, i < 4 ? C.teal : C.gold, C.white);
  text(f, "Summary label " + i, summaryRows[i][0], 104, yy + 5, 150, 18, 14, "Medium", C.ink);
  text(f, "Summary value " + i, summaryRows[i][1], 262, yy + 5, 82, 18, 14, "Bold", C.ink, "RIGHT");
}
panel(f, "Base Operations", 410, 136, 930, 464);
rect(f, "Camp map structural placeholder", 442, 196, 560, 340, "#d7c5a4", C.line2, 6, 1);
for (let i = 0; i < buildings.length; i++) {
  const bx = 476 + (i % 3) * 168;
  const by = 235 + Math.floor(i / 3) * 142;
  badge(f, buildings[i].icon, bx, by, 46, buildings[i].color, C.white);
  text(f, "Map building " + buildings[i].name, buildings[i].name, bx - 36, by + 56, 118, 20, 11, "Bold", C.ink, "CENTER");
}
text(f, "Operational state", "Core loops visible at a glance: resource pressure, survivor readiness, building condition, and mission queue.", 1036, 200, 250, 84, 16, "Medium", C.ink);
statRow(f, "Camp Morale", .72, 1036, 320, 250, C.green);
statRow(f, "Security", .58, 1036, 360, 250, C.gold);
statRow(f, "Supplies", .44, 1036, 400, 250, C.rust);
panel(f, "Priority Tasks", 410, 624, 930, 350);
const tasks = [["Repair Workshop bench", "Needs Scrap 35"], ["Scout Riverside Gas Station", "Squad 4/4 ready"], ["Treat minor wounds", "June assigned"], ["Upgrade Water Collector", "Parts missing"]];
for (let i = 0; i < tasks.length; i++) {
  rect(f, "Task row " + i, 442, 680 + i * 62, 856, 48, i === 1 ? "#e8ead8" : "#f8ecd2", C.line2, 4, 1);
  badge(f, String(i + 1), 458, 690 + i * 62, 28, i === 1 ? C.green : C.teal, C.white);
  text(f, "Task title " + i, tasks[i][0], 500, 689 + i * 62, 360, 18, 14, "Bold", C.ink);
  text(f, "Task meta " + i, tasks[i][1], 500, 709 + i * 62, 360, 16, 12, "Medium", C.muted);
  addButton(f, i === 1 ? "START" : "QUEUE", 1126, 684 + i * 62, 140, i === 1 ? C.green : C.teal, ">");
}
panel(f, "Survivor Watch", 1370, 136, 510, 838);
for (let i = 0; i < 5; i++) {
  const s = survivors[i];
  rect(f, "Watch survivor row " + s.name, 1402, 196 + i * 116, 446, 94, "#f8ecd2", C.line2, 5, 1);
  portrait(f, s.name, 1416, 208 + i * 116, 64, 64, s.color);
  text(f, "Watch name " + s.name, s.name, 1496, 212 + i * 116, 120, 20, 15, "Bold", C.ink);
  text(f, "Watch role " + s.name, s.role + " / " + s.trait, 1496, 234 + i * 116, 160, 18, 12, "Medium", C.teal);
  progress(f, "Watch hp " + s.name, 1496, 262 + i * 116, 210, 9, s.hpv, C.green);
  text(f, "Watch hp text " + s.name, s.hp, 1714, 255 + i * 116, 70, 18, 12, "Bold", C.ink, "RIGHT");
}

// 02 Survivors
f = addScreen("Survivors", "Roster and survivor detail", "Survivors", 2040, 0);
panel(f, "Roster", 40, 136, 500, 838);
for (let i = 0; i < survivors.length; i++) {
  const s = survivors[i];
  const yy = 194 + i * 104;
  rect(f, "Roster row " + s.name, 70, yy, 438, 82, i === 0 ? "#e7ead8" : "#f8ecd2", i === 0 ? C.green : C.line2, 5, 1);
  portrait(f, s.name, 84, yy + 10, 58, 58, s.color);
  text(f, "Roster name " + s.name, s.name, 156, yy + 12, 118, 20, 16, "Bold", C.ink);
  text(f, "Roster role " + s.name, s.role + " / " + s.trait, 156, yy + 36, 160, 16, 12, "Medium", C.teal);
  progress(f, "Roster hp " + s.name, 318, yy + 20, 116, 8, s.hpv, C.green);
  text(f, "Roster hp value " + s.name, s.hp, 318, yy + 36, 116, 16, 11, "Bold", C.ink, "RIGHT");
}
panel(f, "Mara Detail", 570, 136, 770, 838);
portrait(f, "Mara large", 610, 198, 286, 326, survivors[0].color);
text(f, "Detail name", "MARA", 928, 204, 220, 46, 42, "Bold", C.black);
text(f, "Detail class", "SCAVENGER  /  CAREFUL", 932, 256, 260, 18, 14, "Bold", C.teal);
text(f, "Detail loadout", "Weapon: Rusty Knife\nTrait: Careful\nStatus: Idle in Camp", 932, 310, 320, 92, 16, "Medium", C.ink);
statRow(f, "Health", .92, 610, 560, 650, C.green);
statRow(f, "Fatigue", .28, 610, 606, 650, C.teal);
statRow(f, "Morale", .78, 610, 652, 650, C.green);
text(f, "Skill heading", "SKILLS", 610, 724, 180, 22, 18, "Bold", C.teal);
const maraSkills = [["Scavenging", .8], ["Survival", .4], ["Melee", .2], ["Firearms", .0], ["Mechanics", .0], ["Medicine", .0]];
for (let i = 0; i < maraSkills.length; i++) {
  const sx = 610 + (i % 2) * 330;
  const sy = 766 + Math.floor(i / 2) * 52;
  statRow(f, maraSkills[i][0], maraSkills[i][1], sx, sy, 280, maraSkills[i][1] > .45 ? C.green : C.gold);
}
panel(f, "Equipment and Actions", 1370, 136, 510, 838);
const eq = [["Weapon", "Rusty Knife"], ["Armor", "Leather Jacket"], ["Support", "Backpack"], ["Supply", "Medkit"]];
for (let i = 0; i < eq.length; i++) {
  rect(f, "Equipment row " + i, 1404, 198 + i * 98, 444, 72, "#f8ecd2", C.line2, 5, 1);
  badge(f, eq[i][0].substring(0, 1), 1422, 214 + i * 98, 38, i === 0 ? C.rust : C.teal, C.white);
  text(f, "Equipment label " + i, eq[i][0].toUpperCase(), 1476, 208 + i * 98, 110, 16, 11, "Bold", C.teal);
  text(f, "Equipment value " + i, eq[i][1], 1476, 228 + i * 98, 230, 22, 15, "Bold", C.ink);
}
addButton(f, "REST", 1404, 640, 210, C.green, "<");
addButton(f, "TREAT", 1636, 640, 210, C.teal, "+");
addButton(f, "ASSIGN", 1404, 710, 210, C.gold, ">");
addButton(f, "DISMISS", 1636, 710, 210, C.rust, "X");

// 03 Buildings
f = addScreen("Buildings", "Production, support, utility", "Buildings", 4080, 0);
panel(f, "Capacity", 40, 136, 330, 838);
const capRows = [["Total Buildings", "6 / 12"], ["Survivor Cap", "3"], ["Squad Size", "2"], ["Water Cap", "100"], ["Food Cap", "110"], ["Medicine Cap", "30"]];
for (let i = 0; i < capRows.length; i++) {
  text(f, "Capacity label " + i, capRows[i][0], 72, 204 + i * 58, 170, 18, 14, "Medium", C.ink);
  text(f, "Capacity value " + i, capRows[i][1], 260, 204 + i * 58, 70, 18, 15, "Bold", C.black, "RIGHT");
}
text(f, "Building note", "Building cards use icon/card layout only. Production thumbnails are intentionally not used in this structure-first pass.", 72, 646, 250, 90, 14, "Medium", C.muted);
for (let i = 0; i < buildings.length; i++) {
  buildingCard(f, comps, buildings[i], 410 + (i % 3) * 490, 148 + Math.floor(i / 3) * 372);
}

// 04 Expedition Setup
f = addScreen("Expedition Setup", "Choose zone, squad, and policy", "Expeditions", 0, 1160);
panel(f, "Available Zones", 40, 136, 430, 838);
const zones = [["Riverside Gas Station", "Scavenge / Ambush risk"], ["Old Farmstead", "Food / Low threat"], ["Collapsed Highway", "Parts / Raiders"], ["Abandoned Clinic", "Medicine / Infection"]];
for (let i = 0; i < zones.length; i++) {
  rect(f, "Zone row " + i, 72, 202 + i * 132, 366, 104, i === 0 ? "#e7ead8" : "#f8ecd2", i === 0 ? C.green : C.line2, 5, 1);
  badge(f, String(i + 1), 92, 226 + i * 132, 42, i === 0 ? C.green : C.teal, C.white);
  text(f, "Zone name " + i, zones[i][0], 152, 216 + i * 132, 210, 20, 15, "Bold", C.ink);
  text(f, "Zone meta " + i, zones[i][1], 152, 244 + i * 132, 230, 36, 12, "Medium", C.muted);
}
panel(f, "Mission Plan", 500, 136, 820, 838);
rect(f, "Route map structural", 536, 202, 748, 250, "#d4c29e", C.line2, 5, 1);
for (let i = 0; i < 6; i++) {
  rect(f, "Route node " + i, 604 + i * 112, 314 + (i % 2) * 26, 18, 18, i === 0 ? C.green : i === 5 ? C.red : C.white, C.teal, 9, 2);
  if (i < 5) line(f, "Route segment " + i, 622 + i * 112, 322 + (i % 2) * 26, 104, 3, C.teal, .65);
}
text(f, "Mission title", "SCAVENGE RUN / RIVERSIDE GAS STATION", 536, 486, 480, 28, 20, "Bold", C.black);
text(f, "Mission copy", "Primary goal: recover Scrap, Food, Water, and possible Weapon Parts. Keep noise low; threat escalates if the squad delays.", 536, 526, 700, 58, 16, "Medium", C.ink);
const planStats = [["Distance", "5"], ["Threat", "High"], ["Round Limit", "None"], ["Weather", "Cloudy"]];
for (let i = 0; i < planStats.length; i++) {
  rect(f, "Plan stat " + i, 536 + (i % 2) * 360, 620 + Math.floor(i / 2) * 76, 320, 52, "#f8ecd2", C.line2, 4, 1);
  text(f, "Plan label " + i, planStats[i][0].toUpperCase(), 554 + (i % 2) * 360, 632 + Math.floor(i / 2) * 76, 140, 14, 11, "Bold", C.teal);
  text(f, "Plan value " + i, planStats[i][1], 720 + (i % 2) * 360, 626 + Math.floor(i / 2) * 76, 100, 24, 18, "Bold", C.black, "RIGHT");
}
addButton(f, "START EXPEDITION", 970, 844, 300, C.rust, ">");
panel(f, "Squad 4/4", 1350, 136, 530, 838);
for (let i = 0; i < 4; i++) {
  const s = survivors[i];
  rect(f, "Squad setup " + s.name, 1384, 202 + i * 142, 462, 112, "#f8ecd2", C.line2, 5, 1);
  portrait(f, s.name, 1402, 218 + i * 142, 78, 78, s.color);
  text(f, "Squad setup name " + s.name, s.name, 1502, 218 + i * 142, 120, 22, 17, "Bold", C.ink);
  text(f, "Squad setup meta " + s.name, s.role + " / " + s.weapon, 1502, 244 + i * 142, 220, 18, 12, "Medium", C.teal);
  progress(f, "Squad setup hp " + s.name, 1502, 278 + i * 142, 180, 8, s.hpv, C.green);
  text(f, "Squad setup hp text " + s.name, s.hp, 1696, 270 + i * 142, 90, 18, 12, "Bold", C.ink, "RIGHT");
}

// 05 Combat
f = frame(page, "Ashfall Camp - Full HD / Expedition Monitor", 2040, 1160, 1920, 1080, C.paper);
frames.push(f);
rect(f, "Combat outer stroke", 20, 20, 1880, 1040, C.paper, "#5b5144", 10, 2);
rect(f, "Combat header", 20, 20, 1880, 88, C.paper2, "#d4bf98", 10, 1);
badge(f, "AC", 40, 34, 54, C.teal, C.white);
text(f, "Combat title", "EXPEDITION MONITOR", 112, 32, 430, 34, 30, "Bold", C.black);
text(f, "Combat subtitle", "Mission: Scavenge Run  /  Riverside Gas Station", 114, 70, 520, 18, 13, "Bold", C.teal);
text(f, "Combat state", "NORMAL   DAY 7   ROUND 1", 1460, 45, 360, 22, 16, "Bold", C.black, "RIGHT");
panel(f, "Squad 4/4", 40, 126, 450, 198);
for (let i = 0; i < 4; i++) {
  portrait(f, survivors[i].name, 66 + i * 102, 184, 78, 78, survivors[i].color);
  text(f, "Compact squad " + survivors[i].name, survivors[i].name, 66 + i * 102, 266, 78, 16, 11, "Bold", C.ink, "CENTER");
}
panel(f, "Encounter", 506, 126, 510, 198);
rect(f, "Encounter field", 536, 180, 296, 104, "#d0bea0", C.line2, 4, 1);
text(f, "Encounter name", "AMBUSH SITE", 858, 182, 120, 18, 13, "Bold", C.teal);
text(f, "Encounter threat", "THREAT: HIGH", 858, 212, 124, 18, 13, "Bold", C.red);
text(f, "Encounter conditions", "Enemies: 4\nRound limit: none\nVictory: defeat all enemies", 858, 242, 124, 52, 12, "Medium", C.ink);
panel(f, "Time and Status", 1032, 126, 240, 198);
statRow(f, "Time", .62, 1060, 188, 180, C.teal);
statRow(f, "Threat", .74, 1060, 232, 180, C.red);
statRow(f, "Noise", .36, 1060, 276, 180, C.gold);
panel(f, "Zone and Log", 1288, 126, 592, 198);
rect(f, "Mini map", 1320, 182, 296, 104, "#c9b58e", C.line2, 4, 1);
for (let i = 0; i < 5; i++) rect(f, "Mini route node " + i, 1352 + i * 54, 224 + (i % 2) * 16, 14, 14, i === 0 ? C.green : i === 4 ? C.red : C.white, C.teal, 7, 1);
text(f, "Zone meta", "DISTANCE LEFT  5\nWEATHER  CLOUDY", 1320, 296, 280, 22, 12, "Bold", C.black);
const logs = ["Round 1 begins", "Mara gains +1 stealth", "Raider prepares", "Feral Dog closes in"];
for (let i = 0; i < logs.length; i++) {
  rect(f, "Log bullet " + i, 1646, 186 + i * 26, 8, 8, i === 0 ? C.teal : i === 1 ? C.green : C.red, null, 4, 0);
  text(f, "Combat log " + i, logs[i], 1662, 180 + i * 26, 170, 16, 11, "Medium", C.ink);
}
rect(f, "Phase panel", 40, 344, 1840, 58, "#ddc8a4", C.line, 6, 1);
text(f, "Phase label", "COMBAT PHASE", 60, 359, 200, 24, 22, "Bold", C.black);
rect(f, "Phase player", 290, 362, 690, 20, C.green, null, 3, 0);
rect(f, "Phase clash", 1006, 362, 230, 20, C.gold, null, 3, 0);
rect(f, "Phase enemy", 1262, 362, 560, 20, C.red, null, 3, 0);
text(f, "Phase player text", "PLAYER TURN", 290, 364, 690, 16, 12, "Bold", C.white, "CENTER");
text(f, "Phase clash text", "CLASH", 1006, 364, 230, 16, 12, "Bold", C.black, "CENTER");
text(f, "Phase enemy text", "ENEMY TURN", 1262, 364, 560, 16, 12,  "Bold", C.white, "CENTER");
rect(f, "Battlefield", 40, 424, 1840, 448, "#c9b899", C.line, 6, 1);
text(f, "Player lane", "PLAYER SQUAD", 64, 444, 210, 18, 13, "Bold", C.teal);
text(f, "Enemy lane", "ENEMY LINE", 1132, 444, 210, 18, 13, "Bold", C.red);
for (let i = 0; i < 4; i++) {
  battleCard(f, comps, "player", {
    name: survivors[i].name, role: survivors[i].role, level: "LVL " + (i === 0 ? "4" : i === 1 ? "4" : "3"),
    hp: survivors[i].hp, hpv: survivors[i].hpv, trait: survivors[i].trait.toUpperCase(),
    stats: [String(34 + i * 6), String(10 + i * 2), String(11 + i)], icons: ["W", "A", "K"], color: survivors[i].color
  }, 64 + i * 192, 484);
}
rect(f, "VS circle", 846, 548, 228, 228, "#ddcfb4", C.line, 114, 2, .72);
text(f, "VS", "VS", 846, 628, 228, 34, 28, "Bold", C.line, "CENTER");
line(f, "VS arrow left", 810, 654, 58, 8, C.line, .8);
line(f, "VS arrow right", 1052, 654, 58, 8, C.line, .8);
for (let i = 0; i < 4; i++) {
  battleCard(f, comps, "enemy", enemies[i], 1130 + i * 192, 484);
}
rect(f, "Bottom action tray", 40, 900, 1840, 140, C.panel, C.line2, 6, 1);
badge(f, "EN", 70, 930, 62, C.gold, C.black);
text(f, "Energy count", "4 / 5", 150, 944, 100, 32, 28, "Bold", C.black);
text(f, "Abilities heading", "ABILITIES", 320, 922, 180, 18, 14, "Bold", C.teal);
const abs = ["Aim", "Guard", "Treat", "Move", "Item"];
for (let i = 0; i < abs.length; i++) {
  badge(f, abs[i].substring(0, 1), 322 + i * 86, 956, 46, i < 2 ? C.gold : C.teal, C.white);
  text(f, "Ability " + abs[i], abs[i], 300 + i * 86, 1008, 88, 16, 10, "Medium", C.ink, "CENTER");
}
text(f, "Combat phase info", "Use abilities or move to prepare for the clash. Four enemy cards fit fully on the right lane with readable HP, name, stats, and slots.", 736, 930, 420, 56, 14, "Medium", C.ink);
addButton(f, "RETREAT", 1210, 944, 190, C.teal, "<");
addButton(f, "USE ITEM", 1430, 944, 210, C.green, "+");
addButton(f, "END TURN", 1668, 944, 180, C.rust, ">");

// 06 Workshop
f = addScreen("Workshop", "Craft, repair, and equipment flow", "Workshop", 4080, 1160);
panel(f, "Inventory", 40, 136, 500, 838);
const items = ["Rusty Knife", "Metal Pipe", "Machete", "Rusty Revolver", "Sawn-off Shotgun", "Hunting Rifle", "Leather Jacket", "Scrap Armor", "Medkit", "Toolkit", "Ammo Pack", "Backpack"];
for (let i = 0; i < items.length; i++) {
  const ix = 72 + (i % 2) * 212;
  const iy = 196 + Math.floor(i / 2) * 116;
  rect(f, "Inventory item " + items[i], ix, iy, 184, 82, "#f8ecd2", C.line2, 5, 1);
  badge(f, items[i].substring(0, 1), ix + 12, iy + 18, 38, i < 6 ? C.rust : C.teal, C.white);
  text(f, "Inventory item text " + items[i], items[i], ix + 62, iy + 16, 104, 34, 12, "Bold", C.ink);
  text(f, "Inventory item meta " + items[i], i < 6 ? "Weapon" : "Gear", ix + 62, iy + 54, 80, 14, 10, "Medium", C.muted);
}
panel(f, "Selected Item", 570, 136, 720, 838);
badge(f, "HR", 626, 210, 96, C.rust, C.white);
text(f, "Workshop item name", "HUNTING RIFLE", 750, 214, 280, 34, 28, "Bold", C.black);
text(f, "Workshop item meta", "Weapon / Firearms / Noise 3", 752, 256, 280, 20, 14, "Bold", C.teal);
statRow(f, "Damage", .70, 626, 330, 560, C.rust);
statRow(f, "Durability", .52, 626, 380, 560, C.gold);
statRow(f, "Accuracy", .68, 626, 430, 560, C.green);
text(f, "Workshop repair copy", "Repair cost uses Scrap and Weapon Parts. Crafting stays visible beside survivor assignment so the player can compare loadout impact.", 626, 510, 560, 72, 16, "Medium", C.ink);
addButton(f, "REPAIR", 626, 638, 190, C.green, "+");
addButton(f, "CRAFT COPY", 844, 638, 210, C.teal, ">");
addButton(f, "DISASSEMBLE", 1082, 638, 190, C.rust, "X");
panel(f, "Crafting Queue", 1320, 136, 560, 838);
for (let i = 0; i < 4; i++) {
  rect(f, "Queue row " + i, 1354, 202 + i * 128, 492, 94, "#f8ecd2", C.line2, 5, 1);
  badge(f, String(i + 1), 1372, 228 + i * 128, 42, i === 0 ? C.green : C.gold, C.white);
  text(f, "Queue title " + i, i === 0 ? "Repair Hunting Rifle" : i === 1 ? "Craft Medkit" : i === 2 ? "Patch Leather Jacket" : "Assemble Ammo Pack", 1432, 220 + i * 128, 220, 20, 15, "Bold", C.ink);
  progress(f, "Queue progress " + i, 1432, 258 + i * 128, 260, 8, i === 0 ? .62 : i === 1 ? .28 : .0, i === 0 ? C.green : C.gold);
}

// 07 Radio
f = addScreen("Radio", "Recruitment and signal operations", "Radio", 0, 2320);
panel(f, "Signal Status", 40, 136, 500, 838);
badge(f, "RT", 82, 206, 92, C.teal, C.white);
text(f, "Radio tower name", "RADIO TOWER", 198, 212, 220, 28, 24, "Bold", C.black);
text(f, "Radio tower meta", "Level 1 / Recruitment unlocked", 200, 248, 260, 18, 13, "Bold", C.teal);
statRow(f, "Signal", .54, 82, 330, 360, C.teal);
statRow(f, "Power", .42, 82, 382, 360, C.gold);
statRow(f, "Noise Risk", .36, 82, 434, 360, C.rust);
text(f, "Radio copy", "Radio is a gameplay panel: recruit survivors, discover zones, and choose broadcast policies without decorative stickers blocking fields.", 82, 536, 360, 90, 15, "Medium", C.ink);
panel(f, "Recruit Candidates", 570, 136, 760, 838);
const candidates = [survivors[4], survivors[5], survivors[6]];
for (let i = 0; i < candidates.length; i++) {
  const s = candidates[i];
  rect(f, "Candidate " + s.name, 610, 206 + i * 212, 680, 164, "#f8ecd2", C.line2, 5, 1);
  portrait(f, s.name, 632, 226 + i * 212, 112, 112, s.color);
  text(f, "Candidate name " + s.name, s.name.toUpperCase(), 770, 226 + i * 212, 180, 24, 20, "Bold", C.ink);
  text(f, "Candidate role " + s.name, s.role + " / " + s.trait + " / " + s.weapon, 772, 256 + i * 212, 300, 18, 13, "Bold", C.teal);
  text(f, "Candidate skills " + s.name, s.skills.map(function(k) { return k[0] + " " + Math.round(k[1] * 5); }).join("   "), 772, 298 + i * 212, 330, 22, 13, "Medium", C.ink);
  addButton(f, "RECRUIT", 1110, 260 + i * 212, 144, C.green, "+");
}
panel(f, "Broadcast Actions", 1360, 136, 520, 838);
const radioActions = [["Call for Survivors", "Higher recruit chance"], ["Scout Frequencies", "Find nearby zones"], ["Request Trade", "Chance for supplies"], ["Silent Watch", "Lower threat gain"]];
for (let i = 0; i < radioActions.length; i++) {
  rect(f, "Radio action " + i, 1394, 206 + i * 134, 452, 92, "#f8ecd2", C.line2, 5, 1);
  badge(f, String(i + 1), 1412, 232 + i * 134, 42, i === 0 ? C.green : C.teal, C.white);
  text(f, "Radio action title " + i, radioActions[i][0], 1474, 224 + i * 134, 220, 20, 15, "Bold", C.ink);
  text(f, "Radio action meta " + i, radioActions[i][1], 1474, 250 + i * 134, 220, 18, 12, "Medium", C.muted);
}

// 08 Report
f = addScreen("After Action Report", "Mission outcome and loot review", "Report", 2040, 2320);
panel(f, "Mission Result", 40, 136, 560, 838);
text(f, "Report result", "RIVERSIDE GAS STATION", 86, 204, 360, 28, 24, "Bold", C.black);
text(f, "Report result meta", "Scavenge Run / Success / Day 7", 88, 240, 300, 18, 13, "Bold", C.teal);
statRow(f, "Threat Avoided", .62, 86, 324, 420, C.green);
statRow(f, "Noise", .36, 86, 374, 420, C.gold);
statRow(f, "Squad Condition", .78, 86, 424, 420, C.green);
text(f, "Report copy", "Outcome view keeps player decisions visible: loot, wounds, XP, and follow-up actions fit on the same Full HD screen.", 86, 526, 420, 72, 16, "Medium", C.ink);
panel(f, "Loot Gained", 630, 136, 610, 838);
const loot = [["Scrap", "+42"], ["Food", "+12"], ["Water", "+8"], ["Weapon Parts", "+3"], ["Medicine", "+1"]];
for (let i = 0; i < loot.length; i++) {
  rect(f, "Loot row " + i, 670, 204 + i * 112, 530, 78, "#f8ecd2", C.line2, 5, 1);
  badge(f, loot[i][0].substring(0, 1), 692, 224 + i * 112, 38, i === 0 ? C.black : i === 1 ? C.green : i === 2 ? C.teal : i === 3 ? C.gold : C.red, C.white);
  text(f, "Loot name " + i, loot[i][0], 748, 220 + i * 112, 180, 22, 16, "Bold", C.ink);
  text(f, "Loot value " + i, loot[i][1], 1080, 220 + i * 112, 80, 24, 20, "Bold", C.green, "RIGHT");
}
panel(f, "Survivor Results", 1270, 136, 610, 838);
for (let i = 0; i < 4; i++) {
  const s = survivors[i];
  rect(f, "Report survivor " + s.name, 1306, 204 + i * 142, 538, 104, "#f8ecd2", C.line2, 5, 1);
  portrait(f, s.name, 1324, 218 + i * 142, 72, 72, s.color);
  text(f, "Report survivor name " + s.name, s.name, 1416, 218 + i * 142, 120, 22, 17, "Bold", C.ink);
  text(f, "Report survivor meta " + s.name, "XP +" + (i + 1) * 2 + "  /  no active wounds", 1416, 246 + i * 142, 220, 18, 12, "Medium", C.teal);
  progress(f, "Report survivor hp " + s.name, 1416, 280 + i * 142, 220, 8, s.hpv, C.green);
}
addButton(f, "CONTINUE", 1594, 868, 214, C.rust, ">");

// Audit text bounds and text-text overlaps inside each frame.
function bounds(node) {
  const b = node.absoluteBoundingBox;
  if (!b) return null;
  return { x: b.x, y: b.y, w: b.width, h: b.height, r: b.x + b.width, b: b.y + b.height };
}
function intersects(a, b) {
  const x = Math.max(0, Math.min(a.r, b.r) - Math.max(a.x, b.x));
  const y = Math.max(0, Math.min(a.b, b.b) - Math.max(a.y, b.y));
  return x > 2 && y > 2;
}
const audit = [];
for (let fi = 0; fi < frames.length; fi++) {
  const fr = frames[fi];
  const fb = bounds(fr);
  const texts = fr.findAll(function(n) { return n.type === "TEXT" && n.visible !== false; });
  const outOfBounds = [];
  const overlaps = [];
  for (let i = 0; i < texts.length; i++) {
    const a = bounds(texts[i]);
    if (!a) continue;
    if (a.x < fb.x - 1 || a.y < fb.y - 1 || a.r > fb.r + 1 || a.b > fb.b + 1) outOfBounds.push(texts[i].name);
    for (let j = i + 1; j < texts.length; j++) {
      const b = bounds(texts[j]);
      if (b && intersects(a, b)) overlaps.push(texts[i].name + " <> " + texts[j].name);
    }
  }
  audit.push({ frame: fr.name, nodeId: fr.id, textCount: texts.length, outOfBounds: outOfBounds.slice(0, 12), overlapCount: overlaps.length, overlaps: overlaps.slice(0, 12) });
}

figma.viewport.scrollAndZoomIntoView(frames);
return {
  pageName: page.name,
  frames: frames.map(function(fr) { return { name: fr.name, id: fr.id }; }),
  components: Object.keys(comps).map(function(key) { return comps[key].name; }),
  audit: audit
};
`;

async function waitForBridge() {
  for (let attempt = 0; attempt < 36; attempt++) {
    const statusResult = await callTool("figma_get_status", { probe: true }, 20000);
    const status = parseJsonContent(statusResult) || {};
    if (status?.setup?.valid === true && status?.setup?.probeResult?.success === true) {
      return status;
    }
    await new Promise((resolve) => setTimeout(resolve, 1500));
  }
  throw new Error("Desktop Bridge did not connect to the build MCP server on port 9225");
}

async function main() {
  await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-figma-builder", version: "1.0.0" },
  });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  const status = await waitForBridge();
  writeFileSync(join(outDir, "status-before-build.json"), JSON.stringify(status, null, 2));

  const executeResult = await callTool("figma_execute", { code: buildCode, timeout: 30000 }, 90000);
  const executeJson = parseJsonContent(executeResult);
  writeFileSync(join(outDir, "build-result.json"), JSON.stringify(executeJson, null, 2));
  if (!executeJson?.success) {
    throw new Error(`figma_execute failed: ${JSON.stringify(executeJson)}`);
  }

  const built = executeJson.result;
  const savedScreenshots = [];
  for (const fr of built.frames) {
    const shot = await callTool("figma_take_screenshot", { nodeId: fr.id, scale: 0.5, format: "png" }, 90000);
    const saved = saveScreenshot(fr.name, shot);
    savedScreenshots.push({ frame: fr.name, id: fr.id, ...saved });
  }
  writeFileSync(join(outDir, "screenshots.json"), JSON.stringify(savedScreenshots, null, 2));

  console.log(JSON.stringify({
    pageName: built.pageName,
    frames: built.frames.length,
    components: built.components,
    audit: built.audit,
    screenshots: savedScreenshots.map((s) => s.filePath),
  }, null, 2));
}

main()
  .catch((error) => {
    console.error(error.stack || error.message);
    process.exitCode = 1;
  })
  .finally(() => {
    killServerTree();
  });
