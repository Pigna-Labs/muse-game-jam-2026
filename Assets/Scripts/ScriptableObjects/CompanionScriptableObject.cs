using UnityEngine;

[CreateAssetMenu(fileName = "Companion", menuName = "Scriptable Objects/Companion")]
public class CompanionScriptableObject : ScriptableObject
{
        [SerializeField] private string displayName;
        [SerializeField] private GameObject modelPrefab;
        [SerializeField] private bool unlocked;

        public string DisplayName => displayName;
        public GameObject ModelPrefab => modelPrefab;
        public bool Unlocked => unlocked;
}