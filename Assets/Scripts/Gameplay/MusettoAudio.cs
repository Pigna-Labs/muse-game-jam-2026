using MuseGameJam.StateSystem;
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
    /// pulito/coccole (durano quanto l'azione), una volta sola per gli eventi
    /// (cibo/felice). Tornando a un altro stato l'audio precedente viene fermato.
    ///
    /// IDLE in due stati:
    ///   MusettoIdelAnimation        -> "stance" idle, ciclo neutro SENZA audio.
    ///   MusettoIdleTriggerAnimation -> "gesto" idle, one-shot CON audio (idle.clip).
    /// Mentre è nella stance, dopo 3-6 cicli (casuale) questo script spara il
    /// parametro IdleTrigger: l'Animator passa al gesto, qui suoniamo il verso una
    /// volta, poi l'Animator torna alla stance (transizione gestita in editor).
    /// Dal gesto si può comunque passare agli altri stati (pet/clean/eat/happy).
    ///
    /// SETUP IN EDITOR (lo fa l'umano):
    ///   - Questo script sul GameObject con l'Animator (o assegna l'Animator nel campo).
    ///   - Transizioni: stance --[IdleTrigger]--> gesto (Has Exit Time off);
    ///     gesto --[exit time]--> stance; e dal gesto anche verso pet/clean/eat/happy.
    ///   - Trascina i 6 AudioClip negli slot (idle = audio del gesto).
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

        [Header("Gesto idle (IdleTrigger)")]
        [Tooltip("Min cicli della stance idle prima di scattare il gesto (IdleTrigger).")]
        [SerializeField] private int idleEveryMinCycles = 3;
        [Tooltip("Max cicli della stance idle prima di scattare il gesto (IdleTrigger).")]
        [SerializeField] private int idleEveryMaxCycles = 6;

        // Hash dei nomi di stato (devono combaciare col controller).
        private static readonly int IdleState        = Animator.StringToHash("MusettoIdelAnimation");        // stance
        private static readonly int IdleTriggerState = Animator.StringToHash("MusettoIdleTriggerAnimation"); // gesto
        private static readonly int CiboState     = Animator.StringToHash("MusettoCiboAnimation");
        private static readonly int PulitoState   = Animator.StringToHash("MusettoPulitoAnimation");
        private static readonly int FeliceState   = Animator.StringToHash("MusettoFeliceAnimation");
        private static readonly int CoccoleState  = Animator.StringToHash("MusettoCoccoleAnimation");

        // Parametro che fa passare dalla stance al gesto idle.
        private static readonly int IdleTriggerParam = Animator.StringToHash("IdleTrigger");

        private AudioSource _source;  // unica sorgente: suoni di stato + verso del gesto idle
        private int _currentState;    // shortNameHash dello stato per cui sta suonando

        // Conteggio cicli della stance idle per decidere quando scattare il gesto.
        private float _lastIdleNormalized;
        private int _idleCyclesPlayed;
        private int _idleCyclesTarget;

        private void Awake()
        {
            if (animator == null) animator = GetComponent<Animator>();
            if (animator == null)
                Debug.LogWarning("[MusettoAudio] Nessun Animator assegnato o trovato.");

            _source = gameObject.AddComponent<AudioSource>();
            _source.playOnAwake = false;
            _source.spatialBlend = 0f; // 2D

            PickNextIdleTarget();
        }

        private void Update()
        {
            if (animator == null) return;

            // Con un overlay aperto (camera, challenges, unlockables...) la main è in
            // pausa: il musetto deve tacere. Ferma l'audio in corso e non riprodurre nulla.
            if (IsMainPaused())
            {
                if (_source.isPlaying) _source.Stop();
                _currentState = 0; // forza il replay del suono di stato al ritorno
                return;
            }

            var info = animator.GetCurrentAnimatorStateInfo(0);
            int state = info.shortNameHash;

            if (state != _currentState)
            {
                _currentState = state;
                OnStateChanged(state);
            }

            // Mentre è in idle, conta i cicli dell'animazione ed emetti il verso
            // ogni N cicli (N casuale). Negli altri stati l'idle resta zitto.
            if (state == IdleState)
                TickIdle(info.normalizedTime);
        }

        // Suoni legati al CAMBIO di stato.
        //  - stance idle: nessun audio (ci pensa il gesto).
        //  - gesto idle (IdleTrigger): one-shot con idle.clip.
        private void OnStateChanged(int state)
        {
            StateAudio audio = null;
            bool loop = false;

            if (state == PulitoState)           { audio = pulito;  loop = true; }
            else if (state == CoccoleState)     { audio = coccole; loop = true; }
            else if (state == CiboState)        { audio = cibo;    loop = false; }
            else if (state == FeliceState)      { audio = felice;  loop = false; }
            else if (state == IdleTriggerState) { audio = idle;    loop = false; } // gesto -> verso
            // IdleState (stance) -> nessun audio

            if (audio == null || audio.clip == null) { _source.Stop(); }
            else
            {
                _source.Stop();
                _source.clip = audio.clip;
                _source.loop = loop;
                _source.volume = audio.volume * masterVolume;
                _source.pitch = audio.pitch;
                _source.Play();
            }

            // Entrando nella stance reimposta il conteggio: il gesto scatterà
            // dopo qualche ciclo, non subito.
            if (state == IdleState) ResetIdleCounter(normalized: 0f);
        }

        // Conta i cicli completati della STANCE idle (ogni volta che normalizedTime
        // scavalca un intero) e, raggiunto il target casuale, fa scattare il gesto
        // (IdleTrigger). L'Animator passa allo stato gesto, dove suona il verso.
        private void TickIdle(float normalizedTime)
        {
            float frac = normalizedTime - Mathf.Floor(normalizedTime);
            // Wrap: la parte frazionaria è "tornata indietro" -> un ciclo è completato.
            if (frac < _lastIdleNormalized)
            {
                _idleCyclesPlayed++;
                if (_idleCyclesPlayed >= _idleCyclesTarget)
                {
                    animator.SetTrigger(IdleTriggerParam);
                    PickNextIdleTarget();
                    _idleCyclesPlayed = 0;
                }
            }
            _lastIdleNormalized = frac;
        }

        // La main è "in pausa" quando un overlay (camera ecc.) è sullo stack.
        // Se non c'è una GameStateMachine in scena, consideriamo non in pausa.
        private static bool IsMainPaused()
        {
            var sm = GameStateMachine.Instance;
            return sm != null && sm.HasOverlayOpen;
        }

        private void PickNextIdleTarget()
        {
            int min = Mathf.Max(1, idleEveryMinCycles);
            int max = Mathf.Max(min, idleEveryMaxCycles);
            _idleCyclesTarget = Random.Range(min, max + 1); // inclusivo
        }

        private void ResetIdleCounter(float normalized)
        {
            _idleCyclesPlayed = 0;
            _lastIdleNormalized = normalized - Mathf.Floor(normalized);
            PickNextIdleTarget();
        }
    }
}
