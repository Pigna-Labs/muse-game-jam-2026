using MuseGameJam.StateSystem;
using MuseGameJam.UI;
using UnityEngine;

namespace MuseGameJam.States
{
    /// <summary>
    /// Unlockables menu overlay, opened by the "unlockables" button of the MainUI.
    ///
    /// Pushed onto the stack by MainUIController: the GameStateMachine pauses the
    /// main state, the OverlayState base hides the main UI, and this state
    /// instantiates its UI above the main scene.
    ///
    /// It closes (PopOverlay) when:
    ///  - the user presses "Close"  -> UnlockablesMenuController.CloseRequested
    ///  - mobile back               -> OverlayState.HandleBack
    ///
    /// Like the trivia overlay, the UI is a prefab (UIDocument + UnlockablesMenuController):
    /// instantiated on enter and destroyed on exit.
    /// </summary>
    public class StateUnlockablesMenu : OverlayState
    {
        private readonly GameObject unlockablesUiPrefab;
        private readonly Transform parent;
        private GameObject unlockablesUiInstance;
        private UnlockablesMenuController unlockablesUi;

        public StateUnlockablesMenu(GameObject unlockablesUiPrefab, Transform parent, GameObject mainUiObject = null)
            : base(mainUiObject)
        {
            this.unlockablesUiPrefab = unlockablesUiPrefab;
            this.parent = parent;
        }

        // Creates the unlockables menu UI above the main scene.
        protected override void OnEnter()
        {
            unlockablesUiInstance = Object.Instantiate(unlockablesUiPrefab, parent);
            unlockablesUi = unlockablesUiInstance.GetComponent<UnlockablesMenuController>();

            if (unlockablesUi == null)
            {
                throw new MissingComponentException("Unlockables overlay prefab needs a UnlockablesMenuController component.");
            }

            unlockablesUi.CloseRequested += HandleCloseRequested;
        }

        // Mobile back: slide the sheet down (same as the Close button) instead of closing instantly.
        public override bool HandleBack()
        {
            if (unlockablesUi != null)
            {
                unlockablesUi.RequestClose();
                return true;
            }

            return base.HandleBack();
        }

        // Removes the UI instance when the overlay closes.
        protected override void OnExit()
        {
            if (unlockablesUi != null)
            {
                unlockablesUi.CloseRequested -= HandleCloseRequested;
            }

            Object.Destroy(unlockablesUiInstance);
        }

        // The user pressed "Close": close the overlay (back to the main state).
        private void HandleCloseRequested()
        {
            GameStateMachine.Instance.PopOverlay();
        }
    }
}
