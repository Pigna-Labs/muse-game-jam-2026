using System.Collections.Generic;
using UnityEngine;

namespace MuseGameJam.Gameplay
{
    // Area di rilascio che si adatta al tipo di Item che entra nel suo collider:
    //  - Droppable -> quando viene rilasciato DENTRO l'area, stampa il suo nome.
    //  - Draggable -> mentre lo trascini dentro, riempie un progresso 0→1 in base a
    //                 quanto si muove e a quanto tempo resta (minigioco "pulisci la creatura").
    [RequireComponent(typeof(Collider2D))]
    public class DropArea : MonoBehaviour
    {
        [Header("Riempimento progresso 0→1 (Draggable)")]
        // Quanto progresso (su 1) aggiunge ogni unità di movimento dentro l'area. Tienilo piccolo.
        [SerializeField] float movePerUnit = 0.05f;
        // Quanto progresso (su 1) aggiunge ogni secondo passato dentro l'area. Tienilo piccolo.
        [SerializeField] float fillPerSecond = 0.02f;

        readonly HashSet<Item> tracked = new HashSet<Item>();
        readonly Dictionary<Item, Vector3> lastPos = new Dictionary<Item, Vector3>();
        readonly Dictionary<Item, float> progress = new Dictionary<Item, float>();

        void Reset()
        {
            GetComponent<Collider2D>().isTrigger = true;
        }

        void OnTriggerEnter2D(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item == null || !tracked.Add(item)) return;

            lastPos[item] = item.transform.position;
            item.Dropped += OnItemDropped; // per sapere se viene rilasciato mentre è qui dentro

            if (item is Draggable)
                progress[item] = 0f;
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item is not Draggable) return;

            float moved = 0f;
            if (lastPos.TryGetValue(item, out var prev))
                moved = Vector3.Distance(prev, item.transform.position);
            lastPos[item] = item.transform.position;

            // Progresso normalizzato che riempie da 0 a 1 e si ferma a 1.
            progress[item] = Mathf.Clamp01(progress[item] + moved * movePerUnit + Time.deltaTime * fillPerSecond);
            Debug.Log($"{CleanName(item)}: {progress[item]:F3}");
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item != null) Forget(item);
        }

        // Chiamato quando un item tracciato viene rilasciato: se è un Droppable, lo riconosciamo.
        void OnItemDropped(Item item)
        {
            if (item is Droppable)
                Debug.Log($"Droppato dentro l'area: {CleanName(item)}");
            Forget(item);
        }

        void Forget(Item item)
        {
            if (!tracked.Remove(item)) return;
            item.Dropped -= OnItemDropped;
            lastPos.Remove(item);
            progress.Remove(item);
        }

        static string CleanName(Item item) => item.name.Replace("(Clone)", "").Trim();
    }
}
