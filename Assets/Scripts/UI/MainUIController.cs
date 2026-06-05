using System.Collections.Generic;
using MuseGameJam.Gameplay;
using MuseGameJam.States;
using MuseGameJam.StateSystem;
using UnityEngine;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainUIController : MonoBehaviour
    {
        // Una voce della tray: l'etichetta mostrata + il prefab Item assegnato a quell'item.
        [System.Serializable]
        public class TrayItemDef
        {
            public string label = "Item";
            public GameObject itemPrefab;
        }

        [Header("Tray")]
        // Template UXML dell'oggetto-item (TrayItem.uxml).
        [SerializeField] VisualTreeAsset trayItemTemplate;
        // Una lista di item per ciascun pulsante della bottom-bar.
        [SerializeField] List<TrayItemDef> foodItems = new List<TrayItemDef>();
        [SerializeField] List<TrayItemDef> cleanItems = new List<TrayItemDef>();
        [SerializeField] List<TrayItemDef> petItems = new List<TrayItemDef>();

        [Header("Drag & drop")]
        [SerializeField] Camera targetCamera;
        // Distanza dalla camera del piano su cui galleggia l'item mentre segue il cursore.
        [SerializeField] float dragDepth = 10f;
        // Se vero, la tray si nasconde durante il drag (l'item resta visibile nello stage).
        [SerializeField] bool hideTrayWhileDragging = false;

        [Header("QR Scanner")]
        // GameObject del QR scanner (con QrScannerController). Parte DISATTIVATO:
        // il pulsante camera lo attiva, "Chiudi"/scan-ok lo ridisattiva.
        [SerializeField] GameObject qrScannerObject;

        [Header("Animazioni musetto")]
        // Animator del musetto (CharacterAnimController). Quando si scansiona una nuova
        // info (o si sblocca qualcosa) -> trigger "Happy".
        [SerializeField] Animator musettoAnimator;

        [Header("Challenges")]
        // Prefab for the Challenges menu UI (UIDocument + ChallengesMenuController).
        // Instantiated by StateChallengesMenu when the "target" button is pressed.
        [SerializeField] GameObject challengesUiPrefab;
        // Parent transform to instantiate the challenges UI under (leave empty for the scene root).
        [SerializeField] Transform challengesUiParent;

        [Header("Unlockables")]
        // Prefab for the Unlockables menu UI (UIDocument + UnlockablesMenuController).
        // Instantiated by StateUnlockablesMenu when the "unlockables" (notebook) button is pressed.
        [SerializeField] GameObject unlockablesUiPrefab;
        // Parent transform to instantiate the unlockables UI under (leave empty for the scene root).
        [SerializeField] Transform unlockablesUiParent;

        Button cameraButton;
        Button targetButton;
        Button unlockablesButton;
        Button foodButton;
        Button cleanButton;
        Button petButton;

        StatGauge foodGauge;
        StatGauge cleanGauge;
        StatGauge petGauge;

        VisualElement rootElement;
        VisualElement itemTray;
        VisualElement trayContent;

        string openCategory;

        // Stato del drag in corso (uno alla volta).
        GameObject spawnedObject;
        Item spawnedItem;
        VisualElement capturedTrayItem;
        int activePointerId;

        void OnEnable()
        {
            rootElement = GetComponent<UIDocument>().rootVisualElement;

            cameraButton = rootElement.Q<Button>("camera-button");
            targetButton = rootElement.Q<Button>("target-button");
            unlockablesButton = rootElement.Q<Button>("unlockables-button");
            foodButton = rootElement.Q<Button>("food-button");
            cleanButton = rootElement.Q<Button>("clean-button");
            petButton = rootElement.Q<Button>("pet-button");

            foodGauge = rootElement.Q<StatGauge>("food-gauge");
            cleanGauge = rootElement.Q<StatGauge>("clean-gauge");
            petGauge = rootElement.Q<StatGauge>("pet-gauge");

            itemTray = rootElement.Q<VisualElement>("item-tray");
            trayContent = rootElement.Q<VisualElement>("tray-content");

            foodButton.clicked += OnFoodClicked;
            cleanButton.clicked += OnCleanClicked;
            petButton.clicked += OnPetClicked;
            if (cameraButton != null) cameraButton.clicked += OnCameraClicked;
            if (targetButton != null) targetButton.clicked += OnTargetClicked;
            if (unlockablesButton != null) unlockablesButton.clicked += OnUnlockablesClicked;

            // Lo scanner parte chiuso (disattivato).
            if (qrScannerObject != null) qrScannerObject.SetActive(false);

            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        void OnDisable()
        {
            if (foodButton != null) foodButton.clicked -= OnFoodClicked;
            if (cleanButton != null) cleanButton.clicked -= OnCleanClicked;
            if (petButton != null) petButton.clicked -= OnPetClicked;
            if (cameraButton != null) cameraButton.clicked -= OnCameraClicked;
            if (targetButton != null) targetButton.clicked -= OnTargetClicked;
            if (unlockablesButton != null) unlockablesButton.clicked -= OnUnlockablesClicked;
        }

        // Pulsante camera: apre il QR scanner come OVERLAY sullo stack di stati.
        // StateCamera nasconde la main UI, mette in pausa la main e attiva lo scanner;
        // su "Chiudi" o scan riuscito fa PopOverlay e si torna alla main.
        void OnCameraClicked()
        {
            if (qrScannerObject == null) return;
            if (GameStateMachine.Instance == null)
            {
                Debug.LogWarning("MainUIController: nessuna GameStateMachine in scena per aprire la camera.");
                return;
            }

            // Guardia anti-spam: se c'è già uno StateCamera aperto, non pusharne altri.
            if (GameStateMachine.Instance.HasOverlay<StateCamera>()) return;

            // Passa anche QUESTA UI così lo StateCamera la nasconde mentre la camera è aperta.
            var cameraState = new StateCamera(qrScannerObject, gameObject);
            cameraState.OnUrlScanned += HandleUrlScanned;
            GameStateMachine.Instance.PushState(cameraState);
        }

        // L'URL letto dal QR arriva qui: per ora lo logghiamo; lo passeremo poi a un altro script.
        void HandleUrlScanned(string url)
        {
            Debug.Log($"[MainUI] URL dal QR: {url}");
            // Nuova info scansionata -> il musetto è felice (trigger del controller).
            TriggerHappy("QR scansionato");
        }

        // Chiamabile da qualsiasi script quando si ottiene/sblocca un nuovo oggetto:
        // fa partire l'animazione "felice" del musetto.
        public void CelebrateNewItem()
        {
            TriggerHappy("nuovo oggetto");
        }

        // Centralizza il trigger Happy + log diagnostico.
        void TriggerHappy(string motivo)
        {
            if (musettoAnimator == null)
            {
                Debug.LogWarning($"[Anim] musettoAnimator NON assegnato nel MainUIController ({motivo}): trascina il musetto nel campo in Inspector.");
                return;
            }
            var st = musettoAnimator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[Anim] SetTrigger(Happy) [{motivo}]. Stato attuale hash={st.fullPathHash} " +
                      $"normalizedTime={st.normalizedTime:F2}. Se non transita: togli 'Has Exit Time' alla transizione Idle->Felice.");
            musettoAnimator.SetTrigger("Happy");
        }

        // Target button: opens the Challenges menu as an OVERLAY on the state stack.
        // StateChallengesMenu instantiates the challenges UI and pauses the main state;
        // on "Close" or back it calls PopOverlay and returns to the main state.
        void OnTargetClicked()
        {
            if (challengesUiPrefab == null)
            {
                Debug.LogWarning("MainUIController: challengesUiPrefab not assigned in Inspector.");
                return;
            }
            if (GameStateMachine.Instance == null)
            {
                Debug.LogWarning("MainUIController: no GameStateMachine in scene to open the challenges menu.");
                return;
            }

            // Anti-spam guard: if a challenges menu is already open, don't push another.
            if (GameStateMachine.Instance.HasOverlay<StateChallengesMenu>()) return;

            // Pass this UI so the overlay hides it while open (it lives on its own UIDocument).
            var challengesState = new StateChallengesMenu(challengesUiPrefab, challengesUiParent, gameObject);
            GameStateMachine.Instance.PushState(challengesState);
        }

        // Unlockables (notebook) button: opens the Unlockables menu as an OVERLAY on the state stack.
        // StateUnlockablesMenu instantiates the unlockables UI and pauses the main state;
        // on "Close" or back it calls PopOverlay and returns to the main state.
        void OnUnlockablesClicked()
        {
            if (unlockablesUiPrefab == null)
            {
                Debug.LogWarning("MainUIController: unlockablesUiPrefab not assigned in Inspector.");
                return;
            }
            if (GameStateMachine.Instance == null)
            {
                Debug.LogWarning("MainUIController: no GameStateMachine in scene to open the unlockables menu.");
                return;
            }

            // Anti-spam guard: if an unlockables menu is already open, don't push another.
            if (GameStateMachine.Instance.HasOverlay<StateUnlockablesMenu>()) return;

            // Pass this UI so the overlay hides it while open (it lives on its own UIDocument).
            var unlockablesState = new StateUnlockablesMenu(unlockablesUiPrefab, unlockablesUiParent, gameObject);
            GameStateMachine.Instance.PushState(unlockablesState);
        }
        // API per il gameplay: imposta il livello (0..1) di ciascun gauge.
        public void SetFoodLevel(float value) { if (foodGauge != null) foodGauge.value = value; }
        public void SetCleanLevel(float value) { if (cleanGauge != null) cleanGauge.value = value; }
        public void SetPetLevel(float value) { if (petGauge != null) petGauge.value = value; }

        // Ogni categoria mappa a una CareAction: l'item spawnato la riceve, così la
        // DropArea fa partire l'animazione giusta (food->Eat, clean->Clean, pet->Pet).
        void OnFoodClicked() => ToggleTray("FOOD", foodItems, CareAction.Eat);
        void OnCleanClicked() => ToggleTray("CLEAN", cleanItems, CareAction.Clean);
        void OnPetClicked() => ToggleTray("PET", petItems, CareAction.Pet);

        void ToggleTray(string category, List<TrayItemDef> items, CareAction action)
        {
            if (openCategory == category)
            {
                HideTray();
                return;
            }

            openCategory = category;
            itemTray.style.display = DisplayStyle.Flex;
            PopulateTray(items, action);
        }

        void HideTray()
        {
            openCategory = null;
            itemTray.style.display = DisplayStyle.None;
            trayContent.Clear();
        }

        // Riempie la banda clonando il template, un item per ogni TrayItemDef della lista scelta.
        void PopulateTray(List<TrayItemDef> items, CareAction action)
        {
            trayContent.Clear();

            if (trayItemTemplate == null)
            {
                Debug.LogWarning("MainUIController: trayItemTemplate non assegnato in Inspector.");
                return;
            }

            foreach (var def in items)
            {
                var instance = trayItemTemplate.Instantiate();
                var itemRoot = instance.Q<VisualElement>("item") ?? instance;

                var label = instance.Q<Label>("item-label");
                if (label != null) label.text = def.label;

                // Gli elementi interni non devono intercettare i pointer event: li gestisce il root dell'item.
                var icon = instance.Q<Button>("item-icon");
                if (icon != null) icon.pickingMode = PickingMode.Ignore;
                if (label != null) label.pickingMode = PickingMode.Ignore;

                var prefab = def.itemPrefab;
                itemRoot.RegisterCallback<PointerDownEvent>(evt => OnItemPointerDown(evt, itemRoot, prefab, action));
                itemRoot.RegisterCallback<PointerUpEvent>(OnItemPointerUp);
                // Su touch l'OS può annullare il puntatore (PointerCancel) invece di rilasciarlo:
                // senza questo, lo stato del drag resterebbe appeso e bloccherebbe ogni drag successivo.
                itemRoot.RegisterCallback<PointerCancelEvent>(OnItemPointerCancel);

                trayContent.Add(instance);
            }
        }

        void OnItemPointerDown(PointerDownEvent evt, VisualElement trayItem, GameObject prefab, CareAction action)
        {
            if (spawnedObject != null) return; // un drag alla volta

            if (prefab == null)
            {
                Debug.LogWarning("MainUIController: questo TrayItem non ha un itemPrefab assegnato.");
                return;
            }

            capturedTrayItem = trayItem;
            activePointerId = evt.pointerId;
            trayItem.CapturePointer(evt.pointerId); // così i PointerUp arrivano qui anche fuori dall'item

            spawnedObject = Instantiate(prefab);
            spawnedItem = spawnedObject.GetComponent<Item>();
            if (spawnedItem != null)
            {
                // L'azione la decide la categoria (food/clean/pet), non il prefab:
                // così gli stessi prefab base servono tutte le categorie.
                spawnedItem.SetAction(action);
                spawnedItem.BeginDrag(targetCamera != null ? targetCamera : Camera.main, dragDepth);
            }
            else
                Debug.LogWarning("MainUIController: il prefab spawnato non ha un componente Item.");

            if (hideTrayWhileDragging) itemTray.style.display = DisplayStyle.None;

            evt.StopPropagation();
        }

        void OnItemPointerUp(PointerUpEvent evt)
        {
            if (spawnedObject == null || evt.pointerId != activePointerId) return;

            // L'Item gestisce il proprio rilascio (di base sparisce); la DropArea ha già reagito via trigger.
            if (spawnedItem != null) spawnedItem.Drop();
            else Destroy(spawnedObject);

            EndDrag();
        }

        // Puntatore annullato dall'OS (tipico su touch): scartiamo l'item senza droparlo
        // e ripristiniamo lo stato, così la tray resta utilizzabile.
        void OnItemPointerCancel(PointerCancelEvent evt)
        {
            if (spawnedObject == null || evt.pointerId != activePointerId) return;

            Destroy(spawnedObject);
            EndDrag();
        }

        // Ripristino comune dello stato del drag (rilascio o annullamento).
        void EndDrag()
        {
            spawnedObject = null;
            spawnedItem = null;

            if (hideTrayWhileDragging && openCategory != null)
                itemTray.style.display = DisplayStyle.Flex;

            if (capturedTrayItem != null)
            {
                capturedTrayItem.ReleasePointer(activePointerId);
                capturedTrayItem = null;
            }
        }
    }
}
