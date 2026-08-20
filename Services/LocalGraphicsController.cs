using System;
using System.Collections.Generic;
using BepInEx.Logging;
using Maytrix.Menu.Menu;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.XR;

namespace Maytrix.Menu.Services
{
    internal sealed class LocalGraphicsController : IDisposable
    {
        private const float CleanupCooldownSeconds = 60f;
        private readonly MenuSettings _settings;
        private readonly ManualLogSource _logger;
        private readonly List<XRDisplaySubsystem> _displaySubsystems = new List<XRDisplaySubsystem>();

        private LightingSnapshot _lightingBaseline;
        private LightingSnapshot _lastLightingApplied;
        private bool _ownsLighting;
        private float _renderScaleBaseline = 1f;
        private float _lastRenderScaleApplied = 1f;
        private bool _ownsRenderScale;
        private bool _renderScaleAvailable = true;
        private bool _renderScaleWarningLogged;
        private float _requestedRenderScale;
        private float _nextRenderScaleValidationAt = -1f;
        private float _renderScaleValidationDeadline = -1f;
        private bool _validatingBaselineRestore;

        private float _smoothedDelta;
        private float _validSampleSeconds;
        private float _observedPeakFps;
        private float _lowFpsSeconds;
        private float _recoverySeconds;
        private float _warmupUntil;
        private float _lastTransitionAt;
        private float _displayRefreshRate;
        private float _nextDisplayProbeAt;
        private int _automaticPenalty;
        private bool _autoWasEnabled;
        private bool _samplingActive;

        private bool _pendingPerformanceApply;
        private bool _pendingLightingApply;
        private float _pendingApplyAt;
        private int _trackedSceneHandle;
        private float _lastCleanupAt = -CleanupCooldownSeconds;
        private AsyncOperation? _cleanupOperation;

        public LocalGraphicsController(MenuSettings settings, ManualLogSource logger)
        {
            _settings = settings;
            _logger = logger;
            SceneManager.sceneUnloaded += OnSceneUnloaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
            _trackedSceneHandle = SceneManager.GetActiveScene().handle;
            _autoWasEnabled = settings.AutoOptimize.Value;
            RestartSamplingWindow();
            SchedulePersistedOverrides();
        }

        public string FpsLabel => !_samplingActive
            ? "Paused"
            : _validSampleSeconds < 1.5f || _smoothedDelta <= 0f
                ? "Warming up"
                : Mathf.RoundToInt(1f / _smoothedDelta).ToString();

        public string FrameTimeLabel => !_samplingActive || _validSampleSeconds < 1.5f || _smoothedDelta <= 0f
            ? "-- ms"
            : $"{_smoothedDelta * 1000f:0.0} ms";

        public string AutoStatusLabel => !_settings.AutoOptimize.Value
            ? "Off"
            : !_renderScaleAvailable
                ? "Unavailable"
                : _automaticPenalty == 0
                    ? "Watching"
                    : _automaticPenalty == 1
                        ? "Step 1"
                        : "Step 2";

        public string EffectiveProfileLabel
        {
            get
            {
                var level = EffectiveProfileLevel;
                return level == 1 ? "Balanced" : level == 2 ? "Performance" : "Original";
            }
        }

        public string EffectiveGoalLabel => Mathf.RoundToInt(EffectiveOptimizerGoal).ToString();
        public string RenderScaleLabel => !_renderScaleAvailable ? "Unavailable" : $"{ReadRenderScale() * 100f:0}%";
        public bool CanRequestMemoryCleanup => (_cleanupOperation == null || _cleanupOperation.isDone) && Time.realtimeSinceStartup - _lastCleanupAt >= CleanupCooldownSeconds;

        public string CleanupStatusLabel
        {
            get
            {
                if (_cleanupOperation != null && !_cleanupOperation.isDone)
                {
                    return "Working";
                }

                var remaining = CleanupCooldownSeconds - (Time.realtimeSinceStartup - _lastCleanupAt);
                return remaining > 0f ? $"Wait {Mathf.CeilToInt(remaining)}s" : "Ready";
            }
        }

        private int EffectiveProfileLevel => Mathf.Clamp(_settings.QualityProfile.Value + _automaticPenalty, 0, 2);

        private float EffectiveOptimizerGoal
        {
            get
            {
                var ceiling = _displayRefreshRate > 30f ? _displayRefreshRate : EstimateDisplayCeiling();
                return Mathf.Min(_settings.FpsGoal, ceiling);
            }
        }

        public void Tick()
        {
            UpdateCleanupState();
            ProbeDisplayRefreshRate();
            ValidateRenderScaleReadback();

            var canSample = Application.isFocused && Time.timeScale > 0f;
            if (!canSample)
            {
                _samplingActive = false;
                _lowFpsSeconds = 0f;
                _recoverySeconds = 0f;
                return;
            }

            if (!_samplingActive)
            {
                _samplingActive = true;
                RestartSamplingWindow();
            }

            ApplyPendingOverridesIfReady();
            var acceptedDelta = SampleFrameRate();
            UpdateAutomaticOptimization(acceptedDelta);
        }

        public void ApplyPerformanceSettings(bool restartAutomaticTimer = false)
        {
            _pendingPerformanceApply = false;
            if (restartAutomaticTimer)
            {
                _automaticPenalty = 0;
                _autoWasEnabled = _settings.AutoOptimize.Value;
                RestartAutomaticTimer();
            }

            var level = EffectiveProfileLevel;
            if (level == 0)
            {
                RestoreRenderScaleOwned();
                return;
            }

            if (!_renderScaleAvailable)
            {
                return;
            }

            var current = ReadRenderScale();
            if (_ownsRenderScale && !Mathf.Approximately(current, _lastRenderScaleApplied))
            {
                _ownsRenderScale = false;
            }

            if (!_ownsRenderScale)
            {
                _renderScaleBaseline = current;
                _lastRenderScaleApplied = current;
                _ownsRenderScale = true;
            }

            var factor = level == 1 ? 0.90f : 0.80f;
            var floor = Mathf.Min(_renderScaleBaseline, 0.75f);
            var desired = Mathf.Clamp(_renderScaleBaseline * factor, floor, _renderScaleBaseline);
            if (!WriteRenderScale(desired))
            {
                _ownsRenderScale = false;
                return;
            }

            _requestedRenderScale = desired;
            _lastRenderScaleApplied = desired;
            _validatingBaselineRestore = false;
            _nextRenderScaleValidationAt = Time.realtimeSinceStartup + 0.25f;
            _renderScaleValidationDeadline = Time.realtimeSinceStartup + 5f;
        }

        public void ApplyLightingSettings()
        {
            _pendingLightingApply = false;
            var mode = _settings.LightingMode.Value;
            if (mode == 0)
            {
                RestoreLightingOwned();
                return;
            }

            if (_ownsLighting)
            {
                RestoreLightingOwned();
            }

            _lightingBaseline = CaptureLighting();
            var desired = _lightingBaseline;
            if (mode == 1)
            {
                desired.Shadows = ShadowQuality.All;
                desired.ShadowResolution = ShadowResolution.High;
                desired.SoftParticles = true;
                desired.PixelLightCount = Mathf.Max(_lightingBaseline.PixelLightCount, 2);
                desired.AntiAliasing = Mathf.Max(_lightingBaseline.AntiAliasing, 4);
                desired.RealtimeReflectionProbes = true;
            }
            else if (mode == 2)
            {
                desired.Shadows = ShadowQuality.All;
                desired.ShadowResolution = ShadowResolution.Medium;
            }
            else if (mode == 3)
            {
                desired.Shadows = ShadowQuality.HardOnly;
                desired.ShadowResolution = ShadowResolution.Low;
                desired.ShadowDistance = Mathf.Min(_lightingBaseline.ShadowDistance, 20f);
                desired.SoftParticles = false;
                desired.PixelLightCount = Mathf.Min(_lightingBaseline.PixelLightCount, 1);
                desired.AntiAliasing = Mathf.Min(_lightingBaseline.AntiAliasing, 2);
                desired.RealtimeReflectionProbes = false;
            }

            ApplyLighting(desired);
            _lastLightingApplied = CaptureLighting();
            _ownsLighting = true;
        }

        public void ApplyAllSettings(bool restartAutomaticTimer = false)
        {
            ApplyPerformanceSettings(restartAutomaticTimer);
            ApplyLightingSettings();
        }

        public bool RequestMemoryCleanup(out string message)
        {
            if (_cleanupOperation != null && !_cleanupOperation.isDone)
            {
                message = "Cleanup is already running";
                return false;
            }

            var remaining = CleanupCooldownSeconds - (Time.realtimeSinceStartup - _lastCleanupAt);
            if (remaining > 0f)
            {
                message = $"Try again in {Mathf.CeilToInt(remaining)} seconds";
                return false;
            }

            _cleanupOperation = Resources.UnloadUnusedAssets();
            _lastCleanupAt = Time.realtimeSinceStartup;
            message = "Freeing unused assets; a brief hitch is possible";
            _logger.LogInfo("Manual unused-asset cleanup requested.");
            return true;
        }

        public void Dispose()
        {
            SceneManager.sceneUnloaded -= OnSceneUnloaded;
            SceneManager.sceneLoaded -= OnSceneLoaded;
            RestoreRenderScaleOwned(immediate: true);
            RestoreLightingOwned();
            _cleanupOperation = null;
            _displaySubsystems.Clear();
        }

        private float SampleFrameRate()
        {
            var delta = Time.unscaledDeltaTime;
            if (delta <= 0f || delta > 0.20f)
            {
                return 0f;
            }

            if (_smoothedDelta <= 0f)
            {
                _smoothedDelta = delta;
            }
            else
            {
                var alpha = 1f - Mathf.Exp(-delta / 2f);
                _smoothedDelta += (delta - _smoothedDelta) * alpha;
            }

            _validSampleSeconds += delta;
            var fps = 1f / _smoothedDelta;
            if (_validSampleSeconds > 1f && fps <= 240f)
            {
                _observedPeakFps = Mathf.Max(_observedPeakFps, fps);
            }

            return delta;
        }

        private void UpdateAutomaticOptimization(float acceptedDelta)
        {
            var enabled = _settings.AutoOptimize.Value;
            if (enabled != _autoWasEnabled)
            {
                _autoWasEnabled = enabled;
                _automaticPenalty = 0;
                RestartAutomaticTimer();
                ApplyPerformanceSettings();
            }

            if (!enabled || !_renderScaleAvailable || acceptedDelta <= 0f || _validSampleSeconds < 1.5f || Time.realtimeSinceStartup < _warmupUntil || _smoothedDelta <= 0f)
            {
                return;
            }

            var fps = 1f / _smoothedDelta;
            var goal = EffectiveOptimizerGoal;
            var now = Time.realtimeSinceStartup;
            if (fps < goal * 0.82f)
            {
                _lowFpsSeconds += acceptedDelta;
                _recoverySeconds = 0f;
                if (_lowFpsSeconds >= 6f && EffectiveProfileLevel < 2 && _automaticPenalty < 2 && now - _lastTransitionAt >= 15f)
                {
                    _automaticPenalty++;
                    _lastTransitionAt = now;
                    _lowFpsSeconds = 0f;
                    ApplyPerformanceSettings();
                    _logger.LogInfo($"Auto Optimize changed to {AutoStatusLabel}.");
                }
            }
            else if (fps > goal * 0.94f)
            {
                _recoverySeconds += acceptedDelta;
                _lowFpsSeconds = 0f;
                if (_recoverySeconds >= 20f && _automaticPenalty > 0 && now - _lastTransitionAt >= 30f)
                {
                    _automaticPenalty--;
                    _lastTransitionAt = now;
                    _recoverySeconds = 0f;
                    ApplyPerformanceSettings();
                    _logger.LogInfo($"Auto Optimize recovered to {AutoStatusLabel}.");
                }
            }
            else
            {
                _lowFpsSeconds = 0f;
                _recoverySeconds = 0f;
            }
        }

        private void ProbeDisplayRefreshRate()
        {
            if (Time.realtimeSinceStartup < _nextDisplayProbeAt)
            {
                return;
            }

            _nextDisplayProbeAt = Time.realtimeSinceStartup + 5f;
            _displaySubsystems.Clear();
            SubsystemManager.GetSubsystems(_displaySubsystems);
            foreach (var display in _displaySubsystems)
            {
                if (display.running && display.TryGetDisplayRefreshRate(out var refreshRate) && refreshRate > 30f)
                {
                    _displayRefreshRate = refreshRate;
                    return;
                }
            }
        }

        private float EstimateDisplayCeiling()
        {
            if (_validSampleSeconds < 4f)
            {
                return 72f;
            }

            if (_observedPeakFps > 105f) return 120f;
            if (_observedPeakFps > 84f) return 90f;
            if (_observedPeakFps > 76f) return 80f;
            return 72f;
        }

        private void ApplyPendingOverridesIfReady()
        {
            if (Time.realtimeSinceStartup < _pendingApplyAt || _nextRenderScaleValidationAt >= 0f)
            {
                return;
            }

            if (_pendingPerformanceApply)
            {
                ApplyPerformanceSettings(restartAutomaticTimer: true);
            }

            if (_pendingLightingApply)
            {
                ApplyLightingSettings();
            }
        }

        private void SchedulePersistedOverrides()
        {
            _pendingPerformanceApply = _settings.QualityProfile.Value > 0;
            _pendingLightingApply = _settings.LightingMode.Value > 0;
            _pendingApplyAt = Time.realtimeSinceStartup + 1f;
        }

        private void OnSceneUnloaded(Scene scene)
        {
            if (scene.handle != _trackedSceneHandle)
            {
                return;
            }

            RestoreRenderScaleOwned();
            RestoreLightingOwned();
        }

        private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode == LoadSceneMode.Additive)
            {
                RestartSamplingWindow();
                return;
            }

            _trackedSceneHandle = scene.handle;
            _ownsLighting = false;
            _renderScaleAvailable = true;
            _automaticPenalty = 0;
            _smoothedDelta = 0f;
            _validSampleSeconds = 0f;
            _observedPeakFps = 0f;
            RestartSamplingWindow();
            SchedulePersistedOverrides();
        }

        private void RestartSamplingWindow()
        {
            _lowFpsSeconds = 0f;
            _recoverySeconds = 0f;
            _warmupUntil = Time.realtimeSinceStartup + 8f;
            _lastTransitionAt = Time.realtimeSinceStartup;
        }

        private void RestartAutomaticTimer()
        {
            _lowFpsSeconds = 0f;
            _recoverySeconds = 0f;
            _warmupUntil = Time.realtimeSinceStartup + 8f;
            _lastTransitionAt = Time.realtimeSinceStartup;
        }

        private void RestoreRenderScaleOwned(bool immediate = false)
        {
            if (!_ownsRenderScale)
            {
                return;
            }

            var current = ReadRenderScale();
            var validationPending = _nextRenderScaleValidationAt >= 0f;
            if (!validationPending && !Mathf.Approximately(current, _lastRenderScaleApplied))
            {
                _ownsRenderScale = false;
                ClearRenderScaleValidation();
                return;
            }

            if (immediate)
            {
                WriteRenderScale(_renderScaleBaseline);
                _ownsRenderScale = false;
                ClearRenderScaleValidation();
                return;
            }

            if (!WriteRenderScale(_renderScaleBaseline))
            {
                _ownsRenderScale = Mathf.Abs(current - _renderScaleBaseline) > 0.01f;
                ClearRenderScaleValidation();
                return;
            }

            _requestedRenderScale = _renderScaleBaseline;
            _lastRenderScaleApplied = _renderScaleBaseline;
            _validatingBaselineRestore = true;
            _nextRenderScaleValidationAt = Time.realtimeSinceStartup + 0.25f;
            _renderScaleValidationDeadline = Time.realtimeSinceStartup + 3f;
        }

        private void RestoreLightingOwned()
        {
            if (!_ownsLighting)
            {
                return;
            }

            var current = CaptureLighting();
            if (current.Shadows == _lastLightingApplied.Shadows) QualitySettings.shadows = _lightingBaseline.Shadows;
            if (current.ShadowResolution == _lastLightingApplied.ShadowResolution) QualitySettings.shadowResolution = _lightingBaseline.ShadowResolution;
            if (Mathf.Approximately(current.ShadowDistance, _lastLightingApplied.ShadowDistance)) QualitySettings.shadowDistance = _lightingBaseline.ShadowDistance;
            if (current.SoftParticles == _lastLightingApplied.SoftParticles) QualitySettings.softParticles = _lightingBaseline.SoftParticles;
            if (current.PixelLightCount == _lastLightingApplied.PixelLightCount) QualitySettings.pixelLightCount = _lightingBaseline.PixelLightCount;
            if (current.AntiAliasing == _lastLightingApplied.AntiAliasing) QualitySettings.antiAliasing = _lightingBaseline.AntiAliasing;
            if (current.RealtimeReflectionProbes == _lastLightingApplied.RealtimeReflectionProbes) QualitySettings.realtimeReflectionProbes = _lightingBaseline.RealtimeReflectionProbes;
            _ownsLighting = false;
        }

        private static LightingSnapshot CaptureLighting()
        {
            return new LightingSnapshot
            {
                Shadows = QualitySettings.shadows,
                ShadowResolution = QualitySettings.shadowResolution,
                ShadowDistance = QualitySettings.shadowDistance,
                SoftParticles = QualitySettings.softParticles,
                PixelLightCount = QualitySettings.pixelLightCount,
                AntiAliasing = QualitySettings.antiAliasing,
                RealtimeReflectionProbes = QualitySettings.realtimeReflectionProbes
            };
        }

        private static void ApplyLighting(LightingSnapshot snapshot)
        {
            QualitySettings.shadows = snapshot.Shadows;
            QualitySettings.shadowResolution = snapshot.ShadowResolution;
            QualitySettings.shadowDistance = snapshot.ShadowDistance;
            QualitySettings.softParticles = snapshot.SoftParticles;
            QualitySettings.pixelLightCount = snapshot.PixelLightCount;
            QualitySettings.antiAliasing = snapshot.AntiAliasing;
            QualitySettings.realtimeReflectionProbes = snapshot.RealtimeReflectionProbes;
        }

        private void ValidateRenderScaleReadback()
        {
            var now = Time.realtimeSinceStartup;
            if (_nextRenderScaleValidationAt < 0f || now < _nextRenderScaleValidationAt)
            {
                return;
            }

            _nextRenderScaleValidationAt = now + 0.25f;
            var actual = ReadRenderScale();
            if (Mathf.Abs(actual - _requestedRenderScale) <= 0.025f)
            {
                _lastRenderScaleApplied = actual;
                if (_validatingBaselineRestore)
                {
                    _ownsRenderScale = false;
                }
                else
                {
                    _renderScaleAvailable = true;
                }

                ClearRenderScaleValidation();
                return;
            }

            if (now < _renderScaleValidationDeadline)
            {
                return;
            }

            if (!_validatingBaselineRestore && actual < _renderScaleBaseline - 0.01f)
            {
                _lastRenderScaleApplied = actual;
                _renderScaleAvailable = true;
                ClearRenderScaleValidation();
                return;
            }

            _renderScaleAvailable = false;
            if (!_renderScaleWarningLogged)
            {
                _renderScaleWarningLogged = true;
                _logger.LogWarning("The active XR renderer did not accept Maytrix render-scale changes; Auto Optimize is suspended.");
            }

            if (_validatingBaselineRestore)
            {
                _lastRenderScaleApplied = actual;
                _ownsRenderScale = Mathf.Abs(actual - _renderScaleBaseline) > 0.01f;
                ClearRenderScaleValidation();
                return;
            }

            _lastRenderScaleApplied = actual;
            if (!WriteRenderScale(_renderScaleBaseline))
            {
                _ownsRenderScale = Mathf.Abs(actual - _renderScaleBaseline) > 0.01f;
                ClearRenderScaleValidation();
                return;
            }

            _requestedRenderScale = _renderScaleBaseline;
            _lastRenderScaleApplied = _renderScaleBaseline;
            _validatingBaselineRestore = true;
            _nextRenderScaleValidationAt = now + 0.25f;
            _renderScaleValidationDeadline = now + 3f;
        }

        private void ClearRenderScaleValidation()
        {
            _nextRenderScaleValidationAt = -1f;
            _renderScaleValidationDeadline = -1f;
            _validatingBaselineRestore = false;
        }

        private float ReadRenderScale()
        {
            try
            {
                return XRSettings.renderViewportScale;
            }
            catch (Exception exception)
            {
                MarkRenderScaleUnavailable(exception.Message);
                return 1f;
            }
        }

        private bool WriteRenderScale(float value)
        {
            try
            {
                XRSettings.renderViewportScale = Mathf.Clamp01(value);
                return true;
            }
            catch (Exception exception)
            {
                MarkRenderScaleUnavailable(exception.Message);
                return false;
            }
        }

        private void MarkRenderScaleUnavailable(string detail)
        {
            _renderScaleAvailable = false;
            if (_renderScaleWarningLogged)
            {
                return;
            }

            _renderScaleWarningLogged = true;
            _logger.LogWarning($"Render-scale control is unavailable: {detail}");
        }

        private void UpdateCleanupState()
        {
            if (_cleanupOperation != null && _cleanupOperation.isDone)
            {
                _cleanupOperation = null;
                _logger.LogInfo("Unused Unity assets were released.");
            }
        }

        private struct LightingSnapshot
        {
            public ShadowQuality Shadows;
            public ShadowResolution ShadowResolution;
            public float ShadowDistance;
            public bool SoftParticles;
            public int PixelLightCount;
            public int AntiAliasing;
            public bool RealtimeReflectionProbes;
        }
    }
}
