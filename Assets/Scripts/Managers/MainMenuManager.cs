using UnityEngine;

/// <summary>
/// Script específico para la escena del Menú Principal.
/// Su función es comunicarse con el SceneController (que es un Singleton)
/// sin romperse cuando la escena se recarga.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("El nombre exacto de la escena del Hub (o primera escena) que se va a cargar al pulsar Play.")]
    public string firstSceneName = "Hub";

    private void Start()
    {
        // Asegurar que el ratón esté visible y desbloqueado en el menú principal
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
    }

    /// <summary>
    /// Función para el botón de Nueva Partida.
    /// Borra el guardado anterior y resetea el progreso.
    /// </summary>
    public void NewGame()
    {
        SaveManager.DeleteSave();
        
        if (GameManager.instance != null)
        {
            GameManager.instance.ResetProgress();
        }
        
        if (SceneController.instance != null)
        {
            SceneController.instance.LoadScene(firstSceneName);
        }
        else
        {
            Debug.LogError("No se encontró el SceneController en la escena. Asegúrate de tener el prefab.");
        }
    }

    /// <summary>
    /// Función para el botón de Continuar Partida.
    /// Lee el archivo JSON y carga los datos.
    /// </summary>
    public void ContinueGame()
    {
        if (GameManager.instance != null)
        {
            GameManager.instance.LoadGame();
        }
        
        if (SceneController.instance != null)
        {
            SceneController.instance.LoadScene(firstSceneName);
        }
        else
        {
            Debug.LogError("No se encontró el SceneController en la escena. Asegúrate de tener el prefab.");
        }
    }

    /// <summary>
    /// Función para el botón de Salir del menú principal.
    /// </summary>
    public void QuitGame()
    {
        if (SceneController.instance != null)
        {
            SceneController.instance.QuitGame();
        }
        else
        {
            Debug.LogError("No se encontró el SceneController en la escena. Asegúrate de tener el prefab.");
        }
    }
}
