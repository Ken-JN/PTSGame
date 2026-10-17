using UnityEngine;
using TMPro;
using UnityEngine.UI;
using UnityEngine.EventSystems;

public class ItemSlot : MonoBehaviour, IPointerClickHandler
{
    // Item Data
    public string itemName;
    public int itemQuantity;
    public Sprite itemIcon;
    public bool isFull;
    public ItemSlot[] itemSlot;

    // Maximum number of items that fit in one slot
    [SerializeField] private int maxStackSize = 99;

    // Item Slot
    [SerializeField] private TMP_Text quantityText;
    [SerializeField] private Image itemImage;
    //selected item
    [SerializeField] private Image selectedPanel;
    public bool isItemSelected;
    public InventoryManager inventoryManager;

    // Check if this slot can stack more of the same item
    public bool CanStackWith(string otherItemName)
    {
        return !isFull && itemName == otherItemName;
    }
    public void Start()
    {
        inventoryManager = FindObjectOfType<InventoryManager>();
    }

    public void AddItem(ItemDetails item)
    {
        itemName = item.ItemName;
        itemIcon = item.ItemIcon;
        AddQuantity(item.Quantity);
    }

    // Increase the count of items already in this slot
    public void AddQuantity(int amount)
    {
        itemQuantity += amount;
        isFull = itemQuantity >= maxStackSize;

        quantityText.text = itemQuantity.ToString();
        quantityText.enabled = true;
        itemImage.sprite = itemIcon;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if(eventData.button == PointerEventData.InputButton.Left)
        {
            LeftClick();
        }
        if(eventData.button == PointerEventData.InputButton.Right)
        {
            RightClick();
        }
    }
    public void DeselectAllSlots()
    {
        foreach (ItemSlot slot in itemSlot)
        {
            slot.selectedPanel.gameObject.SetActive(false);
            slot.isItemSelected = false;
        }
        
    }

    private void LeftClick()
    {
        Debug.Log("Left-clicked on item: " + itemName);
        inventoryManager.DeselectAllSlots();
        selectedPanel.gameObject.SetActive(true);
        isItemSelected = true;
    }

    private void RightClick()
    {
        Debug.Log("Right-clicked on item: " + itemName);
        // selectedPanel.gameObject.SetActive(false);
        // isItemSelected = false;
    }
    

}

