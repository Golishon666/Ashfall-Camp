return {
  fileKey: figma.fileKey,
  editorType: figma.editorType,
  page: figma.currentPage.name,
  topLevelNodes: figma.currentPage.children.map(node => ({
    id: node.id,
    name: node.name,
    type: node.type
  }))
};
