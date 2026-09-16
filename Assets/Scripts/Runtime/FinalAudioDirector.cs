using UnityEngine;

namespace FragmentsOfHer.Assignment3
{
    /// <summary>Original procedural ambient score and interaction SFX for the final prototype.</summary>
    public sealed class FinalAudioDirector : MonoBehaviour
    {
        public static FinalAudioDirector Instance { get; private set; }
        public bool MusicReady => musicSource != null && musicSource.clip != null;
        public bool EffectsReady => effectsSource != null && interactionClip != null && dangerClip != null;

        private AudioSource musicSource;
        private AudioSource effectsSource;
        private AudioClip interactionClip;
        private AudioClip dangerClip;
        private AudioClip impactClip;
        private AudioClip finaleClip;

        private void Awake()
        {
            Instance = this;
            musicSource = gameObject.AddComponent<AudioSource>();
            musicSource.loop = true; musicSource.playOnAwake = false; musicSource.spatialBlend = 0f; musicSource.volume = .28f;
            effectsSource = gameObject.AddComponent<AudioSource>();
            effectsSource.loop = false; effectsSource.playOnAwake = false; effectsSource.spatialBlend = 0f; effectsSource.volume = .62f;
            musicSource.clip = CreateAmbientMusic();
            interactionClip = CreateTone("Virtue Resonance", .52f, 392f, 659f, .32f, false);
            dangerClip = CreateTone("Danger Warning", .42f, 115f, 76f, .5f, true);
            impactClip = CreateTone("Nightmare Impact", .28f, 92f, 42f, .58f, true);
            finaleClip = CreateTone("Final Integration", 1.6f, 261.63f, 783.99f, .4f, false);
        }

        public void StartMusic() { if (musicSource != null && !musicSource.isPlaying) musicSource.Play(); }
        public void PlayInteraction(int id) { if (effectsSource != null) effectsSource.PlayOneShot(interactionClip, id == 11 ? 1f : .72f); }
        public void PlayDanger() { if (effectsSource != null) effectsSource.PlayOneShot(dangerClip, .85f); }
        public void PlayImpact() { if (effectsSource != null) effectsSource.PlayOneShot(impactClip, .9f); }
        public void PlayFinale() { if (effectsSource != null) effectsSource.PlayOneShot(finaleClip, 1f); }

        private static AudioClip CreateAmbientMusic()
        {
            const int frequency = 22050;
            const int seconds = 24;
            int count = frequency * seconds;
            float[] data = new float[count];
            uint noise = 2463534242u;
            for (int i = 0; i < count; i++)
            {
                float t = i / (float)frequency;
                noise ^= noise << 13; noise ^= noise >> 17; noise ^= noise << 5;
                float breath = ((noise & 65535) / 32767.5f - 1f) * .012f;
                float progression = Mathf.Floor(t / 6f) % 4f;
                float root = progression == 0f ? 110f : progression == 1f ? 130.81f : progression == 2f ? 146.83f : 98f;
                float pad = Mathf.Sin(2f * Mathf.PI * root * t) * .055f
                          + Mathf.Sin(2f * Mathf.PI * root * 1.5f * t) * .035f
                          + Mathf.Sin(2f * Mathf.PI * root * 2f * t) * .018f;
                float pulse = Mathf.Sin(2f * Mathf.PI * .125f * t) * .5f + .5f;
                float edgeFade = Mathf.Min(1f, Mathf.Min(t, seconds - t) / 1.2f);
                data[i] = (pad * (.62f + pulse * .38f) + breath) * edgeFade;
            }
            AudioClip clip = AudioClip.Create("A Light Within - Inner Light", count, 1, frequency, false);
            clip.SetData(data, 0);
            return clip;
        }

        private static AudioClip CreateTone(string name, float seconds, float startHz, float endHz, float volume, bool noisy)
        {
            const int frequency = 22050;
            int count = Mathf.CeilToInt(seconds * frequency);
            float[] data = new float[count];
            uint noise = 123456789u;
            for (int i = 0; i < count; i++)
            {
                float n = i / (float)count;
                float hz = Mathf.Lerp(startHz, endHz, n);
                float envelope = Mathf.Sin(Mathf.PI * n) * (1f - n * .25f);
                noise = 1664525u * noise + 1013904223u;
                float grit = noisy ? ((noise & 65535) / 32767.5f - 1f) * .35f : 0f;
                data[i] = (Mathf.Sin(2f * Mathf.PI * hz * i / frequency) + grit) * envelope * volume;
            }
            AudioClip clip = AudioClip.Create(name, count, 1, frequency, false);
            clip.SetData(data, 0);
            return clip;
        }
    }
}
