using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    private InputAction inventoryAction;

    [SerializeField] private GameObject inventoryCanvas;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        inventoryAction = InputSystem.actions.FindAction("Inventory");
    }

    // Update is called once per frame
    void Update()
    {
        if (inventoryAction.triggered)
        {
            inventoryCanvas.SetActive(!inventoryCanvas.activeSelf);
            Debug.Log("Inventory button pressed");

            Time.timeScale = inventoryCanvas.activeSelf ? 0f : 1f; 
        }
    }
}
