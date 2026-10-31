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
                InventoryActive = false;
                Time.timeScale = 1f;
                InventoryCanvas.SetActive(false);
            }
        }
    }

    private void CreateSlots()
    {
        if (InventoryCanvas == null) return;

        ItemSlot slotPrefab = Resources.Load<ItemSlot>("ItemSlot");
        if (slotPrefab == null)
        {
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

    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        selectedSlot = slot;
        slot.SetSelected(true);

        if (detailsPanel != null)
        {
            detailsPanel.Show(slot);
        }
    }

    public void AddItem(ItemDetails item)
    {
        if (itemSlot == null || itemSlot.Length == 0) return;

        int quantityToAdd = item.Quantity > 0 ? item.Quantity : 1;

        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i] != null && itemSlot[i].CanStackWith(item))
            {
                itemSlot[i].AddQuantity(quantityToAdd);

                if (selectedSlot == itemSlot[i] && detailsPanel != null)
                {
                    detailsPanel.Show(itemSlot[i]);
                }
                return;
            }
        }

        for (int i = 0; i < itemSlot.Length; i++)
        {
            if (itemSlot[i] != null && !itemSlot[i].hasItem)
            {
                itemSlot[i].AddItem(item);
                return;
            }
        }
    }
}
