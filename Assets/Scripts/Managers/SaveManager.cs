using UnityEngine;
using System.IO;

public static class SaveManager
{
    private static string SavePath => Application.persistentDataPath + "/savefile.json";

    public static void SaveGame(SaveData data)
    {
        string json = JsonUtility.ToJson(data, true); // true para que se formatee bonito en múltiples líneas
        File.WriteAllText(SavePath, json);
        Debug.Log("Partida guardada en: " + SavePath);
    }

    public static SaveData LoadGame()
    {
        if (File.Exists(SavePath))
        {
            string json = File.ReadAllText(SavePath);
            SaveData data = JsonUtility.FromJson<SaveData>(json);
            Debug.Log("Partida cargada correctamente.");
            return data;
        }
        else
        {
            Debug.Log("No se encontró archivo de guardado. Se creará una partida nueva.");
            return new SaveData(); // Devuelve los valores por defecto (Día 1, etc.)
        }
    }

    public static bool HasSaveFile()
    {
        return File.Exists(SavePath);
    }

    public static void DeleteSave()
    {
        if (File.Exists(SavePath))
        {
            File.Delete(SavePath);
            Debug.Log("Partida borrada con éxito.");
        }
    }
}
