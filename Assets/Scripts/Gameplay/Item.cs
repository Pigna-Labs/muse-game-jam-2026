using UnityEngine;
using UnityEngine.InputSystem;

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
        var pointer = Pointer.current;
        if (pointer == null || cam == null) return;
        var screen = pointer.position.ReadValue();
        var world = cam.ScreenToWorldPoint(new Vector3(screen.x, screen.y, dragDepth));
        transform.position = world;
        OnDragMove(world);
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
