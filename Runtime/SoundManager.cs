using System;
using System.Collections.Generic;
using Cysharp.Threading.Tasks;
using LM.EasyPool;
using UnityEngine;

namespace LM
{
    public class SoundManager : MonoBehaviour
    {
        public Sound[] sounds;

        [SerializeField] private Transform audioSourceParent;

        [SerializeField] private Instantiator unityInstantiator;

        [SerializeField] private int maxPoolSize = 50;

        [SerializeField] private bool disallowDestroyOnLoad = true;

        private EasyPool<AudioSource> _audioSourcePool;

        [Header("Pooling Settings")]
        private GameObject _audioSourcePrefab;

        private float _effectsVolume = 1f;

        private float _masterVolume = 1f;
        private float _musicVolume = 1f;

        private Dictionary<string, Sound> _soundsDictionary;

        private void Awake()
        {
            if (disallowDestroyOnLoad)
                DontDestroyOnLoad(gameObject);

            _audioSourcePrefab = Resources.Load<GameObject>("AudioSource");

            if (!_audioSourcePrefab)
            {
                Debug.LogError("SoundManager: AudioSource Prefab is not assigned!", this);
                enabled = false;
                return;
            }

            if (!unityInstantiator)
            {
                Debug.LogError("SoundManager: Unity Instantiator is not assigned!", this);
                enabled = false;
                return;
            }

            _soundsDictionary = new Dictionary<string, Sound>();

            try
            {
                _audioSourcePool = new EasyPool<AudioSource>(
                    _audioSourcePrefab,
                    audioSourceParent,
                    unityInstantiator,
                    EasyPool<AudioSource>.PoolType.Stack,
                    true,
                    maxPoolSize
                );
            }
            catch (Exception e)
            {
                Debug.LogError($"SoundManager: Failed to initialize AudioSource pool: {e.Message}", this);
                enabled = false;
                return;
            }

            foreach (var sound in sounds)
            {
                if (!sound.clip)
                {
                    Debug.LogWarning($"Sound '{sound.identifier}' has no AudioClip assigned. Skipping.");
                    continue;
                }

                sound.originalVolume = sound.volume;

                if (!sound.allowOverlap)
                {
                    sound.dedicatedSource = gameObject.AddComponent<AudioSource>();
                    ConfigureAudioSource(sound.dedicatedSource, sound, null);
                    sound.dedicatedSource.playOnAwake = false;
                }

                _soundsDictionary[sound.identifier] = sound;
            }

            Debug.Log(
                $"SoundManager initialized with {_soundsDictionary.Count} sounds and pool for '{_audioSourcePrefab.name}'.");
        }

        #region Playback Methods

        public async UniTaskVoid Play(string identifier, Vector3? position = null)
        {
            if (!_soundsDictionary.TryGetValue(identifier, out var sound))
            {
                Debug.LogError($"Sound '{identifier}' not found in dictionary!");
                return;
            }

            if (!sound.allowOverlap)
                await PlayDedicatedSource(sound, position);
            else
                await PlayPooledSource(sound, position);
        }

        private async UniTask PlayDedicatedSource(Sound sound, Vector3? position)
        {
            if (!sound.dedicatedSource)
            {
                Debug.LogError($"Sound '{sound.identifier}' is marked as non-overlapping but has no dedicated source!");
                return;
            }

            if (sound.dedicatedSource.isPlaying && sound.isLoop) return;

            ConfigureAudioSource(sound.dedicatedSource, sound, position);
            sound.dedicatedSource.Play();

            await UniTask.Delay(TimeSpan.FromSeconds(sound.dedicatedSource.Duration()));
        }

        private async UniTask PlayPooledSource(Sound sound, Vector3? position)
        {
            AudioSource audioSource;
            try
            {
                audioSource = _audioSourcePool.Get();
                if (!audioSource || !audioSource)
                {
                    Debug.LogError($"Pool returned invalid object for sound '{sound.identifier}'.", audioSource);
                    if (audioSource) _audioSourcePool.Release(audioSource);
                    return;
                }
            }
            catch (Exception e)
            {
                Debug.LogError($"Failed to get AudioSource from pool for '{sound.identifier}': {e.Message}", this);
                return;
            }

            ConfigureAudioSource(audioSource, sound, position);

            if (audioSource.GetComponent<IReturnToPool<AudioSource>>() is { } returner) returner.OnConfigured();

            audioSource.Play();

            await UniTask.Delay(TimeSpan.FromSeconds(audioSource.Duration()));
        }

        private void ConfigureAudioSource(AudioSource source, Sound sound, Vector3? position)
        {
            if (!source || sound == null || !sound.clip) return;

            source.clip = sound.clip;
            source.volume = CalculateActualVolume(sound);
            source.pitch = sound.pitch;
            source.loop = sound.isLoop;

            if (position.HasValue)
            {
                source.transform.position = position.Value;
                source.spatialBlend = 1.0f;
            }
            else
            {
                source.transform.position = transform.position;
                source.spatialBlend = 0f;
            }
        }

        // Stop, Pause, UnPause primarily work reliably for non-overlapping sounds.

        public void Stop(string identifier)
        {
            if (_soundsDictionary.TryGetValue(identifier, out var sound) && !sound.allowOverlap &&
                sound.dedicatedSource != null)
                sound.dedicatedSource.Stop();
            else if (sound is { allowOverlap: true })
                Debug.LogWarning(
                    $"Stop() called on overlapping sound '{identifier}'. Only stops dedicated source, not pooled instances.");
        }

        public void Pause(string identifier)
        {
            if (_soundsDictionary.TryGetValue(identifier, out var sound) && !sound.allowOverlap &&
                sound.dedicatedSource != null)
                sound.dedicatedSource.Pause();
            else if (sound is { allowOverlap: true })
                Debug.LogWarning(
                    $"Pause() called on overlapping sound '{identifier}'. Not supported for pooled instances.");
        }

        public void UnPause(string identifier)
        {
            if (_soundsDictionary.TryGetValue(identifier, out var sound) && !sound.allowOverlap &&
                sound.dedicatedSource != null)
                sound.dedicatedSource.UnPause();
            else if (sound is { allowOverlap: true })
                Debug.LogWarning(
                    $"UnPause() called on overlapping sound '{identifier}'. Not supported for pooled instances.");
        }

        public bool IsPlaying(string identifier)
        {
            if (!_soundsDictionary.TryGetValue(identifier, out var sound)) return false;

            switch (sound.allowOverlap)
            {
                case false when sound.dedicatedSource != null:
                    return sound.dedicatedSource.isPlaying;
                case true:
                    Debug.LogWarning($"IsPlaying() check for overlapping sound '{identifier}' is simplified.");
                    return false;
            }

            Debug.LogError($"Sound '{identifier}' not found for IsPlaying check!");
            return false;
        }

        #endregion

        #region Volume Control

        private float CalculateActualVolume(Sound sound)
        {
            var typeVolume = sound.type == Sound.Type.Music ? _musicVolume : _effectsVolume;
            return Mathf.Clamp01(_masterVolume * typeVolume * sound.originalVolume);
        }

        private void ApplyAllVolumes()
        {
            foreach (var sound in _soundsDictionary.Values)
                if (!sound.allowOverlap && sound.dedicatedSource)
                    sound.dedicatedSource.volume = CalculateActualVolume(sound);
        }

        public void SetMasterVolume(float volume)
        {
            _masterVolume = Mathf.Clamp01(volume);
            ApplyAllVolumes();
        }

        public void SetMusicVolume(float volume)
        {
            _musicVolume = Mathf.Clamp01(volume);
            ApplyAllVolumes();
        }

        public void SetEffectsVolume(float volume)
        {
            _effectsVolume = Mathf.Clamp01(volume);
            ApplyAllVolumes();
        }

        #endregion
    }

    [Serializable]
    public class Sound
    {
        public enum Type
        {
            Effect,
            Music
        }

        public string identifier;
        public AudioClip clip;

        [Range(0f, 1f)] public float volume = 1f;
        [Range(.1f, 3f)] public float pitch = 1f;

        [Tooltip("Should this sound loop indefinitely?")]
        public bool isLoop;

        [Tooltip("Allow multiple instances of this sound to play at the same time?")]
        public bool allowOverlap = true;

        public Type type;

        [HideInInspector] public AudioSource dedicatedSource;
        [HideInInspector] public float originalVolume;
    }
}