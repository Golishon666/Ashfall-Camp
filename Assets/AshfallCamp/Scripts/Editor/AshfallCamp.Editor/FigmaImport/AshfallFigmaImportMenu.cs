using UnityEditor;
using UnityEngine;

namespace AshfallCamp.Editor.FigmaImport
{
    public static class AshfallFigmaImportMenu
    {
        [MenuItem("Tools/Ashfall Camp/Figma Import/Import All Panels")]
        public static void ImportAllPanels()
        {
            FigmaMcpRunner.ExportAllPanels();
            RebuildAllPanelsFromLastPayload();
        }

        [MenuItem("Tools/Ashfall Camp/Figma Import/Import Survivors Panel")]
        public static void ImportSurvivorsPanel()
        {
            FigmaMcpRunner.ExportSurvivorsPanel();
            RebuildSurvivorsFromLastPayload();
        }

        [MenuItem("Tools/Ashfall Camp/Figma Import/Rebuild All Panels From Last Payload")]
        public static void RebuildAllPanelsFromLastPayload()
        {
            var document = SurvivorsFigmaProductionUpdater.LoadPayload();
            SurvivorsFigmaProductionUpdater.RebuildFromPayload(document);
            Debug.Log("Rebuilt Ashfall UI panels from Figma payload: " + SurvivorsFigmaProductionUpdater.PayloadPath);
        }

        [MenuItem("Tools/Ashfall Camp/Figma Import/Rebuild Survivors From Last Payload")]
        public static void RebuildSurvivorsFromLastPayload()
        {
            var document = SurvivorsFigmaProductionUpdater.LoadPayload();
            SurvivorsFigmaProductionUpdater.RebuildFromPayload(document);
            Debug.Log("Rebuilt Ashfall UI panels from Figma payload: " + SurvivorsFigmaProductionUpdater.PayloadPath);
        }
    }
}
