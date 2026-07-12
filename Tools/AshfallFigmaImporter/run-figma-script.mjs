import { spawn, spawnSync } from "node:child_process";
import { existsSync, readFileSync } from "node:fs";
import { resolve } from "node:path";

const scriptPath = resolve(process.argv[2] || "");
if (!existsSync(scriptPath)) throw new Error(`Figma script not found: ${scriptPath}`);

const port = process.env.FIGMA_WS_PORT || "9225";
const expectedFileName = process.env.ASHFALL_FIGMA_FILE || "Ashfall Camp UI Concept";
const mcpCmd = `${process.env.APPDATA}\\npm\\figma-console-mcp.cmd`;
if (!existsSync(mcpCmd)) throw new Error(`figma-console-mcp not found: ${mcpCmd}`);

const child = spawn("cmd.exe", ["/d", "/s", "/c", mcpCmd], {
  env: { ...process.env, FIGMA_WS_PORT: port, NO_COLOR: "1" },
  stdio: ["pipe", "pipe", "pipe"],
  windowsHide: true,
});

let nextId = 1;
let buffer = "";
const pending = new Map();

function send(method, params = {}, timeoutMs = 45000) {
  const id = nextId++;
  child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", id, method, params })}\n`);
  return new Promise((resolvePromise, rejectPromise) => {
    const timer = setTimeout(() => {
      pending.delete(id);
      rejectPromise(new Error(`Timeout waiting for ${method}`));
    }, timeoutMs);
    pending.set(id, { resolve: resolvePromise, reject: rejectPromise, timer });
  });
}

function notify(method, params = {}) {
  child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", method, params })}\n`);
}

function parse(result) {
  const value = result?.content?.find(item => item.type === "text")?.text;
  if (!value) return null;
  try { return JSON.parse(value); } catch { return { rawText: value }; }
}

child.stdout.on("data", chunk => {
  buffer += chunk.toString("utf8");
  let index;
  while ((index = buffer.indexOf("\n")) >= 0) {
    const line = buffer.slice(0, index).trim();
    buffer = buffer.slice(index + 1);
    if (!line) continue;
    let message;
    try { message = JSON.parse(line); } catch { continue; }
    if (!message.id || !pending.has(message.id)) continue;
    const call = pending.get(message.id);
    pending.delete(message.id);
    clearTimeout(call.timer);
    message.error ? call.reject(new Error(JSON.stringify(message.error))) : call.resolve(message.result);
  }
});
child.stderr.on("data", chunk => process.stderr.write(chunk));

function killTree() {
  try { child.stdin.end(); } catch {}
  if (child.pid) spawnSync("taskkill.exe", ["/pid", String(child.pid), "/t", "/f"], { stdio: "ignore" });
}

try {
  await send("initialize", { protocolVersion: "2025-06-18", capabilities: {}, clientInfo: { name: "ashfall-figma-sync", version: "1.0.0" } });
  notify("notifications/initialized", {});
  await send("tools/list", {});

  let status;
  for (let attempt = 0; attempt < 40; attempt++) {
    status = parse(await send("tools/call", { name: "figma_get_status", arguments: { probe: true } }, 20000));
    const fileName = status?.currentFileName || status?.transport?.websocket?.connectedFile?.fileName || status?.transport?.websocket?.connectedFiles?.find(file => file.isActive)?.fileName || "";
    if (status?.setup?.valid && status?.setup?.probeResult?.success) {
      if (fileName !== expectedFileName) throw new Error(`Connected Figma file is "${fileName}", expected "${expectedFileName}".`);
      break;
    }
    await new Promise(resolvePromise => setTimeout(resolvePromise, 1000));
  }
  if (!status?.setup?.probeResult?.success) throw new Error("Figma Desktop Bridge did not connect.");

  const code = readFileSync(scriptPath, "utf8");
  const result = parse(await send("tools/call", { name: "figma_execute", arguments: { code, timeout: 240000 } }, 300000));
  if (!result?.success) throw new Error(`figma_execute failed: ${JSON.stringify(result)?.slice(0, 4000)}`);
  process.stdout.write(`${JSON.stringify(result.result, null, 2)}\n`);
} finally {
  killTree();
}
