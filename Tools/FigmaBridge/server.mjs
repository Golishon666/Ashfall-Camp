import http from "node:http";
import crypto from "node:crypto";

const host = process.env.FIGMA_BRIDGE_HOST || "localhost";
const port = Number(process.env.FIGMA_BRIDGE_PORT || 4827);
const token = process.env.FIGMA_BRIDGE_TOKEN || crypto.randomBytes(24).toString("base64url");
const requestTimeoutMs = Number(process.env.FIGMA_BRIDGE_TIMEOUT_MS || 120000);

let pluginSocket = null;
let pluginInfo = null;
const pending = new Map();

function sendJson(res, status, body) {
  const text = JSON.stringify(body, null, 2);
  res.writeHead(status, {
    "content-type": "application/json; charset=utf-8",
    "content-length": Buffer.byteLength(text)
  });
  res.end(text);
}

function readBody(req, maxBytes = 5 * 1024 * 1024) {
  return new Promise((resolve, reject) => {
    let size = 0;
    const chunks = [];
    req.on("data", chunk => {
      size += chunk.length;
      if (size > maxBytes) {
        reject(new Error(`request body exceeds ${maxBytes} bytes`));
        req.destroy();
        return;
      }
      chunks.push(chunk);
    });
    req.on("end", () => resolve(Buffer.concat(chunks).toString("utf8")));
    req.on("error", reject);
  });
}

function requestToken(req, body) {
  const auth = req.headers.authorization || "";
  if (auth.startsWith("Bearer ")) return auth.slice("Bearer ".length).trim();
  if (req.headers["x-figma-bridge-token"]) return String(req.headers["x-figma-bridge-token"]);
  if (body && typeof body.token === "string") return body.token;
  return "";
}

function requireToken(req, body) {
  return requestToken(req, body) === token;
}

function health() {
  return {
    ok: true,
    host,
    port,
    pluginConnected: Boolean(pluginSocket),
    pluginInfo,
    pendingRequests: pending.size
  };
}

function runInFigma({ code, payload = null, timeoutMs = requestTimeoutMs }) {
  if (!pluginSocket) {
    throw new Error("No Figma plugin is connected. Start the local bridge plugin inside Figma first.");
  }
  if (typeof code !== "string" || code.trim().length === 0) {
    throw new Error("Missing non-empty code string.");
  }

  const requestId = crypto.randomUUID();
  const timeout = Math.max(1000, Math.min(Number(timeoutMs) || requestTimeoutMs, 10 * 60 * 1000));

  const promise = new Promise((resolve, reject) => {
    const timer = setTimeout(() => {
      pending.delete(requestId);
      reject(new Error(`Figma request timed out after ${timeout} ms.`));
    }, timeout);

    pending.set(requestId, {
      resolve: value => {
        clearTimeout(timer);
        resolve(value);
      },
      reject: error => {
        clearTimeout(timer);
        reject(error);
      }
    });
  });

  pluginSocket.send({
    type: "figma.run",
    requestId,
    code,
    payload
  });

  return promise;
}

const server = http.createServer(async (req, res) => {
  try {
    const url = new URL(req.url || "/", `http://${host}:${port}`);

    if (req.method === "GET" && url.pathname === "/health") {
      sendJson(res, 200, health());
      return;
    }

    if (req.method === "POST" && (url.pathname === "/run" || url.pathname === "/eval")) {
      const raw = await readBody(req);
      const body = raw ? JSON.parse(raw) : {};
      if (!requireToken(req, body)) {
        sendJson(res, 401, { ok: false, error: "Invalid or missing FIGMA_BRIDGE_TOKEN." });
        return;
      }

      const result = await runInFigma(body);
      sendJson(res, 200, { ok: true, result });
      return;
    }

    sendJson(res, 404, { ok: false, error: "Not found." });
  } catch (error) {
    sendJson(res, 500, {
      ok: false,
      error: error instanceof Error ? error.message : String(error)
    });
  }
});

server.on("upgrade", (req, socket) => {
  const url = new URL(req.url || "/", `http://${host}:${port}`);
  if (url.pathname !== "/plugin" && url.pathname !== "/ws") {
    socket.destroy();
    return;
  }

  const key = req.headers["sec-websocket-key"];
  if (!key) {
    socket.destroy();
    return;
  }

  const accept = crypto
    .createHash("sha1")
    .update(`${key}258EAFA5-E914-47DA-95CA-C5AB0DC85B11`)
    .digest("base64");

  socket.write([
    "HTTP/1.1 101 Switching Protocols",
    "Upgrade: websocket",
    "Connection: Upgrade",
    `Sec-WebSocket-Accept: ${accept}`,
    "",
    ""
  ].join("\r\n"));

  const ws = new WebSocketPeer(socket);
  ws.onMessage = message => handlePluginMessage(ws, message);
  ws.onClose = () => {
    if (pluginSocket === ws) {
      pluginSocket = null;
      pluginInfo = null;
      for (const [id, request] of pending) {
        pending.delete(id);
        request.reject(new Error("Figma plugin disconnected."));
      }
    }
  };
});

function handlePluginMessage(ws, message) {
  let data;
  try {
    data = JSON.parse(message);
  } catch {
    ws.send({ type: "bridge.error", error: "Invalid JSON message." });
    return;
  }

  if (data.type === "hello") {
    if (data.token !== token) {
      ws.send({ type: "bridge.error", error: "Invalid token." });
      ws.close();
      return;
    }

    if (pluginSocket && pluginSocket !== ws) pluginSocket.close();
    pluginSocket = ws;
    pluginInfo = {
      connectedAt: new Date().toISOString(),
      pluginVersion: data.pluginVersion || "unknown",
      figmaFileKey: data.figmaFileKey || null,
      editorType: data.editorType || null
    };
    ws.send({ type: "hello.ok", health: health() });
    return;
  }

  if (data.type === "figma.result") {
    const request = pending.get(data.requestId);
    if (!request) return;
    pending.delete(data.requestId);
    if (data.ok) request.resolve(data.result);
    else request.reject(new Error(data.error || "Figma plugin returned an error."));
  }
}

class WebSocketPeer {
  constructor(socket) {
    this.socket = socket;
    this.buffer = Buffer.alloc(0);
    this.closed = false;
    this.onMessage = null;
    this.onClose = null;

    socket.on("data", chunk => this.receive(chunk));
    socket.on("close", () => this.close(false));
    socket.on("error", () => this.close(false));
  }

  send(value) {
    if (this.closed) return;
    const payload = Buffer.from(JSON.stringify(value), "utf8");
    let header;
    if (payload.length < 126) {
      header = Buffer.from([0x81, payload.length]);
    } else if (payload.length < 65536) {
      header = Buffer.alloc(4);
      header[0] = 0x81;
      header[1] = 126;
      header.writeUInt16BE(payload.length, 2);
    } else {
      header = Buffer.alloc(10);
      header[0] = 0x81;
      header[1] = 127;
      header.writeBigUInt64BE(BigInt(payload.length), 2);
    }
    this.socket.write(Buffer.concat([header, payload]));
  }

  receive(chunk) {
    this.buffer = Buffer.concat([this.buffer, chunk]);

    while (this.buffer.length >= 2) {
      const first = this.buffer[0];
      const second = this.buffer[1];
      const opcode = first & 0x0f;
      const masked = Boolean(second & 0x80);
      let length = second & 0x7f;
      let offset = 2;

      if (length === 126) {
        if (this.buffer.length < offset + 2) return;
        length = this.buffer.readUInt16BE(offset);
        offset += 2;
      } else if (length === 127) {
        if (this.buffer.length < offset + 8) return;
        length = Number(this.buffer.readBigUInt64BE(offset));
        offset += 8;
      }

      const maskLength = masked ? 4 : 0;
      if (this.buffer.length < offset + maskLength + length) return;

      let payload = this.buffer.subarray(offset + maskLength, offset + maskLength + length);
      if (masked) {
        const mask = this.buffer.subarray(offset, offset + 4);
        payload = Buffer.from(payload.map((byte, i) => byte ^ mask[i % 4]));
      }

      this.buffer = this.buffer.subarray(offset + maskLength + length);

      if (opcode === 0x8) {
        this.close();
        return;
      }
      if (opcode === 0x9) {
        this.socket.write(Buffer.from([0x8a, 0x00]));
        continue;
      }
      if (opcode === 0x1 && this.onMessage) {
        this.onMessage(payload.toString("utf8"));
      }
    }
  }

  close(writeFrame = true) {
    if (this.closed) return;
    this.closed = true;
    if (writeFrame) {
      try {
        this.socket.write(Buffer.from([0x88, 0x00]));
      } catch {}
    }
    try {
      this.socket.end();
    } catch {}
    if (this.onClose) this.onClose();
  }
}

server.listen(port, host, () => {
  console.log(`Figma bridge listening on http://${host}:${port}`);
  console.log(`WebSocket endpoint: ws://${host}:${port}/plugin`);
  console.log(`FIGMA_BRIDGE_TOKEN=${token}`);
  console.log("Keep this process running while the Figma plugin is open.");
});
