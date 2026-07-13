const fonts = {
  heading: { family: "Oswald", style: "SemiBold" },
  body: { family: "Inter", style: "Regular" },
  medium: { family: "Inter", style: "Medium" },
  bold: { family: "Inter", style: "Bold" }
};
for (const font of Object.values(fonts)) await figma.loadFontAsync(font);

const page = figma.currentPage;
const C = {
  void: "#0B0E0D", wall: "#111713", panel: "#18201B", panel2: "#202A23",
  metal: "#2B312A", edge: "#726043", paper: "#E7D7B6", paper2: "#F1E5CC",
  ink: "#28251F", muted: "#7B6D56", cream: "#F0E4CB", signal: "#73C7A4",
  signalDim: "#315F50", amber: "#D19A3A", rust: "#A94C35", danger: "#D35C45",
  sage: "#64794D", black: "#080A09"
};

const rgb = hex => { const n = parseInt(hex.slice(1), 16); return { r: ((n >> 16) & 255) / 255, g: ((n >> 8) & 255) / 255, b: (n & 255) / 255 }; };
const solid = (hex, opacity = 1) => ({ type: "SOLID", color: rgb(hex), opacity });
const shadow = (color = "#000000", opacity = .42, x = 0, y = 10, blur = 24) => ({ type: "DROP_SHADOW", color: { ...rgb(color), a: opacity }, offset: { x, y }, radius: blur, spread: 0, visible: true, blendMode: "NORMAL" });

function box(node, name, x, y, w, h) { node.name = name; node.x = x; node.y = y; node.resize(w, h); return node; }
function frame(parent, name, x, y, w, h, fill = null, stroke = null, radius = 0) {
  const n = box(figma.createFrame(), name, x, y, w, h);
  n.fills = fill ? [solid(fill)] : [];
  n.strokes = stroke ? [solid(stroke, .88)] : [];
  n.strokeWeight = stroke ? 1 : 0;
  n.cornerRadius = radius;
  parent.appendChild(n);
  return n;
}
function rect(parent, name, x, y, w, h, fill, radius = 0, opacity = 1) {
  const n = box(figma.createRectangle(), name, x, y, w, h);
  n.fills = [solid(fill, opacity)]; n.cornerRadius = radius; parent.appendChild(n); return n;
}
function line(parent, name, x, y, w, color = C.edge, opacity = .5, h = 1) { return rect(parent, name, x, y, w, h, color, 0, opacity); }
function text(parent, name, value, x, y, w, h, size = 16, color = C.cream, font = "body", align = "LEFT") {
  const n = box(figma.createText(), name, x, y, w, h);
  n.fontName = fonts[font]; n.fontSize = size; n.fills = [solid(color)];
  n.textAlignHorizontal = align; n.textAlignVertical = "CENTER"; n.textAutoResize = "NONE";
  n.characters = String(value); parent.appendChild(n); return n;
}
function image(parent, name, hash, x, y, w, h, radius = 0) {
  const n = box(figma.createRectangle(), name, x, y, w, h); n.cornerRadius = radius;
  n.fills = hash ? [{ type: "IMAGE", imageHash: hash, scaleMode: "FILL" }] : [solid(C.muted)];
  parent.appendChild(n); return n;
}
function button(parent, name, label, x, y, w, h, tone = "primary") {
  const fill = tone === "primary" ? C.signalDim : tone === "danger" ? "#38201A" : tone === "disabled" ? C.metal : C.panel2;
  const stroke = tone === "primary" ? C.signal : tone === "danger" ? C.rust : tone === "disabled" ? "#4B4E46" : C.amber;
  const n = frame(parent, `figunity:button Button ${name}`, x, y, w, h, fill, stroke, 4);
  if (tone !== "disabled") n.effects = [shadow("#000000", .25, 0, 4, 9)];
  text(n, "Label", label, 14, 0, w - 28, h, 19, tone === "disabled" ? "#7B7D73" : C.cream, "heading", "CENTER");
  return n;
}
function screw(parent, x, y) {
  rect(parent, "Screw", x, y, 12, 12, C.black, 6, .9);
  line(parent, "Screw Slot", x + 3, y + 5, 6, C.edge, .9, 2);
}
function metalPlate(parent, name, x, y, w, h, fill = C.panel, stroke = C.edge, radius = 5) {
  const n = frame(parent, name, x, y, w, h, fill, stroke, radius);
  n.effects = [shadow("#000000", .35, 0, 7, 16)];
  screw(n, 8, 8); screw(n, w - 20, 8); screw(n, 8, h - 20); screw(n, w - 20, h - 20);
  return n;
}
function iconSocket(parent, label, x, y, active = false) {
  const n = frame(parent, "Icon Socket", x, y, 54, 54, active ? C.signalDim : C.panel, active ? C.signal : C.edge, 27);
  text(n, "Glyph", label, 0, 0, 54, 54, 16, active ? C.signal : C.cream, "heading", "CENTER");
  return n;
}
function resource(parent, label, value, hash, x, y, w = 188) {
  const n = frame(parent, `Resource ${label}`, x, y, w, 54, C.panel2, C.edge, 4);
  image(n, "Icon", hash, 10, 9, 36, 36, 4);
  text(n, "Label", label, 56, 3, w - 66, 22, 12, "#A99B82", "medium");
  text(n, "Value", value, 56, 24, w - 66, 25, 16, C.cream, "bold");
  return n;
}
function waveform(parent, x, y, w, h) {
  const n = frame(parent, "Signal Waveform", x, y, w, h, "#0A1511", C.signalDim, 4);
  for (let i = 0; i < 66; i++) {
    const center = h / 2;
    const pulse = (Math.sin(i * .88) + Math.sin(i * .31 + 1.4) * .72 + Math.sin(i * 1.77) * .28) / 2;
    const amp = 6 + Math.abs(pulse) * (i > 21 && i < 47 ? 28 : 17);
    rect(n, `Wave ${i}`, 12 + i * ((w - 24) / 66), center - amp / 2, 2, amp, C.signal, 1, .82);
  }
  line(n, "Centerline", 12, h / 2, w - 24, C.signal, .22);
  return n;
}
function levelRow(parent, level, title, description, y, active = false) {
  const n = frame(parent, `Radio Tower Level ${level}`, 16, y, 244, 80, active ? "#24372E" : C.panel2, active ? C.signal : C.edge, 4);
  rect(n, "Level Badge", 12, 14, 48, 48, active ? C.signalDim : C.metal, 24);
  text(n, "Level", `LV ${level}`, 12, 14, 48, 48, 14, active ? C.signal : C.cream, "heading", "CENTER");
  text(n, "Perk", title, 72, 10, 156, 28, 16, active ? C.signal : C.cream, "heading");
  text(n, "Description", description, 72, 37, 156, 31, 12, active ? "#B8D9CA" : "#A99B82", "medium");
  return n;
}

const portraitAnna = "8235ce114d70224cf6d34c2bb0fea17c0bf228c4";
const portraitBoris = "9a23920df1416ce461afa051072df2e413823bd2";
const scrapIcon = "48f837ac90df4b506d0d654c68700f0127f8ea2a";
const foodIcon = "a9e080386f04cac5c2fd4d610059b8cbc3e963fc";
const waterIcon = "08e643b69fb4b1e7aee1aa7c2e8943c3802e8f9a";
const partsIcon = "eb7f727d853b13e87af208f6041e08ae7b6ab0dc";
const medicineIcon = "a338ba86c6e5bd9af5fc52d1b9c251aa5e897e2a";
const intelIcon = "084bdc506ddf5c5a04cfb4208649f67e7f9e349a";

let root = page.findOne(n => n.type === "FRAME" && n.name === "08 Radio Recruitment Choice Elementwise");
if (!root) {
  let maxX = 0; for (const child of page.children) maxX = Math.max(maxX, child.x + child.width);
  root = figma.createFrame(); root.name = "08 Radio Recruitment Choice Elementwise"; root.x = maxX + 240; root.y = 0; page.appendChild(root);
} else {
  for (const child of [...root.children]) child.remove();
}
root.resize(1920, 1080); root.fills = [solid(C.void)]; root.clipsContent = true;

// Quiet, cinematic radio-room backdrop built from editable vector layers.
rect(root, "Back Wall", 0, 0, 1920, 1080, C.wall);
rect(root, "Warm Left Light", 0, 82, 570, 886, "#5C3D1E", 0, .18);
rect(root, "Cold Signal Light", 1110, 82, 810, 886, C.signalDim, 0, .14);
for (let i = 0; i < 9; i++) line(root, `Wall Seam ${i}`, i * 240, 82, 1, "#364039", .18, 886);
for (let i = 0; i < 8; i++) {
  const cable = rect(root, `Hanging Cable ${i}`, 60 + i * 252, 80, 4, 75 + (i % 3) * 28, "#050706", 2, .72);
  cable.rotation = i % 2 ? 3 : -3;
}

// Resource header.
const top = frame(root, "Top Resource Bar", 0, 0, 1920, 82, "#0A0D0C", C.edge);
text(top, "Game Mark", "ASHFALL CAMP", 24, 0, 220, 82, 26, C.amber, "heading");
resource(top, "SCRAP", "500", scrapIcon, 270, 14, 168);
resource(top, "FOOD", "28 / 50", foodIcon, 450, 14, 188);
resource(top, "WATER", "34 / 40", waterIcon, 650, 14, 188);
resource(top, "PARTS", "16", partsIcon, 850, 14, 168);
resource(top, "MEDICINE", "7 / 20", medicineIcon, 1030, 14, 192);
resource(top, "RADIO INTEL", "3", intelIcon, 1234, 14, 210);
text(top, "Day", "DAY 12", 1550, 0, 114, 82, 18, C.cream, "heading", "CENTER");
text(top, "Time", "18:42", 1672, 0, 114, 82, 22, C.amber, "heading", "CENTER");
button(top, "Settings", "SETTINGS", 1798, 16, 106, 50, "secondary");

// Radio tower status and upgrade affordances.
const tower = metalPlate(root, "Radio Tower Status", 20, 102, 278, 846, C.panel, C.edge, 6);
text(tower, "Eyebrow", "COMMUNICATIONS", 24, 22, 230, 24, 12, C.signal, "bold");
text(tower, "Title", "RADIO TOWER", 24, 45, 230, 48, 29, C.cream, "heading");
line(tower, "Header Divider", 20, 102, 238, C.edge, .65);
text(tower, "Signal Label", "SIGNAL STRENGTH", 20, 118, 150, 25, 13, "#A99B82", "medium");
text(tower, "Signal Value", "72%", 184, 116, 74, 28, 16, C.signal, "bold", "RIGHT");
const strength = frame(tower, "figunity:passive-slider SignalStrength", 20, 151, 238, 10, "#0B0E0D", null, 5);
rect(strength, "Fill", 0, 0, 171, 10, C.signal, 5);
text(tower, "Status", "CHANNEL STABLE\n2 SIGNALS LOCKED", 20, 178, 238, 55, 16, C.cream, "heading");
line(tower, "Level Divider", 20, 247, 238, C.edge, .55);
text(tower, "Level Title", "TOWER PERKS", 20, 260, 238, 30, 15, C.amber, "heading");
levelRow(tower, 1, "2 SIGNALS", "Choose one survivor", 300, true);
levelRow(tower, 2, "3 SIGNALS", "Wider candidate pool", 390, false);
levelRow(tower, 3, "FREE REFRESH", "Once per broadcast", 480, false);
const next = frame(tower, "Next Upgrade", 16, 582, 244, 120, "#171B18", C.edge, 4);
text(next, "Title", "NEXT UPGRADE", 14, 10, 216, 26, 14, C.cream, "heading");
text(next, "Body", "Unlock a third candidate\nand improve signal clarity.", 14, 38, 216, 42, 12, "#A99B82", "medium");
text(next, "Cost", "240 SCRAP  •  60 PARTS", 14, 84, 216, 24, 13, C.amber, "bold");
button(tower, "UpgradeTower", "UPGRADE TOWER", 16, 720, 244, 54, "secondary");
text(tower, "Flavor", "Every voice could change the camp.\nEvery response gives away our position.", 20, 785, 238, 44, 11, "#877A66", "medium", "CENTER");

// Main recruitment signal console.
const consolePanel = metalPlate(root, "Radio Recruitment Console", 316, 102, 1584, 846, "#121714", C.edge, 6);
const titlePlate = metalPlate(consolePanel, "Radio Header Plate", 470, 16, 644, 74, "#151A17", C.amber, 4);
text(titlePlate, "Title", "RADIO", 0, 0, 644, 74, 42, C.cream, "heading", "CENTER");
text(consolePanel, "Subtitle", "SURVIVOR SIGNALS", 28, 20, 330, 30, 17, C.signal, "heading");
text(consolePanel, "Level", "TOWER LEVEL 1", 1220, 20, 330, 30, 15, C.amber, "heading", "RIGHT");

const signalPanel = frame(consolePanel, "Signal Receiver", 28, 104, 1528, 82, "#07120E", C.signalDim, 4);
text(signalPanel, "Antenna", "RX", 16, 0, 54, 82, 22, C.signal, "heading", "CENTER");
waveform(signalPanel, 78, 10, 965, 62);
text(signalPanel, "Signals", "SIGNALS RECEIVED", 1062, 11, 264, 28, 14, "#9EB9AA", "medium");
text(signalPanel, "Signals Count", "02", 1320, 0, 176, 82, 34, C.signal, "heading", "RIGHT");

function candidateCard(parent, x, portraitHash, name, role, skill, skillValue, trait, traitNote, accent) {
  const card = frame(parent, `Candidate Card ${name}`, x, 208, 694, 476, C.paper, C.edge, 5);
  card.effects = [shadow("#000000", .48, 0, 12, 24)];
  rect(card, "Paper Highlight", 5, 5, 684, 466, C.paper2, 3, .32);
  image(card, `Portrait ${name}`, portraitHash, 18, 18, 274, 368, 3);
  rect(card, "Portrait Vignette", 18, 18, 274, 368, C.black, 3, .12);
  rect(card, "Role Strip", 18, 338, 274, 48, C.panel, 0, .9);
  text(card, "Role", role, 28, 338, 254, 48, 16, C.cream, "heading", "CENTER");
  text(card, "Candidate Name", name, 318, 22, 348, 56, 35, C.ink, "heading");
  line(card, "Name Divider", 318, 83, 348, C.edge, .75);
  text(card, "Primary Label", "PRIMARY SKILL", 318, 98, 200, 24, 12, C.muted, "bold");
  const skillBox = frame(card, "Primary Skill", 318, 126, 348, 74, C.paper2, C.edge, 4);
  rect(skillBox, "Skill Icon", 14, 13, 48, 48, accent, 24);
  text(skillBox, "Skill Glyph", skill === "MEDICINE" ? "+" : "◎", 14, 13, 48, 48, 28, C.paper2, "heading", "CENTER");
  text(skillBox, "Skill Name", skill, 76, 8, 184, 28, 16, C.ink, "heading");
  text(skillBox, "Skill Rank", String(skillValue), 264, 0, 66, 74, 30, accent, "heading", "RIGHT");
  text(skillBox, "Skill Note", skill === "MEDICINE" ? "Treats injuries faster" : "Accurate under pressure", 76, 35, 188, 25, 11, C.muted, "medium");
  text(card, "Trait Label", "TRAIT", 318, 222, 200, 24, 12, C.muted, "bold");
  const traitBox = frame(card, "Trait", 318, 250, 348, 82, "#D8C49F", C.edge, 3);
  rect(traitBox, "Trait Mark", 14, 17, 8, 48, C.rust, 3);
  text(traitBox, "Trait Name", trait, 38, 7, 292, 31, 18, C.ink, "heading");
  text(traitBox, "Trait Effect", traitNote, 38, 37, 292, 31, 12, C.muted, "medium");
  text(card, "Signal Meta", "UNRECRUITED  •  SIGNAL VERIFIED", 318, 349, 348, 28, 12, C.sage, "bold");
  button(card, `Recruit${name}`, `RECRUIT ${name}`, 18, 402, 648, 58, "primary");
  return card;
}

candidateCard(consolePanel, 28, portraitAnna, "ANNA", "FIELD MEDIC", "MEDICINE", 4, "FRAIL", "Maximum health -15%", C.sage);
candidateCard(consolePanel, 834, portraitBoris, "BORIS", "RANGED GUARD", "FIREARMS", 3, "HUNGRY", "Consumes 25% more food", C.rust);

const warning = frame(consolePanel, "Choice Warning", 28, 702, 1528, 54, "#241512", C.rust, 4);
rect(warning, "Warning Mark", 18, 13, 28, 28, C.danger, 14);
text(warning, "Warning Glyph", "!", 18, 13, 28, 28, 18, C.cream, "bold", "CENTER");
text(warning, "Warning Text", "CHOOSE ONE — THE OTHER SIGNAL WILL BE LOST", 60, 0, 1408, 54, 19, "#E4775F", "heading", "CENTER");

const actionBar = frame(consolePanel, "Broadcast Action Bar", 28, 770, 1528, 58, C.panel, C.edge, 4);
text(actionBar, "Cost Label", "BROADCAST COST", 18, 0, 150, 58, 13, "#A99B82", "medium");
image(actionBar, "Cost Icon", partsIcon, 172, 13, 32, 32, 3);
text(actionBar, "Cost", "10 PARTS", 212, 0, 114, 58, 16, C.amber, "bold");
line(actionBar, "Cost Divider", 350, 11, 1, C.edge, .7, 36);
text(actionBar, "Cooldown Label", "NEXT BROADCAST", 378, 0, 160, 58, 13, "#A99B82", "medium");
text(actionBar, "Cooldown", "18:42", 542, 0, 94, 58, 18, C.cream, "heading");
button(actionBar, "RefreshSignals", "REFRESH  •  LV 3", 818, 7, 214, 44, "disabled");
button(actionBar, "SkipSignals", "SKIP SIGNALS", 1046, 7, 220, 44, "danger");
button(actionBar, "Broadcast", "BROADCAST", 1280, 7, 230, 44, "disabled");

// Full project navigation, built as real button groups for Unity import.
const nav = frame(root, "Bottom Navigation", 0, 968, 1920, 112, "#090C0B", C.edge);
const navItems = [
  ["CAMP", "CP"], ["SURVIVORS", "SV"], ["INVENTORY", "IN"],
  ["EXPEDITIONS", "EX"], ["RADIO", "RX"], ["REPORTS", "RP"]
];
navItems.forEach((item, index) => {
  const x = 18 + index * 316;
  const active = item[0] === "RADIO";
  const b = frame(nav, `figunity:button Navigation ${item[0]}`, x, 12, 302, 88, active ? "#20352C" : C.panel, active ? C.signal : C.edge, 5);
  if (active) rect(b, "Active Rail", 0, 0, 302, 5, C.signal, 3);
  iconSocket(b, item[1], 18, 17, active);
  text(b, "Label", item[0], 88, 0, 190, 88, 18, active ? C.signal : C.cream, "heading");
});

figma.currentPage.selection = [root];
figma.viewport.scrollAndZoomIntoView([root]);
return {
  frameId: root.id,
  frameName: root.name,
  buttons: root.findAll(n => n.name.startsWith("figunity:button")).length,
  textNodes: root.findAll(n => n.type === "TEXT").length,
  candidateCards: root.findAll(n => n.name.startsWith("Candidate Card")).length
};
