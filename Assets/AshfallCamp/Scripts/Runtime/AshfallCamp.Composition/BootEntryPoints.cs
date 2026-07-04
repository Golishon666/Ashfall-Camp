using System;
using System.Collections.Generic;
using System.Globalization;
using System.Threading;
using AshfallCamp.Application;
using AshfallCamp.Domain;
using AshfallCamp.Presentation;
using Cysharp.Threading.Tasks;
using R3;
using UnityEngine;
using VContainer.Unity;

namespace AshfallCamp.Composition
{
    public sealed class GameBootstrapper : IAsyncStartable
    {
        private readonly ISaveLoadUseCase _saveLoad;
        private readonly IOfflineProgressUseCase _offlineProgress;
        private readonly IUnixTimeProvider _clock;

        public GameBootstrapper(ISaveLoadUseCase saveLoad, IOfflineProgressUseCase offlineProgress, IUnixTimeProvider clock)
        {
            _saveLoad = saveLoad;
            _offlineProgress = offlineProgress;
            _clock = clock;
        }

        public async UniTask StartAsync(CancellationToken cancellation = default)
        {
            var load = await _saveLoad.LoadOrCreateAsync(cancellation);
            if (load == null || load.CreatedNew || load.State == null) return;

            var report = await _offlineProgress.ExecuteAsync(_clock.NowUnixMs, cancellation);
            if (report != null && report.AppliedSeconds > 0)
            {
                await _saveLoad.SaveAsync(cancellation);
            }
        }
    }

    public sealed class GameClockLoop : ITickable
    {
        private readonly ITickGameUseCase _tickGame;
        private readonly IGameConfigProvider _configs;
        private readonly ISaveLoadUseCase _saveLoad;
        private readonly IGameStateReader _reader;
        private double _accumulator;
        private bool _isTicking;

        public GameClockLoop(ITickGameUseCase tickGame, IGameConfigProvider configs, ISaveLoadUseCase saveLoad, IGameStateReader reader)
        {
            _tickGame = tickGame;
            _configs = configs;
            _saveLoad = saveLoad;
            _reader = reader;
        }

        public void Tick()
        {
            if (_configs.Current == null) return;
            _accumulator += Time.unscaledDeltaTime;
            var step = Math.Max(0.1, _configs.Current.Balance.SimulationTickSeconds);
            if (_accumulator < step || _isTicking) return;
            _accumulator -= step;
            TickStepAsync(step, CancellationToken.None).Forget();
        }

        public async UniTask TickStepAsync(double step, CancellationToken ct)
        {
            if (_isTicking) return;
            _isTicking = true;
            try
            {
                var result = await _tickGame.ExecuteAsync(step, ct);
                if (result != null && result.HasCriticalProgress && IsAutosaveEnabled())
                {
                    await _saveLoad.SaveAsync(ct);
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isTicking = false;
            }
        }

        private bool IsAutosaveEnabled()
        {
            var settings = _reader.State.CurrentValue.Settings;
            return settings == null || settings.AutosaveEnabled;
        }
    }

    public sealed class AutoSaveLoop : ITickable
    {
        private readonly ISaveLoadUseCase _saveLoad;
        private readonly IGameConfigProvider _configs;
        private readonly IGameStateReader _reader;
        private double _accumulator;
        private bool _isSaving;

        public AutoSaveLoop(ISaveLoadUseCase saveLoad, IGameConfigProvider configs, IGameStateReader reader)
        {
            _saveLoad = saveLoad;
            _configs = configs;
            _reader = reader;
        }

        public void Tick()
        {
            if (_configs.Current == null || !_reader.State.CurrentValue.Settings.AutosaveEnabled) return;
            _accumulator += Time.unscaledDeltaTime;
            if (_accumulator < _configs.Current.Balance.AutosaveSeconds || _isSaving) return;
            _accumulator = 0;
            SaveAsync().Forget();
        }

        private async UniTaskVoid SaveAsync()
        {
            _isSaving = true;
            try
            {
                await _saveLoad.SaveAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isSaving = false;
            }
        }
    }

    public sealed class CampHudPresenter : IStartable, ITickable, IDisposable
    {
        private readonly IUiRootView _view;
        private readonly IGameStateReader _reader;
        private readonly IGameStateWriter _writer;
        private readonly IGameConfigProvider _configs;
        private readonly IUpgradeBuildingUseCase _upgradeBuilding;
        private readonly ILaunchExpeditionUseCase _launchExpedition;
        private readonly IBroadcastRecruitmentUseCase _broadcastRecruitment;
        private readonly IRecruitSurvivorUseCase _recruitSurvivor;
        private readonly ISkipRecruitmentCandidatesUseCase _skipRecruitmentCandidates;
        private readonly IRepairItemUseCase _repairItem;
        private readonly IEquipItemUseCase _equipItem;
        private readonly IUseMedicineUseCase _useMedicine;
        private readonly IStartEmergencyScavengeUseCase _startEmergencyScavenge;
        private readonly ISetAutosaveUseCase _setAutosave;
        private readonly ISaveLoadUseCase _saveLoad;
        private readonly HashSet<string> _knownFinishedExpeditionIds = new HashSet<string>(StringComparer.Ordinal);
        private IDisposable _stateSubscription;
        private double _accumulator;
        private bool _expeditionTrackingInitialized;
        private bool _isMarkingOfflineReportPresented;
        private bool _isUpgrading;
        private bool _isLaunching;
        private bool _isRecruiting;
        private bool _isRepairing;
        private bool _isEquipping;
        private bool _isUsingMedicine;
        private bool _isStartingEmergencyScavenge;
        private bool _isSettingAutosave;
        private bool _isManualSaving;

        public CampHudPresenter(
            IUiRootView view,
            IGameStateReader reader,
            IGameStateWriter writer,
            IGameConfigProvider configs,
            IUpgradeBuildingUseCase upgradeBuilding,
            ILaunchExpeditionUseCase launchExpedition,
            IBroadcastRecruitmentUseCase broadcastRecruitment,
            IRecruitSurvivorUseCase recruitSurvivor,
            ISkipRecruitmentCandidatesUseCase skipRecruitmentCandidates,
            IRepairItemUseCase repairItem,
            IEquipItemUseCase equipItem,
            IUseMedicineUseCase useMedicine,
            IStartEmergencyScavengeUseCase startEmergencyScavenge,
            ISetAutosaveUseCase setAutosave,
            ISaveLoadUseCase saveLoad)
        {
            _view = view;
            _reader = reader;
            _writer = writer;
            _configs = configs;
            _upgradeBuilding = upgradeBuilding;
            _launchExpedition = launchExpedition;
            _broadcastRecruitment = broadcastRecruitment;
            _recruitSurvivor = recruitSurvivor;
            _skipRecruitmentCandidates = skipRecruitmentCandidates;
            _repairItem = repairItem;
            _equipItem = equipItem;
            _useMedicine = useMedicine;
            _startEmergencyScavenge = startEmergencyScavenge;
            _setAutosave = setAutosave;
            _saveLoad = saveLoad;
        }

        public void Start()
        {
            _view.UpgradeRequested += OnUpgradeRequested;
            _view.ExpeditionLaunchRequested += OnExpeditionLaunchRequested;
            _view.BroadcastRecruitmentRequested += OnBroadcastRecruitmentRequested;
            _view.RecruitRequested += OnRecruitRequested;
            _view.RecruitmentCandidatesSkipRequested += OnRecruitmentCandidatesSkipRequested;
            _view.RepairItemRequested += OnRepairItemRequested;
            _view.EquipItemRequested += OnEquipItemRequested;
            _view.UseMedicineRequested += OnUseMedicineRequested;
            _view.EmergencyScavengeRequested += OnEmergencyScavengeRequested;
            _view.AutosaveChanged += OnAutosaveChanged;
            _view.ManualSaveRequested += OnManualSaveRequested;
            _stateSubscription = _reader.State.Subscribe(_ => RenderCurrent());
            RenderCurrent();
        }

        public void Tick()
        {
            if (_configs.Current == null) return;
            _accumulator += Time.unscaledDeltaTime;
            if (_accumulator < 0.2f) return;
            _accumulator = 0;
            RenderCurrent();
        }

        public void Dispose()
        {
            _view.UpgradeRequested -= OnUpgradeRequested;
            _view.ExpeditionLaunchRequested -= OnExpeditionLaunchRequested;
            _view.BroadcastRecruitmentRequested -= OnBroadcastRecruitmentRequested;
            _view.RecruitRequested -= OnRecruitRequested;
            _view.RecruitmentCandidatesSkipRequested -= OnRecruitmentCandidatesSkipRequested;
            _view.RepairItemRequested -= OnRepairItemRequested;
            _view.EquipItemRequested -= OnEquipItemRequested;
            _view.UseMedicineRequested -= OnUseMedicineRequested;
            _view.EmergencyScavengeRequested -= OnEmergencyScavengeRequested;
            _view.AutosaveChanged -= OnAutosaveChanged;
            _view.ManualSaveRequested -= OnManualSaveRequested;
            _stateSubscription?.Dispose();
        }

        private void OnUpgradeRequested(string buildingId)
        {
            if (_isUpgrading) return;
            UpgradeAsync(buildingId).Forget();
        }

        private void OnExpeditionLaunchRequested(ExpeditionLaunchViewRequest request)
        {
            if (_isLaunching || request == null) return;
            LaunchExpeditionAsync(request).Forget();
        }

        private void OnRecruitRequested(RecruitSurvivorViewRequest request)
        {
            if (_isRecruiting || request == null) return;
            RecruitAsync(request).Forget();
        }

        private void OnBroadcastRecruitmentRequested()
        {
            if (_isRecruiting) return;
            BroadcastRecruitmentAsync().Forget();
        }

        private void OnRecruitmentCandidatesSkipRequested()
        {
            if (_isRecruiting) return;
            SkipRecruitmentCandidatesAsync().Forget();
        }

        private void OnRepairItemRequested(RepairItemRequest request)
        {
            if (_isRepairing || request == null) return;
            RepairItemAsync(request).Forget();
        }

        private void OnEquipItemRequested(EquipItemRequest request)
        {
            if (_isEquipping || request == null) return;
            EquipItemAsync(request).Forget();
        }

        private void OnUseMedicineRequested(UseMedicineRequest request)
        {
            if (_isUsingMedicine || request == null) return;
            UseMedicineAsync(request).Forget();
        }

        private void OnEmergencyScavengeRequested()
        {
            if (_isStartingEmergencyScavenge) return;
            StartEmergencyScavengeAsync().Forget();
        }

        private void OnAutosaveChanged(bool enabled)
        {
            if (_isSettingAutosave) return;
            SetAutosaveAsync(enabled).Forget();
        }

        private void OnManualSaveRequested()
        {
            if (_isManualSaving) return;
            ManualSaveAsync().Forget();
        }

        private async UniTaskVoid UpgradeAsync(string buildingId)
        {
            _isUpgrading = true;
            try
            {
                var result = await _upgradeBuilding.ExecuteAsync(buildingId, CancellationToken.None);
                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Building upgrade blocked: " + string.Join(", ", result.Validation.Errors));
                    ShowBlockedToast(result.Validation);
                }
                else if (result.Building != null)
                {
                    await SaveCriticalProgressAsync();
                    ShowToast(
                        CampToastIds.BuildingUpgraded,
                        ResolveBuildingName(result.Building.Id),
                        result.Building.Level.ToString(CultureInfo.InvariantCulture));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isUpgrading = false;
            }
        }

        private async UniTaskVoid LaunchExpeditionAsync(ExpeditionLaunchViewRequest request)
        {
            _isLaunching = true;
            try
            {
                if (_configs.Current == null) return;

                var state = _reader.State.CurrentValue;
                var survivorIds = request.SurvivorIds.Count > 0
                    ? new System.Collections.Generic.List<string>(request.SurvivorIds)
                    : SelectIdleSurvivors(state);
                if (survivorIds.Count == 0)
                {
                    Debug.LogWarning("Expedition launch blocked: no idle survivors.");
                    ShowToast(CampToastIds.NoIdleSurvivors);
                    return;
                }

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var result = await _launchExpedition.ExecuteAsync(new LaunchExpeditionRequest
                {
                    ZoneId = request.ZoneId,
                    PolicyId = ResolvePolicyId(request.PolicyId, _configs.Current),
                    SurvivorIds = survivorIds,
                    Seed = CreateLaunchSeed(state, now),
                    NowUnixMs = now,
                    ConfirmWarnings = request.ConfirmWarnings
                }, CancellationToken.None);

                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Expedition launch blocked: " + string.Join(", ", result.Validation.Errors));
                    ShowBlockedToast(result.Validation);
                }
                else if (result.Expedition != null)
                {
                    await SaveCriticalProgressAsync();
                    ShowToast(CampToastIds.ExpeditionLaunched, ResolveZoneName(result.Expedition.ZoneId));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isLaunching = false;
            }
        }

        private async UniTaskVoid BroadcastRecruitmentAsync()
        {
            _isRecruiting = true;
            try
            {
                if (_configs.Current == null) return;

                var state = _reader.State.CurrentValue;
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var result = await _broadcastRecruitment.ExecuteAsync(new BroadcastRecruitmentRequest
                {
                    Seed = CreateLaunchSeed(state, now),
                    NowUnixMs = now
                }, CancellationToken.None);

                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Recruitment broadcast blocked: " + string.Join(", ", result.Validation.Errors));
                    ShowBlockedToast(result.Validation);
                }
                else
                {
                    await SaveCriticalProgressAsync();
                    ShowToast(CampToastIds.RecruitmentBroadcast, result.CandidateIds.Count.ToString(CultureInfo.InvariantCulture));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isRecruiting = false;
            }
        }

        private async UniTaskVoid SkipRecruitmentCandidatesAsync()
        {
            _isRecruiting = true;
            try
            {
                var result = await _skipRecruitmentCandidates.ExecuteAsync(CancellationToken.None);
                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Recruitment skip blocked: " + string.Join(", ", result.Validation.Errors));
                    ShowBlockedToast(result.Validation);
                }
                else
                {
                    await SaveCriticalProgressAsync();
                    ShowToast(CampToastIds.RecruitmentSkipped);
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isRecruiting = false;
            }
        }

        private async UniTaskVoid RecruitAsync(RecruitSurvivorViewRequest request)
        {
            _isRecruiting = true;
            try
            {
                if (_configs.Current == null) return;

                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var result = await _recruitSurvivor.ExecuteAsync(new RecruitSurvivorRequest
                {
                    CandidateId = request.CandidateId,
                    NowUnixMs = now
                }, CancellationToken.None);

                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Recruitment blocked: " + string.Join(", ", result.Validation.Errors));
                    ShowBlockedToast(result.Validation);
                }
                else if (result.Survivor != null)
                {
                    await SaveCriticalProgressAsync();
                    ShowToast(CampToastIds.SurvivorRecruited, ResolveSurvivorName(result.Survivor));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isRecruiting = false;
            }
        }

        private async UniTaskVoid RepairItemAsync(RepairItemRequest request)
        {
            _isRepairing = true;
            try
            {
                var result = await _repairItem.ExecuteAsync(request, CancellationToken.None);
                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Workshop repair blocked: " + string.Join(", ", result.Validation.Errors));
                    ShowBlockedToast(result.Validation);
                }
                else if (result.Item != null)
                {
                    await SaveCriticalProgressAsync();
                    ShowToast(CampToastIds.ItemRepaired, ResolveItemName(result.Item));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isRepairing = false;
            }
        }

        private async UniTaskVoid EquipItemAsync(EquipItemRequest request)
        {
            _isEquipping = true;
            try
            {
                var result = await _equipItem.ExecuteAsync(request, CancellationToken.None);
                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Workshop equip blocked: " + string.Join(", ", result.Validation.Errors));
                    ShowBlockedToast(result.Validation);
                }
                else if (result.Survivor != null && result.Item != null)
                {
                    await SaveCriticalProgressAsync();
                    ShowToast(CampToastIds.ItemEquipped, ResolveSurvivorName(result.Survivor), ResolveItemName(result.Item));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isEquipping = false;
            }
        }

        private async UniTaskVoid UseMedicineAsync(UseMedicineRequest request)
        {
            _isUsingMedicine = true;
            try
            {
                var result = await _useMedicine.ExecuteAsync(request, CancellationToken.None);
                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Use medicine blocked: " + string.Join(", ", result.Validation.Errors));
                    ShowBlockedToast(result.Validation);
                }
                else if (result.Healed && result.Survivor != null)
                {
                    await SaveCriticalProgressAsync();
                    ShowToast(CampToastIds.MedicineUsed, ResolveSurvivorName(result.Survivor));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isUsingMedicine = false;
            }
        }

        private async UniTaskVoid StartEmergencyScavengeAsync()
        {
            _isStartingEmergencyScavenge = true;
            try
            {
                var now = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
                var result = await _startEmergencyScavenge.ExecuteAsync(new EmergencyScavengeRequest
                {
                    NowUnixMs = now
                }, CancellationToken.None);
                if (!result.Validation.IsValid)
                {
                    Debug.LogWarning("Emergency scavenge blocked: " + string.Join(", ", result.Validation.Errors));
                    ShowBlockedToast(result.Validation);
                }
                else if (result.Started)
                {
                    await SaveCriticalProgressAsync();
                    ShowToast(
                        CampToastIds.EmergencyScavengeStarted,
                        Math.Ceiling(result.DurationSeconds).ToString(CultureInfo.InvariantCulture));
                }

                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isStartingEmergencyScavenge = false;
            }
        }

        private async UniTaskVoid SetAutosaveAsync(bool enabled)
        {
            _isSettingAutosave = true;
            try
            {
                await _setAutosave.ExecuteAsync(enabled, CancellationToken.None);
                ShowToast(enabled ? CampToastIds.AutosaveEnabled : CampToastIds.AutosaveDisabled);
                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isSettingAutosave = false;
            }
        }

        private async UniTaskVoid ManualSaveAsync()
        {
            _isManualSaving = true;
            try
            {
                await _saveLoad.SaveAsync(CancellationToken.None);
                ShowToast(CampToastIds.ManualSaved);
                if (_configs.Current != null)
                {
                    RenderCurrent();
                }
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isManualSaving = false;
            }
        }

        private async UniTask SaveCriticalProgressAsync()
        {
            if (!IsAutosaveEnabled()) return;
            await _saveLoad.SaveAsync(CancellationToken.None);
        }

        private void ShowBlockedToast(ValidationResult validation)
        {
            ShowToast(CampToastIds.ActionBlocked, FormatValidation(validation));
        }

        private void ShowToast(string id, params string[] args)
        {
            _view.ShowToast(new CampToastRequest(id, args));
        }

        private static string FormatValidation(ValidationResult validation)
        {
            if (validation == null || validation.Errors == null) return string.Empty;
            return string.Join("; ", validation.Errors);
        }

        private string ResolveBuildingName(string buildingId)
        {
            var config = _configs.Current;
            if (config != null &&
                !string.IsNullOrWhiteSpace(buildingId) &&
                config.Buildings.TryGetValue(buildingId, out var definition) &&
                !string.IsNullOrWhiteSpace(definition.Name))
            {
                return definition.Name;
            }

            return buildingId ?? string.Empty;
        }

        private string ResolveZoneName(string zoneId)
        {
            var config = _configs.Current;
            if (config != null &&
                !string.IsNullOrWhiteSpace(zoneId) &&
                config.Zones.TryGetValue(zoneId, out var definition) &&
                !string.IsNullOrWhiteSpace(definition.Name))
            {
                return definition.Name;
            }

            return zoneId ?? string.Empty;
        }

        private string ResolveItemName(InventoryItemState item)
        {
            if (item == null) return string.Empty;
            var config = _configs.Current;
            if (config != null &&
                !string.IsNullOrWhiteSpace(item.ItemId) &&
                config.Items.TryGetValue(item.ItemId, out var definition) &&
                !string.IsNullOrWhiteSpace(definition.Name))
            {
                return definition.Name;
            }

            return item.ItemId ?? string.Empty;
        }

        private static string ResolveSurvivorName(SurvivorState survivor)
        {
            if (survivor == null) return string.Empty;
            return string.IsNullOrWhiteSpace(survivor.Name) ? survivor.Id ?? string.Empty : survivor.Name;
        }

        private bool IsAutosaveEnabled()
        {
            var settings = _reader.State.CurrentValue.Settings;
            return settings == null || settings.AutosaveEnabled;
        }

        private static System.Collections.Generic.List<string> SelectIdleSurvivors(AshfallCamp.Domain.GameState state)
        {
            var result = new System.Collections.Generic.List<string>();
            var maxCount = Math.Max(1, state.SquadSize);
            foreach (var survivor in state.Survivors)
            {
                if (result.Count >= maxCount) break;
                if (survivor.State == AshfallCamp.Domain.SurvivorActivityState.Idle)
                {
                    result.Add(survivor.Id);
                }
            }

            return result;
        }

        private static string ResolvePolicyId(string requestedPolicyId, AshfallCamp.Domain.GameConfigSnapshot config)
        {
            if (!string.IsNullOrWhiteSpace(requestedPolicyId) && config.Policies.ContainsKey(requestedPolicyId))
            {
                return requestedPolicyId;
            }

            foreach (var policyId in config.Policies.Keys)
            {
                return policyId;
            }

            return string.Empty;
        }

        private static uint CreateLaunchSeed(AshfallCamp.Domain.GameState state, long nowUnixMs)
        {
            unchecked
            {
                return ((uint)nowUnixMs) ^ ((uint)Math.Max(1, state.NextId) * 2654435761u);
            }
        }

        private void RenderCurrent()
        {
            if (_configs.Current == null) return;
            var state = _reader.State.CurrentValue;
            HandleFinishedExpeditionReports(state);
            HandleOfflineReportPresentation(state);
            _view.Render(state, _configs.Current);
        }

        private void HandleOfflineReportPresentation(GameState state)
        {
            if (!ShouldPresentOfflineReport(state)) return;

            state.LastOfflineReport.WasPresented = true;
            var minutes = Math.Ceiling(state.LastOfflineReport.AppliedSeconds / 60.0).ToString(CultureInfo.InvariantCulture);
            ShowToast(CampToastIds.OfflineReportReady, minutes);
            _view.OpenReports();
            MarkOfflineReportPresentedAsync().Forget();
        }

        private bool ShouldPresentOfflineReport(GameState state)
        {
            if (state == null || state.LastOfflineReport == null || state.LastOfflineReport.WasPresented) return false;
            if (_isMarkingOfflineReportPresented || _configs.Current == null || _configs.Current.Balance == null) return false;

            var minimumSeconds = Math.Max(0, _configs.Current.Balance.OfflineReportMinimumSeconds);
            return state.LastOfflineReport.AppliedSeconds >= minimumSeconds;
        }

        private async UniTaskVoid MarkOfflineReportPresentedAsync()
        {
            if (_isMarkingOfflineReportPresented) return;
            _isMarkingOfflineReportPresented = true;
            try
            {
                await _writer.MutateAsync(state =>
                {
                    if (state.LastOfflineReport != null)
                    {
                        state.LastOfflineReport.WasPresented = true;
                    }

                    return state;
                }, CancellationToken.None);
                await _saveLoad.SaveAsync(CancellationToken.None);
            }
            catch (Exception ex)
            {
                Debug.LogException(ex);
            }
            finally
            {
                _isMarkingOfflineReportPresented = false;
            }
        }

        private void HandleFinishedExpeditionReports(GameState state)
        {
            if (state == null || state.Expeditions == null) return;

            if (!_expeditionTrackingInitialized)
            {
                TrackFinishedExpeditions(state);
                _expeditionTrackingInitialized = true;
                return;
            }

            ExpeditionState latestNewFinished = null;
            for (var i = 0; i < state.Expeditions.Count; i++)
            {
                var expedition = state.Expeditions[i];
                if (expedition == null || string.IsNullOrWhiteSpace(expedition.Id)) continue;
                if (!IsFinishedExpedition(expedition.Status)) continue;

                if (_knownFinishedExpeditionIds.Add(expedition.Id))
                {
                    latestNewFinished = expedition;
                }
            }

            if (latestNewFinished == null) return;

            var toastId = latestNewFinished.Status == ExpeditionStatus.Completed
                ? CampToastIds.ExpeditionCompleted
                : CampToastIds.ExpeditionFailed;
            ShowToast(toastId, ResolveZoneName(latestNewFinished.ZoneId));
            _view.OpenReports();
        }

        private void TrackFinishedExpeditions(GameState state)
        {
            for (var i = 0; i < state.Expeditions.Count; i++)
            {
                var expedition = state.Expeditions[i];
                if (expedition == null || string.IsNullOrWhiteSpace(expedition.Id)) continue;
                if (IsFinishedExpedition(expedition.Status))
                {
                    _knownFinishedExpeditionIds.Add(expedition.Id);
                }
            }
        }

        private static bool IsFinishedExpedition(ExpeditionStatus status)
        {
            return status == ExpeditionStatus.Completed || status == ExpeditionStatus.Failed;
        }
    }
}
