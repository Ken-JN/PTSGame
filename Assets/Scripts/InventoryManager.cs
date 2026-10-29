using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.UI;

/// <summary>
///     SKRIP PENGELOLA INVENTORY (INVENTORY MANAGER)
///     - Membuka dan menutup panel Inventory saat menekan tombol "I"
///     - Menyimpan item yang diambil oleh player
///     - Menampilkan detail item saat slot diklik
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
        // Deteksi apakah tombol I ditekan atau action Inventory dipicu
        bool ditekan = false;
        if (inventoryAction != null && inventoryAction.triggered)
        {
            ditekan = true;
        }
        else if (Keyboard.current != null && Keyboard.current.iKey.wasPressedThisFrame)
        {
            ditekan = true;
        }

        if (ditekan && InventoryCanvas != null)
        {
            if (!InventoryActive)
            {
                // Buka Inventory
                InventoryActive = true;
                Time.timeScale = 0f; // Pause game saat inventory terbuka
                Debug.Log("<color=cyan>[INVENTORY]</color> Inventory Dibuka!");
                InventoryCanvas.SetActive(true);
            }
            else
            {
                // Tutup Inventory
                InventoryActive = false;
                Time.timeScale = 1f; // Lanjutkan game
                Debug.Log("<color=cyan>[INVENTORY]</color> Inventory Ditutup!");
                InventoryCanvas.SetActive(false);
            }
        }
    }

    // Membuat slot secara otomatis di dalam grid layout
    private void CreateSlots()
    {
        if (InventoryCanvas == null) return;

        ItemSlot slotPrefab = Resources.Load<ItemSlot>("ItemSlot");
        if (slotPrefab == null)
        {
            Debug.LogError("Prefab ItemSlot tidak ditemukan di folder Resources.", this);
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

    // Dipanggil saat pemain mengklik salah satu slot item
    public void SelectSlot(ItemSlot slot)
    {
        if (selectedSlot != null)
        {
            selectedSlot.SetSelected(false);
        }

        selectedSlot = slot;
        slot.SetSelected(true);

        // Tampilkan detail item di panel kanan
        if (detailsPanel != null)
        {
            detailsPanel.Show(slot.ItemDetails);
        }
    }

    // Menambahkan item yang diambil ke slot yang masih kosong
    public void AddItem(ItemDetails item)
    {
        Debug.Log($"<color=green>[ITEM DIKUASAI]</color> {item.ItemName} masuk ke inventory!");

        if (itemSlot == null || itemSlot.Length == 0)
        {
            Debug.LogWarning("Inventory tidak memiliki slot!");
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

        Debug.LogWarning("Inventory penuh!");
    }
}
