using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour, GrabableItem
{
    InputAction inventoryAction;
    [SerializeField] private GameObject InventoryCanvas;
    private bool InventoryActive;

    private int slotCount = 5;
    private ItemSlot[] itemSlots;

    
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        InventoryCanvas.SetActive(false);
        inventoryAction = InputSystem.actions.FindAction("Inventory");
        InventoryActive = false;

    }

    private void CreateSlots()
    {
        ItemSlot slotPrefab = Resources.Load<ItemSlot>("ItemSlot");
        if(slotPrefab == null)
        {
            Debug.LogError("ItemSlot prefab not found in Resources folder.");
            itemSlots = new ItemSlot[0];
            return;
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (inventoryAction.triggered && InventoryCanvas != null && InventoryActive == false)
        {
            InventoryActive = true;
            Time.timeScale = 0f;
            Debug.Log("Inventory is active");
            InventoryCanvas.SetActive(true);
        } else if (inventoryAction.triggered && InventoryCanvas != null && InventoryActive == true)
        {
            InventoryActive = false;
            Time.timeScale = 1f;
            Debug.Log("Inventory is deactivated");
            InventoryCanvas.SetActive(false);
        }
    }

    public void AddItem(ItemDetails item)
    {
        Debug.Log("Item added to inventory: " + item.ItemName + " Quantity: " + item.Quantity);
        // Add item to inventory logic here
    }
}
