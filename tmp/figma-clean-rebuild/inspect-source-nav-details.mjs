import { spawn, spawnSync } from "node:child_process";

const mcpCmd = `${process.env.APPDATA}\\npm\\figma-console-mcp.cmd`;
const child = spawn("cmd.exe", ["/d", "/s", "/c", mcpCmd], {
  env: { ...process.env, FIGMA_WS_PORT: "9225", NO_COLOR: "1" },
  stdio: ["pipe", "pipe", "pipe"],
  windowsHide: true,
});
let id = 1;
let buffer = "";
const pending = new Map();
function send(method, params = {}, timeout = 45000) {
  const msgId = id++;
  child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", id: msgId, method, params })}\n`);
  return new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      if (!pending.has(msgId)) return;
      pending.delete(msgId);
      reject(new Error(`Timeout ${method}`));
    }, timeout);
    pending.set(msgId, { resolve, reject, timer });
  });
}
function notify(method, params = {}) {
  child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", method, params })}\n`);
}
function callTool(name, args = {}, timeout = 60000) {
  return send("tools/call", { name, arguments: args }, timeout);
}
function parse(result) {
  const text = result?.content?.find((i) => i.type === "text")?.text;
  return text ? JSON.parse(text) : null;
}
child.stdout.on("data", (chunk) => {
  buffer += chunk.toString("utf8");
  let idx;
  while ((idx = buffer.indexOf("\n")) >= 0) {
    const line = buffer.slice(0, idx).trim();
    buffer = buffer.slice(idx + 1);
    if (!line) continue;
    let msg;
    try { msg = JSON.parse(line); } catch { continue; }
    if (msg.id && pending.has(msg.id)) {
      const p = pending.get(msg.id);
      pending.delete(msg.id);
      clearTimeout(p.timer);
      msg.error ? p.reject(new Error(JSON.stringify(msg.error))) : p.resolve(msg.result);
    }
  }
});
child.stderr.on("data", (chunk) => process.stderr.write(chunk));
function kill() {
  try { child.stdin.end(); } catch {}
  if (child.pid) spawnSync("taskkill.exe", ["/pid", String(child.pid), "/t", "/f"], { stdio: "ignore" });
}
async function waitBridge() {
  for (let i = 0; i < 36; i++) {
    const status = parse(await callTool("figma_get_status", { probe: true }, 20000));
    if (status?.setup?.valid && status?.setup?.probeResult?.success) return;
    await new Promise((r) => setTimeout(r, 1500));
  }
  throw new Error("bridge not connected");
}
const code = String.raw`
await figma.loadAllPagesAsync();
function rel(parent, node) {
  const pb = parent.absoluteBoundingBox;
  const nb = node.absoluteBoundingBox;
  return pb && nb ? { x: Math.round(nb.x - pb.x), y: Math.round(nb.y - pb.y), w: Math.round(nb.width), h: Math.round(nb.height) } : null;
}
function nodeInfo(parent, n) {
  return {
    id: n.id,
    type: n.type,
    name: n.name,
    rel: rel(parent, n),
    text: n.type === "TEXT" ? n.characters : undefined,
    fills: n.fills && Array.isArray(n.fills) ? n.fills.slice(0, 2) : undefined,
    childCount: n.children ? n.children.length : 0
  };
}
const sourcePage = figma.root.children.find(p => p.name === "Page 1");
const sourceFrame = sourcePage.children.find(n => n.name === "Ashfall Camp - Full HD / 01 Camp Dashboard");
const resource = sourceFrame.children.find(n => n.name === "Top Resource Bar");
const nav = sourceFrame.children.find(n => n.name === "Bottom Nav / Camp");
return {
  sourceFrame: nodeInfo(sourceFrame, sourceFrame),
  resource: {
    root: nodeInfo(sourceFrame, resource),
    descendants: resource.findAll(n => n.type === "TEXT" || n.type === "RECTANGLE" || n.type === "INSTANCE").map(n => nodeInfo(resource, n))
  },
  nav: {
    root: nodeInfo(sourceFrame, nav),
    descendants: nav.findAll(n => n.type === "TEXT" || n.type === "RECTANGLE" || n.type === "FRAME" || n.type === "INSTANCE").map(n => nodeInfo(nav, n))
  }
};
`;
async function main() {
  await send("initialize", { protocolVersion: "2025-06-18", capabilities: {}, clientInfo: { name: "codex-nav-detail", version: "1.0.0" } });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  await waitBridge();
  const result = parse(await callTool("figma_execute", { code, timeout: 30000 }, 90000));
  console.log(JSON.stringify(result?.result || result, null, 2));
}
main().catch((e) => { console.error(e.stack || e.message); process.exitCode = 1; }).finally(kill);
