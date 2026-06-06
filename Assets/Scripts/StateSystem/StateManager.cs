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

        [Header("Splash screen (00_Entry)")]
        [Tooltip("Seconds the muse logo fades in / stays / fades out before loading 01_Main.")]
        [SerializeField] private float splashFadeInDuration = 0.5f;
        [SerializeField] private float splashHoldDuration = 0.4f;
        [SerializeField] private float splashFadeOutDuration = 0.5f;

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

            GameStateMachine.Instance.PushState(new StateSplashScreen(
                mainSceneName,
                splashFadeInDuration,
                splashHoldDuration,
                splashFadeOutDuration));
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
