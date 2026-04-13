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

    private void OnEnable()
    {
        pauseInput.Enable();
    }

    private void OnDisable()
    {
        pauseInput.Disable();
    }

    void Update()
    {
        if (pauseInput.triggered)
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
