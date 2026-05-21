using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ReplayAppManager : MonoBehaviour
{
    [Header("UI Pantallas de Cámara")]
    public RawImage forwardCameraImage;
    public RawImage reverseCameraImage;

    [Header("Resolución de Render")]
    public int renderWidth = 1024;
    public int renderHeight = 1024;

    [Header("Selector de día (opcional)")]
    public TextMeshProUGUI dayLabel;
    public Button previousDayButton;
    public Button nextDayButton;

    private RenderTexture forwardRT;
    private RenderTexture reverseRT;
    private Camera forwardCloneCam;
    private Camera reverseCloneCam;
    private int currentPlaybackDay = -1;
    private bool isPlaying;

    private void Awake()
    {
        if (previousDayButton != null)
        {
            previousDayButton.onClick.RemoveListener(ShowPreviousDay);
            previousDayButton.onClick.AddListener(ShowPreviousDay);
        }

        if (nextDayButton != null)
        {
            nextDayButton.onClick.RemoveListener(ShowNextDay);
            nextDayButton.onClick.AddListener(ShowNextDay);
        }
    }

    public void OnEnable()
    {
        if (ReplayManager.instance == null)
        {
            Debug.LogWarning("ReplayManager no disponible. Entra al nivel al menos una vez o asegúrate de que persiste entre escenas.");
            return;
        }

        currentPlaybackDay = ReplayManager.instance.GetDefaultPlaybackDay();
        ReplayManager.instance.SetPlaybackDay(currentPlaybackDay);
        UpdateDayLabel();
    }

    public void OnDisable()
    {
        StopReplay();
    }

    public void ShowPreviousDay()
    {
        if (ReplayManager.instance == null) return;
        StopReplay();
        currentPlaybackDay = ReplayManager.instance.CyclePlaybackDay(-1);
        ReplayManager.instance.SetPlaybackDay(currentPlaybackDay);
        UpdateDayLabel();
        PlayReplay();
    }

    public void ShowNextDay()
    {
        if (ReplayManager.instance == null) return;
        StopReplay();
        currentPlaybackDay = ReplayManager.instance.CyclePlaybackDay(1);
        ReplayManager.instance.SetPlaybackDay(currentPlaybackDay);
        UpdateDayLabel();
        PlayReplay();
    }

    public void PlayReplay()
    {
        if (ReplayManager.instance == null) return;

        if (currentPlaybackDay < 0)
        {
            currentPlaybackDay = ReplayManager.instance.GetDefaultPlaybackDay();
            ReplayManager.instance.SetPlaybackDay(currentPlaybackDay);
        }

        if (!ReplayManager.instance.HasPlaybackData(currentPlaybackDay))
        {
            Debug.LogWarning($"No hay replay guardado para el día {currentPlaybackDay}.");
            UpdateDayLabel();
            return;
        }

        StopReplay();
        StartCoroutine(StartReplayRoutine());
    }

    private IEnumerator StartReplayRoutine()
    {
        CreateRenderTextures();
        BindTexturesToUI();

        ReplayManager.instance.StartPlaybackForDay(currentPlaybackDay);
        UpdateDayLabel();

        yield return null;
        yield return new WaitForEndOfFrame();

        GameObject playerClone = ReplayManager.instance.GetPlayerClone();
        if (playerClone == null)
        {
            Debug.LogError("No se instanció el clon del Player durante el Replay. Revisa los prefabs del ReplayManager.");
            yield break;
        }

        bool camerasReady = ReplayPlaybackUtility.TryConfigureReplayCameras(
            playerClone, forwardRT, reverseRT, out forwardCloneCam, out reverseCloneCam);

        if (!camerasReady)
        {
            Debug.LogWarning(
                "No se pudieron configurar ambas cámaras del replay. Revisa CameraPivot/ForwardCamera/ReverseCamera en el prefab del jugador.");
        }

        isPlaying = true;
    }

    public void StopReplay()
    {
        isPlaying = false;

        if (ReplayManager.instance != null)
        {
            ReplayManager.instance.StopPlayback();
        }

        ReleaseCameraTargets();
        CleanupRenderTextures();
        ClearUIImageTextures();
    }

    private void CreateRenderTextures()
    {
        CleanupRenderTextures();

        forwardRT = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
        reverseRT = new RenderTexture(renderWidth, renderHeight, 24, RenderTextureFormat.ARGB32);
        forwardRT.Create();
        reverseRT.Create();
    }

    private void BindTexturesToUI()
    {
        if (forwardCameraImage != null)
        {
            forwardCameraImage.enabled = true;
            forwardCameraImage.raycastTarget = true;
            forwardCameraImage.texture = forwardRT;
            forwardCameraImage.color = Color.white;
            forwardCameraImage.gameObject.SetActive(true);
            forwardCameraImage.SetMaterialDirty();
        }

        if (reverseCameraImage != null)
        {
            reverseCameraImage.enabled = true;
            reverseCameraImage.raycastTarget = true;
            reverseCameraImage.texture = reverseRT;
            reverseCameraImage.color = Color.white;
            reverseCameraImage.gameObject.SetActive(true);
            reverseCameraImage.SetMaterialDirty();
        }
    }

    private void ReleaseCameraTargets()
    {
        if (forwardCloneCam != null)
        {
            forwardCloneCam.targetTexture = null;
            forwardCloneCam = null;
        }

        if (reverseCloneCam != null)
        {
            reverseCloneCam.targetTexture = null;
            reverseCloneCam = null;
        }
    }

    private void ClearUIImageTextures()
    {
        if (forwardCameraImage != null)
        {
            forwardCameraImage.texture = null;
        }

        if (reverseCameraImage != null)
        {
            reverseCameraImage.texture = null;
        }
    }

    private void UpdateDayLabel()
    {
        if (dayLabel == null) return;

        if (ReplayManager.instance == null || !ReplayManager.instance.HasAnyPlaybackData())
        {
            dayLabel.text = "Sin grabaciones";
            return;
        }

        dayLabel.text = $"Día {currentPlaybackDay}";
    }

    private void CleanupRenderTextures()
    {
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
