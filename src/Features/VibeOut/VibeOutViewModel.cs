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

        [ObservableProperty]
        private int _shaderType = 0; // 0=Balatro, 1=Psychedelic

        [ObservableProperty]
        private string _shaderName = "Balatro";

        // ============================================
        // 8-TRACK VOLUME CONTROLS (PROPER MVVM WAY)
        // ============================================
        [ObservableProperty]
        private float _drums1Volume = 1.0f;

        [ObservableProperty]
        private float _drums2Volume = 1.0f;

        [ObservableProperty]
        private float _bass1Volume = 1.0f;

        [ObservableProperty]
        private float _bass2Volume = 1.0f;

        [ObservableProperty]
        private float _chords1Volume = 1.0f;

        [ObservableProperty]
        private float _chords2Volume = 1.0f;

        [ObservableProperty]
        private float _melody1Volume = 1.0f;

        [ObservableProperty]
        private float _melody2Volume = 1.0f;

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
                else if (e.PropertyName == nameof(Drums1Volume)) OnVolumeChanged("Drums1", Drums1Volume, 0.0f);
                else if (e.PropertyName == nameof(Drums2Volume)) OnVolumeChanged("Drums2", Drums2Volume, 0.0f);
                else if (e.PropertyName == nameof(Bass1Volume)) OnVolumeChanged("Bass1", Bass1Volume, -0.3f);
                else if (e.PropertyName == nameof(Bass2Volume)) OnVolumeChanged("Bass2", Bass2Volume, 0.1f);
                else if (e.PropertyName == nameof(Chords1Volume)) OnVolumeChanged("Chords1", Chords1Volume, -0.15f);
                else if (e.PropertyName == nameof(Chords2Volume)) OnVolumeChanged("Chords2", Chords2Volume, 0.05f);
                else if (e.PropertyName == nameof(Melody1Volume)) OnVolumeChanged("Melody1", Melody1Volume, -0.15f);
                else if (e.PropertyName == nameof(Melody2Volume)) OnVolumeChanged("Melody2", Melody2Volume, 0.05f);
            };
        }
        
        /// <summary>
        /// Load saved volume settings from UserProfile
        /// </summary>
        public void LoadVolumeSettings()
        {
            try
            {
                var profileService = ServiceHelper.GetRequiredService<UserProfileService>();
                var settings = profileService.GetProfile().VibeOutSettings;

                Drums1Volume = settings.Drums1Volume;
                Drums2Volume = settings.Drums2Volume;
                Bass1Volume = settings.Bass1Volume;
                Bass2Volume = settings.Bass2Volume;
                Chords1Volume = settings.Chords1Volume;
                Chords2Volume = settings.Chords2Volume;
                Melody1Volume = settings.Melody1Volume;
                Melody2Volume = settings.Melody2Volume;

                DebugLogger.Log("VibeOut", "Loaded volume settings from profile");
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VibeOut", $"Failed to load volume settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Save current volume settings to UserProfile
        /// </summary>
        private void SaveVolumeSettings()
        {
            try
            {
                var profileService = ServiceHelper.GetRequiredService<UserProfileService>();
                var profile = profileService.GetProfile();

                profile.VibeOutSettings.Drums1Volume = Drums1Volume;
                profile.VibeOutSettings.Drums2Volume = Drums2Volume;
                profile.VibeOutSettings.Bass1Volume = Bass1Volume;
                profile.VibeOutSettings.Bass2Volume = Bass2Volume;
                profile.VibeOutSettings.Chords1Volume = Chords1Volume;
                profile.VibeOutSettings.Chords2Volume = Chords2Volume;
                profile.VibeOutSettings.Melody1Volume = Melody1Volume;
                profile.VibeOutSettings.Melody2Volume = Melody2Volume;

                profileService.SaveProfile();
            }
            catch (Exception ex)
            {
                DebugLogger.LogError("VibeOut", $"Failed to save volume settings: {ex.Message}");
            }
        }

        /// <summary>
        /// Called when any track volume changes (from slider)
        /// </summary>
        private void OnVolumeChanged(string trackName, float volume, float pan)
        {
            if (_audioManager != null)
            {
                _audioManager.SetTrackVolume(trackName, volume, pan);
            }
            SaveVolumeSettings();
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

                // Get the singleton audio manager from DI (DON'T create a new one!)
                _audioManager = ServiceHelper.GetRequiredService<VibeAudioManager>();
                _audioManager.SetMasterVolume(MasterVolume);

                // Load saved volume settings and apply to audio manager
                LoadVolumeSettings();

                // Subscribe to audio events
                _audioManager.AudioAnalysisUpdated += OnAudioAnalysisUpdated;
                _audioManager.BeatDetected += OnBeatDetected;

                AudioState = "Manual Mix";

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
            // Audio transitions removed - user controls volumes manually via sliders!
            // Vibe intensity still affects visual status only
            AudioState = $"Manual Mix (Intensity: {_vibeIntensity})";
            DebugLogger.Log("VibeOut", $"🎵 Vibe intensity changed to {_vibeIntensity} (audio controlled by sliders)");
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
                
                // Track last good seed for vibe intensity
                if (score > 30)
                {
                    _lastGoodSeed = DateTime.UtcNow;
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

        [RelayCommand]
        public void ToggleShader()
        {
            ShaderType = (ShaderType + 1) % 2; // Toggle between 0 (Balatro) and 1 (Psychedelic)
            ShaderName = ShaderType == 0 ? "Balatro" : "Psychedelic";
            VibeStatus = $"Shader: {ShaderName}";
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
