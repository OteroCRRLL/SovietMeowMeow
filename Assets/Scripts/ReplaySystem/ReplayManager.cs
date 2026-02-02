using UnityEngine;
using System.Collections.Generic;

public class ReplayManager : MonoBehaviour
{
    public List<string> recordableTags = new List<string>();


    private List<ReplayObject> allReplayObjects = new List<ReplayObject>();
    private bool isRecordingGlobal = false; //Controls manager state

    private float recordingStartTime;



    void Update()
    {
        //Testing
        if (Input.GetKeyDown(KeyCode.R)) StartRecording();
        if (Input.GetKeyDown(KeyCode.P)) StartPlayback();
    }

    public void StartRecording()
    {
        Debug.Log("---- Recording Started ----");
        isRecordingGlobal = true;

        //Save actual time for reference
        recordingStartTime = Time.time;

        foreach (var obj in allReplayObjects)
        {
            if (obj != null) obj.StartRecording();
        }
    }

    public void StartPlayback()
    {
        Debug.Log("---- Replay Reproducing ----");
        isRecordingGlobal = false;

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

                if (isRecordingGlobal)
                {
                    float timeOffset = Time.time - recordingStartTime;

                    Debug.Log($"Object  {newObj.name} registered with offset: {timeOffset}");
                    newObj.StartRecording(timeOffset);
                }

            }
        }

        else
        {
            Debug.LogWarning($"The object {newObj.name} with tag {newObj.tag} is not in the list");
        }

    }
}