using System.Collections.Generic;

[System.Serializable]
public class SaveData
{
    public int currentDay = 1;
    public bool hasDeployedToday = false;
    
    // Futuras expansiones para la Beta
    public float totalViews = 0f;
    public List<string> unlockedPOIs = new List<string>();
    public List<string> hubInventory = new List<string>();
}
