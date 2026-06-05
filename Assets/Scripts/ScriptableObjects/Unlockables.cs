using System.Collections.Generic;
using UnityEngine;

namespace MuseGameJam.Gameplay
{
        [CreateAssetMenu(fileName = "Unlockables", menuName = "Scriptable Objects/Unlockables")]
        public class Unlockables : ScriptableObject
        {
                [SerializeField] private List<Food> foods = new();
                [SerializeField] private List<Companion> companions = new();

                public IReadOnlyList<Food> Foods => foods;
                public IReadOnlyList<Companion> Companions => companions;
        }
}
