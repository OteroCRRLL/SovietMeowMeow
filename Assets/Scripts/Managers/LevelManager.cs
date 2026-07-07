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

        PositionExtractionPoint();
    }

    /// <summary>
    /// Mueve el ExtractionPoint ya colocado en la escena a uno de los ExtractionSpawnerPoint
    /// disponibles, elegido al azar. Puede haber cualquier cantidad de ExtractionSpawnerPoint
    /// en Unity; no hace falta arrastrar nada a ningún array, se detectan solos.
    /// </summary>
    private void PositionExtractionPoint()
    {
        ExtractionSpawnerPoint[] points = FindObjectsOfType<ExtractionSpawnerPoint>();
        if (points.Length == 0)
        {
            Debug.LogWarning("LevelManager: No hay ningún ExtractionSpawnerPoint en la escena, el punto de extracción se queda donde esté colocado.");
            return;
        }

        ExtractionPoint extractionPoint = FindObjectOfType<ExtractionPoint>();
        if (extractionPoint == null)
        {
            Debug.LogWarning("LevelManager: No se encontró ningún ExtractionPoint en la escena para reposicionar.");
            return;
        }

        ExtractionSpawnerPoint selected = points[UnityEngine.Random.Range(0, points.Length)];
        extractionPoint.transform.SetPositionAndRotation(selected.transform.position, selected.transform.rotation);

        Debug.Log($"LevelManager: Punto de extracción movido a '{selected.gameObject.name}'.");
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

            int baseScore = 0;
            int bonusScore = 0;
            int viewsObtained = 0;
            float moneyEarned = 0f;

            if (CameraScoring.instance != null)
            {
                baseScore = CameraScoring.instance.GetBaseScore();
                bonusScore = CameraScoring.instance.GetCombatBonusScore();
                viewsObtained = CameraScoring.instance.GetCurrentScore();
                moneyEarned = viewsObtained * 0.5f;
                GameManager.instance.currentMoney += moneyEarned;
                Debug.Log($"Extraction: {viewsObtained} views converted to ${moneyEarned}. Total money: ${GameManager.instance.currentMoney}");
                CameraScoring.instance.ShowFinalScore();
            }

            GameManager.instance.hasDeployedToday = true; // El jugador ya hizo su misión de hoy
            GameManager.instance.SaveGame();

            System.Action goToHub = () =>
            {
                Debug.Log("Extraction complete. Returning to Hub.");
                SceneController.instance.LoadScene("Hub"); // Cambiar al nombre exacto de la escena del Hub
            };

            if (ExtractionSummaryScreenManager.instance != null)
            {
                ExtractionSummaryScreenManager.instance.Show(baseScore, bonusScore, viewsObtained, moneyEarned, goToHub);
            }
            else
            {
                goToHub();
            }
        }
        else
        {
            Debug.LogError("LevelManager: No se puede extraer al jugador, faltan referencias globales.");
        }
    }
}
