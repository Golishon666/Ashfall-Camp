import { spawn } from "node:child_process";

const cmd = `${process.env.APPDATA}\\npm\\figma-console-mcp.cmd`;
const child = spawn("cmd.exe", ["/d", "/s", "/c", cmd], {
  env: { ...process.env, FIGMA_WS_PORT: "9224", NO_COLOR: "1" },
  stdio: ["pipe", "pipe", "pipe"],
  windowsHide: true,
});

let nextId = 1;
let buffer = "";
const pending = new Map();

function send(method, params = {}) {
  const id = nextId++;
  const msg = { jsonrpc: "2.0", id, method, params };
  child.stdin.write(`${JSON.stringify(msg)}\n`);
  return new Promise((resolve, reject) => {
    pending.set(id, { resolve, reject, method });
    setTimeout(() => {
      if (pending.has(id)) {
        pending.delete(id);
        reject(new Error(`Timeout waiting for ${method}`));
      }
    }, 30000);
  });
}

function notify(method, params = {}) {
  child.stdin.write(`${JSON.stringify({ jsonrpc: "2.0", method, params })}\n`);
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
      console.error(`[server] ${line}`);
      continue;
    }
    if (msg.id && pending.has(msg.id)) {
      const pendingCall = pending.get(msg.id);
      pending.delete(msg.id);
      if (msg.error) pendingCall.reject(new Error(JSON.stringify(msg.error)));
      else pendingCall.resolve(msg.result);
    } else {
      console.error(`[notify] ${JSON.stringify(msg)}`);
    }
  }
});

child.stderr.on("data", (chunk) => {
  process.stderr.write(chunk);
});

child.on("exit", (code) => {
  for (const { reject, method } of pending.values()) {
    reject(new Error(`Server exited while waiting for ${method}, code ${code}`));
  }
  pending.clear();
});

async function main() {
  const init = await send("initialize", {
    protocolVersion: "2025-06-18",
    capabilities: {},
    clientInfo: { name: "codex-local", version: "1.0.0" },
  });
  console.log(JSON.stringify({ init }, null, 2));
  notify("notifications/initialized", {});

  const listed = await send("tools/list", {});
  const tools = listed.tools || [];
  console.log(JSON.stringify({
    count: tools.length,
    selected: tools
      .filter((tool) => /figma_(execute|get_status|take_screenshot|navigate|reconnect|diagnose)/.test(tool.name))
      .map((tool) => ({ name: tool.name, inputSchema: tool.inputSchema })),
  }, null, 2));

  child.stdin.end();
  child.kill();
}

main().catch((error) => {
  console.error(error.stack || error.message);
  child.kill();
  process.exit(1);
});
