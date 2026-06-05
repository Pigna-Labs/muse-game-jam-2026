using MuseGameJam.StateSystem;
using MuseGameJam.Trivia;
using UnityEngine;

namespace MuseGameJam.States
{
    public class StateMainGame : GameState
    {
        private readonly GameStateMachine stateMachine;
        private readonly GameObject triviaUiPrefab;
        private readonly TriviaQuestion startingTriviaQuestion;
        private readonly Transform overlayRoot;
        private readonly bool openTriviaOnEnter;
        private bool openedStartingTrivia;

        public StateMainGame(GameStateMachine stateMachine)
            : this(stateMachine, null, null, null, false)
        {
        }

        public StateMainGame(
            GameStateMachine stateMachine,
            GameObject triviaUiPrefab,
            TriviaQuestion startingTriviaQuestion,
            Transform overlayRoot,
            bool openTriviaOnEnter)
        {
            this.stateMachine = stateMachine;
            this.triviaUiPrefab = triviaUiPrefab;
            this.startingTriviaQuestion = startingTriviaQuestion;
            this.overlayRoot = overlayRoot;
            this.openTriviaOnEnter = openTriviaOnEnter;
        }

        // Starts the main game mode and optionally opens the first trivia overlay.
        public override void Enter()
        {
            if (!openTriviaOnEnter || openedStartingTrivia)
            {
                return;
            }

            openedStartingTrivia = true;
            OpenTriviaQuestion(startingTriviaQuestion);
        }

        // Opens a trivia question above the main game without replacing the main game state.
        public void OpenTriviaQuestion(TriviaQuestion question)
        {
            if (stateMachine == null)
            {
                Debug.LogError("StateMainGame needs a GameStateMachine before it can open trivia.");
                return;
            }

            if (triviaUiPrefab == null || question == null)
            {
                Debug.LogWarning("StateMainGame cannot open trivia until a TriviaUI prefab and question are assigned.");
                return;
            }

            stateMachine.PushOverlay(new TriviaQuestionOverlayState(stateMachine, triviaUiPrefab, question, overlayRoot));
        }
    }
}
