import { spawn, spawnSync } from "node:child_process";
import { existsSync, mkdirSync, writeFileSync } from "node:fs";
import { join } from "node:path";

const outDir = join(process.cwd(), "tmp", "figma-unity-export-verification");
mkdirSync(outDir, { recursive: true });

const mcpCmd = `${process.env.APPDATA}\\npm\\figma-console-mcp.cmd`;
if (!existsSync(mcpCmd)) {
  throw new Error(`figma-console-mcp not found: ${mcpCmd}`);
}

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

function pngDimensions(bufferValue) {
  const signature = "89504e470d0a1a0a";
  if (bufferValue.subarray(0, 8).toString("hex") !== signature) return null;
  return {
    width: bufferValue.readUInt32BE(16),
    height: bufferValue.readUInt32BE(20),
  };
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
  if (child.pid) {
    spawnSync("taskkill.exe", ["/pid", String(child.pid), "/t", "/f"], { stdio: "ignore" });
  }
}

async function waitForBridge() {
  for (let i = 0; i < 40; i++) {
    const status = parseJsonContent(await callTool("figma_get_status", { probe: true }, 20000)) || {};
    if (status?.setup?.valid === true && status?.setup?.probeResult?.success === true) return status;
    await new Promise((resolve) => setTimeout(resolve, 1500));
  }
  throw new Error("Desktop Bridge did not connect");
}

async function exportScreenshot(scale) {
  const shot = await callTool("figma_take_screenshot", {
    nodeId: "45:11759",
    scale,
    format: "png",
  }, 180000);

  const image = shot?.content?.find((item) => item.type === "image");
  const meta = parseJsonContent(shot) || {};
  if (!image?.data) {
    throw new Error(`figma_take_screenshot returned no image at scale ${scale}: ${JSON.stringify(meta)}`);
  }

  const png = Buffer.from(image.data, "base64");
  const dimensions = pngDimensions(png);
  const filePath = join(outDir, scale >= 2 ? "camp-dashboard-elementwise-4k.png" : "camp-dashboard-elementwise-1080p.png");
  writeFileSync(filePath, png);
  return { filePath, scale, dimensions, meta };
}

function directExportCode(scale) {
  return `
await figma.loadAllPagesAsync();
const node = await figma.getNodeByIdAsync("45:11759");
if (!node) throw new Error("Node 45:11759 was not found");
if (!("exportAsync" in node)) throw new Error("Node does not support exportAsync: " + node.type);
const bytes = await node.exportAsync({ format: "PNG", constraint: { type: "SCALE", value: ${scale} } });
return {
  nodeId: node.id,
  nodeName: node.name,
  nodeType: node.type,
  requestedScale: ${scale},
  expectedWidth: Math.round(("width" in node ? node.width : 0) * ${scale}),
  expectedHeight: Math.round(("height" in node ? node.height : 0) * ${scale}),
  byteLength: bytes.length,
  base64: figma.base64Encode(bytes)
};
`;
}

async function exportDirect(scale) {
  const executed = parseJsonContent(await callTool("figma_execute", {
    code: directExportCode(scale),
    timeout: 180000,
  }, 240000));

  if (!executed?.success || !executed?.result?.base64) {
    throw new Error(`figma_execute export failed at scale ${scale}: ${JSON.stringify(executed)}`);
  }

  const png = Buffer.from(executed.result.base64, "base64");
  const dimensions = pngDimensions(png);
  const filePath = join(outDir, scale >= 2 ? "camp-dashboard-elementwise-exportasync-4k.png" : "camp-dashboard-elementwise-exportasync-1080p.png");
  writeFileSync(filePath, png);
  return {
    filePath,
    scale,
    dimensions,
    meta: {
      nodeId: executed.result.nodeId,
      nodeName: executed.result.nodeName,
      nodeType: executed.result.nodeType,
      requestedScale: executed.result.requestedScale,
      expectedWidth: executed.result.expectedWidth,
      expectedHeight: executed.result.expectedHeight,
      byteLength: executed.result.byteLength,
      source: "figma_execute_exportAsync",
    },
  };
}

try {
  await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-ashfall-highres-export", version: "1.0.0" },
  });
  notify("notifications/initialized", {});
  await send("tools/list", {});
  await waitForBridge();

  let result;
  try {
    result = await exportDirect(2);
  } catch (error) {
    process.stderr.write(`[warn] 2x direct export failed, retrying 1x: ${error.message}\n`);
    try {
      result = await exportDirect(1);
    } catch (fallbackError) {
      process.stderr.write(`[warn] 1x direct export failed, falling back to capped screenshot: ${fallbackError.message}\n`);
      result = await exportScreenshot(1);
    }
  }

  console.log(JSON.stringify(result, null, 2));
} finally {
  killTree();
}
