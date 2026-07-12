using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEditor.Tilemaps;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AshfallCamp.Editor
{
    [CustomGridBrush(false, false, false, "Ashfall Production V2 Brush")]
    public sealed class CampMapProductionV2Brush : GridBrush
    {
        [SerializeField] private TileBase[] tiles = Array.Empty<TileBase>();
        [SerializeField] private string[] tileLabels = Array.Empty<string>();
        [SerializeField] private int selectedIndex;

        public IReadOnlyList<TileBase> Tiles => tiles;

        public IReadOnlyList<string> TileLabels => tileLabels;

        public int SelectedIndex => selectedIndex;

        public TileBase SelectedTile => tiles != null && tiles.Length > 0
            ? tiles[Mathf.Clamp(selectedIndex, 0, tiles.Length - 1)]
            : null;

        public void Configure(IReadOnlyList<ProductionTileSpec> specs)
        {
            tiles = new TileBase[specs.Count];
            tileLabels = new string[specs.Count];
            for (var index = 0; index < specs.Count; index++)
            {
                var spec = specs[index];
                tiles[index] = AssetDatabase.LoadAssetAtPath<TileBase>(spec.TileAssetPath);
                tileLabels[index] = spec.Category + " / " + spec.Id;
            }

            selectedIndex = Mathf.Clamp(selectedIndex, 0, Mathf.Max(0, tiles.Length - 1));
            SyncSelectedTile();
            EditorUtility.SetDirty(this);
        }

        public void SelectTile(int index)
        {
            selectedIndex = Mathf.Clamp(index, 0, Mathf.Max(0, tiles.Length - 1));
            SyncSelectedTile();
            EditorUtility.SetDirty(this);
        }

        private void OnEnable()
        {
            SyncSelectedTile();
        }

        private void SyncSelectedTile()
        {
            Init(Vector3Int.one, Vector3Int.zero);
            SetTile(Vector3Int.zero, SelectedTile);
        }
    }

    [CustomEditor(typeof(CampMapProductionV2Brush))]
    public sealed class CampMapProductionV2BrushEditor : GridBrushEditor
    {
        public override void OnPaintInspectorGUI()
        {
            var productionBrush = target as CampMapProductionV2Brush;
            if (productionBrush == null)
            {
                return;
            }

            EditorGUILayout.LabelField("Ashfall Camp Production V2", EditorStyles.boldLabel);
            EditorGUILayout.HelpBox(
                $"Select one of the {productionBrush.Tiles.Count} production tiles. The brush paints a single cell and never modifies gameplay state.",
                MessageType.Info);

            var labels = new string[productionBrush.TileLabels.Count];
            for (var index = 0; index < labels.Length; index++)
            {
                labels[index] = productionBrush.TileLabels[index];
            }

            EditorGUI.BeginChangeCheck();
            var selectedIndex = labels.Length > 0
                ? EditorGUILayout.Popup("Tile", productionBrush.SelectedIndex, labels)
                : 0;
            if (EditorGUI.EndChangeCheck())
            {
                Undo.RecordObject(productionBrush, "Select Ashfall Camp Tile");
                productionBrush.SelectTile(selectedIndex);
            }

            using (new EditorGUI.DisabledScope(true))
            {
                EditorGUILayout.ObjectField("Selected Asset", productionBrush.SelectedTile, typeof(TileBase), false);
            }

            if (GUILayout.Button("Open Production V2 Palette"))
            {
                GridPaintingState.palette = AssetDatabase.LoadAssetAtPath<GameObject>(
                    CampMapProductionV2Manifest.PalettePath);
            }
        }
    }
}
