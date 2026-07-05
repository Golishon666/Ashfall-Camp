import { spawn, spawnSync } from "node:child_process";
import { writeFileSync } from "node:fs";
import { join } from "node:path";

const outDir = join(process.cwd(), "tmp", "figma-clean-rebuild");
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

function parseJsonContent(toolResult) {
  const text = toolResult?.content?.find((item) => item.type === "text")?.text;
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
      if (msg.error) call.reject(new Error(JSON.stringify(msg.error)));
      else call.resolve(msg.result);
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
function b(node) {
  const box = node.absoluteBoundingBox;
  return box ? { x: Math.round(box.x), y: Math.round(box.y), w: Math.round(box.width), h: Math.round(box.height) } : null;
}
function within(frame, node) {
  const fb = frame.absoluteBoundingBox;
  const nb = node.absoluteBoundingBox;
  if (!fb || !nb) return null;
  return { x: Math.round(nb.x - fb.x), y: Math.round(nb.y - fb.y), w: Math.round(nb.width), h: Math.round(nb.height) };
}
const result = [];
for (const page of figma.root.children) {
  const frames = page.children.filter(n => n.type === "FRAME" && /Ashfall Camp|Full HD|Camp Dashboard|Expedition Monitor|Buildings|Survivors/i.test(n.name));
  result.push({
    page: page.name,
    pageId: page.id,
    childCount: page.children.length,
    frames: frames.map(fr => {
      const children = fr.children || [];
      const navish = children.filter(n => {
        const r = within(fr, n);
        return r && (r.y > 920 || /nav|bottom|resource|pill|top resource/i.test(n.name));
      }).slice(0, 80).map(n => ({ id: n.id, name: n.name, type: n.type, rel: within(fr, n), abs: b(n), childCount: n.children ? n.children.length : 0 }));
      return { id: fr.id, name: fr.name, rel: within(fr, fr), abs: b(fr), childCount: children.length, navish };
    })
  });
}
return result;
`;

async function main() {
  await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-inspect-nav", version: "1.0.0" },
  });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  await waitForBridge();
  const toolResult = await callTool("figma_execute", { code, timeout: 30000 }, 90000);
  const parsed = parseJsonContent(toolResult);
  writeFileSync(join(outDir, "inspect-nav-resources.json"), JSON.stringify(parsed, null, 2));
  console.log(JSON.stringify(parsed?.result || parsed, null, 2));
}

main().catch((error) => {
  console.error(error.stack || error.message);
  process.exitCode = 1;
}).finally(killTree);
