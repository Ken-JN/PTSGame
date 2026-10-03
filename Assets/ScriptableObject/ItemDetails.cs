using UnityEngine;

[CreateAssetMenu(fileName = "ItemDetails", menuName = "Scriptable Objects/ItemDetails")]
public class ItemDetails : ScriptableObject
{
    public string itemName;
    public int quantity;
    public Sprite itemIcon;
    public string itemDescription;
}