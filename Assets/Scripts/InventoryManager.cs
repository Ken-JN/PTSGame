using UnityEngine;
using UnityEngine.InputSystem;

public class InventoryManager : MonoBehaviour
{
    private InputAction inventoryAction;

    public GameObject inventoryUI;
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
            inventoryUI.SetActive(!inventoryUI.activeSelf);
            Debug.Log("Inventory button pressed");
        }
    }
}
