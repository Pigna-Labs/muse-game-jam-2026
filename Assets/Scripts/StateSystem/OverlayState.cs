using UnityEngine;

namespace MuseGameJam.StateSystem
{
    /// <summary>
    /// Base class for overlay states pushed above the main game UI.
    ///
    /// Centralizes the behavior shared by every overlay:
    ///  - hides the main UI while the overlay is active, so its separate UIDocument
    ///    panel does not render over the overlay nor keep receiving taps underneath it;
    ///  - closes the overlay on mobile back (HandleBack -> PopOverlay).
    ///
    /// Subclasses implement OnEnter/OnExit for their own setup and teardown; the main
    /// UI is already hidden by the time OnEnter runs and is restored after OnExit.
    /// </summary>
    public abstract class OverlayState : GameState
    {
        private readonly GameObject mainUiObject;

        protected OverlayState(GameObject mainUiObject)
        {
            this.mainUiObject = mainUiObject;
        }

        // Hides the main UI, then runs the subclass setup.
        public sealed override void Enter()
        {
            if (mainUiObject != null)
            {
                mainUiObject.SetActive(false);
            }

            OnEnter();
        }

        // Runs the subclass teardown, then restores the main UI.
        public sealed override void Exit()
        {
            OnExit();

            if (mainUiObject != null)
            {
                mainUiObject.SetActive(true);
            }
        }

        // Mobile back = close this overlay.
        public override bool HandleBack()
        {
            GameStateMachine.Instance.PopOverlay();
            return true;
        }

        // Called after the main UI is hidden: create/activate the overlay here.
        protected abstract void OnEnter();

        // Called before the main UI is restored: tear down the overlay here.
        protected abstract void OnExit();
    }
}
