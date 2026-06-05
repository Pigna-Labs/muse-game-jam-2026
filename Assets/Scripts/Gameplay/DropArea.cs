using UnityEngine;

namespace MuseGameJam.Gameplay
{
    // Base per l'area di rilascio. Inoltra i trigger 2D filtrandoli sui soli Droppable,
    // così le sottoclassi non si occupano dei collider grezzi:
    //  - food  -> override OnDroppableEnter per "riconoscere" cosa è entrato
    //  - clean -> override OnDroppableStay per reagire a quanto/dove viene trascinato
    //  - pet   -> come clean
    [RequireComponent(typeof(Collider2D))]
    public class DropArea : MonoBehaviour
    {
        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var droppable = other.GetComponentInParent<Droppable>();
            if (droppable != null) OnDroppableEnter(droppable);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var droppable = other.GetComponentInParent<Droppable>();
            if (droppable != null) OnDroppableStay(droppable);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var droppable = other.GetComponentInParent<Droppable>();
            if (droppable != null) OnDroppableExit(droppable);
        }

        protected virtual void OnDroppableEnter(Droppable droppable) { }
        protected virtual void OnDroppableStay(Droppable droppable) { }
        protected virtual void OnDroppableExit(Droppable droppable) { }
    }
}
