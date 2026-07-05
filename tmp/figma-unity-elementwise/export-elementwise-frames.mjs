import { spawn, spawnSync } from "node:child_process";
import { existsSync, mkdirSync, rmSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const frames = [
  { frameId: "45:11721", slug: "resource-bar", unityName: "ResourceBarElementwise" },
  { frameId: "45:11957", slug: "buildings", unityName: "BuildingsElementwise" },
  { frameId: "45:12157", slug: "expedition-planning", unityName: "ExpeditionPlanningElementwise" },
  { frameId: "45:12415", slug: "workshop", unityName: "WorkshopElementwise" },
  { frameId: "45:12650", slug: "world-map", unityName: "WorldMapElementwise" },
];

const outDir = join(process.cwd(), "tmp", "figma-unity-elementwise");
mkdirSync(outDir, { recursive: true });

const mcpCmd = `${process.env.APPDATA}\\npm\\figma-console-mcp.cmd`;
if (!existsSync(mcpCmd)) throw new Error(`figma-console-mcp not found: ${mcpCmd}`);

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
  try {
    child.stdin.end();
  } catch {}
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

function extractionCode(frameId) {
  return `
await figma.loadAllPagesAsync();
const root = await figma.getNodeByIdAsync(${JSON.stringify(frameId)});
if (!root) throw new Error("Frame not found: " + ${JSON.stringify(frameId)});
if (!root.absoluteBoundingBox) throw new Error("Frame has no absoluteBoundingBox");

const rootBounds = root.absoluteBoundingBox;
const exports = [];
let nodeCount = 0;
let textCount = 0;
let visualCount = 0;

function clonePaint(p) {
  if (!p || p.visible === false) return null;
  const out = { type: p.type, opacity: p.opacity == null ? 1 : p.opacity };
  if (p.color) out.color = p.color;
  if (p.scaleMode) out.scaleMode = p.scaleMode;
  if (p.imageHash) out.hasImage = true;
  return out;
}

function visiblePaints(paints) {
  return Array.isArray(paints) ? paints.map(clonePaint).filter(Boolean) : [];
}

function hasRenderablePaint(node) {
  return visiblePaints(node.fills).length > 0 ||
    visiblePaints(node.strokes).length > 0 ||
    (Array.isArray(node.effects) && node.effects.some(e => e.visible !== false));
}

function visibleChildren(node) {
  return "children" in node ? node.children.filter(c => c.visible !== false && c.absoluteBoundingBox) : [];
}

function textLooksLikeIcon(node) {
  if (node.type !== "TEXT") return false;
  const value = (node.characters || "").trim();
  if (!value) return false;
  if ((node.name || "").toLowerCase().includes("icon") && value.length <= 6) return true;
  if (value.length <= 4 && /[^\\x20-\\x7E]/.test(value)) return true;
  return false;
}

function boundsOf(node) {
  const b = node.absoluteBoundingBox || { x: rootBounds.x, y: rootBounds.y, width: 0, height: 0 };
  return { x: b.x - rootBounds.x, y: b.y - rootBounds.y, width: b.width, height: b.height };
}

function textData(node) {
  let fontName = null;
  if (node.fontName && typeof node.fontName === "object") {
    fontName = { family: node.fontName.family || "", style: node.fontName.style || "" };
  }
  return {
    characters: node.characters || "",
    fontName,
    fontSize: typeof node.fontSize === "number" ? node.fontSize : 16,
    lineHeight: node.lineHeight || null,
    letterSpacing: node.letterSpacing || null,
    textAlignHorizontal: node.textAlignHorizontal || "LEFT",
    textAlignVertical: node.textAlignVertical || "TOP",
    textAutoResize: node.textAutoResize || "NONE"
  };
}

function renderMode(node, children) {
  const b = boundsOf(node);
  if (b.width <= 0 || b.height <= 0) return "skip";
  if (node.type === "TEXT") return textLooksLikeIcon(node) ? "visual" : "text";
  if (children.length === 0) return hasRenderablePaint(node) ? "visual" : "container";
  return hasRenderablePaint(node) ? "background" : "container";
}

function walk(node, parentId, depth, siblingIndex, path) {
  nodeCount++;
  const children = visibleChildren(node);
  const mode = renderMode(node, children);
  if (mode === "text") textCount++;
  if (mode === "visual" || mode === "background") visualCount++;

  const b = boundsOf(node);
  const data = {
    id: node.id,
    parentId,
    name: node.name || node.type,
    type: node.type,
    depth,
    siblingIndex,
    path,
    renderMode: mode,
    clipsContent: !!node.clipsContent,
    opacity: node.opacity == null ? 1 : node.opacity,
    bounds: b,
    fills: visiblePaints(node.fills),
    strokes: visiblePaints(node.strokes),
    strokeWeight: typeof node.strokeWeight === "number" ? node.strokeWeight : 0,
    cornerRadius: typeof node.cornerRadius === "number" ? node.cornerRadius : 0,
    text: node.type === "TEXT" ? textData(node) : null,
    children: []
  };

  if (mode === "visual" || mode === "background") {
    exports.push({ id: node.id, name: node.name || node.type, mode, bounds: b, path });
  }

  data.children = children.map((child, index) =>
    walk(child, node.id, depth + 1, index, path + "/" + (child.name || child.type) + "[" + index + "]"));
  return data;
}

async function exportNode(item, index) {
  const node = await figma.getNodeByIdAsync(item.id);
  if (!node || !("exportAsync" in node)) return { ...item, error: "not exportable" };

  let target = node;
  let cleanup = null;
  if (item.mode === "background" && "children" in node) {
    const clone = node.clone();
    node.parent.appendChild(clone);
    clone.x = node.x;
    clone.y = node.y;
    clone.visible = true;
    while (clone.children.length > 0) clone.children[0].remove();
    target = clone;
    cleanup = clone;
  }

  const bytes = await target.exportAsync({ format: "PNG", constraint: { type: "SCALE", value: 2 } });
  if (cleanup) cleanup.remove();
  return { ...item, index, scale: 2, byteLength: bytes.length, base64: figma.base64Encode(bytes) };
}

const tree = walk(root, null, 0, 0, root.name || root.type);
const exported = [];
for (let i = 0; i < exports.length; i++) exported.push(await exportNode(exports[i], i));

return {
  frameId: root.id,
  frameName: root.name,
  width: root.width,
  height: root.height,
  rootBounds,
  nodeCount,
  textCount,
  visualCount,
  tree,
  exports: exported
};
`;
}

function safeName(value) {
  return String(value || "node")
    .replace(/[^a-z0-9]+/gi, "-")
    .replace(/^-|-$/g, "")
    .slice(0, 54)
    .toLowerCase() || "node";
}

function attachAssetPaths(node, exportMap) {
  if (exportMap.has(node.id)) node.assetPath = exportMap.get(node.id);
  for (const child of node.children || []) attachAssetPaths(child, exportMap);
}

try {
  await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-ashfall-elementwise-bulk-export", version: "1.0.0" },
  });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  await waitForBridge();

  const summary = [];
  for (const frame of frames) {
    const assetDir = join(
      process.cwd(),
      "Assets",
      "AshfallCamp",
      "Art",
      "UI",
      "FigmaExports",
      frame.unityName,
      "ElementAssets",
    );
    rmSync(assetDir, { recursive: true, force: true });
    mkdirSync(assetDir, { recursive: true });

    const result = parseJsonContent(await callTool("figma_execute", {
      code: extractionCode(frame.frameId),
      timeout: 240000,
    }, 300000));

    if (!result?.success || !result?.result?.tree) {
      throw new Error(`figma_execute failed for ${frame.slug}: ${JSON.stringify(result)?.slice(0, 4000)}`);
    }

    const payload = result.result;
    const exportMap = new Map();
    let saved = 0;
    for (const item of payload.exports || []) {
      if (!item.base64 || item.error) continue;
      const fileName = `${String(item.index).padStart(4, "0")}-${safeName(item.name)}-${item.id.replace(/[^a-z0-9]+/gi, "-")}.png`;
      const unityPath = `Assets/AshfallCamp/Art/UI/FigmaExports/${frame.unityName}/ElementAssets/${fileName}`;
      writeFileSync(join(assetDir, fileName), Buffer.from(item.base64, "base64"));
      exportMap.set(item.id, unityPath);
      saved++;
      delete item.base64;
    }
    attachAssetPaths(payload.tree, exportMap);

    const jsonPath = join(outDir, `${frame.slug}-elementwise-tree.json`);
    writeFileSync(jsonPath, JSON.stringify(payload, null, 2));

    summary.push({
      slug: frame.slug,
      jsonPath,
      assetDir,
      frameName: payload.frameName,
      frameSize: `${payload.width}x${payload.height}`,
      nodeCount: payload.nodeCount,
      textCount: payload.textCount,
      visualCount: payload.visualCount,
      exportedPngs: saved,
    });
  }

  console.log(JSON.stringify(summary, null, 2));
} finally {
  killTree();
}
