using UnityEngine;
using System.Collections.Generic;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager instance;
    public List<string> recordableTags = new List<string>();
    
    [Header("Replay Prefabs Database")]
    [Tooltip("Drag here the prefabs (Player, Enemies) so the system can clone them.")]
    public List<GameObject> spawnablePrefabs = new List<GameObject>();
    
    [Tooltip("Drag here the Level Geometry prefab to be cloned during Replay in Hub.")]
    public GameObject levelGeometryPrefab;

    // Active objects being recorded in the current scene
    private List<ReplayObject> allReplayObjects = new List<ReplayObject>();
    
    // Stored data for the Hub
    public List<ReplaySessionData> recordedSessions = new List<ReplaySessionData>();
    
    // Replay clones spawned in the Hub
    private List<GameObject> activeReplayClones = new List<GameObject>();
    private GameObject activeLevelClone;

    public bool IsRecordingGlobal = false;
    public bool hasStartedRecording = false;
    
    // Internal timer that only advances when recording
    public float currentRecordedTime = 0f;

    public float maxCapacity = 100;
    public float currentCapacity = 100;

    [Header("UI")]
    public GameObject recordingIndicatorUI;
    public float blinkSpeed = 2f;
    private CanvasGroup indicatorCanvasGroup;

    private void Awake()
    {
        if (instance == null)
        {
            instance = this;
            transform.SetParent(null); // Asegurar que sea root para DontDestroyOnLoad
            DontDestroyOnLoad(gameObject);
            
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
        }
        else
        {
            if (recordingIndicatorUI != null)
            {
                recordingIndicatorUI.SetActive(false);
            }
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        // Detenemos grabación primero para que todos guarden sus datos antes de limpiar
        StopRecording();
        allReplayObjects.Clear();
    }

    public void LinkIndicatorUI(GameObject newUI)
    {
        recordingIndicatorUI = newUI;
        indicatorCanvasGroup = recordingIndicatorUI.GetComponent<CanvasGroup>();
        if (indicatorCanvasGroup == null)
        {
            indicatorCanvasGroup = recordingIndicatorUI.AddComponent<CanvasGroup>();
        }
        
        if (recordingIndicatorUI != null)
        {
            recordingIndicatorUI.SetActive(IsRecordingGlobal);
            if (!IsRecordingGlobal && indicatorCanvasGroup != null) 
            {
                indicatorCanvasGroup.alpha = 1f;
            }
        }
    }

    private void Start()
    {
        if (recordingIndicatorUI != null) 
        {
            recordingIndicatorUI.SetActive(false);
            indicatorCanvasGroup = recordingIndicatorUI.GetComponent<CanvasGroup>();
            if (indicatorCanvasGroup == null)
            {
                indicatorCanvasGroup = recordingIndicatorUI.AddComponent<CanvasGroup>();
            }
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            string currentScene = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;
            if (currentScene == "Hub" || currentScene == "MainMenu")
            {
                Debug.Log("Grabación desactivada en el " + currentScene);
                return;
            }

            if (IsRecordingGlobal) PauseRecording();
            else ResumeRecording();
        }
        if (Input.GetKeyDown(KeyCode.T)) StopRecording(); 

        if (IsRecordingGlobal)
        {
            // The internal timer only advances while we are recording!
            currentRecordedTime += Time.deltaTime;
            
            if (currentCapacity > 0)
            {
                currentCapacity -= Time.deltaTime;
                if (currentCapacity <= 0)
                {
                    currentCapacity = 0;
                    Debug.Log("Capacidad de grabación agotada. Deteniendo grabación automática.");
                    PauseRecording();
                }
            }
        }

        if (IsRecordingGlobal && indicatorCanvasGroup != null)
        {
            indicatorCanvasGroup.alpha = Mathf.PingPong(Time.time * blinkSpeed, 1f);
        }
    }

    public void ResumeRecording()
    {
        if (currentCapacity <= 0) return;
        
        if (!hasStartedRecording)
        {
            Debug.Log("---- Recording Started ----");
            IsRecordingGlobal = true;
            hasStartedRecording = true;
            currentRecordedTime = 0f;
            
            // Limpiamos grabaciones anteriores al iniciar una nueva
            recordedSessions.Clear();

            if (recordingIndicatorUI != null) recordingIndicatorUI.SetActive(true);

            foreach (var obj in allReplayObjects)
            {
                if (obj != null) obj.StartRecording();
            }
        }
        else
        {
            Debug.Log("---- Recording Resumed ----");
            IsRecordingGlobal = true;

            if (recordingIndicatorUI != null) recordingIndicatorUI.SetActive(true);

            foreach (var obj in allReplayObjects)
            {
                if (obj != null) obj.ResumeRecording();
            }
        }
    }

    public void PauseRecording()
    {
        if (!IsRecordingGlobal) return;
        Debug.Log("---- Recording Paused ----");
        IsRecordingGlobal = false;

        if (recordingIndicatorUI != null) recordingIndicatorUI.SetActive(false);

        foreach (var obj in allReplayObjects)
        {
            if (obj != null) obj.PauseRecording();
        }
    }

    public void StopRecording()
    {
        if (!hasStartedRecording) return;
        Debug.Log("---- Recording Stopped ----");
        IsRecordingGlobal = false;
        hasStartedRecording = false;

        if (recordingIndicatorUI != null) recordingIndicatorUI.SetActive(false);

        foreach (var obj in allReplayObjects)
        {
            if (obj != null) obj.StopRecording();
        }
    }

    public void ResetCapacity()
    {
        currentCapacity = maxCapacity;
    }

    // Usado por los objetos al morir o al detener grabación
    public void SaveSessionData(ReplaySessionData data)
    {
        if (data != null && data.frames.Count > 0)
        {
            recordedSessions.Add(data);
        }
    }

    public void RegisterObject(ReplayObject newObj)
    {
        if (recordableTags.Contains(newObj.gameObject.tag) || recordableTags.Contains(newObj.tag))
        {
            if (!allReplayObjects.Contains(newObj))
            {
                allReplayObjects.Add(newObj);

                if (hasStartedRecording)
                {
                    // Usa el tiempo continuo grabado como su punto de entrada
                    newObj.StartRecording(currentRecordedTime);
                    if (!IsRecordingGlobal)
                    {
                        newObj.PauseRecording();
                    }
                }
            }
        }
    }

    public Vector3 replayOffset = new Vector3(0, -500, 0);

    // --- REPLAY EN EL HUB ---

    public void StartPlaybackFromData()
    {
        Debug.Log("---- Replay Reproducing (From Data) ----");
        IsRecordingGlobal = false;
        hasStartedRecording = false;

        if (recordingIndicatorUI != null) recordingIndicatorUI.SetActive(false);

        // Limpiar clones anteriores si hubiera
        StopPlayback();

        // 1. Instanciar Nivel desplazado para no chocar con el Hub
        if (levelGeometryPrefab != null)
        {
            activeLevelClone = Instantiate(levelGeometryPrefab, replayOffset, Quaternion.identity);
        }

        // 2. Instanciar clones de los datos grabados
        foreach (var session in recordedSessions)
        {
            GameObject prefabToSpawn = spawnablePrefabs.Find(p => p.name == session.prefabName || p.tag == session.objectTag);
            
            // Especialmente para el player, podemos usar su tag si el nombre no coincide
            if (prefabToSpawn == null && session.isPlayer)
            {
                prefabToSpawn = spawnablePrefabs.Find(p => p.CompareTag("Player"));
            }

            if (prefabToSpawn != null)
            {
                // Instanciar el clon
                GameObject clone = Instantiate(prefabToSpawn);
                activeReplayClones.Add(clone);

                // Configurar su ReplayObject para reproducir
                ReplayObject replayObj = clone.GetComponent<ReplayObject>();
                if (replayObj != null)
                {
                    replayObj.SetupForReplay(session);
                    replayObj.StartReplay();
                }
            }
            else
            {
                Debug.LogWarning("No se encontró un prefab para instanciar en Replay: " + session.prefabName);
            }
        }
    }

    public void StopPlayback()
    {
        foreach (var clone in activeReplayClones)
        {
            if (clone != null) Destroy(clone);
        }
        activeReplayClones.Clear();

        if (activeLevelClone != null)
        {
            Destroy(activeLevelClone);
        }
    }

    public GameObject GetPlayerClone()
    {
        return activeReplayClones.Find(c => c.CompareTag("Player"));
    }
}
