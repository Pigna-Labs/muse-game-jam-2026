using System.Collections.Generic;
using MuseGameJam.Gameplay;
using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainUIController : MonoBehaviour
    {
        // Una voce della tray: l'etichetta mostrata + il droppable assegnato a quell'item.
        [System.Serializable]
        public class TrayItemDef
        {
            public string label = "Item";
            public GameObject droppablePrefab;
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
        // Distanza dalla camera del piano su cui galleggia il droppable mentre segue il cursore.
        [SerializeField] float dragDepth = 10f;
        // Se vero, la tray si nasconde durante il drag (l'oggetto resta visibile nello stage).
        [SerializeField] bool hideTrayWhileDragging = false;

        Button shopButton;
        Button cameraButton;
        Button foodButton;
        Button cleanButton;
        Button petButton;

        VisualElement rootElement;
        VisualElement itemTray;
        VisualElement trayContent;

        string openCategory;

        // Stato del drag in corso (uno alla volta).
        GameObject activeDroppable;
        Droppable activeDroppableComp;
        VisualElement activeItem;
        int activePointerId;

        void OnEnable()
        {
            rootElement = GetComponent<UIDocument>().rootVisualElement;

            shopButton = rootElement.Q<Button>("shop-button");
            cameraButton = rootElement.Q<Button>("camera-button");
            foodButton = rootElement.Q<Button>("food-button");
            cleanButton = rootElement.Q<Button>("clean-button");
            petButton = rootElement.Q<Button>("pet-button");

            itemTray = rootElement.Q<VisualElement>("item-tray");
            trayContent = rootElement.Q<VisualElement>("tray-content");

            foodButton.clicked += OnFoodClicked;
            cleanButton.clicked += OnCleanClicked;
            petButton.clicked += OnPetClicked;

            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        void OnDisable()
        {
            if (foodButton != null) foodButton.clicked -= OnFoodClicked;
            if (cleanButton != null) cleanButton.clicked -= OnCleanClicked;
            if (petButton != null) petButton.clicked -= OnPetClicked;
        }

        void Update()
        {
            // Il droppable segue il cursore finché c'è un drag attivo.
            if (activeDroppable == null) return;
            var pointer = Pointer.current;
            if (pointer == null) return;
            MoveDroppableTo(pointer.position.ReadValue());
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

                var prefab = def.droppablePrefab;
                itemRoot.RegisterCallback<PointerDownEvent>(evt => OnItemPointerDown(evt, itemRoot, prefab));
                itemRoot.RegisterCallback<PointerUpEvent>(OnItemPointerUp);

                trayContent.Add(instance);
            }
        }

        void OnItemPointerDown(PointerDownEvent evt, VisualElement item, GameObject prefab)
        {
            if (activeDroppable != null) return; // un drag alla volta

            if (prefab == null)
            {
                Debug.LogWarning("MainUIController: questo TrayItem non ha un droppablePrefab assegnato.");
                return;
            }

            activeItem = item;
            activePointerId = evt.pointerId;
            item.CapturePointer(evt.pointerId); // così i PointerUp arrivano qui anche fuori dall'item

            activeDroppable = Instantiate(prefab);
            activeDroppableComp = activeDroppable.GetComponent<Droppable>();

            var pointer = Pointer.current;
            if (pointer != null) MoveDroppableTo(pointer.position.ReadValue());

            if (activeDroppableComp != null) activeDroppableComp.OnDragBegin();

            if (hideTrayWhileDragging) itemTray.style.display = DisplayStyle.None;

            evt.StopPropagation();
        }

        void OnItemPointerUp(PointerUpEvent evt)
        {
            if (activeDroppable == null || evt.pointerId != activePointerId) return;

            // La logica di esito (riconoscimento food, accumulo clean/pet) l'ha gestita la DropArea
            // tramite i trigger durante il drag. Qui chiudiamo solo il drag.
            if (activeDroppableComp != null) activeDroppableComp.OnDragEnd();
            else Destroy(activeDroppable);

            activeDroppable = null;
            activeDroppableComp = null;

            if (hideTrayWhileDragging && openCategory != null)
                itemTray.style.display = DisplayStyle.Flex;

            if (activeItem != null)
            {
                activeItem.ReleasePointer(activePointerId);
                activeItem = null;
            }
        }

        void MoveDroppableTo(Vector2 screenPos)
        {
            var cam = targetCamera != null ? targetCamera : Camera.main;
            if (cam == null) return;
            var world = cam.ScreenToWorldPoint(new Vector3(screenPos.x, screenPos.y, dragDepth));
            activeDroppable.transform.position = world;
            if (activeDroppableComp != null) activeDroppableComp.OnDragUpdate(world);
        }
    }
}
