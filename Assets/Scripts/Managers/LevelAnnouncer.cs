using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using TMPro;

public class LevelAnnouncer : MonoBehaviour
{
    public static LevelAnnouncer instance;

    [Header("UI References")]
    [Tooltip("El componente de texto de TextMeshPro donde se mostrará el anuncio.")]
    public TextMeshProUGUI announcerText;
    
    [Tooltip("El CanvasGroup que controla la transparencia (Alpha) del texto.")]
    public CanvasGroup canvasGroup;

    [Header("Settings")]
    [Tooltip("Tiempo en segundos que el texto permanece visible antes de empezar a desaparecer.")]
    public float displayDuration = 3f;
    
    [Tooltip("Tiempo en segundos que tarda el texto en desaparecer por completo.")]
    public float fadeDuration = 2f;

    private Coroutine fadeCoroutine;

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

    private void OnEnable()
    {
        //evento que se dispara cada vez que carga una escena
        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDisable()
    {
        //evitar errores si este objeto se destruye
        SceneManager.sceneLoaded -= OnSceneLoaded;
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        // Evita mostrar anuncios en el Menú Principal
        if (scene.name == "MainMenu")
        {
            if (canvasGroup != null) canvasGroup.alpha = 0f;
            return;
        }

        string locationName = "Unknown Area";

        // Determina el nombre de la ubicación según la escena
        if (scene.name == "Hub")
        {
            locationName = "Home";
        }
        else if (scene.name == "SampleScene" || scene.name.StartsWith("Day_"))
        {
            locationName = "Battlefield";
        }

        // Obtiene el día actual del GameManager
        int currentDay = 1;
        if (GameManager.instance != null)
        {
            currentDay = GameManager.instance.currentDay;
        }

        
        string finalMessage = $"{locationName} - Day {currentDay}";

      
        ShowAnnouncement(finalMessage);
    }

    public void ShowAnnouncement(string message)
    {
        if (announcerText == null || canvasGroup == null)
        {
            Debug.LogWarning("LevelAnnouncer: Faltan referencias al TextMeshPro o CanvasGroup.");
            return;
        }

        
        if (fadeCoroutine != null)
        {
            StopCoroutine(fadeCoroutine);
        }

        
        fadeCoroutine = StartCoroutine(FadeOutRoutine(message));
    }

    private IEnumerator FadeOutRoutine(string message)
    {
        
        announcerText.text = message;
        canvasGroup.alpha = 1f;

        
        yield return new WaitForSeconds(displayDuration);

       
        float elapsedTime = 0f;
        while (elapsedTime < fadeDuration)
        {
            elapsedTime += Time.deltaTime;
            
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
            yield return null; 
        }

        canvasGroup.alpha = 0f;
    }
}
