using UnityEngine;
using UnityEngine.InputSystem;

namespace MuseGameJam.Gameplay
{
    // Cosa fa questo item al musetto quando viene usato sull'area.
    // La DropArea lo legge per far partire l'animazione/parametro giusto.
    public enum CareAction
    {
        None,   // nessuna animazione
        Eat,    // cibo  -> trigger "Eating"
        Pet,    // coccole -> bool "IsPetting" (mentre è nell'area)
        Clean,  // pulizia -> bool "IsCleaning" (mentre è nell'area)
    }

    // Comportamento base di un oggetto preso dalla tray:
    //  - segue il cursore mentre è "in mano"
    //  - al rilascio sparisce
    // Le specializzazioni (Draggable, Droppable) aggiungono la loro interazione tramite gli hook.
    // Serve un Collider2D (trigger) sul prefab per farsi rilevare dalle DropArea; il Rigidbody2D
    // (kinematic) lo richiede questo componente e fa scattare i trigger su un oggetto in movimento.
    [RequireComponent(typeof(Rigidbody2D))]
    public class Item : MonoBehaviour
    {
        // Emesso quando l'item viene rilasciato, appena prima che sparisca.
        // Le DropArea che lo contengono lo ascoltano per reagire al "drop".
        public event System.Action<Item> Dropped;

        // Emesso quando l'item contribuisce alla cura del musetto (cibo dato, strofinata, coccola):
        // porta l'azione e di quanto far salire (0..1) la barra corrispondente. La DropArea lo
        // scatena (al rilascio per il cibo, in continuo per pulizia/coccole); la UI alza il gauge.
        public event System.Action<CareAction, float> CareApplied;

        // Chiamato dalla DropArea quando l'item produce il suo effetto sul musetto.
        public void ApplyCare(float amount) => CareApplied?.Invoke(action, amount);

        [Tooltip("Cosa fa questo item al musetto: la DropArea fa partire l'anim corrispondente. " +
                 "Es. cibo=Eat, spugna=Clean, mano=Pet.")]
        [SerializeField] CareAction action = CareAction.None;

        // Azione di cura associata a questo item (per le animazioni del musetto).
        public CareAction Action => action;

        // Permette di impostare l'azione allo spawn (es. il MainUI la deriva dalla
        // categoria food/clean/pet), senza dover serializzare prefab separati.
        public void SetAction(CareAction value) => action = value;

        Camera cam;
        float dragDepth;
        bool following;

        void Reset()
        {
            var rb = GetComponent<Rigidbody2D>();
            rb.bodyType = RigidbodyType2D.Kinematic;
            rb.gravityScale = 0f;
        }

        // Chiamato dal controller appena l'oggetto viene preso dalla tray.
        public void BeginDrag(Camera dragCamera, float depth)
        {
            cam = dragCamera;
            dragDepth = depth;
            following = true;
            OnPicked();
            FollowCursor(); // snap immediato: evita il frame iniziale fuori posizione
        }

        void Update()
        {
            if (following) FollowCursor();
        }

        void FollowCursor()
        {
            if (cam == null || !TryGetPointerPosition(out var screen)) return;
            var world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, dragDepth));
            transform.position = world;
            OnDragMove(world);
        }

        // Posizione schermo del puntatore attivo, robusta su mobile.
        // Su touch leggiamo il tocco primario direttamente dalla Touchscreen: Pointer.current
        // su Android può puntare a un Mouse fantasma o restare stale, quindi non seguirebbe il dito.
        // Su desktop/editor non c'è Touchscreen e si usa Pointer.current (il mouse).
        static bool TryGetPointerPosition(out Vector2 screenPosition)
        {
            var touch = Touchscreen.current;
            if (touch != null && touch.primaryTouch.press.isPressed)
            {
                screenPosition = touch.primaryTouch.position.ReadValue();
                return true;
            }

            var pointer = Pointer.current;
            if (pointer != null)
            {
                screenPosition = pointer.position.ReadValue();
                return true;
            }

            screenPosition = default;
            return false;
        }

        // Chiamato dal controller al rilascio. Base: l'oggetto sparisce.
        public virtual void Drop()
        {
            following = false;
            OnDrop();
            Dropped?.Invoke(this); // l'area reagisce mentre l'oggetto è ancora valido
            Destroy(gameObject);
        }

        // --- Hook per le sottoclassi ---
        protected virtual void OnPicked() { }
        protected virtual void OnDragMove(Vector3 worldPosition) { }
        protected virtual void OnDrop() { }
    }
}
