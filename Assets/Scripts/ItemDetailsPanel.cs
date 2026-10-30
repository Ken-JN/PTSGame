using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ItemDetailsPanel : MonoBehaviour
{
    [SerializeField] private Image itemImage;
    [SerializeField] private TMP_Text itemNameText;
    [SerializeField] private TMP_Text itemQuantityText;
    [SerializeField] private TMP_Text itemDescriptionText;

    // Called by InventoryManager whenever the player selects a slot
    public void Show(ItemSlot slot)
    {
        if (slot == null || slot.ItemDetails == null) return;
        Show(slot.ItemDetails, slot.itemQuantity);
    }

    public void Show(ItemDetails item, int currentQuantity)
    {
        if (item == null) return;

        if (itemImage != null)
        {
            itemImage.sprite = item.ItemIcon;
            itemImage.enabled = item.ItemIcon != null;
        }

        if (itemNameText != null) itemNameText.text = item.ItemName;
        if (itemQuantityText != null) itemQuantityText.text = "Quantity: " + currentQuantity;
        if (itemDescriptionText != null) itemDescriptionText.text = item.Description;
    }

    public void Show(ItemDetails item)
    {
        if (item == null) return;
        Show(item, item.Quantity);
    }
}
