using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
///     INVENTORY MANAGER
///     - Toggles the Inventory panel when pressing "I"
///     - Stores items collected by the player
///     - Displays item details when a slot is clicked
/// </summary>
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
                // Open Inventory
                InventoryActive = true;
                Time.timeScale = 0f; // Pause game while inventory is open
                Debug.Log("<color=cyan>[INVENTORY]</color> Inventory Opened!");
                InventoryCanvas.SetActive(true);
            }
            else
            {
                // Close Inventory
                InventoryActive = false;
                Time.timeScale = 1f; // Resume game
                Debug.Log("<color=cyan>[INVENTORY]</color> Inventory Closed!");
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
            detailsPanel.Show(slot.ItemDetails);
        }
    }

    // Adds a collected item to an available empty slot
    public void AddItem(ItemDetails item)
    {
        Debug.Log($"<color=green>[ITEM OBTAINED]</color> {item.ItemName} added to inventory!");

        if (itemSlot == null || itemSlot.Length == 0)
        {
            Debug.LogWarning("Inventory has no slots configured!");
            return;
        }

        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (!itemSlot[i].isFull)
            {
                itemSlot[i].AddItem(item);
                return;
            }
        }

        Debug.LogWarning("Inventory is full!");
    }
}
