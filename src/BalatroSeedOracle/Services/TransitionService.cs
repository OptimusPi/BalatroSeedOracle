using System;
using Avalonia.Threading;
using BalatroSeedOracle.Helpers;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Manages smooth shader parameter transitions with LERP interpolation.
    /// Supports app startup transitions, search progress transitions, etc.
    /// Single active transition at a time for simplicity.
    /// </summary>
    public class TransitionService
    {
        private VisualizerPresetTransition? _activeTransition;
        private Action<ShaderParameters>? _applyCallback;
        private DispatcherTimer? _updateTimer;
        private bool _isRunning = false;

        /// <summary>
        /// Gets whether a transition is currently active
        /// </summary>
        public bool IsTransitionActive => _isRunning;

        /// <summary>
        /// Gets the current transition progress (0.0 to 1.0), or null if no active transition
        /// </summary>
        public float? CurrentProgress => _activeTransition?.CurrentProgress;

        /// <summary>
        /// Starts a new transition with the given parameters.
        /// If a transition is already running, it will be replaced.
        /// </summary>
        /// <param name="startParams">Starting shader parameters</param>
        /// <param name="endParams">Ending shader parameters</param>
        /// <param name="applyCallback">Callback to apply interpolated parameters</param>
        /// <param name="duration">Optional: Duration for time-based auto-transition</param>
        public void StartTransition(
            ShaderParameters startParams,
            ShaderParameters endParams,
            Action<ShaderParameters> applyCallback,
            TimeSpan? duration = null
        )
        {
            // Stop any existing transition
            StopTransition();

            DebugLogger.LogImportant(
                "TransitionService",
                $"Starting transition (duration: {duration?.TotalSeconds ?? 0:F1}s)"
            );

            _activeTransition = new VisualizerPresetTransition
            {
                StartParameters = startParams,
                EndParameters = endParams,
                CurrentProgress = 0f,
            };

            _applyCallback = applyCallback;
            _isRunning = true;

            // Apply initial state immediately
            ApplyCurrentState();

            // If duration is specified, start time-based auto-transition
            if (duration.HasValue)
            {
                _activeTransition.StartTimeBasedTransition(duration.Value);
                StartUpdateTimer();
            }
        }

        /// <summary>
        /// Manually sets the transition progress (0.0 to 1.0).
        /// Use this for progress-driven transitions (e.g., sprite loading, search progress).
        /// </summary>
        public void SetProgress(float progress)
        {
            if (!_isRunning || _activeTransition == null)
            {
                DebugLogger.LogError("TransitionService", "Cannot set progress - no active transition");
                return;
            }

            _activeTransition.CurrentProgress = Math.Clamp(progress, 0f, 1f);
            ApplyCurrentState();

            // If transition is complete, stop it
            if (_activeTransition.CurrentProgress >= 1.0f)
            {
                DebugLogger.LogImportant("TransitionService", "Transition complete!");
                StopTransition();
            }
        }

        /// <summary>
        /// Stops the current transition
        /// </summary>
        public void StopTransition()
        {
            if (!_isRunning)
                return;

            DebugLogger.Log("TransitionService", "Stopping transition");

            StopUpdateTimer();
            _isRunning = false;
            _activeTransition = null;
            _applyCallback = null;
        }

        /// <summary>
        /// Applies the current interpolated shader parameters via the callback
        /// </summary>
        private void ApplyCurrentState()
        {
            if (_activeTransition == null || _applyCallback == null)
                return;

            var interpolated = _activeTransition.GetInterpolatedParameters();
            _applyCallback(interpolated);
        }

        /// <summary>
        /// Starts a timer for time-based auto-transitions (16ms = ~60fps)
        /// </summary>
        private void StartUpdateTimer()
        {
            _updateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(16), // ~60fps
            };

            _updateTimer.Tick += (s, e) =>
            {
                if (_activeTransition == null)
                {
                    StopUpdateTimer();
                    return;
                }

                // Update progress based on elapsed time
                _activeTransition.UpdateProgressFromElapsedTime();
                ApplyCurrentState();

                // Stop when complete
                if (_activeTransition.CurrentProgress >= 1.0f)
                {
                    DebugLogger.LogImportant("TransitionService", "Time-based transition complete!");
                    StopTransition();
                }
            };

            _updateTimer.Start();
        }

        /// <summary>
        /// Stops the update timer
        /// </summary>
        private void StopUpdateTimer()
        {
            if (_updateTimer != null)
            {
                _updateTimer.Stop();
                _updateTimer = null;
            }
        }
    }
}
