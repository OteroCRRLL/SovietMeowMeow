using UnityEngine;
using System.Collections.Generic;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager instance;
    public List<string> recordableTags = new List<string>();

    private List<ReplayObject> allReplayObjects = new List<ReplayObject>();
    public bool IsRecordingGlobal = false;
    public bool hasStartedRecording = false;
    private float recordingStartTime;

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
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
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
            if (IsRecordingGlobal) PauseRecording();
            else ResumeRecording();
        }
        if (Input.GetKeyDown(KeyCode.T)) StopRecording(); // Detener grabacion
        if (Input.GetKeyDown(KeyCode.P)) StartPlayback();

        if (this.IsRecordingGlobal && currentCapacity > 0)
        {
            currentCapacity -= Time.deltaTime;
            if (currentCapacity <= 0)
            {
                currentCapacity = 0;
                Debug.Log("Capacidad de grabación agotada. Deteniendo grabación automática.");
                this.PauseRecording();
            }
        }

        // Lógica de parpadeo (opacidad arriba y abajo)
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
            recordingStartTime = Time.time;

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

    public void StartPlayback()
    {
        Debug.Log("---- Replay Reproducing ----");
        IsRecordingGlobal = false;
        hasStartedRecording = false;

        if (recordingIndicatorUI != null) recordingIndicatorUI.SetActive(false);

        foreach (var obj in allReplayObjects)
        {
            if (obj != null) obj.StartReplay();
        }
    }

    public void RegisterObject(ReplayObject newObj)
    {
        if (recordableTags.Contains(newObj.tag))
        {
            if (!allReplayObjects.Contains(newObj))
            {
                allReplayObjects.Add(newObj);

                if (hasStartedRecording)
                {
                    float timeOffset = Time.time - recordingStartTime;
                    newObj.StartRecording(timeOffset);
                    if (!IsRecordingGlobal)
                    {
                        newObj.PauseRecording();
                    }
                }
            }
        }
    }
}