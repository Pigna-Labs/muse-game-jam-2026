using UnityEngine;

// Oggetto che viene "lasciato" su una DropArea e lì riconosciuto (es. cibo per FOOD).
// Il riconoscimento del tipo avviene lato DropArea (OnItemEnter / al rilascio); qui si mette
// ciò che riguarda l'oggetto nel momento in cui viene droppato.
public class Droppable : Item
{
    protected override void OnDrop()
    {
        // es. notifica "sono stato lasciato". L'area riconosce il tipo via trigger.
    }
}
