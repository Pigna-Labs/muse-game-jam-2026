using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "Challenge", menuName = "Scriptable Objects/Challenge")]
public class ChallengeSO : ScriptableObject
{
        [SerializeField] private string displayName;
        [SerializeField] private List<InfoSO> infos = new();
        [SerializeField] private UnlockableSO reward;

        public string DisplayName => displayName;
        public IReadOnlyList<InfoSO> Infos => infos;
        public UnlockableSO Reward => reward;
}
