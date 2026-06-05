using UnityEngine;

namespace MuseGameJam.Gameplay
{
        [CreateAssetMenu(fileName = "Food", menuName = "Scriptable Objects/Unlockables/Food")]
        public class Food : ScriptableObject, IUnlockable
        {
                [SerializeField] private string displayName;
                [SerializeField] private Sprite image;

                public string DisplayName => displayName;
                public Sprite Image => image;
        }
}
