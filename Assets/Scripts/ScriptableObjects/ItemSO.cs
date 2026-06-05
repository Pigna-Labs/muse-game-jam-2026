using UnityEngine;

public enum ItemType { Food, Soap, Toy }

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class ItemSO : ScriptableObject
{
        [SerializeField] private string displayName;
        [SerializeField] private ItemType itemType;
        [SerializeField] private int quantity;
        [SerializeField] private int gain;
        [SerializeField] private Sprite image;

        public string DisplayName => displayName;
        public ItemType ItemType => itemType;
        public int Quantity => quantity;
        public int Gain => gain;
        public Sprite Image => image;
}
