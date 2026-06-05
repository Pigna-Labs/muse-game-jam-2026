using MuseGameJam.StateSystem;
using MuseGameJam.Trivia;
using UnityEngine;

namespace MuseGameJam.States
{
    public class MainGameStateBootstrap : MonoBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;
        [SerializeField] private GameObject triviaUiPrefab;
        [SerializeField] private TriviaQuestion startingTriviaQuestion;
        [SerializeField] private Transform overlayRoot;
        [SerializeField] private bool openTriviaOnStart = true;

        private MainGameState mainGameState;

        // Creates and enters the main game state using explicit scene and asset references.
        private void Start()
        {
            RequireReference(stateMachine, "MainGameStateBootstrap needs a GameStateMachine reference.");
            RequireReference(triviaUiPrefab, "MainGameStateBootstrap needs the TriviaUI prefab assigned.");
            RequireReference(startingTriviaQuestion, "MainGameStateBootstrap needs a starting TriviaQuestion asset assigned.");
            RequireReference(overlayRoot, "MainGameStateBootstrap needs an overlay root Transform assigned.");

            mainGameState = new MainGameState(
                stateMachine,
                triviaUiPrefab,
                startingTriviaQuestion,
                overlayRoot,
                openTriviaOnStart);

            stateMachine.ChangeState(mainGameState);
        }

        // Opens the configured starting trivia question from a mobile UI button or touch interaction.
        public void OpenStartingTriviaQuestion()
        {
            mainGameState.OpenTriviaQuestion(startingTriviaQuestion);
        }

        // Returns a required scene or asset reference, throwing loudly when a prefab or Inspector field is missing.
        private T RequireReference<T>(T reference, string message) where T : Object
        {
            if (reference == null)
            {
                throw new MissingReferenceException(message);
            }

            return reference;
        }
    }
}
