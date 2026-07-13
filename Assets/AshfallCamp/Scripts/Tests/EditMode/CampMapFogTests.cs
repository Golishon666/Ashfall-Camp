using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using AshfallCamp.Presentation;
using NUnit.Framework;
using TMPro;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.Tilemaps;
using UnityEngine.UI;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class CampMapFogTests
    {
        [Test]
        public void InitializationRevealsThreeByThreeAndClassifiesFrontierAndDeepFog()
        {
            var state = CreateState(500);
            var topology = CreateTopology(4);
            var validation = MapFogSystem.Initialize(state, CreateBalance(), topology);

            Assert.True(validation.IsValid, string.Join(" ", validation.Errors));
            Assert.True(state.MapFogInitialized);
            Assert.AreEqual(9, state.RevealedMapCells.Count);
            Assert.True(MapFogSystem.IsRevealed(state, new MapCellCoordinate(1, 1)));
            Assert.AreEqual(MapFogVisibility.Frontier, MapFogSystem.GetVisibility(state, topology, new MapCellCoordinate(2, 2)));
            Assert.AreEqual(MapFogVisibility.Deep, MapFogSystem.GetVisibility(state, topology, new MapCellCoordinate(4, 4)));
        }

        [Test]
        public void RevealUsesChebyshevDistanceAndSpendsScrapAtomically()
        {
            var state = CreateState(250);
            var balance = CreateBalance();
            var topology = CreateTopology(4);
            MapFogSystem.Initialize(state, balance, topology);

            var distanceTwo = MapFogSystem.TryReveal(state, balance, topology, new MapCellCoordinate(2, 2));
            Assert.True(distanceTwo.Validation.IsValid, string.Join(" ", distanceTwo.Validation.Errors));
            Assert.AreEqual(2, distanceTwo.Distance);
            Assert.AreEqual(100, distanceTwo.Cost);
            Assert.AreEqual(150, state.Resources["scrap"]);
            Assert.AreEqual(MapFogVisibility.Frontier, MapFogSystem.GetVisibility(state, topology, new MapCellCoordinate(3, 3)));
            Assert.AreEqual(150, MapFogSystem.CalculateCost(balance, 3));
        }

        [Test]
        public void DeepFogAndInsufficientScrapCannotBeRevealed()
        {
            var state = CreateState(99);
            var balance = CreateBalance();
            var topology = CreateTopology(4);
            MapFogSystem.Initialize(state, balance, topology);

            var deep = MapFogSystem.TryReveal(state, balance, topology, new MapCellCoordinate(4, 4));
            Assert.False(deep.Validation.IsValid);
            Assert.AreEqual(99, state.Resources["scrap"]);
            var unaffordable = MapFogSystem.TryReveal(state, balance, topology, new MapCellCoordinate(2, 0));
            Assert.False(unaffordable.Validation.IsValid);
            Assert.AreEqual(99, state.Resources["scrap"]);
            Assert.False(MapFogSystem.IsRevealed(state, new MapCellCoordinate(2, 0)));
        }

        [Test]
        public void RadioactiveSeaRevealsForFreeBesideRevealedLandButCannotBePurchased()
        {
            var state = CreateState(200);
            var balance = CreateBalance();
            var topology = CreateTopology(4);
            topology.RadioactiveSeaCells.Add(new MapCellCoordinate(3, 0));
            topology.RevealableCells.RemoveAll(cell => cell.Equals(new MapCellCoordinate(3, 0)));
            MapFogSystem.Initialize(state, balance, topology);

            Assert.AreEqual(MapFogVisibility.Deep, MapFogSystem.GetVisibility(state, topology, new MapCellCoordinate(3, 0)));
            var land = MapFogSystem.TryReveal(state, balance, topology, new MapCellCoordinate(2, 0));
            Assert.True(land.Validation.IsValid);
            Assert.AreEqual(MapFogVisibility.Revealed, MapFogSystem.GetVisibility(state, topology, new MapCellCoordinate(3, 0)));
            var remaining = state.Resources["scrap"];
            var sea = MapFogSystem.TryReveal(state, balance, topology, new MapCellCoordinate(3, 0));
            Assert.False(sea.Validation.IsValid);
            Assert.AreEqual(remaining, state.Resources["scrap"]);
        }

        [Test]
        public void SaveRoundTripPreservesFogProgressAndLegacySaveCanInitialize()
        {
            var folder = Path.Combine(Path.GetTempPath(), "AshfallCampMapFogTests", System.Guid.NewGuid().ToString("N"));
            try
            {
                var repository = new JsonSaveRepository(folder);
                var state = CreateState(150);
                state.MapFogInitialized = true;
                state.RevealedMapCells.Add(new MapCellCoordinate(-2, 7));
                repository.SaveAsync(state, CancellationToken.None).GetAwaiter().GetResult();
                var loaded = repository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult().State;
                Assert.True(loaded.MapFogInitialized);
                CollectionAssert.Contains(loaded.RevealedMapCells, new MapCellCoordinate(-2, 7));

                File.WriteAllText(Path.Combine(folder, "save.json"), "{\"Version\":\"0.1.0\",\"Resources\":[{\"Id\":\"scrap\",\"Value\":150}]}");
                var legacy = repository.LoadAsync(CancellationToken.None).GetAwaiter().GetResult().State;
                Assert.False(legacy.MapFogInitialized);
                Assert.NotNull(legacy.RevealedMapCells);
                Assert.True(MapFogSystem.Initialize(legacy, CreateBalance(), CreateTopology(2)).IsValid);
                Assert.True(legacy.MapFogInitialized);
            }
            finally
            {
                if (Directory.Exists(folder)) Directory.Delete(folder, true);
            }
        }

        [Test]
        public void BootSceneFogViewUsesManualTerrainAndColliderFreeSortedOverlays()
        {
            var previousPath = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene("Assets/AshfallCamp/Scenes/SC_Boot.unity", OpenSceneMode.Single);
            try
            {
                var worldRoot = scene.GetRootGameObjects().Single(item => item.name == "WorldTileMap");
                var view = worldRoot.GetComponent<CampMapFogView>();
                Assert.NotNull(view);
                string error;
                Assert.True(view.TryBuildTopology(out error), error);
                Assert.NotNull(view.Topology);
                Assert.Greater(view.Topology.RevealableCells.Count, 9);

                AssertOverlay(view.FrontierFogTilemap, 10);
                AssertOverlay(view.DeepFogTilemap, 11);
                var serialized = new SerializedObject(view);
                Assert.NotNull(serialized.FindProperty("popupRoot").objectReferenceValue);
                Assert.IsInstanceOf<Button>(serialized.FindProperty("openButton").objectReferenceValue);
                Assert.NotNull(serialized.FindProperty("campCoreTile").objectReferenceValue);
                Assert.NotNull(serialized.FindProperty("radioactiveSeaTile").objectReferenceValue);
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousPath) && previousPath != scene.path)
                {
                    EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
                }
            }
        }

        [Test]
        public void FrontierPopupShowsPriceAndDisablesOpeningWithoutScrap()
        {
            var previousPath = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene("Assets/AshfallCamp/Scenes/SC_Boot.unity", OpenSceneMode.Single);
            try
            {
                var view = scene.GetRootGameObjects().Single(item => item.name == "WorldTileMap").GetComponent<CampMapFogView>();
                string error;
                Assert.True(view.TryBuildTopology(out error), error);
                var state = CreateState(500);
                var config = new GameConfigSnapshot { Balance = CreateBalance() };
                MapFogSystem.Initialize(state, config.Balance, view.Topology);
                view.Render(state, config);
                var frontier = view.Topology.RevealableCells.First(cell => MapFogSystem.GetVisibility(state, view.Topology, cell) == MapFogVisibility.Frontier);
                ShowPopup(view, frontier);

                var serialized = new SerializedObject(view);
                var button = (Button)serialized.FindProperty("openButton").objectReferenceValue;
                var action = (TextMeshProUGUI)serialized.FindProperty("actionLabel").objectReferenceValue;
                var price = (TextMeshProUGUI)serialized.FindProperty("priceLabel").objectReferenceValue;
                Assert.True(view.IsPopupVisible);
                var popupCanvasField = typeof(CampMapFogView).GetField("_popupCanvas", BindingFlags.Instance | BindingFlags.NonPublic);
                Assert.NotNull(popupCanvasField);
                var popupCanvas = (Canvas)popupCanvasField.GetValue(view);
                Assert.NotNull(popupCanvas);
                Assert.AreEqual(100, popupCanvas.sortingOrder);
                Assert.NotNull(popupCanvas.GetComponent<GraphicRaycaster>());
                Assert.True(button.interactable);
                Assert.AreEqual("OPEN", action.text);
                Assert.AreEqual("100 SCRAP", price.text);

                state.Resources["scrap"] = 0;
                view.Render(state, config);
                ShowPopup(view, frontier);
                Assert.False(button.interactable);
                Assert.AreEqual("NOT ENOUGH SCRAP", action.text);
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousPath) && previousPath != scene.path)
                {
                    EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
                }
            }
        }

        [Test]
        public void MapClickRoutesRevealedCellsAndKeepsFrontierForUnlockPopup()
        {
            var previousPath = EditorSceneManager.GetActiveScene().path;
            var scene = EditorSceneManager.OpenScene("Assets/AshfallCamp/Scenes/SC_Boot.unity", OpenSceneMode.Single);
            try
            {
                var view = scene.GetRootGameObjects().Single(item => item.name == "WorldTileMap").GetComponent<CampMapFogView>();
                string error;
                Assert.True(view.TryBuildTopology(out error), error);
                var state = CreateState(500);
                var config = new GameConfigSnapshot { Balance = CreateBalance() };
                MapFogSystem.Initialize(state, config.Balance, view.Topology);
                var revealed = new MapCellCoordinate(0, -2);
                CollectionAssert.Contains(view.Topology.RevealableCells, revealed);
                state.RevealedMapCells.Add(revealed);
                view.Render(state, config);

                var clicked = new MapCellCoordinate(int.MinValue, int.MinValue);
                var frontierClicked = new MapCellCoordinate(int.MinValue, int.MinValue);
                view.RevealedCellClicked += cell => clicked = cell;
                view.FrontierCellClicked += cell => frontierClicked = cell;
                view.HandleMapClick(revealed);
                Assert.AreEqual(revealed, clicked);
                Assert.False(view.IsPopupVisible);

                var frontier = view.Topology.RevealableCells.First(cell => MapFogSystem.GetVisibility(state, view.Topology, cell) == MapFogVisibility.Frontier);
                view.HandleMapClick(frontier);
                Assert.True(view.IsPopupVisible);
                Assert.AreEqual(frontier, view.DisplayedCell);
                Assert.AreEqual(frontier, frontierClicked);
                Assert.AreEqual(revealed, clicked, "Frontier clicks must not be routed to the world tile tooltip.");
            }
            finally
            {
                if (!string.IsNullOrEmpty(previousPath) && previousPath != scene.path)
                {
                    EditorSceneManager.OpenScene(previousPath, OpenSceneMode.Single);
                }
            }
        }

        private static void AssertOverlay(Tilemap tilemap, int sortingOrder)
        {
            Assert.NotNull(tilemap);
            Assert.AreEqual(new Vector3(0.5f, 0.5f, 0f), tilemap.tileAnchor);
            Assert.AreEqual(sortingOrder, tilemap.GetComponent<TilemapRenderer>().sortingOrder);
            Assert.AreEqual("AshfallCamp/FogOfWarAnimated", tilemap.GetComponent<TilemapRenderer>().sharedMaterial.shader.name);
            Assert.IsNull(tilemap.GetComponent<TilemapCollider2D>());
        }

        private static void ShowPopup(CampMapFogView view, MapCellCoordinate cell)
        {
            var method = typeof(CampMapFogView).GetMethod("ShowPopup", BindingFlags.Instance | BindingFlags.NonPublic);
            Assert.NotNull(method);
            method.Invoke(view, new object[] { cell });
        }

        private static GameState CreateState(int scrap)
        {
            var state = new GameState();
            state.Resources["scrap"] = scrap;
            return state;
        }

        private static BalanceDefinition CreateBalance()
        {
            return new BalanceDefinition
            {
                MapRevealResourceId = "scrap",
                MapRevealBaseCost = 50,
                MapInitialRevealRadius = 1
            };
        }

        private static MapFogTopology CreateTopology(int radius)
        {
            var topology = new MapFogTopology { Core = new MapCellCoordinate(0, 0) };
            for (var y = -radius; y <= radius; y++)
            {
                for (var x = -radius; x <= radius; x++)
                {
                    topology.RevealableCells.Add(new MapCellCoordinate(x, y));
                }
            }
            return topology;
        }
    }
}
