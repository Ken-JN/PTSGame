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

            if (itemCollector == null)
            {
                FindInventoryCollector();
            }

            if (itemCollector != null)
            {
                itemCollector.AddItem(ItemDetails);
            }
        }

        Destroy(gameObject);
    }
}
