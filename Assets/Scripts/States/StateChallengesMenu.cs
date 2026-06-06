using MuseGameJam.StateSystem;
using MuseGameJam.UI;
using UnityEngine;

namespace MuseGameJam.States
{
    /// <summary>
    /// Challenges menu overlay, opened by the "target" button of the MainUI.
    ///
    /// Pushed onto the stack by MainUIController: the GameStateMachine pauses the
    /// main state and this state instantiates its UI above the main scene. The
    /// challenges panel renders above the main UI (higher PanelSettings sort order),
    /// so the main UI is kept visible behind it (hideMainUi: false) instead of hidden.
    ///
    /// It closes (PopOverlay) when:
    ///  - the user presses "Close"  -> ChallengesMenuController.CloseRequested
    ///  - mobile back               -> OverlayState.HandleBack
    ///
    /// Like the trivia overlay, the UI is a prefab (UIDocument + ChallengesMenuController):
    /// instantiated on enter and destroyed on exit.
    /// </summary>
    public class StateChallengesMenu : OverlayState
    {
        private readonly GameObject challengesUiPrefab;
        private readonly Transform parent;
        private GameObject challengesUiInstance;
        private ChallengesMenuController challengesUi;

        public StateChallengesMenu(GameObject challengesUiPrefab, Transform parent, GameObject mainUiObject = null)
            : base(mainUiObject, hideMainUi: false)
        {
            this.challengesUiPrefab = challengesUiPrefab;
            this.parent = parent;
        }

        // Creates the challenges menu UI above the main scene.
        protected override void OnEnter()
        {
            challengesUiInstance = Object.Instantiate(challengesUiPrefab, parent);
            challengesUi = challengesUiInstance.GetComponent<ChallengesMenuController>();

            if (challengesUi == null)
            {
                throw new MissingComponentException("Challenges overlay prefab needs a ChallengesMenuController component.");
            }

            challengesUi.CloseRequested += HandleCloseRequested;
        }

        // Mobile back: slide the sheet down (same as the Close button) instead of closing instantly.
        public override bool HandleBack()
        {
            if (challengesUi != null)
            {
                challengesUi.RequestClose();
                return true;
            }

            return base.HandleBack();
        }

        // Removes the UI instance when the overlay closes.
        protected override void OnExit()
        {
            if (challengesUi != null)
            {
                challengesUi.CloseRequested -= HandleCloseRequested;
            }

            Object.Destroy(challengesUiInstance);
        }

        // The user pressed "Close": close the overlay (back to the main state).
        private void HandleCloseRequested()
        {
            GameStateMachine.Instance.PopOverlay();
        }
    }
}
