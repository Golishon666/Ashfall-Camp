const BRIDGE_PLUGIN_VERSION = "0.1.2";

figma.showUI(__html__, {
  width: 460,
  height: 620,
  themeColors: true
});

figma.ui.postMessage({
  type: "figma-info",
  fileKey: figma.fileKey || null,
  editorType: figma.editorType || null,
  pluginVersion: BRIDGE_PLUGIN_VERSION
});

figma.ui.onmessage = async message => {
  if (!message || message.type !== "run-js") return;

  try {
    const result = await runUserCode(message.code, message.payload);
    figma.ui.postMessage({
      type: "run-js-result",
      requestId: message.requestId,
      ok: true,
      result: serialize(result)
    });
  } catch (error) {
    figma.ui.postMessage({
      type: "run-js-result",
      requestId: message.requestId,
      ok: false,
      error: error && error.stack ? error.stack : String(error)
    });
  }
};

async function runUserCode(code, payload) {
  if (typeof code !== "string" || code.trim().length === 0) {
    throw new Error("Bridge command code must be a non-empty string.");
  }

  const fn = new Function(
    "figma",
    "payload",
    "helpers",
    `"use strict"; return (async () => {\n${code}\n})()`
  );

  return await fn(figma, payload, helpers);
}

const helpers = {
  rgb(hex) {
    const h = hex.replace("#", "");
    return {
      r: parseInt(h.slice(0, 2), 16) / 255,
      g: parseInt(h.slice(2, 4), 16) / 255,
      b: parseInt(h.slice(4, 6), 16) / 255
    };
  },

  solid(hex, opacity = 1) {
    return { type: "SOLID", color: helpers.rgb(hex), opacity };
  },

  nodeSummary(node) {
    if (!node || typeof node !== "object" || !("id" in node)) return null;
    return {
      id: node.id,
      name: node.name,
      type: node.type,
      x: "x" in node ? node.x : undefined,
      y: "y" in node ? node.y : undefined,
      width: "width" in node ? node.width : undefined,
      height: "height" in node ? node.height : undefined
    };
  },

  async setText(textNode, characters) {
    if (!textNode || textNode.type !== "TEXT") {
      throw new Error("helpers.setText expects a TEXT node.");
    }
    const segments = textNode.getStyledTextSegments(["fontName"]);
    const unique = new Map();
    for (const segment of segments) {
      unique.set(JSON.stringify(segment.fontName), segment.fontName);
    }
    await Promise.all([...unique.values()].map(font => figma.loadFontAsync(font)));
    textNode.characters = characters;
    return helpers.nodeSummary(textNode);
  },

  async screenshot(node, scale = 1) {
    if (!node || typeof node.exportAsync !== "function") {
      throw new Error("helpers.screenshot expects an exportable scene node.");
    }
    const bytes = await node.exportAsync({
      format: "PNG",
      constraint: { type: "SCALE", value: scale }
    });
    return {
      mime: "image/png",
      base64: bytesToBase64(bytes),
      node: helpers.nodeSummary(node)
    };
  }
};

function serialize(value, depth = 0, seen = new Set()) {
  if (depth > 8) return "[MaxDepth]";
  if (value === undefined) return null;
  if (value === null || typeof value === "string" || typeof value === "number" || typeof value === "boolean") {
    return value;
  }
  if (typeof value === "bigint") return value.toString();
  if (typeof value === "function") return "[Function]";
  if (value instanceof Uint8Array) return { type: "Uint8Array", length: value.length };
  if (typeof value === "object") {
    if (seen.has(value)) return "[Circular]";
    seen.add(value);
    if ("id" in value && "type" in value && "name" in value) return helpers.nodeSummary(value);
    if (Array.isArray(value)) return value.map(item => serialize(item, depth + 1, seen));
    const output = {};
    for (const key of Object.keys(value)) {
      output[key] = serialize(value[key], depth + 1, seen);
    }
    return output;
  }
  return String(value);
}

function bytesToBase64(bytes) {
  let binary = "";
  const chunkSize = 0x8000;
  for (let i = 0; i < bytes.length; i += chunkSize) {
    const chunk = bytes.subarray(i, i + chunkSize);
    binary += String.fromCharCode.apply(null, chunk);
  }
  return btoa(binary);
}
