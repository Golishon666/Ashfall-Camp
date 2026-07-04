import http from "node:http";
import fs from "node:fs";

const args = parseArgs(process.argv.slice(2));
const host = args.host || process.env.FIGMA_BRIDGE_HOST || "localhost";
const port = Number(args.port || process.env.FIGMA_BRIDGE_PORT || 4827);
const token = args.token || process.env.FIGMA_BRIDGE_TOKEN || "";
const timeoutMs = Number(args.timeout || process.env.FIGMA_BRIDGE_TIMEOUT_MS || 120000);

if (!token) {
  fail("Missing token. Pass --token or set FIGMA_BRIDGE_TOKEN.");
}

const code = await readCode(args);
const payload = readPayload(args.payload);
const result = await postJson({
  host,
  port,
  path: "/run",
  token,
  body: { code, payload, timeoutMs }
});

process.stdout.write(`${JSON.stringify(result, null, 2)}\n`);
if (!result.ok) process.exitCode = 1;

function parseArgs(values) {
  const parsed = {};
  for (let i = 0; i < values.length; i += 1) {
    const key = values[i];
    if (!key.startsWith("--")) continue;
    parsed[key.slice(2)] = values[i + 1];
    i += 1;
  }
  return parsed;
}

async function readCode(parsed) {
  if (parsed.code) return parsed.code;
  if (parsed.file) return fs.readFileSync(parsed.file, "utf8");
  return await new Promise(resolve => {
    let text = "";
    process.stdin.setEncoding("utf8");
    process.stdin.on("data", chunk => {
      text += chunk;
    });
    process.stdin.on("end", () => resolve(text));
  });
}

function readPayload(value) {
  if (!value) return null;
  if (fs.existsSync(value)) return JSON.parse(fs.readFileSync(value, "utf8"));
  return JSON.parse(value);
}

function postJson({ host, port, path, token, body }) {
  const text = JSON.stringify(body);
  return new Promise((resolve, reject) => {
    const req = http.request(
      {
        host,
        port,
        path,
        method: "POST",
        headers: {
          "content-type": "application/json",
          "content-length": Buffer.byteLength(text),
          "x-figma-bridge-token": token
        }
      },
      res => {
        let raw = "";
        res.setEncoding("utf8");
        res.on("data", chunk => {
          raw += chunk;
        });
        res.on("end", () => {
          try {
            resolve(JSON.parse(raw));
          } catch (error) {
            reject(error);
          }
        });
      }
    );
    req.on("error", reject);
    req.write(text);
    req.end();
  });
}

function fail(message) {
  process.stderr.write(`${message}\n`);
  process.exit(1);
}
