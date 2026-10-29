using UnityEngine;

/// <summary>
///     Skrip yang dipasang pada objek collectible di arena game.
///     Saat disentuh oleh Player, item akan masuk ke Inventory dan
///     jika item tersebut adalah Kunci Emas atau Kunci KW, akan menambah progres kemenangan di GameManager.
/// </summary>
public class Item : MonoBehaviour
{
    [Header("Pengaturan Item")]
    [Tooltip("Data item ScriptableObject (misal Koentji Emas atau Koentji KW)")]
    [SerializeField] private ItemDetails ItemDetails;
    private GrabableItem itemCollector;

    private void Awake()
    {
        CariInventoryCollector();
    }

    private void CariInventoryCollector()
    {
        // Cari komponen InventoryManager di scene yang mengimplementasikan GrabableItem
        foreach (MonoBehaviour component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (component is GrabableItem collector)
            {
                itemCollector = collector;
                break;
            }
        }
    }

    private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            if (ItemDetails != null)
            {
                string namaItem = ItemDetails.ItemName.ToLower();

                // 1. Cek apakah ini Kunci KW
                if (namaItem.Contains("kw") || namaItem.Contains("kawe") || CompareTag("FakeKey"))
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.KumpulkanKunciKw();
                    }
                }
                // 2. Cek apakah ini Kunci Emas
                else if (namaItem.Contains("emas") || namaItem.Contains("gold") || CompareTag("GoldKey"))
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.KumpulkanKunciEmas();
                    }
                }
                // 3. Item lainnya (tambah poin umum)
                else
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.TambahPoin(25);
                    }
                }

                // 4. Masukkan ke Inventory
                if (itemCollector == null)
                {
                    CariInventoryCollector();
                }

                if (itemCollector != null)
                {
                    itemCollector.AddItem(ItemDetails);
                }
            }

            // Hancurkan objek di arena setelah diambil
            Destroy(gameObject);
        }
    }
}
