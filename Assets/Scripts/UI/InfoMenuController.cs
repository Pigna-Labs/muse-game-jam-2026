using System;
using MuseGameJam.Gameplay;
using UnityEngine;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    /// <summary>
    /// Controller for the Info collection menu UI (InfoMenu.uxml).
    ///
    /// Mirrors UnlockablesMenuController: a full-screen bottom sheet that slides up on open
    /// and down on close. It fills a single grid from the Unlockables asset's Info list.
    ///
    /// Each Info is drawn as a tile (icon + caption). A locked Info shows its sprite tinted
    /// black and "???" as the caption. An unlocked Info shows the art and name, and is
    /// clickable: tapping it raises InfoSelected so the owning state can open the detail page.
    ///
    /// Stays decoupled from the state system: it exposes CloseRequested (after the slide-down)
    /// and InfoSelected; StateInfoMenu owns the lifecycle.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class InfoMenuController : MonoBehaviour
    {
        private const string OpenClass = "info-menu-panel--open";

        // USS classes for the generated tiles (see InfoMenu.uss).
        private const string TileClass = "info-tile";
        private const string TileClickableClass = "info-tile--clickable";
        private const string TileImageClass = "info-tile__image";
        private const string TileImageLockedClass = "info-tile__image--locked";
        private const string TileLabelClass = "info-tile__label";
        private const string TileLabelLockedClass = "info-tile__label--locked";

        // Caption shown instead of the name while an entry is still locked.
        private const string LockedCaption = "???";

        // Must match the transition-duration of .info-menu-panel in InfoMenu.uss.
        private const long SlideMilliseconds = 320;

        // Source of the Info catalog to display (the same asset the QR scan unlocks against).
        [SerializeField] private Unlockables unlockables;

        private UIDocument document;
        private VisualElement panel;
        private VisualElement grid;
        private Button closeButton;
        private bool closing;

        // Raised when the user requests to close the info menu (after the slide-down).
        public event Action CloseRequested;

        // Raised when the player taps an unlocked Info tile. The owning state opens its detail.
        public event Action<InfoSO> InfoSelected;

        // Binds to the UXML elements and starts the slide-in when the UIDocument becomes active.
        private void OnEnable()
        {
            closing = false;
            BindElements();
            PopulateInfos();
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

        // Finds the named elements in InfoMenu.uxml and wires the close button.
        private void BindElements()
        {
            document = GetComponent<UIDocument>();

            VisualElement root = RequireReference(document.rootVisualElement, "InfoMenu UIDocument has no root visual element. Check that its Visual Tree Asset is assigned.");
            panel = RequireElement<VisualElement>(root, "info-menu-panel");
            grid = RequireElement<VisualElement>(root, "info-grid");
            closeButton = RequireElement<Button>(root, "info-menu-close");

            closeButton.clicked -= RequestClose;
            closeButton.clicked += RequestClose;
        }

        // Fills the grid with one tile per Info from the Unlockables asset.
        private void PopulateInfos()
        {
            grid.Clear();

            if (unlockables == null)
            {
                Debug.LogWarning("InfoMenuController: Unlockables asset not assigned in Inspector.");
                return;
            }

            foreach (InfoSO info in unlockables.Infos)
            {
                if (info != null)
                {
                    grid.Add(CreateTile(info));
                }
            }
        }

        // Builds a single tile: the icon on top (black while locked) and a caption below.
        // Unlocked tiles get a clickable affordance and open the Info's detail page on tap.
        private VisualElement CreateTile(InfoSO info)
        {
            bool unlocked = info.Unlocked;

            VisualElement tile = new VisualElement();
            tile.AddToClassList(TileClass);

            VisualElement icon = new VisualElement();
            icon.AddToClassList(TileImageClass);
            if (info.Image != null)
            {
                icon.style.backgroundImage = new StyleBackground(info.Image);
            }
            if (!unlocked)
            {
                icon.AddToClassList(TileImageLockedClass);
            }
            tile.Add(icon);

            Label caption = new Label(unlocked ? info.DisplayName : LockedCaption);
            caption.AddToClassList(TileLabelClass);
            if (!unlocked)
            {
                caption.AddToClassList(TileLabelLockedClass);
            }
            tile.Add(caption);

            if (unlocked)
            {
                tile.AddToClassList(TileClickableClass);
                tile.AddManipulator(new Clickable(() => InfoSelected?.Invoke(info)));
            }

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
                throw new MissingReferenceException($"InfoMenu.uxml is missing a {typeof(T).Name} named {elementName}.");
            }

            return element;
        }
    }
}
