using UnityEngine;

[CreateAssetMenu(fileName = "Companion", menuName = "Scriptable Objects/Companion")]
public class CompanionSO : ScriptableObject
{
        [SerializeField] private string displayName;
        [SerializeField] private Sprite image;
        [SerializeField] private bool unlocked;

        public string DisplayName => displayName;
        public Sprite Image => image;
        public bool Unlocked => unlocked;
}