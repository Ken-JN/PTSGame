using UnityEngine;


public class Item : MonoBehaviour
{

    [SerializeField] private ItemDetails ItemDetails;
    private GrabableItem ItemCollector;
       void Awake()
    {
        foreach (MonoBehaviour component in FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None))
        {
            if (component is GrabableItem collector)
            {
                ItemCollector = collector;
                break;
            }
        }

        if (ItemCollector == null)
        {
            Debug.LogError("No active component implementing GrabableItem was found in the scene.", this);
        }
    }

 private void OnTriggerEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player") && ItemDetails != null && ItemCollector != null)
        {
            ItemCollector.AddItem(ItemDetails);
            Destroy(gameObject);
        }
    }

}
