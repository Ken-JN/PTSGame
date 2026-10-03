using UnityEngine;


public class Item : MonoBehaviour, GrabableItem
{

    itemDetails itemDetails;
    public void GrabItem()
    {
        Debug.Log("Item grabbed!");
        Destroy(gameObject); // Implementation for grabbing the item
    }

    private void OnColliderEnter2D(Collider2D other)
    {
        if (other.CompareTag("Player"))
        {
            Debug.Log("Item grabbed!");
            GrabItem();
            // Optional: Destroy the item after grabbing
        }
    }

}
