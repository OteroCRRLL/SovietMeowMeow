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

    [Header("Animations to record")]
    public string[] boolParams;

    private ReplaySessionData sessionData;
    private bool isRecording = false;
    private bool isReplaying = false;
    private float timeTimer = 0;
    
    // Timer para la reproducción de los clones
    private float replayPlaybackTime = 0f;
    private int currentFrameIndex = 0;

    private void Start()
    {
        if (ReplayManager.instance != null && !isReplaying)
        {
            ReplayManager.instance.RegisterObject(this);
        }
    }

    private void Update()
    {
        if (isRecording)
        {
            timeTimer += Time.deltaTime;
            if (timeTimer >= recordInterval)
            {
                RecordFrame();
                timeTimer = 0;
            }
        }
        else if (isReplaying && sessionData != null && sessionData.frames.Count > 0)
        {
            replayPlaybackTime += Time.deltaTime;

            // Esperar al momento en que spawneó
            if (replayPlaybackTime < sessionData.spawnOffset)
            {
                ToggleVisuals(false);
                return;
            }

            // Ocultar si ya se destruyó
            if (sessionData.destroyTime != -1f && replayPlaybackTime >= sessionData.destroyTime)
            {
                ToggleVisuals(false);
                return;
            }

            ToggleVisuals(true);

            // Buscar los dos frames entre los que estamos
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
                float percent = 0f;
                if (timeDiff > 0.0001f)
                {
                    percent = (replayPlaybackTime - frameA.frameTime) / timeDiff;
                }

                ApplyFrameInterpolated(frameA, frameB, Mathf.Clamp01(percent));
            }
            else
            {
                // Mantenemos el último frame
                ApplyFrameInterpolated(sessionData.frames[sessionData.frames.Count - 1], sessionData.frames[sessionData.frames.Count - 1], 1f);
            }
        }
    }

    void RecordFrame()
    {
        if (sessionData == null) return;

        ReplayFrame f = new ReplayFrame();

        // Guardamos el tiempo real de la grabación (saltando pausas)
        f.frameTime = ReplayManager.instance.currentRecordedTime;

        // 1. Transform
        f.position = transform.position;
        f.rotation = transform.rotation;

        // 2. Extras
        if (extraTransforms.Length > 0)
        {
            f.extraRotations = new Quaternion[extraTransforms.Length];
            for (int i = 0; i < extraTransforms.Length; i++)
            {
                if (extraTransforms[i] != null)
                {
                    f.extraRotations[i] = extraTransforms[i].localRotation;
                }
            }
        }

        // 3. Animations
        if (anim != null)
        {
            f.isAnimEnabled = anim.enabled;

            f.boolValues = new bool[boolParams.Length];
            for (int i = 0; i < boolParams.Length; i++)
            {
                f.boolValues[i] = anim.GetBool(boolParams[i]);
            }
        }

        sessionData.frames.Add(f);
    }

    void ApplyFrameInterpolated(ReplayFrame frameA, ReplayFrame frameB, float percent)
    {
        Vector3 offset = Vector3.zero;
        if (ReplayManager.instance != null)
        {
            offset = ReplayManager.instance.replayOffset;
        }

        transform.position = offset + Vector3.Lerp(frameA.position, frameB.position, percent);
        transform.rotation = Quaternion.Slerp(frameA.rotation, frameB.rotation, percent);

        if (frameA.extraRotations != null && extraTransforms.Length > 0)
        {
            for (int i = 0; i < extraTransforms.Length; i++)
            {
                if (extraTransforms[i] != null && i < frameB.extraRotations.Length)
                {
                    extraTransforms[i].localRotation = Quaternion.Slerp(frameA.extraRotations[i], frameB.extraRotations[i], percent);
                }
            }
        }

        if (anim != null)
        {
            anim.enabled = frameA.isAnimEnabled;

            if (frameA.boolValues != null)
            {
                for (int i = 0; i < boolParams.Length; i++)
                {
                    if (i < frameA.boolValues.Length)
                    {
                        anim.SetBool(boolParams[i], frameA.boolValues[i]);
                    }
                }
            }
        }
    }

    void ToggleVisuals(bool state)
    {
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = state;

        Canvas[] canvases = GetComponentsInChildren<Canvas>();
        foreach (var c in canvases) c.enabled = state;

        if (anim != null) anim.enabled = state;
    }

    // Controls for Recording
    public void StartRecording(float offset = 0f)
    {
        sessionData = new ReplaySessionData();
        
        // Tratar de limpiar el nombre de "(Clone)"
        sessionData.prefabName = gameObject.name.Replace("(Clone)", "").Trim();
        sessionData.objectTag = gameObject.tag;
        sessionData.spawnOffset = offset;
        sessionData.isPlayer = gameObject.CompareTag("Player");

        isRecording = true;
        isReplaying = false;
        timeTimer = 0;
    }

    public void PauseRecording()
    {
        isRecording = false;
    }

    public void ResumeRecording()
    {
        isRecording = true;
    }

    public void StopRecording()
    {
        isRecording = false;
        
        // Guardar los datos en el ReplayManager
        if (sessionData != null && sessionData.frames.Count > 0)
        {
            if (ReplayManager.instance != null)
            {
                ReplayManager.instance.SaveSessionData(sessionData);
            }
        }
    }

    // Llamar cuando el objeto muere pero no debe destruirse todavía para el replay
    public void RecordDeath()
    {
        if (isRecording && sessionData != null)
        {
            sessionData.destroyTime = ReplayManager.instance.currentRecordedTime;
            StopRecording();
        }
    }

    private void OnDisable()
    {
        if (isRecording)
        {
            RecordDeath();
        }
    }
    
    private void OnDestroy()
    {
        if (isRecording)
        {
            RecordDeath();
        }
    }

    // Controls for Replay (clones)
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

        if (rb) rb.isKinematic = true;
        if (agent) agent.enabled = false;

        // Desactivar scripts de control para que no interfieran en el replay
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this && script != anim)
            {
                script.enabled = false;
            }
        }
        
        // Desactivar colisiones si las hay
        Collider[] colliders = GetComponentsInChildren<Collider>();
        foreach(var col in colliders)
        {
            col.enabled = false;
        }

        if (sessionData != null && sessionData.spawnOffset > 0)
        {
            ToggleVisuals(false);
        }

        AudioListener listener = GetComponentInChildren<AudioListener>();
        if (listener != null)
        {
            listener.enabled = false;
        }
    }
}