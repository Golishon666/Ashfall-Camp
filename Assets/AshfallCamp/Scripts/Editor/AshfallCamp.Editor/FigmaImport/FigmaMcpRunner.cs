using System;
using System.Diagnostics;
using System.IO;
using System.Text;
using UnityEditor;
using UnityEngine;

namespace AshfallCamp.Editor.FigmaImport
{
    public static class FigmaMcpRunner
    {
        private const int TimeoutMilliseconds = 420000;

        public static void ExportSurvivorsPanel()
        {
            ExportAllPanels();
        }

        public static void ExportAllPanels()
        {
            var workspace = Path.GetFullPath(Path.Combine(UnityEngine.Application.dataPath, ".."));
            var scriptPath = Path.Combine(workspace, "Tools", "AshfallFigmaImporter", "export-figma-node.mjs");
            if (!File.Exists(scriptPath))
            {
                throw new FileNotFoundException("Ashfall Figma importer worker is missing.", scriptPath);
            }

            var nodePath = ResolveNodePath();
            var startInfo = new ProcessStartInfo
            {
                FileName = nodePath,
                Arguments = Quote(scriptPath),
                WorkingDirectory = workspace,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true,
                StandardOutputEncoding = Encoding.UTF8,
                StandardErrorEncoding = Encoding.UTF8
            };

            startInfo.EnvironmentVariables["FIGMA_WS_PORT"] = "9225";
            startInfo.EnvironmentVariables["ASHFALL_FIGMA_FILE"] = "Ashfall Camp UI Concept";

            using (var process = new Process { StartInfo = startInfo })
            {
                var stdout = new StringBuilder();
                var stderr = new StringBuilder();
                process.OutputDataReceived += (_, args) =>
                {
                    if (args.Data != null) stdout.AppendLine(args.Data);
                };
                process.ErrorDataReceived += (_, args) =>
                {
                    if (args.Data != null) stderr.AppendLine(args.Data);
                };

                process.Start();
                process.BeginOutputReadLine();
                process.BeginErrorReadLine();

                if (!process.WaitForExit(TimeoutMilliseconds))
                {
                    try
                    {
                        process.Kill();
                    }
                    catch
                    {
                        // Process may already have exited between timeout and Kill.
                    }

                    throw new TimeoutException("Timed out while exporting Figma UI payload.");
                }

                if (process.ExitCode != 0)
                {
                    throw new InvalidOperationException(
                        "Figma UI export failed.\nSTDOUT:\n" + stdout + "\nSTDERR:\n" + stderr);
                }

                UnityEngine.Debug.Log("Ashfall Figma export completed:\n" + stdout);
            }

            AssetDatabase.Refresh();
        }

        private static string ResolveNodePath()
        {
            var programFiles = Environment.GetFolderPath(Environment.SpecialFolder.ProgramFiles);
            var nodePath = Path.Combine(programFiles, "nodejs", "node.exe");
            if (File.Exists(nodePath)) return nodePath;

            return "node";
        }

        private static string Quote(string value)
        {
            return "\"" + value.Replace("\"", "\\\"") + "\"";
        }
    }
}
