namespace MuseGameJam.StateSystem
{
    public interface IGameState
    {
        // Called once when this state becomes active and should create or enable its UI/gameplay.
        void Enter();

        // Called once when this state is removed and should clean up anything it owns.
        void Exit();

        // Called when another state or overlay is placed above this one.
        void Pause();

        // Called when the state above this one is removed and this state becomes active again.
        void Resume();

        // Called every frame only while this is the top active state.
        void Tick(float deltaTime);

        // Called by the state machine when mobile back or a visible back button is pressed.
        bool HandleBack();
    }
}
