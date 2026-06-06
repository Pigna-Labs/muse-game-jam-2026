using System;
using UnityEngine;
using UnityEngine.UIElements;

namespace MuseGameJam.UI
{
    /// <summary>
    /// Controller for the Info detail menu UI (InfoDetail.uxml).
    ///
    /// Mirrors the other menu controllers: a full-screen bottom sheet that slides up on open
    /// and down on close. Shows one Info in detail: its icon, display name, and a description
    /// authored in Markdown (converted to UI Toolkit rich text via MarkdownRichText).
    ///
    /// The owning StateInfoDetail feeds the Info through SetInfo right after instantiation.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class InfoDetailController : MonoBehaviour
    {
        private const string OpenClass = "info-detail-panel--open";

        // Must match the transition-duration of .info-detail-panel in InfoDetail.uss.
        private const long SlideMilliseconds = 320;

        private UIDocument document;
        private VisualElement panel;
        private VisualElement image;
        private Label title;
        private Label description;
        private Button closeButton;
        private bool closing;

        // The Info to show. Stored so SetInfo works whether it is called before or after OnEnable.
        private InfoSO info;

        // Raised when the user requests to close the detail menu (after the slide-down).
        public event Action CloseRequested;

        // Binds to the UXML elements and starts the slide-in when the UIDocument becomes active.
        private void OnEnable()
        {
            closing = false;
            BindElements();
            ApplyInfo();
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

        // Sets the Info to display. Safe to call before the UIDocument has bound its elements.
        public void SetInfo(InfoSO nextInfo)
        {
            info = nextInfo;
            ApplyInfo();
        }

        // Pushes the current Info onto the bound elements (no-op until they are bound).
        private void ApplyInfo()
        {
            if (panel == null)
            {
                return;
            }

            if (title != null)
            {
                title.text = info != null ? info.DisplayName : string.Empty;
            }

            if (image != null)
            {
                image.style.backgroundImage = info != null && info.Image != null
                    ? new StyleBackground(info.Image)
                    : StyleKeyword.None;
            }

            if (description != null)
            {
                description.enableRichText = true;
                description.text = info != null ? MarkdownRichText.Convert(info.Description) : string.Empty;
            }
        }

        // Finds the named elements in InfoDetail.uxml and wires the close button.
        private void BindElements()
        {
            document = GetComponent<UIDocument>();

            VisualElement root = RequireReference(document.rootVisualElement, "InfoDetail UIDocument has no root visual element. Check that its Visual Tree Asset is assigned.");
            panel = RequireElement<VisualElement>(root, "info-detail-panel");
            image = RequireElement<VisualElement>(root, "info-detail-image");
            title = RequireElement<Label>(root, "info-detail-title");
            description = RequireElement<Label>(root, "info-detail-description");
            closeButton = RequireElement<Button>(root, "info-detail-close");

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
                throw new MissingReferenceException($"InfoDetail.uxml is missing a {typeof(T).Name} named {elementName}.");
            }

            return element;
        }
    }
}
