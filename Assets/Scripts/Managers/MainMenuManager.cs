using UnityEngine;

/// <summary>
/// Script específico para la escena del Menú Principal.
/// Su función es comunicarse con el SceneController (que es un Singleton)
/// sin romperse cuando la escena se recarga.
/// </summary>
public class MainMenuManager : MonoBehaviour
{
    [Header("Configuración de Escenas")]
    [Tooltip("El nombre exacto de la escena del juego que se va a cargar al pulsar Play.")]
    public string gameSceneName = "GameScene";

    /// <summary>
    /// Función para el botón de Play del menú principal.
    /// Llama al Singleton del SceneController que siempre sobrevive.
    /// </summary>
    public void PlayGame()
    {
        if (SceneController.instance != null)
        {
            SceneController.instance.LoadScene(gameSceneName);
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
