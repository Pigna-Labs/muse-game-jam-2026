using MuseGameJam.StateSystem;
using MuseGameJam.Trivia;
using MuseGameJam.UI;
using UnityEngine;

namespace MuseGameJam.States
{
    public class StateMainGame : GameState
    {
        private readonly GameObject triviaUiPrefab;
        private readonly TriviaQuestion startingTriviaQuestion;
        private readonly Transform overlayRoot;
        private readonly bool openTriviaOnEnter;
        private bool openedStartingTrivia;
        private bool randomizedStats;

        public StateMainGame()
            : this(null, null, null, false)
        {
        }

        public StateMainGame(
            GameObject triviaUiPrefab,
            TriviaQuestion startingTriviaQuestion,
            Transform overlayRoot,
            bool openTriviaOnEnter)
        {
            this.triviaUiPrefab = triviaUiPrefab;
            this.startingTriviaQuestion = startingTriviaQuestion;
            this.overlayRoot = overlayRoot;
            this.openTriviaOnEnter = openTriviaOnEnter;
        }

        // Starts the main game mode and optionally opens the first trivia overlay.
        public override void Enter()
        {
            RandomizeCreatureStats();

            if (!openTriviaOnEnter || openedStartingTrivia)
            {
                return;
            }

            openedStartingTrivia = true;
            OpenTriviaQuestion(startingTriviaQuestion);
        }

        // All'ingresso nel gioco le tre barre (food/clean/pet) partono a valori casuali.
        // Una volta sola: lo stato non viene ri-entrato quando un overlay viene chiuso.
        private void RandomizeCreatureStats()
        {
            if (randomizedStats)
            {
                return;
            }

            randomizedStats = true;

            var ui = Object.FindFirstObjectByType<MainUIController>();
            if (ui != null)
            {
                ui.RandomizeStats();
            }
            else
            {
                Debug.LogWarning("StateMainGame: nessun MainUIController in scena per randomizzare le barre.");
            }
        }

        // Opens a trivia question above the main game without replacing the main game state.
        public void OpenTriviaQuestion(TriviaQuestion question)
        {
            if (GameStateMachine.Instance == null)
            {
                Debug.LogError("StateMainGame needs a GameStateMachine before it can open trivia.");
                return;
            }

            if (triviaUiPrefab == null || question == null)
            {
                Debug.LogWarning("StateMainGame cannot open trivia until a TriviaUI prefab and question are assigned.");
                return;
            }

            GameStateMachine.Instance.PushState(new TriviaQuestionOverlayState(triviaUiPrefab, question, overlayRoot));
        }
    }
}
