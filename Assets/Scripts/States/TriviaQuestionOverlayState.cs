using MuseGameJam.StateSystem;
using MuseGameJam.Trivia;
using MuseGameJam.UI;
using UnityEngine;

namespace MuseGameJam.States
{
    public class TriviaQuestionOverlayState : GameState
    {
        private readonly GameStateMachine stateMachine;
        private readonly GameObject triviaUiPrefab;
        private readonly TriviaQuestion question;
        private readonly Transform parent;
        private GameObject triviaUiInstance;
        private TriviaUiController triviaUi;

        public TriviaQuestionOverlayState(
            GameStateMachine stateMachine,
            GameObject triviaUiPrefab,
            TriviaQuestion question,
            Transform parent)
        {
            this.stateMachine = stateMachine;
            this.triviaUiPrefab = triviaUiPrefab;
            this.question = question;
            this.parent = parent;
        }

        // Creates the authored trivia UI prefab above the current main game scene.
        public override void Enter()
        {
            triviaUiInstance = Object.Instantiate(triviaUiPrefab, parent);
            triviaUi = triviaUiInstance.GetComponent<TriviaUiController>();

            if (triviaUi == null)
            {
                throw new MissingComponentException("Trivia overlay prefab needs a TriviaUiController component.");
            }

            triviaUi.SetQuestion(question);
            triviaUi.AnswerSelected += HandleAnswerSelected;
        }

        // Removes the trivia UI prefab instance when the overlay closes.
        public override void Exit()
        {
            triviaUi.AnswerSelected -= HandleAnswerSelected;
            Object.Destroy(triviaUiInstance);
        }

        // Closes the trivia overlay when the mobile back action reaches this state.
        public override bool HandleBack()
        {
            stateMachine.PopOverlay();
            return true;
        }

        // Receives the selected answer index after TriviaUiController has shown feedback.
        private void HandleAnswerSelected(int answerIndex)
        {
        }
    }
}
