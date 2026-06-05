using System;
using System.Collections.Generic;
using System.Text;
using UnityEngine;

namespace MuseGameJam.StateSystem
{
    public class GameStateMachine : MonoBehaviour
    {
        private readonly List<IGameState> stateStack = new();

        [SerializeField] private bool persistAcrossScenes = true;

        public static GameStateMachine Instance { get; private set; }

        public IGameState CurrentState => stateStack.Count > 0 ? stateStack[0] : null;
        public IGameState TopState => stateStack.Count > 0 ? stateStack[^1] : null;
        public IEnumerable<IGameState> States => stateStack;
        public IEnumerable<IGameState> Overlays => GetOverlays();
        public int StateCount => stateStack.Count;
        public int OverlayCount => Mathf.Max(0, stateStack.Count - 1);
        public bool HasOverlayOpen => OverlayCount > 0;

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

            ClearStack();
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

        // Pushes a state above the current top state.
        public void PushState(IGameState nextState)
        {
            if (nextState == null)
            {
                Debug.LogError("Cannot push a null game state.");
                return;
            }

            TopState?.Pause();
            stateStack.Add(nextState);
            LogStateStack($"Pushed state {GetStateName(nextState)}");
            nextState.Enter();
        }

        // Replaces the whole stack with a new state.
        public void TransitionToState(IGameState nextState)
        {
            if (nextState == null)
            {
                Debug.LogError("Cannot transition to a null game state.");
                return;
            }

            ClearStack();
            stateStack.Add(nextState);
            LogStateStack($"Transitioned to {GetStateName(nextState)}");
            nextState.Enter();
        }

        // Removes the top state and resumes the state underneath it.
        public void PopState()
        {
            if (stateStack.Count == 0)
            {
                return;
            }

            IGameState poppedState = PopTop();
            TopState?.Resume();
            LogStateStack($"Popped state {GetStateName(poppedState)}");
        }

        // Closes the current top overlay and resumes the next state underneath it.
        public void PopOverlay()
        {
            if (!HasOverlayOpen)
            {
                return;
            }

            PopState();
        }

        // Closes every overlay and resumes the main state if any overlay was open.
        public void PopAllOverlays()
        {
            PopAllOverlays(resumeCurrentState: true);
        }

        // Closes all overlays, optionally skipping resume when the main state will exit next.
        private void PopAllOverlays(bool resumeCurrentState)
        {
            bool hadOverlays = HasOverlayOpen;

            while (HasOverlayOpen)
            {
                IGameState poppedOverlay = PopTop();
                LogStateStack($"Popped overlay {GetStateName(poppedOverlay)}");
            }

            if (resumeCurrentState && hadOverlays)
            {
                CurrentState?.Resume();
            }
        }

        // Routes mobile back behavior to the top state first, then falls back to overlay closing.
        public void Back()
        {
            if (TopState != null && TopState.HandleBack())
            {
                return;
            }

            if (HasOverlayOpen)
            {
                PopOverlay();
                return;
            }

            BackUnhandled?.Invoke();
        }

        // Checks whether a specific overlay type is already open before pushing duplicates.
        public bool HasOverlay<TOverlay>() where TOverlay : IGameState
        {
            for (int i = stateStack.Count - 1; i > 0; i--)
            {
                if (stateStack[i] is TOverlay)
                {
                    return true;
                }
            }

            return false;
        }

        // Prints the current main state, active top state, and overlay stack for state transition debugging.
        private void LogStateStack(string action)
        {
            Debug.Log(
                $"[StateMachine] {action}\n" +
                $"Current state: {GetStateName(CurrentState)}\n" +
                $"Running state: {GetStateName(TopState)}\n" +
                $"State stack: {GetStateStackDescription()}",
                this);
        }

        // Returns a readable state name for transition logs.
        private string GetStateName(IGameState state)
        {
            return state == null ? "None" : state.GetType().Name;
        }

        // Builds a bottom-to-top view of every active state.
        private string GetStateStackDescription()
        {
            if (stateStack.Count == 0)
            {
                return "Empty";
            }

            StringBuilder builder = new();

            for (int i = 0; i < stateStack.Count; i++)
            {
                if (i > 0)
                {
                    builder.Append(" > ");
                }

                builder.Append(GetStateName(stateStack[i]));
            }

            return builder.ToString();
        }

        private IEnumerable<IGameState> GetOverlays()
        {
            for (int i = 1; i < stateStack.Count; i++)
            {
                yield return stateStack[i];
            }
        }

        private void ClearStack()
        {
            while (stateStack.Count > 0)
            {
                PopTop();
            }
        }

        // Removes the top state and runs its Exit cleanup, returning it for logging.
        private IGameState PopTop()
        {
            IGameState poppedState = stateStack[^1];
            stateStack.RemoveAt(stateStack.Count - 1);
            poppedState.Exit();
            return poppedState;
        }
    }
}
