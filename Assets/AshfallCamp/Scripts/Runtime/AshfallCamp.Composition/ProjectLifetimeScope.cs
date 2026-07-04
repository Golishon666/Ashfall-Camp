using AshfallCamp.Application;
using AshfallCamp.Infrastructure;
using AshfallCamp.Presentation;
using UnityEngine;
using VContainer;
using VContainer.Unity;

namespace AshfallCamp.Composition
{
    public sealed class ProjectLifetimeScope : LifetimeScope
    {
        [SerializeField] private GameConfigDatabaseSO configDatabase;
        [SerializeField] private CampDashboardView campDashboardView;

        public void SetDashboardReferences(GameConfigDatabaseSO database, CampDashboardView dashboardView)
        {
            configDatabase = database;
            campDashboardView = dashboardView;
        }

        protected override void Configure(IContainerBuilder builder)
        {
            builder.RegisterInstance(configDatabase).AsSelf();
            if (campDashboardView != null)
            {
                builder.RegisterComponent(campDashboardView).As<IUiRootView>();
            }

            builder.Register<ScriptableObjectGameConfigProvider>(Lifetime.Singleton).As<IGameConfigProvider>();
            builder.Register<GameStateStore>(Lifetime.Singleton)
                .As<IGameStateReader>()
                .As<IGameStateWriter>()
                .AsSelf();
            builder.Register<ISaveRepository>(_ => new JsonSaveRepository(), Lifetime.Singleton);
            builder.Register<SystemUnixTimeProvider>(Lifetime.Singleton).As<IUnixTimeProvider>();
            builder.Register<LaunchExpeditionUseCase>(Lifetime.Singleton).As<ILaunchExpeditionUseCase>();
            builder.Register<UpgradeBuildingUseCase>(Lifetime.Singleton).As<IUpgradeBuildingUseCase>();
            builder.Register<RecruitSurvivorUseCase>(Lifetime.Singleton).As<IRecruitSurvivorUseCase>();
            builder.Register<TickGameUseCase>(Lifetime.Singleton).As<ITickGameUseCase>();
            builder.Register<OfflineProgressUseCase>(Lifetime.Singleton).As<IOfflineProgressUseCase>();
            builder.Register<SaveLoadUseCase>(Lifetime.Singleton).As<ISaveLoadUseCase>();

            builder.RegisterEntryPoint<GameBootstrapper>();
            builder.RegisterEntryPoint<GameClockLoop>();
            builder.RegisterEntryPoint<AutoSaveLoop>();
            builder.RegisterEntryPoint<CampHudPresenter>();
        }
    }
}
