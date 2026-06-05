using MuseGameJam.States;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MuseGameJam.StateSystem
{
    [RequireComponent(typeof(GameStateMachine))]
    public class StateManager : MonoBehaviour
    {
        [SerializeField] private string entrySceneName = "00_Entry";
        [SerializeField] private string mainSceneName = "01_Main";

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
            if (GameStateMachine.Instance.TopState is StateSplashScreen)
            {
                return;
            }

            GameStateMachine.Instance.PushState(new StateSplashScreen(mainSceneName));
        }

        private void TransitionToMainGame()
        {
            if (GameStateMachine.Instance.TopState is StateMainGame)
            {
                return;
            }

            GameStateMachine.Instance.TransitionToState(new StateMainGame());
        }
    }
}
