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
        [SerializeField] private CampMapFogView campMapFogView;
        [SerializeField] private CampWorldTileView campWorldTileView;

        public void SetDashboardReferences(GameConfigDatabaseSO database, CampDashboardView dashboardView)
        {
            configDatabase = database;
            campDashboardView = dashboardView;
        }

        public void SetMapFogView(CampMapFogView view)
        {
            campMapFogView = view;
        }

        public void SetWorldTileView(CampWorldTileView view)
        {
            campWorldTileView = view;
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
            builder.Register<BroadcastRecruitmentUseCase>(Lifetime.Singleton).As<IBroadcastRecruitmentUseCase>();
            builder.Register<RecruitSurvivorUseCase>(Lifetime.Singleton).As<IRecruitSurvivorUseCase>();
            builder.Register<SkipRecruitmentCandidatesUseCase>(Lifetime.Singleton).As<ISkipRecruitmentCandidatesUseCase>();
            builder.Register<RepairItemUseCase>(Lifetime.Singleton).As<IRepairItemUseCase>();
            builder.Register<EquipItemUseCase>(Lifetime.Singleton).As<IEquipItemUseCase>();
            builder.Register<UseMedicineUseCase>(Lifetime.Singleton).As<IUseMedicineUseCase>();
            builder.Register<StartRestUseCase>(Lifetime.Singleton).As<IStartRestUseCase>();
            builder.Register<StopRestUseCase>(Lifetime.Singleton).As<IStopRestUseCase>();
            builder.Register<StartEmergencyScavengeUseCase>(Lifetime.Singleton).As<IStartEmergencyScavengeUseCase>();
            builder.Register<SetAutosaveUseCase>(Lifetime.Singleton).As<ISetAutosaveUseCase>();
            builder.Register<MapFogUseCase>(Lifetime.Singleton).As<IMapFogUseCase>();
            builder.Register<TickGameUseCase>(Lifetime.Singleton).As<ITickGameUseCase>();
            builder.Register<OfflineProgressUseCase>(Lifetime.Singleton).As<IOfflineProgressUseCase>();
            builder.Register<SaveLoadUseCase>(Lifetime.Singleton).As<ISaveLoadUseCase>();
            var lifecycleSaveAdapter = GetComponent<ApplicationLifecycleSaveAdapter>();
            if (lifecycleSaveAdapter != null)
            {
                builder.RegisterBuildCallback(container => lifecycleSaveAdapter.Construct(container.Resolve<ISaveLoadUseCase>()));
            }

            builder.RegisterEntryPoint<GameBootstrapper>();
            builder.RegisterEntryPoint<GameClockLoop>();
            builder.RegisterEntryPoint<AutoSaveLoop>();
            builder.RegisterEntryPoint<CampHudPresenter>();
            if (campMapFogView != null)
            {
                builder.RegisterComponent(campMapFogView).AsSelf();
                builder.RegisterEntryPoint<CampMapFogPresenter>();
            }
            if (campWorldTileView != null && campMapFogView != null)
            {
                builder.RegisterComponent(campWorldTileView).AsSelf();
                builder.RegisterEntryPoint<CampWorldTilePresenter>();
            }
            else if (campWorldTileView != null)
            {
                Debug.LogError("CampWorldTileView requires CampMapFogView for map click routing.", campWorldTileView);
            }
        }
    }
}
