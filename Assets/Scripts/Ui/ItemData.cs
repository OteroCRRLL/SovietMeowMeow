using UnityEngine;

[CreateAssetMenu(fileName = "New Item", menuName = "Shop/Item Data")]
public class ItemData : ScriptableObject
{
    public string itemID;
    public string itemName;
    [TextArea(2, 4)]
    public string description;
    public float price;
    public Sprite uiIcon;
    public GameObject itemPrefab; // El modelo 3D con su script de comportamiento
}