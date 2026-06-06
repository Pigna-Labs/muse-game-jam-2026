using MuseGameJam.StateSystem;
using MuseGameJam.UI;
using UnityEngine;

namespace MuseGameJam.States
{
    /// <summary>
    /// Info detail overlay: shows one Info in full (icon, name, markdown description).
    ///
    /// Pushed either:
    ///  - from StateInfoMenu when an unlocked entry is tapped (renders above the list sheet), or
    ///  - from MainUIController when the speech-bubble CTA of a freshly scanned Info is pressed
    ///    (renders above the main UI).
    ///
    /// Its panel needs a higher PanelSettings sort order than whatever is underneath
    /// (the info list, and the main UI), so it passes hideMainUi:false and just draws on top.
    ///
    /// It closes (PopOverlay) when:
    ///  - the user presses "Close"  -> InfoDetailController.CloseRequested
    ///  - mobile back               -> OverlayState.HandleBack (animated through RequestClose)
    /// </summary>
    public class StateInfoDetail : OverlayState
    {
        private readonly GameObject infoDetailUiPrefab;
        private readonly Transform parent;
        private readonly InfoSO info;
        private GameObject infoDetailUiInstance;
        private InfoDetailController infoDetailUi;

        public StateInfoDetail(
            GameObject infoDetailUiPrefab,
            Transform parent,
            InfoSO info,
            GameObject mainUiObject = null)
            : base(mainUiObject, hideMainUi: false)
        {
            this.infoDetailUiPrefab = infoDetailUiPrefab;
            this.parent = parent;
            this.info = info;
        }

        // Creates the info detail UI above whatever is on the stack and feeds it the Info.
        protected override void OnEnter()
        {
            infoDetailUiInstance = Object.Instantiate(infoDetailUiPrefab, parent);
            infoDetailUi = infoDetailUiInstance.GetComponent<InfoDetailController>();

            if (infoDetailUi == null)
            {
                throw new MissingComponentException("Info detail overlay prefab needs an InfoDetailController component.");
            }

            infoDetailUi.SetInfo(info);
            infoDetailUi.CloseRequested += HandleCloseRequested;
        }

        // Mobile back: slide the sheet down (same as the Close button) instead of closing instantly.
        public override bool HandleBack()
        {
            if (infoDetailUi != null)
            {
                infoDetailUi.RequestClose();
                return true;
            }

            return base.HandleBack();
        }

        // Removes the UI instance when the overlay closes.
        protected override void OnExit()
        {
            if (infoDetailUi != null)
            {
                infoDetailUi.CloseRequested -= HandleCloseRequested;
            }

            Object.Destroy(infoDetailUiInstance);
        }

        // The user pressed "Close": close the overlay (back to the state underneath).
        private void HandleCloseRequested()
        {
            GameStateMachine.Instance.PopOverlay();
        }
    }
}
