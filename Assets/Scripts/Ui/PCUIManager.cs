using UnityEngine;
using UnityEngine.InputSystem;
using System.Collections;

public class PCUIManager : MonoBehaviour
{
    [Header("UI Panels")]
    [Tooltip("El Canvas del PC (Debe estar en World Space y situado en la pantalla del modelo 3D)")]
    public GameObject pcCanvas;
    public GameObject desktopPanel;
    public GameObject[] appPanels;

    [Header("Camera Settings")]
    [Tooltip("Velocidad de transición de la cámara. Pon un número alto (ej. 100) para que sea instantáneo.")]
    public float cameraTransitionSpeed = 5f;

    private GameObject currentPlayer;
    private MonoBehaviour playerController;
    private MonoBehaviour cameraController;
    private MonoBehaviour playerInteraction;
    private Camera playerCamera;

    // Guardar posición original de la cámara
    private Transform originalCameraParent;
    private Vector3 originalCameraLocalPosition;
    private Quaternion originalCameraLocalRotation;

    private bool isPCOpen = false;
    private bool isTransitioning = false;

    private void Start()
    {
        // El Canvas del PC puede estar encendido siempre si quieres que se vea la pantalla aunque no estés usándolo,
        // pero por ahora lo ocultaremos hasta que interactúes con él, según el diseño inicial.
        if (pcCanvas != null)
        {
            pcCanvas.SetActive(false);
        }
    }

    private void Update()
    {
        if (isPCOpen && !isTransitioning && Keyboard.current.escapeKey.wasPressedThisFrame)
        {
            ClosePC();
        }
    }

    public void OpenPC(GameObject playerObj, Transform pcCameraTarget)
    {
        if (isTransitioning) return;

        currentPlayer = playerObj;
        
        // Obtener scripts
        playerController = currentPlayer.GetComponent("PlayerController") as MonoBehaviour;
        cameraController = currentPlayer.GetComponentInChildren<CameraController>();
        playerInteraction = currentPlayer.GetComponent("PlayerInteraction") as MonoBehaviour;

        if (cameraController != null)
        {
            playerCamera = currentPlayer.GetComponentInChildren<Camera>();
            
            if (playerCamera == null)
            {
                playerCamera = Camera.main;
            }

            Debug.Log("PCUIManager: Cámara encontrada: " + (playerCamera != null ? playerCamera.gameObject.name : "NULA"));
            
            // Asignar la cámara del jugador como Event Camera del Canvas dinámicamente
            if (playerCamera != null && pcCanvas != null)
            {
                Canvas canvasComponent = pcCanvas.GetComponent<Canvas>();
                if (canvasComponent != null)
                {
                    canvasComponent.worldCamera = playerCamera;
                }
            }
        }

        // Desactivar controles
        if (playerController != null) playerController.enabled = false;
        if (cameraController != null) cameraController.enabled = false;
        if (playerInteraction != null) playerInteraction.enabled = false;

        // Iniciar transición de la cámara
        if (playerCamera != null)
        {
            StartCoroutine(TransitionCameraToPC(pcCameraTarget));
        }
        else
        {
            // Si por algún motivo no hay cámara, abrir la UI directamente
            FinishOpeningPC();
        }
    }

    private IEnumerator TransitionCameraToPC(Transform targetTransform)
    {
        isTransitioning = true;

        // Guardar estado original de la cámara
        originalCameraParent = playerCamera.transform.parent;
        originalCameraLocalPosition = playerCamera.transform.localPosition;
        originalCameraLocalRotation = playerCamera.transform.localRotation;

        // Desemparentar la cámara del jugador momentáneamente para moverla libremente
        playerCamera.transform.SetParent(null);

        // Asegurar que el objeto destino está activo para que la cámara no se desactive al emparentarla
        targetTransform.gameObject.SetActive(true);
        
        // Mover hacia la cámara del PC suavemente
        while (Vector3.Distance(playerCamera.transform.position, targetTransform.position) > 0.01f ||
               Quaternion.Angle(playerCamera.transform.rotation, targetTransform.rotation) > 0.1f)
        {
            playerCamera.transform.position = Vector3.Lerp(playerCamera.transform.position, targetTransform.position, Time.deltaTime * cameraTransitionSpeed);
            playerCamera.transform.rotation = Quaternion.Slerp(playerCamera.transform.rotation, targetTransform.rotation, Time.deltaTime * cameraTransitionSpeed);
            yield return null;
        }

        // Asegurar que está exactamente en la posición final y emparentarla al PC para que siga a la pantalla si el PC se moviera
        playerCamera.transform.position = targetTransform.position;
        playerCamera.transform.rotation = targetTransform.rotation;
        playerCamera.transform.SetParent(targetTransform);

        FinishOpeningPC();
    }

    private void FinishOpeningPC()
    {
        isTransitioning = false;
        isPCOpen = true;

        Cursor.lockState = CursorLockMode.None;
        Cursor.visible = true;

        if (pcCanvas != null) pcCanvas.SetActive(true);
        ShowDesktop();
    }

    public void ClosePC()
    {
        if (isTransitioning) return;

        isPCOpen = false;
        if (pcCanvas != null) pcCanvas.SetActive(false); // Opcional: podrías dejar el Canvas encendido si quieres que la pantalla siga "viva" al alejarte.

        Cursor.lockState = CursorLockMode.Locked;
        Cursor.visible = false;

        if (playerCamera != null && originalCameraParent != null)
        {
            StartCoroutine(TransitionCameraBack());
        }
        else
        {
            FinishClosingPC();
        }
    }

    private IEnumerator TransitionCameraBack()
    {
        isTransitioning = true;

        // Desemparentar del PC
        playerCamera.transform.SetParent(originalCameraParent);

        // La posición objetivo ahora es su posición local original dentro del jugador
        Vector3 targetLocalPos = originalCameraLocalPosition;
        Quaternion targetLocalRot = originalCameraLocalRotation;

        while (Vector3.Distance(playerCamera.transform.localPosition, targetLocalPos) > 0.01f ||
               Quaternion.Angle(playerCamera.transform.localRotation, targetLocalRot) > 0.1f)
        {
            playerCamera.transform.localPosition = Vector3.Lerp(playerCamera.transform.localPosition, targetLocalPos, Time.deltaTime * cameraTransitionSpeed);
            playerCamera.transform.localRotation = Quaternion.Slerp(playerCamera.transform.localRotation, targetLocalRot, Time.deltaTime * cameraTransitionSpeed);
            yield return null;
        }

        playerCamera.transform.localPosition = targetLocalPos;
        playerCamera.transform.localRotation = targetLocalRot;

        FinishClosingPC();
    }

    private void FinishClosingPC()
    {
        isTransitioning = false;

        // Reactivar controles
        if (playerController != null) playerController.enabled = true;
        if (cameraController != null) cameraController.enabled = true;
        
        // Retraso para evitar interactuar en el mismo frame
        Invoke(nameof(ReenableInteraction), 0.1f);
    }

    private void ReenableInteraction()
    {
        if (playerInteraction != null) playerInteraction.enabled = true;
    }

    public void ShowDesktop()
    {
        if (desktopPanel != null) desktopPanel.SetActive(true);
        
        foreach (var app in appPanels)
        {
            if (app != null) app.SetActive(false);
        }
    }

    public void OpenApp(GameObject appToOpen)
    {
        foreach (var app in appPanels)
        {
            if (app != null) app.SetActive(false);
        }

        if (appToOpen != null)
        {
            appToOpen.SetActive(true);
        }
    }
}