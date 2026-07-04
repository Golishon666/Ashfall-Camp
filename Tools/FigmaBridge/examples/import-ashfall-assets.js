const VERSION = "ashfall-asset-import-2026-07-05";

const images = Array.isArray(payload && payload.images) ? payload.images : [];
if (!images.length) {
  return { version: VERSION, imported: [], skipped: 0 };
}

const page = figma.currentPage;
const imported = [];
for (let i = 0; i < images.length; i += 1) {
  const item = images[i];
  if (!item || !item.name || !item.base64) continue;

  const bytes = base64ToBytes(item.base64);
  const hash = figma.createImage(bytes).hash;
  let node = page.findOne(candidate => candidate.name === item.name);
  if (!node || !("fills" in node) || !("resize" in node)) {
    node = figma.createFrame();
    page.appendChild(node);
  }

  node.name = item.name;
  node.x = item.x ?? (-520 + (i % 4) * 128);
  node.y = item.y ?? (1760 + Math.floor(i / 4) * 112);
  node.resize(item.width || 96, item.height || 96);
  node.fills = [{ type: "IMAGE", imageHash: hash, scaleMode: "FILL" }];
  if ("clipsContent" in node) node.clipsContent = true;
  imported.push({ name: item.name, id: node.id, hash });
}

return {
  version: VERSION,
  imported,
  importedCount: imported.length
};

function base64ToBytes(base64) {
  const binary = atob(base64);
  const bytes = new Uint8Array(binary.length);
  for (let i = 0; i < binary.length; i += 1) {
    bytes[i] = binary.charCodeAt(i);
  }
  return bytes;
}
