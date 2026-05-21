using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;

    [Header("Audio")]
    public AudioSource catMeowAudioSource;
    public AudioClip catMeowClip;
    public float catMeowDelay = 1f;

    private Coroutine catMeowCoroutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null); // Asegurar que sea root para DontDestroyOnLoad
            DontDestroyOnLoad(gameObject); // Permite que el manager persista entre escenas
            if (catMeowAudioSource == null) catMeowAudioSource = gameObject.AddComponent<AudioSource>();
            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            if (catMeowClip != null)
            {
                instance.catMeowClip = catMeowClip;
                instance.catMeowDelay = catMeowDelay;
                if (instance.catMeowAudioSource == null) instance.catMeowAudioSource = instance.gameObject.AddComponent<AudioSource>();
                instance.QueueCatMeowIfNeeded(SceneManager.GetActiveScene().name);
            }

            Destroy(gameObject);
        }
    }

    private void Start()
    {
        QueueCatMeowIfNeeded(SceneManager.GetActiveScene().name);
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        QueueCatMeowIfNeeded(scene.name);
    }

    private void QueueCatMeowIfNeeded(string sceneName)
    {
        if (sceneName != "Hub" && sceneName != "Blockout") return;
        if (catMeowClip == null || catMeowAudioSource == null) return;

        if (catMeowCoroutine != null)
        {
            StopCoroutine(catMeowCoroutine);
        }

        catMeowCoroutine = StartCoroutine(PlayCatMeowAfterDelay());
    }

    private IEnumerator PlayCatMeowAfterDelay()
    {
        yield return new WaitForSeconds(catMeowDelay);

        if (catMeowAudioSource != null && catMeowClip != null)
        {
            catMeowAudioSource.PlayOneShot(catMeowClip);
        }

        catMeowCoroutine = null;
    }

    /// <summary>
    /// Carga una escena directamente por su nombre de forma sincrónica.
    /// </summary>
    public void LoadScene(string sceneName)
    {
        SceneManager.LoadScene(sceneName);
    }

    /// <summary>
    /// Carga una escena directamente por su índice en Build Settings de forma sincrónica.
    /// </summary>
    public void LoadScene(int sceneIndex)
    {
        SceneManager.LoadScene(sceneIndex);
    }

    /// <summary>
    /// Carga una escena de forma asíncrona (útil para mostrar pantallas de carga).
    /// </summary>
    public void LoadSceneAsync(string sceneName)
    {
        StartCoroutine(LoadSceneCoroutine(sceneName));
    }

    /// <summary>
    /// Carga una escena de forma asíncrona por su índice.
    /// </summary>
    public void LoadSceneAsync(int sceneIndex)
    {
        StartCoroutine(LoadSceneCoroutine(sceneIndex));
    }


    private IEnumerator LoadSceneCoroutine(string sceneName)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    private IEnumerator LoadSceneCoroutine(int sceneIndex)
    {
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneIndex);

        while (!asyncLoad.isDone)
        {
            yield return null;
        }
    }

    /// <summary>
    /// Recarga la escena actual.
    /// </summary>
    public void ReloadCurrentScene()
    {
        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    /// <summary>
    /// Sale de la aplicación.
    /// </summary>
    public void QuitGame()
    {
        Debug.Log("Quit Game Requested");
        Application.Quit();
        
#if UNITY_EDITOR
        UnityEditor.EditorApplication.isPlaying = false;
#endif
    }
}
