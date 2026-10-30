using UnityEngine;

public class Item : MonoBehaviour
{
    [SerializeField] private ItemDetails ItemDetails;
    [SerializeField] private AudioClip pickupSound;

    private GrabableItem itemCollector;

    private void Awake()
    {
        FindInventoryCollector();

        if (pickupSound == null)
        {
            pickupSound = Resources.Load<AudioClip>("Sounds/Pickup");
        }
    }

    private void FindInventoryCollector()
    {
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
        if (!other.CompareTag("Player")) return;

        if (ItemDetails != null)
        {
            string itemName = ItemDetails.ItemName.ToLower();

            // 1. Silver Key
            if (itemName.Contains("silver") || CompareTag("SilverKey"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CollectSilverKey();
                }
                else if (pickupSound != null)
                {
                    TopDownCharacter2D.FX.SoundManager.PlaySoundEffect(pickupSound);
                }
            }
            // 2. Gold Key
            else if (itemName.Contains("gold") || CompareTag("GoldKey"))
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.CollectGoldKey();
                }
                else if (pickupSound != null)
                {
                    TopDownCharacter2D.FX.SoundManager.PlaySoundEffect(pickupSound);
                }
            }
            // 3. Other items
            else
            {
                if (GameManager.Instance != null)
                {
                    GameManager.Instance.AddPoints(25);
                }
                if (pickupSound != null)
                {
                    TopDownCharacter2D.FX.SoundManager.PlaySoundEffect(pickupSound);
                }
            }

            // 4. Add to player inventory
            if (itemCollector == null)
            {
                FindInventoryCollector();
            }

            if (itemCollector != null)
            {
                itemCollector.AddItem(ItemDetails);
            }
        }

        // Destroy item object in arena after pickup
        Destroy(gameObject);
    }
}
