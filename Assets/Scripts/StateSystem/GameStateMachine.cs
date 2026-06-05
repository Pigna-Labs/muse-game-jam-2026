using System;
using System.Collections.Generic;
using UnityEngine;

namespace MuseGameJam.StateSystem
{
    public class GameStateMachine : MonoBehaviour
    {
        private readonly Stack<IGameState> overlays = new();
        private IGameState currentState;

        [SerializeField] private bool persistAcrossScenes = true;

        public static GameStateMachine Instance { get; private set; }

        public IGameState CurrentState => currentState;
        public IGameState TopState => overlays.Count > 0 ? overlays.Peek() : currentState;
        public IEnumerable<IGameState> Overlays => overlays;
        public int OverlayCount => overlays.Count;
        public bool HasOverlayOpen => overlays.Count > 0;

        public event Action BackUnhandled;
        public event Action<bool> ApplicationPauseChanged;
        public event Action<bool> ApplicationFocusChanged;

        // Sets up the singleton instance before any state code asks for the state machine.
        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(gameObject);
                return;
            }

            Instance = this;

            if (persistAcrossScenes)
            {
                DontDestroyOnLoad(gameObject);
            }
        }

        // Ticks only the top active state so paused gameplay does not keep advancing.
        private void Update()
        {
            TopState?.Tick(Time.deltaTime);
        }

        // Cleans up active state objects when the state machine GameObject is destroyed.
        private void OnDestroy()
        {
            if (Instance != this)
            {
                return;
            }

            PopAllOverlays(resumeCurrentState: false);
            currentState?.Exit();
            currentState = null;
            Instance = null;
        }

        // Broadcasts mobile app pause changes so game states can decide how to react.
        private void OnApplicationPause(bool paused)
        {
            ApplicationPauseChanged?.Invoke(paused);
        }

        // Broadcasts mobile focus changes for platforms where focus loss should affect flow.
        private void OnApplicationFocus(bool hasFocus)
        {
            ApplicationFocusChanged?.Invoke(hasFocus);
        }

        // Replaces the main app mode after closing any active overlays.
        public void ChangeState(IGameState nextState)
        {
            if (nextState == null)
            {
                Debug.LogError("Cannot change to a null game state.");
                return;
            }

            PopAllOverlays(resumeCurrentState: false);
            currentState?.Exit();
            currentState = nextState;
            currentState.Enter();
        }

        // Opens a temporary state above the current top state, such as pause or settings.
        public void PushOverlay(IGameState overlay)
        {
            if (overlay == null)
            {
                Debug.LogError("Cannot push a null overlay state.");
                return;
            }

            TopState?.Pause();
            overlays.Push(overlay);
            overlay.Enter();
        }

        // Closes the current top overlay and resumes the next state underneath it.
        public void PopOverlay()
        {
            if (overlays.Count == 0)
            {
                return;
            }

            overlays.Pop().Exit();
            TopState?.Resume();
        }

        // Closes every overlay and resumes the main state if any overlay was open.
        public void PopAllOverlays()
        {
            PopAllOverlays(resumeCurrentState: true);
        }

        // Closes all overlays, optionally skipping resume when the main state will exit next.
        private void PopAllOverlays(bool resumeCurrentState)
        {
            bool hadOverlays = overlays.Count > 0;

            while (overlays.Count > 0)
            {
                overlays.Pop().Exit();
            }

            if (resumeCurrentState && hadOverlays)
            {
                currentState?.Resume();
            }
        }

        // Routes mobile back behavior to the top state first, then falls back to overlay closing.
        public void Back()
        {
            if (TopState != null && TopState.HandleBack())
            {
                return;
            }

            if (overlays.Count > 0)
            {
                PopOverlay();
                return;
            }

            BackUnhandled?.Invoke();
        }

        // Checks whether a specific overlay type is already open before pushing duplicates.
        public bool HasOverlay<TOverlay>() where TOverlay : IGameState
        {
            foreach (IGameState overlay in overlays)
            {
                if (overlay is TOverlay)
                {
                    return true;
                }
            }

            return false;
        }
    }
}
