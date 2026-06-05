using System.Collections.Generic;
using UnityEngine;

namespace MuseGameJam.Gameplay
{
        [CreateAssetMenu(fileName = "Unlockable Manager", menuName = "Scriptable Objects/Unlockable Manager")]
        public class Unlockables : ScriptableObject
        {
                [SerializeField] private List<ItemSO> foods = new();
                [SerializeField] private List<ItemSO> soaps = new();
                [SerializeField] private List<ItemSO> toys = new();
                [SerializeField] private List<CompanionSO> companions = new();

                public IReadOnlyList<ItemSO> Foods => foods;
                public  IReadOnlyList<ItemSO> Soaps => soaps;
                public IReadOnlyList<ItemSO> Toys => toys;
                public IReadOnlyList<CompanionSO> Companions => companions;
        }
}
