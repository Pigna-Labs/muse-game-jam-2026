using System;
using UnityEngine;

namespace MuseGameJam.UI
{
    /// <summary>
    /// Fa partire una sequenza di speech bubble di tutorial sopra il musetto.
    ///
    /// Ogni bolla avanza alla PRESSIONE di un pulsante "Avanti" (la call-to-action
    /// dello <see cref="SpeechBubbleController"/>), non a tempo: la bolla resta a
    /// video finché l'utente non tocca il pulsante.
    ///
    /// Non duplica nessuna logica del fumetto: usa solo i metodi pubblici esistenti
    /// (Show con CTA e Hide). I testi sono configurati qui in Inspector.
    /// </summary>
    public class TutorialManager : MonoBehaviour
    {
        [Header("Riferimenti")]
        [Tooltip("Il controller del fumetto sul UIDocument della MainUI. Trascinalo qui in Inspector.")]
        [SerializeField] private SpeechBubbleController speechBubble;

        [Header("Avvio")]
        [Tooltip("Se attivo, il tutorial parte da solo in Start().")]
        [SerializeField] private bool playOnStart = true;

        [Header("Pulsanti")]
        [Tooltip("Etichetta del pulsante per passare al testo successivo.")]
        [SerializeField] private string nextLabel = "Avanti ▶";

        [Tooltip("Etichetta del pulsante sull'ultimo testo (chiude il tutorial).")]
        [SerializeField] private string lastLabel = "Ho capito!";

        [Tooltip("Secondi di permanenza di ogni bolla del tutorial. Alto = resta finché non premi il pulsante.")]
        [SerializeField] private float stepAutoHideSeconds = 999f;

        [Header("Passi del tutorial")]
        [Tooltip("I testi del tutorial, mostrati in ordine. Ognuno avanza premendo il pulsante.")]
        [SerializeField, TextArea]
        private string[] steps =
        {
            "Ciao! Io sono Musetto, la creatura del MUSE 🌿 Sarò io a guidarti!",
            "Il mio compito? Imparare con te. Il tuo? Prenderti cura di me 💚",
            "Vedi le tre barre in alto? Sono i miei bisogni: cibo 🍎, pulizia 🫧 e coccole 🤗",
            "Quando una barra si abbassa, vuol dire che mi serve aiuto in quella cosa lì.",
            "Usa i tre pulsanti in basso per scegliere cosa darmi: pappa, qualcosa per lavarmi o una coccola.",
            "Poi trascina l'oggetto su di me e lasciala andare: io ci penso! ✨",
            "Vedi il pulsante con la fotocamera? 📷 Serve per inquadrare i QR sparsi nel museo.",
            "Ogni QR che scansiono mi insegna qualcosa di nuovo: io divento più felice e tu più sapiente! 🎉",
            "Tutto quello che impariamo finisce nel taccuino 📒 Aprilo quando vuoi per riguardarlo.",
            "Col bersaglio 🎯 trovi le sfide: completale scansionando i QR giusti e sblocchi delle ricompense!",
            "Se serve silenzio, col tasto dell'audio 🔊 puoi mettermi in muto (non mi offendo, promesso).",
            "È tutto! Ora prenditi cura di me ed esplora il museo. Andiamo! 🐾",
        };

        private int currentStep;

        private void Start()
        {
            if (playOnStart) Play();
        }

        /// <summary>Fa partire la sequenza di tutorial dall'inizio.</summary>
        public void Play()
        {
            if (speechBubble == null)
            {
                Debug.LogWarning("[TutorialManager] SpeechBubble non assegnato in Inspector: nessun tutorial mostrato.");
                return;
            }
            if (steps == null || steps.Length == 0) return;

            currentStep = 0;
            ShowStep(currentStep);
        }

        /// <summary>Interrompe il tutorial e chiude la bolla.</summary>
        public void Stop()
        {
            if (speechBubble != null) speechBubble.Hide();
        }

        // Mostra il passo indicato con un pulsante "Avanti"/"Ho capito" che porta al prossimo.
        private void ShowStep(int index)
        {
            if (index < 0 || index >= steps.Length)
            {
                Stop();
                return;
            }

            bool isLast = index == steps.Length - 1;
            string buttonLabel = isLast ? lastLabel : nextLabel;
            Action onPress = isLast ? (Action)Stop : () => ShowStep(index + 1);

            speechBubble.Show(steps[index], buttonLabel, onPress, null, stepAutoHideSeconds);
        }
    }
}
