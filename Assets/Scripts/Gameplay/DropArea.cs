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
        // Quanto sale (su 1) la barra Food quando un cibo viene rilasciato dentro l'area.
        [SerializeField] float foodGainPerDrop = 0.25f;

        [Header("Animazioni musetto")]
        // Animator del musetto (CharacterAnimController). Quando un cibo (Droppable)
        // viene rilasciato dentro l'area, fa partire il trigger "Eating".
        // Se lasciato vuoto, nessuna animazione (resta solo il log).
        [SerializeField] Animator musettoAnimator;

        // Emesso quando un'azione di cura viene eseguita (cibo droppato, pulizia/coccola
        // iniziata). Il MainUIController lo ascolta per nascondere l'hint di quella
        // categoria una volta che il giocatore ha capito cosa fare. Statico così non
        // serve collegare riferimenti in Inspector.
        public static event System.Action<CareAction> CareActionPerformed;

        [Header("VFX pulizia")]
        // VFX di bolle di sapone (sul child "Particle System"). Emette mentre pulisci
        // e si spegne al rilascio. Se vuoto, nessun effetto (resta solo l'animazione).
        [SerializeField] CleaningBubblesVfx cleaningVfx;

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

            // Azioni "continue" (bool): restano attive finché l'item è dentro l'area.
            // Coccole e pulizia partono già mentre trascini, non al rilascio.
            switch (item.Action)
            {
                case CareAction.Pet:   SetAnimBool("IsPetting", true);  break;
                case CareAction.Clean: SetAnimBool("IsCleaning", true); cleaningVfx?.Play(); break;
            }

            // Pet/Clean contano come "eseguite" appena l'item entra nell'area (primo contatto).
            if (item.Action == CareAction.Pet || item.Action == CareAction.Clean)
                CareActionPerformed?.Invoke(item.Action);
        }

        void OnTriggerStay2D(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item is not Draggable) return;

            float moved = 0f;
            if (lastPos.TryGetValue(item, out var prev))
                moved = Vector3.Distance(prev, item.transform.position);
            lastPos[item] = item.transform.position;

            // Incremento di questo frame: dipende da quanto muovi e da quanto resti dentro.
            float delta = moved * movePerUnit + Time.deltaTime * fillPerSecond;

            // Progresso normalizzato che riempie da 0 a 1 e si ferma a 1 (minigioco locale).
            progress[item] = Mathf.Clamp01(progress[item] + delta);

            // Pulizia/coccole riempiono la barra corrispondente in tempo reale, mentre interagisci.
            item.ApplyCare(delta);
        }

        void OnTriggerExit2D(Collider2D other)
        {
            var item = other.GetComponentInParent<Item>();
            if (item != null) Forget(item);
        }

        // Chiamato quando un item tracciato viene rilasciato dentro l'area.
        void OnItemDropped(Item item)
        {
            Debug.Log($"Droppato dentro l'area: {CleanName(item)} (azione={item.Action})");

            // Azione "istantanea" (trigger): il cibo fa partire l'anim "mangia" al rilascio
            // e alza la barra Food.
            if (item.Action == CareAction.Eat)
            {
                SetAnimTrigger("Eating");
                item.ApplyCare(foodGainPerDrop);
                CareActionPerformed?.Invoke(CareAction.Eat); // cibo droppato = azione eseguita
            }

            Forget(item); // Forget azzera anche gli eventuali bool continui (vedi sotto)
        }

        void Forget(Item item)
        {
            if (!tracked.Remove(item)) return;
            item.Dropped -= OnItemDropped;
            lastPos.Remove(item);
            progress.Remove(item);

            // Uscendo/rilasciando, spegni le azioni continue accese all'ingresso.
            switch (item.Action)
            {
                case CareAction.Pet:   SetAnimBool("IsPetting", false);  break;
                case CareAction.Clean: SetAnimBool("IsCleaning", false); cleaningVfx?.Stop(); break;
            }
        }

        // ---- Helper Animator (con log diagnostico) --------------------------

        void SetAnimTrigger(string param)
        {
            if (musettoAnimator == null)
            {
                Debug.LogWarning($"[Anim] musettoAnimator NON assegnato nel DropArea (trigger '{param}'): trascina il musetto nel campo in Inspector.");
                return;
            }
            var st = musettoAnimator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[Anim] SetTrigger({param}). statoHash={st.fullPathHash} normalizedTime={st.normalizedTime:F2}");
            musettoAnimator.SetTrigger(param);
        }

        void SetAnimBool(string param, bool value)
        {
            if (musettoAnimator == null)
            {
                Debug.LogWarning($"[Anim] musettoAnimator NON assegnato nel DropArea (bool '{param}'): trascina il musetto nel campo in Inspector.");
                return;
            }
            Debug.Log($"[Anim] SetBool({param}, {value})");
            musettoAnimator.SetBool(param, value);
        }

        static string CleanName(Item item) => item.name.Replace("(Clone)", "").Trim();
    }
}
