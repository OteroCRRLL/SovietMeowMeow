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
            if (ReplayManager.instance != null) ReplayManager.instance.StopRecording();
            if (CameraScoring.instance != null) 
            {
                int viewsObtained = CameraScoring.instance.GetCurrentScore();
                float moneyEarned = viewsObtained * 0.5f;
                GameManager.instance.currentMoney += moneyEarned;
                Debug.Log($"Extraction: {viewsObtained} views converted to ${moneyEarned}. Total money: ${GameManager.instance.currentMoney}");
                CameraScoring.instance.ShowFinalScore();
            }
           
            GameManager.instance.hasDeployedToday = true; // El jugador ya hizo su misión de hoy
            
            Debug.Log("Extraction complete. Returning to Hub.");
            SceneController.instance.LoadScene("Hub"); // Cambiar al nombre exacto de la escena del Hub
        }
        else
        {
            Debug.LogError("LevelManager: No se puede extraer al jugador, faltan referencias globales.");
        }
    }
}
