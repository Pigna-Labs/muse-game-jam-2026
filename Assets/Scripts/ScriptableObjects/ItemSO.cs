using UnityEngine;

public enum ItemType { Food, Soap, Toy }

[CreateAssetMenu(fileName = "Item", menuName = "Scriptable Objects/Item")]
public class ItemSO : UnlockableSO
{
        [SerializeField] private ItemType itemType;

        public ItemType ItemType => itemType;
}
