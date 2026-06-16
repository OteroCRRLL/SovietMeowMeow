using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public static LevelManager instance;

    [Header("Level Settings")]
    [Tooltip("Lista de posibles puntos de spawn para el jugador.")]
    public Transform[] playerSpawnPoints;

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
        // Seleccionar un spawn random
        Transform selectedSpawn = null;
        if (playerSpawnPoints != null && playerSpawnPoints.Length > 0)
        {
            int randomIndex = UnityEngine.Random.Range(0, playerSpawnPoints.Length);
            selectedSpawn = playerSpawnPoints[randomIndex];
            Debug.Log($"LevelManager: Seleccionado spawn random {randomIndex} de {playerSpawnPoints.Length}.");
        }
        else
        {
            Debug.LogWarning("LevelManager: La lista de playerSpawnPoints está vacía o es null.");
        }

        // Al empezar el nivel, instanciar al jugador.
        if (GameManager.instance != null && selectedSpawn != null)
        {
            GameManager.instance.SpawnPlayer(selectedSpawn);
        }
        else
        {
            Debug.LogWarning("LevelManager: Faltan referencias para spawnear al jugador (GameManager o spawn seleccionado).");
        }
    }

    /// <summary>
    /// Llamado cuando el jugador interactúa con el ExtractionPoint
    /// </summary>
    public void ExtractPlayer()
    {
        if (GameManager.instance != null && SceneController.instance != null)
        {
            if (ReplayManager.instance != null)
            {
                ReplayManager.instance.StopRecording();
                ReplayManager.instance.ArchiveMissionForDay(GameManager.instance.currentDay);
                ReplayManager.instance.recordedSessions.Clear();
            }
            if (CameraScoring.instance != null) 
            {
                int viewsObtained = CameraScoring.instance.GetCurrentScore();
                float moneyEarned = viewsObtained * 0.5f;
                GameManager.instance.currentMoney += moneyEarned;
                Debug.Log($"Extraction: {viewsObtained} views converted to ${moneyEarned}. Total money: ${GameManager.instance.currentMoney}");
                CameraScoring.instance.ShowFinalScore();
            }
           
            GameManager.instance.hasDeployedToday = true; // El jugador ya hizo su misión de hoy
            GameManager.instance.SaveGame();
            
            Debug.Log("Extraction complete. Returning to Hub.");
            SceneController.instance.LoadScene("Hub"); // Cambiar al nombre exacto de la escena del Hub
        }
        else
        {
            Debug.LogError("LevelManager: No se puede extraer al jugador, faltan referencias globales.");
        }
    }
}
