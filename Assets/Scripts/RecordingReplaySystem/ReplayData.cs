using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ReplayFrame
{
    // Basic Data
    public Vector3 position;
    public Quaternion rotation;

    // Animation Data
    public bool[] boolValues;

    // Hierarchy Data
    public Quaternion[] extraRotations;

    //Record animator state
    public bool isAnimEnabled;

    // Frame Time (Continuous recorded time)
    public float frameTime;
}

[System.Serializable]
public class ReplaySessionData
{
    public string prefabPath; // Use a path or an ID to know what to instantiate
    public string prefabName; // Name of the prefab to load from Resources or a manager
    public List<ReplayFrame> frames = new List<ReplayFrame>();
    public float spawnOffset;
    public float destroyTime = -1f; // -1 if never destroyed during recording
    public string objectTag;
    
    // For identifying player specifically
    public bool isPlayer = false;
}
