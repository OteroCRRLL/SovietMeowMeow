using UnityEngine;
using System.Collections.Generic;

public class ReplayManager : MonoBehaviour
{
    public List<string> recordableTags = new List<string>();

    private List<ReplayObject> allReplayObjects = new List<ReplayObject>();
    private bool isRecordingGlobal = false; //Controls manager state

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

    //In-game spawned objects (bullets)
    public void RegisterObject(ReplayObject newObj)
    {
        if (recordableTags.Contains(newObj.tag))
        {
            if (!allReplayObjects.Contains(newObj))
            {
                allReplayObjects.Add(newObj);

                if (isRecordingGlobal)
                {
                    newObj.StartRecording();
                }

            }
        }

    }
}