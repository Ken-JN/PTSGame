using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour
{
    // Item Data
    public string itemName;
    public int itemQuantity;
    public Sprite itemIcon;
    public bool hasItem;
    public bool isFull => itemQuantity >= maxStackSize;

    // Item Slot
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image itemImage;
    [SerializeField] private int maxStackSize = 99;
    
   public void AddItem(ItemDetails item)
    {
        itemName = item.ItemName;
        itemQuantity = item.Quantity;
        itemIcon = item.ItemIcon;
        hasItem = true;

        quantityText.text = itemQuantity.ToString();
        quantityText.enabled = true;
        if (itemImage != null)
        {
            itemImage.sprite = itemIcon;
            itemImage.enabled = true;
        }
    }

    public bool CanStackWith(string otherItemName)
    {
        return hasItem && itemName == otherItemName && !isFull;
    }

     public void AddQuantity(int amount)
    {
        itemQuantity += amount;

        quantityText.text = itemQuantity.ToString();
        quantityText.enabled = true;
        if (itemImage != null && itemIcon != null)
        {
            itemImage.sprite = itemIcon;
            itemImage.enabled = true;
        }
    }
    
}
