import http from "node:http";
import { McpServer } from "@modelcontextprotocol/sdk/server/mcp.js";
import { StdioServerTransport } from "@modelcontextprotocol/sdk/server/stdio.js";
import { z } from "zod";

const bridgeHost = process.env.FIGMA_BRIDGE_HOST || "localhost";
const bridgePort = Number(process.env.FIGMA_BRIDGE_PORT || 4827);
const bridgeToken = process.env.FIGMA_BRIDGE_TOKEN || "";

const server = new McpServer({
  name: "ashfall-figma-bridge",
  version: "0.1.0"
});

server.tool(
  "figma_run",
  "Run JavaScript in the currently open Figma file through the local bridge plugin.",
  {
    code: z.string().describe("Plugin API JavaScript. Supports top-level await and return."),
    payload: z.unknown().optional().describe("Optional JSON payload exposed as payload in the plugin runtime."),
    timeoutMs: z.number().int().min(1000).max(600000).optional()
  },
  async ({ code, payload = null, timeoutMs = 120000 }) => {
    if (!bridgeToken) {
      return {
        isError: true,
        content: [{ type: "text", text: "Missing FIGMA_BRIDGE_TOKEN in the MCP server environment." }]
      };
    }

    const response = await postJson({
      host: bridgeHost,
      port: bridgePort,
      path: "/run",
      token: bridgeToken,
      body: { code, payload, timeoutMs }
    });

    return {
      isError: !response.ok,
      content: [{ type: "text", text: JSON.stringify(response, null, 2) }]
    };
  }
);

server.tool(
  "figma_bridge_health",
  "Check whether the local Figma bridge server and plugin are connected.",
  {},
  async () => {
    const response = await getJson({ host: bridgeHost, port: bridgePort, path: "/health" });
    return {
      content: [{ type: "text", text: JSON.stringify(response, null, 2) }]
    };
  }
);

await server.connect(new StdioServerTransport());

function getJson({ host, port, path }) {
  return new Promise((resolve, reject) => {
    const req = http.request({ host, port, path, method: "GET" }, res => {
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
    });
    req.on("error", reject);
    req.end();
  });
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
