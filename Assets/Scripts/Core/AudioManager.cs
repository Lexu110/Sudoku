using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

namespace SudokuGame
{
    public enum Sfx { Click, Hover, Select, Correct, Wrong, Erase, Victory }

    /// <summary>
    /// Sound effects and background music, all synthesized in code so the project needs no audio files.
    /// To use your own sounds later, replace the clips in the dictionary with AudioClips loaded from the project.
    /// </summary>
    public class AudioManager : MonoBehaviour
    {
        const int SfxRate = 44100;
        const int MusicRate = 22050;
        const string MusicKey = "sudoku_music_volume";
        const string SfxKey = "sudoku_sfx_volume";

        public static AudioManager Instance { get; private set; }

        static float musicVolume, sfxVolume;
        static bool volumesLoaded;

        AudioSource sfxSource, musicSource;
        readonly Dictionary<Sfx, AudioClip> clips = new Dictionary<Sfx, AudioClip>();

        static void EnsureVolumes()
        {
            if (volumesLoaded) return;
            volumesLoaded = true;
            musicVolume = PlayerPrefs.GetFloat(MusicKey, 0.35f);
            sfxVolume = PlayerPrefs.GetFloat(SfxKey, 0.7f);
        }

        public static float MusicVolume
        {
            get { EnsureVolumes(); return musicVolume; }
            set
            {
                EnsureVolumes();
                musicVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(MusicKey, musicVolume);
                if (Instance != null) Instance.musicSource.volume = musicVolume;
            }
        }

        public static float SfxVolume
        {
            get { EnsureVolumes(); return sfxVolume; }
            set
            {
                EnsureVolumes();
                sfxVolume = Mathf.Clamp01(value);
                PlayerPrefs.SetFloat(SfxKey, sfxVolume);
            }
        }

        /// <summary>Safe to call before the manager exists.</summary>
        public static void Play(Sfx sfx, float volume = 1f)
        {
            if (Instance == null || SfxVolume <= 0f) return;
            if (Instance.clips.TryGetValue(sfx, out var clip))
                Instance.sfxSource.PlayOneShot(clip, volume * SfxVolume);
        }

        void Awake()
        {
            Instance = this;
            sfxSource = gameObject.AddComponent<AudioSource>();
            sfxSource.playOnAwake = false;

            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.playOnAwake = false;
            musicSource.loop = true;
            musicSource.volume = MusicVolume;

            BuildSfx();
            StartCoroutine(BuildAndPlayMusic());
        }

        void OnApplicationQuit()
        {
            PlayerPrefs.Save();
        }

        // ---------- Sound effects ----------

        static AudioClip Make(string name, float seconds, Func<float, float> sample)
        {
            int n = Mathf.CeilToInt(seconds * SfxRate);
            var data = new float[n];
            for (int i = 0; i < n; i++)
            {
                float t = i / (float)SfxRate;
                // A 3 ms fade-in avoids a click at the start of every sound.
                data[i] = Mathf.Clamp(sample(t) * Mathf.Min(1f, t / 0.003f), -1f, 1f);
            }
            var clip = AudioClip.Create(name, n, 1, SfxRate, false);
            clip.SetData(data, 0);
            return clip;
        }

        static float Sine(float t, float freq) => Mathf.Sin(2f * Mathf.PI * freq * t);

        // A soft bell: the note plus a quieter octave above, both fading out.
        static float Bell(float t, float freq, float decay)
        {
            if (t < 0f) return 0f;
            return Sine(t, freq) * Mathf.Exp(-decay * t) + 0.3f * Sine(t, freq * 2f) * Mathf.Exp(-decay * 1.6f * t);
        }

        void BuildSfx()
        {
            clips[Sfx.Click] = Make("Click", 0.15f, t => Bell(t, 880f, 40f) * 0.35f);
            clips[Sfx.Hover] = Make("Hover", 0.05f, t => Sine(t, 1400f) * Mathf.Exp(-90f * t) * 0.08f);
            clips[Sfx.Select] = Make("Select", 0.1f, t => Bell(t, 520f, 45f) * 0.25f);

            clips[Sfx.Correct] = Make("Correct", 0.6f, t =>
                (Bell(t, 659.25f, 12f) + Bell(t - 0.09f, 987.77f, 10f)) * 0.3f);

            // Two notes a tritone apart sound tense, which suits a mistake.
            clips[Sfx.Wrong] = Make("Wrong", 0.4f, t =>
                (Sine(t, 110f) + Sine(t, 155.6f) * 0.8f) * Mathf.Exp(-8f * t) * 0.3f);

            clips[Sfx.Erase] = Make("Erase", 0.12f, t =>
                Mathf.Sin(2f * Mathf.PI * (600f * t - 750f * t * t)) * Mathf.Exp(-20f * t) * 0.25f);

            var arpeggio = new[] { 523.25f, 659.25f, 783.99f, 1046.5f, 1318.5f };
            clips[Sfx.Victory] = Make("Victory", 2f, t =>
            {
                float sum = 0f;
                for (int i = 0; i < arpeggio.Length; i++) sum += Bell(t - i * 0.11f, arpeggio[i], 3.5f) * 0.2f;
                return sum;
            });
        }

        // ---------- Background music ----------

        // A slow, calm loop: soft pad chords (Am, F, C, G), a bass note and a gentle plucked arpeggio with an echo.
        IEnumerator BuildAndPlayMusic()
        {
            const float chordSeconds = 8f;
            float[][] chords =
            {
                new[] { 220.00f, 261.63f, 329.63f },
                new[] { 174.61f, 220.00f, 261.63f },
                new[] { 261.63f, 329.63f, 392.00f },
                new[] { 196.00f, 246.94f, 293.66f }
            };

            int total = Mathf.RoundToInt(chords.Length * chordSeconds * MusicRate);
            var pad = new float[total];
            var pluck = new float[total];

            for (int c = 0; c < chords.Length; c++)
            {
                int start = Mathf.RoundToInt(c * chordSeconds * MusicRate);
                foreach (float f in chords[c]) AddPad(pad, start, chordSeconds + 2.5f, f, 0.07f);
                AddPad(pad, start, chordSeconds + 2.5f, chords[c][0] * 0.5f, 0.12f);
                yield return null;
            }

            float stepSeconds = 0.5f;
            int steps = Mathf.RoundToInt(chords.Length * chordSeconds / stepSeconds);
            int[] pattern = { 0, 1, 2, 1 };
            for (int s = 0; s < steps; s++)
            {
                if (s % 8 == 7) continue;
                float time = s * stepSeconds;
                var chord = chords[Mathf.Min((int)(time / chordSeconds), chords.Length - 1)];
                AddPluck(pluck, Mathf.RoundToInt(time * MusicRate), chord[pattern[s % 4]] * 2f, 0.09f);
                if (s % 16 == 0) yield return null;
            }

            AddEcho(pluck, 0.375f, 0.35f);

            var mix = new float[total];
            float peak = 0.0001f;
            for (int i = 0; i < total; i++)
            {
                mix[i] = pad[i] + pluck[i];
                peak = Mathf.Max(peak, Mathf.Abs(mix[i]));
            }
            float gain = 0.8f / peak;
            for (int i = 0; i < total; i++) mix[i] *= gain;

            var clip = AudioClip.Create("Music", total, 1, MusicRate, false);
            clip.SetData(mix, 0);
            musicSource.clip = clip;
            musicSource.volume = MusicVolume;
            musicSource.Play();
        }

        // Writes wrap around the end of the buffer so the loop has no seam.
        static void AddPad(float[] buffer, int startSample, float seconds, float freq, float amp)
        {
            int n = buffer.Length;
            int length = Mathf.RoundToInt(seconds * MusicRate);
            const float attack = 2f, release = 2.5f;
            for (int j = 0; j < length; j++)
            {
                float t = j / (float)MusicRate;
                float env = Mathf.Min(1f, t / attack) * Mathf.Min(1f, (seconds - t) / release);
                float v = Sine(t, freq) + 0.6f * Sine(t, freq * 1.004f) + 0.15f * Sine(t, freq * 2f);
                buffer[(startSample + j) % n] += v * env * env * amp;
            }
        }

        static void AddPluck(float[] buffer, int startSample, float freq, float amp)
        {
            int n = buffer.Length;
            int length = Mathf.RoundToInt(1.8f * MusicRate);
            for (int j = 0; j < length; j++)
            {
                float t = j / (float)MusicRate;
                float env = Mathf.Exp(-3.2f * t) * Mathf.Min(1f, t / 0.005f);
                float v = Sine(t, freq) + 0.35f * Sine(t, freq * 2f) * Mathf.Exp(-2f * t);
                buffer[(startSample + j) % n] += v * env * amp;
            }
        }

        static void AddEcho(float[] buffer, float delaySeconds, float feedback)
        {
            int n = buffer.Length;
            int d = Mathf.RoundToInt(delaySeconds * MusicRate);
            var dry = (float[])buffer.Clone();
            for (int i = 0; i < n; i++)
                buffer[i] += feedback * dry[(i - d + n) % n] + feedback * 0.5f * dry[(i - 2 * d + 2 * n) % n];
        }
    }
}
