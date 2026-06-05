namespace MuseGameJam.StateSystem
{
    public abstract class GameState : IGameState
    {
        // Override to create or enable this state's UI/gameplay when it becomes active.
        public virtual void Enter()
        {
        }

        // Override to destroy or disable anything this state owns before it is removed.
        public virtual void Exit()
        {
        }

        // Override to freeze or dim this state when another state is placed above it.
        public virtual void Pause()
        {
        }

        // Override to restore this state after the state above it has been removed.
        public virtual void Resume()
        {
        }

        // Override for frame-based work that should run only while this state is on top.
        public virtual void Tick(float deltaTime)
        {
        }

        // Override to handle mobile back behavior; return true when the state consumed it.
        public virtual bool HandleBack()
        {
            return false;
        }
    }
}
