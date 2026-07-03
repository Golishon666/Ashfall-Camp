# Ashfall Camp — Unity Architecture Contract for MCP Server

> **Документ:** архитектурный контракт Unity-проекта  
> **Проект:** Ashfall Camp  
> **Назначение:** использовать как обязательный технический контекст для Unity MCP agent / Codex / Cursor / Claude Code при создании кода, сцен, префабов, конфигов и тестов.  
> **Парный документ:** `ashfall_camp_game_design.md` содержит геймдизайн, контент, баланс и UX-описание игры.

---

## A. Главная установка для Unity MCP agent

`Ashfall Camp` нельзя делать как быстрый MonoBehaviour-прототип. Проект с первого дня должен строиться как нормальное Unity-приложение:

- gameplay-контент хранится в ScriptableObject config assets;
- runtime-state отделён от config-state;
- все application/use-case операции асинхронные через UniTask;
- UI строго следует MVP;
- View — минимальный MonoBehaviour на prefab;
- Presenter — plain C# class;
- все зависимости создаются через VContainer;
- состояние и команды UI идут через R3;
- тексты — только TextMeshPro;
- движения и анимации — через DOTween;
- код следует SOLID;
- в начале и в конце каждой задачи Unity MCP agent обязан сверяться с архитектурными требованиями и выдавать отчёт соответствия.

---

## B. Обязательный стек

| Область | Решение | Обязательное правило |
|---|---|---|
| Reactive state | **R3** | `ReactiveProperty`, `ReadOnlyReactiveProperty`, `Observable` для state и UI commands |
| Async | **UniTask** | Все use cases, simulation runners, save/load, navigation, presenter commands возвращают `UniTask` / `UniTask<T>` |
| DI | **VContainer** | Services, stores, factories, presenters, entry points создаются через DI |
| UI pattern | **MVP** | Абсолютно каждый screen/widget: View interface + prefab View + Presenter + ViewModel |
| Text | **TextMeshPro** | Никакого legacy `UnityEngine.UI.Text` |
| Animation | **DOTween** | Все движения, transitions, UI feedback, panel show/hide, card animations через DOTween |
| UI creation | Prefabs | Весь UI создаётся из prefab assets, настройки UI тоже через prefab/config |
| Data | ScriptableObject configs | Никакого hardcode чисел, цветов, текстов, prefab paths, durations, easings |
| Save | JSON DTO + migrations | Не сериализовать R3/Unity objects напрямую |
| Tests | EditMode + PlayMode | Domain/use cases отдельно, MVP UI отдельно |

---

## C. Абсолютные запреты

Запрещено:

- `GameObject.Find`, `FindObjectOfType`, `FindAnyObjectByType` в runtime-code;
- service locator;
- static singletons для gameplay/services;
- корутины для gameplay flow;
- `async void`, кроме технически неизбежных Unity event wrappers, но в проекте их лучше не использовать вообще;
- `Task.Delay`, `Thread.Sleep`, `System.Threading.Timer` в gameplay;
- бизнес-логика внутри MonoBehaviour;
- прямые изменения GameState из View;
- UI-кнопки через Inspector `OnClick` с прямым вызовом gameplay-методов;
- создание UI из кода вместо prefab;
- хардкод balance values в C#;
- хардкод UI colors, animation durations, easings, texts, localization strings;
- direct `Resources.Load` для основного контента;
- Unity Animator для обычных UI transitions, если это можно сделать DOTween;
- создание новой зависимости/package без явного решения;
- смешивание save DTO и runtime model;
- nullable string ids без validation;
- presenter без dispose subscriptions;
- async operation без `CancellationToken`.

---

## D. Async policy

Пользовательское требование: “весь код должен быть асинхронным”. В проекте это трактуется как архитектурный контракт:

1. Все public application/use-case/service методы с side effects возвращают `UniTask` / `UniTask<T>`.
2. Все Presenter command handlers возвращают `UniTask`.
3. Все navigation, screen open/close, animations, save/load, config loading, simulation runners — async.
4. Все long-running loops имеют `CancellationToken`.
5. Pure domain formulas могут быть sync только как внутренняя deterministic math-логика без Unity API, без side effects и без blocking.

Правильно:

```csharp
public interface ILaunchExpeditionUseCase
{
    UniTask<LaunchExpeditionResult> ExecuteAsync(LaunchExpeditionRequest request, CancellationToken ct);
}
```

Допустимо для pure domain:

```csharp
public static int CalculateDamage(CombatantSnapshot attacker, CombatantSnapshot defender, WeaponConfig config)
{
    return Math.Max(config.MinimumDamage, attacker.Power + config.BaseDamage - defender.Defense);
}
```

Неправильно:

```csharp
public void LaunchExpedition() { }
IEnumerator SimulateCombat() { }
async void OnClick() { }
```

---

## E. SOLID как обязательное требование

Код должен следовать SOLID не декларативно, а проверяемо.

### E.1. Single Responsibility Principle

Каждый класс имеет одну причину для изменения.

Примеры:

| Класс | Единственная ответственность |
|---|---|
| `CampDashboardView` | Отрисовка prefab-ссылок и экспонирование UI commands |
| `CampDashboardPresenter` | Binding dashboard read model к View и обработка UI commands |
| `LaunchExpeditionUseCase` | Запуск экспедиции и orchestration validation/spend/state update |
| `CombatResolver` | Pure расчёт одного combat tick |
| `ExpeditionSimulationRunner` | Async progression running экспедиции |
| `SaveRepository` | Чтение/запись save DTO |
| `ConfigValidator` | Проверка config assets |

Неправильно: `GameManager` делает ресурсы, бой, UI, save, навигацию и анимации.

### E.2. Open/Closed Principle

Новый resource, item, enemy, zone, building, UI screen, animation preset должен добавляться через config/factory/catalog, а не через переписывание core service.

Пример:

- добавить новый ресурс `electronics` → добавить `ResourceConfigSO`, icon, localization key, starting/rate config;
- не менять `ResourceService` под конкретный `electronics`.

### E.3. Liskov Substitution Principle

Любая реализация интерфейса должна вести себя как контракт.

Пример:

- `IExpeditionSetupView` не должен частично игнорировать `SetInteractable(false)`;
- `ISaveRepository` в fake/in-memory реализации должен сохранять и возвращать same DTO semantics.

### E.4. Interface Segregation Principle

Интерфейсы маленькие и специализированные.

Правильно:

```csharp
public interface IGameStateReader { }
public interface IGameStateWriter { }
public interface ILaunchExpeditionUseCase { }
public interface IExpeditionCommandUseCase { }
```

Неправильно:

```csharp
public interface IGameService
{
    UniTask LaunchExpeditionAsync(...);
    UniTask SaveAsync(...);
    UniTask UpgradeBuildingAsync(...);
    UniTask RecruitAsync(...);
    UniTask OpenScreenAsync(...);
}
```

### E.5. Dependency Inversion Principle

High-level modules зависят от abstractions, не от concrete Unity objects.

Правильно:

```csharp
public sealed class LaunchExpeditionUseCase
{
    private readonly IGameStateReader _reader;
    private readonly IGameStateWriter _writer;
    private readonly IGameConfigProvider _configs;
}
```

Неправильно:

```csharp
public sealed class LaunchExpeditionUseCase
{
    private GameConfigDatabaseSO _database;
    private CampDashboardView _view;
}
```

### E.6. SOLID self-check для каждой задачи

В конце любой реализации агент должен ответить:

```text
SOLID check:
- SRP: pass/fail + почему
- OCP: pass/fail + что расширяется через config/factory/catalog
- LSP: pass/fail + какие интерфейсы соблюдены
- ISP: pass/fail + нет ли fat interfaces
- DIP: pass/fail + нет ли зависимости application/domain от concrete Unity classes
```

---

## F. DOTween animation contract

DOTween — единый слой для движения и анимаций.

### F.1. Что анимируется через DOTween

Через DOTween делать:

- screen fade in/out;
- modal show/hide;
- toast show/hide;
- button press feedback;
- card hover/select/focus;
- resource gain pulse;
- warning shake;
- panel slide;
- progress bar smooth fill;
- combat log row появление;
- survivor card reorder/move;
- expedition timeline marker movement;
- camp overview marker pulse;
- world/map icon movement;
- любые UI transitions;
- 2D object movement, если появится визуальная карта/лагерь.

### F.2. Что не должно быть в DOTween hardcoded

Запрещено хардкодить:

- duration;
- ease;
- delay;
- punch scale;
- shake strength;
- color values;
- target offsets;
- alpha values, если они являются style decision.

Все настройки хранятся в:

```text
UiAnimationConfigSO
MotionAnimationConfigSO
UiThemeConfigSO
```

### F.3. DOTween service

Presentation layer получает animation service, но View может физически проигрывать tween, потому что только View знает `RectTransform`, `CanvasGroup`, `Image`.

Рекомендуемый контракт:

```csharp
public interface IUiTweenService
{
    UniTask FadeInAsync(CanvasGroup canvasGroup, UiAnimationId animationId, CancellationToken ct);
    UniTask FadeOutAsync(CanvasGroup canvasGroup, UiAnimationId animationId, CancellationToken ct);
    UniTask SlideInAsync(RectTransform target, UiAnimationId animationId, CancellationToken ct);
    UniTask PunchAsync(RectTransform target, UiAnimationId animationId, CancellationToken ct);
    UniTask ShakeAsync(RectTransform target, UiAnimationId animationId, CancellationToken ct);
}
```

### F.4. View animation methods

Каждый screen view должен иметь async show/hide methods:

```csharp
public interface IScreenView
{
    UniTask ShowAsync(CancellationToken ct);
    UniTask HideAsync(CancellationToken ct);
}
```

Пример:

```csharp
public sealed class CampDashboardView : MonoBehaviour, ICampDashboardView
{
    [SerializeField] private CanvasGroup rootCanvasGroup;
    [SerializeField] private RectTransform rootPanel;

    private IUiTweenService _tweenService;

    [Inject]
    public void Construct(IUiTweenService tweenService)
    {
        _tweenService = tweenService;
    }

    public UniTask ShowAsync(CancellationToken ct)
    {
        return _tweenService.FadeInAsync(rootCanvasGroup, UiAnimationId.ScreenFade, ct);
    }

    public UniTask HideAsync(CancellationToken ct)
    {
        return _tweenService.FadeOutAsync(rootCanvasGroup, UiAnimationId.ScreenFade, ct);
    }
}
```

### F.5. DOTween + UniTask wrapper

Tween completion must be awaitable via UniTask. Создать один adapter, не писать wrapper каждый раз.

```csharp
public interface ITweenAwaiter
{
    UniTask AwaitAsync(Tween tween, CancellationToken ct);
}
```

Правила:

- если `CancellationToken` отменён — tween убивается;
- если View destroyed — tween убивается;
- tween exceptions/errors идут в `IAsyncErrorHandler`;
- не оставлять активные tweens после закрытия screen;
- использовать target/linking, чтобы DOTween убивал tween при destroy target.

### F.6. DOTween usage rules

- Не создавать tween в Presenter напрямую, если Presenter не владеет Unity object.
- Presenter может вызвать `view.PlayResourceGainAsync(...)`.
- View может использовать DOTween, но только для визуального поведения.
- Duration/ease берутся из config.
- DOTween sequences должны быть killed/disposed при close/destroy.
- UI tweens должны работать при pause/timeScale=0, если это задано config.
- Gameplay simulation не должна зависеть от completion UI tween, кроме screen transitions.

---

## G. Unity MCP self-check protocol

Unity MCP agent обязан сверяться с архитектурными требованиями **в начале и конце каждой работы**.

### G.1. Start-of-work architecture check

Перед кодом агент должен вывести/зафиксировать:

```text
Architecture pre-check:
- Task understood: [one sentence]
- Affected layer(s): Domain / Application / Infrastructure / Presentation / Composition / Editor / Tests
- Required pattern: MVP / UseCase / Config / VContainer registration / R3 binding / UniTask async / DOTween animation
- Required prefabs: [list or none]
- Required configs: [list or none]
- Hardcode risk: [what could become hardcoded and where config will live]
- MonoBehaviour risk: [which classes are allowed to be MonoBehaviour]
- SOLID risk: [possible SRP/OCP/DIP risks]
```

### G.2. End-of-work architecture compliance report

После работы агент должен вывести:

```text
Architecture compliance report:
- R3: pass/fail — where observable values/UI commands are used
- UniTask: pass/fail — async methods and cancellation tokens
- VContainer: pass/fail — registrations/factories/entry points
- MVP: pass/fail — View interface + View prefab + Presenter + ViewModel
- TextMeshPro: pass/fail — no legacy UI Text
- DOTween: pass/fail — animations/movement through DOTween/config
- Prefab UI: pass/fail — no runtime UI construction except prefab instantiation
- Config-driven: pass/fail — no hardcoded balance/UI/animation values
- Minimal MonoBehaviours: pass/fail — only Views/LifetimeScopes/adapters
- SOLID: pass/fail — SRP/OCP/LSP/ISP/DIP notes
- Tests/validation: pass/fail — added/updated tests or config validation
- Banned APIs scan: pass/fail — no GameObject.Find/FindObjectOfType/coroutines/async void
- Compile status: pass/fail — errors fixed or listed
```

### G.3. If architecture check fails

Если агент обнаружил нарушение, он не должен “просто продолжить”. Он должен:

1. назвать нарушение;
2. исправить его;
3. если исправить невозможно в рамках задачи — явно написать причину и предложить минимальный архитектурный fix;
4. не оставлять нарушение молча.

---

## H. Unity project structure

```text
Assets/
  AshfallCamp/
    Docs/
      AshfallCamp_Unity_MCP_TDD.md
    Art/
      UI/
      Characters/
      Icons/
      Environments/
    Audio/
      Music/
      SFX/
    Configs/
      Game/
        GameConfigDatabase.asset
        BalanceConfig.asset
        TimeConfig.asset
      Resources/
        ResourceCatalog.asset
      Survivors/
        SurvivorCatalog.asset
        BackgroundCatalog.asset
        TraitCatalog.asset
      Expeditions/
        ZoneCatalog.asset
        ExpeditionPolicyCatalog.asset
        EventCatalog.asset
      Combat/
        EnemyCatalog.asset
        CombatBalanceConfig.asset
      Items/
        ItemCatalog.asset
      Buildings/
        BuildingCatalog.asset
        UpgradeCatalog.asset
      UI/
        UiThemeConfig.asset
        TypographyConfig.asset
        UiScreenPrefabCatalog.asset
        UiAnimationConfig.asset
        MotionAnimationConfig.asset
    Prefabs/
      Composition/
        ProjectLifetimeScope.prefab
      UI/
        Screens/
          PF_UI_CampDashboard.prefab
          PF_UI_SurvivorsRoster.prefab
          PF_UI_SurvivorDetail.prefab
          PF_UI_ExpeditionSelect.prefab
          PF_UI_ExpeditionSetup.prefab
          PF_UI_ExpeditionMonitor.prefab
          PF_UI_AfterActionReport.prefab
          PF_UI_Buildings.prefab
          PF_UI_Workshop.prefab
          PF_UI_Radio.prefab
          PF_UI_OfflineReport.prefab
          PF_UI_Settings.prefab
        Shared/
          PF_UI_TopResourceBar.prefab
          PF_UI_BottomNavBar.prefab
          PF_UI_SurvivorCard.prefab
          PF_UI_ZoneCard.prefab
          PF_UI_ItemCard.prefab
          PF_UI_BuildingCard.prefab
          PF_UI_ModalConfirm.prefab
          PF_UI_Tooltip.prefab
          PF_UI_Toast.prefab
    Scenes/
      SC_Boot.unity
      SC_Game.unity
    Scripts/
      Runtime/
        AshfallCamp.Domain/
        AshfallCamp.Application/
        AshfallCamp.Infrastructure/
        AshfallCamp.Presentation/
        AshfallCamp.Composition/
        AshfallCamp.UnityAdapters/
      Editor/
        AshfallCamp.Editor/
      Tests/
        EditMode/
        PlayMode/
```

---

## I. Assembly definitions

```text
AshfallCamp.Domain.asmdef
AshfallCamp.Application.asmdef
AshfallCamp.Infrastructure.asmdef
AshfallCamp.Presentation.asmdef
AshfallCamp.Composition.asmdef
AshfallCamp.UnityAdapters.asmdef
AshfallCamp.Editor.asmdef
AshfallCamp.Tests.EditMode.asmdef
AshfallCamp.Tests.PlayMode.asmdef
```

Dependencies:

```text
Domain:
  no UnityEngine
  no R3
  no UniTask
  no VContainer
  no DOTween

Application:
  Domain
  R3
  UniTask

Infrastructure:
  Domain
  Application
  UnityEngine
  UniTask

Presentation:
  Domain
  Application
  UnityEngine
  UnityEngine.UI
  TMPro
  R3
  UniTask
  DOTween

Composition:
  Domain
  Application
  Infrastructure
  Presentation
  UnityEngine
  VContainer
  UniTask

Editor:
  Domain
  Infrastructure
  UnityEditor

Tests.EditMode:
  Domain
  Application
  Infrastructure

Tests.PlayMode:
  all runtime assemblies
```

---

## J. VContainer DI contract

### J.1. Composition root

`ProjectLifetimeScope` — единственный главный composition root в boot scene.

```csharp
public sealed class ProjectLifetimeScope : LifetimeScope
{
    [SerializeField] private GameConfigDatabaseSO configDatabase;
    [SerializeField] private UiRootView uiRootView;

    protected override void Configure(IContainerBuilder builder)
    {
        builder.RegisterInstance(configDatabase);

        builder.Register<ScriptableObjectGameConfigProvider>(Lifetime.Singleton)
            .As<IGameConfigProvider>();

        builder.Register<GameStateStore>(Lifetime.Singleton)
            .As<IGameStateReader>()
            .As<IGameStateWriter>()
            .AsSelf();

        builder.Register<LaunchExpeditionUseCase>(Lifetime.Singleton).As<ILaunchExpeditionUseCase>();
        builder.Register<ExpeditionCommandUseCase>(Lifetime.Singleton).As<IExpeditionCommandUseCase>();
        builder.Register<ExpeditionSimulationRunner>(Lifetime.Singleton).As<IExpeditionSimulationRunner>();
        builder.Register<SaveLoadUseCase>(Lifetime.Singleton).As<ISaveLoadUseCase>();
        builder.Register<OfflineProgressUseCase>(Lifetime.Singleton).As<IOfflineProgressUseCase>();
        builder.Register<BuildingUseCase>(Lifetime.Singleton).As<IBuildingUseCase>();
        builder.Register<RecruitSurvivorUseCase>(Lifetime.Singleton).As<IRecruitSurvivorUseCase>();

        builder.Register<UiNavigator>(Lifetime.Singleton).As<IUiNavigator>();
        builder.Register<UiPrefabFactory>(Lifetime.Singleton).As<IUiPrefabFactory>();
        builder.Register<UiTweenService>(Lifetime.Singleton).As<IUiTweenService>();
        builder.Register<TweenAwaiter>(Lifetime.Singleton).As<ITweenAwaiter>();

        builder.Register<CampDashboardPresenterFactory>(Lifetime.Singleton);
        builder.Register<SurvivorsRosterPresenterFactory>(Lifetime.Singleton);
        builder.Register<SurvivorDetailPresenterFactory>(Lifetime.Singleton);
        builder.Register<ExpeditionSelectPresenterFactory>(Lifetime.Singleton);
        builder.Register<ExpeditionSetupPresenterFactory>(Lifetime.Singleton);
        builder.Register<ExpeditionMonitorPresenterFactory>(Lifetime.Singleton);
        builder.Register<AfterActionReportPresenterFactory>(Lifetime.Singleton);
        builder.Register<BuildingsPresenterFactory>(Lifetime.Singleton);
        builder.Register<WorkshopPresenterFactory>(Lifetime.Singleton);
        builder.Register<RadioPresenterFactory>(Lifetime.Singleton);
        builder.Register<OfflineReportPresenterFactory>(Lifetime.Singleton);

        builder.RegisterComponent(uiRootView).As<IUiRootView>();

        builder.RegisterEntryPoint<GameBootstrapper>();
        builder.RegisterEntryPoint<GameClockLoop>();
        builder.RegisterEntryPoint<AutoSaveLoop>();
    }
}
```

### J.2. Minimal MonoBehaviour policy

Allowed MonoBehaviours:

```text
ProjectLifetimeScope
GameLifetimeScope if needed
UiRootView
Screen Views
Reusable component Views, e.g. SurvivorCardView
Unity lifecycle adapters, e.g. ApplicationPauseAdapter
Visual-only DOTween adapters if needed
```

Forbidden MonoBehaviours:

```text
GameManagerMonoBehaviour
ResourceManagerMonoBehaviour
CombatManagerMonoBehaviour
SaveManagerMonoBehaviour
ExpeditionManagerMonoBehaviour
BuildingManagerMonoBehaviour
```

---

## K. R3 reactive state and UI commands

### K.1. Store ownership

Only application services/use cases mutate state.

```csharp
public interface IGameStateReader
{
    ReadOnlyReactiveProperty<ResourceState> Resources { get; }
    ReadOnlyReactiveProperty<IReadOnlyList<SurvivorState>> Survivors { get; }
    ReadOnlyReactiveProperty<IReadOnlyList<ExpeditionState>> Expeditions { get; }
    ReadOnlyReactiveProperty<IReadOnlyList<BuildingState>> Buildings { get; }
    ReadOnlyReactiveProperty<CampStatusState> CampStatus { get; }
}

public interface IGameStateWriter
{
    UniTask MutateAsync(Func<GameState, GameState> mutation, CancellationToken ct);
}
```

### K.2. UI command contract

Views expose commands as observables:

```csharp
public interface IExpeditionSetupView : IScreenView
{
    Observable<Unit> LaunchClicked { get; }
    Observable<SurvivorId> SurvivorSlotClicked { get; }
    Observable<ExpeditionPolicyId> PolicySelected { get; }

    void Render(ExpeditionSetupViewModel vm);
    void SetInteractable(bool value);
    void ShowValidationWarnings(IReadOnlyList<UiWarningViewModel> warnings);
}
```

Presenter uses async subscriptions:

```csharp
_view.LaunchClicked
    .SubscribeAwait(
        async (_, commandCt) => await LaunchAsync(commandCt),
        AwaitOperation.Drop)
    .AddTo(_disposables);
```

Rules:

- `AwaitOperation.Drop` for submit buttons;
- `AwaitOperation.Switch` for search/filter input;
- `AwaitOperation.Sequential` for queued operations;
- dispose subscriptions on close;
- no subscription ownership inside View except local visual-only events.

---

## L. MVP UI contract

For every screen:

```text
I[Screen]View.cs
[Screen]View.cs
[Screen]Presenter.cs
[Screen]PresenterFactory.cs
[Screen]ViewModel.cs
PF_UI_[Screen].prefab
```

Example:

```text
ICampDashboardView.cs
CampDashboardView.cs
CampDashboardPresenter.cs
CampDashboardPresenterFactory.cs
CampDashboardViewModel.cs
PF_UI_CampDashboard.prefab
```

### L.1. View rules

View:

- MonoBehaviour;
- serialized prefab references only;
- exposes R3 Observables for UI commands;
- renders ViewModel;
- uses TMP_Text;
- may call DOTween only for visual animation methods;
- does not call gameplay services;
- does not mutate GameState;
- does not format gameplay formulas;
- does not instantiate screens directly;
- does not contain hardcoded colors/texts/durations/eases.

### L.2. Presenter rules

Presenter:

- plain C#;
- created through factory/DI;
- owns subscriptions;
- binds read models to view;
- handles UI commands;
- calls async use cases;
- calls `IUiNavigator`;
- awaits View show/hide animation if needed;
- disposes subscriptions and cancellation tokens;
- does not own Unity serialized refs;
- does not access DOTween directly unless it is a presentation service with no Unity target ownership.

### L.3. ViewModel rules

ViewModel:

- immutable;
- UI-ready;
- no mutation methods;
- no Unity components except safe references such as Sprite keys if the project chooses them;
- no direct access to services;
- values already formatted through localization/formatting service.

---

## M. UI prefab contract

Every screen and reusable UI block must be prefab-based.

### M.1. Screen prefab catalog

```csharp
[CreateAssetMenu(menuName = "Ashfall Camp/UI/Screen Prefab Catalog")]
public sealed class UiScreenPrefabCatalogSO : ScriptableObject
{
    [SerializeField] private List<UiScreenPrefabEntry> screens;
}

[Serializable]
public sealed class UiScreenPrefabEntry
{
    [SerializeField] private string screenId;
    [SerializeField] private GameObject prefab;
}
```

### M.2. Required shared prefabs

```text
PF_UI_TopResourceBar
PF_UI_BottomNavBar
PF_UI_AlertCard
PF_UI_SurvivorCard
PF_UI_ZoneCard
PF_UI_ItemCard
PF_UI_BuildingCard
PF_UI_ProgressBar
PF_UI_StatBar
PF_UI_CommandButton
PF_UI_ModalConfirm
PF_UI_Tooltip
PF_UI_Toast
```

### M.3. UI settings through prefab/config

UI layout decisions must live in:

```text
prefab serialized fields
UiThemeConfigSO
TypographyConfigSO
UiAnimationConfigSO
UiLayoutConfigSO
MotionAnimationConfigSO
```

Not in Presenter and not in random constants class.

---


### M.4. Friendly UI design contract

Новый визуальный принцип проекта: **friendly survival UI**.

Интерфейс должен ощущаться как понятная, спокойная панель управления лагерем, а не как шумный военный терминал. Постапокалипсис остаётся в теме предметов, иллюстраций и иконок, но **не должен мешать читаемости**.

#### Обязательные правила friendly UI

- Меньше визуального шума: grunge-текстуры только на рамках/фоне, не поверх текста.
- Больше воздуха: достаточные отступы между карточками, секциями и кнопками.
- Панели должны быть чистыми: лёгкая бумага/пыль/металл, без агрессивной грязи на каждом элементе.
- Основной фон — тёплый нейтральный, а не почти чёрный.
- Чёрный/угольный использовать для текста, тонких рамок и глубины, но не как доминирующий цвет всего экрана.
- Ржаво-оранжевый использовать как акцент, а не заливку всего интерфейса.
- Красный использовать только для настоящей опасности: смерть, критическая рана, провал, нехватка ресурса.
- Один экран — максимум 1–2 активных акцентных цвета.
- Карточки survivors, zones, items и buildings должны читаться за 1–2 секунды.
- На скриншоте игрок должен сначала видеть “что делать дальше”, потом детали.
- Избегать мелкого декоративного текста, псевдо-потёртостей поверх цифр и перегруженных декоративных рамок.
- Использовать friendly empty states: “No one is wounded”, “All squads returned”, “Ready to launch”, а не мрачные/карательные сообщения.
- Состояния Success/Warning/Danger должны отличаться цветом, иконкой и текстом, не только цветом.
- UI-анимации через DOTween должны быть мягкими и полезными: fade/slide/scale для фидбека, без резких тряских эффектов.

#### Палитра friendly survival UI

| Token | Назначение |
|---|---|
| `WarmPaper` | Основные карточки и readable panels |
| `SoftSand` | Фон вторичных секций |
| `SageGreen` | Success, stable camp, safe actions |
| `FadedTeal` | Info, radio, neutral data |
| `DustyOlive` | Secondary controls |
| `RustAccent` | Основной CTA/accent, умеренно |
| `SoftAmber` | Warning |
| `MutedRed` | Danger only |
| `CharcoalText` | Основной текст |
| `SoftSteel` | Разделители, рамки, inactive UI |

#### Арт-направление

Визуальная формула:

```text
cozy post-apocalyptic camp management, readable survival dashboard, warm worn paper panels, soft sand and sage palette, clean card UI, friendly idle RPG, light dust not heavy grime, hopeful rebuilding tone
```

Не использовать как основной тон:

```text
grimdark, horror UI, military command bunker, excessive dirt, black-on-black panels, noisy scratches over text, skulls everywhere, harsh red alerts everywhere
```

---

## N. TextMeshPro contract

- Use `TMP_Text` for every label.
- Use `TMP_InputField` for every input.
- No `UnityEngine.UI.Text`.
- Font assets come from `TypographyConfigSO`.
- Display strings come from localization/config keys.
- Use `SetText()` for dynamic text where possible.
- Do not concatenate rich text directly in View unless formatting service already prepared it.

---

## O. Config-driven system

### O.1. Main config database

```csharp
[CreateAssetMenu(menuName = "Ashfall Camp/Game Config Database")]
public sealed class GameConfigDatabaseSO : ScriptableObject
{
    [SerializeField] private ResourceCatalogSO resources;
    [SerializeField] private SurvivorCatalogSO survivors;
    [SerializeField] private BackgroundCatalogSO backgrounds;
    [SerializeField] private TraitCatalogSO traits;
    [SerializeField] private ZoneCatalogSO zones;
    [SerializeField] private EnemyCatalogSO enemies;
    [SerializeField] private ItemCatalogSO items;
    [SerializeField] private BuildingCatalogSO buildings;
    [SerializeField] private UpgradeCatalogSO upgrades;
    [SerializeField] private EventCatalogSO events;
    [SerializeField] private BalanceConfigSO balance;
    [SerializeField] private TimeConfigSO time;
    [SerializeField] private UiThemeConfigSO uiTheme;
    [SerializeField] private TypographyConfigSO typography;
    [SerializeField] private UiScreenPrefabCatalogSO uiScreens;
    [SerializeField] private UiAnimationConfigSO uiAnimations;
    [SerializeField] private MotionAnimationConfigSO motionAnimations;
}
```

### O.2. Application-facing config provider

Application layer sees plain snapshots, not ScriptableObject:

```csharp
public interface IGameConfigProvider
{
    UniTask<GameConfigSnapshot> LoadAsync(CancellationToken ct);
    GameConfigSnapshot Current { get; }
}
```

### O.3. Config validation

Create menu:

```text
Tools/Ashfall Camp/Validate Configs
```

Validation checks:

- duplicate ids;
- empty ids;
- missing icons;
- missing TMP fonts;
- missing prefab references;
- invalid resource references;
- invalid item references;
- invalid enemy references;
- invalid zone loot tables;
- invalid animation ids;
- invalid DOTween ease tokens;
- missing localization keys;
- negative values where forbidden;
- zero expedition duration;
- survivor capacity below starting roster;
- missing VContainer scene references.

Validation errors should block build.

---

## P. Save/load contract

- Save file uses DTO, not runtime model directly.
- Save/load async via UniTask.
- Save contains version and timestamp.
- Save supports migrations.
- Save does not serialize R3 properties, ScriptableObjects, GameObjects, Sprites, TMP assets or DOTween Tweens.

```csharp
public interface ISaveLoadUseCase
{
    UniTask<LoadGameResult> LoadOrCreateAsync(CancellationToken ct);
    UniTask SaveAsync(CancellationToken ct);
}
```

---

## Q. Unity implementation tips

### Q.1. General Unity tips

- Use asmdef from the start.
- Keep Domain assembly Unity-free.
- Commit `.meta` files.
- Use prefabs for every reusable UI part.
- Keep scenes thin: composition root + UI root + cameras/lights if needed.
- Avoid `Update`; use async loops/entry points/tick services.
- Pool repeated UI cards and combat log rows.
- Use ScrollRect virtualization for large rosters/inventories later.
- Do not put long logic in property getters.
- Use `CancellationToken` from screen lifecycle for presenter subscriptions/animations.
- Use `OnApplicationPause`/`OnApplicationQuit` only in a Unity adapter, forwarding to async save use case.
- Use Editor validation tools before builds.
- Use fake repositories/configs for tests.
- Profile UI rebuilds and overdraw early.
- Avoid huge canvases constantly rebuilding; split static/dynamic sections into separate canvases when needed.
- Keep localization keys stable even before real localization.

### Q.2. UI tips

- Shared top resource bar should be a prefab reused across screens.
- Screen prefabs should have `CanvasGroup` root for DOTween fade.
- Avoid nested layout groups with constantly changing content in hot areas.
- Keep interactive states visible: disabled/loading/selected/warning.
- All buttons should have view-level observable commands.
- Avoid direct `button.onClick.AddListener` in Presenter if View can expose `Observable<Unit>`.
- No hardcoded string labels in View.
- Use tooltip prefab for formulas and warnings.

### Q.3. Async tips

- Never ignore cancellation.
- For screen close, cancel screen CTS before destroying view.
- Await hide animation before destroying screen prefab if desired.
- Save/load errors should show modal but not crash silently.
- Do not use fire-and-forget except through controlled helper with error handler.

### Q.4. DOTween tips

- Kill tweens on destroy/close.
- Keep animation ids in config.
- Prefer sequences for screen transitions.
- Use unscaled update for UI if pause/timeScale may be zero.
- Avoid animating layout-driven properties every frame if layout groups constantly rebuild.
- For progress bars, tween value/fill smoothly but state remains authoritative in GameState.
- DOTween is visual; gameplay result must not depend on animation completion except UI transition flow.

### Q.5. VContainer tips

- Composition root owns registrations.
- Prefer constructor injection for plain C#.
- Register MonoBehaviour views/components explicitly.
- Use factories for presenters that need runtime view parameter.
- Do not call resolver from arbitrary gameplay classes.
- Keep lifetimes obvious and documented.

---

## R. MCP implementation prompt

Use this as the default prompt for implementation tasks:

```text
You are working on Ashfall Camp in Unity through Unity MCP server.

Before coding:
1. Read Docs/AshfallCamp_Unity_MCP_TDD.md.
2. Run Architecture pre-check.
3. Inspect existing project structure, packages, asmdefs, prefabs, configs.
4. List files/prefabs/configs to create or modify.

Hard requirements:
- SOLID.
- R3 for reactive values and UI commands.
- UniTask for all application operations and async flows.
- VContainer DI.
- MVP for absolutely every UI screen/widget.
- TextMeshPro for all text.
- DOTween for movement and animations, including UI.
- Minimal MonoBehaviours.
- No hardcode. Use ScriptableObject configs.
- All UI through prefabs and prefab catalogs.
- UI settings through prefabs/configs.
- No banned APIs.

After coding:
1. Run compile/tests/console check if MCP tools allow it.
2. Fix compile errors.
3. Run Architecture compliance report.
4. State exactly which requirements passed or failed.
```

---

## S. Banned API scan list

At the end of a task, search for:

```text
GameObject.Find
FindObjectOfType
FindAnyObjectByType
FindObjectsOfType
IEnumerator
StartCoroutine
StopCoroutine
async void
UnityEngine.UI.Text
Resources.Load
new GameObject
DOTween.To(  // allowed only in animation service/views, not application/domain
static .*Instance
```

This scan is not enough by itself, but it catches common architecture violations.

---

## T. Unity-specific MVP screen list

Every screen below must have View interface, View MonoBehaviour, Presenter, PresenterFactory, ViewModel, prefab, and tests where meaningful.

1. `CampDashboard`
2. `SurvivorsRoster`
3. `SurvivorDetail`
4. `ExpeditionSelect`
5. `ExpeditionSetup`
6. `ExpeditionMonitor`
7. `AfterActionReport`
8. `Buildings`
9. `Workshop`
10. `Radio`
11. `OfflineReport`
12. `Settings`
13. `ModalConfirm`
14. `Tooltip`
15. `Toast`

---

## U. Note about config-data blocks below

The original design spec used JSON-looking examples for readability. In Unity implementation these are **not hardcoded JSON files by default**. Treat them as serialized shapes for ScriptableObject config assets and editor validation. The source of truth in Unity is:

```text
Assets/AshfallCamp/Configs/**/*.asset
```

Optional JSON export/import may be added later only as tooling.

---

---

# Appendix A — Architecture acceptance checklist for pull requests / MCP tasks

Every completed feature must satisfy this checklist.

## A.1. Required pass criteria

```text
[ ] No compile errors.
[ ] No banned API usage.
[ ] Public application/use-case operations are UniTask-based.
[ ] Every async method has CancellationToken unless it is a tiny adapter with documented reason.
[ ] R3 is used for observable state and UI commands.
[ ] UI follows MVP: View interface + View prefab + Presenter + ViewModel.
[ ] Text uses TextMeshPro.
[ ] UI follows friendly survival UI contract: readable, lower-noise, warmer, less grimdark.
[ ] Animations/movement use DOTween.
[ ] DOTween durations/eases come from config.
[ ] Dependencies are registered through VContainer.
[ ] No service locator/static gameplay singleton.
[ ] No hardcoded balance values.
[ ] No hardcoded UI strings/colors/animation values.
[ ] New content is added through ScriptableObject configs.
[ ] Config validation passes.
[ ] Presenter disposes subscriptions.
[ ] View contains no business logic.
[ ] Domain layer has no UnityEngine/R3/UniTask/VContainer/DOTween dependency.
[ ] SOLID check completed.
[ ] Tests added/updated for changed domain/use case logic.
```

## A.2. Required final MCP response format

```text
Done: [short summary]

Changed:
- [files]
- [prefabs]
- [configs]
- [tests]

Architecture compliance report:
- R3: pass/fail — details
- UniTask: pass/fail — details
- VContainer: pass/fail — details
- MVP: pass/fail — details
- TextMeshPro: pass/fail — details
- Friendly UI: pass/fail — readability/noise/palette/details
- DOTween: pass/fail — details
- Prefab UI: pass/fail — details
- Config-driven: pass/fail — details
- Minimal MonoBehaviours: pass/fail — details
- SOLID: pass/fail — details
- Banned APIs: pass/fail — details
- Compile/tests: pass/fail — details

Known issues:
- [none or list]
```

## A.3. If any item fails

The feature is not complete. Fix the architecture issue before moving to the next task.

---
