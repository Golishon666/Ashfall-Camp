using AshfallCamp.Composition;
using AshfallCamp.Presentation;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Tilemaps;

namespace AshfallCamp.Editor
{
    public static class WorldTileInteractionSetup
    {
        private const string MapPrefabPath = "Assets/AshfallCamp/Scenes/WorldTileMap.prefab";
        private const string TooltipPrefabPath = "Assets/FIGUNITY/Prefabs/PF_11WorldTileTooltipElementwise.prefab";
        private const string ScenePath = "Assets/AshfallCamp/Scenes/SC_Boot.unity";

        [MenuItem("Tools/Ashfall Camp/World Map/Setup Tile Interaction")]
        public static void Setup()
        {
            var root = PrefabUtility.LoadPrefabContents(MapPrefabPath);
            try
            {
                var view = root.GetComponent<CampWorldTileView>() ?? root.AddComponent<CampWorldTileView>();
                var content = FindTilemap(root.transform, "Tilemap");
                var markers = FindTilemap(root.transform, "LocationMarkerOverlay");
                var tooltip = AssetDatabase.LoadAssetAtPath<GameObject>(TooltipPrefabPath);
                var serialized = new SerializedObject(view);
                serialized.FindProperty("contentTilemap").objectReferenceValue = content;
                serialized.FindProperty("markerTilemap").objectReferenceValue = markers;
                serialized.FindProperty("worldCamera").objectReferenceValue = null;
                serialized.FindProperty("tooltipPrefab").objectReferenceValue = tooltip;
                serialized.FindProperty("uiSortingOrder").intValue = 110;
                serialized.ApplyModifiedPropertiesWithoutUndo();
                if (markers != null)
                {
                    markers.gameObject.SetActive(true);
                    var renderer = markers.GetComponent<TilemapRenderer>();
                    if (renderer != null) renderer.sortingOrder = 12;
                    var collider = markers.GetComponent<TilemapCollider2D>();
                    if (collider != null) Object.DestroyImmediate(collider);
                }
                PrefabUtility.SaveAsPrefabAsset(root, MapPrefabPath);
            }
            finally
            {
                PrefabUtility.UnloadPrefabContents(root);
            }

            var scene = EditorSceneManager.OpenScene(ScenePath, OpenSceneMode.Single);
            var scope = Object.FindFirstObjectByType<ProjectLifetimeScope>(FindObjectsInactive.Include);
            var sceneView = Object.FindFirstObjectByType<CampWorldTileView>(FindObjectsInactive.Include);
            if (scope == null || sceneView == null) throw new MissingReferenceException("SC_Boot requires ProjectLifetimeScope and CampWorldTileView.");
            scope.SetWorldTileView(sceneView);
            EditorUtility.SetDirty(scope);
            EditorSceneManager.MarkSceneDirty(scene);
            EditorSceneManager.SaveScene(scene);
            AssetDatabase.SaveAssets();
            Debug.Log("World tile interaction configured with Figma tooltip and expedition marker overlay.");
        }

        private static Tilemap FindTilemap(Transform root, string name)
        {
            foreach (var tilemap in root.GetComponentsInChildren<Tilemap>(true))
            {
                if (tilemap.name == name) return tilemap;
            }
            return null;
        }
    }
}
