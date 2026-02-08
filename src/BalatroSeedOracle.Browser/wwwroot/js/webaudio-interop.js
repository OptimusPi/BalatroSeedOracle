// Web Audio API Interop for Avalonia Browser
// Provides multi-track audio playback, volume control, and FFT analysis using Web Audio API

window.WebAudioManager = {
    audioContext: null,
    tracks: new Map(),
    sfxTracks: new Map(),
    analyzers: new Map(),
    masterGain: null,
    isInitialized: false,

    // Initialize Web Audio API context
    initialize: async function() {
        if (this.isInitialized && this.audioContext) {
            return true;
        }

        try {
            // Create AudioContext (use legacy constructor for Safari compatibility)
            this.audioContext = new (window.AudioContext || window.webkitAudioContext)();
            
            // Create master gain node for volume control
            this.masterGain = this.audioContext.createGain();
            this.masterGain.gain.value = 0; // Start muted
            this.masterGain.connect(this.audioContext.destination);

            // Resume context if suspended (required for user interaction)
            if (this.audioContext.state === 'suspended') {
                await this.audioContext.resume();
            }

            this.isInitialized = true;
            console.log('Web Audio API initialized successfully');
            return true;
        } catch (error) {
            console.error('Failed to initialize Web Audio API:', error);
            throw error;
        }
    },

    // Load and play an audio track
    loadTrack: async function(trackName, audioUrl, loop = true) {
        if (!this.isInitialized) {
            await this.initialize();
        }

        try {
            // Fetch audio file
            const response = await fetch(audioUrl);
            if (!response.ok) {
                console.warn(`Audio file not found: ${audioUrl} (${response.status})`);
                return false;
            }
            const arrayBuffer = await response.arrayBuffer();
            if (arrayBuffer.byteLength === 0) {
                console.warn(`Audio file empty: ${audioUrl}`);
                return false;
            }
            const audioBuffer = await this.audioContext.decodeAudioData(arrayBuffer);

            // Create buffer source
            const source = this.audioContext.createBufferSource();
            source.buffer = audioBuffer;
            source.loop = loop;

            // Create gain node for track volume
            const gainNode = this.audioContext.createGain();
            gainNode.gain.value = 1.0;

            // Create analyser for FFT
            const analyser = this.audioContext.createAnalyser();
            analyser.fftSize = 2048;
            analyser.smoothingTimeConstant = 0.8;

            // Connect: source -> gain -> analyser -> master gain -> destination
            source.connect(gainNode);
            gainNode.connect(analyser);
            analyser.connect(this.masterGain);

            // Start playback
            source.start(0);

            // Store track info
            this.tracks.set(trackName, {
                source: source,
                gain: gainNode,
                analyser: analyser,
                buffer: audioBuffer,
                isPlaying: true
            });
            this.analyzers.set(trackName, analyser);

            console.log(`Loaded and playing track: ${trackName}`);
            return true;
        } catch (error) {
            console.error(`Error loading track ${trackName}:`, error);
            throw error;
        }
    },

    // Load sound effect (one-shot, no loop)
    loadSfx: async function(sfxName, audioUrl) {
        if (!this.isInitialized) {
            await this.initialize();
        }

        try {
            const response = await fetch(audioUrl);
            if (!response.ok) {
                console.warn(`SFX file not found: ${audioUrl} (${response.status})`);
                return false;
            }
            const arrayBuffer = await response.arrayBuffer();
            if (arrayBuffer.byteLength === 0) {
                console.warn(`SFX file empty: ${audioUrl}`);
                return false;
            }
            const audioBuffer = await this.audioContext.decodeAudioData(arrayBuffer);

            this.sfxTracks.set(sfxName, audioBuffer);
            console.log(`Loaded SFX: ${sfxName}`);
            return true;
        } catch (error) {
            console.error(`Error loading SFX ${sfxName}:`, error);
            throw error;
        }
    },

    // Play sound effect
    playSfx: function(sfxName, volume = 1.0) {
        if (!this.isInitialized || !this.sfxTracks.has(sfxName)) {
            return false;
        }

        try {
            const audioBuffer = this.sfxTracks.get(sfxName);
            const source = this.audioContext.createBufferSource();
            source.buffer = audioBuffer;

            const gainNode = this.audioContext.createGain();
            gainNode.gain.value = volume;

            source.connect(gainNode);
            gainNode.connect(this.audioContext.destination);

            source.start(0);
            return true;
        } catch (error) {
            console.error(`Error playing SFX ${sfxName}:`, error);
            return false;
        }
    },

    // Set track volume
    setTrackVolume: function(trackName, volume) {
        const track = this.tracks.get(trackName);
        if (track && track.gain) {
            track.gain.gain.value = Math.max(0, Math.min(1, volume));
            return true;
        }
        return false;
    },

    // Set master volume
    setMasterVolume: function(volume) {
        if (this.masterGain) {
            this.masterGain.gain.value = Math.max(0, Math.min(1, volume));
            return true;
        }
        return false;
    },

    // Mute/unmute track
    setTrackMuted: function(trackName, muted) {
        const track = this.tracks.get(trackName);
        if (track && track.gain) {
            track.gain.gain.value = muted ? 0 : 1;
            return true;
        }
        return false;
    },

    // Get FFT data for a track
    getFrequencyData: function(trackName) {
        const analyser = this.analyzers.get(trackName);
        if (!analyser) {
            return null;
        }

        const bufferLength = analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);
        analyser.getByteFrequencyData(dataArray);

        // Convert to normalized float array (0-1)
        const normalized = new Float32Array(bufferLength);
        for (let i = 0; i < bufferLength; i++) {
            normalized[i] = dataArray[i] / 255.0;
        }

        return Array.from(normalized);
    },

    // Get frequency bands (Bass: 20-250Hz, Mid: 250-2000Hz, High: 2000-20000Hz)
    // Sample rate is typically 44100Hz, so:
    // - Bass: indices 0-12 (approx)
    // - Mid: indices 13-102 (approx)
    // - High: indices 103-1024 (approx)
    getFrequencyBands: function(trackName) {
        const analyser = this.analyzers.get(trackName);
        if (!analyser) {
            return { bassAvg: 0, bassPeak: 0, midAvg: 0, midPeak: 0, highAvg: 0, highPeak: 0 };
        }

        const bufferLength = analyser.frequencyBinCount;
        const dataArray = new Uint8Array(bufferLength);
        analyser.getByteFrequencyData(dataArray);

        const sampleRate = this.audioContext.sampleRate;
        const nyquist = sampleRate / 2;
        const freqPerBin = nyquist / bufferLength;

        // Calculate band ranges
        const bassEnd = Math.floor(250 / freqPerBin);
        const midEnd = Math.floor(2000 / freqPerBin);

        // Calculate averages and peaks
        let bassSum = 0, bassPeak = 0;
        let midSum = 0, midPeak = 0;
        let highSum = 0, highPeak = 0;

        for (let i = 0; i < bufferLength; i++) {
            const value = dataArray[i] / 255.0;
            if (i < bassEnd) {
                bassSum += value;
                bassPeak = Math.max(bassPeak, value);
            } else if (i < midEnd) {
                midSum += value;
                midPeak = Math.max(midPeak, value);
            } else {
                highSum += value;
                highPeak = Math.max(highPeak, value);
            }
        }

        return {
            bassAvg: bassSum / bassEnd,
            bassPeak: bassPeak,
            midAvg: midSum / (midEnd - bassEnd),
            midPeak: midPeak,
            highAvg: highSum / (bufferLength - midEnd),
            highPeak: highPeak
        };
    },

    // Pause all tracks
    pause: function() {
        if (this.audioContext && this.audioContext.state === 'running') {
            this.audioContext.suspend();
            return true;
        }
        return false;
    },

    // Resume all tracks
    resume: async function() {
        if (this.audioContext && this.audioContext.state === 'suspended') {
            await this.audioContext.resume();
            return true;
        }
        return false;
    },

    // Dispose/cleanup
    dispose: function() {
        this.tracks.forEach(track => {
            try {
                if (track.source) {
                    track.source.stop();
                }
            } catch (e) {
                // Ignore errors when stopping
            }
        });
        this.tracks.clear();
        this.sfxTracks.clear();
        this.analyzers.clear();

        if (this.audioContext) {
            this.audioContext.close();
            this.audioContext = null;
        }

        this.isInitialized = false;
    }
};
