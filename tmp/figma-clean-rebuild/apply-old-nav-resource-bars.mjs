import { spawn, spawnSync } from "node:child_process";
import { mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const outDir = join(process.cwd(), "tmp", "figma-clean-rebuild");
const shotsDir = join(outDir, "screenshots-after-resource-copy");
mkdirSync(shotsDir, { recursive: true });

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
  for (let i = 0; i < 36; i++) {
    const status = parseJsonContent(await callTool("figma_get_status", { probe: true }, 20000)) || {};
    if (status?.setup?.valid === true && status?.setup?.probeResult?.success === true) return status;
    await new Promise((resolve) => setTimeout(resolve, 1500));
  }
  throw new Error("Desktop Bridge did not connect");
}

const code = String.raw`
await figma.loadAllPagesAsync();

async function loadFont(style) {
  try {
    await figma.loadFontAsync({ family: "Inter", style });
    return { family: "Inter", style };
  } catch (error) {
    await figma.loadFontAsync({ family: "Inter", style: "Regular" });
    return { family: "Inter", style: "Regular" };
  }
}
const FONT_REG = await loadFont("Regular");
const FONT_BOLD = await loadFont("Bold");

async function makeEditable(t) {
  try {
    const segs = t.getStyledTextSegments(["fontName"]);
    for (const seg of segs) {
      if (seg.fontName && typeof seg.fontName !== "symbol") {
        try { await figma.loadFontAsync(seg.fontName); } catch {}
      }
    }
  } catch {}
}

function rgb(hex) {
  const h = hex.replace("#", "");
  return { r: parseInt(h.slice(0, 2), 16) / 255, g: parseInt(h.slice(2, 4), 16) / 255, b: parseInt(h.slice(4, 6), 16) / 255 };
}
function paint(hex, opacity) {
  return { type: "SOLID", color: rgb(hex), opacity: opacity === undefined ? 1 : opacity };
}
function rect(parent, name, x, y, w, h, fill, stroke, radius, weight) {
  const n = figma.createRectangle();
  n.name = name;
  parent.appendChild(n);
  n.x = x; n.y = y; n.resize(w, h);
  n.fills = fill ? [paint(fill)] : [];
  n.strokes = stroke ? [paint(stroke)] : [];
  n.strokeWeight = weight || (stroke ? 1 : 0);
  n.cornerRadius = radius || 0;
  return n;
}
function text(parent, name, value, x, y, w, h, size, style, color, align) {
  const t = figma.createText();
  t.name = name;
  parent.appendChild(t);
  t.x = x; t.y = y; t.resize(w, h);
  t.textAutoResize = "NONE";
  t.fontName = style === "Bold" ? FONT_BOLD : FONT_REG;
  t.fontSize = size;
  t.lineHeight = { unit: "PIXELS", value: Math.ceil(size * 1.2) };
  t.fills = [paint(color)];
  t.textAlignHorizontal = align || "LEFT";
  t.textAlignVertical = "CENTER";
  t.characters = value;
  return t;
}
function bounds(node) {
  const b = node.absoluteBoundingBox;
  return b ? { x: b.x, y: b.y, w: b.width, h: b.height, r: b.x + b.width, b: b.y + b.height } : null;
}
function intersects(a, b) {
  const x = Math.max(0, Math.min(a.r, b.r) - Math.max(a.x, b.x));
  const y = Math.max(0, Math.min(a.b, b.b) - Math.max(a.y, b.y));
  return x > 2 && y > 2;
}

const page = figma.root.children.find(p => p.name === "Ashfall Camp - Clean Full HD");
if (!page) throw new Error("Clean Full HD page not found");
await figma.setCurrentPageAsync(page);

const sourcePage = figma.root.children.find(p => p.name === "Page 1");
if (!sourcePage) throw new Error("Source Page 1 not found");
const sourceDashboard = sourcePage.children.find(n => n.name === "Ashfall Camp - Full HD / 01 Camp Dashboard");
if (!sourceDashboard) throw new Error("Source dashboard frame not found");
const sourceResource = sourceDashboard.children.find(n => n.name === "Top Resource Bar");
if (!sourceResource) throw new Error("Source resource node not found");

const screenMap = [
  { name: "Ashfall Camp - Full HD / Camp Dashboard", active: "Camp" },
  { name: "Ashfall Camp - Full HD / Survivors", active: "Survivors" },
  { name: "Ashfall Camp - Full HD / Buildings", active: "Buildings" },
  { name: "Ashfall Camp - Full HD / Expedition Setup", active: "Expeditions" },
  { name: "Ashfall Camp - Full HD / Expedition Monitor", active: "Combat" },
  { name: "Ashfall Camp - Full HD / Workshop", active: "Workshop" },
  { name: "Ashfall Camp - Full HD / Radio", active: "Radio" },
  { name: "Ashfall Camp - Full HD / After Action Report", active: "Report" }
];
const tabs = ["Camp", "Survivors", "Expeditions", "Buildings", "Combat", "Workshop", "Radio", "Report"];
const resources = [
  { key: "Scrap", label: "Scrap", value: "125" },
  { key: "Food", label: "Food", value: "8/50" },
  { key: "Water", label: "Water", value: "6/40" },
  { key: "Medicine", label: "Medicine", value: "1/20" },
  { key: "Weapon Parts", label: "Parts", value: "0" }
];

function removeOldHeaderResource(frame) {
  const nodes = frame.findAll(n =>
    n.name === "Top Resource Bar" ||
    n.name === "Copied / Top Resource Bar" ||
    n.name.indexOf("Instance / Resource Pill /") === 0 ||
    n.name.indexOf("Resource icon") === 0 ||
    n.name.indexOf("TXT / Resource ") === 0
  );
  for (let i = nodes.length - 1; i >= 0; i--) nodes[i].remove();
}

async function cloneResource(frame) {
  const bar = sourceResource.clone();
  frame.appendChild(bar);
  bar.name = "Copied / Top Resource Bar";
  bar.x = 1132; bar.y = 34;
  bar.resize(750, 65);

  for (const res of resources) {
    let pill = bar.findOne(n => n.name === "Resource Pill / " + res.key);
    if (!pill) continue;
    if (pill.type === "INSTANCE") pill = pill.detachInstance();
    pill.name = "Resource Pill / " + res.label;
    const value = pill.findOne(n => n.type === "TEXT" && n.name === "Value");
    const label = pill.findOne(n => n.type === "TEXT" && n.name === "Label");
    if (value) {
      await makeEditable(value);
      value.fontName = FONT_BOLD;
      value.characters = res.value;
      value.textAlignHorizontal = "LEFT";
    }
    if (label) {
      await makeEditable(label);
      label.fontName = FONT_REG;
      label.characters = res.label;
      label.textAlignHorizontal = "LEFT";
    }
  }
  return bar.id;
}

function buildAdaptedNav(frame, active) {
  let nav = sourceNav.clone();
  frame.appendChild(nav);
  if (nav.type === "INSTANCE") nav = nav.detachInstance();
  nav.name = "Copied / Bottom Nav / " + active;
  nav.x = 128; nav.y = 970;
  nav.resize(1664, 88);
  if ("clipsContent" in nav) nav.clipsContent = false;

  const oldChildren = [...nav.children];
  for (const child of oldChildren) child.remove();

  const activeFill = "#25606a";
  const inactiveFill = "#fdf5e8";
  const iconInactive = "#d7c8af";
  const ink = "#252725";
  const cream = "#fff9ec";
  const stroke = "#b69d78";

  const tabW = 188;
  const gap = 16;
  const startX = 24;
  const y = 14;
  for (let i = 0; i < tabs.length; i++) {
    const label = tabs[i];
    const isActive = label === active;
    const x = startX + i * (tabW + gap);
    rect(nav, "Tab " + label, x, y, tabW, 59, isActive ? activeFill : inactiveFill, stroke, 4, 1);
    rect(nav, "Tab Icon " + label, x + 16, y + 18, 22, 22, isActive ? cream : iconInactive, null, 3, 0);
    text(nav, "Tab Label " + label, label.toUpperCase(), x + 48, y + 19, tabW - 58, 22, label === "Expeditions" ? 11 : 12, "Bold", isActive ? cream : ink, "LEFT");
  }
  return nav.id;
}

const mutated = [];
for (const item of screenMap) {
  const fr = page.children.find(n => n.name === item.name);
  if (!fr) throw new Error("Missing target frame: " + item.name);
  removeOldHeaderResource(fr);
  const resourceId = await cloneResource(fr);
  mutated.push({ frame: fr.name, frameId: fr.id, resourceId, active: item.active });
}

const combatFrame = page.children.find(n => n.name === "Ashfall Camp - Full HD / Expedition Monitor");
if (combatFrame) {
  const combatState = combatFrame.findOne(n => n.type === "TEXT" && n.name === "TXT / Combat state");
  if (combatState) {
    await makeEditable(combatState);
    combatState.x = 690;
    combatState.y = 43;
    combatState.resize(360, 22);
    combatState.textAlignHorizontal = "RIGHT";
    combatState.textAlignVertical = "CENTER";
  }
}

const audit = [];
for (const item of screenMap) {
  const fr = page.children.find(n => n.name === item.name);
  const fb = bounds(fr);
  const texts = fr.findAll(n => n.type === "TEXT" && n.visible !== false);
  const overlaps = [];
  const outOfBounds = [];
  for (let i = 0; i < texts.length; i++) {
    const a = bounds(texts[i]);
    if (!a) continue;
    if (a.x < fb.x - 1 || a.y < fb.y - 1 || a.r > fb.r + 1 || a.b > fb.b + 1) outOfBounds.push(texts[i].name);
    for (let j = i + 1; j < texts.length; j++) {
      const b = bounds(texts[j]);
      if (b && intersects(a, b)) overlaps.push(texts[i].name + " <> " + texts[j].name);
    }
  }
  audit.push({ frame: fr.name, nodeId: fr.id, overlapCount: overlaps.length, overlaps: overlaps.slice(0, 12), outOfBounds: outOfBounds.slice(0, 12) });
}

figma.viewport.scrollAndZoomIntoView(screenMap.map(item => page.children.find(n => n.name === item.name)));
return { mutated, audit };
`;

async function main() {
  await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-apply-old-resource-bar", version: "1.0.0" },
  });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  await waitForBridge();
  const executed = parseJsonContent(await callTool("figma_execute", { code, timeout: 30000 }, 90000));
  writeFileSync(join(outDir, "apply-old-resource-bar-result.json"), JSON.stringify(executed, null, 2));
  if (!executed?.success) throw new Error(`figma_execute failed: ${JSON.stringify(executed)}`);
  const result = executed.result;
  const screenshots = [];
  for (const item of result.mutated) {
    const shot = await callTool("figma_take_screenshot", { nodeId: item.frameId, scale: 0.5, format: "png" }, 90000);
    const saved = saveScreenshot(item.frame, shot);
    screenshots.push({ frame: item.frame, ...saved });
  }
  writeFileSync(join(outDir, "apply-old-resource-bar-screenshots.json"), JSON.stringify(screenshots, null, 2));
  console.log(JSON.stringify({ audit: result.audit, screenshots: screenshots.map(s => s.filePath) }, null, 2));
}

main()
  .catch((error) => {
    console.error(error.stack || error.message);
    process.exitCode = 1;
  })
  .finally(killTree);
