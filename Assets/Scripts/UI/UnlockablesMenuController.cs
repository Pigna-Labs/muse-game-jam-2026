using System;
using System.Collections.Generic;
using MuseGameJam.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    /// <summary>
    /// Controller for the Unlockables menu UI (Unlockables.uxml).
    ///
    /// The menu is a full-screen bottom sheet: it slides up from the bottom on open
    /// and slides back down on close. Sliding is a USS 'translate' transition; this
    /// controller only toggles the open class and times the close so the overlay is
    /// not destroyed before the slide-down finishes.
    ///
    /// On open it also fills two subsections - Food and Companions - by iterating the
    /// Unlockables asset. Each entry is drawn as a tile; locked entries show their
    /// sprite tinted fully black (a silhouette), so the player sees the shape but not
    /// the art until it is unlocked.
    ///
    /// Stays decoupled from the state system: it only exposes CloseRequested once the
    /// close animation is done. The StateUnlockablesMenu owns the lifecycle (PopOverlay).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class UnlockablesMenuController : MonoBehaviour
    {
        private const string OpenClass = "unlockables-panel--open";

        // USS classes for the generated tiles (see Unlockables.uss).
        private const string TileClass = "unlockable-tile";
        private const string TileImageClass = "unlockable-tile__image";
        private const string TileImageLockedClass = "unlockable-tile__image--locked";
        private const string TileLabelClass = "unlockable-tile__label";
        private const string TileLabelLockedClass = "unlockable-tile__label--locked";

        // Caption shown instead of the name while an entry is still locked.
        private const string LockedCaption = "???";

        // Must match the transition-duration of .unlockables-panel in Unlockables.uss.
        private const long SlideMilliseconds = 320;

        // Source of the food and companion entries to display.
        [SerializeField] private Unlockables unlockables;

        private UIDocument document;
        private VisualElement panel;
        private VisualElement companionsGrid;
        private VisualElement foodGrid;
        private VisualElement soapsGrid;
        private VisualElement toysGrid;
        private Button closeButton;
        private bool closing;

        // Raised when the user requests to close the unlockables menu (after the slide-down).
        public event Action CloseRequested;

        // Binds to the UXML elements and starts the slide-in when the UIDocument becomes active.
        private void OnEnable()
        {
            closing = false;
            BindElements();
            PopulateSections();
            PlayOpenAnimation();
        }

        // Detaches callbacks so re-enabling the object does not double-register them.
        private void OnDisable()
        {
            if (closeButton != null)
            {
                closeButton.clicked -= RequestClose;
                closeButton = null;
            }
        }

        // Finds the named elements in Unlockables.uxml and wires the close button.
        private void BindElements()
        {
            document = GetComponent<UIDocument>();

            VisualElement root = RequireReference(document.rootVisualElement, "Unlockables UIDocument has no root visual element. Check that its Visual Tree Asset is assigned.");
            panel = RequireElement<VisualElement>(root, "unlockables-panel");
            companionsGrid = RequireElement<VisualElement>(root, "companions-grid");
            foodGrid = RequireElement<VisualElement>(root, "food-grid");
            soapsGrid = RequireElement<VisualElement>(root, "soaps-grid");
            toysGrid = RequireElement<VisualElement>(root, "toys-grid");
            closeButton = RequireElement<Button>(root, "unlockables-close");

            closeButton.clicked -= RequestClose;
            closeButton.clicked += RequestClose;
        }

        // Fills the Companions, Foods, Soaps and Toys grids from the Unlockables asset.
        // Each entry honours its unlock state (ships-unlocked flag or unlocked this session
        // via the ChallengeManager registry); still-locked entries are drawn blackened.
        private void PopulateSections()
        {
            companionsGrid.Clear();
            foodGrid.Clear();
            soapsGrid.Clear();
            toysGrid.Clear();

            if (unlockables == null)
            {
                Debug.LogWarning("UnlockablesMenuController: Unlockables asset not assigned in Inspector.");
                return;
            }

            foreach (CompanionSO companion in unlockables.Companions)
            {
                if (companion == null) continue;
                companionsGrid.Add(CreateTile(companion.Image, companion.DisplayName, IsUnlocked(companion)));
            }

            PopulateItemGrid(foodGrid, unlockables.Foods);
            PopulateItemGrid(soapsGrid, unlockables.Soaps);
            PopulateItemGrid(toysGrid, unlockables.Toys);
        }

        // Adds one tile per item, blackened until the item has been unlocked.
        private void PopulateItemGrid(VisualElement grid, IReadOnlyList<ItemSO> items)
        {
            foreach (ItemSO item in items)
            {
                if (item == null) continue;
                grid.Add(CreateTile(item.Image, item.DisplayName, IsUnlocked(item)));
            }
        }

        // An entry is unlocked if it ships unlocked or was unlocked this session (registry).
        private static bool IsUnlocked(UnlockableSO unlockable)
        {
            return ChallengeManager.Instance != null
                ? ChallengeManager.Instance.IsUnlocked(unlockable)
                : unlockable.Unlocked;
        }

        // Builds a single tile: the sprite on top (tinted black while locked) and a caption
        // below (the name once unlocked, "???" while still locked).
        private VisualElement CreateTile(Sprite image, string displayName, bool unlocked)
        {
            VisualElement tile = new VisualElement();
            tile.AddToClassList(TileClass);

            VisualElement icon = new VisualElement();
            icon.AddToClassList(TileImageClass);
            if (image != null)
            {
                icon.style.backgroundImage = new StyleBackground(image);
            }
            if (!unlocked)
            {
                icon.AddToClassList(TileImageLockedClass);
            }
            tile.Add(icon);

            Label caption = new Label(unlocked ? displayName : LockedCaption);
            caption.AddToClassList(TileLabelClass);
            if (!unlocked)
            {
                caption.AddToClassList(TileLabelLockedClass);
            }
            tile.Add(caption);

            return tile;
        }

        // Adds the open class once the panel has its first (closed) layout, so the
        // translate transition animates from off-screen up into place.
        private void PlayOpenAnimation()
        {
            panel.RemoveFromClassList(OpenClass);
            panel.RegisterCallback<GeometryChangedEvent>(OnPanelFirstLayout);
        }

        private void OnPanelFirstLayout(GeometryChangedEvent evt)
        {
            panel.UnregisterCallback<GeometryChangedEvent>(OnPanelFirstLayout);
            panel.AddToClassList(OpenClass);
        }

        // Slides the sheet back down, then asks the owning state to close once it is off-screen.
        // Public so the state can route mobile back through the same animated close.
        public void RequestClose()
        {
            if (closing)
            {
                return;
            }

            closing = true;
            panel.RemoveFromClassList(OpenClass);
            panel.schedule.Execute(() => CloseRequested?.Invoke()).StartingIn(SlideMilliseconds);
        }

        // Returns a required Unity reference or throws, so missing Inspector references are visible in the console.
        private T RequireReference<T>(T reference, string message) where T : class
        {
            if (reference == null)
            {
                throw new MissingReferenceException(message);
            }

            return reference;
        }

        // Finds a required UXML element by name and throws when the asset is out of sync with this controller.
        private T RequireElement<T>(VisualElement root, string elementName) where T : VisualElement
        {
            T element = root.Q<T>(elementName);

            if (element == null)
            {
                throw new MissingReferenceException($"Unlockables.uxml is missing a {typeof(T).Name} named {elementName}.");
            }

            return element;
        }
    }
}
