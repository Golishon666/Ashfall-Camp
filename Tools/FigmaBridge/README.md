# Ashfall Local Figma Bridge

Local bridge for driving an open Figma file without the hosted Figma connector call limit.

The flow is:

1. Start `server.mjs` on `localhost`.
2. Run the development plugin in Figma.
3. Paste the server token into the plugin UI.
4. Send Figma Plugin API JavaScript through `client.mjs` or the optional MCP wrapper.

## Security

This bridge can execute arbitrary Figma Plugin API JavaScript in the open file. Keep it on `localhost`, keep the token private, and stop the server when done.

## Start The Server

```powershell
cd "C:\Unity Project\Ashfall Camp\Tools\FigmaBridge"
$env:FIGMA_BRIDGE_TOKEN = "choose-a-long-random-token"
node server.mjs
```

If `FIGMA_BRIDGE_TOKEN` is omitted, the server prints a temporary random token.

## Install The Figma Plugin

1. Open Figma desktop.
2. Go to `Plugins` -> `Development` -> `Import plugin from manifest...`.
3. Select `C:\Unity Project\Ashfall Camp\Tools\FigmaBridge\plugin\manifest.json`.
4. Run `Ashfall Local Bridge`.
5. Paste the token from the server output.
6. Click `Connect`.

## Send A Test Command

In another PowerShell:

```powershell
cd "C:\Unity Project\Ashfall Camp\Tools\FigmaBridge"
$env:FIGMA_BRIDGE_TOKEN = "choose-a-long-random-token"
node client.mjs --file examples/ping.js
```

You should get the current Figma page and top-level nodes as JSON.

## Run Inline Code

```powershell
node client.mjs --code "const r = figma.createRectangle(); r.resize(120, 80); r.fills = [helpers.solid('#C96A3A')]; figma.currentPage.appendChild(r); return helpers.nodeSummary(r);"
```

Bridge scripts run inside an async function and receive:

- `figma`: the Figma Plugin API global.
- `payload`: optional JSON from `client.mjs --payload`.
- `helpers`: small helpers for colors, text updates, screenshots, and node summaries.

## Optional MCP Wrapper

Install dependencies once:

```powershell
cd "C:\Unity Project\Ashfall Camp\Tools\FigmaBridge"
npm install
```

Then configure an MCP client to run:

```powershell
node "C:\Unity Project\Ashfall Camp\Tools\FigmaBridge\mcp-server.mjs"
```

Environment:

```powershell
FIGMA_BRIDGE_TOKEN=choose-a-long-random-token
FIGMA_BRIDGE_HOST=localhost
FIGMA_BRIDGE_PORT=4827
```

MCP tools exposed:

- `figma_bridge_health`
- `figma_run`

The MCP wrapper still requires `server.mjs` to be running and the Figma plugin to be connected.
