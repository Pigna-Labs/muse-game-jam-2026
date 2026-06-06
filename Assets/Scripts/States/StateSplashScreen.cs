using MuseGameJam.StateSystem;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UIElements;

namespace MuseGameJam.States
{
    /// <summary>
    /// Entry splash shown in 00_Entry. The visuals are authored in the scene: a
    /// UIDocument (Splash.uxml/Splash.uss) draws the muse logo centered on a full-screen
    /// black background. This state only animates the logo's opacity (fade in -> hold ->
    /// fade out) and then loads the next scene (01_Main).
    ///
    /// The fade is driven by Tick(), which the GameStateMachine calls every frame while
    /// this state is on top of the stack.
    /// </summary>
    public class StateSplashScreen : GameState
    {
        private const string LogoElementName = "logo";

        private readonly string nextSceneName;
        private readonly float fadeInDuration;
        private readonly float holdDuration;
        private readonly float fadeOutDuration;

        private VisualElement logoElement;
        private AsyncOperation sceneLoad;
        private float elapsed;
        private bool loadStarted;
        private bool activationRequested;

        public StateSplashScreen(string nextSceneName)
            : this(nextSceneName, 0.8f, 0.8f, 0.8f)
        {
        }

        public StateSplashScreen(
            string nextSceneName,
            float fadeInDuration,
            float holdDuration,
            float fadeOutDuration)
        {
            this.nextSceneName = nextSceneName;
            this.fadeInDuration = Mathf.Max(0f, fadeInDuration);
            this.holdDuration = Mathf.Max(0f, holdDuration);
            this.fadeOutDuration = Mathf.Max(0f, fadeOutDuration);
        }

        private float TotalDuration => fadeInDuration + holdDuration + fadeOutDuration;

        // Grabs the logo element from the scene's splash UIDocument and starts the timer.
        public override void Enter()
        {
            if (string.IsNullOrWhiteSpace(nextSceneName))
            {
                Debug.LogError("StateSplashScreen needs a scene name to load.");
                return;
            }

            elapsed = 0f;
            loadStarted = false;
            activationRequested = false;
            sceneLoad = null;

            var document = Object.FindFirstObjectByType<UIDocument>();
            if (document == null)
            {
                Debug.LogWarning("StateSplashScreen: no UIDocument found in 00_Entry; skipping the splash.");
                return;
            }

            logoElement = document.rootVisualElement?.Q<VisualElement>(LogoElementName);
            if (logoElement == null)
            {
                Debug.LogWarning($"StateSplashScreen: no '#{LogoElementName}' element in the splash UXML.");
                return;
            }

            logoElement.style.opacity = 0f;
        }

        // Drives the fade, starts the async load when fade-out begins so loading happens
        // in parallel with the fade, and activates the new scene the moment the logo
        // reaches opacity 0.
        public override void Tick(float deltaTime)
        {
            elapsed += deltaTime;

            if (logoElement != null)
            {
                logoElement.style.opacity = ComputeLogoOpacity(elapsed);
            }

            float fadeOutStart = fadeInDuration + holdDuration;
            if (!loadStarted && elapsed >= fadeOutStart)
            {
                loadStarted = true;
                sceneLoad = SceneManager.LoadSceneAsync(nextSceneName);
                if (sceneLoad != null)
                {
                    sceneLoad.allowSceneActivation = false;
                }
            }

            if (!activationRequested && elapsed >= TotalDuration && sceneLoad != null)
            {
                activationRequested = true;
                sceneLoad.allowSceneActivation = true;
            }
        }

        // 0 -> 1 during fade-in, 1 while holding, 1 -> 0 during fade-out (smoothed).
        private float ComputeLogoOpacity(float time)
        {
            if (time < fadeInDuration)
            {
                return fadeInDuration > 0f ? Mathf.SmoothStep(0f, 1f, time / fadeInDuration) : 1f;
            }

            float holdEnd = fadeInDuration + holdDuration;
            if (time < holdEnd)
            {
                return 1f;
            }

            if (time < TotalDuration)
            {
                return fadeOutDuration > 0f
                    ? Mathf.SmoothStep(1f, 0f, (time - holdEnd) / fadeOutDuration)
                    : 0f;
            }

            return 0f;
        }
    }
}
