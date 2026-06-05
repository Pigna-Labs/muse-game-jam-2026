using UnityEngine;

// Base per gli oggetti trascinabili dalla tray. Il controller li sposta e chiama questi hook;
// le sottoclassi (food / clean / pet) mettono qui la loro logica e interagiscono con la
// DropArea tramite trigger 2D. Serve un Collider2D (trigger) + Rigidbody2D per far scattare
// i trigger su un oggetto in movimento.
[RequireComponent(typeof(Rigidbody2D))]
public class Droppable : MonoBehaviour
{
    void Reset()
    {
        // Configurazione tipica per un oggetto che si muove a mano ma deve generare trigger.
        var rb = GetComponent<Rigidbody2D>();
        rb.bodyType = RigidbodyType2D.Kinematic;
        rb.gravityScale = 0f;
    }

    // Preso dalla tray.
    public virtual void OnDragBegin() { }

    // Ogni frame mentre segue il cursore (posizione mondo aggiornata).
    public virtual void OnDragUpdate(Vector3 worldPosition) { }

    // Rilasciato. Default: l'oggetto sparisce (la DropArea ha già reagito via trigger).
    public virtual void OnDragEnd() { Destroy(gameObject); }
}
