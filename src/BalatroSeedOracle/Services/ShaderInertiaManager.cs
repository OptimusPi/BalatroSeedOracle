using System;
using System.Collections.Generic;
using BalatroSeedOracle.Models;

namespace BalatroSeedOracle.Services
{
    /// <summary>
    /// Manages physics-based inertia for shader parameters
    /// Provides "alive" feel with velocity and decay for AddInertia mode
    /// </summary>
    public class ShaderInertiaManager
    {
        private readonly Dictionary<string, ShaderParamState> _paramStates = new();

        /// <summary>
        /// Internal state for a shader parameter with inertia
        /// </summary>
        private class ShaderParamState
        {
            public float CurrentValue { get; set; }
            public float Velocity { get; set; }
            public float DecayRate { get; set; } = 0.9f;
        }

        /// <summary>
        /// Update a shader parameter using SetValue mode (instant, direct)
        /// </summary>
        public float UpdateSetValue(
            string paramName,
            float triggerIntensity,
            float multiplier,
            float minValue,
            float maxValue
        )
        {
            float value = triggerIntensity * multiplier;
            return Math.Clamp(value, minValue, maxValue);
        }

        /// <summary>
        /// Update a shader parameter using AddInertia mode (physics-based, smooth)
        /// Each frame:
        /// 1. Add trigger force to velocity (if trigger is active)
        /// 2. Apply velocity to current value
        /// 3. Decay velocity by decay rate
        /// </summary>
        public float UpdateAddInertia(
            string paramName,
            float triggerIntensity,
            float multiplier,
            float decayRate,
            float minValue,
            float maxValue,
            float deltaTime = 0.016f
        ) // Default 60 FPS
        {
            // Get or create state for this parameter
            if (!_paramStates.TryGetValue(paramName, out var state))
            {
                state = new ShaderParamState
                {
                    CurrentValue = 0f,
                    Velocity = 0f,
                    DecayRate = decayRate,
                };
                _paramStates[paramName] = state;
            }

            // Update decay rate (can change from UI)
            state.DecayRate = decayRate;

            // Add force from trigger (impulse)
            // Trigger intensity is typically 0-1, multiply by multiplier for effect strength
            float force = triggerIntensity * multiplier;
            state.Velocity += force;

            // Apply velocity to current value
            state.CurrentValue += state.Velocity * deltaTime * 60f; // Normalize to 60 FPS

            // Decay velocity (friction/drag)
            state.Velocity *= state.DecayRate;

            // Clamp value to min/max range
            state.CurrentValue = Math.Clamp(state.CurrentValue, minValue, maxValue);

            // If velocity is very small and value is close to zero, snap to zero to prevent drift
            if (Math.Abs(state.Velocity) < 0.001f && Math.Abs(state.CurrentValue) < 0.01f)
            {
                state.Velocity = 0f;
                state.CurrentValue = 0f;
            }

            return state.CurrentValue;
        }

        /// <summary>
        /// Reset a shader parameter's inertia state to zero
        /// </summary>
        public void ResetParameter(string paramName)
        {
            if (_paramStates.TryGetValue(paramName, out var state))
            {
                state.CurrentValue = 0f;
                state.Velocity = 0f;
            }
        }

        /// <summary>
        /// Reset all shader parameters to zero
        /// </summary>
        public void ResetAll()
        {
            _paramStates.Clear();
        }

        /// <summary>
        /// Get the current value of a shader parameter (without updating)
        /// </summary>
        public float GetCurrentValue(string paramName)
        {
            return _paramStates.TryGetValue(paramName, out var state) ? state.CurrentValue : 0f;
        }

        /// <summary>
        /// Get the current velocity of a shader parameter
        /// </summary>
        public float GetCurrentVelocity(string paramName)
        {
            return _paramStates.TryGetValue(paramName, out var state) ? state.Velocity : 0f;
        }

        /// <summary>
        /// Manually set the value and velocity of a shader parameter
        /// Useful for testing or direct control
        /// </summary>
        public void SetState(string paramName, float value, float velocity = 0f)
        {
            if (!_paramStates.TryGetValue(paramName, out var state))
            {
                state = new ShaderParamState();
                _paramStates[paramName] = state;
            }

            state.CurrentValue = value;
            state.Velocity = velocity;
        }
    }
}
