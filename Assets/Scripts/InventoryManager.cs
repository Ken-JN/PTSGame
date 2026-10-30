using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

public class InventoryManager : MonoBehaviour, GrabableItem
{
    private InputAction inventoryAction;
    [SerializeField] private GameObject InventoryCanvas;
    [SerializeField] private int slotCount = 16;
    [SerializeField] private ItemDetailsPanel detailsPanel;
    private bool InventoryActive = false;
    private ItemSlot selectedSlot;
    public ItemSlot[] itemSlot;

    void Start()
    {
        CreateSlots();
        if (InventoryCanvas != null)
        {
            InventoryCanvas.SetActive(false);
        }

        if (InputSystem.actions != null)
        {
            inventoryAction = InputSystem.actions.FindAction("Inventory");
        }
    }

    void Update()
    {
        // Detect if the I key or Inventory action is triggered
        bool isPressed = false;
        if (inventoryAction != null && inventoryAction.triggered)
        {
            isPressed = true;
        }
        else if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            isPressed = true;
        }

        if (isPressed && InventoryCanvas != null)
        {
            if (!InventoryActive)
            {
                InventoryActive = true;
                Time.timeScale = 0f; 
                InventoryCanvas.SetActive(true);
            }
            else
            {
                // Close Inventory
                InventoryActive = false;
                Time.timeScale = 1f; // Resume game
                InventoryCanvas.SetActive(false);
            }
        }
    }

    // Automatically instantiate slots in grid layout
    private void CreateSlots()
    {
        if (InventoryCanvas == null) return;

        ItemSlot slotPrefab = Resources.Load<ItemSlot>("ItemSlot");
        if (slotPrefab == null)
        {
            Debug.LogError("Prefab ItemSlot not found in Resources folder.", this);
            itemSlot = new ItemSlot[0];
            return;
        }

        Transform parent = InventoryCanvas.transform;
        GridLayoutGroup grid = InventoryCanvas.GetComponentInChildren<GridLayoutGroup>(true);
        if (grid != null)
        {
            parent = grid.transform;
        }

        itemSlot = new ItemSlot[slotCount];
        for (int i = 0; i < itemSlot.Length; i++)
        {
            itemSlot[i] = Instantiate(slotPrefab, parent);
            itemSlot[i].Setup(this);
        }
    }

    // Called when the player clicks on an item slot
    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        selectedSlot = slot;
        slot.SetSelected(true);

        // Display item details on the right panel
        if (detailsPanel != null)
        {
            detailsPanel.Show(slot);
        }
    }

    // Adds a collected item to an existing stack or an available empty slot
    public void AddItem(ItemDetails item)
    {
        if (itemSlot == null || itemSlot.Length == 0)
        {
            Debug.LogWarning("Inventory has no slots configured!");
            return;
        }

        int quantityToAdd = item.Quantity > 0 ? item.Quantity : 1;

        // 1. Try to stack into an existing slot containing the same item
        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i] != null && itemSlot[i].CanStackWith(item))
            {
                itemSlot[i].AddQuantity(quantityToAdd);

                // If this slot is currently selected, refresh details panel
                if (selectedSlot == itemSlot[i] && detailsPanel != null)
                {
                    detailsPanel.Show(itemSlot[i]);
                }
                return;
            }
        }

        // 2. If it cannot be stacked, find the first empty slot
        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i] != null && !itemSlot[i].hasItem)
            {
                itemSlot[i].AddItem(item);
                return;
            }
        }

        Debug.LogWarning("Inventory is full!");
    }
}
