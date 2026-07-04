using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Composition;
using AshfallCamp.Domain;
using Cysharp.Threading.Tasks;
using NUnit.Framework;

namespace AshfallCamp.Tests.EditMode
{
    public sealed class BootRuntimeTests
    {
        [Test]
        public void BootstrapperAppliesOfflineProgressForLoadedSaveAndPersistsIt()
        {
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult
            {
                State = new GameState(),
                CreatedNew = false
            });
            var offline = new FakeOfflineProgressUseCase(new OfflineProgressReport { AppliedSeconds = 60 });
            var bootstrapper = new GameBootstrapper(saveLoad, offline, new FixedUnixTimeProvider(123456));

            bootstrapper.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, saveLoad.LoadCalls);
            Assert.AreEqual(1, offline.ExecuteCalls);
            Assert.AreEqual(123456, offline.LastNowUnixMs);
            Assert.AreEqual(1, saveLoad.SaveCalls);
        }

        [Test]
        public void BootstrapperSkipsOfflineProgressForNewSave()
        {
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult
            {
                State = new GameState(),
                CreatedNew = true
            });
            var offline = new FakeOfflineProgressUseCase(new OfflineProgressReport { AppliedSeconds = 60 });
            var bootstrapper = new GameBootstrapper(saveLoad, offline, new FixedUnixTimeProvider(123456));

            bootstrapper.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, saveLoad.LoadCalls);
            Assert.AreEqual(0, offline.ExecuteCalls);
            Assert.AreEqual(0, saveLoad.SaveCalls);
        }

        [Test]
        public void BootstrapperDoesNotPersistWhenNoOfflineSecondsApplied()
        {
            var saveLoad = new FakeSaveLoadUseCase(new LoadGameResult
            {
                State = new GameState(),
                CreatedNew = false
            });
            var offline = new FakeOfflineProgressUseCase(new OfflineProgressReport());
            var bootstrapper = new GameBootstrapper(saveLoad, offline, new FixedUnixTimeProvider(123456));

            bootstrapper.StartAsync(CancellationToken.None).GetAwaiter().GetResult();

            Assert.AreEqual(1, offline.ExecuteCalls);
            Assert.AreEqual(0, saveLoad.SaveCalls);
        }

        private sealed class FixedUnixTimeProvider : IUnixTimeProvider
        {
            public FixedUnixTimeProvider(long nowUnixMs)
            {
                NowUnixMs = nowUnixMs;
            }

            public long NowUnixMs { get; private set; }
        }

        private sealed class FakeSaveLoadUseCase : ISaveLoadUseCase
        {
            private readonly LoadGameResult _loadResult;

            public FakeSaveLoadUseCase(LoadGameResult loadResult)
            {
                _loadResult = loadResult;
            }

            public int LoadCalls { get; private set; }
            public int SaveCalls { get; private set; }

            public UniTask<LoadGameResult> LoadOrCreateAsync(CancellationToken ct)
            {
                LoadCalls++;
                return UniTask.FromResult(_loadResult);
            }

            public UniTask SaveAsync(CancellationToken ct)
            {
                SaveCalls++;
                return UniTask.CompletedTask;
            }
        }

        private sealed class FakeOfflineProgressUseCase : IOfflineProgressUseCase
        {
            private readonly OfflineProgressReport _report;

            public FakeOfflineProgressUseCase(OfflineProgressReport report)
            {
                _report = report;
            }

            public int ExecuteCalls { get; private set; }
            public long LastNowUnixMs { get; private set; }

            public UniTask<OfflineProgressReport> ExecuteAsync(long nowUnixMs, CancellationToken ct)
            {
                ExecuteCalls++;
                LastNowUnixMs = nowUnixMs;
                return UniTask.FromResult(_report);
            }
        }
    }
}
