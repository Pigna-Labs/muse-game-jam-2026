using UnityEngine;

namespace MuseGameJam.Gameplay
{
        [CreateAssetMenu(fileName = "Companion", menuName = "Scriptable Objects/Unlockables/Companion")]
        public class Companion : ScriptableObject, IUnlockable
        {
                [SerializeField] private string displayName;
                [SerializeField] private Sprite image;

                public string DisplayName => displayName;
                public Sprite Image => image;
        }
}
