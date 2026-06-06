using UnityEngine;

namespace MuseGameJam.UI
{
    // Singleton che espone i tre suoni UI (Positive / Neutral / Negative).
    // Mettilo sulla stessa scena root come componente; assegna le clip in Inspector.
    public class UISoundManager : MonoBehaviour
    {
        public static UISoundManager Instance { get; private set; }

        [Header("UI Clips")]
        [SerializeField] private AudioClip positive;
        [SerializeField] private AudioClip neutral;
        [SerializeField] private AudioClip negative;

        [Range(0f, 1f)]
        [SerializeField] private float volume = 1f;

        private AudioSource source;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }
            Instance = this;
            source = gameObject.AddComponent<AudioSource>();
            source.playOnAwake = false;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void PlayPositive() => Play(positive);
        public void PlayNeutral()  => Play(neutral);
        public void PlayNegative() => Play(negative);

        private void Play(AudioClip clip)
        {
            if (clip == null || source == null) return;
            source.PlayOneShot(clip, volume);
        }
    }
}
