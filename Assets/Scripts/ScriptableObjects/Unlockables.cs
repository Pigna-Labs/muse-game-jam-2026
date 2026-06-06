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
                [SerializeField] private List<InfoSO> infos = new();

                public IReadOnlyList<ItemSO> Foods => foods;
                public  IReadOnlyList<ItemSO> Soaps => soaps;
                public IReadOnlyList<ItemSO> Toys => toys;
                public IReadOnlyList<CompanionSO> Companions => companions;
                public IReadOnlyList<InfoSO> Infos => infos;

                // Returns the Info asset whose QrValue matches the scanned text, or null if none does.
                public InfoSO FindInfoByQrValue(string qrValue)
                {
                        if (string.IsNullOrEmpty(qrValue)) return null;

                        foreach (InfoSO info in infos)
                        {
                                if (info != null && info.QrValue == qrValue)
                                {
                                        return info;
                                }
                        }

                        return null;
                }
        }
}
