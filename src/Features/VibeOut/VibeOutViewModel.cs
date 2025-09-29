using System;
using System.Collections.ObjectModel;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using BalatroSeedOracle.Services;
using BalatroSeedOracle.Helpers;
using static BalatroSeedOracle.Services.VibeAudioManager;

namespace BalatroSeedOracle.Features.VibeOut
{
    public partial class VibeOutViewModel : ObservableObject
    {
        // Core vibe system
        private VibeAudioManager? _audioManager;
        private int _vibeIntensity = 0; // 0-3 scale
        private readonly Random _random = new();
        private readonly DispatcherTimer _animationTimer;
        private readonly DispatcherTimer _vibeUpdateTimer;
        
        // Matrix rain data
        public ObservableCollection<FallingSeed> MatrixSeeds { get; } = new();
        public ObservableCollection<SeedPill> Pills { get; } = new();
        public ObservableCollection<string> RecentSeeds { get; } = new();
        
        // Vibe intensity tracking
        private int _beatCounter = 0;
        private DateTime _lastGoodSeed = DateTime.MinValue;
        private DateTime _searchStartTime = DateTime.UtcNow;
        
        [ObservableProperty]
        private bool _isVibing;
        
        [ObservableProperty]
        private string _vibeStatus = "Ready to vibe";
        
        [ObservableProperty]
        private string _currentSeed = "";
        
        [ObservableProperty]
        private float _masterVolume = 0.7f;
        
        [ObservableProperty]
        private string _audioState = "MainMenu";
        
        [ObservableProperty] 
        private float _audioBass = 0f;
        
        [ObservableProperty]
        private float _audioMid = 0f;
        
        [ObservableProperty]
        private float _audioTreble = 0f;
        
        [ObservableProperty]
        private float _audioPeak = 0f;

        public VibeOutViewModel()
        {
            // Animation timer for visual effects
            _animationTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(50) // 20 FPS for smooth visuals
            };
            _animationTimer.Tick += OnAnimationTick;
            
            // Vibe intensity update timer
            _vibeUpdateTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromSeconds(2) // Check vibe intensity every 2 seconds
            };
            _vibeUpdateTimer.Tick += OnVibeUpdateTick;
            
            // Subscribe to volume changes
            PropertyChanged += (s, e) =>
            {
                if (e.PropertyName == nameof(MasterVolume) && _audioManager != null)
                {
                    _audioManager.SetMasterVolume(MasterVolume);
                }
            };
        }
        
        [RelayCommand]
        public void StartVibing()
        {
            if (IsVibing) return;
            
            try
            {
                IsVibing = true;
                VibeStatus = "🎵 VIBING INITIATED 🎵";
                _searchStartTime = DateTime.UtcNow;
                
                // Initialize the epic audio system
                _audioManager = new VibeAudioManager();
                _audioManager.SetMasterVolume(MasterVolume);
                
                // Subscribe to audio events
                _audioManager.AudioAnalysisUpdated += OnAudioAnalysisUpdated;
                _audioManager.BeatDetected += OnBeatDetected;
                
                // Start in search mode
                _audioManager.OnSearchStarted();
                AudioState = "VibeLevel1";
                
                // Start animation timers
                _animationTimer.Start();
                _vibeUpdateTimer.Start();
                
                DebugLogger.Log("VibeOut", "🎵 VIBE OUT MODE ACTIVATED! 🎵");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VibeOut", $"Failed to start vibing: {ex.Message}");
                VibeStatus = "Vibe failed to start (audio error)";
            }
        }
        
        [RelayCommand]
        public void StopVibing()
        {
            IsVibing = false;
            VibeStatus = "Vibe session ended";
            
            _animationTimer.Stop();
            _vibeUpdateTimer.Stop();
            
            // Clean up audio
            if (_audioManager != null)
            {
                _audioManager.AudioAnalysisUpdated -= OnAudioAnalysisUpdated;
                _audioManager.BeatDetected -= OnBeatDetected;
                _audioManager.Dispose();
                _audioManager = null;
            }
            
            DebugLogger.Log("VibeOut", "👋 Vibe session ended");
        }
        
        private void OnAudioAnalysisUpdated(float bass, float mid, float treble, float peak)
        {
            // Update UI properties for binding
            Dispatcher.UIThread.Post(() =>
            {
                AudioBass = bass;
                AudioMid = mid;
                AudioTreble = treble;
                AudioPeak = peak;
            });
        }
        
        private void OnBeatDetected(float beatIntensity)
        {
            // Spawn visual effect on beat
            Dispatcher.UIThread.Post(() =>
            {
                // Every few beats, add a special effect
                if (_random.NextSingle() < beatIntensity * 0.3f)
                {
                    AddBeatVisualEffect();
                }
            });
        }
        
        private void AddBeatVisualEffect()
        {
            // Add a beat-synchronized falling seed
            var beatSeed = new FallingSeed
            {
                Text = "♪",
                X = _random.Next(50, 1870),
                Y = -20,
                Speed = 3 + AudioBass * 5,
                Opacity = 0.8,
                IsBeatEffect = true
            };
            
            MatrixSeeds.Add(beatSeed);
        }
        
        private void OnVibeUpdateTick(object? sender, EventArgs e)
        {
            // Calculate current vibe intensity based on search activity
            UpdateVibeIntensity();
        }
        
        private void UpdateVibeIntensity()
        {
            var searchDuration = DateTime.UtcNow - _searchStartTime;
            var timeSinceLastGoodSeed = DateTime.UtcNow - _lastGoodSeed;
            
            int newVibeIntensity = 0;
            
            // Award vibe points for various conditions
            if (searchDuration.TotalMinutes > 1) newVibeIntensity++;      // Sustained search
            if (RecentSeeds.Count > 5) newVibeIntensity++;               // Finding seeds
            if (timeSinceLastGoodSeed.TotalSeconds < 30) newVibeIntensity++; // Recent good seed
            
            // Transition audio state based on vibe intensity
            if (newVibeIntensity != _vibeIntensity)
            {
                _vibeIntensity = newVibeIntensity;
                TransitionAudioState();
                UpdateVibeStatus();
            }
        }
        
        private void TransitionAudioState()
        {
            if (_audioManager == null) return;
            
            var newState = _vibeIntensity switch
            {
                0 => Services.AudioState.VibeLevel1,
                1 => Services.AudioState.VibeLevel1, 
                2 => Services.AudioState.VibeLevel2, // DRUMS2 KICKS IN! 🔥
                3 => Services.AudioState.VibeLevel3, // MAXIMUM VIBE! 🚀
                _ => Services.AudioState.VibeLevel1
            };
            
            _audioManager.TransitionTo(newState);
            AudioState = newState.ToString();
            
            DebugLogger.Log("VibeOut", $"🎵 Audio transition to {newState} (vibe intensity: {_vibeIntensity})");
        }
        
        private void UpdateVibeStatus()
        {
            VibeStatus = _vibeIntensity switch
            {
                0 => "🎵 Chilling with the beats",
                1 => "🎶 Vibe building up...",
                2 => "🔥 SICK BEATS ACTIVATED!",
                3 => "🚀 MAXIMUM VIBE OVERDRIVE!",
                _ => "🎵 Vibing"
            };
        }
        
        public void ProcessSeedResult(string seed, int score, int[]? shouldValues = null)
        {
            Dispatcher.UIThread.Post(() =>
            {
                // Add to recent seeds list
                RecentSeeds.Insert(0, $"{seed} ({score})");
                while (RecentSeeds.Count > 25) // Keep last 25 seeds
                {
                    RecentSeeds.RemoveAt(RecentSeeds.Count - 1);
                }
                
                // Create falling seed with score-based properties
                var fallingSeed = new FallingSeed 
                { 
                    Text = seed,
                    X = _random.Next(50, 1870),
                    Y = -20,
                    Speed = Math.Clamp(2 + score / 10f, 2, 15),
                    Opacity = Math.Clamp(0.5f + score / 100f, 0.5f, 1f),
                    IsHighScore = score > 50
                };
                
                MatrixSeeds.Add(fallingSeed);
                
                // Keep matrix manageable
                while (MatrixSeeds.Count > 150)
                {
                    MatrixSeeds.RemoveAt(0);
                }
                
                // Check for good seed (triggers audio progression)
                if (score > 30)
                {
                    _lastGoodSeed = DateTime.UtcNow;
                    AwardVibeBonus(score);
                }
                
                // Every 16 seeds, spawn interactive pill
                if (++_beatCounter % 16 == 0)
                {
                    SpawnSeedPill(seed, score);
                }
                
                // Celebration for rare seeds
                if (score > 80)
                {
                    TriggerCelebration(score);
                }
                
                CurrentSeed = seed;
            });
        }
        
        private void AwardVibeBonus(int score)
        {
            // Award bonus based on score
            if (score > 50 && _audioManager != null)
            {
                _audioManager.OnGoodSeedFound(score);
                DebugLogger.Log("VibeOut", $"🎵 Good seed bonus! Score: {score}");
            }
        }
        
        private void SpawnSeedPill(string seed, int score)
        {
            var pill = new SeedPill 
            {
                Seed = seed,
                Score = score,
                X = _random.Next(200, 1720),
                Y = -50,
                TargetY = _random.Next(200, 800),
                IsSpecial = score > 60
            };
            
            Pills.Add(pill);
            
            // Keep pills manageable
            while (Pills.Count > 20)
            {
                Pills.RemoveAt(0);
            }
        }
        
        private void TriggerCelebration(int score)
        {
            VibeStatus = $"🎉 EPIC SEED! Score: {score} 🎉";
            
            // Create celebration seeds
            for (int i = 0; i < 5; i++)
            {
                var celebSeed = new FallingSeed
                {
                    Text = "★",
                    X = _random.Next(100, 1820),
                    Y = -10,
                    Speed = 5 + _random.NextSingle() * 3,
                    Opacity = 1f,
                    IsCelebration = true
                };
                MatrixSeeds.Add(celebSeed);
            }
            
            DebugLogger.Log("VibeOut", $"🎉 CELEBRATION! Epic seed with score {score}");
        }
        
        private void OnAnimationTick(object? sender, EventArgs e)
        {
            // Update falling seeds
            for (int i = MatrixSeeds.Count - 1; i >= 0; i--)
            {
                var seed = MatrixSeeds[i];
                seed.Y += seed.Speed;
                
                // Fade out gradually
                seed.Opacity = Math.Max(0, seed.Opacity - 0.003f);
                
                // Remove if off screen or faded
                if (seed.Y > 1080 || seed.Opacity <= 0)
                {
                    MatrixSeeds.RemoveAt(i);
                }
            }
            
            // Update pills (smooth movement to target)
            for (int i = Pills.Count - 1; i >= 0; i--)
            {
                var pill = Pills[i];
                
                // Smooth movement towards target
                var deltaY = pill.TargetY - pill.Y;
                pill.Y += deltaY * 0.1; // Smooth easing
                pill.Rotation += 1;
                
                // Remove after lifetime expires
                if (pill.Lifetime++ > 600) // 30 seconds at 50ms intervals
                {
                    Pills.RemoveAt(i);
                }
            }
            
            // Reset celebration status after delay
            if (VibeStatus.Contains("EPIC SEED") && _random.NextSingle() < 0.02f)
            {
                UpdateVibeStatus(); // Return to normal status
            }
        }
        
        [RelayCommand]
        public async Task CopySeed(string seedInfo)
        {
            try
            {
                // Extract just the seed part (before the score)
                var seed = seedInfo.Split(' ')[0];
                await ClipboardService.CopyToClipboardAsync(seed);
                DebugLogger.Log("VibeOut", $"📋 Copied seed: {seed}");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VibeOut", $"Copy failed: {ex.Message}");
            }
        }
        
        [RelayCommand]
        public void TogglePause()
        {
            if (_audioManager?.IsPaused == true)
            {
                _audioManager.Resume();
                DebugLogger.Log("VibeOut", "▶️ Resumed audio");
            }
            else
            {
                _audioManager?.Pause();
                DebugLogger.Log("VibeOut", "⏸️ Paused audio");
            }
        }
        
        [RelayCommand]
        public void IncreaseVolume()
        {
            MasterVolume = Math.Min(1.0f, MasterVolume + 0.1f);
        }
        
        [RelayCommand]
        public void DecreaseVolume()
        {
            MasterVolume = Math.Max(0.0f, MasterVolume - 0.1f);
        }
        
        [RelayCommand]
        public void ClearHistory()
        {
            RecentSeeds.Clear();
            MatrixSeeds.Clear();
            Pills.Clear();
            DebugLogger.Log("VibeOut", "🧹 History cleared");
        }
        
        /// <summary>
        /// Called from SearchModalViewModel to update vibe intensity
        /// </summary>
        public void UpdateIntensity(int intensity)
        {
            _vibeIntensity = Math.Clamp(intensity, 0, 3);
            TransitionAudioState();
            UpdateVibeStatus();
        }
        
        /// <summary>
        /// Award vibe points for achievements
        /// </summary>
        public void AwardVibePoints(int points, string reason)
        {
            _vibeIntensity = Math.Min(3, _vibeIntensity + points);
            DebugLogger.Log("VibeOut", $"🎵 Vibe points +{points}: {reason} (intensity now {_vibeIntensity})");
            TransitionAudioState();
            UpdateVibeStatus();
        }
    }
    
    public class FallingSeed : ObservableObject
    {
        public string Text { get; set; } = "";
        public double X { get; set; }
        public double Y { get; set; }
        public double Speed { get; set; }
        public double Opacity { get; set; }
        public bool IsHighScore { get; set; }
        public bool IsBeatEffect { get; set; }
        public bool IsCelebration { get; set; }
    }
    
    public class SeedPill : ObservableObject
    {
        public string Seed { get; set; } = "";
        public int Score { get; set; }
        public double X { get; set; }
        public double Y { get; set; }
        public double TargetY { get; set; }
        public double Rotation { get; set; }
        public int Lifetime { get; set; }
        public bool IsSpecial { get; set; }
    }
}
