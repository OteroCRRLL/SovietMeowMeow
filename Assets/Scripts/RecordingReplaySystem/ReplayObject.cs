using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;

public class ReplayObject : MonoBehaviour
{
    [Header("Settings")]
    [Tooltip("Frame capturing interval (0.05 = 20fps, 0.1 = 10fps)")]
    public float recordInterval = 0.05f;
    public List<string> recordableTags = new List<string>();

    [Header("References")]
    public Animator anim;
    public Rigidbody rb;
    public NavMeshAgent agent;

    public Transform[] extraTransforms;

    [Header("Legacy bool params (opcional, el Animator se captura automáticamente)")]
    public string[] boolParams;

    private ReplaySessionData sessionData;
    private bool isRecording = false;
    private bool isReplaying = false;
    private float timeTimer = 0f;
    private float replayPlaybackTime = 0f;
    private int currentFrameIndex = 0;
    private int nextAudioEventIndex = 0;
    private AudioSource replayAudioSource;
    private readonly Dictionary<string, AudioClip> audioClipCache = new Dictionary<string, AudioClip>();

    private void Start()
    {
        if (ReplayManager.instance != null && !isReplaying)
        {
            ReplayManager.instance.RegisterObject(this);
        }
    }

    private void Update()
    {
        if (!isActiveAndEnabled || !gameObject) return;

        if (isRecording)
        {
            timeTimer += Time.deltaTime;
            if (timeTimer >= recordInterval)
            {
                RecordFrame();
                timeTimer = 0f;
            }
            return;
        }

        if (!isReplaying || sessionData == null || sessionData.frames == null || sessionData.frames.Count == 0)
        {
            return;
        }

        replayPlaybackTime += Time.deltaTime;
        ProcessAudioEvents();

        if (replayPlaybackTime < sessionData.spawnOffset)
        {
            SetRenderersEnabled(false);
            return;
        }

        if (sessionData.destroyTime != -1f && replayPlaybackTime >= sessionData.destroyTime)
        {
            SetRenderersEnabled(false);
            return;
        }

        SetRenderersEnabled(true);

        while (currentFrameIndex < sessionData.frames.Count - 2 &&
               sessionData.frames[currentFrameIndex + 1].frameTime < replayPlaybackTime)
        {
            currentFrameIndex++;
        }

        if (currentFrameIndex < sessionData.frames.Count - 1)
        {
            ReplayFrame frameA = sessionData.frames[currentFrameIndex];
            ReplayFrame frameB = sessionData.frames[currentFrameIndex + 1];
            float timeDiff = frameB.frameTime - frameA.frameTime;
            float percent = timeDiff > 0.0001f
                ? Mathf.Clamp01((replayPlaybackTime - frameA.frameTime) / timeDiff)
                : 0f;
            ApplyFrameInterpolated(frameA, frameB, percent);
        }
        else
        {
            ReplayFrame lastFrame = sessionData.frames[sessionData.frames.Count - 1];
            ApplyFrameInterpolated(lastFrame, lastFrame, 1f);
        }
    }

    void RecordFrame()
    {
        if (sessionData == null || ReplayManager.instance == null) return;

        ReplayFrame frame = new ReplayFrame
        {
            frameTime = ReplayManager.instance.currentRecordedTime,
            position = ReplayManager.instance.WorldToRecordedPosition(transform.position),
            rotation = transform.rotation
        };

        if (extraTransforms != null && extraTransforms.Length > 0)
        {
            frame.extraRotations = new Quaternion[extraTransforms.Length];
            for (int i = 0; i < extraTransforms.Length; i++)
            {
                Transform t = extraTransforms[i];
                frame.extraRotations[i] = t != null ? t.localRotation : Quaternion.identity;
            }
        }

        if (anim != null)
        {
            frame.isAnimEnabled = anim.enabled;
            frame.animatorSnapshot = ReplayAnimatorUtility.Capture(anim);

            if (boolParams != null && boolParams.Length > 0)
            {
                MergeLegacyBoolParams(ref frame);
            }
        }

        sessionData.frames.Add(frame);
    }

    private void MergeLegacyBoolParams(ref ReplayFrame frame)
    {
        if (frame.animatorSnapshot == null)
        {
            frame.animatorSnapshot = new ReplayAnimatorSnapshot();
        }

        var boolNames = new List<string>(frame.animatorSnapshot.boolNames ?? System.Array.Empty<string>());
        var boolValues = new List<bool>(frame.animatorSnapshot.boolValues ?? System.Array.Empty<bool>());

        foreach (string param in boolParams)
        {
            if (string.IsNullOrEmpty(param) || boolNames.Contains(param)) continue;
            boolNames.Add(param);
            boolValues.Add(anim.GetBool(param));
        }

        frame.animatorSnapshot.boolNames = boolNames.ToArray();
        frame.animatorSnapshot.boolValues = boolValues.ToArray();
    }

    void ApplyFrameInterpolated(ReplayFrame frameA, ReplayFrame frameB, float percent)
    {
        if (!isActiveAndEnabled || transform == null) return;

        Vector3 recordedPosition = Vector3.Lerp(frameA.position, frameB.position, percent);
        if (ReplayManager.instance != null)
        {
            transform.position = ReplayManager.instance.RecordedToWorldPosition(recordedPosition);
        }
        else
        {
            transform.position = recordedPosition;
        }

        transform.rotation = Quaternion.Slerp(frameA.rotation, frameB.rotation, percent);

        ApplyExtraRotations(frameA, frameB, percent);

        if (anim != null)
        {
            anim.enabled = frameA.isAnimEnabled;
            ReplayAnimatorSnapshot snapshot = percent < 0.5f ? frameA.animatorSnapshot : frameB.animatorSnapshot;
            ReplayAnimatorUtility.Apply(anim, snapshot);
        }
    }

    private void ApplyExtraRotations(ReplayFrame frameA, ReplayFrame frameB, float percent)
    {
        if (extraTransforms == null || extraTransforms.Length == 0) return;
        if (frameA.extraRotations == null || frameB.extraRotations == null) return;

        for (int i = 0; i < extraTransforms.Length; i++)
        {
            Transform t = extraTransforms[i];
            if (t == null) continue;
            if (i >= frameA.extraRotations.Length || i >= frameB.extraRotations.Length) continue;

            t.localRotation = Quaternion.Slerp(frameA.extraRotations[i], frameB.extraRotations[i], percent);
        }
    }

    private void SetRenderersEnabled(bool state)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>(true);
        foreach (Renderer renderer in renderers)
        {
            if (renderer == null || renderer.GetComponent<Camera>() != null) continue;
            renderer.enabled = state;
        }
    }

    private void ProcessAudioEvents()
    {
        if (sessionData.audioEvents == null || sessionData.audioEvents.Count == 0 || replayAudioSource == null)
        {
            return;
        }

        while (nextAudioEventIndex < sessionData.audioEvents.Count &&
               sessionData.audioEvents[nextAudioEventIndex].time <= replayPlaybackTime)
        {
            ReplayAudioEvent audioEvent = sessionData.audioEvents[nextAudioEventIndex];
            AudioClip clip = ResolveAudioClip(audioEvent.clipName);
            if (clip != null)
            {
                replayAudioSource.pitch = audioEvent.pitch;
                replayAudioSource.PlayOneShot(clip, audioEvent.volume);
            }
            nextAudioEventIndex++;
        }
    }

    private AudioClip ResolveAudioClip(string clipName)
    {
        if (string.IsNullOrEmpty(clipName)) return null;
        if (audioClipCache.TryGetValue(clipName, out AudioClip cached)) return cached;

        AudioClip[] clips = Resources.FindObjectsOfTypeAll<AudioClip>();
        foreach (AudioClip clip in clips)
        {
            if (clip.name == clipName)
            {
                audioClipCache[clipName] = clip;
                return clip;
            }
        }

        return null;
    }

    public void RegisterAudioEvent(string clipName, float volume, float pitch)
    {
        if (!isRecording || sessionData == null || ReplayManager.instance == null) return;

        sessionData.audioEvents.Add(new ReplayAudioEvent
        {
            time = ReplayManager.instance.currentRecordedTime,
            clipName = clipName,
            volume = volume,
            pitch = pitch
        });
    }

    public void AutoConfigureComponents()
    {
        if (anim == null) anim = GetComponentInChildren<Animator>();
        if (rb == null) rb = GetComponent<Rigidbody>();
        if (agent == null) agent = GetComponent<NavMeshAgent>();
    }

    public static string ResolveCatalogId(GameObject go)
    {
        if (go == null) return string.Empty;
        if (go.CompareTag("Player")) return "Player";

        if (go.GetComponent<SoldierBrain>() != null) return "SoldierAgent";
        if (go.GetComponent<DroneBrain>() != null) return "DroneAgent";
        if (go.GetComponent<TankBrain>() != null) return "TankAgent";

        string cleanedName = go.name.Replace("(Clone)", "").Trim();
        if (cleanedName.Contains("Bullet")) return cleanedName.Contains("Small") ? "SmallBullet" : "Bullet";

        return cleanedName;
    }

    public void StartRecording(float offset = 0f)
    {
        if (isRecording)
        {
            return;
        }

        AutoConfigureComponents();

        if (sessionData == null)
        {
            sessionData = new ReplaySessionData
            {
                prefabName = gameObject.name.Replace("(Clone)", "").Trim(),
                catalogId = ResolveCatalogId(gameObject),
                sourceInstanceId = gameObject.GetInstanceID(),
                objectTag = gameObject.tag,
                spawnOffset = offset,
                isPlayer = gameObject.CompareTag("Player")
            };
        }

        isRecording = true;
        isReplaying = false;
        timeTimer = 0f;
    }

    public bool IsCurrentlyRecording => isRecording;

    public void PauseRecording() => isRecording = false;
    public void ResumeRecording() => isRecording = true;

    public void StopRecording()
    {
        isRecording = false;
    }

    public ReplaySessionData GetSessionData()
    {
        return sessionData;
    }

    public void RecordDeath()
    {
        if (isRecording && sessionData != null && ReplayManager.instance != null)
        {
            sessionData.destroyTime = ReplayManager.instance.currentRecordedTime;
            StopRecording();
        }
    }

    private void OnDisable()
    {
        if (isReplaying)
        {
            isReplaying = false;
            return;
        }

        if (isRecording)
        {
            RecordDeath();
        }
    }

    private void OnDestroy()
    {
        if (isReplaying)
        {
            isReplaying = false;
            return;
        }

        if (isRecording)
        {
            RecordDeath();
        }
    }

    public void SetupForReplay(ReplaySessionData data)
    {
        sessionData = data;
    }

    public void StartReplay()
    {
        isRecording = false;
        isReplaying = true;
        currentFrameIndex = 0;
        replayPlaybackTime = 0f;
        nextAudioEventIndex = 0;

        replayAudioSource = GetComponent<AudioSource>();
        if (replayAudioSource == null)
        {
            replayAudioSource = gameObject.AddComponent<AudioSource>();
        }
        replayAudioSource.playOnAwake = false;
        replayAudioSource.spatialBlend = 0f;

        ApplyFirstReplayFrame();

        if (sessionData != null && sessionData.spawnOffset > 0f && replayPlaybackTime < sessionData.spawnOffset)
        {
            SetRenderersEnabled(false);
        }
        else
        {
            SetRenderersEnabled(true);
        }
    }

    private void ApplyFirstReplayFrame()
    {
        if (sessionData == null || sessionData.frames == null || sessionData.frames.Count == 0)
        {
            return;
        }

        replayPlaybackTime = Mathf.Max(0f, sessionData.spawnOffset);
        currentFrameIndex = 0;
        ReplayFrame firstFrame = sessionData.frames[0];
        ApplyFrameInterpolated(firstFrame, firstFrame, 0f);
    }

    public void StopReplayPlayback()
    {
        isReplaying = false;
        if (replayAudioSource != null)
        {
            replayAudioSource.Stop();
        }
    }
}
