using System;
using System.Threading;
using AshfallCamp.Infrastructure;
using UnityEditor;
using UnityEngine;

namespace AshfallCamp.Editor
{
    public static class ConfigValidationMenu
    {
        private const string DatabasePath = "Assets/AshfallCamp/Configs/GameConfigDatabase.asset";

        [MenuItem("Tools/Ashfall Camp/Validate Configs")]
        public static void ValidateConfigs()
        {
            var database = AssetDatabase.LoadAssetAtPath<GameConfigDatabaseSO>(DatabasePath);
            if (database == null)
            {
                Debug.LogError("Ashfall Camp config validation failed: GameConfigDatabase asset is missing.");
                return;
            }

            try
            {
                var provider = new ScriptableObjectGameConfigProvider(database);
                var task = provider.GetType().GetMethod("LoadAsync").Invoke(provider, new object[] { CancellationToken.None });
                var awaiter = task.GetType().GetMethod("GetAwaiter").Invoke(task, null);
                awaiter.GetType().GetMethod("GetResult").Invoke(awaiter, null);
                Debug.Log("Ashfall Camp config validation passed.");
            }
            catch (Exception ex)
            {
                var cause = ex.InnerException ?? ex;
                Debug.LogError("Ashfall Camp config validation failed: " + cause.Message);
            }
        }
    }
}
