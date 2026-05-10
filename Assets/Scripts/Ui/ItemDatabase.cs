using UnityEngine;
using System.Collections.Generic;

[CreateAssetMenu(fileName = "ItemDatabase", menuName = "Shop/Item Database")]
public class ItemDatabase : ScriptableObject
{
    public List<ItemData> allItems = new List<ItemData>();

    public ItemData GetItemByID(string id)
    {
        foreach (var item in allItems)
        {
            if (item.itemID == id)
                return item;
        }
        return null;
    }
}