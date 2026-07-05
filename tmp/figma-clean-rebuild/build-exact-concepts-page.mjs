import { spawn, spawnSync } from "node:child_process";
import { mkdirSync, readFileSync, writeFileSync } from "node:fs";
import { basename, join } from "node:path";

const outDir = join(process.cwd(), "tmp", "figma-exact-concepts");
const shotsDir = join(outDir, "screenshots");
mkdirSync(shotsDir, { recursive: true });

const images = [
  {
    key: "resource",
    title: "00 Resource Panel Exact",
    path: "C:\\Users\\jaros\\AppData\\Local\\Temp\\codex-clipboard-49ce7b18-fc5d-4dd9-b720-962836011177.png",
    frame: { w: 1920, h: 188 },
  },
  {
    key: "dashboard",
    title: "01 Camp Dashboard Exact",
    path: "C:\\Unity Project\\Ashfall Camp\\Assets\\Concepts\\ui_concept_camp_dashboard_01.png",
    frame: { w: 1920, h: 1080 },
    coverCampSummary: true,
  },
  {
    key: "buildings",
    title: "02 Buildings Exact",
    path: "C:\\Unity Project\\Ashfall Camp\\Assets\\Concepts\\ui_concept_buildings_01.png",
    frame: { w: 1920, h: 1080 },
  },
  {
    key: "expedition",
    title: "03 Expedition Planning Exact",
    path: "C:\\Unity Project\\Ashfall Camp\\Assets\\Concepts\\ui_concept_expedition_setup_01.png",
    frame: { w: 1920, h: 1080 },
  },
  {
    key: "workshop",
    title: "04 Workshop Exact",
    path: "C:\\Unity Project\\Ashfall Camp\\Assets\\Concepts\\ui_concept_workshop_01.png",
    frame: { w: 1920, h: 1080 },
  },
  {
    key: "map",
    title: "05 World Map Exact",
    path: "C:\\Unity Project\\Ashfall Camp\\Assets\\Concepts\\ui_concept_world_map_01.png",
    frame: { w: 1920, h: 1080 },
  },
];

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

const specsForFigma = images.map((item) => ({
  key: item.key,
  title: item.title,
  sourceName: basename(item.path),
  frame: item.frame,
  coverCampSummary: Boolean(item.coverCampSummary),
}));

const createCode = String.raw`
await figma.loadAllPagesAsync();

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
async function font(style) {
  try {
    await figma.loadFontAsync({ family: "Inter", style });
    return { family: "Inter", style };
  } catch {
    await figma.loadFontAsync({ family: "Inter", style: "Regular" });
    return { family: "Inter", style: "Regular" };
  }
}
const bold = await font("Bold");
const reg = await font("Regular");
function text(parent, name, value, x, y, w, h, size, style, color) {
  const t = figma.createText();
  t.name = name;
  parent.appendChild(t);
  t.x = x; t.y = y; t.resize(w, h);
  t.textAutoResize = "NONE";
  t.fontName = style === "Bold" ? bold : reg;
  t.fontSize = size;
  t.lineHeight = { unit: "PIXELS", value: Math.ceil(size * 1.2) };
  t.fills = [paint(color)];
  t.characters = value;
  return t;
}

const pageName = "Ashfall Camp - Exact Concepts";
const existing = figma.root.children.filter(p => p.name === pageName);
const fallback = figma.root.children.find(p => p.name !== pageName);
if (fallback) await figma.setCurrentPageAsync(fallback);
for (const p of existing) {
  if (figma.root.children.length > 1) p.remove();
}
const page = figma.createPage();
page.name = pageName;
await figma.setCurrentPageAsync(page);

const specs = SPECS_JSON;
const created = [];
for (let i = 0; i < specs.length; i++) {
  const spec = specs[i];
  const col = i % 2;
  const row = Math.floor(i / 2);
  const x = col * 2040;
  const y = row * 1240;
  const f = figma.createFrame();
  f.name = "Ashfall Camp - Full HD Exact / " + spec.title;
  page.appendChild(f);
  f.x = x;
  f.y = y;
  f.resize(spec.frame.w, spec.frame.h);
  f.fills = [paint("#efe4cf")];
  f.clipsContent = false;
  const imageRect = rect(f, "Exact concept image / " + spec.sourceName, 0, 0, spec.frame.w, spec.frame.h, "#efe4cf", null, 0, 0);
  created.push({ key: spec.key, title: spec.title, frameId: f.id, imageNodeId: imageRect.id, sourceName: spec.sourceName });

  if (spec.coverCampSummary) {
    rect(f, "Removed Camp Summary cover", 32, 665, 385, 265, "#f1e4ca", "#c6ad86", 8, 1);
  }
}
figma.viewport.scrollAndZoomIntoView(page.children);
return { pageName, created };
`.replace("SPECS_JSON", JSON.stringify(specsForFigma));

async function main() {
  await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-exact-concept-page", version: "1.0.0" },
  });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  await waitForBridge();

  const createdResult = parseJsonContent(await callTool("figma_execute", { code: createCode, timeout: 30000 }, 90000));
  writeFileSync(join(outDir, "create-result.json"), JSON.stringify(createdResult, null, 2));
  if (!createdResult?.success) throw new Error(`figma_execute failed: ${JSON.stringify(createdResult)}`);

  const created = createdResult.result.created;
  for (const item of created) {
    const image = images.find((img) => img.key === item.key);
    const base64 = readFileSync(image.path).toString("base64");
    const fillResult = parseJsonContent(await callTool("figma_set_image_fill", {
      nodeIds: [item.imageNodeId],
      imageData: base64,
      scaleMode: "FILL",
    }, 120000));
    if (!fillResult?.success) throw new Error(`image fill failed for ${item.title}: ${JSON.stringify(fillResult)}`);
  }

  const screenshots = [];
  for (const item of created) {
    const shot = await callTool("figma_take_screenshot", {
      nodeId: item.frameId,
      scale: item.key === "resource" ? 1 : 0.5,
      format: "png",
    }, 120000);
    const saved = saveScreenshot(item.title, shot);
    screenshots.push({ title: item.title, frameId: item.frameId, ...saved });
  }
  writeFileSync(join(outDir, "screenshots.json"), JSON.stringify(screenshots, null, 2));
  console.log(JSON.stringify({
    pageName: createdResult.result.pageName,
    frames: created.map((item) => ({ title: item.title, frameId: item.frameId })),
    screenshots: screenshots.map((shot) => shot.filePath),
  }, null, 2));
}

main().catch((error) => {
  console.error(error.stack || error.message);
  process.exitCode = 1;
}).finally(killTree);
