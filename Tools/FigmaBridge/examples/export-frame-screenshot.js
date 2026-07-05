const frameName = payload && payload.frameName;
const scale = payload && payload.scale ? Number(payload.scale) : 0.25;

if (!frameName) {
  throw new Error("payload.frameName is required.");
}

const frame = figma.currentPage.findOne(node => node.type === "FRAME" && node.name === frameName);
if (!frame) {
  throw new Error(`Frame not found: ${frameName}`);
}

return await helpers.screenshot(frame, scale);
