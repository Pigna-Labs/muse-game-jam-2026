using MuseGameJam.States;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MuseGameJam.StateSystem
{
    [RequireComponent(typeof(GameStateMachine))]
    public class StateManager : MonoBehaviour
    {
        [SerializeField] private GameStateMachine stateMachine;
        [SerializeField] private string entrySceneName = "00_Entry";
        [SerializeField] private string mainSceneName = "01_Main";

        private void Awake()
        {
            if (stateMachine == null)
            {
                stateMachine = GetComponent<GameStateMachine>();
            }
        }

        private void OnEnable()
        {
            SceneManager.sceneLoaded += HandleSceneLoaded;
        }

        private void Start()
        {
            HandleSceneLoaded(SceneManager.GetActiveScene(), LoadSceneMode.Single);
        }

        private void OnDisable()
        {
            SceneManager.sceneLoaded -= HandleSceneLoaded;
        }

        private void HandleSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (scene.name == entrySceneName)
            {
                PushSplashScreen();
                return;
            }

            if (scene.name == mainSceneName)
            {
                TransitionToMainGame();
            }
        }

        private void PushSplashScreen()
        {
            if (stateMachine.TopState is StateSplashScreen)
            {
                return;
            }

            stateMachine.PushState(new StateSplashScreen(mainSceneName));
        }

        private void TransitionToMainGame()
        {
            if (stateMachine.TopState is StateMainGame)
            {
                return;
            }

            stateMachine.TransitionToState(new StateMainGame(stateMachine));
        }
    }
}
