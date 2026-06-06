using UnityEngine;

// Shared base for everything the player can unlock (companions, items).
// The 'unlocked' flag means "unlocked from the start" (some entries ship unlocked
// in this prototype). Entries unlocked at runtime are tracked by ChallengeManager's
// registry instead, so this stays an authoring-time start state.
public abstract class UnlockableSO : ScriptableObject
{
        [SerializeField] protected string displayName;
        [SerializeField] protected Sprite image;
        [SerializeField] protected bool unlocked;

        public string DisplayName => displayName;
        public Sprite Image => image;
        public bool Unlocked => unlocked;
}
