using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    /// <summary>
    /// Speech bubble shown on the main screen, anchored ABOVE the musetto.
    ///
    /// It lives on the same UIDocument as the MainUI (the #speech-bubble element in
    /// MainUI.uxml) and, while visible, projects the musetto's world position into the
    /// panel each frame so the bubble follows the character (LateUpdate + RuntimePanelUtils).
    ///
    /// Decoupled from gameplay: callers just push a line with Show(...). A line can carry
    /// an optional call-to-action button whose click runs a callback (e.g. open a UI) and
    /// closes the bubble. The bubble auto-hides after a few seconds unless reconfigured.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class SpeechBubbleController : MonoBehaviour
    {
        [Header("Anchor")]
        [Tooltip("Il musetto: la bolla viene messa sopra questo transform. Se vuoto, la bolla resta nascosta.")]
        [SerializeField] private Transform anchor;
        [Tooltip("Camera che inquadra il musetto. Se vuoto usa Camera.main.")]
        [SerializeField] private Camera worldCamera;
        [Tooltip("Offset in unità mondo applicato all'anchor (alza il punto d'aggancio sopra la testa).")]
        [SerializeField] private Vector3 worldOffset = new Vector3(0f, 1.5f, 0f);
        [Tooltip("Offset finale in pixel di pannello (fine tuning della posizione della bolla).")]
        [SerializeField] private Vector2 panelOffset = Vector2.zero;

        [Header("Timing")]
        [Tooltip("Secondi prima che la bolla si chiuda da sola. <= 0 = resta finché non viene chiusa o sostituita.")]
        [SerializeField] private float autoHideSeconds = 4f;

        [Header("Messaggi")]
        [Tooltip("Testo quando scansioni un QR e sblocchi una NUOVA info.")]
        [SerializeField, TextArea] private string infoUnlockedMessage = "Ho imparato qualcosa di nuovo! 🎉";
        [Tooltip("Testo quando scansioni un QR di un'info GIÀ sbloccata.")]
        [SerializeField, TextArea] private string infoAlreadyKnownMessage = "Questo lo conosco già! 😉";
        [Tooltip("Testo quando il QR scansionato non corrisponde a nessuna info del museo.")]
        [SerializeField, TextArea] private string qrNotRecognizedMessage = "Mmh, questo non è un QR del museo! 🤔";
        [Tooltip("Etichetta della call-to-action che (in futuro) aprirà la UI dell'info.")]
        [SerializeField] private string ctaLabel = "Scopri";

        [Header("Typewriter")]
        [Tooltip("Caratteri al secondo dell'effetto macchina da scrivere. <= 0 = testo immediato.")]
        [SerializeField] private float typewriterSpeed = 7f;

        [Header("Galleggiamento")]
        [Tooltip("Ampiezza in pixel di pannello dell'ondeggiamento verticale. 0 = fermo.")]
        [SerializeField] private float floatAmplitude = 12f;
        [Tooltip("Velocità dell'ondeggiamento.")]
        [SerializeField] private float floatSpeed = 2f;

        private VisualElement bubble;
        private VisualElement icon;
        private Label label;
        private Button cta;

        private Action ctaCallback;
        private float hideAtTime;
        private bool visible;

        // Stato dell'effetto macchina da scrivere.
        private string fullMessage = string.Empty;
        private int revealedChars;
        private float nextCharTime;
        private bool revealing;

        public bool IsVisible => visible;

        // Lets gameplay code point the bubble at the character without an Inspector wire.
        public void SetAnchor(Transform target) => anchor = target;

        private void OnEnable()
        {
            Bind();
            HideImmediate();
        }

        private void OnDisable()
        {
            if (cta != null) cta.clicked -= OnCtaClicked;
        }

        // Finds the bubble elements in MainUI.uxml and wires the CTA button.
        private void Bind()
        {
            UIDocument document = GetComponent<UIDocument>();
            VisualElement root = document != null ? document.rootVisualElement : null;
            if (root == null)
            {
                Debug.LogWarning("[SpeechBubble] rootVisualElement non pronto: controlla UIDocument/Source Asset.");
                return;
            }

            bubble = root.Q<VisualElement>("speech-bubble");
            icon   = root.Q<VisualElement>("speech-bubble-icon");
            label  = root.Q<Label>("speech-bubble-text");
            cta    = root.Q<Button>("speech-bubble-cta");

            if (cta != null)
            {
                cta.clicked -= OnCtaClicked;
                cta.clicked += OnCtaClicked;
            }

            if (worldCamera == null) worldCamera = Camera.main;
        }

        /// <summary>Mostra la bolla con solo testo.</summary>
        public void Show(string message) => Show(message, null, null);

        /// <summary>
        /// Mostra la bolla con testo, una call-to-action opzionale e un'icona opzionale a sinistra.
        /// Se <paramref name="onCta"/> è null o <paramref name="ctaLabel"/> è vuoto, il pulsante resta nascosto.
        /// Se <paramref name="icon"/> è null l'icona resta nascosta.
        /// </summary>
        public void Show(string message, string ctaLabel, Action onCta, Sprite icon = null)
        {
            if (bubble == null)
            {
                Bind();
                if (bubble == null)
                {
                    Debug.LogWarning("[SpeechBubble] elemento 'speech-bubble' non trovato: il controller deve stare sulla UIDocument con Source Asset = MainUI.uxml.");
                    return;
                }
            }

            BeginTypewriter(message);

            ctaCallback = onCta;
            bool hasCta = onCta != null && !string.IsNullOrEmpty(ctaLabel);
            if (cta != null)
            {
                cta.text = ctaLabel;
                cta.style.display = hasCta ? DisplayStyle.Flex : DisplayStyle.None;
            }

            SetIcon(icon);

            bubble.style.display = DisplayStyle.Flex;
            visible = true;

            UpdatePosition(); // posiziona subito, prima del primo frame visibile
        }

        // Mostra lo sprite nell'icona a sinistra, o la nasconde se sprite è null.
        private void SetIcon(Sprite sprite)
        {
            if (icon == null) return;

            if (sprite != null)
            {
                icon.style.backgroundImage = new StyleBackground(sprite);
                icon.style.display = DisplayStyle.Flex;
            }
            else
            {
                icon.style.backgroundImage = StyleKeyword.None;
                icon.style.display = DisplayStyle.None;
            }
        }

        // Avvia l'effetto macchina da scrivere; l'auto-hide parte solo a testo completo
        // (vedi RevealComplete). Con typewriterSpeed <= 0 il testo appare tutto subito.
        private void BeginTypewriter(string message)
        {
            fullMessage = message ?? string.Empty;
            revealedChars = 0;

            if (typewriterSpeed <= 0f || fullMessage.Length == 0)
            {
                revealing = false;
                if (label != null) label.text = fullMessage;
                RevealComplete();
                return;
            }

            revealing = true;
            nextCharTime = Time.unscaledTime; // primo carattere al tick successivo
            if (label != null) label.text = string.Empty;
            hideAtTime = -1f; // niente auto-hide finché il testo non è completo
        }

        // Svela i caratteri dovuti per questo frame.
        private void UpdateTypewriter()
        {
            if (!revealing) return;

            float interval = 1f / typewriterSpeed;
            while (revealedChars < fullMessage.Length && Time.unscaledTime >= nextCharTime)
            {
                revealedChars++;
                nextCharTime += interval;
            }

            if (label != null) label.text = fullMessage.Substring(0, revealedChars);

            if (revealedChars >= fullMessage.Length)
            {
                revealing = false;
                RevealComplete();
            }
        }

        // Testo completo: (ri)avvia il timer di auto-hide.
        private void RevealComplete()
        {
            hideAtTime = autoHideSeconds > 0f ? Time.unscaledTime + autoHideSeconds : -1f;
        }

        /// <summary>Fumetto per una NUOVA info appena sbloccata, con la sua icona a sinistra.</summary>
        public void ShowInfoUnlocked(Sprite icon, Action onCta) => Show(infoUnlockedMessage, ctaLabel, onCta, icon);

        /// <summary>Fumetto per un'info già sbloccata (ri-scansione del QR), con la sua icona a sinistra.</summary>
        public void ShowInfoAlreadyKnown(Sprite icon, Action onCta) => Show(infoAlreadyKnownMessage, ctaLabel, onCta, icon);

        /// <summary>Fumetto quando il QR scansionato non corrisponde a nessuna info (nessuna icona, nessuna CTA).</summary>
        public void ShowQrNotRecognized() => Show(qrNotRecognizedMessage);

        /// <summary>Chiude la bolla.</summary>
        public void Hide() => HideImmediate();

        private void HideImmediate()
        {
            visible = false;
            revealing = false;
            ctaCallback = null;
            if (bubble != null) bubble.style.display = DisplayStyle.None;
        }

        private void OnCtaClicked()
        {
            Action callback = ctaCallback;
            HideImmediate();
            callback?.Invoke();
        }

        private void LateUpdate()
        {
            if (!visible) return;

            UpdateTypewriter();

            if (hideAtTime > 0f && Time.unscaledTime >= hideAtTime)
            {
                HideImmediate();
                return;
            }

            UpdatePosition();
        }

        // Projects the anchor's world position into panel space and places the bubble above it.
        private void UpdatePosition()
        {
            if (bubble == null || anchor == null) return;
            if (worldCamera == null)
            {
                worldCamera = Camera.main;
                if (worldCamera == null) return;
            }

            IPanel panel = bubble.panel;
            if (panel == null) return;

            Vector3 worldPoint = anchor.position + worldOffset;

            // Se il punto è dietro la camera, nascondi la bolla per non mostrarla "ribaltata".
            if (worldCamera.WorldToViewportPoint(worldPoint).z <= 0f)
            {
                bubble.style.visibility = Visibility.Hidden;
                return;
            }
            bubble.style.visibility = Visibility.Visible;

            Vector2 panelPoint = RuntimePanelUtils.CameraTransformWorldToPanel(panel, worldPoint, worldCamera);
            panelPoint += panelOffset;

            // Ondeggiamento verticale: la bolla "galleggia" su e giù.
            float floatOffset = floatAmplitude != 0f
                ? Mathf.Sin(Time.unscaledTime * floatSpeed) * floatAmplitude
                : 0f;

            // Centra la bolla orizzontalmente sul punto e appoggia il suo fondo sul punto (sopra la testa).
            bubble.style.position = Position.Absolute;
            bubble.style.left = panelPoint.x - bubble.resolvedStyle.width * 0.5f;
            bubble.style.top  = panelPoint.y - bubble.resolvedStyle.height + floatOffset;
        }
    }
}
