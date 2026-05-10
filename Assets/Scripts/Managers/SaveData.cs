using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int currentDay = 1;
    public bool hasDeployedToday = false;
    
    // Cuota de dinero
    public float currentMoney = 0f;
    public float requiredMoneyQuota = 10000f;
    
    // Futuras expansiones para la Beta
    public float totalViews = 0f;
    public List<string> unlockedPOIs = new List<string>();
    
    // Inventario y equipamiento
    public List<string> hubInventory = new List<string>();
    public string[] equippedItems = new string[3] { "", "", "" };
}
