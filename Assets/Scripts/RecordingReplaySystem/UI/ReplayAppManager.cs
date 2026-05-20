using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ReplayAppManager : MonoBehaviour
{
    [Header("UI Pantallas de Cámara")]
    public RawImage forwardCameraImage;
    public RawImage reverseCameraImage;

    [Header("Resolución de Render")]
    public int renderWidth = 1024;
    public int renderHeight = 1024;

    private RenderTexture forwardRT;
    private RenderTexture reverseRT;

    private Camera forwardCloneCam;
    private Camera reverseCloneCam;

    public void OnEnable()
    {
        // Al abrir la app del PC, si no hay grabaciones, podríamos mostrar un mensaje,
        // pero por ahora simplemente empezamos la reproducción si hay algo.
        if (ReplayManager.instance != null && ReplayManager.instance.recordedSessions.Count > 0)
        {
            PlayReplay();
        }
        else
        {
            Debug.LogWarning("No hay datos de Replay para reproducir.");
        }
    }

    public void OnDisable()
    {
        // Al cerrar la app o salir del PC, detenemos el replay y limpiamos memorias
        StopReplay();
    }

    public void PlayReplay()
    {
        if (ReplayManager.instance == null) return;

        // Limpiar RT anteriores si los hay
        CleanupRenderTextures();

        // Crear nuevas RenderTextures
        forwardRT = new RenderTexture(renderWidth, renderHeight, 24);
        reverseRT = new RenderTexture(renderWidth, renderHeight, 24);

        // Asignar al UI
        if (forwardCameraImage != null) forwardCameraImage.texture = forwardRT;
        if (reverseCameraImage != null) reverseCameraImage.texture = reverseRT;

        // Iniciar la reproducción instanciando clones
        ReplayManager.instance.StartPlaybackFromData();

        // Buscar las cámaras en el clon del jugador
        GameObject playerClone = ReplayManager.instance.GetPlayerClone();
        if (playerClone != null)
        {
            Camera[] cams = playerClone.GetComponentsInChildren<Camera>(true);
            foreach (Camera cam in cams)
            {
                if (cam.gameObject.name.Contains("ForwardCamera"))
                {
                    forwardCloneCam = cam;
                    forwardCloneCam.targetTexture = forwardRT;
                    // Evitar que renderice audio si tuviera un listener extra
                    AudioListener al = cam.GetComponent<AudioListener>();
                    if (al) al.enabled = false;
                }
                else if (cam.gameObject.name.Contains("ReverseCamera"))
                {
                    reverseCloneCam = cam;
                    reverseCloneCam.targetTexture = reverseRT;
                    AudioListener al = cam.GetComponent<AudioListener>();
                    if (al) al.enabled = false;
                }
                else
                {
                    // Desactivar cualquier otra cámara del clon para evitar conflictos de pantalla
                    cam.enabled = false;
                }
            }

            if (forwardCloneCam == null) Debug.LogWarning("No se encontró ForwardCamera en el clon del Player");
            if (reverseCloneCam == null) Debug.LogWarning("No se encontró ReverseCamera en el clon del Player");
        }
        else
        {
            Debug.LogError("No se instanció el clon del Player durante el Replay. Revisa tus Prefabs en el ReplayManager.");
        }
    }

    public void StopReplay()
    {
        if (ReplayManager.instance != null)
        {
            ReplayManager.instance.StopPlayback();
        }

        CleanupRenderTextures();

        if (forwardCameraImage != null) forwardCameraImage.texture = null;
        if (reverseCameraImage != null) reverseCameraImage.texture = null;
    }

    private void CleanupRenderTextures()
    {
        if (forwardCloneCam != null) forwardCloneCam.targetTexture = null;
        if (reverseCloneCam != null) reverseCloneCam.targetTexture = null;

        if (forwardRT != null)
        {
            forwardRT.Release();
            Destroy(forwardRT);
            forwardRT = null;
        }

        if (reverseRT != null)
        {
            reverseRT.Release();
            Destroy(reverseRT);
            reverseRT = null;
        }
    }
}
