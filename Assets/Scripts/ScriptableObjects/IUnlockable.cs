using UnityEngine;

namespace MuseGameJam.Gameplay
{
        public interface IUnlockable
        {
                string DisplayName { get; }
                Sprite Image { get; }
        }
}
