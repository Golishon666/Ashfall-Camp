const frameNames = [
  "Ashfall Camp - Full HD / 01 Camp Dashboard",
  "Ashfall Camp - Full HD / 02 Survivors",
  "Ashfall Camp - Full HD / 03 Expeditions",
  "Ashfall Camp - Full HD / 04 Buildings",
  "Ashfall Camp - Full HD / 05 Workshop",
  "Ashfall Camp - Full HD / 06 Radio",
  "Ashfall Camp - Full HD / 07 Expedition Monitor"
];

const frames = frameNames.map(name => {
  const node = figma.currentPage.findOne(candidate => candidate.type === "FRAME" && candidate.name === name);
  if (!node) throw new Error(`Missing frame: ${name}`);
  return node;
});

return frames.map(frame => ({
  frame: frame.name,
  outOfBounds: collectOutOfBounds(frame),
  blockOverlaps: collectSiblingOverlaps(frame, isLayoutBlock),
  textOverlaps: collectSiblingOverlaps(frame, isTextNode)
}));

function collectOutOfBounds(root) {
  const issues = [];
  walk(root, node => {
    if (node === root || !isVisibleNode(node) || !hasBox(node)) return;
    const box = absoluteBox(node, root);
    if (box.x < -0.5 || box.y < -0.5 || box.x + box.width > root.width + 0.5 || box.y + box.height > root.height + 0.5) {
      issues.push(summary(node, box));
    }
  });
  return issues;
}

function collectSiblingOverlaps(root, predicate) {
  const issues = [];
  walk(root, parent => {
    if (!("children" in parent)) return;
    const candidates = parent.children
      .filter(node => node !== root && isVisibleNode(node) && hasBox(node) && predicate(node))
      .map(node => ({ node, box: absoluteBox(node, root) }))
      .filter(item => item.box.width >= 8 && item.box.height >= 8);

    for (let i = 0; i < candidates.length; i += 1) {
      for (let j = i + 1; j < candidates.length; j += 1) {
        const a = candidates[i];
        const b = candidates[j];
        const area = overlapArea(a.box, b.box);
        if (area <= 1) continue;
        const smaller = Math.min(a.box.width * a.box.height, b.box.width * b.box.height);
        const ratio = area / Math.max(1, smaller);
        if (ratio < 0.08 && area < 80) continue;
        issues.push({
          parent: parent.name,
          a: summary(a.node, a.box),
          b: summary(b.node, b.box),
          overlapArea: Math.round(area),
          overlapRatio: Math.round(ratio * 100) / 100
        });
      }
    }
  });
  return issues;
}

function isLayoutBlock(node) {
  if (!["FRAME", "COMPONENT", "INSTANCE", "GROUP"].includes(node.type)) return false;
  const name = String(node.name || "").toLowerCase();
  if (name.includes("root") || name.includes("asset-driven") || name.includes("structure")) return false;
  if (name.includes("top resource bar") || name.includes("bottom nav")) return true;
  if (name.includes("panel") || name.includes("bar") || name.includes("card") || name.includes("slot") || name.includes("queue") || name.includes("recipe") || name.includes("button") || name.includes("row")) return true;
  return (node.width || 0) >= 80 && (node.height || 0) >= 40;
}

function isTextNode(node) {
  if (node.type !== "TEXT") return false;
  const name = String(node.name || "").toLowerCase();
  if (name.length === 1 || name === "pt" || name === "ct" || name === "et") return false;
  return (node.width || 0) >= 10 && (node.height || 0) >= 8;
}

function isVisibleNode(node) {
  return node.visible !== false && node.opacity !== 0;
}

function hasBox(node) {
  return "x" in node && "y" in node && "width" in node && "height" in node;
}

function overlapArea(a, b) {
  const x = Math.max(0, Math.min(a.x + a.width, b.x + b.width) - Math.max(a.x, b.x));
  const y = Math.max(0, Math.min(a.y + a.height, b.y + b.height) - Math.max(a.y, b.y));
  return x * y;
}

function absoluteBox(node, ancestor) {
  let x = node.x || 0;
  let y = node.y || 0;
  let parent = node.parent;
  while (parent && parent !== ancestor && "x" in parent && "y" in parent) {
    x += parent.x || 0;
    y += parent.y || 0;
    parent = parent.parent;
  }
  return {
    x: Math.round(x * 10) / 10,
    y: Math.round(y * 10) / 10,
    width: Math.round((node.width || 0) * 10) / 10,
    height: Math.round((node.height || 0) * 10) / 10
  };
}

function summary(node, box) {
  return {
    name: node.name,
    type: node.type,
    box
  };
}

function walk(node, visit) {
  visit(node);
  if ("children" in node) {
    for (const child of node.children) walk(child, visit);
  }
}
