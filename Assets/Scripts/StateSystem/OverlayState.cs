using UnityEngine;

namespace MuseGameJam.StateSystem
{
    /// <summary>
    /// Base class for overlay states pushed above the main game UI.
    ///
    /// Centralizes the behavior shared by every overlay:
    ///  - optionally hides the main UI while the overlay is active (see hideMainUi),
    ///    so its separate UIDocument panel does not render over the overlay nor keep
    ///    receiving taps underneath it;
    ///  - closes the overlay on mobile back (HandleBack -> PopOverlay).
    ///
    /// Subclasses implement OnEnter/OnExit for their own setup and teardown.
    ///
    /// hideMainUi controls whether the main UI is deactivated for the lifetime of the
    /// overlay. Overlays whose own panel has a higher PanelSettings sort order render
    /// above the main UI anyway, so they can pass hideMainUi: false to keep it visible
    /// behind them (e.g. so it shows through during a slide-in animation). The overlay's
    /// own full-screen root still captures taps, so the main UI stays non-interactive
    /// while covered. Fully opaque/replacement overlays (e.g. the QR camera) keep the
    /// default and hide it.
    /// </summary>
    public abstract class OverlayState : GameState
    {
        private readonly GameObject mainUiObject;
        private readonly bool hideMainUi;

        protected OverlayState(GameObject mainUiObject, bool hideMainUi = true)
        {
            this.mainUiObject = mainUiObject;
            this.hideMainUi = hideMainUi;
        }

        // Hides the main UI (unless opted out), then runs the subclass setup.
        public sealed override void Enter()
        {
            if (hideMainUi && mainUiObject != null)
            {
                mainUiObject.SetActive(false);
            }

            OnEnter();
        }

        // Runs the subclass teardown, then restores the main UI if it was hidden.
        public sealed override void Exit()
        {
            OnExit();

            if (hideMainUi && mainUiObject != null)
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
