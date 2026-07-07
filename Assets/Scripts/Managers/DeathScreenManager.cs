using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.SceneManagement;
using TMPro;

public class DeathScreenManager : MonoBehaviour
{
    public static DeathScreenManager instance;

    [Header("UI References")]
    [Tooltip("El CanvasGroup para hacer el fade (debe tener un panel negro y el texto como hijos).")]
    public CanvasGroup deathCanvasGroup;
    
    [Tooltip("El texto donde aparecerá el mensaje de muerte.")]
    public TextMeshProUGUI deathText;

    [Header("Settings")]
    [Tooltip("Tiempo en segundos que tarda en ponerse la pantalla negra.")]
    public float fadeToBlackDuration = 2f;
    [Tooltip("Tiempo en segundos que la pantalla se queda negra mostrando el texto.")]
    public float displayDuration = 3f;
    [Tooltip("Tiempo en segundos que tarda en volver la visión al cargar el menú principal.")]
    public float fadeToClearDuration = 1.5f;

    [TextArea]
    public string defaultDeathMessage = "YOU DIED\n\nProgress lost.";

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            // Vive dentro de _SYSTEMS, así que no hace falta DontDestroyOnLoad aquí.
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (deathCanvasGroup != null)
        {
            deathCanvasGroup.alpha = 0f;
            deathCanvasGroup.interactable = false;
            deathCanvasGroup.blocksRaycasts = false;
        }
    }

    /// <summary>
    /// Se invoca desde el evento OnDeath() del HealthSystem del jugador.
    /// </summary>
    public void ShowDeathScreen()
    {
        if (deathCanvasGroup == null)
        {
            Debug.LogError("DeathScreenManager: No se asignó el CanvasGroup de la pantalla de muerte.");
            // Si falla la UI, se fuerza el reinicio como mínimo
            ForceRestart();
            return;
        }

        StartCoroutine(DeathSequenceRoutine(defaultDeathMessage));
    }

    public void ShowDeathScreen(string customMessage)
    {
        if (deathCanvasGroup == null)
        {
            ForceRestart();
            return;
        }
        StartCoroutine(DeathSequenceRoutine(customMessage));
    }

    private IEnumerator DeathSequenceRoutine(string message)
    {
        Time.timeScale = 0f;

        if (deathText != null)
        {
            deathText.text = message;
        }

        deathCanvasGroup.blocksRaycasts = true;

        // Fase 1: Fade in al negro
        float elapsed = 0f;
        while (elapsed < fadeToBlackDuration)
        {
            // unscaledDeltaTime porque Time.timeScale está pausado
            elapsed += Time.unscaledDeltaTime;
            deathCanvasGroup.alpha = Mathf.Lerp(0f, 1f, elapsed / fadeToBlackDuration);
            yield return null;
        }
        deathCanvasGroup.alpha = 1f;

        // Fase 2: Mostrar el mensaje y resetear el progreso del juego en silencio
        string sceneToLoad = "MainMenu";
        if (GameManager.instance != null)
        {
            bool survived = GameManager.instance.FailDay(); // Esto avanza el día o resetea el progreso
            if (survived)
            {
                sceneToLoad = "Hub";
            }
            else
            {
                if (deathText != null && !deathText.text.Contains("FIRED"))
                {
                    deathText.text += "\n\nYOU WERE FIRED (Quota failed)";
                }
            }
        }
        
        yield return new WaitForSecondsRealtime(displayDuration);

        // Liberar el ratón antes de cambiar de escena, por si el objeto se destruye
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        // Fase 3: Cargar la escena correspondiente (Hub o MainMenu)
        if (SceneController.instance != null)
        {
            SceneController.instance.LoadScene(sceneToLoad);
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }

        Time.timeScale = 1f;

        // Fase 4: Fade out del negro para volver a ver el menú principal
        elapsed = 0f;
        while (elapsed < fadeToClearDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            deathCanvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsed / fadeToClearDuration);
            yield return null;
        }

        deathCanvasGroup.alpha = 0f;
        deathCanvasGroup.blocksRaycasts = false;
    }

    private void ForceRestart()
    {
        string sceneToLoad = "MainMenu";
        if (GameManager.instance != null)
        {
            bool survived = GameManager.instance.FailDay();
            if (survived) sceneToLoad = "Hub";
        }
        
        Time.timeScale = 1f;
        
        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;
        
        if (SceneController.instance != null)
        {
            SceneController.instance.LoadScene(sceneToLoad);
        }
        else
        {
            SceneManager.LoadScene(sceneToLoad);
        }
    }
}
