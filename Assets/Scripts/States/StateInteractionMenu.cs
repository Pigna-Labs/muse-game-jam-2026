using MuseGameJam.Gameplay;
using MuseGameJam.StateSystem;
using MuseGameJam.UI;

namespace MuseGameJam.States
{
    /// <summary>
    /// Overlay representing the open interaction tray (food/clean/pet).
    ///
    /// Pushed onto the stack by the FOOD/CLEAN/PET buttons through MainUIController.
    /// Unlike the other overlays it does NOT hide the main UI: the tray is part of the
    /// main UI and the buttons must stay interactive (hideMainUi: false, mainUi: null).
    ///
    /// The tray closes (PopOverlay) when:
    ///  - an object is picked up (drag start)  -> MainUIController.InteractionDismissRequested
    ///  - the user taps outside the tray       -> idem
    ///  - the same category is pressed again   -> toggle in the controller (PopOverlay)
    ///  - mobile back                          -> OverlayState.HandleBack
    ///
    /// The controller owns the UI elements: this state only drives their lifecycle
    /// (ShowTray on enter, HideTray on exit) and listens for the dismiss request.
    /// </summary>
    public class StateInteractionMenu : OverlayState
    {
        private readonly MainUIController controller;
        private readonly CareAction action;

        public StateInteractionMenu(MainUIController controller, CareAction action)
            : base(mainUiObject: null, hideMainUi: false)
        {
            this.controller = controller;
            this.action = action;
        }

        // Category (care action) of the open tray.
        public CareAction Action => action;

        // Shows the tray for the category and starts listening for the dismiss request.
        protected override void OnEnter()
        {
            if (controller == null)
            {
                GameStateMachine.Instance.PopOverlay();
                return;
            }

            controller.InteractionDismissRequested += HandleDismiss;
            controller.ShowTray(action);
        }

        // Hides the tray and detaches.
        protected override void OnExit()
        {
            if (controller != null)
            {
                controller.InteractionDismissRequested -= HandleDismiss;
                controller.HideTray();
            }
        }

        // Object picked up or tap outside: close the tray (back to main).
        private void HandleDismiss()
        {
            GameStateMachine.Instance.PopOverlay();
        }
    }
}
