import { spawn, spawnSync } from "node:child_process";
import { existsSync, mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const outDir = join(process.cwd(), "tmp", "figma-card-battle-page");
const shotsDir = join(outDir, "screenshots");
mkdirSync(shotsDir, { recursive: true });

const P = (...parts) => join(process.cwd(), ...parts);

const assetPaths = {
  bgBattle: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Backgrounds", "ui_bg_survivor_detail_01.png"),

  survivor01: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_01.png"),
  survivor02: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_02.png"),
  survivor03: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_03.png"),
  survivor04: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_battle_survivor_04.png"),
  enemyRaider: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_enemy_raider_01.png"),
  enemyHound: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_creature_mid_boneback_hound_01.png"),
  enemyRoach: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_creature_weak_ash_roach_01.png"),
  enemyArmored: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Characters", "ui_character_enemy_elite_auto_gunner_01.png"),

  resScrap: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_scrap.png"),
  resFood: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_food.png"),
  resWater: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_water.png"),
  resParts: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_parts.png"),
  resMedicine: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_medicine.png"),
  resIntel: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Resources", "ui_icon_resource_radio_intel.png"),

  heart: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_icon_health_heart.png"),
  energy: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_icon_energy_lightning.png"),
  morale: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Status", "ui_icon_status_morale.png"),
  shield: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_icon_stat_armor.png"),
  attack: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Stats", "ui_icon_stat_attack_simple.png"),
  knife: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_machete.png"),
  pistol: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_pistol.png"),
  rifle: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_rifle.png"),
  shotgun: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_shotgun.png"),
  medkit: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Equipment", "ui_icon_equipment_medkit.png"),
  claws: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Weapons", "Creature", "ui_icon_weapon_creature_claw_swipe.png"),
  acid: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Weapons", "Creature", "ui_icon_weapon_creature_acid_spit.png"),
  bite: P("Assets", "AshfallCamp", "Art", "UI", "Production", "Icons", "Weapons", "Creature", "ui_icon_weapon_creature_hound_pack_bite.png"),

  playerPortraitBacking: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_portrait_backing_parchment.png"),
  playerNameplate: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_player_nameplate.png"),
  playerTagPlate: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_player_tag_plate.png"),
  playerHealthTrack: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_player_health_track_empty.png"),
  playerEquipmentSlot: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_player_equipment_slot.png"),
  playerStatValuePlate: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_stat_value_plate.png"),
  enemyBadgeSocket: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_enemy_badge_socket.png"),
  enemyNameplate: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_enemy_nameplate.png"),
  enemyTagPlate: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_enemy_tag_plate.png"),
  enemyHealthTrack: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_enemy_health_track_empty.png"),
  enemyEquipmentSlot: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "BattleCardElements", "ui_card_enemy_equipment_slot.png"),
  turnPhaseBadge: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_badge_turn_phase.png"),
  combatPhaseBar: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_bar_combat_phase_empty.png"),
  actionsCommandBar: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_bar_actions_command_empty.png"),
  actionPointPip: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_pip_action_point_empty.png"),
  battleEmblem: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_emblem_battlefield_clash.png"),
  energyPanel: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_panel_energy_points_empty.png"),
  rowAbilities: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_row_abilities_empty.png"),
  miniEquipmentSlots: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_row_mini_equipment_slots_empty.png"),
  endTurnButton: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_button_end_turn_empty.png"),
  retreatButton: P("Assets", "AshfallCamp", "Art", "UI", "Production", "ExpeditionMonitor", "ui_button_retreat_empty.png"),
};

for (const [key, filePath] of Object.entries(assetPaths)) {
  if (!existsSync(filePath)) throw new Error(`Missing image asset ${key}: ${filePath}`);
}

const data = {
  resources: [
    { label: "Scrap", value: "1,250", asset: "resScrap" },
    { label: "Food", value: "28 / 50", asset: "resFood" },
    { label: "Water", value: "18 / 40", asset: "resWater" },
    { label: "Weapon Parts", value: "12", asset: "resParts" },
    { label: "Medicine", value: "9 / 20", asset: "resMedicine" },
    { label: "Radio Intel", value: "6", asset: "resIntel" },
  ],
  survivors: [
    { name: "Asha", role: "SLASH", hp: 38, maxHp: 45, energy: 22, morale: 68, ability: "Quick Slash", portrait: "survivor01", weapon: "knife", roleIcon: "X" },
    { name: "Bram", role: "GUARD", hp: 52, maxHp: 60, energy: 18, morale: 61, ability: "Steady Shot", portrait: "survivor02", weapon: "pistol", roleIcon: "O" },
    { name: "Cora", role: "MEDIC", hp: 41, maxHp: 50, energy: 24, morale: 72, ability: "Support", portrait: "survivor03", weapon: "medkit", roleIcon: "+" },
    { name: "Dima", role: "COVER", hp: 30, maxHp: 40, energy: 15, morale: 62, ability: "Cover Fire", portrait: "survivor04", weapon: "shotgun", roleIcon: "III" },
  ],
  enemies: [
    { name: "Raider", role: "ENEMY", hp: 26, maxHp: 45, energy: 15, morale: 40, ability: "Brutal Swing", portrait: "enemyRaider", weapon: "knife" },
    { name: "Mutant Stray", role: "BEAST", hp: 22, maxHp: 40, energy: 20, morale: 35, ability: "Rabid Lunge", portrait: "enemyHound", weapon: "bite" },
    { name: "Ash Roach", role: "VERMIN", hp: 12, maxHp: 30, energy: 10, morale: 25, ability: "Acid Spit", portrait: "enemyRoach", weapon: "acid" },
    { name: "Armored Raider", role: "ELITE", hp: 48, maxHp: 70, energy: 18, morale: 30, ability: "Suppressing Fire", portrait: "enemyArmored", weapon: "rifle" },
  ],
  lanes: [
    { from: 0, to: 0, value: "14", icon: "attack", color: "#86b34b" },
    { from: 1, to: 1, value: "9", icon: "attack", color: "#d6a93a" },
    { from: 2, to: 2, value: "11", icon: "medkit", color: "#8bbb57" },
    { from: 3, to: 3, value: "7", icon: "shield", color: "#d98b31" },
  ],
  actions: [
    { title: "Focus Fire", desc: "All survivors attack the same target.", asset: "attack", active: true },
    { title: "Use Medkit", desc: "Heal a survivor for 15 HP.", asset: "medkit", active: false },
    { title: "Retreat", desc: "End the combat and return to camp.", asset: "shield", active: false },
  ],
  log: [
    { actor: "Turn 3", kind: "neutral", text: "begins." },
    { actor: "Asha", kind: "ally", text: "uses Quick Slash on Raider for 14 damage." },
    { actor: "Raider", kind: "enemy", text: "uses Brutal Swing on Bram for 6 damage." },
    { actor: "Bram", kind: "ally", text: "uses Steady Shot on Mutant Stray for 9 damage." },
    { actor: "Mutant Stray", kind: "enemy", text: "uses Rabid Lunge on Cora for 5 damage." },
    { actor: "Cora", kind: "ally", text: "uses Support on Dima for 8 morale." },
    { actor: "Ash Roach", kind: "enemy", text: "uses Acid Spit on Cora for 4 damage." },
    { actor: "Dima", kind: "ally", text: "uses Cover Fire on Armored Raider for 7 damage." },
    { actor: "Tick", kind: "neutral", text: "2 of 3..." },
  ],
};

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
await figma.loadFontAsync({ family: "Inter", style: "Regular" });

const DATA = DATA_JSON;
const imageTargets = [];
const createdNodeIds = [];

async function loadFontChoice(choices) {
  for (const font of choices) {
    try {
      await figma.loadFontAsync(font);
      return font;
    } catch {}
  }
  const fallback = { family: "Inter", style: "Regular" };
  await figma.loadFontAsync(fallback);
  return fallback;
}

const Fonts = {
  reg: await loadFontChoice([{ family: "Inter", style: "Regular" }]),
  med: await loadFontChoice([{ family: "Inter", style: "Medium" }, { family: "Inter", style: "Regular" }]),
  semi: await loadFontChoice([{ family: "Inter", style: "Semi Bold" }, { family: "Inter", style: "Bold" }]),
  bold: await loadFontChoice([{ family: "Inter", style: "Bold" }, { family: "Inter", style: "Semi Bold" }]),
};

const C = {
  charcoal: "#0e0d0a",
  metal: "#171a17",
  metal2: "#22251f",
  metalLine: "#756142",
  parchment: "#efe2c8",
  parchment2: "#f8ecd3",
  parchmentDark: "#c7a77a",
  ink: "#17130e",
  muted: "#6b5942",
  green: "#527a2e",
  greenLight: "#88b64a",
  red: "#9f321e",
  red2: "#c55435",
  rust: "#9a4e2e",
  amber: "#f0a13a",
  gold: "#bf8a36",
  white: "#fff5dd",
};

const RasterAssets = new Set([
  "bgBattle",
  "survivor01", "survivor02", "survivor03", "survivor04",
  "enemyRaider", "enemyHound", "enemyRoach", "enemyArmored",
  "resScrap", "resFood", "resWater", "resParts", "resMedicine", "resIntel",
  "heart", "energy", "morale", "shield", "attack", "knife", "pistol", "rifle", "shotgun", "medkit", "claws", "acid", "bite",
  "playerPortraitBacking", "playerNameplate", "playerTagPlate", "playerHealthTrack", "playerEquipmentSlot", "playerStatValuePlate",
  "enemyBadgeSocket", "enemyNameplate", "enemyTagPlate", "enemyHealthTrack", "enemyEquipmentSlot",
  "turnPhaseBadge", "combatPhaseBar", "actionsCommandBar", "actionPointPip", "battleEmblem", "energyPanel",
  "rowAbilities", "miniEquipmentSlots", "endTurnButton", "retreatButton",
]);

function rgb(hex) {
  const h = hex.replace("#", "");
  return { r: parseInt(h.slice(0, 2), 16) / 255, g: parseInt(h.slice(2, 4), 16) / 255, b: parseInt(h.slice(4, 6), 16) / 255 };
}

function paint(hex, opacity = 1) {
  return { type: "SOLID", color: rgb(hex), opacity };
}

function shadow(color, opacity, x, y, blur, spread = 0) {
  return { type: "DROP_SHADOW", color: { ...rgb(color), a: opacity }, offset: { x, y }, radius: blur, spread, visible: true, blendMode: "NORMAL" };
}

function inner(color, opacity, x, y, blur, spread = 0) {
  return { type: "INNER_SHADOW", color: { ...rgb(color), a: opacity }, offset: { x, y }, radius: blur, spread, visible: true, blendMode: "NORMAL" };
}

function add(node) {
  createdNodeIds.push(node.id);
  return node;
}

function frame(parent, name, x, y, w, h, fill = null, stroke = null, radius = 0, weight = 1) {
  const n = add(figma.createFrame());
  n.name = name;
  parent.appendChild(n);
  n.x = x; n.y = y; n.resize(w, h);
  n.clipsContent = false;
  n.fills = fill ? [paint(fill)] : [];
  n.strokes = stroke ? [paint(stroke)] : [];
  n.strokeWeight = stroke ? weight : 0;
  n.cornerRadius = radius;
  return n;
}

function rect(parent, name, x, y, w, h, fill = null, stroke = null, radius = 0, opacity = 1, weight = 1) {
  const n = add(figma.createRectangle());
  n.name = name;
  parent.appendChild(n);
  n.x = x; n.y = y; n.resize(w, h);
  n.fills = fill ? [paint(fill, opacity)] : [];
  n.strokes = stroke ? [paint(stroke)] : [];
  n.strokeWeight = stroke ? weight : 0;
  n.cornerRadius = radius;
  return n;
}

function ellipse(parent, name, x, y, w, h, fill = null, stroke = null, opacity = 1) {
  const n = add(figma.createEllipse());
  n.name = name;
  parent.appendChild(n);
  n.x = x; n.y = y; n.resize(w, h);
  n.fills = fill ? [paint(fill, opacity)] : [];
  n.strokes = stroke ? [paint(stroke)] : [];
  n.strokeWeight = stroke ? 1 : 0;
  return n;
}

function text(parent, name, value, x, y, w, h, size, font, color, align = "LEFT") {
  const t = add(figma.createText());
  t.name = name;
  parent.appendChild(t);
  t.x = x; t.y = y; t.resize(w, h);
  t.fontName = font;
  t.textAutoResize = "NONE";
  t.fontSize = size;
  t.lineHeight = { unit: "PIXELS", value: Math.ceil(size * 1.18) };
  t.fills = [paint(color)];
  t.textAlignHorizontal = align;
  t.characters = value;
  return t;
}

function image(parent, name, asset, x, y, w, h, radius = 0, stroke = null, opacity = 1) {
  if (RasterAssets.has(asset)) {
    const n = rect(parent, name, x, y, w, h, "#403728", stroke, radius, opacity, stroke ? 1 : 0);
    imageTargets.push({ nodeId: n.id, asset, name });
    return n;
  }

  let fill = "#2a241b";
  let line = stroke || "#8a6a42";
  let op = opacity;
  let weight = stroke ? 1 : 0;
  let rr = radius;
  if (asset.indexOf("enemy") === 0 || asset.indexOf("Enemy") >= 0) {
    fill = "#2a1713"; line = "#a04b31"; weight = 1; rr = Math.max(radius, 3);
  } else if (asset.indexOf("player") === 0 || asset.indexOf("Player") >= 0) {
    fill = "#1f2819"; line = "#6f8a41"; weight = 1; rr = Math.max(radius, 3);
  }
  if (asset.indexOf("Nameplate") >= 0 || asset.indexOf("TagPlate") >= 0) {
    fill = asset.indexOf("enemy") === 0 ? "#d7aa79" : "#e5c996";
    line = asset.indexOf("enemy") === 0 ? "#8d3b28" : "#6e7a43";
    weight = 1;
    rr = 4;
  }
  if (asset.indexOf("HealthTrack") >= 0 || asset.indexOf("StatValuePlate") >= 0) {
    fill = "#d9ba82"; line = "#7b5c35"; weight = 1; rr = 4;
  }
  if (asset.indexOf("EquipmentSlot") >= 0) {
    fill = asset.indexOf("enemy") === 0 ? "#1f1110" : "#162014";
    line = asset.indexOf("enemy") === 0 ? "#9e472e" : "#6f8a41";
    weight = 2;
    rr = 4;
  }
  if (asset === "combatPhaseBar" || asset === "actionsCommandBar" || asset === "energyPanel") {
    fill = "#11130f"; line = "#72593a"; weight = 1; rr = 4; op = Math.min(op, 0.95);
  }
  if (asset.indexOf("Button") >= 0) {
    fill = "#24332f"; line = "#9a7a4a"; weight = 1; rr = 5;
  }
  const n = rect(parent, name + " / vector treatment", x, y, w, h, fill, line, rr, op, weight);
  n.effects = [inner("#ffffff", 0.08, 0, 1, 3), inner("#000000", 0.2, 0, -2, 4)];
  if (w > 24 && h > 18) {
    rect(parent, name + " bevel top", x + 3, y + 3, Math.max(2, w - 6), 1, "#fff1c8", null, 0, 0.16);
    rect(parent, name + " bevel bottom", x + 3, y + h - 4, Math.max(2, w - 6), 1, "#24170e", null, 0, 0.22);
  }
  return n;
}

function roughBorder(parent, x, y, w, h, color, opacity = 0.28) {
  rect(parent, "rough top edge", x + 10, y + 4, w - 20, 2, color, null, 0, opacity);
  rect(parent, "rough bottom edge", x + 18, y + h - 7, w - 36, 2, color, null, 0, opacity * 0.75);
  rect(parent, "rough left edge", x + 5, y + 12, 2, h - 24, color, null, 0, opacity * 0.6);
  rect(parent, "rough right edge", x + w - 8, y + 18, 2, h - 36, color, null, 0, opacity * 0.55);
}

function hpBar(parent, x, y, w, value, max, color, label) {
  image(parent, "hp icon / " + label, "heart", x, y - 4, 24, 24, 0);
  text(parent, "hp value / " + label, value + " / " + max, x + 34, y - 2, 88, 24, 18, Fonts.semi, C.ink);
  rect(parent, "hp track / " + label, x, y + 29, w, 12, "#756c5d", "#4f4435", 5, 0.8);
  rect(parent, "hp fill / " + label, x + 1, y + 30, Math.max(12, (w - 2) * value / max), 10, color, null, 5);
}

function statIcon(parent, asset, value, x, y, tint) {
  ellipse(parent, "stat socket", x, y, 28, 28, "#d3b77f", "#82643a", 0.92);
  image(parent, "stat icon", asset, x + 3, y + 3, 22, 22, 0);
  text(parent, "stat value", String(value), x + 38, y + 2, 44, 24, 18, Fonts.semi, tint || C.ink);
}

function abilityBox(parent, x, y, w, asset, label, enemy = false) {
  const b = frame(parent, "ability / " + label, x, y, w, 44, enemy ? "#d9b889" : "#e2cba3", "#c2a16f", 5, 1);
  b.effects = [inner("#5b3a19", 0.18, 0, 2, 4)];
  image(b, "ability icon / " + label, asset, 12, 9, 26, 26, 0);
  text(b, "ability label / " + label, label, 44, 10, w - 50, 22, 16, Fonts.semi, enemy ? "#652719" : "#2b2519");
}

function characterCard(parent, data, side, index) {
  const ally = side === "ally";
  const x = ally ? 34 : 1206;
  const y = 108 + index * 224;
  const w = ally ? 493 : 380;
  const h = 208;
  const stroke = ally ? "#536d33" : "#9b422c";
  const accent = ally ? C.green : C.rust;
  const hpColor = ally ? C.greenLight : C.red;
  const card = frame(parent, (ally ? "Ally" : "Enemy") + " card / " + data.name, x, y, w, h, C.parchment, stroke, 7, 2);
  card.effects = [shadow("#000000", 0.5, 0, 7, 12), inner("#ffffff", 0.16, 0, 1, 4)];
  roughBorder(card, 0, 0, w, h, ally ? "#5d6f39" : "#9c3f2c", 0.3);

  const badge = frame(card, "role badge / " + data.role, 0, 0, 55, 88, accent, ally ? "#8eaa62" : "#ce6a44", 0, 1);
  badge.effects = [shadow("#000000", 0.4, 2, 3, 6)];
  text(badge, "role glyph", ally ? data.roleIcon : "SK", 9, 17, 38, 32, ally && data.roleIcon.length > 1 ? 16 : 28, Fonts.bold, C.white, "CENTER");
  text(badge, "role label", data.role, 5, 53, 45, 20, 8, Fonts.bold, C.white, "CENTER");

  image(card, "portrait / " + data.name, data.portrait, 62, 13, ally ? 164 : 124, 182, 3, ally ? "#47642f" : "#9f3e2a");
  rect(card, "portrait grime overlay", 62, 13, ally ? 164 : 124, 182, "#000000", null, 3, 0.12);
  const infoX = ally ? 252 : 154;
  const nameSize = ally ? 32 : (data.name.length > 12 ? 23 : 27);
  text(card, "name / " + data.name, data.name, infoX, 20, ally ? 210 : 206, 42, nameSize, Fonts.bold, C.ink);
  hpBar(card, infoX, 72, ally ? 232 : 206, data.hp, data.maxHp, hpColor, data.name);
  statIcon(card, "energy", data.energy, infoX, 124, "#1f1b12");
  statIcon(card, "morale", data.morale, infoX + 112, 124, ally ? "#233315" : "#692517");
  abilityBox(card, infoX, 154, ally ? 236 : 205, data.weapon, data.ability, !ally);
  if (ally && data.name === "Dima") image(card, "defense icon / Dima", "shield", infoX + 8, 162, 26, 26, 0);
  return card;
}

function resourceHud(parent) {
  const title = frame(parent, "title plate / Card Battle", 15, 16, 370, 72, "#171612", "#7e633d", 2, 2);
  title.effects = [shadow("#000000", 0.55, 0, 5, 10)];
  roughBorder(title, 0, 0, 370, 72, "#a58454", 0.42);
  text(title, "screen title", "Card Battle", 74, 15, 252, 45, 38, Fonts.bold, C.white);

  const bar = frame(parent, "top resource HUD", 432, 14, 1070, 66, "#111a18", "#76613e", 3, 2);
  bar.effects = [shadow("#000000", 0.48, 0, 4, 10), inner("#e9c070", 0.12, 0, 1, 3)];
  DATA.resources.forEach((item, i) => {
    const x = 18 + i * 172;
    image(bar, "resource icon / " + item.label, item.asset, x, 13, 42, 42, 0);
    text(bar, "resource label / " + item.label, item.label, x + 58, 10, 112, 20, 16, Fonts.semi, "#e7d3aa");
    text(bar, "resource value / " + item.label, item.value, x + 58, 32, 112, 22, 18, Fonts.bold, C.white);
    if (i > 0) rect(bar, "resource divider", x - 14, 10, 1, 46, "#7b6744", null, 0, 0.65);
  });

  ["?", "SET"].forEach((label, i) => {
    const b = frame(parent, i === 0 ? "help button" : "settings button", 1754 + i * 72, 20, 54, 54, "#191916", "#8d6f45", 6, 2);
    b.effects = [shadow("#000000", 0.45, 0, 4, 8)];
    text(b, i === 0 ? "help glyph" : "settings glyph", label, 0, i === 0 ? 4 : 16, 54, 46, i === 0 ? 34 : 15, Fonts.bold, C.white, "CENTER");
  });
}

function turnTracker(parent) {
  text(parent, "turn label", "Turn 3", 870, 110, 180, 34, 32, Fonts.bold, C.white, "CENTER");
  text(parent, "tick label", "Tick 2 / 3", 882, 151, 160, 24, 20, Fonts.semi, "#f3d69d", "CENTER");
  rect(parent, "tick track", 776, 194, 286, 4, "#5c4c32", null, 2, 0.9);
  for (let i = 0; i < 5; i++) {
    const cx = 794 + i * 59;
    ellipse(parent, "tick dot " + (i + 1), cx, 188, 14, 14, i === 2 ? "#ffd068" : "#9b6f27", "#5e4019", i === 2 ? 1 : 0.8);
    if (i === 2) ellipse(parent, "active tick glow", cx - 12, 176, 38, 38, "#f5a332", null, 0.22);
  }
}

function attackLane(parent, lane, index) {
  const y = 304 + index * 138;
  const ally = DATA.survivors[lane.from];
  const enemy = DATA.enemies[lane.to];
  const color = lane.color;
  image(parent, "lane ally avatar / " + ally.name, ally.portrait, 600, y - 28, 70, 70, 6, "#6f8a41");
  const action = frame(parent, "lane action icon / " + ally.name, 682, y - 20, 56, 56, "#27351d", color, 4, 2);
  image(action, "lane icon image / " + ally.name, lane.icon, 11, 11, 34, 34, 0);
  rect(parent, "lane beam left glow / " + ally.name, 748, y + 5, 135, 4, color, null, 2, 0.75);
  rect(parent, "lane beam left hot / " + ally.name, 748, y + 6, 135, 1, "#fff4c8", null, 1, 0.75);
  text(parent, "lane damage / " + ally.name, lane.value, 895, y - 16, 64, 46, 38, Fonts.bold, C.white, "CENTER");
  rect(parent, "lane beam right glow / " + ally.name, 956, y + 5, 98, 4, C.amber, null, 2, 0.78);
  text(parent, "lane arrow / " + ally.name, ">>>", 1015, y - 16, 64, 40, 38, Fonts.bold, C.amber, "CENTER");
  ellipse(parent, "impact glow / " + enemy.name, 1058, y - 25, 86, 86, C.amber, null, 0.18);
  image(parent, "lane enemy avatar / " + enemy.name, enemy.portrait, 1072, y - 18, 62, 62, 6, C.red2);
  for (let s = 0; s < 5; s++) {
    ellipse(parent, "impact spark " + index + "-" + s, 1044 + s * 17, y - 26 + (s % 2) * 52, 8, 8, C.amber, null, 0.65 - s * 0.08);
  }
}

function actionCards(parent) {
  DATA.actions.forEach((item, i) => {
    const x = 626 + i * 200;
    const card = frame(parent, "action card / " + item.title, x, 844, 184, 202, C.parchment2, item.active ? C.amber : "#b79a6d", 8, item.active ? 3 : 2);
    card.effects = item.active
      ? [shadow(C.amber, 0.72, 0, 0, 14, 2), inner("#ffffff", 0.22, 0, 1, 4)]
      : [shadow("#000000", 0.4, 0, 4, 10), inner("#5b3a19", 0.16, 0, 2, 4)];
    roughBorder(card, 0, 0, 184, 202, item.active ? C.amber : "#8a6c45", item.active ? 0.34 : 0.24);
    image(card, "action icon / " + item.title, item.asset, 64, 30, 54, 54, 0);
    text(card, "action title / " + item.title, item.title, 16, 102, 152, 32, 24, Fonts.bold, C.ink, "CENTER");
    text(card, "action desc / " + item.title, item.desc, 24, 138, 136, 52, 13, Fonts.med, "#3a2c1f", "CENTER");
    if (item.active) ellipse(card, "breathing glow marker", -9, -9, 202, 220, C.amber, null, 0.05);
  });
}

function combatLog(parent) {
  const log = frame(parent, "Combat Log panel", 1608, 110, 280, 880, "#101511", "#7d6240", 4, 2);
  log.effects = [shadow("#000000", 0.62, 0, 8, 12), inner("#e3bb77", 0.1, 0, 1, 4)];
  text(log, "combat log title", "Combat Log", 0, 24, 280, 30, 24, Fonts.bold, "#ecd6a8", "CENTER");
  rect(log, "combat log divider", 20, 62, 240, 1, "#765c39", null, 0, 0.75);
  DATA.log.forEach((entry, i) => {
    const y = 90 + i * 74;
    const color = entry.kind === "ally" ? "#86b957" : entry.kind === "enemy" ? "#d86b43" : "#ecd6a8";
    text(log, "log actor / " + i, entry.actor, 24, y, 220, 22, 15, Fonts.bold, color);
    text(log, "log body / " + i, entry.text, 24, y + 21, 230, 36, 14, Fonts.med, i === 0 || i === DATA.log.length - 1 ? "#e9d3a8" : "#d6c8a7");
  });
  const toggle = frame(log, "Auto Battle On toggle", 20, 810, 240, 48, "#252819", "#8b6a3e", 5, 2);
  toggle.effects = [inner("#f5bd54", 0.18, 0, 1, 4)];
  image(toggle, "auto battle icon", "attack", 18, 11, 26, 26, 0);
  text(toggle, "auto battle label", "Auto Battle: On", 54, 12, 170, 24, 18, Fonts.bold, "#dcca89");
}

function backButton(parent) {
  const b = frame(parent, "Back button", 39, 1004, 140, 52, "#1c1d17", "#8a6d44", 5, 2);
  b.effects = [shadow("#000000", 0.42, 0, 3, 7)];
  text(b, "back arrow", "<", 14, 5, 34, 34, 34, Fonts.bold, "#efd9aa", "CENTER");
  text(b, "back label", "Back", 58, 12, 58, 24, 22, Fonts.bold, "#efd9aa");
}

function buildScreen() {
  const pageBase = "Card Battle - Figma Interface";
  let page = figma.root.children.find(p => p.name === pageBase);
  if (!page) page = figma.createPage();
  const pageName = pageBase;
  page.name = pageName;
  return { page, pageName };
}

const { page, pageName } = buildScreen();
await figma.setCurrentPageAsync(page);
for (const child of [...page.children]) child.remove();

const screen = frame(page, "Card Battle - 1920x1080", 0, 0, 1920, 1080, C.charcoal, null, 0, 0);
screen.clipsContent = false;
image(screen, "battlefield background / existing asset", "bgBattle", 0, 0, 1920, 1080, 0);
rect(screen, "global dark vignette", 0, 0, 1920, 1080, "#000000", null, 0, 0.48);
ellipse(screen, "central warm dust glow", 610, 130, 720, 720, C.amber, null, 0.08);
for (let i = 0; i < 48; i++) {
  const x = 520 + ((i * 71) % 780);
  const y = 190 + ((i * 47) % 650);
  ellipse(screen, "dust mote " + i, x, y, 2 + (i % 3), 2 + (i % 3), "#d2a55b", null, 0.12 + (i % 4) * 0.03);
}

resourceHud(screen);
turnTracker(screen);
DATA.survivors.forEach((item, i) => characterCard(screen, item, "ally", i));
DATA.enemies.forEach((item, i) => characterCard(screen, item, "enemy", i));
DATA.lanes.forEach((lane, i) => attackLane(screen, lane, i));
actionCards(screen);
combatLog(screen);
backButton(screen);

figma.currentPage.selection = [screen];
figma.viewport.scrollAndZoomIntoView([screen]);

return {
  success: true,
  pageName,
  screenId: screen.id,
  createdNodeIds,
  imageTargets,
  stats: {
    pageCount: figma.root.children.length,
    imageTargetCount: imageTargets.length,
    createdNodeCount: createdNodeIds.length,
  },
};
`.replace("DATA_JSON", JSON.stringify(data));

async function main() {
  await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-card-battle-figma-page", version: "1.0.0" },
  });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  const status = await waitForBridge();

  if (status?.currentFileName && status.currentFileName !== "Ashfall Camp UI Concept") {
    throw new Error(`Wrong active Figma file: ${status.currentFileName}`);
  }

  const createResult = parseJsonContent(await callTool("figma_execute", { code: figmaCode, timeout: 60000 }, 180000));
  writeFileSync(join(outDir, "create-result.json"), JSON.stringify(createResult, null, 2));
  if (!createResult?.success) throw new Error(`figma_execute failed: ${JSON.stringify(createResult)}`);

  const uniqueTargets = new Map();
  for (const target of createResult.result.imageTargets) {
    if (!uniqueTargets.has(target.asset)) uniqueTargets.set(target.asset, []);
    uniqueTargets.get(target.asset).push(target.nodeId);
  }

  const fillResults = [];
  for (const [asset, nodeIds] of uniqueTargets.entries()) {
    const imageData = readFileSync(assetPaths[asset]).toString("base64");
    const result = parseJsonContent(await callTool("figma_set_image_fill", {
      nodeIds,
      imageData,
      scaleMode: "FILL",
    }, 120000));
    fillResults.push({ asset, nodeCount: nodeIds.length, result });
    if (!result?.success) throw new Error(`image fill failed for ${asset}: ${JSON.stringify(result)}`);
  }
  writeFileSync(join(outDir, "fill-results.json"), JSON.stringify(fillResults, null, 2));

  const shot = await callTool("figma_take_screenshot", {
    nodeId: createResult.result.screenId,
    scale: 0.5,
    format: "png",
  }, 120000);
  const screenshot = saveScreenshot("card-battle-figma-interface", shot);
  writeFileSync(join(outDir, "screenshot.json"), JSON.stringify(screenshot, null, 2));

  console.log(JSON.stringify({
    pageName: createResult.result.pageName,
    screenId: createResult.result.screenId,
    imageTargetCount: createResult.result.stats.imageTargetCount,
    createdNodeCount: createResult.result.stats.createdNodeCount,
    filledAssetCount: fillResults.length,
    screenshot: screenshot?.filePath,
  }, null, 2));
}

main().catch((error) => {
  console.error(error.stack || error.message);
  process.exitCode = 1;
}).finally(killTree);
