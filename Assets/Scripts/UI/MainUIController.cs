using System.Collections.Generic;
using MuseGameJam.Gameplay;
using MuseGameJam.States;
using MuseGameJam.StateSystem;
using UnityEngine;
using UnityEngine.UIElements;
#if UNITY_EDITOR
using UnityEngine.InputSystem;
#endif

namespace MuseGameJam.UI
{
    [RequireComponent(typeof(UIDocument))]
    public class MainUIController : MonoBehaviour
    {
        [Header("Tray")]
        // UXML template for a tray item (TrayItem.uxml).
        [SerializeField] VisualTreeAsset trayItemTemplate;
        // Source of unlockable entries: the tray shows the icons of UNLOCKED items in each
        // category (Foods -> food, Soaps -> clean, Toys -> pet).
        [SerializeField] Unlockables unlockables;

        // Base prefab spawned at selection time: NOT serialized; loaded from Resources and
        // picked by category. Food=Droppable (left on the area), Clean/Pet=Draggable (rubbed
        // on the area). The dragged object's icon comes from the selected item (see
        // OnItemPointerDown).
        const string DroppableResourcePath = "Interaction/Droppable";
        const string DraggableResourcePath = "Interaction/Draggable";
        GameObject droppablePrefab;
        GameObject draggablePrefab;

        [Header("Drag & drop")]
        [SerializeField] Camera targetCamera;
        // Distance from the camera of the plane the item floats on while following the cursor.
        [SerializeField] float dragDepth = 10f;
        // World-space size of the dragged object's largest side. Item sprites have different
        // pixel counts: without normalizing, a large one would fill the screen. The object is
        // scaled so the sprite occupies this size.
        [SerializeField] float draggedItemWorldSize = 1.5f;

        [Header("QR Scanner")]
        // QR scanner GameObject (carries QrScannerController). Starts DISABLED: the camera
        // button activates it, "Close"/scan-ok disables it again.
        [SerializeField] GameObject qrScannerObject;

        [Header("Musetto animations")]
        // Musetto animator (CharacterAnimController). When a new info is scanned (or
        // something is unlocked) -> "Happy" trigger.
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

        [Header("Speech bubble")]
        // Speech bubble above musetto (lives on the same UIDocument as the MainUI).
        // Texts and the CTA label are configured on the SpeechBubbleController.
        [SerializeField] SpeechBubbleController speechBubble;

        // Raised when the user presses the speech bubble's CTA: hook the info UI here
        // when it gets implemented.
        public event System.Action<InfoSO> InfoUiRequested;

        Button cameraButton;
        Button targetButton;
        Button unlockablesButton;
        Button foodButton;
        Button cleanButton;
        Button petButton;
        Button audioToggleButton;

        StatGauge foodGauge;
        StatGauge cleanGauge;
        StatGauge petGauge;

        VisualElement rootElement;
        VisualElement itemTray;
        VisualElement trayContent;
        VisualElement trayHint;
        Label trayHintLabel;

        // Hint text for each category (what the player should do).
        const string FoodHint = "Dai da mangiare a Musetto!";
        const string CleanHint = "Lava Musetto!";
        const string PetHint = "Coccola Musetto!";

        // Care actions the player has already completed at least once: their hint stays
        // hidden afterwards. While an action is NOT in this set, the hint keeps appearing
        // when the matching menu is opened (until the action is completed).
        readonly HashSet<CareAction> careDone = new HashSet<CareAction>();

        // Open tray: which category and whether it is visible. The tray's lifecycle is owned
        // by StateInteractionMenu (ShowTray/HideTray); here we just track the current state.
        bool trayOpen;
        CareAction openAction;

        // Dismiss request for the tray (object picked up or tap outside): listened to by
        // StateInteractionMenu, which calls PopOverlay (-> HideTray).
        public event System.Action InteractionDismissRequested;

        // Active drag state (one at a time). The pointer is captured on the root, not on the
        // individual tray item: that way closing the tray at drag-start does not break release.
        GameObject spawnedObject;
        Item spawnedItem;
        VisualElement capturedElement;
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
            audioToggleButton = rootElement.Q<Button>("audio-toggle-button");

            foodGauge = rootElement.Q<StatGauge>("food-gauge");
            cleanGauge = rootElement.Q<StatGauge>("clean-gauge");
            petGauge = rootElement.Q<StatGauge>("pet-gauge");

            itemTray = rootElement.Q<VisualElement>("item-tray");
            trayContent = rootElement.Q<VisualElement>("tray-content");
            trayHint = rootElement.Q<VisualElement>("tray-hint");
            trayHintLabel = rootElement.Q<Label>("tray-hint-label");

            foodButton.clicked += OnFoodClicked;
            cleanButton.clicked += OnCleanClicked;
            petButton.clicked += OnPetClicked;
            if (cameraButton != null) cameraButton.clicked += OnCameraClicked;
            if (targetButton != null) targetButton.clicked += OnTargetClicked;
            if (unlockablesButton != null) unlockablesButton.clicked += OnUnlockablesClicked;
            if (audioToggleButton != null) audioToggleButton.clicked += OnAudioToggleClicked;

            // The drag captures the pointer on the ROOT (not on the tray item): that way
            // closing the tray at drag-start does not lose the release. Up/Cancel land here.
            rootElement.RegisterCallback<PointerUpEvent>(OnPointerUp);
            rootElement.RegisterCallback<PointerCancelEvent>(OnPointerCancel);
            // Tap outside the tray = dismiss. Registered in the capture phase (TrickleDown)
            // so no descendant can swallow the event before we evaluate it.
            rootElement.RegisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);

            // Reflect the current audio state on the icon immediately.
            UpdateAudioToggleIcon();

            // When a care action is performed, the hint for that category disappears.
            DropArea.CareActionPerformed += OnCareActionPerformed;

            // The scanner starts closed (disabled).
            if (qrScannerObject != null) qrScannerObject.SetActive(false);

            if (targetCamera == null)
                targetCamera = Camera.main;
        }

        // The three bars start at random values (extremes 0/1 are avoided for readability).
        // Called by StateMainGame on entering the game, so it runs once and is not
        // re-randomized every time the UI re-enables after returning from an overlay.
        public void RandomizeStats()
        {
            if (foodGauge != null) foodGauge.value = Random.Range(0.2f, 0.8f);
            if (cleanGauge != null) cleanGauge.value = Random.Range(0.2f, 0.8f);
            if (petGauge != null) petGauge.value = Random.Range(0.2f, 0.8f);
        }

        // An item contributed care: raise the matching action's gauge.
        void OnCareApplied(CareAction action, float amount)
        {
            switch (action)
            {
                case CareAction.Eat:   AddToGauge(foodGauge, amount);  break;
                case CareAction.Clean: AddToGauge(cleanGauge, amount); break;
                case CareAction.Pet:   AddToGauge(petGauge, amount);   break;
            }
        }

        static void AddToGauge(StatGauge gauge, float amount)
        {
            if (gauge != null) gauge.value += amount; // StatGauge's setter clamps to 0..1
        }

        void OnDisable()
        {
            if (foodButton != null) foodButton.clicked -= OnFoodClicked;
            if (cleanButton != null) cleanButton.clicked -= OnCleanClicked;
            if (petButton != null) petButton.clicked -= OnPetClicked;
            if (cameraButton != null) cameraButton.clicked -= OnCameraClicked;
            if (targetButton != null) targetButton.clicked -= OnTargetClicked;
            if (unlockablesButton != null) unlockablesButton.clicked -= OnUnlockablesClicked;
            if (audioToggleButton != null) audioToggleButton.clicked -= OnAudioToggleClicked;

            if (rootElement != null)
            {
                rootElement.UnregisterCallback<PointerUpEvent>(OnPointerUp);
                rootElement.UnregisterCallback<PointerCancelEvent>(OnPointerCancel);
                rootElement.UnregisterCallback<PointerDownEvent>(OnRootPointerDown, TrickleDown.TrickleDown);
            }

            DropArea.CareActionPerformed -= OnCareActionPerformed;
        }

        // Global audio toggle: mute/unmute ALL audio (music, musetto, camera).
        // AudioListener.pause halts the entire audio scene in one shot.
        void OnAudioToggleClicked()
        {
            AudioListener.pause = !AudioListener.pause;
            UpdateAudioToggleIcon();
            UISoundManager.Instance?.PlayNeutral();
        }

        void UpdateAudioToggleIcon()
        {
            if (audioToggleButton != null)
                audioToggleButton.text = AudioListener.pause ? "🔇" : "🔊";
        }

        // A care action was performed at least once: that category's hint is no longer
        // needed. If it is the one currently shown, hide it now.
        void OnCareActionPerformed(CareAction action)
        {
            careDone.Add(action);
            if (trayOpen && openAction == action)
                HideHint();
        }

#if UNITY_EDITOR
        // TEST (editor only): press Q to simulate scanning the "Funghi Tropicali" info QR.
        // Goes through the same HandleUrlScanned as a real scan (lookup -> unlock -> bubble).
        // TODO: remove when testing is done.
        const string FunghiTestQr = "https://www.muse.it/home/scopri-il-museo/percorso-espositivo/piano-meno1/foresta-tropicale-montana/funghi-tropicali/";

        void Update()
        {
            if (Keyboard.current != null && Keyboard.current.qKey.wasPressedThisFrame)
            {
                Debug.Log("[MainUI] TEST: scan Funghi via tasto Q");
                HandleUrlScanned(FunghiTestQr);
            }
        }
#endif

        // Camera button: opens the QR scanner as an OVERLAY on the state stack.
        // StateCamera hides the main UI, pauses the main state and activates the scanner;
        // on "Close" or a successful scan it calls PopOverlay and returns to the main state.
        void OnCameraClicked()
        {
            UISoundManager.Instance?.PlayNeutral();
            if (qrScannerObject == null) return;
            if (GameStateMachine.Instance == null)
            {
                Debug.LogWarning("MainUIController: no GameStateMachine in scene to open the camera.");
                return;
            }

            // Anti-spam guard: if a StateCamera is already open, don't push another.
            if (GameStateMachine.Instance.HasOverlay<StateCamera>()) return;

            // Pass this UI too so StateCamera hides it while the camera is open.
            var cameraState = new StateCamera(qrScannerObject, gameObject);
            cameraState.OnUrlScanned += HandleUrlScanned;
            GameStateMachine.Instance.PushState(cameraState);
        }

        // The value read from the QR arrives here: we look for an Info asset with the same
        // QrValue and, if it is not yet unlocked, unlock it.
        void HandleUrlScanned(string url)
        {
            Debug.Log($"[MainUI] QR letto: {url}");

            if (unlockables == null)
            {
                Debug.LogWarning("MainUIController: Unlockables non assegnato in Inspector: impossibile sbloccare le info.");
                return;
            }

            Debug.Log($"[MainUI] Cerco info per QR '{url}' (norm='{Unlockables.NormalizeUrl(url)}') — {unlockables.Infos?.Count ?? 0} info nel catalogo.");
            if (unlockables.Infos != null)
                foreach (var i in unlockables.Infos)
                    Debug.Log($"[MainUI]   candidata: '{i?.DisplayName}' norm='{Unlockables.NormalizeUrl(i?.QrValue)}'");

            InfoSO info = unlockables.FindInfoByQrValue(url);
            if (info == null)
            {
                Debug.Log($"[MainUI] Nessuna info corrisponde al QR (anche dopo normalizzazione). Confronta le forme 'norm=' qui sopra.");
                // Feedback al giocatore: questo QR non è del museo.
                if (speechBubble != null) speechBubble.ShowQrNotRecognized();
                return;
            }
            Debug.Log($"[MainUI] Info trovata: '{info.DisplayName}'.");
            
            // Mark the matching Info as scanned in every challenge that contains it.
            bool matched = ChallengeManager.Instance != null && ChallengeManager.Instance.RegisterScan(url);

            // Only when the QR advanced a challenge -> musetto is happy.
            if (matched)
            {
                TriggerHappy("QR scanned");
            }

            // Unlock() returns true only the first time (locked -> unlocked).
            if (info.Unlock())
            {
                Debug.Log($"[MainUI] Info sbloccata: {info.DisplayName}");
                // New info unlocked -> musetto is happy.
                TriggerHappy("info unlocked");
                ShowInfoBubble(info, unlocked: true);
            }
            else
            {
                // QR for an already-unlocked info: no unlock, but we still show the bubble.
                ShowInfoBubble(info, unlocked: false);
            }
        }

        // Shows the speech bubble above musetto. Text and CTA live on SpeechBubbleController;
        // here we only choose WHICH event it is (unlock vs already known) and what the CTA does (open the UI).
        void ShowInfoBubble(InfoSO info, bool unlocked)
        {
            if (speechBubble == null)
            {
                Debug.LogWarning("MainUIController: SpeechBubble non assegnato in Inspector: nessun fumetto mostrato.");
                return;
            }

            if (unlocked) speechBubble.ShowInfoUnlocked(info.Image, () => OpenInfoUi(info));
            else          speechBubble.ShowInfoAlreadyKnown(info.Image, () => OpenInfoUi(info));
        }

        // Speech bubble CTA: for now log + event. The info UI will be implemented later.
        void OpenInfoUi(InfoSO info)
        {
            Debug.Log($"[MainUI] CTA fumetto: apri UI per '{info?.DisplayName}' (UI non ancora implementata).");
            InfoUiRequested?.Invoke(info);
        }

        // Callable from any script when a new object is obtained/unlocked: starts the
        // musetto's "happy" animation.
        public void CelebrateNewItem()
        {
            TriggerHappy("new object");
        }

        // Centralizes the Happy trigger + diagnostic log.
        void TriggerHappy(string reason)
        {
            if (musettoAnimator == null)
            {
                Debug.LogWarning($"[Anim] musettoAnimator NOT assigned on MainUIController ({reason}): drag musetto into the field in the Inspector.");
                return;
            }
            var st = musettoAnimator.GetCurrentAnimatorStateInfo(0);
            Debug.Log($"[Anim] SetTrigger(Happy) [{reason}]. Current state hash={st.fullPathHash} " +
                      $"normalizedTime={st.normalizedTime:F2}. If it does not transition: remove 'Has Exit Time' from the Idle->Felice transition.");
            musettoAnimator.SetTrigger("Happy");
        }

        // Target button: opens the Challenges menu as an OVERLAY on the state stack.
        // StateChallengesMenu instantiates the challenges UI and pauses the main state;
        // on "Close" or back it calls PopOverlay and returns to the main state.
        void OnTargetClicked()
        {
            UISoundManager.Instance?.PlayNeutral();
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
            UISoundManager.Instance?.PlayNeutral();
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
        // Gameplay API: sets each gauge's level (0..1).
        public void SetFoodLevel(float value) { if (foodGauge != null) foodGauge.value = value; }
        public void SetCleanLevel(float value) { if (cleanGauge != null) cleanGauge.value = value; }
        public void SetPetLevel(float value) { if (petGauge != null) petGauge.value = value; }

        // Each category maps to a CareAction (food->Eat, clean->Clean, pet->Pet).
        // The buttons open/close the tray through StateInteractionMenu.
        void OnFoodClicked()  { UISoundManager.Instance?.PlayNeutral(); ToggleInteraction(CareAction.Eat);   }
        void OnCleanClicked() { UISoundManager.Instance?.PlayNeutral(); ToggleInteraction(CareAction.Clean); }
        void OnPetClicked()   { UISoundManager.Instance?.PlayNeutral(); ToggleInteraction(CareAction.Pet);   }

        // Opens the tray for the category as a StateInteractionMenu, or closes it if it is
        // already open on the same category (toggle). Switching category closes and reopens.
        void ToggleInteraction(CareAction action)
        {
            if (GameStateMachine.Instance == null)
            {
                Debug.LogWarning("MainUIController: no GameStateMachine in scene to open the tray.");
                return;
            }

            bool wasOpen = trayOpen;
            CareAction previous = openAction;

            // Close the currently open tray, if any (pop -> StateInteractionMenu.OnExit -> HideTray).
            if (wasOpen)
                GameStateMachine.Instance.PopOverlay();

            // Same category = toggle off; different category (or none) = open the chosen one.
            if (wasOpen && previous == action)
                return;

            GameStateMachine.Instance.PushState(new StateInteractionMenu(this, action));
        }

        // Opens the tray for the given category. Public: called by StateInteractionMenu on
        // enter. The tray stays visible until the state is closed.
        public void ShowTray(CareAction action)
        {
            trayOpen = true;
            openAction = action;
            itemTray.style.display = DisplayStyle.Flex;

            // The hint stays visible until the player has COMPLETED the category's action at
            // least once; afterwards it does not reappear.
            if (!careDone.Contains(action))
                ShowHint(HintFor(action));
            else
                HideHint();

            PopulateTray(ItemsFor(action), action);
        }

        // Hides and clears the tray. Public: called by StateInteractionMenu on exit.
        public void HideTray()
        {
            trayOpen = false;
            itemTray.style.display = DisplayStyle.None;
            HideHint();
            trayContent.Clear();
        }

        // The unlocked items for the category (food->Foods, clean->Soaps, pet->Toys).
        IReadOnlyList<ItemSO> ItemsFor(CareAction action)
        {
            if (unlockables == null) return null;
            return action switch
            {
                CareAction.Eat => unlockables.Foods,
                CareAction.Clean => unlockables.Soaps,
                CareAction.Pet => unlockables.Toys,
                _ => null,
            };
        }

        static string HintFor(CareAction action) => action switch
        {
            CareAction.Eat => FoodHint,
            CareAction.Clean => CleanHint,
            CareAction.Pet => PetHint,
            _ => string.Empty,
        };

        // Shows the hint (arrow + text) above the tray with the category's message.
        void ShowHint(string text)
        {
            if (trayHintLabel != null) trayHintLabel.text = text;
            if (trayHint != null) trayHint.style.display = DisplayStyle.Flex;
        }

        void HideHint()
        {
            if (trayHint != null) trayHint.style.display = DisplayStyle.None;
        }

        // Fills the tray by cloning the template, one icon per UNLOCKED item in the chosen
        // category. The selected item carries its own icon: at selection (OnItemPointerDown)
        // it decides which sprite to display on the dragged object. No text, icons only.
        void PopulateTray(IReadOnlyList<ItemSO> items, CareAction action)
        {
            trayContent.Clear();

            if (trayItemTemplate == null)
            {
                Debug.LogWarning("MainUIController: trayItemTemplate not assigned in Inspector.");
                return;
            }

            if (items == null) return;

            foreach (var item in items)
            {
                if (item == null || !IsUnlocked(item)) continue;

                var instance = trayItemTemplate.Instantiate();
                var itemRoot = instance.Q<VisualElement>("item") ?? instance;

                // The icon shows the item's sprite; it must not intercept pointer events
                // (the item's root handles them).
                var icon = instance.Q<Button>("item-icon");
                if (icon != null)
                {
                    icon.pickingMode = PickingMode.Ignore;
                    if (item.Image != null)
                        icon.style.backgroundImage = new StyleBackground(item.Image);
                }

                // Only PointerDown is on the item (it decides which icon/action to spawn).
                // Up and Cancel are handled by the root (see OnEnable), so closing the tray
                // at drag-start does not break release.
                itemRoot.RegisterCallback<PointerDownEvent>(evt => OnItemPointerDown(evt, item, action));

                trayContent.Add(instance);
            }
        }

        // An item counts as unlocked if it ships unlocked or was unlocked this session
        // (ChallengeManager registry). Same logic as the unlockables UI.
        static bool IsUnlocked(UnlockableSO unlockable)
        {
            return ChallengeManager.Instance != null
                ? ChallengeManager.Instance.IsUnlocked(unlockable)
                : unlockable.Unlocked;
        }

        void OnItemPointerDown(PointerDownEvent evt, ItemSO item, CareAction action)
        {
            if (spawnedObject != null) return; // one drag at a time

            // The base prefab is decided by the category (not serialized: loaded from Resources).
            var prefab = PrefabFor(action);
            if (prefab == null)
            {
                Debug.LogWarning($"MainUIController: base prefab for {action} not found in Resources.");
                return;
            }

            // Capture on the ROOT: tray tiles are destroyed when the tray closes at
            // drag-start, but the root stays and keeps receiving Up/Cancel.
            capturedElement = rootElement;
            activePointerId = evt.pointerId;
            rootElement.CapturePointer(evt.pointerId);

            spawnedObject = Instantiate(prefab);

            // The dragged object shows the SELECTED item's icon: the same base prefab serves
            // every item in the category; the appearance comes from the chosen item.
            if (item != null && item.Image != null)
            {
                var renderer = spawnedObject.GetComponent<SpriteRenderer>();
                if (renderer != null)
                {
                    renderer.sprite = item.Image;
                    renderer.color = Color.white; // the base prefabs carry a placeholder tint
                    FitToWorldSize(spawnedObject.transform, renderer, draggedItemWorldSize);
                }
            }

            spawnedItem = spawnedObject.GetComponent<Item>();
            if (spawnedItem != null)
            {
                // The action is decided by the category (food/clean/pet), not the prefab:
                // that way the same base prefabs serve every category.
                spawnedItem.SetAction(action);
                // While in use on musetto, DropArea fires CareApplied: we raise the right gauge.
                spawnedItem.CareApplied += OnCareApplied;
                spawnedItem.BeginDrag(targetCamera != null ? targetCamera : Camera.main, dragDepth);
            }
            else
                Debug.LogWarning("MainUIController: the spawned prefab has no Item component.");

            evt.StopPropagation();

            // Object picked up: the tray disappears (close StateInteractionMenu). Done last,
            // so the full drag setup completes before the tray is cleared/hidden.
            InteractionDismissRequested?.Invoke();
        }

        // Base prefab for the category, loaded from Resources and cached.
        // Food (Eat) -> Droppable (left on the area); Clean/Pet -> Draggable (rubbed on it).
        GameObject PrefabFor(CareAction action)
        {
            if (action == CareAction.Eat)
                return droppablePrefab != null
                    ? droppablePrefab
                    : (droppablePrefab = Resources.Load<GameObject>(DroppableResourcePath));

            return draggablePrefab != null
                ? draggablePrefab
                : (draggablePrefab = Resources.Load<GameObject>(DraggableResourcePath));
        }

        // Scales the object so the sprite's largest side occupies 'targetSize' world units,
        // regardless of the sprite's pixels/PPU.
        static void FitToWorldSize(Transform target, SpriteRenderer renderer, float targetSize)
        {
            if (renderer.sprite == null || targetSize <= 0f) return;

            Vector2 spriteSize = renderer.sprite.bounds.size; // world units at scale 1
            float largest = Mathf.Max(spriteSize.x, spriteSize.y);
            if (largest <= 0f) return;

            float scale = targetSize / largest;
            target.localScale = new Vector3(scale, scale, 1f);
        }

        void OnPointerUp(PointerUpEvent evt)
        {
            if (spawnedObject == null || evt.pointerId != activePointerId) return;

            // Item handles its own release (by default it disappears); DropArea already reacted via trigger.
            if (spawnedItem != null) spawnedItem.Drop();
            else Destroy(spawnedObject);

            EndDrag();
        }

        // Pointer cancelled by the OS (typical on touch): drop the item without releasing it
        // and restore state so the tray stays usable.
        void OnPointerCancel(PointerCancelEvent evt)
        {
            if (spawnedObject == null || evt.pointerId != activePointerId) return;

            Destroy(spawnedObject);
            EndDrag();
        }

        // Common drag-state cleanup (release or cancel). The tray stays closed: it
        // disappeared on pickup and only reopens when the category button is pressed again.
        void EndDrag()
        {
            if (spawnedItem != null) spawnedItem.CareApplied -= OnCareApplied;
            spawnedObject = null;
            spawnedItem = null;

            if (capturedElement != null)
            {
                capturedElement.ReleasePointer(activePointerId);
                capturedElement = null;
            }
        }

        // Tap outside the tray (and not on a category button) = close the tray. The
        // food/clean/pet buttons are excluded: their toggle handles close/category switch.
        void OnRootPointerDown(PointerDownEvent evt)
        {
            if (!trayOpen) return;

            var target = evt.target as VisualElement;
            if (target == null) return;

            if (IsWithin(target, itemTray)) return; // inside the tray: it handles drag/hint
            if (IsWithin(target, foodButton) || IsWithin(target, cleanButton) || IsWithin(target, petButton))
                return; // category button: the toggle handles it

            InteractionDismissRequested?.Invoke();
        }

        // True if 'node' is 'ancestor' or one of its descendants.
        static bool IsWithin(VisualElement node, VisualElement ancestor)
        {
            if (ancestor == null) return false;
            for (var e = node; e != null; e = e.parent)
                if (e == ancestor) return true;
            return false;
        }
    }
}
