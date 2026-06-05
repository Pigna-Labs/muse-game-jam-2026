using UnityEngine;

[CreateAssetMenu(fileName = "Data", menuName = "Scriptable Objects/Unlockable")]
public class UnlockableScriptableObject : ScriptableObject
{
        [SerializeField] private string displayName;
        [SerializeField] private string qrValue;          // unique id — what the QR scan returns
        [SerializeField] private Sprite icon;
        [SerializeField] private GameObject modelPrefab;

        public string DisplayName => displayName;
        public string QrValue => qrValue;
        public Sprite Icon => icon;
        public GameObject ModelPrefab => modelPrefab;
}
