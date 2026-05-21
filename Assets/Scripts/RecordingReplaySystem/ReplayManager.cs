using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager instance;
    public List<string> recordableTags = new List<string>();
    
    [Header("Replay Clone Prefabs (Assign Here)")]
    public GameObject playerClonePrefab;
    public GameObject soldierClonePrefab;
    public GameObject droneClonePrefab;
    public GameObject tankClonePrefab;

    [Header("Replay Prefabs Database (Fallback)")]
    [Tooltip("Drag here the prefabs (Player, Enemies) so the system can clone them.")]
    public List<GameObject> spawnablePrefabs = new List<GameObject>();
    
    [Tooltip("Drag here the Level Geometry prefab to be cloned during Replay in Hub.")]
    public GameObject levelGeometryPrefab;

    [Header("Grabación de escena completa")]
    [Tooltip("Registra todos los objetos relevantes de la escena, no solo los que pasan por cámara.")]
    public bool autoRecordFullScene = true;
    [Tooltip("Intervalo para registrar enemigos/objetos que spawnean durante la misión.")]
    public float sceneScanInterval = 1f;

    // Espacio de reproducción (dinámico según WorldGeometry)
    [HideInInspector] public Vector3 playbackWorldOrigin = Vector3.zero;

    // Active objects being recorded in the current scene
    private List<ReplayObject> allReplayObjects = new List<ReplayObject>();
    
    // Grabación en curso de la misión actual
    [HideInInspector] public List<ReplaySessionData> recordedSessions = new List<ReplaySessionData>();

    // Historial por día (persiste entre escenas y guardados)
    [HideInInspector] public List<DayReplayArchive> replayArchive = new List<DayReplayArchive>();

    [HideInInspector] [SerializeField] private int selectedPlaybackDay = -1;
    
    // Replay clones spawned in the Hub
    private List<GameObject> activeReplayClones = new List<GameObject>();

    [HideInInspector] public bool IsRecordingGlobal = false;
    [HideInInspector] public bool hasStartedRecording = false;
    
    // Internal timer that only advances when recording
    [HideInInspector] public float currentRecordedTime = 0f;

    [HideInInspector] public float maxCapacity = 100;
    [HideInInspector] public float currentCapacity = 100;

    [Header("UI")]
    public GameObject recordingIndicatorUI;
    public float blinkSpeed = 2f;
    private CanvasGroup indicatorCanvasGroup;

    [Header("Audio")]
    public AudioSource cameraAudioSource;
    public AudioClip cameraRecordingClip;

    private static readonly string[] DefaultRecordableTags = { "Player", "Bullet", "Enemy", "SmallBullet", "Default" };
    private float nextSceneScanTime;
    private Vector3 recordingWorldOrigin;
    private bool usesRelativeRecording = true;

#if UNITY_EDITOR
    private const string DefaultLevelGeometryPath = "Assets/Prefabs/Buildings/WorldGeometry.prefab";
#endif

    private void Awake()
    {
        EnsureDefaultTags();
        EnsureLevelGeometryPrefab();

        if (instance == null)
        {
            instance = this;
            transform.SetParent(null);
            DontDestroyOnLoad(gameObject);
            UnityEngine.SceneManagement.SceneManager.sceneLoaded += OnSceneLoaded;
            
            if (cameraAudioSource == null) cameraAudioSource = gameObject.AddComponent<AudioSource>();
        }
        else if (instance != this)
        {
            instance.MergeSceneConfiguration(this);
            Destroy(gameObject);
        }
    }

    private void OnDestroy()
    {
        if (instance == this)
        {
            UnityEngine.SceneManagement.SceneManager.sceneLoaded -= OnSceneLoaded;
            instance = null;
        }
    }

    private void OnSceneLoaded(UnityEngine.SceneManagement.Scene scene, UnityEngine.SceneManagement.LoadSceneMode mode)
    {
        StopPlayback();
        FinalizeMissionRecording();
        allReplayObjects.Clear();
    }

    private void EnsureDefaultTags()
    {
        if (recordableTags == null || recordableTags.Count == 0)
        {
            recordableTags = new List<string>(DefaultRecordableTags);
        }
    }

    private void MergeSceneConfiguration(ReplayManager sceneInstance)
    {
        EnsureDefaultTags();
        sceneInstance.EnsureDefaultTags();

        if (sceneInstance.spawnablePrefabs != null && sceneInstance.spawnablePrefabs.Count > 0)
        {
            spawnablePrefabs = sceneInstance.spawnablePrefabs;
        }

        if (IsValidPrefabReference(sceneInstance.levelGeometryPrefab))
        {
            levelGeometryPrefab = sceneInstance.levelGeometryPrefab;
        }

        if (sceneInstance.recordableTags != null && sceneInstance.recordableTags.Count > 0)
        {
            recordableTags = sceneInstance.recordableTags;
        }

        if (sceneInstance.cameraRecordingClip != null)
        {
            cameraRecordingClip = sceneInstance.cameraRecordingClip;
        }

        if (cameraAudioSource == null)
        {
            cameraAudioSource = gameObject.AddComponent<AudioSource>();
        }

        EnsureLevelGeometryPrefab();
    }

    private void EnsureLevelGeometryPrefab()
    {
        if (IsValidPrefabReference(levelGeometryPrefab)) return;

        if (GameManager.instance != null && IsValidPrefabReference(GameManager.instance.replayLevelGeometryPrefab))
        {
            levelGeometryPrefab = GameManager.instance.replayLevelGeometryPrefab;
            return;
        }

#if UNITY_EDITOR
        GameObject loaded = UnityEditor.AssetDatabase.LoadAssetAtPath<GameObject>(DefaultLevelGeometryPath);
        if (loaded != null)
        {
            levelGeometryPrefab = loaded;
            Debug.Log("ReplayManager: levelGeometryPrefab cargado desde Assets.");
        }
#endif
    }

    private static bool IsValidPrefabReference(GameObject prefab)
    {
        if (prefab == null) return false;

#if UNITY_EDITOR
        return UnityEditor.PrefabUtility.IsPartOfPrefabAsset(prefab);
#else
        return !prefab.scene.IsValid();
#endif
    }

    public void ScanSceneForRecordables()
    {
        foreach (ReplayObject existing in FindObjectsByType<ReplayObject>(FindObjectsInactive.Exclude, FindObjectsSortMode.None))
        {
            if (existing != null)
            {
                existing.AutoConfigureComponents();
                RegisterObject(existing);
            }
        }

        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            ScanHierarchyForRecordables(root.transform);
        }
    }

    private void ScanHierarchyForRecordables(Transform node)
    {
        if (node == null) return;

        GameObject go = node.gameObject;
        if (ShouldAutoRecord(go))
        {
            ReplayObject replayObject = go.GetComponent<ReplayObject>();
            if (replayObject == null)
            {
                replayObject = go.AddComponent<ReplayObject>();
            }

            replayObject.AutoConfigureComponents();
            RegisterObject(replayObject);
        }

        for (int i = 0; i < node.childCount; i++)
        {
            ScanHierarchyForRecordables(node.GetChild(i));
        }
    }

    private bool ShouldAutoRecord(GameObject go)
    {
        if (go == null || !go.activeInHierarchy) return false;
        if (go.GetComponent<ReplayManager>() != null) return false;

        if (recordableTags.Contains(go.tag)) return true;
        if (go.GetComponent<SoldierBrain>() != null) return true;
        if (go.GetComponent<DroneBrain>() != null) return true;
        if (go.GetComponent<TankBrain>() != null) return true;

        return false;
    }

    public Vector3 WorldToRecordedPosition(Vector3 worldPosition)
    {
        if (!usesRelativeRecording) return worldPosition;
        return worldPosition - recordingWorldOrigin;
    }

    public Vector3 RecordedToWorldPosition(Vector3 recordedPosition)
    {
        if (!usesRelativeRecording)
        {
            return recordedPosition + (playbackWorldOrigin - recordingWorldOrigin);
        }

        return playbackWorldOrigin + recordedPosition;
    }

    private void CaptureRecordingOrigin()
    {
        recordingWorldOrigin = FindWorldGeometryOriginInActiveScene();
        Debug.Log($"Replay: origen de grabación = {recordingWorldOrigin}");
    }

    private Vector3 FindWorldGeometryOriginInActiveScene()
    {
        GameObject worldGeometry = GameObject.Find("WorldGeometry");
        if (worldGeometry != null)
        {
            return worldGeometry.transform.position;
        }

        foreach (GameObject root in UnityEngine.SceneManagement.SceneManager.GetActiveScene().GetRootGameObjects())
        {
            if (root.name.Contains("WorldGeometry"))
            {
                return root.transform.position;
            }
        }

        return Vector3.zero;
    }

    private void ApplyPlaybackSpaceForDay(int dayNumber)
    {
        playbackWorldOrigin = FindWorldGeometryOriginInActiveScene();

        DayReplayArchive archive = replayArchive.Find(a => a.dayNumber == dayNumber);
        if (archive != null)
        {
            recordingWorldOrigin = archive.recordingOrigin;
            usesRelativeRecording = archive.usesRelativeCoordinates;
            return;
        }

        usesRelativeRecording = true;
    }

    private GameObject FindGameplayPrefabForSession(ReplaySessionData session)
    {
        if (session == null) return null;

        if (!string.IsNullOrEmpty(session.catalogId))
        {
            GameObject byCatalog = spawnablePrefabs.Find(p => p != null && p.name == session.catalogId);
            if (byCatalog != null) return byCatalog;
        }

        GameObject byName = spawnablePrefabs.Find(p =>
            p != null && (p.name == session.prefabName || session.prefabName.StartsWith(p.name)));
        if (byName != null) return byName;

        GameObject byTag = spawnablePrefabs.Find(p => p != null && p.tag == session.objectTag);
        if (byTag != null) return byTag;

        if (session.isPlayer)
        {
            return spawnablePrefabs.Find(p => p != null && p.CompareTag("Player"));
        }

        return null;
    }

    private GameObject FindVisualPrefabForSession(ReplaySessionData session)
    {
        if (session == null) return null;

        if (session.isPlayer && IsValidPrefabReference(playerClonePrefab))
            return playerClonePrefab;
        if (session.catalogId == "SoldierAgent" && IsValidPrefabReference(soldierClonePrefab))
            return soldierClonePrefab;
        if (session.catalogId == "DroneAgent" && IsValidPrefabReference(droneClonePrefab))
            return droneClonePrefab;
        if (session.catalogId == "TankAgent" && IsValidPrefabReference(tankClonePrefab))
            return tankClonePrefab;

        return FindGameplayPrefabForSession(session);
    }

    private Vector3 GetSpawnPose(ReplaySessionData session, out Quaternion rotation)
    {
        rotation = Quaternion.identity;
        if (session == null || session.frames == null || session.frames.Count == 0)
        {
            return playbackWorldOrigin;
        }

        ReplayFrame firstFrame = session.frames[0];
        rotation = firstFrame.rotation;
        return RecordedToWorldPosition(firstFrame.position);
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
            currentRecordedTime += Time.deltaTime;

            if (autoRecordFullScene && Time.time >= nextSceneScanTime)
            {
                nextSceneScanTime = Time.time + sceneScanInterval;
                ScanSceneForRecordables();
            }
            
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
        
        if (cameraAudioSource != null && cameraRecordingClip != null)
        {
            cameraAudioSource.PlayOneShot(cameraRecordingClip);
        }

        if (!hasStartedRecording)
        {
            Debug.Log("---- Recording Started ----");
            IsRecordingGlobal = true;
            hasStartedRecording = true;
            // currentRecordedTime = 0f; no reset to maintain timeline
            
            // Solo limpiamos la misión en curso, no el archivo histórico por día
            // recordedSessions.Clear(); no clear to allow appending

            if (recordingIndicatorUI != null) recordingIndicatorUI.SetActive(true);

            CaptureRecordingOrigin();
            usesRelativeRecording = true;

            if (autoRecordFullScene)
            {
                ScanSceneForRecordables();
            }

            foreach (var obj in allReplayObjects)
            {
                if (obj != null && !obj.IsCurrentlyRecording)
                {
                    obj.StartRecording(currentRecordedTime);
                }
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

    public void SaveSessionData(ReplaySessionData data)
    {
        if (data != null && data.frames.Count > 0)
        {
            recordedSessions.Add(data);
        }
    }

    public int GetCurrentGameDay()
    {
        return GameManager.instance != null ? GameManager.instance.currentDay : 1;
    }

    /// <summary>
    /// Guarda la misión actual en el archivo del día indicado (normalmente el día en curso al extraer).
    /// </summary>
    public void ArchiveMissionForDay(int dayNumber)
    {
        if (recordedSessions == null || recordedSessions.Count == 0) return;

        DayReplayArchive archive = replayArchive.Find(a => a.dayNumber == dayNumber);
        if (archive == null)
        {
            archive = new DayReplayArchive { dayNumber = dayNumber, sessions = new List<ReplaySessionData>() };
            replayArchive.Add(archive);
        }

        archive.sessions = CloneSessionList(recordedSessions);
        archive.recordingOrigin = recordingWorldOrigin;
        archive.usesRelativeCoordinates = usesRelativeRecording;
        replayArchive.Sort((a, b) => a.dayNumber.CompareTo(b.dayNumber));

        Debug.Log($"Replay archivado para el día {dayNumber} ({archive.sessions.Count} sesiones). Origen: {recordingWorldOrigin}");
    }

    public void FinalizeMissionRecording()
    {
        if (hasStartedRecording)
        {
            StopRecording();
        }

        foreach (var obj in allReplayObjects)
        {
            if (obj != null)
            {
                var data = obj.GetSessionData();
                if (data != null && data.frames.Count > 0 && !recordedSessions.Contains(data))
                {
                    recordedSessions.Add(data);
                }
            }
        }

        if (recordedSessions.Count > 0)
        {
            ArchiveMissionForDay(GetCurrentGameDay());
            recordedSessions.Clear();
        }

        currentRecordedTime = 0f;
    }

    public List<int> GetAvailableReplayDays()
    {
        List<int> days = replayArchive
            .Where(a => a.sessions != null && a.sessions.Count > 0)
            .Select(a => a.dayNumber)
            .OrderBy(d => d)
            .ToList();

        if (recordedSessions.Count > 0 && !days.Contains(GetCurrentGameDay()))
        {
            days.Add(GetCurrentGameDay());
            days.Sort();
        }

        return days;
    }

    public int GetDefaultPlaybackDay()
    {
        List<int> days = GetAvailableReplayDays();
        if (days.Count == 0) return GetCurrentGameDay();

        int currentDay = GetCurrentGameDay();
        if (days.Contains(currentDay)) return currentDay;
        return days[days.Count - 1];
    }

    public void SetPlaybackDay(int dayNumber)
    {
        selectedPlaybackDay = dayNumber;
    }

    public bool HasPlaybackData(int dayNumber)
    {
        if (dayNumber == GetCurrentGameDay() && recordedSessions.Count > 0) return true;

        DayReplayArchive archive = replayArchive.Find(a => a.dayNumber == dayNumber);
        return archive != null && archive.sessions != null && archive.sessions.Count > 0;
    }

    public bool HasAnyPlaybackData()
    {
        return GetAvailableReplayDays().Count > 0;
    }

    public List<ReplaySessionData> GetPlaybackSessions(int dayNumber)
    {
        if (dayNumber == GetCurrentGameDay() && recordedSessions.Count > 0)
        {
            return recordedSessions;
        }

        DayReplayArchive archive = replayArchive.Find(a => a.dayNumber == dayNumber);
        if (archive != null && archive.sessions != null && archive.sessions.Count > 0)
        {
            return archive.sessions;
        }

        return recordedSessions;
    }

    public int CyclePlaybackDay(int direction)
    {
        List<int> days = GetAvailableReplayDays();
        if (days.Count == 0)
        {
            selectedPlaybackDay = GetCurrentGameDay();
            return selectedPlaybackDay;
        }

        if (selectedPlaybackDay < 0 || !days.Contains(selectedPlaybackDay))
        {
            selectedPlaybackDay = GetDefaultPlaybackDay();
            return selectedPlaybackDay;
        }

        int index = days.IndexOf(selectedPlaybackDay);
        index = (index + direction + days.Count) % days.Count;
        selectedPlaybackDay = days[index];
        return selectedPlaybackDay;
    }

    public List<DayReplayArchive> ExportArchiveForSave()
    {
        return replayArchive
            .Where(a => a.sessions != null && a.sessions.Count > 0)
            .Select(a => new DayReplayArchive
            {
                dayNumber = a.dayNumber,
                sessions = CloneSessionList(a.sessions),
                recordingOrigin = a.recordingOrigin,
                usesRelativeCoordinates = a.usesRelativeCoordinates
            })
            .ToList();
    }

    public void ImportArchiveFromSave(List<DayReplayArchive> savedArchive)
    {
        replayArchive = savedArchive ?? new List<DayReplayArchive>();
        replayArchive.Sort((a, b) => a.dayNumber.CompareTo(b.dayNumber));
        selectedPlaybackDay = GetDefaultPlaybackDay();
    }

    public void ClearAllReplayData()
    {
        recordedSessions.Clear();
        replayArchive.Clear();
        selectedPlaybackDay = -1;
        StopPlayback();
    }

    private static List<ReplaySessionData> CloneSessionList(List<ReplaySessionData> source)
    {
        var copy = new List<ReplaySessionData>();
        foreach (ReplaySessionData session in source)
        {
            if (session == null || session.frames == null || session.frames.Count == 0) continue;

            copy.Add(new ReplaySessionData
            {
                prefabPath = session.prefabPath,
                prefabName = session.prefabName,
                catalogId = session.catalogId,
                sourceInstanceId = session.sourceInstanceId,
                spawnOffset = session.spawnOffset,
                destroyTime = session.destroyTime,
                objectTag = session.objectTag,
                isPlayer = session.isPlayer,
                frames = CloneFrameList(session.frames),
                audioEvents = session.audioEvents != null
                    ? new List<ReplayAudioEvent>(session.audioEvents)
                    : new List<ReplayAudioEvent>()
            });
        }
        return copy;
    }

    private static List<ReplayFrame> CloneFrameList(List<ReplayFrame> source)
    {
        var frames = new List<ReplayFrame>();
        if (source == null) return frames;

        foreach (ReplayFrame frame in source)
        {
            ReplayFrame copy = frame;
            if (frame.extraRotations != null)
            {
                copy.extraRotations = (Quaternion[])frame.extraRotations.Clone();
            }

            if (frame.animatorSnapshot != null)
            {
                copy.animatorSnapshot = CloneAnimatorSnapshot(frame.animatorSnapshot);
            }

            frames.Add(copy);
        }

        return frames;
    }

    private static ReplayAnimatorSnapshot CloneAnimatorSnapshot(ReplayAnimatorSnapshot source)
    {
        return new ReplayAnimatorSnapshot
        {
            boolNames = source.boolNames != null ? (string[])source.boolNames.Clone() : System.Array.Empty<string>(),
            boolValues = source.boolValues != null ? (bool[])source.boolValues.Clone() : System.Array.Empty<bool>(),
            floatNames = source.floatNames != null ? (string[])source.floatNames.Clone() : System.Array.Empty<string>(),
            floatValues = source.floatValues != null ? (float[])source.floatValues.Clone() : System.Array.Empty<float>(),
            intNames = source.intNames != null ? (string[])source.intNames.Clone() : System.Array.Empty<string>(),
            intValues = source.intValues != null ? (int[])source.intValues.Clone() : System.Array.Empty<int>(),
            layerHashes = source.layerHashes != null ? (int[])source.layerHashes.Clone() : System.Array.Empty<int>(),
            layerNormalizedTimes = source.layerNormalizedTimes != null
                ? (float[])source.layerNormalizedTimes.Clone()
                : System.Array.Empty<float>()
        };
    }

    public void RegisterObject(ReplayObject newObj)
    {
        if (newObj == null || !ShouldAutoRecord(newObj.gameObject)) return;

        if (!allReplayObjects.Contains(newObj))
        {
            allReplayObjects.Add(newObj);

            if (hasStartedRecording && !newObj.IsCurrentlyRecording)
            {
                newObj.StartRecording(currentRecordedTime);
                if (!IsRecordingGlobal)
                {
                    newObj.PauseRecording();
                }
            }
        }
    }

    // --- REPLAY EN EL HUB ---

    public void StartPlaybackFromData()
    {
        StartPlaybackForDay(selectedPlaybackDay >= 0 ? selectedPlaybackDay : GetDefaultPlaybackDay());
    }

    public void StartPlaybackForDay(int dayNumber)
    {
        List<ReplaySessionData> sessions = GetPlaybackSessions(dayNumber);
        if (sessions == null || sessions.Count == 0)
        {
            Debug.LogWarning($"No hay datos de replay para el día {dayNumber}.");
            return;
        }

        selectedPlaybackDay = dayNumber;
        Debug.Log($"---- Replay Reproducing (Day {dayNumber}) ----");

        IsRecordingGlobal = false;
        hasStartedRecording = false;

        if (recordingIndicatorUI != null) recordingIndicatorUI.SetActive(false);

        StopPlayback();
        ApplyPlaybackSpaceForDay(dayNumber);

        Debug.Log($"Replay: origen de reproducción configurado en {playbackWorldOrigin} (origen grabado {recordingWorldOrigin}).");

        foreach (ReplaySessionData session in sessions)
        {
            GameObject prefabToSpawn = FindVisualPrefabForSession(session);
            if (prefabToSpawn == null)
            {
                Debug.LogWarning($"No hay prefab visual/juego para: {session.catalogId} ({session.prefabName})");
                continue;
            }

            Vector3 spawnPosition = GetSpawnPose(session, out Quaternion spawnRotation);
            GameObject clone = ReplayPlaybackUtility.SpawnVisualReplayClone(prefabToSpawn, spawnPosition, spawnRotation);
            activeReplayClones.Add(clone);

            ReplayObject replayObj = clone.GetComponent<ReplayObject>();
            if (replayObj == null)
            {
                replayObj = clone.AddComponent<ReplayObject>();
            }

            replayObj.AutoConfigureComponents();
            replayObj.SetupForReplay(session);
            replayObj.StartReplay();
        }

        Debug.Log($"Replay: {activeReplayClones.Count} actores instanciados, {sessions.Count} sesiones.");
    }

    public void StopPlayback()
    {
        foreach (var clone in activeReplayClones)
        {
            if (clone == null) continue;

            ReplayObject replayObj = clone.GetComponent<ReplayObject>();
            if (replayObj != null)
            {
                replayObj.StopReplayPlayback();
            }

            Destroy(clone);
        }
        activeReplayClones.Clear();
    }

    public GameObject GetPlayerClone()
    {
        GameObject byTag = activeReplayClones.Find(c => c != null && c.CompareTag("Player"));
        if (byTag != null) return byTag;

        if (IsValidPrefabReference(playerClonePrefab))
        {
            GameObject byName = activeReplayClones.Find(c =>
                c != null && c.name.StartsWith(playerClonePrefab.name));
            if (byName != null) return byName;
        }

        return activeReplayClones.Count > 0 ? activeReplayClones[0] : null;
    }
}
