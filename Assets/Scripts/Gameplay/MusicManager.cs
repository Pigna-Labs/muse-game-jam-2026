using MuseGameJam.StateSystem;
using MuseGameJam.States;
using MuseGameJam.Visuals;
using UnityEngine;

namespace MuseGameJam.Gameplay
{
    /// <summary>
    /// Musica di sottofondo della scena principale, a layer sincronizzati:
    ///   - mainLoop : base sempre attiva (loop continuo).
    ///   - dayLayer : layer "giorno", sopra la base, attivo di giorno.
    ///   - nightLayer: layer "notte", sopra la base, attivo di notte.
    ///
    /// I tre AudioSource partono insieme e restano in loop in sync; al cambio
    /// giorno/notte si fa CROSSFADE tra il volume del layer giorno e quello notte
    /// (la base non cambia). Giorno/notte è deciso da DayNight (ora reale del
    /// dispositivo), la stessa logica usata da DayNightImage.
    ///
    /// FADE overlay: quando si apre un overlay (camera/challenges/unlockables) la
    /// musica fa fade out totale; alla chiusura, fade in. Così passando alla camera
    /// la musica della scena principale sfuma.
    ///
    /// SETUP IN EDITOR (lo fa l'umano):
    ///   - Metti questo script su un GameObject della scena principale.
    ///   - Trascina i 3 AudioClip negli slot (per ora basta Main Loop).
    ///   - I 3 clip dovrebbero avere la STESSA durata/bpm per restare in sync.
    /// </summary>
    public class MusicManager : MonoBehaviour
    {
        [Header("Layer musicali")]
        [Tooltip("Base sempre attiva (loop continuo).")]
        [SerializeField] private AudioClip mainLoop;
        [Tooltip("Layer giorno, sopra la base. Opzionale per ora.")]
        [SerializeField] private AudioClip dayLayer;
        [Tooltip("Layer notte, sopra la base. Opzionale per ora.")]
        [SerializeField] private AudioClip nightLayer;

        [Header("Volumi")]
        [Range(0f, 1f)] [SerializeField] private float mainVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float dayVolume = 1f;
        [Range(0f, 1f)] [SerializeField] private float nightVolume = 1f;
        [Tooltip("Moltiplicatore globale su tutta la musica.")]
        [Range(0f, 1f)] [SerializeField] private float masterVolume = 1f;
        [Tooltip("Guadagno in dB applicato a TUTTA la musica (0 = invariato, +3 = un filo più forte, " +
                 "valori negativi = più piano). Configurabile da Inspector. NB: AudioSource.volume è " +
                 "clampato a 1.0, quindi per sentire un boost reale i volumi base sopra vanno tenuti < 1.0.")]
        [Range(-24f, 12f)] [SerializeField] private float gainDb = 3f;

        // Fattore lineare equivalente a gainDb (dB -> ampiezza). +3 dB ≈ 1.41, 0 dB = 1.0.
        private float GainLinear => Mathf.Pow(10f, gainDb / 20f);

        [Header("Giorno/Notte")]
        [SerializeField, Range(0, 23)] private int sunriseHour = DayNight.DefaultSunriseHour;
        [SerializeField, Range(1, 24)] private int sunsetHour = DayNight.DefaultSunsetHour;
        [Tooltip("Durata del crossfade giorno<->notte (secondi).")]
        [SerializeField] private float dayNightFade = 2f;
        [Tooltip("Ogni quanti secondi ricontrollare se è cambiato giorno/notte.")]
        [SerializeField] private float dayNightCheckInterval = 30f;

        [Header("Fade overlay")]
        [Tooltip("Durata del fade quando si apre/chiude un overlay (secondi).")]
        [SerializeField] private float overlayFade = 1f;
        [Tooltip("Volume della musica (0..1) quando è aperto un menu di navigazione " +
                 "(challenges/unlockables): abbassata ma ancora udibile.")]
        [Range(0f, 1f)] [SerializeField] private float menuDuck = 0.5f;
        // La modalità camera (StateCamera) azzera del tutto la musica (0).

        private AudioSource _main;
        private AudioSource _day;
        private AudioSource _night;

        private bool _isDay;
        private float _dnTimer;

        // Fade overlay: moltiplicatore 0..1 applicato a tutta la musica.
        private float _overlayMul = 1f;
        private float _overlayTarget = 1f;

        private void Awake()
        {
            _main  = MakeSource(mainLoop);
            _day   = MakeSource(dayLayer);
            _night = MakeSource(nightLayer);

            _isDay = DayNight.IsDaytime(sunriseHour, sunsetHour);
        }

        private void OnEnable()
        {
            // Avvia i 3 layer insieme: restano sincronizzati perché partono allo stesso istante.
            PlaySynced();

            // Imposta subito i volumi day/night senza fade (stato iniziale).
            float gain = GainLinear;
            _day.volume   = (_isDay ? dayVolume : 0f) * masterVolume * _overlayMul * gain;
            _night.volume = (_isDay ? 0f : nightVolume) * masterVolume * _overlayMul * gain;
            _main.volume  = mainVolume * masterVolume * _overlayMul * gain;

            _dnTimer = 0f;
        }

        private void Update()
        {
            // 1) Fade overlay: camera = silenzio totale, menu di navigazione = duck al 50%,
            //    nessun overlay = volume pieno.
            _overlayTarget = ComputeOverlayTarget();
            if (!Mathf.Approximately(_overlayMul, _overlayTarget))
            {
                float step = (overlayFade > 0f) ? Time.unscaledDeltaTime / overlayFade : 1f;
                _overlayMul = Mathf.MoveTowards(_overlayMul, _overlayTarget, step);
            }

            // 2) Controllo periodico giorno/notte per il crossfade.
            _dnTimer += Time.unscaledDeltaTime;
            if (_dnTimer >= dayNightCheckInterval)
            {
                _dnTimer = 0f;
                bool nowDay = DayNight.IsDaytime(sunriseHour, sunsetHour);
                if (nowDay != _isDay) _isDay = nowDay; // il crossfade lo fa il blocco volumi sotto
            }

            // 3) Applica i volumi target con smoothing (crossfade day/night + overlay).
            ApplyVolumes();
        }

        private void ApplyVolumes()
        {
            float dnStep = (dayNightFade > 0f) ? Time.unscaledDeltaTime / dayNightFade : 1f;

            float dayTarget   = (_isDay ? dayVolume : 0f);
            float nightTarget = (_isDay ? 0f : nightVolume);

            // crossfade day/night
            float curDay   = Mathf.MoveTowards(NormalizedVol(_day, dayVolume), dayTarget, dnStep);
            float curNight = Mathf.MoveTowards(NormalizedVol(_night, nightVolume), nightTarget, dnStep);

            // applica con master + overlay + gain (dB)
            float gain = GainLinear;
            _main.volume  = mainVolume * masterVolume * _overlayMul * gain;
            _day.volume   = curDay     * masterVolume * _overlayMul * gain;
            _night.volume = curNight   * masterVolume * _overlayMul * gain;
        }

        // Riporta il volume corrente di un source alla scala "0..volumeBase" (toglie master+overlay+gain),
        // così il MoveTowards lavora sullo stesso spazio dei target.
        private float NormalizedVol(AudioSource src, float baseVol)
        {
            float denom = masterVolume * _overlayMul * GainLinear;
            if (denom <= 0.0001f) return baseVol == 0f ? 0f : src.volume; // evita /0 mentre è in fade
            return src.volume / denom;
        }

        private void PlaySynced()
        {
            // Tutti dallo stesso istante DSP, così i loop restano allineati.
            double start = AudioSettings.dspTime + 0.05;
            PlayAt(_main, start);
            PlayAt(_day, start);
            PlayAt(_night, start);
        }

        private static void PlayAt(AudioSource src, double dspTime)
        {
            if (src == null || src.clip == null) return;
            src.PlayScheduled(dspTime);
        }

        private AudioSource MakeSource(AudioClip clip)
        {
            var src = gameObject.AddComponent<AudioSource>();
            src.clip = clip;
            src.loop = true;
            src.playOnAwake = false;
            src.spatialBlend = 0f; // 2D
            src.volume = 0f;
            return src;
        }

        // Volume target (0..1) della musica in base a cosa è aperto:
        //  - modalità camera (StateCamera)      -> 0   (silenzio totale)
        //  - altro overlay (menu navigazione)   -> menuDuck (abbassata ma udibile)
        //  - nessun overlay                     -> 1   (pieno)
        private float ComputeOverlayTarget()
        {
            var sm = GameStateMachine.Instance;
            if (sm == null) return 1f;
            if (sm.HasOverlay<StateCamera>()) return 0f;   // camera: muto
            if (sm.HasOverlayOpen) return menuDuck;        // altri menu: duck
            return 1f;
        }
    }
}
