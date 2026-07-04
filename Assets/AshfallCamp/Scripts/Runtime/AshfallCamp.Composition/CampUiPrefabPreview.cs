using System;
using System.Threading;
using AshfallCamp.Domain;
using AshfallCamp.Infrastructure;
using AshfallCamp.Presentation;
using UnityEngine;

namespace AshfallCamp.Composition
{
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(CampDashboardView))]
    public sealed class CampUiPrefabPreview : MonoBehaviour
    {
        [SerializeField] private GameConfigDatabaseSO configDatabase;
        [SerializeField] private CampDashboardView dashboardView;
        [SerializeField] private bool renderInEditMode = true;

        public void SetReferences(GameConfigDatabaseSO database, CampDashboardView view)
        {
            configDatabase = database;
            dashboardView = view;
            RenderPreview();
        }

        private void Reset()
        {
            dashboardView = GetComponent<CampDashboardView>();
        }

        private void OnEnable()
        {
            QueueRenderPreview();
        }

        private void OnValidate()
        {
            QueueRenderPreview();
        }

        private void QueueRenderPreview()
        {
            if (UnityEngine.Application.isPlaying) return;
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall -= RenderPreview;
            UnityEditor.EditorApplication.delayCall += RenderPreview;
#else
            RenderPreview();
#endif
        }

        public void RenderPreview()
        {
#if UNITY_EDITOR
            if (this == null) return;
#endif
            if (UnityEngine.Application.isPlaying || !renderInEditMode || configDatabase == null) return;
            if (dashboardView == null) dashboardView = GetComponent<CampDashboardView>();
            if (dashboardView == null) return;

            try
            {
                var provider = new ScriptableObjectGameConfigProvider(configDatabase);
                var config = provider.LoadAsync(CancellationToken.None).GetAwaiter().GetResult();
                var state = GameStateFactory.CreateNew(config, DateTimeOffset.UtcNow.ToUnixTimeMilliseconds());
                dashboardView.Render(state, config);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
        }
    }
}
