using UnityEngine;

namespace MuseGameJam.Gameplay
{
    /// <summary>
    /// Riproduce l'audio del musetto in base allo stato dell'Animator.
    ///
    /// Ogni stato del CharacterAnimController ha la sua clip:
    ///   MusettoIdelAnimation    -> idle    (loop)
    ///   MusettoPulitoAnimation  -> pulito  (loop, dura quanto pulisci)
    ///   MusettoCoccoleAnimation -> coccole (loop, dura quanto accarezzi)
    ///   MusettoCiboAnimation    -> cibo    (one-shot)
    ///   MusettoFeliceAnimation  -> felice  (one-shot)
    ///
    /// Quando lo stato dell'Animator cambia, parte l'audio relativo: in loop per
    /// gli stati continui (idle/pulito/coccole), una volta sola per gli eventi
    /// (cibo/felice). Tornando a un altro stato l'audio precedente viene fermato.
    ///
    /// SETUP IN EDITOR (lo fa l'umano):
    ///   Metti questo script sullo stesso GameObject che ha l'Animator del musetto
    ///   (oppure assegna l'Animator nel campo), poi trascina i 5 AudioClip negli slot.
    /// </summary>
    public class MusettoAudio : MonoBehaviour
    {
        [Tooltip("L'Animator del musetto. Se vuoto, lo cerca su questo GameObject.")]
        [SerializeField] private Animator animator;

        // Audio di un singolo stato, con tuning indipendente (volume + pitch).
        [System.Serializable]
        public class StateAudio
        {
            public AudioClip clip;
            [Range(0f, 1f)] public float volume = 1f;
            [Range(0.1f, 3f)] public float pitch = 1f;
        }

        [Header("Clip per stato (volume/pitch indipendenti)")]
        [SerializeField] private StateAudio idle;     // loop
        [SerializeField] private StateAudio pulito;   // loop
        [SerializeField] private StateAudio coccole;  // loop
        [SerializeField] private StateAudio cibo;     // one-shot
        [SerializeField] private StateAudio felice;   // one-shot

        [Header("Master")]
        [Tooltip("Moltiplicatore globale applicato a tutti i volumi sopra.")]
        [Range(0f, 1f)]
        [SerializeField] private float masterVolume = 1f;

        // Hash dei nomi di stato (devono combaciare col controller).
        private static readonly int IdleState    = Animator.StringToHash("MusettoIdelAnimation");
        private static readonly int CiboState     = Animator.StringToHash("MusettoCiboAnimation");
        private static readonly int PulitoState   = Animator.StringToHash("MusettoPulitoAnimation");
        private static readonly int FeliceState   = Animator.StringToHash("MusettoFeliceAnimation");
        private static readonly int CoccoleState  = Animator.StringToHash("MusettoCoccoleAnimation");

        private AudioSource _source;
        private int _currentState; // shortNameHash dello stato per cui sta suonando

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null)
                Debug.LogWarning("[MusettoAudio] Nessun Animator assegnato o trovato.");

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D
        }

        private void Update()
        {
            if (animator == null) return;

            int state = animator.GetCurrentAnimatorStateInfo(0).shortNameHash;
            if (state == _currentState) return; // nessun cambio di stato

            _currentState = state;
            PlayForState(state);
        }

        private void PlayForState(int state)
        {
            // (audio, loop?) per ciascuno stato. Stati non mappati -> silenzio.
            StateAudio audio = null;
            bool loop = false;

            if (state == IdleState)        { audio = idle;    loop = true; }
            else if (state == PulitoState) { audio = pulito;  loop = true; }
            else if (state == CoccoleState){ audio = coccole; loop = true; }
            else if (state == CiboState)   { audio = cibo;    loop = false; }
            else if (state == FeliceState) { audio = felice;  loop = false; }

            if (audio == null || audio.clip == null) { _source.Stop(); return; }

            _source.Stop();
            _source.clip = audio.clip;
            _source.loop = loop;
            _source.volume = audio.volume * masterVolume; // tuning per-clip * master
            _source.pitch = audio.pitch;
            _source.Play();
        }
    }
}
