import { spawn, spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const outDir = join(process.cwd(), "tmp", "figma-missing-screens");
const shotsDir = join(outDir, "screenshots");
mkdirSync(shotsDir, { recursive: true });

const P = (...parts) => join(process.cwd(), ...parts);

const assetPaths = {
  bgCamp: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Backgrounds", "ui_bg_camp_overview_01.png"),
  bgReport: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Backgrounds", "ui_bg_after_action_report_01.png"),
  bgSurvivors: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Backgrounds", "ui_bg_survivors_roster_01.png"),

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
  resIntel: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_radio_intel.png"),
  resFuel: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_fuel.png"),
  resElectronics: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_electronics.png"),

  statusHealthy: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_healthy.png"),
  statusWounded: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_wounded.png"),
  statusHealing: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_healing.png"),
  statusDanger: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_danger.png"),
  statusSafe: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_safe.png"),
  statusMorale: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_morale.png"),
  statusScouting: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_scouting.png"),
  statusAssigned: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_assigned.png"),
  statusIdle: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_idle.png"),
  statusFatigue: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_fatigue.png"),
  statusLevel: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_level_up.png"),

  eqRifle: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_rifle.png"),
  eqPistol: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_pistol.png"),
  eqShotgun: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_shotgun.png"),
  eqVest: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_armor_vest.png"),
  eqMedkit: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_medkit.png"),
  eqBackpack: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_backpack.png"),
  eqCanteen: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_canteen.png"),
  eqRadio: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_radio.png"),
  eqAmmo: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_ammo_box.png"),
  eqHatchet: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_hatchet.png"),
  eqMachete: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_machete.png"),
  eqTape: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_duct_tape.png"),
  eqFlashlight: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_flashlight.png"),

  radioOperator: P("Assets", "AshfallCamp", "Art", "UI", "Production", "RadioRecruitment", "ui_illustration_radio_operator_clean.png"),
  radioSignalMeter: P("Assets", "AshfallCamp", "Art", "UI", "Production", "RadioRecruitment", "ui_radio_signal_meter_empty.png"),

  reportOutcome: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Reports", "ui_report_outcome_badge_socket.png"),
  reportThumb: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Reports", "ui_report_thumbnail_frame_empty.png"),
  reportReward: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Reports", "ui_report_reward_slot_empty.png"),

  survivor01: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_01.png"),
  survivor02: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_02.png"),
  survivor03: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_03.png"),
  survivor04: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_04.png"),
  survivor05: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_05.png"),
  survivor06: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_06.png"),
  survivor07: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_07.png"),
  survivor08: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_08.png"),
  enemyRaider: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_enemy_raider_01.png"),
  enemyGhoul: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_creature_weak_feral_ghoul_01.png"),
  enemyHound: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_creature_mid_boneback_hound_01.png"),
  enemyDuelist: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_enemy_mid_pistol_duelist_01.png"),
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
  panel: "#f0dfbd",
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

function ellipse(parent, name, x, y, w, h, fill = C.parchment2, strokeHex = null, opacity = 1) {
  const n = figma.createEllipse();
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
  n.textAutoResize = "NONE";
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

function slider(parent, name, x, y, w, value, color = C.green, h = 8, handle = true) {
  const g = frame(parent, "Slider - " + name, x, y, w, 22, null);
  rect(g, "Track", 0, 7, w, h, "#d8c5a6", "#b99e75", h / 2);
  rect(g, "Fill", 0, 7, Math.max(4, w * value), h, color, null, h / 2);
  if (handle) {
    const knob = ellipse(g, "Handle", Math.max(0, w * value - 6), 3, 14, 14, C.parchment2, color);
    knob.strokeWeight = 2;
  }
  return g;
}

function signalScope(parent, name, x, y, w, h) {
  const g = frame(parent, name, x, y, w, h, "#263027", "#6f775f", 5);
  rect(g, "Inner Glow", 6, 6, w - 12, h - 12, "#1f2a22", null, 3, 0.9);
  for (let i = 1; i < 8; i++) {
    const gx = 6 + i * ((w - 12) / 8);
    line(g, "Grid V " + i, gx, 8, gx, h - 8, "#55624f", 1).opacity = 0.35;
  }
  for (let i = 1; i < 4; i++) {
    const gy = 8 + i * ((h - 16) / 4);
    line(g, "Grid H " + i, 8, gy, w - 8, gy, "#55624f", 1).opacity = 0.35;
  }
  line(g, "Center Baseline", 8, 42, w - 8, 42, "#7fb260", 1).opacity = 0.35;
  const bars = [8, 12, 20, 30, 18, 42, 56, 32, 24, 64, 38, 26, 18, 34, 46, 28, 20, 14, 22, 30, 18, 12, 8, 10];
  bars.forEach((height, i) => {
    const bx = 18 + i * 13;
    const by = 42 - height / 2;
    rect(g, "Wave Bar " + i, bx, by, 4, height, "#9fcf76", null, 2, 0.9);
  });
  return g;
}

function sectionTitle(parent, label, x, y, w, size = 20) {
  txt(parent, "Title - " + label, label, x, y, w, 30, size, "Bold", C.teal);
  line(parent, "Rule - " + label, x, y + 34, x + w, y + 34, C.line, 1);
}

function panel(parent, name, x, y, w, h) {
  const p = frame(parent, "Panel - " + name, x, y, w, h, C.parchment2, C.line, 6);
  rect(p, "Soft Fill", 0, 0, w, h, C.parchment2, null, 6, 0.7);
  return p;
}

function button(parent, label, x, y, w, h, fill = C.green, icon = "") {
  const b = frame(parent, "Button - " + label, x, y, w, h, fill, C.line2, 5, 1);
  const bright = fill === C.green || fill === C.teal || fill === C.tealDark || fill === C.red || fill === C.gold || fill === C.blue;
  txt(b, "Label", icon ? icon + "  " + label : label, 12, 0, w - 24, h, Math.min(19, Math.max(12, h * 0.34)), "Bold", bright ? C.white : C.ink, "CENTER");
  return b;
}

function brand(parent, compact = false) {
  const g = frame(parent, "Brand", 44, 24, compact ? 430 : 520, 92, null);
  rect(g, "Banner", 0, 0, 84, 92, C.teal, null, 0, 0.9);
  img(g, "navCamp", "Camp Mark", 18, 18, 48, 48, "FIT");
  txt(g, "Name", "ASHFALL CAMP", 110, 0, compact ? 300 : 390, 58, compact ? 36 : 40, "Bold", C.ink);
  txt(g, "Motto", "REBUILD  -  SURVIVE  -  THRIVE", 114, 58, 330, 26, 13, "Bold", C.teal);
  line(g, "Motto Rule Left", 112, 57, 162, 57, C.teal, 1);
  line(g, "Motto Rule Right", compact ? 340 : 438, 57, compact ? 404 : 502, 57, C.teal, 1);
  return g;
}

function topNav(parent, active) {
  const items = [
    ["CAMP", "navCamp"],
    ["SURVIVORS", "navSurvivors"],
    ["MISSIONS", "navExpedition"],
    ["WORKSHOP", "navWorkshop"],
    ["RADIO", "navRadio"],
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

function pageFrame(name, x, y, bgKey, activeNav = null) {
  const f = frame(figma.currentPage, "Ashfall Camp - Full HD Elements / " + name, x, y, 1920, 1080, C.parchment);
  frameIds.push({ title: name, frameId: f.id });
  img(f, bgKey, "Background Art", 0, 0, 1920, 1080, "FILL", 0, null, 0.18);
  rect(f, "Parchment Wash", 0, 0, 1920, 1080, C.parchment, null, 0, 0.82);
  brand(f);
  if (activeNav) topNav(f, activeNav);
  return f;
}

function statChip(parent, iconKey, label, value, x, y, w) {
  const g = frame(parent, "Stat - " + label, x, y, w, 38, null);
  const valueText = String(value);
  const valueW = valueText.length > 4 ? 82 : 52;
  img(g, iconKey, "Icon", 0, 5, 28, 28, "FIT");
  txt(g, "Label", label, 38, 0, w - valueW - 44, 18, 10, "Bold", C.muted);
  txt(g, "Value", valueText, w - valueW, 0, valueW, 38, valueText.length > 4 ? 12 : 18, "Bold", C.ink, "RIGHT");
  return g;
}

function resourceCost(parent, items, x, y) {
  items.forEach((item, i) => {
    img(parent, item[0], "Cost Icon - " + item[1], x + i * 92, y, 30, 30, "FIT");
    txt(parent, "Cost Value - " + item[1], String(item[2]), x + i * 92 + 38, y - 2, 42, 34, 17, "Bold", C.ink);
  });
}

function buildRadio(x, y) {
  const f = pageFrame("08 Radio Recruitment Elementwise", x, y, "bgCamp", "RADIO");
  txt(f, "Screen Eyebrow", "RADIO / RECRUITMENT", 690, 108, 520, 38, 27, "Bold", C.teal, "CENTER");
  line(f, "Title Rule Left", 590, 126, 660, 126, C.teal, 1);
  line(f, "Title Rule Right", 1240, 126, 1310, 126, C.teal, 1);
  const quote = panel(f, "Quote Strip", 1482, 32, 360, 96);
  txt(quote, "Quote", "\"We don't just survive out here.\nWe build tomorrow.\"", 34, 12, 260, 50, 16, "Semi Bold", C.teal);

  const call = panel(f, "Broadcast Call", 36, 158, 410, 430);
  sectionTitle(call, "BROADCAST CALL", 72, 24, 288);
  img(call, "navRadio", "Radio Icon", 22, 22, 38, 38, "FIT");
  img(call, "radioOperator", "Radio Operator", 250, 56, 116, 112, "FIT", 5, C.line, 0.9);
  txt(call, "Description", "Send a call for survivors using the camp radio. Higher investment reaches farther and attracts more skilled people.", 32, 92, 220, 96, 18, "Regular", C.ink);
  line(call, "Cost Rule", 32, 210, 370, 210, C.line, 1);
  txt(call, "Cost Label", "RESOURCE COST", 32, 218, 200, 24, 13, "Bold", C.muted);
  resourceCost(call, [["resScrap", "Scrap", 120], ["resWater", "Water", 60], ["resIntel", "Intel", 30]], 36, 258);
  txt(call, "Strength Label", "CALL STRENGTH", 32, 322, 150, 22, 13, "Bold", C.muted);
  slider(call, "Call Strength", 36, 352, 230, 0.72, C.green, 12);
  txt(call, "Strength State", "GOOD", 300, 346, 70, 30, 16, "Bold", C.green, "CENTER");
  button(call, "BROADCAST NOW", 32, 378, 338, 42, C.green, "");

  const tuner = panel(f, "Signal Tuner", 36, 608, 410, 402);
  sectionTitle(tuner, "SIGNAL TUNER", 72, 22, 288);
  img(tuner, "eqRadio", "Tuner Icon", 24, 22, 36, 36, "FIT");
  signalScope(tuner, "Signal Waveform Scope", 32, 82, 346, 80);
  txt(tuner, "Frequency Label", "FREQUENCY", 32, 178, 150, 20, 12, "Bold", C.muted);
  button(tuner, "-", 32, 208, 44, 40, C.teal, "");
  const freq = frame(tuner, "Stepper - Frequency", 88, 208, 216, 40, C.parchment, C.line, 4);
  txt(freq, "Value", "107.45 MHz", 0, 0, 216, 40, 20, "Bold", C.ink, "CENTER");
  button(tuner, "+", 318, 208, 44, 40, C.teal, "");
  txt(tuner, "Range Label", "SCAN RANGE", 32, 274, 150, 20, 12, "Bold", C.muted);
  slider(tuner, "Scan Range", 42, 300, 296, 0.55, C.teal, 8);
  txt(tuner, "Range Low", "100 MHz", 32, 326, 90, 22, 13, "Semi Bold", C.ink);
  txt(tuner, "Range High", "120 MHz", 286, 326, 90, 22, 13, "Semi Bold", C.ink, "RIGHT");
  button(tuner, "SCAN FOR SIGNALS", 32, 358, 338, 42, C.blue, "");

  const candidates = panel(f, "Incoming Survivor Candidates", 470, 158, 904, 632);
  sectionTitle(candidates, "INCOMING SURVIVOR CANDIDATES", 72, 22, 610);
  img(candidates, "navRadio", "Radio Waves", 26, 22, 34, 34, "FIT");
  txt(candidates, "Signal State", "New signals detected.", 650, 20, 190, 30, 13, "Medium", C.ink, "RIGHT");
  ellipse(candidates, "Signal Dot", 854, 32, 10, 10, C.gold, null);
  const recruits = [
    { name: "MAYA", role: "Builder", level: 3, portrait: "survivor01", badge: "eqHatchet", desc: "Used to fix just about anything. Great with structures and tools.", costs: [["resScrap", 80], ["resWater", 40], ["resIntel", 20]] },
    { name: "HENRY", role: "Cook", level: 4, portrait: "survivor02", badge: "resFood", desc: "Keeps spirits up with hearty meals and practical wisdom.", costs: [["resScrap", 60], ["resWater", 30], ["resIntel", 15]] },
    { name: "LEAH", role: "Medic", level: 3, portrait: "survivor03", badge: "eqMedkit", desc: "Calm under pressure. Knows how to treat wounds and illness.", costs: [["resScrap", 80], ["resWater", 40], ["resIntel", 20]] },
    { name: "JAX", role: "Scout", level: 2, portrait: "survivor04", badge: "eqRadio", desc: "Eyes and ears of the wasteland. Finds routes and resources.", costs: [["resScrap", 80], ["resWater", 30], ["resIntel", 15]] },
  ];
  recruits.forEach((r, i) => {
    const x0 = 30 + i * 216;
    const card = frame(candidates, "Recruit Card - " + r.name, x0, 92, 200, 500, C.parchment, C.line, 6);
    img(card, r.portrait, "Portrait", 20, 20, 160, 168, "FILL", 6, C.line);
    ellipse(card, "Role Badge Socket", 146, 22, 42, 42, C.teal, C.parchment);
    img(card, r.badge, "Role Badge", 154, 30, 26, 26, "FIT");
    txt(card, "Name", r.name, 20, 194, 110, 34, 23, "Bold", C.ink);
    txt(card, "Role", r.role, 20, 232, 100, 24, 15, "Medium", C.ink);
    txt(card, "Level", "Level " + r.level, 120, 232, 56, 24, 14, "Bold", C.teal, "RIGHT");
    line(card, "Desc Rule", 20, 270, 180, 270, C.line, 1);
    txt(card, "Description", r.desc, 20, 282, 160, 70, 13, "Regular", C.ink);
    txt(card, "Skills Label", "Skills:", 20, 374, 52, 22, 12, "Medium", C.ink);
    img(card, r.badge, "Skill 1", 76, 372, 24, 24, "FIT");
    img(card, i === 1 ? "statusMorale" : i === 2 ? "resMedicine" : "eqTape", "Skill 2", 112, 372, 24, 24, "FIT");
    img(card, i === 3 ? "navRadio" : "resParts", "Skill 3", 148, 372, 24, 24, "FIT");
    button(card, "RECRUIT", 20, 410, 160, 42, C.green, "");
    r.costs.forEach((c, j) => {
      img(card, c[0], "Recruit Cost Icon " + j, 30 + j * 52, 462, 20, 20, "FIT");
      txt(card, "Recruit Cost " + j, String(c[1]), 54 + j * 52, 460, 30, 24, 12, "Bold", C.ink);
    });
  });

  const future = panel(f, "Future Contacts", 470, 812, 904, 198);
  sectionTitle(future, "FUTURE CONTACTS", 72, 20, 610);
  img(future, "navSurvivors", "People Icon", 26, 20, 34, 34, "FIT");
  txt(future, "Hint", "Keep calling. More voices out there.", 670, 20, 190, 30, 13, "Medium", C.ink, "RIGHT");
  for (let i = 0; i < 5; i++) {
    const locked = frame(future, "Locked Contact Slot " + (i + 1), 30 + i * 170, 78, 148, 96, C.panel, C.line, 5);
    txt(locked, "Lock", "LOCKED", 0, 14, 148, 24, 14, "Bold", C.muted, "CENTER");
    txt(locked, "Name", "Unknown Contact", 0, 42, 148, 22, 12, "Medium", C.ink, "CENTER");
    txt(locked, "Copy", "Continue broadcasting\nto unlock.", 16, 64, 116, 28, 10, "Regular", C.ink, "CENTER");
  }

  const intel = panel(f, "Signal Intel", 1404, 158, 480, 760);
  sectionTitle(intel, "SIGNAL INTEL", 72, 22, 350);
  img(intel, "statusScouting", "Intel Icon", 26, 22, 36, 36, "FIT");
  const intelRows = [
    ["OLD HIGHWAY RUN", "Distance: Near", "A rest stop and service road still show weak movement.", ["resScrap", "resParts", "resWater"]],
    ["REDWOOD OUTSKIRTS", "Distance: Moderate", "A logging camp is quiet but useful signals repeat.", ["resFood", "resWater", "resScrap"]],
    ["ABANDONED HOSPITAL", "Distance: Far", "Medical pings repeat through a cracked relay tower.", ["resMedicine", "eqMedkit", "resWater"]],
  ];
  intelRows.forEach((row, i) => {
    const r = frame(intel, "Intel Card - " + row[0], 28, 90 + i * 200, 424, 170, C.parchment, C.line, 6);
    img(r, i === 0 ? "eqRadio" : i === 1 ? "statusScouting" : "resMedicine", "Signal Icon", 18, 18, 54, 54, "FIT", 4, C.line);
    txt(r, "Name", row[0], 90, 18, 250, 26, 17, "Bold", C.ink);
    txt(r, "Distance", row[1], 90, 48, 180, 24, 13, "Medium", C.ink);
    txt(r, "Desc", row[2], 90, 78, 280, 42, 13, "Regular", C.ink);
    txt(r, "Rewards Label", "REWARDS", 90, 128, 80, 20, 11, "Bold", C.muted);
    row[3].forEach((icon, j) => img(r, icon, "Reward " + j, 172 + j * 38, 124, 28, 28, "FIT"));
  });
  const footer = panel(f, "Radio Footer Note", 1404, 940, 480, 70);
  img(footer, "eqRadio", "Footer Radio", 24, 18, 34, 34, "FIT");
  txt(footer, "Copy", "Better signals. Better people. Stronger tomorrow.", 76, 16, 350, 34, 16, "Medium", C.ink);
}

const party = [
  { name: "JADE", role: "SCOUT", level: 18, hp: "420/420", value: 1, fatigue: 0.3, portrait: "survivor01", icon: "eqRifle", color: C.green },
  { name: "BORIS", role: "GUARD", level: 17, hp: "460/460", value: 1, fatigue: 0.32, portrait: "survivor02", icon: "eqVest", color: C.blue },
  { name: "MIRA", role: "MEDIC", level: 16, hp: "380/380", value: 1, fatigue: 0.42, portrait: "survivor03", icon: "eqMedkit", color: C.gold },
  { name: "OWEN", role: "RANGER", level: 16, hp: "440/440", value: 1, fatigue: 0.35, portrait: "survivor04", icon: "eqTape", color: C.rust },
];

const enemies = [
  { name: "RAIDER SCOUT", level: 14, hp: "320/320", portrait: "enemyRaider", tag: "EVASIVE", icon: "eqRifle", atk: 46, def: 12, spd: 14 },
  { name: "FERAL GHOUL", level: 14, hp: "360/360", portrait: "enemyGhoul", tag: "RAGE", icon: "eqMachete", atk: 54, def: 10, spd: 9 },
  { name: "MUTANT HOUND", level: 14, hp: "380/380", portrait: "enemyHound", tag: "PACK HUNTER", icon: "statusDanger", atk: 50, def: 14, spd: 13 },
  { name: "PISTOL DUELIST", level: 15, hp: "300/300", portrait: "enemyDuelist", tag: "SUPPRESS", icon: "eqPistol", atk: 58, def: 9, spd: 12 },
];

function compactSquadCard(parent, data, index, x) {
  const c = frame(parent, "Squad Compact Card - " + data.name, x, 64, 120, 174, C.parchment, C.line, 5);
  ellipse(c, "Index Badge", 8, 8, 28, 28, data.color, C.parchment);
  txt(c, "Index", String(index + 1), 8, 8, 28, 28, 16, "Bold", C.white, "CENTER");
  img(c, data.portrait, "Portrait", 16, 28, 88, 98, "FILL", 5, C.line);
  img(c, data.icon, "Role Icon", 86, 12, 24, 24, "FIT");
  txt(c, "Hp", data.hp, 14, 126, 92, 24, 15, "Bold", C.white, "CENTER");
  slider(c, "HP", 14, 150, 92, data.value, C.green, 7, false);
  slider(c, "Fatigue", 14, 166, 92, data.fatigue, C.gold, 5, false);
}

function battleCard(parent, data, x, y, enemy = false) {
  const card = frame(parent, (enemy ? "Enemy Card - " : "Player Card - ") + data.name, x, y, 190, 392, enemy ? "#332c23" : "#362f25", C.line2, 6, 2);
  rect(card, "Inner Parchment", 8, 8, 174, 376, "#2b2720", "#13110e", 4, 0.92);
  ellipse(card, "Index Socket", 16, 18, 30, 30, enemy ? C.rust : data.color, C.parchment);
  txt(card, "Index", enemy ? "!" : String(party.indexOf(data) + 1), 16, 18, 30, 30, 15, "Bold", C.white, "CENTER");
  txt(card, "Name", data.name, 54, 18, 94, 28, 17, "Bold", C.white);
  txt(card, "Level", "LVL " + data.level, 140, 20, 42, 24, 11, "Bold", C.white, "RIGHT");
  txt(card, "Role", enemy ? data.tag : data.role, 54, 44, 100, 22, 12, "Bold", enemy ? C.rust : C.gold);
  img(card, data.portrait, "Portrait", 14, 72, 162, 178, "FILL", 5, "#111111");
  txt(card, "Hp Value", data.hp, 20, 246, 150, 34, 23, "Bold", C.white, "CENTER");
  slider(card, "HP", 20, 284, 150, enemy ? 0.86 : 1, enemy ? C.rust : C.green, 9, false);
  img(card, data.icon, "Trait Icon", 20, 316, 24, 24, "FIT");
  txt(card, "Trait", enemy ? data.tag : data.role, 52, 312, 112, 28, 13, "Bold", enemy ? C.rust : data.color);
  const slots = [
    enemy ? data.icon : "eqRifle",
    enemy ? "eqVest" : "eqVest",
    enemy ? "eqBackpack" : "eqBackpack",
  ];
  slots.forEach((key, i) => {
    rect(card, "Ability Slot " + i, 22 + i * 48, 342, 38, 34, "#201c17", C.line2, 3);
    img(card, key, "Ability Icon " + i, 27 + i * 48, 346, 28, 26, "FIT");
  });
  const statStrip = frame(card, "Stats Strip", 16, 366, 158, 20, "#17130f", C.line2, 2);
  line(statStrip, "Stat Divider 1", 52, 3, 52, 17, "#4c3b24", 1);
  line(statStrip, "Stat Divider 2", 104, 3, 104, 17, "#4c3b24", 1);
  const statValues = [
    ["eqMachete", String(enemy ? data.atk : data.level * 3)],
    ["eqVest", String(enemy ? data.def : Math.max(10, data.level))],
    ["statusFatigue", String(enemy ? data.spd : Math.max(8, data.level - 5))],
  ];
  statValues.forEach((stat, i) => {
    img(statStrip, stat[0], "Stat Icon " + i, 8 + i * 52, 2, 16, 16, "FIT");
    txt(statStrip, "Stat Value " + i, stat[1], 25 + i * 52, 0, 22, 20, 12, "Bold", C.gold, "CENTER");
  });
}

function buildCombat(x, y) {
  const f = pageFrame("09 Expedition Combat Elementwise", x, y, "bgReport", null);
  txt(f, "Screen Title", "EXPEDITION MONITOR", 610, 20, 610, 46, 36, "Bold", C.ink, "CENTER");
  txt(f, "Mission Line", "MISSION: SCAVENGE RUN    NORMAL    DAY 7", 610, 66, 610, 30, 18, "Bold", C.ink, "CENTER");

  const squad = panel(f, "Squad", 18, 112, 594, 280);
  sectionTitle(squad, "SQUAD", 18, 12, 160);
  txt(squad, "Count", "4/4", 470, 12, 80, 30, 20, "Bold", C.ink, "RIGHT");
  party.forEach((p, i) => compactSquadCard(squad, p, i, 18 + i * 140));

  const encounter = panel(f, "Encounter Intel", 628, 112, 420, 280);
  sectionTitle(encounter, "ENCOUNTER", 18, 12, 260);
  txt(encounter, "Threat", "THREAT: HIGH", 300, 16, 96, 24, 13, "Bold", C.rust, "RIGHT");
  const overview = frame(encounter, "Encounter Conditions", 22, 64, 376, 146, C.parchment, C.line, 5);
  txt(overview, "Name", "AMBUSH SITE", 20, 16, 180, 28, 18, "Bold", C.teal);
  txt(overview, "Body", "Enemy movement blocks the road. Spend actions to prepare, then resolve the clash.", 20, 54, 330, 50, 15, "Regular", C.ink);
  img(overview, "statusDanger", "Enemies Icon", 20, 112, 24, 24, "FIT");
  txt(overview, "Enemies Label", "ENEMIES", 54, 104, 82, 20, 10, "Bold", C.muted);
  txt(overview, "Enemies Value", "4", 140, 104, 32, 38, 20, "Bold", C.ink, "CENTER");
  img(overview, "statusSafe", "Victory Icon", 204, 112, 24, 24, "FIT");
  txt(overview, "Victory Label", "VICTORY", 238, 104, 76, 20, 10, "Bold", C.muted);
  txt(overview, "Victory Value", "Defeat all", 308, 104, 58, 38, 11, "Bold", C.ink, "RIGHT");
  const rules = frame(encounter, "Round Rules", 22, 222, 376, 40, C.parchment, C.line, 4);
  txt(rules, "Limit", "ROUND LIMIT", 14, 0, 120, 40, 12, "Bold", C.muted);
  txt(rules, "Value", "UNLIMITED", 148, 0, 96, 40, 15, "Bold", C.ink, "CENTER");
  txt(rules, "Distance", "DISTANCE LEFT  5", 262, 0, 100, 40, 12, "Bold", C.ink, "RIGHT");

  const status = panel(f, "Time And Status", 1064, 112, 240, 280);
  sectionTitle(status, "TIME & STATUS", 18, 12, 186);
  statChip(status, "statusIdle", "TIME", "10:30", 24, 66, 174);
  slider(status, "Daylight", 92, 92, 88, 0.58, C.teal, 8);
  statChip(status, "statusDanger", "DANGER", "HIGH", 24, 124, 174);
  slider(status, "Danger", 92, 150, 88, 0.74, C.rust, 8);
  statChip(status, "resFuel", "SUPPLIES", "LOW", 24, 184, 174);
  slider(status, "Supplies", 92, 210, 88, 0.36, C.gold, 8);

  const log = panel(f, "Combat Log", 1320, 112, 270, 280);
  sectionTitle(log, "COMBAT LOG", 18, 12, 210);
  ["Round 1 begins", "Jade gains stealth", "Raider scout charges", "Boris braces", "Enemy prepares"].forEach((entry, i) => {
    ellipse(log, "Log Dot " + i, 22, 70 + i * 32, 8, 8, i === 2 || i === 4 ? C.rust : C.teal, null);
    txt(log, "Log Entry " + i, entry, 40, 58 + i * 32, 190, 32, 12, "Medium", C.ink);
  });
  button(log, "OPEN LOG", 54, 228, 160, 42, C.parchment, "");

  const action = panel(f, "Actions", 1606, 112, 296, 280);
  sectionTitle(action, "ACTIONS", 18, 12, 220);
  button(action, "RETREAT", 28, 70, 240, 42, C.teal, "");
  button(action, "USE ITEM", 28, 126, 240, 42, C.green, "");
  button(action, "VIEW ENEMY", 28, 182, 240, 42, C.gold, "");
  button(action, "END TURN", 28, 238, 240, 42, C.rust, "");

  const board = frame(f, "Combat Board", 18, 414, 1884, 510, "#5f533f", C.line2, 6);
  rect(board, "Board Wash", 0, 0, 1884, 510, "#6d6049", null, 6, 0.72);
  const phase = frame(board, "Phase Track", 276, 16, 1320, 48, null);
  rect(phase, "Player Turn Fill", 0, 8, 630, 28, C.green, C.greenDark, 2);
  txt(phase, "Player Turn Label", "PLAYER TURN", 0, 8, 630, 28, 15, "Bold", C.white, "CENTER");
  ellipse(phase, "Clash Seal", 610, 0, 58, 58, C.charcoal, C.gold);
  txt(phase, "Clash Text", "X", 610, 0, 58, 58, 24, "Bold", C.gold, "CENTER");
  rect(phase, "Clash Fill", 668, 8, 240, 28, C.gold, C.charcoal, 2);
  txt(phase, "Clash Label", "CLASH", 668, 8, 240, 28, 14, "Bold", C.ink, "CENTER");
  rect(phase, "Enemy Turn Fill", 908, 8, 410, 28, C.rust, "#6b2f20", 2);
  txt(phase, "Enemy Turn Label", "ENEMY TURN", 908, 8, 410, 28, 14, "Bold", C.white, "CENTER");
  txt(board, "Player Column Label", "SURVIVORS", 34, 18, 160, 34, 24, "Bold", C.white);
  txt(board, "Enemy Column Label", "ENEMIES", 1668, 18, 160, 34, 24, "Bold", C.white, "RIGHT");

  party.forEach((p, i) => battleCard(board, p, 34 + i * 210, 84, false));
  txt(board, "Versus", "VS", 884, 240, 120, 36, 18, "Bold", C.parchment, "CENTER");
  line(board, "Left Clash Arrow", 832, 258, 888, 258, C.line, 8);
  line(board, "Right Clash Arrow", 1000, 258, 1056, 258, C.line, 8);
  enemies.forEach((e, i) => battleCard(board, e, 1034 + i * 210, 84, true));

  const bottom = frame(f, "Bottom Action Bar", 18, 942, 1884, 116, C.parchment2, C.line, 6);
  const energy = frame(bottom, "Energy Counter", 20, 16, 290, 84, null);
  ellipse(energy, "Energy Icon", 8, 10, 64, 64, C.teal, C.gold);
  txt(energy, "Lightning", "E", 8, 10, 64, 64, 30, "Bold", C.gold, "CENTER");
  txt(energy, "Value", "4/5", 88, 18, 80, 50, 31, "Bold", C.ink);
  for (let i = 0; i < 6; i++) ellipse(energy, "Pip " + i, 176 + i * 24, 36, 14, 14, i < 5 ? C.teal : C.parchment, C.line);
  const abilities = frame(bottom, "Abilities", 330, 16, 560, 84, null);
  txt(abilities, "Title", "ABILITIES", 0, 0, 120, 28, 15, "Bold", C.ink);
  ["eqRifle", "eqVest", "eqMedkit", "statusScouting", "eqRadio"].forEach((key, i) => {
    ellipse(abilities, "Ability Socket " + i, 150 + i * 78, 16, 48, 48, C.parchment, C.line2);
    img(abilities, key, "Ability Icon " + i, 160 + i * 78, 26, 28, 28, "FIT");
    ellipse(abilities, "Ability Cost Socket " + i, 166 + i * 78, 64, 16, 16, C.greenDark, null);
    txt(abilities, "Ability Cost " + i, i === 2 ? "2" : "1", 166 + i * 78, 64, 16, 16, 9, "Bold", C.white, "CENTER");
  });
  const phaseInfo = frame(bottom, "Phase Info", 910, 16, 270, 84, null);
  txt(phaseInfo, "Title", "PHASE INFO", 0, 0, 120, 24, 13, "Bold", C.ink);
  txt(phaseInfo, "Copy", "Use abilities or move to prepare for the clash. Defeat all enemies to complete the encounter.", 0, 24, 250, 50, 12, "Regular", C.ink);
  const turn = frame(bottom, "Turn Counter", 1200, 16, 150, 84, null);
  txt(turn, "Title", "TURN", 0, 0, 150, 28, 13, "Bold", C.ink, "CENTER");
  txt(turn, "Value", "1 / INF", 0, 28, 150, 42, 28, "Bold", C.ink, "CENTER");
  button(bottom, "END TURN", 1370, 22, 476, 70, C.rust, "");
}

function reportResourceRow(parent, icon, label, source, value, y) {
  img(parent, icon, "Icon - " + label, 22, y + 5, 30, 30, "FIT");
  txt(parent, "Label - " + label, label, 64, y, 116, 20, 12, "Bold", C.ink);
  txt(parent, "Source - " + label, source, 64, y + 20, 112, 18, 9, "Regular", C.muted);
  txt(parent, "Value - " + label, value, 184, y, 52, 38, 14, "Bold", C.ink, "RIGHT");
  line(parent, "Rule - " + label, 22, y + 44, 236, y + 44, C.line, 1);
}

function buildReports(x, y) {
  const f = pageFrame("10 Reports Elementwise", x, y, "bgReport", null);
  rect(f, "Center Spine", 956, 0, 8, 1080, "#514538", null, 0, 0.25);

  const left = frame(f, "After Action Report Page", 26, 24, 900, 1032, C.parchment2, C.line2, 4);
  img(left, "bgReport", "Report Paper Texture", 0, 0, 900, 1032, "FILL", 4, null, 0.18);
  rect(left, "Report Paper Wash", 0, 0, 900, 1032, C.parchment2, null, 4, 0.88);
  brand(left, true);
  img(left, "reportThumb", "Location Photo Frame", 32, 146, 260, 176, "FIT", 4);
  img(left, "bgCamp", "Location Photo", 50, 164, 224, 136, "FILL", 4, C.line);
  txt(left, "Title", "AFTER ACTION REPORT", 320, 134, 520, 48, 31, "Bold", C.teal, "CENTER");
  txt(left, "Meta", "EXPEDITION TO: OLD HIGHWAY DEPOT       DURATION: 2 DAYS", 320, 188, 520, 28, 12, "Bold", C.muted, "CENTER");
  const outcome = frame(left, "Outcome Banner", 300, 230, 540, 130, C.parchment, C.line, 5);
  img(outcome, "reportOutcome", "Outcome Badge Socket", 22, 22, 78, 78, "FIT");
  ellipse(outcome, "Success Badge", 34, 34, 54, 54, C.green, null);
  txt(outcome, "Check", "OK", 34, 34, 54, 54, 18, "Bold", C.white, "CENTER");
  txt(outcome, "Status", "SUCCESS", 148, 20, 220, 46, 34, "Bold", C.green);
  txt(outcome, "Copy", "The expedition was a success.\nYour survivors returned safely with valuable supplies.", 150, 70, 298, 46, 12, "Medium", C.ink);
  txt(outcome, "Stamp", "WELL DONE", 430, 48, 88, 36, 12, "Bold", C.line2, "CENTER");

  const resources = panel(left, "Resources Gained", 32, 388, 260, 266);
  sectionTitle(resources, "RESOURCES GAINED", 22, 16, 210, 16);
  [
    ["resScrap", "SCRAP", "Salvaged metal", "+320"],
    ["resFood", "FOOD", "Enough to feed the camp", "+480"],
    ["resWater", "WATER", "Clean and safe", "+210"],
    ["resMedicine", "MEDICINE", "Vital supplies recovered", "+75"],
    ["resParts", "PARTS", "Useful mechanical components", "+120"],
  ].forEach((row, i) => reportResourceRow(resources, row[0], row[1], row[2], row[3], 56 + i * 38));

  const items = panel(left, "Items Found", 314, 388, 250, 266);
  sectionTitle(items, "ITEMS FOUND", 20, 16, 205, 16);
  [
    ["eqTape", "Tool Kit", "Used for building and repairs."],
    ["eqRadio", "Long-Range Radio", "Improves scouting range."],
    ["eqVest", "Field Jacket", "Better protection from elements."],
    ["resFood", "Box of Seeds", "Cotton and carrot seeds."],
  ].forEach((row, i) => {
    const yy = 58 + i * 50;
    img(items, row[0], "Item Icon " + i, 20, yy, 42, 42, "FIT", 4, C.line);
    txt(items, "Item Name " + i, row[1], 76, yy, 130, 22, 13, "Bold", C.ink);
    txt(items, "Item Desc " + i, row[2], 76, yy + 22, 140, 20, 10, "Regular", C.muted);
  });

  const xp = panel(left, "Survivor XP", 586, 388, 254, 266);
  sectionTitle(xp, "SURVIVOR XP", 20, 16, 210, 16);
  party.forEach((p, i) => {
    const yy = 58 + i * 48;
    img(xp, p.portrait, "Portrait " + i, 20, yy, 38, 38, "FILL", 4, C.line);
    txt(xp, "Name " + i, p.name, 70, yy, 88, 18, 11, "Bold", C.ink);
    txt(xp, "Gain " + i, "+240 XP", 160, yy, 66, 18, 11, "Bold", C.ink, "RIGHT");
    slider(xp, "XP " + p.name, 70, yy + 20, 130, 0.54 + i * 0.08, C.green, 6, false);
  });

  const wounds = panel(left, "Wounds And Conditions", 32, 672, 330, 230);
  sectionTitle(wounds, "WOUNDS & CONDITIONS", 20, 14, 290, 16);
  [
    ["survivor01", "MAYA", "Minor scratches", "statusHealthy"],
    ["survivor02", "HENRY", "Sprained ankle", "statusHealing"],
    ["survivor03", "LEAH", "Fatigue", "statusHealthy"],
    ["survivor04", "JAX", "Minor scratches", "statusHealthy"],
  ].forEach((row, i) => {
    const yy = 54 + i * 42;
    img(wounds, row[0], "Portrait " + i, 22, yy, 34, 34, "FILL", 3, C.line);
    txt(wounds, "Name " + i, row[1], 66, yy, 78, 16, 10, "Bold", C.ink);
    txt(wounds, "State " + i, row[2], 66, yy + 16, 130, 16, 10, "Regular", C.muted);
    img(wounds, row[3], "Status " + i, 286, yy + 6, 24, 24, "FIT");
  });

  const events = panel(left, "Events Encountered", 384, 672, 456, 230);
  sectionTitle(events, "EVENTS ENCOUNTERED", 20, 14, 400, 16);
  [
    ["statusSafe", "Friendly Survivors", "Met a group of survivors and traded supplies.", "Good"],
    ["eqBackpack", "Abandoned Cache", "Discovered a hidden stash of useful items.", "Good"],
    ["statusDanger", "Bandit Ambush", "Ambushed on the road, but fought them off.", "Danger"],
    ["resIntel", "Mysterious Signal", "Picked up a strange radio signal.", "Intel"],
  ].forEach((row, i) => {
    const yy = 54 + i * 42;
    img(events, row[0], "Event Icon " + i, 18, yy, 30, 30, "FIT");
    txt(events, "Event Name " + i, row[1], 60, yy, 160, 17, 11, "Bold", C.ink);
    txt(events, "Event Desc " + i, row[2], 60, yy + 17, 270, 16, 10, "Regular", C.muted);
    txt(events, "Event State " + i, row[3], 356, yy, 72, 30, 12, "Bold", row[3] === "Danger" ? C.rust : C.green, "RIGHT");
  });
  button(left, "SEND AGAIN", 34, 934, 250, 54, C.green, "");
  button(left, "MANAGE SQUAD", 318, 934, 250, 54, C.teal, "");
  button(left, "BACK TO CAMP", 602, 934, 238, 54, C.parchment, "");

  const right = frame(f, "Offline Progress Report Page", 994, 24, 900, 1032, C.parchment2, C.line2, 4);
  img(right, "bgReport", "Report Paper Texture", 0, 0, 900, 1032, "FILL", 4, null, 0.18);
  rect(right, "Report Paper Wash", 0, 0, 900, 1032, C.parchment2, null, 4, 0.88);
  brand(right, true);
  txt(right, "Title", "OFFLINE PROGRESS REPORT", 320, 134, 520, 48, 31, "Bold", C.teal, "CENTER");
  txt(right, "Away Time", "YOU WERE AWAY FOR: 8 HOURS", 320, 184, 520, 26, 13, "Bold", C.muted, "CENTER");
  const welcome = frame(right, "Welcome Banner", 50, 230, 790, 130, C.parchment, C.line, 5);
  img(welcome, "survivor02", "Speaker Portrait", 28, 12, 146, 110, "FILL", 5, C.line);
  txt(welcome, "Welcome", "WELCOME BACK!", 210, 22, 300, 44, 32, "Bold", C.green);
  txt(welcome, "Copy", "Your camp kept moving forward.\nHere's what happened while you were gone.", 212, 72, 360, 42, 14, "Medium", C.ink);

  const gained = panel(right, "Resources Gained", 32, 392, 250, 266);
  sectionTitle(gained, "RESOURCES GAINED", 20, 16, 205, 16);
  [
    ["resScrap", "SCRAP", "From scavenging", "+260"],
    ["resFood", "FOOD", "From farms", "+390"],
    ["resWater", "WATER", "From rain catchers", "+180"],
    ["resMedicine", "MEDICINE", "From herb garden", "+60"],
    ["resParts", "PARTS", "From workshop", "+90"],
  ].forEach((row, i) => reportResourceRow(gained, row[0], row[1], row[2], row[3], 56 + i * 38));

  const completed = panel(right, "Expeditions Completed", 306, 392, 330, 266);
  sectionTitle(completed, "EXPEDITIONS COMPLETED", 20, 16, 285, 16);
  [
    ["RIVERSIDE WAREHOUSE", "Success", "+180  +220  +90"],
    ["SUBURBAN HOMES", "Success", "+140  +170  +70"],
  ].forEach((row, i) => {
    const yy = 62 + i * 90;
    img(completed, "bgCamp", "Expedition Photo " + i, 18, yy, 120, 64, "FILL", 4, C.line);
    img(completed, "statusHealthy", "Status " + i, 152, yy + 4, 18, 18, "FIT");
    txt(completed, "Name " + i, row[0], 176, yy, 130, 20, 12, "Bold", C.ink);
    txt(completed, "State " + i, row[1], 176, yy + 24, 100, 18, 11, "Regular", C.muted);
    txt(completed, "Rewards " + i, row[2], 176, yy + 48, 120, 20, 12, "Bold", C.ink);
  });
  button(completed, "EXPEDITION LOG", 72, 218, 190, 34, C.parchment, "");

  const healing = panel(right, "Treatment And Healing", 660, 392, 206, 266);
  sectionTitle(healing, "TREATMENT", 18, 16, 160, 16);
  [
    ["survivor01", "MAYA", "Fully healed", "statusHealthy"],
    ["survivor02", "HENRY", "90% healed", "statusHealing"],
    ["survivor03", "LEAH", "Fully recovered", "statusHealthy"],
    ["survivor04", "JAX", "Fully healed", "statusHealthy"],
  ].forEach((row, i) => {
    const yy = 58 + i * 46;
    img(healing, row[0], "Portrait " + i, 16, yy, 34, 34, "FILL", 3, C.line);
    txt(healing, "Name " + i, row[1], 58, yy, 72, 16, 10, "Bold", C.ink);
    txt(healing, "State " + i, row[2], 58, yy + 16, 92, 16, 9, "Regular", C.muted);
    img(healing, row[3], "Status " + i, 160, yy + 6, 24, 24, "FIT");
  });

  const production = panel(right, "Camp Production", 32, 680, 330, 220);
  sectionTitle(production, "CAMP PRODUCTION", 20, 14, 285, 16);
  [
    ["resFood", "Farm", "Produced 24 Food", 0.92],
    ["resWater", "Water Catcher", "Collected 18 Water", 0.86],
    ["resParts", "Workshop", "Crafted 6 Parts", 0.78],
    ["resMedicine", "Herb Garden", "Grew 4 Medicine", 0.74],
    ["resIntel", "Listening Post", "Gathered 20 Intel", 0.66],
  ].forEach((row, i) => {
    const yy = 54 + i * 32;
    img(production, row[0], "Icon " + i, 18, yy, 24, 24, "FIT");
    txt(production, "Name " + i, row[1], 52, yy - 2, 110, 16, 10, "Bold", C.ink);
    txt(production, "Desc " + i, row[2], 52, yy + 14, 120, 16, 9, "Regular", C.muted);
    slider(production, "Production " + i, 180, yy + 3, 110, row[3], C.green, 6, false);
  });

  const notes = panel(right, "Notes From Camp", 384, 680, 456, 220);
  sectionTitle(notes, "NOTES FROM THE CAMP", 20, 14, 400, 16);
  [
    ["resFood", "The farm is thriving. We have enough food for now."],
    ["statusMorale", "Henry's spirits are up after a good night's rest."],
    ["resParts", "Workshop upgraded. Tools are more efficient."],
    ["statusDanger", "We're running low on fuel. Consider sending an expedition soon."],
  ].forEach((row, i) => {
    const yy = 58 + i * 38;
    img(notes, row[0], "Note Icon " + i, 20, yy, 24, 24, "FIT");
    txt(notes, "Note " + i, row[1], 58, yy - 4, 330, 32, 11, "Regular", C.ink);
    line(notes, "Note Rule " + i, 58, yy + 34, 410, yy + 34, C.line, 1);
  });
  txt(right, "Signature", "The camp's in good shape. Let's keep it that way.\n- Henry", 454, 904, 320, 44, 16, "Medium", C.teal, "CENTER");
  button(right, "CONTINUE", 328, 964, 320, 46, C.green, "");
}

function descendants(node, out = []) {
  if ("children" in node) {
    for (const child of node.children) {
      out.push(child);
      descendants(child, out);
    }
  }
  return out;
}

function auditCombat(combatFrame) {
  const all = descendants(combatFrame);
  const forbidden = all.filter((node) => {
    const nameHit = /zone|preview|minimap/i.test(node.name || "");
    const textHit = node.type === "TEXT" && /zone|preview|minimap/i.test(node.characters || "");
    return nameHit || textHit;
  }).map((node) => ({ id: node.id, name: node.name, type: node.type }));
  const enemyCards = all.filter((node) => node.name && node.name.startsWith("Enemy Card -")).length;
  const statStrips = all.filter((node) => node.name === "Stats Strip").length;
  const bounds = combatFrame.absoluteBoundingBox;
  const outOfBounds = [];
  for (const node of all) {
    const b = node.absoluteBoundingBox;
    if (!b || !bounds) continue;
    const bad = b.x < bounds.x - 2 || b.y < bounds.y - 2 || b.x + b.width > bounds.x + bounds.width + 2 || b.y + b.height > bounds.y + bounds.height + 2;
    if (bad) outOfBounds.push({ name: node.name, type: node.type });
  }
  return { enemyCards, statStrips, forbidden, outOfBoundsCount: outOfBounds.length, outOfBounds: outOfBounds.slice(0, 12) };
}

const targetNames = new Set([
  "Ashfall Camp - Full HD Elements / 08 Radio Recruitment Elementwise",
  "Ashfall Camp - Full HD Elements / 09 Expedition Combat Elementwise",
  "Ashfall Camp - Full HD Elements / 10 Reports Elementwise",
]);

let page = figma.root.children.find((p) => p.children.some((c) => c.name === "Ashfall Camp - Full HD Elements / 06 Survivors Elementwise"));
if (!page) page = figma.root.children.find((p) => p.name === "Ashfall Camp - Elementwise Concept");
if (!page) {
  page = figma.createPage();
  page.name = "Ashfall Camp - Elementwise Concept";
}
await figma.setCurrentPageAsync(page);

for (const child of [...page.children]) {
  if (targetNames.has(child.name)) child.remove();
}

const existing = page.children.filter((n) => n.type === "FRAME" && n.name.startsWith("Ashfall Camp - Full HD Elements /"));
const maxRight = existing.length ? Math.max(...existing.map((n) => n.x + n.width)) : 0;
const baseX = Math.ceil((maxRight + 180) / 120) * 120;
const baseY = 1500;

buildRadio(baseX, baseY);
buildCombat(baseX + 2040, baseY);
buildReports(baseX + 4080, baseY);

const created = [];
for (const item of frameIds) {
  const node = await figma.getNodeByIdAsync(item.frameId);
  if (node) created.push(node);
}

const combat = created.find((node) => node.name.endsWith("09 Expedition Combat Elementwise"));
const audit = auditCombat(combat);
figma.viewport.scrollAndZoomIntoView(created);
return { pageName: page.name, baseX, baseY, imageTargets, frameIds, audit };
`;

async function main() {
  await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-missing-screens-ashfall", version: "1.0.0" },
  });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  await waitForBridge();

  const createResult = parseJsonContent(await callTool("figma_execute", { code: figmaCode, timeout: 60000 }, 180000));
  writeFileSync(join(outDir, "build-missing-screens-result.json"), JSON.stringify(createResult, null, 2));
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
  writeFileSync(join(outDir, "build-missing-screens-fill-results.json"), JSON.stringify(fillResults, null, 2));

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
  writeFileSync(join(outDir, "build-missing-screens-screenshots.json"), JSON.stringify(screenshots, null, 2));

  console.log(JSON.stringify({
    pageName: createResult.result.pageName,
    baseX: createResult.result.baseX,
    frames: createResult.result.frameIds,
    audit: createResult.result.audit,
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
