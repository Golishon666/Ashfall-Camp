using System;
using System.Threading;
using AshfallCamp.Application;
using Cysharp.Threading.Tasks;
using UnityEngine;

namespace AshfallCamp.Composition
{
    [DisallowMultipleComponent]
    public sealed class ApplicationLifecycleSaveAdapter : MonoBehaviour
    {
        private ISaveLoadUseCase _saveLoad;
        private bool _isSaving;

        public void Construct(ISaveLoadUseCase saveLoad)
        {
            _saveLoad = saveLoad;
        }

        public void NotifyApplicationPaused(bool paused)
        {
            if (paused)
            {
                SaveForLifecycleAsync(CancellationToken.None).Forget();
            }
        }

        public void NotifyApplicationQuitting()
        {
            SaveForLifecycleAsync(CancellationToken.None).Forget();
        }

        public async UniTask SaveForLifecycleAsync(CancellationToken ct)
        {
            if (_saveLoad == null || _isSaving) return;

            _isSaving = true;
            try
            {
                await _saveLoad.SaveAsync(ct);
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
            {
                Debug.LogException(ex, this);
            }
            finally
            {
                _isSaving = false;
            }
        }

        private void OnApplicationPause(bool paused)
        {
            NotifyApplicationPaused(paused);
        }

        private void OnApplicationQuit()
        {
            NotifyApplicationQuitting();
        }
    }
}
