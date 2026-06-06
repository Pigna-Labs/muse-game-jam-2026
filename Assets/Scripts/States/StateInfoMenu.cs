using MuseGameJam.StateSystem;
using MuseGameJam.UI;
using UnityEngine;

namespace MuseGameJam.States
{
    /// <summary>
    /// Info collection menu overlay, opened by the "info" button of the MainUI.
    ///
    /// Same pattern as StateUnlockablesMenu / StateChallengesMenu: pushed onto the stack
    /// by MainUIController, it instantiates its UI above the main scene (the panel has a
    /// higher PanelSettings sort order, so the main UI stays visible behind it -> hideMainUi:false).
    ///
    /// Lists every Info (locked + unlocked). Tapping an unlocked entry raises InfoSelected,
    /// which this state turns into a StateInfoDetail overlay pushed ON TOP of this sheet
    /// (the detail panel sorts above the list, so the list stays open underneath).
    ///
    /// It closes (PopOverlay) when:
    ///  - the user presses "Close"  -> InfoMenuController.CloseRequested
    ///  - mobile back               -> OverlayState.HandleBack (animated through RequestClose)
    /// </summary>
    public class StateInfoMenu : OverlayState
    {
        private readonly GameObject infoMenuUiPrefab;
        private readonly GameObject infoDetailUiPrefab;
        private readonly Transform parent;
        private readonly GameObject mainUiObject;
        private GameObject infoMenuUiInstance;
        private InfoMenuController infoMenuUi;

        public StateInfoMenu(
            GameObject infoMenuUiPrefab,
            GameObject infoDetailUiPrefab,
            Transform parent,
            GameObject mainUiObject = null)
            : base(mainUiObject, hideMainUi: false)
        {
            this.infoMenuUiPrefab = infoMenuUiPrefab;
            this.infoDetailUiPrefab = infoDetailUiPrefab;
            this.parent = parent;
            this.mainUiObject = mainUiObject;
        }

        // Creates the info menu UI above the main scene.
        protected override void OnEnter()
        {
            infoMenuUiInstance = Object.Instantiate(infoMenuUiPrefab, parent);
            infoMenuUi = infoMenuUiInstance.GetComponent<InfoMenuController>();

            if (infoMenuUi == null)
            {
                throw new MissingComponentException("Info menu overlay prefab needs an InfoMenuController component.");
            }

            infoMenuUi.CloseRequested += HandleCloseRequested;
            infoMenuUi.InfoSelected += HandleInfoSelected;
        }

        // Mobile back: slide the sheet down (same as the Close button) instead of closing instantly.
        public override bool HandleBack()
        {
            if (infoMenuUi != null)
            {
                infoMenuUi.RequestClose();
                return true;
            }

            return base.HandleBack();
        }

        // Removes the UI instance when the overlay closes.
        protected override void OnExit()
        {
            if (infoMenuUi != null)
            {
                infoMenuUi.CloseRequested -= HandleCloseRequested;
                infoMenuUi.InfoSelected -= HandleInfoSelected;
            }

            Object.Destroy(infoMenuUiInstance);
        }

        // An unlocked entry was tapped: open its detail page as an overlay above this sheet.
        private void HandleInfoSelected(InfoSO info)
        {
            if (info == null)
            {
                return;
            }

            GameStateMachine.Instance.PushState(
                new StateInfoDetail(infoDetailUiPrefab, parent, info, mainUiObject));
        }

        // The user pressed "Close": close the overlay (back to the main state).
        private void HandleCloseRequested()
        {
            GameStateMachine.Instance.PopOverlay();
        }
    }
}
