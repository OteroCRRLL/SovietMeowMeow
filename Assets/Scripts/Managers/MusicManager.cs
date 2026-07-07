using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

/// <summary>
/// Reproduce música de ambiente y hace crossfade al cambiar de escena:
/// una pista para el menú principal y otra para el Hub/nivel.
/// Singleton persistente (igual que GameManager/SceneController): vive dentro
/// de _SYSTEMS, se desengancha en Awake() y sobrevive a las cargas de escena.
/// </summary>
public class MusicManager : MonoBehaviour
{
    public static MusicManager instance;

    [Header("Audio Sources")]
    public AudioSource sourceA;
    public AudioSource sourceB;

    [Header("Pistas")]
    [Tooltip("Suena en la escena MainMenu.")]
    public AudioClip menuMusic;
    [Tooltip("Suena en el Hub y en el nivel (Blockout).")]
    public AudioClip gameplayMusic;

    [Header("Ajustes")]
    [Range(0f, 1f)] public float musicVolume = 0.5f;
    public float crossfadeDuration = 2f;
    public string menuSceneName = "MainMenu";

    private AudioSource activeSource;
    private AudioSource inactiveSource;
    private AudioClip currentClip;
    private Coroutine crossfadeCoroutine;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);

            if (sourceA == null) sourceA = gameObject.AddComponent<AudioSource>();
            if (sourceB == null) sourceB = gameObject.AddComponent<AudioSource>();

            sourceA.loop = true;
            sourceB.loop = true;
            sourceA.playOnAwake = false;
            sourceB.playOnAwake = false;
            sourceA.volume = 0f;
            sourceB.volume = 0f;

            activeSource = sourceA;
            inactiveSource = sourceB;

            SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private void Start()
    {
        if (instance == this)
        {
            PlayForScene(SceneManager.GetActiveScene().name);
        }
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
        PlayForScene(scene.name);
    }

    private void PlayForScene(string sceneName)
    {
        AudioClip targetClip = (sceneName == menuSceneName) ? menuMusic : gameplayMusic;
        if (targetClip == null || targetClip == currentClip) return;

        currentClip = targetClip;

        if (crossfadeCoroutine != null) StopCoroutine(crossfadeCoroutine);
        crossfadeCoroutine = StartCoroutine(CrossfadeTo(targetClip));
    }

    private IEnumerator CrossfadeTo(AudioClip clip)
    {
        inactiveSource.clip = clip;
        inactiveSource.volume = 0f;
        inactiveSource.Play();

        float startActiveVolume = activeSource.volume;
        float elapsed = 0f;
        while (elapsed < crossfadeDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = elapsed / crossfadeDuration;
            activeSource.volume = Mathf.Lerp(startActiveVolume, 0f, t);
            inactiveSource.volume = Mathf.Lerp(0f, musicVolume, t);
            yield return null;
        }

        activeSource.volume = 0f;
        activeSource.Stop();
        inactiveSource.volume = musicVolume;

        AudioSource temp = activeSource;
        activeSource = inactiveSource;
        inactiveSource = temp;
    }
}
