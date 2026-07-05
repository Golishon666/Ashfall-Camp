import { spawn, spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const outDir = join(process.cwd(), "tmp", "figma-elementwise");
const shotsDir = join(outDir, "screenshots");
mkdirSync(shotsDir, { recursive: true });

const P = (...parts) => join(process.cwd(), ...parts);
const assetPaths = {
  bgCamp: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Backgrounds", "ui_bg_camp_overview_01.png"),
  bgExpedition: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Backgrounds", "ui_bg_expedition_setup_01.png"),
  mapBase: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Map", "ui_map_base_world_4k.png"),
  mapRoute: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Map", "ui_map_route_overlay.png"),
  navCamp: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_camp.png"),
  navSurvivors: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_survivors.png"),
  navBuildings: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_buildings.png"),
  navWorkshop: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_workshop.png"),
  navExpedition: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_expedition.png"),
  navRadio: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Navigation", "ui_icon_nav_radio.png"),
  resScrap: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_scrap.png"),
  resFood: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_food.png"),
  resWater: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_water.png"),
  resMedicine: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_medicine.png"),
  resParts: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_parts.png"),
  resRadio: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_radio_intel.png"),
  statusHealthy: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_healthy.png"),
  statusMorale: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_morale.png"),
  statusSafe: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_safe.png"),
  statusWounded: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_wounded.png"),
  statusIdle: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_idle.png"),
  statusLevel: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_level_up.png"),
  eqRifle: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_rifle.png"),
  eqPistol: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_pistol.png"),
  eqVest: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_armor_vest.png"),
  eqMedkit: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_medkit.png"),
  eqBackpack: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_backpack.png"),
  eqAmmo: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_ammo_box.png"),
  eqHatchet: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_hatchet.png"),
  eqCanteen: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_canteen.png"),
  eqTape: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_duct_tape.png"),
  eqFlashlight: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_flashlight.png"),
  eqRadio: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_radio.png"),
  survivor1: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_01.png"),
  survivor2: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_02.png"),
  survivor3: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_03.png"),
  survivor4: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_04.png"),
  markerCamp: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Map", "ui_map_marker_camp.png"),
  markerHospital: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Map", "ui_map_marker_hospital.png"),
  markerPolice: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Map", "ui_map_marker_police_outpost.png"),
  markerScavenging: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Map", "ui_map_marker_scavenging.png"),
  markerForest: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Map", "ui_map_marker_forest.png"),
  markerHazard: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Map", "ui_map_marker_hazard.png"),
  markerUnknown: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Map", "ui_map_marker_unknown.png"),
  markerEvent: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Map", "ui_map_marker_event.png"),
  buildingBarracks: P("tmp", "figma-elementwise", "crops", "building-barracks.png"),
  buildingWorkshop: P("tmp", "figma-elementwise", "crops", "building-workshop.png"),
  buildingWater: P("tmp", "figma-elementwise", "crops", "building-water-collector.png"),
  buildingInfirmary: P("tmp", "figma-elementwise", "crops", "building-infirmary.png"),
  buildingRadio: P("tmp", "figma-elementwise", "crops", "building-radio-tower.png"),
  zoneStore: P("tmp", "figma-elementwise", "crops", "zone-abandoned-store.png"),
  zoneDry: P("tmp", "figma-elementwise", "crops", "zone-dry-suburb.png"),
  zoneClinic: P("tmp", "figma-elementwise", "crops", "zone-ruined-clinic.png"),
  zonePolice: P("tmp", "figma-elementwise", "crops", "zone-police-outpost.png"),
  zoneTunnel: P("tmp", "figma-elementwise", "crops", "zone-mutant-tunnel.png"),
  expGas: P("tmp", "figma-elementwise", "crops", "expedition-gas-station.png"),
  expWind: P("tmp", "figma-elementwise", "crops", "expedition-wind-farm.png"),
  dashWounded: P("tmp", "figma-elementwise", "crops", "dashboard-alert-survivor.png"),
  dashIdle: P("tmp", "figma-elementwise", "crops", "dashboard-idle-survivor.png"),
  dashUpgrade: P("tmp", "figma-elementwise", "crops", "dashboard-upgrade-building.png"),
  workshopRifle: P("tmp", "figma-elementwise", "crops", "workshop-rifle-large.png"),
  workshopBench: P("tmp", "figma-elementwise", "crops", "workshop-workbench.png"),
  mapDry: P("tmp", "figma-elementwise", "crops", "map-location-dry-suburb.png"),
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
    pending.set(id, { resolve, reject, timer, method });
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

function rgb(hex) {
  const h = hex.replace("#", "");
  return { r: parseInt(h.slice(0, 2), 16) / 255, g: parseInt(h.slice(2, 4), 16) / 255, b: parseInt(h.slice(4, 6), 16) / 255 };
}
function paint(hex, opacity = 1) {
  return { type: "SOLID", color: rgb(hex), opacity };
}
const C = {
  ink: "#1f211d",
  muted: "#5e5548",
  parchment: "#efe2c8",
  parchment2: "#f6ead2",
  line: "#c8ad83",
  line2: "#a98b61",
  teal: "#245f67",
  tealDark: "#15474f",
  green: "#557840",
  rust: "#b95c2f",
  gold: "#b7832c",
  red: "#a54834",
  blue: "#5b8791",
  charcoal: "#3a3329",
};

async function loadFont(style) {
  try {
    await figma.loadFontAsync({ family: "Inter", style });
    return { family: "Inter", style };
  } catch {
    await figma.loadFontAsync({ family: "Inter", style: "Regular" });
    return { family: "Inter", style: "Regular" };
  }
}
const FONT = {
  regular: await loadFont("Regular"),
  medium: await loadFont("Medium"),
  bold: await loadFont("Bold"),
  semi: await loadFont("Semi Bold"),
};

const imageTargets = [];
const frameIds = [];

function assertSize(kind, name, w, h) {
  if (!Number.isFinite(w) || !Number.isFinite(h) || w < 0 || h < 0) {
    throw new Error(kind + " invalid size for " + name + ": " + w + " x " + h);
  }
}
function makeFrame(parent, name, x, y, w, h, fill = null, stroke = null, radius = 0, strokeWeight = 1) {
  assertSize("Frame", name, w, h);
  const f = figma.createFrame();
  f.name = name;
  parent.appendChild(f);
  f.x = x; f.y = y; f.resize(w, h);
  f.fills = fill ? [paint(fill)] : [];
  f.strokes = stroke ? [paint(stroke)] : [];
  f.strokeWeight = stroke ? strokeWeight : 0;
  f.cornerRadius = radius;
  f.clipsContent = true;
  return f;
}
function rect(parent, name, x, y, w, h, fill = null, stroke = null, radius = 0, strokeWeight = 1, opacity = 1) {
  assertSize("Rectangle", name, w, h);
  const n = figma.createRectangle();
  n.name = name;
  parent.appendChild(n);
  n.x = x; n.y = y; n.resize(w, h);
  n.fills = fill ? [paint(fill, opacity)] : [];
  n.strokes = stroke ? [paint(stroke)] : [];
  n.strokeWeight = stroke ? strokeWeight : 0;
  n.cornerRadius = radius;
  return n;
}
function ellipse(parent, name, x, y, w, h, fill = null, stroke = null, strokeWeight = 1, opacity = 1) {
  assertSize("Ellipse", name, w, h);
  const n = figma.createEllipse();
  n.name = name;
  parent.appendChild(n);
  n.x = x; n.y = y; n.resize(w, h);
  n.fills = fill ? [paint(fill, opacity)] : [];
  n.strokes = stroke ? [paint(stroke)] : [];
  n.strokeWeight = stroke ? strokeWeight : 0;
  return n;
}
function line(parent, name, x, y, w, h = 1, color = C.line, opacity = 1) {
  return rect(parent, name, x, y, w, h, color, null, 0, 0, opacity);
}
function text(parent, name, value, x, y, w, h, size = 16, weight = "regular", color = C.ink, align = "LEFT") {
  assertSize("Text", name, w, h);
  const t = figma.createText();
  t.name = name;
  parent.appendChild(t);
  t.x = x; t.y = y; t.resize(w, h);
  t.textAutoResize = "NONE";
  t.fontName = FONT[weight] || FONT.regular;
  t.fontSize = size;
  t.lineHeight = { unit: "PIXELS", value: Math.ceil(size * 1.22) };
  t.textAlignHorizontal = align;
  t.textAlignVertical = "TOP";
  t.fills = [paint(color)];
  t.characters = value;
  return t;
}
function image(parent, key, name, x, y, w, h, scaleMode = "FILL", radius = 0, stroke = null) {
  const n = rect(parent, name, x, y, w, h, "#eee0c4", stroke, radius, stroke ? 1 : 0);
  imageTargets.push({ key, nodeId: n.id, scaleMode });
  return n;
}
function progress(parent, name, x, y, w, h, value, color = C.green) {
  rect(parent, name + " / track", x, y, w, h, "#cfc2a9", null, h / 2);
  rect(parent, name + " / fill", x, y, Math.max(2, w * value), h, color, null, h / 2);
}
function button(parent, label, x, y, w, h, color = C.rust, iconText = null) {
  const b = makeFrame(parent, "Button / " + label, x, y, w, h, color, "#7e3d23", 5, 1.5);
  if (iconText) text(b, "Icon", iconText, 16, h / 2 - 11, 28, 24, 20, "bold", "#fff5dc", "CENTER");
  text(b, "Label", label, iconText ? 44 : 0, h / 2 - 12, iconText ? w - 52 : w, 28, label.length > 13 ? 15 : 18, "bold", "#fff5dc", "CENTER");
  return b;
}
function smallButton(parent, label, x, y, w, h) {
  const b = makeFrame(parent, "Small Button / " + label, x, y, w, h, "#f1e3ca", C.line2, 4, 1);
  text(b, "Label", label, 0, h / 2 - 9, w, 20, 12, "bold", C.muted, "CENTER");
  return b;
}
function panel(parent, title, x, y, w, h, iconKey = null) {
  const p = makeFrame(parent, "Panel / " + title, x, y, w, h, C.parchment2, C.line, 9, 1);
  line(p, "Title rule left", 18, 32, 88, 1, C.line, 0.65);
  line(p, "Title rule right", w - 106, 32, 88, 1, C.line, 0.65);
  if (iconKey) image(p, iconKey, "Title icon", 20, 18, 28, 28, "FIT", 0);
  text(p, "Title", title, iconKey ? 56 : 0, 18, iconKey ? w - 70 : w, 34, 22, "bold", C.teal, iconKey ? "LEFT" : "CENTER");
  return p;
}
function brand(parent, title, subtitle, mode = "large") {
  const flag = makeFrame(parent, "Brand Flag", 42, 18, 120, 138, C.teal, "#173e44", 3, 0);
  image(flag, "navCamp", "Camp mark", 24, 24, 72, 72, "FIT");
  text(parent, "Brand / Title", title, 185, mode === "large" ? 36 : 30, 510, 66, 50, "bold", C.ink);
  text(parent, "Brand / Subtitle", subtitle, 190, mode === "large" ? 110 : 96, 430, 24, 17, "bold", C.teal);
}
function pageBase(name, x, y, title, subtitle) {
  const f = makeFrame(page, "Ashfall Camp - Full HD Elements / " + name, x, y, 1920, 1080, C.parchment, "#3b3227", 0, 8);
  f.clipsContent = true;
  rect(f, "Inner parchment wash", 12, 12, 1896, 1056, "#f4e7ce", "#dac199", 12, 1, 0.88);
  rect(f, "Top left metal corner", 0, 0, 88, 88, "#3a352d", null, 0, 0, 0.9);
  rect(f, "Bottom left metal corner", 0, 990, 88, 90, "#3a352d", null, 0, 0, 0.9);
  rect(f, "Top right metal corner", 1834, 0, 86, 84, "#3a352d", null, 0, 0, 0.9);
  brand(f, title, subtitle);
  frameIds.push({ title: name, frameId: f.id });
  return f;
}
function resourceBar(parent, x, y, w, h, compact = false) {
  const bar = makeFrame(parent, "Resource Bar", x, y, w, h, "#f3e5ca", C.line, 8, 1);
  const items = [
    ["resScrap", "SCRAP", "1,250", "+32 /h"],
    ["resFood", "FOOD", "320", "+28 /h"],
    ["resWater", "WATER", "480", "+24 /h"],
    ["resMedicine", "MEDICINE", "210", "+16 /h"],
    ["resParts", "WEAPON PARTS", "75", "+6 /h"],
    ["resRadio", "RADIO INTEL", "3", "+1 /h"],
    ["navSurvivors", "SURVIVOR CAPACITY", "28 / 36", "8 idle"],
  ];
  const cellW = w / items.length;
  for (let i = 0; i < items.length; i++) {
    const [key, label, value, rate] = items[i];
    const cx = i * cellW;
    if (i > 0) line(bar, "Cell separator " + i, cx, 10, 1, h - 20, C.line, 0.7);
    image(bar, key, "Icon / " + label, cx + 20, 24, compact ? 38 : 50, compact ? 38 : 50, "FIT");
    text(bar, "Label / " + label, label, cx + (compact ? 68 : 84), 18, cellW - 92, 24, compact ? 12 : 14, "bold", C.ink);
    text(bar, "Value / " + label, value, cx + (compact ? 68 : 84), compact ? 43 : 50, cellW - 92, 32, compact ? 19 : 24, "bold", C.ink);
    text(bar, "Rate / " + label, rate, cx + (compact ? 68 : 84), compact ? 72 : 83, cellW - 92, 24, compact ? 12 : 15, "medium", C.muted);
    if (label === "SURVIVOR CAPACITY" && cellW > 135) progress(bar, "Capacity bar", cx + 92, h - 34, cellW - 122, 8, 0.78, C.green);
  }
  return bar;
}
function miniResourceStrip(parent, x, y, w, h) {
  const bar = makeFrame(parent, "Top Mini Resource Strip", x, y, w, h, "#f3e5ca", C.line, 6, 1);
  const items = [
    ["resFood", "2,350"],
    ["resWater", "1,780"],
    ["resScrap", "1,260"],
    ["eqRadio", "980"],
  ];
  const cellW = w / items.length;
  for (let i = 0; i < items.length; i++) {
    const [key, value] = items[i];
    if (i > 0) line(bar, "Mini separator " + i, i * cellW, 8, 1, h - 16, C.line, 0.65);
    image(bar, key, "Mini resource icon " + i, i * cellW + 12, 8, 26, 26, "FIT");
    text(bar, "Mini resource value " + i, value, i * cellW + 44, 10, cellW - 50, 22, 14, "bold", C.ink);
  }
  return bar;
}
function nav(parent, active, x = 410, y = 950, w = 1460, h = 86) {
  const items = [
    ["navCamp", "CAMP"],
    ["navSurvivors", "SURVIVORS"],
    ["navBuildings", "BUILDINGS"],
    ["navWorkshop", "WORKSHOP"],
    ["navExpedition", "MAP"],
  ];
  const gap = 16;
  const itemW = (w - gap * (items.length - 1)) / items.length;
  for (let i = 0; i < items.length; i++) {
    const [key, label] = items[i];
    const isActive = label === active;
    const b = makeFrame(parent, "Bottom Nav / " + label, x + i * (itemW + gap), y, itemW, h, isActive ? C.teal : "#f1e3c8", isActive ? "#123d45" : C.line2, 8, isActive ? 2 : 1);
    image(b, key, "Icon", 28, 20, 44, 44, "FIT");
    text(b, "Label", label, 82, 27, itemW - 100, 32, 22, "bold", isActive ? "#fff5dc" : C.ink);
    if (isActive) {
      const tri = figma.createPolygon();
      tri.name = "Current Tab Pointer";
      b.appendChild(tri);
      tri.pointCount = 3;
      tri.x = itemW / 2 - 10; tri.y = h - 2; tri.resize(20, 14);
      tri.rotation = 180;
      tri.fills = [paint(C.teal)];
      tri.strokes = [];
    }
  }
}
function topTabs(parent, labels, active, x, y, w, h) {
  const gap = 10;
  const itemW = (w - gap * (labels.length - 1)) / labels.length;
  for (let i = 0; i < labels.length; i++) {
    const isActive = labels[i] === active;
    const tab = makeFrame(parent, "Tab / " + labels[i], x + i * (itemW + gap), y, itemW, h, isActive ? C.teal : "#f3e6cf", isActive ? "#123d45" : C.line, 6, 1);
    text(tab, "Label", labels[i], 0, h / 2 - 12, itemW, 28, 18, "bold", isActive ? "#fff5dc" : C.ink, "CENTER");
  }
}
function statRow(parent, iconKey, label, value, y, barValue = null, status = null) {
  image(parent, iconKey, "Icon / " + label, 18, y, 22, 22, "FIT");
  text(parent, "Label / " + label, label, 52, y + 2, 115, 22, 15, "medium", C.ink);
  if (barValue !== null) {
    progress(parent, "Progress / " + label, 170, y + 8, 130, 8, barValue, C.green);
    text(parent, "Status / " + label, status || value, 312, y + 2, 72, 22, 14, "medium", C.green);
  } else {
    text(parent, "Value / " + label, value, 260, y + 2, 115, 22, 16, "bold", C.ink, "RIGHT");
  }
}

function buildResourcePanel(x, y) {
  const f = makeFrame(page, "Ashfall Camp - Resource Dashboard Bar / Elementwise", x, y, 1920, 150, C.parchment, "#3b3227", 0, 4);
  frameIds.push({ title: "00 Resource Bar Elementwise", frameId: f.id });
  resourceBar(f, 8, 14, 1904, 120, false);
  return f;
}

function callout(parent, iconKey, title, line1, line2, x, y, w = 220) {
  const c = makeFrame(parent, "Camp Overview Callout / " + title, x, y, w, 86, "#f1e6d1", C.line2, 8, 1);
  image(c, iconKey, "Icon", 16, 18, 42, 42, "FIT");
  text(c, "Title", title, 70, 16, w - 84, 24, 17, "bold", C.ink);
  text(c, "Line 1", line1, 70, 42, w - 84, 21, 15, "medium", C.ink);
  if (line2) text(c, "Line 2", line2, 70, 62, w - 84, 20, 14, "regular", C.muted);
}

function buildDashboard(x, y) {
  const f = pageBase("01 Camp Dashboard Elementwise", x, y, "ASHFALL CAMP", "REBUILD  •  SURVIVE  •  THRIVE");
  resourceBar(f, 590, 30, 1180, 105, true);
  const status = panel(f, "CAMP STATUS", 34, 158, 385, 285);
  image(status, "statusHealthy", "Thriving icon", 26, 65, 70, 70, "FIT");
  text(status, "Thriving", "THRIVING", 115, 62, 230, 36, 26, "bold", C.green);
  text(status, "Copy", "Our camp is strong and our\nsurvivors are hopeful.", 115, 101, 245, 48, 15, "regular", C.ink);
  statRow(status, "statusMorale", "Morale", "Good", 160, 0.72, "Good");
  statRow(status, "statusSafe", "Safety", "Secure", 196, 0.84, "Secure");
  statRow(status, "eqBackpack", "Supplies", "Stable", 232, 0.88, "Stable");
  statRow(status, "resWater", "Conditions", "Comfortable", 268, 0.76, "Comfort");

  const alerts = panel(f, "RECENT ALERTS", 34, 468, 385, 205);
  function alertRow(y, iconKey, title, body, buttonText) {
    image(alerts, iconKey, "Icon / " + title, 26, y, 34, 34, "FIT");
    text(alerts, "Alert / " + title, title, 72, y - 1, 190, 22, 15, "bold", title.includes("WOUNDED") ? C.red : C.gold);
    text(alerts, "Alert Copy / " + title, body, 72, y + 22, 205, 20, 13, "regular", C.ink);
    smallButton(alerts, buttonText, 292, y + 4, 64, 28);
  }
  alertRow(68, "statusWounded", "WOUNDED SURVIVORS", "2 survivors need medical attention.", "VIEW");
  alertRow(118, "statusIdle", "IDLE SURVIVORS", "8 survivors are idle.", "MANAGE");
  alertRow(168, "statusLevel", "UPGRADE AVAILABLE", "Workshop can be upgraded.", "VIEW");

  const overview = panel(f, "CAMP OVERVIEW", 445, 158, 980, 590);
  image(overview, "bgCamp", "Image / Camp Overview Background", 16, 56, 948, 440, "FILL", 6, C.line);
  callout(overview, "navSurvivors", "BARRACKS", "Level 2", "20 / 24", 112, 150, 230);
  callout(overview, "navWorkshop", "WORKSHOP", "Level 2", "Crafting", 432, 280, 230);
  callout(overview, "navRadio", "RADIO TOWER", "Level 2", "Intel +1 /h", 700, 150, 245);
  callout(overview, "resWater", "WATER COLLECTOR", "Level 2", "448 /h", 84, 405, 245);
  callout(overview, "resMedicine", "INFIRMARY", "Level 1", "2 Wounded", 702, 342, 230);
  text(overview, "Motto", "Every day we rebuild. Every choice we make shapes tomorrow.", 230, 516, 520, 32, 18, "regular", C.muted, "CENTER");

  const right = makeFrame(f, "Right Dashboard Column", 1450, 158, 420, 770, null, null, 0);
  const exp = panel(right, "ACTIVE EXPEDITIONS", 0, 0, 420, 500, "navExpedition");
  text(exp, "Count", "2 / 3", 348, 18, 48, 24, 15, "bold", C.ink, "CENTER");
  function expCard(y, imgKey, title, desc, footer, state, stateColor) {
    const c = makeFrame(exp, "Expedition Card / " + title, 18, y, 384, 150, "#f5ead4", C.line2, 7, 1);
    image(c, imgKey, "Thumbnail", 12, 12, 150, 105, "FILL", 5, C.line);
    text(c, "Title", title, 178, 14, 185, 24, 16, "bold", C.ink);
    text(c, "Type", "Scavenge", 178, 40, 185, 20, 14, "regular", C.ink);
    text(c, "Desc", desc, 178, 62, 180, 45, 13, "regular", C.ink);
    text(c, "Footer", footer, 178, 111, 175, 22, 13, "medium", C.ink);
    rect(c, "State strip", 12, 124, 360, 18, stateColor, null, 2, 0, 0.28);
    text(c, "State", state, 12, 123, 360, 20, 13, "bold", stateColor, "CENTER");
  }
  expCard(62, "expGas", "ABANDONED GAS STATION", "An old gas station. We might find fuel and useful parts.", "6h     3     65%", "IN PROGRESS", C.green);
  expCard(230, "expWind", "WIND FARM", "Wind turbines still stand tall. We could salvage something valuable.", "10h    4     60%", "EN ROUTE", C.blue);
  const slot = makeFrame(exp, "Expedition Slot Available", 18, 400, 384, 70, "#f4e8d1", C.line, 8, 1);
  slot.dashPattern = [8, 6];
  text(slot, "Plus", "+", 120, 18, 34, 34, 28, "bold", C.ink, "CENTER");
  text(slot, "Title", "SLOT AVAILABLE", 166, 18, 150, 24, 15, "bold", C.ink);
  text(slot, "Copy", "Send a team on an expedition.", 166, 42, 170, 20, 13, "regular", C.muted);
  const radio = panel(right, "RADIO INTEL", 0, 526, 420, 214, "navRadio");
  text(radio, "Count", "3", 355, 18, 36, 22, 14, "bold", C.ink, "CENTER");
  text(radio, "Copy", "New transmission received.\nA nearby settlement is willing to trade\nfor medicine and tools.", 28, 68, 285, 72, 16, "regular", C.ink);
  image(radio, "navRadio", "Watermark", 310, 82, 72, 72, "FIT");
  button(radio, "LISTEN", 28, 150, 160, 36, C.rust);
  text(radio, "Generated", "Intel generated: +1 /h", 28, 190, 200, 18, 12, "regular", C.muted);

  function dashCard(x, title, desc, icon, imageKey, btn) {
    const c = makeFrame(f, "Dashboard Action Card / " + title, x, 775, 300, 135, "#f2e4cc", C.line, 8, 1);
    image(c, icon, "Icon", 22, 30, 50, 50, "FIT");
    text(c, "Title", title, 84, 28, 132, 42, 15, "bold", title.includes("WOUNDED") ? C.red : C.gold);
    text(c, "Desc", desc, 84, 72, 132, 34, 12, "regular", C.ink);
    image(c, imageKey, "Portrait / image", 224, 50, 64, 74, "FILL");
    button(c, btn, 32, 98, 166, 30, C.rust);
  }
  dashCard(445, "WOUNDED SURVIVORS", "2 survivors need treatment.", "statusWounded", "dashWounded", "GO TO INFIRMARY");
  dashCard(770, "IDLE SURVIVORS", "8 survivors are idle.", "statusIdle", "dashIdle", "MANAGE SURVIVORS");
  dashCard(1095, "UPGRADE AVAILABLE", "Workshop can be upgraded\nto Level 3.", "statusLevel", "dashUpgrade", "VIEW UPGRADE");
  nav(f, "CAMP", 440, 952, 1430, 84);
  return f;
}

function buildBuildingCard(parent, data, x, y) {
  const c = makeFrame(parent, "Building Card / " + data.title, x, y, 500, 282, "#f3e5cd", C.line2, 8, 1);
  rect(c, "Favorite Ribbon", 20, 20, 32, 42, C.teal, null, 2);
  text(c, "Ribbon Star", "★", 20, 25, 32, 28, 18, "bold", "#fff4d6", "CENTER");
  text(c, "Title", data.title, 70, 22, 285, 30, 25, "bold", C.teal);
  text(c, "Level", data.level, 70, 52, 120, 22, 15, "medium", C.ink);
  image(c, data.img, "Thumbnail", 28, 80, 220, 132, "FILL", 4, C.line);
  text(c, "Workers label", "Workers", 292, 52, 90, 18, 12, "bold", C.ink);
  text(c, "Workers", "● ● ● ● ○ ○   " + data.workers, 292, 72, 180, 20, 15, "bold", data.workerColor || C.ink);
  text(c, "Effects label", "EFFECTS", 292, 106, 90, 18, 12, "bold", C.teal);
  text(c, "Effects", data.effects, 304, 130, 172, 58, 13, "regular", C.ink);
  text(c, "Condition label", "Condition", 292, 198, 80, 18, 12, "regular", C.ink);
  progress(c, "Condition", 370, 204, 86, 8, data.condition, C.green);
  text(c, "Condition value", data.conditionText, 460, 198, 26, 18, 11, "bold", C.ink, "RIGHT");
  line(c, "Cost divider", 16, 224, 468, 1, C.line, 0.75);
  text(c, "Upgrade label", "UPGRADE COST", 24, 240, 108, 18, 11, "bold", C.ink);
  image(c, "resScrap", "Cost scrap", 140, 238, 20, 20, "FIT");
  text(c, "Cost scrap value", data.scrap, 166, 240, 42, 18, 13, "bold", C.ink);
  image(c, "resWater", "Cost water", 224, 238, 20, 20, "FIT");
  text(c, "Cost water value", data.water, 250, 240, 42, 18, 13, "bold", C.ink);
  image(c, "resParts", "Cost parts", 304, 238, 20, 20, "FIT");
  text(c, "Cost parts value", data.parts, 330, 240, 42, 18, 13, "bold", C.ink);
  button(c, "UPGRADE", 378, 230, 106, 38, C.rust);
}

function buildBuildings(x, y) {
  const f = pageBase("02 Buildings Elementwise", x, y, "ASHFALL CAMP", "REBUILD  •  SURVIVE  •  THRIVE");
  rect(f, "Buildings custom header cover", 92, 20, 1160, 112, "#f4e7ce", null, 0, 0, 1);
  text(f, "Screen Title", "BUILDINGS", 205, 58, 300, 58, 44, "bold", C.teal);
  text(f, "Center Brand", "ASHFALL CAMP", 670, 42, 510, 66, 52, "bold", C.ink, "CENTER");
  text(f, "Center Brand Subtitle", "REBUILD  •  SURVIVE  •  THRIVE", 720, 108, 410, 24, 17, "bold", C.teal, "CENTER");
  topTabs(f, ["ALL", "PRODUCTION", "SUPPORT", "UTILITY", "DEFENSE"], "ALL", 205, 145, 980, 56);
  const sidebar = makeFrame(f, "Camp Summary Sidebar", 34, 220, 320, 700, "#f3e5cc", C.line, 8, 1);
  text(sidebar, "Camp Summary", "CAMP SUMMARY", 24, 24, 220, 30, 22, "bold", C.teal);
  const rows = [
    ["navSurvivors", "Population", "42 / 60"],
    ["navSurvivors", "Workers", "28 / 32"],
    ["statusMorale", "Morale", "Good"],
    ["statusSafe", "Safety", "Good"],
    ["resFood", "Food / Day", "324"],
    ["resWater", "Water / Day", "280"],
    ["resRadio", "Power", "210"],
    ["eqBackpack", "Storage", "78%"],
  ];
  for (let i = 0; i < rows.length; i++) {
    const yy = 82 + i * 44;
    line(sidebar, "Row line " + i, 18, yy + 33, 284, 1, C.line, 0.5);
    image(sidebar, rows[i][0], "Icon / " + rows[i][1], 24, yy, 20, 20, "FIT");
    text(sidebar, "Label / " + rows[i][1], rows[i][1], 56, yy + 1, 155, 22, 15, "regular", C.ink);
    text(sidebar, "Value / " + rows[i][1], rows[i][2], 220, yy, 70, 24, 16, "bold", rows[i][2] === "Good" ? C.green : C.ink, "RIGHT");
  }
  text(sidebar, "Capacity Title", "BUILDING CAPACITY", 24, 470, 250, 28, 22, "bold", C.teal);
  text(sidebar, "Capacity Rows", "Total Buildings                         12 / 20\nAssigned Workers                    28 / 32\nIdle Workers                                      4", 24, 522, 266, 88, 16, "regular", C.ink);
  rect(sidebar, "Hint box", 24, 618, 270, 70, "#eadcc2", null, 3, 0, 0.7);
  text(sidebar, "Hint", "Expand your camp to\nunlock more building slots.", 42, 636, 235, 42, 15, "regular", C.muted, "CENTER");
  const data = [
    { title: "BARRACKS", level: "Level 2", img: "buildingBarracks", workers: "5 / 6", effects: "• Increases population\n  capacity.\n• Improves survivor\n  recovery speed.", condition: 0.82, conditionText: "82%", scrap: "240", water: "160", parts: "60" },
    { title: "WORKSHOP", level: "Level 2", img: "buildingWorkshop", workers: "4 / 5", effects: "• Enables item crafting\n  and repairs.\n• Improves tool durability.", condition: 0.78, conditionText: "78%", scrap: "260", water: "120", parts: "80" },
    { title: "WATER COLLECTOR", level: "Level 3", img: "buildingWater", workers: "4 / 4", workerColor: C.green, effects: "• Increases daily\n  water production.\n• Improves water\n  purification.", condition: 0.88, conditionText: "88%", scrap: "280", water: "180", parts: "60" },
    { title: "INFIRMARY", level: "Level 2", img: "buildingInfirmary", workers: "3 / 4", effects: "• Heals injuries faster.\n• Reduces illness chance.", condition: 0.75, conditionText: "75%", scrap: "220", water: "120", parts: "60" },
    { title: "RADIO TOWER", level: "Level 2", img: "buildingRadio", workers: "2 / 3", effects: "• Unlocks radio missions\n  and trades.\n• Improves intel range.", condition: 0.70, conditionText: "70%", scrap: "230", water: "200", parts: "100" },
  ];
  const positions = [[365,220],[875,220],[1385,220],[365,535],[875,535]];
  data.forEach((d, i) => buildBuildingCard(f, d, positions[i][0], positions[i][1]));
  const add = makeFrame(f, "Building Card / Build New Structure", 1385, 535, 500, 282, "#f2e5cf", C.line, 8, 1);
  add.dashPattern = [10, 7];
  text(add, "Plus", "+", 0, 86, 500, 56, 52, "regular", C.line2, "CENTER");
  text(add, "Title", "BUILD NEW STRUCTURE", 0, 158, 500, 28, 17, "bold", C.ink, "CENTER");
  text(add, "Desc", "Expand your camp to unlock\nmore building slots.", 0, 196, 500, 44, 16, "regular", C.muted, "CENTER");
  miniResourceStrip(f, 520, 1008, 620, 44);
  const workers = makeFrame(f, "Bottom Workers Pill", 1160, 1008, 190, 44, "#2f2d25", "#17130e", 4, 1);
  image(workers, "navSurvivors", "Workers icon", 18, 8, 26, 26, "FIT");
  text(workers, "Workers", "28 / 32", 58, 12, 82, 20, 15, "bold", "#fff2d4");
  return f;
}

function zoneCard(parent, data, y) {
  const c = makeFrame(parent, "Zone Card / " + data.title, 20, y, 570, 130, data.selected ? "#eef3e7" : "#f3e5cc", data.selected ? C.teal : C.line, 7, data.selected ? 2 : 1);
  image(c, data.img, "Thumbnail", 12, 16, 230, 96, "FILL", 5, C.line);
  text(c, "Title", data.title, 262, 14, 184, 26, 16, "bold", C.teal);
  text(c, "Desc", data.desc, 262, 48, 185, 42, 12, "regular", C.ink);
  const pillColor = data.risk === "LOW RISK" ? C.green : data.risk === "MEDIUM RISK" ? C.gold : data.risk === "EXTREME RISK" ? "#7a4b91" : C.red;
  rect(c, "Risk pill", 458, 14, 92, 24, pillColor, null, 4);
  text(c, "Risk", data.risk, 458, 18, 92, 18, 11, "bold", "#fff8df", "CENTER");
  const filledDots = data.risk === "LOW RISK" ? 1 : data.risk === "MEDIUM RISK" ? 2 : data.risk === "HIGH RISK" ? 3 : 4;
  for (let i = 0; i < 4; i++) {
    ellipse(c, "Risk Dot " + (i + 1), 464 + i * 18, 46, 12, 12, i < filledDots ? pillColor : null, i < filledDots ? null : C.line2, 1);
  }
  text(c, "Rewards", "REWARDS", 262, 94, 58, 14, 9, "bold", C.muted);
  ["resFood","resWater","resScrap","eqRadio"].slice(0, data.rewards).forEach((key, i) => image(c, key, "Reward " + i, 326 + i * 30, 88, 22, 22, "FIT"));
  text(c, "Power label", "RECOMMENDED POWER", 426, 78, 124, 15, 9, "bold", C.muted, "CENTER");
  text(c, "Power", data.power, 455, 98, 72, 22, 14, "bold", C.ink, "CENTER");
}

function survivorCard(parent, data, x) {
  const c = makeFrame(parent, "Squad Survivor Card / " + data.name, x, 0, 210, 402, "#f3e5cd", C.line, 7, 1);
  image(c, data.img, "Portrait", 14, 14, 182, 150, "FILL", 5, C.line);
  smallButton(c, "×", 174, 10, 24, 24);
  text(c, "Name", data.name, 18, 178, 125, 24, 17, "bold", C.teal);
  text(c, "Role", data.icon + "  " + data.role, 18, 204, 125, 22, 13, "bold", C.ink);
  text(c, "Power label", "POWER", 18, 244, 70, 18, 11, "bold", C.muted);
  text(c, "Power", data.power, 142, 240, 50, 24, 16, "bold", C.ink, "RIGHT");
  text(c, "Fatigue label", "FATIGUE", 18, 280, 70, 18, 11, "bold", C.muted);
  text(c, "Fatigue", data.fatigue, 142, 276, 50, 24, 16, "bold", C.ink, "RIGHT");
  progress(c, "Fatigue", 18, 308, 174, 7, data.fatigueValue, C.gold);
  text(c, "Loadout label", "LOADOUT", 18, 326, 90, 18, 11, "bold", C.muted);
  image(c, data.slot1, "Loadout 1", 18, 348, 72, 42, "FIT", 4, C.line);
  image(c, "eqBackpack", "Loadout 2", 104, 348, 72, 42, "FIT", 4, C.line);
}

function policyCard(parent, data, x, selected = false) {
  const c = makeFrame(parent, "Policy Card / " + data.title, x, 0, 132, 180, selected ? "#eef3e7" : "#f4e6cf", selected ? C.teal : C.line, 6, selected ? 2 : 1);
  text(c, "Icon", data.icon, 0, 18, 132, 40, 32, "bold", selected ? C.teal : C.ink, "CENTER");
  text(c, "Title", data.title, 10, 70, 112, 26, 13, "bold", C.ink, "CENTER");
  text(c, "Desc", data.desc, 12, 105, 108, 58, 12, "regular", C.ink, "CENTER");
  if (selected) {
    rect(c, "Selected dot", 12, 152, 24, 24, C.teal, null, 12);
    text(c, "Check", "✓", 12, 152, 24, 24, 16, "bold", "#fff8df", "CENTER");
  }
}

function buildExpedition(x, y) {
  const f = pageBase("03 Expedition Planning Elementwise", x, y, "ASHFALL CAMP", "REBUILD  •  SURVIVE  •  THRIVE");
  text(f, "Screen Title", "EXPEDITION PLANNING", 620, 58, 720, 60, 40, "bold", C.ink, "CENTER");
  text(f, "Subtitle", "Choose a zone and prepare your squad for the journey.", 710, 116, 540, 26, 17, "regular", C.ink, "CENTER");
  const list = panel(f, "SELECT EXPEDITION ZONE", 26, 154, 620, 820);
  zoneCard(list, { title: "ABANDONED STORE", img: "zoneStore", desc: "A small market, picked clean but still hiding useful scraps.", risk: "LOW RISK", rewards: 3, power: "250" }, 58);
  zoneCard(list, { title: "DRY SUBURB", img: "zoneDry", desc: "Quiet streets and empty homes. Scavenge before others do.", risk: "MEDIUM RISK", rewards: 4, power: "450", selected: true }, 200);
  zoneCard(list, { title: "RUINED CLINIC", img: "zoneClinic", desc: "Medical supplies may remain, but so do the dangers.", risk: "HIGH RISK", rewards: 4, power: "650" }, 342);
  zoneCard(list, { title: "POLICE OUTPOST", img: "zonePolice", desc: "Heavily defended but well stocked with gear and ammo.", risk: "HIGH RISK", rewards: 4, power: "800" }, 484);
  zoneCard(list, { title: "MUTANT TUNNEL", img: "zoneTunnel", desc: "Dark tunnels beneath the city. Proceed with extreme caution.", risk: "EXTREME RISK", rewards: 4, power: "1000" }, 626);
  button(f, "BACK", 40, 990, 180, 58, "#e2d4bc", "←");

  text(f, "Squad Setup title", "SQUAD SETUP", 1110, 154, 220, 30, 22, "bold", C.muted, "CENTER");
  text(f, "Squad Power", "SQUAD POWER   520", 1540, 154, 250, 30, 19, "bold", C.ink);
  const squad = makeFrame(f, "Squad Setup Cards", 670, 192, 1010, 410, null, null, 0);
  survivorCard(squad, { name: "MAYA", role: "SCOUT", icon: "◉", img: "survivor1", power: "130", fatigue: "18%", fatigueValue: 0.18, slot1: "eqHatchet" }, 0);
  survivorCard(squad, { name: "HENRY", role: "BUILDER", icon: "⚒", img: "survivor2", power: "120", fatigue: "22%", fatigueValue: 0.22, slot1: "eqHatchet" }, 230);
  survivorCard(squad, { name: "LEAH", role: "MEDIC", icon: "✚", img: "survivor3", power: "110", fatigue: "20%", fatigueValue: 0.2, slot1: "eqMedkit" }, 460);
  survivorCard(squad, { name: "JAX", role: "SENTRY", icon: "◆", img: "survivor4", power: "160", fatigue: "24%", fatigueValue: 0.24, slot1: "eqRifle" }, 690);
  const add = makeFrame(f, "Add Survivor Slot", 1698, 192, 180, 402, "#f4e6cf", C.line, 7, 1);
  add.dashPattern = [9, 7];
  text(add, "Plus", "+", 0, 120, 180, 58, 60, "regular", C.line2, "CENTER");
  text(add, "Label", "ADD\nSURVIVOR", 0, 198, 180, 58, 17, "bold", C.ink, "CENTER");

  const policy = panel(f, "EXPEDITION POLICY", 670, 626, 720, 205);
  const policies = [
    { title: "SAFE APPROACH", icon: "◆", desc: "Lower risk of encounters and injuries." },
    { title: "BALANCED", icon: "⚖", desc: "Well-balanced approach to risk and reward." },
    { title: "AGGRESSIVE LOOTING", icon: "▣", desc: "Focus on loot and resources." },
    { title: "STEALTHY", icon: "◥", desc: "Avoid fights and stay hidden." },
    { title: "AMMO SAVING", icon: "▮", desc: "Conserve ammo whenever possible." },
  ];
  policies.forEach((p, i) => policyCard(policy, p, 20 + i * 136,  i === 0));
  const supplies = panel(f, "SUPPLIES", 1400, 626, 480, 220);
  text(supplies, "Load", "Total Load  32 / 60", 260, 24, 170, 22, 14, "medium", C.ink, "RIGHT");
  const sup = [["resFood","FOOD","8"],["resWater","WATER","8"],["eqMedkit","MEDKITS","4"],["eqAmmo","AMMO PACKS","6"]];
  for (let i = 0; i < sup.length; i++) {
    const yy = 64 + i * 39;
    line(supplies, "Supply line " + i, 18, yy + 31, 440, 1, C.line, 0.55);
    image(supplies, sup[i][0], "Supply icon / " + sup[i][1], 28, yy, 24, 24, "FIT");
    text(supplies, "Supply label / " + sup[i][1], sup[i][1], 66, yy + 2, 160, 24, 15, "bold", C.ink);
    smallButton(supplies, "-", 292, yy, 24, 24);
    text(supplies, "Supply value / " + sup[i][1], sup[i][2], 340, yy + 2, 28, 22, 15, "bold", C.ink, "CENTER");
    smallButton(supplies, "+", 394, yy, 24, 24);
    image(supplies, "eqBackpack", "Supply bag / " + sup[i][1], 436, yy, 24, 24, "FIT");
  }
  const reward = makeFrame(f, "Expected Rewards", 670, 858, 260, 116, "#f3e5cd", C.line, 6, 1);
  text(reward, "Title", "EXPECTED REWARDS", 0, 16, 260, 22, 14, "bold", C.ink, "CENTER");
  ["resFood","resWater","resScrap","eqBackpack","eqMedkit","eqAmmo"].forEach((key, i) => image(reward, key, "Reward " + i, 36 + i * 32, 44, 24, 24, "FIT"));
  text(reward, "Copy", "Good chance for supplies, parts,\nand useful materials.", 24, 74, 210, 30, 12, "regular", C.ink);
  const duration = makeFrame(f, "Estimated Duration", 948, 858, 260, 116, "#f3e5cd", C.line, 6, 1);
  text(duration, "Title", "EST. DURATION", 0, 16, 260, 22, 14, "bold", C.ink, "CENTER");
  text(duration, "Value", "2 - 3 DAYS", 0, 44, 260, 36, 28, "bold", C.ink, "CENTER");
  text(duration, "Copy", "May vary based on\nencounters and route.", 24, 78, 210, 28, 12, "regular", C.ink, "CENTER");
  const fatigue = makeFrame(f, "Fatigue Impact", 1226, 858, 260, 116, "#f3e5cd", C.line, 6, 1);
  text(fatigue, "Title", "EST. FATIGUE IMPACT", 0, 16, 260, 22, 14, "bold", C.ink, "CENTER");
  text(fatigue, "Value", "MEDIUM", 0, 47, 260, 34, 24, "bold", C.gold, "CENTER");
  text(fatigue, "Copy", "Plan rest after\nreturning to camp.", 24, 78, 210, 28, 12, "regular", C.ink, "CENTER");
  button(f, "AUTO FILL", 1110, 992, 190, 54, "#d6c7ad", "●");
  button(f, "LAUNCH EXPEDITION", 1500, 988, 360, 58, C.green, "✦");
  return f;
}

function inventorySlot(parent, key, x, y, count = "") {
  const s = makeFrame(parent, "Inventory Slot / " + key + " / " + count, x, y, 84, 84, "#eadcc2", C.line, 5, 1);
  image(s, key, "Icon", 12, 10, 56, 56, "FIT");
  if (count) text(s, "Count", count, 54, 60, 26, 18, 13, "bold", C.ink, "RIGHT");
}
function buildWorkshop(x, y) {
  const f = pageBase("04 Workshop Elementwise", x, y, "ASHFALL CAMP", "REBUILD  •  SURVIVE  •  THRIVE");
  rect(f, "Workshop custom header cover", 92, 20, 1120, 112, "#f4e7ce", null, 0, 0, 1);
  text(f, "Workshop Title", "WORKSHOP", 210, 52, 310, 48, 38, "bold", C.ink);
  text(f, "Workshop Subtitle", "Build, repair, and equip to keep everyone alive.", 210, 103, 430, 26, 16, "regular", C.ink);
  text(f, "Center Brand", "ASHFALL CAMP", 670, 42, 510, 66, 52, "bold", C.ink, "CENTER");
  text(f, "Center Brand Subtitle", "REBUILD  •  SURVIVE  •  THRIVE", 720, 108, 410, 24, 17, "bold", C.teal, "CENTER");
  topTabs(f, ["INVENTORY", "REPAIR", "CRAFT", "EQUIP"], "INVENTORY", 170, 154, 1030, 58);
  const inv = panel(f, "INVENTORY", 32, 216, 585, 800);
  const cats = [["ALL","▦"],["WEAPONS","⌁"],["ARMOR","◒"],["MEDICAL","✚"],["MATERIALS","▤"],["MISC","▣"]];
  cats.forEach((c, i) => {
    text(inv, "Category icon / " + c[0], c[1], 34 + i * 80, 62, 42, 30, 24, "bold", i === 0 ? C.teal : C.muted, "CENTER");
    text(inv, "Category label / " + c[0], c[0], 20 + i * 80, 92, 70, 20, 10, "bold", C.ink, "CENTER");
  });
  line(inv, "Category rule", 22, 120, 530, 2, C.line, 0.8);
  text(inv, "Sort", "SORT: NEWEST⌄", 32, 142, 170, 22, 13, "bold", C.ink);
  text(inv, "Capacity", "86 / 120", 460, 142, 70, 22, 13, "medium", C.ink, "RIGHT");
  const icons = ["eqRifle","eqPistol","eqVest","eqHatchet","resParts","eqMedkit","eqTape","resWater","resScrap","eqCanteen","eqFlashlight","eqBackpack","eqRadio","eqAmmo","eqCanteen","eqHatchet","resParts","eqTape","eqBackpack","eqFlashlight","eqHatchet","eqCanteen","eqBackpack","eqRadio","resRadio"];
  for (let r = 0; r < 5; r++) {
    for (let col = 0; col < 5; col++) {
      inventorySlot(inv, icons[r * 5 + col], 32 + col * 100, 178 + r * 100, r === 0 && col === 0 ? "78%" : (r + col) % 3 === 0 ? String((r + 1) * (col + 2)) : "");
    }
  }
  button(inv, "BULK DISMANTLE", 32, 720, 245, 48, C.rust, "♻");
  button(inv, "FILTERS", 320, 720, 230, 48, "#e1d0b4", "▾");

  const sel = panel(f, "SELECTED ITEM", 640, 216, 650, 800);
  image(sel, "workshopRifle", "Selected Item Image", 34, 70, 260, 225, "FIT", 6, C.line);
  text(sel, "Item Title", "RANGER RIFLE", 332, 80, 240, 34, 23, "bold", C.ink);
  text(sel, "Item Type", "Uncommon  •  Primary Weapon", 332, 116, 250, 22, 14, "medium", C.green);
  text(sel, "Item Desc", "Reliable and accurate at\nmedium range.", 332, 150, 250, 48, 15, "regular", C.ink);
  [["DAMAGE",0.46,"46"],["ACCURACY",0.72,"72"],["RANGE",0.65,"65"],["FIRE RATE",0.45,"45"]].forEach((row, i) => {
    text(sel, "Stat label / " + row[0], row[0], 332, 222 + i * 30, 90, 18, 12, "bold", C.ink);
    progress(sel, "Stat / " + row[0], 430, 228 + i * 30, 170, 7, row[1], C.green);
    text(sel, "Stat value / " + row[0], row[2], 604, 220 + i * 30, 24, 18, 12, "bold", C.ink, "RIGHT");
  });
  text(sel, "Durability label", "DURABILITY", 34, 330, 140, 28, 18, "bold", C.muted);
  progress(sel, "Durability", 148, 338, 350, 8, 0.78, C.green);
  text(sel, "Durability value", "MAX 100%", 520, 329, 90, 24, 13, "bold", C.ink, "RIGHT");
  const compare = makeFrame(sel, "Compare Panel", 34, 386, 260, 240, "#f0e1c7", C.line, 5, 1);
  text(compare, "Title", "COMPARE", 18, 18, 160, 24, 16, "bold", C.teal);
  text(compare, "Subtitle", "VS. CURRENTLY EQUIPPED", 18, 48, 200, 20, 11, "bold", C.ink);
  image(compare, "eqRifle", "Current Rifle", 22, 78, 80, 54, "FIT");
  text(compare, "Name", "HUNTING RIFLE\nCommon", 116, 80, 120, 42, 14, "bold", C.ink);
  [["Damage","38"],["Accuracy","60"],["Range","55"],["Fire Rate","35"],["Durability","62%"]].forEach((r, i) => {
    text(compare, "Compare " + r[0], r[0], 18, 140 + i * 18, 80, 16, 10, "regular", C.ink);
    progress(compare, "Compare stat " + r[0], 98, 146 + i * 18, 76, 4, 0.35 + i * 0.07, C.green);
    text(compare, "Compare value " + r[0], r[1] + "  ↓", 180, 138 + i * 18, 55, 16, 10, "bold", C.red, "RIGHT");
  });
  const repair = makeFrame(sel, "Repair Crafting Panel", 314, 386, 300, 240, "#f0e1c7", C.line, 5, 1);
  text(repair, "Title", "REPAIR / CRAFTING", 18, 18, 220, 24, 16, "bold", C.teal);
  text(repair, "Subtitle", "REPAIR MATERIALS (78% → 100%)", 18, 48, 240, 20, 11, "bold", C.ink);
  [["resScrap","12 / 5"],["resParts","28 / 10"],["eqTape","6 / 2"]].forEach((r, i) => {
    const box = makeFrame(repair, "Material / " + r[1], 18 + i * 90, 78, 72, 80, "#eadcc2", C.line, 4, 1);
    image(box, r[0], "Icon", 14, 10, 44, 44, "FIT");
    text(box, "Count", r[1], 8, 58, 56, 18, 15, "bold", C.ink, "CENTER");
  });
  text(repair, "Time", "EST. TIME\n20m", 28, 178, 90, 42, 15, "regular", C.ink);
  text(repair, "Bench", "WORKBENCH LEVEL\nLevel 2", 160, 178, 110, 42, 15, "regular", C.ink);
  button(sel, "REPAIR", 34, 650, 170, 48, C.green, "⚒");
  button(sel, "UPGRADE", 224, 650, 170, 48, C.teal, "↑");
  button(sel, "CRAFT", 414, 650, 170, 48, "#e1d0b4", "⚒");
  const tip = makeFrame(sel, "Tip", 34, 724, 580, 46, "#f0e2c9", C.line, 4, 1);
  text(tip, "Copy", "TIP: Higher workbench levels reduce repair time and increase item durability.", 22, 14, 540, 20, 13, "regular", C.ink);

  const surv = panel(f, "SURVIVOR", 1320, 168, 550, 880);
  image(surv, "survivor1", "Portrait / Maya", 36, 70, 130, 130, "FILL", 5, C.line);
  text(surv, "Name", "MAYA", 195, 78, 170, 32, 24, "bold", C.ink);
  text(surv, "Role", "Builder  ⚒", 195, 112, 160, 24, 16, "regular", C.ink);
  text(surv, "Level", "LEVEL 8", 195, 158, 95, 20, 12, "bold", C.ink);
  progress(surv, "XP", 270, 165, 210, 7, 0.72, C.green);
  text(surv, "XP Copy", "1,240 / 2,000 XP", 390, 150, 120, 20, 11, "regular", C.ink, "RIGHT");
  text(surv, "Healthy", "♥ Healthy", 195, 188, 140, 22, 15, "medium", C.green);
  text(surv, "Loadout title", "LOADOUT", 30, 238, 180, 28, 18, "bold", C.teal);
  const rows2 = [["eqRifle","Ranger Rifle","78%"],["eqPistol","SECONDARY WEAPON\n9mm Pistol","65%"],["eqHatchet","MELEE WEAPON\nHatchet","85%"],["eqVest","ARMOR\nLeather Vest","72%"]];
  rows2.forEach((r, i) => {
    const row = makeFrame(surv, "Loadout Row / " + r[1], 30, 276 + i * 82, 490, 64, "#f3e5cd", C.line, 5, 1);
    image(row, r[0], "Icon", 18, 8, 88, 48, "FIT");
    text(row, "Item", r[1], 200, 12, 220, 42, 15, "regular", C.ink);
    text(row, "Value", r[2], 425, 22, 45, 22, 14, "bold", C.ink, "RIGHT");
  });
  text(surv, "Quick title", "QUICK ITEMS", 30, 628, 180, 26, 17, "bold", C.teal);
  ["eqMedkit","eqTape","resWater","eqCanteen"].forEach((key, i) => inventorySlot(surv, key, 30 + i * 118, 668, i === 0 ? "3" : i === 1 ? "8" : i === 2 ? "5" : "2"));
  button(surv, "AUTO-EQUIP", 30, 774, 190, 44, C.teal, "↑");
  button(surv, "MANAGE SURVIVOR", 240, 774, 230, 44, "#d9c8ad", "●");
  image(surv, "workshopBench", "Workbench", 30, 830, 80, 55, "FIT");
  text(surv, "Bench label", "WORKBENCH\nLevel 2", 125, 830, 150, 44, 14, "bold", C.ink);
  button(surv, "UPGRADE", 392, 830, 118, 40, C.teal);
  return f;
}

function marker(parent, key, title, x, y, riskDots, selected = false) {
  image(parent, key, "Marker Icon / " + title, x, y, 54, 54, "FIT");
  const label = makeFrame(parent, "Map Label / " + title, x + 42, y + 10, title.length > 13 ? 190 : 155, 50, selected ? "#fff3d7" : "#f4e6cf", selected ? C.gold : C.line, 7, selected ? 2 : 1);
  text(label, "Title", title, 14, 8, label.width - 24, 20, 15, "bold", C.ink);
  text(label, "Risk", riskDots, 14, 28, label.width - 24, 18, 14, "bold", riskDots.includes("● ● ●") ? C.red : C.gold);
}
function buildMap(x, y) {
  const f = pageBase("05 World Map Elementwise", x, y, "ASHFALL CAMP", "REBUILD  •  SURVIVE  •  THRIVE");
  miniResourceStrip(f, 1300, 28, 420, 44);
  topTabs(f, ["CAMP", "SURVIVORS", "BUILDINGS", "WORKSHOP", "MAP"], "MAP", 695, 82, 815, 66);
  const power = makeFrame(f, "Camp Power Pill", 1710, 28, 150, 56, C.teal, "#123d45", 6, 1);
  text(power, "Label", "CAMP POWER", 52, 8, 88, 18, 12, "bold", "#fff4dc");
  text(power, "Value", "1,250", 52, 26, 88, 24, 22, "bold", "#fff4dc");
  image(power, "statusSafe", "Shield", 16, 14, 32, 32, "FIT");
  const legend = makeFrame(f, "Legend Panel", 42, 190, 175, 690, "#f2e4cb", C.line, 6, 1);
  text(legend, "Title", "LEGEND", 20, 20, 120, 24, 17, "bold", C.teal);
  text(legend, "Biome", "BIOME TYPES", 20, 70, 120, 20, 12, "bold", C.ink);
  const biomes = [["#7d7d71","Residential Ruins"],["#a9a7a0","City Ruins"],["#8b8776","Industrial Fringe"],["#87955a","Forest Zone"],["#c5a462","Farmland"],["#b7833e","Hazard Zone"],["#6a8c86","Camp (Safe)"]];
  biomes.forEach((b, i) => { rect(legend, "Biome Swatch / " + b[1], 22, 98 + i * 22, 14, 14, b[0], null, 3); text(legend, "Biome Label / " + b[1], b[1], 44, 96 + i * 22, 120, 18, 11, "regular", C.ink); });
  line(legend, "Danger rule", 18, 270, 140, 1, C.line, 0.8);
  text(legend, "Danger", "DANGER LEVEL", 20, 288, 120, 20, 12, "bold", C.ink);
  [["#70a55a","Low Risk"],["#d9982d","Medium Risk"],["#b84b38","High Risk"],["#7d5598","Extreme Risk"]].forEach((b, i) => { rect(legend, "Danger Dot / " + b[1], 24, 316 + i * 24, 16, 16, b[0], "#6b573d", 8); text(legend, "Danger Label / " + b[1], b[1], 48, 314 + i * 24, 110, 18, 11, "regular", C.ink); });
  line(legend, "Markers rule", 18, 430, 140, 1, C.line, 0.8);
  text(legend, "Markers", "MARKERS", 20, 448, 120, 20, 12, "bold", C.ink);
  const marks = [["markerCamp","Main Location"],["markerScavenging","Side Mission"],["markerEvent","Event"],["resScrap","Resource Point"],["markerUnknown","Unknown Location"]];
  marks.forEach((m, i) => { image(legend, m[0], "Legend marker / " + m[1], 22, 476 + i * 25, 18, 18, "FIT"); text(legend, "Marker label / " + m[1], m[1], 48, 475 + i * 25, 120, 18, 11, "regular", C.ink); });
  text(legend, "Filters title", "FILTERS", 20, 632, 90, 22, 15, "bold", C.teal);
  ["ALL","RESOURCES","DANGER","EVENTS","SAFE ROUTES"].forEach((l, i) => smallButton(legend, l, i < 4 ? 20 + (i % 2) * 78 : 20, 660 + Math.floor(i / 2) * 32, i === 4 ? 135 : 70, 24));

  const map = makeFrame(f, "World Map Area", 235, 170, 1245, 785, "#19241e", C.line2, 8, 1);
  image(map, "mapBase", "Map Background", 0, 0, 1245, 785, "FILL", 8);
  image(map, "mapRoute", "Route Overlay", 0, 0, 1245, 785, "FILL", 8);
  marker(map, "markerScavenging", "Dry Suburb", 160, 86, "● ● ○", true);
  marker(map, "markerScavenging", "Abandoned Store", 600, 120, "● ● ○");
  marker(map, "markerForest", "Wind Farm", 920, 225, "● ● ○");
  marker(map, "markerHospital", "Ruined Clinic", 75, 310, "● ● ●");
  marker(map, "markerHospital", "Abandoned Hospital", 55, 616, "● ● ●");
  marker(map, "markerPolice", "Police Outpost", 315, 610, "● ● ○");
  marker(map, "markerForest", "Redwood Outskirts", 600, 630, "● ○ ○");
  marker(map, "markerScavenging", "Old Highway Run", 1030, 440, "● ● ○");
  marker(map, "markerHazard", "Mutant Tunnel", 1030, 685, "● ● ●");
  image(map, "markerCamp", "Camp marker", 595, 385, 72, 72, "FIT");
  const camp = makeFrame(map, "Camp Label", 606, 448, 86, 54, C.teal, "#123d45", 5, 1);
  text(camp, "Camp", "CAMP", 0, 10, 86, 20, 16, "bold", "#fff5dc", "CENTER");
  text(camp, "Safe", "(SAFE ZONE)", 0, 30, 86, 18, 10, "bold", "#fff5dc", "CENTER");
  ["markerUnknown","markerEvent","markerUnknown","resScrap","statusSafe"].forEach((key, i) => image(map, key, "Small Map Marker " + i, [510,235,480,332,660][i], [295,410,690,420,470][i], 36, 36, "FIT"));
  const routeLegend = makeFrame(f, "Travel Routes Legend", 280, 912, 310, 88, "#f3e5cd", C.line, 6, 1);
  text(routeLegend, "Title", "TRAVEL ROUTES", 34, 16, 160, 20, 13, "bold", C.ink);
  [["#5e8e42","Safe Route"],["#c6752c","Risky Route"],["#bf4437","Dangerous Route"]].forEach((r, i) => { line(routeLegend, "Line / " + r[1], 32, 44 + i * 20, 45, 4, r[0]); text(routeLegend, "Label / " + r[1], r[1], 88, 37 + i * 20, 130, 18, 11, "regular", C.ink); });
  const tools = makeFrame(f, "Map Tool Bar", 590, 912, 455, 88, "#f3e5cd", C.line, 6, 1);
  [["⌖","PLACE WAYPOINT"],["×","CLEAR ROUTE"],["−","ZOOM OUT"],["+","ZOOM IN"]].forEach((t, i) => { text(tools, "Tool icon " + t[1], t[0], 30 + i * 105, 18, 70, 32, 26, "bold", C.muted, "CENTER"); text(tools, "Tool label " + t[1], t[1], 16 + i * 105, 55, 98, 18, 10, "bold", C.ink, "CENTER"); });
  button(f, "BACK", 42, 980, 170, 58, "#e2d4bc", "←");

  const info = panel(f, "DRY SUBURB", 1510, 172, 360, 805);
  text(info, "Close", "×", 318, 22, 24, 24, 28, "regular", C.ink, "CENTER");
  image(info, "mapDry", "Location Thumbnail", 22, 72, 316, 174, "FILL", 5, C.line);
  text(info, "Desc", "Once quiet suburbs, now picked clean. Homes hold\nuseful scraps, but watch for prowlers.", 22, 262, 316, 48, 15, "regular", C.ink);
  const powerBox = makeFrame(info, "Recommended Power", 22, 328, 150, 96, "#f3e5cd", C.line, 5, 1);
  text(powerBox, "Label", "RECOMMENDED POWER", 0, 16, 150, 20, 11, "bold", C.muted, "CENTER");
  text(powerBox, "Value", "450", 0, 48, 150, 34, 28, "bold", C.ink, "CENTER");
  const risk = makeFrame(info, "Risk Level", 188, 328, 150, 96, "#f3e5cd", C.line, 5, 1);
  text(risk, "Label", "RISK LEVEL", 0, 16, 150, 20, 11, "bold", C.muted, "CENTER");
  text(risk, "Value", "MEDIUM RISK", 0, 44, 150, 24, 17, "bold", C.gold, "CENTER");
  text(risk, "Dots", "●  ●  ○", 0, 68, 150, 22, 20, "bold", C.gold, "CENTER");
  const rewards = makeFrame(info, "Possible Rewards", 22, 442, 316, 80, "#f3e5cd", C.line, 5, 1);
  text(rewards, "Label", "POSSIBLE REWARDS", 18, 12, 170, 18, 11, "bold", C.muted);
  ["resFood","resWater","resScrap","eqRadio","resParts"].forEach((key, i) => image(rewards, key, "Reward " + i, 18 + i * 58, 36, 36, 36, "FIT"));
  button(info, "PLAN EXPEDITION", 22, 542, 316, 64, C.teal, "✦");
  button(info, "TRACK ROUTE", 22, 620, 316, 54, "#e2d4bc", "↬");
  [["EST. TIME","35m"],["DISTANCE","1.8 km"],["ROUTE SAFETY","72%"]].forEach((r, i) => {
    const b = makeFrame(info, "Metric / " + r[0], 22 + i * 105, 695, 96, 58, "#f3e5cd", C.line, 5, 1);
    text(b, "Label", r[0], 0, 10, 96, 16, 10, "bold", C.muted, "CENTER");
    text(b, "Value", r[1], 0, 30, 96, 22, 17, "bold", C.ink, "CENTER");
  });
  const update = makeFrame(f, "Scout Data Footer", 1360, 1008, 520, 48, "#f3e5cd", C.line, 5, 1);
  text(update, "Last Scouted", "Last Scouted: 2h 18m ago", 24, 15, 210, 18, 13, "regular", C.ink);
  button(update, "UPDATE SCOUT DATA", 270, 6, 230, 36, C.teal, "⌁");
  return f;
}

const pageName = "Ashfall Camp - Elementwise Concept";
const existing = figma.root.children.filter(p => p.name === pageName);
const fallback = figma.root.children.find(p => p.name !== pageName);
if (fallback) await figma.setCurrentPageAsync(fallback);
for (const p of existing) {
  if (figma.root.children.length > 1) p.remove();
}
const page = figma.createPage();
page.name = pageName;
await figma.setCurrentPageAsync(page);

buildResourcePanel(0, 0);
buildDashboard(0, 240);
buildBuildings(2040, 240);
buildExpedition(4080, 240);
buildWorkshop(0, 1500);
buildMap(2040, 1500);

figma.viewport.scrollAndZoomIntoView(page.children);
return { pageName, imageTargets, frameIds };
`;

async function main() {
  await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-elementwise-ashfall", version: "1.0.0" },
  });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  await waitForBridge();

  const createResult = parseJsonContent(await callTool("figma_execute", { code: figmaCode, timeout: 60000 }, 180000));
  writeFileSync(join(outDir, "create-result.json"), JSON.stringify(createResult, null, 2));
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
  writeFileSync(join(outDir, "fill-results.json"), JSON.stringify(fillResults, null, 2));

  const screenshots = [];
  for (const item of createResult.result.frameIds) {
    const shot = await callTool("figma_take_screenshot", {
      nodeId: item.frameId,
      scale: item.title.startsWith("00") ? 1 : 0.42,
      format: "png",
    }, 120000);
    const saved = saveScreenshot(item.title, shot);
    screenshots.push({ title: item.title, frameId: item.frameId, ...saved });
  }
  writeFileSync(join(outDir, "screenshots.json"), JSON.stringify(screenshots, null, 2));
  console.log(JSON.stringify({
    pageName: createResult.result.pageName,
    imageTargetCount: createResult.result.imageTargets.length,
    uniqueImageFillGroups: fillResults.length,
    frames: createResult.result.frameIds,
    screenshots: screenshots.map(s => s.filePath),
  }, null, 2));
}

main().catch((error) => {
  console.error(error.stack || error.message);
  process.exitCode = 1;
}).finally(killTree);
