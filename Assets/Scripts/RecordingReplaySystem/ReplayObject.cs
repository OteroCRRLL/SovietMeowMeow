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

    public Transform[] extraTransforms; // Children with independent behaviours

    [Header("Animations to record")]
    public string[] boolParams;

    private List<ReplayFrame> frames = new List<ReplayFrame>();
    private bool isRecording = false;
    private bool isReplaying = false;
    private float timeTimer = 0;
    private int playbackIndex = 0;

    // NUEVAS VARIABLES PARA EL TIMING
    private float spawnOffset = 0f;       // Cuánto tardó en aparecer este objeto
    private float replayGlobalTimer = 0f; // Temporizador global de la replay actual
    private bool isWaitingToSpawn = false;

    private void Update()
    {
        if (isRecording)
        {
            // Only record each interval
            timeTimer += Time.deltaTime;
            if (timeTimer >= recordInterval)
            {
                RecordFrame();
                timeTimer = 0;
            }
        }
        else if (isReplaying)
        {
            // NUEVO: Lógica de espera por offset
            replayGlobalTimer += Time.deltaTime;

            // Si aún no ha pasado el tiempo de offset, mantenemos el objeto oculto y salimos
            if (replayGlobalTimer < spawnOffset)
            {
                if (!isWaitingToSpawn)
                {
                    ToggleVisuals(false); // Ocultar
                    isWaitingToSpawn = true;
                }
                return; // No ejecutamos interpolación todavía
            }

            // Si ya pasó el tiempo, lo mostramos
            if (isWaitingToSpawn)
            {
                ToggleVisuals(true); // Mostrar
                isWaitingToSpawn = false;
            }

            // --- Lógica normal de reproducción ---
            timeTimer += Time.deltaTime;
            if (timeTimer >= recordInterval)
            {
                playbackIndex++;
                timeTimer = 0;
            }

            if (playbackIndex < frames.Count - 1)
            {
                float lerpPercent = timeTimer / recordInterval;
                ApplyFrameInterpolated(frames[playbackIndex], frames[playbackIndex + 1], lerpPercent);
            }
        }
    }

    void RecordFrame()
    {
        ReplayFrame f = new ReplayFrame();

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

            // Save bools
            f.boolValues = new bool[boolParams.Length];
            for (int i = 0; i < boolParams.Length; i++)
            {
                f.boolValues[i] = anim.GetBool(boolParams[i]);
            }
        }

        frames.Add(f);
    }

    // Reproduce
    void ApplyFrameInterpolated(ReplayFrame frameA, ReplayFrame frameB, float percent)
    {
        // 1. Smooth transform
        transform.position = Vector3.Lerp(frameA.position, frameB.position, percent);
        transform.rotation = Quaternion.Slerp(frameA.rotation, frameB.rotation, percent);

        // 2. Extras
        if (frameA.extraRotations != null && extraTransforms.Length > 0)
        {
            for (int i = 0; i < extraTransforms.Length; i++)
            {
                if (extraTransforms[i] != null)
                {
                    extraTransforms[i].localRotation = Quaternion.Slerp(frameA.extraRotations[i], frameB.extraRotations[i], percent);
                }
            }
        }

        // 3. Animations
        if (anim != null)
        {
            anim.enabled = frameA.isAnimEnabled; // Turns On/off animator

            for (int i = 0; i < boolParams.Length; i++)
            {
                anim.SetBool(boolParams[i], frameA.boolValues[i]);
            }
        }
    }

    // NUEVO: Helper para ocultar visualmente el objeto sin desactivar el script
    // (Si desactivamos el GameObject entero, el Update deja de correr y no puede contar el tiempo)
    void ToggleVisuals(bool state)
    {
        // Renderers (Meshes, SkinnedMeshes)
        Renderer[] renderers = GetComponentsInChildren<Renderer>();
        foreach (var r in renderers) r.enabled = state;

        // UI Canvases (barras de vida, etc)
        Canvas[] canvases = GetComponentsInChildren<Canvas>();
        foreach (var c in canvases) c.enabled = state;

        // Animador (para evitar glitches visuales)
        if (anim != null) anim.enabled = state;
    }

    // Controls
    // MODIFICADO: Ahora acepta el offset
    public void StartRecording(float offset = 0f)
    {
        frames.Clear();
        spawnOffset = offset; // Guardamos el offset
        isRecording = true;
        isReplaying = false;
        timeTimer = 0;
    }

    public void StartReplay()
    {
        isRecording = false;
        isReplaying = true;
        playbackIndex = 0;
        timeTimer = 0;

        // Reiniciamos contadores de offset
        replayGlobalTimer = 0;
        isWaitingToSpawn = false;

        // Disable physics/navmesh
        if (rb) rb.isKinematic = true;
        if (agent) agent.enabled = false;

        // Disable control scripts
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this && script != anim)
            {
                script.enabled = false;
            }
        }

        // Si tiene offset, ocultamos visualmente al inicio
        if (spawnOffset > 0)
        {
            ToggleVisuals(false);
            isWaitingToSpawn = true;
        }
    }

    public void StopReplay()
    {
        isReplaying = false;
        isWaitingToSpawn = false;

        // Aseguramos que se vea al terminar
        ToggleVisuals(true);

        if (rb) rb.isKinematic = false;
        if (agent) agent.enabled = true;

        // Reactivate control scripts
        MonoBehaviour[] scripts = GetComponents<MonoBehaviour>();
        foreach (var script in scripts)
        {
            if (script != this)
            {
                script.enabled = true;
            }
        }
    }
    private void Start()
    {
        ReplayManager manager = FindObjectOfType<ReplayManager>();
        if (manager != null)
        {
            manager.RegisterObject(this);
        }
    }
}