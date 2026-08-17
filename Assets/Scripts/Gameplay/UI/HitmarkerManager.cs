using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

namespace Unity.MP_FPS
{
    public enum HitType
    {
        Body,
        Headshot,
        Kill
    }

    public class HitmarkerManager : MonoBehaviour
    {
        public static HitmarkerManager Instance { get; private set; }

        private AudioSource _audioSource;
        private AudioClip _bodyHitClip;
        private AudioClip _headshotHitClip;
        private AudioClip _killHitClip;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void AutoInitialize()
        {
            if (Instance == null)
            {
                var go = new GameObject("HitmarkerManager");
                DontDestroyOnLoad(go);
                go.AddComponent<HitmarkerManager>();
            }
        }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;
            _audioSource = gameObject.AddComponent<AudioSource>();
            _audioSource.playOnAwake = false;
            _audioSource.spatialBlend = 0f; // 2D UI Sound

            GenerateProceduralAudioClips();
        }

        private void GenerateProceduralAudioClips()
        {
            _bodyHitClip = CreateTone(1800f, 0.04f, 0.5f, 80f);
            _headshotHitClip = CreateTone(2600f, 0.08f, 0.6f, 40f);
            _killHitClip = CreateTone(420f, 0.22f, 0.8f, 15f);
        }

        private AudioClip CreateTone(float frequency, float duration, float volume, float decaySpeed)
        {
            int sampleRate = 44100;
            int sampleCount = Mathf.CeilToInt(sampleRate * duration);
            float[] samples = new float[sampleCount];

            for (int i = 0; i < sampleCount; i++)
            {
                float time = (float)i / sampleRate;
                float envelope = Mathf.Exp(-decaySpeed * time);
                samples[i] = Mathf.Sin(2f * Mathf.PI * frequency * time) * envelope * volume;
            }

            AudioClip clip = AudioClip.Create($"HitSound_{frequency}", sampleCount, 1, sampleRate, false);
            clip.SetData(samples, 0);
            return clip;
        }

        public static void TriggerHit(HitType hitType)
        {
            if (Instance != null)
            {
                Instance.PlayHit(hitType);
            }

            if (InGameHUD.Instance != null)
            {
                InGameHUD.Instance.TriggerHitmarker(hitType);
            }
        }

        public void PlayHit(HitType hitType)
        {
            AudioClip clip = hitType switch
            {
                HitType.Headshot => _headshotHitClip,
                HitType.Kill => _killHitClip,
                _ => _bodyHitClip
            };

            if (clip != null && _audioSource != null)
            {
                _audioSource.PlayOneShot(clip);
            }
        }
    }
}
