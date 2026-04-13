using UnityEngine;
using UnityEngine.InputSystem;

public class GameManager : MonoBehaviour
{
    public static GameManager instance;

    [Header("UI Referencias")]
    public GameObject pauseMenuUI; 

    [Header("Input Actions")]
    public InputAction pauseInput;

    private bool isPaused = false;

    [Header("Game State")]
    public int currentDay = 1;

    [Header("Player Instantiation")]
    public GameObject playerPrefab;
    private GameObject currentPlayerInstance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
    }

    /// <summary>
    /// Instancia al jugador en la posición proporcionada por el manager local (HubManager o LevelManager).
    /// </summary>
    public void SpawnPlayer(Transform spawnPoint)
    {
        if (playerPrefab == null)
        {
            Debug.LogError("Player Prefab is missing in GameManager!");
            return;
        }

        
        if (currentPlayerInstance != null)
        {
            Destroy(currentPlayerInstance);
        }

        currentPlayerInstance = Instantiate(playerPrefab, spawnPoint.position, spawnPoint.rotation);
        
        // Buscar el panel de pausa dentro del jugador instanciado
        Transform[] allChildren = currentPlayerInstance.GetComponentsInChildren<Transform>(true);
        foreach (Transform child in allChildren)
        {
            if (child.name == "PausePanel")
            {
                pauseMenuUI = child.gameObject;
                pauseMenuUI.SetActive(false); // Nos aseguramos de que empiece oculto
                break;
            }
        }
    }

    /// <summary>
    /// Llamado cuando el jugador extrae con éxito de un nivel.
    /// </summary>
    public void CompleteDay()
    {
        currentDay++;
        Debug.Log("Day Completed! New Day: " + currentDay);
        // Guardar progreso u otras lógicas
    }

    /// <summary>
    /// Llamado cuando el jugador muere o falla el nivel.
    /// </summary>
    public void FailDay()
    {
        Debug.Log("Day Failed! Resetting progress...");
        ResetProgress();
    }

    /// <summary>
    /// Reinicia el progreso al inicio del juego.
    /// </summary>
    public void ResetProgress()
    {
        currentDay = 1;
        // Limpiar inventario u otros datos aquí si los hubiera
    }

    private void OnEnable()
    {
        if (pauseInput != null) pauseInput.Enable();
    }

    private void OnDisable()
    {
        if (pauseInput != null) pauseInput.Disable();
    }

    void Update()
    {
        if (pauseInput != null && pauseInput.triggered)
        {
            if (isPaused)
            {
                ResumeGame();
            }
            else
            {
                PauseGame();
            }
        }
    }

    /// <summary>
    /// Pausa el juego por completo.
    /// </summary>
    public void PauseGame()
    {
        isPaused = true;
        Time.timeScale = 0f; // Detiene el tiempo
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(true); // Muestra el menú de pausa
        }

        // Habilitar el cursor para poder usar los botones del UI
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Reanuda el juego
    /// </summary>
    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f; // Restaura el tiempo normal
        
        if (pauseMenuUI != null)
        {
            pauseMenuUI.SetActive(false); // Oculta el menú de pausa
        }

        // Ocultar y bloquear el cursor de nuevo para el juego
        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;
    }

    /// <summary>
    /// Vuelve al menú principal, restaurando el tiempo a la normalidad.
    /// </summary>
    public void ReturnToMainMenu()
    {
        isPaused = false;
        Time.timeScale = 1f; // Restaurar el tiempo antes de cambiar de escena
        
        if (SceneController.instance != null)
        {
            SceneController.instance.LoadScene("MainMenu");
        }
        else
        {
            Debug.LogError("SceneController.instance es nulo. Asegúrate de tener el prefab del SceneController en la escena.");
        }
    }
}
