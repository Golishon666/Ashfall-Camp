const fonts = {
  heading: { family: "Oswald", style: "SemiBold" },
  body: { family: "Inter", style: "Medium" }
};

await figma.loadFontAsync(fonts.heading);
await figma.loadFontAsync(fonts.body);

const page = figma.currentPage;
const lib = page.children.find(node => node.name === "Ashfall Camp - UI Components");
if (!lib) throw new Error("Ashfall Camp - UI Components frame not found.");

const fullHdFrames = page.children
  .filter(node => node.type === "FRAME" && node.name.startsWith("Ashfall Camp - Full HD /"))
  .sort((a, b) => a.name.localeCompare(b.name));

const tabs = [
  { key: "CAMP", label: "CAMP", x: 14 },
  { key: "SURVIVORS", label: "SURVIVORS", x: 286 },
  { key: "EXPEDITIONS", label: "EXPEDITIONS", x: 558 },
  { key: "BUILDINGS", label: "BUILDINGS", x: 830 },
  { key: "WORKSHOP", label: "WORKSHOP", x: 1102 },
  { key: "RADIO", label: "RADIO", x: 1374 }
];

const oldNavSet = lib.findOne(node => node.name === "Ashfall/Navigation/Bottom Nav" && node.type === "COMPONENT_SET");

let baseComp = lib.findOne(node => node.name === "Ashfall/Navigation/Bottom Nav Base" && node.type === "COMPONENT");
let currentComp = lib.findOne(node => node.name === "Ashfall/Navigation/Current Tab" && node.type === "COMPONENT");
if (baseComp) baseComp.remove();
if (currentComp) currentComp.remove();

baseComp = createBaseNavComponent();
currentComp = createCurrentTabComponent();

const updatedScreens = [];
for (const frame of fullHdFrames) {
  const key = screenKey(frame.name);
  const oldNav = frame.children.find(node => node.name === "Bottom navigation");
  const oldCurrent = frame.children.filter(node => node.name.startsWith("Nav current tab"));
  for (const node of oldCurrent) node.remove();

  const navX = oldNav ? oldNav.x : 87.07711791992188;
  const navY = oldNav ? oldNav.y : 971.832275390625;
  if (oldNav) oldNav.remove();

  const base = baseComp.createInstance();
  base.name = "Bottom navigation";
  base.x = navX;
  base.y = navY;
  frame.appendChild(base);

  const current = currentComp.createInstance();
  current.name = `Nav current tab / ${key}`;
  current.x = navX + tabs.find(tab => tab.key === key).x;
  current.y = navY + 10;
  frame.appendChild(current);

  const label = current.findOne(node => node.type === "TEXT" && node.name === "label");
  if (label) {
    label.characters = key;
  }

  updatedScreens.push({ name: frame.name, active: key, navId: base.id, currentId: current.id });
}

if (oldNavSet) oldNavSet.remove();

figma.currentPage.selection = [baseComp, currentComp];
figma.viewport.scrollAndZoomIntoView([lib]);

return {
  baseComponent: { id: baseComp.id, name: baseComp.name },
  currentComponent: { id: currentComp.id, name: currentComp.name },
  removedOldVariantSet: Boolean(oldNavSet),
  updatedScreens
};

function createBaseNavComponent() {
  const comp = figma.createComponent();
  comp.name = "Ashfall/Navigation/Bottom Nav Base";
  comp.description = "Single shared neutral bottom navigation. Current tab selection is a separate overlay component.";
  comp.resize(1664, 86);
  comp.fills = [paint("#EFE2C9", 0.94)];
  comp.strokes = [paint("#B8A68A", 0.72)];
  comp.strokeWeight = 1;
  comp.cornerRadius = 6;
  comp.clipsContent = true;
  lib.appendChild(comp);
  comp.x = 48;
  comp.y = 514;

  for (const tab of tabs) {
    const item = figma.createFrame();
    item.name = `Nav item / ${tab.key}`;
    item.x = tab.x;
    item.y = 10;
    item.resize(250, 66);
    item.fills = [paint("#F7ECD9", 1)];
    item.strokes = [paint("#B8A68A", 0.48)];
    item.strokeWeight = 1;
    item.cornerRadius = 4;
    item.clipsContent = true;
    comp.appendChild(item);

    const icon = figma.createFrame();
    icon.name = "icon";
    icon.x = 38;
    icon.y = 15;
    icon.resize(36, 36);
    icon.fills = [paint("#FFFFFF", 1)];
    icon.strokes = [paint("#7D6A52", 0.45)];
    icon.strokeWeight = 1;
    icon.cornerRadius = 4;
    item.appendChild(icon);

    const label = figma.createText();
    label.name = "label";
    label.x = 92;
    label.y = 20;
    label.resize(128, 32);
    label.textAutoResize = "NONE";
    label.fontName = fonts.heading;
    label.fontSize = 24;
    label.lineHeight = { unit: "PIXELS", value: 30 };
    label.letterSpacing = { unit: "PIXELS", value: 0 };
    label.characters = tab.label;
    label.fills = [paint("#2B2E33", 1)];
    item.appendChild(label);
  }
  return comp;
}

function createCurrentTabComponent() {
  const comp = figma.createComponent();
  comp.name = "Ashfall/Navigation/Current Tab";
  comp.description = "Separate current-tab selection overlay placed above Bottom Nav Base.";
  comp.resize(250, 66);
  comp.fills = [paint("#2F6572", 1)];
  comp.strokes = [paint("#1F4E59", 0.72)];
  comp.strokeWeight = 2;
  comp.cornerRadius = 4;
  comp.clipsContent = true;
  lib.appendChild(comp);
  comp.x = 48;
  comp.y = 650;

  const icon = figma.createFrame();
  icon.name = "icon";
  icon.x = 38;
  icon.y = 15;
  icon.resize(36, 36);
  icon.fills = [paint("#FFFFFF", 1)];
  icon.cornerRadius = 4;
  comp.appendChild(icon);

  const label = figma.createText();
  label.name = "label";
  label.x = 92;
  label.y = 20;
  label.resize(128, 32);
  label.textAutoResize = "NONE";
  label.fontName = fonts.heading;
  label.fontSize = 24;
  label.lineHeight = { unit: "PIXELS", value: 30 };
  label.letterSpacing = { unit: "PIXELS", value: 0 };
  label.characters = "CURRENT";
  label.fills = [paint("#F6EAD0", 1)];
  comp.appendChild(label);

  try {
    const key = comp.addComponentProperty("Label", "TEXT", "CURRENT");
    label.componentPropertyReferences = { characters: key };
  } catch {}

  return comp;
}

function screenKey(name) {
  if (name.includes("Survivors")) return "SURVIVORS";
  if (name.includes("Expeditions")) return "EXPEDITIONS";
  if (name.includes("Buildings")) return "BUILDINGS";
  if (name.includes("Workshop")) return "WORKSHOP";
  if (name.includes("Radio")) return "RADIO";
  return "CAMP";
}

function paint(hex, opacity = 1) {
  return { type: "SOLID", color: helpers.rgb(hex), opacity };
}
