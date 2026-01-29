using UnityEngine;

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

    //
}