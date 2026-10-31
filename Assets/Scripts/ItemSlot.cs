using UnityEngine;
using UnityEngine.EventSystems;
using TMPro;
using UnityEngine.UI;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    public string itemName;
    public int itemQuantity;
    public Sprite itemIcon;
    public bool isFull;

    public ItemDetails ItemDetails => itemDetails;
    public bool hasItem => itemDetails != null && itemQuantity > 0;

    [SerializeField] private int maxStackSize = 99;

    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image itemImage;
    [SerializeField] private GameObject selectedPanel;

    private ItemDetails itemDetails;
    private InventoryManager inventory;

    public void Setup(InventoryManager owner)
    {
        inventory = owner;
        itemDetails = null;
        itemName = "";
        itemQuantity = 0;
        itemIcon = null;
        isFull = false;

        SetSelected(false);
        UpdateUI();
    }

    public bool CanStackWith(ItemDetails item)
    {
        if (!hasItem || item == null) return false;
        if (itemQuantity >= maxStackSize) return false;
        return itemName == item.ItemName || (itemDetails != null && itemDetails == item);
    }

    public bool CanStackWith(string otherItemName)
    {
        if (!hasItem || string.IsNullOrEmpty(otherItemName)) return false;
        if (itemQuantity >= maxStackSize) return false;
        return itemName == otherItemName;
    }

    public void AddItem(ItemDetails item)
    {
        itemDetails = item;
        itemName = item.ItemName;
        itemQuantity = item.Quantity > 0 ? item.Quantity : 1;
        itemIcon = item.ItemIcon;
        isFull = itemQuantity >= maxStackSize;

        UpdateUI();
    }

    public void AddQuantity(int amount)
    {
        itemQuantity += amount;
        isFull = itemQuantity >= maxStackSize;

        UpdateUI();
    }

    public void UpdateUI()
    {
        if (hasItem)
        {
            if (itemImage != null)
            {
                itemImage.sprite = itemIcon;
                itemImage.enabled = itemIcon != null;
            }

            if (quantityText != null)
            {
                quantityText.text = itemQuantity.ToString();
                quantityText.enabled = true;
            }
        }
        else
        {
            if (itemImage != null)
            {
                itemImage.sprite = null;
                itemImage.enabled = false;
            }

            if (quantityText != null)
            {
                quantityText.text = "";
                quantityText.enabled = false;
            }
        }
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (!hasItem || inventory == null) return;
        inventory.SelectSlot(this);
    }

    public void SetSelected(bool selected)
    {
        if (selectedPanel != null)
        {
            selectedPanel.SetActive(selected);
        }
    }
}
