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
        // Al empezar la escena del Hub, pedir al GameManager que instancie al jugador en el SpawnPoint.
        if (GameManager.instance != null && playerSpawnPoint != null)
        {
            GameManager.instance.SpawnPlayer(playerSpawnPoint);
        }
        else
        {
            Debug.LogWarning("HubManager: Faltan referencias para spawnear al jugador (GameManager o SpawnPoint).");
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
