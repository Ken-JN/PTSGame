using UnityEngine;

/// <summary>
///     Attached to collectible item objects in the arena.
///     When touched by the Player, the item is added to the Inventory and
///     if it is a Gold Key or Fake Key, progresses the victory condition in GameManager.
/// </summary>
public class Item : MonoBehaviour
{
    [Header("Item Settings")]
    [Tooltip("Item ScriptableObject data (e.g. Gold Key or Fake Key)")]
    [SerializeField] private ItemDetails ItemDetails;
    private GrabableItem itemCollector;

    private void Awake()
    {
        FindInventoryCollector();
    }

    private void FindInventoryCollector()
    {
        // Find InventoryManager component in the scene that implements GrabableItem
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
                string itemName = ItemDetails.ItemName.ToLower();

                // 1. Check if Fake / Silver Key
                if (itemName.Contains("kw") || itemName.Contains("kawe") || itemName.Contains("fake") || CompareTag("FakeKey"))
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CollectFakeKey();
                    }
                }
                // 2. Check if Gold Key
                else if (itemName.Contains("emas") || itemName.Contains("gold") || CompareTag("GoldKey"))
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.CollectGoldKey();
                    }
                }
                // 3. Other items (general point reward)
                else
                {
                    if (GameManager.Instance != null)
                    {
                        GameManager.Instance.AddPoints(25);
                    }
                }

                // 4. Add to Inventory
                if (itemCollector == null)
                {
                    FindInventoryCollector();
                }

                if (itemCollector != null)
                {
                    itemCollector.AddItem(ItemDetails);
                }
            }

            // Destroy collectible object in the arena upon pickup
            Destroy(gameObject);
        }
    }
}
