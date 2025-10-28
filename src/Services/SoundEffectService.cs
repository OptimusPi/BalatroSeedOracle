using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Avalonia.Threading;
using NAudio.CoreAudioApi;
using NAudio.Wave;

namespace BalatroSeedOracle.Services
{
    public class SoundEffectService
    {
        private static SoundEffectService? _instance;
        public static SoundEffectService Instance => _instance ??= new SoundEffectService();

        private bool _soundEnabled = true;
        private string? _soundsPath;

        // Map our sound effects to Balatro sound file names
        private readonly Dictionary<string, string> _soundMap = new()
        {
            { "card_hover", "highlight2.ogg" },
            { "card_select", "cardSlide1.ogg" },
            { "card_drop", "cardSlide2.ogg" },
            { "button_click", "button.ogg" },
            { "filter_switch", "card1.ogg" },
            { "modal_open", "timpani.ogg" },
            { "modal_close", "cancel.ogg" },
            { "success", "chips2.ogg" },
            { "error", "error.ogg" },
            { "coin", "coin3.ogg" },
            { "whoosh", "whoosh1.ogg" },
        };

        public bool SoundEnabled
        {
            get => _soundEnabled;
            set => _soundEnabled = value;
        }

        public void SetSoundsPath(string path)
        {
            if (Directory.Exists(path))
            {
                _soundsPath = path;
            }
        }

        public void PlayCardHover()
        {
            PlaySound("card_hover");
        }

        public void PlayCardSelect()
        {
            PlaySound("card_select");
        }

        public void PlayCardDrop()
        {
            PlaySound("card_drop");
        }

        public void PlayButtonClick()
        {
            PlaySound("button_click");
        }

        public void PlayFilterSwitch()
        {
            PlaySound("filter_switch");
        }

        public void PlayModalOpen()
        {
            PlaySound("modal_open");
        }

        public void PlayModalClose()
        {
            PlaySound("modal_close");
        }

        public void PlaySuccess()
        {
            PlaySound("success");
        }

        public void PlayError()
        {
            PlaySound("error");
        }

        public void PlayWhoosh()
        {
            PlaySound("whoosh");
        }

        private void PlaySound(string soundName)
        {
            if (!_soundEnabled)
                return;

            Dispatcher.UIThread.Post(() =>
            {
                // Try to get the mapped sound file
                if (_soundMap.TryGetValue(soundName, out var soundFile))
                {
                    // Check if we have a local sounds path first (from external/Balatro)
                    var baseDir = AppDomain.CurrentDomain.BaseDirectory;
                    var localSoundPath = Path.Combine(
                        baseDir,
                        "external",
                        "Balatro",
                        "resources",
                        "sounds",
                        soundFile
                    );

                    // Try absolute path if relative doesn't exist
                    if (!File.Exists(localSoundPath))
                    {
                        localSoundPath = Path.Combine(
                            @"X:\BalatroSeedOracle",
                            "external",
                            "Balatro",
                            "resources",
                            "sounds",
                            soundFile
                        );
                    }
                    if (File.Exists(localSoundPath))
                    {
                        PlaySoundFile(localSoundPath);
                    }
                    // Or if user has set a custom sounds path
                    else if (!string.IsNullOrEmpty(_soundsPath))
                    {
                        var customSoundPath = Path.Combine(_soundsPath, soundFile);
                        if (File.Exists(customSoundPath))
                        {
                            PlaySoundFile(customSoundPath);
                        }
                    }
                    else { }
                }
                else { }
            });
        }

        private void PlaySoundFile(string filePath)
        {
            AudioFileReader? reader = null;
            WaveOutEvent? waveOut = null;

            try
            {
                // Fire and forget - play the sound effect without blocking
                reader = new AudioFileReader(filePath) { Volume = 0.3f };
                waveOut = new WaveOutEvent();

                // Capture references in closure to ensure disposal
                var readerToDispose = reader;
                var waveOutToDispose = waveOut;

                waveOut.PlaybackStopped += (s, e) =>
                {
                    waveOutToDispose?.Dispose();
                    readerToDispose?.Dispose();
                };

                waveOut.Init(reader);
                waveOut.Play();
            }
            catch
            {
                // Sound playback failed - non-critical
                // Clean up if initialization failed
                reader?.Dispose();
                waveOut?.Dispose();
            }
        }
    }
}
