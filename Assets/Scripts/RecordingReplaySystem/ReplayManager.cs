using UnityEngine;
using System.Collections.Generic;

public class ReplayManager : MonoBehaviour
{
    public static ReplayManager instance;
    public List<string> recordableTags = new List<string>();

    private List<ReplayObject> allReplayObjects = new List<ReplayObject>();
    public bool IsRecordingGlobal = false; // Público para que CameraScoring lo lea
    private float recordingStartTime;

    private void Awake()
    {
        if (instance == null) instance = this;
        else Destroy(gameObject);
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R)) StartRecording();
        if (Input.GetKeyDown(KeyCode.T)) StopRecording(); // Detener grabación
        if (Input.GetKeyDown(KeyCode.P)) StartPlayback();
    }

    public void StartRecording()
    {
        Debug.Log("---- Recording Started ----");
        IsRecordingGlobal = true;
        recordingStartTime = Time.time;

        foreach (var obj in allReplayObjects)
        {
            if (obj != null) obj.StartRecording();
        }
    }

    public void StopRecording()
    {
        Debug.Log("---- Recording Stopped ----");
        IsRecordingGlobal = false;
    }

    public void StartPlayback()
    {
        Debug.Log("---- Replay Reproducing ----");
        IsRecordingGlobal = false;

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

                if (IsRecordingGlobal)
                {
                    float timeOffset = Time.time - recordingStartTime;
                    newObj.StartRecording(timeOffset);
                }
            }
        }
    }
}