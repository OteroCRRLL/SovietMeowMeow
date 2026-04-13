using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Level Settings")]
    public Transform playerSpawnPoint;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        // Al empezar el nivel, instanciar al jugador.
        if (GameManager.instance != null && playerSpawnPoint != null)
        {
            GameManager.instance.SpawnPlayer(playerSpawnPoint);
        }
        else
        {
            Debug.LogWarning("LevelManager: Faltan referencias para spawnear al jugador.");
        }
    }

    /// <summary>
    /// Llamado cuando el jugador interactúa con el ExtractionPoint
    /// </summary>
    public void ExtractPlayer()
    {
        if (GameManager.instance != null && SceneController.instance != null)
        {
            // Opcional: Detener la grabación de replays o mostrar interfaz de resumen antes de cargar.
            // Por ahora, se completa el día inmediatamente y se vuelve al Hub.
            GameManager.instance.CompleteDay();
            
            Debug.Log("Extraction complete. Returning to Hub.");
            SceneController.instance.LoadScene("Hub"); // Cambiar al nombre exacto de la escena del Hub
        }
        else
        {
            Debug.LogError("LevelManager: No se puede extraer al jugador, faltan referencias globales.");
        }
    }
}
