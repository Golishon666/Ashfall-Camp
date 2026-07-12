const fonts = {
  heading: { family: "Oswald", style: "SemiBold" },
  body: { family: "Inter", style: "Regular" },
  bodyMedium: { family: "Inter", style: "Medium" }
};
for (const font of Object.values(fonts)) await figma.loadFontAsync(font);

const page = figma.currentPage;
const colors = {
  paper: "#E8D8B8", paperLight: "#F0E4CB", ink: "#292721", muted: "#655947",
  dark: "#181D1A", dark2: "#232923", sage: "#637645", green: "#5F853C",
  amber: "#C78A2A", teal: "#2F7979", rust: "#9C512D", line: "#726043"
};
const createdNodeIds = [];

function rgb(hex) {
  const value = parseInt(hex.slice(1), 16);
  return { r: ((value >> 16) & 255) / 255, g: ((value >> 8) & 255) / 255, b: (value & 255) / 255 };
}
function solid(hex, opacity = 1) { return { type: "SOLID", color: rgb(hex), opacity }; }
function track(node) { createdNodeIds.push(node.id); return node; }
function setBox(node, name, x, y, width, height) {
  node.name = name; node.x = x; node.y = y; node.resize(width, height); return node;
}
function frame(parent, name, x, y, width, height, fill = null, stroke = null, radius = 0) {
  const node = track(figma.createFrame());
  setBox(node, name, x, y, width, height);
  node.fills = fill ? [solid(fill)] : [];
  node.strokes = stroke ? [solid(stroke, 0.65)] : [];
  node.strokeWeight = stroke ? 1 : 0;
  node.cornerRadius = radius;
  parent.appendChild(node);
  return node;
}
function rect(parent, name, x, y, width, height, fill, radius = 0, opacity = 1) {
  const node = track(figma.createRectangle());
  setBox(node, name, x, y, width, height);
  node.fills = [solid(fill, opacity)]; node.cornerRadius = radius; parent.appendChild(node); return node;
}
function text(parent, name, value, x, y, width, height, size = 16, color = colors.ink, style = "body", align = "LEFT") {
  const node = track(figma.createText());
  setBox(node, name, x, y, width, height);
  node.fontName = fonts[style]; node.fontSize = size; node.fills = [solid(color)];
  node.textAlignHorizontal = align; node.textAlignVertical = "CENTER"; node.textAutoResize = "NONE";
  node.characters = String(value); parent.appendChild(node); return node;
}
function imageHash(assetName) {
  const source = page.findOne(node => node.name === assetName && Array.isArray(node.fills) && node.fills.some(fill => fill.type === "IMAGE" && fill.imageHash));
  const existing = source?.fills?.find(fill => fill.type === "IMAGE" && fill.imageHash)?.imageHash;
  if (existing) return existing;
  const reusableHashes = {
    ui_icon_resource_scrap: "48f837ac90df4b506d0d654c68700f0127f8ea2a",
    ui_icon_resource_food: "a9e080386f04cac5c2fd4d610059b8cbc3e963fc",
    ui_icon_resource_water: "08e643b69fb4b1e7aee1aa7c2e8943c3802e8f9a",
    ui_icon_resource_parts: "eb7f727d853b13e87af208f6041e08ae7b6ab0dc",
    ui_icon_resource_medicine: "a338ba86c6e5bd9af5fc52d1b9c251aa5e897e2a",
    ui_icon_equipment_radio: "084bdc506ddf5c5a04cfb4208649f67e7f9e349a",
    ui_character_battle_survivor_01: "8235ce114d70224cf6d34c2bb0fea17c0bf228c4",
    ui_character_battle_survivor_02: "2a2134407936488b8691010ff6493e968d9dfa86",
    ui_character_battle_survivor_03: "9a23920df1416ce461afa051072df2e413823bd2",
    ui_character_battle_survivor_04: "3e2aae2c6f8c03b2b0f2c09c62139adc74247d84",
    ui_character_battle_survivor_05: "9a23920df1416ce461afa051072df2e413823bd2",
    ui_character_battle_survivor_06: "2a2134407936488b8691010ff6493e968d9dfa86",
    ui_icon_equipment_machete: "e85afd38758c1c7c9531e5d94e2c7d0ec5cf2bb5",
    ui_icon_equipment_hatchet: "6efe0fc0f365037c46afd885a4e9067e91209f21",
    ui_icon_equipment_pistol: "247e329d9975448b7f59618b16279723be702b62",
    ui_icon_equipment_armor_vest: "b90a027dbc120267694930333124bc4269cbe0d9",
    ui_icon_equipment_medkit: "463303a775380be60322b38460fc4c2aa0ad695a",
    ui_icon_equipment_duct_tape: "41f47dccdeaf1aa63e61f6a17563856d7ea7cb14",
    ui_icon_equipment_ammo_box: "94241e1878616c9e6564715aa0db8287b7e78b91",
    ui_icon_equipment_backpack: "6efe0fc0f365037c46afd885a4e9067e91209f21"
  };
  return reusableHashes[assetName] || null;
}
function image(parent, name, assetName, x, y, width, height, radius = 0) {
  const node = track(figma.createRectangle());
  setBox(node, name, x, y, width, height); node.cornerRadius = radius;
  const hash = imageHash(assetName);
  node.fills = hash ? [{ type: "IMAGE", imageHash: hash, scaleMode: "FILL" }] : [solid("#B9A586")];
  parent.appendChild(node); return node;
}
function line(parent, y, x = 0, width = parent.width) { return rect(parent, "Divider", x, y, width, 1, colors.line, 0, 0.45); }
function slider(parent, name, x, y, width, value, fill = colors.green) {
  const root = frame(parent, `Slider ${name}`, x, y, width, 9, null, null, 5);
  rect(root, "Track", 0, 0, width, 9, colors.line, 5, 0.32);
  rect(root, "Fill", 0, 0, Math.max(3, width * value), 9, fill, 5);
  return root;
}
function button(parent, name, label, x, y, width, height, active = false) {
  const root = frame(parent, `Button ${name}`, x, y, width, height, active ? colors.sage : colors.dark2, colors.amber, 4);
  text(root, "Label", label, 4, 0, width - 8, height, 15, colors.paperLight, "heading", "CENTER");
  return root;
}

let root = page.findOne(node => node.type === "FRAME" && node.name === "06 Survivors Elementwise");
if (!root) {
  let maxX = 0;
  for (const child of page.children) maxX = Math.max(maxX, child.x + child.width);
  root = track(figma.createFrame());
  root.name = "06 Survivors Elementwise"; root.x = maxX + 240; root.y = 0; root.resize(1920, 1080); page.appendChild(root);
} else {
  createdNodeIds.push(root.id);
  for (const child of [...root.children]) child.remove();
  root.resize(1920, 1080);
}
root.fills = [solid("#D8C6A9")]; root.clipsContent = true;

// Top resources and clock.
const top = frame(root, "Top Resource Bar", 0, 0, 1920, 92, colors.dark, colors.line);
const resources = [
  ["Scrap", "500", "ui_icon_resource_scrap"], ["Food", "8 / 50", "ui_icon_resource_food"],
  ["Water", "6 / 40", "ui_icon_resource_water"], ["Weapon Parts", "0", "ui_icon_resource_parts"],
  ["Medicine", "1 / 20", "ui_icon_resource_medicine"], ["Radio Intel", "3", "ui_icon_equipment_radio"]
];
resources.forEach((resource, index) => {
  const cell = frame(top, `Resource ${resource[0]}`, 270 + index * 218, 10, 208, 70, colors.dark2, colors.line, 4);
  image(cell, "Icon", resource[2], 14, 13, 42, 42, 3);
  text(cell, "Label", resource[0], 66, 8, 132, 27, 17, colors.paperLight, "heading");
  text(cell, "Value", resource[1], 66, 35, 132, 24, 16, colors.amber, "bodyMedium");
});
text(top, "Day", "Day 12", 1630, 0, 110, 92, 20, colors.paperLight, "heading", "CENTER");
text(top, "Time", "14:35", 1780, 0, 110, 92, 22, colors.amber, "heading", "CENTER");

const content = frame(root, "Survivors Content", 12, 104, 1896, 872, colors.paper, colors.line, 6);
const left = frame(content, "Roster Column", 0, 0, 560, 872, colors.paperLight, colors.line, 5);
const center = frame(content, "Survivor Column", 570, 0, 620, 872, colors.paperLight, colors.line, 5);
const right = frame(content, "Inventory Column", 1200, 0, 696, 872, colors.paperLight, colors.line, 5);

text(left, "Title", "Survivors", 24, 12, 340, 48, 31, colors.ink, "heading");
text(left, "Count", "6 / 12", 438, 16, 96, 40, 17, colors.muted, "bodyMedium", "RIGHT");
const rosterViewport = frame(left, "figunity:scroll RosterScroll Viewport", 16, 66, 528, 786, null, null);
rosterViewport.clipsContent = true; rosterViewport.overflowDirection = "VERTICAL";
const roster = frame(rosterViewport, "Content", 0, 0, 528, 910, null, null);
const survivorData = [
  ["Asha", "Idle", "32 / 32", .24, .68, "ui_character_battle_survivor_01"],
  ["Bram", "On Expedition", "36 / 36", .62, .55, "ui_character_battle_survivor_02"],
  ["Cora", "Resting", "28 / 28", .48, .70, "ui_character_battle_survivor_03"],
  ["Dima", "Idle", "30 / 30", .18, .62, "ui_character_battle_survivor_04"],
  ["Elka", "Wounded", "18 / 28", .72, .38, "ui_character_battle_survivor_05"],
  ["Faro", "Idle", "26 / 26", .35, .60, "ui_character_battle_survivor_06"]
];
survivorData.forEach((entry, index) => {
  const card = frame(roster, `Button SurvivorCard ${entry[0]}`, 0, index * 124, 528, 114, colors.paper, index === 0 ? colors.amber : colors.line, 5);
  image(card, "Portrait", entry[5], 8, 8, 92, 98, 4);
  text(card, "Name", entry[0], 114, 7, 150, 34, 24, colors.ink, "heading");
  text(card, "State", entry[1], 114, 42, 150, 24, 13, entry[1] === "Wounded" ? colors.rust : colors.muted, "bodyMedium");
  text(card, "HP Label", `HP  ${entry[2]}`, 276, 5, 226, 24, 13, colors.ink, "bodyMedium");
  slider(card, "HP", 276, 29, 222, parseInt(entry[2]) / parseInt(entry[2].split("/")[1]), entry[1] === "Wounded" ? colors.rust : colors.green);
  text(card, "Fatigue Label", `Fatigue  ${Math.round(entry[3] * 100)} / 100`, 276, 39, 226, 24, 12, colors.ink, "body");
  slider(card, "Fatigue", 276, 65, 222, entry[3], colors.amber);
  text(card, "Morale Label", `Morale  ${Math.round(entry[4] * 100)} / 100`, 276, 75, 226, 24, 12, colors.ink, "body");
  slider(card, "Morale", 276, 99, 222, entry[4], colors.sage);
});
button(roster, "RecruitSurvivor", "+  Recruit Survivor", 0, 760, 528, 58, false);

image(center, "Selected Portrait", "ui_character_battle_survivor_01", 18, 18, 230, 250, 5);
text(center, "Selected Name", "Asha", 270, 16, 320, 50, 38, colors.ink, "heading");
text(center, "Selected Level", "Level 3", 270, 66, 150, 34, 24, colors.teal, "heading");
text(center, "XP Value", "420 / 900", 466, 72, 120, 26, 15, colors.ink, "bodyMedium", "RIGHT");
slider(center, "XP", 270, 108, 316, .467, colors.teal);
text(center, "Background", "Background\nMechanic", 270, 130, 300, 55, 16, colors.ink, "bodyMedium");
text(center, "Traits", "Traits\nResourceful     Quick Learner", 270, 190, 320, 62, 15, colors.ink, "bodyMedium");
line(center, 282, 18, 584);
const skills = [["Scavenging", .62], ["Melee", .48], ["Firearms", .55], ["Survival", .60], ["Mechanics", .72], ["Medicine", .41]];
skills.forEach((skill, index) => {
  const y = 300 + index * 42;
  text(center, `Skill ${skill[0]}`, skill[0], 28, y, 150, 28, 17, colors.ink, "heading");
  slider(center, skill[0], 182, y + 9, 326, skill[1], colors.sage);
  text(center, `Skill Value ${skill[0]}`, String(Math.round(skill[1] * 100)), 522, y, 60, 28, 16, colors.ink, "bodyMedium", "RIGHT");
});
line(center, 564, 18, 584);
const loadout = [
  ["Weapon", "Rusty Knife", "ui_icon_equipment_machete"], ["Armor", "Leather Jacket", "ui_icon_equipment_armor_vest"],
  ["Utility", "Medkit", "ui_icon_equipment_medkit"], ["Backpack", "Backpack", "ui_icon_equipment_backpack"]
];
loadout.forEach((slot, index) => {
  const card = frame(center, `Equipment Slot ${slot[0]}`, 18 + index * 146, 584, 136, 194, colors.paper, colors.line, 4);
  text(card, "Slot Label", slot[0], 4, 4, 128, 28, 16, colors.ink, "heading", "CENTER");
  image(card, "Icon", slot[2], 24, 38, 88, 94, 4);
  text(card, "Name", slot[1], 4, 140, 128, 42, 13, colors.ink, "bodyMedium", "CENTER");
});
button(center, "EquipSelected", "Equip", 108, 798, 404, 52, true);

text(right, "Inventory Title", "Inventory", 22, 12, 360, 44, 30, colors.ink, "heading");
["All", "Weapons", "Armor", "Utility", "Materials"].forEach((label, index) => button(right, `Filter ${label}`, label, 20 + index * 132, 60, 124, 40, index === 0));
const inventoryViewport = frame(right, "figunity:scroll InventoryScroll Viewport", 20, 112, 656, 548, null, null);
inventoryViewport.clipsContent = true; inventoryViewport.overflowDirection = "VERTICAL";
const inventory = frame(inventoryViewport, "Content", 0, 0, 656, 720, null, null);
const sections = [
  ["Weapons", [["Rusty Knife", "ui_icon_equipment_machete"], ["Metal Pipe", "ui_icon_equipment_hatchet"], ["Rusty Revolver", "ui_icon_equipment_pistol"]]],
  ["Armor", [["Leather Jacket", "ui_icon_equipment_armor_vest"], ["Scrap Armor", "ui_icon_equipment_armor_vest"]]],
  ["Utility", [["Medkit", "ui_icon_equipment_medkit"], ["Toolkit", "ui_icon_equipment_duct_tape"], ["Ammo Pack", "ui_icon_equipment_ammo_box"], ["Backpack", "ui_icon_equipment_backpack"]]],
  ["Materials", [["Scrap 500", "ui_icon_resource_scrap"], ["Weapon Parts 0", "ui_icon_resource_parts"], ["Medicine 1", "ui_icon_resource_medicine"], ["Radio Intel 3", "ui_icon_equipment_radio"]]]
];
sections.forEach((section, sectionIndex) => {
  const sectionRoot = frame(inventory, `Section ${section[0]}`, 0, sectionIndex * 164, 656, 152, null, null);
  text(sectionRoot, "Title", section[0], 0, 0, 360, 28, 18, colors.ink, "heading");
  text(sectionRoot, "Count", String(section[1].length), 600, 0, 56, 28, 15, colors.ink, "bodyMedium", "RIGHT");
  line(sectionRoot, 30, 0, 656);
  const rowViewport = frame(sectionRoot, `figunity:scroll ${section[0]}Scroll Viewport`, 0, 38, 656, 108, null, null);
  rowViewport.clipsContent = true; rowViewport.overflowDirection = "HORIZONTAL";
  const row = frame(rowViewport, "Content", 0, 0, Math.max(780, section[1].length * 138), 108, null, null);
  section[1].forEach((item, index) => {
    const tile = frame(row, `Button Item ${item[0]}`, index * 138, 0, 128, 104, colors.paper, sectionIndex === 0 && index === 0 ? colors.amber : colors.line, 4);
    image(tile, "Icon", item[1], 32, 5, 64, 68, 3);
    text(tile, "Name", item[0], 4, 76, 120, 25, 12, colors.ink, "bodyMedium", "CENTER");
  });
});
const selected = frame(right, "Selected Item Panel", 20, 678, 656, 172, colors.paper, colors.line, 4);
image(selected, "Selected Icon", "ui_icon_equipment_machete", 14, 14, 126, 140, 4);
text(selected, "Selected Name", "Rusty Knife", 156, 12, 280, 34, 20, colors.ink, "heading");
text(selected, "Description", "A dull blade, but better than nothing.", 156, 47, 330, 38, 13, colors.muted, "body");
text(selected, "Stats", "Damage 4     Accuracy 78%     Weight 1.0", 156, 92, 390, 30, 14, colors.ink, "bodyMedium");
button(selected, "Equip", "Equip", 486, 110, 154, 46, true).fills = [solid(colors.rust)];

const nav = frame(root, "Bottom Navigation", 330, 994, 1260, 72, colors.dark, colors.line, 5);
["Camp", "Survivors", "Inventory", "Expeditions", "Radio", "Reports"].forEach((label, index) => {
  const item = button(nav, `Nav ${label}`, label, index * 210, 0, 210, 72, label === "Survivors");
  if (label === "Survivors") rect(item, "Current Tab Marker", 12, 0, 186, 4, colors.amber, 2);
});

root.setSharedPluginData("ashfall", "productionScreen", "survivors-reference-v1");
page.selection = [root];
figma.viewport.scrollAndZoomIntoView([root]);
return { createdNodeIds, mutatedNodeIds: [root.id], frameId: root.id, frameName: root.name, width: root.width, height: root.height, nodeCount: createdNodeIds.length };
