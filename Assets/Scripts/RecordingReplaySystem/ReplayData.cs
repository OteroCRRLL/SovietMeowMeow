using UnityEngine;
using System.Collections.Generic;

[System.Serializable]
public struct ReplayFrame
{
    public Vector3 position;
    public Quaternion rotation;

    public Quaternion[] extraRotations;
    public bool isAnimEnabled;
    public ReplayAnimatorSnapshot animatorSnapshot;

    public float frameTime;
}

[System.Serializable]
public class ReplayAnimatorSnapshot
{
    public string[] boolNames = System.Array.Empty<string>();
    public bool[] boolValues = System.Array.Empty<bool>();
    public string[] floatNames = System.Array.Empty<string>();
    public float[] floatValues = System.Array.Empty<float>();
    public string[] intNames = System.Array.Empty<string>();
    public int[] intValues = System.Array.Empty<int>();
    public int[] layerHashes = System.Array.Empty<int>();
    public float[] layerNormalizedTimes = System.Array.Empty<float>();
}

[System.Serializable]
public class ReplayAudioEvent
{
    public float time;
    public string clipName;
    public float volume = 1f;
    public float pitch = 1f;
}

[System.Serializable]
public class ReplaySessionData
{
    public string prefabPath;
    public string prefabName;
    public string catalogId;
    public int sourceInstanceId;
    public List<ReplayFrame> frames = new List<ReplayFrame>();
    public List<ReplayAudioEvent> audioEvents = new List<ReplayAudioEvent>();
    public float spawnOffset;
    public float destroyTime = -1f;
    public string objectTag;
    public bool isPlayer = false;
}

[System.Serializable]
public class DayReplayArchive
{
    public int dayNumber;
    public List<ReplaySessionData> sessions = new List<ReplaySessionData>();
    public Vector3 recordingOrigin;
    public bool usesRelativeCoordinates = true;
}
