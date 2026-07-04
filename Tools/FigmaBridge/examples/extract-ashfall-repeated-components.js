const RUN_ID = "ashfall-repeated-components-2026-07-05";
const NS = "ashfall";

const fonts = {
  title: { family: "Anton", style: "Regular" },
  heading: { family: "Oswald", style: "SemiBold" },
  body: { family: "Inter", style: "Regular" },
  bodyMedium: { family: "Inter", style: "Medium" }
};

for (const font of Object.values(fonts)) {
  await figma.loadFontAsync(font);
}

const page = figma.currentPage;
const fullHdFrames = page.children
  .filter(node => node.type === "FRAME" && node.name.startsWith("Ashfall Camp - Full HD /"))
  .sort((a, b) => a.name.localeCompare(b.name));

if (fullHdFrames.length !== 6) {
  throw new Error(`Expected 6 Full HD screens, found ${fullHdFrames.length}.`);
}

removePreviousLibrary();

const bounds = pageBounds();
const libX = Math.ceil(bounds.maxX + 220);
const libY = Math.floor(bounds.minY);
const lib = createLibraryFrame(libX, libY);
const componentIds = [];
const mutatedIds = [];

const source = fullHdFrames[0];

const brandSourceNodes = topLevel(source, ["Camp banner mark", "Game title", "Divider", "Game subtitle"]);
const brandBounds = localBounds(brandSourceNodes);
const brandComp = componentFromTopLevelNodes(
  "Ashfall/Header/Brand Block",
  brandSourceNodes,
  libX + 48,
  libY + 156,
  brandBounds,
  "Shared brand lockup for all game screens."
);

const resourceComp = componentFromSingleNode(
  "Ashfall/Header/Resource Bar",
  source.findOne(node => node.name === "Top resource bar"),
  libX + 560,
  libY + 156,
  "Shared resource summary: scrap, food, water, medicine, parts."
);

const capacityComp = componentFromSingleNode(
  "Ashfall/Header/Survivor Capacity",
  source.findOne(node => node.name === "Survivor capacity"),
  libX + 48,
  libY + 340,
  "Shared survivor population/capacity meter."
);

const noteComp = componentFromSingleNode(
  "Ashfall/Header/Motivational Note",
  source.findOne(node => node.name === "Motivational note"),
  libX + 260,
  libY + 340,
  "Shared day note/quote card."
);

const navVariantComponents = [];
const navVariantByScreen = new Map();
for (const frame of fullHdFrames) {
  const key = screenKey(frame.name);
  const nav = frame.findOne(node => node.name === "Bottom navigation");
  const variant = componentFromSingleNode(
    `Active=${key}`,
    nav,
    libX + 48,
    libY + 514 + navVariantComponents.length * 112,
    `Bottom navigation variant with ${key} active.`
  );
  navVariantComponents.push(variant);
  navVariantByScreen.set(key, variant);
}

const navSet = figma.combineAsVariants(navVariantComponents, page);
navSet.name = "Ashfall/Navigation/Bottom Nav";
navSet.description = "Full width bottom navigation with one variant per active game panel.";
navSet.x = libX + 48;
navSet.y = libY + 514;
navSet.fills = [{ type: "SOLID", color: rgb("#EFE2C9"), opacity: 0.35 }];
navSet.cornerRadius = 8;
tag(navSet, "component/navigation/bottom-nav");
componentIds.push(navSet.id);

let navChildY = 40;
for (const child of navSet.children) {
  const active = child.variantProperties && child.variantProperties.Active;
  child.x = 40;
  child.y = navChildY;
  navChildY += child.height + 26;
  if (active) navVariantByScreen.set(active, child);
}
fitToChildren(navSet, 40);

const panelSide = createPanelShellComponent("Ashfall/Panel/Side Shell", 326, 820, libX + 48, libY + 1340);
const panelMain = createPanelShellComponent("Ashfall/Panel/Main Shell", 520, 360, libX + 420, libY + 1340);
const buttonPrimary = createButtonComponent("Ashfall/Button/Primary", "PRIMARY ACTION", true, libX + 980, libY + 1340);
const buttonSecondary = createButtonComponent("Ashfall/Button/Secondary", "SECONDARY", false, libX + 1240, libY + 1340);
const badge = createBadgeComponent("Ashfall/Badge/Status", "STATUS", libX + 980, libY + 1430);
const progress = createProgressComponent("Ashfall/Progress Bar", libX + 1160, libY + 1438);
const cardComp = createInfoCardComponent("Ashfall/Card/Info", libX + 980, libY + 1518);

const standaloneComponents = [brandComp, resourceComp, capacityComp, noteComp, panelSide, panelMain, buttonPrimary, buttonSecondary, badge, progress, cardComp];
for (const comp of standaloneComponents) {
  componentIds.push(comp.id);
}

replaceCommonShellInstances({
  brandComp,
  brandBounds,
  resourceComp,
  capacityComp,
  noteComp,
  navVariantByScreen
});

addDocLabels(lib, [
  ["Header", "Brand, resource bar, survivor capacity, and note are now component instances in every Full HD screen."],
  ["Navigation", "Bottom Nav is a variant set. Each screen uses the matching active-state instance."],
  ["Reusable Atoms", "Panel shells, buttons, badge, card, and progress bar are available for new panels."]
]);

reparentIntoLibrary(lib, [navSet, ...standaloneComponents]);

const selectionNodes = [lib, navSet, ...standaloneComponents].filter(Boolean);
figma.currentPage.selection = selectionNodes;
figma.viewport.scrollAndZoomIntoView([lib]);

return {
  runId: RUN_ID,
  libraryFrame: { id: lib.id, name: lib.name, x: lib.x, y: lib.y, width: lib.width, height: lib.height },
  componentsCreated: componentIds.length,
  componentNames: [navSet, ...standaloneComponents].map(node => node.name),
  screensUpdated: mutatedIds.length,
  mutatedNodeIds: mutatedIds
};

function removePreviousLibrary() {
  for (const node of [...page.children]) {
    const key = node.getSharedPluginData ? node.getSharedPluginData(NS, "key") : "";
    if (node.name === "Ashfall Camp - UI Components" || key.startsWith("component/") || key === "documentation/components") {
      node.remove();
    }
  }
}

function createLibraryFrame(x, y) {
  const frame = figma.createFrame();
  frame.name = "Ashfall Camp - UI Components";
  frame.x = x;
  frame.y = y;
  frame.resize(2020, 2220);
  frame.fills = [{ type: "SOLID", color: rgb("#F3E8D3"), opacity: 1 }];
  frame.strokes = [{ type: "SOLID", color: rgb("#7D6A52"), opacity: 0.72 }];
  frame.strokeWeight = 3;
  frame.cornerRadius = 8;
  frame.clipsContent = false;
  tag(frame, "documentation/components");

  text(frame, "Library title", "ASHFALL CAMP UI COMPONENTS", 48, 42, 760, 58, fonts.title, 48, "#2B2E33");
  text(frame, "Library subtitle", "Repeated elements extracted from the Full HD game screens", 50, 104, 720, 28, fonts.heading, 18, "#1F4E59");
  rule(frame, 48, 138, 1924);
  return frame;
}

function reparentIntoLibrary(libraryFrame, nodes) {
  for (const node of nodes) {
    const x = node.x - libraryFrame.x;
    const y = node.y - libraryFrame.y;
    libraryFrame.appendChild(node);
    node.x = x;
    node.y = y;
  }
}

function componentFromTopLevelNodes(name, nodes, x, y, bounds, description) {
  if (!nodes.length) throw new Error(`Missing source nodes for ${name}`);
  const wrapper = figma.createFrame();
  wrapper.name = `${name} / source`;
  wrapper.x = x;
  wrapper.y = y;
  wrapper.resize(bounds.width, bounds.height);
  wrapper.fills = [];
  wrapper.clipsContent = false;
  page.appendChild(wrapper);
  for (const node of nodes) {
    const clone = node.clone();
    clone.x = node.x - bounds.x;
    clone.y = node.y - bounds.y;
    wrapper.appendChild(clone);
  }
  const component = figma.createComponentFromNode(wrapper);
  component.name = name;
  component.description = description;
  tag(component, keyFromName(name));
  return component;
}

function componentFromSingleNode(name, node, x, y, description) {
  if (!node) throw new Error(`Missing source node for ${name}`);
  const clone = node.clone();
  clone.x = x;
  clone.y = y;
  page.appendChild(clone);
  const component = figma.createComponentFromNode(clone);
  component.name = name;
  component.description = description;
  tag(component, keyFromName(name));
  return component;
}

function replaceCommonShellInstances(components) {
  for (const frame of fullHdFrames) {
    const key = screenKey(frame.name);
    replaceBrand(frame, components.brandComp, components.brandBounds);
    replaceSingle(frame, "Top resource bar", components.resourceComp);
    replaceSingle(frame, "Survivor capacity", components.capacityComp);
    replaceSingle(frame, "Motivational note", components.noteComp);
    replaceSingle(frame, "Bottom navigation", components.navVariantByScreen.get(key));
    mutatedIds.push(frame.id);
  }
}

function replaceBrand(frame, component, bounds) {
  const nodes = topLevel(frame, ["Camp banner mark", "Game title", "Divider", "Game subtitle"]);
  if (!nodes.length) return;
  const instance = component.createInstance();
  instance.name = "Header / Brand Block";
  instance.x = bounds.x;
  instance.y = bounds.y;
  frame.appendChild(instance);
  for (const node of nodes) node.remove();
}

function replaceSingle(frame, name, component) {
  const node = frame.findOne(candidate => candidate.name === name);
  if (!node || !component) return;
  const instance = component.createInstance();
  instance.name = name;
  instance.x = node.x;
  instance.y = node.y;
  frame.appendChild(instance);
  node.remove();
}

function createPanelShellComponent(name, width, height, x, y) {
  const comp = figma.createComponent();
  comp.name = name;
  comp.x = x;
  comp.y = y;
  comp.resize(width, height);
  comp.fills = [{ type: "SOLID", color: rgb("#F5EAD6"), opacity: 0.9 }];
  comp.strokes = [{ type: "SOLID", color: rgb("#B8A68A"), opacity: 0.72 }];
  comp.strokeWeight = 2;
  comp.cornerRadius = 8;
  comp.clipsContent = true;
  comp.description = "Reusable parchment panel shell for Ashfall Camp UI layouts.";
  text(comp, "Panel title", name.includes("Main") ? "PANEL TITLE" : "SIDE PANEL", 0, 20, width, 34, fonts.heading, 25, "#1F4E59", "CENTER");
  rule(comp, 26, 58, width - 52);
  tag(comp, keyFromName(name));
  page.appendChild(comp);
  return comp;
}

function createButtonComponent(name, label, primary, x, y) {
  const comp = figma.createComponent();
  comp.name = name;
  comp.x = x;
  comp.y = y;
  comp.resize(primary ? 242 : 196, 52);
  comp.fills = [{ type: "SOLID", color: rgb(primary ? "#C96A3A" : "#F8EEDC"), opacity: 1 }];
  comp.strokes = [{ type: "SOLID", color: rgb(primary ? "#8D3F22" : "#7D6A52"), opacity: 0.72 }];
  comp.strokeWeight = 2;
  comp.cornerRadius = 6;
  const labelNode = text(comp, "label", label, 0, 14, comp.width, 24, fonts.heading, 16, primary ? "#F6EAD0" : "#2B2E33", "CENTER");
  try {
    const key = comp.addComponentProperty("Label", "TEXT", label);
    labelNode.componentPropertyReferences = { characters: key };
  } catch {}
  comp.description = primary ? "High-emphasis orange action button." : "Low-emphasis parchment secondary button.";
  tag(comp, keyFromName(name));
  page.appendChild(comp);
  return comp;
}

function createBadgeComponent(name, label, x, y) {
  const comp = figma.createComponent();
  comp.name = name;
  comp.x = x;
  comp.y = y;
  comp.resize(104, 28);
  comp.fills = [{ type: "SOLID", color: rgb("#2F6572"), opacity: 0.95 }];
  comp.cornerRadius = 14;
  const labelNode = text(comp, "label", label, 0, 6, 104, 18, fonts.heading, 12, "#F6EAD0", "CENTER");
  try {
    const key = comp.addComponentProperty("Label", "TEXT", label);
    labelNode.componentPropertyReferences = { characters: key };
  } catch {}
  comp.description = "Compact status badge used for risk, health, and state labels.";
  tag(comp, keyFromName(name));
  page.appendChild(comp);
  return comp;
}

function createProgressComponent(name, x, y) {
  const comp = figma.createComponent();
  comp.name = name;
  comp.x = x;
  comp.y = y;
  comp.resize(220, 12);
  comp.fills = [];
  comp.description = "Reusable progress/status bar with parchment track and colored fill.";
  const track = rect(comp, "track", 0, 0, 220, 12, "#D8CDB8", 0.92, 6);
  const fill = rect(comp, "fill", 0, 0, 148, 12, "#2F6572", 1, 6);
  track.locked = true;
  fill.locked = false;
  tag(comp, keyFromName(name));
  page.appendChild(comp);
  return comp;
}

function createInfoCardComponent(name, x, y) {
  const comp = figma.createComponent();
  comp.name = name;
  comp.x = x;
  comp.y = y;
  comp.resize(310, 152);
  comp.fills = [{ type: "SOLID", color: rgb("#FFF3DE"), opacity: 0.94 }];
  comp.strokes = [{ type: "SOLID", color: rgb("#B8A68A"), opacity: 0.72 }];
  comp.strokeWeight = 1;
  comp.cornerRadius = 8;
  rect(comp, "accent", 0, 0, 8, 152, "#2F6572", 1, 4);
  const titleNode = text(comp, "title", "CARD TITLE", 24, 16, 260, 28, fonts.heading, 18, "#2B2E33");
  const bodyNode = text(comp, "body", "Short descriptive copy for camp actions, risks, rewards, or notes.", 24, 52, 260, 72, fonts.body, 13, "#2B2E33");
  try {
    const titleKey = comp.addComponentProperty("Title", "TEXT", "CARD TITLE");
    const bodyKey = comp.addComponentProperty("Body", "TEXT", bodyNode.characters);
    titleNode.componentPropertyReferences = { characters: titleKey };
    bodyNode.componentPropertyReferences = { characters: bodyKey };
  } catch {}
  comp.description = "Reusable information card with accent rail.";
  tag(comp, keyFromName(name));
  page.appendChild(comp);
  return comp;
}

function addDocLabels(parent, docs) {
  let y = 420;
  for (const [title, body] of docs) {
    const box = figma.createFrame();
    box.name = `Doc / ${title}`;
    box.x = 560;
    box.y = y;
    box.resize(430, 96);
    box.fills = [{ type: "SOLID", color: rgb("#FFF3DE"), opacity: 0.72 }];
    box.strokes = [{ type: "SOLID", color: rgb("#B8A68A"), opacity: 0.5 }];
    box.strokeWeight = 1;
    box.cornerRadius = 8;
    parent.appendChild(box);
    text(box, "title", title.toUpperCase(), 18, 14, 380, 24, fonts.heading, 17, "#1F4E59");
    text(box, "body", body, 18, 42, 380, 42, fonts.body, 13, "#2B2E33");
    y += 116;
  }
}

function topLevel(frame, names) {
  const result = [];
  const remaining = [...names];
  for (const child of frame.children) {
    const index = remaining.indexOf(child.name);
    if (index >= 0) {
      result.push(child);
      remaining.splice(index, 1);
    }
  }
  return result;
}

function localBounds(nodes) {
  const minX = Math.min(...nodes.map(node => node.x));
  const minY = Math.min(...nodes.map(node => node.y));
  const maxX = Math.max(...nodes.map(node => node.x + node.width));
  const maxY = Math.max(...nodes.map(node => node.y + node.height));
  return { x: minX, y: minY, width: maxX - minX, height: maxY - minY };
}

function pageBounds() {
  const nodes = page.children.filter(node => "x" in node && "width" in node);
  return {
    minX: Math.min(...nodes.map(node => node.x)),
    minY: Math.min(...nodes.map(node => node.y)),
    maxX: Math.max(...nodes.map(node => node.x + node.width)),
    maxY: Math.max(...nodes.map(node => node.y + node.height))
  };
}

function fitToChildren(node, pad) {
  const maxX = Math.max(...node.children.map(child => child.x + child.width));
  const maxY = Math.max(...node.children.map(child => child.y + child.height));
  node.resizeWithoutConstraints(maxX + pad, maxY + pad);
}

function screenKey(name) {
  if (name.includes("Survivors")) return "SURVIVORS";
  if (name.includes("Expeditions")) return "EXPEDITIONS";
  if (name.includes("Buildings")) return "BUILDINGS";
  if (name.includes("Workshop")) return "WORKSHOP";
  if (name.includes("Radio")) return "RADIO";
  return "CAMP";
}

function keyFromName(name) {
  return "component/" + name.toLowerCase().replace(/[^a-z0-9]+/g, "/").replace(/^\/|\/$/g, "");
}

function tag(node, key) {
  node.setSharedPluginData(NS, "run_id", RUN_ID);
  node.setSharedPluginData(NS, "key", key);
}

function text(parent, name, value, x, y, w, h, fontName, size, fill, align = "LEFT") {
  const node = figma.createText();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.textAutoResize = "NONE";
  node.fontName = fontName;
  node.fontSize = size;
  node.lineHeight = { unit: "PIXELS", value: Math.round(size * 1.24) };
  node.letterSpacing = { unit: "PIXELS", value: 0 };
  node.textAlignHorizontal = align;
  node.characters = value;
  node.fills = [{ type: "SOLID", color: rgb(fill), opacity: 1 }];
  parent.appendChild(node);
  return node;
}

function rule(parent, x, y, w) {
  return rect(parent, "divider", x, y, w, 1, "#B8A68A", 0.65, 0);
}

function rect(parent, name, x, y, w, h, fill, opacity = 1, radius = 0) {
  const node = figma.createRectangle();
  node.name = name;
  node.x = x;
  node.y = y;
  node.resize(w, h);
  node.cornerRadius = radius;
  node.fills = [{ type: "SOLID", color: rgb(fill), opacity }];
  parent.appendChild(node);
  return node;
}

function rgb(hex) {
  return helpers.rgb(hex);
}
