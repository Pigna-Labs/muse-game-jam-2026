using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    /// <summary>
    /// Controller for the Challenges menu UI (Challenges.uxml).
    ///
    /// The menu is a full-screen bottom sheet: it slides up from the bottom on open
    /// and slides back down on close. Sliding is a USS 'translate' transition; this
    /// controller only toggles the open class and times the close so the overlay is
    /// not destroyed before the slide-down finishes.
    ///
    /// Stays decoupled from the state system: it only exposes CloseRequested once the
    /// close animation is done. The StateChallengesMenu owns the lifecycle (PopOverlay).
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class ChallengesMenuController : MonoBehaviour
    {
        private const string OpenClass = "challenges-panel--open";

        // Must match the transition-duration of .challenges-panel in Challenges.uss.
        private const long SlideMilliseconds = 320;

        private UIDocument document;
        private VisualElement panel;
        private Button closeButton;
        private bool closing;

        // Raised when the user requests to close the challenges menu (after the slide-down).
        public event Action CloseRequested;

        // Binds to the UXML elements and starts the slide-in when the UIDocument becomes active.
        private void OnEnable()
        {
            closing = false;
            BindElements();
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

        // Finds the named elements in Challenges.uxml and wires the close button.
        private void BindElements()
        {
            document = GetComponent<UIDocument>();

            VisualElement root = RequireReference(document.rootVisualElement, "Challenges UIDocument has no root visual element. Check that its Visual Tree Asset is assigned.");
            panel = RequireElement<VisualElement>(root, "challenges-panel");
            closeButton = RequireElement<Button>(root, "challenges-close");

            closeButton.clicked -= RequestClose;
            closeButton.clicked += RequestClose;
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
                throw new MissingReferenceException($"Challenges.uxml is missing a {typeof(T).Name} named {elementName}.");
            }

            return element;
        }
    }
}
