using UnityEngine;

public class HubManager : MonoBehaviour
{
    public static HubManager instance;

    [Header("Spawn Settings")]
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
        if (GameManager.instance != null && playerSpawnPoint != null)
        {
            GameManager.instance.SpawnPlayer(playerSpawnPoint);
        }
        else
        {
            Debug.LogWarning("HubManager: Faltan referencias para spawnear al jugador (GameManager o SpawnPoint).");
        }

        // Cartel de introducción tras pulsar "Nueva Partida"
        if (GameManager.instance != null && GameManager.instance.pendingIntroScreen)
        {
            GameManager.instance.pendingIntroScreen = false;
            if (IntroScreenManager.instance != null)
            {
                IntroScreenManager.instance.Show();
            }
        }
    }

    /// <summary>
    /// Llamado desde la Puerta de Misión o Terminal para desplegar al jugador.
    /// </summary>
    public void DeployToDay()
    {
        if (GameManager.instance != null && SceneController.instance != null)
        {
            if (ReplayManager.instance != null)
            {
                ReplayManager.instance.StopRecording();
                ReplayManager.instance.ResetCapacity();
            }
            if (CameraScoring.instance != null) CameraScoring.instance.ResetScore();
            
            string nextScene = "Blockout"; 
            
            Debug.Log("Deploying to " + nextScene + "... Day: " + GameManager.instance.currentDay);
            SceneController.instance.LoadScene(nextScene);
        }
        else
        {
            Debug.LogError("HubManager: No se puede desplegar, faltan GameManager o SceneController.");
        }
    }
}
