using MuseGameJam.StateSystem;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace MuseGameJam.States
{
    public class StateSplashScreen : GameState
    {
        private readonly string nextSceneName;

        public StateSplashScreen(string nextSceneName)
        {
            this.nextSceneName = nextSceneName;
        }

        public override void Enter()
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogError("StateSplashScreen needs a scene name to load.");
                return;
            }

            SceneManager.LoadScene(nextSceneName);
        }
    }
}
