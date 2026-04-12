using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class SceneController : MonoBehaviour
{
    public static SceneController instance;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            DontDestroyOnLoad(gameObject); // Permite que el manager persista entre escenas
        }
        else
        {
            Destroy(gameObject);
        }
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
        // Aquí puedes invocar eventos de inicio de carga (ej: mostrar UI de carga)

        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(sceneName);
        
        // Evita que la escena se active inmediatamente (opcional, útil si quieres hacer transiciones)
        // asyncLoad.allowSceneActivation = false;

        while (!asyncLoad.isDone)
        {
            // float progress = Mathf.Clamp01(asyncLoad.progress / 0.9f);
            // Actualiza aquí tu barra de progreso usando 'progress'

            // Si pusiste allowSceneActivation = false, deberás ponerlo a true cuando progress >= 1
            // if (asyncLoad.progress >= 0.9f) asyncLoad.allowSceneActivation = true;

            yield return null;
        }

        // Aquí puedes invocar eventos de fin de carga (ej: ocultar UI de carga)
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
