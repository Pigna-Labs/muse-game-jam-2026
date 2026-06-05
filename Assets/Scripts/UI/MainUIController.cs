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

        [Header("Challenges")]
        // Prefab for the Challenges menu UI (UIDocument + ChallengesMenuController).
        // Instantiated by StateChallengesMenu when the "target" button is pressed.
        [SerializeField] GameObject challengesUiPrefab;
        // Parent transform to instantiate the challenges UI under (leave empty for the scene root).
        [SerializeField] Transform challengesUiParent;

        Button cameraButton;
        Button targetButton;
        Button foodButton;
        Button cleanButton;
        Button petButton;

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
            foodButton = rootElement.Q<Button>("food-button");
            cleanButton = rootElement.Q<Button>("clean-button");
            petButton = rootElement.Q<Button>("pet-button");

            itemTray = rootElement.Q<VisualElement>("item-tray");
            trayContent = rootElement.Q<VisualElement>("tray-content");

            foodButton.clicked += OnFoodClicked;
            cleanButton.clicked += OnCleanClicked;
            petButton.clicked += OnPetClicked;
            if (cameraButton != null) cameraButton.clicked += OnCameraClicked;
            if (targetButton != null) targetButton.clicked += OnTargetClicked;

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

        void OnFoodClicked() => ToggleTray("FOOD", foodItems);
        void OnCleanClicked() => ToggleTray("CLEAN", cleanItems);
        void OnPetClicked() => ToggleTray("PET", petItems);

        void ToggleTray(string category, List<TrayItemDef> items)
        {
            if (openCategory == category)
            {
                HideTray();
                return;
            }

            openCategory = category;
            itemTray.style.display = DisplayStyle.Flex;
            PopulateTray(items);
        }

        void HideTray()
        {
            openCategory = null;
            itemTray.style.display = DisplayStyle.None;
            trayContent.Clear();
        }

        // Riempie la banda clonando il template, un item per ogni TrayItemDef della lista scelta.
        void PopulateTray(List<TrayItemDef> items)
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
                itemRoot.RegisterCallback<PointerDownEvent>(evt => OnItemPointerDown(evt, itemRoot, prefab));
                itemRoot.RegisterCallback<PointerUpEvent>(OnItemPointerUp);

                trayContent.Add(instance);
            }
        }

        void OnItemPointerDown(PointerDownEvent evt, VisualElement trayItem, GameObject prefab)
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
                spawnedItem.BeginDrag(targetCamera != null ? targetCamera : Camera.main, dragDepth);
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
