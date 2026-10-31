using UnityEngine;

namespace TopDownCharacter2D.FX
{

    [RequireComponent(typeof(AudioSource))]
    public class SoundManager : MonoBehaviour
    {
        public static SoundManager instance;

        [SerializeField] [Range(0f, 1f)] private float soundEffectVolume = 0.8f;
        [SerializeField] [Range(0f, 1f)] private float soundEffectPitchVariance = 0.1f;
        [SerializeField] [Range(0f, 1f)] private float musicVolume = 0.5f;

        private AudioSource musicAudioSource;

        private void Awake()
        {
            if (instance == null)
            {
                instance = this;
            }
            musicAudioSource = GetComponent<AudioSource>();
        }


        public static void PlaySoundEffect(AudioClip audio, Vector3 position)
        {
            PlayClipAt(audio, position, true);
        }

        public static void PlaySoundEffect(AudioClip audio)
        {
            PlayClipAt(audio, Vector3.zero, false);
        }

        public static void ChangeBackGroundMusic(AudioClip music)
        {
            if (instance == null || music == null) return;
            if (instance.musicAudioSource == null) instance.musicAudioSource = instance.GetComponent<AudioSource>();
            if (instance.musicAudioSource != null)
            {
                instance.musicAudioSource.Stop();
                instance.musicAudioSource.clip = music;
                instance.musicAudioSource.volume = instance.musicVolume;
                instance.musicAudioSource.Play();
            }
        }

              private static void PlayClipAt(AudioClip clip, Vector3 pos, bool spatialize)
        {
            if (clip == null) return;

            // Safe singleton retrieval
            if (instance == null)
            {
                instance = FindFirstObjectByType<SoundManager>();
                if (instance == null)
                {
                    GameObject managerObj = new GameObject("SoundManager");
                    instance = managerObj.AddComponent<SoundManager>();
                }
            }

            float volume = 0.8f;
            float pitchVariance = 0.05f;

            if (instance != null)
            {
                // Ensure volume is clearly audible (at least 0.6f if set lower)
                volume = instance.soundEffectVolume > 0.01f ? Mathf.Max(instance.soundEffectVolume, 0.6f) : 0.8f;
                pitchVariance = instance.soundEffectPitchVariance;
            }

            GameObject audioObj = new GameObject($"TempAudio_{clip.name}");
            audioObj.transform.position = pos;

            AudioSource source = audioObj.AddComponent<AudioSource>();
            source.clip = clip;
            source.volume = volume;
            source.pitch = 1f + Random.Range(-pitchVariance, pitchVariance);
            source.spatialBlend = spatialize ? 1f : 0f; 
            source.playOnAwake = false;
            source.Play();

            float destroyDelay = Mathf.Max(clip.length * 2f, 1f);
            Destroy(audioObj, destroyDelay);
        }
    }
}
