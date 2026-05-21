using UnityEngine;
using System.Collections.Generic;

public static class ReplayAnimatorUtility
{
    public static ReplayAnimatorSnapshot Capture(Animator animator)
    {
        if (animator == null) return null;

        var snapshot = new ReplayAnimatorSnapshot();
        var boolNames = new List<string>();
        var boolValues = new List<bool>();
        var floatNames = new List<string>();
        var floatValues = new List<float>();
        var intNames = new List<string>();
        var intValues = new List<int>();

        foreach (AnimatorControllerParameter parameter in animator.parameters)
        {
            switch (parameter.type)
            {
                case AnimatorControllerParameterType.Bool:
                    boolNames.Add(parameter.name);
                    boolValues.Add(animator.GetBool(parameter.name));
                    break;
                case AnimatorControllerParameterType.Float:
                    floatNames.Add(parameter.name);
                    floatValues.Add(animator.GetFloat(parameter.name));
                    break;
                case AnimatorControllerParameterType.Int:
                    intNames.Add(parameter.name);
                    intValues.Add(animator.GetInteger(parameter.name));
                    break;
            }
        }

        snapshot.boolNames = boolNames.ToArray();
        snapshot.boolValues = boolValues.ToArray();
        snapshot.floatNames = floatNames.ToArray();
        snapshot.floatValues = floatValues.ToArray();
        snapshot.intNames = intNames.ToArray();
        snapshot.intValues = intValues.ToArray();

        int layerCount = animator.layerCount;
        snapshot.layerHashes = new int[layerCount];
        snapshot.layerNormalizedTimes = new float[layerCount];
        for (int i = 0; i < layerCount; i++)
        {
            AnimatorStateInfo state = animator.GetCurrentAnimatorStateInfo(i);
            snapshot.layerHashes[i] = state.shortNameHash;
            snapshot.layerNormalizedTimes[i] = state.normalizedTime;
        }

        return snapshot;
    }

    public static void Apply(Animator animator, ReplayAnimatorSnapshot snapshot)
    {
        if (animator == null || snapshot == null) return;

        if (snapshot.boolNames != null && snapshot.boolValues != null)
        {
            int count = Mathf.Min(snapshot.boolNames.Length, snapshot.boolValues.Length);
            for (int i = 0; i < count; i++)
            {
                animator.SetBool(snapshot.boolNames[i], snapshot.boolValues[i]);
            }
        }

        if (snapshot.floatNames != null && snapshot.floatValues != null)
        {
            int count = Mathf.Min(snapshot.floatNames.Length, snapshot.floatValues.Length);
            for (int i = 0; i < count; i++)
            {
                animator.SetFloat(snapshot.floatNames[i], snapshot.floatValues[i]);
            }
        }

        if (snapshot.intNames != null && snapshot.intValues != null)
        {
            int count = Mathf.Min(snapshot.intNames.Length, snapshot.intValues.Length);
            for (int i = 0; i < count; i++)
            {
                animator.SetInteger(snapshot.intNames[i], snapshot.intValues[i]);
            }
        }

        if (snapshot.layerHashes != null && snapshot.layerNormalizedTimes != null)
        {
            int count = Mathf.Min(snapshot.layerHashes.Length, snapshot.layerNormalizedTimes.Length);
            for (int i = 0; i < count; i++)
            {
                if (snapshot.layerHashes[i] != 0)
                {
                    animator.Play(snapshot.layerHashes[i], i, snapshot.layerNormalizedTimes[i]);
                }
            }
        }

        animator.Update(0f);
    }
}
